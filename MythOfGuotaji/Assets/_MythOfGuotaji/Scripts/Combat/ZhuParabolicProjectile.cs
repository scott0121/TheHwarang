using System.Collections;
using System.Collections.Generic;
using MythOfGuotaji.Actor;
using MythOfGuotaji.Cameras;
using MythOfGuotaji.Data;
using UnityEngine;

namespace MythOfGuotaji.Combat
{
    /// <summary>
    /// 주(鴸) 전용 2초 고정 비행 포물선 폭발 투사체
    /// - 모든 VFX / Telegraph / Sound는 MonsterSkillTable(zhu_parabolic)에 직접 등록된 GameObject만 사용
    /// </summary>
    public class ZhuParabolicProjectile : MonoBehaviour, IReflectable
    {
        [Header("Projectile Specs")]
        [SerializeField] private float _flightDuration = 2.0f;
        [SerializeField] private float _arcHeight = 6.0f;
        [SerializeField] private float _aoeRadius = 2.5f;
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
        private MonsterSkillEntry _skill;
        private GameObject _loopSoundObj;

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

        public void ApplyTopDepthPriority()
        {
            var rends = GetComponentsInChildren<Renderer>(true);
            foreach (var r in rends)
            {
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
            MonsterTableAssetPlayer.StopLoopSound(ref _loopSoundObj);
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

        public void Initialize(GameObject owner, Vector3 startPos, Vector3 targetPos, float damage = 18f, float flightDuration = 2.0f, float arcHeight = 6.0f, float aoeRadius = 2.5f, MonsterSkillEntry skill = null)
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
            _skill = skill ?? (GameDataManager.Instance?.MonsterSkills?.GetSkill("zhu_parabolic"));

            transform.position = startPos;

            SetupVisual();

            if (_skill != null)
            {
                if (_skill.trailVfxPrefab != null)
                {
                    var trail = Instantiate(_skill.trailVfxPrefab, transform, false);
                    trail.transform.localPosition = Vector3.zero;
                }
                if (_skill.enableLoopSound && _skill.loopSoundPrefab != null)
                {
                    _loopSoundObj = MonsterTableAssetPlayer.StartLoopSound(_skill.loopSoundPrefab, transform, startPos);
                }
            }

            CreateCircleTelegraph();
        }

        private void SetupVisual()
        {
            if (GetComponentInChildren<ParticleSystem>() != null)
            {
                return;
            }

            var rend = GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                var shader = Actor.ExpGem.ResolveSafeShader("Universal Render Pipeline/Unlit", "Universal Render Pipeline/Lit", "Sprites/Default", "Unlit/Color");
                _projMat = shader != null ? new Material(shader) : (rend.sharedMaterial != null ? new Material(rend.sharedMaterial) : null);
                if (_projMat != null)
                {
                    Color c = new Color(0.85f, 0.15f, 0.95f);
                    _projMat.color = c;
                    if (_projMat.HasProperty("_BaseColor")) _projMat.SetColor("_BaseColor", c);
                    rend.material = _projMat;
                }
            }
        }

        private static Shader s_cachedTelegraphShader;
        private static readonly System.Collections.Generic.Dictionary<Color32, Material> s_sharedTelegraphMaterials = new();
        private static readonly System.Collections.Generic.Dictionary<int, Mesh> s_cachedRingMeshes = new();

        public static Material GetSharedTelegraphMaterial(Color col)
        {
            Color32 key = col;
            if (s_sharedTelegraphMaterials.TryGetValue(key, out var mat) && mat != null)
            {
                return mat;
            }
            mat = CreateTelegraphMaterial(col);
            s_sharedTelegraphMaterials[key] = mat;
            return mat;
        }

        public static Material CreateTelegraphMaterial(Color col)
        {
            if (s_cachedTelegraphShader == null)
            {
                s_cachedTelegraphShader = Actor.ExpGem.ResolveSafeShader("Sprites/Default", "Universal Render Pipeline/Unlit", "Unlit/Color");
            }
            Material m = new Material(s_cachedTelegraphShader);
            m.color = col;
            if (m.HasProperty("_Color")) m.SetColor("_Color", col);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);
            if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1);
            if (m.HasProperty("_Blend")) m.SetFloat("_Blend", 0);
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 100;
            return m;
        }

        private void CreateCircleTelegraph()
        {
            if (_telegraphObj != null) Destroy(_telegraphObj);

            GameObject circlePrefab = _skill?.telegraphPrefab;
            if (circlePrefab == null)
            {
                return;
            }

            _telegraphObj = Instantiate(circlePrefab, new Vector3(_targetPos.x, 0.08f, _targetPos.z), Quaternion.identity);
            _telegraphObj.name = "[Zhu_CircleTelegraph]";

            var existingFill = _telegraphObj.transform.Find("TelegraphFill");
            GameObject fillObj = existingFill != null ? existingFill.gameObject : _telegraphObj;
            fillObj.transform.localScale = new Vector3(_aoeRadius * 2f, 0.01f, _aoeRadius * 2f);

            var mr = fillObj.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sortingOrder = 60;
                Color fillCol = _isReflected ? new Color(0.15f, 0.85f, 1f, 0.40f) : new Color(0.95f, 0.15f, 0.25f, 0.40f);
                _telegraphMat = CreateTelegraphMaterial(fillCol);
                mr.material = _telegraphMat;
            }

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

            int meshKey = Mathf.RoundToInt(outerR * 100f) * 10000 + Mathf.RoundToInt(innerR * 100f);
            if (!s_cachedRingMeshes.TryGetValue(meshKey, out Mesh mesh) || mesh == null)
            {
                mesh = new Mesh { name = $"CachedRing_{outerR:F2}_{innerR:F2}" };
                int segments = 40;
                Vector3[] verts = new Vector3[segments * 2];
                int[] tris = new int[segments * 12];

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

                    tris[triBase + 0] = currentInner;
                    tris[triBase + 1] = nextOuter;
                    tris[triBase + 2] = currentOuter;

                    tris[triBase + 3] = currentInner;
                    tris[triBase + 4] = nextInner;
                    tris[triBase + 5] = nextOuter;

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
                s_cachedRingMeshes[meshKey] = mesh;
            }
            mf.sharedMesh = mesh;

            Color borderCol = _isReflected ? new Color(0.35f, 0.95f, 1f, 0.9f) : new Color(1f, 0.3f, 0.15f, 0.9f);
            _borderMat = CreateTelegraphMaterial(borderCol);
            mr.material = _borderMat;
        }

        private void Update()
        {
            if (_hasLanded) return;

            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _flightDuration);

            Vector3 currentPos = Vector3.Lerp(_startPos, _targetPos, t);
            currentPos.y += 4f * _arcHeight * t * (1f - t);
            transform.position = currentPos;

            float nextT = Mathf.Clamp01((_elapsed + 0.02f) / _flightDuration);
            Vector3 nextPos = Vector3.Lerp(_startPos, _targetPos, nextT);
            nextPos.y += 4f * _arcHeight * nextT * (1f - nextT);
            Vector3 dir = (nextPos - currentPos);
            if (dir.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(dir.normalized);
            }

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

            if (t >= 1.0f)
            {
                Explode();
            }
        }

        private void Explode()
        {
            _hasLanded = true;
            MonsterTableAssetPlayer.StopLoopSound(ref _loopSoundObj);

            if (_telegraphObj != null)
            {
                Destroy(_telegraphObj);
            }

            if (QuarterViewCamera.Instance != null)
            {
                QuarterViewCamera.Instance.TriggerShake(0.35f, 0.15f);
            }

            if (_skill != null)
            {
                MonsterTableAssetPlayer.SpawnVfx(_skill.impactVfxPrefab, _targetPos, Quaternion.identity);
                MonsterTableAssetPlayer.PlayOneShotSound(_skill.impactSoundPrefab, _targetPos);
            }

            if (!_isReflected)
            {
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
                    }
                }
            }
            else
            {
                Collider[] hits = Physics.OverlapSphere(_targetPos, _aoeRadius + 0.5f);
                foreach (var col in hits)
                {
                    var monster = col.GetComponentInParent<MonsterAI>();
                    if (monster != null && !monster.IsDead)
                    {
                        Vector3 mPos = monster.transform.position;
                        mPos.y = 0f;
                        float critChanceZ = DaggerWeapon.Instance != null ? DaggerWeapon.Instance.CritChance : 0.15f;
                        bool isCritZ = UnityEngine.Random.value < critChanceZ;
                        DamageInfo info = new DamageInfo(_damage * _reflectedDamageMultiplier, isCritZ, _targetPos, (mPos - _targetPos).normalized, 7.0f, ElementType.Fire, PlayerController.Instance != null ? PlayerController.Instance.gameObject : null, isReflected: true);
                        monster.TakeDamage(info);
                    }
                }
            }

            Destroy(gameObject);
        }

        public void Reflect(GameObject newAttacker, Vector3? customDirection = null, float damageMultiplier = 1.5f, float speedMultiplier = 1.8f)
        {
            if (_hasLanded) return;

            _isReflected = true;
            _reflectedDamageMultiplier = damageMultiplier;
            _flightDuration = Mathf.Max(0.5f, _flightDuration / speedMultiplier);
            _startPos = transform.position;

            if (_owner != null)
            {
                _targetPos = _owner.transform.position;
            }
            else
            {
                _targetPos = _startPos - transform.forward * 15f;
            }
            _targetPos.y = 0.05f;

            _elapsed = 0f;

            if (_telegraphObj != null)
            {
                _telegraphObj.transform.position = new Vector3(_targetPos.x, 0.05f, _targetPos.z);
            }

            if (_projMat != null)
            {
                Color c = new Color(0.2f, 0.95f, 1f);
                _projMat.color = c;
                if (_projMat.HasProperty("_BaseColor")) _projMat.SetColor("_BaseColor", c);
            }

            if (_skill != null && _skill.parryVfxPrefab != null)
            {
                MonsterTableAssetPlayer.SpawnVfx(_skill.parryVfxPrefab, transform.position, Quaternion.identity);
            }
        }

        private void OnDestroy()
        {
            MonsterTableAssetPlayer.StopLoopSound(ref _loopSoundObj);
            if (_telegraphObj != null) Destroy(_telegraphObj);
            if (_telegraphMat != null) Destroy(_telegraphMat);
            if (_borderMat != null) Destroy(_borderMat);
            if (_projMat != null) Destroy(_projMat);
        }
    }
}
