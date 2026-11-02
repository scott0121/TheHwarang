using MythOfGuotaji.Actor;
using MythOfGuotaji.Core;
using MythOfGuotaji.Dungeon;
using MythOfGuotaji.UI.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MythOfGuotaji.UI
{
    /// <summary>
    /// 보스 토벌 시 활성화되는 Stage Clear GUI (UGUI + TMP, 코드 빌드)
    /// - 클리어 직후 1초간 모든 입력 잠금
    /// - 1초 경과 후 액션 버튼(마우스/게임패드 선택 가능) 및 키 입력(H/Esc, R/Space/Enter) 활성화
    /// - 키 입력 시 던전 재배치 및 시작룸 재출발 루프 수행
    /// </summary>
    public class StageClearUI : UIScreen
    {
        public static StageClearUI Instance { get; private set; }

        private bool _isShowing = false;
        private bool _isSuccess = true;
        private float _stateTimer = 0f;
        private bool _canAcceptInput = false;

        // UGUI 요소
        private RectTransform _box;
        private RectTransform _borderRoot;
        private TextMeshProUGUI _title;
        private TextMeshProUGUI _sub1;
        private TextMeshProUGUI _sub2;
        private TextMeshProUGUI _waitLabel;
        private TextMeshProUGUI _promptLabel;
        private Button _hubButton;
        private Button _retryButton;
        private TextMeshProUGUI _retryLabel;

        protected override int SortingOrder => UITheme.Sorting.Modal;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureBuilt();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged += HandleStateChanged;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged -= HandleStateChanged;
            }
            if (Instance == this) Instance = null;
        }

        // ── UI 빌드 ───────────────────────────────────────────────
        protected override void Build(RectTransform root)
        {
            // 1. 전체 화면 다크 오버레이 (뒤 클릭 차단)
            UIKit.CreatePanel(root, "Dim", UITheme.Dim, true, true);

            // 2. STAGE RESULT 메인 배너 박스 (880 x 310)
            var boxImg = UIKit.CreatePanel(root, "Box", UITheme.PanelDark, false, true);
            _box = boxImg.rectTransform;
            UIKit.SetAnchored(_box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 62f), new Vector2(880f, 310f));
            _borderRoot = UIKit.CreateBorder(_box, "Border", new Color(0.85f, 0.7f, 0.25f, 0.9f), 2f, 2f);

            // 3. 타이틀 / 서브 텍스트 (상단 기준 오프셋 배치)
            _title = UIKit.CreateLabel(_box, "Title", "", 34f, UITheme.Gold, TextAlignmentOptions.Center, FontStyles.Bold, false, false);
            PlaceTop(_title.rectTransform, 22f, 45f);

            var subColor = new Color(0.9f, 0.95f, 1f, 0.9f);
            _sub1 = UIKit.CreateLabel(_box, "Sub1", "", 18f, subColor, TextAlignmentOptions.Center, FontStyles.Normal, false, false);
            PlaceTop(_sub1.rectTransform, 78f, 30f);
            _sub2 = UIKit.CreateLabel(_box, "Sub2", "", 18f, subColor, TextAlignmentOptions.Center, FontStyles.Normal, false, false);
            PlaceTop(_sub2.rectTransform, 112f, 25f);

            // 4. 대기 라벨 (입력 잠금 중)
            _waitLabel = UIKit.CreateLabel(_box, "Wait", UIText.Get("stageclear.wait", "기억의 파편을 갈무리하는 중..."), 14f,
                new Color(0.7f, 0.7f, 0.7f, 0.7f), TextAlignmentOptions.Center, FontStyles.Italic, false, false);
            PlaceTop(_waitLabel.rectTransform, 195f, 35f);

            // 5. 액션 버튼 2종
            _hubButton = UIKit.CreateButton(_box, "HubButton", UIText.Get("stageclear.btn.hub", "곡도 사당 귀환 [H / Esc]"), ReturnToShrineHub,
                17f, new Color(0.2f, 0.75f, 0.95f, 1f), new Color(0.04f, 0.08f, 0.12f, 1f));
            PlaceTopCenter((RectTransform)_hubButton.transform, -132f, 175f, 240f, 48f);

            _retryButton = UIKit.CreateButton(_box, "RetryButton", "", ExecuteStageRestart,
                17f, new Color(0.25f, 0.9f, 0.55f, 1f), new Color(0.04f, 0.08f, 0.12f, 1f));
            PlaceTopCenter((RectTransform)_retryButton.transform, 132f, 175f, 240f, 48f);
            _retryLabel = (TextMeshProUGUI)UIKit.GetButtonLabel(_retryButton);

            _promptLabel = UIKit.CreateLabel(_box, "Prompt",
                UIText.Get("stageclear.prompt", "키보드 [H] 곡도 사당 거점(Intro 씬) 이동   |   [R / Space] 미궁 즉시 재출발"), 15f,
                new Color(0.8f, 0.9f, 1f, 1f), TextAlignmentOptions.Center, FontStyles.Bold, false, false);
            PlaceTop(_promptLabel.rectTransform, 245f, 30f);
        }

        /// <summary>박스 상단 기준 오프셋(px)/높이로 가로 전체 배치 (원본 IMGUI 오프셋과 동일 좌표계).</summary>
        private static void PlaceTop(RectTransform rt, float top, float height)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -top);
            rt.sizeDelta = new Vector2(0f, height);
        }

        private static void PlaceTopCenter(RectTransform rt, float centerX, float top, float width, float height)
        {
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(centerX, -top);
            rt.sizeDelta = new Vector2(width, height);
        }

        // ── 로직 ──────────────────────────────────────────────────
        private void HandleStateChanged(GameState newState)
        {
            if (newState == GameState.StageSuccess)
            {
                TriggerStageResult(true);
            }
            else if (newState == GameState.StageFailed)
            {
                TriggerStageResult(false);
            }
        }

        public void TriggerStageClear()
        {
            TriggerStageResult(true);
        }

        public void TriggerStageResult(bool isSuccess)
        {
            _isSuccess = isSuccess;
            _isShowing = true;
            _stateTimer = 0f;
            _canAcceptInput = false;

            ApplyResultTexts();
            ApplyInputState();
            Show();

            // 플레이어 입력 잠금
            var player = FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                player.SetInputLock(true);
            }
        }

        /// <summary>성공/실패에 따른 문구·색상 적용 (결과 표시 시 1회).</summary>
        private void ApplyResultTexts()
        {
            var gm = GameManager.Instance;
            int kills = gm != null ? gm.CurrentKills : 0;

            UIKit.SetBorderColor(_borderRoot, _isSuccess ? new Color(0.85f, 0.7f, 0.25f, 0.9f) : new Color(0.9f, 0.25f, 0.25f, 0.9f));
            _title.color = _isSuccess ? new Color(1f, 0.88f, 0.3f) : new Color(1f, 0.35f, 0.35f);

            if (_isSuccess)
            {
                int shards = gm != null && gm.SaveData != null ? gm.SaveData.memoryShards : 1;
                _title.text = UIText.Get("stageclear.title.success", "★ STAGE CLEAR — 기억의 핵 정화 완료 ★");
                _sub1.text = UIText.Get("stageclear.sub.success", "산해경의 요괴를 토벌하고 진성여왕의 왜곡된 기억의 조각을 회수하였습니다!");
                _sub2.text = $"토벌한 요괴: {kills}마리  |  기억의 조각 +1 (보유: {shards}개)  |  인연 포인트 +5";
                _retryLabel.text = UIText.Get("stageclear.btn.next", "다음 미궁 진군 [R / Space]");
                UIKit.SetButtonColor(_retryButton, new Color(0.25f, 0.9f, 0.55f, 1f));
            }
            else
            {
                int blessing = gm != null && gm.SaveData != null ? gm.SaveData.retryBlessingStacks : 1;
                _title.text = UIText.Get("stageclear.title.fail", "STAGE FAILED — 보옥의 결계 발동");
                _sub1.text = UIText.Get("stageclear.sub.fail", "거타지가 쓰러지는 순간, 보옥(보석 토끼)이 빛을 발하며 시간을 되감습니다...");
                _sub2.text = $"쓰러뜨린 요괴: {kills}마리  |  재도전 가호 {blessing}중첩 (피해 감면 +{blessing * 3}%, 보호막 +{blessing * 5}%)";
                _retryLabel.text = UIText.Get("stageclear.btn.retry", "즉시 재도전 [R / Space]");
                UIKit.SetButtonColor(_retryButton, new Color(1f, 0.75f, 0.25f, 1f));
            }
        }

        /// <summary>입력 허용 여부에 따라 버튼/프롬프트 vs 대기 문구 전환.</summary>
        private void ApplyInputState()
        {
            _hubButton.gameObject.SetActive(_canAcceptInput);
            _retryButton.gameObject.SetActive(_canAcceptInput);
            _promptLabel.gameObject.SetActive(_canAcceptInput);
            _waitLabel.gameObject.SetActive(!_canAcceptInput);
        }

        private void Update()
        {
            if (!_isShowing) return;

            _stateTimer += Time.unscaledDeltaTime;

            // 1초 경과 시 입력 허용 활성화
            if (_stateTimer >= 1.0f && !_canAcceptInput)
            {
                _canAcceptInput = true;
                ApplyInputState();
                // 게임패드/키보드 네비게이션: 첫 버튼 선택
                UIEventSystem.SelectFirst(_retryButton);
            }

            if (_canAcceptInput)
            {
                // 결과 화면에서는 항상 마우스 커서 사용 (원본 IMGUI 동작과 동일)
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;

                // 깜빡이는 안내 문구
                float blinkAlpha = Mathf.PingPong(Time.unscaledTime * 3.5f, 0.6f) + 0.4f;
                var c = _promptLabel.color;
                c.a = blinkAlpha;
                _promptLabel.color = c;

                var kb = Keyboard.current;
                if (kb != null && (kb.hKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame))
                {
                    ReturnToShrineHub();
                    return;
                }
                if (kb != null && (kb.rKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
                {
                    ExecuteStageRestart();
                    return;
                }
            }
        }

        private void ReturnToShrineHub()
        {
            if (!_isShowing) return;
            _isShowing = false;
            _canAcceptInput = false;
            Hide();
            BossHealthBarUI.Instance?.Hide();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.ReturnToIntroHub();
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(GameManager.IntroSceneName);
            }
        }

        private bool CheckAnyKeyPressed()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.anyKey.wasPressedThisFrame) return true;

            var mouse = Mouse.current;
            if (mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame || mouse.middleButton.wasPressedThisFrame)) return true;

            var pad = Gamepad.current;
            if (pad != null)
            {
                if (pad.buttonSouth.wasPressedThisFrame || pad.buttonEast.wasPressedThisFrame ||
                    pad.buttonNorth.wasPressedThisFrame || pad.buttonWest.wasPressedThisFrame ||
                    pad.startButton.wasPressedThisFrame || pad.selectButton.wasPressedThisFrame)
                {
                    return true;
                }
            }

            return false;
        }

        private void ExecuteStageRestart()
        {
            if (!_isShowing) return;
            _isShowing = false;
            _canAcceptInput = false;
            Hide();

            var player = FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                player.SetInputLock(false);
            }

            // 보스 HP 바 닫기
            BossHealthBarUI.Instance?.Hide();

            // 던전 재생성 및 시작룸 재출발!
            if (MazeGenerator.Instance != null)
            {
                MazeGenerator.Instance.RestartStage();
            }
        }
    }
}
