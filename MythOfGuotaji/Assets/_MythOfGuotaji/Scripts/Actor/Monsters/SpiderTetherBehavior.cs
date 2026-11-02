using System.Collections;
using MythOfGuotaji.Combat;
using MythOfGuotaji.Core;
using MythOfGuotaji.Data;
using UnityEngine;

namespace MythOfGuotaji.Actor
{
    [MonsterBehavior(MonsterType.Spider_Tether)]
    public class SpiderTetherBehavior : MeleeMonsterBehavior
    {
        private SpiderWebTether _activeTether;
        private Coroutine _tetherRoutine;

        public override void ApplyPatternStats()
        {
            var skill = GetMonsterSkillData("spider_web_tether");
            AttackRange = (skill != null && skill.maxRange > 0f) ? skill.maxRange : 12.0f;
            AttackCooldown = (skill != null && skill.cooldown > 0f) ? skill.cooldown : 4.5f;
            TelegraphDuration = (skill != null && skill.telegraphDuration > 0f) ? skill.telegraphDuration : 0.3f;
        }

        public override bool TrySpecialSkill(float distToPlayer, Vector3 toPlayer)
        {
            // 이미 거미줄 집중 견인 중이면 상태 유지
            if (IsActing) return true;

            var skill = GetMonsterSkillData("spider_web_tether");
            float minR = (skill != null && skill.minRange > 0f) ? skill.minRange : 3.0f;
            float maxR = (skill != null && skill.maxRange > 0f) ? skill.maxRange : AttackRange;
            float cd = (skill != null && skill.cooldown > 0f) ? skill.cooldown : AttackCooldown;

            // 1. minRange ~ maxRange 사이에 플레이어가 있고, 쿨다운 시간이 돌아왔으며, 시야가 확보되었을 때
            if (distToPlayer >= minR && distToPlayer <= maxR && Time.time >= LastAttackTime + cd && HasLineOfSightToPlayer())
            {
                if (CanStartAttack())
                {
                    if (_tetherRoutine != null) StopCoroutine(_tetherRoutine);
                    _tetherRoutine = StartCoroutine(SpiderTetherRoutine(skill));
                    return true;
                }
            }

            return false;
        }

        private IEnumerator SpiderTetherRoutine(MonsterSkillEntry skill)
        {
            IsActing = true;
            LastAttackTime = Time.time;

            if (Target == null || IsDead)
            {
                IsActing = false;
                yield break;
            }

            // 시선 회전
            Vector3 toTarget = Target.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(toTarget.normalized);
            }

            // Telegraph 선딜레이 및 전조 표시
            float windup = (skill != null && skill.telegraphDuration > 0f) ? skill.telegraphDuration : TelegraphDuration;
            if (windup > 0f)
            {
                if (skill != null && skill.castSoundPrefab != null)
                {
                    MonsterTableAssetPlayer.PlayOneShotSound(skill.castSoundPrefab, transform.position);
                }
                if (skill != null && skill.castVfxPrefab != null)
                {
                    MonsterTableAssetPlayer.SpawnVfx(skill.castVfxPrefab, transform.position + Vector3.up * 0.8f, transform.rotation);
                }

                float elapsedTelegraph = 0f;
                while (elapsedTelegraph < windup)
                {
                    if (IsDead || Target == null || IsStunned)
                    {
                        IsActing = false;
                        yield break;
                    }

                    Vector3 lookDir = Target.position - transform.position;
                    lookDir.y = 0f;
                    if (lookDir.sqrMagnitude > 0.01f)
                    {
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir.normalized), Time.deltaTime * 15f);
                    }

                    elapsedTelegraph += Time.deltaTime;
                    yield return null;
                }
            }

            if (IsDead || Target == null || IsStunned)
            {
                IsActing = false;
                yield break;
            }

            var pc = Target.GetComponent<PlayerController>();
            if (pc == null || pc.IsDead)
            {
                IsActing = false;
                yield break;
            }

            PlayAttackAnimation();

            // 2. 거미줄 오브젝트 생성 및 결속 (Projectile 투사체 충돌 자폭 방지를 위해 독립 GameObject로 생성)
            GameObject tetherGo = new GameObject("SpiderWebTether");
            _activeTether = tetherGo.AddComponent<SpiderWebTether>();

            float tetherDuration = (skill != null && skill.cooldown > 0f) ? skill.cooldown : AttackCooldown;
            float pullSpeed = (skill != null && Mathf.Abs(skill.knockbackPower) > 0.1f) ? Mathf.Abs(skill.knockbackPower) : 3.5f;

            _activeTether.Attach(Owner, pc, duration: tetherDuration, speed: pullSpeed, skill: skill);

            // 3. 거미줄이 연결된 쿨다운 시간 동안 캐릭터를 집중해서 잡고 있음 (Channeling)
            float elapsedChannel = 0f;
            while (elapsedChannel < tetherDuration)
            {
                if (IsDead || Target == null || pc.IsDead || IsStunned)
                {
                    break;
                }

                // 4. 플레이어가 대시로 탈출했거나 사슬이 끊어지면 조기 탈출
                if (_activeTether == null || !_activeTether.IsAttached)
                {
                    break;
                }

                // 플레이어 방향으로 지속 집중 주시
                Vector3 lookDir = Target.position - transform.position;
                lookDir.y = 0f;
                if (lookDir.sqrMagnitude > 0.01f)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir.normalized), Time.deltaTime * 10f);
                }

                elapsedChannel += Time.deltaTime;
                yield return null;
            }

            // 거미줄 정리
            if (_activeTether != null)
            {
                _activeTether.Detach();
                _activeTether = null;
            }

            // 5. 스킬 종료 후 쿨다운 타이머 시작 기준 시점 갱신 (쿨다운 시간이 돌아가면 다시 1번으로 복귀)
            LastAttackTime = Time.time;

            float postDelay = (skill != null && skill.postAttackDelay > 0f) ? skill.postAttackDelay : 0.3f;
            if (postDelay > 0f)
            {
                yield return new WaitForSeconds(postDelay);
            }

            IsActing = false;
        }

        public override void PerformAttack()
        {
            // TrySpecialSkill 및 SpiderTetherRoutine에서 전담
        }

        public override void ResetState()
        {
            base.ResetState();
            if (_tetherRoutine != null)
            {
                StopCoroutine(_tetherRoutine);
                _tetherRoutine = null;
            }
            if (_activeTether != null)
            {
                _activeTether.Detach();
                _activeTether = null;
            }
            IsActing = false;
        }
    }
}
