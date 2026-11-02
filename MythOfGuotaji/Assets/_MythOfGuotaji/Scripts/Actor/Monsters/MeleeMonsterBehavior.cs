using MythOfGuotaji.Data;
using System.Collections;
using MythOfGuotaji.Combat;
using MythOfGuotaji.Core;
using UnityEngine;

namespace MythOfGuotaji.Actor
{
    public class MeleeMonsterBehavior : MonsterBehavior
    {
        public override void PerformAttack()
        {
            LastAttackTime = Time.time;
            PlayAttackAnimation();
            if (Target != null && Target.TryGetComponent<IDamageable>(out var damageable))
            {
                DamageInfo dmg = new DamageInfo(AttackDamage, false, transform.position, transform.forward, KnockbackPower, ElementType.Physical, gameObject);
                damageable.TakeDamage(dmg);
            }
        }

        public override void StartMeleeAttack()
        {
            StartCoroutine(NormalMeleeAttackRoutine());
        }

        protected IEnumerator NormalMeleeAttackRoutine()
        {
            IsActing = true;
            LastMeleeAttackTime = Time.time;
            PlayAttackAnimation();

            var stat = Data.GameDataManager.Instance?.MonsterStats?.GetStat(Type);
            float windup = (stat?.normalAttack != null && stat.normalAttack.windupTime > 0f) ? stat.normalAttack.windupTime : 0.2f;
            float recovery = (stat?.normalAttack != null && stat.normalAttack.recoveryTime > 0f) ? stat.normalAttack.recoveryTime : 0.2f;
            float hitbox = (stat?.normalAttack != null && stat.normalAttack.hitboxRadius > 0f) ? stat.normalAttack.hitboxRadius : 1.0f;
            float dmg = MeleeAttackDamage;

            if (stat?.normalAttack != null)
            {
                if (stat.normalAttack.attackVfxPrefab != null)
                {
                    VFXManager.Instance?.Spawn(stat.normalAttack.attackVfxPrefab, transform.position + transform.forward * 0.8f + Vector3.up * 0.5f, transform.rotation);
                }
                if (stat.normalAttack.attackSoundPrefab != null)
                {
                    MonsterTableAssetPlayer.PlayOneShotSound(stat.normalAttack.attackSoundPrefab, transform.position);
                }
            }

            yield return new WaitForSeconds(windup);

            if (!IsDead && Target != null)
            {
                float dist = Vector3.Distance(transform.position, Target.position);
                if (dist <= MeleeAttackRange + hitbox)
                {
                    if (Target.TryGetComponent<IDamageable>(out var damageable))
                    {
                        DamageInfo damageInfo = new DamageInfo(dmg, false, transform.position, transform.forward, KnockbackPower, ElementType.Physical, gameObject);
                        damageable.TakeDamage(damageInfo);

                        if (stat?.normalAttack != null)
                        {
                            if (stat.normalAttack.hitVfxPrefab != null)
                            {
                                VFXManager.Instance?.Spawn(stat.normalAttack.hitVfxPrefab, Target.position + Vector3.up * 0.8f, Quaternion.identity);
                            }
                            if (stat.normalAttack.hitSoundPrefab != null)
                            {
                                MonsterTableAssetPlayer.PlayOneShotSound(stat.normalAttack.hitSoundPrefab, Target.position);
                            }
                        }
                    }
                }
            }

            yield return new WaitForSeconds(recovery);
            IsActing = false;
        }
    }
}
