using System;
using System.Collections.Generic;
using MythOfGuotaji.Actor;
using UnityEngine;

namespace MythOfGuotaji.Data
{
    /// <summary>
    /// 몬스터 스킬 패턴 유형
    /// </summary>
    public enum MonsterSkillType
    {
        MeleeAttack,        // 기본 근접 연타/후리기
        MinionDash,         // 잡몹 전용 5m 돌진 & 착지 0.7m 충격파
        TurtleCharge,       // 대형 거북 8m~20m 돌진 분쇄
        RangedStraight,     // 독침/화살 등의 직선 원거리 투사체 & 카이팅
        RangedParabolic,    // Zhu 등의 포물선 곡사 투척 (벽 무시)
        EliteBoulder,       // 열쇠 엘리트의 굴러가는 바위 투척
        BossVacuum,         // 천년여우 1번 패턴: 5m 영혼 흡인 소용돌이
        BossCleave,         // 천년여우 2번 패턴: 3.5m 120도 부채꼴 요화 분쇄
        AOESlam,            // 지면 강타 충격파
        SummonMinions,      // 부하 요괴 소환 / 환영 분신
        ParryReflect,       // 패링 유도 반사형 투사체 (도깨비 화염구)
        TetherPull,         // 사슬/거미줄 견인 (슬라이딩 탈출)
        ShieldGuard,        // 전면 철갑 방패 가드 (풀차징 화살로 파괴)
        EnergyAbsorb,       // 에너지 흡수 폭주 (차징 금지, 일반사격 전용)
        KamikazeExplode,    // 초고속 자폭 돌진
        TurretBarrage       // 고정 포대 회전 사방 탄막
    }

    /// <summary>
    /// 테이블에 직접 등록된 사운드 및 VFX GameObject 재생 유틸리티
    /// - 1회성 사운드: GameObject 인스턴스화 후 AudioSource 클립 종료 시점까지 체크하여 재생 후 자동 파괴
    /// - 루프 사운드: 별도 활성화 플래그 및 루프 GameObject를 통해 지속 재생 및 상태 종료 시 정지/파괴
    /// </summary>
    public static class MonsterTableAssetPlayer
    {
        /// <summary>
        /// 테이블에 등록된 VFX GameObject를 생성하고 파티클 종료 시 자동 정리
        /// </summary>
        public static GameObject SpawnVfx(GameObject vfxPrefab, Vector3 position, Quaternion rotation, Transform parent = null, float fallbackLifetime = 2.5f)
        {
            if (vfxPrefab == null) return null;

            GameObject instance = parent != null
                ? UnityEngine.Object.Instantiate(vfxPrefab, position, rotation, parent)
                : UnityEngine.Object.Instantiate(vfxPrefab, position, rotation);

            float maxLifetime = 0f;
            var particles = instance.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particles.Length; i++)
            {
                var ps = particles[i];
                if (ps == null) continue;
                var main = ps.main;
                if (main.loop)
                {
                    maxLifetime = Mathf.Max(maxLifetime, fallbackLifetime);
                }
                else
                {
                    float duration = main.duration + main.startLifetime.constantMax;
                    if (duration > maxLifetime) maxLifetime = duration;
                }
            }

            if (maxLifetime <= 0.05f)
            {
                maxLifetime = fallbackLifetime;
            }

            UnityEngine.Object.Destroy(instance, maxLifetime + 0.1f);
            return instance;
        }

        /// <summary>
        /// 테이블에 등록된 VFX GameObject를 생성하고 지정된 지속시간 후 자동 정리
        /// </summary>
        public static GameObject SpawnVfx(GameObject vfxPrefab, Vector3 position, Quaternion rotation, float fallbackLifetime)
        {
            return SpawnVfx(vfxPrefab, position, rotation, null, fallbackLifetime);
        }

        /// <summary>
        /// 테이블에 등록된 1회성 사운드 GameObject를 생성하고 해당 클립 종료 시까지 체크하여 플레이 후 자동 제거
        /// </summary>
        public static GameObject PlayOneShotSound(GameObject soundPrefab, Vector3 position, Transform followTarget = null)
        {
            if (soundPrefab == null) return null;

            GameObject soundGo = UnityEngine.Object.Instantiate(soundPrefab, position, Quaternion.identity);
            soundGo.name = $"[OneShotSound_{soundPrefab.name}]";

            var tracker = soundGo.GetComponent<OneShotSoundTracker>();
            if (tracker == null)
            {
                tracker = soundGo.AddComponent<OneShotSoundTracker>();
            }
            tracker.Initialize(followTarget);
            return soundGo;
        }

        /// <summary>
        /// 테이블에 별도 세팅된 루프 사운드 GameObject를 시작하여 인스턴스를 반환 (enableLoop이 true이고 프리팹이 할당된 경우에만 작동)
        /// </summary>
        public static GameObject StartLoopSound(GameObject loopSoundPrefab, bool enableLoop, Transform ownerTransform = null)
        {
            GameObject instance = null;
            return StartLoopSound(loopSoundPrefab, enableLoop, ownerTransform, ref instance);
        }

        /// <summary>
        /// 테이블에 별도 세팅된 루프 사운드 GameObject를 지정 위치에서 시작하여 인스턴스를 반환
        /// </summary>
        public static GameObject StartLoopSound(GameObject loopSoundPrefab, bool enableLoop, Vector3 position, Transform ownerTransform = null)
        {
            if (!enableLoop || loopSoundPrefab == null)
            {
                return null;
            }

            GameObject activeLoopInstance = ownerTransform != null
                ? UnityEngine.Object.Instantiate(loopSoundPrefab, position, Quaternion.identity, ownerTransform)
                : UnityEngine.Object.Instantiate(loopSoundPrefab, position, Quaternion.identity);

            activeLoopInstance.name = $"[LoopSound_{loopSoundPrefab.name}]";

            var sources = activeLoopInstance.GetComponentsInChildren<AudioSource>(true);
            for (int i = 0; i < sources.Length; i++)
            {
                var src = sources[i];
                if (src == null) continue;
                src.loop = true;
                if (src.clip != null && !src.isPlaying)
                {
                    src.Play();
                }
            }

            return activeLoopInstance;
        }

        /// <summary>
        /// 테이블에 별도 세팅된 루프 사운드 GameObject를 시작 (enableLoop이 true이고 프리팹이 할당된 경우에만 작동)
        /// </summary>
        public static GameObject StartLoopSound(GameObject loopSoundPrefab, bool enableLoop, Transform ownerTransform, ref GameObject activeLoopInstance)
        {
            if (!enableLoop || loopSoundPrefab == null)
            {
                StopLoopSound(ref activeLoopInstance);
                return null;
            }

            if (activeLoopInstance != null)
            {
                return activeLoopInstance;
            }

            Vector3 spawnPos = ownerTransform != null ? ownerTransform.position : Vector3.zero;
            activeLoopInstance = ownerTransform != null
                ? UnityEngine.Object.Instantiate(loopSoundPrefab, spawnPos, Quaternion.identity, ownerTransform)
                : UnityEngine.Object.Instantiate(loopSoundPrefab, spawnPos, Quaternion.identity);

            activeLoopInstance.name = $"[LoopSound_{loopSoundPrefab.name}]";

            var sources = activeLoopInstance.GetComponentsInChildren<AudioSource>(true);
            for (int i = 0; i < sources.Length; i++)
            {
                var src = sources[i];
                if (src == null) continue;
                src.loop = true;
                if (src.clip != null && !src.isPlaying)
                {
                    src.Play();
                }
            }

            return activeLoopInstance;
        }

        /// <summary>
        /// 실행 중인 루프 사운드 GameObject를 즉시 정지 및 파괴
        /// </summary>
        public static void StopLoopSound(GameObject activeLoopInstance)
        {
            if (activeLoopInstance == null) return;
            StopLoopSound(ref activeLoopInstance);
        }

        /// <summary>
        /// 실행 중인 루프 사운드 GameObject를 즉시 정지 및 파괴하고 참조를 null로 초기화
        /// </summary>
        public static void StopLoopSound(ref GameObject activeLoopInstance)
        {
            if (activeLoopInstance == null) return;

            var sources = activeLoopInstance.GetComponentsInChildren<AudioSource>(true);
            for (int i = 0; i < sources.Length; i++)
            {
                if (sources[i] != null && sources[i].isPlaying)
                {
                    sources[i].Stop();
                }
            }

            UnityEngine.Object.Destroy(activeLoopInstance);
            activeLoopInstance = null;
        }
    }

    /// <summary>
    /// 1회성 사운드 GameObject의 AudioSource 클립 재생 상태를 매 프레임 체크하여 클립 종료 시점에 오브젝트를 자동 파괴하는 컴포넌트
    /// </summary>
    public class OneShotSoundTracker : MonoBehaviour
    {
        private AudioSource[] _sources;
        private Transform _followTarget;
        private float _fallbackEndTime;
        private bool _initialized;

        public void Initialize(Transform followTarget)
        {
            _followTarget = followTarget;
            _sources = GetComponentsInChildren<AudioSource>(true);

            float maxClipDuration = 0f;
            bool hasValidClip = false;

            if (_sources != null)
            {
                for (int i = 0; i < _sources.Length; i++)
                {
                    var src = _sources[i];
                    if (src == null) continue;

                    src.loop = false;
                    if (src.clip != null)
                    {
                        hasValidClip = true;
                        if (!src.isPlaying)
                        {
                            src.Play();
                        }
                        float pitch = Mathf.Abs(src.pitch) > 0.01f ? Mathf.Abs(src.pitch) : 1.0f;
                        float clipDuration = src.clip.length / pitch;
                        if (clipDuration > maxClipDuration)
                        {
                            maxClipDuration = clipDuration;
                        }
                    }
                }
            }

            if (!hasValidClip)
            {
                Destroy(gameObject);
                return;
            }

            _fallbackEndTime = Time.time + maxClipDuration + 0.1f;
            _initialized = true;
        }

        private void Update()
        {
            if (!_initialized) return;

            if (_followTarget != null)
            {
                transform.position = _followTarget.position;
            }

            bool anyPlaying = false;
            if (_sources != null)
            {
                for (int i = 0; i < _sources.Length; i++)
                {
                    var src = _sources[i];
                    if (src != null && src.isPlaying)
                    {
                        anyPlaying = true;
                        break;
                    }
                }
            }

            // 모든 클립 재생이 끝났거나 최대 클립 길이에 도달한 경우 즉시 정리
            if (!anyPlaying || Time.time >= _fallbackEndTime)
            {
                Destroy(gameObject);
            }
        }
    }

    /// <summary>
    /// 몬스터 공격 패턴 구조화 엔트리
    /// - 모든 VFX / Telegraph / Projectile / Sound를 문자열이 아닌 직접 GameObject 참조로 통일
    /// </summary>
    [Serializable]
    public class MonsterSkillEntry
    {
        [Header("기본 정보")]
        [Tooltip("고유 스킬 식별자 (e.g. minion_dash, turtle_charge, zhu_parabolic, boss_vacuum, boss_cleave, elite_boulder)")]
        public string skillId = "skill_id";

        [Tooltip("스킬 명칭")]
        public string skillName = "스킬 이름";

        [Tooltip("스킬 동작 설명")]
        [TextArea(2, 4)]
        public string description = "스킬 상세 설명";

        [Tooltip("스킬 패턴 유형")]
        public MonsterSkillType skillType = MonsterSkillType.MeleeAttack;

        [Header("사거리 및 발동 조건")]
        [Tooltip("스킬 발동 최소 사거리 (m)")]
        public float minRange = 0f;

        [Tooltip("스킬 발동 최대 사거리 (m)")]
        public float maxRange = 5.0f;

        [Header("공격력 및 쿨다운")]
        [Tooltip("공격력 계수 (몬스터 기본 공격력의 배수)")]
        public float damageMultiplier = 1.0f;

        [Tooltip("고정 피해량 (0보다 크면 계수 대신 고정 피해 적용)")]
        public float fixedDamage = 0f;

        [Tooltip("스킬 재사용 대기시간 (초)")]
        public float cooldown = 3.0f;

        [Tooltip("피격 시 플레이어 넉백 파워 (m)")]
        public float knockbackPower = 2.0f;

        [Header("전조(Telegraph) 및 범위(AOE)")]
        [Tooltip("스킬 시전 전 예고 장판 표시 시간 (초)")]
        public float telegraphDuration = 0.4f;

        [Tooltip("전조 장판 형태 (None, Line, Circle, Sector, Box)")]
        public string telegraphShape = "Circle";

        [Tooltip("전조 장판 표시 색상")]
        public Color telegraphColor = new Color(1.0f, 0.25f, 0.15f, 0.65f);

        [Tooltip("단검 패링(튕겨내기) 가능 여부")]
        public bool isParryable = false;

        [Tooltip("타격/폭발/장판 반경 (m)")]
        public float aoeRadius = 1.5f;

        [Tooltip("부채꼴 타격 각도 (도, Sector 형태일 때)")]
        public float aoeAngle = 90.0f;

        [Tooltip("공격 후 이동 불가 후딜레이 시간 (초)")]
        public float postAttackDelay = 0.3f;

        // 하위 호환 및 편의 접근자 (maxRange, aoeAngle, postAttackDelay 연동)
        public float range { get => maxRange; set => maxRange = value; }
        public float angle { get => aoeAngle; set => aoeAngle = value; }
        public float recoveryTime { get => postAttackDelay; set => postAttackDelay = value; }
        public float windupTime { get => telegraphDuration; set => telegraphDuration = value; }

        [Header("돌진 및 기동 (Dash / Charge)")]
        [Tooltip("돌진 이동 거리 (m)")]
        public float dashDistance = 5.0f;

        [Tooltip("돌진 이동 속도 (m/s)")]
        public float dashSpeed = 12.0f;

        [Tooltip("돌진 지속 시간 (초)")]
        public float dashDuration = 0.3f;

        [Tooltip("돌진 판정 폭 (m)")]
        public float dashWidth = 0.8f;

        [Header("투사체 및 원거리 (Projectile)")]
        [Tooltip("직선 투사체 비행 속도 (m/s)")]
        public float projectileSpeed = 6.0f;

        [Tooltip("포물선 곡사 비행 시간 (초)")]
        public float flightDuration = 1.8f;

        [Tooltip("포물선 정점 높이 (m)")]
        public float arcHeight = 6.0f;

        [Tooltip("동시 발사체 개수")]
        public int projectileCount = 1;

        [Tooltip("다중 발사 시 탄퍼짐 각도 (도)")]
        public float spreadAngle = 0f;

        [Tooltip("관통 횟수 (0이면 단일 적중 소멸)")]
        public int pierceCount = 0;

        [Tooltip("투사체의 패링 분류 (Normal: 기본 푸른/초록 패링가능, Enhanced: 강화 황금색 2배피해, Unparryable: 패링불가 붉은색)")]
        public Combat.ProjectileParryType projectileParryType = Combat.ProjectileParryType.Normal;

        [Header("직접 게임오브젝트 참조 (Projectile / Telegraph / VFX)")]
        [Tooltip("투사체 또는 소환 스킬 오브젝트 프리팹 (직접 GameObject 등록)")]
        public GameObject projectilePrefab;

        [Tooltip("파편/보조 투사체 프리팹 (예: 바위 파괴 시 튀는 RockShard 등 직접 GameObject 등록)")]
        public GameObject subProjectilePrefab;

        [Tooltip("시전 전조 장판(Telegraph) 프리팹 (직접 GameObject 등록)")]
        public GameObject telegraphPrefab;

        [Tooltip("시전/발사구 머즐 이펙트 프리팹 (직접 GameObject 등록)")]
        public GameObject castVfxPrefab;

        [Tooltip("비행/돌진 궤적 트레일 이펙트 프리팹 (직접 GameObject 등록)")]
        public GameObject trailVfxPrefab;

        [Tooltip("적중/착지/폭발 충격 이펙트 프리팹 (직접 GameObject 등록)")]
        public GameObject impactVfxPrefab;

        [Tooltip("단검 패링/반사 성공 시 스파크 이펙트 프리팹 (직접 GameObject 등록)")]
        public GameObject parryVfxPrefab;

        [Header("직접 게임오브젝트 참조 (Sound - 1회성 및 루프 별도 세팅)")]
        [Tooltip("스킬 시전/발사 시 클립 종료까지 1회성으로 재생할 사운드 GameObject")]
        public GameObject castSoundPrefab;

        [Tooltip("스킬 적중/착탄/폭발 시 클립 종료까지 1회성으로 재생할 사운드 GameObject")]
        public GameObject impactSoundPrefab;

        [Tooltip("스킬 유지(돌진/비행/회전 등) 중 루프 사운드 사용 여부")]
        public bool enableLoopSound = false;

        [Tooltip("스킬 유지(돌진/비행/회전 등) 중 반복 재생할 루프 사운드 GameObject")]
        public GameObject loopSoundPrefab;

        /// <summary>
        /// 직접 등록된 투사체 프리팹 반환
        /// </summary>
        public GameObject GetProjectilePrefab(bool logErrorOnFail = false)
        {
            if (projectilePrefab != null) return projectilePrefab;
            if (logErrorOnFail)
            {
                Debug.LogWarning($"[MonsterSkillTable] 스킬 '{skillId}' ({skillName})의 projectilePrefab이 비어 있습니다. MonsterSkillTable.asset에서 GameObject를 연결해주세요.");
            }
            return null;
        }

        /// <summary>
        /// 직접 등록된 보조 투사체/파편 프리팹 반환
        /// </summary>
        public GameObject GetSubProjectilePrefab(bool logErrorOnFail = false)
        {
            return subProjectilePrefab;
        }

        /// <summary>
        /// 직접 등록된 텔레그래프 프리팹 반환
        /// </summary>
        public GameObject GetTelegraphPrefab(bool logErrorOnFail = false)
        {
            return telegraphPrefab;
        }

        /// <summary>
        /// 직접 등록된 시전/머즐 이펙트 프리팹 반환
        /// </summary>
        public GameObject GetCastVfxPrefab(bool logErrorOnFail = false)
        {
            return castVfxPrefab;
        }

        /// <summary>
        /// 직접 등록된 트레일 이펙트 프리팹 반환
        /// </summary>
        public GameObject GetTrailVfxPrefab(bool logErrorOnFail = false)
        {
            return trailVfxPrefab;
        }

        /// <summary>
        /// 직접 등록된 충격/폭발 이펙트 프리팹 반환
        /// </summary>
        public GameObject GetImpactVfxPrefab(bool logErrorOnFail = false)
        {
            return impactVfxPrefab;
        }

        /// <summary>
        /// 직접 등록된 패링 이펙트 프리팹 반환
        /// </summary>
        public GameObject GetParryVfxPrefab(bool logErrorOnFail = false)
        {
            return parryVfxPrefab;
        }
    }

    /// <summary>
    /// 몬스터 스킬 패턴 테이블 (ScriptableObject)
    /// - 모든 몬스터의 특수 스킬/공격 패턴을 중앙 집중 구조화하여 보관
    /// </summary>
    [CreateAssetMenu(fileName = "MonsterSkillTable", menuName = "MythOfGuotaji/Tables/MonsterSkillTable")]
    public class MonsterSkillTableSO : ScriptableObject
    {
        public List<MonsterSkillEntry> skills = new();

        public MonsterSkillEntry GetSkill(string skillId)
        {
            if (string.IsNullOrEmpty(skillId)) return null;
            var found = skills.Find(s => s != null && s.skillId.Equals(skillId, StringComparison.OrdinalIgnoreCase));
            if (found != null) return found;

            return GetDefaultSkill(skillId);
        }

        public MonsterSkillEntry GetSkillForMonster(MonsterType type)
        {
            string skillId = type switch
            {
                MonsterType.Minion => "minion_dash",
                MonsterType.Minion_Archer => "toxic_spit",
                MonsterType.Minion_BigMon => "turtle_charge",
                MonsterType.Minion_Zhu => "zhu_parabolic",
                MonsterType.Elite => "elite_boulder",
                MonsterType.FoxBoss => "boss_vacuum",
                MonsterType.Dokkaebi_Fire => "dokkaebi_fireball",
                MonsterType.Boar_Charger => "boar_charge",
                MonsterType.Spider_Tether => "spider_web_tether",
                MonsterType.Mud_Slime => "mud_puddle",
                MonsterType.Phantom_Fox => "phantom_decoy",
                MonsterType.Iron_Shield => "iron_shield_stance",
                MonsterType.Energy_Absorber => "energy_absorb",
                MonsterType.Bomb_Sparrow => "sparrow_kamikaze",
                MonsterType.Totem_Turret => "totem_turret_barrage",
                _ => "minion_dash"
            };
            return GetSkill(skillId);
        }

        public static MonsterSkillEntry GetDefaultSkill(string skillId)
        {
            return skillId?.ToLower() switch
            {
                "minion_dash" => new MonsterSkillEntry
                {
                    skillId = "minion_dash",
                    skillName = "미니언 5m 돌진 & 충격파",
                    description = "5m 내 접근 시 0.3초 예고 후 즉시 돌진하여 착지 지점 반경 70cm 광역 피해",
                    skillType = MonsterSkillType.MinionDash,
                    minRange = 1.5f,
                    maxRange = 5.0f,
                    damageMultiplier = 1.0f,
                    fixedDamage = 6.0f,
                    cooldown = 1.0f,
                    telegraphDuration = 0.3f,
                    telegraphShape = "StretchBox",
                    aoeRadius = 0.7f,
                    dashDistance = 5.0f,
                    dashSpeed = 16.0f,
                    dashDuration = 0.3f,
                    dashWidth = 0.5f,
                    knockbackPower = 1.5f
                },
                "turtle_charge" => new MonsterSkillEntry
                {
                    skillId = "turtle_charge",
                    skillName = "황동 거북 20m 가속 돌진 분쇄",
                    description = "플레이어를 향해 0.4초 예고 후 20m 거리를 가속 돌진하여 충돌 시 3m 충격파 발생",
                    skillType = MonsterSkillType.TurtleCharge,
                    minRange = 2.0f,
                    maxRange = 20.0f,
                    damageMultiplier = 1.5f,
                    fixedDamage = 14.0f,
                    cooldown = 3.5f,
                    telegraphDuration = 0.4f,
                    telegraphShape = "StretchBox",
                    aoeRadius = 3.0f,
                    dashDistance = 20.0f,
                    dashSpeed = 10.0f,
                    dashDuration = 3.0f,
                    dashWidth = 1.5f,
                    knockbackPower = 4.0f
                },
                "toxic_spit" => new MonsterSkillEntry
                {
                    skillId = "toxic_spit",
                    skillName = "맹독충 15m 원거리 독침 사격",
                    description = "사거리 15m에서 6m/s 속도의 독침을 직선 발사하고 6m 이내 접근 시 카이팅 후퇴",
                    skillType = MonsterSkillType.RangedStraight,
                    minRange = 2.0f,
                    maxRange = 15.0f,
                    damageMultiplier = 1.0f,
                    fixedDamage = 7.0f,
                    cooldown = 0.7f,
                    telegraphDuration = 0.3f,
                    telegraphShape = "TargetCircle",
                    projectileSpeed = 6.0f,
                    projectileCount = 1
                },
                "zhu_parabolic" => new MonsterSkillEntry
                {
                    skillId = "zhu_parabolic",
                    skillName = "Zhu 박격포 20m 포물선 곡사 투척",
                    description = "벽을 무시하고 플레이어 발밑으로 5m 원형 장판 표시 후 2초간 6m 높이로 곡사 투척, 착지 시 2.5m 대폭발",
                    skillType = MonsterSkillType.RangedParabolic,
                    minRange = 2.5f,
                    maxRange = 20.0f,
                    damageMultiplier = 1.3f,
                    fixedDamage = 18.0f,
                    cooldown = 3.0f,
                    telegraphDuration = 0.5f,
                    telegraphShape = "TargetCircle",
                    aoeRadius = 2.5f,
                    flightDuration = 2.0f,
                    arcHeight = 6.0f
                },
                "elite_boulder" => new MonsterSkillEntry
                {
                    skillId = "elite_boulder",
                    skillName = "열쇠 엘리트 바위 굴리기",
                    description = "지면을 굴러가며 경로상의 모든 것을 짓밟는 거대한 암석을 투척하여 18 피해 및 넉백",
                    skillType = MonsterSkillType.EliteBoulder,
                    minRange = 2.0f,
                    maxRange = 9.0f,
                    damageMultiplier = 1.5f,
                    fixedDamage = 18.0f,
                    cooldown = 5.0f,
                    telegraphDuration = 0.6f,
                    telegraphShape = "StretchBox",
                    aoeRadius = 2.0f,
                    projectileSpeed = 8.0f,
                    knockbackPower = 5.0f
                },
                "boss_vacuum" => new MonsterSkillEntry
                {
                    skillId = "boss_vacuum",
                    skillName = "천년여우 1번: 영혼 흡인 소용돌이",
                    description = "보스 중심 5m 범위 내의 플레이어를 1.2초 동안 끌어당기며 이동을 봉쇄하고 지속 요화 피해",
                    skillType = MonsterSkillType.BossVacuum,
                    minRange = 0f,
                    maxRange = 5.5f,
                    damageMultiplier = 1.2f,
                    fixedDamage = 15.0f,
                    cooldown = 6.0f,
                    telegraphDuration = 0.5f,
                    telegraphShape = "StretchArc90",
                    aoeAngle = 90.0f,
                    aoeRadius = 5.0f,
                    knockbackPower = -3.0f
                },
                "boss_cleave" => new MonsterSkillEntry
                {
                    skillId = "boss_cleave",
                    skillName = "천년여우 2번: 120도 부채꼴 요화 분쇄",
                    description = "전방 3.5m 120도 부채꼴 영역으로 날카로운 요호의 발톱을 휘둘러 28 피해 및 3m 넉백",
                    skillType = MonsterSkillType.BossCleave,
                    minRange = 0f,
                    maxRange = 3.5f,
                    damageMultiplier = 2.0f,
                    fixedDamage = 28.0f,
                    cooldown = 4.0f,
                    telegraphDuration = 0.4f,
                    telegraphShape = "StretchArc180",
                    aoeRadius = 3.5f,
                    aoeAngle = 180.0f,
                    knockbackPower = 3.0f
                },
                "dokkaebi_fireball" => new MonsterSkillEntry
                {
                    skillId = "dokkaebi_fireball",
                    skillName = "도깨비불 핑퐁 패링 화염구",
                    description = "원거리에서 5m/s 속도의 푸른 화염구를 발사. 단검으로 패링 시 2배 속도로 반사되어 적에게 직격하며 3초간 그로기 및 보호막 분쇄",
                    skillType = MonsterSkillType.ParryReflect,
                    minRange = 3.0f,
                    maxRange = 14.0f,
                    damageMultiplier = 1.0f,
                    fixedDamage = 12.0f,
                    cooldown = 2.5f,
                    telegraphShape = "None",
                    projectileSpeed = 5.0f,
                    aoeRadius = 0.8f
                },
                "boar_charge" => new MonsterSkillEntry
                {
                    skillId = "boar_charge",
                    skillName = "저돌맹진 멧돼지 돌진 분쇄",
                    description = "0.35초 예고 후 14m/s 속도로 15m를 직선 돌진. 돌진 직전 단검 패링 시 벽에 충돌하며 기절, 또는 슬라이딩 무적으로 회피",
                    skillType = MonsterSkillType.TurtleCharge,
                    minRange = 2.5f,
                    maxRange = 15.0f,
                    damageMultiplier = 1.6f,
                    fixedDamage = 16.0f,
                    cooldown = 3.0f,
                    telegraphDuration = 0.35f,
                    telegraphShape = "StretchBox",
                    dashDistance = 15.0f,
                    dashSpeed = 14.0f,
                    dashDuration = 1.1f,
                    dashWidth = 1.2f,
                    aoeRadius = 1.5f,
                    knockbackPower = 4.0f
                },
                "spider_web_tether" => new MonsterSkillEntry
                {
                    skillId = "spider_web_tether",
                    skillName = "요선 거미 영혼 사슬 견인",
                    description = "12m 거리에서 끈적한 명주실 사슬을 던져 플레이어를 자신 앞으로 끌어당김. 슬라이딩 무적 대시로 사슬을 즉시 절단 탈출 가능",
                    skillType = MonsterSkillType.TetherPull,
                    minRange = 3.0f,
                    maxRange = 12.0f,
                    damageMultiplier = 0.5f,
                    fixedDamage = 8.0f,
                    cooldown = 4.5f,
                    telegraphDuration = 0.3f,
                    telegraphShape = "TargetConnect",
                    aoeRadius = 1.0f,
                    knockbackPower = -3.5f
                },
                "mud_puddle" => new MonsterSkillEntry
                {
                    skillId = "mud_puddle",
                    skillName = "진흙 이괴 늪지 슬로우 장판",
                    description = "플레이어 발밑에 반경 3m의 진흙 늪지를 소환하여 4초간 이동속도를 60% 감속시키고 카이팅을 봉쇄",
                    skillType = MonsterSkillType.AOESlam,
                    minRange = 1.0f,
                    maxRange = 9.0f,
                    damageMultiplier = 0.4f,
                    fixedDamage = 5.0f,
                    cooldown = 5.0f,
                    telegraphDuration = 0.5f,
                    telegraphShape = "TargetCircle",
                    aoeRadius = 3.0f
                },
                "phantom_decoy" or "phantom_illusion" => new MonsterSkillEntry
                {
                    skillId = "phantom_decoy",
                    skillName = "환영 여우귀 3방향 분신술",
                    description = "피격 시 연막을 터뜨리며 본체 1마리와 체력 1짜리 가짜 환영 2마리로 3방향 순간 분신. 관통 화살이나 단검 베기로 한 번에 분쇄 가능",
                    skillType = MonsterSkillType.SummonMinions,
                    minRange = 0f,
                    maxRange = 10.0f,
                    cooldown = 7.0f,
                    telegraphDuration = 0.1f,
                    telegraphShape = "None",
                    aoeRadius = 2.0f
                },
                "iron_shield_stance" or "iron_shield_guard" => new MonsterSkillEntry
                {
                    skillId = "iron_shield_stance",
                    skillName = "흑철 방패 가드 (풀차징 필수)",
                    description = "전면에 철갑 방패를 세워 일반 화살을 100% 도탄(0 피해). 오직 3단계 풀차징 관통 화살로만 방패를 분쇄(Armor Break)하여 격파 가능",
                    skillType = MonsterSkillType.ShieldGuard,
                    minRange = 0f,
                    maxRange = 4.0f,
                    cooldown = 2.0f,
                    telegraphShape = "None",
                    aoeAngle = 120.0f
                },
                "energy_absorb" or "energy_absorb_surge" => new MonsterSkillEntry
                {
                    skillId = "energy_absorb",
                    telegraphShape = "None",
                    skillName = "영력 흡수 괴물 (차징 금지)",
                    description = "강력한 차징 샷을 맞으면 에너지를 흡수하여 체력을 회복하고 1.4배 폭주. 오직 빠른 1단계 일반 연사나 단검 평타로만 피해 누적 처치",
                    skillType = MonsterSkillType.EnergyAbsorb,
                    minRange = 0f,
                    maxRange = 3.0f,
                    cooldown = 1.5f
                },
                "sparrow_kamikaze" or "kamikaze_dive" => new MonsterSkillEntry
                {
                    skillId = "sparrow_kamikaze",
                    skillName = "자폭 참새 떼 12m/s 급강하",
                    description = "체력 10의 초고속 자폭 요괴. 12m/s로 날아와 2m 자폭 폭발을 일으킴. 버스트 사격 요격 또는 슬라이딩 0.5초 무적 회피가 핵심",
                    skillType = MonsterSkillType.KamikazeExplode,
                    minRange = 0.5f,
                    maxRange = 12.0f,
                    damageMultiplier = 1.5f,
                    fixedDamage = 18.0f,
                    cooldown = 1.0f,
                    telegraphShape = "TargetConnect",
                    dashDistance = 8.0f,
                    dashSpeed = 12.0f,
                    dashDuration = 0.6f,
                    aoeRadius = 2.0f
                },
                "totem_turret_barrage" or "turret_quad_barrage" => new MonsterSkillEntry
                {
                    skillId = "totem_turret_barrage",
                    skillName = "사방신 토템 고정 포대 탄막",
                    description = "이동하지 않는 고정형 석상. 4방향으로 회전형 영력 탄환을 지속 발사. 모든 탄환은 단검으로 패링하여 역폭파 가능",
                    skillType = MonsterSkillType.TurretBarrage,
                    minRange = 2.0f,
                    maxRange = 25.0f,
                    damageMultiplier = 1.0f,
                    fixedDamage = 8.0f,
                    cooldown = 0.6f,
                    telegraphShape = "None",
                    projectileSpeed = 7.0f,
                    projectileCount = 8,
                    spreadAngle = 45.0f,
                    projectileParryType = Combat.ProjectileParryType.Enhanced
                },
                _ => null
            };
        }

        public static readonly string[] DefaultSkillIds = new[]
        {
            "minion_dash",
            "toxic_spit",
            "turtle_charge",
            "zhu_parabolic",
            "elite_boulder",
            "boss_vacuum",
            "boss_cleave",
            "dokkaebi_fireball",
            "boar_charge",
            "spider_web_tether",
            "mud_puddle",
            "phantom_decoy",
            "iron_shield_stance",
            "energy_absorb",
            "sparrow_kamikaze",
            "totem_turret_barrage"
        };

        public void PopulateDefaults()
        {
            if (skills == null) skills = new List<MonsterSkillEntry>();
            foreach (var id in DefaultSkillIds)
            {
                if (!skills.Exists(s => s != null && s.skillId.Equals(id, StringComparison.OrdinalIgnoreCase)))
                {
                    var def = GetDefaultSkill(id);
                    if (def != null) skills.Add(def);
                }
            }
        }
    }
}
