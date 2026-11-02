using MythOfGuotaji.Data;
using MythOfGuotaji.Combat;
using MythOfGuotaji.Core;
using UnityEngine;

namespace MythOfGuotaji.Actor
{
    [MonsterBehavior(MonsterType.Phantom_Fox)]
    public class PhantomFoxBehavior : MeleeMonsterBehavior
    {
        private bool _hasSplitPhantoms = false;
        private bool _isPhantomClone = false;

        public override void ResetState()
        {
            _hasSplitPhantoms = false;
            _isPhantomClone = false;
        }

        public override void ApplyPatternStats()
        {
            AttackRange = 6.0f;
            AttackCooldown = 2.0f;
        }

        public override DamageResponse PreDamage(DamageInfo info)
        {
            if (!_hasSplitPhantoms && !_isPhantomClone)
            {
                _hasSplitPhantoms = true;
                SpawnFoxPhantoms();
            }
            return DamageResponse.Proceed;
        }

        public void MarkAsClone()
        {
            _isPhantomClone = true;
            _hasSplitPhantoms = true;
            CurrentHp = 1f;
            MaxHp = 1f;
            AttackDamage = 3f;
            SetRendererColor(new Color(1f, 1f, 1f, 0.5f));
        }

        private void SpawnFoxPhantoms()
        {
            var skill = GetMonsterSkillData("phantom_decoy");
            if (skill != null && skill.castVfxPrefab != null)
            {
                VFXManager.Instance?.Spawn(skill.castVfxPrefab, transform.position, Quaternion.identity);
            }
            else
            {
                VFXManager.Instance?.Spawn("HCFX_Smoke_01", transform.position, Quaternion.identity);
            }
            if (skill != null && skill.castSoundPrefab != null)
            {
                MonsterTableAssetPlayer.PlayOneShotSound(skill.castSoundPrefab, transform.position);
            }

            for (int i = 0; i < 2; i++)
            {
                float angle = (i == 0 ? -45f : 45f);
                Vector3 offset = Quaternion.Euler(0f, angle, 0f) * (transform.forward * 2.0f);
                Vector3 spawnPos = transform.position + offset;

                GameObject cloneGo = Object.Instantiate(gameObject, spawnPos, transform.rotation);
                cloneGo.name = $"{name}_Phantom_{i + 1}";
                var cloneAi = cloneGo.GetComponent<MonsterAI>();
                if (cloneAi != null && cloneAi.Behavior is PhantomFoxBehavior cloneBehavior)
                {
                    cloneBehavior.MarkAsClone();
                }
            }
        }
    }
}
