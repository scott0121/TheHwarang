using UnityEngine;
using UnityEditor;
using System.IO;

public class MaterialTextureAutoAssigner
{
    [MenuItem("Tools/Auto Assign Textures to Materials")]
    public static void AutoAssignTextures()
    {
        // 1. 현재 선택된 폴더 가져오기
        string selectedPath = "Assets";
        foreach (Object obj in Selection.GetFiltered(typeof(Object), SelectionMode.Assets))
        {
            selectedPath = AssetDatabase.GetAssetPath(obj);
            if (!Directory.Exists(selectedPath)) 
                selectedPath = Path.GetDirectoryName(selectedPath);
            break;
        }

        // 현재 폴더 안의 모든 머티리얼 검색
        string[] matFiles = Directory.GetFiles(selectedPath, "*.mat", SearchOption.TopDirectoryOnly);

        if (matFiles.Length == 0)
        {
            Debug.LogWarning("선택한 폴더에 머티리얼(.mat) 파일이 없습니다.");
            return;
        }

        int count = 0;
        foreach (string matFile in matFiles)
        {
            string assetPath = matFile.Replace('\\', '/');
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            
            if (mat != null)
            {
                string matName = mat.name;
                bool isModified = false;

                // 2 & 3. BaseTexture 할당 (Material명.png)
                Texture2D baseTex = FindTexture(selectedPath, matName);
                if (baseTex != null)
                {
                    // URP 및 Standard 셰이더 호환
                    if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", baseTex); 
                    else if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", baseTex); 
                    isModified = true;
                }

                // 2 & 4. MetallicTexture 할당 (Material명_metalic.png)
                Texture2D metallicTex = FindTexture(selectedPath, matName + "_metallic");
                if (metallicTex != null)
                {
                    if (mat.HasProperty("_MetallicGlossMap")) mat.SetTexture("_MetallicGlossMap", metallicTex);
                    isModified = true;
                }

                // 2 & 5. NormalTexture 할당 (Material명_normal.png)
                Texture2D normalTex = FindTexture(selectedPath, matName + "_normal");
                if (normalTex != null)
                {
                    if (mat.HasProperty("_BumpMap")) mat.SetTexture("_BumpMap", normalTex);
                    isModified = true;
                }

                // 6. 머티리얼 변화 상태 저장
                if (isModified)
                {
                    EditorUtility.SetDirty(mat); // 변경 사항 마킹
                    count++;
                    Debug.Log($"[{matName}] 텍스처 자동 할당 완료.");
                }
            }
        }

        // 에셋 데이터베이스에 물리적 저장
        AssetDatabase.SaveAssets();
        Debug.Log($"작업 완료! 총 {count}개의 머티리얼을 업데이트했습니다.");
    }

    // 텍스처 검색 헬퍼 메서드 (현재 폴더 및 PNG 하위 폴더 검색)
    private static Texture2D FindTexture(string folderPath, string fileName)
    {
        string[] possiblePaths = new string[]
        {
            $"{folderPath}/{fileName}.png",      // 현재 폴더에 있는 경우
            $"{folderPath}/PNG/{fileName}.png"   // 이전 작업으로 생성된 PNG 폴더에 있는 경우
        };

        foreach (string path in possiblePaths)
        {
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex != null) return tex;
        }

        return null;
    }
}