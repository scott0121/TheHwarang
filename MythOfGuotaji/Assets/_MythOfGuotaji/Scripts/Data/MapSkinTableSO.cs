using System;
using System.Collections.Generic;
using UnityEngine;

namespace MythOfGuotaji.Data
{
    [Serializable]
    public class MapSkinCategory
    {
        [Tooltip("맵 스타일 카테고리 명칭 (예: Base, Cave, Temple 등)")]
        public string categoryName = "Base";

        [Tooltip("해당 카테고리에 속한 방 프리팹 목록 (예: DungeonRoom_Base_00 ~ 12)")]
        public List<GameObject> roomPrefabs = new();
    }

    /// <summary>
    /// 던전 맵 스킨(방 프리팹) 테이블 ScriptableObject:
    /// - 스테이지별 또는 던전 테마별 방 외형 프리팹 프리셋 관리
    /// </summary>
    [CreateAssetMenu(fileName = "MapSkinTable", menuName = "MythOfGuotaji/MapSkinTable")]
    public class MapSkinTableSO : ScriptableObject
    {
        [Header("Map Style Categories")]
        public List<MapSkinCategory> categories = new();

        /// <summary>
        /// 지정한 카테고리에서 무작위 방 프리팹 1종 반환
        /// </summary>
        public GameObject GetRandomRoomPrefab(int categoryIndex)
        {
            if (categories == null || categories.Count == 0) return null;
            int idx = Mathf.Clamp(categoryIndex, 0, categories.Count - 1);
            var cat = categories[idx];
            if (cat == null || cat.roomPrefabs == null || cat.roomPrefabs.Count == 0) return null;

            int randIdx = UnityEngine.Random.Range(0, cat.roomPrefabs.Count);
            return cat.roomPrefabs[randIdx];
        }

        /// <summary>
        /// 카테고리 이름으로 무작위 방 프리팹 1종 반환
        /// </summary>
        public GameObject GetRandomRoomPrefabByName(string categoryName)
        {
            if (categories == null || categories.Count == 0) return null;
            var cat = categories.Find(c => string.Equals(c.categoryName, categoryName, StringComparison.OrdinalIgnoreCase));
            if (cat == null || cat.roomPrefabs == null || cat.roomPrefabs.Count == 0)
            {
                return GetRandomRoomPrefab(0);
            }
            int randIdx = UnityEngine.Random.Range(0, cat.roomPrefabs.Count);
            return cat.roomPrefabs[randIdx];
        }
    }
}
