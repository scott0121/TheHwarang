using MythOfGuotaji.Data;
using System.Collections;
using System.Collections.Generic;
using MythOfGuotaji.Combat;
using MythOfGuotaji.Core;
using UnityEngine;

namespace MythOfGuotaji.Actor
{
    [MonsterBehavior(MonsterType.Minion_BigMon)]
    public class BigMonBehavior : MeleeMonsterBehavior
    {
        private bool _dashHitWallObstacle = false;
        private bool _dashHitPlayer = false;
        private bool _hasPenetratedDamageDealt = false;

        public override void ApplyPatternStats()
        {
            AttackRange = 20.0f;
            AttackCooldown = 3.5f;
            DashDistance = 20.0f;
            DashDuration = 3.0f;
            DashWidth = 1.5f;
            SpecialAttackRadius = 3.0f;
            TelegraphDuration = 0.4f;
        }

        public override bool AllowMoveAnimationWhileActing => true;

        protected override bool CanStartAttack() => HasClearChargePathToPlayer();

        public override void PerformAttack()
        {
            if (Target == null) return;
            Vector3 toPlayer = Target.position - transform.position;
            toPlayer.y = 0f;
            float dist = toPlayer.magnitude;

            if (!HasClearChargePathToPlayer())
            {
                NavigateTowardsTarget(dist);
                return;
            }

            StartCoroutine(BigMonChargeAttackRoutine());
        }

        public override void PlayAttackMotion()
        {
            PlayFirstState(0.05f, "SmashAttackForwardLeft", "SmashAttackLeft", "SpittersAttack");
        }

        public override void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (!IsActing) return;
            if (hit.collider == null || hit.collider.gameObject == gameObject) return;

            // [정책]: 몬스터끼리는 절대 충돌하지 않음
            if (hit.collider.CompareTag("Monster") || hit.collider.GetComponentInParent<MonsterAI>() != null)
            {
                return;
            }

            if (hit.collider.CompareTag("Player") || hit.collider.GetComponentInParent<PlayerController>() != null)
            {
                _dashHitPlayer = true;
                return;
            }

            if (hit.normal.y > 0.5f) return;

            // 벽 또는 닫힌 문 충돌
            if (Vector3.Dot(hit.normal, -transform.forward) > 0.15f)
            {
                _dashHitWallObstacle = true;
            }
        }

        public bool HasClearChargePathToPlayer()
        {
            if (Target == null) return false;

            Vector3 myEye = transform.position + Vector3.up * 1.0f;
            Vector3 playerEye = Target.position + Vector3.up * 1.0f;
            Vector3 toPlayer = playerEye - myEye;
            float dist = toPlayer.magnitude;

            if (dist < 0.2f) return true;
            Vector3 dir = toPlayer / dist;

            RaycastHit[] hits = Physics.RaycastAll(myEye, dir, dist, ObstacleMask, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var hit in hits)
            {
                if (hit.collider == null || hit.collider.gameObject == gameObject) continue;
                if (hit.collider.CompareTag("Monster") || hit.collider.GetComponentInParent<MonsterAI>() != null) continue;
                if (hit.collider.CompareTag("Player") || hit.collider.GetComponentInParent<PlayerController>() != null) break;
                return false;
            }

            RaycastHit[] sphereHits = Physics.SphereCastAll(myEye, 0.45f, dir, dist, ObstacleMask, QueryTriggerInteraction.Ignore);
            System.Array.Sort(sphereHits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var sHit in sphereHits)
            {
                if (sHit.collider == null || sHit.collider.gameObject == gameObject) continue;
                if (sHit.collider.CompareTag("Monster") || sHit.collider.GetComponentInParent<MonsterAI>() != null) continue;
                if (sHit.collider.CompareTag("Player") || sHit.collider.GetComponentInParent<PlayerController>() != null) break;
                return false;
            }

            return true;
        }

        private IEnumerator BigMonChargeAttackRoutine()
        {
            IsActing = true;
            LastAttackTime = Time.time;
            _dashHitWallObstacle = false;
            _dashHitPlayer = false;
            _hasPenetratedDamageDealt = false;

            if (Target == null)
            {
                IsActing = false;
                yield break;
            }

            Vector3 startPos = transform.position;
            Vector3 toPlayer = Target.position - startPos;
            toPlayer.y = 0f;
            Vector3 dashDir = toPlayer.sqrMagnitude > 0.001f ? toPlayer.normalized : transform.forward;
            transform.rotation = Quaternion.LookRotation(dashDir);

            var bigMonSkill = GetMonsterSkillData("turtle_charge");

            // [정책 1]: 캐릭터와의 거리가 최대 돌진거리(20m)보다 짧더라도, 벽에 닿지 않는 한 최대치로 목표점 설정
            float maxRangeSetting = (bigMonSkill != null && bigMonSkill.maxChargeDistance > 0f)
                ? bigMonSkill.maxChargeDistance
                : (DashDistance > 0f ? DashDistance : 20.0f);

            float totalChargeDist = CalculateMaxChargeDistance(startPos, dashDir, maxRangeSetting, ObstacleMask);

            // 전조 장판(Telegraph) 생성: 전체 돌진 거리 반영
            float telegraphWidth = DashWidth > 0f ? DashWidth : 1.5f;
            CleanupTelegraph();
            ActiveTelegraph = CreateDashTelegraph(startPos, dashDir, totalChargeDist, telegraphWidth);
            PlayIdleAnimation();

            float prepDuration = (bigMonSkill != null && bigMonSkill.telegraphDuration > 0f)
                ? bigMonSkill.telegraphDuration
                : (TelegraphDuration > 0f ? TelegraphDuration : 0.4f);

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

            if (!HasClearChargePathToPlayer())
            {
                CleanupTelegraph();
                IsActing = false;
                yield break;
            }

            PlayMoveAnimation();

            var trailVfx = bigMonSkill?.trailVfxPrefab ?? bigMonSkill?.castVfxPrefab;
            if (trailVfx != null)
            {
                VFXManager.Instance?.Spawn(trailVfx, transform.position, Quaternion.LookRotation(dashDir));
            }
            if (bigMonSkill != null && bigMonSkill.castSoundPrefab != null)
            {
                MonsterTableAssetPlayer.PlayOneShotSound(bigMonSkill.castSoundPrefab, transform.position);
            }

            // [정책 3]: 로그가속/감속 수치를 MonsterSkillTable에서 동적으로 읽어와 적용
            float chargeDuration = (bigMonSkill != null && bigMonSkill.dashDuration > 0f)
                ? bigMonSkill.dashDuration
                : (DashDuration > 0f ? DashDuration : 3.0f);

            float accelExp = (bigMonSkill != null && bigMonSkill.chargeAccelerationExponent > 0f)
                ? bigMonSkill.chargeAccelerationExponent
                : 1.35f;

            float speedMult = (bigMonSkill != null && bigMonSkill.chargeSpeedMultiplier > 0f)
                ? bigMonSkill.chargeSpeedMultiplier
                : 1.5f;

            float initialOffset = (bigMonSkill != null)
                ? bigMonSkill.chargeInitialSpeedOffset
                : 0.3f;

            ChargeSkillType chargeType = (bigMonSkill != null)
                ? bigMonSkill.chargeType
                : ChargeSkillType.StopOnPlayer;

            // [핵심 물리 개선]: 돌진 중에는 CharacterController의 물리 충돌 및 마찰 감속을 완전히 해제
            // - 다른 몬스터(동일 거북이 포함)와 충돌하여 감속되는 것을 원천 차단
            // - 플레이어와 닿았을 때도 물리 엔진 마찰로 감속되지 않고 원래 가속도 그대로 유지
            bool origDetectCollisions = Controller != null && Controller.detectCollisions;
            if (Controller != null) Controller.detectCollisions = false;

            var colliders = gameObject.GetComponentsInChildren<Collider>();
            var origTriggers = new bool[colliders.Length];
            for (int i = 0; i < colliders.Length; i++)
            {
                origTriggers[i] = colliders[i].isTrigger;
                colliders[i].isTrigger = true;
            }

            float chargeElapsed = 0f;
            float traveled = 0f;
            Vector3 lastPos = transform.position;

            try
            {
                while (chargeElapsed < chargeDuration && traveled < totalChargeDist && !_dashHitWallObstacle)
                {
                    if (IsDead)
                    {
                        CleanupTelegraph();
                        break;
                    }

                    float dt = Time.deltaTime;
                    chargeElapsed += dt;

                    float t = Mathf.Clamp01(chargeElapsed / chargeDuration);
                    float speedFactor = Mathf.Pow(t, accelExp) * speedMult + initialOffset;
                    float currentSpeed = (totalChargeDist / chargeDuration) * speedFactor;

                    Vector3 moveDelta = dashDir * (currentSpeed * dt);
                    moveDelta.y = -9.81f * dt;

                    SafeMove(moveDelta);

                    Vector3 currentPos = transform.position;
                    currentPos.y = 0f;
                    lastPos.y = 0f;
                    traveled += Vector3.Distance(currentPos, lastPos);
                    lastPos = transform.position;

                    // 벽 및 닫힌 문 판정 (정적 장애물만 검사)
                    if (Physics.SphereCast(transform.position + Vector3.up * 0.8f, 0.45f, dashDir, out RaycastHit wHit, 0.9f, ObstacleMask, QueryTriggerInteraction.Ignore))
                    {
                        bool isOtherMonster = wHit.collider.CompareTag("Monster") || wHit.collider.GetComponentInParent<MonsterAI>() != null;
                        bool isTargetPlayer = Target != null && (wHit.collider.gameObject == Target.gameObject || wHit.collider.GetComponentInParent<PlayerController>() != null);

                        if (!isOtherMonster && !isTargetPlayer)
                        {
                            var autoDoor = wHit.collider.GetComponentInParent<Dungeon.DungeonAutoDoor>();
                            if (autoDoor == null || !autoDoor.IsOpen)
                            {
                                _dashHitWallObstacle = true;
                                break;
                            }
                        }
                    }

                    // 플레이어 충돌 및 강타(Bounce) 판정
                    if (Target != null)
                    {
                        Vector3 myPos = transform.position;
                        myPos.y = 0f;
                        Vector3 pPos = Target.position;
                        pPos.y = 0f;
                        float distToPlayer = Vector3.Distance(myPos, pPos);

                        if (distToPlayer <= 1.6f || _dashHitPlayer)
                        {
                            var pc = Target.GetComponent<PlayerController>() ?? Target.GetComponentInParent<PlayerController>();
                            if (pc != null && !_hasPenetratedDamageDealt)
                            {
                                _hasPenetratedDamageDealt = true;

                                // [핵심 물리 연출]: 무거운 거체가 플레이어를 강하게 튕겨내어 붕 날려보냄
                                Vector3 heavyKnockback = dashDir * 22f + Vector3.up * 4.5f;
                                pc.ApplyKnockback(heavyKnockback);

                                DamageInfo dmg = new DamageInfo(AttackDamage, false, transform.position, dashDir, KnockbackPower * 2.5f, ElementType.Physical, gameObject);
                                pc.TakeDamage(dmg);

                                if (Cameras.QuarterViewCamera.Instance != null)
                                {
                                    Cameras.QuarterViewCamera.Instance.TriggerShake(0.6f, 0.4f);
                                }
                            }

                            if (chargeType == ChargeSkillType.StopOnPlayer)
                            {
                                // 1번 타입: 플레이어를 강하게 튕겨낸 뒤 제동을 걸며 착지 충격파 발생
                                _dashHitPlayer = true;
                                break;
                            }
                            else if (chargeType == ChargeSkillType.PenetratePlayer)
                            {
                                // 2번 타입: 플레이어를 튕겨내고 단 1의 감속도 없이 원래 속도로 끝까지 질주
                                _dashHitPlayer = false;
                            }
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

            CleanupTelegraph();

            Vector3 impactPos = transform.position;
            impactPos.y = 0f;

            PlayAttackAnimation();
            PlayImpactSound();

            if (Cameras.QuarterViewCamera.Instance != null)
            {
                Cameras.QuarterViewCamera.Instance.TriggerShake(0.45f, 0.35f);
            }

            float shockwaveRadius = (bigMonSkill != null && bigMonSkill.impactRadius > 0f)
                ? bigMonSkill.impactRadius
                : (SpecialAttackRadius > 0f ? SpecialAttackRadius : 3.0f);

            SpawnShockwaveVisualEffect(impactPos, shockwaveRadius);

            if (bigMonSkill != null && bigMonSkill.impactVfxPrefab != null)
            {
                VFXManager.Instance?.Spawn(bigMonSkill.impactVfxPrefab, impactPos + Vector3.up * 0.2f, Quaternion.identity);
            }
            if (bigMonSkill != null && bigMonSkill.impactSoundPrefab != null)
            {
                MonsterTableAssetPlayer.PlayOneShotSound(bigMonSkill.impactSoundPrefab, impactPos);
            }

            Collider[] hitColliders = Physics.OverlapSphere(impactPos, shockwaveRadius);
            foreach (var col in hitColliders)
            {
                if (col.CompareTag("Player") || col.GetComponentInParent<PlayerController>() != null)
                {
                    if (col.TryGetComponent<IDamageable>(out var dmgReceiver))
                    {
                        DamageInfo dmg = new DamageInfo(AttackDamage, false, impactPos, dashDir, KnockbackPower * 1.5f, ElementType.Physical, gameObject);
                        dmgReceiver.TakeDamage(dmg);
                    }
                    break;
                }
            }

            yield return new WaitForSeconds(PostAttackDelay > 0f ? PostAttackDelay : 0.4f);
            IsActing = false;
        }
    }
}
