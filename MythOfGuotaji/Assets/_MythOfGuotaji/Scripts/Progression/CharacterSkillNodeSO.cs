using System;
using System.Collections.Generic;
using UnityEngine;

namespace MythOfGuotaji.Progression
{
    public enum SkillModifierType
    {
        AttackPowerPercent, // 기본 공격력 증가 %
        ExtraArrows,        // 화살 개수 추가 (+1, +2 등)
        PierceCount,        // 관통 횟수 증가
        DaggerDamage,       // 단검 피해량 증가
        DaggerCooldownReduction, // 단검 쿨다운 감소
        MoveSpeedPercent    // 이동 속도 증가 %
    }

    [Serializable]
    public struct SkillModifier
    {
        public SkillModifierType type;
        public float valuePerLevel;
    }

    [CreateAssetMenu(fileName = "SkillNode_New", menuName = "MythOfGuotaji/Progression/CharacterSkillNode")]
    public class CharacterSkillNodeSO : ScriptableObject
    {
        [Header("Skill Info")]
        public string skillId;
        public string skillName;
        [TextArea(2, 3)] public string description;
        public int maxLevel = 5;
        public int costPerLevel = 1;

        [Header("Prerequisites")]
        public List<string> prerequisiteSkillIds = new();

        [Header("Modifiers")]
        public List<SkillModifier> modifiers = new();
    }

    [CreateAssetMenu(fileName = "SkillTree_Archer", menuName = "MythOfGuotaji/Progression/CharacterSkillTree")]
    public class CharacterSkillTreeSO : ScriptableObject
    {
        public string professionName = "신라 궁사 (Archer)";
        public List<CharacterSkillNodeSO> nodes = new();

        public CharacterSkillNodeSO FindSkill(string id)
        {
            return nodes.Find(s => s != null && s.skillId == id);
        }
    }
}
