using System;
using System.Collections.Generic;
using MythOfGuotaji.Actor;
using UnityEngine;

namespace MythOfGuotaji.Data
{
    [Serializable]
    public class MonsterStatEntry
    {
        public MonsterType type;
        public string displayName;
        public float maxHp;
        public float moveSpeed;
        public float attackDamage;
        public float attackRange;
        public float attackCooldown;
        public float separationRadius;
        public bool dropsBossKey;
        public int affinityReward;
        public int goldReward;
        public float scaleMultiplier;
        public Color tintColor;

        [Header("Attack Pattern & Projectile Settings")]
        [Tooltip("원거리 투사체 비행 속도 (m/s)")]
        public float projectileSpeed = 6.0f;

        [Tooltip("원거리 카이팅 후퇴 거리 (m, 0이면 후퇴 없음)")]
        public float kiteDistance = 0.0f;

        [Tooltip("공격 피격 시 넉백 파워")]
        public float knockbackPower = 2.0f;

        [Tooltip("특수 공격 전조(차징/장판) 대기 시간 (초)")]
        public float telegraphDuration = 0.5f;

        [Tooltip("특수 공격(휩쓸기/폭발) 반경 (m)")]
        public float specialAttackRadius = 2.5f;

        [Tooltip("특수 공격 부채꼴 각도 (도)")]
        public float specialAttackAngle = 90.0f;

        [Tooltip("포물선/특수 투사체 비행 시간 (초)")]
        public float flightDuration = 2.0f;

        [Tooltip("포물선 정점 높이 (m)")]
        public float arcHeight = 6.0f;

        [Tooltip("공격 후 이동 불가 후딜레이 시간 (초)")]
        public float postAttackDelay = 0.2f;

        [Header("Drop Rewards")]
        [Tooltip("사망 시 드롭하는 영혼석(보석) 개수")]
        public int gemDropCount = 1;

        [Tooltip("보석 1개당 획득 경험치")]
        public int expPerGem = 1;

        [Tooltip("사망 시 체력 회복 하트 드롭 확률 (0.0 ~ 1.0)")]
        public float heartDropChance = 0.25f;

        [Tooltip("하트 획득 시 플레이어 체력 회복량")]
        public float heartHealAmount = 5.0f;

        [Header("Minion Dash Attack Settings")]
        [Tooltip("돌진 사거리 (m)")]
        public float dashDistance = 2.0f;

        [Tooltip("돌진 폭 (m)")]
        public float dashWidth = 0.5f;

        [Tooltip("돌진 돌파 시간 (초)")]
        public float dashDuration = 0.3f;
    }

    [CreateAssetMenu(fileName = "MonsterStatsTable", menuName = "MythOfGuotaji/Tables/MonsterStatsTable")]
    public class MonsterStatsTableSO : ScriptableObject
    {
        public List<MonsterStatEntry> entries = new List<MonsterStatEntry>();

        public MonsterStatEntry GetStat(MonsterType type)
        {
            var found = entries.Find(e => e != null && e.type == type);
            if (found != null) return found;

            // Fallback default stats
            return type switch
            {
                MonsterType.Minion => new MonsterStatEntry
                {
                    type = MonsterType.Minion,
                    displayName = "산해경 잡몹",
                    maxHp = 35f,
                    moveSpeed = 4.2f,
                    attackDamage = 6f,
                    attackRange = 5.0f,
                    attackCooldown = 1.0f,
                    separationRadius = 0.9f,
                    dropsBossKey = false,
                    affinityReward = 0,
                    goldReward = 2,
                    scaleMultiplier = 1.0f,
                    tintColor = new Color(0.85f, 0.2f, 0.2f),
                    knockbackPower = 2.0f,
                    telegraphDuration = 0.3f,
                    specialAttackRadius = 0.7f,
                    specialAttackAngle = 0f,
                    kiteDistance = 0f,
                    gemDropCount = 1,
                    expPerGem = 1,
                    dashDistance = 5.0f,
                    dashWidth = 0.5f,
                    dashDuration = 0.3f
                },
                MonsterType.Minion_Archer => new MonsterStatEntry
                {
                    type = MonsterType.Minion_Archer,
                    displayName = "맹독충",
                    maxHp = 30f,
                    moveSpeed = 2.7f,
                    attackDamage = 7f,
                    attackRange = 15f,
                    attackCooldown = 0.7f,
                    separationRadius = 1.1f,
                    dropsBossKey = false,
                    affinityReward = 0,
                    goldReward = 3,
                    scaleMultiplier = 0.9f,
                    tintColor = new Color(0.2f, 0.9f, 0.25f), // 형광 맹독 녹색
                    projectileSpeed = 6.0f,
                    kiteDistance = 4.5f,
                    knockbackPower = 2.0f,
                    gemDropCount = 1,
                    expPerGem = 1
                },
                MonsterType.Minion_BigMon => new MonsterStatEntry
                {
                    type = MonsterType.Minion_BigMon,
                    displayName = "거북이",
                    maxHp = 60f,
                    moveSpeed = 2.0f,
                    attackDamage = 12f,
                    attackRange = 20.0f,
                    attackCooldown = 3.5f,
                    separationRadius = 1.5f,
                    dropsBossKey = false,
                    affinityReward = 0,
                    goldReward = 5,
                    scaleMultiplier = 1.45f,
                    tintColor = new Color(0.25f, 0.45f, 0.35f), // 단단한 청록/암석색
                    telegraphDuration = 0.4f,
                    specialAttackRadius = 3.0f,
                    specialAttackAngle = 360.0f,
                    knockbackPower = 12.0f,
                    kiteDistance = 0f,
                    gemDropCount = 2,
                    expPerGem = 1,
                    dashDistance = 20.0f,
                    dashWidth = 1.5f,
                    dashDuration = 3.0f,
                    heartDropChance = 0.40f,
                    heartHealAmount = 5.0f
                },
                MonsterType.Minion_Zhu => new MonsterStatEntry
                {
                    type = MonsterType.Minion_Zhu,
                    displayName = "주(鴸)",
                    maxHp = 50f,
                    moveSpeed = 1.2f,
                    attackDamage = 18f,
                    attackRange = 20f,
                    attackCooldown = 3.0f,
                    separationRadius = 1.4f,
                    dropsBossKey = false,
                    affinityReward = 0,
                    goldReward = 6,
                    scaleMultiplier = 1.15f,
                    tintColor = new Color(0.72f, 0.22f, 0.85f), // 신비로운 요괴 보라색
                    flightDuration = 2.0f,
                    arcHeight = 6.0f,
                    specialAttackRadius = 2.5f, // 지름 5m = 반경 2.5m
                    postAttackDelay = 0.2f,
                    kiteDistance = 5.0f,
                    gemDropCount = 2,
                    expPerGem = 1
                },
                MonsterType.Elite => new MonsterStatEntry
                {
                    type = MonsterType.Elite,
                    displayName = "정예 열쇠 수문장",
                    maxHp = 120f,
                    moveSpeed = 3.5f,
                    attackDamage = 15f,
                    attackRange = 1.8f,
                    attackCooldown = 1.2f,
                    separationRadius = 1.5f,
                    dropsBossKey = true,
                    affinityReward = 1,
                    goldReward = 20,
                    scaleMultiplier = 1.5f,
                    tintColor = new Color(0.95f, 0.75f, 0.1f),
                    knockbackPower = 4.0f,
                    kiteDistance = 0f,
                    gemDropCount = 5,
                    expPerGem = 2
                },
                MonsterType.FoxBoss => new MonsterStatEntry
                {
                    type = MonsterType.FoxBoss,
                    displayName = "구미호 여우 요괴",
                    maxHp = 350f,
                    moveSpeed = 4.8f,
                    attackDamage = 20f,
                    attackRange = 2.5f,
                    attackCooldown = 0.8f,
                    separationRadius = 2.2f,
                    dropsBossKey = false,
                    affinityReward = 5,
                    goldReward = 100,
                    scaleMultiplier = 2.2f,
                    tintColor = new Color(0.9f, 0.4f, 0.1f),
                    knockbackPower = 6.0f,
                    kiteDistance = 0f,
                    gemDropCount = 12,
                    expPerGem = 3
                },
                _ => new MonsterStatEntry { maxHp = 50f, moveSpeed = 4f, attackDamage = 10f, scaleMultiplier = 1f }
            };
        }
    }
}
