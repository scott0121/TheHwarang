using System.Collections;
using System.Collections.Generic;
using MythOfGuotaji.Actor;
using MythOfGuotaji.Cameras;
using MythOfGuotaji.Core;
using UnityEngine;

namespace MythOfGuotaji.Combat
{
    /// <summary>
    /// 주(鴸) 전용 2초 고정 비행 포물선 폭발 투사체
    /// - 플레이어 위치에 지름 5m(반경 2.5m) 원형 장판 표시
    /// - 2초 동안 포물선 궤적으로 비행하여 목표 지점에 낙하
    /// - 착지 시 지름 5m 광역 폭발 피해 (18 데미지)
    /// - 단검(Dagger)으로 쳐내면 날아오던 각도로 몬스터에게 반사(패링) 가능!
    /// </summary>
    public class ZhuParabolicProjectile : MonoBehaviour, IReflectable
    {
        [Header("Projectile Specs")]
        [SerializeField] private float _flightDuration = 2.0f; // 무조건 2초 고정
        [SerializeField] private float _arcHeight = 6.0f;      // 포물선 정점 높이
        [SerializeField] private float _aoeRadius = 2.5f;      // 지름 5m = 반경 2.5m
        [SerializeField] private float _damage = 18f;

        private Vector3 _startPos;
        private Vector3 _targetPos;
        private float _elapsed = 0f;
        private bool _isReflected = false;
        private GameObject _owner;
        private GameObject _telegraphObj;
        private Material _projMat;
        private Material _telegraphMat;
        private Material _borderMat;
        private bool _hasLanded = false;

        private float _reflectedDamageMultiplier = 1.5f;

        public bool IsFromPlayer => _isReflected;
        public Vector3 Position => transform.position;
        public ProjectileParryType ParryType { get; set; } = ProjectileParryType.Normal;

        private void Awake()
        {
            ApplyTopDepthPriority();
        }

        private void OnEnable()
        {
            ApplyTopDepthPriority();
            if (!Projectile.ActiveReflectables.Contains(this))
            {
                Projectile.ActiveReflectables.Add(this);
            }
        }

        /// <summary>
        /// 곡사 탄환 본체의 뎁스를 최우선(sortingOrder 5000, renderQueue 3500)으로 설정
        /// </summary>
        public void ApplyTopDepthPriority()
        {
            var rends = GetComponentsInChildren<Renderer>(true);
            foreach (var r in rends)
            {
                // 바닥 예고 인디케이터 장판은 제외하고 투사체 본체만 최우선 적용
                if (r.gameObject == _telegraphObj || (r.transform.parent != null && r.transform.parent.gameObject == _telegraphObj))
                {
                    continue;
                }
                r.sortingOrder = 5000;
                if (r.materials != null)
                {
                    foreach (var mat in r.materials)
                    {
                        if (mat != null && mat.renderQueue < 3500)
                        {
                            mat.renderQueue = 3500;
                        }
                    }
                }
            }
        }

        private void OnDisable()
        {
            Projectile.ActiveReflectables.Remove(this);
            if (_telegraphObj != null)
            {
                Destroy(_telegraphObj);
                _telegraphObj = null;
            }
            if (_telegraphMat != null)
            {
                Destroy(_telegraphMat);
                _telegraphMat = null;
            }
            if (_borderMat != null)
            {
                Destroy(_borderMat);
                _borderMat = null;
            }
        }

        public void Initialize(GameObject owner, Vector3 startPos, Vector3 targetPos, float damage = 18f, float flightDuration = 2.0f, float arcHeight = 6.0f, float aoeRadius = 2.5f)
        {
            _owner = owner;
            _startPos = startPos;
            _targetPos = targetPos;
            _targetPos.y = 0.05f;
            _damage = damage;
            if (flightDuration > 0f) _flightDuration = flightDuration;
            if (arcHeight > 0f) _arcHeight = arcHeight;
            if (aoeRadius > 0f) _aoeRadius = aoeRadius;
            _elapsed = 0f;
            _hasLanded = false;
            _isReflected = false;

            transform.position = startPos;

            // 시각적 구체(보라/흑염) 생성 및 화염 트레일 부착
            SetupVisual();
            Core.VFXManager.Instance?.AttachZhuProjectileFire(transform);

            // 바닥에 원형 전조 장판 생성 (설정된 aoeRadius 반영)
            CreateCircleTelegraph();
        }

        private void SetupVisual()
        {
            // HCFX_ElementOrb_01 등 자체 파티클/메시가 있는 프리팹은 고유 비주얼 유지
            if (GetComponentInChildren<ParticleSystem>() != null)
            {
                return;
            }

            var rend = GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                _projMat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
                Color c = new Color(0.85f, 0.15f, 0.95f); // 주(鴸)의 요괴 보라색 화염
                _projMat.color = c;
                if (_projMat.HasProperty("_BaseColor")) _projMat.SetColor("_BaseColor", c);
                rend.material = _projMat;
            }
        }

        public static Material CreateTelegraphMaterial(Color col)
        {
            Shader s = Shader.Find("Sprites/Default") 
                    ?? Shader.Find("Universal Render Pipeline/Unlit") 
                    ?? Shader.Find("Unlit/Color");
            Material m = new Material(s);
            m.color = col;
            if (m.HasProperty("_Color")) m.SetColor("_Color", col);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);
            if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1); // Transparent
            if (m.HasProperty("_Blend")) m.SetFloat("_Blend", 0);     // Alpha
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off); // 양면 렌더링
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 100;
            return m;
        }

        private void CreateCircleTelegraph()
        {
            if (_telegraphObj != null) Destroy(_telegraphObj);

            GameObject circlePrefab = null;
            if (Data.GameDataManager.Instance != null && Data.GameDataManager.Instance.Prefabs != null)
            {
                circlePrefab = Data.PrefabDatabaseSO.ResolvePrefab(ref Data.GameDataManager.Instance.Prefabs.circleTelegraphPrefab, "Prefabs/Telegraph_Circle");
            }
            else
            {
                circlePrefab = Resources.Load<GameObject>("Prefabs/Telegraph_Circle");
            }

            if (circlePrefab != null)
            {
                _telegraphObj = Instantiate(circlePrefab, new Vector3(_targetPos.x, 0.08f, _targetPos.z), Quaternion.identity);
                _telegraphObj.name = "[Zhu_CircleTelegraph]";
            }
            else
            {
                _telegraphObj = new GameObject("[Zhu_CircleTelegraph]");
                _telegraphObj.transform.position = new Vector3(_targetPos.x, 0.08f, _targetPos.z);
            }

            // 1. 내부 반투명 원판 (프리팹 내 TelegraphFill 재활용 또는 생성)
            var existingFill = _telegraphObj.transform.Find("TelegraphFill");
            GameObject fillObj;
            if (existingFill != null)
            {
                fillObj = existingFill.gameObject;
            }
            else
            {
                fillObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                fillObj.name = "TelegraphFill";
                fillObj.transform.SetParent(_telegraphObj.transform, false);
                fillObj.transform.localPosition = Vector3.zero;
                var col = fillObj.GetComponent<Collider>();
                if (col != null) Destroy(col);
            }
            fillObj.transform.localScale = new Vector3(_aoeRadius * 2f, 0.01f, _aoeRadius * 2f);

            var mr = fillObj.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sortingOrder = 60;
                Color fillCol = _isReflected ? new Color(0.15f, 0.85f, 1f, 0.40f) : new Color(0.95f, 0.15f, 0.25f, 0.40f);
                _telegraphMat = CreateTelegraphMaterial(fillCol);
                mr.material = _telegraphMat;
            }

            // 2. 외곽선 테두리 링 생성 (선명한 빨간/주황 경계선)
            var existingRing = _telegraphObj.transform.Find("TelegraphRingBorder");
            if (existingRing != null) Destroy(existingRing.gameObject);
            CreateOuterRing(_telegraphObj.transform, _aoeRadius, Mathf.Max(0.1f, _aoeRadius - 0.15f));
        }

        private void CreateOuterRing(Transform parent, float outerR, float innerR)
        {
            GameObject ring = new GameObject("TelegraphRingBorder");
            ring.transform.SetParent(parent, false);
            ring.transform.localPosition = new Vector3(0f, 0.02f, 0f);

            var mf = ring.AddComponent<MeshFilter>();
            var mr = ring.AddComponent<MeshRenderer>();
            mr.sortingOrder = 65;

            Mesh mesh = new Mesh();
            int segments = 40;
            Vector3[] verts = new Vector3[segments * 2];
            int[] tris = new int[segments * 12]; // 양면 렌더링 6 + 6 = 12

            float angleStep = 360f / segments;
            for (int i = 0; i < segments; i++)
            {
                float rad = Mathf.Deg2Rad * (i * angleStep);
                float cos = Mathf.Cos(rad);
                float sin = Mathf.Sin(rad);

                verts[i * 2] = new Vector3(cos * innerR, 0f, sin * innerR);
                verts[i * 2 + 1] = new Vector3(cos * outerR, 0f, sin * outerR);
            }

            for (int i = 0; i < segments; i++)
            {
                int currentInner = i * 2;
                int currentOuter = i * 2 + 1;
                int nextInner = ((i + 1) % segments) * 2;
                int nextOuter = ((i + 1) % segments) * 2 + 1;

                int triBase = i * 12;

                // 윗면 (Clockwise from top)
                tris[triBase + 0] = currentInner;
                tris[triBase + 1] = nextOuter;
                tris[triBase + 2] = currentOuter;

                tris[triBase + 3] = currentInner;
                tris[triBase + 4] = nextInner;
                tris[triBase + 5] = nextOuter;

                // 아랫면 (Double-sided)
                tris[triBase + 6] = currentInner;
                tris[triBase + 7] = currentOuter;
                tris[triBase + 8] = nextOuter;

                tris[triBase + 9] = currentInner;
                tris[triBase + 10] = nextOuter;
                tris[triBase + 11] = nextInner;
            }

            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mf.mesh = mesh;

            Color borderCol = _isReflected ? new Color(0.35f, 0.95f, 1f, 0.9f) : new Color(1f, 0.3f, 0.15f, 0.9f);
            _borderMat = CreateTelegraphMaterial(borderCol);
            mr.material = _borderMat;
        }

        private void Update()
        {
            if (_hasLanded) return;

            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _flightDuration);

            // 포물선 궤적 계산
            Vector3 currentPos = Vector3.Lerp(_startPos, _targetPos, t);
            currentPos.y += 4f * _arcHeight * t * (1f - t);
            transform.position = currentPos;

            // 진행 방향 회전
            float nextT = Mathf.Clamp01((_elapsed + 0.02f) / _flightDuration);
            Vector3 nextPos = Vector3.Lerp(_startPos, _targetPos, nextT);
            nextPos.y += 4f * _arcHeight * nextT * (1f - nextT);
            Vector3 dir = (nextPos - currentPos);
            if (dir.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(dir.normalized);
            }

            // 바닥 장판 펄스/채움 효과
            if (_telegraphMat != null)
            {
                float pulse = Mathf.PingPong(Time.time * 6f, 0.25f);
                Color baseC = _isReflected 
                    ? new Color(0.15f, 0.85f, 1f, 0.35f + pulse) 
                    : new Color(0.95f, 0.15f, 0.25f, 0.30f + pulse);
                _telegraphMat.color = baseC;
                if (_telegraphMat.HasProperty("_Color")) _telegraphMat.SetColor("_Color", baseC);
                if (_telegraphMat.HasProperty("_BaseColor")) _telegraphMat.SetColor("_BaseColor", baseC);
            }
            if (_borderMat != null)
            {
                float borderPulse = Mathf.PingPong(Time.time * 6f, 0.2f);
                Color bCol = _isReflected 
                    ? new Color(0.35f, 0.95f, 1f, 0.8f + borderPulse) 
                    : new Color(1f, 0.35f, 0.15f, 0.85f + borderPulse);
                _borderMat.color = bCol;
                if (_borderMat.HasProperty("_Color")) _borderMat.SetColor("_Color", bCol);
                if (_borderMat.HasProperty("_BaseColor")) _borderMat.SetColor("_BaseColor", bCol);
            }

            // 2초 도달 시 착지 및 폭발!
            if (t >= 1.0f)
            {
                Explode();
            }
        }

        private void Explode()
        {
            _hasLanded = true;

            // 바닥 가이드 장판 즉시 제거 (공격 후 바닥에 자국 남기지 않음)
            if (_telegraphObj != null)
            {
                Destroy(_telegraphObj);
            }

            // 카메라 셰이크 및 5m 지면 대폭발 VFX
            if (QuarterViewCamera.Instance != null)
            {
                QuarterViewCamera.Instance.TriggerShake(0.35f, 0.15f);
            }
            Core.VFXManager.Instance?.SpawnZhuGroundExplosion(_targetPos, _aoeRadius);

            if (!_isReflected)
            {
                // 플레이어 피격 판정 (주변 5미터 = 반경 2.5미터 광역)
                var player = PlayerController.Instance != null ? PlayerController.Instance : FindAnyObjectByType<PlayerController>();
                if (player != null && !player.IsDead)
                {
                    Vector3 pPos = player.transform.position;
                    pPos.y = 0f;
                    Vector3 hitCenter = _targetPos;
                    hitCenter.y = 0f;

                    if (Vector3.Distance(pPos, hitCenter) <= _aoeRadius + 0.5f)
                    {
                        DamageInfo info = new DamageInfo(_damage, false, _targetPos, (pPos - hitCenter).normalized, 4.0f, ElementType.Fire, _owner);
                        player.TakeDamage(info);
                        Debug.Log($"<color=red><b>[주(鴸)의 폭발 적중!]</b> 거타지가 {_damage} 광역 피해를 입었습니다!</color>");
                    }
                }
            }
            else
            {
                // 반사된 경우: 착지 지점 주변 몬스터들에게 배율 피해!
                Collider[] hits = Physics.OverlapSphere(_targetPos, _aoeRadius + 0.5f);
                foreach (var col in hits)
                {
                    var monster = col.GetComponentInParent<MonsterAI>();
                    if (monster != null && !monster.IsDead)
                    {
                        Vector3 mPos = monster.transform.position;
                        mPos.y = 0f;
                        DamageInfo info = new DamageInfo(_damage * _reflectedDamageMultiplier, true, _targetPos, (mPos - _targetPos).normalized, 7.0f, ElementType.Fire, PlayerController.Instance != null ? PlayerController.Instance.gameObject : null);
                        monster.TakeDamage(info);
                    }
                }
                Debug.Log($"<color=cyan><b>[반사된 주(鴸) 투사체 폭발!]</b> 몬스터들에게 {_damage * _reflectedDamageMultiplier} 광역 피해 적중!</color>");
            }

            Destroy(gameObject);
        }

        /// <summary>
        /// 단검 패링으로 투사체를 쳐냈을 때 호출
        /// - 날아오던 각도로 몬스터(발사자)에게 역포물선으로 반사!
        /// </summary>
        public void Reflect(GameObject newAttacker, Vector3? customDirection = null, float damageMultiplier = 1.5f, float speedMultiplier = 1.8f)
        {
            if (_hasLanded) return;

            _isReflected = true;
            _reflectedDamageMultiplier = damageMultiplier;
            _flightDuration = Mathf.Max(0.5f, _flightDuration / speedMultiplier);
            _startPos = transform.position;

            // 발사자(주) 위치로 반사, 발사자가 없으면 역방향
            if (_owner != null)
            {
                _targetPos = _owner.transform.position;
            }
            else
            {
                _targetPos = _startPos - transform.forward * 15f;
            }
            _targetPos.y = 0.05f;

            _elapsed = 0f; // 반사된 시점부터 다시 포물선 비행

            // 장판 위치 이동 및 색상 청록색(아군)으로 변환
            if (_telegraphObj != null)
            {
                _telegraphObj.transform.position = new Vector3(_targetPos.x, 0.05f, _targetPos.z);
            }

            // 투사체 색상도 찬란한 청록색으로 변환
            if (_projMat != null)
            {
                Color c = new Color(0.2f, 0.95f, 1f);
                _projMat.color = c;
                if (_projMat.HasProperty("_BaseColor")) _projMat.SetColor("_BaseColor", c);
            }

            Debug.Log("<color=yellow><b>[주(鴸)의 마법 투사체 패링 성공!]</b> 폭발 구체가 요괴를 향해 반사됩니다!</color>");
        }

        private void OnDestroy()
        {
            if (_telegraphObj != null) Destroy(_telegraphObj);
            if (_telegraphMat != null) Destroy(_telegraphMat);
            if (_borderMat != null) Destroy(_borderMat);
            if (_projMat != null) Destroy(_projMat);
        }
    }
}
