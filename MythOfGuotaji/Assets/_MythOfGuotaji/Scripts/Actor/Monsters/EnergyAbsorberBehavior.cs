using MythOfGuotaji.Data;
using MythOfGuotaji.Combat;
using MythOfGuotaji.Core;
using UnityEngine;

namespace MythOfGuotaji.Actor
{
    [MonsterBehavior(MonsterType.Energy_Absorber)]
    public class EnergyAbsorberBehavior : MeleeMonsterBehavior
    {
        public override void ApplyPatternStats()
        {
            AttackRange = 3.0f;
            AttackCooldown = 1.5f;
        }

        public override DamageResponse PreDamage(DamageInfo info)
        {
            if (!info.isCharged) return DamageResponse.Proceed;

            CurrentHp = Mathf.Min(MaxHp * 1.5f, CurrentHp + info.amount);
            transform.localScale *= 1.15f;
            AttackDamage *= 1.2f;

            var absorbSkill = GetMonsterSkillData("energy_absorb");
            var absorbVfx = absorbSkill?.castVfxPrefab ?? absorbSkill?.impactVfxPrefab;
            if (absorbVfx != null)
            {
                VFXManager.Instance?.Spawn(absorbVfx, transform.position, Quaternion.identity);
            }
            else
            {
                VFXManager.Instance?.Spawn("HCFX_DarkAura_01", transform.position, Quaternion.identity);
            }
            if (absorbSkill != null && absorbSkill.castSoundPrefab != null)
            {
                MonsterTableAssetPlayer.PlayOneShotSound(absorbSkill.castSoundPrefab, transform.position);
            }
            return DamageResponse.Absorbed;
        }
    }
}
