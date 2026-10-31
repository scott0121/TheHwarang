using System;
using System.Collections.Generic;
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

        [Tooltip("화살 비행거리 Normal (m)")]
        public float normalRange = 15f;

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
    public class BowEntry
    {
        [Tooltip("무기 고유 ID (예: Bow_00, Bow_01)")]
        public string weaponId = "Bow_00";

        [Tooltip("무기 이름 (한국어 표기: 활_00 기본 곡도 활 등)")]
        public string weaponName = "기본 곡도 활";

        [TextArea(2, 4)]
        [Tooltip("무기 설명 및 특징")]
        public string description = "서해 용궁의 신성한 활시위. 안정적인 연사력과 차징 성능을 자랑하는 균형형 활입니다.";

        [Header("Weapon Dedicated Prefabs")]
        [Tooltip("일반 화살 투사체 게임오브젝트")]
        public GameObject modelPrefab;
        public GameObject normalArrowPrefab;

        [Tooltip("차징 화살 투사체 게임오브젝트")]
        public GameObject chargedArrowPrefab;

        [Header("Dedicated Audio")]
        [Tooltip("일반 발사 사운드 프리팹")]
        public GameObject fireSoundPrefab;

        [Tooltip("차징 발사 사운드 프리팹")]
        public GameObject chargedFireSoundPrefab;

        [Header("Weapon Stats")]
        public BowBalanceStats stats = new BowBalanceStats();
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

        [Header("Critical Settings")]
        [Tooltip("단검 치명타 확률 (0~1)")]
        public float critChance = 0.15f;

        [Tooltip("단검 치명타 피해량 배수")]
        public float critMultiplier = 2.0f;
    }

    [System.Serializable]
    public class DaggerEntry
    {
        [Tooltip("무기 고유 ID (예: Dagger_00, Dagger_01)")]
        public string weaponId = "Dagger_00";

        [Tooltip("무기 이름 (한국어 표기: 단검_00 기본 옥단검 등)")]
        public string weaponName = "기본 옥단검";

        [TextArea(2, 4)]
        [Tooltip("무기 설명 및 특징")]
        public string description = "곡도 사당의 옥빛 비수. 근접 적을 호형으로 베어내며 투사체를 패링 반사합니다.";

        [Header("Weapon Dedicated Prefabs")]
        [Tooltip("단검 베기 검기 VFX 프리팹")]
        public GameObject modelPrefab;
        public GameObject slashVfxPrefab;

        [Tooltip("단검 패링 성공 마법진/스파크 VFX 프리팹")]
        public GameObject parryVfxPrefab;

        [Header("Dedicated Audio")]
        [Tooltip("단검 휘두르기 사운드 프리팹")]
        public GameObject swingSoundPrefab;

        [Tooltip("패링 성공 튕겨내기 사운드 프리팹")]
        public GameObject parrySuccessSoundPrefab;

        [Header("Weapon Stats")]
        public DaggerBalanceStats stats = new DaggerBalanceStats();
    }

    [CreateAssetMenu(fileName = "WeaponStatsTable", menuName = "MythOfGuotaji/Tables/WeaponStatsTable")]
    public class WeaponStatsTableSO : ScriptableObject
    {
        [Header("Bows Catalog (활 종류 목록)")]
        public List<BowEntry> bows = new List<BowEntry>();

        [Header("Daggers Catalog (단검 종류 목록)")]
        public List<DaggerEntry> daggers = new List<DaggerEntry>();

        // 하위 호환성 Getter
        public BowBalanceStats bow => GetBow("Bow_00")?.stats ?? new BowBalanceStats();
        public DaggerBalanceStats dagger => GetDagger("Dagger_00")?.stats ?? new DaggerBalanceStats();

        /// <summary>
        /// 무기 ID로 활 조회 (예: "Bow_00", "활_00", "Bow_01", "활_01")
        /// </summary>
        public BowEntry GetBow(string id)
        {
            if (bows == null || bows.Count == 0) return null;
            if (string.IsNullOrEmpty(id)) return bows[0];

            string normalized = NormalizeWeaponId(id);
            var match = bows.Find(b => NormalizeWeaponId(b.weaponId) == normalized);
            return match ?? bows[0];
        }

        /// <summary>
        /// 무기 ID로 단검 조회 (예: "Dagger_00", "단검_00", "Dagger_01", "단검_01")
        /// </summary>
        public DaggerEntry GetDagger(string id)
        {
            if (daggers == null || daggers.Count == 0) return null;
            if (string.IsNullOrEmpty(id)) return daggers[0];

            string normalized = NormalizeWeaponId(id);
            var match = daggers.Find(d => NormalizeWeaponId(d.weaponId) == normalized);
            return match ?? daggers[0];
        }

        private static string NormalizeWeaponId(string id)
        {
            if (string.IsNullOrEmpty(id)) return "";
            string s = id.Trim().ToLowerInvariant();
            s = s.Replace("활_", "bow_");
            s = s.Replace("활", "bow_");
            s = s.Replace("단검_", "dagger_");
            s = s.Replace("단검", "dagger_");
            return s;
        }
    }
}
