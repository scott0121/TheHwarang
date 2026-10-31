using System.Collections;
using MythOfGuotaji.Actor;
using MythOfGuotaji.Cameras;
using MythOfGuotaji.Data;
using UnityEngine;

namespace MythOfGuotaji.Combat
{
    /// <summary>
    /// 정예 요괴(Elite KeyKeeper)가 시전하는 굴러가는 거대 암석 투사체
    /// - 모든 VFX / 파편 프리팹 / 1회성 및 루프 Sound는 MonsterSkillTable(elite_boulder)에 직접 등록된 GameObject만 사용
    /// </summary>
    public class RollingBoulder : MonoBehaviour, IReflectable
    {
        private GameObject _caster;
        private Vector3 _moveDir;
        private float _speed = 10f;
        private float _maxDistance = 20f;
        private float _damage = 25f;
        private GameObject _guideBox;
        private MonsterSkillEntry _skill;
        private GameObject _loopSoundObj;

        private float _traveledDistance = 0f;
        private float _radius = 0.45f;
        private bool _isDestroyed = false;
        private bool _isReflected = false;
        private LayerMask _collisionMask;
        private float _nextDustTime = 0f;

        public bool IsFromPlayer => _isReflected;
        public Vector3 Position => transform.position;
        public ProjectileParryType ParryType => _skill != null ? _skill.projectileParryType : ProjectileParryType.Normal;

        private void OnEnable()
        {
            if (!Projectile.ActiveReflectables.Contains(this))
            {
                Projectile.ActiveReflectables.Add(this);
            }
        }

        private void OnDisable()
        {
            Projectile.ActiveReflectables.Remove(this);
            MonsterTableAssetPlayer.StopLoopSound(ref _loopSoundObj);
        }

        public void Reflect(GameObject newAttacker, Vector3? customDirection = null, float damageMultiplier = 1.5f, float speedMultiplier = 1.8f)
        {
            if (_isDestroyed) return;

            _isReflected = true;
            _damage *= damageMultiplier;
            _speed *= speedMultiplier;
            _traveledDistance = 0f;

            if (customDirection.HasValue && customDirection.Value.sqrMagnitude > 0.001f)
            {
                _moveDir = customDirection.Value.normalized;
            }
            else if (_caster != null && !_caster.GetComponent<IDamageable>().IsDead)
            {
                _moveDir = (_caster.transform.position - transform.position).normalized;
                _moveDir.y = 0f;
            }
            else
            {
                _moveDir = -_moveDir;
            }

            if (_moveDir.sqrMagnitude < 0.001f) _moveDir = transform.forward;
            transform.rotation = Quaternion.LookRotation(_moveDir);

            ApplyReflectedVisual();

            if (_guideBox != null)
            {
                _guideBox.transform.rotation = Quaternion.LookRotation(_moveDir);
                var rends = _guideBox.GetComponentsInChildren<Renderer>();
                Color friendlyTelegraph = new Color(0.2f, 0.95f, 1f, 0.45f);
                foreach (var r in rends)
                {
                    if (r != null && r.material != null)
                    {
                        r.material.color = friendlyTelegraph;
                        if (r.material.HasProperty("_BaseColor")) r.material.SetColor("_BaseColor", friendlyTelegraph);
                    }
                }
            }

            if (_skill != null && _skill.parryVfxPrefab != null)
            {
                MonsterTableAssetPlayer.SpawnVfx(_skill.parryVfxPrefab, transform.position, Quaternion.identity);
            }
            QuarterViewCamera.Instance?.TriggerShake(0.4f, 0.25f);
        }

        private void ApplyReflectedVisual()
        {
            var rends = GetComponentsInChildren<Renderer>(true);
            Color reflectedCol = new Color(0.2f, 0.95f, 1f);
            foreach (var r in rends)
            {
                if (r != null && r.material != null)
                {
                    r.material.color = reflectedCol;
                    if (r.material.HasProperty("_BaseColor")) r.material.SetColor("_BaseColor", reflectedCol);
                    if (r.material.HasProperty("_EmissionColor"))
                    {
                        r.material.EnableKeyword("_EMISSION");
                        r.material.SetColor("_EmissionColor", reflectedCol * 1.5f);
                    }
                }
            }
        }

        public void Initialize(GameObject caster, Vector3 startPos, Vector3 direction, float damage, GameObject guideBox = null, float speed = -1f, float maxDistance = -1f, MonsterSkillEntry skill = null)
        {
            _caster = caster;
            _moveDir = direction.normalized;
            _damage = damage;
            _guideBox = guideBox;
            if (speed > 0f) _speed = speed;
            if (maxDistance > 0f) _maxDistance = maxDistance;
            _traveledDistance = 0f;
            _isDestroyed = false;
            _skill = skill ?? (GameDataManager.Instance?.MonsterSkills?.GetSkill("elite_boulder"));

            transform.position = startPos + Vector3.up * _radius;
            transform.rotation = Quaternion.LookRotation(_moveDir);

            _collisionMask = LayerMask.GetMask("Default") | ~LayerMask.GetMask("Ignore Raycast", "Water", "UI");

            if (_skill != null && _skill.enableLoopSound && _skill.loopSoundPrefab != null)
            {
                _loopSoundObj = MonsterTableAssetPlayer.StartLoopSound(_skill.loopSoundPrefab, transform, transform.position);
            }
        }

        public void Initialize(GameObject caster, Vector3 startPos, Vector3 direction, float damage, float speed, float maxDistance, GameObject guideBox = null, MonsterSkillEntry skill = null)
        {
            Initialize(caster, startPos, direction, damage, guideBox, speed, maxDistance, skill);
        }

        private void Update()
        {
            if (_isDestroyed) return;

            float dt = Time.deltaTime;
            float step = _speed * dt;

            Vector3 currentPos = transform.position;
            if (Physics.SphereCast(currentPos, _radius * 0.9f, _moveDir, out RaycastHit hit, step, _collisionMask, QueryTriggerInteraction.Ignore))
            {
                if (_caster != null && (hit.collider.gameObject == _caster || hit.collider.transform.IsChildOf(_caster.transform)))
                {
                    // 시전자 통과
                }
                else
                {
                    if (!_isReflected)
                    {
                        // 적 바위는 다른 몬스터를 통과하고 플레이어/벽에만 충돌
                        if (hit.collider.CompareTag("Monster") || hit.collider.GetComponentInParent<MonsterAI>() != null)
                        {
                            // 다른 몬스터 통과
                        }
                        else
                        {
                            var player = hit.collider.GetComponentInParent<PlayerController>();
                            if (player != null && !player.IsDead)
                            {
                                DamageInfo dmg = new DamageInfo(_damage, false, transform.position, _moveDir, 12f, ElementType.Physical, _caster);
                                player.TakeDamage(dmg);
                                ShatterAndDestroy(hit.point);
                                return;
                            }

                            ShatterAndDestroy(hit.point);
                            return;
                        }
                    }
                    else
                    {
                        var monster = hit.collider.GetComponentInParent<MonsterAI>();
                        if (monster != null && !monster.IsDead)
                        {
                            float critChanceB = DaggerWeapon.Instance != null ? DaggerWeapon.Instance.CritChance : 0.15f;
                            bool isCritB = UnityEngine.Random.value < critChanceB;
                            DamageInfo dmg = new DamageInfo(_damage, isCritB, transform.position, _moveDir, 16f, ElementType.Physical, null, isReflected: true);
                            monster.TakeDamage(dmg);
                            monster.ApplyStun(3.0f);
                            QuarterViewCamera.Instance?.TriggerShake(0.5f, 0.3f);
                            ShatterAndDestroy(hit.point);
                            return;
                        }

                        if (hit.collider.GetComponentInParent<PlayerController>() == null)
                        {
                            ShatterAndDestroy(hit.point);
                            return;
                        }
                    }
                }
            }

            Collider[] overlaps = Physics.OverlapSphere(currentPos, _radius + 0.25f, _collisionMask, QueryTriggerInteraction.Ignore);
            foreach (var col in overlaps)
            {
                if (!_isReflected)
                {
                    if (_caster != null && (col.gameObject == _caster || col.transform.IsChildOf(_caster.transform))) continue;
                    var player = col.GetComponentInParent<PlayerController>();
                    if (player != null && !player.IsDead)
                    {
                        DamageInfo dmg = new DamageInfo(_damage, false, transform.position, _moveDir, 12f, ElementType.Physical, _caster);
                        player.TakeDamage(dmg);
                        ShatterAndDestroy(currentPos);
                        return;
                    }
                }
                else
                {
                    var monster = col.GetComponentInParent<MonsterAI>();
                    if (monster != null && !monster.IsDead)
                    {
                        float critChanceB = DaggerWeapon.Instance != null ? DaggerWeapon.Instance.CritChance : 0.15f;
                            bool isCritB = UnityEngine.Random.value < critChanceB;
                            DamageInfo dmg = new DamageInfo(_damage, isCritB, transform.position, _moveDir, 16f, ElementType.Physical, null, isReflected: true);
                        monster.TakeDamage(dmg);
                        monster.ApplyStun(3.0f);
                        QuarterViewCamera.Instance?.TriggerShake(0.5f, 0.3f);
                        ShatterAndDestroy(currentPos);
                        return;
                    }
                }
            }

            transform.position += _moveDir * step;
            _traveledDistance += step;

            float rollDeg = (step / _radius) * Mathf.Rad2Deg;
            transform.Rotate(Vector3.Cross(Vector3.up, _moveDir), rollDeg, Space.World);

            if (Time.time >= _nextDustTime)
            {
                _nextDustTime = Time.time + 0.15f;
                if (_skill != null && _skill.trailVfxPrefab != null)
                {
                    MonsterTableAssetPlayer.SpawnVfx(_skill.trailVfxPrefab, transform.position, Quaternion.LookRotation(-_moveDir));
                }
            }

            if (_traveledDistance >= _maxDistance)
            {
                ShatterAndDestroy(transform.position);
            }
        }

        private void ShatterAndDestroy(Vector3 impactPoint)
        {
            if (_isDestroyed) return;
            _isDestroyed = true;

            MonsterTableAssetPlayer.StopLoopSound(ref _loopSoundObj);

            if (_skill != null)
            {
                MonsterTableAssetPlayer.PlayOneShotSound(_skill.impactSoundPrefab, impactPoint);
                MonsterTableAssetPlayer.SpawnVfx(_skill.impactVfxPrefab, impactPoint, Quaternion.identity);
            }

            SpawnRockShatterEffect(impactPoint);

            if (Cameras.QuarterViewCamera.Instance != null)
            {
                Cameras.QuarterViewCamera.Instance.TriggerShake(0.35f, 0.3f);
            }

            if (_guideBox != null)
            {
                Destroy(_guideBox);
                _guideBox = null;
            }

            Destroy(gameObject);
        }

        private void SpawnRockShatterEffect(Vector3 center)
        {
            GameObject shardPrefab = _skill?.subProjectilePrefab;
            if (shardPrefab == null) return;

            int shardCount = Random.Range(6, 9);
            for (int i = 0; i < shardCount; i++)
            {
                Vector3 shardPos = center + Random.insideUnitSphere * 0.3f + Vector3.up * 0.3f;
                GameObject shard = Instantiate(shardPrefab, shardPos, Random.rotation);
                shard.name = "RockShard";
                shard.transform.localScale = Vector3.one * Random.Range(0.18f, 0.32f);

                var rb = shard.GetComponent<Rigidbody>();
                if (rb == null) rb = shard.AddComponent<Rigidbody>();
                Vector3 burstDir = (Random.onUnitSphere + Vector3.up * 0.8f).normalized;
                rb.linearVelocity = burstDir * Random.Range(4f, 8f);
                rb.angularVelocity = Random.insideUnitSphere * 15f;

                Destroy(shard, Random.Range(0.6f, 0.9f));
            }
        }

        private void OnDestroy()
        {
            MonsterTableAssetPlayer.StopLoopSound(ref _loopSoundObj);
            if (_guideBox != null)
            {
                Destroy(_guideBox);
                _guideBox = null;
            }
        }
    }
}
