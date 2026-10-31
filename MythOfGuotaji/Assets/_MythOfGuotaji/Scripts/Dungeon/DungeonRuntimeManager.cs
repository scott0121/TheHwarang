using System;
using System.Collections.Generic;
using MythOfGuotaji.Actor;
using MythOfGuotaji.Core;
using UnityEngine;

namespace MythOfGuotaji.Dungeon
{
    /// <summary>
    /// 던전 런타임 플레이 및 상호작용 관리자
    /// - 플레이어 위치 및 방 추적 (문턱 히스테리시스 적용)
    /// - 몬스터 BFS 최단 경로 네비게이션 웨이포인트 제공
    /// - 던전 10대 문 개폐 정책 및 인카운터 전투 상태 실시간 판정
    /// - 보스방 잠금/해제 및 스테이지 재시작 조율
    /// </summary>
    public class DungeonRuntimeManager : MonoBehaviour
    {
        public static DungeonRuntimeManager Instance { get; private set; }

        private MazeGrid _grid;
        private IReadOnlyDictionary<Vector2Int, RoomController> _rooms;
        private float _roomSize = 39f;

        private Vector2Int _playerCurrentRoom = Vector2Int.zero;
        private float _doorPolicyCheckTimer = 0f;
        private readonly HashSet<string> _activeCombatTriggerDoors = new();
        private readonly HashSet<string> _doorsClosedByCurrentCombat = new();
        private readonly HashSet<string> _doorsOpenBeforeCurrentCombat = new();

        public Vector2Int PlayerCurrentRoom => _playerCurrentRoom;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Initialize(MazeGrid grid, IReadOnlyDictionary<Vector2Int, RoomController> rooms, float roomSize)
        {
            _grid = grid;
            _rooms = rooms;
            _roomSize = roomSize > 0f ? roomSize : 39f;
            _playerCurrentRoom = Vector2Int.zero;
            ClearCombatTriggerDoors();

            // 시작방 기준 상하좌우 2칸 방 사전 스폰 및 활성화 초기화
            UpdateMonsterActivityAndPreSpawnAround(Vector2Int.zero, 2);
        }

        private float _proximityCullTimer = 0f;

        private void Update()
        {
            if (PlayerController.Instance != null && (PlayerController.Instance.IsInCombat || HasAnyActiveCombat()))
            {
                _doorPolicyCheckTimer += Time.deltaTime;
                if (_doorPolicyCheckTimer >= 0.1f)
                {
                    _doorPolicyCheckTimer = 0f;
                    UpdateDungeonDoorPolicy();
                }
            }

            // 플레이어 위치 기반 상하좌우 2칸 몬스터 사전 스폰 및 휴면(Dormant) 컬링 (0.2초 주기)
            _proximityCullTimer += Time.deltaTime;
            if (_proximityCullTimer >= 0.2f)
            {
                _proximityCullTimer = 0f;
                UpdateMonsterActivityAndPreSpawnAround(GetPlayerCurrentRoom(), 2);
            }
        }

        /// <summary>
        /// [최적화 규칙 1 & 2]: 플레이어가 위치한 방 기준 상하좌우 2칸까지만 사전 스폰하고,
        /// 2칸 초과 범위에 있는 방의 비전투 몬스터는 휴면(Dormant) 처리하여 Update 및 물리 연산을 완전히 스킵합니다.
        /// </summary>
        public void UpdateMonsterActivityAndPreSpawnAround(Vector2Int playerRoom, int activeRange = 2)
        {
            if (_rooms == null) return;

            foreach (var kvp in _rooms)
            {
                if (kvp.Value == null) continue;

                int dx = Mathf.Abs(kvp.Key.x - playerRoom.x);
                int dy = Mathf.Abs(kvp.Key.y - playerRoom.y);
                bool inRange = dx <= activeRange && dy <= activeRange;
                // [최적화 2: 룸 메쉬 오클루전 컬링]
                kvp.Value.SetRoomVisualActive(inRange);


                if (inRange)
                {
                    // 2칸 이내 방: 아직 스폰되지 않았으면 지금 스폰!
                    if (!kvp.Value.HasPreSpawned)
                    {
                        kvp.Value.PreSpawnMonsters();
                    }
                    kvp.Value.SetMonstersDormant(false);
                }
                else
                {
                    // 2칸 밖의 방: 비어있거나 스폰되었더라도 비전투 몬스터는 휴면(Dormant) 처리
                    if (kvp.Value.HasPreSpawned)
                    {
                        kvp.Value.SetMonstersDormant(true);
                    }
                }
            }
        }

        #region Player & Room Spatial Tracking

        /// <summary>
        /// 플레이어가 현재 머물고 있는 방을 안정적으로 판정 (문턱 히스테리시스 적용)
        /// - 문턱(Doorway) 근처에 있을 때는 이전 방 소속을 유지하여 경계선 오인식 방지
        /// - 새 방 내부 마진(halfSize - 3.2m) 안쪽으로 확실히 들어왔을 때만 현재 방을 갱신
        /// </summary>
        public Vector2Int GetPlayerCurrentRoom()
        {
            if (PlayerController.Instance == null) return _playerCurrentRoom;
            Vector3 pPos = PlayerController.Instance.transform.position;

            float halfInterior = (_roomSize * 0.5f) - 3.2f;

            // 1. 현재 방 내부 영역 안에 여전히 있는지 검사
            Vector3 curCenter = GetRoomCenterWorldPos(_playerCurrentRoom);
            if (Mathf.Abs(pPos.x - curCenter.x) <= halfInterior && Mathf.Abs(pPos.z - curCenter.z) <= halfInterior)
            {
                return _playerCurrentRoom;
            }

            // 2. 다른 방의 내부 영역으로 확실히 들어왔는지 검사
            if (_rooms != null)
            {
                foreach (var kvp in _rooms)
                {
                    Vector3 rCenter = GetRoomCenterWorldPos(kvp.Key);
                    if (Mathf.Abs(pPos.x - rCenter.x) <= halfInterior && Mathf.Abs(pPos.z - rCenter.z) <= halfInterior)
                    {
                        _playerCurrentRoom = kvp.Key;
                        return _playerCurrentRoom;
                    }
                }
            }

            // 3. 경계선(문턱)에 걸쳐 있는 경우 이전 방 소속 유지
            return _playerCurrentRoom;
        }

        public Vector2Int GetRoomCoordsAtWorldPos(Vector3 worldPos)
        {
            int x = Mathf.RoundToInt(worldPos.x / _roomSize);
            int y = Mathf.RoundToInt(worldPos.z / _roomSize);
            return new Vector2Int(x, y);
        }

        public Vector3 GetRoomCenterWorldPos(Vector2Int gridPos)
        {
            return new Vector3(gridPos.x * _roomSize, 1.0f, gridPos.y * _roomSize);
        }

        public Vector3 GetDoorwayWorldPos(Vector2Int fromRoom, Direction dir)
        {
            Vector3 center = GetRoomCenterWorldPos(fromRoom);
            Vector2Int offset = MazeGrid.GetOffset(dir);
            // 문턱(0.5f * _roomSize)을 완전히 넘어 다음 방 안쪽(+2.5m)까지 진입하도록 목표점 설정
            return new Vector3(center.x + offset.x * (_roomSize * 0.5f + 2.5f), 1.0f, center.z + offset.y * (_roomSize * 0.5f + 2.5f));
        }

        public RoomController GetRoomAt(Vector2Int pos)
        {
            if (_rooms != null && _rooms.TryGetValue(pos, out var room))
            {
                return room;
            }
            return null;
        }

        /// <summary>
        /// BFS를 통해 현재 방에서 목표 방으로 가는 최단 경로 상의 '다음 통로 문(Doorway)' 위치를 계산
        /// </summary>
        public Vector3 GetNextWaypointTowards(Vector2Int currentRoom, Vector2Int targetRoom)
        {
            if (_grid == null || currentRoom == targetRoom)
            {
                return GetRoomCenterWorldPos(targetRoom);
            }

            Queue<Vector2Int> queue = new();
            Dictionary<Vector2Int, Vector2Int> parent = new();
            HashSet<Vector2Int> visited = new();

            queue.Enqueue(currentRoom);
            visited.Add(currentRoom);

            bool found = false;
            while (queue.Count > 0)
            {
                var curr = queue.Dequeue();
                if (curr == targetRoom)
                {
                    found = true;
                    break;
                }

                var cell = _grid.GetCell(curr);
                if (cell == null) continue;

                foreach (Direction dir in Enum.GetValues(typeof(Direction)))
                {
                    if (cell.IsOpen(dir))
                    {
                        Vector2Int nextPos = curr + MazeGrid.GetOffset(dir);
                        if (!visited.Contains(nextPos) && _grid.GetCell(nextPos) != null)
                        {
                            visited.Add(nextPos);
                            parent[nextPos] = curr;
                            queue.Enqueue(nextPos);
                        }
                    }
                }
            }

            if (!found)
            {
                return GetRoomCenterWorldPos(targetRoom);
            }

            // targetRoom에서 역추적하여 currentRoom 바로 다음 방(step) 도출
            Vector2Int step = targetRoom;
            while (parent.TryGetValue(step, out var p) && p != currentRoom)
            {
                step = p;
            }

            Vector2Int dirVec = step - currentRoom;
            Direction outDir = Direction.North;
            if (dirVec == new Vector2Int(0, 1)) outDir = Direction.North;
            else if (dirVec == new Vector2Int(1, 0)) outDir = Direction.East;
            else if (dirVec == new Vector2Int(0, -1)) outDir = Direction.South;
            else if (dirVec == new Vector2Int(-1, 0)) outDir = Direction.West;

            return GetDoorwayWorldPos(currentRoom, outDir);
        }

        #endregion

        #region Boss Door Management

        /// <summary>
        /// 보스방으로 진입하는 모든 인접 방의 문 소켓을 보스 봉인(BossLock) 상태로 잠금
        /// </summary>
        public void LockBossRoomNeighborSockets(Vector2Int bossPos)
        {
            if (_rooms == null) return;

            foreach (Direction dir in Enum.GetValues(typeof(Direction)))
            {
                Vector2Int neighborPos = bossPos + MazeGrid.GetOffset(dir);
                if (_rooms.TryGetValue(neighborPos, out var neighborRoom))
                {
                    Direction oppositeDir = MazeGrid.GetOpposite(dir);
                    var socket = neighborRoom.GetSocket(oppositeDir);
                    if (socket != null && socket.CurrentState == DoorState.Open)
                    {
                        socket.SetState(DoorState.BossLock);
                    }
                }
            }
        }

        /// <summary>
        /// 중간보스 처치 후 열쇠 획득 시 보스방 관문 해제 (던전 내 모든 LockedDoor 해제)
        /// </summary>
        public void UnlockBossDoors()
        {
            if (_rooms != null)
            {
                foreach (var kvp in _rooms)
                {
                    var room = kvp.Value;
                    if (room == null || room.RoomType != RoomType.KeyRoom) continue;

                    foreach (Direction dir in System.Enum.GetValues(typeof(Direction)))
                    {
                        var socket = room.GetSocket(dir);
                        if (socket != null && socket.CurrentState == DoorState.BossLock)
                        {
                            socket.UnlockWithKey();
                        }
                    }
                }
            }

            Debug.Log("<color=gold>[DungeonRuntimeManager] 중간보스 출구 관문이 열쇠로 봉인 해제되었습니다!</color>");
        }

        #endregion

        #region Ten Dungeon Door Policies Engine (던전 문 10대 정책 관리 엔진)

        public static string GetDoorPairKey(Vector2Int a, Vector2Int b)
        {
            return a.x < b.x || (a.x == b.x && a.y < b.y)
                ? $"{a.x}_{a.y}:{b.x}_{b.y}"
                : $"{b.x}_{b.y}:{a.x}_{a.y}";
        }

        public void RegisterCombatTriggerDoor(string pairKey)
        {
            if (!string.IsNullOrEmpty(pairKey))
            {
                _activeCombatTriggerDoors.Add(pairKey);
            }
        }

        public void ClearCombatTriggerDoors()
        {
            _activeCombatTriggerDoors.Clear();
            _doorsClosedByCurrentCombat.Clear();
            _doorsOpenBeforeCurrentCombat.Clear();
        }

        public bool IsCombatTriggerDoor(string pairKey)
        {
            return _activeCombatTriggerDoors.Contains(pairKey);
        }

        public void SnapshotDoorsOpenedBeforeCombat()
        {
            _doorsOpenBeforeCurrentCombat.Clear();
            var allDoors = FindObjectsByType<DungeonAutoDoor>(FindObjectsInactive.Include);
            foreach (var door in allDoors)
            {
                if (door != null && door.IsOpen)
                {
                    string pairKey = GetDoorPairKeyFromDoor(door);
                    if (!string.IsNullOrEmpty(pairKey))
                    {
                        _doorsOpenBeforeCurrentCombat.Add(pairKey);
                    }
                }
            }
        }

        public static string GetDoorPairKeyFromDoor(DungeonAutoDoor door)
        {
            if (door == null) return string.Empty;
            var socket = door.GetComponentInParent<DoorSocket>();
            if (socket == null) return string.Empty;
            var room = socket.GetComponentInParent<RoomController>();
            if (room == null) return string.Empty;
            Vector2Int posA = room.GridPosition;
            Vector2Int posB = posA + MazeGrid.GetOffset(socket.Direction);
            return GetDoorPairKey(posA, posB);
        }

        public bool HasAnyActiveCombat()
        {
            if (_rooms != null)
            {
                foreach (var room in _rooms.Values)
                {
                    if (room != null && room.IsEncounterActive && (!room.WavesCompleted || room.LivingMonsterCount > 0))
                    {
                        return true;
                    }
                }
            }

            var activeMonsters = MonsterAI.ActiveMonsters;
            for (int i = 0; i < activeMonsters.Count; i++)
            {
                var m = activeMonsters[i];
                if (m != null && !m.IsDead && m.IsAggroed)
                {
                    return true;
                }
            }

            return false;
        }

        public void CheckAndEndCombatIfAllCleared()
        {
            if (!HasAnyActiveCombat())
            {
                ClearCombatTriggerDoors();
                PlayerController.Instance?.SetInCombat(false);
                ReopenAllOpenedDoorsAfterCombat();
            }
            else
            {
                UpdateDungeonDoorPolicy();
            }
        }

        public void ReopenAllOpenedDoorsAfterCombat()
        {
            var allDoors = FindObjectsByType<DungeonAutoDoor>(FindObjectsInactive.Include);
            foreach (var door in allDoors)
            {
                if (door != null && door.HasEverBeenOpened)
                {
                    door.ReopenAfterCombat();
                }
            }
        }

        private HashSet<string> GetDoorPairsConnectingPlayerToMonsters(Vector2Int playerPos, HashSet<Vector2Int> combatRooms)
        {
            HashSet<string> connectingPairs = new();
            if (_grid == null || combatRooms.Count == 0 || _rooms == null) return connectingPairs;

            Vector3 playerWorldPos = PlayerController.Instance != null ? PlayerController.Instance.transform.position : Vector3.zero;

            // [안전망 1]: 플레이어가 어떤 문 근처(5.0m 이내)에 있고, 그 문이 교전 방과 연결되어 있다면 즉시 보호
            foreach (var kvp in _rooms)
            {
                var room = kvp.Value;
                if (room == null) continue;

                foreach (Direction dir in Enum.GetValues(typeof(Direction)))
                {
                    var socket = room.GetSocket(dir);
                    if (socket != null && socket.AutoDoor != null)
                    {
                        Vector2Int neighborPos = room.GridPosition + MazeGrid.GetOffset(dir);
                        if (combatRooms.Contains(neighborPos) || combatRooms.Contains(room.GridPosition))
                        {
                            float d = Vector3.Distance(socket.AutoDoor.transform.position, playerWorldPos);
                            if (d <= 5.0f)
                            {
                                connectingPairs.Add(GetDoorPairKey(room.GridPosition, neighborPos));
                            }
                        }
                    }
                }
            }

            foreach (var targetRoom in combatRooms)
            {
                if (targetRoom == playerPos)
                {
                    continue;
                }

                Queue<Vector2Int> queue = new();
                Dictionary<Vector2Int, Vector2Int> parent = new();
                HashSet<Vector2Int> visited = new();

                queue.Enqueue(playerPos);
                visited.Add(playerPos);

                bool reached = false;
                while (queue.Count > 0)
                {
                    var curr = queue.Dequeue();
                    if (curr == targetRoom)
                    {
                        reached = true;
                        break;
                    }

                    var cell = _grid.GetCell(curr);
                    if (cell == null) continue;

                    var currRoom = GetRoomAt(curr);

                    foreach (Direction dir in Enum.GetValues(typeof(Direction)))
                    {
                        if (cell.IsOpen(dir))
                        {
                            Vector2Int nextPos = curr + MazeGrid.GetOffset(dir);
                            if (!visited.Contains(nextPos) && _grid.GetCell(nextPos) != null)
                            {
                                // 보스 출구 봉인문(BossLock)이 아닌 일반 연결 통로라면 경로로 탐색하여 단절 방지
                                bool canTraverse = true;
                                if (currRoom != null)
                                {
                                    var socket = currRoom.GetSocket(dir);
                                    if (socket != null && socket.CurrentState == DoorState.BossLock && !socket.IsPermanentlyUnlocked)
                                    {
                                        canTraverse = false;
                                    }
                                }

                                if (canTraverse)
                                {
                                    visited.Add(nextPos);
                                    parent[nextPos] = curr;
                                    queue.Enqueue(nextPos);
                                }
                            }
                        }
                    }
                }

                if (reached)
                {
                    Vector2Int step = targetRoom;
                    while (parent.ContainsKey(step))
                    {
                        Vector2Int p = parent[step];
                        connectingPairs.Add(GetDoorPairKey(p, step));
                        step = p;
                    }
                }
            }

            return connectingPairs;
        }

        
        public bool IsDoorConnectingToActiveCombat(Vector2Int roomPos, Direction dir)
        {
            if (_grid == null) return false;
            Vector2Int neighborPos = roomPos + MazeGrid.GetOffset(dir);
            string pairKey = GetDoorPairKey(roomPos, neighborPos);

            // 1. 활성 전투 직전 유발 문이거나 전투 중 열린 문
            if (_activeCombatTriggerDoors.Contains(pairKey)) return true;

            // 2. 인접 방에 살아있는 교전 몬스터가 있는지 확인
            var neighborRoom = GetRoomAt(neighborPos);
            if (neighborRoom != null && !neighborRoom.IsCleared && neighborRoom.LivingMonsterCount > 0)
            {
                return true;
            }

            // 3. 플레이어와 몬스터 사이를 잇는 BFS 경로상 문인지 확인
            Vector2Int playerPos = GetPlayerCurrentRoom();
            HashSet<Vector2Int> combatRooms = new();
            var activeMonsters = MonsterAI.ActiveMonsters;
            for (int i = 0; i < activeMonsters.Count; i++)
            {
                var m = activeMonsters[i];
                if (m != null && !m.IsDead && (m.IsAggroed || (m.OwningRoom != null && m.OwningRoom.IsEncounterActive)))
                {
                    combatRooms.Add(GetRoomCoordsAtWorldPos(m.transform.position));
                }
            }
            if (_rooms != null)
            {
                foreach (var r in _rooms.Values)
                {
                    if (r != null && r.IsEncounterActive && !r.WavesCompleted)
                    {
                        combatRooms.Add(r.GridPosition);
                    }
                }
            }

            var pathPairs = GetDoorPairsConnectingPlayerToMonsters(playerPos, combatRooms);
            return pathPairs.Contains(pairKey);
        }

        public void UpdateDungeonDoorPolicy()
        {
            if (PlayerController.Instance == null) return;

            bool anyActiveCombat = HasAnyActiveCombat();
            if (!anyActiveCombat && !PlayerController.Instance.IsInCombat)
            {
                ReopenAllOpenedDoorsAfterCombat();
                return;
            }

            Vector3 playerWorldPos = PlayerController.Instance.transform.position;
            Vector2Int playerPos = GetPlayerCurrentRoom();

            // 1. 교전 중인 몬스터 방 및 남은 웨이브 방 수집
            HashSet<Vector2Int> combatRooms = new();
            var activeMonsters = MonsterAI.ActiveMonsters;
            for (int i = 0; i < activeMonsters.Count; i++)
            {
                var m = activeMonsters[i];
                if (m != null && !m.IsDead)
                {
                    bool isEngaged = m.IsAggroed || (m.OwningRoom != null && m.OwningRoom.IsEncounterActive);
                    if (isEngaged)
                    {
                        Vector2Int mPos = GetRoomCoordsAtWorldPos(m.transform.position);
                        combatRooms.Add(mPos);
                    }
                }
            }

            if (_rooms != null)
            {
                foreach (var room in _rooms.Values)
                {
                    if (room != null && room.IsEncounterActive && !room.WavesCompleted)
                    {
                        combatRooms.Add(room.GridPosition);
                    }
                }
            }

            if (combatRooms.Count == 0 && !anyActiveCombat)
            {
                PlayerController.Instance.SetInCombat(false);
                ReopenAllOpenedDoorsAfterCombat();
                return;
            }

            PlayerController.Instance.SetInCombat(true);

            HashSet<string> pathDoorPairs = GetDoorPairsConnectingPlayerToMonsters(playerPos, combatRooms);

            // 2. 모든 자동문 순회 평가
            var allDoors = FindObjectsByType<DungeonAutoDoor>(FindObjectsInactive.Include);
            foreach (var door in allDoors)
            {
                if (door == null) continue;
                var socket = door.GetComponentInParent<DoorSocket>();
                if (socket == null) continue;
                var room = socket.GetComponentInParent<RoomController>();
                if (room == null) continue;

                if (door.DoorType == DungeonDoorType.LockedDoor && !door.IsKeyUnlocked)
                {
                    continue;
                }

                if (!door.HasEverBeenOpened)
                {
                    continue;
                }

                if (door.IsPermanentlyOpen)
                {
                    continue;
                }

                Vector2Int posA = room.GridPosition;
                Vector2Int posB = posA + MazeGrid.GetOffset(socket.Direction);
                string pairKey = GetDoorPairKey(posA, posB);

                bool isRecentlyOpened = (Time.time - door.LastOpenedTime < 3.0f);
                bool isCombatTriggerDoor = _activeCombatTriggerDoors.Contains(pairKey);
                bool isBetweenPlayerAndMonsters = pathDoorPairs.Contains(pairKey);
                bool isGatewayToCombat = (posA == playerPos && combatRooms.Contains(posB)) || (posB == playerPos && combatRooms.Contains(posA));
                bool isOpenedDuringCombat = door.OpenedDuringCombat;

                // [사용자 핵심 규칙]: 
                // 1. 방금 전투를 유발한 문 (CombatTriggerDoor)
                // 2. 플레이어↔전투몬스터 연결 통로 (BFS 경로 isBetweenPlayerAndMonsters / isGatewayToCombat)
                // 3. 전투 시작 3초 이내에 열린 문 (isRecentlyOpened)
                // 4. 전투 중에 열려 관통하는 문 (isOpenedDuringCombat)
                // '플레이어 문 근처 6m' 조건은 후퇴 금지 정책 준수를 위해 완전히 제거!
                if (isCombatTriggerDoor || isBetweenPlayerAndMonsters || isGatewayToCombat || isRecentlyOpened || isOpenedDuringCombat)
                {
                    door.ForceOpen();
                }
                else
                {
                    // 위의 4가지 열림 유지 조건에 해당하지 않는 모든 이전에 열었던 문은 자동으로 닫힘
                    if (door.IsOpen && !door.IsActorInDoorway(1.2f))
                    {
                        door.CloseByCombat();
                    }
                }
            }
        }

        #endregion

        #region Stage Restart

        public void RestartStage()
        {
            Debug.Log("<color=cyan>[DungeonRuntimeManager] 스테이지 초기화 및 시작룸 재출발 시작!</color>");

            var player = FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                player.Respawn();
            }

            if (MazeGenerator.Instance != null)
            {
                MazeGenerator.Instance.GenerateDungeon();
            }
        }

        #endregion
    }
}
