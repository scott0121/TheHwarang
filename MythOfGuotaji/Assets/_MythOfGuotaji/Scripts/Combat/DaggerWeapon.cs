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
        [SerializeField] private float _reflectSpeedMultiplier = 1.2f;
        [SerializeField] private LayerMask _enemyLayer;

        private float _lastAttackTime = -99f;
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
            // 플레이어의 반사 궤도를 높이 1미터 평면으로 계산하여, 공중에 높이 떠 있는 곡사 투사체 등은 반사 불가하도록 정밀도 향상
            int reflectedCount = 0;
            if (_canReflectProjectiles)
            {
                float swingPlaneY = (owner != null ? owner.transform.position.y : transform.position.y) + 1.0f; // 높이 1미터 평면
                const float allowedHeightTolerance = 1f; // 수직 허용 오차 (0.35m ~ 1.65m 범위 내에 도달했을 때만 반사 가능)

                for (int i = Projectile.ActiveReflectables.Count - 1; i >= 0; i--)
                {
                    var refTarget = Projectile.ActiveReflectables[i];
                    if (refTarget == null || refTarget.IsFromPlayer) continue;

                    // 1m 평면 높이 일치 여부 검사 (공중에 높이 뜬 상태는 제외)
                    float heightDiff = Mathf.Abs(refTarget.Position.y - swingPlaneY);
                    if (heightDiff > allowedHeightTolerance)
                    {
                        continue;
                    }

                    Vector3 toRef = refTarget.Position - center;
                    toRef.y = 0f;
                    float dist = toRef.magnitude;
                    if (dist <= _attackRadius + 0.8f)
                    {
                        float angle = dist > 0.05f ? Vector3.Angle(aimDirection, toRef.normalized) : 0f;
                        if (angle <= (_attackAngle * 0.5f) + 20f)
                        {
                            refTarget.Reflect(owner);
                            Core.VFXManager.Instance?.SpawnDaggerParrySpark(refTarget.Position);
                            reflectedCount++;
                        }
                    }
                }
            }

            if (reflectedCount > 0)
            {
                if (QuarterViewCamera.Instance != null)
                {
                    QuarterViewCamera.Instance.TriggerShake(0.35f, 0.2f);
                }
                Debug.Log($"<color=yellow><b>[단검 패링 반사!]</b> {reflectedCount}개의 적 원거리 투사체/마법을 날아오던 각도로 완벽하게 반사했습니다!</color>");
            }

            return true;
        }

        /// <summary>
        /// 단검을 휘두를 때 전방으로 뻗어 나가는 화려한 청록색 초승달 검기 메쉬 생성
        /// </summary>
        private void CreateSlashVisualEffect(Vector3 center, Vector3 aimDir)
        {
            GameObject slashObj = new GameObject("DaggerSlashVFX");
            slashObj.transform.position = center + Vector3.up * 0.1f;
            slashObj.transform.rotation = Quaternion.LookRotation(aimDir);

            var meshFilter = slashObj.AddComponent<MeshFilter>();
            var meshRend = slashObj.AddComponent<MeshRenderer>();

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
