#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using MythOfGuotaji.Cameras;
using MythOfGuotaji.Test;

namespace MythOfGuotaji.Editor
{
    public static class MonsterTestSceneCreator
    {
        [MenuItem("MythOfGuotaji/Scenes/Create MonsterTest Scene", priority = 10)]
        public static void CreateMonsterTestScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Directional Light
            var lightObj = new GameObject("Directional Light");
            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.color = new Color(1f, 0.96f, 0.9f);
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // 2. Main Camera
            var camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            var cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.fieldOfView = 60f;
            camObj.transform.position = new Vector3(39f, 25f, -15f);
            camObj.transform.rotation = Quaternion.Euler(50f, 0f, 0f);
            camObj.AddComponent<QuarterViewCamera>();

            // 3. MonsterTestController
            var ctrlObj = new GameObject("[MonsterTestController]");
            ctrlObj.AddComponent<MonsterTestController>();

            string scenePath = "Assets/Scenes/MonsterTest.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.Refresh();

            Debug.Log($"<color=lime><b>[MonsterTestSceneCreator]</b> MonsterTest 씬이 성공적으로 생성되었습니다: {scenePath}</color>");
        }
    }
}
#endif

