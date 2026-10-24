using MythOfGuotaji.Actor;
using UnityEngine;

namespace MythOfGuotaji.UI
{
    /// <summary>
    /// 보스방 진입 시 화면 상단에 가로로 크고 웅장하게 표시되는 보스 전용 HP 바
    /// </summary>
    public class BossHealthBarUI : MonoBehaviour
    {
        public static BossHealthBarUI Instance { get; private set; }

        private MonsterAI _currentBoss;
        private float _displayHpRatio = 1f;
        private float _displayAlpha = 0f;
        private Texture2D _whiteTex;

        [Header("Boss Display Info")]
        [SerializeField] private string _bossTitle = "✦ 산해경의 재앙 · 천년 여우 요괴 ✦";
        [SerializeField] private float _barWidthRatio = 0.52f; // 화면 가로 52%
        [SerializeField] private float _barHeight = 22f;

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
        }

        public void ShowBoss(MonsterAI boss)
        {
            _currentBoss = boss;
            _displayHpRatio = boss != null ? Mathf.Clamp01(boss.CurrentHp / boss.MaxHp) : 1f;
            _displayAlpha = 1f;
        }

        public void Hide()
        {
            _currentBoss = null;
            _displayAlpha = 0f;
        }

        private void Update()
        {
            bool hasActiveBoss = _currentBoss != null && !_currentBoss.IsDead && _currentBoss.gameObject.activeInHierarchy;
            float targetAlpha = hasActiveBoss ? 1f : 0f;
            _displayAlpha = Mathf.MoveTowards(_displayAlpha, targetAlpha, Time.deltaTime * 3.5f);

            if (hasActiveBoss)
            {
                float targetRatio = Mathf.Clamp01(_currentBoss.CurrentHp / _currentBoss.MaxHp);
                _displayHpRatio = Mathf.Lerp(_displayHpRatio, targetRatio, Time.deltaTime * 6f);
            }
        }

        private void OnGUI()
        {
            if (_displayAlpha <= 0.01f || _currentBoss == null) return;

            float screenW = Screen.width;
            float screenH = Screen.height;
            float scale = screenH / 1080f;

            float barWidth = Mathf.Clamp(screenW * _barWidthRatio, 400f, 850f);
            float barHeight = _barHeight * scale;
            float topY = 32f * scale;
            float leftX = (screenW - barWidth) * 0.5f;

            Color prevColor = GUI.color;

            // 1. 보스 명칭 타이틀 라벨
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(17f * scale),
                fontStyle = FontStyle.Bold
            };
            titleStyle.normal.textColor = new Color(1f, 0.88f, 0.45f, _displayAlpha); // 찬란한 황금색

            Rect titleRect = new Rect(leftX, topY - 26f * scale, barWidth, 24f * scale);
            string displayTitle = (_currentBoss != null && _currentBoss.Level > 0)
                ? $"[Lv.{_currentBoss.Level}] {_bossTitle}"
                : _bossTitle;
            GUI.Label(titleRect, displayTitle, titleStyle);

            // 2. 바 배경 외곽선 및 테두리 (흑금/칠흑 프레임)
            Rect outerRect = new Rect(leftX - 3, topY - 3, barWidth + 6, barHeight + 6);
            GUI.color = new Color(0.04f, 0.04f, 0.06f, 0.95f * _displayAlpha);
            GUI.DrawTexture(outerRect, _whiteTex);

            Rect borderRect = new Rect(leftX - 1, topY - 1, barWidth + 2, barHeight + 2);
            GUI.color = new Color(0.75f, 0.6f, 0.2f, 0.85f * _displayAlpha);
            GUI.DrawTexture(borderRect, _whiteTex);

            // 3. 다크 체력 배경
            Rect innerBgRect = new Rect(leftX, topY, barWidth, barHeight);
            GUI.color = new Color(0.18f, 0.04f, 0.04f, 0.9f * _displayAlpha);
            GUI.DrawTexture(innerBgRect, _whiteTex);

            // 4. 체력 채움 바 (그라데이션 심홍 / 화염 레드)
            float fillWidth = barWidth * _displayHpRatio;

            if (fillWidth > 0.5f)
            {
                Rect fillRect = new Rect(leftX, topY, fillWidth, barHeight);
                Color barColor = Color.Lerp(new Color(0.9f, 0.15f, 0.1f), new Color(1.0f, 0.35f, 0.1f), _displayHpRatio);
                barColor.a *= _displayAlpha;
                GUI.color = barColor;
                GUI.DrawTexture(fillRect, _whiteTex);
            }

            // 5. HP 수치 텍스트 (예: 350 / 350 (100%))
            GUIStyle hpTextStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(12f * scale),
                fontStyle = FontStyle.Bold
            };
            hpTextStyle.normal.textColor = new Color(1f, 1f, 1f, 0.95f * _displayAlpha);

            string hpText = $"{Mathf.Max(0, _currentBoss.CurrentHp):F0} / {_currentBoss.MaxHp:F0} ({(_displayHpRatio * 100):F0}%)";
            GUI.Label(new Rect(leftX, topY, barWidth, barHeight), hpText, hpTextStyle);

            GUI.color = prevColor;
        }

        private void OnDestroy()
        {
            if (_whiteTex != null) Destroy(_whiteTex);
        }
    }
}
