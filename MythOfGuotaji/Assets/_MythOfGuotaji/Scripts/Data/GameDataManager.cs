using UnityEngine;

namespace MythOfGuotaji.Data
{
    /// <summary>
    /// 게임 전역의 밸런스 테이블 및 프리팹 데이터베이스를 총괄 관리하는 싱글톤
    /// - ScriptableObject 데이터 및 프리팹 로드
    /// - 수치 테이블화 및 런타임 조회 지원
    /// </summary>
    public class GameDataManager : MonoBehaviour
    {
        public static GameDataManager Instance { get; private set; }

        [Header("Databases & Tables")]
        [SerializeField] private PrefabDatabaseSO _prefabDatabase;
        [SerializeField] private PlayerStatsTableSO _playerStatsTable;
        [SerializeField] private WeaponStatsTableSO _weaponStatsTable;
        [SerializeField] private MonsterStatsTableSO _monsterStatsTable;
        [SerializeField] private MonsterSkillVFXTableSO _monsterSkillVFXTable;
        [SerializeField] private DungeonConfigTableSO _dungeonConfigTable;
        [SerializeField] private HelperDataSO _helperData;
        [SerializeField] private InRunBoonTableSO _inRunBoonTable;
        [SerializeField] private BoonRarityTableSO _boonRarityTable;

        public PrefabDatabaseSO Prefabs => _prefabDatabase;
        public PlayerStatsTableSO PlayerStats => _playerStatsTable;
        public WeaponStatsTableSO WeaponStats => _weaponStatsTable;
        public MonsterStatsTableSO MonsterStats => _monsterStatsTable;
        public MonsterSkillVFXTableSO MonsterSkillVFX => _monsterSkillVFXTable;
        public DungeonConfigTableSO DungeonConfig => _dungeonConfigTable;
        public HelperDataSO HelperData => _helperData;
        public InRunBoonTableSO InRunBoonTable => _inRunBoonTable;
        public BoonRarityTableSO BoonRarityTable => _boonRarityTable;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadTablesIfNull();
        }

        public void LoadTablesIfNull()
        {
            if (_prefabDatabase == null)
            {
                _prefabDatabase = Resources.Load<PrefabDatabaseSO>("Data/PrefabDatabase");
            }
            if (_playerStatsTable == null)
            {
                _playerStatsTable = Resources.Load<PlayerStatsTableSO>("Data/PlayerStatsTable");
            }
            if (_weaponStatsTable == null)
            {
                _weaponStatsTable = Resources.Load<WeaponStatsTableSO>("Data/WeaponStatsTable");
            }
            if (_monsterStatsTable == null)
            {
                _monsterStatsTable = Resources.Load<MonsterStatsTableSO>("Data/MonsterStatsTable");
            }
            if (_monsterSkillVFXTable == null)
            {
                _monsterSkillVFXTable = Resources.Load<MonsterSkillVFXTableSO>("Data/MonsterSkillVFXTable");
            }
            if (_dungeonConfigTable == null)
            {
                _dungeonConfigTable = Resources.Load<DungeonConfigTableSO>("Data/DungeonConfigTable");
            }
            if (_helperData == null)
            {
                _helperData = Resources.Load<HelperDataSO>("Data/HelperData");
            }
            if (_inRunBoonTable == null)
            {
                _inRunBoonTable = Resources.Load<InRunBoonTableSO>("Data/InRunBoonTable");
            }
            if (_boonRarityTable == null)
            {
                _boonRarityTable = Resources.Load<BoonRarityTableSO>("Data/BoonRarityTable");
            }
        }
    }
}
