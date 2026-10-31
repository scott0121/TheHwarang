using System.IO;
using UnityEngine;

namespace MythOfGuotaji.Core
{
    /// <summary>
    /// 상업용 안정성을 갖춘 로컬 JSON 세이브/로드 매니저 (Atomic Write + Backup 복구 + 스키마 마이그레이션 지원)
    /// </summary>
    public static class SaveSystem
    {
        private static readonly string SaveFileName = "MythOfGuotaji_Save.json";
        private static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);
        private static string TempSavePath => SavePath + ".tmp";
        private static string BackupSavePath => SavePath + ".bak";

        public static void Save(SaveData data)
        {
            if (data == null) return;
            try
            {
                data.schemaVersion = 2;
                data.lastSavedUtc = System.DateTime.UtcNow.ToString("o");

                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(TempSavePath, json);

                if (File.Exists(SavePath))
                {
                    File.Copy(SavePath, BackupSavePath, true);
                }

                File.Copy(TempSavePath, SavePath, true);
                File.Delete(TempSavePath);
                Debug.Log($"[SaveSystem] Saved successfully (v{data.schemaVersion}) to: {SavePath}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SaveSystem] Failed to save: {ex.Message}");
            }
        }

        public static SaveData Load()
        {
            SaveData loaded = TryLoadFromPath(SavePath) ?? TryLoadFromPath(BackupSavePath);
            if (loaded != null)
            {
                ValidateAndMigrate(loaded);
                return loaded;
            }

            Debug.Log("[SaveSystem] Creating new default save data.");
            SaveData defaultData = new SaveData();
            Save(defaultData);
            return defaultData;
        }

        private static SaveData TryLoadFromPath(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        SaveData data = JsonUtility.FromJson<SaveData>(json);
                        if (data != null)
                        {
                            Debug.Log($"[SaveSystem] Loaded save data from: {path}");
                            return data;
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[SaveSystem] Failed to load from '{path}': {ex.Message}");
            }
            return null;
        }

        private static void ValidateAndMigrate(SaveData data)
        {
            if (data.unlockedScenarioNodeIds == null) data.unlockedScenarioNodeIds = new System.Collections.Generic.List<string> { "ROOT_START" };
            if (data.unlockedCoreMemoryIds == null) data.unlockedCoreMemoryIds = new System.Collections.Generic.List<string>();
            if (!data.unlockedCoreMemoryIds.Contains("MEM_CH1_FIRST_STRING"))
            {
                data.unlockedCoreMemoryIds.Add("MEM_CH1_FIRST_STRING");
            }
            if (string.IsNullOrEmpty(data.equippedCoreMemoryId))
            {
                data.equippedCoreMemoryId = "MEM_CH1_FIRST_STRING";
            }
            if (string.IsNullOrEmpty(data.selectedStageCode))
            {
                data.selectedStageCode = "1-1";
            }
            data.currentStage = Mathf.Clamp(data.currentStage, 1, 22);
            data.totalCollectedShards = Mathf.Clamp(data.totalCollectedShards, 0, 22);
            data.retryBlessingStacks = Mathf.Clamp(data.retryBlessingStacks, 0, 5);
        }

        public static void DeleteSave()
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
            if (File.Exists(BackupSavePath)) File.Delete(BackupSavePath);
            if (File.Exists(TempSavePath)) File.Delete(TempSavePath);
            Debug.Log("[SaveSystem] Save data and backups reset completed.");
        }
    }
}
