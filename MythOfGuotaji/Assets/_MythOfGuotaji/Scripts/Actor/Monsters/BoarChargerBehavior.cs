using MythOfGuotaji.Data;
using System.Collections;
using System.Collections.Generic;
using MythOfGuotaji.Combat;
using MythOfGuotaji.Core;
using UnityEngine;

namespace MythOfGuotaji.Actor
{
    [MonsterBehavior(MonsterType.Boar_Charger)]
    public class BoarChargerBehavior : MeleeMonsterBehavior
    {
        private bool _hasPenetratedDamageDealt = false;

        public override void ApplyPatternStats()
        {
            AttackRange = 15.0f;
            AttackCooldown = 3.0f;
            DashDistance = 15.0f;
            DashDuration = 1.1f;
            DashWidth = 1.2f;
            SpecialAttackRadius = 1.5f;
            TelegraphDuration = 0.35f;
        }

        public override void PerformAttack()
        {
            StartCoroutine(BoarChargeAttackRoutine());
        }

        private IEnumerator BoarChargeAttackRoutine()
        {
            IsActing = true;
            LastAttackTime = Time.time;
            _hasPenetratedDamageDealt = false;

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

            var skill = GetMonsterSkillData("boar_charge");

            // [정책 1]: 캐릭터와의 거리가 짧더라도 벽에 닿지 않는 한 최대 돌진거리(15m)까지 목표점 설정
            float maxRangeSetting = (skill != null && skill.maxChargeDistance > 0f)
                ? skill.maxChargeDistance
                : (DashDistance > 0f ? DashDistance : 15.0f);

            float totalChargeDist = CalculateMaxChargeDistance(startPos, dashDir, maxRangeSetting, ObstacleMask);

            // 전조 장판(Telegraph) 생성: 전체 돌진 거리 반영
            float telegraphWidth = DashWidth > 0f ? DashWidth : 1.2f;
            CleanupTelegraph();
            ActiveTelegraph = CreateDashTelegraph(startPos, dashDir, totalChargeDist, telegraphWidth);
            PlayIdleAnimation();

            float prepDuration = (skill != null && skill.telegraphDuration > 0f) ? skill.telegraphDuration : 0.35f;

            float elapsed = 0f;
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

            if (skill != null && skill.castSoundPrefab != null)
            {
                MonsterTableAssetPlayer.PlayOneShotSound(skill.castSoundPrefab, transform.position);
            }

            float chargeSpeed = (skill != null && skill.dashSpeed > 0f) ? skill.dashSpeed : 14.0f;
            float duration = totalChargeDist / chargeSpeed;
            float tElapsed = 0f;
            float traveled = 0f;
            bool hitWall = false;
            Vector3 lastPos = transform.position;

            ChargeSkillType chargeType = (skill != null) ? skill.chargeType : ChargeSkillType.StopOnPlayer;

            // [핵심 물리 개선]: 돌진 중에는 물리 충돌/감속을 해제하여 다른 몬스터와 부딪혀도 감속 제로
            bool origDetectCollisions = Controller != null && Controller.detectCollisions;
            if (Controller != null) Controller.detectCollisions = false;

            var colliders = gameObject.GetComponentsInChildren<Collider>();
            var origTriggers = new bool[colliders.Length];
            for (int i = 0; i < colliders.Length; i++)
            {
                origTriggers[i] = colliders[i].isTrigger;
                colliders[i].isTrigger = true;
            }

            try
            {
                while (tElapsed < duration && traveled < totalChargeDist && !hitWall)
                {
                    if (IsDead)
                    {
                        break;
                    }

                    float dt = Time.deltaTime;
                    tElapsed += dt;

                    Vector3 move = dashDir * (chargeSpeed * dt);
                    move.y = -9.81f * dt;

                    SafeMove(move);

                    Vector3 curPos = transform.position;
                    curPos.y = 0f;
                    lastPos.y = 0f;
                    traveled += Vector3.Distance(curPos, lastPos);
                    lastPos = transform.position;

                    // 벽 및 닫힌 문 판정 (정적 장애물만 검사, 다른 몬스터는 충돌 무시)
                    if (Physics.SphereCast(transform.position + Vector3.up * 0.6f, 0.4f, dashDir, out RaycastHit wallHit, 0.8f, ObstacleMask, QueryTriggerInteraction.Ignore))
                    {
                        bool isOtherMonster = wallHit.collider.CompareTag("Monster") || wallHit.collider.GetComponentInParent<MonsterAI>() != null;
                        bool isTargetPlayer = Target != null && (wallHit.collider.gameObject == Target.gameObject || wallHit.collider.GetComponentInParent<PlayerController>() != null);

                        if (!isOtherMonster && !isTargetPlayer)
                        {
                            var autoDoor = wallHit.collider.GetComponentInParent<Dungeon.DungeonAutoDoor>();
                            if (autoDoor == null || !autoDoor.IsOpen)
                            {
                                hitWall = true;
                                ApplyStun(2.5f);
                                if (skill != null && skill.impactSoundPrefab != null)
                                {
                                    MonsterTableAssetPlayer.PlayOneShotSound(skill.impactSoundPrefab, transform.position);
                                }
                                break;
                            }
                        }
                    }

                    // 플레이어 충돌 및 튕겨내기 판정
                    if (Target != null && Vector3.Distance(transform.position, Target.position) <= 1.5f)
                    {
                        var pc = Target.GetComponent<PlayerController>() ?? Target.GetComponentInParent<PlayerController>();
                        if (pc != null && !_hasPenetratedDamageDealt)
                        {
                            _hasPenetratedDamageDealt = true;

                            // 무거운 돌진체가 플레이어를 강하게 튕겨내어 붕 띄움
                            Vector3 heavyKnockback = dashDir * 20f + Vector3.up * 4.0f;
                            pc.ApplyKnockback(heavyKnockback);

                            DamageInfo dmg = new DamageInfo(AttackDamage, false, transform.position, dashDir, KnockbackPower * 2.5f, ElementType.Physical, gameObject);
                            pc.TakeDamage(dmg);

                            if (Cameras.QuarterViewCamera.Instance != null)
                            {
                                Cameras.QuarterViewCamera.Instance.TriggerShake(0.5f, 0.35f);
                            }
                        }

                        if (chargeType == ChargeSkillType.StopOnPlayer)
                        {
                            break;
                        }
                    }

                    yield return null;
                }
            }
            finally
            {
                // 돌진 종료 시 물리 충돌 정상 복원
                if (Controller != null) Controller.detectCollisions = origDetectCollisions;
                for (int i = 0; i < colliders.Length; i++)
                {
                    if (colliders[i] != null) colliders[i].isTrigger = origTriggers[i];
                }
            }

            yield return new WaitForSeconds(0.3f);
            IsActing = false;
        }
    }
}
