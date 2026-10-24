using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using MythOfGuotaji.Actor;
using MythOfGuotaji.Data;
using UnityEditor;
using UnityEngine;

namespace MythOfGuotaji.Editor
{
    /// <summary>
    /// 게임 내 모든 수치 테이블(ScriptableObject)을 CSV 시트로 내보내거나(Export),
    /// 외부에서 편집한 CSV 시트를 ScriptableObject로 불러오는(Import) 시트 관리 도구
    /// </summary>
    public static class MythOfGuotajiDataSheetManager
    {
        public const string SheetDirectory = "Assets/_MythOfGuotaji/DataSheets";
        public const string DataDirectory = "Assets/_MythOfGuotaji/Resources/Data";

        [MenuItem("MythOfGuotaji/Data/Export All Tables to CSV Sheets")]
        public static void ExportAllTablesToCsv()
        {
            EnsureDirectories();

            ExportPlayerStats();
            ExportWeaponStats();
            ExportMonsterStats();
            ExportDungeonConfig();

            AssetDatabase.Refresh();
            Debug.Log("<color=green>[DataSheetManager] 모든 테이블을 CSV 시트(Assets/_MythOfGuotaji/DataSheets/)로 내보냈습니다.</color>");
        }

        [MenuItem("MythOfGuotaji/Data/Import All CSV Sheets to Tables")]
        public static void ImportAllCsvToTables()
        {
            EnsureDirectories();

            ImportPlayerStats();
            ImportWeaponStats();
            ImportMonsterStats();
            ImportDungeonConfig();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=cyan>[DataSheetManager] CSV 시트 데이터를 모든 ScriptableObject 테이블에 반영 완료했습니다.</color>");
        }

        public static void EnsureDirectories()
        {
            if (!Directory.Exists(SheetDirectory))
            {
                Directory.CreateDirectory(SheetDirectory);
            }
            if (!Directory.Exists(DataDirectory))
            {
                Directory.CreateDirectory(DataDirectory);
            }
        }

        #region Player Stats
        private static void ExportPlayerStats()
        {
            var table = LoadOrCreateAsset<PlayerStatsTableSO>($"{DataDirectory}/PlayerStatsTable.asset");
            var sb = new StringBuilder();
            sb.AppendLine("MaxHp,MoveSpeed,DashSpeed,DashDuration,DashCooldown");
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "{0},{1},{2},{3},{4}",
                table.maxHp, table.moveSpeed, table.dashSpeed, table.dashDuration, table.dashCooldown));

            File.WriteAllText($"{SheetDirectory}/PlayerStats.csv", sb.ToString(), Encoding.UTF8);
        }

        private static void ImportPlayerStats()
        {
            string path = $"{SheetDirectory}/PlayerStats.csv";
            if (!File.Exists(path)) return;

            var lines = File.ReadAllLines(path);
            if (lines.Length < 2) return;

            var cols = lines[1].Split(',');
            if (cols.Length < 5) return;

            var table = LoadOrCreateAsset<PlayerStatsTableSO>($"{DataDirectory}/PlayerStatsTable.asset");
            table.maxHp = float.Parse(cols[0], CultureInfo.InvariantCulture);
            table.moveSpeed = float.Parse(cols[1], CultureInfo.InvariantCulture);
            table.dashSpeed = float.Parse(cols[2], CultureInfo.InvariantCulture);
            table.dashDuration = float.Parse(cols[3], CultureInfo.InvariantCulture);
            table.dashCooldown = float.Parse(cols[4], CultureInfo.InvariantCulture);

            EditorUtility.SetDirty(table);
        }
        #endregion

        #region Weapon Stats
        private static void ExportWeaponStats()
        {
            var table = LoadOrCreateAsset<WeaponStatsTableSO>($"{DataDirectory}/WeaponStatsTable.asset");
            var sb = new StringBuilder();
            sb.AppendLine("Category,BaseDamage,AttackRateOrCooldown,CritChanceOrAngle,CritMultiplierOrRadius,Count,SpreadAngle,Pierce,Knockback,ProjSpeed,ProjLifeTime,ProjRadius");
            
            // Bow row
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "Bow,{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10}",
                table.bow.baseDamage, table.bow.attackRate, table.bow.critChance, table.bow.critMultiplier,
                table.bow.baseArrowCount, table.bow.spreadAngle, table.bow.basePierceCount, table.bow.knockbackPower,
                table.bow.projectileSpeed, table.bow.projectileLifeTime, table.bow.projectileHitRadius));

            // Dagger row
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "Dagger,{0},{1},{2},{3},0,0,0,{4},0,0,0",
                table.dagger.baseDamage, table.dagger.cooldown, table.dagger.attackAngle, table.dagger.attackRadius,
                table.dagger.knockback));

            File.WriteAllText($"{SheetDirectory}/WeaponStats.csv", sb.ToString(), Encoding.UTF8);
        }

        private static void ImportWeaponStats()
        {
            string path = $"{SheetDirectory}/WeaponStats.csv";
            if (!File.Exists(path)) return;

            var lines = File.ReadAllLines(path);
            if (lines.Length < 3) return;

            var table = LoadOrCreateAsset<WeaponStatsTableSO>($"{DataDirectory}/WeaponStatsTable.asset");

            // Bow row
            var bowCols = lines[1].Split(',');
            if (bowCols.Length >= 12)
            {
                table.bow.baseDamage = float.Parse(bowCols[1], CultureInfo.InvariantCulture);
                table.bow.attackRate = float.Parse(bowCols[2], CultureInfo.InvariantCulture);
                table.bow.critChance = float.Parse(bowCols[3], CultureInfo.InvariantCulture);
                table.bow.critMultiplier = float.Parse(bowCols[4], CultureInfo.InvariantCulture);
                table.bow.baseArrowCount = int.Parse(bowCols[5], CultureInfo.InvariantCulture);
                table.bow.spreadAngle = float.Parse(bowCols[6], CultureInfo.InvariantCulture);
                table.bow.basePierceCount = int.Parse(bowCols[7], CultureInfo.InvariantCulture);
                table.bow.knockbackPower = float.Parse(bowCols[8], CultureInfo.InvariantCulture);
                table.bow.projectileSpeed = float.Parse(bowCols[9], CultureInfo.InvariantCulture);
                table.bow.projectileLifeTime = float.Parse(bowCols[10], CultureInfo.InvariantCulture);
                table.bow.projectileHitRadius = float.Parse(bowCols[11], CultureInfo.InvariantCulture);
            }

            // Dagger row
            var daggerCols = lines[2].Split(',');
            if (daggerCols.Length >= 9)
            {
                table.dagger.baseDamage = float.Parse(daggerCols[1], CultureInfo.InvariantCulture);
                table.dagger.cooldown = float.Parse(daggerCols[2], CultureInfo.InvariantCulture);
                table.dagger.attackAngle = float.Parse(daggerCols[3], CultureInfo.InvariantCulture);
                table.dagger.attackRadius = float.Parse(daggerCols[4], CultureInfo.InvariantCulture);
                table.dagger.knockback = float.Parse(daggerCols[8], CultureInfo.InvariantCulture);
            }

            EditorUtility.SetDirty(table);
        }
        #endregion

        #region Monster Stats
        private static void ExportMonsterStats()
        {
            var table = LoadOrCreateAsset<MonsterStatsTableSO>($"{DataDirectory}/MonsterStatsTable.asset");
            if (table.entries == null || table.entries.Count == 0)
            {
                // 채워넣기
                table.entries = new List<MonsterStatEntry>
                {
                    table.GetStat(MonsterType.Minion),
                    table.GetStat(MonsterType.Elite),
                    table.GetStat(MonsterType.FoxBoss)
                };
                EditorUtility.SetDirty(table);
            }

            var sb = new StringBuilder();
            sb.AppendLine("Type,DisplayName,MaxHp,MoveSpeed,AttackDamage,AttackRange,AttackCooldown,SeparationRadius,DropsBossKey,AffinityReward,GoldReward,ScaleMultiplier,ColorHex");

            foreach (var e in table.entries)
            {
                string colHex = ColorUtility.ToHtmlStringRGBA(e.tintColor);
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},#{12}",
                    e.type, e.displayName, e.maxHp, e.moveSpeed, e.attackDamage, e.attackRange, e.attackCooldown,
                    e.separationRadius, e.dropsBossKey, e.affinityReward, e.goldReward, e.scaleMultiplier, colHex));
            }

            File.WriteAllText($"{SheetDirectory}/MonsterStats.csv", sb.ToString(), Encoding.UTF8);
        }

        private static void ImportMonsterStats()
        {
            string path = $"{SheetDirectory}/MonsterStats.csv";
            if (!File.Exists(path)) return;

            var lines = File.ReadAllLines(path);
            if (lines.Length < 2) return;

            var table = LoadOrCreateAsset<MonsterStatsTableSO>($"{DataDirectory}/MonsterStatsTable.asset");
            table.entries.Clear();

            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                var cols = lines[i].Split(',');
                if (cols.Length < 13) continue;

                if (!Enum.TryParse<MonsterType>(cols[0], out var mType)) continue;

                var entry = new MonsterStatEntry
                {
                    type = mType,
                    displayName = cols[1],
                    maxHp = float.Parse(cols[2], CultureInfo.InvariantCulture),
                    moveSpeed = float.Parse(cols[3], CultureInfo.InvariantCulture),
                    attackDamage = float.Parse(cols[4], CultureInfo.InvariantCulture),
                    attackRange = float.Parse(cols[5], CultureInfo.InvariantCulture),
                    attackCooldown = float.Parse(cols[6], CultureInfo.InvariantCulture),
                    separationRadius = float.Parse(cols[7], CultureInfo.InvariantCulture),
                    dropsBossKey = bool.Parse(cols[8]),
                    affinityReward = int.Parse(cols[9], CultureInfo.InvariantCulture),
                    goldReward = int.Parse(cols[10], CultureInfo.InvariantCulture),
                    scaleMultiplier = float.Parse(cols[11], CultureInfo.InvariantCulture)
                };

                if (ColorUtility.TryParseHtmlString(cols[12], out var col))
                {
                    entry.tintColor = col;
                }
                else
                {
                    entry.tintColor = Color.white;
                }

                table.entries.Add(entry);
            }

            EditorUtility.SetDirty(table);
        }
        #endregion

        #region Dungeon Config
        private static void ExportDungeonConfig()
        {
            var table = LoadOrCreateAsset<DungeonConfigTableSO>($"{DataDirectory}/DungeonConfigTable.asset");
            var sb = new StringBuilder();
            sb.AppendLine("GridSize,RoomSize,WallHeight,DoorWidth,ExtraLoopRatio,MinDistanceForKeyRoom,NormalRoomMinionCount,TrapRoomMinionCount,KeyRoomMinionCount");
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "{0},{1},{2},{3},{4},{5},{6},{7},{8}",
                table.gridSize, table.roomSize, table.wallHeight, table.doorWidth, table.extraLoopRatio,
                table.minDistanceForKeyRoom, table.normalRoomMinionCount, table.trapRoomMinionCount, table.keyRoomMinionCount));

            File.WriteAllText($"{SheetDirectory}/DungeonConfig.csv", sb.ToString(), Encoding.UTF8);
        }

        private static void ImportDungeonConfig()
        {
            string path = $"{SheetDirectory}/DungeonConfig.csv";
            if (!File.Exists(path)) return;

            var lines = File.ReadAllLines(path);
            if (lines.Length < 2) return;

            var cols = lines[1].Split(',');
            if (cols.Length < 9) return;

            var table = LoadOrCreateAsset<DungeonConfigTableSO>($"{DataDirectory}/DungeonConfigTable.asset");
            table.gridSize = int.Parse(cols[0], CultureInfo.InvariantCulture);
            table.roomSize = float.Parse(cols[1], CultureInfo.InvariantCulture);
            table.wallHeight = float.Parse(cols[2], CultureInfo.InvariantCulture);
            table.doorWidth = float.Parse(cols[3], CultureInfo.InvariantCulture);
            table.extraLoopRatio = float.Parse(cols[4], CultureInfo.InvariantCulture);
            table.minDistanceForKeyRoom = int.Parse(cols[5], CultureInfo.InvariantCulture);
            table.normalRoomMinionCount = int.Parse(cols[6], CultureInfo.InvariantCulture);
            table.trapRoomMinionCount = int.Parse(cols[7], CultureInfo.InvariantCulture);
            table.keyRoomMinionCount = int.Parse(cols[8], CultureInfo.InvariantCulture);

            EditorUtility.SetDirty(table);
        }
        #endregion

        public static T LoadOrCreateAsset<T>(string assetPath) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, assetPath);
            }
            return asset;
        }
    }
}
