using System;
using System.Collections.Generic;
using UnityEngine;

namespace MythOfGuotaji.Progression
{
    public enum ScenarioUnlockType
    {
        UnlockDungeon,      // 신규 던전 / 챕터 해금
        RecruitCompanion,   // 신규 AI 동료 합류 (호원, 선녀 등)
        UnlockProfession,   // 직업 전문화 방향 해금 (신궁, 묵궁, 살수 등)
        StoryEvent          // 설화 스토리 이벤트 및 영구 패시브
    }

    /// <summary>
    /// 메인 시나리오 트리의 개별 노드 정의 (ScriptableObject)
    /// </summary>
    [CreateAssetMenu(fileName = "ScenarioNode_New", menuName = "MythOfGuotaji/Progression/ScenarioNode")]
    public class ScenarioNodeSO : ScriptableObject
    {
        [Header("Node Info")]
        public string nodeId;
        public string title;
        [TextArea(2, 4)] public string description;

        [Header("Costs & Prerequisites")]
        public int requiredAffinityPoints = 3; // 소모 인연 포인트
        public List<string> prerequisiteNodeIds = new(); // 선행 노드 ID 목록
        public List<string> exclusiveNodeIds = new();    // 상호 배타 노드 (선택 시 다른 쪽 잠김)

        [Header("Unlock Impact")]
        public ScenarioUnlockType unlockType = ScenarioUnlockType.UnlockDungeon;
        public string targetIdentifier; // 해금 대상 ID (예: "Chapter_02", "Companion_Howon", "Prof_GhostBow")
    }

    /// <summary>
    /// 전체 시나리오 트리 그래프 컨테이너
    /// </summary>
    [CreateAssetMenu(fileName = "ScenarioTree_Main", menuName = "MythOfGuotaji/Progression/ScenarioTree")]
    public class ScenarioTreeSO : ScriptableObject
    {
        public string treeName = "삼국유사 거타지 메인 설화 트리";
        public List<ScenarioNodeSO> allNodes = new();

        public ScenarioNodeSO FindNode(string id)
        {
            return allNodes.Find(n => n != null && n.nodeId == id);
        }
    }
}
