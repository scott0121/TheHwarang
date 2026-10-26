using System;
using System.Collections.Generic;
using MythOfGuotaji.Actor;
using MythOfGuotaji.Core;
using UnityEngine;

namespace MythOfGuotaji.Progression
{
    /// <summary>
    /// 직업별 캐릭터 트리 관리자
    /// - 스킬 포인트 투자로 스킬 레벨업
    /// - [핵심 요구사항] 언제든지 리셋 가능 (투자된 스킬 포인트 100% 환급)
    /// </summary>
    public class CharacterTreeManager : MonoBehaviour
    {
        public static CharacterTreeManager Instance { get; private set; }

        [SerializeField] private CharacterSkillTreeSO _currentTree;

        public event Action OnSkillsUpdated;

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
            if (_currentTree == null)
            {
                _currentTree = Resources.Load<CharacterSkillTreeSO>("Data/CharacterSkillTree_Archer");
            }
            ApplySkillsToPlayer();
        }

        public int GetSkillLevel(string skillId)
        {
            var save = GameManager.Instance.SaveData;
            if (save == null) return 0;

            var entry = save.skillInvestments.Find(e => e.skillNodeId == skillId);
            return entry != null ? entry.investedLevel : 0;
        }

        public bool CanInvest(CharacterSkillNodeSO skill, out string reason)
        {
            reason = string.Empty;
            if (skill == null)
            {
                reason = "유효하지 않은 스킬입니다.";
                return false;
            }

            var save = GameManager.Instance.SaveData;
            int currentLvl = GetSkillLevel(skill.skillId);

            if (currentLvl >= skill.maxLevel)
            {
                reason = "이미 최대 레벨에 도달했습니다.";
                return false;
            }

            if (save.characterSkillPoints < skill.costPerLevel)
            {
                reason = $"스킬 포인트가 부족합니다. (필요: {skill.costPerLevel}, 보유: {save.characterSkillPoints})";
                return false;
            }

            // 선행 스킬 검증
            foreach (var preId in skill.prerequisiteSkillIds)
            {
                if (GetSkillLevel(preId) <= 0)
                {
                    reason = $"선행 스킬 [{preId}]를 먼저 1레벨 이상 습득해야 합니다.";
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 스킬 레벨업 (포인트 소모)
        /// </summary>
        public bool TryInvestSkill(CharacterSkillNodeSO skill)
        {
            if (!CanInvest(skill, out string reason))
            {
                Debug.LogWarning($"[CharacterTree] 투자 실패: {reason}");
                return false;
            }

            var save = GameManager.Instance.SaveData;
            save.characterSkillPoints -= skill.costPerLevel;

            var entry = save.skillInvestments.Find(e => e.skillNodeId == skill.skillId);
            if (entry != null)
            {
                entry.investedLevel++;
            }
            else
            {
                save.skillInvestments.Add(new SkillInvestmentEntry(skill.skillId, 1));
            }

            GameManager.Instance.SaveGame();
            ApplySkillsToPlayer();
            OnSkillsUpdated?.Invoke();
            Debug.Log($"<color=green>[CharacterTree] '{skill.skillName}' 레벨업! (현재 Lv.{GetSkillLevel(skill.skillId)})</color>");
            return true;
        }

        /// <summary>
        /// 캐릭터 트리 전체 리셋 (투자된 모든 스킬 포인트 환급)
        /// </summary>
        public void ResetAllSkills()
        {
            var save = GameManager.Instance.SaveData;
            if (save == null) return;

            int refundedPoints = 0;
            foreach (var entry in save.skillInvestments)
            {
                refundedPoints += entry.investedLevel;
            }

            save.characterSkillPoints += refundedPoints;
            save.skillInvestments.Clear();

            GameManager.Instance.SaveGame();
            ApplySkillsToPlayer();
            OnSkillsUpdated?.Invoke();
            Debug.Log($"<color=yellow>[CharacterTree] 스킬 트리가 성공적으로 리셋되었습니다! (환급된 포인트: +{refundedPoints})</color>");
        }

        public void ApplySkillsToPlayer()
        {
            var player = FindAnyObjectByType<PlayerController>();
            if (player == null || _currentTree == null) return;

            int extraArrows = 0;
            int extraPierce = 0;
            float extraSpeed = 0f;

            foreach (var entry in GameManager.Instance.SaveData.skillInvestments)
            {
                var skill = _currentTree.FindSkill(entry.skillNodeId);
                if (skill == null) continue;

                foreach (var mod in skill.modifiers)
                {
                    float totalVal = mod.valuePerLevel * entry.investedLevel;
                    switch (mod.type)
                    {
                        case SkillModifierType.ExtraArrows:
                            extraArrows += (int)totalVal;
                            break;
                        case SkillModifierType.PierceCount:
                            extraPierce += (int)totalVal;
                            break;
                        case SkillModifierType.MoveSpeedPercent:
                            extraSpeed += totalVal * 0.01f;
                            break;
                    }
                }
            }

            player.ApplyStatBonus(0f, extraSpeed, extraArrows, extraPierce);
        }
    }
}
