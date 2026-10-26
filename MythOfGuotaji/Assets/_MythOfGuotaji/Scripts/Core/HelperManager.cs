using MythOfGuotaji.Actor;
using MythOfGuotaji.Data;
using UnityEngine;

namespace MythOfGuotaji.Core
{
    /// <summary>
    /// 동료 헬퍼(Helper)의 소환, 선택, 라이프사이클을 총괄하는 매니저
    /// - 향후 스테이지 진입 시 원하는 헬퍼(영혼 토끼, 도깨비불, 조롱박 등)를 선택할 수 있도록 설계
    /// </summary>
    public class HelperManager : MonoBehaviour
    {
        public static HelperManager Instance { get; private set; }

        [Header("Helper Selection")]
        [SerializeField] private string _selectedHelperId = "Helper_SpiritRabbit";

        private HelperCompanion _currentHelper;

        public HelperCompanion CurrentHelper => _currentHelper;
        public string SelectedHelperId => _selectedHelperId;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            EnsureHelperSpawned();
        }

        public void SelectHelper(string helperId)
        {
            _selectedHelperId = helperId;
            Debug.Log($"<color=cyan>[HelperManager] 활성 헬퍼 변경: {_selectedHelperId}</color>");

            // 이미 스폰되어 있다면 즉시 새 헬퍼로 교체
            if (_currentHelper != null)
            {
                Destroy(_currentHelper.gameObject);
                _currentHelper = null;
                EnsureHelperSpawned();
            }
        }

        public HelperCompanion EnsureHelperSpawned()
        {
            if (_currentHelper != null) return _currentHelper;

            var existing = FindAnyObjectByType<HelperCompanion>();
            if (existing != null)
            {
                _currentHelper = existing;
                return _currentHelper;
            }

            var player = FindAnyObjectByType<PlayerController>();
            if (player == null) return null; // 인게임 플레이어가 없는 아웃게임 씬에서는 자동 스폰하지 않음

            Vector3 spawnPos = player.transform.position - player.transform.forward * 2f + Vector3.up * 0.8f;

            GameObject helperPrefab = null;
            if (GameDataManager.Instance != null && GameDataManager.Instance.Prefabs != null)
            {
                helperPrefab = PrefabDatabaseSO.ResolvePrefab(ref GameDataManager.Instance.Prefabs.helperCompanionPrefab, "Prefabs/Helper_SpiritRabbit");
            }
            else
            {
                helperPrefab = Resources.Load<GameObject>("Prefabs/Helper_SpiritRabbit");
            }

            GameObject helperObj;
            if (helperPrefab != null)
            {
                helperObj = Instantiate(helperPrefab, spawnPos, Quaternion.identity);
                helperObj.name = "Helper_SpiritRabbit";
            }
            else
            {
                helperObj = new GameObject("[HelperCompanion]");
                helperObj.transform.position = spawnPos;
            }

            _currentHelper = helperObj.GetComponent<HelperCompanion>();
            if (_currentHelper == null) _currentHelper = helperObj.AddComponent<HelperCompanion>();

            Debug.Log("<color=cyan><b>[HelperManager]</b> 동료 헬퍼 프리팹이 플레이어 곁에 소환되었습니다.</color>");
            return _currentHelper;
        }

        public void RepositionBehindPlayer()
        {
            if (_currentHelper == null)
            {
                EnsureHelperSpawned();
                return;
            }

            var player = FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                var cc = _currentHelper.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                Vector3 behind = player.transform.position - player.transform.forward * 2f;
                behind.y = 0.85f;
                _currentHelper.transform.position = behind;
                Physics.SyncTransforms();
                if (cc != null) cc.enabled = true;
            }
        }
    }
}
