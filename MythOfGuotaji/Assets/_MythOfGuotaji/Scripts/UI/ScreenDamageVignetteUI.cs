using MythOfGuotaji.UI.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace MythOfGuotaji.UI
{
    /// <summary>
    /// 캐릭터 피격 시 화면 주변에 붉은색 혈흔/비네트 플래시 피격 이펙트를 표시하는 UI
    /// - 절차적 원형 비네트 텍스처를 동적 생성하여 화면 외곽에 즉각적인 피격 피드백 제공
    /// - 피격 시 최대 강도로 점멸 후 부드럽게 페이드 아웃
    /// </summary>
    public class ScreenDamageVignetteUI : UIScreen
    {
        private static ScreenDamageVignetteUI _instance;
        public static ScreenDamageVignetteUI Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<ScreenDamageVignetteUI>();
                    if (_instance == null)
                    {
                        GameObject prefab = null;
                        if (Data.GameDataManager.Instance != null && Data.GameDataManager.Instance.Prefabs != null)
                        {
                            prefab = Data.PrefabDatabaseSO.ResolvePrefab(ref Data.GameDataManager.Instance.Prefabs.screenDamageVignetteUIPrefab, "Prefabs/GUI/ScreenDamageVignetteUI");
                        }
                        else
                        {
                            prefab = Resources.Load<GameObject>("Prefabs/GUI/ScreenDamageVignetteUI");
                        }

                        var go = prefab != null ? Instantiate(prefab) : new GameObject("[ScreenDamageVignetteUI]");
                        go.name = "ScreenDamageVignetteUI";
                        _instance = go.GetComponent<ScreenDamageVignetteUI>();
                        if (_instance == null) _instance = go.AddComponent<ScreenDamageVignetteUI>();
                    }
                }
                return _instance;
            }
        }

        [Header("Vignette Settings")]
        [SerializeField] private float _fadeSpeed = 2.4f;
        [SerializeField] private Color _vignetteColor = new Color(0.85f, 0.03f, 0.06f, 1.0f);

        private Texture2D _vignetteTex;
        private float _currentAlpha = 0f;
        private RawImage _vignetteImage;
        private float _appliedAlpha = -1f;

        // UGUI: 비네트는 입력을 막으면 안 되므로 레이캐스터/이벤트시스템 불필요
        protected override int SortingOrder => UITheme.Sorting.Vignette;
        protected override bool NeedsRaycaster => false;
        protected override bool NeedsEventSystem => false;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            GenerateProceduralVignetteTexture();
            EnsureBuilt();
        }

        protected override void Build(RectTransform root)
        {
            _vignetteImage = new GameObject("Vignette", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage)).GetComponent<RawImage>();
            _vignetteImage.transform.SetParent(root, false);
            UIKit.Stretch(_vignetteImage.rectTransform);
            _vignetteImage.texture = _vignetteTex;
            _vignetteImage.raycastTarget = false;
            _vignetteImage.color = new Color(1f, 1f, 1f, 0f);
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
            if (_vignetteTex != null)
            {
                Destroy(_vignetteTex);
                _vignetteTex = null;
            }
        }

        /// <summary>
        /// 캐릭터 피격 시 호출: 화면 주변 붉은 비네트 플래시 발동
        /// </summary>
        public void TriggerDamageFlash(float intensity = 0.85f)
        {
            _currentAlpha = Mathf.Max(_currentAlpha, intensity);
        }

        private void Update()
        {
            if (_currentAlpha > 0f)
            {
                _currentAlpha = Mathf.MoveTowards(_currentAlpha, 0f, Time.deltaTime * _fadeSpeed);
            }
        }

        // IMGUI(OnGUI) 대신 UGUI RawImage 로 렌더링: 알파가 바뀔 때만 갱신, 투명하면 Canvas 비활성
        private void LateUpdate()
        {
            if (_vignetteImage == null || _vignetteTex == null) return;

            bool shouldShow = _currentAlpha > 0.002f;
            if (shouldShow != IsVisible) SetVisible(shouldShow);
            if (!shouldShow) { _appliedAlpha = -1f; return; }

            if (!Mathf.Approximately(_appliedAlpha, _currentAlpha))
            {
                _appliedAlpha = _currentAlpha;
                _vignetteImage.color = new Color(1f, 1f, 1f, _currentAlpha);
            }
        }

        /// <summary>
        /// 중심부는 투명하고 외곽 가장자리로 갈수록 짙어지는 고해상도 붉은 비네트 텍스처 절차적 생성
        /// </summary>
        private void GenerateProceduralVignetteTexture()
        {
            int size = 256;
            _vignetteTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            _vignetteTex.wrapMode = TextureWrapMode.Clamp;
            _vignetteTex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2(0.5f, 0.5f);

            for (int y = 0; y < size; y++)
            {
                float v = (y + 0.5f) / size;
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size;
                    Vector2 uv = new Vector2(u, v);

                    // 중심으로부터의 타원형 거리 (화면 비율 고려하여 수평/수직 약간 보정)
                    float dist = Vector2.Distance(uv, center) * 2.0f;

                    // 중심 0.45 안쪽은 투명, 외곽 1.15까지 매끄럽게 붉은색 보간
                    float alpha = Mathf.SmoothStep(0.40f, 1.10f, dist);

                    // 화면 최외곽 모서리 강조
                    float edgeDist = Mathf.Max(Mathf.Abs(u - 0.5f), Mathf.Abs(v - 0.5f)) * 2.0f;
                    float cornerAlpha = Mathf.SmoothStep(0.60f, 1.0f, edgeDist) * 0.4f;

                    float finalAlpha = Mathf.Clamp01(alpha + cornerAlpha);
                    pixels[y * size + x] = new Color(_vignetteColor.r, _vignetteColor.g, _vignetteColor.b, finalAlpha);
                }
            }

            _vignetteTex.SetPixels(pixels);
            _vignetteTex.Apply();
        }
    }
}
