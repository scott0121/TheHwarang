using System;
using System.Collections.Generic;
using MythOfGuotaji.Actor;
using UnityEngine;

namespace MythOfGuotaji.Data
{
    /// <summary>
    /// 몬스터 전용 특수 드롭 아이템 엔트리 (보스방 열쇠, 유물, 룬 등)
    /// </summary>
    [Serializable]
    public class SpecialDropEntry
    {
        [Tooltip("아이템 고유 식별자 (e.g. BossKey, AncientRune)")]
        public string itemId = "BossKey";

        [Tooltip("아이템 표시 이름")]
        public string itemName = "보스방 황금 열쇠";

        [Tooltip("드롭 확률 (1.0 = 100% 확정 드롭)")]
        [Range(0f, 1f)] public float dropChance = 1.0f;

        [Tooltip("드롭 시 재생할 VFX 프리팹 (GameObject 직접 등록)")]
        public GameObject dropVfxPrefab;

        [Tooltip("드롭 시 재생할 1회성 사운드 프리팹 (GameObject 직접 등록)")]
        public GameObject dropSoundPrefab;
    }

    /// <summary>
    /// 몬스터 기본 평타 공격(Attack Normal) 파라미터
    /// </summary>
    [Serializable]
    public class NormalAttackInfo
    {
        [Tooltip("기본 공격 사거리 (m)")]
        public float attackRange = 1.8f;

        [Tooltip("기본 공격 피해량")]
        public float attackDamage = 8.0f;

        [Tooltip("기본 공격 쿨다운 (초)")]
        public float attackCooldown = 1.2f;

        [Tooltip("공격 시전 선딜레이 시간 (초)")]
        public float windupTime = 0.2f;

        [Tooltip("공격 시전 후딜레이 시간 (초)")]
        public float recoveryTime = 0.2f;

        [Tooltip("근접 타격 판정 반경 (m)")]
        public float hitboxRadius = 1.0f;

        [Tooltip("공격 궤적 휘두르기 VFX 프리팹 (GameObject 직접 등록)")]
        public GameObject attackVfxPrefab;

        [Tooltip("적중 시 피격 충격 VFX 프리팹 (GameObject 직접 등록)")]
        public GameObject hitVfxPrefab;

        [Tooltip("공격 시전 1회성 사운드 프리팹 (GameObject 직접 등록)")]
        public GameObject attackSoundPrefab;

        [Tooltip("공격 적중 1회성 사운드 프리팹 (GameObject 직접 등록)")]
        public GameObject hitSoundPrefab;

        [Tooltip("전조 장판 형태 (None, StretchBox, SelfCircle, TargetCircle, StretchArc30~210, TargetConnect)")]
        public string telegraphType = "None";

        [Tooltip("전조 장판 프리팹 (GameObject 직접 등록, None일 경우 비워둠)")]
        public GameObject telegraphPrefab;
    }

    /// <summary>
    /// 통합 몬스터 데이터 엔트리
    /// - 기본 정보 / Stats / Drop Rewards / Special Drops / Attack Normal / Attack Special / VFX & Sound
    /// </summary>
    [Serializable]
    public class MonsterStatEntry
    {
        [Header("1. 기본 정보 (Identification)")]
        public MonsterType type;
        public string displayName = "몬스터 이름";
        [TextArea(2, 3)] public string description = "몬스터 설명";
        public string category = "Normal"; // Normal, Ranged, Heavy, Elite, Boss
        public float scaleMultiplier = 1.0f;
        public Color tintColor = Color.white;
        [Tooltip("몬스터 기본 본체 프리팹 (지정 시 이 프리팹을 최우선 스폰)")]
        public GameObject monsterPrefab;

        [Header("2. 스탯 (Stats)")]
        public float maxHp = 40.0f;
        public float moveSpeed = 4.0f;
        public float defense = 0f;
        public float separationRadius = 0.9f;
        public float hitStunDuration = 0.15f;

        [Header("3. 기본 드롭 보상 (Drop Rewards)")]
        public int gemDropCount = 1;
        public int expPerGem = 1;
        public int goldReward = 5;
        public int affinityReward = 2;
        [Range(0f, 1f)] public float heartDropChance = 0.25f;
        public float heartHealAmount = 5.0f;

        [Header("4. 스페셜 드롭 테이블 (Special Drops)")]
        [Tooltip("열쇠 등 이 몬스터가 사망 시 꼭 드롭해야 하는 특수 아이템 목록")]
        public List<SpecialDropEntry> specialDrops = new();

        [Header("5. 기본 공격 (Attack Normal)")]
        public NormalAttackInfo normalAttack = new();

        [Header("6. 특수 스킬 (Attack Special)")]
        [Tooltip("몬스터 스킬 테이블(MonsterSkillTable)에서 지정된 스킬 ID 연결 (e.g. minion_dash, turtle_charge)")]
        public List<string> specialSkillIds = new();

        [Header("7. 피격/사망/상태 VFX (GameObject 직접 등록)")]
        [Tooltip("몬스터 피격 시 재생할 VFX 프리팹")]
        public GameObject hitVfxPrefab;

        [Tooltip("방패 도탄/방어 시 재생할 VFX 프리팹")]
        public GameObject deflectVfxPrefab;

        [Tooltip("보호막/방패 파괴 또는 스턴 시 재생할 VFX 프리팹")]
        public GameObject guardBreakVfxPrefab;

        [Tooltip("몬스터 사망 시 재생할 VFX 프리팹")]
        public GameObject deathVfxPrefab;

        [Header("8. 사운드 프리팹 설정 (1회성 종료 시까지 재생 & 루프 사운드 별도 설정)")]
        [Tooltip("몬스터 스폰 시 1회성 사운드 프리팹 (클립 종료 시까지 재생)")]
        public GameObject spawnSoundPrefab;

        [Tooltip("몬스터 피격 시 1회성 사운드 프리팹 (클립 종료 시까지 재생)")]
        public GameObject hitSoundPrefab;

        [Tooltip("몬스터 사망 시 1회성 사운드 프리팹 (클립 종료 시까지 재생)")]
        public GameObject deathSoundPrefab;

        [Tooltip("이동/상시 루프 사운드 활성화 여부")]
        public bool enableMoveLoopSound = false;

        [Tooltip("이동/상시 루프 사운드 프리팹 (루프 재생, 정지/사망 시 해제)")]
        public GameObject moveLoopSoundPrefab;

        // ==========================================
        // 하위 호환성 프로퍼티 및 필드
        // ==========================================
        public float attackDamage
        {
            get => normalAttack != null ? normalAttack.attackDamage : 8f;
            set { if (normalAttack != null) normalAttack.attackDamage = value; }
        }

        public float attackRange
        {
            get
            {
                if (normalAttack != null && normalAttack.attackRange > 0f)
                {
                    return normalAttack.attackRange;
                }
                return GetDefaultAttackRange(type);
            }
            set { if (normalAttack != null) normalAttack.attackRange = value; }
        }

        public float attackCooldown
        {
            get
            {
                if (normalAttack != null && normalAttack.attackCooldown > 0f)
                {
                    return normalAttack.attackCooldown;
                }
                return GetDefaultAttackCooldown(type);
            }
            set { if (normalAttack != null) normalAttack.attackCooldown = value; }
        }

        public float windupTime
        {
            get => normalAttack != null ? normalAttack.windupTime : 0.2f;
            set { if (normalAttack != null) normalAttack.windupTime = value; }
        }

        public float recoveryTime
        {
            get => normalAttack != null ? normalAttack.recoveryTime : (postAttackDelay > 0f ? postAttackDelay : 0.2f);
            set { if (normalAttack != null) normalAttack.recoveryTime = value; postAttackDelay = value; }
        }

        public float hitboxRadius
        {
            get => normalAttack != null ? normalAttack.hitboxRadius : 1.0f;
            set { if (normalAttack != null) normalAttack.hitboxRadius = value; }
        }

        public static float GetDefaultAttackRange(MonsterType type)
        {
            return type switch
            {
                MonsterType.Minion => 5.0f,
                MonsterType.Minion_Archer => 15.0f,
                MonsterType.Minion_BigMon => 20.0f,
                MonsterType.Minion_Zhu => 20.0f,
                MonsterType.Elite => 3.0f,
                MonsterType.FoxBoss => 3.5f,
                MonsterType.Dokkaebi_Fire => 14.0f,
                MonsterType.Boar_Charger => 15.0f,
                MonsterType.Spider_Tether => 12.0f,
                MonsterType.Mud_Slime => 9.0f,
                MonsterType.Phantom_Fox => 6.0f,
                MonsterType.Iron_Shield => 3.5f,
                MonsterType.Energy_Absorber => 3.0f,
                MonsterType.Bomb_Sparrow => 8.0f,
                MonsterType.Totem_Turret => 16.0f,
                _ => 2f
            };
        }

        public static float GetDefaultAttackCooldown(MonsterType type)
        {
            return type switch
            {
                MonsterType.Minion => 1.0f,
                MonsterType.Minion_Archer => 0.7f,
                MonsterType.Minion_BigMon => 3.5f,
                MonsterType.Minion_Zhu => 3.0f,
                MonsterType.Elite => 1.8f,
                MonsterType.FoxBoss => 1.2f,
                MonsterType.Dokkaebi_Fire => 2.5f,
                MonsterType.Boar_Charger => 3.0f,
                MonsterType.Spider_Tether => 4.5f,
                MonsterType.Mud_Slime => 5.0f,
                MonsterType.Phantom_Fox => 2.0f,
                MonsterType.Iron_Shield => 2.0f,
                MonsterType.Energy_Absorber => 1.5f,
                MonsterType.Bomb_Sparrow => 1.0f,
                MonsterType.Totem_Turret => 1.0f,
                _ => 1.2f
            };
        }

        public bool dropsBossKey
        {
            get => specialDrops != null && specialDrops.Exists(d => d.itemId == "BossKey" && d.dropChance > 0f);
            set
            {
                if (value)
                {
                    if (specialDrops == null) specialDrops = new List<SpecialDropEntry>();
                    if (!specialDrops.Exists(d => d.itemId == "BossKey"))
                    {
                        specialDrops.Add(new SpecialDropEntry { itemId = "BossKey", itemName = "보스방 황금 열쇠", dropChance = 1f });
                    }
                }
                else
                {
                    specialDrops?.RemoveAll(d => d.itemId == "BossKey");
                }
            }
        }

        // 기존 필드 하위 호환
        public float kiteDistance = 0.0f;
        public float knockbackPower = 2.0f;
        public float telegraphDuration = 0.4f;
        public float specialAttackRadius = 2.5f;
        public float specialAttackAngle = 90.0f;
        public float postAttackDelay = 0.2f;
        public float dashDistance = 5.0f;
        public float dashWidth = 0.5f;
        public float dashDuration = 0.3f;
    }

    /// <summary>
    /// 통합 몬스터 데이터 테이블 (ScriptableObject)
    /// </summary>
    [CreateAssetMenu(fileName = "MonsterStatsTable", menuName = "MythOfGuotaji/Tables/MonsterStatsTable")]
    public class MonsterStatsTableSO : ScriptableObject
    {
        [Header("몬스터 간 충돌/밀어내기/마찰 전역 물리 설정 (테스트 및 밸런싱용)")]
        [Tooltip("돌진 등 스킬로 이동 시 문/벽/패링/플레이어 외 다른 몬스터와의 충돌을 완전히 무시합니다.")]
        public bool ignoreMonsterCollisionDuringSkillDash = true;

        [Tooltip("몬스터끼리 겹치거나 비빌 때 발생하는 접선 마찰 계수 (0 = 무마찰로 매끄럽게 미끄러짐, 1 = 정지 마찰)")]
        [Range(0f, 1f)]
        public float monsterCollisionFriction = 0.02f;

        [Tooltip("몬스터끼리 겹쳤을 때 서로 밀어내는 단단함(탄성 강도) (높을수록 단단하게 즉시 밀어냄)")]
        [Range(1f, 40f)]
        public float monsterSeparationPushStiffness = 14.0f;

        [Tooltip("한 지점에 동시에 스폰되거나 정지 상태에서 겹쳤을 때 자연스럽게 미끄러져 퍼지는 최대 속도 (m/s)")]
        [Range(1f, 30f)]
        public float monsterSpawnOverlapSlideSpeed = 10.0f;

        [Tooltip("기본 이동 시 앞을 가로막은 다른 몬스터를 돌아서 미끄러져 나가는 우회 가중치")]
        [Range(0f, 3f)]
        public float monsterDetourSlideWeight = 1.15f;

        [Tooltip("separationRadius 대비 단단한 몸체 반경 비율 (이 반경 안으로 겹치면 강한 반발력으로 밀어냄)")]
        [Range(0.3f, 1.2f)]
        public float monsterBodyRadiusRatio = 0.85f;

        [Header("몬스터 엔트리 목록")]
        public List<MonsterStatEntry> entries = new();

        public MonsterStatEntry GetStat(MonsterType type)
        {
            var found = entries.Find(e => e != null && e.type == type);
            if (found != null) return found;

            return GetDefaultStat(type);
        }

        public GameObject GetMonsterPrefab(MonsterType type)
        {
            var stat = GetStat(type);
            if (stat != null && stat.monsterPrefab != null)
            {
                return stat.monsterPrefab;
            }
            return null;
        }

        public static MonsterStatEntry GetDefaultStat(MonsterType type)
        {
            return type switch
            {
                MonsterType.Minion => new MonsterStatEntry
                {
                    type = MonsterType.Minion,
                    displayName = "산해경 잡몹",
                    description = "플레이어를 향해 떼지어 몰려들며 5m 돌진 충격파를 사용하는 기본 요괴",
                    category = "Normal",
                    maxHp = 35f,
                    moveSpeed = 4.2f,
                    defense = 0f,
                    separationRadius = 0.9f,
                    scaleMultiplier = 1.0f,
                    tintColor = Color.white,
                    gemDropCount = 1,
                    expPerGem = 1,
                    goldReward = 3,
                    affinityReward = 1,
                    heartDropChance = 0.25f,
                    heartHealAmount = 5.0f,
                    normalAttack = new NormalAttackInfo
                    {
                        attackRange = 5.0f,
                        attackDamage = 6.0f,
                        attackCooldown = 1.0f,
                        hitboxRadius = 0.8f
                    },
                    specialSkillIds = new List<string> { "minion_dash" },
                    dashDistance = 5.0f,
                    dashWidth = 0.5f,
                    dashDuration = 0.3f,
                    telegraphDuration = 0.3f
                },
                MonsterType.Minion_Zhu => new MonsterStatEntry
                {
                    type = MonsterType.Minion_Zhu,
                    displayName = "산해경 주(Zhu)",
                    description = "벽을 무시하는 포물선 곡사 박격포를 투척하는 원거리 곡사형 요괴",
                    category = "Ranged",
                    maxHp = 50f,
                    moveSpeed = 3.2f,
                    defense = 2f,
                    separationRadius = 1.2f,
                    scaleMultiplier = 1.2f,
                    tintColor = new Color(1f, 0.65f, 0.65f),
                    gemDropCount = 2,
                    expPerGem = 2,
                    goldReward = 8,
                    affinityReward = 2,
                    heartDropChance = 0.30f,
                    heartHealAmount = 5.0f,
                    normalAttack = new NormalAttackInfo
                    {
                        attackRange = 20.0f,
                        attackDamage = 10.0f,
                        attackCooldown = 3.0f,
                        hitboxRadius = 1.2f
                    },
                    specialSkillIds = new List<string> { "zhu_parabolic" },
                    specialAttackRadius = 2.5f,
                    postAttackDelay = 0.3f,
                    telegraphDuration = 0.5f
                },
                MonsterType.Minion_BigMon => new MonsterStatEntry
                {
                    type = MonsterType.Minion_BigMon,
                    displayName = "황동 거북 요괴",
                    description = "높은 체력과 방어력으로 플레이어를 향해 20m를 쇄도하는 중장갑 돌진 요괴",
                    category = "Heavy",
                    maxHp = 90f,
                    moveSpeed = 2.8f,
                    defense = 5f,
                    separationRadius = 1.6f,
                    scaleMultiplier = 1.5f,
                    tintColor = new Color(0.7f, 0.85f, 0.7f),
                    gemDropCount = 3,
                    expPerGem = 2,
                    goldReward = 12,
                    affinityReward = 3,
                    heartDropChance = 0.35f,
                    heartHealAmount = 8.0f,
                    normalAttack = new NormalAttackInfo
                    {
                        attackRange = 20.0f,
                        attackDamage = 14.0f,
                        attackCooldown = 3.5f,
                        hitboxRadius = 1.4f
                    },
                    specialSkillIds = new List<string> { "turtle_charge" },
                    dashDistance = 20.0f,
                    dashWidth = 1.5f,
                    dashDuration = 3.0f,
                    telegraphDuration = 0.4f,
                    specialAttackRadius = 3.0f
                },
                MonsterType.Minion_Archer => new MonsterStatEntry
                {
                    type = MonsterType.Minion_Archer,
                    displayName = "산해경 비충 요괴",
                    description = "빠른 속도로 원거리 독침을 쏘며 거리가 좁혀지면 후퇴하는 기동형 요괴",
                    category = "Ranged",
                    maxHp = 30f,
                    moveSpeed = 4.8f,
                    defense = 0f,
                    separationRadius = 1.0f,
                    scaleMultiplier = 0.9f,
                    tintColor = new Color(0.85f, 0.75f, 1f),
                    gemDropCount = 1,
                    expPerGem = 1,
                    goldReward = 4,
                    affinityReward = 1,
                    heartDropChance = 0.20f,
                    heartHealAmount = 5.0f,
                    normalAttack = new NormalAttackInfo
                    {
                        attackRange = 15.0f,
                        attackDamage = 7.0f,
                        attackCooldown = 0.7f,
                        hitboxRadius = 0.6f
                    },
                    specialSkillIds = new List<string> { "toxic_spit" },
                    kiteDistance = 6.0f,
                    telegraphDuration = 0.3f
                },
                MonsterType.Elite => new MonsterStatEntry
                {
                    type = MonsterType.Elite,
                    displayName = "열쇠지기 중간보스 (Key Keeper)",
                    description = "중간보스방에서 2차 웨이브에 등장하며, 거대한 바위를 굴리고 사망 시 보스방 황금 열쇠를 100% 확정 드롭함",
                    category = "Elite",
                    maxHp = 220f,
                    moveSpeed = 3.6f,
                    defense = 8f,
                    separationRadius = 2.2f,
                    scaleMultiplier = 1.8f,
                    tintColor = new Color(1.0f, 0.85f, 0.2f),
                    gemDropCount = 6,
                    expPerGem = 3,
                    goldReward = 40,
                    affinityReward = 8,
                    heartDropChance = 1.0f,
                    heartHealAmount = 15.0f,
                    specialDrops = new List<SpecialDropEntry>
                    {
                        new SpecialDropEntry
                        {
                            itemId = "BossKey",
                            itemName = "보스방 황금 열쇠",
                            dropChance = 1.0f
                        }
                    },
                    normalAttack = new NormalAttackInfo
                    {
                        attackRange = 3.0f,
                        attackDamage = 16.0f,
                        attackCooldown = 1.8f,
                        hitboxRadius = 1.6f
                    },
                    specialSkillIds = new List<string> { "elite_boulder" }
                },
                MonsterType.FoxBoss => new MonsterStatEntry
                {
                    type = MonsterType.FoxBoss,
                    displayName = "천년 여우 요괴 (Boss)",
                    description = "보스방의 2차 웨이브에서 강림하는 대요괴. 5m 영혼 흡인 소용돌이와 120도 부채꼴 요화 분쇄를 구사함",
                    category = "Boss",
                    maxHp = 600f,
                    moveSpeed = 4.0f,
                    defense = 12f,
                    separationRadius = 3.0f,
                    scaleMultiplier = 2.6f,
                    tintColor = new Color(1.0f, 0.35f, 0.35f),
                    gemDropCount = 15,
                    expPerGem = 5,
                    goldReward = 150,
                    affinityReward = 25,
                    heartDropChance = 1.0f,
                    heartHealAmount = 30.0f,
                    normalAttack = new NormalAttackInfo
                    {
                        attackRange = 3.5f,
                        attackDamage = 24.0f,
                        attackCooldown = 1.2f,
                        hitboxRadius = 2.0f
                    },
                    specialSkillIds = new List<string> { "boss_vacuum", "boss_cleave" }
                },
                MonsterType.Dokkaebi_Fire => new MonsterStatEntry
                {
                    type = MonsterType.Dokkaebi_Fire,
                    displayName = "도깨비 화염술사",
                    description = "패링 가능한 푸른 화염구를 발사하는 요괴. 패링 성공 시 역직격하여 보호막 분쇄 및 3초 기절",
                    category = "Ranged",
                    maxHp = 45f,
                    moveSpeed = 3.2f,
                    defense = 6f, // 마법 보호막
                    separationRadius = 1.2f,
                    scaleMultiplier = 1.1f,
                    tintColor = new Color(0.3f, 0.6f, 1.0f),
                    gemDropCount = 2,
                    expPerGem = 2,
                    goldReward = 8,
                    affinityReward = 2,
                    heartDropChance = 0.3f,
                    heartHealAmount = 5.0f,
                    normalAttack = new NormalAttackInfo
                    {
                        attackRange = 14.0f,
                        attackDamage = 10.0f,
                        attackCooldown = 2.5f,
                        hitboxRadius = 0.8f
                    },
                    specialSkillIds = new List<string> { "dokkaebi_fireball" },
                    kiteDistance = 5.0f
                },
                MonsterType.Boar_Charger => new MonsterStatEntry
                {
                    type = MonsterType.Boar_Charger,
                    displayName = "저돌맹진 멧돼지 요괴",
                    description = "플레이어를 향해 14m/s로 15m를 돌진하는 맹수. 돌진 직전 단검 패링 시 벽 충돌 기절, 또는 슬라이딩 무적 회피",
                    category = "Heavy",
                    maxHp = 70f,
                    moveSpeed = 3.5f,
                    defense = 3f,
                    separationRadius = 1.4f,
                    scaleMultiplier = 1.3f,
                    tintColor = new Color(0.7f, 0.45f, 0.3f),
                    gemDropCount = 2,
                    expPerGem = 2,
                    goldReward = 10,
                    affinityReward = 2,
                    heartDropChance = 0.35f,
                    heartHealAmount = 8.0f,
                    normalAttack = new NormalAttackInfo
                    {
                        attackRange = 15.0f,
                        attackDamage = 16.0f,
                        attackCooldown = 3.0f,
                        hitboxRadius = 1.2f
                    },
                    specialSkillIds = new List<string> { "boar_charge" },
                    dashDistance = 15.0f,
                    dashDuration = 1.1f,
                    dashWidth = 1.2f,
                    telegraphDuration = 0.35f
                },
                MonsterType.Spider_Tether => new MonsterStatEntry
                {
                    type = MonsterType.Spider_Tether,
                    displayName = "요선 거미 (사슬 견인)",
                    description = "12m 거리에서 명주실 사슬을 던져 플레이어를 끌어당김. 슬라이딩 무적 대시로 즉시 절단 탈출 가능",
                    category = "Support",
                    maxHp = 40f,
                    moveSpeed = 3.8f,
                    defense = 1f,
                    separationRadius = 1.1f,
                    scaleMultiplier = 1.0f,
                    tintColor = new Color(0.8f, 0.2f, 0.8f),
                    gemDropCount = 2,
                    expPerGem = 2,
                    goldReward = 7,
                    affinityReward = 2,
                    heartDropChance = 0.25f,
                    heartHealAmount = 5.0f,
                    normalAttack = new NormalAttackInfo
                    {
                        attackRange = 12.0f,
                        attackDamage = 8.0f,
                        attackCooldown = 4.5f,
                        hitboxRadius = 1.0f
                    },
                    specialSkillIds = new List<string> { "spider_web_tether" }
                },
                MonsterType.Mud_Slime => new MonsterStatEntry
                {
                    type = MonsterType.Mud_Slime,
                    displayName = "진흙 이괴 (장판 바인드)",
                    description = "3m 진흙 늪지 장판을 소환하여 이동속도를 60% 감속시키고 카이팅을 봉쇄하는 점액 요괴",
                    category = "Support",
                    maxHp = 55f,
                    moveSpeed = 2.0f,
                    defense = 4f,
                    separationRadius = 1.3f,
                    scaleMultiplier = 1.2f,
                    tintColor = new Color(0.45f, 0.35f, 0.2f),
                    gemDropCount = 2,
                    expPerGem = 2,
                    goldReward = 8,
                    affinityReward = 2,
                    heartDropChance = 0.3f,
                    heartHealAmount = 6.0f,
                    normalAttack = new NormalAttackInfo
                    {
                        attackRange = 9.0f,
                        attackDamage = 6.0f,
                        attackCooldown = 5.0f,
                        hitboxRadius = 1.2f
                    },
                    specialSkillIds = new List<string> { "mud_puddle" }
                },
                MonsterType.Phantom_Fox => new MonsterStatEntry
                {
                    type = MonsterType.Phantom_Fox,
                    displayName = "환영 여우귀 (분신 잔상술)",
                    description = "피격 시 본체 1마리와 체력 1짜리 가짜 환영 2마리로 3방향 분신. 관통 화살이나 단검으로 분쇄",
                    category = "Agile",
                    maxHp = 35f,
                    moveSpeed = 5.0f,
                    defense = 0f,
                    separationRadius = 1.0f,
                    scaleMultiplier = 0.95f,
                    tintColor = new Color(1.0f, 0.5f, 0.7f),
                    gemDropCount = 1,
                    expPerGem = 2,
                    goldReward = 9,
                    affinityReward = 2,
                    heartDropChance = 0.25f,
                    heartHealAmount = 5.0f,
                    normalAttack = new NormalAttackInfo
                    {
                        attackRange = 6.0f,
                        attackDamage = 8.0f,
                        attackCooldown = 2.0f,
                        hitboxRadius = 0.8f
                    },
                    specialSkillIds = new List<string> { "phantom_decoy" }
                },
                MonsterType.Iron_Shield => new MonsterStatEntry
                {
                    type = MonsterType.Iron_Shield,
                    displayName = "흑철 방패병 (풀차징 필수)",
                    description = "철갑 방패로 일반 화살을 100% 도탄(0 피해). 오직 3단계 풀차징 관통 화살로만 방패를 파괴하여 격파 가능",
                    category = "Heavy",
                    maxHp = 100f,
                    moveSpeed = 2.2f,
                    defense = 10f,
                    separationRadius = 1.5f,
                    scaleMultiplier = 1.35f,
                    tintColor = new Color(0.4f, 0.45f, 0.55f),
                    gemDropCount = 3,
                    expPerGem = 3,
                    goldReward = 15,
                    affinityReward = 3,
                    heartDropChance = 0.4f,
                    heartHealAmount = 10.0f,
                    normalAttack = new NormalAttackInfo
                    {
                        attackRange = 3.5f,
                        attackDamage = 14.0f,
                        attackCooldown = 2.0f,
                        hitboxRadius = 1.2f
                    },
                    specialSkillIds = new List<string> { "iron_shield_stance" }
                },
                MonsterType.Energy_Absorber => new MonsterStatEntry
                {
                    type = MonsterType.Energy_Absorber,
                    displayName = "영력 흡수 괴물 (차징 금지)",
                    description = "차징 샷 피격 시 체력 회복 및 1.4배 거대화 폭주. 오직 1단계 일반 연사나 단검 평타로만 피해 누적 처치",
                    category = "Gimmick",
                    maxHp = 80f,
                    moveSpeed = 3.0f,
                    defense = 2f,
                    separationRadius = 1.3f,
                    scaleMultiplier = 1.2f,
                    tintColor = new Color(0.6f, 0.2f, 0.9f),
                    gemDropCount = 3,
                    expPerGem = 3,
                    goldReward = 14,
                    affinityReward = 3,
                    heartDropChance = 0.35f,
                    heartHealAmount = 8.0f,
                    normalAttack = new NormalAttackInfo
                    {
                        attackRange = 3.0f,
                        attackDamage = 10.0f,
                        attackCooldown = 1.5f,
                        hitboxRadius = 1.0f
                    },
                    specialSkillIds = new List<string> { "energy_absorb" }
                },
                MonsterType.Bomb_Sparrow => new MonsterStatEntry
                {
                    type = MonsterType.Bomb_Sparrow,
                    displayName = "자폭 참새 떼 (초고속 물량)",
                    description = "체력 10의 초고속 자폭 요괴. 12m/s로 날아와 2m 자폭 폭발. 버스트 사격 요격 또는 슬라이딩 0.5초 무적 회피가 핵심",
                    category = "Swarm",
                    maxHp = 10f,
                    moveSpeed = 12.0f,
                    defense = 0f,
                    separationRadius = 0.6f,
                    scaleMultiplier = 0.65f,
                    tintColor = new Color(1.0f, 0.3f, 0.2f),
                    gemDropCount = 1,
                    expPerGem = 1,
                    goldReward = 2,
                    affinityReward = 1,
                    heartDropChance = 0.15f,
                    heartHealAmount = 4.0f,
                    normalAttack = new NormalAttackInfo
                    {
                        attackRange = 8.0f,
                        attackDamage = 18.0f,
                        attackCooldown = 1.0f,
                        hitboxRadius = 1.0f
                    },
                    specialSkillIds = new List<string> { "sparrow_kamikaze" },
                    dashDistance = 8.0f,
                    dashDuration = 0.6f,
                    dashWidth = 0.8f
                },
                MonsterType.Totem_Turret => new MonsterStatEntry
                {
                    type = MonsterType.Totem_Turret,
                    displayName = "사방신 토템 (고정 포대)",
                    description = "이동하지 않는 고정형 석상. 4방향 회전 탄환을 지속 발사. 모든 탄환은 단검으로 패링하여 역폭파 가능",
                    category = "Turret",
                    maxHp = 120f,
                    moveSpeed = 0f, // 고정 포대
                    defense = 5f,
                    separationRadius = 2.0f,
                    scaleMultiplier = 1.6f,
                    tintColor = new Color(0.85f, 0.85f, 0.7f),
                    gemDropCount = 4,
                    expPerGem = 3,
                    goldReward = 20,
                    affinityReward = 4,
                    heartDropChance = 0.5f,
                    heartHealAmount = 12.0f,
                    normalAttack = new NormalAttackInfo
                    {
                        attackRange = 16.0f,
                        attackDamage = 8.0f,
                        attackCooldown = 1.0f,
                        hitboxRadius = 1.0f
                    },
                    specialSkillIds = new List<string> { "totem_turret_barrage" }
                },
                _ => new MonsterStatEntry
                {
                    type = type,
                    displayName = type.ToString(),
                    maxHp = 40f,
                    moveSpeed = 4f,
                    attackDamage = 8f,
                    attackRange = 2f,
                    attackCooldown = 1.2f
                }
            };
        }
    }
}
