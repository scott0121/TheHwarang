#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using MythOfGuotaji.Actor;
using MythOfGuotaji.Data;

namespace MythOfGuotaji.EditorTools
{
    [InitializeOnLoad]
    public static class MonsterPrefabGenerator
    {
        static MonsterPrefabGenerator()
        {
            EditorApplication.delayCall += GenerateMissingPrefabs;
        }

        [MenuItem("MythOfGuotaji/Generate Missing Monster Prefabs")]
        public static void GenerateMissingPrefabs()
        {
            string folderPath = "Assets/_MythOfGuotaji/Resources/Prefabs";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets/_MythOfGuotaji/Resources", "Prefabs");
            }

            // 1. Archer (맹독충) - Muscomorph
            string archerPath = $"{folderPath}/Monster_Archer.prefab";
            GameObject archerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(archerPath);
            if (archerPrefab == null)
            {
                GameObject temp = CreateMonsterObject(
                    "Monster_Archer",
                    MonsterType.Minion_Archer,
                    "Assets/_MythOfGuotaji/ArtAssets/NewMonsterAssets/Prefabs/Muscomorph.prefab",
                    30f, 2.7f, 7f, 15f, 0.7f,
                    colRadius: 0.5f, colHeight: 1.8f
                );
                archerPrefab = PrefabUtility.SaveAsPrefabAsset(temp, archerPath);
                Object.DestroyImmediate(temp);
                Debug.Log($"<color=green>[MonsterPrefabGenerator] Created Monster_Archer.prefab successfully!</color>");
            }

            // 2. BigMon (거북이) - Clypeosaurus
            string bigMonPath = $"{folderPath}/Monster_BigMon.prefab";
            GameObject bigMonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(bigMonPath);
            if (bigMonPrefab == null)
            {
                GameObject temp = CreateMonsterObject(
                    "Monster_BigMon",
                    MonsterType.Minion_BigMon,
                    "Assets/_MythOfGuotaji/ArtAssets/NewMonsterAssets/Prefabs/Clypeosaurus.prefab",
                    60f, 2.0f, 12f, 2.0f, 1.8f,
                    colRadius: 0.9f, colHeight: 2.0f
                );
                bigMonPrefab = PrefabUtility.SaveAsPrefabAsset(temp, bigMonPath);
                Object.DestroyImmediate(temp);
                Debug.Log($"<color=green>[MonsterPrefabGenerator] Created Monster_BigMon.prefab successfully!</color>");
            }

            // 3. Zhu (주) - Vespomorph
            string zhuPath = $"{folderPath}/Monster_Zhu.prefab";
            GameObject zhuPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(zhuPath);
            if (zhuPrefab == null)
            {
                GameObject temp = CreateMonsterObject(
                    "Monster_Zhu",
                    MonsterType.Minion_Zhu,
                    "Assets/_MythOfGuotaji/ArtAssets/NewMonsterAssets/Prefabs/Vespomorph.prefab",
                    50f, 1.2f, 18f, 20f, 3.0f,
                    colRadius: 0.6f, colHeight: 2.0f
                );
                zhuPrefab = PrefabUtility.SaveAsPrefabAsset(temp, zhuPath);
                Object.DestroyImmediate(temp);
                Debug.Log($"<color=green>[MonsterPrefabGenerator] Created Monster_Zhu.prefab successfully!</color>");
            }

            // Assign to PrefabDatabaseSO
            string dbPath = "Assets/_MythOfGuotaji/Resources/Data/PrefabDatabase.asset";
            var db = AssetDatabase.LoadAssetAtPath<PrefabDatabaseSO>(dbPath);
            if (db != null)
            {
                bool dirty = false;
                if (db.archerMonsterPrefab == null && archerPrefab != null) { db.archerMonsterPrefab = archerPrefab; dirty = true; }
                if (db.bigMonMonsterPrefab == null && bigMonPrefab != null) { db.bigMonMonsterPrefab = bigMonPrefab; dirty = true; }
                if (db.zhuMonsterPrefab == null && zhuPrefab != null) { db.zhuMonsterPrefab = zhuPrefab; dirty = true; }

                if (dirty)
                {
                    EditorUtility.SetDirty(db);
                    AssetDatabase.SaveAssets();
                    Debug.Log($"<color=cyan>[MonsterPrefabGenerator] Linked archer/bigMon/zhu prefabs into PrefabDatabase.asset!</color>");
                }
            }
        }

        private static GameObject CreateMonsterObject(
            string monsterName,
            MonsterType type,
            string skinPrefabPath,
            float hp, float speed, float damage, float attackRange, float attackCooldown,
            float colRadius, float colHeight)
        {
            GameObject root = new GameObject(monsterName);
            root.tag = "Monster";

            var cc = root.AddComponent<CharacterController>();
            cc.height = colHeight;
            cc.radius = colRadius;
            cc.center = new Vector3(0f, colHeight * 0.5f, 0f);

            var col = root.AddComponent<CapsuleCollider>();
            col.height = colHeight;
            col.radius = colRadius;
            col.center = new Vector3(0f, colHeight * 0.5f, 0f);

            var ai = root.AddComponent<MonsterAI>();
            var so = new SerializedObject(ai);
            var typeProp = so.FindProperty("_type");
            if (typeProp != null) typeProp.enumValueIndex = (int)type;
            var hpProp = so.FindProperty("_maxHp");
            if (hpProp != null) hpProp.floatValue = hp;
            var spdProp = so.FindProperty("_moveSpeed");
            if (spdProp != null) spdProp.floatValue = speed;
            var dmgProp = so.FindProperty("_attackDamage");
            if (dmgProp != null) dmgProp.floatValue = damage;
            var rangeProp = so.FindProperty("_attackRange");
            if (rangeProp != null) rangeProp.floatValue = attackRange;
            var cdProp = so.FindProperty("_attackCooldown");
            if (cdProp != null) cdProp.floatValue = attackCooldown;
            var sepProp = so.FindProperty("_separationRadius");
            if (sepProp != null) sepProp.floatValue = colRadius * 1.5f;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Instantiate skin child
            GameObject skinAsset = AssetDatabase.LoadAssetAtPath<GameObject>(skinPrefabPath);
            if (skinAsset != null)
            {
                GameObject skinInstance = (GameObject)PrefabUtility.InstantiatePrefab(skinAsset, root.transform);
                skinInstance.transform.localPosition = Vector3.zero;
                skinInstance.transform.localRotation = Quaternion.identity;
                skinInstance.transform.localScale = Vector3.one;
            }

            return root;
        }
    }
}
#endif
