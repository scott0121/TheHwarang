using System.IO;
using UnityEngine;

namespace MythOfGuotaji.Core
{
    /// <summary>
    /// 로컬 JSON 기반 영구 세이브/로드 매니저
    /// </summary>
    public static class SaveSystem
    {
        private static readonly string SaveFileName = "MythOfGuotaji_Save.json";
        private static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        public static void Save(SaveData data)
        {
            try
            {
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(SavePath, json);
                Debug.Log($"[SaveSystem] Saved successfully to: {SavePath}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SaveSystem] Failed to save: {ex.Message}");
            }
        }

        public static SaveData Load()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    string json = File.ReadAllText(SavePath);
                    SaveData data = JsonUtility.FromJson<SaveData>(json);
                    if (data != null)
                    {
                        Debug.Log("[SaveSystem] Loaded existing save data.");
                        return data;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SaveSystem] Failed to load, creating default save: {ex.Message}");
            }

            Debug.Log("[SaveSystem] Creating new default save data.");
            SaveData defaultData = new SaveData();
            Save(defaultData);
            return defaultData;
        }

        public static void DeleteSave()
        {
            if (File.Exists(SavePath))
            {
                File.Delete(SavePath);
                Debug.Log("[SaveSystem] Save data reset completed.");
            }
        }
    }
}
