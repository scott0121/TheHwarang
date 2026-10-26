using MythOfGuotaji.Actor;
using MythOfGuotaji.Core;
using MythOfGuotaji.Dungeon;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MythOfGuotaji.UI
{
    /// <summary>
    /// 보스 토벌 시 활성화되는 Stage Clear GUI
    /// - 클리어 직후 1초간 모든 입력 잠금
    /// - 1초 경과 후 "PRESS ANY KEY" 표시 및 키 입력 감지
    /// - 키 입력 시 던전 재배치 및 시작룸 재출발 루프 수행
    /// </summary>
    public class StageClearUI : MonoBehaviour
    {
        public static StageClearUI Instance { get; private set; }

        private bool _isShowing = false;
        private bool _isSuccess = true;
        private float _stateTimer = 0f;
        private bool _canAcceptInput = false;
        private Texture2D _whiteTex;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _whiteTex = new Texture2D(1, 1);
            _whiteTex.SetPixel(0, 0, Color.white);
            _whiteTex.Apply();

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
            if (_whiteTex != null) Destroy(_whiteTex);
        }

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

            // 플레이어 입력 잠금
            var player = FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                player.SetInputLock(true);
            }
        }

        private void Update()
        {
            if (!_isShowing) return;

            _stateTimer += Time.unscaledDeltaTime;

            // 1초 경과 시 입력 허용 활성화
            if (_stateTimer >= 1.0f && !_canAcceptInput)
            {
                _canAcceptInput = true;
            }

            if (_canAcceptInput)
            {
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
            _isShowing = false;
            _canAcceptInput = false;
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
            _isShowing = false;
            _canAcceptInput = false;

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

        private void OnGUI()
        {
            if (!_isShowing) return;

            float screenW = Screen.width;
            float screenH = Screen.height;
            float scale = screenH / 1080f;

            Color prevColor = GUI.color;

            // 1. 전체 화면 다크 오버레이
            GUI.color = new Color(0f, 0f, 0f, 0.72f);
            GUI.DrawTexture(new Rect(0, 0, screenW, screenH), _whiteTex);

            // 2. STAGE RESULT 메인 배너 박스
            float boxW = Mathf.Clamp(screenW * 0.58f, 520f, 880f);
            float boxH = 310f * scale;
            float boxX = (screenW - boxW) * 0.5f;
            float boxY = (screenH - boxH) * 0.42f;

            GUI.color = new Color(0.06f, 0.08f, 0.12f, 0.94f);
            GUI.DrawTexture(new Rect(boxX, boxY, boxW, boxH), _whiteTex);

            Color borderColor = _isSuccess ? new Color(0.85f, 0.7f, 0.25f, 0.9f) : new Color(0.9f, 0.25f, 0.25f, 0.9f);
            GUI.color = borderColor;
            DrawRectBorder(new Rect(boxX - 2, boxY - 2, boxW + 4, boxH + 4), 2f);

            // 3. 타이틀 텍스트
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(34f * scale),
                fontStyle = FontStyle.Bold
            };
            titleStyle.normal.textColor = _isSuccess ? new Color(1f, 0.88f, 0.3f) : new Color(1f, 0.35f, 0.35f);
            string titleText = _isSuccess ? "★ STAGE CLEAR — 기억의 핵 정화 완료 ★" : "☠ STAGE FAILED — 보옥의 결계 발동 ☠";
            GUI.Label(new Rect(boxX, boxY + 22f * scale, boxW, 45f * scale), titleText, titleStyle);

            // 4. 서브 설명 텍스트
            GUIStyle subStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(18f * scale),
                fontStyle = FontStyle.Normal
            };
            subStyle.normal.textColor = new Color(0.9f, 0.95f, 1f, 0.9f);

            if (_isSuccess)
            {
                GUI.Label(new Rect(boxX, boxY + 78f * scale, boxW, 30f * scale), "산해경의 요괴를 토벌하고 진성여왕의 왜곡된 기억의 조각을 회수하였습니다!", subStyle);
                int kills = GameManager.Instance != null ? GameManager.Instance.CurrentKills : 0;
                int shards = GameManager.Instance != null && GameManager.Instance.SaveData != null ? GameManager.Instance.SaveData.memoryShards : 1;
                GUI.Label(new Rect(boxX, boxY + 112f * scale, boxW, 25f * scale), $"토벌한 요괴: {kills}마리  |  기억의 조각 +1 (보유: {shards}개)  |  인연 포인트 +5", subStyle);
            }
            else
            {
                GUI.Label(new Rect(boxX, boxY + 78f * scale, boxW, 30f * scale), "거타지가 쓰러지는 순간, 보옥(보석 토끼)이 빛을 발하며 시간을 되감습니다...", subStyle);
                int kills = GameManager.Instance != null ? GameManager.Instance.CurrentKills : 0;
                int blessing = GameManager.Instance != null && GameManager.Instance.SaveData != null ? GameManager.Instance.SaveData.retryBlessingStacks : 1;
                GUI.Label(new Rect(boxX, boxY + 112f * scale, boxW, 25f * scale), $"쓰러뜨린 요괴: {kills}마리  |  재도전 가호 {blessing}중첩 (피해 감면 +{blessing * 3}%, 보호막 +{blessing * 5}%)", subStyle);
            }

            // 5. 1초 경과 후 액션 버튼 2종 (곡도 사당 거점 귀환 / 즉시 재출격)
            if (_canAcceptInput)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;

                float btnW = 240f * scale;
                float btnH = 48f * scale;
                float gap = 24f * scale;
                float startX = boxX + (boxW - (btnW * 2f + gap)) * 0.5f;
                float btnY = boxY + 175f * scale;

                GUIStyle btnStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = Mathf.RoundToInt(17f * scale),
                    fontStyle = FontStyle.Bold
                };

                GUI.color = new Color(0.2f, 0.75f, 0.95f, 1f);
                if (GUI.Button(new Rect(startX, btnY, btnW, btnH), "⛩️ 곡도 사당 귀환 [H / Esc]", btnStyle))
                {
                    ReturnToShrineHub();
                }

                GUI.color = _isSuccess ? new Color(0.25f, 0.9f, 0.55f, 1f) : new Color(1f, 0.75f, 0.25f, 1f);
                string retryLabel = _isSuccess ? "🏹 다음 미궁 진군 [R / Space]" : "🔄 즉시 재도전 [R / Space]";
                if (GUI.Button(new Rect(startX + btnW + gap, btnY, btnW, btnH), retryLabel, btnStyle))
                {
                    ExecuteStageRestart();
                }

                float blinkAlpha = Mathf.PingPong(Time.unscaledTime * 3.5f, 0.6f) + 0.4f;
                GUIStyle promptStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = Mathf.RoundToInt(15f * scale),
                    fontStyle = FontStyle.Bold
                };
                promptStyle.normal.textColor = new Color(0.8f, 0.9f, 1f, blinkAlpha);
                GUI.Label(new Rect(boxX, boxY + 245f * scale, boxW, 30f * scale), "키보드 [H] 곡도 사당 거점(Intro 씬) 이동   |   [R / Space] 미궁 즉시 재출발", promptStyle);
            }
            else
            {
                GUIStyle waitStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = Mathf.RoundToInt(14f * scale),
                    fontStyle = FontStyle.Italic
                };
                waitStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f, 0.7f);
                GUI.Label(new Rect(boxX, boxY + 195f * scale, boxW, 35f * scale), "기억의 파편을 갈무리하는 중...", waitStyle);
            }

            GUI.color = prevColor;
        }

        private void DrawRectBorder(Rect r, float thickness)
        {
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, thickness), _whiteTex);
            GUI.DrawTexture(new Rect(r.x, r.yMax - thickness, r.width, thickness), _whiteTex);
            GUI.DrawTexture(new Rect(r.x, r.y, thickness, r.height), _whiteTex);
            GUI.DrawTexture(new Rect(r.xMax - thickness, r.y, thickness, r.height), _whiteTex);
        }
    }
}
