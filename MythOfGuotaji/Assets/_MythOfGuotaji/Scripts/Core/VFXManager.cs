using System.Collections.Generic;
using UnityEngine;

namespace MythOfGuotaji.Core
{
    /// <summary>
    /// 게임 전역 이펙트(VFX) 매니저
    /// - Hyper Casual FX 라이브러리 프리팹을 동적 캐싱 및 스폰
    /// - 플레이어 대시, 차징 오라, 피격 스파크, 단검 참격/패링, 투사체 및 몬스터 스킬(폭발, 충격파, 독) 최적화 스케일링 제공
    /// </summary>
    public class VFXManager : MonoBehaviour
    {
        private static VFXManager _instance;
        public static VFXManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("[VFXManager]");
                    _instance = go.AddComponent<VFXManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private readonly Dictionary<string, GameObject> _cachedPrefabs = new();
        private const string VFXResourcePath = "VFX/Hyper Casual FX/";

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Resources/VFX/Hyper Casual FX/ 폴더에서 프리팹 로드 및 캐시
        /// </summary>
        public GameObject GetVFXPrefab(string vfxName)
        {
            if (string.IsNullOrEmpty(vfxName)) return null;

            if (_cachedPrefabs.TryGetValue(vfxName, out var cached) && cached != null)
            {
                return cached;
            }

            var prefab = Resources.Load<GameObject>(VFXResourcePath + vfxName);
            if (prefab != null)
            {
                _cachedPrefabs[vfxName] = prefab;
            }
            else
            {
                Debug.LogWarning($"[VFXManager] VFX 프리팹을 찾을 수 없습니다: {VFXResourcePath + vfxName}");
            }
            return prefab;
        }

        /// <summary>
        /// 공용 VFX 스폰 (위치, 회전, 스케일, 생명주기 관리)
        /// </summary>
        public GameObject Spawn(string vfxName, Vector3 position, Quaternion rotation, float scale = 1.0f, float autoDestroyTime = 2.0f, Transform parent = null)
        {
            var prefab = GetVFXPrefab(vfxName);
            return Spawn(prefab, position, rotation, scale, autoDestroyTime, parent);
        }

        /// <summary>
        /// 프리팹 GameObject 직접 전달 스폰
        /// </summary>
        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, float scale = 1.0f, float autoDestroyTime = 2.0f, Transform parent = null)
        {
            if (prefab == null) return null;

            GameObject instance = Instantiate(prefab, position, rotation, parent);
            if (instance != null)
            {
                ApplyHierarchyScaling(instance, scale);
                ApplyAlphaReduction(instance, 0.70f);

                if (autoDestroyTime > 0f)
                {
                    Destroy(instance, autoDestroyTime);
                }
            }
            return instance;
        }

        /// <summary>
        /// 사운드 프리팹 1회성 스폰 및 자동 재생 파괴
        /// </summary>
        public GameObject PlaySoundPrefab(GameObject soundPrefab, Vector3 position, float autoDestroyTime = 2.5f)
        {
            if (soundPrefab == null) return null;
            GameObject soundObj = Instantiate(soundPrefab, position, Quaternion.identity);
            if (soundObj != null)
            {
                var audio = soundObj.GetComponent<AudioSource>();
                if (audio != null)
                {
                    audio.Play();
                    float life = audio.clip != null ? Mathf.Max(audio.clip.length + 0.1f, 0.5f) : autoDestroyTime;
                    Destroy(soundObj, life);
                }
                else
                {
                    Destroy(soundObj, autoDestroyTime);
                }
            }
            return soundObj;
        }

        private void ApplyHierarchyScaling(GameObject go, float scale)
        {
            go.transform.localScale = Vector3.one * scale;
            var psArray = go.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in psArray)
            {
                var main = ps.main;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            }
        }

        /// <summary>
        /// 전체 이펙트의 투명도를 70% 수준으로 일괄 감쇄하고, 렌더링 소팅 오더를 기본 10으로 설정하여 탄환류(5000)보다 아래에 위치시킴
        /// </summary>
        private void ApplyAlphaReduction(GameObject go, float factor = 0.70f)
        {
            if (go == null) return;

            // 1. 파티클 시스템 투명도 70% 감쇄 및 소팅 오더 10 설정
            var psArray = go.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in psArray)
            {
                var main = ps.main;
                var startCol = main.startColor;
                switch (startCol.mode)
                {
                    case ParticleSystemGradientMode.Color:
                        Color c = startCol.color;
                        c.a *= factor;
                        main.startColor = c;
                        break;
                    case ParticleSystemGradientMode.TwoColors:
                        Color cMin = startCol.colorMin;
                        Color cMax = startCol.colorMax;
                        cMin.a *= factor;
                        cMax.a *= factor;
                        main.startColor = new ParticleSystem.MinMaxGradient(cMin, cMax);
                        break;
                    case ParticleSystemGradientMode.Gradient:
                        if (startCol.gradient != null)
                        {
                            main.startColor = ScaleGradientAlpha(startCol.gradient, factor);
                        }
                        break;
                    case ParticleSystemGradientMode.TwoGradients:
                        if (startCol.gradientMin != null && startCol.gradientMax != null)
                        {
                            main.startColor = new ParticleSystem.MinMaxGradient(
                                ScaleGradientAlpha(startCol.gradientMin, factor),
                                ScaleGradientAlpha(startCol.gradientMax, factor)
                            );
                        }
                        break;
                }

                var colOverLifetime = ps.colorOverLifetime;
                if (colOverLifetime.enabled)
                {
                    var colGrad = colOverLifetime.color;
                    if (colGrad.mode == ParticleSystemGradientMode.Gradient && colGrad.gradient != null)
                    {
                        colOverLifetime.color = ScaleGradientAlpha(colGrad.gradient, factor);
                    }
                    else if (colGrad.mode == ParticleSystemGradientMode.TwoGradients && colGrad.gradientMin != null && colGrad.gradientMax != null)
                    {
                        colOverLifetime.color = new ParticleSystem.MinMaxGradient(
                            ScaleGradientAlpha(colGrad.gradientMin, factor),
                            ScaleGradientAlpha(colGrad.gradientMax, factor)
                        );
                    }
                }

                var psRend = ps.GetComponent<ParticleSystemRenderer>();
                if (psRend != null && psRend.sortingOrder < 1000)
                {
                    psRend.sortingOrder = 10;
                }
            }

            // 2. 메시 렌더러 / 트레일 렌더러 재질 투명도 70% 감쇄
            var rends = go.GetComponentsInChildren<Renderer>(true);
            foreach (var r in rends)
            {
                if (r.sortingOrder < 1000)
                {
                    r.sortingOrder = 10;
                }

                if (r is ParticleSystemRenderer) continue;

                foreach (var mat in r.materials)
                {
                    if (mat == null) continue;
                    if (mat.HasProperty("_BaseColor"))
                    {
                        Color c = mat.GetColor("_BaseColor");
                        c.a *= factor;
                        mat.SetColor("_BaseColor", c);
                    }
                    if (mat.HasProperty("_Color"))
                    {
                        Color c = mat.GetColor("_Color");
                        c.a *= factor;
                        mat.SetColor("_Color", c);
                    }
                    if (mat.HasProperty("_TintColor"))
                    {
                        Color c = mat.GetColor("_TintColor");
                        c.a *= factor;
                        mat.SetColor("_TintColor", c);
                    }
                }
            }
        }

        private Gradient ScaleGradientAlpha(Gradient grad, float factor)
        {
            var newGrad = new Gradient();
            var alphaKeys = grad.alphaKeys;
            var newAlphaKeys = new GradientAlphaKey[alphaKeys.Length];
            for (int i = 0; i < alphaKeys.Length; i++)
            {
                newAlphaKeys[i] = new GradientAlphaKey(alphaKeys[i].alpha * factor, alphaKeys[i].time);
            }
            newGrad.SetKeys(grad.colorKeys, newAlphaKeys);
            return newGrad;
        }

        // ==========================================
        // 1. 플레이어 관련 액션 VFX
        // ==========================================

        /// <summary>
        /// 플레이어 대시 시 발밑 모래/공기 먼지 이펙트
        /// </summary>
        public GameObject SpawnPlayerDashDust(Vector3 position, Vector3 direction)
        {
            Quaternion rot = direction.sqrMagnitude > 0.001f ? Quaternion.LookRotation(direction) : Quaternion.identity;
            var playerStats = Data.GameDataManager.Instance?.PlayerStats;
            if (playerStats != null && playerStats.dashDustVfxPrefab != null)
            {
                return Spawn(playerStats.dashDustVfxPrefab, position + Vector3.up * 0.05f, rot, scale: 1.25f, autoDestroyTime: 1.2f);
            }
            return Spawn("HCFX_Dust_Dash_01", position + Vector3.up * 0.05f, rot, scale: 1.25f, autoDestroyTime: 1.2f);
        }

        /// <summary>
        /// 플레이어 무적/슬라이딩 회피 성공 시 반짝임 이펙트
        /// </summary>
        public GameObject SpawnPlayerEvasionShine(Vector3 position)
        {
            var playerStats = Data.GameDataManager.Instance?.PlayerStats;
            if (playerStats != null && playerStats.evasionShineVfxPrefab != null)
            {
                return Spawn(playerStats.evasionShineVfxPrefab, position + Vector3.up * 0.8f, Quaternion.identity, scale: 0.9f, autoDestroyTime: 0.6f);
            }
            return Spawn("HCFX_Shine_01", position + Vector3.up * 0.8f, Quaternion.identity, scale: 0.9f, autoDestroyTime: 0.6f);
        }

        /// <summary>
        /// 플레이어 활 차징 단계별 오라 (0.3s ~ 3.0s)
        /// </summary>
        public GameObject SpawnBowChargingAura(Transform parent, int step)
        {
            var playerStats = Data.GameDataManager.Instance?.PlayerStats;
            GameObject targetPrefab = null;
            if (playerStats != null)
            {
                targetPrefab = step switch
                {
                    1 => playerStats.chargeAuraStep1VfxPrefab,
                    2 => playerStats.chargeAuraStep2VfxPrefab,
                    _ => playerStats.chargeAuraStep3VfxPrefab
                };
            }

            if (targetPrefab != null)
            {
                return Spawn(targetPrefab, parent.position + Vector3.up * 0.6f, Quaternion.identity, scale: 1.0f, autoDestroyTime: 0f, parent: parent);
            }

            string prefabName = step switch
            {
                1 => "HCFX_Charging_01_Step1",
                2 => "HCFX_Charging_01_Step2",
                _ => "HCFX_Charging_01_Step3"
            };
            return Spawn(prefabName, parent.position + Vector3.up * 0.6f, Quaternion.identity, scale: 1.0f, autoDestroyTime: 0f, parent: parent);
        }

        /// <summary>
        /// 차징 화살 발사 순간 폭발적 방출 이펙트
        /// </summary>
        public GameObject SpawnBowChargeReleaseBurst(Vector3 position, Vector3 direction)
        {
            Quaternion rot = direction.sqrMagnitude > 0.001f ? Quaternion.LookRotation(direction) : Quaternion.identity;
            var playerStats = Data.GameDataManager.Instance?.PlayerStats;
            if (playerStats != null && playerStats.chargeReleaseBurstVfxPrefab != null)
            {
                return Spawn(playerStats.chargeReleaseBurstVfxPrefab, position + Vector3.up * 0.6f, rot, scale: 1.3f, autoDestroyTime: 1.5f);
            }
            return Spawn("HCFX_Charging_01_Step3_End", position + Vector3.up * 0.6f, rot, scale: 1.3f, autoDestroyTime: 1.5f);
        }

        /// <summary>
        /// 단검 부채꼴 베기(Slash) 검기 플래시 이펙트
        /// </summary>
        public GameObject SpawnDaggerSlash(Vector3 position, Vector3 direction, float radius = 3.0f, GameObject customSlashPrefab = null)
        {
            Quaternion rot = direction.sqrMagnitude > 0.001f ? Quaternion.LookRotation(direction) : Quaternion.identity;
            if (customSlashPrefab != null)
            {
                return Spawn(customSlashPrefab, position + direction * 0.6f + Vector3.up * 0.7f, rot, scale: radius * 0.55f, autoDestroyTime: 0.8f);
            }
            var playerStats = Data.GameDataManager.Instance?.PlayerStats;
            if (playerStats != null && playerStats.defaultDaggerSlashVfxPrefab != null)
            {
                return Spawn(playerStats.defaultDaggerSlashVfxPrefab, position + direction * 0.6f + Vector3.up * 0.7f, rot, scale: radius * 0.55f, autoDestroyTime: 0.8f);
            }
            // HCFX_Flash_02는 화려한 호형 참격 플래시
            return Spawn("HCFX_Flash_02", position + direction * 0.6f + Vector3.up * 0.7f, rot, scale: radius * 0.55f, autoDestroyTime: 0.8f);
        }

        /// <summary>
        /// 단검 투사체 패링/반사 마법진 스파크 이펙트
        /// </summary>
        public GameObject SpawnDaggerParrySpark(Vector3 position, GameObject customParryPrefab = null)
        {
            if (customParryPrefab != null)
            {
                return Spawn(customParryPrefab, position, Quaternion.identity, scale: 1.4f, autoDestroyTime: 1.0f);
            }
            Spawn("HCFX_MagicCircle_01", position + Vector3.up * 0.05f, Quaternion.Euler(90f, 0f, 0f), scale: 1.2f, autoDestroyTime: 0.8f);
            return Spawn("HCFX_Hit_06", position, Quaternion.identity, scale: 1.4f, autoDestroyTime: 1.0f);
        }

        // ==========================================
        // 2. 투사체 피격 및 충돌 VFX
        // ==========================================

        /// <summary>
        /// 화살 / 무기 피격 시 타격 스파크 및 충격 이펙트
        /// </summary>
        public GameObject SpawnHitImpact(Vector3 position, Vector3 direction, bool isCritical = false)
        {
            Quaternion rot = direction.sqrMagnitude > 0.001f ? Quaternion.LookRotation(-direction) : Quaternion.identity;
            var playerStats = Data.GameDataManager.Instance?.PlayerStats;
            if (playerStats != null)
            {
                var customHitPrefab = isCritical ? playerStats.criticalHitSparkVfxPrefab : playerStats.hitSparkVfxPrefab;
                if (customHitPrefab != null)
                {
                    return Spawn(customHitPrefab, position, rot, scale: isCritical ? 1.6f : 1.1f, autoDestroyTime: 1.0f);
                }
            }
            string hitPrefab = isCritical ? "HCFX_Hit_07" : "HCFX_Hit_04";
            float scale = isCritical ? 1.6f : 1.1f;
            return Spawn(hitPrefab, position, rot, scale: scale, autoDestroyTime: 1.0f);
        }

        /// <summary>
        /// 차징 관통 화살 비행 트레일 오라
        /// </summary>
        public GameObject AttachChargedArrowTrail(Transform parent)
        {
            var go = Spawn("HCFX_Energy_06", parent.position, parent.rotation, scale: 0.8f, autoDestroyTime: 0f, parent: parent);
            if (go != null)
            {
                foreach (var r in go.GetComponentsInChildren<Renderer>(true)) r.sortingOrder = 4900;
            }
            return go;
        }

        // ==========================================
        // 3. 몬스터 스킬 및 패턴 VFX
        // ==========================================

        /// <summary>
        /// 잡몹(Minion) 돌진 먼지 (테이블 연결 프리팹 우선 로드, 없으면 HCFX_Dust_01)
        /// </summary>
        public GameObject SpawnMinionDashDust(Vector3 position, Vector3 direction, GameObject customPrefab = null)
        {
            Quaternion rot = direction.sqrMagnitude > 0.001f ? Quaternion.LookRotation(direction) : Quaternion.identity;
            if (customPrefab != null)
            {
                return Spawn(customPrefab, position + Vector3.up * 0.05f, rot, scale: 0.9f, autoDestroyTime: 1.0f);
            }
            var skill = Data.GameDataManager.Instance?.MonsterSkills?.GetSkill("minion_dash");
            var targetPrefab = skill?.castVfxPrefab ?? skill?.trailVfxPrefab;
            if (targetPrefab != null)
            {
                return Spawn(targetPrefab, position + Vector3.up * 0.05f, rot, scale: 0.9f, autoDestroyTime: 1.0f);
            }
            return Spawn("HCFX_Dust_01", position + Vector3.up * 0.05f, rot, scale: 0.9f, autoDestroyTime: 1.0f);
        }

        /// <summary>
        /// 잡몹(Minion) 70cm 도착 충격 이펙트 (테이블 연결 프리팹 우선 로드, 없으면 HCFX_Dust_FloorHit_01)
        /// </summary>
        public GameObject SpawnMinionArrivalImpact(Vector3 position, float radius = 0.7f, GameObject customPrefab = null)
        {
            if (customPrefab != null)
            {
                return Spawn(customPrefab, position + Vector3.up * 0.05f, Quaternion.identity, scale: radius * 1.5f, autoDestroyTime: 1.0f);
            }
            var skill = Data.GameDataManager.Instance?.MonsterSkills?.GetSkill("minion_dash");
            if (skill != null && skill.impactVfxPrefab != null)
            {
                return Spawn(skill.impactVfxPrefab, position + Vector3.up * 0.05f, Quaternion.identity, scale: radius * 1.5f, autoDestroyTime: 1.0f);
            }
            return Spawn("HCFX_Dust_FloorHit_01", position + Vector3.up * 0.05f, Quaternion.identity, scale: radius * 1.5f, autoDestroyTime: 1.0f);
        }

        /// <summary>
        /// 맹독충(Minion_Archer) 독침 발사구 및 피격/착지 독물 튐 이펙트 (테이블 연결 프리팹 우선 로드, 없으면 HCFX_Poison_01)
        /// </summary>
        public GameObject SpawnVenomMuzzle(Vector3 position, Vector3 direction, GameObject customPrefab = null)
        {
            Quaternion rot = direction.sqrMagnitude > 0.001f ? Quaternion.LookRotation(direction) : Quaternion.identity;
            if (customPrefab != null)
            {
                return Spawn(customPrefab, position, rot, scale: 0.9f, autoDestroyTime: 1.0f);
            }
            var skill = Data.GameDataManager.Instance?.MonsterSkills?.GetSkill("toxic_spit");
            if (skill != null && skill.castVfxPrefab != null)
            {
                return Spawn(skill.castVfxPrefab, position, rot, scale: 0.9f, autoDestroyTime: 1.0f);
            }
            return Spawn("HCFX_Poison_01", position, rot, scale: 0.9f, autoDestroyTime: 1.0f);
        }

        public GameObject SpawnVenomSplash(Vector3 position, GameObject customPrefab = null)
        {
            if (customPrefab != null)
            {
                return Spawn(customPrefab, position + Vector3.up * 0.05f, Quaternion.identity, scale: 1.3f, autoDestroyTime: 1.5f);
            }
            var skill = Data.GameDataManager.Instance?.MonsterSkills?.GetSkill("toxic_spit");
            if (skill != null && skill.impactVfxPrefab != null)
            {
                return Spawn(skill.impactVfxPrefab, position + Vector3.up * 0.05f, Quaternion.identity, scale: 1.3f, autoDestroyTime: 1.5f);
            }
            return Spawn("HCFX_Poison_02", position + Vector3.up * 0.05f, Quaternion.identity, scale: 1.3f, autoDestroyTime: 1.5f);
        }

        /// <summary>
        /// 거북이(Minion_BigMon) 돌진 시 후방 대시 흙먼지 폭풍 & 3m 정지 "쿵!" 충격파 (테이블 연결 프리팹 우선 로드, 없으면 HCFX_Dust_Dash_01)
        /// </summary>
        public GameObject SpawnBigMonRunDust(Vector3 position, Vector3 direction, GameObject customPrefab = null)
        {
            Quaternion rot = direction.sqrMagnitude > 0.001f ? Quaternion.LookRotation(-direction) : Quaternion.identity;
            if (customPrefab != null)
            {
                return Spawn(customPrefab, position + Vector3.up * 0.05f, rot, scale: 1.4f, autoDestroyTime: 1.0f);
            }
            var skill = Data.GameDataManager.Instance?.MonsterSkills?.GetSkill("turtle_charge");
            if (skill != null && skill.trailVfxPrefab != null)
            {
                return Spawn(skill.trailVfxPrefab, position + Vector3.up * 0.05f, rot, scale: 1.4f, autoDestroyTime: 1.0f);
            }
            return Spawn("HCFX_Dust_Dash_01", position + Vector3.up * 0.05f, rot, scale: 1.4f, autoDestroyTime: 1.0f);
        }

        public GameObject SpawnBigMonGroundImpact(Vector3 position, float radius = 3.0f, GameObject customPrefab = null)
        {
            if (customPrefab != null)
            {
                return Spawn(customPrefab, position + Vector3.up * 0.05f, Quaternion.identity, scale: radius * 0.75f, autoDestroyTime: 1.8f);
            }
            var skill = Data.GameDataManager.Instance?.MonsterSkills?.GetSkill("turtle_charge");
            if (skill != null && skill.impactVfxPrefab != null)
            {
                return Spawn(skill.impactVfxPrefab, position + Vector3.up * 0.05f, Quaternion.identity, scale: radius * 0.75f, autoDestroyTime: 1.8f);
            }
            // 바닥 충격파 링 + 폭발성 흙먼지 복합 스폰
            Spawn("HCFX_Dust_FloorHit_03", position + Vector3.up * 0.05f, Quaternion.identity, scale: radius * 0.75f, autoDestroyTime: 1.8f);
            return Spawn("HCFX_Explosion_03", position + Vector3.up * 0.1f, Quaternion.identity, scale: radius * 0.65f, autoDestroyTime: 1.8f);
        }

        /// <summary>
        /// 주(鴸) 곡사 투사체 비행 화염 및 5m 착지 대폭발 이펙트 (테이블 연결 프리팹 우선 로드, 없으면 HCFX_Fire_01)
        /// </summary>
        public GameObject AttachZhuProjectileFire(Transform parent, GameObject customPrefab = null)
        {
            GameObject targetPrefab = customPrefab;
            if (targetPrefab == null)
            {
                var skill = Data.GameDataManager.Instance?.MonsterSkills?.GetSkill("zhu_parabolic");
                if (skill != null && skill.trailVfxPrefab != null)
                {
                    targetPrefab = skill.trailVfxPrefab;
                }
            }
            var go = targetPrefab != null
                ? Spawn(targetPrefab, parent.position, parent.rotation, scale: 0.9f, autoDestroyTime: 0f, parent: parent)
                : Spawn("HCFX_Fire_01", parent.position, parent.rotation, scale: 0.9f, autoDestroyTime: 0f, parent: parent);
            if (go != null)
            {
                foreach (var r in go.GetComponentsInChildren<Renderer>(true)) r.sortingOrder = 4900;
            }
            return go;
        }

        public GameObject SpawnZhuGroundExplosion(Vector3 position, float radius = 5.0f, GameObject customPrefab = null)
        {
            if (customPrefab != null)
            {
                return Spawn(customPrefab, position + Vector3.up * 0.1f, Quaternion.identity, scale: radius * 0.65f, autoDestroyTime: 2.0f);
            }
            var skill = Data.GameDataManager.Instance?.MonsterSkills?.GetSkill("zhu_parabolic");
            if (skill != null && skill.impactVfxPrefab != null)
            {
                return Spawn(skill.impactVfxPrefab, position + Vector3.up * 0.1f, Quaternion.identity, scale: radius * 0.65f, autoDestroyTime: 2.0f);
            }
            return Spawn("HCFX_Explosion_01", position + Vector3.up * 0.1f, Quaternion.identity, scale: radius * 0.65f, autoDestroyTime: 2.0f);
        }

        /// <summary>
        /// 몬스터 평타 공격 시전 궤적 VFX 스폰 (MonsterStatsTableSO의 normalAttack.attackVfxPrefab 연동)
        /// </summary>
        public GameObject SpawnMonsterAttackVFX(Actor.MonsterType type, Vector3 position, Quaternion rotation, GameObject customPrefab = null)
        {
            if (customPrefab != null)
            {
                return Spawn(customPrefab, position, rotation, scale: 1.0f, autoDestroyTime: 1.0f);
            }
            var stat = Data.GameDataManager.Instance?.MonsterStats?.GetStat(type);
            if (stat?.normalAttack != null && stat.normalAttack.attackVfxPrefab != null)
            {
                return Spawn(stat.normalAttack.attackVfxPrefab, position, rotation, scale: 1.0f, autoDestroyTime: 1.0f);
            }
            return null;
        }

        /// <summary>
        /// 몬스터 피격 충격 VFX 스폰 (MonsterStatsTableSO의 hitVfxPrefab 연동)
        /// </summary>
        public GameObject SpawnMonsterHitVFX(Actor.MonsterType type, Vector3 position, Quaternion rotation, GameObject customPrefab = null)
        {
            if (customPrefab != null)
            {
                return Spawn(customPrefab, position, rotation, scale: 1.1f, autoDestroyTime: 1.0f);
            }
            var stat = Data.GameDataManager.Instance?.MonsterStats?.GetStat(type);
            if (stat != null && stat.hitVfxPrefab != null)
            {
                return Spawn(stat.hitVfxPrefab, position, rotation, scale: 1.1f, autoDestroyTime: 1.0f);
            }
            return Spawn("HCFX_Hit_01", position, rotation, scale: 1.1f, autoDestroyTime: 1.0f);
        }

        /// <summary>
        /// 몬스터 방패/면역 도탄 VFX 스폰 (MonsterStatsTableSO의 deflectVfxPrefab 연동)
        /// </summary>
        public GameObject SpawnMonsterDeflectVFX(Actor.MonsterType type, Vector3 position, Quaternion rotation, GameObject customPrefab = null)
        {
            if (customPrefab != null)
            {
                return Spawn(customPrefab, position, rotation, scale: 1.2f, autoDestroyTime: 1.0f);
            }
            var stat = Data.GameDataManager.Instance?.MonsterStats?.GetStat(type);
            if (stat != null && stat.deflectVfxPrefab != null)
            {
                return Spawn(stat.deflectVfxPrefab, position, rotation, scale: 1.2f, autoDestroyTime: 1.0f);
            }
            return Spawn("HCFX_Sparks_01", position, rotation, scale: 1.2f, autoDestroyTime: 1.0f);
        }

        /// <summary>
        /// 몬스터 사망 시 폭발/연기 VFX 스폰 (MonsterStatsTableSO의 deathVfxPrefab 연동)
        /// </summary>
        public GameObject SpawnMonsterDeathVFX(Actor.MonsterType type, Vector3 position, Quaternion rotation, GameObject customPrefab = null)
        {
            if (customPrefab != null)
            {
                return Spawn(customPrefab, position, rotation, scale: 1.2f, autoDestroyTime: 2.0f);
            }
            var stat = Data.GameDataManager.Instance?.MonsterStats?.GetStat(type);
            if (stat != null && stat.deathVfxPrefab != null)
            {
                return Spawn(stat.deathVfxPrefab, position, rotation, scale: 1.2f, autoDestroyTime: 2.0f);
            }
            return Spawn("HCFX_Explosion_01", position, rotation, scale: 1.2f, autoDestroyTime: 2.0f);
        }

        // ==========================================
        // 4. 영혼석 & 헬퍼 & 레벨업 보상 VFX
        // ==========================================

        /// <summary>
        /// 영혼석 흡수/수집 시 반짝이는 영혼 구체 획득 이펙트
        /// </summary>
        public GameObject SpawnGemCollectEffect(Vector3 position)
        {
            return Spawn("HCFX_Marble_01_Get", position + Vector3.up * 0.4f, Quaternion.identity, scale: 1.2f, autoDestroyTime: 1.0f);
        }

        /// <summary>
        /// 레벨업 / 스킬 선택 시 머리 위 승천 버프 이펙트
        /// </summary>
        public GameObject SpawnLevelUpBuff(Transform target)
        {
            return Spawn("HCFX_Buff_Up", target.position + Vector3.up * 0.1f, Quaternion.identity, scale: 1.2f, autoDestroyTime: 2.2f, parent: target);
        }

        /// <summary>
        /// 2차, 3차 요괴 웨이브 출현 시 무리 중심 바닥에서 하강하는 소환 결계/버프 연출
        /// </summary>
        public GameObject SpawnWaveSpawnVFX(Vector3 center)
        {
            return Spawn("HCFX_Buff_Down", center + Vector3.up * 0.1f, Quaternion.identity, scale: 2.5f, autoDestroyTime: 2.0f);
        }

        /// <summary>
        /// 하트(체력) 회복 시 본체 몸에서 발동되는 하트 버스트 이펙트 (HCFX_Heart)
        /// </summary>
        public GameObject SpawnHealBuff(Transform target)
        {
            if (target == null) return null;
            var playerStats = Data.GameDataManager.Instance?.PlayerStats;
            if (playerStats != null && playerStats.healBuffVfxPrefab != null)
            {
                return Spawn(playerStats.healBuffVfxPrefab, target.position + Vector3.up * 0.4f, Quaternion.identity, scale: 1.25f, autoDestroyTime: 1.5f, parent: target);
            }
            return Spawn("HCFX_Heart", target.position + Vector3.up * 0.4f, Quaternion.identity, scale: 1.25f, autoDestroyTime: 1.5f, parent: target);
        }
    }
}
