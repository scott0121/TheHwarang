using MythOfGuotaji.Data;
using MythOfGuotaji.Combat;
using MythOfGuotaji.Core;
using UnityEngine;

namespace MythOfGuotaji.Actor
{
    [MonsterBehavior(MonsterType.Totem_Turret)]
    public class TotemTurretBehavior : MeleeMonsterBehavior
    {
        private float _totemRotationAngle = 0f;
        private bool _hasTotemFiredOnce = false;

        public override bool RequiresLineOfSight => false;

        public override void ResetState()
        {
            _hasTotemFiredOnce = false;
        }

        public override void ApplyPatternStats()
        {
            AttackRange = 25.0f;
            AttackCooldown = 1.0f;
            MoveSpeed = 2.4f;
        }

        public override bool TickCustomBehavior(float distToPlayer, Vector3 toPlayer)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toPlayer.normalized), Time.deltaTime * 6f);
            if (distToPlayer <= AttackRange)
            {
                Owner.TryAttackPlayer();
                PlayIdleAnimation();
                return true;
            }
            else if (!_hasTotemFiredOnce)
            {
                NavigateTowardsTarget(distToPlayer);
                PlayMoveAnimation();
                return true;
            }
            else
            {
                Owner.TryAttackPlayer();
                PlayIdleAnimation();
                return true;
            }
        }

        public override void PerformAttack()
        {
            LastAttackTime = Time.time;
            PlayAttackAnimation();
            FireTotemBarrage();
        }

        public override DamageResponse PreDamage(DamageInfo info)
        {
            if (info.isReflected) return DamageResponse.Proceed;

            var stat = Data.GameDataManager.Instance?.MonsterStats?.GetStat(Type);
            if (stat != null && stat.deflectVfxPrefab != null)
            {
                VFXManager.Instance?.Spawn(stat.deflectVfxPrefab, transform.position + Vector3.up * 1f, Quaternion.identity);
            }
            else
            {
                VFXManager.Instance?.Spawn("HCFX_Sparks_01", transform.position + Vector3.up * 1f, Quaternion.identity);
            }
            Cameras.QuarterViewCamera.Instance?.TriggerShake(0.12f, 0.08f);
            return DamageResponse.Absorbed;
        }

        private void FireTotemBarrage()
        {
            _hasTotemFiredOnce = true;
            MoveSpeed = 0f;
            _totemRotationAngle += 45f;

            var skill = GetMonsterSkillData("totem_turret_barrage");
            GameObject totemOrbPrefab = skill?.GetProjectilePrefab();
            if (totemOrbPrefab == null && Data.GameDataManager.Instance != null && Data.GameDataManager.Instance.Prefabs != null)
            {
                totemOrbPrefab = Data.PrefabDatabaseSO.ResolvePrefab(ref Data.GameDataManager.Instance.Prefabs.totemEnergyOrbPrefab, "Prefabs/Projectile_TotemOrb");
            }
            else if (totemOrbPrefab == null)
            {
                totemOrbPrefab = Resources.Load<GameObject>("Prefabs/Projectile_TotemOrb");
            }

            for (int i = 0; i < 4; i++)
            {
                float angle = _totemRotationAngle + (i * 90f);
                Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;

                GameObject go;
                if (totemOrbPrefab != null)
                {
                    go = Object.Instantiate(totemOrbPrefab, transform.position + Vector3.up * 1f, Quaternion.LookRotation(dir));
                    go.name = "TotemEnergyOrb";

                    var oldProj = go.GetComponent<Projectile>();
                    if (oldProj != null) Object.DestroyImmediate(oldProj);
                }
                else
                {
                    go = new GameObject("TotemEnergyOrb");
                    var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    sphere.transform.SetParent(go.transform, false);
                    sphere.transform.localScale = Vector3.one * 0.5f;
                    var col = sphere.GetComponent<Collider>();
                    if (col != null) Object.Destroy(col);
                    var mr = sphere.GetComponent<MeshRenderer>();
                    if (mr != null)
                    {
                        Color goldenColor = new Color(1.0f, 0.82f, 0.1f, 0.95f);
                        mr.material = new Material(Shader.Find("Sprites/Default")) { color = goldenColor };
                    }
                }

                var orb = go.GetComponent<TotemEnergyOrb>();
                if (orb == null) orb = go.AddComponent<TotemEnergyOrb>();
                orb.Initialize(gameObject, transform.position + Vector3.up * 1f, dir, AttackDamage, 6.0f);
                orb.ParryType = Combat.ProjectileParryType.Enhanced;
            }

            if (skill != null && skill.castVfxPrefab != null)
            {
                VFXManager.Instance?.Spawn(skill.castVfxPrefab, transform.position, Quaternion.identity);
            }
            else
            {
                VFXManager.Instance?.Spawn("HCFX_Flash_01", transform.position, Quaternion.identity);
            }
            if (skill != null && skill.castSoundPrefab != null)
            {
                MonsterTableAssetPlayer.PlayOneShotSound(skill.castSoundPrefab, transform.position);
            }
        }
    }
}
