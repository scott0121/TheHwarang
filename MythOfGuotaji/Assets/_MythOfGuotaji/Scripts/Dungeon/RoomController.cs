using System.Collections;
using System.Collections.Generic;
using MythOfGuotaji.Actor;
using MythOfGuotaji.Core;
using UnityEngine;

namespace MythOfGuotaji.Dungeon
{
    /// <summary>
    /// 개별 방 제어기 (10대 던전 문 정책 적용)
    /// - 스타트 룸(RoomType.Start)은 몬스터가 스폰되지 않는 100% 안전지대 (규칙 1)
    /// - 방 문이 열렸을 때 몬스터가 있다면 즉시 자동으로 전투 시작 (규칙 6)
    /// - 새로운 방에 진입 후 잠시 뒤(0.8초) 연결된 방 중 몬스터가 있는 방은 문이 자동 개방 (단, 중간보스방 입구/최종보스방 입구는 예외로 문 앞에 가야만 열림) (규칙 7)
    /// - 중간보스방 출구는 중간보스 처치 + 열쇠 획득 + 해당 전투 종료 후 문 앞에 가야만 열림 (규칙 8)
    /// - 전투 종료 시 한번 열었던 문은 모두 다시 열림 (규칙 10)
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

        [Header("Boss & Elite Spawn Guards")]
        private bool _bossSpawned = false;
        private bool _keyEliteSpawned = false;
        private bool _isBossDefeated = false;
        private bool _isKeyEliteDefeated = false;

        [Header("Door Batch Opening (Rule 7)")]
        private bool _allDoorsOpened = false;
        private bool _centerTriggerActivated = false;
        private float _roomEntryTimer = 0f;
        private Transform _playerTransform;
        private const float RoomEntryAutoOpenDelay = 0.8f; // 새로운 방 진입 후 잠시 뒤(0.8초) 연결된 몬스터 방 자동 개방
        private const float RoomCenterOpenRadius = 10.0f;

        public RoomType RoomType => _roomType;
        public bool IsCleared => _isCleared;
        public bool IsEncounterActive => _isEncounterActive;
        public bool WavesCompleted => _wavesCompleted;
        public bool IsBossDefeated => _isBossDefeated;
        public bool IsKeyEliteDefeated => _isKeyEliteDefeated;
        public Vector2Int GridPosition => gridPosition;
        public bool AllDoorsOpened => _allDoorsOpened;
        public bool CenterTriggerActivated => _centerTriggerActivated;

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
            // [규칙 1 절대 보장]: (0,0) 좌표는 무조건 시작 안전방(RoomType.Start)으로 강제
            if (gridPos == Vector2Int.zero)
            {
                type = RoomType.Start;
            }
            _roomType = type;
            _isCleared = (_roomType == RoomType.Start);
            _roomEntryTimer = 0f;
            _centerTriggerActivated = false;
        }

        private void Update()
        {
            if (_playerTransform == null)
            {
                var player = FindAnyObjectByType<PlayerController>();
                if (player != null) _playerTransform = player.transform;
            }

            // 1. [규칙 7]: 새로운 방에 진입했을 때 진입 후 잠시 뒤(0.8초) 그 방과 연결된 방들 중,
            //    몬스터가 있는 방은 모두 문이 자동으로 열림 (단, 중간보스방 입구, 최종보스방 입구는 예외로 문 앞에 가야만 열림)
            if (_playerTransform != null && _roomType != RoomType.Start && gridPosition != Vector2Int.zero)
            {
                float roomSize = MazeGenerator.Instance != null ? MazeGenerator.Instance.RoomSize : 39f;
                float entryMargin = 3.2f; // 문턱(Doorway)에서 방 안쪽으로 3.2m 이상 진입 시 방 진입 판정
                float halfBound = (roomSize * 0.5f) - entryMargin;

                float dx = Mathf.Abs(_playerTransform.position.x - transform.position.x);
                float dz = Mathf.Abs(_playerTransform.position.z - transform.position.z);
                bool isPlayerInsideThisRoom = (dx <= halfBound && dz <= halfBound);

                if (isPlayerInsideThisRoom)
                {
                    // 방 안으로 진입했을 때 아직 이 방의 몬스터들과 전투가 시작되지 않았다면 즉시 전투 시작 (규칙 6 & 9)
                    if (!_isCleared && !_isEncounterActive)
                    {
                        StartCombatEncounter();
                        MazeGenerator.Instance?.UpdateDungeonDoorPolicy();
                    }

                    // 일반/함정 방에 진입한 경우 잠시 뒤(0.8초) 연결된 몬스터 방들의 문을 자동 개방 (규칙 7)
                    if (!_centerTriggerActivated && _roomType != RoomType.Boss && _roomType != RoomType.KeyRoom)
                    {
                        _roomEntryTimer += Time.deltaTime;
                        if (_roomEntryTimer >= RoomEntryAutoOpenDelay)
                        {
                            _centerTriggerActivated = true;
                            OpenUnopenedConnectedDoors();
                        }
                    }
                }
            }

            // 2. 전투 진행 중인 방의 몬스터 정리 및 웨이브 종료 검사
            if (_isEncounterActive)
            {
                _spawnedEnemies.RemoveAll(e => e == null || !e.activeInHierarchy || (e.TryGetComponent<MonsterAI>(out var ai) ? ai.IsDead : true));

                // 모든 웨이브 스폰이 완료되었고 남은 몬스터가 0마리일 때 방 클리어 판정!
                if (_wavesCompleted && _spawnedEnemies.Count == 0)
                {
                    // 최종보스방인 경우 최종보스 처치(_isBossDefeated) 확인 필수
                    if (_roomType == RoomType.Boss && !_isBossDefeated)
                    {
                        return;
                    }

                    // 중간보스방인 경우 중간보스 처치(_isKeyEliteDefeated) 확인 필수
                    if (_roomType == RoomType.KeyRoom && !_isKeyEliteDefeated)
                    {
                        return;
                    }

                    CompleteRoomEncounter();
                }
            }
        }

        private void CompleteRoomEncounter()
        {
            _isEncounterActive = false;
            _isCleared = true;
            Debug.Log($"<color=lime>[RoomController] 방 ({gridPosition.x},{gridPosition.y}) [{_roomType}]의 모든 요괴를 소탕했습니다! 방 클리어 완료.</color>");

            UnlockAllConnectedDoors();

            // [규칙 10]: 전투가 종료된 뒤에는 한번 열었던 문은 모두 다시 열림
            MazeGenerator.Instance?.CheckAndEndCombatIfAllCleared();

            if (_roomType == RoomType.Boss)
            {
                UI.BossHealthBarUI.Instance?.Hide();
                Debug.Log("<color=gold><b>[RoomController] 최종보스 및 모든 지원 요괴 소탕 완료! 스테이지 클리어!</b></color>");
                GameManager.Instance?.CompleteStage(affinityReward: 5, goldReward: 100);
            }
            else if (_roomType == RoomType.KeyRoom)
            {
                Debug.Log("<color=gold><b>[RoomController] 중간보스방 전투가 완전히 종료되었습니다! 이제 중간보스방 출구 관문 앞으로 다가가면 문을 열 수 있습니다.</b></color>");
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

            // [규칙 1]: 스타트 룸(0,0)은 절대 안전지대 (어떤 경우에도 몬스터 스폰 없음)
            if (gridPosition == Vector2Int.zero || _roomType == RoomType.Start)
            {
                _roomType = RoomType.Start;
                _isCleared = true;
                return;
            }

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
                    SpawnNormalHorde(); // 1차 웨이브는 일반 잡몹 무리만 대기 (중간보스는 2차 웨이브에서 스폰)
                    break;
                case RoomType.Boss:
                    SpawnNormalHorde(); // 1차 웨이브는 일반 잡몹 무리만 대기 (최종보스는 2차 웨이브에서 스폰)
                    break;
            }
        }

        /// <summary>
        /// [규칙 7]: 새로운 방에 진입했을 때 진입 후 잠시 뒤 그 방과 연결된 방들 중,
        /// 몬스터가 있는 방은 모두 문이 자동으로 열림.
        /// (단, 중간보스방 입구, 최종보스방 입구, 중간보스방 잠긴 출구는 예외로 문 앞에 가야만 열림)
        /// </summary>
        public void OpenUnopenedConnectedDoors()
        {
            // 1. 현재 방의 전투 인카운터 시작 보장
            if (!_isCleared && _roomType != RoomType.Start)
            {
                StartCombatEncounter();
            }

            // 2. 현재 방과 연결된 4방향 소켓 중 몬스터가 있는 이웃 방의 문을 자동 개방
            DoorSocket[] sockets = { _socketNorth, _socketEast, _socketSouth, _socketWest };
            foreach (var socket in sockets)
            {
                if (socket == null || socket.CurrentState == DoorState.Wall) continue;

                // 잠긴 문(중간보스 출구)은 자동 개방 대상에서 절대 제외 (규칙 3 & 8)
                if (socket.CurrentState == DoorState.BossLock ||
                    (socket.AutoDoor != null && socket.AutoDoor.DoorType == DungeonDoorType.LockedDoor && !socket.AutoDoor.IsKeyUnlocked))
                {
                    continue;
                }

                // 연결된 이웃 방 확인
                RoomController neighborRoom = null;
                if (MazeGenerator.Instance != null)
                {
                    Vector2Int neighborPos = gridPosition + MazeGrid.GetOffset(socket.Direction);
                    neighborRoom = MazeGenerator.Instance.GetRoomAt(neighborPos);
                }

                if (neighborRoom == null) continue;

                // [규칙 7 예외]: 중간보스방 입구(KeyRoom), 최종보스방 입구(Boss)는 문 앞에 가야만 열림!
                if (neighborRoom.RoomType == RoomType.KeyRoom || neighborRoom.RoomType == RoomType.Boss)
                {
                    continue;
                }

                // [규칙 7 조건]: 이웃 방에 살아있는 몬스터가 있는 경우에만 자동으로 문을 염!
                if (!neighborRoom._hasPreSpawned)
                {
                    neighborRoom.PreSpawnMonsters();
                }

                bool neighborHasMonsters = !neighborRoom.IsCleared &&
                                           neighborRoom.RoomType != RoomType.Start &&
                                           neighborRoom.LivingMonsterCount > 0;
                if (!neighborHasMonsters)
                {
                    continue;
                }

                // 문 자동 개방
                socket.SetState(DoorState.Open);
                if (socket.AutoDoor != null)
                {
                    socket.AutoDoor.OpenDoorByCenterTrigger();
                }
                string pairKey = socket.GetPairKey();
                if (!string.IsNullOrEmpty(pairKey))
                {
                    MazeGenerator.Instance?.RegisterCombatTriggerDoor(pairKey);
                }

                // 연결된 이웃 방의 맞은편 소켓도 함께 개방 및 [규칙 6] 몬스터 자동 전투 시작
                Direction oppDir = MazeGrid.GetOpposite(socket.Direction);
                var oppSocket = neighborRoom.GetSocket(oppDir);
                if (oppSocket != null && oppSocket.CurrentState != DoorState.Wall)
                {
                    oppSocket.SetState(DoorState.Open);
                    if (oppSocket.AutoDoor != null)
                    {
                        oppSocket.AutoDoor.OpenDoorByCenterTrigger();
                    }
                }

                neighborRoom.OnDoorOpenedToCombat();
            }

            OnDoorOpened();
            MazeGenerator.Instance?.UpdateDungeonDoorPolicy();
        }

        public void OpenAllDoorsInRoom()
        {
            if (_allDoorsOpened) return;
            _allDoorsOpened = true;

            DoorSocket[] sockets = { _socketNorth, _socketEast, _socketSouth, _socketWest };
            foreach (var socket in sockets)
            {
                if (socket == null || socket.CurrentState == DoorState.Wall) continue;

                if (socket.CurrentState == DoorState.BossLock && (GameManager.Instance == null || !GameManager.Instance.HasBossKey))
                {
                    continue;
                }

                socket.OpenPermanently();

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
        /// [규칙 6]: 방 문이 열렸을 때 그 방에 몬스터들이 있다면, 몬스터들과는 자동으로 전투가 시작되는 형태
        /// </summary>
        public void OnDoorOpenedToCombat()
        {
            if (_isCleared || _isEncounterActive || _roomType == RoomType.Start || gridPosition == Vector2Int.zero) return;
            StartCombatEncounter();
        }

        public void OnDoorOpened()
        {
            if (_isCleared || _isEncounterActive || _roomType == RoomType.Start || gridPosition == Vector2Int.zero) return;
            StartCombatEncounter();
        }

        /// <summary>
        /// 방 전투 인카운터 시작:
        /// 1. 플레이어를 즉시 전투 상태(IsInCombat = true)로 전환
        /// 2. 처음 스폰된 요괴들(웨이브 1)이 즉시 플레이어를 향해 공격 개시
        /// 3. 2, 3차(보스/중간보스는 최대 5차) 웨이브 스폰 코루틴 가동
        /// </summary>
        public void StartCombatEncounter()
        {
            if (_isCleared || _isEncounterActive || _roomType == RoomType.Start || gridPosition == Vector2Int.zero) return;

            if (!_hasPreSpawned) PreSpawnMonsters();

            _isEncounterActive = true;
            _wavesCompleted = false;

            // 플레이어 전투 상태 돌입!
            PlayerController.Instance?.SetInCombat(true);

            // 1. 처음 스폰되었던 몬스터들이 즉시 어그로 획득 후 공격 개시!
            AlertAllEnemiesInRoom();

            if (_waveCoroutine != null) StopCoroutine(_waveCoroutine);
            _waveCoroutine = StartCoroutine(RoomWaveRoutine());

            MazeGenerator.Instance?.UpdateDungeonDoorPolicy();

            Debug.Log($"<color=yellow>[RoomController] 방 ({gridPosition.x},{gridPosition.y}) [{_roomType}] 문 개방! 요괴 자동 전투 개시 및 웨이브 타이머 시작.</color>");
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
                if (enemies != null)
                {
                    _spawnedEnemies.AddRange(enemies);
                    foreach (var m in enemies)
                    {
                        if (m != null && m.TryGetComponent<MonsterAI>(out var ai)) ai.OwningRoom = this;
                    }
                }
            }
        }

        private void SpawnTrapWave()
        {
            if (HordeSpawner.Instance != null)
            {
                Vector3 center = _spawnPoint != null ? _spawnPoint.position : transform.position;
                _initialMonsterCount = 8;
                var enemies = HordeSpawner.Instance.SpawnHorde(center, count: _initialMonsterCount, radius: 6f);
                if (enemies != null)
                {
                    _spawnedEnemies.AddRange(enemies);
                    foreach (var m in enemies)
                    {
                        if (m != null && m.TryGetComponent<MonsterAI>(out var ai)) ai.OwningRoom = this;
                    }
                }
            }
        }

        private void SpawnWaveHorde(Vector3 center, int count, float spawnPause)
        {
            if (HordeSpawner.Instance != null)
            {
                var horde = HordeSpawner.Instance.SpawnHorde(center, count: count, radius: 6f);
                if (horde != null)
                {
                    _spawnedEnemies.AddRange(horde);
                    foreach (var m in horde)
                    {
                        if (m != null && m.TryGetComponent<MonsterAI>(out var ai))
                        {
                            ai.OwningRoom = this;
                            ai.SetSpawnDelay(spawnPause);
                            ai.TriggerAggro();
                        }
                    }
                }
            }
        }

        private IEnumerator RoomWaveRoutine()
        {
            bool isBossOrKeyRoom = (_roomType == RoomType.Boss || _roomType == RoomType.KeyRoom);
            int totalWaves = isBossOrKeyRoom ? 5 : 3;
            float waveInterval = isBossOrKeyRoom ? 20.0f : 30.0f;
            float spawnPause = 0.3f;

            if (Data.GameDataManager.Instance != null && Data.GameDataManager.Instance.DungeonConfig != null)
            {
                spawnPause = Data.GameDataManager.Instance.DungeonConfig.waveSpawnPauseDuration;
                if (!isBossOrKeyRoom)
                {
                    waveInterval = Data.GameDataManager.Instance.DungeonConfig.waveInterval;
                }
            }

            // 웨이브 1은 이미 교전 중
            for (int currentWave = 2; currentWave <= totalWaves; currentWave++)
            {
                // 보스 또는 중간보스가 이미 처치되었다면 추가 웨이브 중단
                if ((_roomType == RoomType.Boss && _isBossDefeated) ||
                    (_roomType == RoomType.KeyRoom && _isKeyEliteDefeated))
                {
                    break;
                }

                float timer = 0f;
                while (timer < waveInterval && LivingMonsterCount > 0)
                {
                    if ((_roomType == RoomType.Boss && _isBossDefeated) ||
                        (_roomType == RoomType.KeyRoom && _isKeyEliteDefeated))
                    {
                        break;
                    }
                    timer += Time.deltaTime;
                    yield return null;
                }

                if ((_roomType == RoomType.Boss && _isBossDefeated) ||
                    (_roomType == RoomType.KeyRoom && _isKeyEliteDefeated))
                {
                    break;
                }

                Vector3 center = _spawnPoint != null ? _spawnPoint.position : transform.position;

                // [2차 웨이브 특별 스폰 처리]
                if (currentWave == 2)
                {
                    if (_roomType == RoomType.Boss)
                    {
                        Core.VFXManager.Instance?.SpawnWaveSpawnVFX(center);
                        EnsureBossSpawned();

                        MonsterAI bossAI = null;
                        foreach (var e in _spawnedEnemies)
                        {
                            if (e != null && e.TryGetComponent<MonsterAI>(out var ai) && ai.Type == MonsterType.FoxBoss && !ai.IsDead)
                            {
                                bossAI = ai;
                                break;
                            }
                        }
                        if (bossAI != null && UI.BossHealthBarUI.Instance != null)
                        {
                            UI.BossHealthBarUI.Instance.ShowBoss(bossAI);
                        }

                        Debug.Log($"<color=red><b>[RoomController] 보스방 ({gridPosition.x},{gridPosition.y}) 2차 웨이브: 최종보스 '구미 요괴' 강림!</b></color>");
                    }
                    else if (_roomType == RoomType.KeyRoom)
                    {
                        Core.VFXManager.Instance?.SpawnWaveSpawnVFX(center);
                        EnsureKeyEliteSpawned();

                        Debug.Log($"<color=gold><b>[RoomController] 중간보스방 ({gridPosition.x},{gridPosition.y}) 2차 웨이브: 키키퍼 소환!</b></color>");
                    }
                    else
                    {
                        Core.VFXManager.Instance?.SpawnWaveSpawnVFX(center);
                        int waveCount = _initialMonsterCount > 0 ? _initialMonsterCount : 6;
                        SpawnWaveHorde(center, waveCount, spawnPause);
                    }
                }
                else
                {
                    int supportCount = isBossOrKeyRoom ? 4 : (_initialMonsterCount > 0 ? _initialMonsterCount : 6);
                    Core.VFXManager.Instance?.SpawnWaveSpawnVFX(center);
                    SpawnWaveHorde(center, supportCount, spawnPause);
                    Debug.Log($"<color=orange>[RoomController] 방 ({gridPosition.x},{gridPosition.y}) [{_roomType}] {currentWave}차 추가 웨이브 ({supportCount}마리) 스폰 완료!</color>");
                }

                yield return null;
            }

            _wavesCompleted = true;
            _waveCoroutine = null;
        }

        /// <summary>
        /// 플레이어가 방 트리거에 진입했을 때 호출
        /// </summary>
        public void OnPlayerEnterRoom()
        {
            if (_isCleared || _roomType == RoomType.Start || gridPosition == Vector2Int.zero) return;

            if (!_hasPreSpawned) PreSpawnMonsters();

            StartCombatEncounter();

            if (_roomType == RoomType.Boss)
            {
                MonsterAI bossAI = null;
                foreach (var e in _spawnedEnemies)
                {
                    if (e != null && e.TryGetComponent<MonsterAI>(out var ai) && ai.Type == MonsterType.FoxBoss && !ai.IsDead)
                    {
                        bossAI = ai;
                        break;
                    }
                }
                if (bossAI != null && UI.BossHealthBarUI.Instance != null)
                {
                    UI.BossHealthBarUI.Instance.ShowBoss(bossAI);
                }
            }
        }

        public void EnsureKeyEliteSpawned()
        {
            if (_keyEliteSpawned) return;

            var allMonsters = FindObjectsByType<MonsterAI>(FindObjectsInactive.Exclude);
            foreach (var m in allMonsters)
            {
                if (m != null && m.DropsBossKeyOnDeath && !m.IsDead)
                {
                    _keyEliteSpawned = true;
                    if (!_spawnedEnemies.Contains(m.gameObject))
                    {
                        _spawnedEnemies.Add(m.gameObject);
                    }
                    m.OwningRoom = this;
                    return;
                }
            }

            _keyEliteSpawned = true;
            Debug.LogWarning($"<color=orange>[RoomController] 방 ({gridPosition.x},{gridPosition.y})에 중간보스(키키퍼)를 소환합니다.</color>");
            SpawnKeyElite();
        }

        public void EnsureBossSpawned()
        {
            if (_bossSpawned) return;

            var allMonsters = FindObjectsByType<MonsterAI>(FindObjectsInactive.Exclude);
            foreach (var m in allMonsters)
            {
                if (m != null && m.Type == MonsterType.FoxBoss && !m.IsDead)
                {
                    _bossSpawned = true;
                    if (!_spawnedEnemies.Contains(m.gameObject))
                    {
                        _spawnedEnemies.Add(m.gameObject);
                    }
                    m.OwningRoom = this;
                    return;
                }
            }

            Debug.LogWarning($"<color=orange>[RoomController] 방 ({gridPosition.x},{gridPosition.y})에 구미 요괴 보스를 소환합니다.</color>");
            SpawnFoxBoss();
        }

        /// <summary>
        /// 최종보스 처치 시 호출 - 추가 웨이브를 중단하고 남은 잔당 소탕 대기
        /// </summary>
        public void OnBossDefeated()
        {
            _isBossDefeated = true;
            Debug.Log("<color=red><b>[RoomController] 보스(FoxBoss)가 쓰러졌습니다! 추가 웨이브를 중단하고 남은 잔당 요괴 소탕을 대기합니다.</b></color>");

            if (_waveCoroutine != null)
            {
                StopCoroutine(_waveCoroutine);
                _waveCoroutine = null;
            }
            _wavesCompleted = true;

            _spawnedEnemies.RemoveAll(e => e == null || !e.activeInHierarchy || (e.TryGetComponent<MonsterAI>(out var ai) ? ai.IsDead : true));

            if (_spawnedEnemies.Count == 0)
            {
                CompleteRoomEncounter();
            }
        }

        /// <summary>
        /// [규칙 8]: 중간보스(키키퍼) 처치 시 호출 - 추가 웨이브를 중단하고 방 내 지원 요괴들을 정리하여 전투 종료
        /// </summary>
        public void OnKeyEliteDefeated()
        {
            _keyEliteSpawned = true;
            _isKeyEliteDefeated = true;
            Debug.Log("<color=gold><b>[RoomController] 중간보스(키키퍼)가 처치되어 열쇠를 획득했습니다! 중간보스방 전투가 종료됩니다.</b></color>");

            if (_waveCoroutine != null)
            {
                StopCoroutine(_waveCoroutine);
                _waveCoroutine = null;
            }
            _wavesCompleted = true;

            // 중간보스 처치 시 방 내의 남은 지원 잡몹들도 일괄 소탕 처리
            for (int i = _spawnedEnemies.Count - 1; i >= 0; i--)
            {
                var enemy = _spawnedEnemies[i];
                if (enemy != null && enemy.TryGetComponent<MonsterAI>(out var ai) && !ai.IsDead)
                {
                    ai.Die();
                }
            }
            _spawnedEnemies.Clear();

            CompleteRoomEncounter();
        }

        private void SpawnKeyElite()
        {
            var spawner = HordeSpawner.Instance ?? FindAnyObjectByType<HordeSpawner>();
            if (spawner != null)
            {
                Vector3 center = _spawnPoint != null ? _spawnPoint.position : transform.position;
                var elite = spawner.SpawnEliteKeyKeeper(center);
                if (elite != null)
                {
                    _spawnedEnemies.Add(elite);
                    if (elite.TryGetComponent<MonsterAI>(out var ai))
                    {
                        ai.OwningRoom = this;
                        ai.TriggerAggro();
                    }
                }

                _initialMonsterCount = 5;
                var minions = spawner.SpawnHorde(center, count: _initialMonsterCount, radius: 6f);
                if (minions != null)
                {
                    _spawnedEnemies.AddRange(minions);
                    foreach (var m in minions)
                    {
                        if (m != null && m.TryGetComponent<MonsterAI>(out var ai))
                        {
                            ai.OwningRoom = this;
                            ai.TriggerAggro();
                        }
                    }
                }
                Debug.Log($"<color=gold>[RoomController] 방 ({gridPosition.x},{gridPosition.y})에 키키퍼 및 호위 요괴들이 소환되었습니다.</color>");
            }
        }

        private void SpawnFoxBoss()
        {
            var spawner = HordeSpawner.Instance ?? FindAnyObjectByType<HordeSpawner>();
            if (spawner != null)
            {
                Vector3 center = _spawnPoint != null ? _spawnPoint.position : transform.position;
                var boss = spawner.SpawnFoxBoss(center);
                if (boss != null)
                {
                    _spawnedEnemies.Add(boss);
                    if (boss.TryGetComponent<MonsterAI>(out var ai))
                    {
                        ai.OwningRoom = this;
                        ai.TriggerAggro();
                    }
                    _bossSpawned = true;
                    Debug.Log($"<color=red><b>[RoomController] 최종 결전 보스 '구미 요괴'가 방 ({gridPosition.x},{gridPosition.y})에 강림했습니다!</b></color>");
                }
                else
                {
                    _bossSpawned = false;
                    Debug.LogError("[RoomController] FoxBoss 스폰 실패!");
                }
            }
            else
            {
                _bossSpawned = false;
                Debug.LogError("[RoomController] HordeSpawner를 찾을 수 없습니다!");
            }
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

        private void TryUnlock(Direction dir)
        {
            var s = GetSocket(dir);
            // 주의: BossLock(중간보스방 출구 관문)은 전투 종료만으로 풀리지 않고 플레이어가 열쇠를 가지고 문 앞에 가야만 해제됨 (규칙 8)
            if (s != null && s.CurrentState == DoorState.CombatLock)
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
