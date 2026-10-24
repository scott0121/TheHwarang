using System;
using UnityEngine;

namespace MythOfGuotaji.Progression
{
    public enum BoonRarity
    {
        Common,
        Rare,
        Legendary
    }

    /// <summary>
    /// 스테이지(로그라이크 런) 내에서만 유지되는 일시 축복 정의
    /// </summary>
    [CreateAssetMenu(fileName = "Boon_New", menuName = "MythOfGuotaji/Progression/InRunBoon")]
    public class InRunBoonSO : ScriptableObject
    {
        public string boonId;
        public string boonTitle;
        [TextArea(2, 3)] public string description;
        public BoonRarity rarity = BoonRarity.Common;

        [Header("Table Level & Cap Progression")]
        public int currentRank = 1;
        public int maxRank = 5;
        public float currentAccumulatedValue = 0f;
        public float maxAccumulatedValue = 0f;
        public Data.BoonStatType statType;

        [Header("Survival & Mobility")]
        [Tooltip("최대 체력 증가 (기존 대비 50% 감쇄: +15)")]
        public float bonusHp = 0f;

        [Tooltip("이동 속도 증가 (기존 대비 50% 감쇄: +0.75m/s)")]
        public float bonusMoveSpeed = 0f;

        [Tooltip("대시 쿨다운 감소 (초)")]
        public float dashCooldownReduction = 0f;

        [Header("Bow & Volley Boosts")]
        [Tooltip("기본 화살 공격력 증가")]
        public float bonusAttackDamage = 0f;

        [Tooltip("연사 속도 증가")]
        public float bonusAttackRate = 0f;

        [Tooltip("화살 다발 추가 발사 수")]
        public int extraArrows = 0;

        [Tooltip("화살 추가 관통 수")]
        public int extraPierce = 0;

        [Header("Charged Arrow Boosts")]
        [Tooltip("차징 소요 시간 단축 (초)")]
        public float chargeSpeedBonus = 0f;

        [Tooltip("차징 관통 사거리 추가 (m)")]
        public float bonusChargeRange = 0f;

        [Tooltip("차징 화살 피해 배율 증가")]
        public float bonusChargeDamageMultiplier = 0f;

        [Header("Dagger & Parrying Boosts")]
        [Tooltip("단검 근접 공격력 증가")]
        public float bonusDaggerDamage = 0f;

        [Tooltip("단검 공격 반경 증가 (m)")]
        public float bonusDaggerRadius = 0f;

        [Tooltip("투사체 반사 피해 배율 증가")]
        public float bonusDaggerReflectDamage = 0f;

        [Header("Global Cooldown (GCD) & Recovery")]
        [Tooltip("글로벌 쿨타임 감소 (초)")]
        public float gcdReduction = 0f;

        [Tooltip("생명 하트 드롭 확률 증가 (0.0 ~ 1.0)")]
        public float heartDropRateBonus = 0f;

        [Tooltip("하트 획득 시 추가 회복량")]
        public float heartHealBonus = 0f;
    }
}
