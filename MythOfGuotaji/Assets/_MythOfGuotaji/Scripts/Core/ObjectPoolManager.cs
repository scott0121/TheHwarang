using System.Collections.Generic;
using UnityEngine;

namespace MythOfGuotaji.Core
{
    /// <summary>
    /// 대규모 투사체, 요괴 몬스터, 이펙트 생성을 위한 고성능 오브젝트 풀 매니저
    /// </summary>
    public class ObjectPoolManager : MonoBehaviour
    {
        public static ObjectPoolManager Instance { get; private set; }

        private readonly Dictionary<string, Queue<GameObject>> _poolDictionary = new();
        private readonly Dictionary<string, GameObject> _prefabDictionary = new();
        private readonly Dictionary<string, Transform> _poolRootDictionary = new();

        private void Awake()
        {
            if (Instance != null && Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// 특정 프리팹을 사전에 풀링
        /// </summary>
        public void Prewarm(GameObject prefab, int count)
        {
            if (prefab == null) return;
            string key = prefab.name;

            if (!_poolDictionary.ContainsKey(key))
            {
                _poolDictionary[key] = new Queue<GameObject>();
                _prefabDictionary[key] = prefab;

                GameObject root = new GameObject($"[Pool_{key}]");
                root.transform.SetParent(transform);
                _poolRootDictionary[key] = root.transform;
            }

            for (int i = 0; i < count; i++)
            {
                GameObject obj = Instantiate(prefab, _poolRootDictionary[key]);
                obj.SetActive(false);
                _poolDictionary[key].Enqueue(obj);
            }
        }

        /// <summary>
        /// 풀에서 인스턴스 가져오기
        /// </summary>
        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null) return null;
            string key = prefab.name;

            GameObject instance = null;
            if (_poolDictionary.TryGetValue(key, out var queue))
            {
                while (queue.Count > 0)
                {
                    var candidate = queue.Dequeue();
                    if (candidate != null)
                    {
                        instance = candidate;
                        break;
                    }
                }
            }

            if (instance == null)
            {
                instance = Instantiate(prefab);
            }

            instance.SetActive(false);
            var cc = instance.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            instance.transform.SetPositionAndRotation(position, rotation);
            Physics.SyncTransforms();

            instance.SetActive(true);
            if (cc != null) cc.enabled = true;
            return instance;
        }

        /// <summary>
        /// 사용 완료된 오브젝트를 풀로 반환
        /// </summary>
        public void Despawn(GameObject instance, string poolKey)
        {
            if (instance == null) return;

            var cc = instance.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            instance.SetActive(false);
            if (_poolRootDictionary.TryGetValue(poolKey, out Transform root) && root != null)
            {
                instance.transform.SetParent(root);
            }

            if (!_poolDictionary.ContainsKey(poolKey))
            {
                _poolDictionary[poolKey] = new Queue<GameObject>();
            }

            _poolDictionary[poolKey].Enqueue(instance);
        }

        /// <summary>
        /// 모든 오브젝트 풀 비우기 (던전 재배치 또는 씬 초기화 시 호출)
        /// </summary>
        public void ClearAll()
        {
            foreach (var kvp in _poolDictionary)
            {
                var q = kvp.Value;
                while (q.Count > 0)
                {
                    var item = q.Dequeue();
                    if (item != null)
                    {
                        Destroy(item);
                    }
                }
            }
            _poolDictionary.Clear();
        }
    }
}
