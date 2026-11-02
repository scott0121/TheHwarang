using System.Collections;
using MythOfGuotaji.Combat;
using MythOfGuotaji.Core;
using MythOfGuotaji.Data;
using UnityEngine;

namespace MythOfGuotaji.Actor
{
    [MonsterBehavior(MonsterType.Elite)]
    public class EliteBehavior : MonsterBehavior
    {
        private float _lastEliteSkillTime = -99f;
        private float _lastLeapSmashTime = -99f;

        public override void ResetState()
        {
            _lastEliteSkillTime = -99f;
            _lastLeapSmashTime = -99f;
        }

        public override void ApplyPatternStats()
        {
            AttackRange = 3.0f;
            AttackCooldown = 1.8f;
        }

        public override bool TrySpecialSkill(float distToPlayer, Vector3 toPlayer)
        {
            // 2번 스킬: 20미터 안의 플레이어를 목표로 도약 강타 (LeapSmash, 쿨다운 6초)
            if (distToPlayer >= 4.0f && distToPlayer <= 20.0f && Time.time >= _lastLeapSmashTime + 6.0f)
            {
                StartCoroutine(EliteLeapSmashRoutine());
                return true;
            }

            // 1번 스킬: 굴러가는 바위 투척 (EliteBoulder, 쿨다운 3초)
            if (distToPlayer >= 5.0f && distToPlayer <= 20.0f && Time.time >= _lastEliteSkillTime + 3.0f && HasLineOfSightToPlayer())
            {
                StartCoroutine(EliteBoulderSkillRoutine());
                return true;
            }

            return false;
        }

        public override void PerformAttack()
        {
            StartCoroutine(EliteMeleeAttackRoutine());
        }

        public override void StartMeleeAttack()
        {
            StartCoroutine(EliteMeleeAttackRoutine());
        }

        public override void PlayAttackMotion()
        {
            PlayFirstState(0.05f, "Bite", "Attack", "Attack01");
        }

        private IEnumerator EliteMeleeAttackRoutine()
        {
            IsActing = true;
            LastAttackTime = Time.time;

            PlayAttackAnimation();

            var stat = Data.GameDataManager.Instance?.MonsterStats?.GetStat(Type);
            if (stat?.normalAttack != null)
            {
                if (stat.normalAttack.attackVfxPrefab != null)
                {
                    VFXManager.Instance?.Spawn(stat.normalAttack.attackVfxPrefab, transform.position + transform.forward * 1.0f + Vector3.up * 0.7f, transform.rotation);
                }
                if (stat.normalAttack.attackSoundPrefab != null)
                {
                    MonsterTableAssetPlayer.PlayOneShotSound(stat.normalAttack.attackSoundPrefab, transform.position);
                }
            }

            yield return new WaitForSeconds(0.3f);

            if (!IsDead && Target != null)
            {
                float dist = Vector3.Distance(transform.position, Target.position);
                if (dist <= AttackRange + 0.8f)
                {
                    if (Target.TryGetComponent<IDamageable>(out var damageable))
                    {
                        DamageInfo dmg = new DamageInfo(AttackDamage, false, transform.position, transform.forward, KnockbackPower, ElementType.Physical, gameObject);
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
                    }
                }
            }

            yield return new WaitForSeconds(0.3f);
            IsActing = false;
        }

        /// <summary>
        /// [엘리트 2번 스킬]: 20m 안의 플레이어 현재 위치를 목표로 0.5초 예고 후 1.5초간 10m 높이 포물선 도약 강타
        /// - 가는 과정은 충돌(부딪힘) 없음
        /// - Target 위치에 5m 원형 텔레그래프 장판 표시 (패링 불가)
        /// - 착지 시 5m 반경 충격파 피해
        /// </summary>
        private IEnumerator EliteLeapSmashRoutine()
        {
            IsActing = true;
            _lastLeapSmashTime = Time.time;
            LastAttackTime = Time.time;

            if (Target == null)
            {
                IsActing = false;
                yield break;
            }

            var skill = GetMonsterSkillData("elite_leap_smash");

            Vector3 startPos = transform.position;
            // 플레이어의 현재 바닥 위치를 도약 목표점으로 락온
            Vector3 targetFloorPos = Target.position;
            targetFloorPos.y = startPos.y;

            Vector3 toTarget = targetFloorPos - startPos;
            toTarget.y = 0f;
            Vector3 leapDir = toTarget.sqrMagnitude > 0.001f ? toTarget.normalized : transform.forward;
            transform.rotation = Quaternion.LookRotation(leapDir);

            // 1. Telegraph: Target 위치에 5미터 원형으로 표시 (패링 불가 붉은색 경고)
            float shockwaveRadius = (skill != null && skill.aoeRadius > 0f) ? skill.aoeRadius : 5.0f;
            Color teleColor = (skill != null) ? skill.telegraphColor : new Color(0.95f, 0.2f, 0.1f, 0.85f);
            GameObject circleTelegraph = CreateCircleTelegraph(targetFloorPos, shockwaveRadius, teleColor);

            PlayIdleAnimation();

            // 0.5초 Windup (전조 시간)
            float windupDuration = (skill != null && skill.telegraphDuration > 0f) ? skill.telegraphDuration : 0.5f;
            float elapsed = 0f;
            while (elapsed < windupDuration)
            {
                if (IsDead)
                {
                    if (circleTelegraph != null) Object.Destroy(circleTelegraph);
                    IsActing = false;
                    yield break;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            PlayAttackAnimation();

            if (skill != null && skill.castSoundPrefab != null)
            {
                MonsterTableAssetPlayer.PlayOneShotSound(skill.castSoundPrefab, transform.position);
            }
            else
            {
                VFXManager.Instance?.Spawn("HCFX_Flash_01", transform.position, Quaternion.identity);
            }

            // 2. 1.5초간 10미터 높이까지 포물선 비행 (가는 과정 부딪힘 없음)
            float flightTime = (skill != null && skill.leapDuration > 0f) ? skill.leapDuration : 1.5f;
            float maxArcHeight = (skill != null && skill.leapHeight > 0f) ? skill.leapHeight : 10.0f;
            float flightElapsed = 0f;

            // 비행 중에는 CharacterController 충돌 판정을 일시 해제하여 어떤 오브젝트와도 부딪히지 않음
            bool ccOriginallyEnabled = Controller != null && Controller.enabled;
            if (Controller != null) Controller.enabled = false;

            while (flightElapsed < flightTime)
            {
                if (IsDead)
                {
                    if (circleTelegraph != null) Object.Destroy(circleTelegraph);
                    if (Controller != null) Controller.enabled = ccOriginallyEnabled;
                    IsActing = false;
                    yield break;
                }

                flightElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(flightElapsed / flightTime);

                // 수평 위치 Lerp
                Vector3 currentPos = Vector3.Lerp(startPos, targetFloorPos, t);
                // 수직 포물선 곡선 (정점 높이: 10m)
                float verticalOffset = Mathf.Sin(t * Mathf.PI) * maxArcHeight;
                currentPos.y = Mathf.Lerp(startPos.y, targetFloorPos.y, t) + verticalOffset;

                transform.position = currentPos;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(leapDir), Time.deltaTime * 10f);

                yield return null;
            }

            // 착지 복귀
            transform.position = targetFloorPos;
            if (Controller != null) Controller.enabled = ccOriginallyEnabled;

            if (circleTelegraph != null)
            {
                Object.Destroy(circleTelegraph);
            }

            // 3. 착지 시 5미터 반경 충격파 발생
            PlayImpactSound();

            if (Cameras.QuarterViewCamera.Instance != null)
            {
                Cameras.QuarterViewCamera.Instance.TriggerShake(0.55f, 0.4f);
            }

            SpawnShockwaveVisualEffect(targetFloorPos, shockwaveRadius);

            if (skill != null && skill.impactVfxPrefab != null)
            {
                VFXManager.Instance?.Spawn(skill.impactVfxPrefab, targetFloorPos + Vector3.up * 0.2f, Quaternion.identity);
            }
            else
            {
                VFXManager.Instance?.Spawn("HCFX_Explosion_01", targetFloorPos + Vector3.up * 0.2f, Quaternion.identity);
            }

            if (skill != null && skill.impactSoundPrefab != null)
            {
                MonsterTableAssetPlayer.PlayOneShotSound(skill.impactSoundPrefab, targetFloorPos);
            }

            // 5미터 반경 충격파 피해 적용 (패링 불가)
            float damageAmount = (skill != null && skill.fixedDamage > 0f) ? skill.fixedDamage : AttackDamage * 2.0f;
            float knockback = (skill != null && skill.knockbackPower > 0f) ? skill.knockbackPower : KnockbackPower * 2.5f;

            Collider[] hitColliders = Physics.OverlapSphere(targetFloorPos, shockwaveRadius);
            foreach (var col in hitColliders)
            {
                if (col.CompareTag("Player") || col.GetComponentInParent<PlayerController>() != null)
                {
                    if (col.TryGetComponent<IDamageable>(out var dmgReceiver))
                    {
                        Vector3 knockbackDir = (col.transform.position - targetFloorPos);
                        knockbackDir.y = 0f;
                        if (knockbackDir.sqrMagnitude < 0.001f) knockbackDir = leapDir;
                        knockbackDir.Normalize();

                        DamageInfo dmg = new DamageInfo(damageAmount, false, targetFloorPos, knockbackDir, knockback, ElementType.Physical, gameObject);
                        dmgReceiver.TakeDamage(dmg);
                    }
                    break;
                }
            }

            float postDelay = (skill != null && skill.postAttackDelay > 0f) ? skill.postAttackDelay : 0.4f;
            yield return new WaitForSeconds(postDelay);
            IsActing = false;
        }

        private IEnumerator EliteBoulderSkillRoutine()
        {
            IsActing = true;
            _lastEliteSkillTime = Time.time;
            LastAttackTime = Time.time;

            if (Target == null)
            {
                IsActing = false;
                yield break;
            }

            Vector3 startPos = transform.position;
            Vector3 toPlayer = Target.position - startPos;
            toPlayer.y = 0f;
            Vector3 throwDir = toPlayer.sqrMagnitude > 0.001f ? toPlayer.normalized : transform.forward;
            transform.rotation = Quaternion.LookRotation(throwDir);

            Color telegraphColor = new Color(0.95f, 0.55f, 0.15f, 0.35f);
            GameObject guideBox = CreateFloorGuideBox(startPos, throwDir, 20.0f, 0.5f, telegraphColor);

            var skill = GetMonsterSkillData("elite_boulder");
            if (skill != null && skill.castVfxPrefab != null)
            {
                VFXManager.Instance?.Spawn(skill.castVfxPrefab, startPos + Vector3.up * 0.8f + throwDir * 1.5f, Quaternion.LookRotation(throwDir));
            }
            if (skill != null && skill.castSoundPrefab != null)
            {
                MonsterTableAssetPlayer.PlayOneShotSound(skill.castSoundPrefab, startPos);
            }

            float telegraphDuration = (skill != null && skill.telegraphDuration > 0f) ? skill.telegraphDuration : 0.5f;
            float elapsed = 0f;
            while (elapsed < telegraphDuration)
            {
                if (IsDead)
                {
                    if (guideBox != null) Object.Destroy(guideBox);
                    IsActing = false;
                    yield break;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            PlayAttackAnimation();

            GameObject boulderPrefab = skill?.GetProjectilePrefab();
            if (boulderPrefab == null && Data.GameDataManager.Instance != null && Data.GameDataManager.Instance.Prefabs != null)
            {
                boulderPrefab = Data.PrefabDatabaseSO.ResolvePrefab(ref Data.GameDataManager.Instance.Prefabs.rollingBoulderPrefab, "Prefabs/Projectile_RollingBoulder");
            }
            else if (boulderPrefab == null)
            {
                boulderPrefab = Resources.Load<GameObject>("Prefabs/Projectile_RollingBoulder");
            }

            GameObject boulderObj = null;
            if (boulderPrefab != null)
            {
                boulderObj = Object.Instantiate(boulderPrefab, startPos + throwDir * 0.8f, Quaternion.LookRotation(throwDir));
            }
            else
            {
                boulderObj = new GameObject("RollingBoulder_Elite");
            }

            var boulder = boulderObj.GetComponent<RollingBoulder>();
            if (boulder == null) boulder = boulderObj.AddComponent<RollingBoulder>();
            float boulderDamage = AttackDamage * 1.5f;
            boulder.Initialize(gameObject, startPos + throwDir * 0.8f, throwDir, boulderDamage, guideBox);

            yield return new WaitForSeconds(0.3f);
            IsActing = false;
        }
    }
}
