using System.Collections.Generic;
using MythOfGuotaji.Actor;
using MythOfGuotaji.Cameras;
using MythOfGuotaji.Core;
using MythOfGuotaji.Data;
using MythOfGuotaji.UI;
using MythOfGuotaji.UI.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MythOfGuotaji.Test
{
    /// <summary>
    /// MonsterTest.scene 전용 테스트 컨트롤러 (UGUI + TMP)
    /// - 씬에 이미 배치된 배경(0,0,0: 플레이어 스폰방 / 32,0,0: 몬스터 배치방) 활용
    /// - 게임 본편과 동일한 쿼터뷰 카메라(QuarterViewCamera 추적 및 Lookahead) 지원
    /// - 플레이어 HP 바(PlayerHealthBarUI), 차징 게이지(ChargeIndicatorUI), 몬스터 HP 바(MonsterHealthBarManager) 실시간 연동
    /// </summary>
    public class MonsterTestController : MonoBehaviour
    {
        public static MonsterTestController Instance { get; private set; }

        public const float RoomSpacing = 32f;
        public static readonly Vector3 LeftRoomCenter = new Vector3(0f, 0f, 0f);
        public static readonly Vector3 RightRoomCenter = new Vector3(32f, 0f, 0f);

        [Header("Room & Environment References")]
        [SerializeField] private Transform _leftRoomAnchor;
        [SerializeField] private Transform _rightRoomAnchor;

        [Header("Testing State")]
        [SerializeField] private bool _isTesting = false;
        private PlayerController _playerInstance;
        private readonly List<MonsterAI> _spawnedMonsters = new();

        [Header("Testing Weapons & GodMode")]
        [SerializeField] private string _selectedBowId = "Bow_00";
        [SerializeField] private string _selectedDaggerId = "Dagger_00";
        [SerializeField] private bool _isGodMode = false;

        [Header("Placement State")]
        private MonsterType? _selectedMonsterType = null;
        private GameObject _previewInstance = null;

        private struct MonsterInfo
        {
            public MonsterType type;
            public string korName;
            public string category;

            public MonsterInfo(MonsterType type, string korName, string category)
            {
                this.type = type;
                this.korName = korName;
                this.category = category;
            }
        }

        private static readonly MonsterInfo[] AllMonsterInfos = new MonsterInfo[]
        {
            new(MonsterType.Minion, "일반 요괴 (근접 돌진)", "근접 돌진"),
            new(MonsterType.Minion_Archer, "원거리 궁병 (원거리 사격)", "원거리 사격"),
            new(MonsterType.Minion_BigMon, "중형 요괴 (충격파 강타)", "충격파 강타"),
            new(MonsterType.Minion_Zhu, "곡사포 주 (원거리 곡사)", "원거리 곡사"),
            new(MonsterType.Elite, "엘리트 도깨비 (포효/돌진)", "엘리트"),
            new(MonsterType.FoxBoss, "보스 구미호 (진공파/휩쓸기)", "보스"),
            new(MonsterType.Dokkaebi_Fire, "도깨비불 (유도 화염구)", "기믹-투사체"),
            new(MonsterType.Boar_Charger, "멧돼지 돌격대 (저돌맹진)", "기믹-돌진"),
            new(MonsterType.Spider_Tether, "거미 요괴 (독 거미줄 결속)", "기믹-속박"),
            new(MonsterType.Mud_Slime, "진흙 슬라임 (감속 장판)", "기믹-장판"),
            new(MonsterType.Phantom_Fox, "환영 여우 (분신술/은신)", "기믹-은신분신"),
            new(MonsterType.Iron_Shield, "철갑 방패 요괴 (전방 가드)", "기믹-방패"),
            new(MonsterType.Energy_Absorber, "기 흡수기 (공격 반사)", "기믹-반사"),
            new(MonsterType.Bomb_Sparrow, "자폭 참새 떼 (초고속 물량)", "기믹-자폭"),
            new(MonsterType.Totem_Turret, "사방신 토템 (강화 고정 포탑)", "기믹-포탑"),
        };

        // UGUI Elements
        private Canvas _canvas;
        private TextMeshProUGUI _testToggleLabel;
        private Image _testToggleBg;
        private RectTransform _leftListPanel;
        private TextMeshProUGUI _overlayStatusLabel;

        // Camera Reference
        private QuarterViewCamera _qvCamera;

        private void Awake()
        {
            Instance = this;
            Time.timeScale = 1f;

            EnsureSystemDependencies();
            FindSceneExistingRooms();
            SetupCameraSystem();
            EnsureGameGUIs();
            BuildUGUI();
        }

        private void Start()
        {
            SetupCameraForPlacement();
        }

        /// <summary>
        /// Scene에 이미 설치된 배경(0,0,0 / 32,0,0) 바인딩
        /// </summary>
        private void FindSceneExistingRooms()
        {
            var allTransforms = FindObjectsByType<Transform>(FindObjectsSortMode.None);
            foreach (var t in allTransforms)
            {
                if (t.name.Contains("DungeonRoom_Base") || t.name.Contains("Room"))
                {
                    if (Vector3.Distance(t.position, LeftRoomCenter) < 8f && _leftRoomAnchor == null)
                    {
                        _leftRoomAnchor = t;
                    }
                    else if (Vector3.Distance(t.position, RightRoomCenter) < 8f && _rightRoomAnchor == null)
                    {
                        _rightRoomAnchor = t;
                    }
                }
            }
        }

        /// <summary>
        /// 게임 본편과 동일한 QuarterViewCamera 보장
        /// </summary>
        private void SetupCameraSystem()
        {
            var mainCam = Camera.main;
            if (mainCam == null)
            {
                var camObj = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                camObj.tag = "MainCamera";
                mainCam = camObj.GetComponent<Camera>();
            }

            _qvCamera = mainCam.GetComponent<QuarterViewCamera>();
            if (_qvCamera == null)
            {
                _qvCamera = mainCam.gameObject.AddComponent<QuarterViewCamera>();
            }

            // MainCamera 프리팹 설정값 동기화
            var camPrefab = Resources.Load<GameObject>("Prefabs/MainCamera");
            if (camPrefab != null)
            {
                var prefabQv = camPrefab.GetComponent<QuarterViewCamera>();
                var prefabCam = camPrefab.GetComponent<Camera>();
                if (prefabQv != null && prefabCam != null)
                {
                    _qvCamera.ApplyPrefabSettings(prefabQv, prefabCam);
                }
            }
        }

        private void SetupCameraForPlacement()
        {
            if (_qvCamera != null)
            {
                _qvCamera.SetTarget(null);
                _qvCamera.enabled = false;
            }

            var cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = RightRoomCenter + new Vector3(0f, 24f, -15f);
                cam.transform.rotation = Quaternion.Euler(53f, 0f, 0f);
            }
        }

        /// <summary>
        /// 게임 본편과 동일한 인게임 UI(플레이어 HP 바, 몬스터 체력바, 차징 인디케이터 등) 보장
        /// </summary>
        private void EnsureGameGUIs()
        {
            // 1. 플레이어 체력/기력 바
            if (FindAnyObjectByType<PlayerHealthBarUI>() == null)
            {
                PlayerHealthBarUI.EnsureInstance();
            }

            // 2. 몬스터 머리 위 체력바 매니저
            if (FindAnyObjectByType<MonsterHealthBarManager>() == null)
            {
                var mhbObj = new GameObject("[MonsterHealthBarManager]");
                mhbObj.AddComponent<MonsterHealthBarManager>();
            }

            // 3. 차징 인디케이터
            if (FindAnyObjectByType<ChargeIndicatorUI>() == null)
            {
                var ciObj = new GameObject("[ChargeIndicatorUI]");
                ciObj.AddComponent<ChargeIndicatorUI>();
            }

            // 4. 화면 피격 비네트
            if (FindAnyObjectByType<ScreenDamageVignetteUI>() == null)
            {
                var sdvPrefab = Resources.Load<GameObject>("Prefabs/GUI/ScreenDamageVignetteUI");
                if (sdvPrefab != null) Instantiate(sdvPrefab);
            }
        }

        private void EnsureSystemDependencies()
        {
            UIEventSystem.Ensure();

            if (GameDataManager.Instance == null)
            {
                var gdmObj = new GameObject("[GameDataManager]");
                gdmObj.AddComponent<GameDataManager>();
            }
            if (GameManager.Instance == null)
            {
                var gmObj = new GameObject("[GameManager]");
                gmObj.AddComponent<GameManager>();
            }
            if (VFXManager.Instance == null)
            {
                var vfxObj = new GameObject("[VFXManager]");
                vfxObj.AddComponent<VFXManager>();
            }
            if (ObjectPoolManager.Instance == null)
            {
                var poolObj = new GameObject("[ObjectPoolManager]");
                poolObj.AddComponent<ObjectPoolManager>();
            }
        }

        private void BuildUGUI()
        {
            UIEventSystem.Ensure();
            if (_canvas != null) return;
            _canvas = UIKit.CreateCanvas("[MonsterTest_Canvas]", UITheme.Sorting.Debug, transform, true);

            // 1. 상단 중앙 시작/종료 토글 버튼
            var toggleBtn = UIKit.CreateButton(_canvas.transform, "ToggleBtn", "▶ 테스트 시작 (Start Test)", OnToggleTest, 16f, new Color(0.2f, 0.75f, 0.3f), Color.white);
            UIKit.SetAnchored(toggleBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(260f, 48f));
            _testToggleBg = toggleBtn.GetComponent<Image>();
            _testToggleLabel = toggleBtn.GetComponentInChildren<TextMeshProUGUI>();

            // 2. 상단 우측 리로드 & 삭제 버튼
            var reloadBtn = UIKit.CreateButton(_canvas.transform, "ReloadBtn", "⚡ 테이블 실시간 리로드", () =>
            {
                GameDataManager.Instance?.ReloadMonsterTables();
            }, 12f, new Color(0.2f, 0.5f, 0.8f), Color.white);
            UIKit.SetAnchored(reloadBtn.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -20f), new Vector2(180f, 36f));

            var clearBtn = UIKit.CreateButton(_canvas.transform, "ClearBtn", "🗑 몬스터 전체 삭제", () =>
            {
                ClearAllSpawnedMonsters();
            }, 12f, new Color(0.8f, 0.35f, 0.2f), Color.white);
            UIKit.SetAnchored(clearBtn.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -62f), new Vector2(180f, 32f));

            // 3. 좌측 몬스터 리스트 패널
            var listPanelImg = UIKit.CreatePanel(_canvas.transform, "MonsterListPanel", new Color(0.08f, 0.08f, 0.12f, 0.9f), false, true);
            _leftListPanel = listPanelImg.rectTransform; 

            UIKit.SetAnchored(_leftListPanel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, 0f), new Vector2(240f, 980f));

            var listTitle = UIKit.CreateLabel(_leftListPanel, "Title", "몬스터 선택 배치", 14f, UITheme.Gold, TextAlignmentOptions.Center, FontStyles.Bold, false, false);
            UIKit.SetAnchored(listTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(220f, 24f));

            var contentRect = UIKit.CreateRect("Content", _leftListPanel);
            UIKit.SetAnchored(contentRect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(10f, -50f), new Vector2(220f, 920f));

            var vlg = contentRect.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 4f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            for (int i = 0; i < AllMonsterInfos.Length; i++)
            {
                var info = AllMonsterInfos[i];
                var mBtn = UIKit.CreateButton(contentRect, "Btn_" + info.type, info.korName, () =>
                {
                    SelectMonsterForPlacement(info.type);
                }, 18f, UITheme.ButtonNormal, Color.white);
                RectTransform btnRect = mBtn.GetComponent<RectTransform>();
                btnRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 50f);
                var le = mBtn.gameObject.AddComponent<LayoutElement>();
                le.preferredHeight = 10f;
                le.minHeight = 10f;
            }

            // 4. 테스트 중 오버레이 라벨
            _overlayStatusLabel = UIKit.CreateLabel(_canvas.transform, "OverlayStatus", "", 13f, Color.white, TextAlignmentOptions.TopLeft, FontStyles.Normal, false, false);
            UIKit.SetAnchored(_overlayStatusLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -20f), new Vector2(320f, 100f));
            _overlayStatusLabel.enabled = false;
        }

        private void Update()
        {
            // ESC로 테스트 중단
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame && _isTesting)
            {
                EndTest();
                UpdateTestButtonVisual();
                return;
            }

            HandlePlacementInput();

            if (_isTesting && _overlayStatusLabel != null && _overlayStatusLabel.enabled)
            {
                int livingMonsters = 0;
                for (int i = 0; i < _spawnedMonsters.Count; i++)
                {
                    if (_spawnedMonsters[i] != null && !_spawnedMonsters[i].IsDead) livingMonsters++;
                }

                float hp = _playerInstance != null ? _playerInstance.CurrentHp : 0f;
                float maxHp = _playerInstance != null ? _playerInstance.MaxHp : 100f;

                _overlayStatusLabel.text =
                    $"<b>전투 테스트 진행 중</b>\n" +
                    $"• 플레이어 HP: {Mathf.CeilToInt(hp)} / {maxHp}\n" +
                    $"• 생존 요괴: {livingMonsters} 마리\n" +
                    $"• [ESC] 또는 상단 버튼으로 테스트 정지";
            }
        }

        private void OnToggleTest()
        {
            if (_isTesting)
            {
                EndTest();
            }
            else
            {
                StartTest();
            }
            UpdateTestButtonVisual();
        }

        private void UpdateTestButtonVisual()
        {
            if (_testToggleBg != null)
            {
                _testToggleBg.color = _isTesting ? new Color(0.85f, 0.25f, 0.25f) : new Color(0.2f, 0.75f, 0.3f);
            }
            if (_testToggleLabel != null)
            {
                _testToggleLabel.text = _isTesting ? "■ 테스트 종료 (End Test)" : "▶ 테스트 시작 (Start Test)";
            }
            if (_leftListPanel != null)
            {
                _leftListPanel.gameObject.SetActive(!_isTesting);
            }
            if (_overlayStatusLabel != null)
            {
                _overlayStatusLabel.enabled = _isTesting;
            }
        }

        private void ClearAllSpawnedMonsters()
        {
            for (int i = 0; i < _spawnedMonsters.Count; i++)
            {
                if (_spawnedMonsters[i] != null)
                {
                    Destroy(_spawnedMonsters[i].gameObject);
                }
            }
            _spawnedMonsters.Clear();
        }

        private void SelectMonsterForPlacement(MonsterType type)
        {
            _selectedMonsterType = type;
            if (_previewInstance != null) Destroy(_previewInstance);

            var monPrefab = ResolveMonsterPrefab(type);
            if (monPrefab != null)
            {
                _previewInstance = Instantiate(monPrefab, RightRoomCenter, Quaternion.identity);
                _previewInstance.name = "Preview_" + type;
                var colliders = _previewInstance.GetComponentsInChildren<Collider>();
                foreach (var col in colliders) col.enabled = false;
                var ai = _previewInstance.GetComponent<MonsterAI>();
                if (ai != null) ai.enabled = false;
            }
        }

        private GameObject ResolveMonsterPrefab(MonsterType type)
        {
            string prefabName = type switch
            {
                MonsterType.Minion => "Monster_Minion",
                MonsterType.Minion_Archer => "Monster_Minion_Archer",
                MonsterType.Minion_BigMon => "Monster_Minion_BigMon",
                MonsterType.Minion_Zhu => "Monster_Minion_Zhu",
                MonsterType.Elite => "Monster_Elite",
                MonsterType.FoxBoss => "Monster_FoxBoss",
                MonsterType.Dokkaebi_Fire => "Monster_Dokkaebi_Fire",
                MonsterType.Boar_Charger => "Monster_Boar_Charger",
                MonsterType.Spider_Tether => "Monster_Spider_Tether",
                MonsterType.Mud_Slime => "Monster_Mud_Slime",
                MonsterType.Phantom_Fox => "Monster_Phantom_Fox",
                MonsterType.Iron_Shield => "Monster_Iron_Shield",
                MonsterType.Energy_Absorber => "Monster_Energy_Absorber",
                MonsterType.Bomb_Sparrow => "Monster_Bomb_Sparrow",
                MonsterType.Totem_Turret => "Monster_Totem_Turret",
                _ => "Monster_Minion"
            };

            var prefab = Resources.Load<GameObject>("Prefabs/" + prefabName);
            if (prefab == null && GameDataManager.Instance != null && GameDataManager.Instance.Prefabs != null)
            {
                prefab = GameDataManager.Instance.Prefabs.GetMonsterPrefab(type);
            }
            return prefab;
        }

        private void HandlePlacementInput()
        {
            if (_isTesting) return;

            var mouse = Mouse.current;
            if (mouse == null) return;

            var cam = Camera.main;
            if (cam == null) return;

            Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

            if (groundPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                if (_previewInstance != null)
                {
                    _previewInstance.transform.position = hitPoint;
                }

                if (mouse.leftButton.wasPressedThisFrame && _selectedMonsterType.HasValue)
                {
                    // UI 클릭 방지
                    if (!UnityEngine.EventSystems.EventSystem.current || !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                    {
                        SpawnMonsterAtPosition(_selectedMonsterType.Value, hitPoint);
                    }
                }
            }

            if (mouse.rightButton.wasPressedThisFrame)
            {
                if (_previewInstance != null)
                {
                    Destroy(_previewInstance);
                    _previewInstance = null;
                    _selectedMonsterType = null;
                }
            }
        }

        private void SpawnMonsterAtPosition(MonsterType type, Vector3 pos)
        {
            var prefab = ResolveMonsterPrefab(type);
            if (prefab == null) return;

            var go = Instantiate(prefab, pos, Quaternion.identity);
            go.name = $"Spawned_{type}_{_spawnedMonsters.Count + 1}";
            var ai = go.GetComponent<MonsterAI>();
            if (ai != null)
            {
                ai.Setup(type, 100f, 4f, 10f, false, "TestPool", -1f, -1f, 0);
                ai.SetSpawnDelay(99999f);
                _spawnedMonsters.Add(ai);
            }
        }

        private void StartTest()
        {
            _isTesting = true;

            if (_previewInstance != null)
            {
                Destroy(_previewInstance);
                _previewInstance = null;
            }

            // 플레이어 생성 (LeftRoomCenter: 0, 0, 0)
            var playerPrefab = Resources.Load<GameObject>("Prefabs/Player_Guotaji");
            if (playerPrefab != null && _playerInstance == null)
            {
                var pObj = Instantiate(playerPrefab, LeftRoomCenter + Vector3.up * 0.5f, Quaternion.identity);
                pObj.name = "Player_Guotaji";
                _playerInstance = pObj.GetComponent<PlayerController>();
            }

            // 배치된 몬스터 활성화 및 어그로 발동
            for (int i = 0; i < _spawnedMonsters.Count; i++)
            {
                if (_spawnedMonsters[i] != null)
                {
                    _spawnedMonsters[i].SetSpawnDelay(0f);
                    _spawnedMonsters[i].TriggerAggro();
                }
            }

            // 게임 본편과 동일하게 카메라가 플레이어를 추적하도록 바인딩
            if (_qvCamera == null && Camera.main != null)
            {
                _qvCamera = Camera.main.GetComponent<QuarterViewCamera>() ?? Camera.main.gameObject.AddComponent<QuarterViewCamera>();
            }

            if (_qvCamera != null && _playerInstance != null)
            {
                _qvCamera.enabled = true;
                _qvCamera.SetTarget(_playerInstance.transform);
            }
        }

        private void EndTest()
        {
            _isTesting = false;

            if (_qvCamera != null)
            {
                _qvCamera.SetTarget(null);
                _qvCamera.enabled = false;
            }

            if (_playerInstance != null)
            {
                Destroy(_playerInstance.gameObject);
                _playerInstance = null;
            }

            SetupCameraForPlacement();
        }
    }
}
