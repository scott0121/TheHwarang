using MythOfGuotaji.Data;
using MythOfGuotaji.Combat;
using MythOfGuotaji.Core;
using UnityEngine;

namespace MythOfGuotaji.Actor
{
    [MonsterBehavior(MonsterType.Minion_Archer)]
    public class ArcherBehavior : MeleeMonsterBehavior
    {
        public override void ApplyPatternStats()
        {
            AttackRange = 15.0f;
            AttackCooldown = 0.7f;
            KiteDistance = 6.0f;
        }

        public override void PerformAttack()
        {
            if (Target == null) return;
            Vector3 toPlayer = Target.position - transform.position;
            toPlayer.y = 0f;

            LastAttackTime = Time.time;
            ShootVenomProjectile(toPlayer.normalized);
        }

        public override void PlayAttackMotion()
        {
            PlayFirstState(0.05f, "Spit", "SpitAttack");
        }

        private void ShootVenomProjectile(Vector3 dir)
        {
            PlayAttackAnimation();
            var skill = GetMonsterSkillData("toxic_spit");
            Vector3 spawnPos = transform.position + Vector3.up * 1.0f + dir * 1.1f;
            VFXManager.Instance?.SpawnVenomMuzzle(spawnPos, dir, skill?.castVfxPrefab);
            if (skill != null && skill.castSoundPrefab != null)
            {
                MonsterTableAssetPlayer.PlayOneShotSound(skill.castSoundPrefab, spawnPos);
            }

            GameObject venomPrefab = skill?.GetProjectilePrefab();
            if (venomPrefab == null && Data.GameDataManager.Instance != null && Data.GameDataManager.Instance.Prefabs != null)
            {
                venomPrefab = Data.PrefabDatabaseSO.ResolvePrefab(ref Data.GameDataManager.Instance.Prefabs.venomProjectilePrefab, "Prefabs/Projectile_Venom");
            }
            else if (venomPrefab == null)
            {
                venomPrefab = Resources.Load<GameObject>("Prefabs/Projectile_Venom");
            }

            GameObject orb = null;
            if (venomPrefab != null)
            {
                orb = Object.Instantiate(venomPrefab, spawnPos, Quaternion.LookRotation(dir));
            }
            else
            {
                var orbPrefab = VFXManager.Instance?.GetVFXPrefab("HCFX_ElementOrb_04");
                if (orbPrefab != null)
                {
                    orb = Object.Instantiate(orbPrefab, spawnPos, Quaternion.LookRotation(dir));
                    orb.transform.localScale = Vector3.one * 0.75f;
                }
                else
                {
                    orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    orb.transform.position = spawnPos;
                    orb.transform.rotation = Quaternion.LookRotation(dir);
                    orb.transform.localScale = Vector3.one * 0.45f;
                }
            }

            orb.name = "VenomProjectile_ToxicBug";
            orb.tag = "Monster";

            var col = orb.GetComponent<Collider>();
            if (col == null) col = orb.AddComponent<SphereCollider>();
            col.isTrigger = true;

            var rb = orb.GetComponent<Rigidbody>();
            if (rb == null) rb = orb.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            var proj = orb.GetComponent<Projectile>();
            if (proj == null) proj = orb.AddComponent<Projectile>();
            DamageInfo dmg = new DamageInfo(AttackDamage, false, spawnPos, dir, KnockbackPower, ElementType.Poison, gameObject);
            float speed = (skill != null && skill.projectileSpeed > 0f) ? skill.projectileSpeed : 6.0f;
            var parry = (skill != null) ? skill.projectileParryType : ProjectileParryType;
            proj.Initialize(dmg, pierceCount: 0, ~LayerMask.GetMask("Ignore Raycast"), poolKey: "", maxRange: AttackRange, unlimitedPierce: false, speed: speed, parryType: parry);
            if (skill != null)
            {
                proj.ConfigureSkillAssets(skill.impactVfxPrefab, skill.parryVfxPrefab, skill.impactSoundPrefab);
            }
            proj.ApplyTopDepthPriority();
        }
    }
}
