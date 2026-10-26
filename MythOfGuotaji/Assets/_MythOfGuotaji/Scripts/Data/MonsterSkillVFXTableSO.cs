using System;
using System.Collections.Generic;
using MythOfGuotaji.Actor;
using UnityEngine;

namespace MythOfGuotaji.Data
{
    [Serializable]
    public class MonsterSkillVFXEntry
    {
        [Header("Monster Identification")]
        public MonsterType monsterType;
        public string monsterName;
        public string skillName;
        [TextArea(2, 3)] public string skillDescription;

        [Header("Skill Gameplay Stats")]
        [Tooltip("스킬 발동 사정거리 (m)")]
        public float attackRange = 2.0f;

        [Tooltip("스킬 기본 피해량")]
        public float attackDamage = 10.0f;

        [Tooltip("스킬 재사용 대기시간 (초)")]
        public float cooldown = 2.0f;

        [Tooltip("바닥 예고 장판(Telegraph) 지속시간 (초)")]
        public float telegraphDuration = 0.4f;

        [Tooltip("광역 타격/충격파 반경 (m)")]
        public float aoeRadius = 1.0f;

        [Tooltip("피격 시 넉백 거리 (m)")]
        public float knockbackDistance = 2.0f;

        [Tooltip("원거리 투사체 비행 속도 (m/s)")]
        public float projectileSpeed = 0f;

        [Tooltip("포물선 곡사 비행 시간 (초)")]
        public float flightDuration = 0f;

        [Tooltip("포물선 정점 높이 (m)")]
        public float arcHeight = 0f;

        [Header("Associated Visual Effects (VFX Prefab Names)")]
        [Tooltip("시전 전조 장판 이펙트")]
        public string telegraphVFX = "";

        [Tooltip("시전/발사구 머즐 이펙트")]
        public string castOrMuzzleVFX = "";

        [Tooltip("투사체 또는 돌진 궤적 트레일 이펙트")]
        public string projectileOrTrailVFX = "";

        [Tooltip("적중/착지/폭발 충격 이펙트")]
        public string impactOrExplosionVFX = "";

        [Tooltip("사운드 이펙트 이름")]
        public string soundCue = "";
    }

    /// <summary>
    /// 몬스터별 특수 스킬, 스텟, 비주얼 이펙트(VFX) 매핑을 중앙 집중 관리하는 ScriptableObject
    /// - 몬스터명 - 특수스킬 - 해당하는 스텟 - 해당하는 VFX 구조 제공
    /// </summary>
    [CreateAssetMenu(fileName = "MonsterSkillVFXTable", menuName = "MythOfGuotaji/Tables/MonsterSkillVFXTable")]
    public class MonsterSkillVFXTableSO : ScriptableObject
    {
        public List<MonsterSkillVFXEntry> entries = new();

        public MonsterSkillVFXEntry GetEntry(MonsterType type)
        {
            var found = entries.Find(e => e != null && e.monsterType == type);
            if (found != null) return found;

            // 기본값 자동 생성 반환
            return GetDefaultEntry(type);
        }

        public static MonsterSkillVFXEntry GetDefaultEntry(MonsterType type)
        {
            return type switch
            {
                MonsterType.Minion => new MonsterSkillVFXEntry
                {
                    monsterType = MonsterType.Minion,
                    monsterName = "산해경 잡몹 (Minion)",
                    skillName = "5m 돌진 & 착지 70cm 충격파",
                    skillDescription = "5m 내 접근 시 0.3초 예고 후 즉시 돌진하여 착지 지점 반경 70cm 광역 피해 및 2m 근접 공격 연계",
                    attackRange = 5.0f,
                    attackDamage = 6.0f,
                    cooldown = 1.0f,
                    telegraphDuration = 0.3f,
                    aoeRadius = 0.7f,
                    knockbackDistance = 1.5f,
                    projectileSpeed = 0f,
                    telegraphVFX = "Telegraph_Rect_5x0.5",
                    castOrMuzzleVFX = "HCFX_Dust_01",
                    projectileOrTrailVFX = "HCFX_Dust_01",
                    impactOrExplosionVFX = "HCFX_Dust_FloorHit_01",
                    soundCue = "Minion_Dash"
                },

                MonsterType.Minion_Archer => new MonsterSkillVFXEntry
                {
                    monsterType = MonsterType.Minion_Archer,
                    monsterName = "맹독충 (Minion_Archer)",
                    skillName = "맹독 투사체 원거리 사격",
                    skillDescription = "15m 사거리에서 원거리 맹독 투사체를 발사하고 5m 이내 접근 시 카이팅 후퇴",
                    attackRange = 15.0f,
                    attackDamage = 7.0f,
                    cooldown = 0.7f,
                    telegraphDuration = 0.1f,
                    aoeRadius = 0.5f,
                    knockbackDistance = 0.5f,
                    projectileSpeed = 6.0f,
                    castOrMuzzleVFX = "HCFX_Poison_01",
                    projectileOrTrailVFX = "HCFX_ElementOrb_04",
                    impactOrExplosionVFX = "HCFX_Poison_02",
                    soundCue = "Venom_Shoot"
                },

                MonsterType.Minion_BigMon => new MonsterSkillVFXEntry
                {
                    monsterType = MonsterType.Minion_BigMon,
                    monsterName = "거북이 (Minion_BigMon)",
                    skillName = "20m 가속 돌진 & 3m 충격파",
                    skillDescription = "20m x 1.5m 바닥 가이드라인 유지 상태로 3초간 가속 돌진, 경로 내 3m 넉백 및 정지 시 쿵! 3m 충격파 발동",
                    attackRange = 20.0f,
                    attackDamage = 12.0f,
                    cooldown = 4.0f,
                    telegraphDuration = 0.4f,
                    aoeRadius = 3.0f,
                    knockbackDistance = 3.0f,
                    projectileSpeed = 0f,
                    telegraphVFX = "Telegraph_Rect_20x1.5",
                    castOrMuzzleVFX = "HCFX_Dust_Dash_01",
                    projectileOrTrailVFX = "HCFX_Dust_Dash_01",
                    impactOrExplosionVFX = "HCFX_Dust_FloorHit_03 + HCFX_Explosion_03",
                    soundCue = "BigMon_KoongThud"
                },

                MonsterType.Minion_Zhu => new MonsterSkillVFXEntry
                {
                    monsterType = MonsterType.Minion_Zhu,
                    monsterName = "주(鴸, Minion_Zhu)",
                    skillName = "지름 5m 곡사 화염탄 대폭발",
                    skillDescription = "캐릭터 위치에 지름 5m 원형 예고 GUI 표시 후 2초 고정 포물선 비행 화염탄 투하 및 5m 지면 대폭발",
                    attackRange = 20.0f,
                    attackDamage = 18.0f,
                    cooldown = 3.0f,
                    telegraphDuration = 2.0f,
                    aoeRadius = 2.5f, // 지름 5m
                    knockbackDistance = 2.5f,
                    projectileSpeed = 0f,
                    flightDuration = 2.0f,
                    arcHeight = 6.0f,
                    telegraphVFX = "Telegraph_Circle_5m",
                    castOrMuzzleVFX = "HCFX_Fire_01",
                    projectileOrTrailVFX = "HCFX_ElementOrb_01",
                    impactOrExplosionVFX = "HCFX_Explosion_01",
                    soundCue = "Zhu_FireExplosion"
                },

                MonsterType.Elite => new MonsterSkillVFXEntry
                {
                    monsterType = MonsterType.Elite,
                    monsterName = "정예 수문장 (Elite)",
                    skillName = "수문장의 철퇴 강타",
                    skillDescription = "단단한 방어력과 높은 체력으로 접근하여 강력한 근접 강타, 처치 시 보스방 열쇠 드롭",
                    attackRange = 1.8f,
                    attackDamage = 15.0f,
                    cooldown = 1.2f,
                    telegraphDuration = 0.35f,
                    aoeRadius = 1.2f,
                    knockbackDistance = 2.5f,
                    telegraphVFX = "Telegraph_Circle_1.2m",
                    castOrMuzzleVFX = "HCFX_Flash_01",
                    projectileOrTrailVFX = "HCFX_Dust_01",
                    impactOrExplosionVFX = "HCFX_Hit_06",
                    soundCue = "Elite_Smash"
                },

                MonsterType.FoxBoss => new MonsterSkillVFXEntry
                {
                    monsterType = MonsterType.FoxBoss,
                    monsterName = "구미호 요괴 (FoxBoss)",
                    skillName = "구미호의 포효 & 광폭화 연타",
                    skillDescription = "어그로 시 포효 애니메이션과 함께 결계를 형성하고 광폭화 연속 공격 시전",
                    attackRange = 2.5f,
                    attackDamage = 20.0f,
                    cooldown = 0.8f,
                    telegraphDuration = 0.3f,
                    aoeRadius = 2.0f,
                    knockbackDistance = 3.0f,
                    telegraphVFX = "Telegraph_Circle_2m",
                    castOrMuzzleVFX = "HCFX_MagicCircle_01",
                    projectileOrTrailVFX = "HCFX_Charging_01_Step3",
                    impactOrExplosionVFX = "HCFX_Explosion_03",
                    soundCue = "Boss_Roar"
                },

                MonsterType.Dokkaebi_Fire => new MonsterSkillVFXEntry
                {
                    monsterType = MonsterType.Dokkaebi_Fire,
                    monsterName = "도깨비 화염술사 (Dokkaebi_Fire)",
                    skillName = "3연발 화염 탄막",
                    skillDescription = "캐릭터를 조준하여 3연발 화염 투사체 발사. 단검 패링 가능",
                    attackRange = 12.0f,
                    attackDamage = 10.0f,
                    cooldown = 2.0f,
                    telegraphDuration = 0.25f,
                    aoeRadius = 0.8f,
                    knockbackDistance = 1.0f,
                    projectileSpeed = 9.0f,
                    castOrMuzzleVFX = "HCFX_Fire_01",
                    projectileOrTrailVFX = "HCFX_ElementOrb_01",
                    impactOrExplosionVFX = "HCFX_Explosion_01",
                    soundCue = "Fireball_Cast"
                },

                MonsterType.Boar_Charger => new MonsterSkillVFXEntry
                {
                    monsterType = MonsterType.Boar_Charger,
                    monsterName = "저돌맹진 멧돼지 (Boar_Charger)",
                    skillName = "초고속 저돌맹진",
                    skillDescription = "0.5초 예고 후 전방 14m 초고속 돌진. 벽 충돌 시 2초간 기절",
                    attackRange = 14.0f,
                    attackDamage = 14.0f,
                    cooldown = 4.0f,
                    telegraphDuration = 0.5f,
                    aoeRadius = 1.5f,
                    knockbackDistance = 6.0f,
                    projectileSpeed = 16.0f,
                    telegraphVFX = "Telegraph_Rect_14x1.2",
                    castOrMuzzleVFX = "HCFX_Dust_Dash_01",
                    projectileOrTrailVFX = "HCFX_Dust_Dash_01_Air",
                    impactOrExplosionVFX = "HCFX_Explosion_01",
                    soundCue = "Boar_Charge"
                },

                MonsterType.Spider_Tether => new MonsterSkillVFXEntry
                {
                    monsterType = MonsterType.Spider_Tether,
                    monsterName = "요선 거미 (Spider_Tether)",
                    skillName = "요선 포박 & 인력 끌어당김",
                    skillDescription = "12m 사거리 거미줄 발사. 적중 시 몬스터 앞으로 끌어당김 + 0.5초 스턴",
                    attackRange = 12.0f,
                    attackDamage = 5.0f,
                    cooldown = 4.5f,
                    telegraphDuration = 0.3f,
                    aoeRadius = 0.6f,
                    knockbackDistance = -8.0f,
                    projectileSpeed = 12.0f,
                    castOrMuzzleVFX = "HCFX_Web_Shoot",
                    projectileOrTrailVFX = "HCFX_Energy_01",
                    impactOrExplosionVFX = "HCFX_Hit_02",
                    soundCue = "Spider_Tether"
                },

                MonsterType.Mud_Slime => new MonsterSkillVFXEntry
                {
                    monsterType = MonsterType.Mud_Slime,
                    monsterName = "진흙 슬라임 (Mud_Slime)",
                    skillName = "진흙 수렁 장판 생성",
                    skillDescription = "캐릭터 발밑에 직경 3m 진흙 수렁 생성. 4초간 이동속도 50% 감속",
                    attackRange = 9.0f,
                    attackDamage = 6.0f,
                    cooldown = 5.0f,
                    telegraphDuration = 0.5f,
                    aoeRadius = 3.0f,
                    knockbackDistance = 0f,
                    telegraphVFX = "Telegraph_Circle_3m",
                    castOrMuzzleVFX = "HCFX_Buff_Down",
                    impactOrExplosionVFX = "HCFX_Poison_02",
                    soundCue = "Mud_Splash"
                },

                MonsterType.Phantom_Fox => new MonsterSkillVFXEntry
                {
                    monsterType = MonsterType.Phantom_Fox,
                    monsterName = "환영 여우 (Phantom_Fox)",
                    skillName = "환영 분신 & 잔상 점멸",
                    skillDescription = "피격 시 2마리 환영 분신 생성 및 캐릭터 사각으로 점멸",
                    attackRange = 6.0f,
                    attackDamage = 8.0f,
                    cooldown = 2.0f,
                    telegraphDuration = 0.2f,
                    aoeRadius = 1.0f,
                    knockbackDistance = 1.5f,
                    castOrMuzzleVFX = "HCFX_Shine_01",
                    projectileOrTrailVFX = "HCFX_Charging_01_Step2",
                    impactOrExplosionVFX = "HCFX_Hit_04",
                    soundCue = "Fox_Blink"
                },

                MonsterType.Iron_Shield => new MonsterSkillVFXEntry
                {
                    monsterType = MonsterType.Iron_Shield,
                    monsterName = "철갑 방패 도깨비 (Iron_Shield)",
                    skillName = "철옹성 금강 가드",
                    skillDescription = "전방 180도 일반 화살/단검 피해 0. 풀차징 화살이나 배후 공격으로만 피해 가능",
                    attackRange = 3.5f,
                    attackDamage = 12.0f,
                    cooldown = 2.0f,
                    telegraphDuration = 0.3f,
                    aoeRadius = 1.5f,
                    knockbackDistance = 3.0f,
                    telegraphVFX = "Telegraph_Sector_180",
                    castOrMuzzleVFX = "HCFX_Shield_Guard",
                    impactOrExplosionVFX = "HCFX_Hit_06",
                    soundCue = "Shield_Clang"
                },

                MonsterType.Energy_Absorber => new MonsterSkillVFXEntry
                {
                    monsterType = MonsterType.Energy_Absorber,
                    monsterName = "마력 흡수충 (Energy_Absorber)",
                    skillName = "마력 흡수 & 체력 재생",
                    skillDescription = "차징 화살 피격 시 데미지 무효화 및 체력 회복. 일반 화살 및 단검으로만 처치 가능",
                    attackRange = 3.0f,
                    attackDamage = 7.0f,
                    cooldown = 1.5f,
                    telegraphDuration = 0.2f,
                    aoeRadius = 1.2f,
                    knockbackDistance = 1.0f,
                    castOrMuzzleVFX = "HCFX_Aura_Absorption",
                    projectileOrTrailVFX = "HCFX_Buff_Up",
                    impactOrExplosionVFX = "HCFX_Hit_05",
                    soundCue = "Energy_Absorb"
                },

                MonsterType.Bomb_Sparrow => new MonsterSkillVFXEntry
                {
                    monsterType = MonsterType.Bomb_Sparrow,
                    monsterName = "자폭 맹금참새 (Bomb_Sparrow)",
                    skillName = "비행 급강하 자폭 공격",
                    skillDescription = "초고속(12m/s) 이동 후 2m 반경 자폭 폭발 (25 피해). 체력 10으로 낮아 접근 전 처치 필요",
                    attackRange = 8.0f,
                    attackDamage = 25.0f,
                    cooldown = 1.0f,
                    telegraphDuration = 0.35f,
                    aoeRadius = 2.0f,
                    knockbackDistance = 4.0f,
                    projectileSpeed = 12.0f,
                    telegraphVFX = "Telegraph_Circle_2m",
                    castOrMuzzleVFX = "HCFX_Energy_02",
                    projectileOrTrailVFX = "HCFX_Dust_Dash_01_Air",
                    impactOrExplosionVFX = "HCFX_Explosion_02",
                    soundCue = "Sparrow_Explode"
                },

                MonsterType.Totem_Turret => new MonsterSkillVFXEntry
                {
                    monsterType = MonsterType.Totem_Turret,
                    monsterName = "사방신 토템 (Totem_Turret)",
                    skillName = "사방신 토템 고정 포대 탄막",
                    skillDescription = "일반 공격 면역. 패링된 탄환으로만 피해. 사거리 접근 후 제자리 고정 4방향 회전 탄막 난사",
                    attackRange = 16.0f,
                    attackDamage = 8.0f,
                    cooldown = 1.0f,
                    telegraphDuration = 0.2f,
                    aoeRadius = 1.0f,
                    knockbackDistance = 1.0f,
                    projectileSpeed = 6.0f,
                    castOrMuzzleVFX = "HCFX_MagicCircle_02",
                    projectileOrTrailVFX = "HCFX_Energy_03",
                    impactOrExplosionVFX = "HCFX_Hit_03",
                    soundCue = "Totem_Fire"
                },

                _ => new MonsterSkillVFXEntry { monsterType = type, monsterName = type.ToString() }
            };
        }
    }
}
