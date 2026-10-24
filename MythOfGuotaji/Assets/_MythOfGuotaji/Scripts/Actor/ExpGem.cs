using System.Collections.Generic;
using UnityEngine;

namespace MythOfGuotaji.Actor
{
    /// <summary>
    /// 요괴 처치 시 바닥에 드롭되는 경험치 보석 (영혼석)
    /// - 플레이어는 직접 줍지 않음 (캐릭터 충돌 무시)
    /// - 동료 헬퍼가 3미터 이내로 접근 시 진공청소기처럼 자석 흡수되어 헬퍼에게 획득됨
    /// - 헬퍼가 흡수한 보석으로 스킬 오브젝트(축복 선택)를 제련하여 플레이어에게 제시함
    /// </summary>
    public class ExpGem : MonoBehaviour
    {
        public static readonly List<ExpGem> ActiveGems = new();

        [Header("Gem Values")]
        [SerializeField] private int _expValue = 1;

        [Header("Motion & Vacuum Settings")]
        [SerializeField] private float _initialSpeed = 5.0f;
        [SerializeField] private float _maxVacuumSpeed = 19.0f;
        [SerializeField] private float _acceleration = 24.0f;

        private bool _isVacuuming = false;
        private HelperCompanion _targetHelper;
        private float _currentVacuumSpeed;
        private Vector3 _velocity;
        private bool _isLanded = false;
        private float _targetFloorY = 0.45f;
        private float _bobOffset;
        private Renderer _renderer;
        private static Material _gemMaterial;
        private static Mesh _diamondMesh;

        public int ExpValue => _expValue;
        public bool IsVacuuming => _isVacuuming;
        public bool IsLanded => _isLanded;

        private void Awake()
        {
            _bobOffset = Random.Range(0f, 10f);
            SetupVisualIfMissing();
        }

        private void OnEnable()
        {
            if (!ActiveGems.Contains(this))
            {
                ActiveGems.Add(this);
            }
            _isVacuuming = false;
            _targetHelper = null;
            _currentVacuumSpeed = _initialSpeed;
        }

        private void OnDisable()
        {
            ActiveGems.Remove(this);
            _isVacuuming = false;
            _targetHelper = null;
        }

        public void Initialize(int expValue, Vector3 spawnVelocity, float floorY = 0.45f)
        {
            _expValue = expValue;
            _velocity = spawnVelocity;
            _targetFloorY = floorY;
            _isLanded = false;
            _isVacuuming = false;
            _targetHelper = null;
            _currentVacuumSpeed = _initialSpeed;

            UpdateMaterialColor();
        }

        private void Update()
        {
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

            // 2. 헬퍼의 3m 진공청소기 흡수 이동
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

                // 흡수 회전
                transform.Rotate(Vector3.up, 480f * Time.deltaTime, Space.World);

                // 헬퍼 도달 판정
                if (Vector3.Distance(transform.position, targetPos) < 0.4f)
                {
                    CollectByHelper();
                }
                return;
            }

            // 3. 바닥 대기 중: 반짝이는 회전 및 부유(Bobbing) 애니메이션
            transform.Rotate(Vector3.up, 75f * Time.deltaTime, Space.World);
            float y = _targetFloorY + Mathf.Sin((Time.time + _bobOffset) * 3.5f) * 0.10f;
            transform.position = new Vector3(transform.position.x, y, transform.position.z);
        }

        /// <summary>
        /// 헬퍼가 3미터 이내로 접근했을 때 진공 흡수 시작
        /// </summary>
        public void StartVacuum(HelperCompanion helper)
        {
            if (_isVacuuming || helper == null) return;
            _isVacuuming = true;
            _targetHelper = helper;
            _currentVacuumSpeed = _initialSpeed;
        }

        private void CollectByHelper()
        {
            if (_targetHelper != null)
            {
                _targetHelper.CollectGem(_expValue);
            }

            SpawnCollectEffect(transform.position);

            ActiveGems.Remove(this);
            Destroy(gameObject);
        }

        private void SpawnCollectEffect(Vector3 pos)
        {
            if (Core.VFXManager.Instance != null)
            {
                Core.VFXManager.Instance.SpawnGemCollectEffect(pos);
                return;
            }

            // 수집 시 반짝이는 스파크 파티클 / 플래시
            var fx = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fx.name = "GemCollectFx";
            fx.transform.position = pos;
            fx.transform.localScale = Vector3.one * 0.35f;

            var col = fx.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var r = fx.GetComponent<Renderer>();
            if (r != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
                mat.color = _expValue >= 2 ? new Color(1f, 0.85f, 0.2f, 0.9f) : new Color(0.2f, 0.95f, 0.85f, 0.9f);
                r.material = mat;
            }

            Destroy(fx, 0.22f);
        }

        public static void SpawnGems(Vector3 center, int count, int expPerGem, float floorY = 0.45f)
        {
            if (count <= 0) return;

            for (int i = 0; i < count; i++)
            {
                Vector3 spawnPos = center + Vector3.up * 0.4f;
                Vector3 vel;

                if (count == 1)
                {
                    vel = new Vector3(Random.Range(-0.8f, 0.8f), Random.Range(2.5f, 4.0f), Random.Range(-0.8f, 0.8f));
                }
                else
                {
                    float angle = (i / (float)count) * Mathf.PI * 2f + Random.Range(-0.25f, 0.25f);
                    float spreadSpeed = Random.Range(2.2f, 4.2f);
                    vel = new Vector3(Mathf.Cos(angle) * spreadSpeed, Random.Range(3.2f, 5.2f), Mathf.Sin(angle) * spreadSpeed);
                }

                var gemObj = new GameObject($"ExpGem_v{expPerGem}");
                gemObj.transform.position = spawnPos;
                var gem = gemObj.AddComponent<ExpGem>();
                gem.Initialize(expPerGem, vel, floorY);
            }
        }

        private void SetupVisualIfMissing()
        {
            _renderer = GetComponent<Renderer>();
            if (_renderer == null)
            {
                var filter = gameObject.AddComponent<MeshFilter>();
                if (_diamondMesh == null)
                {
                    _diamondMesh = CreateOctahedronDiamondMesh();
                }
                filter.sharedMesh = _diamondMesh;

                _renderer = gameObject.AddComponent<MeshRenderer>();
            }

            // 물리 콜라이더 (플레이어 충돌 수집 방지: 트리거이나 캐릭터가 밟아도 줍지 않음)
            var col = GetComponent<SphereCollider>();
            if (col == null)
            {
                col = gameObject.AddComponent<SphereCollider>();
                col.isTrigger = true;
                col.radius = 0.35f;
            }

            transform.localScale = Vector3.one * (_expValue >= 3 ? 0.45f : 0.32f);
            UpdateMaterialColor();
        }

        private void UpdateMaterialColor()
        {
            if (_renderer == null) return;

            Color gemColor = _expValue switch
            {
                >= 3 => new Color(1.0f, 0.82f, 0.15f), // 고등급 보석: 영롱한 황금빛
                2 => new Color(0.2f, 0.85f, 1.0f),     // 중등급: 비취빛 청옥
                _ => new Color(0.1f, 0.95f, 0.75f)     // 기본: 영혼 에메랄드/청록
            };

            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
            mat.color = gemColor;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", gemColor);
            _renderer.material = mat;
        }

        /// <summary>
        /// 다면체 영혼석(Gem) 메쉬 프로시저럴 생성
        /// </summary>
        private static Mesh CreateOctahedronDiamondMesh()
        {
            Mesh m = new Mesh { name = "Procedural_GemMesh" };

            // 6개 정점: 상단 apex, 하단 apex, 적도 4각
            Vector3 top = new Vector3(0, 0.5f, 0);
            Vector3 bottom = new Vector3(0, -0.5f, 0);
            Vector3 f = new Vector3(0, 0, 0.38f);
            Vector3 r = new Vector3(0.38f, 0, 0);
            Vector3 b = new Vector3(0, 0, -0.38f);
            Vector3 l = new Vector3(-0.38f, 0, 0);

            // 8개 삼각형 (상단 4개, 하단 4개)
            Vector3[] vertices = new Vector3[]
            {
                // Top pyramid
                top, f, r,
                top, r, b,
                top, b, l,
                top, l, f,
                // Bottom pyramid
                bottom, r, f,
                bottom, b, r,
                bottom, l, b,
                bottom, f, l
            };

            int[] triangles = new int[24];
            for (int i = 0; i < 24; i++) triangles[i] = i;

            m.vertices = vertices;
            m.triangles = triangles;
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }
    }
}
