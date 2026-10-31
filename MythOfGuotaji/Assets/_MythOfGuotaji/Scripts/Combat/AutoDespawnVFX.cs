using System.Collections;
using UnityEngine;
using MythOfGuotaji.Core;

namespace MythOfGuotaji.Combat
{
    /// <summary>
    /// VFX 프리팹이 재생을 마친 후 씬에 남아있지 않도록 자동으로 ObjectPool 반환 또는 Destroy 처리하는 컴포넌트
    /// </summary>
    public class AutoDespawnVFX : MonoBehaviour
    {
        [SerializeField] private float _lifeTime = 0.8f;
        [SerializeField] private string _poolKey = "";

        private Coroutine _despawnRoutine;

        private void Awake()
        {
            if (string.IsNullOrEmpty(_poolKey))
            {
                _poolKey = gameObject.name.Replace("(Clone)", "").Trim();
            }
        }

        private void OnEnable()
        {
            // 파티클 시스템이 있을 경우 활성화 시 깨끗하게 다시 재생
            var particleSystems = GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in particleSystems)
            {
                ps.Clear();
                ps.Play();
            }

            if (_despawnRoutine != null) StopCoroutine(_despawnRoutine);
            _despawnRoutine = StartCoroutine(RoutineDespawn());
        }

        private void OnDisable()
        {
            if (_despawnRoutine != null)
            {
                StopCoroutine(_despawnRoutine);
                _despawnRoutine = null;
            }
        }

        private IEnumerator RoutineDespawn()
        {
            yield return new WaitForSeconds(_lifeTime);

            if (ObjectPoolManager.Instance != null && !string.IsNullOrEmpty(_poolKey))
            {
                ObjectPoolManager.Instance.Despawn(gameObject, _poolKey);
            }
            else
            {
                Destroy(gameObject);
            }
            _despawnRoutine = null;
        }
    }
}
