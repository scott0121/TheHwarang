using UnityEngine;

namespace MythOfGuotaji.Combat
{
    /// <summary>
    /// 투사체 패링(반사) 분류 및 특성:
    /// - Normal: 푸른색 / 초록색 => 패링 가능, 기본 패링 데미지
    /// - Enhanced: 황금색 => 사방신 토템 등, 패링 가능, 패링 시 2배(200%) 데미지
    /// - Unparryable: 붉은색 => 패링 불가 (레이저, 점액 등)
    /// </summary>
    public enum ProjectileParryType
    {
        Normal = 0,      // 기본 투사체 (푸른색 / 초록색, 패링 가능, 기본 피해)
        Enhanced = 1,    // 강화 투사체 (황금색, 패링 가능, 패링 시 2배 피해)
        Unparryable = 2  // 패링불가 투사체 (붉은색, 패링 불가)
    }

    /// <summary>
    /// 단검으로 쳐내어 반사(패링) 가능한 투사체, 마법 구체, 투척체 인터페이스
    /// </summary>
    public interface IReflectable
    {
        bool IsFromPlayer { get; }
        Vector3 Position { get; }
        ProjectileParryType ParryType { get; }
        bool CanParry => ParryType != ProjectileParryType.Unparryable;
        void Reflect(GameObject newAttacker, Vector3? customDirection = null, float damageMultiplier = 1.5f, float speedMultiplier = 1.8f);
    }
}
