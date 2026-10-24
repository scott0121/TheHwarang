using System.IO;
using MythOfGuotaji.Dungeon;
using MythOfGuotaji.Progression;
using UnityEditor;
using UnityEngine;

namespace MythOfGuotaji.Editor
{
    public static class MythOfGuotajiSampleDataGenerator
    {
        [MenuItem("MythOfGuotaji/Generate Sample ScriptableObjects")]
        public static void GenerateSampleAssets()
        {
            string dataPath = "Assets/_MythOfGuotaji/Data";
            if (!AssetDatabase.IsValidFolder(dataPath))
            {
                Directory.CreateDirectory(dataPath);
                AssetDatabase.Refresh();
            }

            // 1. 시나리오 노드 샘플 생성
            var rootNode = ScriptableObject.CreateInstance<ScenarioNodeSO>();
            rootNode.nodeId = "ROOT_START";
            rootNode.title = "거타지의 출항";
            rootNode.description = "진성여왕의 명을 받아 당나라로 떠나는 사신선에 호위 무사로 탑승한다.";
            rootNode.requiredAffinityPoints = 0;
            rootNode.unlockType = ScenarioUnlockType.StoryEvent;
            AssetDatabase.CreateAsset(rootNode, $"{dataPath}/ScenarioNode_01_Start.asset");

            var companionNode = ScriptableObject.CreateInstance<ScenarioNodeSO>();
            companionNode.nodeId = "SCENE_RECRUIT_HOWON";
            companionNode.title = "인연: 서해 노인의 부름과 호원";
            companionNode.description = "작도(까치섬)에서 마주한 노인의 간청을 듣고, 지원 동료 '호원'과 인연을 맺는다.";
            companionNode.requiredAffinityPoints = 3;
            companionNode.prerequisiteNodeIds.Add("ROOT_START");
            companionNode.unlockType = ScenarioUnlockType.RecruitCompanion;
            companionNode.targetIdentifier = "Companion_Howon";
            AssetDatabase.CreateAsset(companionNode, $"{dataPath}/ScenarioNode_02_Howon.asset");

            var dungeonNode = ScriptableObject.CreateInstance<ScenarioNodeSO>();
            dungeonNode.nodeId = "SCENE_UNLOCK_FOX_LAIR";
            dungeonNode.title = "비경: 여우 요괴의 본거지";
            dungeonNode.description = "붉은 안개 속 숨겨진 구미호의 굴을 향하는 미로를 개방한다.";
            dungeonNode.requiredAffinityPoints = 5;
            dungeonNode.prerequisiteNodeIds.Add("SCENE_RECRUIT_HOWON");
            dungeonNode.unlockType = ScenarioUnlockType.UnlockDungeon;
            dungeonNode.targetIdentifier = "Chapter_02_FoxLair";
            AssetDatabase.CreateAsset(dungeonNode, $"{dataPath}/ScenarioNode_03_FoxLair.asset");

            // 2. 캐릭터 스킬 샘플 생성
            var skill1 = ScriptableObject.CreateInstance<CharacterSkillNodeSO>();
            skill1.skillId = "SKILL_MULTI_ARROW";
            skill1.skillName = "연노(連弩)의 기술";
            skill1.description = "한 번에 발사되는 화살의 개수가 증가합니다.";
            skill1.maxLevel = 3;
            skill1.costPerLevel = 1;
            skill1.modifiers.Add(new SkillModifier { type = SkillModifierType.ExtraArrows, valuePerLevel = 1f });
            AssetDatabase.CreateAsset(skill1, $"{dataPath}/Skill_MultiArrow.asset");

            var skill2 = ScriptableObject.CreateInstance<CharacterSkillNodeSO>();
            skill2.skillId = "SKILL_PIERCE";
            skill2.skillName = "신궁의 묵직한 살";
            skill2.description = "화살이 적을 꿰뚫고 관통합니다.";
            skill2.maxLevel = 3;
            skill2.costPerLevel = 1;
            skill2.modifiers.Add(new SkillModifier { type = SkillModifierType.PierceCount, valuePerLevel = 1f });
            AssetDatabase.CreateAsset(skill2, $"{dataPath}/Skill_Pierce.asset");

            // 3. 챕터 테마 샘플 생성
            var theme = ScriptableObject.CreateInstance<ChapterThemeSO>();
            theme.chapterIndex = 1;
            theme.chapterName = "제1장: 서해 작도의 요괴 미로";
            theme.ambientColor = new Color(0.2f, 0.28f, 0.38f);
            AssetDatabase.CreateAsset(theme, $"{dataPath}/ChapterTheme_01.asset");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=green>[SampleDataGenerator] 샘플 ScriptableObject 에셋 생성 완료!</color>");
        }
    }
}
