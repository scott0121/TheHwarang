using System;
using System.Collections.Generic;

namespace MythOfGuotaji.Core
{
    [Serializable]
    public class SkillInvestmentEntry
    {
        public string skillNodeId;
        public int investedLevel;

        public SkillInvestmentEntry() {}
        public SkillInvestmentEntry(string id, int level)
        {
            skillNodeId = id;
            investedLevel = level;
        }
    }

    [Serializable]
    public class SaveData
    {
        // 1. 시나리오 트리 (영구적, 리셋 불가)
        public List<string> unlockedScenarioNodeIds = new();
        public int affinityPoints = 0; // 인연 포인트
        public List<string> unlockedCompanionIds = new(); // 합류한 동료 목록

        // 2. 캐릭터 트리 (직업별 커스텀, 언제든 리셋 가능)
        public string currentProfession = "Archer_Basic";
        public int characterSkillPoints = 0;
        public List<SkillInvestmentEntry> skillInvestments = new();

        // 3. 메타 진행도 (기획서 v1.1b 곡도 사당 거점 & 기억의 우물 연동)
        public int currentChapter = 1;
        public int currentStage = 1;
        public string selectedStageCode = "1-1";
        public int gold = 0;

        // 4. 기획서 v1.1b: 기억의 조각 (전 22개) 및 기억의 우물 4대 계열 영구 강화
        public int memoryShards = 4;            // 현재 사용 가능한 기억의 조각
        public int totalCollectedShards = 4;    // 누적 획득한 기억의 조각 (최대 22)
        public int wellBowLevel = 0;            // 활 계열 (화살 위력, 충전 속도, 약점 피해)
        public int wellDaggerLevel = 0;         // 단검 계열 (단검 위력, 패링 유예, 반사 피해)
        public int wellBookLevel = 0;           // 보옥 계열 (보석 획득량, 보호막 효율, 시작방 회복량)
        public int wellSurvivalLevel = 0;       // 생존 계열 (최대 체력, 회피 재사용, 회복 효율)

        // 5. 기획서 v1.1b: 핵심 기억(6종) 및 재도전 가호(Retry Blessing)
        public string equippedCoreMemoryId = "MEM_CH1_FIRST_STRING";
        public List<string> unlockedCoreMemoryIds = new();
        public int retryBlessingStacks = 0;     // 연속 사망 시 중첩 (피해 감면 +3%/중첩, 보옥 보호막 +5%/중첩)
        public bool returnedFromDeath = false;  // 사망 후 사당 우물가로 귀환했는지 여부

        // 생성자 기본값
        public SaveData()
        {
            unlockedScenarioNodeIds = new List<string> { "ROOT_START" };
            unlockedCoreMemoryIds = new List<string> { "MEM_CH1_FIRST_STRING" };
            affinityPoints = 10;
            characterSkillPoints = 5;
            memoryShards = 4;
            totalCollectedShards = 4;
            selectedStageCode = "1-1";
            equippedCoreMemoryId = "MEM_CH1_FIRST_STRING";
        }
    }
}
