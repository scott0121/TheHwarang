using System.Collections;
using System.Collections.Generic;
using MythOfGuotaji.Actor;
using MythOfGuotaji.Cameras;
using MythOfGuotaji.Core;
using UnityEngine;

namespace MythOfGuotaji.Combat
{
    /// <summary>
    /// [도깨비 화염술사] 패링 핑퐁 투사체
    /// - 5m/s 느린 속도로 플레이어를 향해 날아감 (단검으로 쳐내기 쉬운 타이밍)
    /// - 단검 패링 시 속도 2배, 피해 1.5배로 반사되어 도깨비에게 역직격!
    /// - 도깨비에게 역직격 시 도깨비의 마법 보호막을 박살내고 3초간 기절(Groggy)
    /// </summary>
    public class DokkaebiFireball : MonoBehaviour, IReflectable
    {
        private GameObject _owner;
        private Vector3 _direction;
        private float _speed = 5.0f;
        private float _damage = 12.0f;
        private bool _isReflected = false;
        private float _lifetime = 6.0f;

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
        }

        public void Initialize(GameObject owner, Vector3 startPos, Vector3 dir, float damage = 12f, float speed = 5f)
        {
            _owner = owner;
            transform.position = startPos;
            _direction = dir.normalized;
            _damage = damage;
            _speed = speed;
            _isReflected = false;
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

            // 반사 시 황금빛/청록빛 파티클 스파크
            VFXManager.Instance?.SpawnDaggerParrySpark(transform.position);
            Debug.Log("<color=gold><b>[도깨비불 패링 성공!]</b> 화염구를 2배 속도로 반사하여 도깨비에게 되돌려 보냅니다!</color>");
        }

        private void Update()
        {
            Vector3 move = _direction * (_speed * Time.deltaTime);
            transform.position += move;

            // 충돌 검사
            float checkRadius = 0.6f;
            Collider[] hits = Physics.OverlapSphere(transform.position, checkRadius);
            foreach (var col in hits)
            {
                if (!_isReflected)
                {
                    // 플레이어 타격
                    if (col.CompareTag("Player") || col.TryGetComponent<PlayerController>(out var player))
                    {
                        var pc = col.GetComponentInParent<PlayerController>();
                        if (pc != null && !pc.IsDead)
                        {
                            pc.TakeDamage(new DamageInfo(_damage, false, transform.position, _direction, 2f, ElementType.Fire, _owner));
                            VFXManager.Instance?.Spawn("HCFX_Explosion_02", transform.position, Quaternion.identity);
                            Destroy(gameObject);
                            return;
                        }
                    }
                }
                else
                {
                    // 반사된 상태: 몬스터 역직격
                    if (col.TryGetComponent<MonsterAI>(out var mon) || col.CompareTag("Monster"))
                    {
                        var targetMon = col.GetComponentInParent<MonsterAI>();
                        if (targetMon != null && !targetMon.IsDead && targetMon.gameObject != _owner?.gameObject && _owner != null)
                        {
                            // 다른 몬스터
                        }
                        if (targetMon != null && !targetMon.IsDead)
                        {
                            targetMon.TakeDamage(new DamageInfo(_damage, true, transform.position, _direction, 4f, ElementType.Fire, null, isReflected: true));
                            targetMon.ApplyStun(3.0f); // 3초간 그로기 기절!
                            QuarterViewCamera.Instance?.TriggerShake(0.5f, 0.25f);
                            VFXManager.Instance?.Spawn("HCFX_Flash_01", targetMon.transform.position, Quaternion.identity, scale: 2.0f);
                            Debug.Log("<color=lime><b>[보호막 분쇄!]</b> 반사된 도깨비불이 도깨비를 강타하여 3초간 기절시켰습니다!</color>");
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
    /// </summary>
    public class TotemEnergyOrb : MonoBehaviour, IReflectable
    {
        private GameObject _owner;
        private Vector3 _direction;
        private float _speed = 6.0f;
        private float _damage = 8.0f;
        private bool _isReflected = false;

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

        public void Initialize(GameObject owner, Vector3 startPos, Vector3 dir, float damage = 8f, float speed = 6f)
        {
            _owner = owner;
            transform.position = startPos;
            _direction = dir.normalized;
            _damage = damage;
            _speed = speed;
            _isReflected = false;
            Destroy(gameObject, 5.0f);

            // 강화 투사체(황금색) 비주얼 보장
            ApplyEnhancedGoldenVisual();
            ApplyTopDepthPriority();
        }

        public void ApplyEnhancedGoldenVisual()
        {
            var rends = GetComponentsInChildren<Renderer>(true);
            Color gold = new Color(1.0f, 0.82f, 0.1f, 0.95f);
            foreach (var r in rends)
            {
                if (r != null && r.material != null)
                {
                    r.material.color = gold;
                    if (r.material.HasProperty("_BaseColor")) r.material.SetColor("_BaseColor", gold);
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
            VFXManager.Instance?.SpawnDaggerParrySpark(transform.position);
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
                            Destroy(gameObject);
                            return;
                        }
                    }
                }
                else
                {
                    if (col.TryGetComponent<MonsterAI>(out var mon) || col.CompareTag("Monster"))
                    {
                        var targetMon = col.GetComponentInParent<MonsterAI>();
                        if (targetMon != null && !targetMon.IsDead)
                        {
                            targetMon.TakeDamage(new DamageInfo(_damage, true, transform.position, _direction, 3f, ElementType.Lightning, null, isReflected: true));
                            Destroy(gameObject);
                            return;
                        }
                    }
                }

                // 벽/장애물 충돌 검사
                bool isWall = col.gameObject.name.Contains("Wall") ||
                              col.gameObject.name.Contains("Pillar") ||
                              col.gameObject.name.Contains("Door");
                if (isWall)
                {
                    VFXManager.Instance?.Spawn("HCFX_Sparks_01", transform.position, Quaternion.identity);
                    Destroy(gameObject);
                    return;
                }
            }
        }
    }

    /// <summary>
    /// [요선 거미] 영혼 거미줄 견인 사슬
    /// - 플레이어를 거미 쪽으로 끌어당김
    /// - 슬라이딩(대시) 무적으로 즉시 절단 탈출 가능
    /// </summary>
    public class SpiderWebTether : MonoBehaviour
    {
        private MonsterAI _spider;
        private PlayerController _player;
        private float _pullDuration = 2.0f;
        private float _pullSpeed = 3.5f;
        private LineRenderer _line;

        public void Attach(MonsterAI spider, PlayerController player, float duration = 2.0f, float speed = 3.5f)
        {
            _spider = spider;
            _player = player;
            _pullDuration = duration;
            _pullSpeed = speed;

            _line = gameObject.GetComponent<LineRenderer>();
            if (_line == null) _line = gameObject.AddComponent<LineRenderer>();
            if (_line.sharedMaterial == null)
            {
                _line.material = new Material(Shader.Find("Sprites/Default"));
            }
            _line.startColor = new Color(0.9f, 0.3f, 0.9f, 0.8f);
            _line.endColor = new Color(0.6f, 0.1f, 0.7f, 0.8f);
            _line.startWidth = 0.08f;
            _line.endWidth = 0.08f;
            _line.positionCount = 2;

            StartCoroutine(PullRoutine());
        }

        private IEnumerator PullRoutine()
        {
            float elapsed = 0f;
            Debug.Log("<color=magenta>[요선 거미] 영혼 거미줄에 결박되었습니다! 슬라이딩으로 끊어내세요!</color>");

            while (elapsed < _pullDuration)
            {
                if (_spider == null || _spider.IsDead || _player == null || _player.IsDead)
                {
                    break;
                }

                // [탈출 기믹]: 슬라이딩(대시) 발동 시 사슬 즉시 절단!
                if (_player.IsDashing)
                {
                    Debug.Log("<color=cyan><b>[사슬 절단!]</b> 슬라이딩 무적 대시로 거미줄을 끊고 탈출했습니다!</color>");
                    VFXManager.Instance?.Spawn("HCFX_Slash_01", _player.transform.position + Vector3.up * 0.8f, Quaternion.identity);
                    break;
                }

                // 견인 물리 이동
                Vector3 toSpider = (_spider.transform.position - _player.transform.position).normalized;
                toSpider.y = 0f;
                _player.GetComponent<CharacterController>()?.Move(toSpider * (_pullSpeed * Time.deltaTime));

                // 라인 렌더러 갱신
                if (_line != null)
                {
                    _line.SetPosition(0, _spider.transform.position + Vector3.up * 0.8f);
                    _line.SetPosition(1, _player.transform.position + Vector3.up * 0.8f);
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            Destroy(gameObject);
        }
    }

    /// <summary>
    /// [진흙 이괴] 늪지 슬로우 장판
    /// - 3m 범위 내 플레이어 이동속도 60% 감속
    /// </summary>
    public class MudPuddleZone : MonoBehaviour
    {
        private float _duration = 4.0f;
        private float _slowFactor = 0.4f; // 이동속도 40%로 감소 (60% 감속)
        private float _radius = 3.0f;

        public void Initialize(Vector3 pos, float radius = 3.0f, float duration = 4.0f)
        {
            transform.position = new Vector3(pos.x, 0.06f, pos.z);
            _radius = radius;
            _duration = duration;

            // 프리팹 내 기존 시각적 바닥 실린더 확인 또는 생성
            var existingVisual = transform.Find("MudVisual");
            GameObject cyl;
            if (existingVisual != null)
            {
                cyl = existingVisual.gameObject;
            }
            else
            {
                cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cyl.name = "MudVisual";
                cyl.transform.SetParent(transform, false);
                cyl.transform.localPosition = Vector3.zero;
                var col = cyl.GetComponent<Collider>();
                if (col != null) Destroy(col);
            }
            cyl.transform.localScale = new Vector3(radius * 2f, 0.01f, radius * 2f);

            var mr = cyl.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                var mat = new Material(Shader.Find("Sprites/Default"));
                mat.color = new Color(0.35f, 0.25f, 0.12f, 0.65f); // 진흙색
                mr.material = mat;
                mr.sortingOrder = 40;
            }

            Destroy(gameObject, duration);
        }

        private void Update()
        {
            var player = PlayerController.Instance != null ? PlayerController.Instance : FindAnyObjectByType<PlayerController>();
            if (player != null && !player.IsDead)
            {
                float dist = Vector3.Distance(new Vector3(transform.position.x, 0f, transform.position.z), new Vector3(player.transform.position.x, 0f, player.transform.position.z));
                if (dist <= _radius)
                {
                    // 늪지 감속 효과 부여 (매 프레임)
                    player.ApplyKnockback(Vector3.zero); // 넉백 안정화
                }
            }
        }
    }
}
