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

        [Header("Player Action Visual Effects (VFX)")]
        [Tooltip("대시 시 발밑 먼지 이펙트 (기본: HCFX_Dust_Dash_01)")]
        public GameObject dashDustVfxPrefab;

        [Tooltip("회피/무적 성공 시 반짝임 이펙트 (기본: HCFX_Shine_01)")]
        public GameObject evasionShineVfxPrefab;

        [Tooltip("피격 시 스파크 이펙트 (기본: HCFX_Hit_04)")]
        public GameObject hitSparkVfxPrefab;

        [Tooltip("치명타/강타 피격 시 스파크 이펙트 (기본: HCFX_Hit_07)")]
        public GameObject criticalHitSparkVfxPrefab;

        [Tooltip("체력 회복 및 치유 버프 이펙트 (기본: HCFX_MagicCircle_01)")]
        public GameObject healBuffVfxPrefab;

        [Tooltip("거타지 사망 폭발 이펙트 (기본: HCFX_Explosion_02)")]
        public GameObject deathVfxPrefab;

        [Header("Bow Charging Aura Step VFX")]
        [Tooltip("차징 1단계 오라 프리팹 (기본: HCFX_Charging_01_Step1)")]
        public GameObject chargeAuraStep1VfxPrefab;

        [Tooltip("차징 2단계 오라 프리팹 (기본: HCFX_Charging_01_Step2)")]
        public GameObject chargeAuraStep2VfxPrefab;

        [Tooltip("차징 3단계 완료 오라 프리팹 (기본: HCFX_Charging_01_Step3)")]
        public GameObject chargeAuraStep3VfxPrefab;

        [Tooltip("차징 화살 발사 순간 폭발적 방출 이펙트 (기본: HCFX_Charging_01_Step3_End)")]
        public GameObject chargeReleaseBurstVfxPrefab;

        [Header("Player Action Sounds (SFX)")]
        [Tooltip("대시 발동 사운드 게임오브젝트/오디오")]
        public GameObject dashSoundPrefab;

        [Tooltip("플레이어 피격 사운드 게임오브젝트/오디오")]
        public GameObject hitSoundPrefab;

        [Tooltip("플레이어 사망 사운드 게임오브젝트/오디오")]
        public GameObject deathSoundPrefab;

        [Tooltip("체력 회복 하트 획득 사운드 게임오브젝트/오디오")]
        public GameObject heartPickupSoundPrefab;

        [Header("Combat Feedback & Timers")]
        [Tooltip("피격 시 비네트 플래시 강도 (기본: 0.85)")]
        public float damageVignetteIntensity = 0.85f;

        [Tooltip("피격 시 카메라 흔들림 지속 시간 (기본: 0.35초)")]
        public float cameraShakeDuration = 0.35f;

        [Tooltip("피격 시 카메라 흔들림 강도 (기본: 0.20)")]
        public float cameraShakeIntensity = 0.20f;

        [Tooltip("스턴 기본 지속 시간 (초)")]
        public float defaultStunDuration = 0.35f;

        [Header("Fallback Weapon Action Prefabs")]
        [Tooltip("기본 활 투사체 프리팹")]
        public GameObject defaultArrowPrefab;

        [Tooltip("차징 활 투사체 프리팹")]
        public GameObject defaultChargedArrowPrefab;

        [Tooltip("단검 참격 이펙트 프리팹")]
        public GameObject defaultDaggerSlashVfxPrefab;
    }
}
