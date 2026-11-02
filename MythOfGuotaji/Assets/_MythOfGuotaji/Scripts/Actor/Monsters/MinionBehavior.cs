using MythOfGuotaji.Data;
using System.Collections;
using MythOfGuotaji.Combat;
using MythOfGuotaji.Core;
using UnityEngine;

namespace MythOfGuotaji.Actor
{
    [MonsterBehavior(MonsterType.Minion)]
    public class MinionBehavior : MeleeMonsterBehavior
    {
        public override bool ShakesCameraOnDeath => false;

        public override void ApplyPatternStats()
        {
            AttackRange = 5.0f;
            DashDistance = 5.0f;
            DashDuration = 0.3f;
            DashWidth = 0.5f;
            TelegraphDuration = 0.3f;
        }

        public override void PerformAttack()
        {
            StartCoroutine(MinionDashAttackRoutine());
        }

        public override void PlayAttackMotion()
        {
            PlayFirstState(0.05f, "BiteAttackForward", "JumpBiteAttack", "BiteAttack");
        }

        private IEnumerator MinionDashAttackRoutine()
        {
            IsActing = true;
            LastAttackTime = Time.time;

            if (Target == null)
            {
                IsActing = false;
                yield break;
            }

            Vector3 startPos = transform.position;
            Vector3 targetPos = Target.position;
            targetPos.y = startPos.y;

            Vector3 dashVec = targetPos - startPos;
            dashVec.y = 0f;
            float dashDist = dashVec.magnitude;
            Vector3 dashDir = dashDist > 0.001f ? dashVec.normalized : transform.forward;

            float maxDash = DashDistance > 0f ? DashDistance : 5.0f;
            dashDist = Mathf.Clamp(dashDist, 1.0f, maxDash);

            transform.rotation = Quaternion.LookRotation(dashDir);
            float dashWidth = DashWidth > 0f ? DashWidth : 0.5f;

            CleanupTelegraph();
            ActiveTelegraph = CreateDashTelegraph(startPos, dashDir, dashDist, dashWidth);
            PlayIdleAnimation();

            float elapsed = 0f;
            float prepDuration = TelegraphDuration > 0f ? TelegraphDuration : 0.3f;
            while (elapsed < prepDuration)
            {
                if (IsDead)
                {
                    CleanupTelegraph();
                    IsActing = false;
                    yield break;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            CleanupTelegraph();

            PlayAttackAnimation();
            var minionSkill = GetMonsterSkillData("minion_dash");
            var dashVfx = minionSkill?.castVfxPrefab ?? minionSkill?.trailVfxPrefab;
            VFXManager.Instance?.SpawnMinionDashDust(startPos, dashDir, dashVfx);
            if (minionSkill != null && minionSkill.castSoundPrefab != null)
            {
                MonsterTableAssetPlayer.PlayOneShotSound(minionSkill.castSoundPrefab, startPos);
            }

            float dashDuration = DashDuration > 0f ? DashDuration : 0.3f;
            float dashElapsed = 0f;
            float dashSpeed = dashDist / dashDuration;

            while (dashElapsed < dashDuration)
            {
                if (IsDead)
                {
                    IsActing = false;
                    yield break;
                }

                float dt = Time.deltaTime;
                dashElapsed += dt;

                Vector3 move = dashDir * (dashSpeed * dt);
                move.y = -9.81f * dt;
                SafeMove(move);

                yield return null;
            }

            Vector3 arrivalPos = transform.position;
            arrivalPos.y = 0f;

            if (Target != null && !IsDead)
            {
                Vector3 pPos = Target.position;
                pPos.y = 0f;
                float arrivalDist = Vector3.Distance(arrivalPos, pPos);
                float aoeRadius = SpecialAttackRadius > 0f ? SpecialAttackRadius : 0.7f;

                VFXManager.Instance?.SpawnMinionArrivalImpact(arrivalPos, aoeRadius, minionSkill?.impactVfxPrefab);
                if (minionSkill != null && minionSkill.impactSoundPrefab != null)
                {
                    MonsterTableAssetPlayer.PlayOneShotSound(minionSkill.impactSoundPrefab, arrivalPos);
                }

                if (arrivalDist <= aoeRadius + 0.35f)
                {
                    if (Target.TryGetComponent<IDamageable>(out var dmgReceiver))
                    {
                        DamageInfo dmg = new DamageInfo(AttackDamage, false, transform.position, dashDir, KnockbackPower, ElementType.Physical, gameObject);
                        dmgReceiver.TakeDamage(dmg);
                    }
                }

                Vector3 toPlayerAfter = Target.position - transform.position;
                toPlayerAfter.y = 0f;
                float distAfter = toPlayerAfter.magnitude;

                if (distAfter <= 2.0f + 0.35f)
                {
                    if (toPlayerAfter.sqrMagnitude > 0.001f)
                    {
                        transform.rotation = Quaternion.LookRotation(toPlayerAfter.normalized);
                    }
                    PlayAttackAnimation();
                    yield return new WaitForSeconds(0.15f);

                    if (!IsDead && Target != null)
                    {
                        Vector3 currentPos = transform.position;
                        currentPos.y = 0f;
                        Vector3 curPPos = Target.position;
                        curPPos.y = 0f;
                        if (Vector3.Distance(currentPos, curPPos) <= 2.2f && Target.TryGetComponent<IDamageable>(out var meleeReceiver))
                        {
                            DamageInfo meleeDmg = new DamageInfo(AttackDamage, false, transform.position, transform.forward, KnockbackPower, ElementType.Physical, gameObject);
                            meleeReceiver.TakeDamage(meleeDmg);
                        }
                    }
                }
            }

            IsActing = false;
        }
    }
}
