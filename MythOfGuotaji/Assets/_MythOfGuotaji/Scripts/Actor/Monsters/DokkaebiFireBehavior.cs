using MythOfGuotaji.Data;
using MythOfGuotaji.Combat;
using MythOfGuotaji.Core;
using UnityEngine;

namespace MythOfGuotaji.Actor
{
    [MonsterBehavior(MonsterType.Dokkaebi_Fire)]
    public class DokkaebiFireBehavior : MeleeMonsterBehavior
    {
        public override void ApplyPatternStats()
        {
            AttackRange = 14.0f;
            AttackCooldown = 2.5f;
            KiteDistance = 5.0f;
        }

        public override void PerformAttack()
        {
            if (Target == null) return;
            LastAttackTime = Time.time;
            PlayAttackAnimation();
            Vector3 toPlayer = (Target.position - transform.position).normalized;
            toPlayer.y = 0f;
            ShootDokkaebiFireball(toPlayer);
        }

        private void ShootDokkaebiFireball(Vector3 dir)
        {
            var skill = GetMonsterSkillData("dokkaebi_fireball");
            Vector3 spawnPos = transform.position + Vector3.up * 1.0f + dir * 0.8f;

            if (skill != null && skill.castVfxPrefab != null)
            {
                VFXManager.Instance?.Spawn(skill.castVfxPrefab, spawnPos, Quaternion.LookRotation(dir));
            }
            if (skill != null && skill.castSoundPrefab != null)
            {
                MonsterTableAssetPlayer.PlayOneShotSound(skill.castSoundPrefab, spawnPos);
            }

            GameObject fireballPrefab = skill?.GetProjectilePrefab();
            if (fireballPrefab == null && Data.GameDataManager.Instance != null && Data.GameDataManager.Instance.Prefabs != null)
            {
                fireballPrefab = Data.PrefabDatabaseSO.ResolvePrefab(ref Data.GameDataManager.Instance.Prefabs.dokkaebiFireballPrefab, "Prefabs/Projectile_DokkaebiFireball");
            }
            else if (fireballPrefab == null)
            {
                fireballPrefab = Resources.Load<GameObject>("Prefabs/Projectile_DokkaebiFireball");
            }

            GameObject go;
            if (fireballPrefab != null)
            {
                go = Object.Instantiate(fireballPrefab, spawnPos, Quaternion.LookRotation(dir));
                go.name = "DokkaebiFireball";
            }
            else
            {
                go = new GameObject("DokkaebiFireball");
                var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.SetParent(go.transform, false);
                sphere.transform.localScale = Vector3.one * 0.6f;
                var col = sphere.GetComponent<Collider>();
                if (col != null) Object.Destroy(col);
                var mr = sphere.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    mr.material = new Material(Shader.Find("Sprites/Default")) { color = new Color(0.2f, 0.8f, 1.0f, 0.95f) };
                }
            }

            var orb = go.GetComponent<DokkaebiFireball>();
            if (orb == null) orb = go.AddComponent<DokkaebiFireball>();
            orb.Initialize(gameObject, spawnPos, dir, AttackDamage, 7.5f);
            orb.ParryType = Combat.ProjectileParryType.Normal;
        }
    }
}
