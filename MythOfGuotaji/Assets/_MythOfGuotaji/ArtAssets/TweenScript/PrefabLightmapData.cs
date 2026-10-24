using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class PrefabLightmapData : MonoBehaviour
{
    [Tooltip("Reassigns shaders when applying the baked lightmaps. Might conflict with some shaders like transparent HDRP.")]
    public bool releaseShaders = true;

    [System.Serializable]
    struct RendererInfo
    {
        public Renderer renderer;
        public int lightmapIndex;
        public Vector4 lightmapOffsetScale;
    }
    [System.Serializable]
    struct LightInfo
    {
        public Light light;
        public int lightmapBaketype;
        public int mixedLightingMode;
    }

    [SerializeField] RendererInfo[] m_RendererInfo;
    [SerializeField] Texture2D[] m_Lightmaps;
    [SerializeField] Texture2D[] m_LightmapsDir;
    [SerializeField] Texture2D[] m_ShadowMasks;
    [SerializeField] LightInfo[] m_LightInfo;

    void Awake()
    {
        Init();
    }

    void Init()
    {
        if (m_RendererInfo == null || m_RendererInfo.Length == 0) return;

        var lightmaps = LightmapSettings.lightmaps;
        int[] offsetsindexes = new int[m_Lightmaps.Length];
        int counttotal = lightmaps.Length;
        List<LightmapData> combinedLightmaps = new List<LightmapData>();

        for (int i = 0; i < m_Lightmaps.Length; i++)
        {
            bool exists = false;
            for (int j = 0; j < lightmaps.Length; j++)
            {
                if (m_Lightmaps[i] == lightmaps[j].lightmapColor)
                {
                    exists = true;
                    offsetsindexes[i] = j;
                }
            }
            if (!exists)
            {
                offsetsindexes[i] = counttotal;
                var newlightmapdata = new LightmapData
                {
                    lightmapColor = m_Lightmaps[i],
                    lightmapDir = m_LightmapsDir.Length == m_Lightmaps.Length ? m_LightmapsDir[i] : default(Texture2D),
                    shadowMask = m_ShadowMasks.Length == m_Lightmaps.Length ? m_ShadowMasks[i] : default(Texture2D),
                };
                combinedLightmaps.Add(newlightmapdata);
                counttotal += 1;
            }
        }

        var combinedLightmaps2 = new LightmapData[counttotal];
        lightmaps.CopyTo(combinedLightmaps2, 0);
        combinedLightmaps.ToArray().CopyTo(combinedLightmaps2, lightmaps.Length);

        bool directional = true;
        foreach (Texture2D t in m_LightmapsDir)
        {
            if (t == null) { directional = false; break; }
        }

        LightmapSettings.lightmapsMode = (m_LightmapsDir.Length == m_Lightmaps.Length && directional) ? LightmapsMode.CombinedDirectional : LightmapsMode.NonDirectional;
        ApplyRendererInfo(m_RendererInfo, offsetsindexes, m_LightInfo);
        LightmapSettings.lightmaps = combinedLightmaps2;
    }

    void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnSceneLoaded(Scene scene, LoadSceneMode mode) { Init(); }
    void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    void ApplyRendererInfo(RendererInfo[] infos, int[] lightmapOffsetIndex, LightInfo[] lightsInfo)
    {
        for (int i = 0; i < infos.Length; i++)
        {
            var info = infos[i];
            if (info.renderer == null) continue;

            info.renderer.lightmapIndex = lightmapOffsetIndex[info.lightmapIndex];
            info.renderer.lightmapScaleOffset = info.lightmapOffsetScale;

            if (releaseShaders)
            {
                Material[] mat = info.renderer.sharedMaterials;
                for (int j = 0; j < mat.Length; j++)
                {
                    if (mat[j] != null && mat[j].shader != null) mat[j].shader = mat[j].shader;
                }
            }
        }

        for (int i = 0; i < lightsInfo.Length; i++)
        {
            if (lightsInfo[i].light == null) continue;
            LightBakingOutput bakingOutput = new LightBakingOutput
            {
                isBaked = true,
                lightmapBakeType = (LightmapBakeType)lightsInfo[i].lightmapBaketype,
                mixedLightingMode = (MixedLightingMode)lightsInfo[i].mixedLightingMode
            };
            lightsInfo[i].light.bakingOutput = bakingOutput;
        }
    }

#if UNITY_EDITOR
    [MenuItem("Assets/Bake Prefab Lightmaps")]
    public static void BakeDataFromMenu()
    {
        PrefabLightmapData currentData = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponent<PrefabLightmapData>() : null;
        if (currentData != null) ExecuteBake(currentData);
        else Debug.LogError("선택된 오브젝트에 PrefabLightmapData 컴포넌트가 없습니다.");
    }

    public static void ExecuteBake(PrefabLightmapData currentData)
    {
        Undo.RegisterCompleteObjectUndo(currentData, "Bake Prefab Lightmaps");

        // =========================================================
        // [1단계] 저장될 폴더 경로 설정 (현재 씬이 저장된 폴더 기준)
        // =========================================================
        string baseFolderPath = "Assets";

        string currentScenePath = SceneManager.GetActiveScene().path;

        if (!string.IsNullOrEmpty(currentScenePath))
        {
            baseFolderPath = Path.GetDirectoryName(currentScenePath).Replace('\\', '/');
        }
        else
        {
            Debug.LogWarning("현재 씬이 저장되지 않았습니다. 기본 경로인 'Assets' 폴더에 라이트맵이 생성됩니다.");
        }

        string prefabName = currentData.gameObject.name;
        foreach (char c in Path.GetInvalidFileNameChars()) { prefabName = prefabName.Replace(c.ToString(), "_"); }

        string targetFolderPath = baseFolderPath + "/" + prefabName + "_Lightmaps";

        if (!Directory.Exists(targetFolderPath))
        {
            Directory.CreateDirectory(targetFolderPath);
            AssetDatabase.ImportAsset(targetFolderPath);
        }

        // =========================================================
        // [사전작업] 현재 씬의 전체 라이트를 찾아 Soft Shadow로 일괄 변경
        // =========================================================

        Light[] allLightsPreBake = FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Light light in allLightsPreBake)
        {
            Undo.RecordObject(light, "Change Shadow Type to Soft");
            light.shadows = LightShadows.Soft;
        }
        Debug.Log($"사전 작업: 씬 내의 총 {allLightsPreBake.Length}개 라이트의 Shadow Type을 'Soft Shadow'로 변경했습니다.");

        // =========================================================
        // [2단계] 라이트맵 베이킹 시작
        // =========================================================
        Debug.Log("라이트맵 베이킹을 시작합니다...");
        Lightmapping.Bake();

        // =========================================================
        // [3단계] 메인 스레드 딜레이 및 데이터베이스 동기화
        // =========================================================
        Debug.Log("베이킹 완료! 유니티 에셋 동기화를 위해 약 2초간 대기합니다...");

        double waitStartTime = EditorApplication.timeSinceStartup;
        EditorApplication.CallbackFunction waitAction = null;

        waitAction = () =>
        {
            if (EditorApplication.timeSinceStartup - waitStartTime < 2.0) return;

            EditorApplication.update -= waitAction;
            AssetDatabase.Refresh();

            int mapCount = LightmapSettings.lightmaps.Length;
            if (mapCount == 0)
            {
                Debug.LogError("생성된 라이트맵이 0개입니다! 프리팹 내 오브젝트들이 'Contribute GI'로 체크되어 있는지 확인하세요.");
                return;
            }

            string[] bakedColorPaths = new string[mapCount];
            string[] bakedDirPaths = new string[mapCount];
            string[] bakedShadowPaths = new string[mapCount];

            for (int i = 0; i < mapCount; i++)
            {
                var data = LightmapSettings.lightmaps[i];
                if (data.lightmapColor != null) bakedColorPaths[i] = AssetDatabase.GetAssetPath(data.lightmapColor);
                if (data.lightmapDir != null) bakedDirPaths[i] = AssetDatabase.GetAssetPath(data.lightmapDir);
                if (data.shadowMask != null) bakedShadowPaths[i] = AssetDatabase.GetAssetPath(data.shadowMask);
            }

            // =========================================================
            // [4단계] 해당 프리팹이 사용하는 '진짜' 인덱스만 추려내기
            // =========================================================
            HashSet<int> usedIndices = new HashSet<int>();
            var renderers = currentData.gameObject.GetComponentsInChildren<MeshRenderer>();

            foreach (MeshRenderer renderer in renderers)
            {
                int idx = renderer.lightmapIndex;
                if (idx >= 0 && idx != 0xFFFE && idx < mapCount)
                {
                    usedIndices.Add(idx);
                }
            }

            // =========================================================
            // [5단계] 추출된 파일만 골라서 '복사' 및 원본 '삭제'
            // =========================================================
            HashSet<string> processedPaths = new HashSet<string>();

            void CopyAndDeleteSpecificFile(string sourceAssetPath)
            {
                if (string.IsNullOrEmpty(sourceAssetPath)) return;
                if (processedPaths.Contains(sourceAssetPath)) return;

                processedPaths.Add(sourceAssetPath);

                string destPath = targetFolderPath + "/" + Path.GetFileName(sourceAssetPath);

                if (AssetDatabase.LoadMainAssetAtPath(destPath) != null)
                {
                    AssetDatabase.MakeEditable(destPath);
                    string metaPath = AssetDatabase.GetTextMetaFilePathFromAssetPath(destPath);
                    if (File.Exists(metaPath)) File.SetAttributes(metaPath, FileAttributes.Normal);
                    if (File.Exists(destPath)) File.SetAttributes(destPath, FileAttributes.Normal);

                    AssetDatabase.DeleteAsset(destPath);
                }

                AssetDatabase.CopyAsset(sourceAssetPath, destPath);
                AssetDatabase.DeleteAsset(sourceAssetPath);
            }

            foreach (int idx in usedIndices)
            {
                CopyAndDeleteSpecificFile(bakedColorPaths[idx]);
                CopyAndDeleteSpecificFile(bakedDirPaths[idx]);
                CopyAndDeleteSpecificFile(bakedShadowPaths[idx]);
            }

            AssetDatabase.Refresh();

            // =========================================================
            // [6단계] 컴포넌트에 새로운 경로의 데이터 적재
            // =========================================================
            var rendererInfos = new List<RendererInfo>();
            var lightmaps = new List<Texture2D>();
            var lightmapsDir = new List<Texture2D>();
            var shadowMasks = new List<Texture2D>();
            var lightsInfos = new List<LightInfo>();

            foreach (MeshRenderer renderer in renderers)
            {
                int idx = renderer.lightmapIndex;
                if (idx >= 0 && idx != 0xFFFE && idx < mapCount)
                {
                    RendererInfo info = new RendererInfo();
                    info.renderer = renderer;

                    if (renderer.lightmapScaleOffset != Vector4.zero)
                    {
                        info.lightmapOffsetScale = renderer.lightmapScaleOffset;

                        Texture2D GetCopiedTexture(string oldTempPath)
                        {
                            if (string.IsNullOrEmpty(oldTempPath)) return null;
                            string finalPath = targetFolderPath + "/" + Path.GetFileName(oldTempPath);
                            return AssetDatabase.LoadAssetAtPath<Texture2D>(finalPath);
                        }

                        Texture2D lightmap = GetCopiedTexture(bakedColorPaths[idx]);
                        Texture2D lightmapDir = GetCopiedTexture(bakedDirPaths[idx]);
                        Texture2D shadowMask = GetCopiedTexture(bakedShadowPaths[idx]);

                        info.lightmapIndex = lightmaps.IndexOf(lightmap);
                        if (info.lightmapIndex == -1)
                        {
                            info.lightmapIndex = lightmaps.Count;
                            if (lightmap != null) lightmaps.Add(lightmap);
                            if (lightmapDir != null) lightmapsDir.Add(lightmapDir);
                            if (shadowMask != null) shadowMasks.Add(shadowMask);
                        }
                        rendererInfos.Add(info);
                    }
                }
            }

            var lights = currentData.gameObject.GetComponentsInChildren<Light>(true);
            foreach (Light l in lights)
            {
                LightInfo lightInfo = new LightInfo();
                lightInfo.light = l;
                lightInfo.lightmapBaketype = (int)l.lightmapBakeType;
                lightInfo.mixedLightingMode = (int)MixedLightingMode.Shadowmask;
                lightsInfos.Add(lightInfo);
            }

            currentData.m_RendererInfo = rendererInfos.ToArray();
            currentData.m_Lightmaps = lightmaps.ToArray();
            currentData.m_LightmapsDir = lightmapsDir.ToArray();
            currentData.m_LightInfo = lightsInfos.ToArray();
            currentData.m_ShadowMasks = shadowMasks.ToArray();

            // =========================================================
            // [사후작업] 컴포넌트 셋팅이 끝난 후, 모든 라이트를 다시 No Shadow로 변경
            // =========================================================
            Light[] allLightsPostBake = FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (Light light in allLightsPostBake)
            {
                Undo.RecordObject(light, "Change Shadow Type to None");
                light.shadows = LightShadows.None;
            }
            Debug.Log($"사후 작업: 씬 내의 총 {allLightsPostBake.Length}개 라이트의 Shadow Type을 'No Shadow'로 복구했습니다.");

            // =========================================================
            // [7단계] 프리팹 덮어쓰기 (Save) - Unity 6 호환 버전
            // =========================================================
            EditorUtility.SetDirty(currentData);

            // 1. 현재 오브젝트가 프리팹의 인스턴스인지, 그리고 원본 프리팹 에셋이 존재하는지 확인
            GameObject targetPrefab = PrefabUtility.GetCorrespondingObjectFromOriginalSource(currentData.gameObject) as GameObject;
            if (targetPrefab != null)
            {
                // 2. 최상위 프리팹 인스턴스 루트를 찾음
                GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(currentData.gameObject);
                if (root != null)
                {
                    // 3. 원본 프리팹 에셋의 경로를 추출
                    string rootPath = AssetDatabase.GetAssetPath(targetPrefab);

                    if (!string.IsNullOrEmpty(rootPath))
                    {
                        // 4. Unpack 하지 않고, 현재 프리팹 인스턴스의 상태를 원본 경로에 그대로 덮어쓰며 연결을 유지합니다.
                        PrefabUtility.SaveAsPrefabAssetAndConnect(root, rootPath, InteractionMode.AutomatedAction);
                        Debug.Log($"프리팹 에셋 갱신 완료: {rootPath}");
                    }
                }
                else
                {
                    // 루트가 따로 없는 독립된 인스턴스인 경우 변동사항 바로 적용
                    PrefabUtility.ApplyPrefabInstance(currentData.gameObject, InteractionMode.AutomatedAction);
                }
            }

            // =========================================================
            // [8단계] 라이트맵 랜더링 즉시 갱신 (화면 리프레시)
            // =========================================================
            currentData.SendMessage("Init");
            SceneView.RepaintAll();

            Debug.Log($"<color=green><b>[{currentData.gameObject.name}]</b></color> 작업 및 화면 갱신 완료! 저장 위치: {targetFolderPath}");
        };

        EditorApplication.update += waitAction;
    }
#endif
}

#if UNITY_EDITOR
[CustomEditor(typeof(PrefabLightmapData))]
public class PrefabLightmapDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        PrefabLightmapData script = (PrefabLightmapData)target;

        GUILayout.Space(15);
        GUI.backgroundColor = new Color(0.3f, 0.6f, 0.9f);

        if (GUILayout.Button("Bake Lightmap", GUILayout.Height(35)))
        {
            if (EditorUtility.DisplayDialog("Prefab Lightmap Bake", "라이트맵 베이킹을 시작하시겠습니까?\n기존 파일은 삭제되고 이동 후 안전하게 재연결됩니다.", "시작", "취소"))
            {
                PrefabLightmapData.ExecuteBake(script);
            }
        }
        GUI.backgroundColor = Color.white;
    }
}
#endif