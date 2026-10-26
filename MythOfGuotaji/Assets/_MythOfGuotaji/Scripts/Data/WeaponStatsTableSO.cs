using UnityEngine;

namespace MythOfGuotaji.Data
{
    [System.Serializable]
    public class BowBalanceStats
    {
        [Tooltip("기본 피해량")]
        public float baseDamage = 25f;

        [Tooltip("초당 발사 속도")]
        public float attackRate = 2.8f;

        [Tooltip("치명타 확률 (0.0 ~ 1.0)")]
        public float critChance = 0.15f;

        [Tooltip("치명타 피해 배율")]
        public float critMultiplier = 2.0f;

        [Tooltip("기본 발사 화살 수")]
        public int baseArrowCount = 1;

        [Tooltip("멀티샷 발사 각도")]
        public float spreadAngle = 12f;

        [Tooltip("기본 관통 횟수")]
        public int basePierceCount = 0;

        [Tooltip("기본 넉백 수치")]
        public float knockbackPower = 4.5f;

        [Header("Projectile Specs")]
        [Tooltip("화살 비행 속도 (m/s)")]
        public float projectileSpeed = 24f;

        [Tooltip("화살 생존 시간 (초)")]
        public float projectileLifeTime = 2.5f;

        [Tooltip("화살 충돌 감지 반경 (m)")]
        public float projectileHitRadius = 0.4f;

        [Header("Charged Shot Settings")]
        [Tooltip("차징 시작 대기 시간 (초)")]
        public float chargeStartTime = 0.3f;

        [Tooltip("최대 차징 도달 시간 (초)")]
        public float maxChargeTime = 3.0f;

        [Tooltip("차징 최소 사거리 (m)")]
        public float chargeMinRange = 0.0f;

        [Tooltip("차징 최대 사거리 (m)")]
        public float chargeMaxRange = 25.0f;

        [Tooltip("차징 피해량 배율")]
        public float chargeDamageMultiplier = 2.0f;

        [Tooltip("차징 투사체 비행 속도 (m/s)")]
        public float chargeProjectileSpeed = 36.0f;

        [Tooltip("차징 넉백 파워")]
        public float chargeKnockback = 12.0f;

        [Tooltip("차징 궤적 UI 폭 (m)")]
        public float chargeTelegraphWidth = 0.3f;

        [Header("Burst Shot Settings")]
        [Tooltip("연사 화살 발사 수 (기본 5발)")]
        public int burstShotCount = 5;

        [Tooltip("연사 총 소요 시간 (초, 기본 1.5초)")]
        public float burstTotalDuration = 1.5f;

        [Tooltip("연사 재사용 대기 시간 (초)")]
        public float burstCooldown = 2.0f;

        [Tooltip("동일 볼리 동시 피격 점감 비율 (1.0 -> 0.5 -> 0.25 ...)")]
        public float multiHitDamageFalloffRatio = 0.5f;
    }

    [System.Serializable]
    public class DaggerBalanceStats
    {
        [Tooltip("단검 기본 피해량")]
        public float baseDamage = 50f;

        [Tooltip("부채꼴 공격 사거리 (m)")]
        public float attackRadius = 3.0f;

        [Tooltip("부채꼴 공격 각도 (도)")]
        public float attackAngle = 150f;

        [Tooltip("단검 재사용 대기 시간 (초)")]
        public float cooldown = 0.5f;

        [Tooltip("단검 넉백 파워")]
        public float knockback = 9.0f;

        [Header("Reflection / Parrying Settings")]
        [Tooltip("투사체/마법 반사(패링) 가능 여부")]
        public bool canReflectProjectiles = true;

        [Tooltip("반사된 투사체 피해량 배율")]
        public float reflectDamageMultiplier = 1.5f;

        [Tooltip("반사된 투사체 비행 속도 배율 (기본 1.8배속)")]
        public float reflectSpeedMultiplier = 1.8f;
    }

    [CreateAssetMenu(fileName = "WeaponStatsTable", menuName = "MythOfGuotaji/Tables/WeaponStatsTable")]
    public class WeaponStatsTableSO : ScriptableObject
    {
        [Header("Bow Configuration")]
        public BowBalanceStats bow = new BowBalanceStats();

        [Header("Dagger Configuration")]
        public DaggerBalanceStats dagger = new DaggerBalanceStats();
    }
}
