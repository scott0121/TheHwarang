using System.Collections;
using System.Collections.Generic;
using MythOfGuotaji.Core;
using MythOfGuotaji.Data;
using MythOfGuotaji.UI.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MythOfGuotaji.UI
{
    /// <summary>
    /// 인트로(시작 타이틀) 및 런 종료 후 사당(Outgame Hub) 통합 매니저 (UGUI + TMP)
    /// - 타이틀 화면 및 신당 4대 탭(스테이지 선택, 기억의 우물, 기억 아카이브, 무기고)을 완벽한 UGUI로 제공합니다.
    /// - 시나리오 데이터는 ScenarioDataSpecV11b 로 분리되어 데이터 주도 아키텍처를 지원합니다.
    /// </summary>
    public class IntroOutgameManager : UIScreen
    {
        public static IntroOutgameManager Instance { get; private set; }

        public enum OutgameTab
        {
            StageSelect = 0,    // 출격 미궁(스테이지 1~22) 선택
            MemoryWell = 1,     // 기억의 우물 (스탯 영구 강화 4계열)
            MemoryArchive = 2,  // 기억 아카이브 (6대 핵심 기억 특성 장착)
            Armory = 3          // 무기고 (시작 활 & 단검 선택)
        }

        [Header("3D Shrine Scene Environment References")]
        [SerializeField] private GameObject _shrineEnvRoot;
        [SerializeField] private Camera _shrineCamera;
        [SerializeField] private Transform _orbitPivot;

        [Header("State")]
        [SerializeField] private bool _isTitleScreen = true;
        [SerializeField] private OutgameTab _currentTab = OutgameTab.StageSelect;

        private int _selectedChapterFilter = 1;
        private int _selectedStageIndex = 1;
        private int _selectedArchiveIndex = 0;
        private string _activeSpeakerName = "보옥 (보석 토끼)";
        private string _activeDialogueText = "“여긴 안전해요. 우물 안쪽은 제 결계 안이니까요. 출격 준비가 되면 미궁의 문을 여세요.”";

        private List<StageBriefingInfo> _stages = new();
        private List<CoreMemoryTraitInfo> _coreMemories = new();

        // UGUI Elements
        private RectTransform _titleScreenRoot;
        private RectTransform _hubScreenRoot;

        // Hub Components
        private TextMeshProUGUI _hubResourceLabel;
        private TextMeshProUGUI _dialogueSpeakerLabel;
        private TextMeshProUGUI _dialogueContentLabel;
        private RectTransform _tabContentRoot;
        private TextMeshProUGUI _stageDetailLabel;

        protected override int SortingOrder => UITheme.Sorting.Popup;
        protected override bool NeedsRaycaster => true;
        protected override bool NeedsEventSystem => true;
        protected override bool StartVisible => true;

        public void BindSceneReferences(GameObject shrineEnvRoot, Camera shrineCam, Transform orbitPivot)
        {
            _shrineEnvRoot = shrineEnvRoot;
            _shrineCamera = shrineCam;
            _orbitPivot = orbitPivot;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            var data = ScenarioDataSpecV11b.LoadData();
            _stages = data.stages;
            _coreMemories = data.memories;

            EnsureManagersAndEnvironment();
            EnsureBuilt();
        }

        private void Start()
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            if (GameManager.Instance != null && GameManager.Instance.SaveData != null)
            {
                var save = GameManager.Instance.SaveData;
                if (save.returnedFromDeath)
                {
                    _isTitleScreen = false;
                    _activeSpeakerName = "보옥 (보석 토끼)";
                    _activeDialogueText = "“흥, 이번엔 봐줄게요. 다음엔 없어요... 아직 기억이 다 안 돌아왔나 봐요. 기억의 우물을 들여다봐요.”";
                }
            }

            Show();
            UpdateScreenVisibility();
            UpdateHubContent();
        }

        protected override void Build(RectTransform root)
        {
            BuildTitleScreen(root);
            BuildHubScreen(root);
        }

        // =========================================================================
        // 1. 타이틀 화면 (Title Screen)
        // =========================================================================
        private void BuildTitleScreen(RectTransform root)
        {
            var titlePanel = UIKit.CreatePanel(root, "TitleScreenPanel", new Color(0.04f, 0.04f, 0.07f, 0.95f), true, true);
            _titleScreenRoot = titlePanel.rectTransform;

            // 로고 & 서브타이틀
            var mainTitle = UIKit.CreateLabel(_titleScreenRoot, "MainTitle", "거타지의 전설", 56f, UITheme.Gold, TextAlignmentOptions.Center, FontStyles.Bold, false, false);
            UIKit.SetAnchored(mainTitle.rectTransform, new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800f, 80f));

            var subTitle = UIKit.CreateLabel(_titleScreenRoot, "SubTitle", "Myth of Guotaji : 인연의 화살과 안개 미궁", 20f, new Color(0.8f, 0.85f, 0.95f, 0.85f), TextAlignmentOptions.Center, FontStyles.Normal, false, false);
            UIKit.SetAnchored(subTitle.rectTransform, new Vector2(0.5f, 0.63f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800f, 40f));

            // 버튼 그룹
            var btnGroup = UIKit.CreateRect("BtnGroup", _titleScreenRoot);
            UIKit.SetAnchored(btnGroup, new Vector2(0.5f, 0.38f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(320f, 180f));

            var vlg = btnGroup.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 14f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // 버튼: 사당 진입 (시작)
            var startBtn = UIKit.CreateButton(btnGroup, "StartBtn", "미궁 진입 (Enter Sanctuary)", () =>
            {
                _isTitleScreen = false;
                UpdateScreenVisibility();
                UpdateHubContent();
            }, 17f, new Color(0.2f, 0.6f, 0.35f), Color.white);
            startBtn.gameObject.AddComponent<LayoutElement>().preferredHeight = 50f;

            // 버튼: 데이터 초기화
            var resetBtn = UIKit.CreateButton(btnGroup, "ResetBtn", "저장 데이터 리셋", () =>
            {
                SaveSystem.DeleteSave();
                if (GameManager.Instance != null) GameManager.Instance.LoadGame();
                UpdateHubContent();
            }, 13f, UITheme.ButtonNormal, new Color(0.85f, 0.6f, 0.6f));
            resetBtn.gameObject.AddComponent<LayoutElement>().preferredHeight = 42f;

            // 버튼: 종료
            var quitBtn = UIKit.CreateButton(btnGroup, "QuitBtn", "게임 종료 (Exit)", () =>
            {
                Application.Quit();
            }, 13f, UITheme.ButtonNormal, Color.white);
            quitBtn.gameObject.AddComponent<LayoutElement>().preferredHeight = 42f;
        }

        // =========================================================================
        // 2. 신당 허브 화면 (Shrine Hub Screen)
        // =========================================================================
        private void BuildHubScreen(RectTransform root)
        {
            var hubPanel = UIKit.CreatePanel(root, "HubScreenPanel", new Color(0.05f, 0.05f, 0.08f, 0.88f), true, false);
            _hubScreenRoot = hubPanel.rectTransform;

            // 상단 바 (자원 및 타이틀)
            var topBar = UIKit.CreatePanel(_hubScreenRoot, "TopBar", new Color(0.08f, 0.08f, 0.12f, 0.95f), false, true);
            UIKit.SetAnchored(topBar.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(1920f, 65f));

            var hubTitle = UIKit.CreateLabel(topBar.transform, "HubTitle", "⛩ 신목의 사당 (Sacred Shrine)", 20f, UITheme.Gold, TextAlignmentOptions.Left, FontStyles.Bold, false, false);
            UIKit.SetAnchored(hubTitle.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(30f, 0f), new Vector2(350f, 40f));

            _hubResourceLabel = UIKit.CreateLabel(topBar.transform, "Resources", "", 15f, Color.white, TextAlignmentOptions.Right, FontStyles.Bold, false, false);
            UIKit.SetAnchored(_hubResourceLabel.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-30f, 0f), new Vector2(600f, 40f));

            // 상단 탭 버튼 그룹
            var tabGroup = UIKit.CreateRect("TabGroup", _hubScreenRoot);
            UIKit.SetAnchored(tabGroup, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(1000f, 44f));
            var hlg = tabGroup.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 16f;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            CreateTabBtn(tabGroup, OutgameTab.StageSelect, "⚔ 출격 미궁 선택");
            CreateTabBtn(tabGroup, OutgameTab.MemoryWell, "🌊 기억의 우물 (강화)");
            CreateTabBtn(tabGroup, OutgameTab.MemoryArchive, "📜 기억 아카이브");
            CreateTabBtn(tabGroup, OutgameTab.Armory, "🏹 무기고");

            // 메인 콘텐츠 영역
            var contentPanel = UIKit.CreatePanel(_hubScreenRoot, "MainContentArea", new Color(0.06f, 0.06f, 0.1f, 0.9f), false, true);
            _tabContentRoot = contentPanel.rectTransform;
            UIKit.SetAnchored(_tabContentRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 25f), new Vector2(1400f, 620f));

            BuildTabContent();

            // 하단 대화창 & 출격 버튼
            var bottomBar = UIKit.CreatePanel(_hubScreenRoot, "BottomBar", new Color(0.08f, 0.08f, 0.12f, 0.95f), false, true);
            UIKit.SetAnchored(bottomBar.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(1920f, 120f));

            var dialogueBox = UIKit.CreateRect("DialogueBox", bottomBar.transform);
            UIKit.SetAnchored(dialogueBox, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(40f, 0f), new Vector2(1300f, 90f));

            _dialogueSpeakerLabel = UIKit.CreateLabel(dialogueBox, "Speaker", "", 15f, UITheme.Gold, TextAlignmentOptions.TopLeft, FontStyles.Bold, false, false);
            UIKit.SetAnchored(_dialogueSpeakerLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(300f, 24f));

            _dialogueContentLabel = UIKit.CreateLabel(dialogueBox, "Content", "", 14f, new Color(0.9f, 0.92f, 0.98f), TextAlignmentOptions.TopLeft, FontStyles.Normal, false, false);
            UIKit.SetAnchored(_dialogueContentLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -28f), new Vector2(1300f, 55f));

            // 우하단 대형 출격 버튼
            var launchBtn = UIKit.CreateButton(bottomBar.transform, "LaunchBtn", "미궁으로 출격 ➔", LaunchSelectedStage, 20f, new Color(0.85f, 0.35f, 0.15f), Color.white);
            UIKit.SetAnchored(launchBtn.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-40f, 0f), new Vector2(260f, 68f));
        }

        private void CreateTabBtn(Transform parent, OutgameTab tab, string label)
        {
            UIKit.CreateButton(parent, "Tab_" + tab, label, () =>
            {
                _currentTab = tab;
                UpdateHubContent();
            }, 14f, UITheme.ButtonNormal, Color.white);
        }

        private void BuildTabContent()
        {
            _stageDetailLabel = UIKit.CreateLabel(_tabContentRoot, "DetailLabel", "", 15f, Color.white, TextAlignmentOptions.TopLeft, FontStyles.Normal, false, false);
            UIKit.SetAnchored(_stageDetailLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1320f, 560f));
        }

        private void UpdateScreenVisibility()
        {
            if (_titleScreenRoot != null) _titleScreenRoot.gameObject.SetActive(_isTitleScreen);
            if (_hubScreenRoot != null) _hubScreenRoot.gameObject.SetActive(!_isTitleScreen);
        }

        private void UpdateHubContent()
        {
            if (_hubResourceLabel != null && GameManager.Instance != null && GameManager.Instance.SaveData != null)
            {
                var save = GameManager.Instance.SaveData;
                _hubResourceLabel.text = $"기억의 조각: <color=yellow>{save.memoryShards}개</color> (누적: {save.totalCollectedShards}/22)   |   금화: <color=yellow>{save.gold}G</color>";
            }

            if (_dialogueSpeakerLabel != null) _dialogueSpeakerLabel.text = _activeSpeakerName;
            if (_dialogueContentLabel != null) _dialogueContentLabel.text = _activeDialogueText;

            if (_stageDetailLabel == null) return;

            if (_currentTab == OutgameTab.StageSelect)
            {
                var stage = (_selectedStageIndex >= 0 && _selectedStageIndex < _stages.Count) ? _stages[_selectedStageIndex] : null;
                if (stage != null)
                {
                    _stageDetailLabel.text =
                        "<size=22><b>⚔ 출격 스테이지: [" + stage.stageCode + "] " + stage.stageTitle + "</b></size>\n\n" +
                        "<b>• 출현 보스 / 수문장:</b> <color=#FFD700>" + stage.bossOrKeeperName + "</color>\n\n" +
                        "<b>• 기믹 및 지형 특성:</b>\n" + stage.gimmickSummary + "\n\n" +
                        "<b>• 시나리오 비망록:</b>\n<color=#A0C0E0>" + stage.scenarioNote + "</color>\n\n" +
                        "<color=#888888>(미궁 출격 시 해당 스테이지의 2블록 선형 미궁이 절차적으로 생성됩니다)</color>";
                }
            }
            else if (_currentTab == OutgameTab.MemoryWell)
            {
                var save = GameManager.Instance?.SaveData;
                int bowLv = save != null ? save.wellBowLevel : 0;
                int daggerLv = save != null ? save.wellDaggerLevel : 0;
                int bookLv = save != null ? save.wellBookLevel : 0;
                int survLv = save != null ? save.wellSurvivalLevel : 0;

                _stageDetailLabel.text =
                    "<size=22><b>🌊 기억의 우물 (우물가 스탯 영구 강화)</b></size>\n\n" +
                    "• <b>활 계열 Lv." + bowLv + ":</b> 화살 위력, 충전 속도, 약점 피해 증폭\n" +
                    "• <b>단검 계열 Lv." + daggerLv + ":</b> 단검 위력, 패링 유예 시간, 반사 데미지 배율\n" +
                    "• <b>보옥 계열 Lv." + bookLv + ":</b> 보석 획득량, 보호막 효율, 시작 영물 버프\n" +
                    "• <b>생존 계열 Lv." + survLv + ":</b> 최대 체력 증가, 회피 무적 시간 증폭, 회복 효율";
            }
            else if (_currentTab == OutgameTab.MemoryArchive)
            {
                _stageDetailLabel.text =
                    "<size=22><b>📜 기억 아카이브 (6대 핵심 기억 슬롯)</b></size>\n\n" +
                    "현재 장착 중인 핵심 기억: <color=#FFD700>첫 번째 활시위 (MEM_CH1_FIRST_STRING)</color>\n\n" +
                    "• <b>효과:</b> 관통 화살 차지 시간 15% 단축, 패링 성공 시 즉시 3발 연사 충전\n" +
                    "• <b>기억의 문구:</b> <i>“활시위를 당길 때마다, 잃어버린 기억의 파편이 손끝을 스쳐 지나간다...”</i>";
            }
            else if (_currentTab == OutgameTab.Armory)
            {
                _stageDetailLabel.text =
                    "<size=22><b>🏹 무기고 (시작 장비 선택)</b></size>\n\n" +
                    "• <b>주무기 (활):</b> 기본 각궁 (Bow_00) — 연사 5발, 관통 차징 사격\n" +
                    "• <b>보조무기 (단검):</b> 호신용 은장도 (Dagger_00) — 근접 베기 및 투사체 패링 반사";
            }
        }

        public void LaunchSelectedStage()
        {
            var stage = (_selectedStageIndex >= 0 && _selectedStageIndex < _stages.Count) ? _stages[_selectedStageIndex] : null;
            if (stage != null && GameManager.Instance != null && GameManager.Instance.SaveData != null)
            {
                GameManager.Instance.SaveData.selectedStageCode = stage.stageCode;
                GameManager.Instance.SaveData.currentStage = stage.globalStageNumber;
                GameManager.Instance.SaveData.returnedFromDeath = false;
                SaveSystem.Save(GameManager.Instance.SaveData);
            }

            Time.timeScale = 1f;
            SceneManager.LoadScene("PlayMain");
        }

        private void EnsureManagersAndEnvironment()
        {
            if (GameManager.Instance == null)
            {
                var gmObj = new GameObject("[GameManager]");
                gmObj.AddComponent<GameManager>();
            }
        }
    }
}