using System.Collections.Generic;
using MythOfGuotaji.Core;
using MythOfGuotaji.Data;
using UnityEngine;

namespace MythOfGuotaji.Combat
{
    /// <summary>
    /// 화살 및 원거리 투사체 (SphereCast + Trigger 이중 검출, 풀링 및 사거리/관통 지원)
    /// - 몬스터 투사체의 경우 MonsterSkillTable에서 전달된 직접 참조 VFX/Sound 프리팹을 우선 사용
    /// </summary>
    public class Projectile : MonoBehaviour, IReflectable
    {
        public static readonly List<IReflectable> ActiveReflectables = new();

        [Header("Projectile Settings")]
        [SerializeField] private float _speed = 24f;
        [SerializeField] private float _lifeTime = 2.5f;
        [SerializeField] private LayerMask _targetLayer;
        [SerializeField] private float _hitRadius = 0.4f;
        [SerializeField] private float _maxRange = 15f;

        private DamageInfo _damageInfo;
        private int _pierceCount = 0;
        private bool _unlimitedPierce = false;
        private float _spawnTime;
        private Vector3 _spawnPos;
        private string _poolKey;
        private bool _isFromPlayer = true;
        private readonly HashSet<int> _hitInstanceIds = new();
        [SerializeField] private ProjectileParryType _parryType = ProjectileParryType.Normal;

        private GameObject _customImpactVfxPrefab;
        private GameObject _customParryVfxPrefab;
        private GameObject _customImpactSoundPrefab;

        public float MaxRange => _maxRange;
        public float Speed => _speed;
        public bool IsFromPlayer => _isFromPlayer;
        public Vector3 Position => transform.position;
        public ProjectileParryType ParryType
        {
            get => _parryType;
            set
            {
                _parryType = value;
                ApplyParryTypeVisual();
            }
        }

        private void Awake()
        {
            ApplyTopDepthPriority();
        }

        private void OnEnable()
        {
            ApplyTopDepthPriority();
            if (!ActiveReflectables.Contains(this))
            {
                ActiveReflectables.Add(this);
            }
        }

        public void ApplyTopDepthPriority()
        {
            var rends = GetComponentsInChildren<Renderer>(true);
            foreach (var r in rends)
            {
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
            ActiveReflectables.Remove(this);
        }

        public void ConfigureSkillAssets(GameObject impactVfxPrefab, GameObject parryVfxPrefab, GameObject impactSoundPrefab)
        {
            _customImpactVfxPrefab = impactVfxPrefab;
            _customParryVfxPrefab = parryVfxPrefab;
            _customImpactSoundPrefab = impactSoundPrefab;
        }

        public void Reflect(GameObject newAttacker, Vector3? customDirection = null, float damageMultiplier = 1.5f, float speedMultiplier = 1.8f)
        {
            if (_parryType == ProjectileParryType.Unparryable) return;

            _isFromPlayer = true;
            _damageInfo.attacker = newAttacker;
            _damageInfo.amount *= damageMultiplier;
            float critChance = DaggerWeapon.Instance != null ? DaggerWeapon.Instance.CritChance : 0.15f;
            _damageInfo.isCritical = UnityEngine.Random.value < critChance;
            _damageInfo.isReflected = true;
            _speed *= speedMultiplier;

            Vector3 reflectDir = customDirection ?? -transform.forward;
            reflectDir.y = 0f;
            if (reflectDir.sqrMagnitude < 0.001f) reflectDir = -transform.forward;
            reflectDir.Normalize();

            transform.forward = reflectDir;
            _spawnPos = transform.position;
            _spawnTime = Time.time;
            _hitInstanceIds.Clear();
            _targetLayer = ~LayerMask.GetMask("Ignore Raycast");

            if (_customParryVfxPrefab != null)
            {
                MonsterTableAssetPlayer.SpawnVfx(_customParryVfxPrefab, transform.position, Quaternion.identity);
            }

            var rend = GetComponentInChildren<Renderer>();
            if (rend != null && rend.material != null)
            {
                Color reflectCol = new Color(0.2f, 0.95f, 1f);
                rend.material.color = reflectCol;
                if (rend.material.HasProperty("_BaseColor")) rend.material.SetColor("_BaseColor", reflectCol);
            }
        }

        public void ApplyParryTypeVisual()
        {
            var rends = GetComponentsInChildren<Renderer>(true);
            Color typeColor = _parryType switch
            {
                ProjectileParryType.Enhanced => new Color(1.0f, 0.82f, 0.1f, 1f),
                ProjectileParryType.Unparryable => new Color(0.95f, 0.18f, 0.18f, 1f),
                _ => new Color(0.2f, 0.95f, 0.35f, 1f)
            };

            foreach (var r in rends)
            {
                if (r != null && r.material != null)
                {
                    r.material.color = typeColor;
                    if (r.material.HasProperty("_BaseColor")) r.material.SetColor("_BaseColor", typeColor);
                }
            }
        }

        public void Initialize(DamageInfo damageInfo, int pierceCount, LayerMask targetLayer, string poolKey, float maxRange = 15f, bool unlimitedPierce = false, float speed = 24f, ProjectileParryType parryType = ProjectileParryType.Normal)
        {
            _damageInfo = damageInfo;
            _pierceCount = pierceCount;
            _targetLayer = targetLayer.value != 0 ? targetLayer : ~LayerMask.GetMask("Ignore Raycast");
            _poolKey = poolKey;
            _maxRange = maxRange;
            _unlimitedPierce = unlimitedPierce;
            _speed = speed;
            _parryType = parryType;
            _spawnTime = Time.time;
            _spawnPos = transform.position;
            _hitInstanceIds.Clear();
            _customImpactVfxPrefab = null;
            _customParryVfxPrefab = null;
            _customImpactSoundPrefab = null;

            _isFromPlayer = _damageInfo.attacker != null &&
                            (_damageInfo.attacker.CompareTag("Player") ||
                             _damageInfo.attacker.GetComponentInParent<Actor.PlayerController>() != null ||
                             _damageInfo.attacker.GetComponentInParent<Actor.CompanionAI>() != null);

            if (!_isFromPlayer)
            {
                ApplyParryTypeVisual();
            }
        }

        private void Update()
        {
            float moveDist = _speed * Time.deltaTime;
            Vector3 startPos = transform.position;
            Vector3 nextPos = startPos + transform.forward * moveDist;

            if (!_isFromPlayer)
            {
                var player = Actor.PlayerController.Instance != null ? Actor.PlayerController.Instance : FindAnyObjectByType<Actor.PlayerController>();
                if (player != null && !player.IsDead)
                {
                    Vector3 playerCenter = player.transform.position + Vector3.up * 1.0f;
                    float distToPlayer = Vector3.Distance(transform.position, playerCenter);
                    if (distToPlayer <= _hitRadius + 0.6f)
                    {
                        OnHitTarget(player.gameObject, playerCenter, transform.forward);
                        return;
                    }
                }
            }

            RaycastHit[] hits = Physics.SphereCastAll(startPos, _hitRadius, transform.forward, moveDist, _targetLayer, QueryTriggerInteraction.Ignore);

            if (hits != null && hits.Length > 0)
            {
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                foreach (var hit in hits)
                {
                    if (hit.collider.gameObject == _damageInfo.attacker) continue;
                    if (_damageInfo.attacker != null && hit.collider.transform.root == _damageInfo.attacker.transform.root) continue;

                    var damageable = hit.collider.GetComponentInParent<IDamageable>();
                    if (damageable != null && !damageable.IsDead)
                    {
                        bool isTargetPlayer = hit.collider.CompareTag("Player") || hit.collider.GetComponentInParent<Actor.PlayerController>() != null;
                        bool isTargetMonster = hit.collider.CompareTag("Monster") || hit.collider.GetComponentInParent<Actor.MonsterAI>() != null;

                        if (_isFromPlayer && !isTargetMonster) continue;
                        if (!_isFromPlayer && !isTargetPlayer) continue;

                        bool shouldStop = OnHitTarget(hit.collider.gameObject, hit.point, transform.forward);
                        if (shouldStop) return;
                    }
                    else if (!hit.collider.isTrigger)
                    {
                        Recycle();
                        return;
                    }
                }
            }

            transform.position = nextPos;

            float traveledDist = Vector3.Distance(_spawnPos, transform.position);
            if (traveledDist >= _maxRange || Time.time - _spawnTime >= _lifeTime)
            {
                Recycle();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject == _damageInfo.attacker) return;
            if (_damageInfo.attacker != null && other.transform.root == _damageInfo.attacker.transform.root) return;

            var damageable = other.GetComponentInParent<IDamageable>();
            if (damageable != null && !damageable.IsDead)
            {
                bool isTargetPlayer = other.CompareTag("Player") || other.GetComponentInParent<Actor.PlayerController>() != null;
                bool isTargetMonster = other.CompareTag("Monster") || other.GetComponentInParent<Actor.MonsterAI>() != null;

                if (_isFromPlayer && !isTargetMonster) return;
                if (!_isFromPlayer && !isTargetPlayer) return;

                OnHitTarget(other.gameObject, other.ClosestPoint(transform.position), transform.forward);
            }
        }

        private bool OnHitTarget(GameObject target, Vector3 hitPoint, Vector3 hitDirection)
        {
            int targetId = target.GetHashCode();
            if (_hitInstanceIds.Contains(targetId)) return false;
            _hitInstanceIds.Add(targetId);

            var damageable = target.GetComponentInParent<IDamageable>();
            if (damageable != null && !damageable.IsDead)
            {
                _damageInfo.hitPoint = hitPoint;
                _damageInfo.hitDirection = hitDirection;
                damageable.TakeDamage(_damageInfo);

                if (_customImpactVfxPrefab != null || _customImpactSoundPrefab != null)
                {
                    MonsterTableAssetPlayer.SpawnVfx(_customImpactVfxPrefab, hitPoint, Quaternion.identity);
                    MonsterTableAssetPlayer.PlayOneShotSound(_customImpactSoundPrefab, hitPoint);
                }
                else if (_isFromPlayer)
                {
                    Core.VFXManager.Instance?.SpawnHitImpact(hitPoint, hitDirection, _damageInfo.isCritical);
                }
            }

            if (!_unlimitedPierce)
            {
                _pierceCount--;
                if (_pierceCount < 0)
                {
                    Recycle();
                    return true;
                }
            }

            return false;
        }

        private void Recycle()
        {
            if (ObjectPoolManager.Instance != null && !string.IsNullOrEmpty(_poolKey))
            {
                ObjectPoolManager.Instance.Despawn(gameObject, _poolKey);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
