using System.Collections.Generic;
using MythOfGuotaji.Core;
using UnityEngine;

namespace MythOfGuotaji.Actor
{
    /// <summary>
    /// 요괴 처치 시 일정 확률로 드롭되는 체력 회복 하트 (생명수 구체)
    /// - 플레이어가 직접 몸으로 접촉하거나, 동료 헬퍼가 3m 이내로 접근하여 진공 흡수/획득 가능
    /// - 획득 시 거타지의 체력을 회복하며, 거타지 본체에 치유 버프 이펙트(초록빛 마법진 + 상승 생명 오라) 발동
    /// </summary>
    public class RecoveryHeart : MonoBehaviour
    {
        public static readonly List<RecoveryHeart> ActiveHearts = new();
        public static float BonusHealAmount = 0f;
        public static float DropRateMultiplier = 1f;

        [Header("Heart Specs")]
        [SerializeField] private float _healAmount = 5f;
        [SerializeField] private float _initialSpeed = 4.5f;
        [SerializeField] private float _maxVacuumSpeed = 18f;
        [SerializeField] private float _acceleration = 22f;

        private bool _isLanded = false;
        private bool _isVacuuming = false;
        private HelperCompanion _targetHelper;
        private float _currentVacuumSpeed;
        private Vector3 _velocity;
        private float _targetFloorY = 0.45f;
        private float _bobOffset;
        private bool _isCollected = false;

        public float HealAmount => _healAmount;
        public bool IsVacuuming => _isVacuuming;
        public bool IsLanded => _isLanded;

        private void Awake()
        {
            _bobOffset = Random.Range(0f, 10f);
            if (Data.GameDataManager.Instance != null && Data.GameDataManager.Instance.PlayerStats != null)
            {
                _healAmount = Data.GameDataManager.Instance.PlayerStats.heartHealAmount;
            }
            BuildHeartVisual();
        }

        private void OnEnable()
        {
            if (!ActiveHearts.Contains(this))
            {
                ActiveHearts.Add(this);
            }
        }

        private void OnDisable()
        {
            ActiveHearts.Remove(this);
            _isVacuuming = false;
            _targetHelper = null;
        }

        public void Initialize(float healAmount, Vector3 spawnVelocity, float floorY = 0.45f)
        {
            _healAmount = healAmount;
            _velocity = spawnVelocity;
            _targetFloorY = floorY;
            _isLanded = false;
            _isVacuuming = false;
            _targetHelper = null;
            _isCollected = false;
            _currentVacuumSpeed = _initialSpeed;
        }

        private void Update()
        {
            if (_isCollected) return;

            // 1. 착지 전 포물선 산란 물리
            if (!_isLanded)
            {
                _velocity.y -= 18.0f * Time.deltaTime;
                transform.position += _velocity * Time.deltaTime;

                if (transform.position.y <= _targetFloorY)
                {
                    _isLanded = true;
                    _velocity = Vector3.zero;
                    transform.position = new Vector3(transform.position.x, _targetFloorY, transform.position.z);
                }
                return;
            }

            // 2. 헬퍼의 3m 진공 흡수 이동
            if (_isVacuuming)
            {
                if (_targetHelper == null || !_targetHelper.gameObject.activeInHierarchy)
                {
                    _isVacuuming = false;
                    return;
                }

                _currentVacuumSpeed = Mathf.Min(_currentVacuumSpeed + _acceleration * Time.deltaTime, _maxVacuumSpeed);
                Vector3 targetPos = _targetHelper.transform.position + Vector3.up * 0.4f;
                transform.position = Vector3.MoveTowards(transform.position, targetPos, _currentVacuumSpeed * Time.deltaTime);
                transform.Rotate(Vector3.up, 420f * Time.deltaTime, Space.World);

                if (Vector3.Distance(transform.position, targetPos) < 0.45f)
                {
                    Collect("동료 헬퍼가 수집하여 본체 전달");
                }
                return;
            }

            // 3. 지면 대기 상태: 부유 및 회전
            transform.Rotate(Vector3.up, 80f * Time.deltaTime, Space.World);
            float y = _targetFloorY + Mathf.Sin((Time.time + _bobOffset) * 4.0f) * 0.12f;
            transform.position = new Vector3(transform.position.x, y, transform.position.z);

            // 4. 플레이어 직접 몸으로 접촉 획득 판정 (반경 1.2m)
            var player = PlayerController.Instance != null ? PlayerController.Instance : FindAnyObjectByType<PlayerController>();
            if (player != null && !player.IsDead)
            {
                Vector3 playerPos = player.transform.position;
                playerPos.y = transform.position.y;
                if (Vector3.Distance(transform.position, playerPos) <= 1.25f)
                {
                    Collect("플레이어 직접 접촉");
                }
            }
        }

        /// <summary>
        /// 헬퍼가 반경 내로 접근했을 때 진공 흡수 시작
        /// </summary>
        public void StartVacuum(HelperCompanion helper)
        {
            if (_isVacuuming || helper == null || _isCollected) return;
            _isVacuuming = true;
            _targetHelper = helper;
            _currentVacuumSpeed = _initialSpeed;
        }

        private void Collect(string collectorName)
        {
            if (_isCollected) return;
            _isCollected = true;

            var player = PlayerController.Instance != null ? PlayerController.Instance : FindAnyObjectByType<PlayerController>();
            if (player != null && !player.IsDead)
            {
                float totalHeal = _healAmount + BonusHealAmount;
                player.Heal(totalHeal);
                VFXManager.Instance?.SpawnHealBuff(player.transform);
                Debug.Log($"<color=magenta><b>[생명 하트 획득! ({collectorName})]</b> 거타지 체력 +{totalHeal:F0} 회복!</color>");
            }

            ActiveHearts.Remove(this);
            Destroy(gameObject);
        }

        public static void SpawnHeart(Vector3 center, float healAmount = 25f, float floorY = 0.45f)
        {
            Vector3 spawnPos = center + Vector3.up * 0.5f;
            Vector3 vel = new Vector3(Random.Range(-1.0f, 1.0f), Random.Range(3.2f, 4.8f), Random.Range(-1.0f, 1.0f));

            GameObject heartPrefab = null;
            if (Data.GameDataManager.Instance != null && Data.GameDataManager.Instance.Prefabs != null)
            {
                heartPrefab = Data.PrefabDatabaseSO.ResolvePrefab(ref Data.GameDataManager.Instance.Prefabs.recoveryHeartPrefab, "Prefabs/RecoveryHeart");
            }
            else
            {
                heartPrefab = Resources.Load<GameObject>("Prefabs/RecoveryHeart");
            }

            GameObject heartObj;
            if (heartPrefab != null)
            {
                heartObj = Instantiate(heartPrefab, spawnPos, Quaternion.identity);
                heartObj.name = "RecoveryHeart";
            }
            else
            {
                heartObj = new GameObject("RecoveryHeart");
                heartObj.transform.position = spawnPos;
            }

            var heart = heartObj.GetComponent<RecoveryHeart>();
            if (heart == null) heart = heartObj.AddComponent<RecoveryHeart>();
            heart.Initialize(healAmount, vel, floorY);
        }

        /// <summary>
        /// 루비빛으로 반짝이는 3D 하트 비주얼 빌드 (프리팹에 이미 존재하면 스킵)
        /// </summary>
        private void BuildHeartVisual()
        {
            if (transform.Find("HeartVisual") != null) return;

            var heartRoot = new GameObject("HeartVisual");
            heartRoot.transform.SetParent(transform, false);
            heartRoot.transform.localPosition = Vector3.zero;

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard");
            var heartMat = new Material(shader);
            Color rubyRed = new Color(1.0f, 0.12f, 0.28f, 0.95f);
            Color rubyEmission = new Color(1.0f, 0.15f, 0.35f) * 1.5f;

            if (heartMat.HasProperty("_BaseColor")) heartMat.SetColor("_BaseColor", rubyRed);
            if (heartMat.HasProperty("_Color")) heartMat.SetColor("_Color", rubyRed);
            if (heartMat.HasProperty("_EmissionColor"))
            {
                heartMat.EnableKeyword("_EMISSION");
                heartMat.SetColor("_EmissionColor", rubyEmission);
            }

            // 좌측 하트 귓바퀴 구체
            var lobeL = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lobeL.name = "Lobe_Left";
            lobeL.transform.SetParent(heartRoot.transform, false);
            lobeL.transform.localPosition = new Vector3(-0.16f, 0.15f, 0f);
            lobeL.transform.localScale = new Vector3(0.32f, 0.32f, 0.24f);
            lobeL.transform.localRotation = Quaternion.Euler(0f, 0f, 25f);
            StripCollider(lobeL);
            lobeL.GetComponent<Renderer>().material = heartMat;

            // 우측 하트 귓바퀴 구체
            var lobeR = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lobeR.name = "Lobe_Right";
            lobeR.transform.SetParent(heartRoot.transform, false);
            lobeR.transform.localPosition = new Vector3(0.16f, 0.15f, 0f);
            lobeR.transform.localScale = new Vector3(0.32f, 0.32f, 0.24f);
            lobeR.transform.localRotation = Quaternion.Euler(0f, 0f, -25f);
            StripCollider(lobeR);
            lobeR.GetComponent<Renderer>().material = heartMat;

            // 하단 뾰족한 역원뿔/다이아몬드 바디
            var tip = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tip.name = "Heart_Tip";
            tip.transform.SetParent(heartRoot.transform, false);
            tip.transform.localPosition = new Vector3(0f, -0.05f, 0f);
            tip.transform.localScale = new Vector3(0.28f, 0.24f, 0.22f);
            tip.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            StripCollider(tip);
            tip.GetComponent<Renderer>().material = heartMat;
        }

        private void StripCollider(GameObject go)
        {
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
        }
    }
}
