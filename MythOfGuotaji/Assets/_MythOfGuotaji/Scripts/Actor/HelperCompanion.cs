using System.Collections.Generic;
using MythOfGuotaji.Data;
using MythOfGuotaji.Progression;
using UnityEngine;

namespace MythOfGuotaji.Actor
{
    public enum HelperState
    {
        FollowingPlayer,   // 캐릭터 뒤를 호위/추종
        GatheringGems,     // 요괴가 없는 안전 구역의 보석 수색 및 수집
        FleeingToPlayer,   // 요괴 접근 시 플레이어 뒤로 긴급 대피
        PresentingSkill    // 보석 정제 완료 후 캐릭터에게 스킬 오브젝트 제시
    }

    /// <summary>
    /// 공격력 없이 캐릭터 뒤를 따르며, 요괴가 없는 안전 구역의 보석을 3m 진공 흡수하여 스킬을 제시하는 동료 헬퍼
    /// </summary>
    public class HelperCompanion : MonoBehaviour
    {
        public static HelperCompanion Instance { get; private set; }

        [Header("Helper Identity")]
        [SerializeField] private string _helperId = "Helper_SpiritRabbit";
        [SerializeField] private string _displayName = "달빛 영혼 토끼";

        [Header("Movement & Settings")]
        [SerializeField] private float _moveSpeed = 6.8f;
        [SerializeField] private float _fleeSpeed = 8.8f;
        [SerializeField] private float _vacuumRadius = 3.0f;        // 3m 진공 흡수 반경
        [SerializeField] private float _gemSafetyRadius = 8.5f;     // 보석 주변 몬스터 안전 거리
        [SerializeField] private float _helperFleeDistance = 5.5f;   // 헬퍼 자신에게 몬스터 접근 시 도망 거리
        [SerializeField] private float _followDistance = 1.9f;

        [Header("State")]
        [SerializeField] private HelperState _currentState = HelperState.FollowingPlayer;

        private Transform _playerTransform;
        private PlayerController _playerController;
        private ExpGem _targetGem;
        private Vector3 _followOffset = new Vector3(-0.9f, 0f, -1.8f);
        private float _hoverHeight = 0.85f;
        private float _bobOffset;
        private bool _isSkillPresented = false;
        private GameObject _vacuumRingObj;
        private GameObject _skillOrbVisual;
        private float _lastGemScanTime = 0f;

        public HelperState CurrentState => _currentState;
        public string DisplayName => _displayName;
        public float VacuumRadius => _vacuumRadius;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _bobOffset = Random.Range(0f, 10f);

            if (Data.GameDataManager.Instance != null && Data.GameDataManager.Instance.HelperData != null)
            {
                ApplyConfig(Data.GameDataManager.Instance.HelperData);
            }

            SetupVisualModel();
            SetupVacuumRingVisual();
        }

        private void Start()
        {
            if (Data.GameDataManager.Instance != null && Data.GameDataManager.Instance.HelperData != null)
            {
                ApplyConfig(Data.GameDataManager.Instance.HelperData);
            }

            FindPlayer();

            if (InRunProgressionManager.Instance != null)
            {
                InRunProgressionManager.Instance.OnLevelUp += HandleLevelUp;
                InRunProgressionManager.Instance.OnBoonSelected += HandleBoonSelected;
            }
        }

        private void OnDestroy()
        {
            if (InRunProgressionManager.Instance != null)
            {
                InRunProgressionManager.Instance.OnLevelUp -= HandleLevelUp;
                InRunProgressionManager.Instance.OnBoonSelected -= HandleBoonSelected;
            }
        }

        public void ApplyConfig(HelperDataSO data)
        {
            if (data == null) return;
            _helperId = data.helperId;
            _displayName = data.displayName;
            _moveSpeed = data.moveSpeed;
            _fleeSpeed = data.fleeSpeed;
            _vacuumRadius = data.vacuumRadius;
            _gemSafetyRadius = data.gemSafetyRadius;
            _helperFleeDistance = data.fleeDistance;
            transform.localScale = Vector3.one * data.scale;
        }

        private void FindPlayer()
        {
            _playerController = FindAnyObjectByType<PlayerController>();
            if (_playerController != null)
            {
                _playerTransform = _playerController.transform;
            }
        }

        private void Update()
        {
            if (_playerTransform == null)
            {
                FindPlayer();
                if (_playerTransform == null) return;
            }

            // 1. 주변 3미터 내 보석 상시 진공청소기 흡수 체크
            CheckAndVacuumNearbyGems();

            // 2. 몬스터 위협 감지: 헬퍼 주변 5.5m 이내에 몬스터가 있으면 즉시 도망
            if (IsHelperInDanger() && _currentState != HelperState.PresentingSkill)
            {
                _currentState = HelperState.FleeingToPlayer;
                _targetGem = null;
            }

            // 3. 상태 머신 실행
            switch (_currentState)
            {
                case HelperState.FollowingPlayer:
                    HandleFollowingPlayer();
                    break;

                case HelperState.GatheringGems:
                    HandleGatheringGems();
                    break;

                case HelperState.FleeingToPlayer:
                    HandleFleeingToPlayer();
                    break;

                case HelperState.PresentingSkill:
                    HandlePresentingSkill();
                    break;
            }

            // 4. 부유(Bobbing) 및 회전 처리
            UpdateHoverAndRotation();
        }

        /// <summary>
        /// 헬퍼 이동 시 주변 3m 안의 보석 및 생명 하트들을 진공청소기처럼 자석 흡수
        /// </summary>
        private void CheckAndVacuumNearbyGems()
        {
            Vector3 helperPos = transform.position;
            float sqrVacRadius = _vacuumRadius * _vacuumRadius;

            // 1. 경험치 보석 진공 흡수
            if (ExpGem.ActiveGems.Count > 0)
            {
                for (int i = ExpGem.ActiveGems.Count - 1; i >= 0; i--)
                {
                    var gem = ExpGem.ActiveGems[i];
                    if (gem == null || !gem.gameObject.activeInHierarchy) continue;

                    Vector3 diff = gem.transform.position - helperPos;
                    diff.y = 0f;

                    if (diff.sqrMagnitude <= sqrVacRadius)
                    {
                        if (!gem.IsVacuuming)
                        {
                            gem.StartVacuum(this);
                        }
                    }
                }
            }

            // 2. 체력 회복 하트 진공 흡수
            if (RecoveryHeart.ActiveHearts.Count > 0)
            {
                for (int i = RecoveryHeart.ActiveHearts.Count - 1; i >= 0; i--)
                {
                    var heart = RecoveryHeart.ActiveHearts[i];
                    if (heart == null || !heart.gameObject.activeInHierarchy) continue;

                    Vector3 diff = heart.transform.position - helperPos;
                    diff.y = 0f;

                    if (diff.sqrMagnitude <= sqrVacRadius)
                    {
                        if (!heart.IsVacuuming)
                        {
                            heart.StartVacuum(this);
                        }
                    }
                }
            }
        }

        private bool IsHelperInDanger()
        {
            if (MonsterAI.ActiveMonsters.Count == 0) return false;

            Vector3 pos = transform.position;
            float sqrFleeDist = _helperFleeDistance * _helperFleeDistance;

            for (int i = 0; i < MonsterAI.ActiveMonsters.Count; i++)
            {
                var m = MonsterAI.ActiveMonsters[i];
                if (m == null || m.IsDead || !m.gameObject.activeInHierarchy) continue;

                Vector3 diff = m.transform.position - pos;
                diff.y = 0f;
                if (diff.sqrMagnitude <= sqrFleeDist)
                {
                    return true;
                }
            }
            return false;
        }

        private void HandleFollowingPlayer()
        {
            Vector3 targetPos = GetPlayerBehindPosition();
            transform.position = Vector3.MoveTowards(transform.position, targetPos, _moveSpeed * Time.deltaTime);

            // 주기적으로 몬스터가 없는 안전 구역의 보석 탐색
            if (Time.time >= _lastGemScanTime + 0.35f)
            {
                _lastGemScanTime = Time.time;
                ExpGem safeGem = FindNearestSafeGem();
                if (safeGem != null)
                {
                    _targetGem = safeGem;
                    _currentState = HelperState.GatheringGems;
                }
            }
        }

        private void HandleGatheringGems()
        {
            // 대상 보석이 사라졌거나 누군가에 의해 흡수 중이거나, 몬스터가 다가와 더 이상 안전하지 않은 경우
            if (_targetGem == null || !_targetGem.gameObject.activeInHierarchy || !IsGemInSafeZone(_targetGem))
            {
                _targetGem = FindNearestSafeGem();
                if (_targetGem == null)
                {
                    _currentState = HelperState.FollowingPlayer;
                    return;
                }
            }

            Vector3 gemPos = _targetGem.transform.position;
            gemPos.y = _hoverHeight;

            // 보석을 향해 이동
            transform.position = Vector3.MoveTowards(transform.position, gemPos, _moveSpeed * Time.deltaTime);

            // 보석 방향을 바라봄
            Vector3 lookDir = (gemPos - transform.position);
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.05f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 10f);
            }

            // 보석에 근접하면 진공 흡수되어 수집됨 (수집되면 다음 프레임에 새 안전 보석 탐색)
            if (Vector3.Distance(transform.position, gemPos) <= 1.2f)
            {
                _targetGem.StartVacuum(this);
            }
        }

        private void HandleFleeingToPlayer()
        {
            Vector3 safeRearPos = GetPlayerBehindPosition();
            transform.position = Vector3.MoveTowards(transform.position, safeRearPos, _fleeSpeed * Time.deltaTime);

            Vector3 lookDir = (safeRearPos - transform.position);
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.05f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 12f);
            }

            // 플레이어 바로 뒤에 도착하고 몬스터가 위험 범위 밖이면 다시 안정 상태 복귀
            if (Vector3.Distance(transform.position, safeRearPos) < 0.6f && !IsHelperInDanger())
            {
                _currentState = HelperState.FollowingPlayer;
            }
        }

        private void HandlePresentingSkill()
        {
            // 플레이어 정면 약간 앞/위에서 스킬 오브젝트를 제시
            Vector3 presentPos = _playerTransform.position + _playerTransform.forward * 1.6f + Vector3.up * 1.3f;
            transform.position = Vector3.MoveTowards(transform.position, presentPos, _moveSpeed * 1.5f * Time.deltaTime);

            // 플레이어를 마주봄
            Vector3 toPlayer = (_playerTransform.position - transform.position);
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude > 0.05f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toPlayer), Time.deltaTime * 8f);
            }

            if (_skillOrbVisual != null)
            {
                _skillOrbVisual.SetActive(true);
                _skillOrbVisual.transform.Rotate(Vector3.up, 180f * Time.deltaTime);
            }
        }

        private Vector3 GetPlayerBehindPosition()
        {
            if (_playerTransform == null) return transform.position;
            Vector3 target = _playerTransform.position + _playerTransform.rotation * _followOffset;
            target.y = _hoverHeight;
            return target;
        }

        /// <summary>
        /// 몬스터가 없는 안전 구역(Safe Zone)에 위치한 가장 가까운 보석 탐색
        /// </summary>
        private ExpGem FindNearestSafeGem()
        {
            if (ExpGem.ActiveGems.Count == 0) return null;

            ExpGem bestGem = null;
            float minDist = float.MaxValue;
            Vector3 helperPos = transform.position;

            for (int i = 0; i < ExpGem.ActiveGems.Count; i++)
            {
                var gem = ExpGem.ActiveGems[i];
                if (gem == null || !gem.gameObject.activeInHierarchy || gem.IsVacuuming) continue;

                // 보석 주변에 살아있는 몬스터가 있는지 검사
                if (IsGemInSafeZone(gem))
                {
                    float d = Vector3.Distance(helperPos, gem.transform.position);
                    if (d < minDist)
                    {
                        minDist = d;
                        bestGem = gem;
                    }
                }
            }
            return bestGem;
        }

        private bool IsGemInSafeZone(ExpGem gem)
        {
            if (gem == null) return false;
            if (MonsterAI.ActiveMonsters.Count == 0) return true; // 방에 몬스터가 전멸했으면 무조건 100% 안전

            Vector3 gemPos = gem.transform.position;
            float sqrSafeDist = _gemSafetyRadius * _gemSafetyRadius;

            for (int i = 0; i < MonsterAI.ActiveMonsters.Count; i++)
            {
                var monster = MonsterAI.ActiveMonsters[i];
                if (monster == null || monster.IsDead || !monster.gameObject.activeInHierarchy) continue;

                Vector3 diff = monster.transform.position - gemPos;
                diff.y = 0f;
                if (diff.sqrMagnitude < sqrSafeDist)
                {
                    return false; // 보석 8.5m 내에 몬스터가 있으면 위험 구역
                }
            }
            return true;
        }

        /// <summary>
        /// 헬퍼가 보석을 흡수 획득했을 때 호출
        /// </summary>
        public void CollectGem(int expAmount)
        {
            if (InRunProgressionManager.Instance != null)
            {
                InRunProgressionManager.Instance.AddExp(expAmount);
            }

            // 가벼운 바운스 애니메이션
            _bobOffset += 1.5f;
        }

        private void HandleLevelUp(int newLevel)
        {
            _currentState = HelperState.PresentingSkill;
            _isSkillPresented = true;
            Debug.Log($"<color=cyan><b>[동료 헬퍼]</b> 영혼석 정제 완료! 캐릭터에게 스킬 오브젝트 제시 (Lv.{newLevel})</color>");
            Core.VFXManager.Instance?.SpawnLevelUpBuff(_playerTransform != null ? _playerTransform : transform);
        }

        private void HandleBoonSelected(InRunBoonSO boon)
        {
            if (_skillOrbVisual != null)
            {
                _skillOrbVisual.SetActive(false);
            }
            _isSkillPresented = false;
            _currentState = HelperState.FollowingPlayer;
            Debug.Log("<color=cyan><b>[동료 헬퍼]</b> 스킬 선택 완료. 다시 호위/수색 모드로 복귀합니다.</color>");
        }

        private void UpdateHoverAndRotation()
        {
            // 상하 부유 애니메이션
            float hoverY = _hoverHeight + Mathf.Sin((Time.time + _bobOffset) * 4f) * 0.12f;
            transform.position = new Vector3(transform.position.x, hoverY, transform.position.z);

            // 이동 중일 때 이동 방향 회전
            if (_currentState == HelperState.FollowingPlayer)
            {
                if (_playerTransform != null)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, _playerTransform.rotation, Time.deltaTime * 6f);
                }
            }

            // 바닥 진공 링 위치 갱신
            if (_vacuumRingObj != null)
            {
                _vacuumRingObj.transform.position = new Vector3(transform.position.x, 0.08f, transform.position.z);
            }
        }

        private void SetupVisualModel()
        {
            // 1. 헬퍼 본체: 귀여운 영물(달빛 토끼/도깨비 영혼) 외형
            var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            body.name = "HelperBody";
            body.transform.SetParent(transform, false);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(0.75f, 0.85f, 0.75f);

            var col = body.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var rend = body.GetComponent<Renderer>();
            if (rend != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                mat.color = new Color(0.92f, 0.95f, 1.0f); // 달빛 은백색
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(0.92f, 0.95f, 1.0f));
                rend.material = mat;
            }

            // 2. 귀/뿔 장식 (토끼/도깨비 귀 2개)
            CreateEar(body.transform, new Vector3(-0.2f, 0.45f, 0.05f), new Vector3(0.18f, 0.45f, 0.18f));
            CreateEar(body.transform, new Vector3(0.2f, 0.45f, 0.05f), new Vector3(0.18f, 0.45f, 0.18f));

            // 3. 눈망울 2개 (초롱초롱한 옥빛/감청색)
            CreateEye(body.transform, new Vector3(-0.16f, 0.05f, 0.32f));
            CreateEye(body.transform, new Vector3(0.16f, 0.05f, 0.32f));

            // 4. 스킬 제시 시 머리 위에 떠오르는 황금 비술 구체 (Skill Orb)
            _skillOrbVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _skillOrbVisual.name = "SkillOrbVisual";
            _skillOrbVisual.transform.SetParent(transform, false);
            _skillOrbVisual.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            _skillOrbVisual.transform.localScale = Vector3.one * 0.45f;
            var orbCol = _skillOrbVisual.GetComponent<Collider>();
            if (orbCol != null) Destroy(orbCol);

            var orbRend = _skillOrbVisual.GetComponent<Renderer>();
            if (orbRend != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
                mat.color = new Color(1f, 0.85f, 0.2f, 0.95f); // 찬란한 황금빛
                orbRend.material = mat;
            }
            _skillOrbVisual.SetActive(false);
        }

        private void CreateEar(Transform parent, Vector3 localPos, Vector3 localScale)
        {
            var ear = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            ear.name = "Ear";
            ear.transform.SetParent(parent, false);
            ear.transform.localPosition = localPos;
            ear.transform.localRotation = Quaternion.Euler(15f, 0f, localPos.x * 25f);
            ear.transform.localScale = localScale;
            var c = ear.GetComponent<Collider>();
            if (c != null) Destroy(c);
            var r = ear.GetComponent<Renderer>();
            if (r != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                mat.color = new Color(0.98f, 0.88f, 0.92f); // 연분홍 귓바퀴
                r.material = mat;
            }
        }

        private void CreateEye(Transform parent, Vector3 localPos)
        {
            var eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eye.name = "Eye";
            eye.transform.SetParent(parent, false);
            eye.transform.localPosition = localPos;
            eye.transform.localScale = new Vector3(0.12f, 0.16f, 0.12f);
            var c = eye.GetComponent<Collider>();
            if (c != null) Destroy(c);
            var r = eye.GetComponent<Renderer>();
            if (r != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
                mat.color = new Color(0.1f, 0.4f, 0.9f); // 푸른 영혼의 눈
                r.material = mat;
            }
        }

        private void SetupVacuumRingVisual()
        {
            // 바닥에 표시되는 3m 진공 흡수 오라 원
            _vacuumRingObj = new GameObject("VacuumAuraRing_3m");
            _vacuumRingObj.transform.position = new Vector3(transform.position.x, 0.08f, transform.position.z);

            var mf = _vacuumRingObj.AddComponent<MeshFilter>();
            var mr = _vacuumRingObj.AddComponent<MeshRenderer>();

            mf.sharedMesh = CreateFlatRingMesh(_vacuumRadius - 0.1f, _vacuumRadius, 36);

            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
            mat.color = new Color(0.15f, 0.9f, 0.8f, 0.45f); // 은은한 청록색 진공 흡수 링
            mr.material = mat;
        }

        private static Mesh CreateFlatRingMesh(float innerRadius, float outerRadius, int segments)
        {
            Mesh mesh = new Mesh { name = "VacuumRingMesh" };
            int vertCount = (segments + 1) * 2;
            Vector3[] vertices = new Vector3[vertCount];
            int[] triangles = new int[segments * 6];

            float angleStep = 360f / segments;
            for (int i = 0; i <= segments; i++)
            {
                float rad = Mathf.Deg2Rad * (i * angleStep);
                float cos = Mathf.Cos(rad);
                float sin = Mathf.Sin(rad);

                vertices[i * 2] = new Vector3(cos * innerRadius, 0f, sin * innerRadius);
                vertices[i * 2 + 1] = new Vector3(cos * outerRadius, 0f, sin * outerRadius);

                if (i < segments)
                {
                    int root = i * 2;
                    triangles[i * 6 + 0] = root;
                    triangles[i * 6 + 1] = root + 1;
                    triangles[i * 6 + 2] = root + 2;

                    triangles[i * 6 + 3] = root + 1;
                    triangles[i * 6 + 4] = root + 3;
                    triangles[i * 6 + 5] = root + 2;
                }
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private void OnGUI()
        {
            // 헬퍼 머리 위 상태 표시
            var cam = Camera.main;
            if (cam == null) return;

            Vector3 screenPos = cam.WorldToScreenPoint(transform.position + Vector3.up * 1.55f);
            if (screenPos.z <= 0f) return;

            float x = screenPos.x - 70f;
            float y = Screen.height - screenPos.y - 15f;

            string statusText = _currentState switch
            {
                HelperState.FollowingPlayer => "<color=#B0E0E6>🐰 동행 중</color>",
                HelperState.GatheringGems => "<color=#00FA9A>💎 안전 보석 수색 중</color>",
                HelperState.FleeingToPlayer => "<color=#FF6347>⚠️ 요괴 출현! 후퇴</color>",
                HelperState.PresentingSkill => "<color=#FFD700>✨ 스킬 제련 완료!</color>",
                _ => ""
            };

            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };

            GUI.Label(new Rect(x, y, 140, 24), $"{statusText}", style);
        }
    }
}
