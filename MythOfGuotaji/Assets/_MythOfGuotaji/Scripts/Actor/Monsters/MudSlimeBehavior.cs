using MythOfGuotaji.Combat;
using MythOfGuotaji.Core;
using MythOfGuotaji.Data;
using UnityEngine;

namespace MythOfGuotaji.Actor
{
    [MonsterBehavior(MonsterType.Mud_Slime)]
    public class MudSlimeBehavior : MeleeMonsterBehavior
    {
        public override void ApplyPatternStats()
        {
            AttackRange = 9.0f;
            AttackCooldown = 5.0f;
            SpecialAttackRadius = 3.0f;
            TelegraphDuration = 0.5f;
        }

        public override void PerformAttack()
        {
            LastAttackTime = Time.time;
            PlayAttackAnimation();
            SpawnMudPuddle();
        }

        private void SpawnMudPuddle()
        {
            if (Target == null) return;
            var skill = GetMonsterSkillData("mud_puddle");
            GameObject mudPrefab = skill?.GetProjectilePrefab();
            if (mudPrefab == null && Data.GameDataManager.Instance != null && Data.GameDataManager.Instance.Prefabs != null)
            {
                mudPrefab = Data.PrefabDatabaseSO.ResolvePrefab(ref Data.GameDataManager.Instance.Prefabs.mudPuddleZonePrefab, "Prefabs/Skill_MudPuddleZone");
            }
            else if (mudPrefab == null)
            {
                mudPrefab = Resources.Load<GameObject>("Prefabs/Skill_MudPuddleZone");
            }

            GameObject mudGo = mudPrefab != null ? Object.Instantiate(mudPrefab) : new GameObject("MudPuddleZone");
            mudGo.name = "MudPuddleZone";
            var mud = mudGo.GetComponent<MudPuddleZone>();
            if (mud == null) mud = mudGo.AddComponent<MudPuddleZone>();
            mud.Initialize(Target.position, radius: 3.0f, duration: 4.0f);

            if (skill != null && skill.castSoundPrefab != null)
            {
                MonsterTableAssetPlayer.PlayOneShotSound(skill.castSoundPrefab, Target.position);
            }
        }
    }
}
