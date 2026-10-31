using System;
using System.Collections.Generic;
using MythOfGuotaji.Actor;
using UnityEngine;

namespace MythOfGuotaji.Data
{
    /// <summary>
    /// 스테이지별 소환 몬스터 규정 엔트리
    /// Stage00 = { 보스몬스터 1종, 열쇠키퍼 몬스터 1종, 초반방 생성몬스터 Array, 후반방 생성 몬스터 Array }
    /// </summary>
    [Serializable]
    public class StageMonsterEntry
    {
        [Tooltip("스테이지 식별 키 (예: Stage00, Stage01)")]
        public string stageKey = "Stage00";

        [Tooltip("해당 스테이지 최종 보스 몬스터 1종")]
        public MonsterType bossMonster = MonsterType.FoxBoss;

        [Tooltip("해당 스테이지 열쇠 키퍼(중간보스) 몬스터 1종")]
        public MonsterType keyKeeperMonster = MonsterType.Elite;

        [Tooltip("초반방(스타트룸 ~ 중간보스룸 / Block 1) 생성 몬스터 목록")]
        public MonsterType[] earlyRoomMonsters = new MonsterType[]
        {
            MonsterType.Minion,
            MonsterType.Minion_Archer
        };

        [Tooltip("후반방(중간보스룸 ~ 최종보스룸 / Block 2) 생성 몬스터 목록")]
        public MonsterType[] lateRoomMonsters = new MonsterType[]
        {
            MonsterType.Minion_BigMon,
            MonsterType.Minion_Zhu,
            MonsterType.Dokkaebi_Fire
        };
    }

    /// <summary>
    /// 던전에 소환되는 몬스터의 종류를 규정하는 ScriptableObject 테이블
    /// </summary>
    [CreateAssetMenu(fileName = "DungeonMonsterTable", menuName = "MythOfGuotaji/DungeonMonsterTable")]
    public class DungeonMonsterTableSO : ScriptableObject
    {
        [Header("Stage Monster Configs")]
        public List<StageMonsterEntry> stageMonsters = new()
        {
            new StageMonsterEntry
            {
                stageKey = "Stage00",
                bossMonster = MonsterType.FoxBoss,
                keyKeeperMonster = MonsterType.Elite,
                earlyRoomMonsters = new[] { MonsterType.Minion, MonsterType.Minion_Archer },
                lateRoomMonsters = new[] { MonsterType.Minion_BigMon, MonsterType.Minion_Zhu, MonsterType.Dokkaebi_Fire }
            },
            new StageMonsterEntry
            {
                stageKey = "Stage01",
                bossMonster = MonsterType.FoxBoss,
                keyKeeperMonster = MonsterType.Boar_Charger,
                earlyRoomMonsters = new[] { MonsterType.Minion, MonsterType.Boar_Charger },
                lateRoomMonsters = new[] { MonsterType.Spider_Tether, MonsterType.Mud_Slime, MonsterType.Phantom_Fox }
            }
        };

        public StageMonsterEntry GetEntry(string stageKey)
        {
            if (stageMonsters == null || stageMonsters.Count == 0) return null;
            if (string.IsNullOrEmpty(stageKey)) return stageMonsters[0];

            var entry = stageMonsters.Find(s => string.Equals(s.stageKey, stageKey, StringComparison.OrdinalIgnoreCase));
            return entry ?? stageMonsters[0];
        }

        public StageMonsterEntry GetEntryByStageNumber(int stageNumber)
        {
            // stageNumber 0 또는 1 => Stage00 / Stage01 변환
            string key = $"Stage{stageNumber:D2}";
            var entry = stageMonsters.Find(s => string.Equals(s.stageKey, key, StringComparison.OrdinalIgnoreCase));
            if (entry != null) return entry;

            // 1-based index 폴백: stageNumber가 1이면 Stage00 시도
            if (stageNumber >= 1)
            {
                string zeroBasedKey = $"Stage{(stageNumber - 1):D2}";
                entry = stageMonsters.Find(s => string.Equals(s.stageKey, zeroBasedKey, StringComparison.OrdinalIgnoreCase));
                if (entry != null) return entry;
            }

            return stageMonsters.Count > 0 ? stageMonsters[0] : null;
        }
    }
}
