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
        public GameObject recoveryHeartPrefab;

        [Header("Combat & Skill Prefabs")]
        public GameObject arrowProjectilePrefab;
        public GameObject chargedArrowProjectilePrefab;
        public GameObject daggerSlashVfxPrefab;
        public GameObject venomProjectilePrefab;
        public GameObject zhuParabolicProjectilePrefab;
        public GameObject dokkaebiFireballPrefab;
        public GameObject totemEnergyOrbPrefab;
        public GameObject rollingBoulderPrefab;
        public GameObject rockShardPrefab;
        public GameObject spiderWebTetherPrefab;
        public GameObject mudPuddleZonePrefab;

        [Header("Telegraph & Wave VFX Prefabs")]
        public GameObject boxTelegraphPrefab;
        public GameObject sectorTelegraphPrefab;
        public GameObject circleTelegraphPrefab;
        public GameObject chargeAimBoxPrefab;
        public GameObject shockwaveRingPrefab;
        public GameObject sweepWavePrefab;
        public GameObject vacuumWavePrefab;
        public GameObject gemCollectVfxPrefab;

        [Header("Monster Prefabs (Core 6)")]
        public GameObject minionMonsterPrefab;
        public GameObject eliteMonsterPrefab;
        public GameObject foxBossPrefab;
        public GameObject archerMonsterPrefab;
        public GameObject bigMonMonsterPrefab;
        public GameObject zhuMonsterPrefab;

        [Header("Monster Prefabs (Gimmick 9)")]
        public GameObject dokkaebiFireMonsterPrefab;
        public GameObject boarChargerMonsterPrefab;
        public GameObject spiderTetherMonsterPrefab;
        public GameObject mudSlimeMonsterPrefab;
        public GameObject phantomFoxMonsterPrefab;
        public GameObject ironShieldMonsterPrefab;
        public GameObject energyAbsorberMonsterPrefab;
        public GameObject bombSparrowMonsterPrefab;
        public GameObject totemTurretMonsterPrefab;

        [Header("Dungeon Architecture Prefabs")]
        public GameObject dungeonRoomPrefab;
        public GameObject doorSocketPrefab;
        public GameObject doorBarrierPrefab;
        public GameObject autoDoorPrefab;
        public GameObject fogShroudPrefab;

        [Header("Camera & System Prefabs")]
        public GameObject mainCameraPrefab;
        public GameObject ingameSystemManagersPrefab;
        public GameObject outgameShrineEnvPrefab;

        [Header("GUI Prefabs")]
        public GameObject playerHealthBarUIPrefab;
        public GameObject boonSelectionUIPrefab;
        public GameObject bossHealthBarUIPrefab;
        public GameObject monsterHealthBarManagerPrefab;
        public GameObject stageClearUIPrefab;
        public GameObject progressionDebugUIPrefab;
        public GameObject screenDamageVignetteUIPrefab;
        public GameObject aimCursorManagerPrefab;
        public GameObject introOutgameUIPrefab;

        /// <summary>
        /// 직렬화된 프리팹 레퍼런스가 비어 있을 경우 Resources/Prefabs 경로에서 자동 로드하여 반환합니다.
        /// </summary>
        public static GameObject ResolvePrefab(ref GameObject field, string resourceRelativePath)
        {
            if (field != null) return field;
            field = Resources.Load<GameObject>(resourceRelativePath);
            return field;
        }
    }
}
