using UnityEngine;

namespace MythOfGuotaji.Data
{
    [CreateAssetMenu(fileName = "PrefabDatabase", menuName = "MythOfGuotaji/Database/PrefabDatabase")]
    public class PrefabDatabaseSO : ScriptableObject
    {
        [Header("Actor Prefabs")]
        public GameObject playerPrefab;
        public GameObject companionPrefab;
        public GameObject helperCompanionPrefab;
        public GameObject expGemPrefab;

        [Header("Combat Prefabs")]
        public GameObject arrowProjectilePrefab;
        public GameObject chargedArrowProjectilePrefab;

        [Header("Monster Prefabs")]
        public GameObject minionMonsterPrefab;
        public GameObject eliteMonsterPrefab;
        public GameObject foxBossPrefab;
        public GameObject archerMonsterPrefab;
        public GameObject bigMonMonsterPrefab;
        public GameObject zhuMonsterPrefab;

        [Header("Dungeon Architecture Prefabs")]
        public GameObject dungeonRoomPrefab;
        public GameObject doorSocketPrefab;
        public GameObject doorBarrierPrefab;

        [Header("Camera & System Prefabs")]
        public GameObject mainCameraPrefab;
    }
}
