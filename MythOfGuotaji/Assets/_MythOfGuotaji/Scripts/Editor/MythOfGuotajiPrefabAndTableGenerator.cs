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

        // 자동 실행 가드: 에셋을 강제로 덮어쓰지 않도록 초기 자동 호출은 비활성화합니다.
        // 필요 시 상단 메뉴 [MythOfGuotaji > Generate All Prefabs and Tables]를 통해 수동 실행하십시오.
        /*
        [InitializeOnLoadMethod]
        private static void AutoGenerateIfMissing()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists($"{DataDir}/PrefabDatabase.asset"))
                {
                    Debug.Log("<color=yellow>[MythOfGuotaji] 프리팹 및 데이터 테이블이 감지되지 않아 자동 생성을 진행합니다...</color>");
                    GenerateAll();
                }
            };
        }
        */

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
            table.dashDuration = 0.2f;
            table.dashCooldown = 1.0f;
            table.sameCategoryGCD = 0.5f;
            table.diffCategoryGCD = 0.2f;
            EditorUtility.SetDirty(table);
            return table;
        }

        private static WeaponStatsTableSO GenerateWeaponStatsTable()
        {
            var table = MythOfGuotajiDataSheetManager.LoadOrCreateAsset<WeaponStatsTableSO>($"{DataDir}/WeaponStatsTable.asset");
            // Bow
            table.bow.baseDamage = 25f;
            table.bow.attackRate = 2.8f;
            table.bow.critChance = 0.15f;
            table.bow.critMultiplier = 2.0f;
            table.bow.baseArrowCount = 1;
            table.bow.spreadAngle = 12f;
            table.bow.basePierceCount = 0;
            table.bow.knockbackPower = 4.5f;
            table.bow.projectileSpeed = 24f;
            table.bow.projectileLifeTime = 2.5f;
            table.bow.projectileHitRadius = 0.4f;
            table.bow.chargeStartTime = 0.3f;
            table.bow.maxChargeTime = 3.0f;
            table.bow.chargeMinRange = 0.0f;
            table.bow.chargeMaxRange = 25.0f;
            table.bow.chargeDamageMultiplier = 2.0f;
            table.bow.chargeProjectileSpeed = 36.0f;
            table.bow.chargeKnockback = 12.0f;
            table.bow.chargeTelegraphWidth = 0.3f;
            table.bow.burstShotCount = 5;
            table.bow.burstTotalDuration = 1.5f;
            table.bow.burstCooldown = 2.0f;
            table.bow.multiHitDamageFalloffRatio = 0.5f;

            // Dagger
            table.dagger.baseDamage = 50f;
            table.dagger.attackRadius = 3.0f;
            table.dagger.attackAngle = 150f;
            table.dagger.cooldown = 0.5f;
            table.dagger.knockback = 9.0f;
            table.dagger.canReflectProjectiles = true;
            table.dagger.reflectDamageMultiplier = 1.5f;
            table.dagger.reflectSpeedMultiplier = 1.2f;

            EditorUtility.SetDirty(table);
            return table;
        }

        private static MonsterStatsTableSO GenerateMonsterStatsTable()
        {
            var table = MythOfGuotajiDataSheetManager.LoadOrCreateAsset<MonsterStatsTableSO>($"{DataDir}/MonsterStatsTable.asset");
            table.entries = new List<MonsterStatEntry>
            {
                new MonsterStatEntry
                {
                    type = MonsterType.Minion,
                    displayName = "산해경 잡몹 (독충/괴조)",
                    maxHp = 35f,
                    moveSpeed = 4.2f,
                    attackDamage = 6f,
                    attackRange = 2.0f,
                    attackCooldown = 1.0f,
                    separationRadius = 0.9f,
                    dropsBossKey = false,
                    affinityReward = 0,
                    goldReward = 2,
                    scaleMultiplier = 1.0f,
                    tintColor = new Color(0.85f, 0.2f, 0.2f),
                    projectileSpeed = 6f,
                    telegraphDuration = 0.3f,
                    specialAttackRadius = 0.5f,
                    gemDropCount = 1,
                    expPerGem = 1,
                    dashDistance = 2.0f,
                    dashWidth = 0.5f,
                    dashDuration = 0.3f
                },
                new MonsterStatEntry
                {
                    type = MonsterType.Minion_Archer,
                    displayName = "맹독충",
                    maxHp = 30f,
                    moveSpeed = 2.7f,
                    attackDamage = 7f,
                    attackRange = 15f,
                    attackCooldown = 0.7f,
                    separationRadius = 1.1f,
                    dropsBossKey = false,
                    affinityReward = 1,
                    goldReward = 3,
                    scaleMultiplier = 0.9f,
                    tintColor = new Color(0.2f, 0.95f, 0.25f),
                    projectileSpeed = 6.0f,
                    kiteDistance = 5.0f,
                    knockbackPower = 2.0f,
                    gemDropCount = 1,
                    expPerGem = 1
                },
                new MonsterStatEntry
                {
                    type = MonsterType.Minion_BigMon,
                    displayName = "거북이",
                    maxHp = 60f,
                    moveSpeed = 2.0f,
                    attackDamage = 12f,
                    attackRange = 2.0f,
                    attackCooldown = 1.8f,
                    separationRadius = 1.4f,
                    dropsBossKey = false,
                    affinityReward = 2,
                    goldReward = 5,
                    scaleMultiplier = 1.4f,
                    tintColor = new Color(0.25f, 0.6f, 0.5f),
                    knockbackPower = 18.0f,
                    telegraphDuration = 0.5f,
                    specialAttackRadius = 2.2f,
                    specialAttackAngle = 90.0f,
                    gemDropCount = 2,
                    expPerGem = 1
                },
                new MonsterStatEntry
                {
                    type = MonsterType.Minion_Zhu,
                    displayName = "주(鴸)",
                    maxHp = 50f,
                    moveSpeed = 1.2f,
                    attackDamage = 18f,
                    attackRange = 20f,
                    attackCooldown = 3.0f,
                    separationRadius = 1.4f,
                    dropsBossKey = false,
                    affinityReward = 2,
                    goldReward = 6,
                    scaleMultiplier = 1.15f,
                    tintColor = new Color(0.72f, 0.22f, 0.85f),
                    projectileSpeed = 10f,
                    kiteDistance = 5.0f,
                    knockbackPower = 4.0f,
                    telegraphDuration = 0.5f,
                    specialAttackRadius = 2.5f,
                    specialAttackAngle = 90.0f,
                    flightDuration = 2.0f,
                    arcHeight = 6.0f,
                    postAttackDelay = 0.2f,
                    gemDropCount = 2,
                    expPerGem = 1
                },
                new MonsterStatEntry
                {
                    type = MonsterType.Elite,
                    displayName = "정예 열쇠 수문장",
                    maxHp = 120f,
                    moveSpeed = 3.5f,
                    attackDamage = 15f,
                    attackRange = 1.8f,
                    attackCooldown = 1.2f,
                    separationRadius = 1.5f,
                    dropsBossKey = true,
                    affinityReward = 1,
                    goldReward = 20,
                    scaleMultiplier = 1.5f,
                    tintColor = new Color(0.95f, 0.75f, 0.1f),
                    knockbackPower = 4.0f,
                    telegraphDuration = 0.5f,
                    specialAttackRadius = 2.5f,
                    specialAttackAngle = 90.0f,
                    gemDropCount = 5,
                    expPerGem = 2
                },
                new MonsterStatEntry
                {
                    type = MonsterType.FoxBoss,
                    displayName = "구미호 여우 요괴",
                    maxHp = 350f,
                    moveSpeed = 4.8f,
                    attackDamage = 20f,
                    attackRange = 2.5f,
                    attackCooldown = 0.8f,
                    separationRadius = 2.2f,
                    dropsBossKey = false,
                    affinityReward = 5,
                    goldReward = 100,
                    scaleMultiplier = 2.2f,
                    tintColor = new Color(0.9f, 0.4f, 0.1f),
                    knockbackPower = 6.0f,
                    telegraphDuration = 0.5f,
                    specialAttackRadius = 2.5f,
                    specialAttackAngle = 90.0f,
                    gemDropCount = 12,
                    expPerGem = 3
                }
            };
            EditorUtility.SetDirty(table);
            return table;
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

        private static HelperDataSO GenerateHelperData()
        {
            var data = MythOfGuotajiDataSheetManager.LoadOrCreateAsset<HelperDataSO>($"{DataDir}/HelperData.asset");
            data.helperId = "Helper_SpiritRabbit";
            data.displayName = "달빛 영혼 토끼";
            data.description = "전투력은 없으나 플레이어 뒤를 따르며, 요괴가 없는 안전 구역의 영혼석을 3미터 진공 흡수하여 비술을 제련해주는 영물입니다.";
            data.moveSpeed = 6.8f;
            data.fleeSpeed = 8.8f;
            data.vacuumRadius = 3.0f;
            data.gemSafetyRadius = 8.5f;
            data.fleeDistance = 5.5f;
            data.bodyColor = new Color(0.9f, 0.95f, 1.0f);
            data.auraColor = new Color(0.2f, 0.9f, 0.8f, 0.35f);
            data.scale = 0.85f;
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
            fill.transform.localScale = new Vector3(5f, 0.01f, 5f);
            StripColliderImmediate(fill);
            SetRendererColor(fill.GetComponent<Renderer>(), new Color(0.95f, 0.15f, 0.25f, 0.40f));

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
