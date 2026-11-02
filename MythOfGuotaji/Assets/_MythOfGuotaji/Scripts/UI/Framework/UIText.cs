using System.Collections.Generic;

namespace MythOfGuotaji.UI.Framework
{
    /// <summary>
    /// 사용자에게 보이는 모든 문자열의 단일 진입점 (현지화 대비 간접 계층).
    /// 사용법: UIText.Get("stage.clear.title", "★ STAGE CLEAR ★")
    /// - 키가 Override 테이블에 있으면 그 값을, 없으면 코드에 적어 둔 한국어 기본값을 반환한다.
    /// - 추후 CSV/JSON/Localization 패키지 연동 시 Override 채우는 부분만 교체하면 된다.
    /// </summary>
    public static class UIText
    {
        private static readonly Dictionary<string, string> _overrides = new Dictionary<string, string>();

        public static string Get(string key, string koreanFallback)
        {
            if (!string.IsNullOrEmpty(key) && _overrides.TryGetValue(key, out var v)) return v;
            return koreanFallback;
        }

        /// <summary>string.Format 형태의 포맷 문자열 지원 (인자 포함 키).</summary>
        public static string Format(string key, string koreanFallbackFormat, params object[] args)
        {
            return string.Format(Get(key, koreanFallbackFormat), args);
        }

        public static void SetOverride(string key, string value)
        {
            if (!string.IsNullOrEmpty(key)) _overrides[key] = value;
        }

        public static void ClearOverrides() => _overrides.Clear();
    }
}
