using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace MythOfGuotaji.UI.Framework
{
    /// <summary>
    /// 씬에 EventSystem 이 정확히 하나, 그리고 New Input System 용 InputSystemUIInputModule 을 쓰도록 보장.
    /// (프로젝트는 레거시 Input 을 쓰지 않으므로 StandaloneInputModule 은 제거한다.)
    /// BoonSelectionUI / DungeonBootstrapper / PlayMainSceneController 의 기존 EnsureEventSystem 과 동일한 규칙.
    /// </summary>
    public static class UIEventSystem
    {
        public static EventSystem Ensure()
        {
            var es = Object.FindAnyObjectByType<EventSystem>();
            if (es == null)
            {
                var go = new GameObject("[EventSystem]");
                es = go.AddComponent<EventSystem>();
                go.AddComponent<InputSystemUIInputModule>();
                return es;
            }

            var standalone = es.GetComponent<StandaloneInputModule>();
            if (standalone != null)
            {
                standalone.enabled = false;
                Object.DestroyImmediate(standalone);
            }
            if (es.GetComponent<InputSystemUIInputModule>() == null)
                es.gameObject.AddComponent<InputSystemUIInputModule>();
            return es;
        }

        /// <summary>키보드/게임패드 네비게이션용: 아무것도 선택되지 않았으면 첫 Selectable 을 선택.</summary>
        public static void SelectFirst(UnityEngine.UI.Selectable first)
        {
            var es = EventSystem.current;
            if (es == null || first == null || !first.gameObject.activeInHierarchy) return;
            es.SetSelectedGameObject(first.gameObject);
        }
    }
}
