using System.Collections.Generic;
using MythOfGuotaji.Combat;
using UnityEngine;

namespace MythOfGuotaji.UI
{
    /// <summary>
    /// 공격 데미지 카운터 UI 프리팹을 관리하고 풀링하여 스폰하는 매니저
    /// </summary>
    public class DamageCounterManager : MonoBehaviour
    {
        private static DamageCounterManager s_instance;
        public static DamageCounterManager Instance
        {
            get
            {
                if (s_instance == null)
                {
                    var existing = FindAnyObjectByType<DamageCounterManager>();
                    if (existing != null)
                    {
                        s_instance = existing;
                    }
                    else
                    {
                        var go = new GameObject("[DamageCounterManager]");
                        s_instance = go.AddComponent<DamageCounterManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return s_instance;
            }
        }

        [Header("Prefab References (Individual or Base)")]
        [SerializeField] private GameObject _normalPrefab;
        [SerializeField] private GameObject _reflectedPrefab;
        [SerializeField] private GameObject _criticalPrefab;
        [SerializeField] private GameObject _basePrefab;

        private readonly Dictionary<DamageCounterType, Queue<AttackDamageCounter>> _pools = new();

        private void Awake()
        {
            if (s_instance != null && s_instance != this)
            {
                Destroy(gameObject);
                return;
            }
            s_instance = this;
            DontDestroyOnLoad(gameObject);

            LoadPrefabsIfNull();
        }

        private void LoadPrefabsIfNull()
        {
            // 개별 프리팹 로드 시도
            if (_normalPrefab == null)
            {
                _normalPrefab = Resources.Load<GameObject>("GUI/AttackDamageCounter_Normal") ??
                                Resources.Load<GameObject>("Prefabs/GUI/AttackDamageCounter_Normal");
            }
            if (_reflectedPrefab == null)
            {
                _reflectedPrefab = Resources.Load<GameObject>("GUI/AttackDamageCounter_Reflected") ??
                                   Resources.Load<GameObject>("Prefabs/GUI/AttackDamageCounter_Reflected");
            }
            if (_criticalPrefab == null)
            {
                _criticalPrefab = Resources.Load<GameObject>("GUI/AttackDamageCounter_Critical") ??
                                  Resources.Load<GameObject>("Prefabs/GUI/AttackDamageCounter_Critical");
            }
            if (_basePrefab == null)
            {
                _basePrefab = Resources.Load<GameObject>("GUI/AttackDamageCounter") ??
                              Resources.Load<GameObject>("Prefabs/GUI/AttackDamageCounter");
            }
        }

        /// <summary>
        /// 데미지 카운터 표시 요청 진입점
        /// </summary>
        /// <param name="damage">입힌 피해량</param>
        /// <param name="info">데미지 정보 (크리티컬, 반사, 공격자)</param>
        /// <param name="monsterBasePosition">몬스터의 바닥 피벗 위치</param>
        public static void ShowDamage(float damage, DamageInfo info, Vector3 monsterBasePosition)
        {
            if (damage <= 0f) return;

            // 플레이어가 가한 공격(일반/크리티컬) 또는 패링 반사 공격인지 확인
            bool isFromPlayer = info.isReflected || (info.attacker != null && (
                info.attacker.CompareTag("Player") ||
                info.attacker.GetComponentInParent<Actor.PlayerController>() != null
            ));

            if (!isFromPlayer) return;

            // 타입 우선순위:
            // 4. 일반/반사의 크리티컬 = 붉은색 숫자 + Outline, size = 45
            // 3. 패링해서 반격한 공격 = 노란색 숫자 + Outline, size = 35
            // 2. 화살/단검 일반공격 = 흰색 숫자 + Outline, size = 30
            DamageCounterType type;
            if (info.isCritical)
            {
                type = DamageCounterType.Critical;
            }
            else if (info.isReflected)
            {
                type = DamageCounterType.Reflected;
            }
            else
            {
                type = DamageCounterType.Normal;
            }

            // 데미지 숫자는 몬스터의 바닥부터 3미터 위에 생성
            Vector3 spawnPos = monsterBasePosition + Vector3.up * 3.0f;

            // 연속 피격 시 텍스트 겹침 완화를 위한 가벼운 수평 오프셋 (+-0.2m)
            spawnPos.x += Random.Range(-0.2f, 0.2f);
            spawnPos.z += Random.Range(-0.2f, 0.2f);

            Instance.Spawn(damage, type, spawnPos);
        }

        public void Spawn(float damage, DamageCounterType type, Vector3 spawnPosition)
        {
            AttackDamageCounter counter = GetOrCreateCounter(type);
            if (counter == null) return;

            counter.gameObject.SetActive(true);
            counter.Initialize(damage, type, spawnPosition, OnCounterComplete);
        }

        private AttackDamageCounter GetOrCreateCounter(DamageCounterType type)
        {
            if (!_pools.TryGetValue(type, out var pool))
            {
                pool = new Queue<AttackDamageCounter>();
                _pools[type] = pool;
            }

            while (pool.Count > 0)
            {
                var candidate = pool.Dequeue();
                if (candidate != null)
                {
                    return candidate;
                }
            }

            // 새 인스턴스 생성
            GameObject prefab = GetPrefabForType(type);
            if (prefab == null)
            {
                // 프리팹이 아직 생성되지 않았거나 없는 경우 런타임 폴백 생성
                GameObject runtimeGo = CreateRuntimeFallbackCounter(type);
                return runtimeGo.GetComponent<AttackDamageCounter>();
            }

            GameObject instanceGo = Instantiate(prefab, transform);
            var counter = instanceGo.GetComponent<AttackDamageCounter>();
            if (counter == null)
            {
                counter = instanceGo.AddComponent<AttackDamageCounter>();
            }
            return counter;
        }

        private GameObject GetPrefabForType(DamageCounterType type)
        {
            LoadPrefabsIfNull();

            switch (type)
            {
                case DamageCounterType.Critical:
                    if (_criticalPrefab != null) return _criticalPrefab;
                    break;
                case DamageCounterType.Reflected:
                    if (_reflectedPrefab != null) return _reflectedPrefab;
                    break;
                case DamageCounterType.Normal:
                    if (_normalPrefab != null) return _normalPrefab;
                    break;
            }

            return _basePrefab;
        }

        private void OnCounterComplete(AttackDamageCounter counter)
        {
            if (counter == null) return;
            counter.gameObject.SetActive(false);

            if (!_pools.TryGetValue(counter.CounterType, out var pool))
            {
                pool = new Queue<AttackDamageCounter>();
                _pools[counter.CounterType] = pool;
            }
            pool.Enqueue(counter);
        }

        /// <summary>
        /// 프리팹 에셋이 에디터/빌드에 존재하지 않는 경우를 대비한 무중단 런타임 폴백 생성기
        /// </summary>
        private GameObject CreateRuntimeFallbackCounter(DamageCounterType type)
        {
            var root = new GameObject($"[Fallback_DamageCounter_{type}]");
            root.transform.SetParent(transform);

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var canvasGroup = root.AddComponent<CanvasGroup>();

            var rt = root.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200f, 60f);
            root.transform.localScale = Vector3.one * 0.02f; // 월드 스케일 적정화

            var textGo = new GameObject("DamageText");
            textGo.transform.SetParent(root.transform, false);
            var textRt = textGo.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;

            var tmp = textGo.AddComponent<TMPro.TextMeshProUGUI>();
            tmp.alignment = TMPro.TextAlignmentOptions.Center;

            var counter = root.AddComponent<AttackDamageCounter>();
            counter.ApplyStyle(type);

            return root;
        }
    }
}
