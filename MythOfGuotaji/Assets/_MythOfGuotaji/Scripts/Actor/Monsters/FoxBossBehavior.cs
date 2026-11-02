using MythOfGuotaji.Data;
using System.Collections;
using MythOfGuotaji.Combat;
using MythOfGuotaji.Core;
using UnityEngine;

namespace MythOfGuotaji.Actor
{
    [MonsterBehavior(MonsterType.FoxBoss)]
    public class FoxBossBehavior : MonsterBehavior
    {
        private int _bossComboIndex = 0;
        private float _lastBossSkill1Time = -99f;
        private float _lastBossSkill2Time = -99f;

        public override void ResetState()
        {
            _lastBossSkill1Time = -99f;
            _lastBossSkill2Time = -99f;
        }

        public override void ApplyPatternStats()
        {
            AttackRange = 3.5f;
            AttackCooldown = 1.2f;
        }

        public override bool TrySpecialSkill(float distToPlayer, Vector3 toPlayer)
        {
            bool canSkill1 = (distToPlayer >= 5.0f && distToPlayer <= 25.0f && Time.time >= _lastBossSkill1Time + 5.0f && HasLineOfSightToPlayer());
            bool canSkill2 = (distToPlayer <= 10.0f && Time.time >= _lastBossSkill2Time + 5.0f && HasLineOfSightToPlayer());

            if (canSkill1)
            {
                StartCoroutine(BossVacuumSkillRoutine());
                return true;
            }
            else if (canSkill2)
            {
                StartCoroutine(BossCleaveSkillRoutine());
                return true;
            }
            return false;
        }

        public override void PerformAttack()
        {
            StartCoroutine(BossMeleeAttackRoutine());
        }

        public override void StartMeleeAttack()
        {
            StartCoroutine(BossMeleeAttackRoutine());
        }

        public override void OnAggroTriggered()
        {
            if (HasAnimState("Roar"))
            {
                StartCoroutine(BossRoarRoutine());
            }
        }

        public override bool HandleDeathProgression()
        {
            UI.BossHealthBarUI.Instance?.Hide();
            if (OwningRoom != null)
            {
                OwningRoom.OnBossDefeated();
            }
            else if (GameManager.Instance != null)
            {
                GameManager.Instance.CompleteStage(affinityReward: 5, goldReward: 100);
            }
            return true;
        }

        public override void PlayAttackMotion()
        {
            string anim = (_bossComboIndex++ % 2 == 0) ? "ClawsAttack3HitCombo" : "SmashAttack";
            if (HasAnimState(anim)) PlayAnimation(anim, 0.05f);
            else if (HasAnimState("ClawsAttack2HitCombo")) PlayAnimation("ClawsAttack2HitCombo", 0.05f);
            else if (HasAnimState("JumpSmashAttack")) PlayAnimation("JumpSmashAttack", 0.05f);
            else if (HasAnimState("SmashAttackForward")) PlayAnimation("SmashAttackForward", 0.05f);
            else if (HasAnimState("Attack")) PlayAnimation("Attack", 0.05f);
            else Owner.SetTriggerSafe("attack");
        }

        private IEnumerator BossRoarRoutine()
        {
            IsActing = true;
            PlayAnimation("Roar", 0.1f);
            if (Cameras.QuarterViewCamera.Instance != null)
            {
                Cameras.QuarterViewCamera.Instance.TriggerShake(0.5f, 0.4f);
            }
            yield return new WaitForSeconds(1.1f);
            IsActing = false;
        }

        private IEnumerator BossMeleeAttackRoutine()
        {
            IsActing = true;
            LastAttackTime = Time.time;

            string anim = (_bossComboIndex++ % 2 == 0) ? "ClawsAttack3HitCombo" : "SmashAttack";
            if (HasAnimState(anim)) PlayAnimation(anim, 0.05f);
            else PlayAttackAnimation();

            var stat = Data.GameDataManager.Instance?.MonsterStats?.GetStat(Type);
            if (stat?.normalAttack != null)
            {
                if (stat.normalAttack.attackVfxPrefab != null)
                {
                    VFXManager.Instance?.Spawn(stat.normalAttack.attackVfxPrefab, transform.position + transform.forward * 1.2f + Vector3.up * 0.8f, transform.rotation);
                }
                if (stat.normalAttack.attackSoundPrefab != null)
                {
                    MonsterTableAssetPlayer.PlayOneShotSound(stat.normalAttack.attackSoundPrefab, transform.position);
                }
            }

            yield return new WaitForSeconds(0.35f);

            if (!IsDead && Target != null)
            {
                float dist = Vector3.Distance(transform.position, Target.position);
                if (dist <= AttackRange + 1.2f)
                {
                    if (Target.TryGetComponent<IDamageable>(out var damageable))
                    {
                        DamageInfo dmg = new DamageInfo(AttackDamage, false, transform.position, transform.forward, KnockbackPower * 1.5f, ElementType.Physical, gameObject);
                        damageable.TakeDamage(dmg);

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

                        if (Cameras.QuarterViewCamera.Instance != null)
                        {
                            Cameras.QuarterViewCamera.Instance.TriggerShake(0.3f, 0.25f);
                        }
                    }
                }
            }

            yield return new WaitForSeconds(0.4f);
            IsActing = false;
        }

        private IEnumerator BossVacuumSkillRoutine()
        {
            IsActing = true;
            _lastBossSkill1Time = Time.time;
            LastAttackTime = Time.time;

            if (Target == null)
            {
                IsActing = false;
                yield break;
            }

            Vector3 startPos = transform.position;
            Vector3 toPlayer = Target.position - startPos;
            toPlayer.y = 0f;
            Vector3 attackDir = toPlayer.magnitude > 0.001f ? toPlayer.normalized : transform.forward;
            transform.rotation = Quaternion.LookRotation(attackDir);

            float range = 18.0f;
            float arcAngle = 60.0f;

            CleanupTelegraph();
            ActiveTelegraph = CreateWorldSectorTelegraph(startPos, attackDir, range, arcAngle, new Color(0.7f, 0.1f, 0.9f, 0.4f));
            ActiveTelegraph.name = "[Boss_VacuumTelegraph]";
            PlayIdleAnimation();

            var skill = GetMonsterSkillData("boss_vacuum");
            float telegraphDuration = (skill != null && skill.telegraphDuration > 0f) ? skill.telegraphDuration : 0.6f;

            float elapsed = 0f;
            while (elapsed < telegraphDuration)
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

            PlayAnimation("Attack", 0.05f);
            PlayImpactSound();
            SpawnVacuumPullEffect(transform.position, attackDir, range);

            if (skill != null && skill.castVfxPrefab != null)
            {
                VFXManager.Instance?.Spawn(skill.castVfxPrefab, transform.position + Vector3.up * 1f, Quaternion.LookRotation(attackDir));
            }
            if (skill != null && skill.castSoundPrefab != null)
            {
                MonsterTableAssetPlayer.PlayOneShotSound(skill.castSoundPrefab, transform.position);
            }

            if (Target != null && !IsDead)
            {
                Vector3 toTarget = Target.position - transform.position;
                toTarget.y = 0f;
                float dist = toTarget.magnitude;

                if (dist <= range && Vector3.Angle(attackDir, toTarget.normalized) <= arcAngle * 0.5f)
                {
                    Vector3 pullTargetPos = transform.position + attackDir * 2.5f;
                    if (Target.TryGetComponent<PlayerController>(out var playerCtrl))
                    {
                        playerCtrl.ApplyPull(pullTargetPos, 0.35f);
                        playerCtrl.ApplyStun(0.5f);
                    }
                    if (Target.TryGetComponent<IDamageable>(out var damageable))
                    {
                        DamageInfo dmg = new DamageInfo(AttackDamage * 0.6f, false, transform.position, attackDir, 0f, ElementType.Physical, gameObject);
                        damageable.TakeDamage(dmg);
                    }
                }
            }

            float postDelay = (skill != null && skill.postAttackDelay > 0f) ? skill.postAttackDelay : 0.5f;
            yield return new WaitForSeconds(postDelay);
            IsActing = false;
        }

        private IEnumerator BossCleaveSkillRoutine()
        {
            IsActing = true;
            _lastBossSkill2Time = Time.time;
            LastAttackTime = Time.time;

            if (Target == null)
            {
                IsActing = false;
                yield break;
            }

            Vector3 startPos = transform.position;
            Vector3 toPlayer = Target.position - startPos;
            toPlayer.y = 0f;
            Vector3 cleaveDir = toPlayer.magnitude > 0.001f ? toPlayer.normalized : transform.forward;
            transform.rotation = Quaternion.LookRotation(cleaveDir);

            float range = 8.0f;
            float arcAngle = 120.0f;

            CleanupTelegraph();
            ActiveTelegraph = CreateWorldSectorTelegraph(startPos, cleaveDir, range, arcAngle, new Color(1.0f, 0.2f, 0.1f, 0.45f));
            ActiveTelegraph.name = "[Boss_CleaveTelegraph]";
            PlayIdleAnimation();

            var skill = GetMonsterSkillData("boss_cleave");
            float telegraphDuration = (skill != null && skill.telegraphDuration > 0f) ? skill.telegraphDuration : 0.5f;

            float elapsed = 0f;
            while (elapsed < telegraphDuration)
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

            PlayAnimation("SmashAttack", 0.05f);
            PlayImpactSound();
            SpawnSweepVisualEffect(transform.position, cleaveDir, range);

            if (skill != null && skill.castVfxPrefab != null)
            {
                VFXManager.Instance?.Spawn(skill.castVfxPrefab, transform.position + cleaveDir * 2f + Vector3.up * 0.5f, Quaternion.LookRotation(cleaveDir));
            }
            if (skill != null && skill.castSoundPrefab != null)
            {
                MonsterTableAssetPlayer.PlayOneShotSound(skill.castSoundPrefab, transform.position);
            }

            if (Target != null && !IsDead)
            {
                Vector3 toTarget = Target.position - transform.position;
                toTarget.y = 0f;
                float dist = toTarget.magnitude;

                if (dist <= range && Vector3.Angle(cleaveDir, toTarget.normalized) <= arcAngle * 0.5f)
                {
                    if (Target.TryGetComponent<IDamageable>(out var damageable))
                    {
                        DamageInfo dmg = new DamageInfo(AttackDamage * 1.5f, false, transform.position, cleaveDir, KnockbackPower * 2.0f, ElementType.Physical, gameObject);
                        damageable.TakeDamage(dmg);
                    }
                    if (Cameras.QuarterViewCamera.Instance != null)
                    {
                        Cameras.QuarterViewCamera.Instance.TriggerShake(0.4f, 0.35f);
                    }
                }
            }

            float postDelay = (skill != null && skill.postAttackDelay > 0f) ? skill.postAttackDelay : 0.4f;
            yield return new WaitForSeconds(postDelay);
            IsActing = false;
        }
    }
}
