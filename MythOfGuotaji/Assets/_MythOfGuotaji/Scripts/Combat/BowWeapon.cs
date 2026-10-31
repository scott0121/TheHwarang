using MythOfGuotaji.Core;
using UnityEngine;

namespace MythOfGuotaji.Combat
{
    /// <summary>
    /// 거타지의 주무기: 활 (Bow)
    /// - 기본 속사(15m 사거리, 멀티샷/관통 적용)
    /// - 차징샷(최대 2초 모으기, 2배 피해, 25m 사거리, 무제한 관통, 강력한 넉백, 멀티샷 미적용 단발)
    /// - 일반 화살 / 강력한 화살 전용 프리팹 분리 지원
    /// </summary>
    public class BowWeapon : MonoBehaviour
    {
        [Header("Weapon Config")]
        [SerializeField] private string _currentBowId = "Bow_00";
        [SerializeField] private GameObject _normalArrowPrefab;
        [SerializeField] private GameObject _chargedArrowPrefab;
        [SerializeField] private GameObject _fireSoundPrefab;
        [SerializeField] private GameObject _chargedFireSoundPrefab;
        [SerializeField] private Transform _firePoint;
        [SerializeField] private float _baseDamage = 25f;
        [SerializeField] private float _attackRate = 2.8f;
        [SerializeField] private float _baseAttackRate = 2.8f; // 초당 발사 횟수
        [SerializeField] private float _critChance = 0.15f;
        [SerializeField] private float _critMultiplier = 2.0f;
        [SerializeField] private LayerMask _enemyLayer;

        [Header("Range & Speed Config")]
        [SerializeField] private float _normalRange = 15f;
        [SerializeField] private float _chargedRange = 25f;
        [SerializeField] private float _normalSpeed = 24f;
        [SerializeField] private float _chargedSpeed = 36f; // 1.5배속 (24 * 1.5 = 36m/s)
        [SerializeField] private float _chargedDamageMultiplier = 2.0f;
        [SerializeField] private float _chargedKnockback = 12.0f;

        [Header("Charge Config (from Table)")]
        [SerializeField] private float _chargeStartTime = 0.3f;
        [SerializeField] private float _maxChargeTime = 3.0f;
        [SerializeField] private float _chargeMinRange = 0.0f;
        [SerializeField] private float _chargeMaxRange = 25.0f;
        [SerializeField] private float _chargeTelegraphWidth = 0.3f;

        [Header("Upgraded Stats (Boon & Skill Driven)")]
        public int arrowCount = 1;           // 발사 화살 수 (멀티샷 - 일반 공격에만 적용)
        public float spreadAngle = 12f;      // 멀티샷 각도
        public int pierceCount = 0;          // 기본 관통 횟수 (일반 공격용)
        public float knockbackPower = 4.5f;  // 일반 공격 넉백 수치
        public ElementType element = ElementType.Physical;

        [Header("Burst Settings")]
        [SerializeField] private int _burstShotCount = 5;
        [SerializeField] private float _burstTotalDuration = 1.5f;
        [SerializeField] private float _burstCooldown = 2.0f;
        [SerializeField] private float _multiHitDamageFalloffRatio = 0.5f;

        private float _lastFireTime = 0f;
        private Coroutine _burstCoroutine;
        private bool _isBursting = false;
        private static int _nextVolleyId = 1;

        public string CurrentBowId => _currentBowId;
        public bool CanFire => Time.time >= _lastFireTime + (1f / _attackRate);
        public bool IsBursting => _isBursting;
        public float AttackRate => _attackRate;
        public float BaseAttackRate => _baseAttackRate > 0.001f ? _baseAttackRate : 2.8f;
        public float AttackRateRatio => BaseAttackRate > 0.001f ? Mathf.Max(0.1f, _attackRate / BaseAttackRate) : 1.0f;
        public float NormalRange => _normalRange;
        public float ChargedRange => _chargedRange;
        public float ChargeStartTime => _chargeStartTime;
        public float MaxChargeTime => _maxChargeTime;
        public float ChargeMinRange => _chargeMinRange;
        public float ChargeMaxRange => _chargeMaxRange;
        public float ChargeTelegraphWidth => _chargeTelegraphWidth;

        private void Awake()
        {
            if (_enemyLayer.value == 0)
            {
                _enemyLayer = ~LayerMask.GetMask("Ignore Raycast");
            }

            string targetBow = "Bow_00";
            if (Core.GameManager.Instance?.SaveData != null && !string.IsNullOrEmpty(Core.GameManager.Instance.SaveData.equippedBowId))
            {
                targetBow = Core.GameManager.Instance.SaveData.equippedBowId;
            }
            EquipBow(targetBow);
        }

        public void EquipBow(string bowId)
        {
            _currentBowId = string.IsNullOrEmpty(bowId) ? "Bow_00" : bowId;
            ApplyStatsFromTable();
        }

        public void ApplyStatsFromTable()
        {
            if (Data.GameDataManager.Instance != null && Data.GameDataManager.Instance.WeaponStats != null)
            {
                var bowEntry = Data.GameDataManager.Instance.WeaponStats.GetBow(_currentBowId);
                if (bowEntry != null)
                {
                    var bowStats = bowEntry.stats;
                    _baseDamage = bowStats.baseDamage;
                    _attackRate = bowStats.attackRate;
                    _baseAttackRate = bowStats.attackRate > 0f ? bowStats.attackRate : 2.8f;
                    _critChance = bowStats.critChance;
                    _critMultiplier = bowStats.critMultiplier;
                    arrowCount = bowStats.baseArrowCount;
                    spreadAngle = bowStats.spreadAngle;
                    pierceCount = bowStats.basePierceCount;
                    knockbackPower = bowStats.knockbackPower;
                    _normalSpeed = bowStats.projectileSpeed > 0f ? bowStats.projectileSpeed : 24f;
                    _chargeStartTime = bowStats.chargeStartTime;
                    _maxChargeTime = bowStats.maxChargeTime;
                    _normalRange = bowStats.normalRange;
                    _chargeMinRange = bowStats.chargeMinRange;
                    _chargeMaxRange = bowStats.chargeMaxRange;
                    _chargedRange = bowStats.chargeMaxRange;
                    _chargedDamageMultiplier = bowStats.chargeDamageMultiplier;
                    _chargedSpeed = bowStats.chargeProjectileSpeed;
                    _chargedKnockback = bowStats.chargeKnockback;
                    _chargeTelegraphWidth = bowStats.chargeTelegraphWidth;
                    _burstShotCount = bowStats.burstShotCount;
                    _burstTotalDuration = bowStats.burstTotalDuration;
                    _burstCooldown = bowStats.burstCooldown;
                    _multiHitDamageFalloffRatio = bowStats.multiHitDamageFalloffRatio;

                    if (bowEntry.normalArrowPrefab != null) _normalArrowPrefab = bowEntry.normalArrowPrefab;
                    if (bowEntry.chargedArrowPrefab != null) _chargedArrowPrefab = bowEntry.chargedArrowPrefab;
                    if (bowEntry.fireSoundPrefab != null) _fireSoundPrefab = bowEntry.fireSoundPrefab;
                    if (bowEntry.chargedFireSoundPrefab != null) _chargedFireSoundPrefab = bowEntry.chargedFireSoundPrefab;
                }
            }

            // 폴백: PlayerStatsTable 및 PrefabDatabase 확인
            if (_normalArrowPrefab == null)
            {
                var playerTable = Data.GameDataManager.Instance?.PlayerStats;
                if (playerTable != null && playerTable.defaultArrowPrefab != null)
                {
                    _normalArrowPrefab = playerTable.defaultArrowPrefab;
                }
                else if (Data.GameDataManager.Instance?.Prefabs != null)
                {
                    _normalArrowPrefab = Data.GameDataManager.Instance.Prefabs.arrowProjectilePrefab;
                }
            }

            if (_chargedArrowPrefab == null)
            {
                var playerTable = Data.GameDataManager.Instance?.PlayerStats;
                if (playerTable != null && playerTable.defaultChargedArrowPrefab != null)
                {
                    _chargedArrowPrefab = playerTable.defaultChargedArrowPrefab;
                }
                else if (Data.GameDataManager.Instance?.Prefabs != null)
                {
                    _chargedArrowPrefab = Data.GameDataManager.Instance.Prefabs.chargedArrowProjectilePrefab;
                }
            }
        }

        /// <summary>
        /// 기본 화살 발사 (단발 또는 1회 발사 분량)
        /// </summary>
        public void FireNormal(Vector3 aimDirection, GameObject owner)
        {
            if (!CanFire) return;
            _lastFireTime = Time.time;
            SpawnNormalArrowVolley(aimDirection, owner);
        }

        /// <summary>
        /// 좌클릭 릴리즈 1회당 1.5초간 5회 연사 발사
        /// </summary>
        public void FireBurst5(System.Func<Vector3> getAimDirection, GameObject owner)
        {
            if (_burstCoroutine != null) StopCoroutine(_burstCoroutine);
            _burstCoroutine = StartCoroutine(Burst5Routine(getAimDirection, owner));
        }

        private System.Collections.IEnumerator Burst5Routine(System.Func<Vector3> getAimDirection, GameObject owner)
        {
            _isBursting = true;
            int totalShots = _burstShotCount > 0 ? _burstShotCount : 5;
            float ratio = AttackRateRatio;
            float baseDuration = _burstTotalDuration > 0f ? _burstTotalDuration : 1.5f;
            float totalDuration = baseDuration / ratio;
            float interval = totalShots > 1 ? totalDuration / (totalShots - 1) : totalDuration;

            for (int i = 0; i < totalShots; i++)
            {
                Vector3 aim = getAimDirection != null ? getAimDirection() : transform.forward;
                SpawnNormalArrowVolley(aim, owner);

                if (i < totalShots - 1)
                {
                    yield return new WaitForSeconds(interval);
                }
            }

            _isBursting = false;
            _burstCoroutine = null;
        }

        public void StopBurst()
        {
            if (_burstCoroutine != null)
            {
                StopCoroutine(_burstCoroutine);
                _burstCoroutine = null;
            }
            _isBursting = false;
        }

        public void SpawnNormalArrowVolley(Vector3 aimDirection, GameObject owner)
        {
            aimDirection.y = 0f;
            if (aimDirection.sqrMagnitude < 0.001f) aimDirection = transform.forward;
            aimDirection.Normalize();

            int currentVolleyId = _nextVolleyId++;

            if (_fireSoundPrefab != null)
            {
                Core.VFXManager.Instance?.PlaySoundPrefab(_fireSoundPrefab, transform.position);
            }

            Vector3 spawnPos = _firePoint != null 
                ? _firePoint.position 
                : new Vector3(transform.position.x, 1.0f, transform.position.z) + aimDirection * 0.8f;

            // 멀티샷 각도 분배
            float startAngle = -spreadAngle * (arrowCount - 1) * 0.5f;

            for (int i = 0; i < arrowCount; i++)
            {
                float currentAngle = startAngle + (i * spreadAngle);
                Quaternion rot = Quaternion.Euler(0f, currentAngle, 0f) * Quaternion.LookRotation(aimDirection);

                GameObject arrowObj;
                string poolKey = _normalArrowPrefab != null ? _normalArrowPrefab.name : "DefaultNormalArrow";

                if (_normalArrowPrefab != null && ObjectPoolManager.Instance != null)
                {
                    arrowObj = ObjectPoolManager.Instance.Spawn(_normalArrowPrefab, spawnPos, rot);
                }
                else
                {
                    arrowObj = CreateFallbackNormalArrow(spawnPos, rot);
                }

                bool isCrit = Random.value < _critChance;
                float finalDamage = isCrit ? _baseDamage * _critMultiplier : _baseDamage;

                DamageInfo dmg = new DamageInfo(
                    finalDamage,
                    isCrit,
                    spawnPos,
                    rot * Vector3.forward,
                    knockbackPower,
                    element,
                    owner,
                    currentVolleyId
                );

                if (arrowObj.TryGetComponent<Projectile>(out var proj))
                {
                    proj.Initialize(dmg, pierceCount, _enemyLayer, poolKey, maxRange: _normalRange, unlimitedPierce: false, speed: _normalSpeed);
                }
            }
        }

        /// <summary>
        /// 차징 화살 발사: 0.3s~3.0s 차징에 따라 사거리(0~25m) 선형 증가, 2배 피해, 1.5배속, 무제한 관통, 강력한 넉백
        /// </summary>
        public void FireCharged(Vector3 aimDirection, GameObject owner, float range = -1f)
        {
            _lastFireTime = Time.time;

            if (_chargedFireSoundPrefab != null)
            {
                Core.VFXManager.Instance?.PlaySoundPrefab(_chargedFireSoundPrefab, transform.position);
            }

            float actualRange = range < 0f ? _chargedRange : Mathf.Clamp(range, 0.2f, _chargedRange);

            aimDirection.y = 0f;
            aimDirection.Normalize();

            Vector3 spawnPos = _firePoint != null 
                ? _firePoint.position 
                : new Vector3(transform.position.x, 1.0f, transform.position.z) + aimDirection * 0.8f;

            Quaternion rot = Quaternion.LookRotation(aimDirection);

            GameObject arrowObj;
            string poolKey = _chargedArrowPrefab != null ? _chargedArrowPrefab.name : "DefaultChargedArrow";

            if (_chargedArrowPrefab != null && ObjectPoolManager.Instance != null)
            {
                arrowObj = ObjectPoolManager.Instance.Spawn(_chargedArrowPrefab, spawnPos, rot);
            }
            else
            {
                arrowObj = CreateFallbackChargedArrow(spawnPos, rot);
            }

            // 차징샷: 기본 피해의 2배 및 치명타 추가 적용
            bool isCrit = Random.value < _critChance;
            float finalDamage = _baseDamage * _chargedDamageMultiplier;
            if (isCrit) finalDamage *= _critMultiplier;

            DamageInfo dmg = new DamageInfo(
                finalDamage,
                isCrit,
                spawnPos,
                rot * Vector3.forward,
                _chargedKnockback,
                element,
                owner,
                volleyId: 0,
                isCharged: true
            );

            if (arrowObj.TryGetComponent<Projectile>(out var proj))
            {
                // 충전 사거리, 1.5배 비행 속도(_chargedSpeed = 36f), 무제한 관통 (unlimitedPierce: true)
                proj.Initialize(dmg, 9999, _enemyLayer, poolKey, maxRange: actualRange, unlimitedPierce: true, speed: _chargedSpeed);
            }

            Debug.Log($"<color=cyan>[강력한 차징 화살 발사!] 피해량: {finalDamage:F0} (2배), 사거리: {actualRange:F1}m/{_chargedRange}m, 속도: {_chargedSpeed}m/s, 무제한 관통!</color>");
        }

        // 이전 인터페이스 호환용
        public void Fire(Vector3 aimDirection, GameObject owner)
        {
            FireNormal(aimDirection, owner);
        }

        private GameObject CreateFallbackNormalArrow(Vector3 pos, Quaternion rot)
        {
            GameObject arrow = new GameObject("FallbackArrow_Normal");
            arrow.transform.SetPositionAndRotation(pos, rot);

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.name = "ArrowVisual";
            visual.transform.SetParent(arrow.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            visual.transform.localScale = new Vector3(0.08f, 0.45f, 0.08f);

            var visualRend = visual.GetComponent<Renderer>();
            if (visualRend != null)
            {
                visualRend.sharedMaterial = ZhuParabolicProjectile.GetSharedTelegraphMaterial(new Color(1f, 0.85f, 0.2f, 1f)); // 밝은 금빛 화살
            }

            if (visual.TryGetComponent<Collider>(out var visualCol))
            {
                Destroy(visualCol);
            }

            var box = arrow.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(0.5f, 0.5f, 1.2f);

            var rb = arrow.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            arrow.AddComponent<Projectile>();
            return arrow;
        }

        private GameObject CreateFallbackChargedArrow(Vector3 pos, Quaternion rot)
        {
            GameObject arrow = new GameObject("FallbackArrow_Charged");
            arrow.transform.SetPositionAndRotation(pos, rot);

            // 대형 화살대 시각화
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.name = "ChargedArrowVisual";
            visual.transform.SetParent(arrow.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            visual.transform.localScale = new Vector3(0.22f, 0.85f, 0.22f);

            var visualRend = visual.GetComponent<Renderer>();
            if (visualRend != null)
            {
                // 강렬하게 타오르는 푸른 번개/적황빛 영기 화살
                visualRend.sharedMaterial = ZhuParabolicProjectile.GetSharedTelegraphMaterial(new Color(0.2f, 0.85f, 1.0f, 1f));
            }

            if (visual.TryGetComponent<Collider>(out var visualCol))
            {
                Destroy(visualCol);
            }

            // 대형 타격 판정 박스
            var box = arrow.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(1.2f, 1.0f, 2.2f);

            var rb = arrow.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            arrow.AddComponent<Projectile>();
            return arrow;
        }

        public void SetAttackRate(float rate) => _attackRate = rate;
        public void AddAttackRate(float add) => _attackRate += add;
        public void AddBaseDamage(float add) => _baseDamage += add;
        public void AddCritChance(float add) => _critChance = Mathf.Clamp01(_critChance + add);
        public void AddChargeKnockbackMultiplier(float mult) => _chargedKnockback *= (1f + mult);
        public void AddChargeDamageMultiplier(float add) => _chargedDamageMultiplier += add;
        public void AddChargeMaxRange(float add) { _chargeMaxRange += add; _chargedRange += add; }
        public void ReduceChargeDuration(float percent)
        {
            float span = _maxChargeTime - _chargeStartTime;
            float reduction = span * percent;
            _maxChargeTime = Mathf.Max(_chargeStartTime + 0.5f, _maxChargeTime - reduction);
        }
    }
}
