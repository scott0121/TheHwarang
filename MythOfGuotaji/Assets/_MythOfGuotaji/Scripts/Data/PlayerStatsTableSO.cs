using UnityEngine;

namespace MythOfGuotaji.Data
{
    [CreateAssetMenu(fileName = "PlayerStatsTable", menuName = "MythOfGuotaji/Tables/PlayerStatsTable")]
    public class PlayerStatsTableSO : ScriptableObject
    {
        [Header("Base Attributes")]
        [Tooltip("기본 최대 체력")]
        public float maxHp = 100f;

        [Tooltip("기본 이동 속도 (m/s)")]
        public float moveSpeed = 7.5f;

        [Header("Dash & Evasion")]
        [Tooltip("대시 이동 속도")]
        public float dashSpeed = 16f;

        [Tooltip("대시 지속 시간 (초)")]
        public float dashDuration = 0.22f;

        [Tooltip("대시 재사용 대기 시간 (초)")]
        public float dashCooldown = 2.0f;

        [Tooltip("대시 발동 시 무적 지속 시간 (초)")]
        public float dashInvincibleDuration = 0.5f;

        [Header("Global Cooldown (GCD)")]
        [Tooltip("동일 카테고리 무기 글로벌 쿨다운 (초)")]
        public float sameCategoryGCD = 0.5f;

        [Tooltip("다른 카테고리 무기/마법 글로벌 쿨다운 (초)")]
        public float diffCategoryGCD = 0.2f;

        [Header("Heart Recovery Settings")]
        [Tooltip("하트 1개당 기본 체력 회복량")]
        public float heartHealAmount = 5.0f;

        [Tooltip("하트 직접 접촉 획득 반경 (m)")]
        public float heartPickupRadius = 1.25f;
    }
}
