using MythOfGuotaji.Data;
using MythOfGuotaji.Combat;
using MythOfGuotaji.Core;
using UnityEngine;

namespace MythOfGuotaji.Actor
{
    [MonsterBehavior(MonsterType.Iron_Shield)]
    public class IronShieldBehavior : MeleeMonsterBehavior
    {
        public override void ApplyPatternStats()
        {
            AttackRange = 3.5f;
            AttackCooldown = 2.0f;
        }

        public override DamageResponse PreDamage(DamageInfo info)
        {
            if (info.isCharged) return DamageResponse.Proceed;

            var stat = Data.GameDataManager.Instance?.MonsterStats?.GetStat(Type);
            if (stat != null && stat.deflectVfxPrefab != null)
            {
                VFXManager.Instance?.Spawn(stat.deflectVfxPrefab, transform.position + Vector3.up * 1f, Quaternion.identity);
            }
            else
            {
                VFXManager.Instance?.Spawn("HCFX_Sparks_01", transform.position + Vector3.up * 1f, Quaternion.identity);
            }
            Cameras.QuarterViewCamera.Instance?.TriggerShake(0.15f, 0.1f);
            return DamageResponse.Absorbed;
        }
    }
}
