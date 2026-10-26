using System;
using System.Collections.Generic;
using UnityEngine;

namespace MythOfGuotaji.Data
{
    [Serializable]
    public class StageDungeonConfig
    {
        [Header("Stage Info")]
        public int stageNumber = 1;
        public string stageName = "1스테이지 (산해경 초입)";

        [Header("Block 1 (스타트룸 -> 중간보스룸)")]
        [Tooltip("스타트룸과 중간보스룸 사이의 최소 방 개수")]
        public int minRoomsBlock1 = 1;
        [Tooltip("스타트룸과 중간보스룸 사이의 최대 메인 방 개수")]
        public int maxRoomsBlock1 = 3;
        [Tooltip("블록 1 샛길(미궁/막다른) 방 개수")]
        public int branchRoomsBlock1 = 2;

        [Header("Block 2 (중간보스룸 -> 최종보스룸)")]
        [Tooltip("중간보스룸과 최종보스룸 사이의 최소 방 개수")]
        public int minRoomsBlock2 = 3;
        [Tooltip("중간보스룸과 최종보스룸 사이의 최대 메인 방 개수")]
        public int maxRoomsBlock2 = 5;
        [Tooltip("블록 2 샛길(미궁/막다른) 방 개수")]
        public int branchRoomsBlock2 = 2;

        [Header("Room Layout")]
        public float roomSize = 30.0f;
        public float extraLoopRatio = 0.1f;
        [Tooltip("적용할 맵 스킨 테이블 카테고리 인덱스 (0: Base, 1: ...)")]
        public int skinCategoryIndex = 0;
    }

    /// <summary>
    /// 스테이지별 던전 생성 파라미터 테이블 ScriptableObject:
    /// - 1블럭(스타트 -> 중간보스) 최소 칸수 보장
    /// - 2블럭(중간보스 -> 최종보스) 최소 칸수 보장
    /// - 각 블럭별 샛길(미궁 막다른 길) 방 개수 제어
    /// </summary>
    [CreateAssetMenu(fileName = "DungeonStageTable", menuName = "MythOfGuotaji/DungeonStageTable")]
    public class DungeonStageTableSO : ScriptableObject
    {
        public List<StageDungeonConfig> stages = new()
        {
            new StageDungeonConfig
            {
                stageNumber = 1,
                stageName = "1스테이지 (산해경 기슭)",
                minRoomsBlock1 = 1, // 1번 스테이지: 스타트 - 1칸 이상 - 중간보스
                maxRoomsBlock1 = 2,
                branchRoomsBlock1 = 2,
                minRoomsBlock2 = 3, // 1번 스테이지: 중간보스 - 3칸 이상 - 최종보스
                maxRoomsBlock2 = 4,
                branchRoomsBlock2 = 2,
                roomSize = 30.0f
            },
            new StageDungeonConfig
            {
                stageNumber = 2,
                stageName = "2스테이지 (여우 요괴 미궁)",
                minRoomsBlock1 = 2, // 2번 스테이지: 스타트 - 2칸 이상 - 중간보스
                maxRoomsBlock1 = 3,
                branchRoomsBlock1 = 3,
                minRoomsBlock2 = 3, // 2번 스테이지: 중간보스 - 3칸 이상 - 최종보스
                maxRoomsBlock2 = 5,
                branchRoomsBlock2 = 3,
                roomSize = 30.0f
            },
            new StageDungeonConfig
            {
                stageNumber = 3,
                stageName = "3스테이지 (산해경 심연)",
                minRoomsBlock1 = 3,
                maxRoomsBlock1 = 4,
                branchRoomsBlock1 = 3,
                minRoomsBlock2 = 4,
                maxRoomsBlock2 = 6,
                branchRoomsBlock2 = 3,
                roomSize = 30.0f
            }
        };

        public StageDungeonConfig GetConfigForStage(int stageNumber)
        {
            if (stages == null || stages.Count == 0) return new StageDungeonConfig();
            var cfg = stages.Find(s => s.stageNumber == stageNumber);
            return cfg ?? stages[0];
        }
    }
}
