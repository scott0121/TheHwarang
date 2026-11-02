using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace MythOfGuotaji.UI.Framework
{
    /// <summary>
    /// 한글 + 라틴을 렌더링할 수 있는 TMP 폰트 보장.
    /// - Resources/Fonts/ 의 Noto Sans KR (OFL) TTF 로 동적(Dynamic) TMP_FontAsset 을 런타임 생성
    /// - 생성한 폰트를 TMP 기본 폰트(LiberationSans SDF)의 Fallback 에도 등록하여
    ///   프리팹에 이미 박혀 있는 TMP 텍스트도 한글이 깨지지 않게 한다 (종료 시 원복 → 에셋 오염 방지)
    /// - 정적 에셋(Resources/Fonts/NotoSansKR-Static SDF)이 있으면 그것을 우선 사용 (Editor 메뉴로 베이크)
    /// </summary>
    public static class UIFontProvider
    {
        public const string FontResourceName = "Fonts/NotoSansKR-VF";
        public const string StaticAssetResourceName = "Fonts/NotoSansKR-Static SDF";

        private static TMP_FontAsset _font;
        private static bool _initialized;
        private static bool _registeredFallback;
        private static TMP_FontAsset _registeredTarget;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _font = null;
            _initialized = false;
            _registeredFallback = false;
            _registeredTarget = null;
        }

        /// <summary>한글 지원 TMP 폰트. 실패하면 TMP 기본 폰트(한글 미지원)를 반환 (null 일 수 있음).</summary>
        public static TMP_FontAsset Font
        {
            get
            {
                if (!_initialized) Initialize();
                return _font != null ? _font : (TMP_Settings.instance != null ? TMP_Settings.defaultFontAsset : null);
            }
        }

        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            // 1) 미리 베이크한 정적 에셋 우선
            _font = Resources.Load<TMP_FontAsset>(StaticAssetResourceName);

            // 2) 동적 에셋 런타임 생성
            if (_font == null)
            {
                var src = Resources.Load<Font>(FontResourceName);
                if (src == null)
                {
                    Debug.LogWarning($"[UIFontProvider] 한글 폰트를 찾지 못했습니다: Resources/{FontResourceName}.ttf — 한글이 □ 로 표시될 수 있습니다. (Noto Sans KR OFL 폰트를 해당 위치에 넣어 주세요)");
                }
                else
                {
                    _font = TMP_FontAsset.CreateFontAsset(
                        src, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024,
                        AtlasPopulationMode.Dynamic, true);
                    if (_font != null) _font.name = "NotoSansKR Dynamic SDF (runtime)";
                    else Debug.LogWarning("[UIFontProvider] TMP_FontAsset 동적 생성에 실패했습니다.");
                }
            }

            RegisterAsFallback();
        }

        private static void RegisterAsFallback()
        {
            if (_font == null || _registeredFallback) return;
            if (TMP_Settings.instance == null) return;

            var def = TMP_Settings.defaultFontAsset;
            if (def == null || def == _font) return;

            if (def.fallbackFontAssetTable == null) def.fallbackFontAssetTable = new List<TMP_FontAsset>();
            if (!def.fallbackFontAssetTable.Contains(_font))
            {
                def.fallbackFontAssetTable.Add(_font);
                _registeredTarget = def;
                _registeredFallback = true;
                Application.quitting += UnregisterFallback;
            }
        }

        private static void UnregisterFallback()
        {
            Application.quitting -= UnregisterFallback;
            if (_registeredTarget != null && _font != null && _registeredTarget.fallbackFontAssetTable != null)
                _registeredTarget.fallbackFontAssetTable.Remove(_font);
            _registeredFallback = false;
            _registeredTarget = null;
        }
    
        public static void ApplyTo(TMP_Text text)
        {
            if (text != null && Font != null) text.font = Font;
        }
}
}
