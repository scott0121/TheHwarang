using System.Collections;
using MythOfGuotaji.Actor;
using UnityEngine;
using UnityEngine.UI;

namespace MythOfGuotaji.UI
{
    /// <summary>
    /// 화면 최하단 중앙 캐릭터 3중 체력 프로그레스바 (UGUI)
    /// - 규격: 폭 400px, 높이 30px (화면 최하단 중앙)
    /// - 3중 레이어 기획의도 완벽 반영:
    ///   1. 바탕 (배경): 붉은색 (초록색이 줄어들면 붉은색 막대가 드러남)
    ///   2. 회복 선행 바 (중간): 노란색 (체력 회복 시 먼저 회복될 목표 체력 위치까지 즉시 상승)
    ///   3. 현재 체력 바 (전면): 초록색 (피격 시 즉시 감소, 회복 시 1초 대기 후 부드럽게 노란 바 위치까지 채워짐)
    /// - RectTransform 앵커 제어와 유니티 스프라이트 듀얼 바인딩으로 Sprite 누락 시에도 100% 칼같이 디스플레이
    /// </summary>
    public class PlayerHealthBarUI : MonoBehaviour
    {
        public static PlayerHealthBarUI Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private RectTransform _containerRect;    // 폭 400px, 높이 30px 바 컨테이너
        [SerializeField] private Image _backgroundImage;          // 1단계: 붉은색 바탕
        [SerializeField] private Image _healLeadBar;              // 2단계: 노란색 회복 선행 바
        [SerializeField] private Image _currentHealthBar;         // 3단계: 초록색 현재 체력 바
        [SerializeField] private Text _healthText;                // 중앙 체력 텍스트 (HP 100 / 100)

        [Header("Animation Settings")]
        [SerializeField] private float _healDelay = 1.0f;         // 회복 후 초록색 채워지기 전 대기 시간 (기획: 1초)
        [SerializeField] private float _fillDuration = 0.35f;     // 초록색이 차오르는 부드러운 시간

        private PlayerController _player;
        private float _lastHp = -1f;
        private float _lastMaxHp = -1f;
        private float _currentDisplayedHpRatio = 1f;
        private Coroutine _healRoutine;
        private bool _isInitialized = false;

        private static Sprite _whiteSprite;
        public static Sprite WhiteSprite
        {
            get
            {
                if (_whiteSprite == null)
                {
                    var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    Color[] cols = { Color.white, Color.white, Color.white, Color.white };
                    tex.SetPixels(cols);
                    tex.Apply();
                    _whiteSprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
                }
                return _whiteSprite;
            }
        }

        public static void EnsureInstance()
        {
            if (Instance != null) return;

            var existing = FindAnyObjectByType<PlayerHealthBarUI>();
            if (existing != null)
            {
                Instance = existing;
                return;
            }

            var prefab = Resources.Load<GameObject>("Prefabs/GUI/PlayerHealthBarUI");
            if (prefab != null)
            {
                var go = Instantiate(prefab);
                go.name = "PlayerHealthBarUI";
            }
            else
            {
                BuildRuntimeHealthBar();
            }
        }

        public static void BuildRuntimeHealthBar()
        {
            var rootGo = new GameObject("PlayerHealthBarUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(PlayerHealthBarUI));
            var canvas = rootGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 60; // 다른 UI보다 전면 정렬

            var scaler = rootGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var rootRect = rootGo.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.sizeDelta = Vector2.zero;

            // 1. HealthBar Container (화면 최하단 중심, 폭 400px, 높이 30px)
            var containerGo = new GameObject("HealthBar_Container", typeof(RectTransform));
            containerGo.transform.SetParent(rootGo.transform, false);
            var contRect = containerGo.GetComponent<RectTransform>();
            contRect.anchorMin = new Vector2(0.5f, 0f);
            contRect.anchorMax = new Vector2(0.5f, 0f);
            contRect.pivot = new Vector2(0.5f, 0.5f);
            contRect.anchoredPosition = new Vector2(0f, 35f); // 화면 하단에서 35px 위
            contRect.sizeDelta = new Vector2(400f, 30f);

            // 외곽 그림자/프레임
            var frameGo = new GameObject("Frame", typeof(RectTransform), typeof(Image));
            frameGo.transform.SetParent(containerGo.transform, false);
            var frameRect = frameGo.GetComponent<RectTransform>();
            frameRect.anchorMin = Vector2.zero;
            frameRect.anchorMax = Vector2.one;
            frameRect.offsetMin = new Vector2(-3f, -3f);
            frameRect.offsetMax = new Vector2(3f, 3f);
            var frameImg = frameGo.GetComponent<Image>();
            frameImg.color = new Color(0.08f, 0.08f, 0.12f, 0.95f);
            frameImg.raycastTarget = false;

            // 2. 1단계 바탕: 붉은색 막대 (초록색이 줄어들면 노출되는 바탕)
            var bgGo = new GameObject("Background_RedBar", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(containerGo.transform, false);
            var bgRect = bgGo.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            var bgImg = bgGo.GetComponent<Image>();
            bgImg.sprite = WhiteSprite;
            bgImg.color = new Color(0.78f, 0.12f, 0.12f, 1f); // 선명한 붉은색
            bgImg.raycastTarget = false;

            // 3. 2단계 중간: 회복 선행 바 (노란색)
            var healGo = new GameObject("HealLead_YellowBar", typeof(RectTransform), typeof(Image));
            healGo.transform.SetParent(containerGo.transform, false);
            var healRect = healGo.GetComponent<RectTransform>();
            healRect.anchorMin = Vector2.zero;
            healRect.anchorMax = Vector2.one;
            healRect.offsetMin = Vector2.zero;
            healRect.offsetMax = Vector2.zero;
            var healImg = healGo.GetComponent<Image>();
            healImg.sprite = WhiteSprite;
            healImg.color = new Color(1.0f, 0.85f, 0.12f, 1f); // 황금 노란색
            healImg.raycastTarget = false;

            // 4. 3단계 전면: 현재 체력 바 (초록색)
            var hpGo = new GameObject("Current_GreenBar", typeof(RectTransform), typeof(Image));
            hpGo.transform.SetParent(containerGo.transform, false);
            var hpRect = hpGo.GetComponent<RectTransform>();
            hpRect.anchorMin = Vector2.zero;
            hpRect.anchorMax = Vector2.one;
            hpRect.offsetMin = Vector2.zero;
            hpRect.offsetMax = Vector2.zero;
            var hpImg = hpGo.GetComponent<Image>();
            hpImg.sprite = WhiteSprite;
            hpImg.color = new Color(0.18f, 0.82f, 0.35f, 1f); // 활력 넘치는 초록색
            hpImg.raycastTarget = false;

            // 5. 체력 텍스트
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            var textGo = new GameObject("HP_Text", typeof(RectTransform), typeof(Text), typeof(Outline));
            textGo.transform.SetParent(containerGo.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var hpText = textGo.GetComponent<Text>();
            hpText.font = font;
            hpText.fontSize = 18;
            hpText.fontStyle = FontStyle.Bold;
            hpText.alignment = TextAnchor.MiddleCenter;
            hpText.color = Color.white;
            hpText.text = "HP 100 / 100";
            hpText.raycastTarget = false;

            var outline = textGo.GetComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            var comp = rootGo.GetComponent<PlayerHealthBarUI>();
            comp.BindComponents(contRect, bgImg, healImg, hpImg, hpText);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureSprites();
        }

        private void EnsureSprites()
        {
            if (_backgroundImage != null && _backgroundImage.sprite == null) _backgroundImage.sprite = WhiteSprite;
            if (_healLeadBar != null && _healLeadBar.sprite == null) _healLeadBar.sprite = WhiteSprite;
            if (_currentHealthBar != null && _currentHealthBar.sprite == null) _currentHealthBar.sprite = WhiteSprite;
        }

        private void Start()
        {
            EnsureSprites();
            FindPlayerAndInitialize();
        }

        private void FindPlayerAndInitialize()
        {
            _player = PlayerController.Instance != null ? PlayerController.Instance : FindAnyObjectByType<PlayerController>();
            if (_player != null && _player.MaxHp > 0f)
            {
                _lastHp = _player.CurrentHp;
                _lastMaxHp = _player.MaxHp;
                _currentDisplayedHpRatio = Mathf.Clamp01(_lastHp / Mathf.Max(1f, _lastMaxHp));
                UpdateImmediate(_lastHp, _lastMaxHp);
                _isInitialized = true;
            }
        }

        private void Update()
        {
            if (_player == null || !_isInitialized)
            {
                FindPlayerAndInitialize();
                if (_player == null) return;
            }

            float currentHp = _player.CurrentHp;
            float maxHp = _player.MaxHp;

            // 1. 최대 체력 변동 (축복 등 스탯 상승)
            if (!Mathf.Approximately(maxHp, _lastMaxHp))
            {
                _lastMaxHp = maxHp;
                UpdateImmediate(currentHp, maxHp);
                _lastHp = currentHp;
                return;
            }

            // 2. 체력 수치 변동 감지
            if (!Mathf.Approximately(currentHp, _lastHp))
            {
                if (currentHp > _lastHp)
                {
                    // [체력 회복]:
                    // 1) 노란색 바가 먼저 회복된 현재 체력 위치까지 즉시 상승
                    // 2) 1초 대기 후 초록색 막대가 부드럽게 채워짐
                    OnHealed(currentHp, maxHp);
                }
                else
                {
                    // [체력 피격/감소]:
                    // 초록색 막대가 즉시 줄어들면서 배경의 붉은색이 드러남
                    OnDamaged(currentHp, maxHp);
                }

                _lastHp = currentHp;
            }

            // 텍스트는 매 프레임 정확한 실시간 수치 표시
            UpdateText(currentHp, maxHp);
        }

        /// <summary>
        /// 피격 시: 초록색 막대와 노란색 막대가 즉시 감소하여 배경의 붉은색 노출
        /// </summary>
        private void OnDamaged(float currentHp, float maxHp)
        {
            if (_healRoutine != null)
            {
                StopCoroutine(_healRoutine);
                _healRoutine = null;
            }

            float targetRatio = Mathf.Clamp01(currentHp / Mathf.Max(1f, maxHp));
            _currentDisplayedHpRatio = targetRatio;

            SetBarRatio(_currentHealthBar, targetRatio);
            SetBarRatio(_healLeadBar, targetRatio);
        }

        /// <summary>
        /// 회복 시: 먼저 노란색 바가 목표 체력으로 즉시 올라가고, 1초 뒤 초록색 바가 차오름
        /// </summary>
        private void OnHealed(float currentHp, float maxHp)
        {
            float targetRatio = Mathf.Clamp01(currentHp / Mathf.Max(1f, maxHp));

            // 1. 노란색 선행 바는 즉시 새 체력 비율로 상승 (노란색 영역 노출!)
            SetBarRatio(_healLeadBar, targetRatio);

            // 2. 초록색 바는 1초 뒤 노란색 바 위치까지 부드럽게 채워짐
            if (_healRoutine != null)
            {
                StopCoroutine(_healRoutine);
            }
            _healRoutine = StartCoroutine(HealFillRoutine(targetRatio));
        }

        private IEnumerator HealFillRoutine(float targetRatio)
        {
            // 기획의도: 1초 동안 노란색 선행 바 표시 유지
            yield return new WaitForSeconds(_healDelay);

            float startRatio = _currentDisplayedHpRatio;
            float elapsed = 0f;

            while (elapsed < _fillDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _fillDuration);
                float curRatio = Mathf.Lerp(startRatio, targetRatio, Mathf.SmoothStep(0f, 1f, t));
                _currentDisplayedHpRatio = curRatio;
                SetBarRatio(_currentHealthBar, curRatio);
                yield return null;
            }

            _currentDisplayedHpRatio = targetRatio;
            SetBarRatio(_currentHealthBar, targetRatio);
            _healRoutine = null;
        }

        /// <summary>
        /// 막대 비율(0.0 ~ 1.0)을 시각적으로 설정
        /// - RectTransform 앵커 제어(Sprite 무관 100% 작동) + Image Type 동시 제어로 완벽 호환
        /// </summary>
        private void SetBarRatio(Image bar, float ratio)
        {
            if (bar == null) return;
            ratio = Mathf.Clamp01(ratio);

            // 1. RectTransform 수평 앵커 제어 (Sprite 및 렌더러 상태와 무관하게 100% 정확히 클리핑)
            var rt = bar.rectTransform;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(ratio, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // 2. Sprite 및 fillAmount 동기화
            if (bar.sprite == null) bar.sprite = WhiteSprite;
            if (bar.type == Image.Type.Filled)
            {
                bar.fillAmount = ratio;
            }
        }

        public void UpdateImmediate(float currentHp, float maxHp)
        {
            float ratio = Mathf.Clamp01(currentHp / Mathf.Max(1f, maxHp));
            _currentDisplayedHpRatio = ratio;
            SetBarRatio(_currentHealthBar, ratio);
            SetBarRatio(_healLeadBar, ratio);
            UpdateText(currentHp, maxHp);
        }

        private void UpdateText(float currentHp, float maxHp)
        {
            if (_healthText != null)
            {
                _healthText.text = $"HP  {Mathf.CeilToInt(currentHp)} / {Mathf.RoundToInt(maxHp)}";
            }
        }

        public void BindComponents(RectTransform contRect, Image bg, Image healLead, Image currentHp, Text hpText)
        {
            _containerRect = contRect;
            _backgroundImage = bg;
            _healLeadBar = healLead;
            _currentHealthBar = currentHp;
            _healthText = hpText;
            EnsureSprites();
        }
    }
}
