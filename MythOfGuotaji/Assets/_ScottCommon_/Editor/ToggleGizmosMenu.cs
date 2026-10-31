using UnityEditor;

public static class ToggleGizmosMenu
{
    private const string MenuPath = "Tools/Toggle All Gizmos #`";

    [MenuItem(MenuPath)]
    public static void ToggleAllGizmos()
    {
        // SceneView가 없는 경우 가장 최근 활성화된 SceneView를 가져옴
        SceneView view = SceneView.lastActiveSceneView;
        if (view == null && SceneView.sceneViews.Count > 0)
        {
            view = (SceneView)SceneView.sceneViews[0];
        }

        if (view != null)
        {
            // Scene 뷰 우측 상단 기즈모 버튼의 전체 활성화 여부를 토글
            view.drawGizmos = !view.drawGizmos;
            view.Repaint();
        }
    }

    // 메뉴 항목 옆에 현재 활성화 상태(체크마크)를 동기화 표시
    [MenuItem(MenuPath, true)]
    public static bool ToggleAllGizmosValidate()
    {
        SceneView view = SceneView.lastActiveSceneView;
        if (view == null && SceneView.sceneViews.Count > 0)
        {
            view = (SceneView)SceneView.sceneViews[0];
        }

        if (view != null)
        {
            Menu.SetChecked(MenuPath, view.drawGizmos);
        }

        return true;
    }
}