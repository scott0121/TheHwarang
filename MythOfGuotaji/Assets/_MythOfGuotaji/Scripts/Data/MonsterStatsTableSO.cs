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

        [Tooltip("드롭 시 재생할 VFX")]
        public string dropVFX = "HCFX_Marble_02_Get";
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

        [Tooltip("공격 궤적 휘두르기 VFX")]
        public string attackVFX = "HCFX_Slash_01";

        [Tooltip("적중 시 피격 충격 VFX")]
        public string hitVFX = "HCFX_Dust_FloorHit_01";
    }

    /// <summary>
    /// 통합 몬스터 데이터 엔트리
    /// - 기본 정보 / Stats / Drop Rewards / Special Drops / Attack Normal / Attack Special
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

        // ==========================================
        // 하위 호환성 프로퍼티 및 필드 (기존 코드 100% 호환)
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
                // [기획 규칙] 몬스터별 실제 스킬 발동/교전 사거리 보장
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
                    _ => normalAttack != null ? normalAttack.attackRange : 2f
                };
            }
            set { if (normalAttack != null) normalAttack.attackRange = value; }
        }

        public float attackCooldown
        {
            get
            {
                // [기획 규칙] 몬스터별 패턴 쿨다운 보장
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
                    _ => normalAttack != null ? normalAttack.attackCooldown : 1.2f
                };
            }
            set { if (normalAttack != null) normalAttack.attackCooldown = value; }
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
        [Header("투사체 및 패링 설정")]
        [Tooltip("몬스터 발사 투사체의 패링 분류 (Normal: 기본 푸른/초록 패링가능, Enhanced: 강화 황금색 패링가능 2배데미지, Unparryable: 패링불가 붉은색)")]
        public Combat.ProjectileParryType projectileParryType = Combat.ProjectileParryType.Normal;
        public float projectileSpeed = 6.0f;
        public float kiteDistance = 0.0f;
        public float knockbackPower = 2.0f;
        public float telegraphDuration = 0.4f;
        public float specialAttackRadius = 2.5f;
        public float specialAttackAngle = 90.0f;
        public float flightDuration = 1.8f;
        public float arcHeight = 6.0f;
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
        public List<MonsterStatEntry> entries = new();

        public MonsterStatEntry GetStat(MonsterType type)
        {
            var found = entries.Find(e => e != null && e.type == type);
            if (found != null) return found;

            return GetDefaultStat(type);
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
                        attackRange = 1.8f,
                        attackDamage = 6.0f,
                        attackCooldown = 1.0f,
                        hitboxRadius = 0.8f,
                        attackVFX = "HCFX_Slash_01"
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
                        attackRange = 8.5f,
                        attackDamage = 10.0f,
                        attackCooldown = 3.0f,
                        hitboxRadius = 1.2f
                    },
                    specialSkillIds = new List<string> { "zhu_parabolic" },
                    flightDuration = 2.0f,
                    arcHeight = 6.0f,
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
                        attackRange = 2.2f,
                        attackDamage = 14.0f,
                        attackCooldown = 2.0f,
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
                        attackRange = 7.0f,
                        attackDamage = 7.0f,
                        attackCooldown = 1.8f,
                        hitboxRadius = 0.6f
                    },
                    specialSkillIds = new List<string> { "toxic_spit" },
                    projectileSpeed = 6.0f,
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
                            dropChance = 1.0f,
                            dropVFX = "HCFX_Marble_02_Get"
                        }
                    },
                    normalAttack = new NormalAttackInfo
                    {
                        attackRange = 2.8f,
                        attackDamage = 16.0f,
                        attackCooldown = 1.5f,
                        hitboxRadius = 1.6f,
                        attackVFX = "HCFX_Slash_01"
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
                        attackRange = 3.2f,
                        attackDamage = 24.0f,
                        attackCooldown = 1.8f,
                        hitboxRadius = 2.0f,
                        attackVFX = "HCFX_Slash_01"
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
                        hitboxRadius = 0.8f,
                        attackVFX = "HCFX_ElementOrb_02"
                    },
                    specialSkillIds = new List<string> { "dokkaebi_fireball" },
                    projectileSpeed = 5.0f,
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
                        hitboxRadius = 1.2f,
                        attackVFX = "HCFX_Dust_Dash_01"
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
                        hitboxRadius = 1.0f,
                        attackVFX = "HCFX_Poison_02"
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
                        hitboxRadius = 1.2f,
                        attackVFX = "HCFX_Poison_01"
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
                        hitboxRadius = 0.8f,
                        attackVFX = "HCFX_Slash_01"
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
                        hitboxRadius = 1.2f,
                        attackVFX = "HCFX_Hit_01"
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
                        hitboxRadius = 1.0f,
                        attackVFX = "HCFX_Energy_01"
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
                        hitboxRadius = 1.0f,
                        attackVFX = "HCFX_Explosion_02"
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
                    projectileParryType = Combat.ProjectileParryType.Enhanced, // [강화 투사체]: 황금색, 패링 시 2배 데미지
                    normalAttack = new NormalAttackInfo
                    {
                        attackRange = 16.0f,
                        attackDamage = 8.0f,
                        attackCooldown = 1.0f,
                        hitboxRadius = 1.0f,
                        attackVFX = "HCFX_MagicCircle_02"
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
