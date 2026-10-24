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

        [MenuItem("MythOfGuotaji/Generate All Prefabs and Tables", priority = 0)]
        public static void GenerateAll()
        {
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

            // 3. 게임 플레이 프리팹 생성
            var playerPrefab = CreatePlayerPrefab();
            var companionPrefab = CreateCompanionPrefab();
            var helperPrefab = CreateHelperPrefab();
            var expGemPrefab = CreateExpGemPrefab();
            var arrowPrefab = CreateArrowPrefab();
            var chargedArrowPrefab = CreateChargedArrowPrefab();
            var minionPrefab = CreateMonsterPrefab(MonsterType.Minion, "Monster_Minion", new Color(0.85f, 0.2f, 0.2f), 1.0f);
            var elitePrefab = CreateMonsterPrefab(MonsterType.Elite, "Monster_Elite", new Color(0.95f, 0.75f, 0.1f), 1.5f);
            var foxBossPrefab = CreateMonsterPrefab(MonsterType.FoxBoss, "Monster_FoxBoss", new Color(0.9f, 0.4f, 0.1f), 2.2f);
            var doorSocketPrefab = CreateDoorSocketPrefab();
            var roomPrefab = CreateDungeonRoomPrefab(doorSocketPrefab);
            var mainCameraPrefab = CreateMainCameraPrefab();

            // 4. 프리팹 데이터베이스 에셋 생성 및 레퍼런스 연결
            var prefabDb = MythOfGuotajiDataSheetManager.LoadOrCreateAsset<PrefabDatabaseSO>($"{DataDir}/PrefabDatabase.asset");
            prefabDb.playerPrefab = playerPrefab;
            prefabDb.companionPrefab = companionPrefab;
            prefabDb.helperCompanionPrefab = helperPrefab;
            prefabDb.expGemPrefab = expGemPrefab;
            prefabDb.arrowProjectilePrefab = arrowPrefab;
            prefabDb.chargedArrowProjectilePrefab = chargedArrowPrefab;
            prefabDb.minionMonsterPrefab = minionPrefab;
            prefabDb.eliteMonsterPrefab = elitePrefab;
            prefabDb.foxBossPrefab = foxBossPrefab;
            prefabDb.doorSocketPrefab = doorSocketPrefab;
            prefabDb.dungeonRoomPrefab = roomPrefab;
            prefabDb.mainCameraPrefab = mainCameraPrefab;

            EditorUtility.SetDirty(prefabDb);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=lime><b>[MythOfGuotaji] 모든 프리팹 및 데이터 테이블(ScriptableObject + CSV) 생성 및 연동 완료!</b></color>");
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

            string path = $"{PrefabsDir}/Player_Guotaji.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateCompanionPrefab()
        {
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

            string path = $"{PrefabsDir}/Companion_Howon.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateHelperPrefab()
        {
            var go = new GameObject("Helper_SpiritRabbit");
            go.AddComponent<Actor.HelperCompanion>();

            string path = $"{PrefabsDir}/Helper_SpiritRabbit.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateExpGemPrefab()
        {
            var go = new GameObject("ExpGem");
            go.AddComponent<Actor.ExpGem>();

            string path = $"{PrefabsDir}/ExpGem.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateArrowPrefab()
        {
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

            string path = $"{PrefabsDir}/Arrow_Projectile.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateChargedArrowPrefab()
        {
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

            string path = $"{PrefabsDir}/Arrow_Charged.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateMonsterPrefab(MonsterType type, string name, Color color, float scale)
        {
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

            string path = $"{PrefabsDir}/{name}.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateDoorSocketPrefab()
        {
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

            string path = $"{PrefabsDir}/DoorSocket.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateDungeonRoomPrefab(GameObject doorSocketPrefab)
        {
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

            string path = $"{PrefabsDir}/DungeonRoom_Base.prefab";
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

            string path = $"{PrefabsDir}/MainCamera.prefab";
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
