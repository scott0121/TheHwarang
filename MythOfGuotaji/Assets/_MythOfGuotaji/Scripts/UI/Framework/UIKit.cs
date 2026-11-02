using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MythOfGuotaji.UI.Framework
{
    /// <summary>
    /// 코드로 UGUI + TextMeshPro 계층을 만드는 정적 팩토리/빌더 모음.
    /// - 모든 라벨은 UIFontProvider 의 한글 지원 폰트를 사용하고 raycastTarget=false 가 기본값
    /// - 버튼은 Selectable 네비게이션(키보드/게임패드)과 마우스 모두 지원
    /// - 흰색 스프라이트는 전역 1개를 공유 (PlayerHealthBarUI.WhiteSprite 와 동일 방식)
    /// </summary>
    public static class UIKit
    {
        // ── White sprite ──────────────────────────────────────────
        private static Sprite _whiteSprite;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { _whiteSprite = null; }

        public static Sprite WhiteSprite
        {
            get
            {
                if (_whiteSprite == null)
                {
                    var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
                    tex.Apply();
                    tex.name = "UIKit_White";
                    _whiteSprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
                    _whiteSprite.name = "UIKit_WhiteSprite";
                }
                return _whiteSprite;
            }
        }

        // ── Canvas / Rect ─────────────────────────────────────────
        /// <summary>ScreenSpaceOverlay + CanvasScaler(1920x1080, match 0.5) Canvas 생성.</summary>
        public static Canvas CreateCanvas(string name, int sortingOrder, Transform parent = null, bool graphicRaycaster = true)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            if (parent != null) go.transform.SetParent(parent, false);

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = UITheme.ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = UITheme.MatchWidthOrHeight;

            if (graphicRaycaster) go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>부모를 꽉 채우도록 앵커/오프셋 설정 (여백 지정 가능).</summary>
        public static RectTransform Stretch(RectTransform rt, float left = 0f, float right = 0f, float top = 0f, float bottom = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        /// <summary>앵커 점 기준 고정 크기 배치.</summary>
        public static RectTransform SetAnchored(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 anchoredPos, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            return rt;
        }

        /// <summary>가로로 늘어나고 세로는 고정 높이인 띠 배치 (anchorY: 0=하단,1=상단).</summary>
        public static RectTransform SetStretchX(RectTransform rt, float anchorY, float posY, float height, float padLeft = 0f, float padRight = 0f)
        {
            rt.anchorMin = new Vector2(0f, anchorY);
            rt.anchorMax = new Vector2(1f, anchorY);
            rt.pivot = new Vector2(0.5f, anchorY);
            rt.offsetMin = new Vector2(padLeft, 0f);
            rt.offsetMax = new Vector2(-padRight, 0f);
            rt.anchoredPosition = new Vector2((padLeft - padRight) * 0.5f, posY);
            rt.sizeDelta = new Vector2(-(padLeft + padRight), height);
            return rt;
        }

        // ── Image / Panel ─────────────────────────────────────────
        public static Image CreateImage(Transform parent, string name, Color color, Sprite sprite = null, bool raycastTarget = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = sprite != null ? sprite : WhiteSprite;
            img.color = color;
            img.raycastTarget = raycastTarget;
            return img;
        }

        /// <summary>단색 패널. stretch=true 면 부모 전체를 채움.</summary>
        public static Image CreatePanel(Transform parent, string name, Color color, bool stretch = false, bool raycastTarget = false)
        {
            var img = CreateImage(parent, name, color, null, raycastTarget);
            if (stretch) Stretch(img.rectTransform);
            return img;
        }

        /// <summary>외곽선 프레임(4변 이미지)을 부모 안쪽 가장자리에 만든다. 반환: 프레임 루트.</summary>
        public static RectTransform CreateBorder(Transform parent, string name, Color color, float thickness, float outset = 0f)
        {
            var root = CreateRect(name, parent);
            Stretch(root, -outset, -outset, -outset, -outset);
            var top = CreateImage(root, "T", color); SetStretchX(top.rectTransform, 1f, 0f, thickness);
            var bot = CreateImage(root, "B", color); SetStretchX(bot.rectTransform, 0f, 0f, thickness);
            var left = CreateImage(root, "L", color);
            left.rectTransform.anchorMin = Vector2.zero; left.rectTransform.anchorMax = new Vector2(0f, 1f);
            left.rectTransform.pivot = new Vector2(0f, 0.5f); left.rectTransform.sizeDelta = new Vector2(thickness, 0f);
            left.rectTransform.anchoredPosition = Vector2.zero;
            var right = CreateImage(root, "R", color);
            right.rectTransform.anchorMin = new Vector2(1f, 0f); right.rectTransform.anchorMax = Vector2.one;
            right.rectTransform.pivot = new Vector2(1f, 0.5f); right.rectTransform.sizeDelta = new Vector2(thickness, 0f);
            right.rectTransform.anchoredPosition = Vector2.zero;
            return root;
        }

        /// <summary>색상 일괄 변경용 (테두리 4변).</summary>
        public static void SetBorderColor(RectTransform border, Color c)
        {
            if (border == null) return;
            foreach (var g in border.GetComponentsInChildren<Image>(true)) g.color = c;
        }

        // ── Label (TMP) ───────────────────────────────────────────
        public static TextMeshProUGUI CreateLabel(Transform parent, string name, string text, float fontSize,
            Color color, TextAlignmentOptions align = TextAlignmentOptions.Center,
            FontStyles style = FontStyles.Normal, bool raycastTarget = false, bool wrap = true)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<TextMeshProUGUI>();
            ApplyLabelStyle(t, fontSize, color, align, style, wrap);
            t.raycastTarget = raycastTarget;
            t.text = text;
            return t;
        }

        public static void ApplyLabelStyle(TMP_Text t, float fontSize, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.Center, FontStyles style = FontStyles.Normal, bool wrap = true)
        {
            var font = UIFontProvider.Font;
            if (font != null) t.font = font;
            t.fontSize = fontSize;
            t.color = color;
            t.alignment = align;
            t.fontStyle = style;
            t.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.richText = true;
        }

        /// <summary>스크롤 없이 한 줄 안에서 줄어드는 라벨(오토사이즈) 옵션 켜기.</summary>
        public static void EnableAutoSize(TMP_Text t, float min, float max)
        {
            t.enableAutoSizing = true;
            t.fontSizeMin = min;
            t.fontSizeMax = max;
        }

        // ── Button ────────────────────────────────────────────────
        /// <summary>
        /// Image + TMP 라벨 버튼. 마우스 클릭과 Selectable 네비게이션(키보드/게임패드 + Submit)을 모두 지원한다.
        /// 크기/위치는 반환된 Button.GetComponent&lt;RectTransform&gt;() 로 지정.
        /// </summary>
        public static Button CreateButton(Transform parent, string name, string label, UnityAction onClick,
            float fontSize = UITheme.FontBody, Color? normal = null, Color? textColor = null)
        {
            Color baseColor = normal ?? UITheme.ButtonNormal;
            var img = CreateImage(parent, name, Color.white, null, true);

            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.ColorTint;
            btn.colors = MakeColors(baseColor);
            var nav = new Navigation { mode = Navigation.Mode.Automatic };
            btn.navigation = nav;
            if (onClick != null) btn.onClick.AddListener(onClick);

            var lbl = CreateLabel(img.transform, "Label", label, fontSize, textColor ?? UITheme.TextPrimary,
                TextAlignmentOptions.Center, FontStyles.Bold);
            Stretch(lbl.rectTransform, 6f, 6f, 2f, 2f);
            return btn;
        }

        /// <summary>버튼의 TMP 라벨 (CreateButton 으로 만든 경우).</summary>
        public static TMP_Text GetButtonLabel(Button b) => b != null ? b.GetComponentInChildren<TMP_Text>(true) : null;

        public static void SetButtonColor(Button b, Color baseColor)
        {
            if (b == null) return;
            b.colors = MakeColors(baseColor);
        }

        // targetGraphic 은 흰색으로 두고 ColorBlock 이 실제 색을 결정 (밝기 변화가 정확하게 보이도록)
        private static ColorBlock MakeColors(Color baseColor)
        {
            var cb = ColorBlock.defaultColorBlock;
            cb.normalColor = baseColor;
            cb.highlightedColor = Color.Lerp(baseColor, Color.white, 0.25f);
            cb.pressedColor = new Color(baseColor.r * 0.7f, baseColor.g * 0.7f, baseColor.b * 0.7f, baseColor.a);
            cb.selectedColor = Color.Lerp(baseColor, Color.white, 0.35f);
            cb.disabledColor = new Color(baseColor.r * 0.5f, baseColor.g * 0.5f, baseColor.b * 0.5f, baseColor.a * 0.5f);
            cb.colorMultiplier = 1f;
            cb.fadeDuration = 0.08f;
            return cb;
        }
        // ── Bar ───────────────────────────────────────────────────
        /// <summary>배경 + 앵커 기반 채움 이미지 한 쌍. fill 은 SetFill 로 비율 조작.</summary>
        public static Image CreateBar(Transform parent, string name, Color bgColor, Color fillColor, out Image fill)
        {
            var bg = CreateImage(parent, name, bgColor);
            fill = CreateImage(bg.transform, "Fill", fillColor);
            Stretch(fill.rectTransform);
            return bg;
        }

        /// <summary>왼쪽 기준 가로 채움 비율 설정 (앵커 방식: 스프라이트와 무관하게 정확).</summary>
        public static void SetFill(Image fill, float ratio)
        {
            if (fill == null) return;
            ratio = Mathf.Clamp01(ratio);
            var rt = fill.rectTransform;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(ratio, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // ── Screen -> Canvas ──────────────────────────────────────
        /// <summary>
        /// 화면 픽셀 좌표(좌하단 원점, WorldToScreenPoint 결과)를 ScaleWithScreenSize Overlay 캔버스의
        /// "중앙 앵커(0.5,0.5) 기준 anchoredPosition" 으로 변환. 할당/호출 비용 최소화를 위해 scaleFactor 를 직접 사용.
        /// </summary>
        public static Vector2 ScreenToCanvasCenter(Canvas canvas, Vector2 screenPoint)
        {
            float f = canvas != null && canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;
            return new Vector2((screenPoint.x - Screen.width * 0.5f) / f, (screenPoint.y - Screen.height * 0.5f) / f);
        }

        /// <summary>현재 화면 높이 기준 캔버스 단위 크기 (화면 크기 / scaleFactor).</summary>
        public static Vector2 CanvasSize(Canvas canvas)
        {
            float f = canvas != null && canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;
            return new Vector2(Screen.width / f, Screen.height / f);
        }
    }
}
