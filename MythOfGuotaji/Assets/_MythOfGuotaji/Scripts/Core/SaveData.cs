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

        // 3. 메타 진행도
        public int currentChapter = 1;
        public int currentStage = 1;
        public int gold = 0;

        // 생성자 기본값
        public SaveData()
        {
            unlockedScenarioNodeIds = new List<string> { "ROOT_START" };
            affinityPoints = 10;
            characterSkillPoints = 5;
        }
    }
}
