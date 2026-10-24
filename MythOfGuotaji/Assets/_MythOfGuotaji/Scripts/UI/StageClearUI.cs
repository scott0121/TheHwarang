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
                TriggerStageClear();
            }
        }

        public void TriggerStageClear()
        {
            _isShowing = true;
            _stateTimer = 0f;
            _canAcceptInput = false;

            // 1초간 플레이어 입력 잠금
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
                if (CheckAnyKeyPressed())
                {
                    ExecuteStageRestart();
                }
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

            if (Input.anyKeyDown) return true;

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

            // 2. STAGE CLEAR 메인 배너 박스
            float boxW = Mathf.Clamp(screenW * 0.55f, 480f, 850f);
            float boxH = 260f * scale;
            float boxX = (screenW - boxW) * 0.5f;
            float boxY = (screenH - boxH) * 0.42f;

            GUI.color = new Color(0.06f, 0.08f, 0.12f, 0.94f);
            GUI.DrawTexture(new Rect(boxX, boxY, boxW, boxH), _whiteTex);

            GUI.color = new Color(0.85f, 0.7f, 0.25f, 0.9f);
            DrawRectBorder(new Rect(boxX - 2, boxY - 2, boxW + 4, boxH + 4), 2f);

            // 3. 타이틀 텍스트
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(34f * scale),
                fontStyle = FontStyle.Bold
            };
            titleStyle.normal.textColor = new Color(1f, 0.88f, 0.3f);
            GUI.Label(new Rect(boxX, boxY + 25f * scale, boxW, 45f * scale), "★ STAGE CLEAR ★", titleStyle);

            // 4. 서브 설명 텍스트
            GUIStyle subStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(18f * scale),
                fontStyle = FontStyle.Normal
            };
            subStyle.normal.textColor = new Color(0.9f, 0.95f, 1f, 0.9f);
            GUI.Label(new Rect(boxX, boxY + 80f * scale, boxW, 30f * scale), "산해경의 요괴를 모두 토벌하고 영기를 정화하였습니다!", subStyle);

            int kills = GameManager.Instance != null ? GameManager.Instance.CurrentKills : 0;
            GUI.Label(new Rect(boxX, boxY + 115f * scale, boxW, 25f * scale), $"토벌한 요괴 수: {kills}마리  |  획득 인연 포인트: +5  |  골드: +100", subStyle);

            // 5. 1초 경과 후 깜빡이는 PRESS ANY KEY
            if (_canAcceptInput)
            {
                float blinkAlpha = Mathf.PingPong(Time.unscaledTime * 3.5f, 0.7f) + 0.3f;
                GUIStyle promptStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = Mathf.RoundToInt(20f * scale),
                    fontStyle = FontStyle.Bold
                };
                promptStyle.normal.textColor = new Color(0.3f, 1f, 0.7f, blinkAlpha);
                GUI.Label(new Rect(boxX, boxY + 185f * scale, boxW, 35f * scale), "▶ PRESS ANY KEY TO RESTART ◀", promptStyle);
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
                GUI.Label(new Rect(boxX, boxY + 185f * scale, boxW, 35f * scale), "결과 정리 중...", waitStyle);
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
