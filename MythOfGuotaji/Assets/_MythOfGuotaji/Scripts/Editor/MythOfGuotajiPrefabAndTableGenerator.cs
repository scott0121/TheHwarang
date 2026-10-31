using System.Collections.Generic;
using System.IO;
using MythOfGuotaji.Actor;
using MythOfGuotaji.Combat;
using MythOfGuotaji.Data;
using MythOfGuotaji.Dungeon;
using UnityEditor;
using UnityEngine;

namespace MythOfGuotaji.Editor
{
    /// <summary>
    /// 프로젝트 내의 모든 액터, 무기 투사체, 몬스터, 방 지오메트리를 프리팹으로 구축하고,
    /// 모든 밸런스 수치 테이블(ScriptableObject 및 CSV)을 원클릭으로 생성/연동하는 에디터 툴
    /// </summary>
    public static class MythOfGuotajiPrefabAndTableGenerator
    {
        public const string PrefabsDir = "Assets/_MythOfGuotaji/Resources/Prefabs";
        public const string DataDir = "Assets/_MythOfGuotaji/Resources/Data";

        [MenuItem("MythOfGuotaji/Generate All Prefabs and Tables", priority = 0)]
        public static void GenerateAll()
        {
            if (!EditorUtility.DisplayDialog("프리팹 및 테이블 동기화", 
                "테이블 및 프리팹 데이터베이스를 갱신하시겠습니까?\n(기존에 커스텀 제작된 프리팹은 보존되며 덮어쓰지 않습니다.)", "진행", "취소"))
            {
                return;
            }

            EnsureDirectories();

            // 1. 데이터 테이블 생성
            var playerTable = GeneratePlayerStatsTable();
            var weaponTable = GenerateWeaponStatsTable();
            var monsterTable = GenerateMonsterStatsTable();
            var skillTable = GenerateMonsterSkillTable();
            var dungeonTable = GenerateDungeonConfigTable();
            var fogConfig = GenerateDungeonFogConfigTable();
            var helperData = GenerateHelperData();

            // 2. CSV 시트 내보내기
            MythOfGuotajiDataSheetManager.ExportAllTablesToCsv();

            // 3. 게임 플레이 프리팹 생성 및 데이터베이스 연동 (기존 프리팹 존재 시 보존)
            GenerateMissingPrefabsAndLinkDatabase();

            Debug.Log("<color=lime><b>[MythOfGuotaji] 모든 프리팹 및 데이터 테이블(ScriptableObject + CSV) 생성 및 연동 완료!</b></color>");
        }

        public static void GenerateMissingPrefabsAndLinkDatabase()
        {
            EnsureDirectories();

            var playerPrefab = CreatePlayerPrefab();
            var companionPrefab = CreateCompanionPrefab();
            var helperPrefab = CreateHelperPrefab();
            var helperAuraPrefab = CreateHelperAuraPrefab();
            var expGemPrefab = CreateExpGemPrefab();
            var recoveryHeartPrefab = CreateRecoveryHeartPrefab();
            var arrowPrefab = CreateArrowPrefab();
            var chargedArrowPrefab = CreateChargedArrowPrefab();
            var daggerSlashPrefab = CreateDaggerSlashVfxPrefab();
            var venomProjPrefab = CreateVenomProjectilePrefab();
            var zhuProjPrefab = CreateZhuParabolicProjectilePrefab();
            var dokkaebiFbPrefab = CreateDokkaebiFireballPrefab();
            var totemOrbPrefab = CreateTotemEnergyOrbPrefab();
            var rollingBoulderPrefab = CreateRollingBoulderPrefab();
            var rockShardPrefab = CreateRockShardPrefab();
            var spiderTetherPrefab = CreateSpiderWebTetherPrefab();
            var mudPuddlePrefab = CreateMudPuddleZonePrefab();

            var boxTelegraphPrefab = CreateBoxTelegraphPrefab();
            var sectorTelegraphPrefab = CreateSectorTelegraphPrefab();
            var circleTelegraphPrefab = CreateCircleTelegraphPrefab();
            var chargeAimBoxPrefab = CreateChargeAimBoxPrefab();
            var stretchBoxTelegraph = CreateStretchBoxTelegraphPrefab();
            var targetConnectTelegraph = CreateTargetConnectTelegraphPrefab();
            CreateStretchArcTelegraphPrefab(30);
            CreateStretchArcTelegraphPrefab(45);
            CreateStretchArcTelegraphPrefab(60);
            CreateStretchArcTelegraphPrefab(90);
            CreateStretchArcTelegraphPrefab(120);
            CreateStretchArcTelegraphPrefab(135);
            CreateStretchArcTelegraphPrefab(180);
            CreateStretchArcTelegraphPrefab(210);
            var shockwaveRingPrefab = CreateWaveCylinderPrefab("VFX_ShockwaveRing", new Color(1f, 0.45f, 0.1f, 0.6f));
            var sweepWavePrefab = CreateWaveCylinderPrefab("VFX_SweepWave", new Color(1f, 0.35f, 0.1f, 0.8f));
            var vacuumWavePrefab = CreateWaveCylinderPrefab("VFX_VacuumWave", new Color(0.7f, 0.1f, 0.9f, 0.7f));
            var gemCollectVfxPrefab = CreateGemCollectVfxPrefab();

            var minionPrefab = CreateMonsterPrefab(MonsterType.Minion, "Monster_Minion", new Color(0.85f, 0.2f, 0.2f), 1.0f);
            var archerPrefab = CreateMonsterPrefab(MonsterType.Minion_Archer, "Monster_Archer", new Color(0.2f, 0.95f, 0.25f), 0.9f);
            var bigMonPrefab = CreateMonsterPrefab(MonsterType.Minion_BigMon, "Monster_BigMon", new Color(0.25f, 0.6f, 0.5f), 1.4f);
            var zhuPrefab = CreateMonsterPrefab(MonsterType.Minion_Zhu, "Monster_Zhu", new Color(0.72f, 0.22f, 0.85f), 1.15f);
            var elitePrefab = CreateMonsterPrefab(MonsterType.Elite, "Monster_Elite", new Color(0.95f, 0.75f, 0.1f), 1.5f);
            var foxBossPrefab = CreateMonsterPrefab(MonsterType.FoxBoss, "Monster_FoxBoss", new Color(0.9f, 0.4f, 0.1f), 2.2f);

            var dokkaebiFireMob = CreateMonsterPrefab(MonsterType.Dokkaebi_Fire, "Monster_DokkaebiFire", new Color(1.0f, 0.4f, 0.15f), 1.05f);
            var boarChargerMob = CreateMonsterPrefab(MonsterType.Boar_Charger, "Monster_BoarCharger", new Color(0.55f, 0.35f, 0.2f), 1.35f);
            var spiderTetherMob = CreateMonsterPrefab(MonsterType.Spider_Tether, "Monster_SpiderTether", new Color(0.65f, 0.2f, 0.75f), 1.0f);
            var mudSlimeMob = CreateMonsterPrefab(MonsterType.Mud_Slime, "Monster_MudSlime", new Color(0.42f, 0.3f, 0.18f), 1.15f);
            var phantomFoxMob = CreateMonsterPrefab(MonsterType.Phantom_Fox, "Monster_PhantomFox", new Color(0.9f, 0.75f, 0.95f), 1.1f);
            var ironShieldMob = CreateMonsterPrefab(MonsterType.Iron_Shield, "Monster_IronShield", new Color(0.45f, 0.52f, 0.6f), 1.3f);
            var energyAbsorberMob = CreateMonsterPrefab(MonsterType.Energy_Absorber, "Monster_EnergyAbsorber", new Color(0.2f, 0.8f, 0.95f), 1.2f);
            var bombSparrowMob = CreateMonsterPrefab(MonsterType.Bomb_Sparrow, "Monster_BombSparrow", new Color(1.0f, 0.25f, 0.15f), 0.75f);
            var totemTurretMob = CreateMonsterPrefab(MonsterType.Totem_Turret, "Monster_TotemTurret", new Color(0.9f, 0.78f, 0.2f), 1.4f);

            var doorSocketPrefab = CreateDoorSocketPrefab();
            var roomPrefab = CreateDungeonRoomPrefab(doorSocketPrefab);
            var autoDoorPrefab = CreateDungeonAutoDoorPrefab();
            var fogShroudPrefab = CreateFogShroudPrefab();
            var mainCameraPrefab = CreateMainCameraPrefab();
            var outgameShrineEnvPrefab = CreateOutgameShrineEnvPrefab();

            MythOfGuotajiGUIPrefabGenerator.GenerateAllGUIPrefabs();

            // 4. 프리팹 데이터베이스 에셋 생성 및 모든 레퍼런스 연결
            var prefabDb = MythOfGuotajiDataSheetManager.LoadOrCreateAsset<PrefabDatabaseSO>($"{DataDir}/PrefabDatabase.asset");
            prefabDb.playerPrefab = playerPrefab;
            prefabDb.companionPrefab = companionPrefab;
            prefabDb.helperCompanionPrefab = helperPrefab;
            prefabDb.expGemPrefab = expGemPrefab;
            prefabDb.recoveryHeartPrefab = recoveryHeartPrefab;

            prefabDb.arrowProjectilePrefab = arrowPrefab;
            prefabDb.chargedArrowProjectilePrefab = chargedArrowPrefab;
            prefabDb.daggerSlashVfxPrefab = daggerSlashPrefab;
            prefabDb.venomProjectilePrefab = venomProjPrefab;
            prefabDb.zhuParabolicProjectilePrefab = zhuProjPrefab;
            prefabDb.dokkaebiFireballPrefab = dokkaebiFbPrefab;
            prefabDb.totemEnergyOrbPrefab = totemOrbPrefab;
            prefabDb.rollingBoulderPrefab = rollingBoulderPrefab;
            prefabDb.rockShardPrefab = rockShardPrefab;
            prefabDb.spiderWebTetherPrefab = spiderTetherPrefab;
            prefabDb.mudPuddleZonePrefab = mudPuddlePrefab;

            prefabDb.boxTelegraphPrefab = boxTelegraphPrefab;
            prefabDb.sectorTelegraphPrefab = sectorTelegraphPrefab;
            prefabDb.circleTelegraphPrefab = circleTelegraphPrefab;
            prefabDb.chargeAimBoxPrefab = chargeAimBoxPrefab;
            prefabDb.shockwaveRingPrefab = shockwaveRingPrefab;
            prefabDb.sweepWavePrefab = sweepWavePrefab;
            prefabDb.vacuumWavePrefab = vacuumWavePrefab;
            prefabDb.gemCollectVfxPrefab = gemCollectVfxPrefab;

            prefabDb.minionMonsterPrefab = minionPrefab;
            prefabDb.archerMonsterPrefab = archerPrefab;
            prefabDb.bigMonMonsterPrefab = bigMonPrefab;
            prefabDb.zhuMonsterPrefab = zhuPrefab;
            prefabDb.eliteMonsterPrefab = elitePrefab;
            prefabDb.foxBossPrefab = foxBossPrefab;

            prefabDb.dokkaebiFireMonsterPrefab = dokkaebiFireMob;
            prefabDb.boarChargerMonsterPrefab = boarChargerMob;
            prefabDb.spiderTetherMonsterPrefab = spiderTetherMob;
            prefabDb.mudSlimeMonsterPrefab = mudSlimeMob;
            prefabDb.phantomFoxMonsterPrefab = phantomFoxMob;
            prefabDb.ironShieldMonsterPrefab = ironShieldMob;
            prefabDb.energyAbsorberMonsterPrefab = energyAbsorberMob;
            prefabDb.bombSparrowMonsterPrefab = bombSparrowMob;
            prefabDb.totemTurretMonsterPrefab = totemTurretMob;

            prefabDb.doorSocketPrefab = doorSocketPrefab;
            prefabDb.dungeonRoomPrefab = roomPrefab;
            prefabDb.autoDoorPrefab = autoDoorPrefab;
            prefabDb.fogShroudPrefab = fogShroudPrefab;
            prefabDb.mainCameraPrefab = mainCameraPrefab;
            prefabDb.outgameShrineEnvPrefab = outgameShrineEnvPrefab;

            EditorUtility.SetDirty(prefabDb);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists(PrefabsDir)) Directory.CreateDirectory(PrefabsDir);
            if (!Directory.Exists(DataDir)) Directory.CreateDirectory(DataDir);
            MythOfGuotajiDataSheetManager.EnsureDirectories();
        }

        #region Table Generation
        private static PlayerStatsTableSO GeneratePlayerStatsTable()
        {
            var table = MythOfGuotajiDataSheetManager.LoadOrCreateAsset<PlayerStatsTableSO>($"{DataDir}/PlayerStatsTable.asset");
            table.maxHp = 100f;
            table.moveSpeed = 7.5f;
            table.dashSpeed = 16f;
            table.dashDuration = 0.22f;
            table.dashCooldown = 2.0f;
            table.dashInvincibleDuration = 0.5f;
            table.sameCategoryGCD = 0.5f;
            table.diffCategoryGCD = 0.2f;
            table.heartHealAmount = 5.0f;
            table.heartPickupRadius = 1.25f;

            // Player Action VFX 바인딩
            table.dashDustVfxPrefab = FindPrefabByName("HCFX_Dust_Dash_01");
            table.evasionShineVfxPrefab = FindPrefabByName("HCFX_Shine_01");
            table.hitSparkVfxPrefab = FindPrefabByName("HCFX_Hit_04");
            table.criticalHitSparkVfxPrefab = FindPrefabByName("HCFX_Hit_07");
            table.healBuffVfxPrefab = FindPrefabByName("HCFX_MagicCircle_01");
            table.deathVfxPrefab = FindPrefabByName("HCFX_Explosion_02");

            // Bow Charging Step VFX
            table.chargeAuraStep1VfxPrefab = FindPrefabByName("HCFX_Charging_01_Step1");
            table.chargeAuraStep2VfxPrefab = FindPrefabByName("HCFX_Charging_01_Step2");
            table.chargeAuraStep3VfxPrefab = FindPrefabByName("HCFX_Charging_01_Step3");
            table.chargeReleaseBurstVfxPrefab = FindPrefabByName("HCFX_Charging_01_Step3_End");

            // Fallback Weapon Action Prefabs
            table.defaultArrowPrefab = FindPrefabByName("Projectile_Arrow");
            table.defaultChargedArrowPrefab = FindPrefabByName("Projectile_ChargedArrow");
            table.defaultDaggerSlashVfxPrefab = FindPrefabByName("VFX_DaggerSlash");

            // Timers & Feedback
            table.damageVignetteIntensity = 0.85f;
            table.cameraShakeDuration = 0.35f;
            table.cameraShakeIntensity = 0.20f;
            table.defaultStunDuration = 0.35f;

            EditorUtility.SetDirty(table);
            return table;
        }

        private static WeaponStatsTableSO GenerateWeaponStatsTable()
        {
            var table = MythOfGuotajiDataSheetManager.LoadOrCreateAsset<WeaponStatsTableSO>($"{DataDir}/WeaponStatsTable.asset");
            
            // 1. Bow Catalog (활_00, 활_01, 활_02)
            table.bows = new List<BowEntry>
            {
                new BowEntry
                {
                    weaponId = "Bow_00",
                    weaponName = "기본 곡도 활 (활_00)",
                    description = "서해 용궁의 신성한 활시위. 안정적인 연사력과 차징 성능을 자랑하는 균형형 활입니다.",
                    normalArrowPrefab = FindPrefabByName("Projectile_Arrow"),
                    chargedArrowPrefab = FindPrefabByName("Projectile_ChargedArrow"),
                    stats = new BowBalanceStats
                    {
                        baseDamage = 25f,
                        attackRate = 2.8f,
                        critChance = 0.15f,
                        critMultiplier = 2.0f,
                        baseArrowCount = 1,
                        spreadAngle = 12f,
                        basePierceCount = 0,
                        knockbackPower = 4.5f,
                        projectileSpeed = 24f,
                        projectileLifeTime = 2.5f,
                        projectileHitRadius = 0.4f,
                        chargeStartTime = 0.3f,
                        maxChargeTime = 3.0f,
                        chargeMinRange = 0.0f,
                        chargeMaxRange = 25.0f,
                        chargeDamageMultiplier = 2.0f,
                        chargeProjectileSpeed = 36.0f,
                        chargeKnockback = 12.0f,
                        chargeTelegraphWidth = 0.3f,
                        burstShotCount = 5,
                        burstTotalDuration = 1.5f,
                        burstCooldown = 2.0f,
                        multiHitDamageFalloffRatio = 0.5f
                    }
                },
                new BowEntry
                {
                    weaponId = "Bow_01",
                    weaponName = "현무 강궁 (활_01)",
                    description = "북방 현무의 무거운 신력을 담은 대궁. 차징 피해와 관통력이 극대화되어 보스와 정예 요괴를 단숨에 꿰뚫습니다.",
                    normalArrowPrefab = FindPrefabByName("Projectile_Arrow"),
                    chargedArrowPrefab = FindPrefabByName("Projectile_ChargedArrow"),
                    stats = new BowBalanceStats
                    {
                        baseDamage = 32f,
                        attackRate = 2.2f,
                        critChance = 0.20f,
                        critMultiplier = 2.5f,
                        baseArrowCount = 1,
                        spreadAngle = 10f,
                        basePierceCount = 1,
                        knockbackPower = 7.0f,
                        projectileSpeed = 28f,
                        projectileLifeTime = 3.0f,
                        projectileHitRadius = 0.5f,
                        chargeStartTime = 0.25f,
                        maxChargeTime = 2.5f,
                        chargeMinRange = 0.0f,
                        chargeMaxRange = 30.0f,
                        chargeDamageMultiplier = 2.8f,
                        chargeProjectileSpeed = 45.0f,
                        chargeKnockback = 18.0f,
                        chargeTelegraphWidth = 0.45f,
                        burstShotCount = 4,
                        burstTotalDuration = 1.2f,
                        burstCooldown = 2.5f,
                        multiHitDamageFalloffRatio = 0.6f
                    }
                },
                new BowEntry
                {
                    weaponId = "Bow_02",
                    weaponName = "주작 연궁 (활_02)",
                    description = "남방 주작의 날렵한 깃털 시위. 1회 릴리즈당 7발의 화살을 폭풍처럼 퍼붓는 속사 특화 활입니다.",
                    normalArrowPrefab = FindPrefabByName("Projectile_Arrow"),
                    chargedArrowPrefab = FindPrefabByName("Projectile_ChargedArrow"),
                    stats = new BowBalanceStats
                    {
                        baseDamage = 20f,
                        attackRate = 3.6f,
                        critChance = 0.12f,
                        critMultiplier = 1.8f,
                        baseArrowCount = 1,
                        spreadAngle = 14f,
                        basePierceCount = 0,
                        knockbackPower = 3.5f,
                        projectileSpeed = 26f,
                        projectileLifeTime = 2.2f,
                        projectileHitRadius = 0.35f,
                        chargeStartTime = 0.4f,
                        maxChargeTime = 3.5f,
                        chargeMinRange = 0.0f,
                        chargeMaxRange = 22.0f,
                        chargeDamageMultiplier = 1.6f,
                        chargeProjectileSpeed = 32.0f,
                        chargeKnockback = 8.0f,
                        chargeTelegraphWidth = 0.25f,
                        burstShotCount = 7,
                        burstTotalDuration = 1.4f,
                        burstCooldown = 1.6f,
                        multiHitDamageFalloffRatio = 0.45f
                    }
                }
            };

            // 2. Dagger Catalog (단검_00, 단검_01, 단검_02)
            table.daggers = new List<DaggerEntry>
            {
                new DaggerEntry
                {
                    weaponId = "Dagger_00",
                    weaponName = "기본 옥단검 (단검_00)",
                    description = "곡도 사당의 옥빛 비수. 부채꼴로 적을 베어내며 투사체를 패링 반사하는 균형형 보조무기입니다.",
                    slashVfxPrefab = FindPrefabByName("VFX_DaggerSlash"),
                    parryVfxPrefab = FindPrefabByName("HCFX_MagicCircle_01"),
                    stats = new DaggerBalanceStats
                    {
                        baseDamage = 50f,
                        attackRadius = 3.0f,
                        attackAngle = 150f,
                        cooldown = 0.5f,
                        knockback = 9.0f,
                        canReflectProjectiles = true,
                        reflectDamageMultiplier = 1.5f,
                        reflectSpeedMultiplier = 1.8f
                    }
                },
                new DaggerEntry
                {
                    weaponId = "Dagger_01",
                    weaponName = "벽력 비수 (단검_01)",
                    description = "번개의 영기를 품은 날카로운 비수. 패링 반사 피해와 비행 속도가 대폭 증가하며 참격 각도가 넓어집니다.",
                    slashVfxPrefab = FindPrefabByName("VFX_DaggerSlash"),
                    parryVfxPrefab = FindPrefabByName("HCFX_MagicCircle_01"),
                    stats = new DaggerBalanceStats
                    {
                        baseDamage = 45f,
                        attackRadius = 3.2f,
                        attackAngle = 180f,
                        cooldown = 0.45f,
                        knockback = 8.0f,
                        canReflectProjectiles = true,
                        reflectDamageMultiplier = 2.5f,
                        reflectSpeedMultiplier = 2.5f
                    }
                },
                new DaggerEntry
                {
                    weaponId = "Dagger_02",
                    weaponName = "백호 귀참도 (단검_02)",
                    description = "서방 백호의 기운을 담은 묵직한 참도. 베기 공격력과 사거리, 넉백이 강력하여 근접 방어에 탁월합니다.",
                    slashVfxPrefab = FindPrefabByName("VFX_DaggerSlash"),
                    parryVfxPrefab = FindPrefabByName("HCFX_MagicCircle_01"),
                    stats = new DaggerBalanceStats
                    {
                        baseDamage = 85f,
                        attackRadius = 3.8f,
                        attackAngle = 160f,
                        cooldown = 0.65f,
                        knockback = 14.0f,
                        canReflectProjectiles = true,
                        reflectDamageMultiplier = 1.2f,
                        reflectSpeedMultiplier = 1.5f
                    }
                }
            };

            EditorUtility.SetDirty(table);
            return table;
        }

        private static GameObject FindPrefabByName(string prefabName)
        {
            if (string.IsNullOrEmpty(prefabName)) return null;
            var obj = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_MythOfGuotaji/Resources/Prefabs/{prefabName}.prefab");
            if (obj != null) return obj;
            string[] guids = AssetDatabase.FindAssets($"{prefabName} t:Prefab");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (Path.GetFileNameWithoutExtension(path) == prefabName)
                {
                    return AssetDatabase.LoadAssetAtPath<GameObject>(path);
                }
            }
            return null;
        }

        private static MonsterStatsTableSO GenerateMonsterStatsTable()
        {
            var table = MythOfGuotajiDataSheetManager.LoadOrCreateAsset<MonsterStatsTableSO>($"{DataDir}/MonsterStatsTable.asset");
            table.entries = new List<MonsterStatEntry>();
            foreach (MonsterType mType in System.Enum.GetValues(typeof(MonsterType)))
            {
                var entry = MonsterStatsTableSO.GetDefaultStat(mType);
                entry.monsterPrefab = mType switch
                {
                    MonsterType.Minion => FindPrefabByName("Monster_Minion") ?? FindPrefabByName("Minion_Monster"),
                    MonsterType.Minion_Archer => FindPrefabByName("Monster_Archer") ?? FindPrefabByName("Monster_Minion_Archer"),
                    MonsterType.Minion_BigMon => FindPrefabByName("Monster_BigMon") ?? FindPrefabByName("Monster_Minion_BigMon"),
                    MonsterType.Minion_Zhu => FindPrefabByName("Monster_Zhu") ?? FindPrefabByName("Monster_Minion_Zhu"),
                    MonsterType.Elite => FindPrefabByName("Monster_Elite") ?? FindPrefabByName("Elite_Monster"),
                    MonsterType.FoxBoss => FindPrefabByName("Monster_FoxBoss") ?? FindPrefabByName("Fox_Boss"),
                    MonsterType.Dokkaebi_Fire => FindPrefabByName("Monster_DokkaebiFire") ?? FindPrefabByName("Monster_Dokkaebi_Fire"),
                    MonsterType.Boar_Charger => FindPrefabByName("Monster_BoarCharger") ?? FindPrefabByName("Monster_Boar_Charger"),
                    MonsterType.Spider_Tether => FindPrefabByName("Monster_SpiderTether") ?? FindPrefabByName("Monster_Spider_Tether"),
                    MonsterType.Mud_Slime => FindPrefabByName("Monster_MudSlime") ?? FindPrefabByName("Monster_Mud_Slime"),
                    MonsterType.Phantom_Fox => FindPrefabByName("Monster_PhantomFox") ?? FindPrefabByName("Monster_Phantom_Fox"),
                    MonsterType.Iron_Shield => FindPrefabByName("Monster_IronShield") ?? FindPrefabByName("Monster_Iron_Shield"),
                    MonsterType.Energy_Absorber => FindPrefabByName("Monster_EnergyAbsorber") ?? FindPrefabByName("Monster_Energy_Absorber"),
                    MonsterType.Bomb_Sparrow => FindPrefabByName("Monster_BombSparrow") ?? FindPrefabByName("Monster_Bomb_Sparrow"),
                    MonsterType.Totem_Turret => FindPrefabByName("Monster_TotemTurret") ?? FindPrefabByName("Monster_Totem_Turret"),
                    _ => null
                };
                if (entry.normalAttack != null)
                {
                    entry.normalAttack.hitVfxPrefab = FindPrefabByName("HCFX_Dust_FloorHit_01");
                    entry.normalAttack.telegraphType = "None";
                    entry.normalAttack.telegraphPrefab = null;
                    entry.normalAttack.attackVfxPrefab = mType switch
                    {
                        MonsterType.Minion => FindPrefabByName("HCFX_Dust_Dash_01") ?? FindPrefabByName("HCFX_Flash_01"),
                        MonsterType.Minion_Archer => FindPrefabByName("HCFX_Poison_01"),
                        MonsterType.Minion_BigMon => FindPrefabByName("HCFX_Dust_FloorHit_01"),
                        MonsterType.Minion_Zhu => FindPrefabByName("HCFX_Feathers"),
                        MonsterType.Elite => FindPrefabByName("HCFX_Dust_FloorHit_02"),
                        MonsterType.FoxBoss => FindPrefabByName("HCFX_Flash_02") ?? FindPrefabByName("VFX_SweepWave"),
                        MonsterType.Dokkaebi_Fire => FindPrefabByName("HCFX_ElementOrb_02"),
                        MonsterType.Boar_Charger => FindPrefabByName("HCFX_Dust_Dash_01"),
                        MonsterType.Spider_Tether => FindPrefabByName("HCFX_Poison_02"),
                        MonsterType.Mud_Slime => FindPrefabByName("HCFX_Poison_01"),
                        MonsterType.Phantom_Fox => FindPrefabByName("HCFX_Portal_01"),
                        MonsterType.Iron_Shield => FindPrefabByName("HCFX_Hit_01"),
                        MonsterType.Energy_Absorber => FindPrefabByName("HCFX_Energy_01"),
                        MonsterType.Bomb_Sparrow => FindPrefabByName("HCFX_Explosion_02"),
                        MonsterType.Totem_Turret => FindPrefabByName("HCFX_ElementOrb_01"),
                        _ => null
                    };
                }
                if (entry.specialDrops != null)
                {
                    foreach (var drop in entry.specialDrops)
                    {
                        if (drop != null)
                        {
                            drop.dropVfxPrefab = FindPrefabByName("HCFX_Marble_02_Get");
                        }
                    }
                }
                entry.hitVfxPrefab = FindPrefabByName("HCFX_Hit_01");
                entry.deflectVfxPrefab = FindPrefabByName("HCFX_Hit_02");
                entry.guardBreakVfxPrefab = FindPrefabByName("HCFX_Explosion_01");
                entry.deathVfxPrefab = (mType == MonsterType.FoxBoss || mType == MonsterType.Elite)
                    ? FindPrefabByName("HCFX_Explosion_01")
                    : FindPrefabByName("HCFX_Dust_FloorHit_01") ?? FindPrefabByName("HCFX_Hit_01");
                table.entries.Add(entry);
            }
            EditorUtility.SetDirty(table);
            return table;
        }

        private static MonsterSkillTableSO GenerateMonsterSkillTable()
        {
            var table = MythOfGuotajiDataSheetManager.LoadOrCreateAsset<MonsterSkillTableSO>($"{DataDir}/MonsterSkillTable.asset");
            table.skills = new List<MonsterSkillEntry>();
            string[] skillIds = new string[]
            {
                "minion_dash", "turtle_charge", "toxic_spit", "zhu_parabolic", "elite_boulder",
                "boss_vacuum", "boss_cleave", "dokkaebi_fireball", "boar_charge", "spider_web_tether",
                "mud_puddle", "phantom_decoy", "iron_shield_stance", "energy_absorb",
                "sparrow_kamikaze", "totem_turret_barrage"
            };
            foreach (var id in skillIds)
            {
                var skill = MonsterSkillTableSO.GetDefaultSkill(id);
                if (skill != null)
                {
                    BindDefaultSkillPrefabReferences(skill);
                    table.skills.Add(skill);
                }
            }
            EditorUtility.SetDirty(table);
            return table;
        }

        private static void BindDefaultSkillPrefabReferences(MonsterSkillEntry s)
        {
            if (s == null) return;
            switch (s.skillId)
            {
                case "minion_dash":
                    s.telegraphShape = "StretchBox";
                    s.telegraphPrefab = FindPrefabByName("Telegraph_StretchBox");
                    s.castVfxPrefab = FindPrefabByName("HCFX_Dust_Dash_01");
                    s.trailVfxPrefab = FindPrefabByName("HCFX_Dust_01");
                    s.impactVfxPrefab = FindPrefabByName("HCFX_Dust_FloorHit_01");
                    s.parryVfxPrefab = FindPrefabByName("HCFX_Hit_03");
                    break;
                case "turtle_charge":
                    s.telegraphShape = "StretchBox";
                    s.telegraphPrefab = FindPrefabByName("Telegraph_StretchBox");
                    s.castVfxPrefab = FindPrefabByName("VFX_ShockwaveRing");
                    s.trailVfxPrefab = FindPrefabByName("HCFX_Dust_Dash_01");
                    s.impactVfxPrefab = FindPrefabByName("HCFX_Dust_FloorHit_02");
                    s.parryVfxPrefab = FindPrefabByName("HCFX_Hit_03");
                    break;
                case "toxic_spit":
                    s.telegraphShape = "TargetCircle";
                    s.telegraphPrefab = FindPrefabByName("Telegraph_Circle");
                    s.projectilePrefab = FindPrefabByName("Projectile_Venom");
                    s.castVfxPrefab = FindPrefabByName("HCFX_Splash_02");
                    s.trailVfxPrefab = FindPrefabByName("HCFX_Splash_01");
                    s.impactVfxPrefab = FindPrefabByName("HCFX_Splash_03");
                    s.parryVfxPrefab = FindPrefabByName("HCFX_Hit_02");
                    break;
                case "zhu_parabolic":
                    s.telegraphShape = "TargetCircle";
                    s.projectilePrefab = FindPrefabByName("Projectile_ZhuParabolic");
                    s.telegraphPrefab = FindPrefabByName("Telegraph_Circle");
                    s.castVfxPrefab = FindPrefabByName("HCFX_Feathers");
                    s.trailVfxPrefab = FindPrefabByName("HCFX_Fire_02");
                    s.impactVfxPrefab = FindPrefabByName("HCFX_Explosion_01");
                    s.parryVfxPrefab = FindPrefabByName("HCFX_Hit_03");
                    break;
                case "elite_boulder":
                    s.telegraphShape = "StretchBox";
                    s.projectilePrefab = FindPrefabByName("RollingBoulder");
                    s.subProjectilePrefab = FindPrefabByName("RockShard");
                    s.telegraphPrefab = FindPrefabByName("Telegraph_StretchBox");
                    s.castVfxPrefab = FindPrefabByName("HCFX_Dust_FloorHit_02");
                    s.trailVfxPrefab = FindPrefabByName("HCFX_Dust_01");
                    s.impactVfxPrefab = FindPrefabByName("HCFX_Explosion_02");
                    s.parryVfxPrefab = FindPrefabByName("HCFX_Hit_03");
                    break;
                case "boss_vacuum":
                    s.telegraphShape = "StretchArc90";
                    s.aoeAngle = 90.0f;
                    s.telegraphPrefab = FindPrefabByName("Telegraph_StretchArc90") ?? FindPrefabByName("Telegraph_Sector");
                    s.castVfxPrefab = FindPrefabByName("VFX_VacuumWave");
                    s.trailVfxPrefab = FindPrefabByName("HCFX_Portal_01");
                    s.impactVfxPrefab = FindPrefabByName("HCFX_Energy_03");
                    break;
                case "boss_cleave":
                    s.telegraphShape = "StretchArc180";
                    s.aoeAngle = 180.0f;
                    s.telegraphPrefab = FindPrefabByName("Telegraph_StretchArc180") ?? FindPrefabByName("Telegraph_Sector");
                    s.castVfxPrefab = FindPrefabByName("VFX_SweepWave");
                    s.trailVfxPrefab = FindPrefabByName("HCFX_Fire_02");
                    s.impactVfxPrefab = FindPrefabByName("HCFX_Explosion_02");
                    s.parryVfxPrefab = FindPrefabByName("HCFX_Hit_03");
                    break;
                case "dokkaebi_fireball":
                    s.telegraphShape = "None";
                    s.telegraphPrefab = null;
                    s.projectilePrefab = FindPrefabByName("Projectile_DokkaebiFireball");
                    s.castVfxPrefab = FindPrefabByName("HCFX_ElementOrb_02");
                    s.trailVfxPrefab = FindPrefabByName("HCFX_Fire_02");
                    s.impactVfxPrefab = FindPrefabByName("HCFX_Explosion_01");
                    s.parryVfxPrefab = FindPrefabByName("HCFX_Hit_03");
                    break;
                case "boar_charge":
                    s.telegraphShape = "StretchBox";
                    s.telegraphPrefab = FindPrefabByName("Telegraph_StretchBox");
                    s.castVfxPrefab = FindPrefabByName("HCFX_Dust_Dash_01");
                    s.trailVfxPrefab = FindPrefabByName("HCFX_Dust_01");
                    s.impactVfxPrefab = FindPrefabByName("HCFX_Dust_FloorHit_02");
                    s.parryVfxPrefab = FindPrefabByName("HCFX_Hit_03");
                    break;
                case "spider_web_tether":
                    s.telegraphShape = "TargetConnect";
                    s.projectilePrefab = FindPrefabByName("Skill_SpiderWebTether");
                    s.telegraphPrefab = FindPrefabByName("Telegraph_TargetConnect");
                    s.castVfxPrefab = FindPrefabByName("HCFX_Poison_02");
                    s.impactVfxPrefab = FindPrefabByName("HCFX_Hit_01");
                    break;
                case "mud_puddle":
                    s.telegraphShape = "TargetCircle";
                    s.projectilePrefab = FindPrefabByName("Skill_MudPuddleZone");
                    s.telegraphPrefab = FindPrefabByName("Telegraph_Circle");
                    s.castVfxPrefab = FindPrefabByName("HCFX_Poison_01");
                    s.impactVfxPrefab = FindPrefabByName("HCFX_Splash_03");
                    break;
                case "phantom_decoy":
                    s.telegraphShape = "None";
                    s.telegraphPrefab = null;
                    s.castVfxPrefab = FindPrefabByName("HCFX_Portal_01");
                    s.impactVfxPrefab = FindPrefabByName("HCFX_Dust_01");
                    break;
                case "iron_shield_stance":
                    s.telegraphShape = "None";
                    s.telegraphPrefab = null;
                    s.castVfxPrefab = FindPrefabByName("HCFX_Hit_02");
                    s.impactVfxPrefab = FindPrefabByName("HCFX_Explosion_01");
                    break;
                case "energy_absorb":
                    s.telegraphShape = "None";
                    s.telegraphPrefab = null;
                    s.castVfxPrefab = FindPrefabByName("HCFX_Fire_02");
                    s.impactVfxPrefab = FindPrefabByName("HCFX_Explosion_01");
                    break;
                case "sparrow_kamikaze":
                    s.telegraphShape = "TargetConnect";
                    s.telegraphPrefab = FindPrefabByName("Telegraph_TargetConnect");
                    s.castVfxPrefab = FindPrefabByName("HCFX_Fire_02");
                    s.trailVfxPrefab = FindPrefabByName("HCFX_Fire_02");
                    s.impactVfxPrefab = FindPrefabByName("HCFX_Explosion_01");
                    s.parryVfxPrefab = FindPrefabByName("HCFX_Hit_03");
                    break;
                case "totem_turret_barrage":
                    s.telegraphShape = "None";
                    s.telegraphPrefab = null;
                    s.projectilePrefab = FindPrefabByName("Projectile_TotemOrb") ?? FindPrefabByName("Projectile_TotemEnergyOrb");
                    s.castVfxPrefab = FindPrefabByName("HCFX_ElementOrb_01");
                    s.impactVfxPrefab = FindPrefabByName("HCFX_Hit_02");
                    break;
            }
        }

        private static DungeonConfigTableSO GenerateDungeonConfigTable()
        {
            var table = MythOfGuotajiDataSheetManager.LoadOrCreateAsset<DungeonConfigTableSO>($"{DataDir}/DungeonConfigTable.asset");
            table.gridSize = 6;
            table.roomSize = 39f;
            table.wallHeight = 4.5f;
            table.doorWidth = 12.0f;
            table.extraLoopRatio = 0.25f;
            table.minDistanceForKeyRoom = 3;
            table.normalRoomMinionCount = 6;
            table.trapRoomMinionCount = 10;
            table.keyRoomMinionCount = 5;
            EditorUtility.SetDirty(table);
            return table;
        }

        private static Dungeon.DungeonFogConfigSO GenerateDungeonFogConfigTable()
        {
            var config = MythOfGuotajiDataSheetManager.LoadOrCreateAsset<Dungeon.DungeonFogConfigSO>($"{DataDir}/DungeonFogConfig.asset");
            config.fogColor = new Color(0.015f, 0.015f, 0.025f, 1.0f);
            config.maxDarkness = 0.90f;
            config.exploredDarkness = 0.50f;
            config.fovAngleDegrees = 160f;
            config.fovEdgeSoftness = 0.05f;
            config.clearDistance = 30f;
            config.falloffDistance = 45f;
            config.alwaysClearRadius = 10f;
            config.rearDarkDistance = 15f;
            config.doorFadeAngle = 15f;
            config.doorHalfWidth = 6.0f;
            EditorUtility.SetDirty(config);
            return config;
        }

        private static GameObject CreateHelperAuraPrefab()
        {
            string path = "{PrefabsDir}/Helper_00_Aura.prefab";
            if (File.Exists(path))
            {
                var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (existing != null) return existing;
            }

            var go = new GameObject("Helper_00_Aura");
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "AuraRing";
            ring.transform.SetParent(go.transform, false);
            ring.transform.localPosition = new Vector3(0f, -0.2f, 0f);
            ring.transform.localScale = new Vector3(1.2f, 0.02f, 1.2f);
            StripColliderImmediate(ring);
            SetRendererColor(ring.GetComponent<Renderer>(), new Color(0.2f, 0.9f, 0.8f, 0.4f));

            var glow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            glow.name = "AuraGlow";
            glow.transform.SetParent(go.transform, false);
            glow.transform.localPosition = Vector3.zero;
            glow.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
            StripColliderImmediate(glow);
            SetRendererColor(glow.GetComponent<Renderer>(), new Color(0.3f, 0.95f, 1.0f, 0.25f));

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static HelperDataSO GenerateHelperData()
        {
            var data = MythOfGuotajiDataSheetManager.LoadOrCreateAsset<HelperDataSO>($"{DataDir}/HelperData.asset");
            var rabbitPrefab = FindPrefabByName("Helper_SpiritRabbit");
            var auraPrefab = FindPrefabByName("Helper_00_Aura") ?? CreateHelperAuraPrefab();

            data.helpers = new List<HelperEntry>
            {
                new HelperEntry
                {
                    helperId = "Helper_00",
                    displayName = "달빛 영혼 토끼",
                    description = "전투력은 없으나 플레이어 뒤를 따르며, 요괴가 없는 안전 구역의 영혼석을 3미터 진공 흡수하여 비술을 제련해주는 영물입니다.",
                    companionPrefab = rabbitPrefab,
                    auraPrefab = auraPrefab,
                    moveSpeed = 6.8f,
                    fleeSpeed = 8.8f,
                    vacuumRadius = 3.0f,
                    gemSafetyRadius = 8.5f,
                    fleeDistance = 5.5f,
                    scale = 0.85f
                },
                new HelperEntry
                {
                    helperId = "Helper_01",
                    displayName = "도깨비 영혼 불",
                    description = "푸른 영혼의 불꽃. 플레이어 곁을 호위하며 안전 구역의 영혼석을 빠른 속도로 진공 흡수합니다.",
                    companionPrefab = rabbitPrefab,
                    auraPrefab = auraPrefab,
                    moveSpeed = 7.2f,
                    fleeSpeed = 9.2f,
                    vacuumRadius = 3.2f,
                    gemSafetyRadius = 8.0f,
                    fleeDistance = 5.0f,
                    scale = 0.8f
                },
                new HelperEntry
                {
                    helperId = "Helper_02",
                    displayName = "신선 조롱박",
                    description = "비술을 머금은 신성한 조롱박 영물. 넓은 진공 흡수 반경으로 영혼석을 모아 비술을 정제합니다.",
                    companionPrefab = rabbitPrefab,
                    auraPrefab = auraPrefab,
                    moveSpeed = 6.5f,
                    fleeSpeed = 8.5f,
                    vacuumRadius = 3.5f,
                    gemSafetyRadius = 9.0f,
                    fleeDistance = 6.0f,
                    scale = 0.9f
                }
            };

            EditorUtility.SetDirty(data);
            return data;
        }
        #endregion

        #region Prefab Creation
        private static GameObject CreatePlayerPrefab()
        {
            string path = $"{PrefabsDir}/Player_Guotaji.prefab";
            if (File.Exists(path))
            {
                var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (existing != null) return existing;
            }

            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Player_Guotaji";
            go.tag = "Player";

            var rend = go.GetComponent<Renderer>();
            SetRendererColor(rend, new Color(0.2f, 0.6f, 0.9f));

            var cc = go.GetComponent<CharacterController>();
            if (cc == null) cc = go.AddComponent<CharacterController>();
            cc.height = 2f;
            cc.radius = 0.5f;
            cc.center = Vector3.zero;

            go.AddComponent<BowWeapon>();
            go.AddComponent<DaggerWeapon>();
            go.AddComponent<PlayerController>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateCompanionPrefab()
        {
            string path = $"{PrefabsDir}/Companion_Howon.prefab";
            if (File.Exists(path))
            {
                var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (existing != null) return existing;
            }

            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Companion_Howon";
            go.tag = "Player";

            var rend = go.GetComponent<Renderer>();
            SetRendererColor(rend, new Color(0.2f, 0.8f, 0.6f));

            var cc = go.GetComponent<CharacterController>();
            if (cc == null) cc = go.AddComponent<CharacterController>();
            cc.height = 2f;
            cc.radius = 0.5f;
            cc.center = Vector3.zero;

            go.AddComponent<CompanionAI>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateHelperPrefab()
        {
            string path = $"{PrefabsDir}/Helper_SpiritRabbit.prefab";
            if (File.Exists(path))
            {
                var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (existing != null) return existing;
            }

            var go = new GameObject("Helper_SpiritRabbit");
            go.AddComponent<Actor.HelperCompanion>();

            var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            body.name = "HelperBody";
            body.transform.SetParent(go.transform, false);
            body.transform.localScale = new Vector3(0.75f, 0.85f, 0.75f);
            StripColliderImmediate(body);
            SetRendererColor(body.GetComponent<Renderer>(), new Color(0.92f, 0.95f, 1.0f));

            CreateHelperEarChild(body.transform, "Ear_Left", new Vector3(-0.2f, 0.45f, 0.05f), new Vector3(0.18f, 0.45f, 0.18f));
            CreateHelperEarChild(body.transform, "Ear_Right", new Vector3(0.2f, 0.45f, 0.05f), new Vector3(0.18f, 0.45f, 0.18f));
            CreateHelperEyeChild(body.transform, "Eye_Left", new Vector3(-0.16f, 0.05f, 0.32f));
            CreateHelperEyeChild(body.transform, "Eye_Right", new Vector3(0.16f, 0.05f, 0.32f));

            var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orb.name = "SkillOrbVisual";
            orb.transform.SetParent(go.transform, false);
            orb.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            orb.transform.localScale = Vector3.one * 0.45f;
            StripColliderImmediate(orb);
            SetRendererColor(orb.GetComponent<Renderer>(), new Color(1f, 0.85f, 0.2f, 0.95f));
            orb.SetActive(false);

            var ring = new GameObject("VacuumAuraRing_3m", typeof(MeshFilter), typeof(MeshRenderer));
            ring.transform.SetParent(go.transform, false);
            ring.transform.localPosition = new Vector3(0f, -0.77f, 0f);
            SetRendererColor(ring.GetComponent<Renderer>(), new Color(0.15f, 0.9f, 0.8f, 0.45f));

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static void CreateHelperEarChild(Transform parent, string name, Vector3 localPos, Vector3 localScale)
        {
            var ear = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            ear.name = name;
            ear.transform.SetParent(parent, false);
            ear.transform.localPosition = localPos;
            ear.transform.localRotation = Quaternion.Euler(15f, 0f, localPos.x * 25f);
            ear.transform.localScale = localScale;
            StripColliderImmediate(ear);
            SetRendererColor(ear.GetComponent<Renderer>(), new Color(0.98f, 0.88f, 0.92f));
        }

        private static void CreateHelperEyeChild(Transform parent, string name, Vector3 localPos)
        {
            var eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eye.name = name;
            eye.transform.SetParent(parent, false);
            eye.transform.localPosition = localPos;
            eye.transform.localScale = new Vector3(0.12f, 0.16f, 0.12f);
            StripColliderImmediate(eye);
            SetRendererColor(eye.GetComponent<Renderer>(), new Color(0.1f, 0.4f, 0.9f));
        }

        private static GameObject CreateExpGemPrefab()
        {
            string path = $"{PrefabsDir}/ExpGem.prefab";
            if (File.Exists(path))
            {
                var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (existing != null) return existing;
            }

            var go = new GameObject("ExpGem", typeof(MeshFilter), typeof(MeshRenderer), typeof(SphereCollider), typeof(Actor.ExpGem));
            go.transform.localScale = Vector3.one * 0.32f;
            var col = go.GetComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.35f;
            SetRendererColor(go.GetComponent<Renderer>(), new Color(0.1f, 0.95f, 0.75f));

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateRecoveryHeartPrefab()
        {
            string path = $"{PrefabsDir}/RecoveryHeart.prefab";
            if (File.Exists(path))
            {
                var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (existing != null) return existing;
            }

            var go = new GameObject("RecoveryHeart", typeof(Actor.RecoveryHeart));
            var heartRoot = new GameObject("HeartVisual");
            heartRoot.transform.SetParent(go.transform, false);

            Color rubyRed = new Color(1.0f, 0.12f, 0.28f, 0.95f);

            var lobeL = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lobeL.name = "Lobe_Left";
            lobeL.transform.SetParent(heartRoot.transform, false);
            lobeL.transform.localPosition = new Vector3(-0.16f, 0.15f, 0f);
            lobeL.transform.localScale = new Vector3(0.32f, 0.32f, 0.24f);
            lobeL.transform.localRotation = Quaternion.Euler(0f, 0f, 25f);
            StripColliderImmediate(lobeL);
            SetRendererColor(lobeL.GetComponent<Renderer>(), rubyRed);

            var lobeR = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lobeR.name = "Lobe_Right";
            lobeR.transform.SetParent(heartRoot.transform, false);
            lobeR.transform.localPosition = new Vector3(0.16f, 0.15f, 0f);
            lobeR.transform.localScale = new Vector3(0.32f, 0.32f, 0.24f);
            lobeR.transform.localRotation = Quaternion.Euler(0f, 0f, -25f);
            StripColliderImmediate(lobeR);
            SetRendererColor(lobeR.GetComponent<Renderer>(), rubyRed);

            var tip = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tip.name = "Heart_Tip";
            tip.transform.SetParent(heartRoot.transform, false);
            tip.transform.localPosition = new Vector3(0f, -0.05f, 0f);
            tip.transform.localScale = new Vector3(0.28f, 0.24f, 0.22f);
            tip.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            StripColliderImmediate(tip);
            SetRendererColor(tip.GetComponent<Renderer>(), rubyRed);

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateDaggerSlashVfxPrefab()
        {
            string path = $"{PrefabsDir}/VFX_DaggerSlash.prefab";
            if (File.Exists(path))
            {
                var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (existing != null) return existing;
            }

            var go = new GameObject("VFX_DaggerSlash", typeof(MeshFilter), typeof(MeshRenderer));
            SetRendererColor(go.GetComponent<Renderer>(), new Color(0.1f, 0.95f, 1f, 0.85f));
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateVenomProjectilePrefab()
        {
            string path = $"{PrefabsDir}/Projectile_Venom.prefab";
            if (File.Exists(path))
            {
                var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (existing != null) return existing;
            }

            var go = new GameObject("Projectile_Venom");
            go.tag = "Monster";
            var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "VenomVisual";
            visual.transform.SetParent(go.transform, false);
            visual.transform.localScale = Vector3.one * 0.45f;
            StripColliderImmediate(visual);
            SetRendererColor(visual.GetComponent<Renderer>(), new Color(0.2f, 0.95f, 0.25f));

            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.35f;
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            go.AddComponent<Projectile>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateZhuParabolicProjectilePrefab()
        {
            string path = $"{PrefabsDir}/Projectile_ZhuParabolic.prefab";
            if (File.Exists(path))
            {
                var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (existing != null) return existing;
            }

            var go = new GameObject("Projectile_ZhuParabolic");
            go.tag = "Monster";
            var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "ZhuOrbVisual";
            visual.transform.SetParent(go.transform, false);
            visual.transform.localScale = Vector3.one * 0.65f;
            StripColliderImmediate(visual);
            SetRendererColor(visual.GetComponent<Renderer>(), new Color(0.85f, 0.15f, 0.95f));

            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.45f;
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            go.AddComponent<ZhuParabolicProjectile>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateDokkaebiFireballPrefab()
        {
            string path = $"{PrefabsDir}/Projectile_DokkaebiFireball.prefab";
            if (File.Exists(path))
            {
                var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (existing != null && existing.GetComponent<Collider>() != null) return existing;
            }

            var go = new GameObject("Projectile_DokkaebiFireball", typeof(DokkaebiFireball));
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "FireballVisual";
            sphere.transform.SetParent(go.transform, false);
            sphere.transform.localScale = Vector3.one * 0.7f;
            StripColliderImmediate(sphere);
            SetRendererColor(sphere.GetComponent<Renderer>(), new Color(1f, 0.45f, 0.1f, 0.9f));

            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.45f;
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateTotemEnergyOrbPrefab()
        {
            string path = $"{PrefabsDir}/Projectile_TotemOrb.prefab";
            if (File.Exists(path))
            {
                var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (existing != null && existing.GetComponent<Collider>() != null && existing.GetComponent<TotemEnergyOrb>() != null && existing.GetComponent<Projectile>() == null) return existing;
            }

            var go = new GameObject("Projectile_TotemOrb", typeof(TotemEnergyOrb));
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "OrbVisual";
            sphere.transform.SetParent(go.transform, false);
            sphere.transform.localScale = Vector3.one * 0.5f;
            StripColliderImmediate(sphere);
            SetRendererColor(sphere.GetComponent<Renderer>(), new Color(1.0f, 0.82f, 0.1f, 0.95f));

            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.35f;
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateRollingBoulderPrefab()
        {
            string path = $"{PrefabsDir}/RollingBoulder.prefab";
            if (File.Exists(path))
            {
                var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (existing != null && existing.GetComponent<Collider>() != null) return existing;
            }

            var go = new GameObject("RollingBoulder", typeof(RollingBoulder));
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "BoulderModel";
            sphere.transform.SetParent(go.transform, false);
            sphere.transform.localScale = Vector3.one * 0.9f;
            StripColliderImmediate(sphere);
            SetRendererColor(sphere.GetComponent<Renderer>(), new Color(0.38f, 0.35f, 0.32f));

            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.45f;
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            Vector3[] cragOffsets =
            {
                new Vector3(0.32f, 0.15f, 0.1f),
                new Vector3(-0.3f, 0.2f, -0.15f),
                new Vector3(0.1f, -0.28f, 0.25f),
                new Vector3(-0.15f, -0.2f, -0.3f)
            };
            for (int i = 0; i < cragOffsets.Length; i++)
            {
                var crag = GameObject.CreatePrimitive(PrimitiveType.Cube);
                crag.name = $"Crag_{i}";
                crag.transform.SetParent(sphere.transform, false);
                crag.transform.localPosition = cragOffsets[i];
                crag.transform.localRotation = Quaternion.Euler(25f * i, 40f * i, 15f * i);
                crag.transform.localScale = Vector3.one * 0.35f;
                StripColliderImmediate(crag);
                SetRendererColor(crag.GetComponent<Renderer>(), new Color(0.25f, 0.22f, 0.2f));
            }

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateRockShardPrefab()
        {
            string path = $"{PrefabsDir}/RockShard.prefab";
            if (File.Exists(path))
            {
                var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (existing != null) return existing;
            }

            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "RockShard";
            go.transform.localScale = Vector3.one * 0.25f;
            StripColliderImmediate(go);
            SetRendererColor(go.GetComponent<Renderer>(), new Color(0.32f, 0.28f, 0.25f));
            go.AddComponent<Rigidbody>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateSpiderWebTetherPrefab()
        {
            string path = $"{PrefabsDir}/Skill_SpiderWebTether.prefab";
            if (File.Exists(path))
            {
                var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (existing != null) return existing;
            }

            var go = new GameObject("Skill_SpiderWebTether", typeof(LineRenderer), typeof(SpiderWebTether));
            var lr = go.GetComponent<LineRenderer>();
            lr.startWidth = 0.08f;
            lr.endWidth = 0.08f;
            lr.positionCount = 2;

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateMudPuddleZonePrefab()
        {
            string path = $"{PrefabsDir}/Skill_MudPuddleZone.prefab";
            if (File.Exists(path))
            {
                var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (existing != null) return existing;
            }

            var go = new GameObject("Skill_MudPuddleZone", typeof(MudPuddleZone));
            var cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cyl.name = "MudVisual";
            cyl.transform.SetParent(go.transform, false);
            cyl.transform.localScale = new Vector3(6f, 0.01f, 6f);
            StripColliderImmediate(cyl);
            SetRendererColor(cyl.GetComponent<Renderer>(), new Color(0.35f, 0.25f, 0.12f, 0.65f));

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateBoxTelegraphPrefab()
        {
            string path = $"{PrefabsDir}/Telegraph_Box.prefab";
            if (File.Exists(path))
            {
                var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (existing != null) return existing;
            }

            var go = new GameObject("Telegraph_Box");
            var fill = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fill.name = "Fill";
            fill.transform.SetParent(go.transform, false);
            fill.transform.localScale = new Vector3(1.0f, 0.005f, 5.0f);
            StripColliderImmediate(fill);
            SetRendererColor(fill.GetComponent<Renderer>(), new Color(1f, 0.25f, 0.1f, 0.35f));

            CreateTelegraphBorderChild(fill.transform, new Vector3(-0.48f, 0.5f, 0f), new Vector3(0.06f, 1.2f, 1f), new Color(1f, 0.4f, 0.15f, 0.85f));
            CreateTelegraphBorderChild(fill.transform, new Vector3(0.48f, 0.5f, 0f), new Vector3(0.06f, 1.2f, 1f), new Color(1f, 0.4f, 0.15f, 0.85f));
            CreateTelegraphBorderChild(fill.transform, new Vector3(0f, 0.5f, 0.5f), new Vector3(1f, 1.2f, 0.04f), new Color(1f, 0.4f, 0.15f, 0.85f));

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static void CreateTelegraphBorderChild(Transform parent, Vector3 localPos, Vector3 localScale, Color color)
        {
            var line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            line.name = "BorderLine";
            line.transform.SetParent(parent, false);
            line.transform.localPosition = localPos;
            line.transform.localScale = localScale;
            StripColliderImmediate(line);
            SetRendererColor(line.GetComponent<Renderer>(), color);
        }

        private static GameObject CreateSectorTelegraphPrefab()
        {
            string path = $"{PrefabsDir}/Telegraph_Sector.prefab";
            if (File.Exists(path))
            {
                var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (existing != null) return existing;
            }

            var go = new GameObject("Telegraph_Sector", typeof(MeshFilter), typeof(MeshRenderer));
            SetRendererColor(go.GetComponent<Renderer>(), new Color(1f, 0.25f, 0.1f, 0.42f));
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateCircleTelegraphPrefab()
        {
            string path = $"{PrefabsDir}/Telegraph_Circle.prefab";
            if (File.Exists(path))
            {
                var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (existing != null) return existing;
            }

            var go = new GameObject("Telegraph_Circle");
            var fill = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            fill.name = "TelegraphFill";
            fill.transform.SetParent(go.transform, false);
            // 지름 1미터 기본 원형 (피벗 중심)
            fill.transform.localPosition = new Vector3(0f, 0.01f, 0f);
            fill.transform.localScale = new Vector3(1.0f, 0.005f, 1.0f);
            StripColliderImmediate(fill);
            SetRendererColor(fill.GetComponent<Renderer>(), new Color(0.95f, 0.15f, 0.25f, 0.40f));

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateStretchBoxTelegraphPrefab()
        {
            string path = $"{PrefabsDir}/Telegraph_StretchBox.prefab";
            if (File.Exists(path))
            {
                var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (existing != null) return existing;
            }

            var go = new GameObject("Telegraph_StretchBox");
            var fill = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fill.name = "Fill";
            fill.transform.SetParent(go.transform, false);
            // 시작점 (0,0,0)을 피벗으로 하여 전방 Z축 (0 ~ 1) 방향으로 확장
            fill.transform.localPosition = new Vector3(0f, 0.01f, 0.5f);
            fill.transform.localScale = new Vector3(1.0f, 0.005f, 1.0f);
            StripColliderImmediate(fill);
            SetRendererColor(fill.GetComponent<Renderer>(), new Color(1f, 0.25f, 0.1f, 0.35f));

            CreateTelegraphBorderChild(fill.transform, new Vector3(-0.48f, 0.5f, 0f), new Vector3(0.04f, 1.2f, 1f), new Color(1f, 0.4f, 0.15f, 0.85f));
            CreateTelegraphBorderChild(fill.transform, new Vector3(0.48f, 0.5f, 0f), new Vector3(0.04f, 1.2f, 1f), new Color(1f, 0.4f, 0.15f, 0.85f));
            CreateTelegraphBorderChild(fill.transform, new Vector3(0f, 0.5f, 0.5f), new Vector3(1f, 1.2f, 0.04f), new Color(1f, 0.4f, 0.15f, 0.85f));

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateStretchArcTelegraphPrefab(int angle)
        {
            string path = $"{PrefabsDir}/Telegraph_StretchArc{angle}.prefab";
            if (File.Exists(path))
            {
                var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (existing != null) return existing;
            }

            string meshesDir = "Assets/_MythOfGuotaji/Resources/Meshes";
            if (!Directory.Exists(meshesDir)) Directory.CreateDirectory(meshesDir);

            string meshPath = $"{meshesDir}/Mesh_StretchArc_{angle}.asset";
            Mesh arcMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (arcMesh == null)
            {
                arcMesh = GenerateUnitArcMesh(angle);
                AssetDatabase.CreateAsset(arcMesh, meshPath);
            }

            var go = new GameObject($"Telegraph_StretchArc{angle}", typeof(MeshFilter), typeof(MeshRenderer));
            go.GetComponent<MeshFilter>().sharedMesh = arcMesh;
            SetRendererColor(go.GetComponent<Renderer>(), new Color(1f, 0.25f, 0.1f, 0.42f));

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static Mesh GenerateUnitArcMesh(float angle, int segments = 32)
        {
            Mesh mesh = new Mesh { name = $"UnitArc_{angle}" };
            Vector3[] vertices = new Vector3[segments + 2];
            int[] triangles = new int[segments * 3];
            Vector2[] uvs = new Vector2[segments + 2];

            vertices[0] = Vector3.zero;
            uvs[0] = new Vector2(0.5f, 0f);

            float halfAngle = angle * 0.5f;
            for (int i = 0; i <= segments; i++)
            {
                float curAngle = Mathf.Lerp(-halfAngle, halfAngle, (float)i / segments);
                float rad = curAngle * Mathf.Deg2Rad;
                float x = Mathf.Sin(rad);
                float z = Mathf.Cos(rad);
                vertices[i + 1] = new Vector3(x, 0.01f, z);
                uvs[i + 1] = new Vector2((x + 1f) * 0.5f, z);
            }

            for (int i = 0; i < segments; i++)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static GameObject CreateTargetConnectTelegraphPrefab()
        {
            string path = $"{PrefabsDir}/Telegraph_TargetConnect.prefab";
            if (File.Exists(path))
            {
                var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (existing != null) return existing;
            }

            var go = new GameObject("Telegraph_TargetConnect", typeof(LineRenderer), typeof(Actor.Monsters.TelegraphTargetConnectTracker));
            var lr = go.GetComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.startWidth = 0.25f;
            lr.endWidth = 0.25f;
            lr.useWorldSpace = true;
            SetRendererColor(lr, new Color(0.9f, 0.15f, 0.85f, 0.65f));

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateChargeAimBoxPrefab()
        {
            string path = $"{PrefabsDir}/Telegraph_ChargeAimBox.prefab";
            if (File.Exists(path))
            {
                var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (existing != null) return existing;
            }

            var go = new GameObject("Telegraph_ChargeAimBox");
            var fill = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fill.name = "FillPlane";
            fill.transform.SetParent(go.transform, false);
            fill.transform.localPosition = new Vector3(0f, 0.02f, 0.5f);
            fill.transform.localScale = new Vector3(0.3f, 0.01f, 1.0f);
            StripColliderImmediate(fill);
            SetRendererColor(fill.GetComponent<Renderer>(), new Color(0.15f, 0.85f, 1.0f, 0.40f));

            CreateTelegraphBorderChild(fill.transform, new Vector3(-0.48f, 0.5f, 0f), new Vector3(0.08f, 1.2f, 1f), new Color(0.35f, 0.95f, 1.0f, 0.85f));
            CreateTelegraphBorderChild(fill.transform, new Vector3(0.48f, 0.5f, 0f), new Vector3(0.08f, 1.2f, 1f), new Color(0.35f, 0.95f, 1.0f, 0.85f));
            CreateTelegraphBorderChild(fill.transform, new Vector3(0f, 0.5f, 0.5f), new Vector3(1f, 1.2f, 0.02f), new Color(0.35f, 0.95f, 1.0f, 0.85f));

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateWaveCylinderPrefab(string prefabName, Color color)
        {
            string path = $"{PrefabsDir}/{prefabName}.prefab";
            if (File.Exists(path))
            {
                var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (existing != null) return existing;
            }

            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = prefabName;
            go.transform.localScale = new Vector3(2f, 0.02f, 2f);
            StripColliderImmediate(go);
            SetRendererColor(go.GetComponent<Renderer>(), color);

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateGemCollectVfxPrefab()
        {
            string path = $"{PrefabsDir}/VFX_GemCollect.prefab";
            if (File.Exists(path))
            {
                var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (existing != null) return existing;
            }

            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "VFX_GemCollect";
            go.transform.localScale = Vector3.one * 0.35f;
            StripColliderImmediate(go);
            SetRendererColor(go.GetComponent<Renderer>(), new Color(0.2f, 0.95f, 0.85f, 0.9f));

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateDungeonAutoDoorPrefab()
        {
            string path = $"{PrefabsDir}/DungeonAutoDoor.prefab";
            if (File.Exists(path))
            {
                var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (existing != null) return existing;
            }

            var go = new GameObject("DungeonAutoDoor", typeof(DungeonAutoDoor));
            float doorWidth = 6.05f;
            float doorHeight = 4.2f;
            float doorThickness = 0.35f;

            var leftObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftObj.name = "AutoDoor_LeftLeaf";
            leftObj.transform.SetParent(go.transform, false);
            leftObj.transform.localPosition = new Vector3(-doorWidth * 0.5f, doorHeight * 0.5f, 0f);
            leftObj.transform.localScale = new Vector3(doorWidth, doorHeight, doorThickness);
            SetRendererColor(leftObj.GetComponent<Renderer>(), new Color(0.24f, 0.16f, 0.11f));

            var rightObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightObj.name = "AutoDoor_RightLeaf";
            rightObj.transform.SetParent(go.transform, false);
            rightObj.transform.localPosition = new Vector3(doorWidth * 0.5f, doorHeight * 0.5f, 0f);
            rightObj.transform.localScale = new Vector3(doorWidth, doorHeight, doorThickness);
            SetRendererColor(rightObj.GetComponent<Renderer>(), new Color(0.24f, 0.16f, 0.11f));

            CreateAutoDoorStud(leftObj.transform);
            CreateAutoDoorStud(rightObj.transform);

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static void CreateAutoDoorStud(Transform parent)
        {
            var stud = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stud.name = "Studs";
            stud.transform.SetParent(parent, false);
            stud.transform.localPosition = new Vector3(0f, 0f, -0.2f);
            stud.transform.localScale = new Vector3(0.3f, 0.8f, 0.15f);
            StripColliderImmediate(stud);
            SetRendererColor(stud.GetComponent<Renderer>(), new Color(0.7f, 0.55f, 0.2f));
        }

        private static GameObject CreateFogShroudPrefab()
        {
            string path = $"{PrefabsDir}/FogShroud_Overlay.prefab";
            if (File.Exists(path))
            {
                var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (existing != null) return existing;
            }

            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "FogShroud_Overlay";
            go.transform.localPosition = new Vector3(0f, 4.65f, 0f);
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            go.transform.localScale = new Vector3(39.78f, 39.78f, 1f);
            StripColliderImmediate(go);
            SetRendererColor(go.GetComponent<Renderer>(), new Color(0.015f, 0.015f, 0.025f, 0.90f));

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        /// <summary>
        /// 아웃게임(Intro.unity) 곡도 사당 거점("보옥의 결계 우물가") 3D 배경 환경 프리팹 생성
        /// </summary>
        private static GameObject CreateOutgameShrineEnvPrefab()
        {
            string path = $"{PrefabsDir}/Outgame_GokdoShrineEnv.prefab";
            if (File.Exists(path))
            {
                var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (existing != null) return existing;
            }

            var root = new GameObject("Outgame_GokdoShrineEnv");

            // 1. 사당 석재 마당 바닥
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            floor.name = "ShrineFloor";
            floor.transform.SetParent(root.transform, false);
            floor.transform.localPosition = new Vector3(0f, -0.2f, 0f);
            floor.transform.localScale = new Vector3(24f, 0.2f, 24f);
            SetRendererColor(floor.GetComponent<Renderer>(), new Color(0.16f, 0.20f, 0.26f));

            // 2. 중앙 기억의 우물 (Well of Memory)
            var wellRoot = new GameObject("WellOfMemory");
            wellRoot.transform.SetParent(root.transform, false);
            wellRoot.transform.localPosition = Vector3.zero;

            var wellRim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wellRim.name = "WellStoneRim";
            wellRim.transform.SetParent(wellRoot.transform, false);
            wellRim.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            wellRim.transform.localScale = new Vector3(3.2f, 0.45f, 3.2f);
            SetRendererColor(wellRim.GetComponent<Renderer>(), new Color(0.32f, 0.36f, 0.42f));

            var wellWater = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wellWater.name = "WellWaterGlow";
            wellWater.transform.SetParent(wellRoot.transform, false);
            wellWater.transform.localPosition = new Vector3(0f, 0.68f, 0f);
            wellWater.transform.localScale = new Vector3(2.6f, 0.25f, 2.6f);
            SetRendererColor(wellWater.GetComponent<Renderer>(), new Color(0.15f, 0.92f, 0.98f));

            var wellLightGo = new GameObject("WellSpiritLight", typeof(Light));
            wellLightGo.transform.SetParent(wellRoot.transform, false);
            wellLightGo.transform.localPosition = new Vector3(0f, 2.2f, 0f);
            var wellLight = wellLightGo.GetComponent<Light>();
            wellLight.type = LightType.Point;
            wellLight.color = new Color(0.25f, 0.92f, 1f);
            wellLight.intensity = 4.5f;
            wellLight.range = 14f;

            // 3. 사당 결계 석주 기둥 4개
            Vector3[] pillarPositions =
            {
                new Vector3(-5.5f, 2.2f, 5.5f),
                new Vector3(5.5f, 2.2f, 5.5f),
                new Vector3(-5.5f, 2.2f, -5.5f),
                new Vector3(5.5f, 2.2f, -5.5f)
            };
            for (int i = 0; i < pillarPositions.Length; i++)
            {
                var p = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                p.name = $"SanctuaryPillar_{i + 1}";
                p.transform.SetParent(root.transform, false);
                p.transform.localPosition = pillarPositions[i];
                p.transform.localScale = new Vector3(0.8f, 2.2f, 0.8f);
                SetRendererColor(p.GetComponent<Renderer>(), new Color(0.26f, 0.30f, 0.38f));
            }

            // 4. 거타지 & 보옥(보석 토끼) & 거점 NPC 마커 배치
            var guotajiMarker = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            guotajiMarker.name = "Guotaji_HubPreview";
            guotajiMarker.transform.SetParent(root.transform, false);
            guotajiMarker.transform.localPosition = new Vector3(-2.2f, 1.0f, -1.2f);
            guotajiMarker.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            SetRendererColor(guotajiMarker.GetComponent<Renderer>(), new Color(0.2f, 0.65f, 0.95f));

            var bookMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bookMarker.name = "BookRabbit_HubPreview";
            bookMarker.transform.SetParent(root.transform, false);
            bookMarker.transform.localPosition = new Vector3(-1.1f, 1.15f, -0.4f);
            bookMarker.transform.localScale = Vector3.one * 0.65f;
            SetRendererColor(bookMarker.GetComponent<Renderer>(), new Color(0.95f, 0.97f, 1.0f));

            var dragonNpc = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            dragonNpc.name = "NPC_DragonKingCouple";
            dragonNpc.transform.SetParent(root.transform, false);
            dragonNpc.transform.localPosition = new Vector3(3.2f, 1.1f, 2.4f);
            SetRendererColor(dragonNpc.GetComponent<Renderer>(), new Color(0.3f, 0.85f, 0.65f));

            var haeroNpc = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            haeroNpc.name = "NPC_HaeroSeaTurtle";
            haeroNpc.transform.SetParent(root.transform, false);
            haeroNpc.transform.localPosition = new Vector3(-3.8f, 0.7f, 2.8f);
            haeroNpc.transform.localScale = new Vector3(1.5f, 0.8f, 1.8f);
            SetRendererColor(haeroNpc.GetComponent<Renderer>(), new Color(0.25f, 0.55f, 0.5f));

            var howonNpc = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            howonNpc.name = "NPC_HowonHwarang";
            howonNpc.transform.SetParent(root.transform, false);
            howonNpc.transform.localPosition = new Vector3(2.6f, 1.0f, -2.0f);
            SetRendererColor(howonNpc.GetComponent<Renderer>(), new Color(0.95f, 0.75f, 0.25f));

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void StripColliderImmediate(GameObject go)
        {
            var col = go.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
        }

        private static GameObject CreateArrowPrefab()
        {
            string path = $"{PrefabsDir}/Arrow_Projectile.prefab";
            if (File.Exists(path))
            {
                var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (existing != null) return existing;
            }

            var go = new GameObject("Arrow_Projectile");

            // 시각적 실린더
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.name = "Visual";
            visual.transform.SetParent(go.transform, false);
            visual.transform.localScale = new Vector3(0.08f, 0.45f, 0.08f);
            visual.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            var visualCol = visual.GetComponent<Collider>();
            if (visualCol != null) Object.DestroyImmediate(visualCol);

            var rend = visual.GetComponent<Renderer>();
            SetRendererColor(rend, new Color(1.0f, 0.9f, 0.3f));

            // SphereCollider 충돌 검출체
            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.4f;

            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;

            go.AddComponent<Projectile>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateChargedArrowPrefab()
        {
            string path = $"{PrefabsDir}/Arrow_Charged.prefab";
            if (File.Exists(path))
            {
                var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (existing != null) return existing;
            }

            var go = new GameObject("Arrow_Charged");

            // 시각적 대형 실린더
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.name = "ChargedVisual";
            visual.transform.SetParent(go.transform, false);
            visual.transform.localScale = new Vector3(0.2f, 0.85f, 0.2f);
            visual.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            var visualCol = visual.GetComponent<Collider>();
            if (visualCol != null) Object.DestroyImmediate(visualCol);

            var rend = visual.GetComponent<Renderer>();
            SetRendererColor(rend, new Color(0.2f, 0.85f, 1.0f)); // 푸른 영기 번개빛

            // SphereCollider 충돌 검출체 (더 큰 피격 판정)
            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.65f;

            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;

            go.AddComponent<Projectile>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateMonsterPrefab(MonsterType type, string name, Color color, float scale)
        {
            string path = $"{PrefabsDir}/{name}.prefab";
            if (File.Exists(path))
            {
                var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (existing != null) return existing;
            }

            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = name;
            go.tag = "Monster";
            go.transform.localScale = Vector3.one * scale;

            var rend = go.GetComponent<Renderer>();
            SetRendererColor(rend, color);

            var cc = go.GetComponent<CharacterController>();
            if (cc == null) cc = go.AddComponent<CharacterController>();
            cc.height = 2f;
            cc.radius = 0.5f;
            cc.center = Vector3.zero;

            go.AddComponent<MonsterAI>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateDoorSocketPrefab()
        {
            string path = $"{PrefabsDir}/DoorSocket.prefab";
            if (File.Exists(path))
            {
                var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (existing != null) return existing;
            }

            var root = new GameObject("DoorSocket");
            var socket = root.AddComponent<DoorSocket>();

            float wallH = 4.5f;
            float doorW = 12.0f;
            float roomSize = 39f;
            float sideWallLen = (roomSize - doorW) * 0.5f; // 16.5m
            float sideOffset = (doorW + sideWallLen) * 0.5f; // 11.25m
            Color wallCol = new Color(0.28f, 0.3f, 0.35f);

            // 좌측 벽
            var leftWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftWall.name = "SideWall_Left";
            leftWall.transform.SetParent(root.transform, false);
            leftWall.transform.localPosition = new Vector3(-sideOffset, wallH * 0.5f, 0f);
            leftWall.transform.localScale = new Vector3(sideWallLen, wallH, 0.9f);
            SetRendererColor(leftWall.GetComponent<Renderer>(), wallCol);

            // 우측 벽
            var rightWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightWall.name = "SideWall_Right";
            rightWall.transform.SetParent(root.transform, false);
            rightWall.transform.localPosition = new Vector3(sideOffset, wallH * 0.5f, 0f);
            rightWall.transform.localScale = new Vector3(sideWallLen, wallH, 0.9f);
            SetRendererColor(rightWall.GetComponent<Renderer>(), wallCol);

            // 중앙 폐쇄벽
            var centerWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            centerWall.name = "SolidCenterWall";
            centerWall.transform.SetParent(root.transform, false);
            centerWall.transform.localPosition = new Vector3(0f, wallH * 0.5f, 0f);
            centerWall.transform.localScale = new Vector3(doorW + 0.1f, wallH, 0.9f);
            SetRendererColor(centerWall.GetComponent<Renderer>(), wallCol);

            // 중앙 봉인 배리어
            var barrier = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barrier.name = "DoorBarrier";
            barrier.transform.SetParent(root.transform, false);
            barrier.transform.localPosition = new Vector3(0f, wallH * 0.5f, 0f);
            barrier.transform.localScale = new Vector3(doorW, wallH * 0.9f, 0.2f);
            SetRendererColor(barrier.GetComponent<Renderer>(), new Color(0.9f, 0.2f, 0.2f, 0.85f));
            barrier.SetActive(false);

            // 기둥 (Visual)
            var pillars = new GameObject("Pillars");
            pillars.transform.SetParent(root.transform, false);

            var pLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pLeft.name = "Pillar_Left";
            pLeft.transform.SetParent(pillars.transform, false);
            pLeft.transform.localPosition = new Vector3(-doorW * 0.5f, wallH * 0.5f, 0f);
            pLeft.transform.localScale = new Vector3(0.8f, wallH * 1.05f, 1.2f);
            SetRendererColor(pLeft.GetComponent<Renderer>(), new Color(0.35f, 0.38f, 0.42f));

            var pRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pRight.name = "Pillar_Right";
            pRight.transform.SetParent(pillars.transform, false);
            pRight.transform.localPosition = new Vector3(doorW * 0.5f, wallH * 0.5f, 0f);
            pRight.transform.localScale = new Vector3(0.8f, wallH * 1.05f, 1.2f);
            SetRendererColor(pRight.GetComponent<Renderer>(), new Color(0.35f, 0.38f, 0.42f));

            socket.InitializeComponents(Direction.North, centerWall, barrier, pillars);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateDungeonRoomPrefab(GameObject doorSocketPrefab)
        {
            string path = $"{PrefabsDir}/DungeonRoom_Base.prefab";
            if (File.Exists(path))
            {
                var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (existing != null) return existing;
            }

            var room = new GameObject("DungeonRoom_Base");
            var ctrl = room.AddComponent<RoomController>();

            float roomSize = 39f;

            // 1. 바닥 (Floor)
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.SetParent(room.transform, false);
            floor.transform.localPosition = Vector3.zero;
            floor.transform.localScale = new Vector3(roomSize / 10f, 1f, roomSize / 10f);
            SetRendererColor(floor.GetComponent<Renderer>(), new Color(0.22f, 0.24f, 0.28f));

            // 2. 트리거 콜라이더
            var boxCol = room.AddComponent<BoxCollider>();
            boxCol.isTrigger = true;
            boxCol.center = new Vector3(0f, 2f, 0f);
            boxCol.size = new Vector3(roomSize * 0.9f, 4f, roomSize * 0.9f);

            // 3. 4방향 소켓 인스턴스화
            var northSocket = InstantiateSocket(doorSocketPrefab, room.transform, Direction.North, new Vector3(0f, 0f, roomSize * 0.5f), Quaternion.identity);
            var eastSocket = InstantiateSocket(doorSocketPrefab, room.transform, Direction.East, new Vector3(roomSize * 0.5f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f));
            var southSocket = InstantiateSocket(doorSocketPrefab, room.transform, Direction.South, new Vector3(0f, 0f, -roomSize * 0.5f), Quaternion.Euler(0f, 180f, 0f));
            var westSocket = InstantiateSocket(doorSocketPrefab, room.transform, Direction.West, new Vector3(-roomSize * 0.5f, 0f, 0f), Quaternion.Euler(0f, 270f, 0f));

            ctrl.SetSockets(northSocket, eastSocket, southSocket, westSocket);

            var prefab = PrefabUtility.SaveAsPrefabAsset(room, path);
            Object.DestroyImmediate(room);
            return prefab;
        }

        private static DoorSocket InstantiateSocket(GameObject socketPrefab, Transform parent, Direction dir, Vector3 localPos, Quaternion localRot)
        {
            GameObject socketObj;
            if (socketPrefab != null)
            {
                socketObj = (GameObject)PrefabUtility.InstantiatePrefab(socketPrefab, parent);
            }
            else
            {
                socketObj = new GameObject($"Socket_{dir}");
                socketObj.transform.SetParent(parent, false);
                socketObj.AddComponent<DoorSocket>();
            }

            socketObj.name = $"Socket_{dir}";
            socketObj.transform.localPosition = localPos;
            socketObj.transform.localRotation = localRot;

            var ds = socketObj.GetComponent<DoorSocket>();
            ds.SetDirection(dir);
            return ds;
        }

        private static GameObject CreateMainCameraPrefab()
        {
            string path = $"{PrefabsDir}/MainCamera.prefab";
            if (File.Exists(path))
            {
                var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (existing != null) return existing;
            }

            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";

            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.043f, 0.047f, 0.063f, 1f); // #0B0C10
            cam.fieldOfView = 48f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 120f;

            go.AddComponent<AudioListener>();

            var qCam = go.AddComponent<Cameras.QuarterViewCamera>();
            go.transform.rotation = Quaternion.Euler(52f, 0f, 0f);

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static void SetRendererColor(Renderer rend, Color color)
        {
            if (rend == null) return;
            var mat = rend.sharedMaterial != null ? new Material(rend.sharedMaterial) : new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            rend.sharedMaterial = mat;
        }
        #endregion
    }
}
