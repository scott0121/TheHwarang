using System.Collections;
using MythOfGuotaji.Combat;
using MythOfGuotaji.Core;
using MythOfGuotaji.Data;
using UnityEngine;

namespace MythOfGuotaji.Actor
{
    public enum DamageResponse
    {
        Proceed,
        Absorbed
    }

    /// <summary>
    /// 몬스터 1종의 고유 행동(공격 패턴, 스탯 보정, 피격 기믹, 애니메이션 선택)을 담당하는 전략 기반 클래스.
    /// </summary>
    public abstract class MonsterBehavior
    {
        public MonsterAI Owner { get; private set; }
        public MonsterType Type { get; private set; }

        public void Bind(MonsterAI owner, MonsterType type)
        {
            Owner = owner;
            Type = type;
            OnBind();
        }

        protected virtual void OnBind() { }

        public virtual void ResetState() { }

        public virtual void ApplyPatternStats() { }

        public virtual bool RequiresLineOfSight => true;

        public virtual bool ShakesCameraOnDeath => true;

        public virtual bool TickCustomBehavior(float distToPlayer, Vector3 toPlayer) => false;

        public virtual bool TrySpecialSkill(float distToPlayer, Vector3 toPlayer)
        {
            if (distToPlayer <= AttackRange && Time.time >= LastAttackTime + AttackCooldown && HasLineOfSightToPlayer())
            {
                if (CanStartAttack())
                {
                    Owner.transform.rotation = Quaternion.Slerp(Owner.transform.rotation, Quaternion.LookRotation(toPlayer.normalized), Time.deltaTime * 10f);
                    Owner.TryAttackPlayer();
                    return true;
                }
            }
            return false;
        }

        protected virtual bool CanStartAttack() => true;

        public abstract void PerformAttack();

        public virtual void StartMeleeAttack() { }

        public virtual DamageResponse PreDamage(DamageInfo info) => DamageResponse.Proceed;

        public virtual void OnAggroTriggered() { }

        public virtual bool HandleDeathProgression() => false;

        public virtual void OnControllerColliderHit(ControllerColliderHit hit) { }

        public virtual string[] MoveAnimationStates => DefaultMoveAnimationStates;

        public virtual bool AllowMoveAnimationWhileActing => false;

        public virtual void PlayAttackMotion()
        {
            PlayFirstState(0.05f, "BiteAttackForward", "BiteAttack", "Bite", "Attack");
        }

        public static readonly string[] DefaultMoveAnimationStates =
        {
            "Run", "WalkForward", "Walk", "WalkForward_RM", "Run_RM", "Fly", "CrawlForward"
        };

        protected void PlayFirstState(float transition, params string[] states)
        {
            for (int i = 0; i < states.Length; i++)
            {
                if (HasAnimState(states[i]))
                {
                    PlayAnimation(states[i], transition);
                    return;
                }
            }
            Owner.SetTriggerSafe("attack");
        }

        // =====================================================================
        // MonsterAI 상태 접근 (Owner 로 위임)
        // =====================================================================
        public Transform transform => Owner.transform;
        public GameObject gameObject => Owner.gameObject;
        public string name => Owner.name;
        public T GetComponent<T>() => Owner.GetComponent<T>();

        public Transform Target => Owner.TargetPlayer;
        public CharacterController Controller => Owner.CharacterController;
        public LayerMask ObstacleMask => Owner.ObstacleLayerMask;
        public GameObject ActiveTelegraph { get => Owner.ActiveTelegraph; set => Owner.ActiveTelegraph = value; }

        public bool IsActing { get => Owner.IsSweeping; set => Owner.IsSweeping = value; }
        public bool IsDead => Owner.IsDead;
        public bool IsStunned => Owner._stunTimer > 0f;
        public int Level => Owner.Level;

        public float LastAttackTime { get => Owner.LastAttackTime; set => Owner.LastAttackTime = value; }
        public float LastMeleeAttackTime { get => Owner.LastMeleeAttackTime; set => Owner.LastMeleeAttackTime = value; }

        public float CurrentHp { get => Owner.CurrentHpField; set => Owner.CurrentHpField = value; }
        public float MaxHp { get => Owner.MaxHpField; set => Owner.MaxHpField = value; }
        public float MoveSpeed { get => Owner.MoveSpeed; set => Owner.MoveSpeed = value; }
        public float AttackDamage { get => Owner.AttackDamage; set => Owner.AttackDamage = value; }
        public float AttackRange { get => Owner.AttackRange; set => Owner.AttackRange = value; }
        public float AttackCooldown { get => Owner.AttackCooldown; set => Owner.AttackCooldown = value; }
        public float KiteDistance { get => Owner.KiteDistance; set => Owner.KiteDistance = value; }
        public float KnockbackPower { get => Owner.KnockbackPower; set => Owner.KnockbackPower = value; }
        public float TelegraphDuration { get => Owner.TelegraphDuration; set => Owner.TelegraphDuration = value; }
        public float SpecialAttackRadius { get => Owner.SpecialAttackRadius; set => Owner.SpecialAttackRadius = value; }
        public float SpecialAttackAngle { get => Owner.SpecialAttackAngle; set => Owner.SpecialAttackAngle = value; }
        public float PostAttackDelay { get => Owner.PostAttackDelay; set => Owner.PostAttackDelay = value; }
        public float DashDistance { get => Owner.DashDistance; set => Owner.DashDistance = value; }
        public float DashWidth { get => Owner.DashWidth; set => Owner.DashWidth = value; }
        public float DashDuration { get => Owner.DashDuration; set => Owner.DashDuration = value; }

        public float MeleeAttackRange => Owner.MeleeAttackRange;
        public float MeleeAttackCooldown => Owner.MeleeAttackCooldown;
        public float MeleeAttackDamage => Owner.MeleeAttackDamage;
        public ProjectileParryType ProjectileParryType => Owner.ProjectileParryType;
        public Dungeon.RoomController OwningRoom => Owner.OwningRoom;

        public Coroutine StartCoroutine(IEnumerator routine) => Owner.StartCoroutine(routine);
        public void StopCoroutine(Coroutine routine) => Owner.StopCoroutine(routine);

        public MonsterSkillEntry GetMonsterSkillData(string skillId) => Owner.GetMonsterSkillData(skillId);
        public bool HasLineOfSightToPlayer() => Owner.HasLineOfSightToPlayer();
        public void NavigateTowardsTarget(float distToPlayer) => Owner.NavigateTowardsTarget(distToPlayer);
        public void SafeMove(Vector3 motion) => Owner.SafeMove(motion);
        public void CleanupTelegraph() => Owner.CleanupTelegraph();
        public void SetRendererColor(Color c) => Owner.SetRendererColor(c);
        public void TriggerAggro() => Owner.TriggerAggro();
        public void ApplyStun(float duration) => Owner.ApplyStun(duration);
        public void Die() => Owner.Die();

        public void PlayAttackAnimation() => Owner.PlayAttackAnimation();
        public void PlayIdleAnimation() => Owner.PlayIdleAnimation();
        public void PlayMoveAnimation() => Owner.PlayMoveAnimation();
        public void PlayAnimation(string stateName, float transitionDuration = 0.1f) => Owner.PlayAnimation(stateName, transitionDuration);
        public bool HasAnimState(string stateName) => Owner.HasAnimState(stateName);

        // 정적 헬퍼 위임
        public GameObject CreateFloorGuideBox(Vector3 startPos, Vector3 dir, float length, float width, Color color)
            => MonsterTelegraphs.CreateFloorGuideBox(startPos, dir, length, width, color);

        public void CreateTelegraphBorderLine(Transform parent, Vector3 localPos, Vector3 localScale, Color color)
            => MonsterTelegraphs.CreateTelegraphBorderLine(parent, localPos, localScale, color);

        public GameObject CreateWorldSectorTelegraph(Vector3 center, Vector3 forward, float radius, float arcAngle, Color color)
            => MonsterTelegraphs.CreateWorldSectorTelegraph(center, forward, radius, arcAngle, color);

                public GameObject CreateCircleTelegraph(Vector3 center, float radius, Color color)
            => MonsterTelegraphs.CreateCircleTelegraph(center, radius, color);

        /// <summary>
        /// 캐릭터와의 거리가 짧더라도 벽/닫힌 문에 닿지 않는 한 최대 돌진 거리를 계산
        /// - 플레이어와 다른 몬스터는 장애물 검사에서 제외(통과)
        /// - 닫힌 문/벽에 걸리면 그 직전까지의 거리를 반환
        /// </summary>
        public float CalculateMaxChargeDistance(Vector3 startPos, Vector3 dashDir, float maxDistance, LayerMask obstacleMask)
        {
            Vector3 eye = startPos + Vector3.up * 0.8f;
            dashDir.y = 0f;
            if (dashDir.sqrMagnitude < 0.001f) dashDir = Vector3.forward;
            dashDir.Normalize();

            RaycastHit[] hits = Physics.SphereCastAll(eye, 0.4f, dashDir, maxDistance, obstacleMask, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            float finalDist = maxDistance;

            foreach (var hit in hits)
            {
                if (hit.collider == null) continue;
                if (hit.collider.gameObject == gameObject) continue;

                // 플레이어와 다른 몬스터는 돌진 경로의 벽/장애물이 아니므로 통과
                if (hit.collider.CompareTag("Player") || hit.collider.GetComponentInParent<PlayerController>() != null) continue;
                if (hit.collider.CompareTag("Monster") || hit.collider.GetComponentInParent<MonsterAI>() != null) continue;

                // 문 검사: 열린 문은 통과, 닫힌 문/벽은 충돌
                var autoDoor = hit.collider.GetComponentInParent<Dungeon.DungeonAutoDoor>();
                if (autoDoor != null && autoDoor.IsOpen) continue;

                // 벽/닫힌 문 발견 -> 벽 직전 0.4m까지를 목표 지점으로 설정
                finalDist = Mathf.Max(1.0f, hit.distance - 0.4f);
                break;
            }

            return finalDist;
        }

        public GameObject CreateDashTelegraph(Vector3 startPos, Vector3 dashDir, float length, float width)
            => MonsterTelegraphs.CreateDashTelegraph(startPos, dashDir, length, width);

        public GameObject CreateSweepTelegraph(float radius, float arcAngle)
            => MonsterTelegraphs.CreateSweepTelegraph(transform.position, transform.forward, radius, arcAngle);

        public void SpawnSweepVisualEffect(Vector3 center, Vector3 forward, float radius)
            => MonsterTelegraphs.SpawnSweepVisualEffect(center, forward, radius);

        public void SpawnShockwaveVisualEffect(Vector3 center, float radius)
            => MonsterTelegraphs.SpawnShockwaveVisualEffect(center, radius);

        public void SpawnVacuumPullEffect(Vector3 center, Vector3 forward, float radius)
            => MonsterTelegraphs.SpawnVacuumPullEffect(center, forward, radius);

        private static AudioClip s_thudClip = null;
        public void PlayImpactSound()
        {
            var audioSrc = Owner.GetComponent<AudioSource>();
            if (audioSrc == null)
            {
                audioSrc = Owner.gameObject.AddComponent<AudioSource>();
                audioSrc.spatialBlend = 1.0f;
                audioSrc.minDistance = 3f;
                audioSrc.maxDistance = 25f;
                audioSrc.rolloffMode = AudioRolloffMode.Linear;
            }

            if (s_thudClip == null)
            {
                int sampleRate = 44100;
                float duration = 0.35f;
                int sampleCount = (int)(sampleRate * duration);
                float[] samples = new float[sampleCount];
                for (int i = 0; i < sampleCount; i++)
                {
                    float t = (float)i / sampleRate;
                    float freq = Mathf.Lerp(160f, 40f, t / duration);
                    float env = Mathf.Exp(-t * 12f);
                    float sin = Mathf.Sin(2f * Mathf.PI * freq * t);
                    float noise = (Random.value * 2f - 1f) * 0.3f * Mathf.Exp(-t * 20f);
                    samples[i] = Mathf.Clamp((sin * 0.8f + noise) * env, -1f, 1f);
                }
                s_thudClip = AudioClip.Create("MonsterImpactThud", sampleCount, 1, sampleRate, false);
                s_thudClip.SetData(samples, 0);
            }

            audioSrc.pitch = Random.Range(0.85f, 1.15f);
            audioSrc.PlayOneShot(s_thudClip, 0.9f);
        }
    }
}
