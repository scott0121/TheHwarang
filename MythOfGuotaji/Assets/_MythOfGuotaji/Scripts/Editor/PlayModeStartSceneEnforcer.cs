#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using MythOfGuotaji.Core;
using MythOfGuotaji.Data;
using MythOfGuotaji.Dungeon;
using MythOfGuotaji.UI;

namespace MythOfGuotaji.Editor
{
    /// <summary>
    /// 1) 프로시저럴 오브젝트들의 프리팹화 및 PrefabDatabase 자동 연결 보장
    /// 2) Outgame 씬(Assets/Scenes/Intro.unity) 및 Ingame 씬(Assets/Scenes/PlayMain.unity) 자동 생성/검증
    /// 3) 에디터의 어느 씬에서 Play 버튼을 누르더라도 항상 Intro.unity에서 시작하도록 playModeStartScene 강제 적용
    /// </summary>
    [InitializeOnLoad]
    public static class PlayModeStartSceneEnforcer
    {
        public const string IntroScenePath = "Assets/Scenes/Intro.unity";
        public const string PlayMainScenePath = "Assets/Scenes/PlayMain.unity";
        public const string SampleScenePath = "Assets/Scenes/SampleScene.unity";

        private const string PrefKeyEnforceIntroStart = "MythOfGuotaji_EnforceIntroStartScene";
        private static bool _initializedSession;

        static PlayModeStartSceneEnforcer()
        {
            EditorApplication.delayCall += OnEditorDelayedInitialize;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        public static bool IsEnforceIntroEnabled
        {
            get => EditorPrefs.GetBool(PrefKeyEnforceIntroStart, true);
            set => EditorPrefs.SetBool(PrefKeyEnforceIntroStart, value);
        }

        private static void OnEditorDelayedInitialize()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                ApplyPlayModeStartScene();
                return;
            }

            if (!_initializedSession)
            {
                _initializedSession = true;
                EnsurePrefabsAndScenesExist(silent: true);
            }

            ApplyPlayModeStartScene();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                ApplyPlayModeStartScene();
            }
        }

        [MenuItem("MythOfGuotaji/Setup Intro & PlayMain Scenes + Enforce Intro Start", priority = 1)]
        public static void ManualSetupAll()
        {
            IsEnforceIntroEnabled = true;
            EnsurePrefabsAndScenesExist(silent: false);
            ApplyPlayModeStartScene();
            Debug.Log("[PlayModeStartSceneEnforcer] 프리팹 생성, Intro/PlayMain 씬 구성 및 Intro 씬 시작 설정이 완료되었습니다.");
        }

        [MenuItem("MythOfGuotaji/Toggle Always Start From Intro Scene", priority = 2)]
        public static void ToggleEnforceIntroStart()
        {
            IsEnforceIntroEnabled = !IsEnforceIntroEnabled;
            ApplyPlayModeStartScene();
            Debug.Log($"[PlayModeStartSceneEnforcer] 항상 Intro 씬에서 Play 시작: {(IsEnforceIntroEnabled ? "활성화됨 (ON)" : "비활성화됨 (OFF - 현재 열린 씬에서 시작)")}");
        }

        public static void ApplyPlayModeStartScene()
        {
            if (!IsEnforceIntroEnabled)
            {
                EditorSceneManager.playModeStartScene = null;
                return;
            }

            SceneAsset introSceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(IntroScenePath);
            if (introSceneAsset != null)
            {
                if (EditorSceneManager.playModeStartScene != introSceneAsset)
                {
                    EditorSceneManager.playModeStartScene = introSceneAsset;
                }
            }
        }

        public static void EnsurePrefabsAndScenesExist(bool silent)
        {
            // 1. 누락된 프리팹이 있거나 수동 실행인 경우 프리팹 생성 및 PrefabDatabase 연결
            bool needsPrefabs = !silent
                || AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_MythOfGuotaji/Resources/Prefabs/Monster_Gimmick_ShieldBandit.prefab") == null
                || AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_MythOfGuotaji/Resources/Prefabs/Outgame_GokdoShrineEnv.prefab") == null
                || AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_MythOfGuotaji/Resources/Prefabs/GUI/IntroOutgameUI.prefab") == null
                || AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_MythOfGuotaji/Resources/Prefabs/GUI/ScreenDamageVignetteUI.prefab") == null;

            if (needsPrefabs)
            {
                MythOfGuotajiPrefabAndTableGenerator.GenerateMissingPrefabsAndLinkDatabase();
            }

            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }

            // 2. Intro.unity / PlayMain.unity 생성 또는 보강
            bool introMissing = !File.Exists(IntroScenePath) || AssetDatabase.LoadAssetAtPath<SceneAsset>(IntroScenePath) == null;
            bool playMainMissing = !File.Exists(PlayMainScenePath) || AssetDatabase.LoadAssetAtPath<SceneAsset>(PlayMainScenePath) == null;

            if (!silent || needsPrefabs || introMissing || playMainMissing)
            {
                BuildOrUpdateScenes(!silent || needsPrefabs);
            }

            // 3. EditorBuildSettings에 Intro, PlayMain, SampleScene 등록
            EnsureBuildSettingsScenes();
        }

        public static void BuildOrUpdateScenes(bool forceRebuild)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            // 현재 열려있는 씬 경로 백업
            string previousScenePath = SceneManager.GetActiveScene().path;
            if (!string.IsNullOrEmpty(previousScenePath) && SceneManager.GetActiveScene().isDirty)
            {
                EditorSceneManager.SaveOpenScenes();
            }

            PrefabDatabaseSO db = Resources.Load<PrefabDatabaseSO>("Data/PrefabDatabase");

            if (forceRebuild || !File.Exists(IntroScenePath))
            {
                CreateIntroScene(db);
            }

            if (forceRebuild || !File.Exists(PlayMainScenePath))
            {
                CreatePlayMainScene(db);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 원래 열려있던 씬 복원 (없으면 Intro.unity 오픈)
            string sceneToRestore = !string.IsNullOrEmpty(previousScenePath) && File.Exists(previousScenePath)
                ? previousScenePath
                : IntroScenePath;

            if (File.Exists(sceneToRestore) && SceneManager.GetActiveScene().path != sceneToRestore)
            {
                EditorSceneManager.OpenScene(sceneToRestore, OpenSceneMode.Single);
            }
        }

        private static void CreateIntroScene(PrefabDatabaseSO db)
        {
            Scene introScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // 카메라 세팅 (곡도 사당 우물가를 내려다보는 구도)
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.transform.position = new Vector3(0f, 7.2f, -9.8f);
                mainCam.transform.rotation = Quaternion.Euler(26f, 0f, 0f);
                mainCam.clearFlags = CameraClearFlags.SolidColor;
                mainCam.backgroundColor = new Color(0.04f, 0.07f, 0.13f, 1f);
            }

            // 라이트 세팅
            Light dirLight = Object.FindAnyObjectByType<Light>();
            if (dirLight != null)
            {
                dirLight.transform.rotation = Quaternion.Euler(42f, -30f, 0f);
                dirLight.color = new Color(0.88f, 0.92f, 1.0f, 1f);
                dirLight.intensity = 1.15f;
            }

            // 곡도 사당 3D 배경 프리팹 배치
            GameObject shrinePrefab = db != null && db.outgameShrineEnvPrefab != null
                ? db.outgameShrineEnvPrefab
                : Resources.Load<GameObject>("Prefabs/Outgame_GokdoShrineEnv");
            if (shrinePrefab != null)
            {
                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(shrinePrefab, introScene);
                inst.name = "Outgame_GokdoShrineEnv";
            }

            // 데이터 및 게임 매니저 프리팹/오브젝트 배치
            GameObject sysRoot = new GameObject("OutgameSystems");
            sysRoot.AddComponent<GameDataManager>();
            sysRoot.AddComponent<GameManager>();

            // 아웃게임 UI 프리팹 배치
            GameObject introUiPrefab = db != null && db.introOutgameUIPrefab != null
                ? db.introOutgameUIPrefab
                : Resources.Load<GameObject>("Prefabs/GUI/IntroOutgameUI");
            if (introUiPrefab != null)
            {
                GameObject uiInst = (GameObject)PrefabUtility.InstantiatePrefab(introUiPrefab, introScene);
                uiInst.name = "IntroOutgameUI";
            }
            else
            {
                GameObject uiFallback = new GameObject("IntroOutgameUI");
                uiFallback.AddComponent<IntroOutgameManager>();
            }

            // EventSystem 배치 (New Input System 호환 InputSystemUIInputModule 사용)
            EnsureSceneEventSystem();

            EditorSceneManager.SaveScene(introScene, IntroScenePath);
        }

        private static void CreatePlayMainScene(PrefabDatabaseSO db)
        {
            Scene playScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. PlayMainSceneController (인게임 씬 총괄 로더)
            GameObject controllerObj = new GameObject("PlayMainSceneController");
            PlayMainSceneController sceneController = controllerObj.AddComponent<PlayMainSceneController>();

            // 2. Directional Light
            GameObject lightObj = new GameObject("Directional Light");
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.96f, 0.9f, 1f);
            light.intensity = 1.2f;
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // 3. IngameSystemManagers 프리팹
            GameObject sysPrefab = db != null && db.ingameSystemManagersPrefab != null
                ? db.ingameSystemManagersPrefab
                : Resources.Load<GameObject>("Prefabs/IngameSystemManagers");
            GameObject sysInst = null;
            if (sysPrefab != null)
            {
                sysInst = (GameObject)PrefabUtility.InstantiatePrefab(sysPrefab, playScene);
                sysInst.name = "IngameSystemManagers";
            }

            // 4. MazeGenerator 오브젝트
            GameObject mazeObj = new GameObject("MazeGenerator");
            MazeGenerator mazeGen = mazeObj.AddComponent<MazeGenerator>();

            // 5. Main Camera 프리팹
            GameObject camPrefab = db != null && db.mainCameraPrefab != null
                ? db.mainCameraPrefab
                : Resources.Load<GameObject>("Prefabs/Main Camera");
            MythOfGuotaji.Cameras.QuarterViewCamera qvCam = null;
            if (camPrefab != null)
            {
                GameObject camInst = (GameObject)PrefabUtility.InstantiatePrefab(camPrefab, playScene);
                camInst.name = "Main Camera";
                qvCam = camInst.GetComponent<MythOfGuotaji.Cameras.QuarterViewCamera>();
            }

            // 6. Player_Guotaji 프리팹
            GameObject playerPrefab = db != null && db.playerPrefab != null
                ? db.playerPrefab
                : Resources.Load<GameObject>("Prefabs/Player_Guotaji");
            MythOfGuotaji.Actor.PlayerController playerCtrl = null;
            if (playerPrefab != null)
            {
                GameObject playerInst = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, playScene);
                playerInst.name = "Player_Guotaji";
                playerInst.transform.position = new Vector3(0f, 1f, 0f);
                playerCtrl = playerInst.GetComponent<MythOfGuotaji.Actor.PlayerController>();
            }

            // 7. Helper_SpiritRabbit 프리팹
            GameObject helperPrefab = db != null && db.helperCompanionPrefab != null
                ? db.helperCompanionPrefab
                : Resources.Load<GameObject>("Prefabs/Helper_SpiritRabbit");
            MythOfGuotaji.Actor.HelperCompanion helperCtrl = null;
            if (helperPrefab != null)
            {
                GameObject helperInst = (GameObject)PrefabUtility.InstantiatePrefab(helperPrefab, playScene);
                helperInst.name = "Helper_SpiritRabbit";
                helperInst.transform.position = new Vector3(-1.5f, 1f, -1.2f);
                helperCtrl = helperInst.GetComponent<MythOfGuotaji.Actor.HelperCompanion>();
            }

            // 8. GUI 프리팹들 배치
            GameObject guiRoot = new GameObject("--- Ingame GUI Prefabs ---");
            GameObject hpUiObj = InstantiateGuiPrefabInScene(playScene, guiRoot.transform, db != null ? db.playerHealthBarUIPrefab : null, "Prefabs/GUI/PlayerHealthBarUI", "PlayerHealthBarUI");
            GameObject boonUiObj = InstantiateGuiPrefabInScene(playScene, guiRoot.transform, db != null ? db.boonSelectionUIPrefab : null, "Prefabs/GUI/BoonSelectionUI", "BoonSelectionUI");
            GameObject bossUiObj = InstantiateGuiPrefabInScene(playScene, guiRoot.transform, db != null ? db.bossHealthBarUIPrefab : null, "Prefabs/GUI/BossHealthBarUI", "BossHealthBarUI");
            GameObject monHpObj = InstantiateGuiPrefabInScene(playScene, guiRoot.transform, db != null ? db.monsterHealthBarManagerPrefab : null, "Prefabs/GUI/MonsterHealthBarManager", "MonsterHealthBarManager");
            GameObject clearUiObj = InstantiateGuiPrefabInScene(playScene, guiRoot.transform, db != null ? db.stageClearUIPrefab : null, "Prefabs/GUI/StageClearUI", "StageClearUI");
            GameObject debugUiObj = InstantiateGuiPrefabInScene(playScene, guiRoot.transform, db != null ? db.progressionDebugUIPrefab : null, "Prefabs/GUI/ProgressionDebugUI", "ProgressionDebugUI");
            GameObject vigUiObj = InstantiateGuiPrefabInScene(playScene, guiRoot.transform, db != null ? db.screenDamageVignetteUIPrefab : null, "Prefabs/GUI/ScreenDamageVignetteUI", "ScreenDamageVignetteUI");
            GameObject aimUiObj = InstantiateGuiPrefabInScene(playScene, guiRoot.transform, db != null ? db.aimCursorManagerPrefab : null, "Prefabs/GUI/AimCursorManager", "AimCursorManager");

            sceneController.BindSceneReferences(
                playerCtrl,
                helperCtrl,
                qvCam,
                mazeGen,
                sysInst != null ? sysInst.GetComponent<MythOfGuotaji.Actor.HordeSpawner>() : null,
                sysInst != null ? sysInst.GetComponent<DungeonFogManager>() : null,
                hpUiObj != null ? hpUiObj.GetComponent<PlayerHealthBarUI>() : null,
                boonUiObj != null ? boonUiObj.GetComponent<BoonSelectionUI>() : null,
                bossUiObj != null ? bossUiObj.GetComponent<BossHealthBarUI>() : null,
                monHpObj != null ? monHpObj.GetComponent<MonsterHealthBarManager>() : null,
                clearUiObj != null ? clearUiObj.GetComponent<StageClearUI>() : null,
                debugUiObj != null ? debugUiObj.GetComponent<ProgressionDebugUI>() : null,
                vigUiObj != null ? vigUiObj.GetComponent<ScreenDamageVignetteUI>() : null,
                aimUiObj != null ? aimUiObj.GetComponent<AimCursorManager>() : null
            );

            // 9. EventSystem 배치 (New Input System 호환 InputSystemUIInputModule 사용)
            EnsureSceneEventSystem();

            EditorSceneManager.SaveScene(playScene, PlayMainScenePath);
        }

        private static void EnsureSceneEventSystem()
        {
            var es = Object.FindAnyObjectByType<EventSystem>();
            if (es == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<EventSystem>();
                esObj.AddComponent<InputSystemUIInputModule>();
            }
            else
            {
                var standalone = es.GetComponent<StandaloneInputModule>();
                if (standalone != null)
                {
                    Object.DestroyImmediate(standalone);
                }
                if (es.GetComponent<InputSystemUIInputModule>() == null)
                {
                    es.gameObject.AddComponent<InputSystemUIInputModule>();
                }
            }
        }

        private static GameObject InstantiateGuiPrefabInScene(Scene scene, Transform parent, GameObject prefabRef, string resourcePath, string name)
        {
            GameObject p = prefabRef != null ? prefabRef : Resources.Load<GameObject>(resourcePath);
            if (p != null)
            {
                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(p, scene);
                inst.name = name;
                inst.transform.SetParent(parent, false);
                return inst;
            }
            return null;
        }

        public static void EnsureBuildSettingsScenes()
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();

            if (File.Exists(IntroScenePath))
                scenes.Add(new EditorBuildSettingsScene(IntroScenePath, true));
            if (File.Exists(PlayMainScenePath))
                scenes.Add(new EditorBuildSettingsScene(PlayMainScenePath, true));
            if (File.Exists(SampleScenePath))
                scenes.Add(new EditorBuildSettingsScene(SampleScenePath, true));

            // 기존에 등록된 다른 씬이 있다면 유지
            foreach (var existing in EditorBuildSettings.scenes)
            {
                if (existing == null || string.IsNullOrEmpty(existing.path)) continue;
                bool alreadyAdded = false;
                for (int i = 0; i < scenes.Count; i++)
                {
                    if (scenes[i].path == existing.path)
                    {
                        alreadyAdded = true;
                        break;
                    }
                }
                if (!alreadyAdded && File.Exists(existing.path))
                {
                    scenes.Add(existing);
                }
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
#endif
