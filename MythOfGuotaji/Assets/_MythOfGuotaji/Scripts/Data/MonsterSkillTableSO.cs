using System;
using System.Collections.Generic;
using UnityEngine;

namespace MythOfGuotaji.Data
{
    /// <summary>
    /// 몬스터 스킬 패턴 유형
    /// </summary>
    public enum MonsterSkillType
    {
        MeleeAttack,        // 기본 근접 연타/후리기
        MinionDash,         // 잡몹 전용 5m 돌진 & 착지 0.7m 충격파
        TurtleCharge,       // 대형 거북 8m~20m 돌진 분쇄
        RangedStraight,     // 독침/화살 등의 직선 원거리 투사체 & 카이팅
        RangedParabolic,    // Zhu 등의 포물선 곡사 투척 (벽 무시)
        EliteBoulder,       // 열쇠 엘리트의 굴러가는 바위 투척
        BossVacuum,         // 천년여우 1번 패턴: 5m 영혼 흡인 소용돌이
        BossCleave,         // 천년여우 2번 패턴: 3.5m 120도 부채꼴 요화 분쇄
        AOESlam,            // 지면 강타 충격파
        SummonMinions,      // 부하 요괴 소환 / 환영 분신
        ParryReflect,       // 패링 유도 반사형 투사체 (도깨비 화염구)
        TetherPull,         // 사슬/거미줄 견인 (슬라이딩 탈출)
        ShieldGuard,        // 전면 철갑 방패 가드 (풀차징 화살로 파괴)
        EnergyAbsorb,       // 에너지 흡수 폭주 (차징 금지, 일반사격 전용)
        KamikazeExplode,    // 초고속 자폭 돌진
        TurretBarrage       // 고정 포대 회전 사방 탄막
    }

    /// <summary>
    /// 몬스터 공격 패턴 구조화 엔트리
    /// - 여러 몬스터가 동일/유사한 스킬을 공유하고 재사용할 수 있도록 독립 테이블로 설계
    /// </summary>
    [Serializable]
    public class MonsterSkillEntry
    {
        [Header("기본 정보")]
        [Tooltip("고유 스킬 식별자 (e.g. minion_dash, turtle_charge, zhu_parabolic, boss_vacuum, boss_cleave, elite_boulder)")]
        public string skillId = "skill_id";

        [Tooltip("스킬 명칭")]
        public string skillName = "스킬 이름";

        [Tooltip("스킬 동작 설명")]
        [TextArea(2, 4)]
        public string description = "스킬 상세 설명";

        [Tooltip("스킬 패턴 유형")]
        public MonsterSkillType skillType = MonsterSkillType.MeleeAttack;

        [Header("사거리 및 발동 조건")]
        [Tooltip("스킬 발동 최소 사거리 (m)")]
        public float minRange = 0f;

        [Tooltip("스킬 발동 최대 사거리 (m)")]
        public float maxRange = 5.0f;

        [Header("공격력 및 쿨다운")]
        [Tooltip("공격력 계수 (몬스터 기본 공격력의 배수)")]
        public float damageMultiplier = 1.0f;

        [Tooltip("고정 피해량 (0보다 크면 계수 대신 고정 피해 적용)")]
        public float fixedDamage = 0f;

        [Tooltip("스킬 재사용 대기시간 (초)")]
        public float cooldown = 3.0f;

        [Tooltip("피격 시 플레이어 넉백 파워 (m)")]
        public float knockbackPower = 2.0f;

        [Header("전조(Telegraph) 및 범위(AOE)")]
        [Tooltip("스킬 시전 전 예고 장판 표시 시간 (초)")]
        public float telegraphDuration = 0.4f;

        [Tooltip("전조 장판 형태 (None, Line, Circle, Sector, Box)")]
        public string telegraphShape = "Circle";

        [Tooltip("타격/폭발/장판 반경 (m)")]
        public float aoeRadius = 1.5f;

        [Tooltip("부채꼴 타격 각도 (도, Sector 형태일 때)")]
        public float aoeAngle = 90.0f;

        [Tooltip("공격 후 이동 불가 후딜레이 시간 (초)")]
        public float postAttackDelay = 0.3f;

        [Header("돌진 및 기동 (Dash / Charge)")]
        [Tooltip("돌진 이동 거리 (m)")]
        public float dashDistance = 5.0f;

        [Tooltip("돌진 이동 속도 (m/s)")]
        public float dashSpeed = 12.0f;

        [Tooltip("돌진 지속 시간 (초)")]
        public float dashDuration = 0.3f;

        [Tooltip("돌진 판정 폭 (m)")]
        public float dashWidth = 0.8f;

        [Header("투사체 및 원거리 (Projectile)")]
        [Tooltip("직선 투사체 비행 속도 (m/s)")]
        public float projectileSpeed = 6.0f;

        [Tooltip("포물선 곡사 비행 시간 (초)")]
        public float flightDuration = 1.8f;

        [Tooltip("포물선 정점 높이 (m)")]
        public float arcHeight = 6.0f;

        [Tooltip("동시 발사체 개수")]
        public int projectileCount = 1;

        [Tooltip("다중 발사 시 탄퍼짐 각도 (도)")]
        public float spreadAngle = 0f;

        [Tooltip("관통 횟수 (0이면 단일 적중 소멸)")]
        public int pierceCount = 0;

        [Tooltip("투사체 프리팹 리소스 이름")]
        public string projectilePrefabName = "";

        [Tooltip("투사체의 패링 분류 (Normal: 기본 푸른/초록 패링가능, Enhanced: 강화 황금색 2배피해, Unparryable: 패링불가 붉은색)")]
        public Combat.ProjectileParryType projectileParryType = Combat.ProjectileParryType.Normal;

        [Header("비주얼 및 사운드 (VFX / SFX)")]
        [Tooltip("시전 전조 장판 이펙트 이름")]
        public string telegraphVFX = "";

        [Tooltip("시전/발사구 머즐 이펙트 이름")]
        public string castVFX = "";

        [Tooltip("비행/돌진 궤적 트레일 이펙트 이름")]
        public string trailVFX = "";

        [Tooltip("적중/착지/폭발 충격 이펙트 이름")]
        public string impactVFX = "";

        [Tooltip("사운드 큐 이름")]
        public string soundCue = "";
    }

    /// <summary>
    /// 몬스터 스킬 패턴 테이블 (ScriptableObject)
    /// - 모든 몬스터의 특수 스킬/공격 패턴을 중앙 집중 구조화하여 보관
    /// </summary>
    [CreateAssetMenu(fileName = "MonsterSkillTable", menuName = "MythOfGuotaji/Tables/MonsterSkillTable")]
    public class MonsterSkillTableSO : ScriptableObject
    {
        public List<MonsterSkillEntry> skills = new();

        public MonsterSkillEntry GetSkill(string skillId)
        {
            if (string.IsNullOrEmpty(skillId)) return null;
            var found = skills.Find(s => s != null && s.skillId.Equals(skillId, StringComparison.OrdinalIgnoreCase));
            if (found != null) return found;

            return GetDefaultSkill(skillId);
        }

        public static MonsterSkillEntry GetDefaultSkill(string skillId)
        {
            return skillId?.ToLower() switch
            {
                "minion_dash" => new MonsterSkillEntry
                {
                    skillId = "minion_dash",
                    skillName = "미니언 5m 돌진 & 충격파",
                    description = "5m 내 접근 시 0.3초 예고 후 즉시 돌진하여 착지 지점 반경 70cm 광역 피해",
                    skillType = MonsterSkillType.MinionDash,
                    minRange = 1.5f,
                    maxRange = 5.0f,
                    damageMultiplier = 1.0f,
                    cooldown = 1.0f,
                    telegraphDuration = 0.3f,
                    telegraphShape = "Box",
                    aoeRadius = 0.7f,
                    dashDistance = 5.0f,
                    dashSpeed = 16.0f,
                    dashDuration = 0.3f,
                    dashWidth = 0.5f,
                    knockbackPower = 1.5f,
                    telegraphVFX = "Telegraph_Rect_5x0.5",
                    castVFX = "HCFX_Dust_01",
                    trailVFX = "HCFX_Dust_01",
                    impactVFX = "HCFX_Dust_FloorHit_01",
                    soundCue = "Minion_Dash"
                },
                "turtle_charge" => new MonsterSkillEntry
                {
                    skillId = "turtle_charge",
                    skillName = "황동 거북 8m 돌진 분쇄",
                    description = "플레이어를 향해 0.4초 차징 후 8m 거리를 10m/s로 쇄도하여 경로 상의 적을 날려버림",
                    skillType = MonsterSkillType.TurtleCharge,
                    minRange = 2.0f,
                    maxRange = 8.0f,
                    damageMultiplier = 1.5f,
                    fixedDamage = 22.0f,
                    cooldown = 4.0f,
                    telegraphDuration = 0.4f,
                    telegraphShape = "Box",
                    aoeRadius = 1.2f,
                    dashDistance = 8.0f,
                    dashSpeed = 10.0f,
                    dashDuration = 0.8f,
                    dashWidth = 1.2f,
                    knockbackPower = 4.0f,
                    telegraphVFX = "Telegraph_Rect_8x1.2",
                    castVFX = "HCFX_Dust_01",
                    trailVFX = "HCFX_Dust_01",
                    impactVFX = "HCFX_Dust_FloorHit_01",
                    soundCue = "Turtle_Charge"
                },
                "toxic_spit" => new MonsterSkillEntry
                {
                    skillId = "toxic_spit",
                    skillName = "독충 원거리 침사격",
                    description = "사거리 7.5m에서 6m/s 속도의 독침을 직선 발사하고 거리가 좁혀지면 카이팅 후퇴",
                    skillType = MonsterSkillType.RangedStraight,
                    minRange = 2.0f,
                    maxRange = 7.5f,
                    damageMultiplier = 1.0f,
                    fixedDamage = 8.0f,
                    cooldown = 2.0f,
                    telegraphDuration = 0.3f,
                    telegraphShape = "Line",
                    projectileSpeed = 6.0f,
                    projectileCount = 1,
                    projectilePrefabName = "Vespomorph_Projectile",
                    castVFX = "HCFX_Poison_01",
                    trailVFX = "HCFX_Poison_02",
                    impactVFX = "HCFX_Poison_01",
                    soundCue = "Toxic_Shoot"
                },
                "zhu_parabolic" => new MonsterSkillEntry
                {
                    skillId = "zhu_parabolic",
                    skillName = "Zhu 박격포 포물선 곡사 투척",
                    description = "벽을 무시하고 플레이어 발밑으로 6m 높이의 붉은 화염구를 포물선으로 투척, 착지 시 2.5m 폭발",
                    skillType = MonsterSkillType.RangedParabolic,
                    minRange = 2.5f,
                    maxRange = 8.5f,
                    damageMultiplier = 1.3f,
                    fixedDamage = 14.0f,
                    cooldown = 3.5f,
                    telegraphDuration = 0.5f,
                    telegraphShape = "Circle",
                    aoeRadius = 2.5f,
                    flightDuration = 1.8f,
                    arcHeight = 6.0f,
                    projectilePrefabName = "Zhu_Parabolic_Projectile",
                    telegraphVFX = "Telegraph_Circle_Red",
                    castVFX = "HCFX_Buff_Up",
                    impactVFX = "HCFX_Splash_01",
                    soundCue = "Zhu_Launch"
                },
                "elite_boulder" => new MonsterSkillEntry
                {
                    skillId = "elite_boulder",
                    skillName = "열쇠 엘리트 바위 굴리기",
                    description = "지면을 굴러가며 경로상의 모든 것을 짓밟는 거대한 암석을 투척하여 18 피해 및 넉백",
                    skillType = MonsterSkillType.EliteBoulder,
                    minRange = 2.0f,
                    maxRange = 9.0f,
                    damageMultiplier = 1.5f,
                    fixedDamage = 18.0f,
                    cooldown = 5.0f,
                    telegraphDuration = 0.6f,
                    telegraphShape = "Box",
                    aoeRadius = 2.0f,
                    projectileSpeed = 8.0f,
                    projectilePrefabName = "Boulder_Projectile",
                    knockbackPower = 5.0f,
                    telegraphVFX = "Telegraph_Rect_8x2",
                    castVFX = "HCFX_Dust_01",
                    impactVFX = "HCFX_Dust_FloorHit_01",
                    soundCue = "Elite_Boulder"
                },
                "boss_vacuum" => new MonsterSkillEntry
                {
                    skillId = "boss_vacuum",
                    skillName = "천년여우 1번: 영혼 흡인 소용돌이",
                    description = "보스 중심 5m 범위 내의 플레이어를 1.2초 동안 끌어당기며 이동을 봉쇄하고 지속 요화 피해",
                    skillType = MonsterSkillType.BossVacuum,
                    minRange = 0f,
                    maxRange = 5.5f,
                    damageMultiplier = 1.2f,
                    fixedDamage = 15.0f,
                    cooldown = 6.0f,
                    telegraphDuration = 0.5f,
                    telegraphShape = "Circle",
                    aoeRadius = 5.0f,
                    knockbackPower = -3.0f, // 음수: 끌어당김
                    telegraphVFX = "Telegraph_Circle_Purple",
                    castVFX = "HCFX_Portal_01",
                    impactVFX = "HCFX_Splash_02",
                    soundCue = "Boss_Vacuum"
                },
                "boss_cleave" => new MonsterSkillEntry
                {
                    skillId = "boss_cleave",
                    skillName = "천년여우 2번: 120도 부채꼴 요화 분쇄",
                    description = "전방 3.5m 120도 부채꼴 영역으로 날카로운 요호의 발톱을 휘둘러 28 피해 및 3m 넉백",
                    skillType = MonsterSkillType.BossCleave,
                    minRange = 0f,
                    maxRange = 3.5f,
                    damageMultiplier = 2.0f,
                    fixedDamage = 28.0f,
                    cooldown = 4.0f,
                    telegraphDuration = 0.4f,
                    telegraphShape = "Sector",
                    aoeRadius = 3.5f,
                    aoeAngle = 120.0f,
                    knockbackPower = 3.0f,
                    telegraphVFX = "Telegraph_Fan_120",
                    castVFX = "HCFX_Splash_02",
                    impactVFX = "HCFX_Slash_01",
                    soundCue = "Boss_Cleave"
                },
                // ==========================================
                // 신규 기믹형 몬스터 스킬 9종
                // ==========================================
                "dokkaebi_fireball" => new MonsterSkillEntry
                {
                    skillId = "dokkaebi_fireball",
                    skillName = "도깨비불 핑퐁 패링 화염구",
                    description = "원거리에서 5m/s 속도의 푸른 화염구를 발사. 단검으로 패링 시 2배 속도로 반사되어 적에게 직격하며 3초간 그로기 및 보호막 분쇄",
                    skillType = MonsterSkillType.ParryReflect,
                    minRange = 3.0f,
                    maxRange = 14.0f,
                    damageMultiplier = 1.0f,
                    fixedDamage = 12.0f,
                    cooldown = 2.5f,
                    projectileSpeed = 5.0f,
                    aoeRadius = 0.8f,
                    projectilePrefabName = "Dokkaebi_Fireball_Projectile",
                    castVFX = "HCFX_ElementOrb_02",
                    trailVFX = "HCFX_Fire_02",
                    impactVFX = "HCFX_Explosion_02",
                    soundCue = "Dokkaebi_Fire"
                },
                "boar_charge" => new MonsterSkillEntry
                {
                    skillId = "boar_charge",
                    skillName = "저돌맹진 멧돼지 돌진 분쇄",
                    description = "0.3초간 땅을 구른 뒤 14m/s 속도로 15m를 직선 돌진. 돌진 직전 0.25초 찰나에 단검 패링 시 뒤로 튕겨나가 벽에 충돌하며 기절, 또는 슬라이딩 무적으로 회피",
                    skillType = MonsterSkillType.TurtleCharge,
                    minRange = 2.5f,
                    maxRange = 15.0f,
                    damageMultiplier = 1.6f,
                    fixedDamage = 16.0f,
                    cooldown = 3.0f,
                    telegraphDuration = 0.35f,
                    telegraphShape = "Box",
                    dashDistance = 15.0f,
                    dashSpeed = 14.0f,
                    dashDuration = 1.1f,
                    dashWidth = 1.2f,
                    aoeRadius = 1.5f,
                    knockbackPower = 4.0f,
                    telegraphVFX = "Telegraph_Rect_15x1.2",
                    castVFX = "HCFX_Dust_Dash_01",
                    impactVFX = "HCFX_Dust_FloorHit_02",
                    soundCue = "Boar_Charge"
                },
                "spider_web_tether" => new MonsterSkillEntry
                {
                    skillId = "spider_web_tether",
                    skillName = "요선 거미 영혼 사슬 견인",
                    description = "12m 거리에서 끈적한 명주실 사슬을 던져 플레이어를 자신 앞으로 끌어당김. 슬라이딩 무적 대시로 사슬을 즉시 절단 탈출 가능",
                    skillType = MonsterSkillType.TetherPull,
                    minRange = 3.0f,
                    maxRange = 12.0f,
                    damageMultiplier = 0.5f,
                    fixedDamage = 8.0f,
                    cooldown = 4.5f,
                    telegraphDuration = 0.3f,
                    aoeRadius = 1.0f,
                    knockbackPower = -3.5f, // 견인
                    telegraphVFX = "Telegraph_Line_Tether",
                    castVFX = "HCFX_Poison_02",
                    impactVFX = "HCFX_Poison_01",
                    soundCue = "Spider_Tether"
                },
                "mud_puddle" => new MonsterSkillEntry
                {
                    skillId = "mud_puddle",
                    skillName = "진흙 이괴 늪지 슬로우 장판",
                    description = "플레이어 발밑에 반경 3m의 진흙 늪지를 소환하여 4초간 이동속도를 60% 감속시키고 카이팅을 봉쇄",
                    skillType = MonsterSkillType.AOESlam,
                    minRange = 1.0f,
                    maxRange = 9.0f,
                    damageMultiplier = 0.4f,
                    fixedDamage = 5.0f,
                    cooldown = 5.0f,
                    telegraphDuration = 0.5f,
                    telegraphShape = "Circle",
                    aoeRadius = 3.0f,
                    telegraphVFX = "Telegraph_Circle_Mud",
                    castVFX = "HCFX_Poison_01",
                    impactVFX = "HCFX_Splash_03",
                    soundCue = "Mud_Puddle"
                },
                "phantom_decoy" => new MonsterSkillEntry
                {
                    skillId = "phantom_decoy",
                    skillName = "환영 여우귀 3방향 분신술",
                    description = "피격 시 연막을 터뜨리며 본체 1마리와 체력 1짜리 가짜 환영 2마리로 3방향 순간 분신. 관통 화살이나 단검 베기로 한 번에 분쇄 가능",
                    skillType = MonsterSkillType.SummonMinions,
                    minRange = 0f,
                    maxRange = 10.0f,
                    cooldown = 7.0f,
                    telegraphDuration = 0.1f,
                    aoeRadius = 2.0f,
                    castVFX = "HCFX_Flash_01",
                    impactVFX = "HCFX_Feathers",
                    soundCue = "Fox_Decoy"
                },
                "iron_shield_stance" => new MonsterSkillEntry
                {
                    skillId = "iron_shield_stance",
                    skillName = "흑철 방패 가드 (풀차징 필수)",
                    description = "전면에 철갑 방패를 세워 일반 화살을 100% 도탄(0 피해). 오직 3단계 풀차징 관통 화살로만 방패를 분쇄(Armor Break)하여 격파 가능",
                    skillType = MonsterSkillType.ShieldGuard,
                    minRange = 0f,
                    maxRange = 4.0f,
                    cooldown = 2.0f,
                    aoeAngle = 120.0f,
                    castVFX = "HCFX_Hit_01",
                    impactVFX = "HCFX_Hit_02",
                    soundCue = "Shield_Block"
                },
                "energy_absorb" => new MonsterSkillEntry
                {
                    skillId = "energy_absorb",
                    skillName = "영력 흡수 괴물 (차징 금지)",
                    description = "강력한 차징 샷을 맞으면 에너지를 흡수하여 체력을 회복하고 1.4배 폭주. 오직 빠른 1단계 일반 연사나 단검 평타로만 피해 누적 처치",
                    skillType = MonsterSkillType.EnergyAbsorb,
                    minRange = 0f,
                    maxRange = 3.0f,
                    cooldown = 1.5f,
                    castVFX = "HCFX_Buff_Up",
                    impactVFX = "HCFX_Energy_01",
                    soundCue = "Energy_Absorb"
                },
                "sparrow_kamikaze" => new MonsterSkillEntry
                {
                    skillId = "sparrow_kamikaze",
                    skillName = "자폭 참새 떼 12m/s 급강하",
                    description = "체력 10의 초고속 자폭 요괴. 12m/s로 날아와 2m 자폭 폭발을 일으킴. 버스트 사격 요격 또는 슬라이딩 0.5초 무적 회피가 핵심",
                    skillType = MonsterSkillType.KamikazeExplode,
                    minRange = 0.5f,
                    maxRange = 12.0f,
                    damageMultiplier = 1.5f,
                    fixedDamage = 18.0f,
                    cooldown = 1.0f,
                    dashDistance = 8.0f,
                    dashSpeed = 12.0f,
                    dashDuration = 0.6f,
                    aoeRadius = 2.0f,
                    castVFX = "HCFX_Energy_02",
                    impactVFX = "HCFX_Explosion_02",
                    soundCue = "Sparrow_Explode"
                },
                "totem_turret_barrage" => new MonsterSkillEntry
                {
                    skillId = "totem_turret_barrage",
                    skillName = "사방신 토템 고정 포대 탄막",
                    description = "이동하지 않는 고정형 석상. 4방향으로 회전형 영력 탄환을 초당 2발씩 지속 발사. 모든 탄환은 단검으로 패링하여 역폭파 가능",
                    skillType = MonsterSkillType.TurretBarrage,
                    minRange = 2.0f,
                    maxRange = 16.0f,
                    damageMultiplier = 0.8f,
                    fixedDamage = 8.0f,
                    cooldown = 1.0f,
                    projectileSpeed = 6.0f,
                    projectileCount = 4,
                    spreadAngle = 90.0f,
                    projectilePrefabName = "Totem_Energy_Orb",
                    projectileParryType = Combat.ProjectileParryType.Enhanced, // [강화 투사체]: 황금색, 패링 시 2배 데미지
                    castVFX = "HCFX_MagicCircle_02",
                    trailVFX = "HCFX_Energy_03",
                    impactVFX = "HCFX_Hit_03",
                    soundCue = "Totem_Fire"
                },
                _ => null
            };
        }
    }
}
