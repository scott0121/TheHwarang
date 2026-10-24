using MythOfGuotaji.Combat;
using UnityEngine;

namespace MythOfGuotaji.Actor
{
    /// <summary>
    /// 시나리오 트리를 통해 합류하는 AI 동행 동료
    /// - 플레이어(거타지)와 일정 거리를 유지하며 미로를 따라 이동
    /// - 사거리 내 산해경 요괴를 감지하여 원거리/근접 지원 사격
    /// - 요괴들의 어그로를 분산하여 생존 보조
    /// </summary>
    public class CompanionAI : MonoBehaviour, IDamageable
    {
        [Header("Companion Stats")]
        [SerializeField] private string _companionId = "Companion_Howon";
        [SerializeField] private float _maxHp = 120f;
        [SerializeField] private float _moveSpeed = 6.8f;
        [SerializeField] private float _followDistance = 3.5f;

        [Header("Combat Support")]
        [SerializeField] private float _attackRange = 8f;
        [SerializeField] private float _attackDamage = 18f;
        [SerializeField] private float _attackCooldown = 1.4f;
        [SerializeField] private LayerMask _enemyLayer;

        private float _currentHp;
        private Transform _playerTransform;
        private CharacterController _cc;
        private float _lastAttackTime = -99f;

        public bool IsDead => _currentHp <= 0f;
        public Transform Transform => transform;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _currentHp = _maxHp;
        }

        private void Start()
        {
            var player = FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                _playerTransform = player.transform;
            }
        }

        private void Update()
        {
            if (IsDead || _playerTransform == null) return;

            Vector3 toPlayer = _playerTransform.position - transform.position;
            toPlayer.y = 0f;
            float distToPlayer = toPlayer.magnitude;

            // 1. 플레이어 추종 이동
            if (distToPlayer > _followDistance)
            {
                Vector3 moveDir = toPlayer.normalized * _moveSpeed;
                moveDir.y = -9.81f;
                if (_cc != null && _cc.enabled)
                {
                    _cc.Move(moveDir * Time.deltaTime);
                }
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toPlayer.normalized), Time.deltaTime * 8f);
            }

            // 2. 사거리 내 가장 가까운 요괴 탐색 및 지원 공격
            FindAndAttackEnemies();
        }

        private void FindAndAttackEnemies()
        {
            if (Time.time < _lastAttackTime + _attackCooldown) return;

            Collider[] hits = Physics.OverlapSphere(transform.position, _attackRange, _enemyLayer);
            Transform nearestEnemy = null;
            float minDist = float.MaxValue;

            foreach (var col in hits)
            {
                float d = Vector3.Distance(transform.position, col.transform.position);
                if (d < minDist)
                {
                    minDist = d;
                    nearestEnemy = col.transform;
                }
            }

            if (nearestEnemy != null)
            {
                _lastAttackTime = Time.time;
                Vector3 dir = (nearestEnemy.position - transform.position).normalized;
                dir.y = 0f;
                transform.rotation = Quaternion.LookRotation(dir);

                if (nearestEnemy.TryGetComponent<IDamageable>(out var damageable))
                {
                    DamageInfo dmg = new DamageInfo(_attackDamage, false, nearestEnemy.position, dir, 2f, ElementType.Lightning, gameObject);
                    damageable.TakeDamage(dmg);
                    Debug.Log($"<color=lightblue>[동료 {_companionId}] 요괴 지원 공격 성공!</color>");
                }
            }
        }

        public void TakeDamage(DamageInfo info)
        {
            if (IsDead) return;
            _currentHp -= info.amount;
            if (_currentHp <= 0f)
            {
                _currentHp = 0f;
                Debug.LogWarning($"[CompanionAI] {_companionId} 동료가 전투 불능 상태에 빠졌습니다.");
                gameObject.SetActive(false);
            }
        }
    }
}
