using System.Collections.Generic;
using MythOfGuotaji.Core;
using UnityEngine;

namespace MythOfGuotaji.Combat
{
    /// <summary>
    /// 화살 및 원거리 투사체 (SphereCast + Trigger 이중 검출, 풀링 및 사거리/관통 지원)
    /// - 사거리 제한 지원 (일반 화살 15m, 강력한 화살 25m)
    /// - 무제한 관통 및 적 타격 중복 방지
    /// - IReflectable 구현: 단검 패링 시 날아오던 각도로 반사
    /// </summary>
    public class Projectile : MonoBehaviour, IReflectable
    {
        public static readonly List<IReflectable> ActiveReflectables = new();

        [Header("Projectile Settings")]
        [SerializeField] private float _speed = 24f;
        [SerializeField] private float _lifeTime = 2.5f;
        [SerializeField] private LayerMask _targetLayer;
        [SerializeField] private float _hitRadius = 0.4f; // 스윕 검출 반경
        [SerializeField] private float _maxRange = 15f;    // 최대 비행 사거리

        private DamageInfo _damageInfo;
        private int _pierceCount = 0;
        private bool _unlimitedPierce = false;
        private float _spawnTime;
        private Vector3 _spawnPos;
        private string _poolKey;
        private bool _isFromPlayer = true;
        private readonly HashSet<int> _hitInstanceIds = new();

        public float MaxRange => _maxRange;
        public float Speed => _speed;
        public bool IsFromPlayer => _isFromPlayer;
        public Vector3 Position => transform.position;

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

        /// <summary>
        /// 탄환류의 뎁스를 최우선(sortingOrder 5000, renderQueue 3500)으로 설정하여 모든 바닥/배경/폭발 이펙트 위로 선명하게 표시
        /// </summary>
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

        /// <summary>
        /// 단검 패링: 날아오던 각도로 반사하며 플레이어 발사체로 전환
        /// </summary>
        public void Reflect(GameObject newAttacker, Vector3? customDirection = null)
        {
            _isFromPlayer = true;
            _damageInfo.attacker = newAttacker;
            _damageInfo.amount *= 1.5f; // 반사된 투사체 위력 강화
            _damageInfo.isCritical = true;

            // 날아오던 각도의 정반대 방향으로 반사
            Vector3 reflectDir = customDirection ?? -transform.forward;
            reflectDir.y = 0f;
            if (reflectDir.sqrMagnitude < 0.001f) reflectDir = -transform.forward;
            reflectDir.Normalize();

            transform.forward = reflectDir;
            _spawnPos = transform.position;
            _spawnTime = Time.time;
            _hitInstanceIds.Clear();
            _targetLayer = ~LayerMask.GetMask("Ignore Raycast");

            var rend = GetComponentInChildren<Renderer>();
            if (rend != null && rend.material != null)
            {
                Color reflectCol = new Color(0.2f, 0.95f, 1f);
                rend.material.color = reflectCol;
                if (rend.material.HasProperty("_BaseColor")) rend.material.SetColor("_BaseColor", reflectCol);
            }

            Debug.Log($"<color=yellow><b>[투사체 반사 성공!]</b> {gameObject.name}이(가) 날아오던 각도로 튕겨져 적을 향해 날아갑니다!</color>");
        }

        public void Initialize(DamageInfo damageInfo, int pierceCount, LayerMask targetLayer, string poolKey, float maxRange = 15f, bool unlimitedPierce = false, float speed = 24f)
        {
            _damageInfo = damageInfo;
            _pierceCount = pierceCount;
            _targetLayer = targetLayer.value != 0 ? targetLayer : ~LayerMask.GetMask("Ignore Raycast");
            _poolKey = poolKey;
            _maxRange = maxRange;
            _unlimitedPierce = unlimitedPierce;
            _speed = speed;
            _spawnTime = Time.time;
            _spawnPos = transform.position;
            _hitInstanceIds.Clear();

            _isFromPlayer = _damageInfo.attacker != null &&
                            (_damageInfo.attacker.CompareTag("Player") ||
                             _damageInfo.attacker.GetComponentInParent<Actor.PlayerController>() != null ||
                             _damageInfo.attacker.GetComponentInParent<Actor.CompanionAI>() != null);
        }

        private void Update()
        {
            float moveDist = _speed * Time.deltaTime;
            Vector3 startPos = transform.position;
            Vector3 nextPos = startPos + transform.forward * moveDist;

            // 0. 적(몬스터) 발사체일 경우 플레이어와의 근접 직격 검사 (신뢰성 보장)
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

            // 1. SphereCastAll로 비행 경로 상의 모든 충돌체 스윕 검사
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

                        // 아군 오사 방지: 플레이어 발사체는 몬스터만 타격, 몬스터 발사체는 플레이어만 타격
                        if (_isFromPlayer && !isTargetMonster) continue;
                        if (!_isFromPlayer && !isTargetPlayer) continue;

                        bool shouldStop = OnHitTarget(hit.collider.gameObject, hit.point, transform.forward);
                        if (shouldStop) return;
                    }
                    else if (!hit.collider.isTrigger)
                    {
                        // 벽 등 비트리거 지형 충돌
                        Recycle();
                        return;
                    }
                }
            }

            transform.position = nextPos;

            // 2. 최대 비행 사거리 도달 또는 수명 초과 시 풀 반환
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

        /// <summary>
        /// 타깃 타격 처리. 관통 소진 등으로 투사체가 파괴되어야 하면 true 반환.
        /// </summary>
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

                // 피격 VFX 연출 (독성 탄환은 독물 튐, 일반/차징은 히트 스파크)
                if (_damageInfo.element == ElementType.Poison)
                {
                    Core.VFXManager.Instance?.SpawnVenomSplash(hitPoint);
                }
                else
                {
                    Core.VFXManager.Instance?.SpawnHitImpact(hitPoint, hitDirection, _damageInfo.isCritical);
                }

                Debug.Log($"<color=orange>[화살 적중!] {target.name}에게 {_damageInfo.amount:F0} 피해! (치명타: {_damageInfo.isCritical}, 무제한 관통: {_unlimitedPierce})</color>");
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
