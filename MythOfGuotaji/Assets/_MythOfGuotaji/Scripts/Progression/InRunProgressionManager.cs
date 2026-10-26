using System;
using System.Collections.Generic;
using MythOfGuotaji.Actor;
using MythOfGuotaji.Core;
using MythOfGuotaji.Data;
using UnityEngine;

namespace MythOfGuotaji.Progression
{
    /// <summary>
    /// 스테이지 런(Run) 내부 한정 레벨업 및 축복(Boon) 관리자
    /// - InRunBoonTableSO 및 BoonRarityTableSO 기반 스탯 테이블화
    /// - 0단계 수치 - Max 수치 - 최대 업그레이드 회수(Max Rank) 기준 단계별 강화
    /// - 희귀도 배수(Common: 1.0x, Rare: 1.5x, Legendary: 2.2x) 및 적용 방식 반영
    /// - 최대 회수 도달 시 선택지 자동 제외
    /// - 스테이지 종료/사망 시 100% 초기화
    /// </summary>
    public class InRunProgressionManager : MonoBehaviour
    {
        public static InRunProgressionManager Instance { get; private set; }

        [Header("Boon Database")]
        [SerializeField] private List<InRunBoonSO> _availableBoons = new();

        [Header("Current Run State")]
        public int currentRunLevel = 1;
        public int currentRunExp = 0;
        public int expToNextLevel = 5;

        private readonly List<InRunBoonSO> _activeBoonsInCurrentRun = new();
        private readonly Dictionary<string, int> _boonUpgradeRanks = new();
        private readonly Dictionary<string, float> _boonAccumulatedValues = new();

        public event Action<int, int> OnExpChanged;
        public event Action<int> OnLevelUp;
        public event Action<List<InRunBoonSO>> OnBoonSelectionRequested;
        public event Action<InRunBoonSO> OnBoonSelected;

        public int GetBoonRank(string boonId) => _boonUpgradeRanks.TryGetValue(boonId, out int r) ? r : 0;
        public float GetBoonAccumulatedValue(string boonId) => _boonAccumulatedValues.TryGetValue(boonId, out float v) ? v : 0f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                // 몬스터 사망 시 직접 경험치를 주지 않고, 헬퍼가 획득한 보석을 통해 AddExp() 호출
                GameManager.Instance.OnStateChanged += HandleGameState;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged -= HandleGameState;
            }
        }

        private void HandleGameState(GameState state)
        {
            // 스테이지 재시작 또는 실패 시 초기화
            if (state == GameState.InStage)
            {
                ResetInRunProgress();
            }
        }

        private void HandleKillExp(int totalKills)
        {
            AddExp(1);
        }

        public void AddExp(int amount)
        {
            currentRunExp += amount;
            OnExpChanged?.Invoke(currentRunExp, expToNextLevel);

            if (currentRunExp >= expToNextLevel)
            {
                LevelUp();
            }
        }

        private void LevelUp()
        {
            currentRunExp -= expToNextLevel;
            currentRunLevel++;
            expToNextLevel = Mathf.RoundToInt(expToNextLevel * 1.5f);

            Debug.Log($"<color=yellow>[InRunProgression] 스테이지 레벨업! (Lv.{currentRunLevel})</color>");
            OnLevelUp?.Invoke(currentRunLevel);

            TriggerBoonSelection();
        }

        private void TriggerBoonSelection()
        {
            List<InRunBoonSO> choices = GetRandomBoonChoices(3);
            OnBoonSelectionRequested?.Invoke(choices);
        }

        private List<InRunBoonSO> GetRandomBoonChoices(int count)
        {
            var boonTable = GameDataManager.Instance != null && GameDataManager.Instance.InRunBoonTable != null
                ? GameDataManager.Instance.InRunBoonTable
                : Resources.Load<InRunBoonTableSO>("Data/InRunBoonTable");

            var rarityTable = GameDataManager.Instance != null && GameDataManager.Instance.BoonRarityTable != null
                ? GameDataManager.Instance.BoonRarityTable
                : Resources.Load<BoonRarityTableSO>("Data/BoonRarityTable");

            List<InRunBoonSO> result = new();

            if (boonTable == null || boonTable.entries == null || boonTable.entries.Count == 0)
            {
                Debug.LogWarning("[InRunProgression] InRunBoonTable을 찾을 수 없습니다.");
                return result;
            }

            // 1. 최대 업그레이드 회수(Max Rank)에 도달하지 않은 후보군 필터링
            List<InRunBoonEntry> candidateEntries = new();
            foreach (var entry in boonTable.entries)
            {
                int currentRank = _boonUpgradeRanks.TryGetValue(entry.boonId, out int cr) ? cr : 0;
                if (currentRank < entry.maxUpgradeCount)
                {
                    candidateEntries.Add(entry);
                }
            }

            if (candidateEntries.Count == 0)
            {
                Debug.Log("<color=yellow>[InRunProgression] 모든 축복의 최대 업그레이드 단계에 도달했습니다!</color>");
                return result;
            }

            // 2. 중복 없이 count개 무작위 추첨
            List<InRunBoonEntry> selectedEntries = new();
            for (int i = 0; i < count && candidateEntries.Count > 0; i++)
            {
                int idx = UnityEngine.Random.Range(0, candidateEntries.Count);
                selectedEntries.Add(candidateEntries[idx]);
                candidateEntries.RemoveAt(idx);
            }

            // 3. 각 선택지에 대해 희귀도 및 스탯 산출 (테이블 기준 배수 및 상한선 적용)
            foreach (var entry in selectedEntries)
            {
                BoonRarity rolledRarity = rarityTable != null ? rarityTable.RollRandomRarity() : entry.defaultRarity;
                if (rolledRarity < entry.defaultRarity)
                {
                    rolledRarity = entry.defaultRarity;
                }

                var rarityConfig = rarityTable != null ? rarityTable.GetConfig(rolledRarity) : null;

                int currentRank = _boonUpgradeRanks.TryGetValue(entry.boonId, out int cr) ? cr : 0;
                int nextRank = currentRank + 1;
                float currentAcc = _boonAccumulatedValues.TryGetValue(entry.boonId, out float ca) ? ca : 0f;
                float remainingCap = Mathf.Max(0f, entry.maxValue - currentAcc);

                // 테이블 기준 1회 상승 수치 산출 (희귀도 배수 및 상한선 적용)
                float stepVal = rarityTable != null
                    ? rarityTable.CalculateFinalValue(rolledRarity, entry.baseStepValue, entry.isIntegerStat, remainingCap)
                    : Mathf.Min(entry.baseStepValue, remainingCap);

                float newAcc = currentAcc + stepVal;

                var b = ScriptableObject.CreateInstance<InRunBoonSO>();
                b.boonId = entry.boonId;
                b.boonTitle = $"{entry.title} (Lv.{nextRank}/{entry.maxUpgradeCount})";
                b.category = entry.category;
                b.rarity = rolledRarity;
                b.rarityColor = rarityConfig != null ? rarityConfig.rarityColor : Color.white;
                b.currentRank = nextRank;
                b.maxRank = entry.maxUpgradeCount;
                b.currentAccumulatedValue = newAcc;
                b.maxAccumulatedValue = entry.maxValue;
                b.statType = entry.statType;

                // 스탯 수치 대입
                switch (entry.statType)
                {
                    case BoonStatType.MaxHp:
                        b.bonusHp = stepVal;
                        break;
                    case BoonStatType.MoveSpeed:
                        b.bonusMoveSpeed = stepVal;
                        break;
                    case BoonStatType.DashCooldown:
                        b.dashCooldownReduction = stepVal;
                        break;
                    case BoonStatType.BowDamage:
                        b.bonusAttackDamage = stepVal;
                        break;
                    case BoonStatType.BowRate:
                        b.bonusAttackRate = stepVal;
                        break;
                    case BoonStatType.ArrowPierce:
                        b.extraPierce = Mathf.RoundToInt(stepVal);
                        break;
                    case BoonStatType.ArrowMulti:
                        b.extraArrows = Mathf.RoundToInt(stepVal);
                        break;
                    case BoonStatType.ChargeRange:
                        b.bonusChargeRange = stepVal;
                        break;
                    case BoonStatType.ChargeDamageMult:
                        b.bonusChargeDamageMultiplier = stepVal;
                        break;
                    case BoonStatType.ChargeSpeed:
                        b.chargeSpeedBonus = stepVal;
                        break;
                    case BoonStatType.DaggerDamage:
                        b.bonusDaggerDamage = stepVal;
                        break;
                    case BoonStatType.DaggerRadius:
                        b.bonusDaggerRadius = stepVal;
                        break;
                    case BoonStatType.DaggerReflectMult:
                        b.bonusDaggerReflectDamage = stepVal;
                        break;
                    case BoonStatType.GcdReduction:
                        b.gcdReduction = stepVal;
                        break;
                    case BoonStatType.HeartDropRate:
                        b.heartDropRateBonus = stepVal;
                        break;
                    case BoonStatType.HeartHealBonus:
                        b.heartHealBonus = stepVal;
                        break;
                }

                // UI 설명 문구 포맷팅 (테이블의 descriptionFormat과 실시간 산출 수치 조합)
                string valStr = FormatStatValue(entry, stepVal);
                string accStr = FormatStatValue(entry, newAcc);
                string maxStr = FormatStatValue(entry, entry.maxValue);
                string rarityTag = rarityConfig != null && rolledRarity != BoonRarity.Common ? $" {rarityConfig.descriptionTag}" : "";

                if (!string.IsNullOrEmpty(entry.descriptionFormat))
                {
                    try
                    {
                        b.description = string.Format(entry.descriptionFormat, valStr, accStr, maxStr) + rarityTag;
                    }
                    catch
                    {
                        b.description = $"{entry.title} +{valStr} (누적: {accStr} / Max: {maxStr}){rarityTag}";
                    }
                }
                else
                {
                    b.description = $"{entry.title} +{valStr} (누적: {accStr} / Max: {maxStr}){rarityTag}";
                }

                result.Add(b);
            }

            return result;
        }

        private static string FormatStatValue(InRunBoonEntry entry, float val)
        {
            if (entry.isIntegerStat)
            {
                return $"{Mathf.RoundToInt(val)}{entry.unit}";
            }
            if (entry.statType == BoonStatType.BowRate || entry.statType == BoonStatType.HeartDropRate || entry.statType == BoonStatType.ChargeSpeed)
            {
                return $"{Mathf.RoundToInt(val * 100)}%";
            }
            if (entry.statType == BoonStatType.DashCooldown || entry.statType == BoonStatType.GcdReduction)
            {
                return $"{val:0.00}{entry.unit}";
            }
            return $"{val:0.##}{entry.unit}";
        }

        public void SelectBoon(InRunBoonSO boon)
        {
            if (boon == null) return;
            _activeBoonsInCurrentRun.Add(boon);

            _boonUpgradeRanks[boon.boonId] = boon.currentRank;
            _boonAccumulatedValues[boon.boonId] = boon.currentAccumulatedValue;

            var player = FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                player.ApplyBoon(boon);
            }

            string maxNotice = boon.currentRank >= boon.maxRank ? " <color=gold>[★최대 강화 MAX 완료]</color>" : "";
            Debug.Log($"<color=lime>[InRunProgression] 축복 선택 완료: {boon.boonTitle} ({boon.description}){maxNotice}</color>");
            OnBoonSelected?.Invoke(boon);
        }

        public void ResetInRunProgress()
        {
            currentRunLevel = 1;
            currentRunExp = 0;
            expToNextLevel = 5;
            _activeBoonsInCurrentRun.Clear();
            _boonUpgradeRanks.Clear();
            _boonAccumulatedValues.Clear();

            // 하트 드롭 및 회복 보너스 리셋
            RecoveryHeart.BonusHealAmount = 0f;
            RecoveryHeart.DropRateMultiplier = 1f;

            // 플레이어의 일시 런 스탯 리셋 (테이블 기본값 복구 및 영구 캐릭터 트리 재적용)
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.ResetRunStats();
            }
        }
    }
}
