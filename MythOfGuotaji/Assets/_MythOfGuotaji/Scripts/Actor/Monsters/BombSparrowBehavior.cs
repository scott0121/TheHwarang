using MythOfGuotaji.Data;
using System.Collections;
using MythOfGuotaji.Combat;
using MythOfGuotaji.Core;
using UnityEngine;

namespace MythOfGuotaji.Actor
{
    [MonsterBehavior(MonsterType.Bomb_Sparrow)]
    public class BombSparrowBehavior : MeleeMonsterBehavior
    {
        public override void ApplyPatternStats()
        {
            AttackRange = 8.0f;
            AttackCooldown = 1.0f;
            DashDistance = 8.0f;
            DashDuration = 0.6f;
            DashWidth = 0.8f;
            SpecialAttackRadius = 2.0f;
        }

        public override void PerformAttack()
        {
            StartCoroutine(SparrowKamikazeRoutine());
        }

        private IEnumerator SparrowKamikazeRoutine()
        {
            IsActing = true;
            LastAttackTime = Time.time;

            if (Target == null)
            {
                IsActing = false;
                yield break;
            }

            Vector3 startPos = transform.position;
            Vector3 toPlayer = Target.position - startPos;
            toPlayer.y = 0f;
            Vector3 dashDir = toPlayer.magnitude > 0.001f ? toPlayer.normalized : transform.forward;
            transform.rotation = Quaternion.LookRotation(dashDir);

            PlayAttackAnimation();
            var skill = GetMonsterSkillData("sparrow_kamikaze");

            if (skill != null && skill.castSoundPrefab != null)
            {
                MonsterTableAssetPlayer.PlayOneShotSound(skill.castSoundPrefab, transform.position);
            }

            float kamikazeSpeed = 12.0f;
            float duration = 0.8f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (IsDead)
                {
                    IsActing = false;
                    yield break;
                }

                float dt = Time.deltaTime;
                elapsed += dt;

                Vector3 move = dashDir * (kamikazeSpeed * dt);
                move.y = -9.81f * dt;

                SafeMove(move);

                if (Target != null && Vector3.Distance(transform.position, Target.position) <= 1.2f)
                {
                    break;
                }

                yield return null;
            }

            Vector3 explodePos = transform.position;
            var impactVfx = skill?.impactVfxPrefab;
            if (impactVfx != null)
            {
                VFXManager.Instance?.Spawn(impactVfx, explodePos + Vector3.up * 0.5f, Quaternion.identity);
            }
            else
            {
                VFXManager.Instance?.Spawn("HCFX_Explosion_01", explodePos + Vector3.up * 0.5f, Quaternion.identity);
            }

            if (skill != null && skill.impactSoundPrefab != null)
            {
                MonsterTableAssetPlayer.PlayOneShotSound(skill.impactSoundPrefab, explodePos);
            }

            Collider[] hits = Physics.OverlapSphere(explodePos, 2.0f);
            foreach (var col in hits)
            {
                if (col.CompareTag("Player") || col.GetComponentInParent<PlayerController>() != null)
                {
                    if (col.TryGetComponent<IDamageable>(out var dmgReceiver))
                    {
                        DamageInfo dmg = new DamageInfo(AttackDamage * 1.5f, false, explodePos, dashDir, KnockbackPower * 2f, ElementType.Fire, gameObject);
                        dmgReceiver.TakeDamage(dmg);
                    }
                    break;
                }
            }

            Die();
            IsActing = false;
        }
    }
}
