using System.Collections.Generic;
using System.IO;
using MythOfGuotaji.UI;
using UnityEditor;
using UnityEngine;

namespace MythOfGuotaji.Editor
{
    /// <summary>
    /// UGUI 기반의 비술(Boon) 선택 모달 창 및 GUI 프리팹들을
    /// Assets/_MythOfGuotaji/Resources/Prefabs/GUI/ 에서 검색하여 PrefabDatabase에 연결하는 에디터 툴
    /// (사용자가 수정한 프리팹을 덮어쓰지 않고, 기존 프리팹을 그대로 사용하며 누락 시 경고만 출력)
    /// </summary>
    public static class MythOfGuotajiGUIPrefabGenerator
    {
        public const string GuiPrefabsDir = "Assets/_MythOfGuotaji/Resources/Prefabs/GUI";

        [InitializeOnLoadMethod]
        private static void AutoVerifyIfMissing()
        {
            EditorApplication.delayCall += () =>
            {
                VerifyAndLinkGuiPrefabs(silent: true);
            };
        }

        [MenuItem("MythOfGuotaji/Link GUI Prefabs to Database", priority = 5)]
        public static void ManualLinkGuiPrefabs()
        {
            EnsureDirectory();
            VerifyAndLinkGuiPrefabs(silent: false);
            Debug.Log("<color=green>[MythOfGuotaji] GUI 프리팹 PrefabDatabase 연결 확인이 완료되었습니다!</color>");
        }

        public static void GenerateAllGUIPrefabs()
        {
            // 하위 호환성 유지: 기존 프리팹을 덮어쓰지 않고 검색 및 데이터베이스 연결만 수행
            VerifyAndLinkGuiPrefabs(silent: false);
        }

        public static void VerifyAndLinkGuiPrefabs(bool silent)
        {
            EnsureDirectory();

            CheckPrefabExists("PlayerHealthBarUI");
            CheckPrefabExists("BoonSelectionUI");
            CheckPrefabExists("BossHealthBarUI");
            CheckPrefabExists("MonsterHealthBarManager");
            CheckPrefabExists("StageClearUI");
            CheckPrefabExists("ProgressionDebugUI");
            CheckPrefabExists("ScreenDamageVignetteUI");
            CheckPrefabExists("AimCursorManager");
            CheckPrefabExists("IntroOutgameUI");
            CheckPrefabExists("IngameSystemManagers");

            LinkGuiPrefabsToDatabase();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static GameObject CheckPrefabExists(string prefabName)
        {
            string prefabPath = $"{GuiPrefabsDir}/{prefabName}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existing == null)
            {
                Debug.LogWarning($"<color=yellow>[MythOfGuotaji] '{prefabPath}' 프리팹을 찾을 수 없습니다. 프로젝트 뷰에서 프리팹을 준비해주세요.</color>");
            }
            return existing;
        }

        private static void LinkGuiPrefabsToDatabase()
        {
            var db = AssetDatabase.LoadAssetAtPath<Data.PrefabDatabaseSO>("Assets/_MythOfGuotaji/Resources/Data/PrefabDatabase.asset");
            if (db == null) return;

            db.playerHealthBarUIPrefab = LoadGuiPrefab("PlayerHealthBarUI");
            db.boonSelectionUIPrefab = LoadGuiPrefab("BoonSelectionUI");
            db.bossHealthBarUIPrefab = LoadGuiPrefab("BossHealthBarUI");
            db.monsterHealthBarManagerPrefab = LoadGuiPrefab("MonsterHealthBarManager");
            db.stageClearUIPrefab = LoadGuiPrefab("StageClearUI");
            db.progressionDebugUIPrefab = LoadGuiPrefab("ProgressionDebugUI");
            db.screenDamageVignetteUIPrefab = LoadGuiPrefab("ScreenDamageVignetteUI");
            db.aimCursorManagerPrefab = LoadGuiPrefab("AimCursorManager");
            db.introOutgameUIPrefab = LoadGuiPrefab("IntroOutgameUI");
            db.ingameSystemManagersPrefab = LoadGuiPrefab("IngameSystemManagers");

            EditorUtility.SetDirty(db);
        }

        private static GameObject LoadGuiPrefab(string prefabName)
        {
            string path = $"{GuiPrefabsDir}/{prefabName}.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning($"<color=yellow>[MythOfGuotaji] GUI 프리팹 누락: '{path}' 을(를) 찾을 수 없습니다.</color>");
            }
            return prefab;
        }

        private static void EnsureDirectory()
        {
            if (!Directory.Exists(GuiPrefabsDir))
            {
                Directory.CreateDirectory(GuiPrefabsDir);
                AssetDatabase.Refresh();
            }
        }
    }
}
