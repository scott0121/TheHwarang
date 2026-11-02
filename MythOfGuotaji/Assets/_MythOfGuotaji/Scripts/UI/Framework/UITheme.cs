using UnityEngine;

namespace MythOfGuotaji.UI.Framework
{
    /// <summary>
    /// UI 룩앤필 중앙 관리 (색상/크기/정렬순서 상수). ScriptableObject 없이 정적 상수로 유지.
    /// 화면 별 하드코딩 색을 줄이고 여기서 한 번에 바꿀 수 있게 한다.
    /// </summary>
    public static class UITheme
    {
        // ── Canvas 설정 ───────────────────────────────────────────
        public static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);
        public const float MatchWidthOrHeight = 0.5f;

        /// <summary>Canvas 정렬 순서 규약 (값이 클수록 전면).</summary>
        public static class Sorting
        {
            public const int WorldOverlay = 10;   // 몬스터 HP 바, 차지 인디케이터
            public const int Hud = 60;
            public const int HUD = 60;
            public const int Popup = 100;            // 플레이어 HP 바
            public const int BossBar = 65;
            public const int Modal = 100;         // 축복 선택, 스테이지 결과
            public const int Outgame = 120;       // 타이틀/아웃게임/시나리오
            public const int Vignette = 200;      // 피격 비네트
            public const int Debug = 500;         // 디버그 패널
        }

        // ── 색상 ──────────────────────────────────────────────────
        public static readonly Color TextPrimary = new Color(0.95f, 0.96f, 0.98f, 1f);
        public static readonly Color TextSecondary = new Color(0.78f, 0.82f, 0.9f, 1f);
        public static readonly Color TextMuted = new Color(0.62f, 0.66f, 0.72f, 1f);
        public static readonly Color Gold = new Color(1f, 0.88f, 0.45f, 1f);
        public static readonly Color Danger = new Color(1f, 0.35f, 0.35f, 1f);
        public static readonly Color Success = new Color(0.25f, 0.9f, 0.55f, 1f);

        public static readonly Color Dim = new Color(0f, 0f, 0f, 0.72f);
        public static readonly Color PanelDark = new Color(0.06f, 0.08f, 0.12f, 0.94f);
        public static readonly Color PanelDebug = new Color(0.04f, 0.05f, 0.08f, 0.88f);

        public static readonly Color BtnDark = new Color(0.14f, 0.2f, 0.3f, 0.95f);
        public static readonly Color White = Color.white;
        public static readonly Color ButtonNormal = new Color(0.14f, 0.2f, 0.3f, 0.95f);
        public static readonly Color ButtonHighlight = new Color(0.24f, 0.36f, 0.52f, 1f);
        public static readonly Color ButtonPressed = new Color(0.1f, 0.14f, 0.22f, 1f);
        public static readonly Color ButtonSelected = new Color(0.3f, 0.45f, 0.65f, 1f);
        public static readonly Color ButtonDisabled = new Color(0.2f, 0.2f, 0.22f, 0.5f);

        // ── 글자 크기 (기준 해상도 1920x1080 기준 px) ─────────────
        public const float FontTitle = 34f;
        public const float FontHeading = 24f;
        public const float FontBody = 18f;
        public const float FontSmall = 14f;
        public const float FontDebug = 14f;
    }
}
