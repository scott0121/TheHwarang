using System.Collections;
using System.Collections.Generic;
using MythOfGuotaji.Actor;
using MythOfGuotaji.Core;
using UnityEngine;

namespace MythOfGuotaji.Dungeon
{
    /// <summary>
    /// 개별 방 제어기
    /// - 방 규격 및 경계 관리
    /// - 4방향 DoorSocket 제어
    /// - 문 개방 시 해당 방 요괴 1차 공격 개시 & 30초 간격 3웨이브 스폰 시스템
    /// - 한번 열린 문은 영원히 닫히지 않음
    /// </summary>
    public class RoomController : MonoBehaviour
    {
        [Header("Room Coordinates & Type")]
        public Vector2Int gridPosition;
        [SerializeField] private RoomType _roomType = RoomType.Normal;

        [Header("Sockets (N, E, S, W)")]
        [SerializeField] private DoorSocket _socketNorth;
        [SerializeField] private DoorSocket _socketEast;
        [SerializeField] private DoorSocket _socketSouth;
        [SerializeField] private DoorSocket _socketWest;

        [Header("Spawning & States")]
        [SerializeField] private Transform _spawnPoint;
        private bool _isCleared = false;
        private bool _isEncounterActive = false;
        private bool _hasPreSpawned = false;
        private readonly List<GameObject> _spawnedEnemies = new();

        [Header("Wave Encounter Settings")]
        private int _initialMonsterCount = 6;
        private Coroutine _waveCoroutine;
        private bool _wavesCompleted = false;

        [Header("Door Batch Opening")]
        private bool _allDoorsOpened = false;
        private Transform _playerTransform;
        private const float RoomCenterOpenRadius = 10.0f;

        public RoomType RoomType => _roomType;
        public bool IsCleared => _isCleared;
        public bool IsEncounterActive => _isEncounterActive;
        public Vector2Int GridPosition => gridPosition;
        public bool AllDoorsOpened => _allDoorsOpened;

        public int LivingMonsterCount
        {
            get
            {
                _spawnedEnemies.RemoveAll(e => e == null || (e.TryGetComponent<MonsterAI>(out var ai) && ai.IsDead));
                return _spawnedEnemies.Count;
            }
        }

        public void InitializeFogOverlay(float roomSize)
        {
            var overlay = GetComponent<RoomFogOverlay>();
            if (overlay == null)
            {
                overlay = gameObject.AddComponent<RoomFogOverlay>();
            }
            overlay.InitializeOverlay(roomSize);
        }

        private void Awake()
        {
            if (_socketNorth == null || _socketEast == null || _socketSouth == null || _socketWest == null)
            {
                AutoBindSockets();
            }
        }

        public void AutoBindSockets()
        {
            var sockets = GetComponentsInChildren<DoorSocket>(true);
            foreach (var s in sockets)
            {
                switch (s.Direction)
                {
                    case Direction.North: _socketNorth = s; break;
                    case Direction.East: _socketEast = s; break;
                    case Direction.South: _socketSouth = s; break;
                    case Direction.West: _socketWest = s; break;
                }
            }
        }

        public void SetSockets(DoorSocket north, DoorSocket east, DoorSocket south, DoorSocket west)
        {
            _socketNorth = north;
            _socketEast = east;
            _socketSouth = south;
            _socketWest = west;
        }

        public void SetupRoom(Vector2Int gridPos, RoomType type)
        {
            gridPosition = gridPos;
            _roomType = type;
            _isCleared = (_roomType == RoomType.Start);
        }

        private void Update()
        {
            // 1. 새로운 방(Start 제외)의 중심부에서 10미터 원 진입 시 방의 모든 문 일괄 자동 개방
            if (!_allDoorsOpened && _roomType != RoomType.Start)
            {
                if (_playerTransform == null)
                {
                    var player = FindAnyObjectByType<PlayerController>();
                    if (player != null) _playerTransform = player.transform;
                }

                if (_playerTransform != null)
                {
                    Vector3 pPos = _playerTransform.position;
                    pPos.y = transform.position.y;
                    float openRadius = (Data.GameDataManager.Instance != null && Data.GameDataManager.Instance.DungeonConfig != null)
                        ? Data.GameDataManager.Instance.DungeonConfig.roomCenterOpenRadius
                        : RoomCenterOpenRadius;
                    if (Vector3.Distance(transform.position, pPos) <= openRadius)
                    {
                        OpenAllDoorsInRoom();
                    }
                }
            }

            // 2. 전투 진행 중인 방의 몬스터 정리 및 3웨이브 종료 검사
            if (_isEncounterActive)
            {
                _spawnedEnemies.RemoveAll(e => e == null || (e.TryGetComponent<MonsterAI>(out var ai) && ai.IsDead));

                // 3차 웨이브까지 모두 스폰 완료되었고 남은 몬스터가 0마리일 때 방 클리어 완료!
                if (_wavesCompleted && _spawnedEnemies.Count == 0)
                {
                    _isEncounterActive = false;
                    _isCleared = true;
                    Debug.Log($"<color=lime>[RoomController] 방 ({gridPosition.x},{gridPosition.y}) [{_roomType}]의 모든 웨이브 요괴를 소탕했습니다! 방 클리어 완료.</color>");

                    UnlockAllConnectedDoors();

                    if (_roomType == RoomType.Boss)
                    {
                        UI.BossHealthBarUI.Instance?.Hide();
                        if (MazeGenerator.Instance != null)
                        {
                            MazeGenerator.Instance.UnlockBossDoors();
                        }
                    }
                }
            }
        }

        public DoorSocket GetSocket(Direction dir)
        {
            return dir switch
            {
                Direction.North => _socketNorth,
                Direction.East => _socketEast,
                Direction.South => _socketSouth,
                Direction.West => _socketWest,
                _ => null
            };
        }

        public void SetSocketState(Direction dir, DoorState state)
        {
            var socket = GetSocket(dir);
            if (socket != null)
            {
                socket.SetState(state);
            }
        }

        /// <summary>
        /// 던전 생성 시 캐릭터가 주변 방에 도착하기 전 미리 요괴들을 방에 스폰해둠
        /// </summary>
        public void PreSpawnMonsters()
        {
            if (_hasPreSpawned) return;
            _hasPreSpawned = true;

            switch (_roomType)
            {
                case RoomType.Start:
                    _isCleared = true;
                    break;
                case RoomType.Normal:
                    SpawnNormalHorde();
                    break;
                case RoomType.Trap:
                    SpawnTrapWave();
                    break;
                case RoomType.KeyRoom:
                    EnsureKeyEliteSpawned();
                    break;
                case RoomType.Boss:
                    EnsureBossSpawned();
                    break;
            }
        }

        /// <summary>
        /// 방의 모든 문을 일괄 자동 개방 (영구 개방):
        /// - 문 가까이 갔을 때 혹은 새로운 방의 중심부에서 10미터 원에 진입했을 때
        /// - 해당 방에 달린 모든 문(ex: 문 3개)을 한꺼번에 동시 영구 개방
        /// </summary>
        public void OpenAllDoorsInRoom()
        {
            if (_allDoorsOpened) return;
            _allDoorsOpened = true;

            DoorSocket[] sockets = { _socketNorth, _socketEast, _socketSouth, _socketWest };
            foreach (var socket in sockets)
            {
                if (socket == null || socket.CurrentState == DoorState.Wall) continue;

                // 보스방 황금 봉인의 경우 열쇠가 없는 상태에서는 강제 개방 금지
                if (socket.CurrentState == DoorState.BossLock && (GameManager.Instance == null || !GameManager.Instance.HasBossKey))
                {
                    continue;
                }

                socket.OpenPermanently();

                // 연결된 이웃 방의 맞은편 소켓도 함께 영구 개방
                if (MazeGenerator.Instance != null)
                {
                    Vector2Int neighborPos = gridPosition + MazeGrid.GetOffset(socket.Direction);
                    var neighborRoom = MazeGenerator.Instance.GetRoomAt(neighborPos);
                    if (neighborRoom != null)
                    {
                        Direction oppDir = MazeGrid.GetOpposite(socket.Direction);
                        var oppSocket = neighborRoom.GetSocket(oppDir);
                        if (oppSocket != null && oppSocket.CurrentState != DoorState.Wall)
                        {
                            oppSocket.OpenPermanently();
                            neighborRoom.OnDoorOpened();
                        }
                    }
                }
            }

            OnDoorOpened();
        }

        /// <summary>
        /// 이 방과 연결된 문이 열렸을 때 호출 (스타트룸 문 개방 포함)
        /// - 문을 연 방에서 처음 스폰되었던 몬스터들이 먼저 공격하고 조건부 3웨이브 전투 시작
        /// </summary>
        public void OnDoorOpened()
        {
            if (_isCleared || _isEncounterActive || _roomType == RoomType.Start) return;
            StartCombatEncounter();
        }

        /// <summary>
        /// 방 전투 인카운터 시작:
        /// 1. 처음 스폰된 요괴들(웨이브 1)이 즉시 플레이어를 향해 공격 개시
        /// 2. 2차 웨이브: 1차 몬스터가 남아있는 경우 30초 후 스폰, 1차 몬스터 전멸 시 즉시 스폰
        /// 3. 3차 웨이브: 2차 웨이브 소환 후 30초 후 혹은 2차 요괴 전멸 시 즉시 스폰
        /// </summary>
        public void StartCombatEncounter()
        {
            if (_isCleared || _isEncounterActive || _roomType == RoomType.Start) return;

            if (!_hasPreSpawned) PreSpawnMonsters();

            _isEncounterActive = true;
            _wavesCompleted = false;

            // 1. 처음 스폰되었던 몬스터들이 먼저 공격!
            AlertAllEnemiesInRoom();

            if (_waveCoroutine != null) StopCoroutine(_waveCoroutine);
            _waveCoroutine = StartCoroutine(RoomWaveRoutine());

            Debug.Log($"<color=yellow>[RoomController] 방 ({gridPosition.x},{gridPosition.y}) [{_roomType}] 문 개방! 1차 웨이브 요괴 공격 개시 및 웨이브 타이머 시작.</color>");
        }

        private IEnumerator RoomWaveRoutine()
        {
            float waveInterval = 30.0f;
            float spawnPause = 0.3f;
            if (Data.GameDataManager.Instance != null && Data.GameDataManager.Instance.DungeonConfig != null)
            {
                waveInterval = Data.GameDataManager.Instance.DungeonConfig.waveInterval;
                spawnPause = Data.GameDataManager.Instance.DungeonConfig.waveSpawnPauseDuration;
            }

            // 웨이브 1은 이미 교전 중
            // 규칙 1: 2차 웨이브는 이전차(1차) 웨이브의 몬스터가 남아있는 경우 30초 후 스폰, 1차 웨이브의 몬스터가 전부 사라진 경우 즉시 소환
            float wave1Timer = 0f;
            while (wave1Timer < waveInterval && LivingMonsterCount > 0)
            {
                wave1Timer += Time.deltaTime;
                yield return null;
            }

            if (_roomType != RoomType.Boss)
            {
                // 웨이브 2: 처음 스폰되었던 몬스터들과 별도로 동일한 숫자의 추가 웨이브 생성
                int waveCount = _initialMonsterCount > 0 ? _initialMonsterCount : 6;
                Vector3 center = _spawnPoint != null ? _spawnPoint.position : transform.position;

                // 스폰 무리 중심에 HCFX_Buff_Down 소환 연출 재생
                Core.VFXManager.Instance?.SpawnWaveSpawnVFX(center);

                if (HordeSpawner.Instance != null)
                {
                    var wave2 = HordeSpawner.Instance.SpawnHorde(center, count: waveCount, radius: 6f);
                    if (wave2 != null)
                    {
                        _spawnedEnemies.AddRange(wave2);
                        foreach (var m in wave2)
                        {
                            if (m != null && m.TryGetComponent<MonsterAI>(out var ai))
                            {
                                ai.SetSpawnDelay(spawnPause); // 0.3초 대기 후 이동 개시
                                ai.TriggerAggro();
                            }
                        }
                    }
                }
                Debug.Log($"<color=orange>[RoomController] 방 ({gridPosition.x},{gridPosition.y}) 2차 추가 웨이브 ({waveCount}마리) 스폰 완료! (1차 처치/경과: {wave1Timer:F1}s, {spawnPause:F1}s 후 이동 개시)</color>");

                // 스폰된 몬스터들이 프레임에 등록되도록 양보
                yield return null;

                // 규칙 2: 3차 웨이브는 1차 공격시작부터 30초가 아닌, 2차 웨이브 소환 후 30초 혹은 2차 웨이브 몬스터가 전부 제거되었을 때 즉시 소환
                float wave2Timer = 0f;
                while (wave2Timer < waveInterval && LivingMonsterCount > 0)
                {
                    wave2Timer += Time.deltaTime;
                    yield return null;
                }

                // 웨이브 3: 세 번째 웨이브 스폰 전 HCFX_Buff_Down 소환 연출 재생
                Core.VFXManager.Instance?.SpawnWaveSpawnVFX(center);

                if (HordeSpawner.Instance != null)
                {
                    var wave3 = HordeSpawner.Instance.SpawnHorde(center, count: waveCount, radius: 6f);
                    if (wave3 != null)
                    {
                        _spawnedEnemies.AddRange(wave3);
                        foreach (var m in wave3)
                        {
                            if (m != null && m.TryGetComponent<MonsterAI>(out var ai))
                            {
                                ai.SetSpawnDelay(spawnPause); // 0.3초 대기 후 이동 개시
                                ai.TriggerAggro();
                            }
                        }
                    }
                }
                Debug.Log($"<color=orange>[RoomController] 방 ({gridPosition.x},{gridPosition.y}) 3차 추가 웨이브 ({waveCount}마리) 스폰 완료! (2차 소환 후 경과: {wave2Timer:F1}s, {spawnPause:F1}s 후 이동 개시) 해당 방 웨이브 스폰 종료.</color>");
            }

            _wavesCompleted = true;
            _waveCoroutine = null;
        }

        /// <summary>
        /// 플레이어가 방 트리거에 진입했을 때 호출
        /// </summary>
        public void OnPlayerEnterRoom()
        {
            if (_isCleared) return;

            if (!_hasPreSpawned) PreSpawnMonsters();

            if (_roomType == RoomType.Boss)
            {
                EnsureBossSpawned();

                if (GameManager.Instance != null && GameManager.Instance.HasBossKey)
                {
                    StartCombatEncounter();

                    // 상단 대형 보스 HP 바 노출
                    MonsterAI bossAI = null;
                    foreach (var e in _spawnedEnemies)
                    {
                        if (e != null && e.TryGetComponent<MonsterAI>(out var ai) && ai.Type == MonsterType.FoxBoss)
                        {
                            bossAI = ai;
                            break;
                        }
                    }
                    if (bossAI == null)
                    {
                        bossAI = GetComponentInChildren<MonsterAI>();
                    }
                    if (bossAI != null && UI.BossHealthBarUI.Instance != null)
                    {
                        UI.BossHealthBarUI.Instance.ShowBoss(bossAI);
                    }
                }
                else
                {
                    Debug.LogWarning("[RoomController] 보스방 열쇠가 필요합니다!");
                }
            }
            else if (_roomType == RoomType.KeyRoom)
            {
                EnsureKeyEliteSpawned();
                StartCombatEncounter();
            }
            else
            {
                StartCombatEncounter();
            }
        }

        private void AlertAllEnemiesInRoom()
        {
            foreach (var enemy in _spawnedEnemies)
            {
                if (enemy != null && enemy.TryGetComponent<MonsterAI>(out var ai))
                {
                    ai.TriggerAggro();
                }
            }
        }

        private void SpawnNormalHorde()
        {
            if (HordeSpawner.Instance != null)
            {
                Vector3 center = _spawnPoint != null ? _spawnPoint.position : transform.position;
                int count = Data.GameDataManager.Instance != null && Data.GameDataManager.Instance.DungeonConfig != null
                    ? Data.GameDataManager.Instance.DungeonConfig.normalRoomMinionCount
                    : 6;
                _initialMonsterCount = count;
                var enemies = HordeSpawner.Instance.SpawnHorde(center, count: count, radius: 6f);
                if (enemies != null) _spawnedEnemies.AddRange(enemies);
            }
        }

        private void SpawnTrapWave()
        {
            if (HordeSpawner.Instance != null)
            {
                Vector3 center = _spawnPoint != null ? _spawnPoint.position : transform.position;
                _initialMonsterCount = 8;
                var enemies = HordeSpawner.Instance.SpawnHorde(center, count: _initialMonsterCount, radius: 6f);
                _spawnedEnemies.AddRange(enemies);
            }
        }

        public void EnsureKeyEliteSpawned()
        {
            _spawnedEnemies.RemoveAll(e => e == null);
            bool hasLivingElite = false;
            foreach (var e in _spawnedEnemies)
            {
                if (e != null && e.TryGetComponent<MonsterAI>(out var ai) && ai.Type == MonsterType.Elite && !ai.IsDead)
                {
                    hasLivingElite = true;
                    break;
                }
            }

            if (!hasLivingElite && (GameManager.Instance == null || !GameManager.Instance.HasBossKey))
            {
                Debug.LogWarning($"<color=orange>[RoomController] 열쇠방 ({gridPosition.x},{gridPosition.y})에 키키퍼가 없어 즉시 소환을 집행합니다.</color>");
                SpawnKeyElite();
            }
        }

        public void EnsureBossSpawned()
        {
            _spawnedEnemies.RemoveAll(e => e == null);
            bool hasLivingBoss = false;
            foreach (var e in _spawnedEnemies)
            {
                if (e != null && e.TryGetComponent<MonsterAI>(out var ai) && ai.Type == MonsterType.FoxBoss && !ai.IsDead)
                {
                    hasLivingBoss = true;
                    break;
                }
            }

            if (!hasLivingBoss)
            {
                Debug.LogWarning($"<color=orange>[RoomController] 보스방 ({gridPosition.x},{gridPosition.y})에 보스가 없어 즉시 소환을 집행합니다.</color>");
                SpawnFoxBoss();
            }
        }

        private void SpawnKeyElite()
        {
            var spawner = HordeSpawner.Instance ?? FindAnyObjectByType<HordeSpawner>();
            if (spawner != null)
            {
                Vector3 center = _spawnPoint != null ? _spawnPoint.position : transform.position;
                var elite = spawner.SpawnEliteKeyKeeper(center);
                if (elite != null) _spawnedEnemies.Add(elite);

                // 정예 보조 잡몹 소환
                _initialMonsterCount = 5;
                var minions = spawner.SpawnHorde(center, count: _initialMonsterCount, radius: 6f);
                if (minions != null) _spawnedEnemies.AddRange(minions);
                Debug.Log($"<color=gold>[RoomController] 열쇠방 ({gridPosition.x},{gridPosition.y})에 키키퍼 정예 요괴가 정상 소환되었습니다.</color>");
            }
        }

        private void SpawnFoxBoss()
        {
            var spawner = HordeSpawner.Instance ?? FindAnyObjectByType<HordeSpawner>();
            if (spawner != null)
            {
                Vector3 center = _spawnPoint != null ? _spawnPoint.position : transform.position;
                var boss = spawner.SpawnFoxBoss(center);
                if (boss != null) _spawnedEnemies.Add(boss);
                _initialMonsterCount = 1;
                _wavesCompleted = true; // 보스는 단일 결전
                Debug.Log($"<color=red>[RoomController] 산해경의 괴수 '여우 요괴'가 보스방 ({gridPosition.x},{gridPosition.y})에 정상 출현했습니다!</color>");
            }
        }

        private void LockAllConnectedDoors(DoorState lockState)
        {
            TryLock(Direction.North, lockState);
            TryLock(Direction.East, lockState);
            TryLock(Direction.South, lockState);
            TryLock(Direction.West, lockState);
        }

        public void UnlockAllConnectedDoors()
        {
            TryUnlock(Direction.North);
            TryUnlock(Direction.East);
            TryUnlock(Direction.South);
            TryUnlock(Direction.West);

            _isEncounterActive = false;
            _isCleared = true;
        }

        private void TryLock(Direction dir, DoorState lockState)
        {
            var s = GetSocket(dir);
            // 한번 열린 문은 열쇠가 있건 없건 이제 영원히 닫히지 않는다
            if (s != null && s.CurrentState == DoorState.Open && !s.IsPermanentlyUnlocked)
            {
                s.SetState(lockState);
            }
        }

        private void TryUnlock(Direction dir)
        {
            var s = GetSocket(dir);
            if (s != null && (s.CurrentState == DoorState.CombatLock || s.CurrentState == DoorState.BossLock))
            {
                s.SetState(DoorState.Open);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
            {
                OnPlayerEnterRoom();
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0f, 0.9f, 1f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, RoomCenterOpenRadius);
        }
    }
}
