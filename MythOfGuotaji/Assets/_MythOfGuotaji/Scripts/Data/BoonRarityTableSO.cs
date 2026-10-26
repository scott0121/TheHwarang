using System;
using System.Collections.Generic;
using MythOfGuotaji.Progression;
using UnityEngine;

namespace MythOfGuotaji.Data
{
    /// <summary>
    /// 축복 희귀도 배수 적용 방식
    /// </summary>
    public enum BoonApplyMode
    {
        MultiplyValue = 0,    // 수치에 희귀도 배수 곱연산 적용 (연속 수치: HP, 이속, 공격력 등)
        FixedValue = 1        // 수치 고정 적용 (이산 정수: 화살 발사 수, 관통 수 등)
    }

    /// <summary>
    /// 개별 희귀도(BoonRarity) 설정 데이터
    /// </summary>
    [Serializable]
    public class BoonRarityConfig
    {
        public BoonRarity rarity;
        public string rarityName;         // "일반", "희귀", "전설"
        public float statMultiplier;      // 스탯 배수 (Common: 1.0x, Rare: 1.5x, Legendary: 2.2x)
        public float rollWeight;          // 뽑기 출현 가중치 (Common: 70, Rare: 24, Legendary: 6)
        public Color rarityColor;         // UI 연출 색상
        public BoonApplyMode applyMode;   // 적용 방식
        public string descriptionTag;     // 부가 설명 태그 (e.g. "[기본]", "[희귀 1.5배]", "[전설 2.2배]")
    }

    /// <summary>
    /// 인게임 축복 희귀도(BoonRarity)별 배수와 출현 가중치, 적용 방식을 총괄하는 독립 테이블
    /// </summary>
    [CreateAssetMenu(fileName = "BoonRarityTable", menuName = "MythOfGuotaji/Data/BoonRarityTable")]
    public class BoonRarityTableSO : ScriptableObject
    {
        [Header("Rarity Multiplier & Weight Configurations")]
        public List<BoonRarityConfig> rarityConfigs = new();

        public BoonRarityConfig GetConfig(BoonRarity rarity)
        {
            if (rarityConfigs != null)
            {
                foreach (var cfg in rarityConfigs)
                {
                    if (cfg.rarity == rarity) return cfg;
                }
            }

            // 안전 기본값 반환 (Common 40%, Rare 30%, Epic 20%, Legendary 10%)
            return rarity switch
            {
                BoonRarity.Legendary => new BoonRarityConfig
                {
                    rarity = BoonRarity.Legendary,
                    rarityName = "전설",
                    statMultiplier = 2.2f,
                    rollWeight = 10f,
                    rarityColor = new Color(1f, 0.84f, 0.0f, 1f), // 노란색 / 황금
                    applyMode = BoonApplyMode.MultiplyValue,
                    descriptionTag = "[전설 2.2x]"
                },
                BoonRarity.Epic => new BoonRarityConfig
                {
                    rarity = BoonRarity.Epic,
                    rarityName = "영웅",
                    statMultiplier = 1.7f,
                    rollWeight = 20f,
                    rarityColor = new Color(0.72f, 0.28f, 1.0f, 1f), // 보라색
                    applyMode = BoonApplyMode.MultiplyValue,
                    descriptionTag = "[영웅 1.7x]"
                },
                BoonRarity.Rare => new BoonRarityConfig
                {
                    rarity = BoonRarity.Rare,
                    rarityName = "희귀",
                    statMultiplier = 1.3f,
                    rollWeight = 30f,
                    rarityColor = new Color(0.2f, 0.65f, 1.0f, 1f), // 파란색
                    applyMode = BoonApplyMode.MultiplyValue,
                    descriptionTag = "[희귀 1.3x]"
                },
                _ => new BoonRarityConfig
                {
                    rarity = BoonRarity.Common,
                    rarityName = "일반",
                    statMultiplier = 1.0f,
                    rollWeight = 40f,
                    rarityColor = new Color(0.95f, 0.95f, 0.95f, 1f), // 흰색
                    applyMode = BoonApplyMode.MultiplyValue,
                    descriptionTag = "[일반]"
                }
            };
        }

        /// <summary>
        /// 설정된 가중치를 기준으로 무작위 희귀도를 추첨
        /// </summary>
        public BoonRarity RollRandomRarity()
        {
            if (rarityConfigs == null || rarityConfigs.Count == 0)
            {
                return BoonRarity.Common;
            }

            float totalWeight = 0f;
            foreach (var cfg in rarityConfigs)
            {
                totalWeight += cfg.rollWeight;
            }

            float roll = UnityEngine.Random.Range(0f, totalWeight);
            float currentSum = 0f;

            foreach (var cfg in rarityConfigs)
            {
                currentSum += cfg.rollWeight;
                if (roll <= currentSum)
                {
                    return cfg.rarity;
                }
            }

            return BoonRarity.Common;
        }

        /// <summary>
        /// 희귀도 배수 및 적용 방식을 반영하여 1단계 상승 최종 스탯 수치를 산출 (남은 Max 한도 클램핑 지원)
        /// </summary>
        public float CalculateFinalValue(BoonRarity rarity, float baseStepValue, bool isIntegerStat, float remainingCap)
        {
            var cfg = GetConfig(rarity);
            float multiplied = baseStepValue;

            if (isIntegerStat || cfg.applyMode == BoonApplyMode.FixedValue)
            {
                // 정수형 스탯(화살 수, 관통 수)은 희귀도 배수 대신 고정 정수 단위 적용
                multiplied = baseStepValue;
            }
            else
            {
                // 연속형 수치는 희귀도 배수를 곱연산
                multiplied = baseStepValue * cfg.statMultiplier;
            }

            // 남은 상한 한도 초과 방지
            if (remainingCap > 0f)
            {
                multiplied = Mathf.Min(multiplied, remainingCap);
            }

            return multiplied;
        }
    }
}
