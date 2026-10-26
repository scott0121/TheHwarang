using System.Collections;
using MythOfGuotaji.Cameras;
using UnityEngine;

namespace MythOfGuotaji.Combat
{
    /// <summary>
    /// 거타지의 보조무기: 단검 (Dagger)
    /// - 위기 탈출용 근접 부채꼴 베기 (Slash)
    /// - 절차적 검기 초승달(Crescent Slash) 시각 효과 생성
    /// - 높은 넉백 및 방어력 약화 효과
    /// </summary>
    public class DaggerWeapon : MonoBehaviour
    {
        [Header("Dagger Config")]
        [SerializeField] private float _baseDamage = 50f;
        [SerializeField] private float _attackRadius = 3.0f; // 근접 베기 반경 (3.0m)
        [SerializeField] private float _attackAngle = 150f;  // 부채꼴 공격 각도 (150도)
        [SerializeField] private float _cooldown = 0.5f;     // 글로벌 쿨다운과 일치 (0.5초)
        [SerializeField] private float _knockback = 9f;      // 넉백 파워
        [SerializeField] private bool _canReflectProjectiles = true;
        [SerializeField] private float _reflectDamageMultiplier = 1.5f;
        [SerializeField] private float _reflectSpeedMultiplier = 1.8f;
        [SerializeField] private LayerMask _enemyLayer;

        private float _lastAttackTime = -99f;
        private Coroutine _parryWindowCoroutine;
        private const float ParryWindowDuration = 0.22f; // 단검 휘두르기 모션 중 패링 판정 지속 시간 (약 0.22초)

        public bool CanSlash => Time.time >= _lastAttackTime + _cooldown;
        public float CooldownRemaining => Mathf.Max(0f, (_lastAttackTime + _cooldown) - Time.time);

        private void Awake()
        {
            if (_enemyLayer.value == 0)
            {
                _enemyLayer = ~LayerMask.GetMask("Ignore Raycast");
            }

            ApplyStatsFromTable();
        }

        public void ApplyStatsFromTable()
        {
            if (Data.GameDataManager.Instance != null && Data.GameDataManager.Instance.WeaponStats != null)
            {
                var daggerStats = Data.GameDataManager.Instance.WeaponStats.dagger;
                _baseDamage = daggerStats.baseDamage;
                _attackRadius = daggerStats.attackRadius;
                _attackAngle = daggerStats.attackAngle;
                _cooldown = daggerStats.cooldown;
                _knockback = daggerStats.knockback;
                _canReflectProjectiles = daggerStats.canReflectProjectiles;
                _reflectDamageMultiplier = daggerStats.reflectDamageMultiplier;
                _reflectSpeedMultiplier = daggerStats.reflectSpeedMultiplier;
            }
        }

        public bool TrySlash(Vector3 aimDirection, GameObject owner)
        {
            if (!CanSlash) return false;
            _lastAttackTime = Time.time;

            aimDirection.y = 0f;
            aimDirection.Normalize();

            Vector3 center = transform.position;
            center.y = 1.0f; // 몬스터 중심 높이

            // 1. 시각적 초승달 검기(Crescent Slash Wave) 및 VFX 참격 플래시 생성
            CreateSlashVisualEffect(center, aimDirection);
            Core.VFXManager.Instance?.SpawnDaggerSlash(center, aimDirection, _attackRadius);

            // 2. 가벼운 타격감 카메라 셰이크
            if (QuarterViewCamera.Instance != null)
            {
                QuarterViewCamera.Instance.TriggerShake(0.2f, 0.12f);
            }

            Debug.Log("<color=cyan><b>[단검 휘두르기!]</b> 전방으로 비수를 빠르게 휘둘렀습니다!</color>");

            // 3. 부채꼴 범위 판정
            Collider[] hits = Physics.OverlapSphere(center, _attackRadius, _enemyLayer, QueryTriggerInteraction.Ignore);
            int hitCount = 0;

            foreach (var col in hits)
            {
                if (col.gameObject == owner) continue;

                var damageable = col.GetComponentInParent<IDamageable>();
                if (damageable != null && !damageable.IsDead)
                {
                    Vector3 toTarget = col.transform.position - center;
                    toTarget.y = 0f;

                    float angle = toTarget.sqrMagnitude > 0.01f ? Vector3.Angle(aimDirection, toTarget.normalized) : 0f;
                    if (angle <= _attackAngle * 0.5f)
                    {
                        hitCount++;
                        Vector3 knockDir = toTarget.sqrMagnitude > 0.01f ? toTarget.normalized : aimDirection;

                        DamageInfo dmg = new DamageInfo(
                            _baseDamage,
                            false,
                            col.transform.position,
                            knockDir,
                            _knockback,
                            ElementType.Physical,
                            owner
                        );
                        damageable.TakeDamage(dmg);
                        Debug.Log($"<color=cyan><b>[단검 베기 적중!]</b> {col.name}에게 {_baseDamage} 피해 및 강력한 넉백 부여!</color>");
                    }
                }
            }

            // 4. 원거리 공격 투사체 / 마법 반사(패링) 판정 (날아오던 각도로 반사)
            // - 즉시 1차 패링 판정 수행
            // - 약 0.22초 동안 패링 판정 윈도우를 열어 휘두르는 도중에 날아오는 투사체도 완벽하게 패링 보장
            if (_canReflectProjectiles)
            {
                CheckAndReflectProjectiles(owner, center, aimDirection);

                if (_parryWindowCoroutine != null)
                {
                    StopCoroutine(_parryWindowCoroutine);
                }
                _parryWindowCoroutine = StartCoroutine(ParryWindowRoutine(owner, aimDirection));
            }

            return true;
        }

        /// <summary>
        /// 단검 휘두르기 모션(0.22초) 동안 매 프레임 패링 영역을 유지하여,
        /// 살짝 일찍 단검을 눌렀어도 날아오는 투사체가 씹히지 않고 100% 쳐내어지도록 보장하는 코루틴
        /// </summary>
        private IEnumerator ParryWindowRoutine(GameObject owner, Vector3 aimDirection)
        {
            float elapsed = 0f;
            while (elapsed < ParryWindowDuration)
            {
                yield return null;
                elapsed += Time.deltaTime;

                if (this == null || !gameObject.activeInHierarchy) yield break;
                Vector3 currentCenter = owner != null ? owner.transform.position : transform.position;
                CheckAndReflectProjectiles(owner, currentCenter, aimDirection);
            }
            _parryWindowCoroutine = null;
        }

        /// <summary>
        /// 투사체 패링 판정 실행
        /// - 일반 투사체 및 곡사 투사체(주), 거대 암석(정예 요괴), 화염구(도깨비), 토템 구체 전수 검사
        /// - 수직(Y축) 허용 오차를 대폭 완화하여 낙하 중인 곡사 투사체나 단차 지형에서도 씹히지 않음
        /// - 초근접(1.3m 이내) 진입 투사체는 전방위 쳐내기 허용으로 억울한 사각지대 피격 방지
        /// </summary>
        private int CheckAndReflectProjectiles(GameObject owner, Vector3 center, Vector3 aimDirection)
        {
            if (!_canReflectProjectiles || Projectile.ActiveReflectables.Count == 0) return 0;

            float swingPlaneY = (owner != null ? owner.transform.position.y : transform.position.y) + 1.0f; // 중심 높이 1m
            const float defaultAllowedHeightTolerance = 3.5f; // 기본 수직 허용 오차 (바닥~공중 4.5m 커버)

            int reflectedCount = 0;

            for (int i = Projectile.ActiveReflectables.Count - 1; i >= 0; i--)
            {
                if (i >= Projectile.ActiveReflectables.Count) continue;
                var refTarget = Projectile.ActiveReflectables[i];
                if (refTarget == null || refTarget.IsFromPlayer) continue;

                Vector3 targetPos = refTarget.Position;
                Vector3 toRef = targetPos - center;
                toRef.y = 0f;
                float horizontalDist = toRef.magnitude;

                // 1. 수평 거리 검사 (단검 공격 반경 + 여유 반경 1.0m)
                float maxReflectDist = _attackRadius + 1.0f;
                if (horizontalDist > maxReflectDist) continue;

                // 2. 수직 높이 검사:
                // - 곡사 포물선 투사체(ZhuParabolicProjectile 등)는 머리 위 낙하 구간(y <= 6.5m) 전체 패링 허용
                // - 일반 투사체는 높이 차이 3.5m 이내면 패링 허용
                bool isHighAltitudeProjectile = refTarget is ZhuParabolicProjectile;
                float heightDiff = Mathf.Abs(targetPos.y - swingPlaneY);
                if (!isHighAltitudeProjectile && heightDiff > defaultAllowedHeightTolerance)
                {
                    continue;
                }
                if (isHighAltitudeProjectile && (targetPos.y > 6.5f || targetPos.y < -1.0f))
                {
                    continue;
                }

                // 3. 각도 검사:
                // - 플레이어 바로 옆/뒤 초근접(1.3m 이내)으로 파고든 투사체는 각도 무관 전방위 패링 허용 (비상 방어)
                // - 그 외에는 단검 부채꼴 각도 + 여유각 25도 이내일 때 패링
                bool isCloseQuarter = horizontalDist <= 1.35f;
                float angle = horizontalDist > 0.05f ? Vector3.Angle(aimDirection, toRef.normalized) : 0f;
                bool isWithinAngle = angle <= (_attackAngle * 0.5f) + 25f;

                if (!isCloseQuarter && !isWithinAngle)
                {
                    continue;
                }

                // 4. 패링 불가 투사체 (Unparryable) 여부 검사
                if (refTarget.ParryType == ProjectileParryType.Unparryable || !refTarget.CanParry)
                {
                    continue;
                }

                // 5. 강화 투사체(Enhanced, 2배 피해) vs 일반 투사체 데미지 배율 적용
                float damageMultiplier = refTarget.ParryType == ProjectileParryType.Enhanced
                    ? _reflectDamageMultiplier * 2.0f
                    : _reflectDamageMultiplier;

                // 6. 반사 실행
                refTarget.Reflect(owner, null, damageMultiplier, _reflectSpeedMultiplier);
                Core.VFXManager.Instance?.SpawnDaggerParrySpark(refTarget.Position);
                reflectedCount++;
            }

            if (reflectedCount > 0)
            {
                if (QuarterViewCamera.Instance != null)
                {
                    QuarterViewCamera.Instance.TriggerShake(0.35f, 0.2f);
                }
                Debug.Log($"<color=yellow><b>[단검 패링 반사!]</b> {reflectedCount}개의 적 원거리 투사체/마법을 날아오던 각도로 완벽하게 반사했습니다!</color>");
            }

            return reflectedCount;
        }

        /// <summary>
        /// 단검을 휘두를 때 전방으로 뻗어 나가는 화려한 청록색 초승달 검기 메쉬 생성 (프리팹 기반)
        /// </summary>
        private void CreateSlashVisualEffect(Vector3 center, Vector3 aimDir)
        {
            GameObject slashPrefab = null;
            if (Data.GameDataManager.Instance != null && Data.GameDataManager.Instance.Prefabs != null)
            {
                slashPrefab = Data.PrefabDatabaseSO.ResolvePrefab(ref Data.GameDataManager.Instance.Prefabs.daggerSlashVfxPrefab, "Prefabs/VFX_DaggerSlash");
            }
            else
            {
                slashPrefab = Resources.Load<GameObject>("Prefabs/VFX_DaggerSlash");
            }

            GameObject slashObj;
            if (slashPrefab != null)
            {
                slashObj = Instantiate(slashPrefab, center + Vector3.up * 0.1f, Quaternion.LookRotation(aimDir));
                slashObj.name = "DaggerSlashVFX";
            }
            else
            {
                slashObj = new GameObject("DaggerSlashVFX");
                slashObj.transform.position = center + Vector3.up * 0.1f;
                slashObj.transform.rotation = Quaternion.LookRotation(aimDir);
            }

            var meshFilter = slashObj.GetComponent<MeshFilter>();
            if (meshFilter == null) meshFilter = slashObj.AddComponent<MeshFilter>();
            var meshRend = slashObj.GetComponent<MeshRenderer>();
            if (meshRend == null) meshRend = slashObj.AddComponent<MeshRenderer>();

            Mesh mesh = new Mesh();
            int segments = 12;
            float startAngle = -_attackAngle * 0.5f;
            float angleStep = _attackAngle / segments;
            float innerR = 1.0f;
            float outerR = _attackRadius;

            Vector3[] verts = new Vector3[(segments + 1) * 2];
            int[] tris = new int[segments * 6];
            Color[] colors = new Color[verts.Length];

            for (int i = 0; i <= segments; i++)
            {
                float currentAngle = startAngle + i * angleStep;
                Quaternion rot = Quaternion.Euler(0f, currentAngle, 0f);
                Vector3 dir = rot * Vector3.forward;

                verts[i * 2] = dir * innerR;
                verts[i * 2 + 1] = dir * outerR;

                float t = Mathf.Sin((float)i / segments * Mathf.PI);
                colors[i * 2] = new Color(0.9f, 1f, 1f, t * 0.85f);
                colors[i * 2 + 1] = new Color(0f, 0.9f, 1f, t * 0.95f);

                if (i < segments)
                {
                    int baseIdx = i * 2;
                    int triIdx = i * 6;
                    tris[triIdx] = baseIdx;
                    tris[triIdx + 1] = baseIdx + 1;
                    tris[triIdx + 2] = baseIdx + 2;

                    tris[triIdx + 3] = baseIdx + 1;
                    tris[triIdx + 4] = baseIdx + 3;
                    tris[triIdx + 5] = baseIdx + 2;
                }
            }

            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.colors = colors;
            mesh.RecalculateNormals();

            meshFilter.mesh = mesh;

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Standard");

            Material mat = new Material(shader);
            Color slashColor = new Color(0.1f, 0.95f, 1f, 0.85f);
            mat.color = slashColor;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", slashColor);
            meshRend.material = mat;

            Destroy(slashObj, 0.16f);
        }

        public void AddBaseDamage(float add) => _baseDamage += add;
        public void AddAttackRadius(float add) => _attackRadius += add;
        public void AddReflectDamageMultiplier(float add) => _reflectDamageMultiplier += add;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, _attackRadius);
        }
    }
}
