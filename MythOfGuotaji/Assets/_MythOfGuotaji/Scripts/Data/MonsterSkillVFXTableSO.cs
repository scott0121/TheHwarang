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

                _ => new MonsterSkillVFXEntry { monsterType = type, monsterName = type.ToString() }
            };
        }
    }
}
