using System.IO;
using MythOfGuotaji.UI;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace MythOfGuotaji.Editor
{
    /// <summary>
    /// 공격 데미지 카운터 프리팹 (Normal, Reflected, Critical, Base)을
    /// Assets/_MythOfGuotaji/Resources/Prefabs/GUI 및 Resources/GUI 에 생성/검증하는 에디터 툴
    /// </summary>
    public static class MythOfGuotajiDamageCounterPrefabGenerator
    {
        public const string PrefabsGuiDir = "Assets/_MythOfGuotaji/Resources/Prefabs/GUI";
        public const string GuiDir = "Assets/_MythOfGuotaji/Resources/GUI";

        [InitializeOnLoadMethod]
        private static void AutoGenerateIfMissing()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists($"{PrefabsGuiDir}/AttackDamageCounter.prefab") ||
                    !File.Exists($"{PrefabsGuiDir}/AttackDamageCounter_Normal.prefab") ||
                    !File.Exists($"{PrefabsGuiDir}/AttackDamageCounter_Reflected.prefab") ||
                    !File.Exists($"{PrefabsGuiDir}/AttackDamageCounter_Critical.prefab"))
                {
                    GenerateAllDamageCounterPrefabs();
                }
            };
        }

        [MenuItem("MythOfGuotaji/Generate Damage Counter Prefabs", priority = 6)]
        public static void GenerateAllDamageCounterPrefabs()
        {
            EnsureDirectories();

            // 1. 개별 프리팹 생성 (Normal, Reflected, Critical)
            CreateOrUpdateCounterPrefab("AttackDamageCounter_Normal", DamageCounterType.Normal, Color.white, 30f);
            CreateOrUpdateCounterPrefab("AttackDamageCounter_Reflected", DamageCounterType.Reflected, new Color(1f, 0.92f, 0.016f), 35f);
            CreateOrUpdateCounterPrefab("AttackDamageCounter_Critical", DamageCounterType.Critical, new Color(1f, 0.25f, 0.25f), 45f);

            // 2. 기본 프리팹 생성 (AttackDamageCounter)
            CreateOrUpdateCounterPrefab("AttackDamageCounter", DamageCounterType.Normal, Color.white, 30f);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=green><b>[MythOfGuotaji] 데미지 카운터 프리팹 생성 및 갱신이 완료되었습니다!</b></color>");
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists(PrefabsGuiDir))
            {
                Directory.CreateDirectory(PrefabsGuiDir);
            }
            if (!Directory.Exists(GuiDir))
            {
                Directory.CreateDirectory(GuiDir);
            }
        }

        private static void CreateOrUpdateCounterPrefab(string prefabName, DamageCounterType counterType, Color textColor, float fontSize)
        {
            // 대상 경로들 (Prefabs/GUI 및 GUI 모두 생성/갱신)
            string path1 = $"{PrefabsGuiDir}/{prefabName}.prefab";
            string path2 = $"{GuiDir}/{prefabName}.prefab";

            GameObject tempGo = new GameObject(prefabName);

            try
            {
                // 1. Root Canvas 설정 (월드 스페이스)
                var rt = tempGo.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(250f, 70f);
                tempGo.transform.localScale = Vector3.one * 0.02f;

                var canvas = tempGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;

                var canvasGroup = tempGo.AddComponent<CanvasGroup>();
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;

                // 2. Text (TMP) 자식 오브젝트 생성
                var textGo = new GameObject("Text (TMP)");
                textGo.transform.SetParent(tempGo.transform, false);

                var textRt = textGo.AddComponent<RectTransform>();
                textRt.anchorMin = Vector2.zero;
                textRt.anchorMax = Vector2.one;
                textRt.sizeDelta = Vector2.zero;
                textRt.anchoredPosition = Vector2.zero;

                textGo.AddComponent<CanvasRenderer>();
                var tmp = textGo.AddComponent<TextMeshProUGUI>();

                if (TMP_Settings.defaultFontAsset != null)
                {
                    tmp.font = TMP_Settings.defaultFontAsset;
                }

                tmp.text = "100";
                tmp.fontSize = fontSize;
                tmp.color = textColor;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.textWrappingMode = TextWrappingModes.NoWrap;
                tmp.outlineColor = Color.black;
                tmp.outlineWidth = (counterType == DamageCounterType.Critical) ? 0.25f : 0.2f;

                // 3. AttackDamageCounter 컴포넌트 추가 및 직렬화 필드 설정
                var counterComp = tempGo.AddComponent<AttackDamageCounter>();
                var so = new SerializedObject(counterComp);
                so.FindProperty("_textMesh").objectReferenceValue = tmp;
                so.FindProperty("_canvasGroup").objectReferenceValue = canvasGroup;
                so.FindProperty("_counterType").enumValueIndex = (int)counterType;
                so.FindProperty("_riseDistance").floatValue = 1.0f;
                so.FindProperty("_duration").floatValue = 3.0f;
                so.ApplyModifiedPropertiesWithoutUndo();

                // 4. 프리팹 에셋으로 저장
                PrefabUtility.SaveAsPrefabAsset(tempGo, path1);
                PrefabUtility.SaveAsPrefabAsset(tempGo, path2);

                Debug.Log($"<color=cyan>[DamageCounter] 프리팹 생성 완료: {path1}, {path2} (Type: {counterType}, Size: {fontSize})</color>");
            }
            finally
            {
                Object.DestroyImmediate(tempGo);
            }
        }
    }
}
