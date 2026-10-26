using MythOfGuotaji.Actor;
using MythOfGuotaji.Cameras;
using MythOfGuotaji.Combat;
using MythOfGuotaji.Data;
using MythOfGuotaji.Dungeon;
using MythOfGuotaji.Progression;
using MythOfGuotaji.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace MythOfGuotaji.Core
{
    /// <summary>
    /// 인게임 씬(PlayMain.unity) 전용 씬 컨트롤러
    /// - 기존의 RuntimeInitializeOnLoadMethod 기반 DungeonBootstrapper를 대체하여,
    ///   PlayMain.scene 하이어라키에 배치된 프리팹/매니저들을 기반으로 모든 인게임 데이터를 로딩 및 초기화합니다.
    /// - 아웃게임(Intro.scene: 곡도 사당 거점)에서 선택한 스테이지, 기억의 우물 강화 수치, 재도전 가호, 핵심 기억을
    ///   거타지(PlayerController)와 던전 생성기(MazeGenerator)에 적용합니다.
    /// </summary>
    public class PlayMainSceneController : MonoBehaviour
    {
        public static PlayMainSceneController Instance { get; private set; }

        [Header("Scene Actor References")]
        [SerializeField] private PlayerController _player;
        [SerializeField] private HelperCompanion _helper;
        [SerializeField] private QuarterViewCamera _mainCamera;

        [Header("Scene Dungeon & System References")]
        [SerializeField] private MazeGenerator _mazeGenerator;
        [SerializeField] private HordeSpawner _hordeSpawner;
        [SerializeField] private DungeonFogManager _dungeonFogManager;

        [Header("Scene GUI References")]
        [SerializeField] private PlayerHealthBarUI _playerHealthBarUI;
        [SerializeField] private BoonSelectionUI _boonSelectionUI;
        [SerializeField] private BossHealthBarUI _bossHealthBarUI;
        [SerializeField] private MonsterHealthBarManager _monsterHealthBarManager;
        [SerializeField] private StageClearUI _stageClearUI;
        [SerializeField] private ProgressionDebugUI _progressionDebugUI;
        [SerializeField] private ScreenDamageVignetteUI _screenDamageVignetteUI;
        [SerializeField] private AimCursorManager _aimCursorManager;

        public void BindSceneReferences(
            PlayerController player,
            HelperCompanion helper,
            QuarterViewCamera mainCamera,
            MazeGenerator mazeGenerator,
            HordeSpawner hordeSpawner,
            DungeonFogManager dungeonFogManager,
            PlayerHealthBarUI playerHealthBarUI,
            BoonSelectionUI boonSelectionUI,
            BossHealthBarUI bossHealthBarUI,
            MonsterHealthBarManager monsterHealthBarManager,
            StageClearUI stageClearUI,
            ProgressionDebugUI progressionDebugUI,
            ScreenDamageVignetteUI screenDamageVignetteUI,
            AimCursorManager aimCursorManager)
        {
            _player = player;
            _helper = helper;
            _mainCamera = mainCamera;
            _mazeGenerator = mazeGenerator;
            _hordeSpawner = hordeSpawner;
            _dungeonFogManager = dungeonFogManager;
            _playerHealthBarUI = playerHealthBarUI;
            _boonSelectionUI = boonSelectionUI;
            _bossHealthBarUI = bossHealthBarUI;
            _monsterHealthBarManager = monsterHealthBarManager;
            _stageClearUI = stageClearUI;
            _progressionDebugUI = progressionDebugUI;
            _screenDamageVignetteUI = screenDamageVignetteUI;
            _aimCursorManager = aimCursorManager;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Time.timeScale = 1f;

            InitializeSceneSystemsFromPrefabs();
        }

        private void Start()
        {
            ApplyOutgameShrineUpgradesToPlayer();
            if (_aimCursorManager != null)
            {
                _aimCursorManager.ApplyAimCursor();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void InitializeSceneSystemsFromPrefabs()
        {
            // 1. 시스템 매니저 프리팹 확인 및 바인딩
            EnsureSystemManagers();

            var db = GameDataManager.Instance != null ? GameDataManager.Instance.Prefabs : Resources.Load<PrefabDatabaseSO>("Data/PrefabDatabase");

            // 2. 플레이어 (Player_Guotaji.prefab)
            if (_player == null) _player = FindAnyObjectByType<PlayerController>();
            if (_player == null)
            {
                GameObject playerPrefab = db != null
                    ? PrefabDatabaseSO.ResolvePrefab(ref db.playerPrefab, "Prefabs/Player_Guotaji")
                    : Resources.Load<GameObject>("Prefabs/Player_Guotaji");

                if (playerPrefab != null)
                {
                    var playerObj = Instantiate(playerPrefab, new Vector3(0f, 1f, 0f), Quaternion.identity);
                    playerObj.name = "Player_Guotaji";
                    _player = playerObj.GetComponent<PlayerController>();
                }
            }

            // 3. 동료 헬퍼 보옥 (Helper_SpiritRabbit.prefab)
            if (_helper == null) _helper = FindAnyObjectByType<HelperCompanion>();
            if (_helper == null && HelperManager.Instance != null)
            {
                _helper = HelperManager.Instance.EnsureHelperSpawned();
            }

            // 4. 메인 쿼터뷰 카메라 (MainCamera.prefab)
            if (_mainCamera == null) _mainCamera = FindAnyObjectByType<QuarterViewCamera>();
            if (_mainCamera == null)
            {
                GameObject camPrefab = db != null
                    ? PrefabDatabaseSO.ResolvePrefab(ref db.mainCameraPrefab, "Prefabs/MainCamera")
                    : Resources.Load<GameObject>("Prefabs/MainCamera");

                Camera existingCam = Camera.main;
                if (existingCam != null)
                {
                    _mainCamera = existingCam.GetComponent<QuarterViewCamera>();
                    if (_mainCamera == null) _mainCamera = existingCam.gameObject.AddComponent<QuarterViewCamera>();
                    if (camPrefab != null)
                    {
                        _mainCamera.ApplyPrefabSettings(camPrefab.GetComponent<QuarterViewCamera>(), camPrefab.GetComponent<Camera>());
                    }
                }
                else if (camPrefab != null)
                {
                    var camObj = Instantiate(camPrefab);
                    camObj.name = "Main Camera";
                    camObj.tag = "MainCamera";
                    _mainCamera = camObj.GetComponent<QuarterViewCamera>();
                }
            }

            if (_mainCamera != null && _player != null)
            {
                _mainCamera.SetTarget(_player.transform);
            }

            // 5. 던전 미로 생성기
            if (_mazeGenerator == null) _mazeGenerator = FindAnyObjectByType<MazeGenerator>();
            if (_mazeGenerator == null)
            {
                var mazeGo = new GameObject("[MazeGenerator]");
                _mazeGenerator = mazeGo.AddComponent<MazeGenerator>();
            }

            // 6. 인게임 UI 프리팹 확인 및 로드
            EnsureEventSystem();
            EnsureGUIPrefabs(db);

            Debug.Log("<color=lime><b>[PlayMainSceneController]</b> PlayMain.scene 기반 모든 프리팹 및 데이터 로딩이 완료되었습니다!</color>");
        }

        private void EnsureSystemManagers()
        {
            if (FindAnyObjectByType<GameDataManager>() == null ||
                FindAnyObjectByType<GameManager>() == null ||
                FindAnyObjectByType<ObjectPoolManager>() == null ||
                FindAnyObjectByType<HordeSpawner>() == null ||
                FindAnyObjectByType<DungeonFogManager>() == null ||
                FindAnyObjectByType<HelperManager>() == null ||
                FindAnyObjectByType<ScenarioTreeManager>() == null ||
                FindAnyObjectByType<CharacterTreeManager>() == null ||
                FindAnyObjectByType<InRunProgressionManager>() == null)
            {
                var sysPrefab = Resources.Load<GameObject>("Prefabs/GUI/IngameSystemManagers");
                if (sysPrefab != null && GameObject.Find("IngameSystemManagers") == null)
                {
                    var sysInstance = Instantiate(sysPrefab);
                    sysInstance.name = "IngameSystemManagers";
                }
            }

            if (FindAnyObjectByType<GameDataManager>() == null)
            {
                new GameObject("[GameDataManager]").AddComponent<GameDataManager>();
            }
            if (FindAnyObjectByType<GameManager>() == null)
            {
                new GameObject("[GameManager]").AddComponent<GameManager>();
            }
            if (FindAnyObjectByType<ObjectPoolManager>() == null)
            {
                new GameObject("[ObjectPoolManager]").AddComponent<ObjectPoolManager>();
            }
            if (_hordeSpawner == null) _hordeSpawner = FindAnyObjectByType<HordeSpawner>();
            if (_hordeSpawner == null)
            {
                _hordeSpawner = new GameObject("[HordeSpawner]").AddComponent<HordeSpawner>();
            }
            if (_dungeonFogManager == null) _dungeonFogManager = FindAnyObjectByType<DungeonFogManager>();
            if (_dungeonFogManager == null)
            {
                _dungeonFogManager = new GameObject("[DungeonFogManager]").AddComponent<DungeonFogManager>();
            }
            if (FindAnyObjectByType<HelperManager>() == null)
            {
                new GameObject("[HelperManager]").AddComponent<HelperManager>();
            }
            if (FindAnyObjectByType<ScenarioTreeManager>() == null)
            {
                new GameObject("[ScenarioTreeManager]").AddComponent<ScenarioTreeManager>();
            }
            if (FindAnyObjectByType<CharacterTreeManager>() == null)
            {
                new GameObject("[CharacterTreeManager]").AddComponent<CharacterTreeManager>();
            }
            if (FindAnyObjectByType<InRunProgressionManager>() == null)
            {
                new GameObject("[InRunProgressionManager]").AddComponent<InRunProgressionManager>();
            }
        }

        private void EnsureGUIPrefabs(PrefabDatabaseSO db)
        {
            if (_playerHealthBarUI == null) _playerHealthBarUI = FindAnyObjectByType<PlayerHealthBarUI>();
            if (_playerHealthBarUI == null)
            {
                PlayerHealthBarUI.EnsureInstance();
                _playerHealthBarUI = PlayerHealthBarUI.Instance;
            }

            if (_boonSelectionUI == null) _boonSelectionUI = FindAnyObjectByType<BoonSelectionUI>();
            if (_boonSelectionUI == null)
            {
                BoonSelectionUI.EnsureInstance();
                _boonSelectionUI = BoonSelectionUI.Instance;
            }

            if (_bossHealthBarUI == null) _bossHealthBarUI = FindAnyObjectByType<BossHealthBarUI>();
            if (_bossHealthBarUI == null)
            {
                var prefab = db != null
                    ? PrefabDatabaseSO.ResolvePrefab(ref db.bossHealthBarUIPrefab, "Prefabs/GUI/BossHealthBarUI")
                    : Resources.Load<GameObject>("Prefabs/GUI/BossHealthBarUI");
                var go = prefab != null ? Instantiate(prefab) : new GameObject("BossHealthBarUI", typeof(BossHealthBarUI));
                go.name = "BossHealthBarUI";
                _bossHealthBarUI = go.GetComponent<BossHealthBarUI>();
            }

            if (_monsterHealthBarManager == null) _monsterHealthBarManager = FindAnyObjectByType<MonsterHealthBarManager>();
            if (_monsterHealthBarManager == null)
            {
                var prefab = db != null
                    ? PrefabDatabaseSO.ResolvePrefab(ref db.monsterHealthBarManagerPrefab, "Prefabs/GUI/MonsterHealthBarManager")
                    : Resources.Load<GameObject>("Prefabs/GUI/MonsterHealthBarManager");
                var go = prefab != null ? Instantiate(prefab) : new GameObject("MonsterHealthBarManager", typeof(MonsterHealthBarManager));
                go.name = "MonsterHealthBarManager";
                _monsterHealthBarManager = go.GetComponent<MonsterHealthBarManager>();
            }

            if (_stageClearUI == null) _stageClearUI = FindAnyObjectByType<StageClearUI>();
            if (_stageClearUI == null)
            {
                var prefab = db != null
                    ? PrefabDatabaseSO.ResolvePrefab(ref db.stageClearUIPrefab, "Prefabs/GUI/StageClearUI")
                    : Resources.Load<GameObject>("Prefabs/GUI/StageClearUI");
                var go = prefab != null ? Instantiate(prefab) : new GameObject("StageClearUI", typeof(StageClearUI));
                go.name = "StageClearUI";
                _stageClearUI = go.GetComponent<StageClearUI>();
            }

            if (_progressionDebugUI == null) _progressionDebugUI = FindAnyObjectByType<ProgressionDebugUI>();
            if (_progressionDebugUI == null)
            {
                var prefab = db != null
                    ? PrefabDatabaseSO.ResolvePrefab(ref db.progressionDebugUIPrefab, "Prefabs/GUI/ProgressionDebugUI")
                    : Resources.Load<GameObject>("Prefabs/GUI/ProgressionDebugUI");
                var go = prefab != null ? Instantiate(prefab) : new GameObject("ProgressionDebugUI", typeof(ProgressionDebugUI));
                go.name = "ProgressionDebugUI";
                _progressionDebugUI = go.GetComponent<ProgressionDebugUI>();
            }

            if (_screenDamageVignetteUI == null) _screenDamageVignetteUI = FindAnyObjectByType<ScreenDamageVignetteUI>();
            if (_screenDamageVignetteUI == null)
            {
                _screenDamageVignetteUI = ScreenDamageVignetteUI.Instance;
            }

            if (_aimCursorManager == null) _aimCursorManager = FindAnyObjectByType<AimCursorManager>();
            if (_aimCursorManager == null)
            {
                var prefab = db != null
                    ? PrefabDatabaseSO.ResolvePrefab(ref db.aimCursorManagerPrefab, "Prefabs/GUI/AimCursorManager")
                    : Resources.Load<GameObject>("Prefabs/GUI/AimCursorManager");
                var go = prefab != null ? Instantiate(prefab) : new GameObject("AimCursorManager", typeof(AimCursorManager));
                go.name = "AimCursorManager";
                _aimCursorManager = go.GetComponent<AimCursorManager>();
            }
        }

        private static void EnsureEventSystem()
        {
            var es = FindAnyObjectByType<EventSystem>();
            if (es == null)
            {
                var esGo = new GameObject("EventSystem");
                es = esGo.AddComponent<EventSystem>();
                esGo.AddComponent<InputSystemUIInputModule>();
            }
            else
            {
                var standalone = es.GetComponent<StandaloneInputModule>();
                if (standalone != null)
                {
                    standalone.enabled = false;
                    DestroyImmediate(standalone);
                }
                if (es.GetComponent<InputSystemUIInputModule>() == null)
                {
                    es.gameObject.AddComponent<InputSystemUIInputModule>();
                }
            }
        }

        /// <summary>
        /// 아웃게임(곡도 사당 거점 - 기억의 우물 & 핵심 기억 & 재도전 가호)의 영구 강화 수치를 인게임 캐릭터에 적용
        /// </summary>
        private void ApplyOutgameShrineUpgradesToPlayer()
        {
            if (_player == null) _player = FindAnyObjectByType<PlayerController>();
            if (_player == null || GameManager.Instance == null || GameManager.Instance.SaveData == null) return;

            var save = GameManager.Instance.SaveData;
            var bow = _player.GetComponentInChildren<BowWeapon>();
            var dagger = _player.GetComponentInChildren<DaggerWeapon>();

            // 1. 활 계열 강화 (레벨당 화살 기본 피해 +4)
            if (save.wellBowLevel > 0 && bow != null)
            {
                bow.AddBaseDamage(save.wellBowLevel * 4f);
            }

            // 2. 단검 계열 강화 (레벨당 단검 피해 +6, 반사 배율 +0.15)
            if (save.wellDaggerLevel > 0 && dagger != null)
            {
                dagger.AddBaseDamage(save.wellDaggerLevel * 6f);
                dagger.AddReflectDamageMultiplier(save.wellDaggerLevel * 0.15f);
            }

            // 3. 보옥 계열 강화 (회복 하트 추가 치유량)
            if (save.wellBookLevel > 0)
            {
                RecoveryHeart.BonusHealAmount = save.wellBookLevel * 4f;
            }

            // 4. 생존 계열 강화 + 재도전 가호 (최대 체력 보너스)
            float bonusMaxHp = (save.wellSurvivalLevel * 15f) + (save.retryBlessingStacks * 10f);
            if (bonusMaxHp > 0f)
            {
                var bonusBoon = ScriptableObject.CreateInstance<InRunBoonSO>();
                bonusBoon.boonTitle = "곡도 사당의 가호";
                bonusBoon.bonusHp = bonusMaxHp;
                bonusBoon.dashCooldownReduction = save.wellSurvivalLevel * 0.08f;
                _player.ApplyBoon(bonusBoon);
            }

            Debug.Log($"<color=cyan>[PlayMainSceneController] 곡도 사당 영구 강화 적용 완료 — 출격 스테이지: {save.selectedStageCode} (활 Lv.{save.wellBowLevel}, 단검 Lv.{save.wellDaggerLevel}, 보옥 Lv.{save.wellBookLevel}, 생존 Lv.{save.wellSurvivalLevel}, 재도전 가호 {save.retryBlessingStacks}단계)</color>");
        }
    }
}
