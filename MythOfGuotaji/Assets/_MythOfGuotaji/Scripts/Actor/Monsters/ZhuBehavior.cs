using MythOfGuotaji.Data;
using System.Collections;
using MythOfGuotaji.Combat;
using MythOfGuotaji.Core;
using UnityEngine;

namespace MythOfGuotaji.Actor
{
    [MonsterBehavior(MonsterType.Minion_Zhu)]
    public class ZhuBehavior : MeleeMonsterBehavior
    {
        public override void ApplyPatternStats()
        {
            AttackRange = 20.0f;
            AttackCooldown = 3.0f;
            SpecialAttackRadius = 2.5f;
            PostAttackDelay = 0.3f;
        }

        private static readonly string[] _zhuMoveStates = { "Fly", "Run", "WalkForward", "Walk", "WalkForward_RM", "Run_RM", "CrawlForward" };
        public override string[] MoveAnimationStates => _zhuMoveStates;

        public override void PerformAttack()
        {
            if (Target == null) return;
            Vector3 toPlayer = Target.position - transform.position;
            toPlayer.y = 0f;
            float dist = toPlayer.magnitude;

            Vector3 eye = transform.position + Vector3.up * 1.0f;
            Vector3 playerEye = Target.position + Vector3.up * 1.0f;
            Vector3 diff = playerEye - eye;

            if (Physics.Raycast(eye, diff.normalized, out RaycastHit hit, diff.magnitude, ObstacleMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.gameObject != Target.gameObject && hit.collider.GetComponentInParent<PlayerController>() == null)
                {
                    NavigateTowardsTarget(dist);
                    return;
                }
            }

            StartCoroutine(ZhuAttackRoutine());
        }

        public override void PlayAttackMotion()
        {
            PlayFirstState(0.05f, "FlySpitAttack", "JumpBiteAttack");
        }

        private IEnumerator ZhuAttackRoutine()
        {
            IsActing = true;
            LastAttackTime = Time.time;
            PlayAttackAnimation();

            var skill = GetMonsterSkillData("zhu_parabolic");
            Vector3 spawnPos = transform.position + Vector3.up * 1.2f + transform.forward * 0.8f;
            Vector3 targetFloorPos = Target != null ? Target.position : transform.position + transform.forward * 10f;
            targetFloorPos.y = 0.05f;

            if (skill != null && skill.castVfxPrefab != null)
            {
                VFXManager.Instance?.Spawn(skill.castVfxPrefab, spawnPos, Quaternion.LookRotation(transform.forward));
            }
            if (skill != null && skill.castSoundPrefab != null)
            {
                MonsterTableAssetPlayer.PlayOneShotSound(skill.castSoundPrefab, spawnPos);
            }

            GameObject zhuProjPrefab = skill?.GetProjectilePrefab();
            if (zhuProjPrefab == null && Data.GameDataManager.Instance != null && Data.GameDataManager.Instance.Prefabs != null)
            {
                zhuProjPrefab = Data.PrefabDatabaseSO.ResolvePrefab(ref Data.GameDataManager.Instance.Prefabs.zhuParabolicProjectilePrefab, "Prefabs/Projectile_ZhuParabolic");
            }
            else if (zhuProjPrefab == null)
            {
                zhuProjPrefab = Resources.Load<GameObject>("Prefabs/Projectile_ZhuParabolic");
            }

            GameObject zhuProjObj = null;
            if (zhuProjPrefab != null)
            {
                zhuProjObj = Object.Instantiate(zhuProjPrefab, spawnPos, Quaternion.identity);
            }
            else
            {
                var zhuPrefab = VFXManager.Instance?.GetVFXPrefab("HCFX_ElementOrb_01");
                if (zhuPrefab != null)
                {
                    zhuProjObj = Object.Instantiate(zhuPrefab, spawnPos, Quaternion.identity);
                    zhuProjObj.transform.localScale = Vector3.one * 0.85f;
                }
                else
                {
                    zhuProjObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    zhuProjObj.transform.position = spawnPos;
                    zhuProjObj.transform.localScale = Vector3.one * 0.65f;
                }
            }

            zhuProjObj.name = "ZhuParabolicProjectile";
            zhuProjObj.tag = "Monster";

            var col = zhuProjObj.GetComponent<Collider>();
            if (col == null) col = zhuProjObj.AddComponent<SphereCollider>();
            col.isTrigger = true;

            var rb = zhuProjObj.GetComponent<Rigidbody>();
            if (rb == null) rb = zhuProjObj.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            var script = zhuProjObj.GetComponent<ZhuParabolicProjectile>();
            if (script == null) script = zhuProjObj.AddComponent<ZhuParabolicProjectile>();
            float flightDuration = (skill != null && skill.flightDuration > 0f) ? skill.flightDuration : 2.0f;
            float arcHeight = (skill != null && skill.arcHeight > 0f) ? skill.arcHeight : 6.0f;
            var parry = (skill != null) ? skill.projectileParryType : ProjectileParryType;
            script.Initialize(gameObject, spawnPos, targetFloorPos, AttackDamage, flightDuration, arcHeight, SpecialAttackRadius);
            script.ParryType = parry;
            script.ApplyTopDepthPriority();

            yield return new WaitForSeconds(PostAttackDelay);
            IsActing = false;
        }
    }
}
