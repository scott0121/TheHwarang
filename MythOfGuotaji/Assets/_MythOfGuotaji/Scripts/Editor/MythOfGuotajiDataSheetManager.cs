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
            ExportMonsterSkills();
            ExportDungeonConfig();

            AssetDatabase.Refresh();
            Debug.Log("<color=green>[DataSheetManager] 모든 테이블(Player, Weapon, MonsterStats, MonsterSkills, DungeonConfig)을 CSV 시트(Assets/_MythOfGuotaji/DataSheets/)로 내보냈습니다.</color>");
        }

        [MenuItem("MythOfGuotaji/Data/Import All CSV Sheets to Tables")]
        public static void ImportAllCsvToTables()
        {
            EnsureDirectories();

            ImportPlayerStats();
            ImportWeaponStats();
            ImportMonsterStats();
            ImportMonsterSkills();
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
                table.entries = new List<MonsterStatEntry>();
                foreach (MonsterType t in Enum.GetValues(typeof(MonsterType)))
                {
                    table.entries.Add(table.GetStat(t));
                }
                EditorUtility.SetDirty(table);
            }

            var sb = new StringBuilder();
            sb.AppendLine("Type,DisplayName,MaxHp,MoveSpeed,AttackDamage,AttackRange,AttackCooldown,SeparationRadius,DropsBossKey,AffinityReward,GoldReward,ScaleMultiplier,ColorHex,Defense,DashDistance,DashDuration,DashWidth,KiteDistance,SpecialSkillIds");

            foreach (var e in table.entries)
            {
                string colHex = ColorUtility.ToHtmlStringRGBA(e.tintColor);
                string skillIds = e.specialSkillIds != null && e.specialSkillIds.Count > 0
                    ? string.Join(";", e.specialSkillIds)
                    : "";
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},#{12},{13},{14},{15},{16},{17},{18}",
                    e.type, e.displayName, e.maxHp, e.moveSpeed, e.attackDamage, e.attackRange, e.attackCooldown,
                    e.separationRadius, e.dropsBossKey, e.affinityReward, e.goldReward, e.scaleMultiplier, colHex,
                    e.defense, e.dashDistance, e.dashDuration, e.dashWidth, e.kiteDistance, skillIds));
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
            if (table.entries == null) table.entries = new List<MonsterStatEntry>();

            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                var cols = lines[i].Split(',');
                if (cols.Length < 13) continue;

                if (!Enum.TryParse<MonsterType>(cols[0], out var mType)) continue;

                // 기존 엔트리가 있으면 패턴/고급 필드를 보존하며 기본 스탯만 갱신 (Merge)
                var entry = table.entries.Find(e => e.type == mType);
                if (entry == null)
                {
                    entry = table.GetStat(mType);
                    table.entries.Add(entry);
                }

                entry.displayName = cols[1];
                entry.maxHp = float.Parse(cols[2], CultureInfo.InvariantCulture);
                entry.moveSpeed = float.Parse(cols[3], CultureInfo.InvariantCulture);
                entry.attackDamage = float.Parse(cols[4], CultureInfo.InvariantCulture);
                entry.attackRange = float.Parse(cols[5], CultureInfo.InvariantCulture);
                entry.attackCooldown = float.Parse(cols[6], CultureInfo.InvariantCulture);
                entry.separationRadius = float.Parse(cols[7], CultureInfo.InvariantCulture);
                entry.dropsBossKey = bool.Parse(cols[8]);
                entry.affinityReward = int.Parse(cols[9], CultureInfo.InvariantCulture);
                entry.goldReward = int.Parse(cols[10], CultureInfo.InvariantCulture);
                entry.scaleMultiplier = float.Parse(cols[11], CultureInfo.InvariantCulture);

                if (ColorUtility.TryParseHtmlString(cols[12], out var col))
                {
                    entry.tintColor = col;
                }

                if (cols.Length >= 19)
                {
                    entry.defense = float.Parse(cols[13], CultureInfo.InvariantCulture);
                    entry.dashDistance = float.Parse(cols[14], CultureInfo.InvariantCulture);
                    entry.dashDuration = float.Parse(cols[15], CultureInfo.InvariantCulture);
                    entry.dashWidth = float.Parse(cols[16], CultureInfo.InvariantCulture);
                    if (cols.Length >= 20)
                    {
                        // 구버전 20개 열 호환: cols[17]이 projectileSpeed, cols[18]이 kiteDistance, cols[19]가 specialSkillIds인 경우
                        if (float.TryParse(cols[17], NumberStyles.Float, CultureInfo.InvariantCulture, out _) &&
                            float.TryParse(cols[18], NumberStyles.Float, CultureInfo.InvariantCulture, out var legacyKite))
                        {
                            entry.kiteDistance = legacyKite;
                            if (!string.IsNullOrWhiteSpace(cols[19]))
                            {
                                entry.specialSkillIds = new List<string>(cols[19].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
                            }
                        }
                        else
                        {
                            entry.kiteDistance = float.Parse(cols[17], CultureInfo.InvariantCulture);
                            if (!string.IsNullOrWhiteSpace(cols[18]))
                            {
                                entry.specialSkillIds = new List<string>(cols[18].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
                            }
                        }
                    }
                    else
                    {
                        // 신버전 19개 열: cols[17]이 kiteDistance, cols[18]이 specialSkillIds
                        entry.kiteDistance = float.Parse(cols[17], CultureInfo.InvariantCulture);
                        if (!string.IsNullOrWhiteSpace(cols[18]))
                        {
                            entry.specialSkillIds = new List<string>(cols[18].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
                        }
                    }
                }
            }

            EditorUtility.SetDirty(table);
        }
        #endregion

        #region Monster Skills
        private static void ExportMonsterSkills()
        {
            var table = LoadOrCreateAsset<MonsterSkillTableSO>($"{DataDirectory}/MonsterSkillTable.asset");
            if (table.skills == null || table.skills.Count == 0)
            {
                table.PopulateDefaults();
                EditorUtility.SetDirty(table);
            }

            var sb = new StringBuilder();
            sb.AppendLine("SkillId,SkillName,SkillType,MinRange,MaxRange,DamageMultiplier,FixedDamage,Cooldown,KnockbackPower,TelegraphDuration,TelegraphShape,AoeRadius,AoeAngle,PostAttackDelay,DashDistance,DashSpeed,DashDuration,DashWidth,ProjectileSpeed,FlightDuration,ArcHeight,ProjectileCount,SpreadAngle,PierceCount,ProjectileParryType");

            foreach (var s in table.skills)
            {
                if (s == null) continue;
                string safeName = (s.skillName ?? "").Replace(",", " ");
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22},{23},{24}",
                    s.skillId, safeName, s.skillType, s.minRange, s.maxRange, s.damageMultiplier, s.fixedDamage,
                    s.cooldown, s.knockbackPower, s.telegraphDuration, s.telegraphShape, s.aoeRadius, s.aoeAngle,
                    s.postAttackDelay, s.dashDistance, s.dashSpeed, s.dashDuration, s.dashWidth,
                    s.projectileSpeed, s.flightDuration, s.arcHeight, s.projectileCount, s.spreadAngle,
                    s.pierceCount, s.projectileParryType));
            }

            File.WriteAllText($"{SheetDirectory}/MonsterSkills.csv", sb.ToString(), Encoding.UTF8);
        }

        private static void ImportMonsterSkills()
        {
            string path = $"{SheetDirectory}/MonsterSkills.csv";
            if (!File.Exists(path)) return;

            var lines = File.ReadAllLines(path);
            if (lines.Length < 2) return;

            var table = LoadOrCreateAsset<MonsterSkillTableSO>($"{DataDirectory}/MonsterSkillTable.asset");
            if (table.skills == null || table.skills.Count == 0)
            {
                table.PopulateDefaults();
            }

            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                var cols = lines[i].Split(',');
                if (cols.Length < 25) continue;

                string skillId = cols[0].Trim();
                if (string.IsNullOrEmpty(skillId)) continue;

                var entry = table.skills.Find(s => s != null && s.skillId == skillId);
                if (entry == null)
                {
                    entry = MonsterSkillTableSO.GetDefaultSkill(skillId);
                    table.skills.Add(entry);
                }

                entry.skillName = cols[1];
                if (Enum.TryParse<MonsterSkillType>(cols[2], out var sType)) entry.skillType = sType;
                entry.minRange = float.Parse(cols[3], CultureInfo.InvariantCulture);
                entry.maxRange = float.Parse(cols[4], CultureInfo.InvariantCulture);
                entry.damageMultiplier = float.Parse(cols[5], CultureInfo.InvariantCulture);
                entry.fixedDamage = float.Parse(cols[6], CultureInfo.InvariantCulture);
                entry.cooldown = float.Parse(cols[7], CultureInfo.InvariantCulture);
                entry.knockbackPower = float.Parse(cols[8], CultureInfo.InvariantCulture);
                entry.telegraphDuration = float.Parse(cols[9], CultureInfo.InvariantCulture);
                entry.telegraphShape = cols[10];
                entry.aoeRadius = float.Parse(cols[11], CultureInfo.InvariantCulture);
                entry.aoeAngle = float.Parse(cols[12], CultureInfo.InvariantCulture);
                entry.postAttackDelay = float.Parse(cols[13], CultureInfo.InvariantCulture);
                entry.dashDistance = float.Parse(cols[14], CultureInfo.InvariantCulture);
                entry.dashSpeed = float.Parse(cols[15], CultureInfo.InvariantCulture);
                entry.dashDuration = float.Parse(cols[16], CultureInfo.InvariantCulture);
                entry.dashWidth = float.Parse(cols[17], CultureInfo.InvariantCulture);
                entry.projectileSpeed = float.Parse(cols[18], CultureInfo.InvariantCulture);
                entry.flightDuration = float.Parse(cols[19], CultureInfo.InvariantCulture);
                entry.arcHeight = float.Parse(cols[20], CultureInfo.InvariantCulture);
                entry.projectileCount = int.Parse(cols[21], CultureInfo.InvariantCulture);
                entry.spreadAngle = float.Parse(cols[22], CultureInfo.InvariantCulture);
                entry.pierceCount = int.Parse(cols[23], CultureInfo.InvariantCulture);
                if (Enum.TryParse<Combat.ProjectileParryType>(cols[24], out var pType)) entry.projectileParryType = pType;
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
