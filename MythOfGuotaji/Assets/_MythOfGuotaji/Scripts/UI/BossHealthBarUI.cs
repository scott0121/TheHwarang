using MythOfGuotaji.Actor;
using MythOfGuotaji.UI.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MythOfGuotaji.UI
{
    /// <summary>
    /// 보스방 진입 시 화면 상단에 가로로 크고 웅장하게 표시되는 보스 전용 HP 바 (UGUI + TMP)
    /// - 이전 IMGUI(OnGUI) 구현을 UIScreen 기반 코드 빌드 UGUI 로 교체 (공개 API 동일)
    /// - 페이드는 CanvasGroup.alpha, 수치 텍스트는 값이 바뀔 때만 SetText (GC 없음)
    /// </summary>
    public class BossHealthBarUI : UIScreen
    {
        public static BossHealthBarUI Instance { get; private set; }

        private MonsterAI _currentBoss;
        private float _displayHpRatio = 1f;
        private float _displayAlpha = 0f;

        [Header("Boss Display Info")]
        [SerializeField] private string _bossTitle = "✦ 산해경의 재앙 · 천년 여우 요괴 ✦";
        [SerializeField] private float _barWidthRatio = 0.52f; // 화면 가로 52%
        [SerializeField] private float _barHeight = 22f;

        // UGUI 요소
        private CanvasGroup _group;
        private RectTransform _barRect;
        private Image _fill;
        private TextMeshProUGUI _titleLabel;
        private TextMeshProUGUI _hpLabel;

        // 변경 감지 캐시 (불필요한 갱신/문자열 생성 방지)
        private float _lastCanvasWidth = -1f;
        private int _lastHp = int.MinValue, _lastMaxHp = int.MinValue, _lastPct = int.MinValue;
        private int _lastLevel = int.MinValue;
        private float _lastFillRatio = -1f;
        private float _lastGroupAlpha = -1f;

        protected override int SortingOrder => UITheme.Sorting.BossBar;
        protected override bool NeedsRaycaster => false;
        protected override bool NeedsEventSystem => false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            EnsureBuilt();
        }

        protected override void Build(RectTransform root)
        {
            // 컨테이너: 상단 중앙 기준, 폭은 LateUpdate 에서 화면 비율로 갱신
            _barRect = UIKit.CreateRect("BossBar", root);
            UIKit.SetAnchored(_barRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -32f), new Vector2(600f, _barHeight));
            _group = _barRect.gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.interactable = false;

            // 1. 보스 명칭 타이틀 (바 위쪽 24px)
            _titleLabel = UIKit.CreateLabel(_barRect, "Title", _bossTitle, 17f, UITheme.Gold, TextAlignmentOptions.Center, FontStyles.Bold, false, false);
            UIKit.SetAnchored(_titleLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0f), new Vector2(0f, 2f), new Vector2(900f, 24f));

            // 2. 프레임 (흑금/칠흑)
            var outer = UIKit.CreatePanel(_barRect, "Outer", new Color(0.04f, 0.04f, 0.06f, 0.95f), true);
            UIKit.Stretch(outer.rectTransform, -3f, -3f, -3f, -3f);
            var border = UIKit.CreatePanel(_barRect, "Border", new Color(0.75f, 0.6f, 0.2f, 0.85f), true);
            UIKit.Stretch(border.rectTransform, -1f, -1f, -1f, -1f);

            // 3. 다크 체력 배경 + 4. 채움 바
            var bg = UIKit.CreateBar(_barRect, "Back", new Color(0.18f, 0.04f, 0.04f, 0.9f), new Color(0.9f, 0.15f, 0.1f), out _fill);
            UIKit.Stretch(bg.rectTransform);

            // 5. HP 수치
            _hpLabel = UIKit.CreateLabel(_barRect, "HpText", "", 12f, new Color(1f, 1f, 1f, 0.95f), TextAlignmentOptions.Center, FontStyles.Bold, false, false);
            UIKit.Stretch(_hpLabel.rectTransform);
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

        private void LateUpdate()
        {
            if (_group == null) return;

            bool show = _displayAlpha > 0.01f && _currentBoss != null;
            if (show != IsVisible) SetVisible(show);
            if (!show) return;

            // 폭: 캔버스 폭의 비율 (400~850), 해상도 변경 시에만 갱신
            float canvasW = UIKit.CanvasSize(Canvas).x;
            if (!Mathf.Approximately(canvasW, _lastCanvasWidth))
            {
                _lastCanvasWidth = canvasW;
                _barRect.sizeDelta = new Vector2(Mathf.Clamp(canvasW * _barWidthRatio, 400f, 850f), _barHeight);
            }

            if (!Mathf.Approximately(_lastGroupAlpha, _displayAlpha))
            {
                _lastGroupAlpha = _displayAlpha;
                _group.alpha = _displayAlpha;
            }

            // 채움 비율 + 색상
            if (!Mathf.Approximately(_lastFillRatio, _displayHpRatio))
            {
                _lastFillRatio = _displayHpRatio;
                UIKit.SetFill(_fill, _displayHpRatio);
                _fill.color = Color.Lerp(new Color(0.9f, 0.15f, 0.1f), new Color(1.0f, 0.35f, 0.1f), _displayHpRatio);
                _fill.enabled = _displayHpRatio * _barRect.sizeDelta.x > 0.5f;
            }

            // 타이틀 (레벨 변경 시에만 재구성)
            int level = _currentBoss.Level;
            if (level != _lastLevel)
            {
                _lastLevel = level;
                _titleLabel.text = level > 0 ? $"[Lv.{level}] {_bossTitle}" : _bossTitle;
            }

            // HP 수치 (정수 값이 바뀔 때만)
            int hp = Mathf.RoundToInt(Mathf.Max(0f, _currentBoss.CurrentHp));
            int maxHp = Mathf.RoundToInt(_currentBoss.MaxHp);
            int pct = Mathf.RoundToInt(_displayHpRatio * 100f);
            if (hp != _lastHp || maxHp != _lastMaxHp || pct != _lastPct)
            {
                _lastHp = hp; _lastMaxHp = maxHp; _lastPct = pct;
                _hpLabel.SetText("{0} / {1} ({2}%)", hp, maxHp, pct);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
