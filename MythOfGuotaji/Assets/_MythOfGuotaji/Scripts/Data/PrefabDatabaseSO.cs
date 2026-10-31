using System;
using UnityEngine;

namespace MythOfGuotaji.Data
{
    /// <summary>
    /// =========================================================================================
    /// [아키텍처 규정 - PrefabDatabaseSO의 단일 책임 원칙 (SRP)]
    /// =========================================================================================
    /// 역할: 전역 씬 인프라 및 시스템 아키텍처 프리팹 카탈로그 (Global Scene & Infrastructure Catalog)
    /// 
    /// 1. 허용되는 참조 항목 (Allowed):
    ///    - 액터 기본 프리팹: 플레이어 루트(Player), 동료/헬퍼 루트(Companion, HelperCompanion)
    ///    - 필드 픽업 아이템: 경험치 보석(ExpGem), 생명 하트(RecoveryHeart)
    ///    - 던전 건축 및 환경: 던전 룸(DungeonRoom), 방 문/결계(DoorSocket, DoorBarrier, AutoDoor), 안개(FogShroud)
    ///    - 시스템 인프라 및 씬 매니저: 메인 카메라(MainCamera), 인게임 시스템 매니저 모음, 아웃게임 사당 3D 환경
    ///    - 전역 UI 프리팹: 체력바, 보은 선택창, 보스 체력바, 클리어창, 조준선 매니저 등
    /// 
    /// 2. 금지 및 이전 대상 항목 (Moved / Deprecated for specific tables):
    ///    - 플레이어 무기별 투사체 및 스킬 VFX -> [WeaponStatsTableSO]로 이전 (BowEntry, DaggerEntry)
    ///    - 플레이어 액션 피드백(대시 먼지, 회피 샤인, 피격, 치유 오라 등) -> [PlayerStatsTableSO]로 이전
    ///    - 몬스터 스킬 투사체, 바닥 장판, 텔레그래프 프리팹 -> [MonsterSkillTableSO]로 이전
    ///    - ※ 하위 호환성을 위해 아래 필드들은 유지되나, 런타임 게임플레이 로직은 전용 TableSO를 최우선 참조합니다.
    /// =========================================================================================
    /// </summary>
    [CreateAssetMenu(fileName = "PrefabDatabase", menuName = "MythOfGuotaji/Database/PrefabDatabase")]
    public class PrefabDatabaseSO : ScriptableObject
    {
        [Header("1. Global Actor Prefabs (System Level)")]
        [Tooltip("플레이어 기본 본체 프리팹")]
        public GameObject playerPrefab;
        [Tooltip("인연 동료 기본 본체 프리팹")]
        public GameObject companionPrefab;
        [Tooltip("보옥 서포터 헬퍼 기본 본체 프리팹")]
        public GameObject helperCompanionPrefab;
        [Tooltip("필드 드롭 경험치 보석 프리팹")]
        public GameObject expGemPrefab;
        [Tooltip("필드 드롭 생명 회복 하트 프리팹")]
        public GameObject recoveryHeartPrefab;

        [Header("2. Combat & Skill Prefabs (Legacy Fallback - Use WeaponStatsTableSO/MonsterSkillTableSO)")]
        [Tooltip("[권장: WeaponStatsTableSO 참조] 기본 화살 투사체")]
        public GameObject arrowProjectilePrefab;
        [Tooltip("[권장: WeaponStatsTableSO 참조] 차징 화살 투사체")]
        public GameObject chargedArrowProjectilePrefab;
        [Tooltip("[권장: WeaponStatsTableSO 참조] 단검 참격 시각 효과")]
        public GameObject daggerSlashVfxPrefab;
        [Tooltip("[권장: MonsterSkillTableSO 참조] 독침 투사체")]
        public GameObject venomProjectilePrefab;
        [Tooltip("[권장: MonsterSkillTableSO 참조] 저주 포물선 투사체")]
        public GameObject zhuParabolicProjectilePrefab;
        [Tooltip("[권장: MonsterSkillTableSO 참조] 도깨비 불씨 투사체")]
        public GameObject dokkaebiFireballPrefab;
        [Tooltip("[권장: MonsterSkillTableSO 참조] 토템 에너지 탄환")]
        public GameObject totemEnergyOrbPrefab;
        [Tooltip("[권장: MonsterSkillTableSO 참조] 굴러가는 바위")]
        public GameObject rollingBoulderPrefab;
        [Tooltip("[권장: MonsterSkillTableSO 참조] 파쇄 암석 파편")]
        public GameObject rockShardPrefab;
        [Tooltip("[권장: MonsterSkillTableSO 참조] 거미줄 연결선")]
        public GameObject spiderWebTetherPrefab;
        [Tooltip("[권장: MonsterSkillTableSO 참조] 진흙 장판 존")]
        public GameObject mudPuddleZonePrefab;

        [Header("3. Telegraph Prefabs (Legacy Fallback - Use MonsterSkillTableSO)")]
        public GameObject boxTelegraphPrefab;
        public GameObject sectorTelegraphPrefab;
        public GameObject circleTelegraphPrefab;
        public GameObject chargeAimBoxPrefab;
        public GameObject shockwaveRingPrefab;
        public GameObject sweepWavePrefab;
        public GameObject vacuumWavePrefab;
        public GameObject gemCollectVfxPrefab;

        [Header("4. Monster Prefabs (Core 6)")]
        public GameObject minionMonsterPrefab;
        public GameObject eliteMonsterPrefab;
        public GameObject foxBossPrefab;
        public GameObject archerMonsterPrefab;
        public GameObject bigMonMonsterPrefab;
        public GameObject zhuMonsterPrefab;

        [Header("5. Monster Prefabs (Gimmick 9)")]
        public GameObject dokkaebiFireMonsterPrefab;
        public GameObject boarChargerMonsterPrefab;
        public GameObject spiderTetherMonsterPrefab;
        public GameObject mudSlimeMonsterPrefab;
        public GameObject phantomFoxMonsterPrefab;
        public GameObject ironShieldMonsterPrefab;
        public GameObject energyAbsorberMonsterPrefab;
        public GameObject bombSparrowMonsterPrefab;
        public GameObject totemTurretMonsterPrefab;

        [Header("6. Dungeon Architecture & Scene Infrastructure")]
        public GameObject dungeonRoomPrefab;
        public GameObject doorSocketPrefab;
        public GameObject doorBarrierPrefab;
        public GameObject autoDoorPrefab;
        public GameObject fogShroudPrefab;

        [Header("7. Camera & System Managers")]
        public GameObject mainCameraPrefab;
        public GameObject ingameSystemManagersPrefab;
        public GameObject outgameShrineEnvPrefab;

        [Header("8. Global UI Prefabs")]
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
        public static GameObject ResolvePrefab(ref GameObject field, string resourceRelativePath, string fallbackPath = null)
        {
            if (field != null) return field;
            field = Resources.Load<GameObject>(resourceRelativePath);
            if (field == null && !string.IsNullOrEmpty(fallbackPath))
            {
                field = Resources.Load<GameObject>(fallbackPath);
            }
            return field;
        }

        /// <summary>
        /// MonsterType에 해당하는 프리팹 반환 (MonsterStatsTableSO 최우선 참조)
        /// </summary>
        public GameObject GetMonsterPrefab(Actor.MonsterType type)
        {
            if (GameDataManager.Instance != null && GameDataManager.Instance.MonsterStats != null)
            {
                var tablePrefab = GameDataManager.Instance.MonsterStats.GetMonsterPrefab(type);
                if (tablePrefab != null) return tablePrefab;
            }

            return type switch
            {
                Actor.MonsterType.Minion => ResolvePrefab(ref minionMonsterPrefab, "Prefabs/Monster_Minion", "Prefabs/Minion_Monster"),
                Actor.MonsterType.Minion_Archer => ResolvePrefab(ref archerMonsterPrefab, "Prefabs/Monster_Archer", "Prefabs/Monster_Minion_Archer"),
                Actor.MonsterType.Minion_BigMon => ResolvePrefab(ref bigMonMonsterPrefab, "Prefabs/Monster_BigMon", "Prefabs/Monster_Minion_BigMon"),
                Actor.MonsterType.Minion_Zhu => ResolvePrefab(ref zhuMonsterPrefab, "Prefabs/Monster_Zhu", "Prefabs/Monster_Minion_Zhu"),
                Actor.MonsterType.Elite => ResolvePrefab(ref eliteMonsterPrefab, "Prefabs/Monster_Elite", "Prefabs/Elite_Monster"),
                Actor.MonsterType.FoxBoss => ResolvePrefab(ref foxBossPrefab, "Prefabs/Monster_FoxBoss", "Prefabs/Fox_Boss"),
                Actor.MonsterType.Dokkaebi_Fire => ResolvePrefab(ref dokkaebiFireMonsterPrefab, "Prefabs/Monster_DokkaebiFire", "Prefabs/Monster_Dokkaebi_Fire"),
                Actor.MonsterType.Boar_Charger => ResolvePrefab(ref boarChargerMonsterPrefab, "Prefabs/Monster_BoarCharger", "Prefabs/Monster_Boar_Charger"),
                Actor.MonsterType.Spider_Tether => ResolvePrefab(ref spiderTetherMonsterPrefab, "Prefabs/Monster_SpiderTether", "Prefabs/Monster_Spider_Tether"),
                Actor.MonsterType.Mud_Slime => ResolvePrefab(ref mudSlimeMonsterPrefab, "Prefabs/Monster_MudSlime", "Prefabs/Monster_Mud_Slime"),
                Actor.MonsterType.Phantom_Fox => ResolvePrefab(ref phantomFoxMonsterPrefab, "Prefabs/Monster_PhantomFox", "Prefabs/Monster_Phantom_Fox"),
                Actor.MonsterType.Iron_Shield => ResolvePrefab(ref ironShieldMonsterPrefab, "Prefabs/Monster_IronShield", "Prefabs/Monster_Iron_Shield"),
                Actor.MonsterType.Energy_Absorber => ResolvePrefab(ref energyAbsorberMonsterPrefab, "Prefabs/Monster_EnergyAbsorber", "Prefabs/Monster_Energy_Absorber"),
                Actor.MonsterType.Bomb_Sparrow => ResolvePrefab(ref bombSparrowMonsterPrefab, "Prefabs/Monster_BombSparrow", "Prefabs/Monster_Bomb_Sparrow"),
                Actor.MonsterType.Totem_Turret => ResolvePrefab(ref totemTurretMonsterPrefab, "Prefabs/Monster_TotemTurret", "Prefabs/Monster_Totem_Turret"),
                _ => minionMonsterPrefab
            };
        }
    }
}
