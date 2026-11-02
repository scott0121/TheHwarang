using UnityEngine;

namespace MythOfGuotaji.UI.Framework
{
    /// <summary>
    /// 코드로 UGUI 계층을 빌드하는 화면(스크린)의 공통 베이스.
    /// - EnsureBuilt() 최초 호출 시 자식 Canvas 를 만들고 Build(root) 를 한 번만 실행
    /// - Show()/Hide()/IsVisible 로 표시 제어 (Canvas GameObject 활성화 토글)
    /// - 이 컴포넌트가 붙은 GameObject 자체는 항상 활성 → Update/코루틴은 계속 동작
    /// 사용법은 _Docs_/07_UGUI_TMP_UI_Architecture.md 참고.
    /// </summary>
    public abstract class UIScreen : MonoBehaviour
    {
        private Canvas _canvas;
        private RectTransform _root;
        private bool _built;
        private bool _visible;

        /// <summary>Canvas 정렬 순서 (UITheme.Sorting 값 사용 권장).</summary>
        protected virtual int SortingOrder => UITheme.Sorting.Modal;

        /// <summary>Canvas 가 GraphicRaycaster 를 가질지 (버튼이 없으면 false 로 비용 절감).</summary>
        protected virtual bool NeedsRaycaster => true;

        /// <summary>빌드 직후 보이는 상태로 시작할지.</summary>
        protected virtual bool StartVisible => false;

        /// <summary>true 면 빌드 시 EventSystem(InputSystemUIInputModule)을 보장.</summary>
        protected virtual bool NeedsEventSystem => true;

        public bool IsVisible => _visible;
        public Canvas Canvas { get { EnsureBuilt(); return _canvas; } }
        public RectTransform Root { get { EnsureBuilt(); return _root; } }

        /// <summary>화면 구성 요소 생성. root 는 Canvas 전체를 채우는 RectTransform.</summary>
        protected abstract void Build(RectTransform root);

        protected virtual void OnShown() { }
        protected virtual void OnHidden() { }

        /// <summary>최초 1회 Canvas/UI 빌드. 여러 번 호출해도 안전.</summary>
        protected void EnsureBuilt()
        {
            if (_built) return;
            _built = true;

            if (NeedsEventSystem) UIEventSystem.Ensure();

            _canvas = UIKit.CreateCanvas(GetType().Name + "_Canvas", SortingOrder, transform, NeedsRaycaster);
            _root = UIKit.CreateRect("Root", _canvas.transform);
            UIKit.Stretch(_root);

            Build(_root);

            _visible = StartVisible;
            _canvas.gameObject.SetActive(_visible);
        }

        public void Show()
        {
            EnsureBuilt();
            if (_visible) return;
            _visible = true;
            _canvas.gameObject.SetActive(true);
            OnShown();
        }

        public void Hide()
        {
            EnsureBuilt();
            if (!_visible) return;
            _visible = false;
            _canvas.gameObject.SetActive(false);
            OnHidden();
        }

        public void SetVisible(bool visible)
        {
            if (visible) Show(); else Hide();
        }
    }
}
