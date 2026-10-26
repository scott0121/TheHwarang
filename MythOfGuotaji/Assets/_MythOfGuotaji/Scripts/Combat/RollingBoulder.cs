using System.Collections;
using MythOfGuotaji.Actor;
using MythOfGuotaji.Cameras;
using MythOfGuotaji.Core;
using UnityEngine;

namespace MythOfGuotaji.Combat
{
    /// <summary>
    /// 정예 요괴(Elite KeyKeeper)가 시전하는 굴러가는 거대 암석 투사체:
    /// - 폭 50cm, 길이 20m 가이드 박스 위를 2초간 등속 운동(10m/s)으로 20미터 굴러감
    /// - 부딪힌 플레이어에게 피해를 입힘
    /// - 단검 패링(IReflectable)으로 쳐내면 1.8배속으로 적을 향해 반사되어 몬스터에게 45 피해 및 3초 기절!
    /// - 20미터 도달 전 충돌 시 큰 소리를 내고 부숴져 사라짐
    /// - 바닥 가이드 박스도 돌덩이의 파괴와 함께 동시에 제거됨
    /// </summary>
    public class RollingBoulder : MonoBehaviour, IReflectable
    {
        private GameObject _caster;
        private Vector3 _moveDir;
        private float _speed = 10f; // 20미터 / 2초 = 10m/s 등속 운동
        private float _maxDistance = 20f;
        private float _damage = 25f;
        private GameObject _guideBox;

        private float _traveledDistance = 0f;
        private float _radius = 0.45f;
        private bool _isDestroyed = false;
        private bool _isReflected = false;
        private LayerMask _collisionMask;
        private float _nextDustTime = 0f;

        private static AudioClip s_rockSmashClip;

        public bool IsFromPlayer => _isReflected;
        public Vector3 Position => transform.position;
        public ProjectileParryType ParryType => ProjectileParryType.Normal;

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

        /// <summary>
        /// 단검 패링으로 거대 암석을 쳐냈을 때 호출: 날아오던 반대 방향(또는 시전자 요괴)으로 역반사!
        /// </summary>
        public void Reflect(GameObject newAttacker, Vector3? customDirection = null, float damageMultiplier = 1.5f, float speedMultiplier = 1.8f)
        {
            if (_isDestroyed) return;

            _isReflected = true;
            _damage *= damageMultiplier;
            _speed *= speedMultiplier;
            _traveledDistance = 0f; // 반사된 시점부터 사거리 재계산

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

            // 반사 시 암석 표면을 신비로운 청록색/황금빛 룬 문양 비주얼로 변경
            ApplyReflectedVisual();

            // 가이드 박스가 남아있다면 반사 방향으로 회전 및 색상 청록색 변경
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

            // 패링 스파크 연출
            VFXManager.Instance?.SpawnDaggerParrySpark(transform.position);
            QuarterViewCamera.Instance?.TriggerShake(0.4f, 0.25f);
            Debug.Log($"<color=yellow><b>[정예 바위 단검 패링 성공!]</b> 굴러오던 암석을 {speedMultiplier:F1}배속으로 쳐내어 적 요괴를 향해 역반사했습니다!</color>");
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

        public void Initialize(GameObject caster, Vector3 startPos, Vector3 direction, float damage, GameObject guideBox)
        {
            _caster = caster;
            _moveDir = direction.normalized;
            _damage = damage;
            _guideBox = guideBox;
            _traveledDistance = 0f;
            _isDestroyed = false;

            transform.position = startPos + Vector3.up * _radius;
            transform.rotation = Quaternion.LookRotation(_moveDir);

            _collisionMask = LayerMask.GetMask("Default") | ~LayerMask.GetMask("Ignore Raycast", "Water", "UI");

            CreateVisual();
        }

        private void CreateVisual()
        {
            if (transform.Find("BoulderModel") != null) return;

            // 구체 메시 렌더러 생성
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "BoulderModel";
            sphere.transform.SetParent(transform, false);
            sphere.transform.localPosition = Vector3.zero;
            sphere.transform.localScale = Vector3.one * (_radius * 2f);

            var col = sphere.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var rend = sphere.GetComponent<Renderer>();
            if (rend != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                mat.color = new Color(0.38f, 0.35f, 0.32f); // 암석 고유의 짙은 암회색
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(0.38f, 0.35f, 0.32f));
                if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.1f);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.15f);
                rend.material = mat;
            }

            // 바위 표면 요철/돌출 장식
            for (int i = 0; i < 4; i++)
            {
                var rockBit = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rockBit.name = "Crag";
                rockBit.transform.SetParent(sphere.transform, false);
                rockBit.transform.localPosition = Random.onUnitSphere * 0.42f;
                rockBit.transform.localRotation = Random.rotation;
                rockBit.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
                var c = rockBit.GetComponent<Collider>();
                if (c != null) Destroy(c);

                var r = rockBit.GetComponent<Renderer>();
                if (r != null)
                {
                    var bitMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                    bitMat.color = new Color(0.25f, 0.22f, 0.2f);
                    if (bitMat.HasProperty("_BaseColor")) bitMat.SetColor("_BaseColor", new Color(0.25f, 0.22f, 0.2f));
                    r.material = bitMat;
                }
            }
        }

        private void Update()
        {
            if (_isDestroyed) return;

            float dt = Time.deltaTime;
            float step = _speed * dt;

            // 1. 전방 충돌 감지 (벽/장애물 및 플레이어)
            Vector3 currentPos = transform.position;
            if (Physics.SphereCast(currentPos, _radius * 0.9f, _moveDir, out RaycastHit hit, step, _collisionMask, QueryTriggerInteraction.Ignore))
            {
                // 자기 자신(시전자)과의 충돌은 무시
                if (_caster != null && (hit.collider.gameObject == _caster || hit.collider.transform.IsChildOf(_caster.transform)))
                {
                    // 시전자 통과
                }
                else
                {
                    // 반사 여부에 따른 충돌 대상 판정
                    if (!_isReflected)
                    {
                        // [적 바위]: 플레이어 충돌 검사
                        var player = hit.collider.GetComponentInParent<PlayerController>();
                        if (player != null && !player.IsDead)
                        {
                            DamageInfo dmg = new DamageInfo(_damage, false, transform.position, _moveDir, 12f, ElementType.Physical, _caster);
                            player.TakeDamage(dmg);
                            Debug.Log($"<color=red><b>[정예 바위 돌덩이 직격!]</b> 플레이어에게 {_damage} 피해 부여!</color>");
                            ShatterAndDestroy(hit.point);
                            return;
                        }
                    }
                    else
                    {
                        // [반사된 아군 바위]: 적 몬스터 충돌 검사
                        var monster = hit.collider.GetComponentInParent<MonsterAI>();
                        if (monster != null && !monster.IsDead)
                        {
                            DamageInfo dmg = new DamageInfo(_damage, true, transform.position, _moveDir, 16f, ElementType.Physical, null, isReflected: true);
                            monster.TakeDamage(dmg);
                            monster.ApplyStun(3.0f); // 3초간 그로기 기절!
                            QuarterViewCamera.Instance?.TriggerShake(0.5f, 0.3f);
                            Debug.Log($"<color=lime><b>[반사된 암석 몬스터 강타!]</b> {monster.name}에게 {_damage:F0} 피해를 입히고 3초간 기절시켰습니다!</color>");
                            ShatterAndDestroy(hit.point);
                            return;
                        }
                    }

                    // 벽 / 장애물 충돌 검사
                    bool isWall = hit.collider.gameObject.name.Contains("Wall") ||
                                  hit.collider.gameObject.name.Contains("Pillar") ||
                                  hit.collider.gameObject.name.Contains("Door") ||
                                  hit.collider.gameObject.name.Contains("Obstacle");

                    if (isWall || (hit.collider.GetComponentInParent<PlayerController>() == null && hit.collider.GetComponentInParent<MonsterAI>() == null))
                    {
                        Debug.Log("<color=orange>[정예 바위 충돌] 벽에 격돌하여 큰 소리를 내며 파괴되었습니다!</color>");
                        ShatterAndDestroy(hit.point);
                        return;
                    }
                }
            }

            // 오버랩 보조 판정
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
                        Debug.Log($"<color=red><b>[정예 바위 돌덩이 오버랩 접촉!]</b> 플레이어에게 {_damage} 피해 부여!</color>");
                        ShatterAndDestroy(currentPos);
                        return;
                    }
                }
                else
                {
                    var monster = col.GetComponentInParent<MonsterAI>();
                    if (monster != null && !monster.IsDead)
                    {
                        DamageInfo dmg = new DamageInfo(_damage, true, transform.position, _moveDir, 16f, ElementType.Physical, null, isReflected: true);
                        monster.TakeDamage(dmg);
                        monster.ApplyStun(3.0f);
                        QuarterViewCamera.Instance?.TriggerShake(0.5f, 0.3f);
                        Debug.Log($"<color=lime><b>[반사된 암석 몬스터 오버랩 강타!]</b> {monster.name}에게 {_damage:F0} 피해를 입히고 3초간 기절시켰습니다!</color>");
                        ShatterAndDestroy(currentPos);
                        return;
                    }
                }
            }

            // 2. 등속 운동 위치 이동
            transform.position += _moveDir * step;
            _traveledDistance += step;

            // 3. 굴러가는 회전 연출 (구체 표면 진행각 반영)
            float rollDeg = (step / _radius) * Mathf.Rad2Deg;
            transform.Rotate(Vector3.Cross(Vector3.up, _moveDir), rollDeg, Space.World);

            // 4. 지면 흙먼지 트레일 연출
            if (Time.time >= _nextDustTime)
            {
                _nextDustTime = Time.time + 0.15f;
                VFXManager.Instance?.SpawnBigMonRunDust(transform.position, _moveDir);
            }

            // 5. 최대 사거리(20m) 도달 시 자연 소멸
            if (_traveledDistance >= _maxDistance)
            {
                ShatterAndDestroy(transform.position);
            }
        }

        private void ShatterAndDestroy(Vector3 impactPoint)
        {
            if (_isDestroyed) return;
            _isDestroyed = true;

            // 1. 큰 파괴 소리 ("쾅/쿵!" 사운드)
            PlayShatterSound(impactPoint);

            // 2. 바위 파편 파쇄 시각 효과
            SpawnRockShatterEffect(impactPoint);

            // 3. 카메라 흔들림 연출
            if (Cameras.QuarterViewCamera.Instance != null)
            {
                Cameras.QuarterViewCamera.Instance.TriggerShake(0.35f, 0.3f);
            }

            // 4. 가이드 박스 동시 제거
            if (_guideBox != null)
            {
                Destroy(_guideBox);
                _guideBox = null;
            }

            Destroy(gameObject);
        }

        private void SpawnRockShatterEffect(Vector3 center)
        {
            // 지면 충격파/먼지
            VFXManager.Instance?.SpawnBigMonGroundImpact(center, 1.5f);

            GameObject shardPrefab = null;
            if (Data.GameDataManager.Instance != null && Data.GameDataManager.Instance.Prefabs != null)
            {
                shardPrefab = Data.PrefabDatabaseSO.ResolvePrefab(ref Data.GameDataManager.Instance.Prefabs.rockShardPrefab, "Prefabs/RockShard");
            }
            else
            {
                shardPrefab = Resources.Load<GameObject>("Prefabs/RockShard");
            }

            // 날아가는 암석 조각 파편 6~8개 생성
            int shardCount = Random.Range(6, 9);
            for (int i = 0; i < shardCount; i++)
            {
                Vector3 shardPos = center + Random.insideUnitSphere * 0.3f + Vector3.up * 0.3f;
                GameObject shard;
                if (shardPrefab != null)
                {
                    shard = Instantiate(shardPrefab, shardPos, Random.rotation);
                    shard.name = "RockShard";
                    shard.transform.localScale = Vector3.one * Random.Range(0.18f, 0.32f);
                }
                else
                {
                    shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    shard.name = "RockShard";
                    shard.transform.position = shardPos;
                    shard.transform.localScale = Vector3.one * Random.Range(0.18f, 0.32f);

                    var c = shard.GetComponent<Collider>();
                    if (c != null) Destroy(c);

                    var r = shard.GetComponent<Renderer>();
                    if (r != null)
                    {
                        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                        mat.color = new Color(0.32f, 0.28f, 0.25f);
                        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(0.32f, 0.28f, 0.25f));
                        r.material = mat;
                    }
                }

                // 물리 넉백 효과를 위한 Rigidbody
                var rb = shard.GetComponent<Rigidbody>();
                if (rb == null) rb = shard.AddComponent<Rigidbody>();
                Vector3 burstDir = (Random.onUnitSphere + Vector3.up * 0.8f).normalized;
                rb.linearVelocity = burstDir * Random.Range(4f, 8f);
                rb.angularVelocity = Random.insideUnitSphere * 15f;

                Destroy(shard, Random.Range(0.6f, 0.9f));
            }
        }

        private void PlayShatterSound(Vector3 pos)
        {
            if (s_rockSmashClip == null)
            {
                int sampleRate = 44100;
                float duration = 0.5f;
                int sampleCount = (int)(sampleRate * duration);
                float[] samples = new float[sampleCount];
                for (int i = 0; i < sampleCount; i++)
                {
                    float t = (float)i / sampleRate;
                    // 저음 타격(thud) + 고주파 파쇄 노이즈 합성
                    float freq = Mathf.Lerp(140f, 35f, t / duration);
                    float sine = Mathf.Sin(2f * Mathf.PI * freq * t);
                    float env = Mathf.Exp(-6.5f * t);
                    float noise = (Random.value * 2f - 1f) * Mathf.Exp(-14.0f * t);
                    samples[i] = Mathf.Clamp((sine * 0.75f + noise * 0.55f) * env, -1f, 1f);
                }
                s_rockSmashClip = AudioClip.Create("Boulder_Smash", sampleCount, 1, sampleRate, false);
                s_rockSmashClip.SetData(samples, 0);
            }

            AudioSource.PlayClipAtPoint(s_rockSmashClip, pos, 1.0f);
        }

        private void OnDestroy()
        {
            if (_guideBox != null)
            {
                Destroy(_guideBox);
                _guideBox = null;
            }
        }
    }
}
