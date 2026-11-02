using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MythOfGuotaji.UI.Framework
{
    /// <summary>
    /// 레이아웃 그룹/스크롤/입력창 빌더 (디버그 패널, 목록형 화면용).
    /// UIKit 이 "단일 요소" 팩토리라면 UILayout 은 "자동 배치 컨테이너" 팩토리.
    /// </summary>
    public static class UILayout
    {
        public static VerticalLayoutGroup CreateVStack(Transform parent, string name, float spacing = 4f, RectOffset padding = null,
            TextAnchor childAlign = TextAnchor.UpperLeft, bool expandWidth = true)
        {
            var rt = UIKit.CreateRect(name, parent);
            var g = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            g.spacing = spacing;
            g.padding = padding ?? new RectOffset(0, 0, 0, 0);
            g.childAlignment = childAlign;
            g.childControlWidth = true;
            g.childControlHeight = true;
            g.childForceExpandWidth = expandWidth;
            g.childForceExpandHeight = false;
            return g;
        }

        public static HorizontalLayoutGroup CreateHStack(Transform parent, string name, float spacing = 4f, RectOffset padding = null,
            TextAnchor childAlign = TextAnchor.MiddleLeft, bool expandHeight = true)
        {
            var rt = UIKit.CreateRect(name, parent);
            var g = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            g.spacing = spacing;
            g.padding = padding ?? new RectOffset(0, 0, 0, 0);
            g.childAlignment = childAlign;
            g.childControlWidth = true;
            g.childControlHeight = true;
            g.childForceExpandWidth = true;
            g.childForceExpandHeight = expandHeight;
            return g;
        }

        /// <summary>레이아웃 그룹 자식의 크기 힌트 (값이 음수면 무시). flexible 은 남는 공간 비율.</summary>
        public static LayoutElement SetLayout(Component c, float preferredWidth = -1f, float preferredHeight = -1f,
            float flexibleWidth = -1f, float flexibleHeight = -1f)
        {
            var le = c.GetComponent<LayoutElement>();
            if (le == null) le = c.gameObject.AddComponent<LayoutElement>();
            if (preferredWidth >= 0f) { le.preferredWidth = preferredWidth; le.minWidth = preferredWidth; }
            if (preferredHeight >= 0f) { le.preferredHeight = preferredHeight; le.minHeight = preferredHeight; }
            if (flexibleWidth >= 0f) le.flexibleWidth = flexibleWidth;
            if (flexibleHeight >= 0f) le.flexibleHeight = flexibleHeight;
            return le;
        }

        /// <summary>세로 스페이서 (VStack 용).</summary>
        public static void AddSpace(Transform parent, float height)
        {
            var rt = UIKit.CreateRect("Space", parent);
            SetLayout(rt, preferredHeight: height);
        }

        /// <summary>컨텐츠 크기에 맞춰 세로로 늘어나는 ContentSizeFitter 추가.</summary>
        public static void FitVertical(Component c)
        {
            var f = c.GetComponent<ContentSizeFitter>();
            if (f == null) f = c.gameObject.AddComponent<ContentSizeFitter>();
            f.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            f.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        }

        /// <summary>세로 스크롤 뷰. content 는 VerticalLayoutGroup + ContentSizeFitter 를 가진다.</summary>
        public static ScrollRect CreateScrollView(Transform parent, string name, out RectTransform content,
            float spacing = 4f, Color? bg = null)
        {
            var bgImg = UIKit.CreateImage(parent, name, bg ?? new Color(0f, 0f, 0f, 0.25f), null, true);
            var scroll = bgImg.gameObject.AddComponent<ScrollRect>();

            var viewport = UIKit.CreateRect("Viewport", bgImg.transform);
            UIKit.Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();

            var vg = CreateVStack(viewport, "Content", spacing);
            content = (RectTransform)vg.transform;
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            FitVertical(content);

            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;
            return scroll;
        }

        /// <summary>TMP_InputField (한 줄). 변경 시 onChanged 호출.</summary>
        public static TMP_InputField CreateInputField(Transform parent, string name, string placeholder,
            UnityAction<string> onChanged, float fontSize = UITheme.FontSmall)
        {
            var bg = UIKit.CreateImage(parent, name, new Color(0.1f, 0.12f, 0.18f, 0.95f), null, true);
            var input = bg.gameObject.AddComponent<TMP_InputField>();

            var area = UIKit.CreateRect("TextArea", bg.transform);
            UIKit.Stretch(area, 8f, 8f, 2f, 2f);
            area.gameObject.AddComponent<RectMask2D>();

            var ph = UIKit.CreateLabel(area, "Placeholder", placeholder, fontSize, UITheme.TextMuted,
                TextAlignmentOptions.MidlineLeft, FontStyles.Italic, false, false);
            UIKit.Stretch(ph.rectTransform);
            var txt = UIKit.CreateLabel(area, "Text", "", fontSize, UITheme.TextPrimary,
                TextAlignmentOptions.MidlineLeft, FontStyles.Normal, false, false);
            UIKit.Stretch(txt.rectTransform);

            input.textViewport = area;
            input.textComponent = txt;
            input.placeholder = ph;
            input.fontAsset = txt.font;
            input.pointSize = fontSize;
            input.lineType = TMP_InputField.LineType.SingleLine;
            if (onChanged != null) input.onValueChanged.AddListener(onChanged);
            return input;
        }
    }
}
