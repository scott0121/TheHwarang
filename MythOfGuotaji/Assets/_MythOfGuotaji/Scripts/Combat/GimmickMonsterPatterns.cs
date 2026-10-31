using System.Collections;
using System.Collections.Generic;
using MythOfGuotaji.Actor;
using MythOfGuotaji.Cameras;
using MythOfGuotaji.Data;
using UnityEngine;

namespace MythOfGuotaji.Combat
{
    /// <summary>
    /// [도깨비 화염술사] 패링 핑퐁 투사체
    /// - 모든 VFX / Sound는 MonsterSkillTable(dokkaebi_fireball)에 직접 등록된 GameObject만 사용
    /// </summary>
    public class DokkaebiFireball : MonoBehaviour, IReflectable
    {
        private GameObject _owner;
        private Vector3 _direction;
        private float _speed = 5.0f;
        private float _damage = 12.0f;
        private bool _isReflected = false;
        private float _lifetime = 6.0f;
        private MonsterSkillEntry _skill;
        private GameObject _loopSoundObj;

        public bool IsFromPlayer => _isReflected;
        public Vector3 Position => transform.position;
        public ProjectileParryType ParryType { get; set; } = ProjectileParryType.Normal;

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

        private void OnDestroy()
        {
            MonsterTableAssetPlayer.StopLoopSound(ref _loopSoundObj);
        }

        public void Initialize(GameObject owner, Vector3 startPos, Vector3 dir, float damage = 12f, float speed = 5f, MonsterSkillEntry skill = null)
        {
            _owner = owner;
            transform.position = startPos;
            _direction = dir.normalized;
            _damage = damage;
            _speed = speed;
            _isReflected = false;
            _skill = skill ?? (GameDataManager.Instance?.MonsterSkills?.GetSkill("dokkaebi_fireball"));
            if (_skill != null)
            {
                ParryType = _skill.projectileParryType;
            }

            if (_skill != null && _skill.enableLoopSound && _skill.loopSoundPrefab != null)
            {
                _loopSoundObj = MonsterTableAssetPlayer.StartLoopSound(_skill.loopSoundPrefab, transform, startPos);
            }

            Destroy(gameObject, _lifetime);
        }

        public void Reflect(GameObject newAttacker, Vector3? customDirection = null, float damageMultiplier = 1.5f, float speedMultiplier = 1.8f)
        {
            _isReflected = true;
            _damage *= damageMultiplier;
            _speed *= speedMultiplier;

            if (customDirection.HasValue && customDirection.Value.sqrMagnitude > 0.001f)
            {
                _direction = customDirection.Value.normalized;
            }
            else if (_owner != null)
            {
                _direction = (_owner.transform.position - transform.position).normalized;
            }
            else
            {
                _direction = -_direction;
            }

            transform.rotation = Quaternion.LookRotation(_direction);

            if (_skill != null && _skill.parryVfxPrefab != null)
            {
                MonsterTableAssetPlayer.SpawnVfx(_skill.parryVfxPrefab, transform.position, Quaternion.identity);
            }
        }

        private void Update()
        {
            Vector3 move = _direction * (_speed * Time.deltaTime);
            transform.position += move;

            float checkRadius = 0.6f;
            Collider[] hits = Physics.OverlapSphere(transform.position, checkRadius);
            foreach (var col in hits)
            {
                if (!_isReflected)
                {
                    if (col.CompareTag("Player") || col.TryGetComponent<PlayerController>(out _))
                    {
                        var pc = col.GetComponentInParent<PlayerController>();
                        if (pc != null && !pc.IsDead)
                        {
                            pc.TakeDamage(new DamageInfo(_damage, false, transform.position, _direction, 2f, ElementType.Fire, _owner));
                            if (_skill != null)
                            {
                                MonsterTableAssetPlayer.SpawnVfx(_skill.impactVfxPrefab, transform.position, Quaternion.identity);
                                MonsterTableAssetPlayer.PlayOneShotSound(_skill.impactSoundPrefab, transform.position);
                            }
                            Destroy(gameObject);
                            return;
                        }
                    }
                }
                else
                {
                    if (col.TryGetComponent<MonsterAI>(out _) || col.CompareTag("Monster"))
                    {
                        var targetMon = col.GetComponentInParent<MonsterAI>();
                        if (targetMon != null && !targetMon.IsDead)
                        {
                            float critChance1 = DaggerWeapon.Instance != null ? DaggerWeapon.Instance.CritChance : 0.15f;
                            bool isCrit1 = UnityEngine.Random.value < critChance1;
                            targetMon.TakeDamage(new DamageInfo(_damage, isCrit1, transform.position, _direction, 4f, ElementType.Fire, null, isReflected: true));
                            targetMon.ApplyStun(3.0f);
                            QuarterViewCamera.Instance?.TriggerShake(0.5f, 0.25f);
                            if (_skill != null)
                            {
                                MonsterTableAssetPlayer.SpawnVfx(_skill.impactVfxPrefab, targetMon.transform.position, Quaternion.identity, 2.0f);
                                MonsterTableAssetPlayer.PlayOneShotSound(_skill.impactSoundPrefab, targetMon.transform.position);
                            }
                            Destroy(gameObject);
                            return;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// [사방신 토템] 고정 포대 회전 탄환 (패링 가능)
    /// - 모든 VFX / Sound는 MonsterSkillTable(totem_turret_barrage)에 직접 등록된 GameObject만 사용
    /// </summary>
    public class TotemEnergyOrb : MonoBehaviour, IReflectable
    {
        private GameObject _owner;
        private Vector3 _direction;
        private float _speed = 6.0f;
        private float _damage = 8.0f;
        private bool _isReflected = false;
        private MonsterSkillEntry _skill;
        private GameObject _loopSoundObj;

        public bool IsFromPlayer => _isReflected;
        public Vector3 Position => transform.position;
        public ProjectileParryType ParryType { get; set; } = ProjectileParryType.Enhanced;

        private void OnEnable()
        {
            ApplyTopDepthPriority();
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

        private void OnDestroy()
        {
            MonsterTableAssetPlayer.StopLoopSound(ref _loopSoundObj);
        }

        public void ApplyTopDepthPriority()
        {
            var rends = GetComponentsInChildren<Renderer>(true);
            foreach (var r in rends)
            {
                r.sortingOrder = 5000;
                if (r.materials != null)
                {
                    foreach (var mat in r.materials)
                    {
                        if (mat != null && mat.renderQueue < 3500)
                        {
                            mat.renderQueue = 3500;
                        }
                    }
                }
            }
        }

        public void Initialize(GameObject owner, Vector3 startPos, Vector3 dir, float damage = 8f, float speed = 6f, MonsterSkillEntry skill = null)
        {
            _owner = owner;
            transform.position = startPos;
            _direction = dir.normalized;
            _damage = damage;
            _speed = speed;
            _isReflected = false;
            _skill = skill ?? (GameDataManager.Instance?.MonsterSkills?.GetSkill("totem_turret_barrage"));
            if (_skill != null)
            {
                ParryType = _skill.projectileParryType;
            }

            if (_skill != null && _skill.enableLoopSound && _skill.loopSoundPrefab != null)
            {
                _loopSoundObj = MonsterTableAssetPlayer.StartLoopSound(_skill.loopSoundPrefab, transform, startPos);
            }

            Destroy(gameObject, 5.0f);

            ApplyEnhancedGoldenVisual();
            ApplyTopDepthPriority();
        }

        public void ApplyEnhancedGoldenVisual()
        {
            var rends = GetComponentsInChildren<Renderer>(true);
            Color gold = new Color(1.0f, 0.82f, 0.1f, 0.95f);
            Material sharedGoldMat = ZhuParabolicProjectile.GetSharedTelegraphMaterial(gold);
            foreach (var r in rends)
            {
                if (r != null && sharedGoldMat != null)
                {
                    r.sharedMaterial = sharedGoldMat;
                }
            }
        }

        public void Reflect(GameObject newAttacker, Vector3? customDirection = null, float damageMultiplier = 1.5f, float speedMultiplier = 1.8f)
        {
            _isReflected = true;
            _damage *= damageMultiplier;
            _speed *= speedMultiplier;

            if (customDirection.HasValue) _direction = customDirection.Value.normalized;
            else if (_owner != null) _direction = (_owner.transform.position - transform.position).normalized;
            else _direction = -_direction;

            transform.rotation = Quaternion.LookRotation(_direction);
            if (_skill != null && _skill.parryVfxPrefab != null)
            {
                MonsterTableAssetPlayer.SpawnVfx(_skill.parryVfxPrefab, transform.position, Quaternion.identity);
            }
        }

        private void Update()
        {
            transform.position += _direction * (_speed * Time.deltaTime);

            Collider[] hits = Physics.OverlapSphere(transform.position, 0.45f);
            foreach (var col in hits)
            {
                if (!_isReflected)
                {
                    if (col.CompareTag("Player") || col.TryGetComponent<PlayerController>(out _))
                    {
                        var pc = col.GetComponentInParent<PlayerController>();
                        if (pc != null && !pc.IsDead)
                        {
                            pc.TakeDamage(new DamageInfo(_damage, false, transform.position, _direction, 1.5f, ElementType.Lightning, _owner));
                            if (_skill != null)
                            {
                                MonsterTableAssetPlayer.SpawnVfx(_skill.impactVfxPrefab, transform.position, Quaternion.identity);
                                MonsterTableAssetPlayer.PlayOneShotSound(_skill.impactSoundPrefab, transform.position);
                            }
                            Destroy(gameObject);
                            return;
                        }
                    }
                }
                else
                {
                    if (col.TryGetComponent<MonsterAI>(out _) || col.CompareTag("Monster"))
                    {
                        var targetMon = col.GetComponentInParent<MonsterAI>();
                        if (targetMon != null && !targetMon.IsDead)
                        {
                            float critChance2 = DaggerWeapon.Instance != null ? DaggerWeapon.Instance.CritChance : 0.15f;
                            bool isCrit2 = UnityEngine.Random.value < critChance2;
                            targetMon.TakeDamage(new DamageInfo(_damage, isCrit2, transform.position, _direction, 3f, ElementType.Lightning, null, isReflected: true));
                            if (_skill != null)
                            {
                                MonsterTableAssetPlayer.SpawnVfx(_skill.impactVfxPrefab, transform.position, Quaternion.identity);
                                MonsterTableAssetPlayer.PlayOneShotSound(_skill.impactSoundPrefab, transform.position);
                            }
                            Destroy(gameObject);
                            return;
                        }
                    }
                }

                bool isWall = col.gameObject.name.Contains("Wall") ||
                              col.gameObject.name.Contains("Pillar") ||
                              col.gameObject.name.Contains("Door");
                if (isWall)
                {
                    if (_skill != null)
                    {
                        MonsterTableAssetPlayer.SpawnVfx(_skill.impactVfxPrefab, transform.position, Quaternion.identity);
                        MonsterTableAssetPlayer.PlayOneShotSound(_skill.impactSoundPrefab, transform.position);
                    }
                    Destroy(gameObject);
                    return;
                }
            }
        }
    }

    /// <summary>
    /// [요선 거미] 영혼 거미줄 견인 사슬 및 트레일 연결
    /// - 스킬 발동 시 일정 시간동안 거미와 플레이어를 트레일(LineRenderer / TrailVFX)로 실시간 연결
    /// - 모든 VFX / Sound는 MonsterSkillTable(spider_web_tether)에 직접 등록된 GameObject만 사용
    /// </summary>
    public class SpiderWebTether : MonoBehaviour
    {
        private MonsterAI _spider;
        private PlayerController _player;
        private float _pullDuration = 2.5f;
        private float _pullSpeed = 4.0f;
        private LineRenderer _line;
        private MonsterSkillEntry _skill;
        private GameObject _loopSoundObj;
        private GameObject _trailVfxObj;

        public void Attach(MonsterAI spider, PlayerController player, float duration = 2.5f, float speed = 4.0f, MonsterSkillEntry skill = null)
        {
            _spider = spider;
            _player = player;
            _skill = skill ?? (GameDataManager.Instance?.MonsterSkills?.GetSkill("spider_web_tether"));

            if (_skill != null)
            {
                if (_skill.aoeRadius > 0f) duration = _skill.aoeRadius;
                if (_skill.projectileSpeed > 0f) speed = _skill.projectileSpeed;
            }

            _pullDuration = duration;
            _pullSpeed = speed;

            if (_skill != null && _skill.enableLoopSound && _skill.loopSoundPrefab != null)
            {
                _loopSoundObj = MonsterTableAssetPlayer.StartLoopSound(_skill.loopSoundPrefab, true, transform);
            }

            // 라인 렌더러 / 트레일 세팅
            _line = gameObject.GetComponent<LineRenderer>();
            if (_line == null) _line = gameObject.AddComponent<LineRenderer>();
            _line.useWorldSpace = true;
            _line.alignment = LineAlignment.View;
            _line.textureMode = LineTextureMode.Tile;
            _line.positionCount = 2;

            Color tetherCol = _skill != null ? _skill.telegraphColor : new Color(0.85f, 0.25f, 0.85f, 0.85f);
            float width = _skill != null && _skill.dashWidth > 0f ? _skill.dashWidth : 0.12f;
            _line.startWidth = width;
            _line.endWidth = width;
            _line.startColor = tetherCol;
            _line.endColor = new Color(tetherCol.r, tetherCol.g, tetherCol.b, tetherCol.a * 0.7f);

            if (_line.sharedMaterial == null)
            {
                _line.sharedMaterial = ZhuParabolicProjectile.GetSharedTelegraphMaterial(tetherCol);
            }

            // 트레일 이펙트 프리팹이 등록되어 있다면 생성하여 연결선 상에 부착
            if (_skill != null && _skill.trailVfxPrefab != null)
            {
                Vector3 midPoint = (_spider.transform.position + _player.transform.position) * 0.5f + Vector3.up * 0.8f;
                _trailVfxObj = Instantiate(_skill.trailVfxPrefab, midPoint, Quaternion.identity, transform);
            }

            StartCoroutine(PullRoutine());
        }

        private IEnumerator PullRoutine()
        {
            float elapsed = 0f;

            while (elapsed < _pullDuration)
            {
                if (_spider == null || _spider.IsDead || _player == null || _player.IsDead)
                {
                    break;
                }

                Vector3 spiderPos = _spider.transform.position + Vector3.up * 0.7f;
                Vector3 playerPos = _player.transform.position + Vector3.up * 0.8f;

                // 매 프레임 거미와 플레이어 사이를 트레일(라인)로 실시간 연결
                if (_line != null)
                {
                    _line.SetPosition(0, spiderPos);
                    _line.SetPosition(1, playerPos);
                }

                if (_trailVfxObj != null)
                {
                    _trailVfxObj.transform.position = Vector3.Lerp(spiderPos, playerPos, Mathf.PingPong(elapsed * 2.5f, 1f));
                    Vector3 diff = playerPos - spiderPos;
                    if (diff.sqrMagnitude > 0.001f)
                    {
                        _trailVfxObj.transform.rotation = Quaternion.LookRotation(diff.normalized);
                    }
                }

                // 슬라이딩(대시) 또는 무적 발동 시 영혼 사슬 즉시 절단 탈출
                if (_player.IsDashing || _player.IsInvincible)
                {
                    if (_skill != null)
                    {
                        MonsterTableAssetPlayer.SpawnVfx(_skill.impactVfxPrefab, playerPos, Quaternion.identity);
                        MonsterTableAssetPlayer.PlayOneShotSound(_skill.impactSoundPrefab, playerPos);
                    }
                    break;
                }

                // 플레이어를 거미 쪽으로 부드럽게 견인
                Vector3 toSpider = (_spider.transform.position - _player.transform.position);
                toSpider.y = 0f;
                if (toSpider.magnitude > 0.5f)
                {
                    _player.GetComponent<CharacterController>()?.Move(toSpider.normalized * (_pullSpeed * Time.deltaTime));
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            MonsterTableAssetPlayer.StopLoopSound(ref _loopSoundObj);
            if (_trailVfxObj != null) Destroy(_trailVfxObj);
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            MonsterTableAssetPlayer.StopLoopSound(ref _loopSoundObj);
            if (_trailVfxObj != null) Destroy(_trailVfxObj);
        }
    }

    /// <summary>
    /// [진흙 이괴] 늪지 슬로우 장판
    /// - 모든 VFX / Sound는 MonsterSkillTable(mud_puddle)에 직접 등록된 GameObject만 사용
    /// </summary>
    public class MudPuddleZone : MonoBehaviour
    {
        private float _duration = 4.0f;
        private float _slowFactor = 0.4f;
        private float _radius = 3.0f;
        private MonsterSkillEntry _skill;
        private GameObject _loopSoundObj;

        public void Initialize(Vector3 pos, float radius = 3.0f, float duration = 4.0f, MonsterSkillEntry skill = null)
        {
            transform.position = new Vector3(pos.x, 0.06f, pos.z);
            _radius = radius;
            _duration = duration;
            _skill = skill ?? (GameDataManager.Instance?.MonsterSkills?.GetSkill("mud_puddle"));

            if (_skill != null && _skill.enableLoopSound && _skill.loopSoundPrefab != null)
            {
                _loopSoundObj = MonsterTableAssetPlayer.StartLoopSound(_skill.loopSoundPrefab, transform, transform.position);
            }

            var existingVisual = transform.Find("MudVisual");
            if (existingVisual != null)
            {
                GameObject cyl = existingVisual.gameObject;
                cyl.transform.localScale = new Vector3(radius * 2f, 0.01f, radius * 2f);

                var mr = cyl.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    mr.sharedMaterial = ZhuParabolicProjectile.GetSharedTelegraphMaterial(new Color(0.35f, 0.25f, 0.12f, 0.65f));
                    mr.sortingOrder = 40;
                }
            }

            Destroy(gameObject, duration);
        }

        private void OnDestroy()
        {
            MonsterTableAssetPlayer.StopLoopSound(ref _loopSoundObj);
        }

        private void Update()
        {
            var player = PlayerController.Instance != null ? PlayerController.Instance : FindAnyObjectByType<PlayerController>();
            if (player != null && !player.IsDead)
            {
                float dist = Vector3.Distance(new Vector3(transform.position.x, 0f, transform.position.z), new Vector3(player.transform.position.x, 0f, player.transform.position.z));
                if (dist <= _radius)
                {
                    player.ApplyMoveSpeedMultiplier(_slowFactor, 0.2f);
                }
            }
        }
    }
}
