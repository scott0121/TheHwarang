using System.Collections.Generic;
using MythOfGuotaji.Actor;
using MythOfGuotaji.Core;
using UnityEngine;

namespace MythOfGuotaji.Dungeon
{
    /// <summary>
    /// 절차적 N x N 정사각형 미로 생성기
    /// - 가로/세로 벽 및 4방향 소켓 정밀 회전/위치 생성
    /// - 시작방 (0,0) 고정, 보스방 (N-1, N-1) 고정
    /// - 완벽 미로(Spanning Tree) + 순환로 추가로 풍부한 우회로 제공
    /// - 몬스터가 방 간 최단 경로를 탐색할 수 있는 BFS Waypoint 네비게이션 제공
    /// </summary>
    public class MazeGenerator : MonoBehaviour
    {
        public static MazeGenerator Instance { get; private set; }

        [Header("Maze Settings")]
        [SerializeField] private int _gridSize = 6; // 6x6 미로 (총 36개 방)
        [SerializeField] private float _roomSize = 39f; // 방 한 변의 길이 (미터, 150% 확대: 39m)
        [SerializeField] private float _wallHeight = 4.5f;
        [SerializeField] private float _doorWidth = 12.0f;
        [SerializeField] private float _extraLoopRatio = 0.25f; // 추가 순환로 비율
        [SerializeField] private int _minDistanceForKeyRoom = 3; // 시작방으로부터 중간보스(열쇠방) 최소 거리

        [Header("Theme & Prefabs")]
        [SerializeField] private ChapterThemeSO _theme;
        [SerializeField] private GameObject _roomPrefab;

        private MazeGrid _grid;
        private readonly Dictionary<Vector2Int, RoomController> _instantiatedRooms = new();
        private Transform _dungeonRoot;

        public Transform DungeonRoot => _dungeonRoot;
        public MazeGrid Grid => _grid;
        public float RoomSize => _roomSize;
        public IReadOnlyDictionary<Vector2Int, RoomController> InstantiatedRooms => _instantiatedRooms;

        public RoomController GetRoomAt(Vector2Int pos)
        {
            if (_instantiatedRooms.TryGetValue(pos, out var room))
            {
                return room;
            }
            return null;
        }

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            GenerateDungeon();
        }

        public void ApplyConfigFromTable()
        {
            if (Data.GameDataManager.Instance != null && Data.GameDataManager.Instance.DungeonConfig != null)
            {
                var cfg = Data.GameDataManager.Instance.DungeonConfig;
                _gridSize = cfg.gridSize;
                _roomSize = cfg.roomSize;
                _wallHeight = cfg.wallHeight;
                _doorWidth = cfg.doorWidth;
                _extraLoopRatio = cfg.extraLoopRatio;
                _minDistanceForKeyRoom = cfg.minDistanceForKeyRoom;
            }

            if (_roomPrefab == null && Data.GameDataManager.Instance != null && Data.GameDataManager.Instance.Prefabs != null)
            {
                _roomPrefab = Data.GameDataManager.Instance.Prefabs.dungeonRoomPrefab;
            }
        }

        /// <summary>
        /// 전체 던전 절차적 생성 및 정밀 흐름도 검증 파이프라인
        /// </summary>
        public void GenerateDungeon()
        {
            ApplyConfigFromTable();
            ClearExistingDungeon();

            const int maxRetries = 50;
            bool generationSuccess = false;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                _grid = new MazeGrid(_gridSize);

                // 1. 보스방(N-1, N-1)을 제외한 모든 일반 방(N*N - 1개)의 논리적 연결 통로 생성 (DFS Spanning Tree)
                GenerateMazeDFS_ExcludingBoss(_grid);

                // 2. 보스방을 인접한 일반 방 하나와 단일 통로로 연결 (Dead-End 보장: 배후 고립 방 발생 원천 차단)
                ConnectBossRoomTerminal(_grid);

                // 3. 일반방들 사이에만 순환 경로(Loop) 추가 (보스방은 제외하여 단일 진입로 유지)
                AddExtraLoops_ExcludingBoss(_grid, _extraLoopRatio);

                // 4. 방 타입 배치 (Start, Boss, KeyRoom [중간보스], Trap)
                AssignRoomTypes(_grid);

                // 5. 전체 흐름도 및 도달 가능성 정밀 검증
                if (ValidateDungeonFlow(_grid, out string validationMsg))
                {
                    generationSuccess = true;
                    Debug.Log($"<color=lime>[MazeGenerator] 던전 검증 100% 통과 (시도 {attempt}회): {validationMsg}</color>");
                    break;
                }
                else
                {
                    Debug.LogWarning($"[MazeGenerator] 던전 검증 실패 (시도 {attempt}/{maxRetries}): {validationMsg}. 재생성합니다.");
                }
            }

            if (!generationSuccess)
            {
                Debug.LogError("[MazeGenerator] 최대 재시도 횟수 초과! 안전 비상 던전을 생성합니다.");
                BuildEmergencyGuaranteedDungeon(_grid);
            }

            // 6. 물리적 방 지오메트리 씬 인스턴스화
            InstantiateDungeonRooms(_grid);

            // 7. 던전 포그(3단계 시야) 및 자동 개폐 문짝(AutoDoor) 설치
            SetupDungeonFogAndAutoDoors(_grid);

            // 8. 플레이어 스폰 위치 (0,0) 배치 (요괴 스폰 전 플레이어를 먼저 시작방에 위치시켜 오감지 방지)
            PositionPlayerAtStart();

            // 9. 전 방 몬스터 사전 스폰 (주변 방들에 미리 요괴 배치)
            PreSpawnAllRoomMonsters();

            // 10. 보스방/열쇠방 무결성 최종 검증 (보스 및 열쇠를 가진 정예 몬스터 100% 보장)
            EnsureSpecialRoomsAndMonsters();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartStage();
            }

            Debug.Log($"<color=green>[MazeGenerator] {_gridSize}x{_gridSize} (총 {_gridSize * _gridSize}개 방, 각 {_roomSize}m) 미로 던전 생성 완료!</color>");
        }

        private void ClearExistingDungeon()
        {
            // 1. 코루틴 및 이전 방 상태 정지
            StopAllCoroutines();
            foreach (var room in _instantiatedRooms.Values)
            {
                if (room != null)
                {
                    room.StopAllCoroutines();
                }
            }

            // 2. 씬에 남아있는 모든 요괴 제거 및 인디케이터 정리
            var allMonsters = FindObjectsByType<MonsterAI>(FindObjectsInactive.Include);
            foreach (var m in allMonsters)
            {
                if (m != null)
                {
                    m.CleanupTelegraph();
                    Destroy(m.gameObject);
                }
            }
            MonsterAI.ActiveMonsters.Clear();
            UI.MonsterHealthBarManager.Instance?.ClearAll();
            Core.ObjectPoolManager.Instance?.ClearAll();

            // 3. 씬에 남아있는 모든 투사체 제거
            var projectiles = FindObjectsByType<Combat.Projectile>(FindObjectsInactive.Include);
            foreach (var p in projectiles)
            {
                if (p != null) Destroy(p.gameObject);
            }
            var zhuProjectiles = FindObjectsByType<Combat.ZhuParabolicProjectile>(FindObjectsInactive.Include);
            foreach (var zp in zhuProjectiles)
            {
                if (zp != null) Destroy(zp.gameObject);
            }

            // 4. 씬에 드롭된 모든 보석 및 하트 제거
            for (int i = Actor.ExpGem.ActiveGems.Count - 1; i >= 0; i--)
            {
                if (Actor.ExpGem.ActiveGems[i] != null)
                {
                    Destroy(Actor.ExpGem.ActiveGems[i].gameObject);
                }
            }
            Actor.ExpGem.ActiveGems.Clear();
            var gems = FindObjectsByType<ExpGem>(FindObjectsInactive.Include);
            foreach (var g in gems)
            {
                if (g != null) Destroy(g.gameObject);
            }

            for (int i = Actor.RecoveryHeart.ActiveHearts.Count - 1; i >= 0; i--)
            {
                if (Actor.RecoveryHeart.ActiveHearts[i] != null)
                {
                    Destroy(Actor.RecoveryHeart.ActiveHearts[i].gameObject);
                }
            }
            Actor.RecoveryHeart.ActiveHearts.Clear();
            var hearts = FindObjectsByType<RecoveryHeart>(FindObjectsInactive.Include);
            foreach (var h in hearts)
            {
                if (h != null) Destroy(h.gameObject);
            }

            // 5. 씬에 남아있는 모든 텔레그래프 및 충격파 인디케이터 제거
            var allObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            foreach (var go in allObjects)
            {
                if (go != null && (go.name.Contains("Telegraph") || go.name.Contains("Shockwave")))
                {
                    Destroy(go);
                }
            }

            // 6. 던전 루트 오브젝트 및 방 인스턴스 초기화
            if (_dungeonRoot != null)
            {
                Destroy(_dungeonRoot.gameObject);
            }
            _dungeonRoot = new GameObject("[Generated_Dungeon]").transform;
            _dungeonRoot.SetParent(transform, false);
            _dungeonRoot.localPosition = Vector3.zero;
            _dungeonRoot.localRotation = Quaternion.identity;
            _dungeonRoot.localScale = Vector3.one;
            _instantiatedRooms.Clear();

            // 7. 보스 체력바 UI 및 상태 초기화
            UI.BossHealthBarUI.Instance?.Hide();
        }

        /// <summary>
        /// 보스방을 제외한 모든 일반 방(N*N - 1개)을 DFS Spanning Tree로 100% 연결
        /// </summary>
        private void GenerateMazeDFS_ExcludingBoss(MazeGrid grid)
        {
            Vector2Int bossPos = new Vector2Int(grid.size - 1, grid.size - 1);
            Stack<MazeCell> stack = new();
            MazeCell current = grid.GetCell(0, 0);
            current.visited = true;
            stack.Push(current);

            while (stack.Count > 0)
            {
                current = stack.Peek();
                List<(Direction dir, MazeCell neighbor)> unvisitedNeighbors = new();

                foreach (Direction dir in System.Enum.GetValues(typeof(Direction)))
                {
                    Vector2Int nextPos = current.gridPos + MazeGrid.GetOffset(dir);
                    if (nextPos == bossPos) continue; // 보스방은 메인 DFS 탐색 대상에서 제외

                    MazeCell neighbor = grid.GetCell(nextPos);
                    if (neighbor != null && !neighbor.visited)
                    {
                        unvisitedNeighbors.Add((dir, neighbor));
                    }
                }

                if (unvisitedNeighbors.Count > 0)
                {
                    var chosen = unvisitedNeighbors[Random.Range(0, unvisitedNeighbors.Count)];
                    current.SetOpen(chosen.dir, true);
                    chosen.neighbor.SetOpen(MazeGrid.GetOpposite(chosen.dir), true);

                    chosen.neighbor.visited = true;
                    stack.Push(chosen.neighbor);
                }
                else
                {
                    stack.Pop();
                }
            }
        }

        /// <summary>
        /// 보스방을 인접한 일반 방 하나와 단일 통로로 연결 (Dead-End 보장: 배후 고립 방 발생 원천 차단)
        /// </summary>
        private void ConnectBossRoomTerminal(MazeGrid grid)
        {
            Vector2Int bossPos = new Vector2Int(grid.size - 1, grid.size - 1);
            MazeCell bossCell = grid.GetCell(bossPos);

            List<(Direction dirFromBoss, Vector2Int neighborPos)> validNeighbors = new();

            Vector2Int southPos = bossPos + MazeGrid.GetOffset(Direction.South);
            if (grid.GetCell(southPos) != null) validNeighbors.Add((Direction.South, southPos));

            Vector2Int westPos = bossPos + MazeGrid.GetOffset(Direction.West);
            if (grid.GetCell(westPos) != null) validNeighbors.Add((Direction.West, westPos));

            if (validNeighbors.Count > 0)
            {
                var chosen = validNeighbors[Random.Range(0, validNeighbors.Count)];
                MazeCell neighbor = grid.GetCell(chosen.neighborPos);

                bossCell.SetOpen(chosen.dirFromBoss, true);
                neighbor.SetOpen(MazeGrid.GetOpposite(chosen.dirFromBoss), true);
                bossCell.visited = true;
            }
        }

        /// <summary>
        /// 일반 방들 사이에만 순환 경로(Loop) 추가 (보스방은 제외하여 단일 진입로 및 최종 목적지 성격 유지)
        /// </summary>
        private void AddExtraLoops_ExcludingBoss(MazeGrid grid, float ratio)
        {
            Vector2Int bossPos = new Vector2Int(grid.size - 1, grid.size - 1);

            for (int x = 0; x < grid.size; x++)
            {
                for (int y = 0; y < grid.size; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    if (pos == bossPos) continue;

                    MazeCell cell = grid.GetCell(pos);
                    if (cell == null) continue;

                    foreach (Direction dir in new[] { Direction.North, Direction.East })
                    {
                        Vector2Int nextPos = cell.gridPos + MazeGrid.GetOffset(dir);
                        if (nextPos == bossPos) continue; // 보스방으로의 추가 통로 금지

                        MazeCell neighbor = grid.GetCell(nextPos);
                        if (neighbor != null && !cell.IsOpen(dir))
                        {
                            if (Random.value < ratio)
                            {
                                cell.SetOpen(dir, true);
                                neighbor.SetOpen(MazeGrid.GetOpposite(dir), true);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 방 타입 배치: 시작방, 보스방, 중간보스(열쇠방), 함정방
        /// - 보스방을 통과하지 않는 BFS 거리 계산
        /// - 중간보스방(열쇠방)은 시작방으로부터 충분한 거리(_minDistanceForKeyRoom)를 확보하며 보스방 입구가 아닌 위치로 엄선
        /// </summary>
        private void AssignRoomTypes(MazeGrid grid)
        {
            Vector2Int startPos = new Vector2Int(0, 0);
            Vector2Int bossPos = new Vector2Int(grid.size - 1, grid.size - 1);

            for (int x = 0; x < grid.size; x++)
            {
                for (int y = 0; y < grid.size; y++)
                {
                    var c = grid.GetCell(x, y);
                    c.roomType = RoomType.Normal;
                    c.distanceFromStart = -1;
                }
            }

            grid.GetCell(startPos).roomType = RoomType.Start;
            grid.GetCell(bossPos).roomType = RoomType.Boss;

            // 보스방을 제외한 BFS로 시작방으로부터의 거리 계산
            Queue<MazeCell> queue = new();
            MazeCell startCell = grid.GetCell(startPos);
            startCell.distanceFromStart = 0;
            queue.Enqueue(startCell);

            List<MazeCell> candidateKeyRooms = new();

            while (queue.Count > 0)
            {
                MazeCell curr = queue.Dequeue();

                foreach (Direction dir in System.Enum.GetValues(typeof(Direction)))
                {
                    if (curr.IsOpen(dir))
                    {
                        Vector2Int nextPos = curr.gridPos + MazeGrid.GetOffset(dir);
                        if (nextPos == bossPos) continue; // 보스방 통과 금지!

                        MazeCell n = grid.GetCell(nextPos);
                        if (n != null && n.distanceFromStart == -1)
                        {
                            n.distanceFromStart = curr.distanceFromStart + 1;
                            queue.Enqueue(n);

                            // 중간보스(열쇠방) 후보 조건:
                            // 1) 시작방 제외
                            // 2) 보스방 바로 앞 입구 방 제외 (보스방과 맨해튼 거리 > 1)
                            // 3) 시작방으로부터 최소 거리 이상
                            int distToBoss = Mathf.Abs(n.gridPos.x - bossPos.x) + Mathf.Abs(n.gridPos.y - bossPos.y);
                            if (n.distanceFromStart >= _minDistanceForKeyRoom && distToBoss > 1)
                            {
                                candidateKeyRooms.Add(n);
                            }
                        }
                    }
                }
            }

            // 후보군이 없을 경우(극단적인 미로 형태), 보스방/시작방 제외 가장 먼 방으로 폴백
            if (candidateKeyRooms.Count == 0)
            {
                int maxDist = -1;
                MazeCell bestCell = null;
                for (int x = 0; x < grid.size; x++)
                {
                    for (int y = 0; y < grid.size; y++)
                    {
                        var cell = grid.GetCell(x, y);
                        if (cell.gridPos != startPos && cell.gridPos != bossPos && cell.distanceFromStart > maxDist)
                        {
                            maxDist = cell.distanceFromStart;
                            bestCell = cell;
                        }
                    }
                }

                // 만약 모든 distanceFromStart가 -1인 경우에도 시작/보스 제외 유효 방 100% 탐색
                if (bestCell == null)
                {
                    for (int x = 0; x < grid.size; x++)
                    {
                        for (int y = 0; y < grid.size; y++)
                        {
                            var cell = grid.GetCell(x, y);
                            if (cell.gridPos != startPos && cell.gridPos != bossPos)
                            {
                                bestCell = cell;
                                break;
                            }
                        }
                        if (bestCell != null) break;
                    }
                }

                if (bestCell != null) candidateKeyRooms.Add(bestCell);
            }

            // 열쇠방(중간보스) 지정
            if (candidateKeyRooms.Count > 0)
            {
                int keyIdx = Random.Range(0, candidateKeyRooms.Count);
                MazeCell keyCell = candidateKeyRooms[keyIdx];
                keyCell.roomType = RoomType.KeyRoom;
                candidateKeyRooms.RemoveAt(keyIdx);
            }

            // 함정방 지정 (그리드 크기에 맞춰 2~3개 지정)
            int trapCount = Mathf.Clamp(grid.size / 2, 1, 3);
            for (int i = 0; i < trapCount && candidateKeyRooms.Count > 0; i++)
            {
                int trapIdx = Random.Range(0, candidateKeyRooms.Count);
                candidateKeyRooms[trapIdx].roomType = RoomType.Trap;
                candidateKeyRooms.RemoveAt(trapIdx);
            }
        }

        /// <summary>
        /// 던전의 전체 흐름도 및 도달 가능성 정밀 검증
        /// 1. 시작방에서 보스방을 거치지 않고 모든 일반방(N*N - 1개)에 도달 가능한지 검증
        /// 2. 보스방 뒤에 갇혀있거나, 보스방을 지나야만 갈 수 있는 방이 전혀 없는지(0개) 검증
        /// 3. 중간보스(열쇠방)가 존재하며 보스방을 거치지 않고 도달 가능한지 검증
        /// 4. 중간보스 처치 후 보스방 입구로 도달 가능한 유효 경로가 있는지 검증
        /// 5. 인접한 모든 방의 문 개폐 상태가 상호 일치하는지(Reciprocal) 검증
        /// </summary>
        public bool ValidateDungeonFlow(MazeGrid grid, out string failureReason)
        {
            Vector2Int startPos = new Vector2Int(0, 0);
            Vector2Int bossPos = new Vector2Int(grid.size - 1, grid.size - 1);
            int totalRooms = grid.size * grid.size;
            int expectedNonBossRooms = totalRooms - 1;

            // 1. 보스방을 거치지 않는 BFS 탐색으로 모든 방의 접근성 확인
            HashSet<Vector2Int> reachableWithoutBoss = new();
            Queue<Vector2Int> queue = new();

            queue.Enqueue(startPos);
            reachableWithoutBoss.Add(startPos);

            while (queue.Count > 0)
            {
                Vector2Int curr = queue.Dequeue();
                MazeCell cell = grid.GetCell(curr);

                foreach (Direction dir in System.Enum.GetValues(typeof(Direction)))
                {
                    if (cell.IsOpen(dir))
                    {
                        Vector2Int next = curr + MazeGrid.GetOffset(dir);
                        if (next == bossPos) continue; // 보스방은 잠겨있으므로 통과 불가

                        if (grid.GetCell(next) != null && !reachableWithoutBoss.Contains(next))
                        {
                            reachableWithoutBoss.Add(next);
                            queue.Enqueue(next);
                        }
                    }
                }
            }

            // 검증 1: 보스방을 제외한 모든 방(N*N - 1개)이 도달 가능한가?
            if (reachableWithoutBoss.Count != expectedNonBossRooms)
            {
                int unreachableCount = expectedNonBossRooms - reachableWithoutBoss.Count;
                failureReason = $"보스방을 통과하지 않고 도달할 수 없는 방이 {unreachableCount}개 발견됨 (도달 가능: {reachableWithoutBoss.Count}/{expectedNonBossRooms})";
                return false;
            }

            // 검증 2: 중간보스(열쇠방) 존재 및 도달성 검증
            MazeCell keyRoomCell = null;
            for (int x = 0; x < grid.size; x++)
            {
                for (int y = 0; y < grid.size; y++)
                {
                    var c = grid.GetCell(x, y);
                    if (c.roomType == RoomType.KeyRoom)
                    {
                        keyRoomCell = c;
                        break;
                    }
                }
                if (keyRoomCell != null) break;
            }

            if (keyRoomCell == null)
            {
                failureReason = "중간보스(열쇠방)가 배치되지 않음";
                return false;
            }

            if (!reachableWithoutBoss.Contains(keyRoomCell.gridPos))
            {
                failureReason = $"중간보스방({keyRoomCell.gridPos})이 보스방을 거치지 않고는 갈 수 없음";
                return false;
            }

            // 검증 3: 보스방 입구 검증 (보스방과 연결된 방 중 최소 하나가 reachableWithoutBoss에 포함되어야 함)
            MazeCell bossCell = grid.GetCell(bossPos);
            int bossDoorCount = 0;
            Vector2Int bossEntrancePos = Vector2Int.zero;

            foreach (Direction dir in System.Enum.GetValues(typeof(Direction)))
            {
                if (bossCell.IsOpen(dir))
                {
                    bossDoorCount++;
                    bossEntrancePos = bossPos + MazeGrid.GetOffset(dir);
                }
            }

            if (bossDoorCount == 0)
            {
                failureReason = "보스방으로 들어가는 문이 전혀 없음";
                return false;
            }

            if (!reachableWithoutBoss.Contains(bossEntrancePos))
            {
                failureReason = $"보스방 진입로({bossEntrancePos})가 시작방에서 도달 불가능함";
                return false;
            }

            // 검증 4: 모든 방의 문 개폐 상태 상호 일치(Reciprocal) 검증
            for (int x = 0; x < grid.size; x++)
            {
                for (int y = 0; y < grid.size; y++)
                {
                    MazeCell cell = grid.GetCell(x, y);
                    foreach (Direction dir in System.Enum.GetValues(typeof(Direction)))
                    {
                        if (cell.IsOpen(dir))
                        {
                            Vector2Int nPos = cell.gridPos + MazeGrid.GetOffset(dir);
                            MazeCell nCell = grid.GetCell(nPos);
                            if (nCell == null || !nCell.IsOpen(MazeGrid.GetOpposite(dir)))
                            {
                                failureReason = $"방 ({x},{y}) {dir} 방향 문과 인접 방의 문 연결 불일치";
                                return false;
                            }
                        }
                    }
                }
            }

            failureReason = $"시작(0,0) -> 중간보스(열쇠:{keyRoomCell.gridPos}, 거리 {keyRoomCell.distanceFromStart}) -> 보스({bossPos}), 총 {totalRooms}개 방 100% 정상 연결 확인";
            return true;
        }

        /// <summary>
        /// 비상 시 100% 안전 보장 던전 생성 (스네이크형 보장 경로)
        /// </summary>
        private void BuildEmergencyGuaranteedDungeon(MazeGrid grid)
        {
            Vector2Int bossPos = new Vector2Int(grid.size - 1, grid.size - 1);

            for (int y = 0; y < grid.size; y++)
            {
                for (int x = 0; x < grid.size; x++)
                {
                    MazeCell cell = grid.GetCell(x, y);
                    if (x < grid.size - 1)
                    {
                        cell.SetOpen(Direction.East, true);
                        grid.GetCell(x + 1, y)?.SetOpen(Direction.West, true);
                    }
                    if (y < grid.size - 1 && (x == 0 || x == grid.size - 1))
                    {
                        cell.SetOpen(Direction.North, true);
                        grid.GetCell(x, y + 1)?.SetOpen(Direction.South, true);
                    }
                }
            }

            grid.GetCell(0, 0).roomType = RoomType.Start;
            grid.GetCell(bossPos).roomType = RoomType.Boss;
            grid.GetCell(grid.size / 2, grid.size / 2).roomType = RoomType.KeyRoom;
        }

        private void InstantiateDungeonRooms(MazeGrid grid)
        {
            for (int x = 0; x < grid.size; x++)
            {
                for (int y = 0; y < grid.size; y++)
                {
                    MazeCell cell = grid.GetCell(x, y);
                    Vector3 worldPos = new Vector3(x * _roomSize, 0f, y * _roomSize);

                    RoomController roomCtrl = CreateRoomInstance(cell, worldPos);
                    _instantiatedRooms[cell.gridPos] = roomCtrl;
                }
            }
        }

        private void SetupDungeonFogAndAutoDoors(MazeGrid grid)
        {
            Vector2Int startPos = new Vector2Int(0, 0);
            HashSet<string> installedDoorPairs = new();

            foreach (var kvp in _instantiatedRooms)
            {
                Vector2Int pos = kvp.Key;
                RoomController room = kvp.Value;
                MazeCell cell = grid.GetCell(pos);
                if (room == null || cell == null) continue;

                // 1. 방 포그 오버레이 초기화
                room.InitializeFogOverlay(_roomSize);

                // 2. 자동 개폐 문짝(AutoDoor) 설치
                foreach (Direction dir in System.Enum.GetValues(typeof(Direction)))
                {
                    if (cell.IsOpen(dir))
                    {
                        Vector2Int neighborPos = pos + MazeGrid.GetOffset(dir);
                        string pairKey = pos.x < neighborPos.x || (pos.x == neighborPos.x && pos.y < neighborPos.y)
                            ? $"{pos.x}_{pos.y}:{neighborPos.x}_{neighborPos.y}"
                            : $"{neighborPos.x}_{neighborPos.y}:{pos.x}_{pos.y}";

                        var socket = room.GetSocket(dir);
                        if (socket == null || (socket.CurrentState != DoorState.Open && socket.CurrentState != DoorState.BossLock)) continue;

                        // 모든 개방 통로 및 보스문에 문짝 설치 (개방 시 영구 개방 및 웨이브 트리거)
                        if (!installedDoorPairs.Contains(pairKey))
                        {
                            InstallAutoDoorOnSocket(socket);
                            installedDoorPairs.Add(pairKey);
                        }
                    }
                }
            }

            // 던전 포그 매니저 활성화 및 초기 시야 갱신
            if (FindAnyObjectByType<DungeonFogManager>() == null)
            {
                var fogMgrObj = new GameObject("[DungeonFogManager]");
                fogMgrObj.AddComponent<DungeonFogManager>();
            }
            DungeonFogManager.Instance.ResetFog();
            DungeonFogManager.Instance.UpdateAllRoomFogStates();
        }

        private void InstallAutoDoorOnSocket(DoorSocket socket)
        {
            if (socket.GetComponentInChildren<DungeonAutoDoor>() != null) return;

            var autoDoorObj = new GameObject("AutoDoor");
            autoDoorObj.transform.SetParent(socket.transform, false);
            autoDoorObj.transform.localPosition = Vector3.zero;
            autoDoorObj.transform.localRotation = Quaternion.identity;

            autoDoorObj.AddComponent<DungeonAutoDoor>();
        }

        private void PreSpawnAllRoomMonsters()
        {
            foreach (var kvp in _instantiatedRooms)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.PreSpawnMonsters();
                }
            }
        }

        /// <summary>
        /// 보스방과 열쇠방(중간보스)의 방 배치 및 몬스터 소환 상태를 100% 보장하는 최종 안전장치
        /// </summary>
        private void EnsureSpecialRoomsAndMonsters()
        {
            RoomController bossRoom = null;
            RoomController keyRoom = null;

            foreach (var kvp in _instantiatedRooms)
            {
                if (kvp.Value == null) continue;
                if (kvp.Value.RoomType == RoomType.Boss) bossRoom = kvp.Value;
                if (kvp.Value.RoomType == RoomType.KeyRoom) keyRoom = kvp.Value;
            }

            Vector2Int bossPos = new Vector2Int(_gridSize - 1, _gridSize - 1);
            if (bossRoom == null && _instantiatedRooms.TryGetValue(bossPos, out var fallbackBossRoom))
            {
                Debug.LogWarning($"[MazeGenerator] 보스방 타입 누락 감지! ({bossPos.x},{bossPos.y}) 방을 보스방으로 강제 지정합니다.");
                fallbackBossRoom.SetupRoom(bossPos, RoomType.Boss);
                bossRoom = fallbackBossRoom;
            }

            if (bossRoom != null)
            {
                bossRoom.EnsureBossSpawned();
            }

            if (keyRoom == null)
            {
                // 중간보스방이 없는 경우, 시작방과 보스방을 제외한 방 중 하나를 강제로 KeyRoom 지정
                foreach (var kvp in _instantiatedRooms)
                {
                    if (kvp.Key != Vector2Int.zero && kvp.Key != bossPos && kvp.Value != null)
                    {
                        keyRoom = kvp.Value;
                        keyRoom.SetupRoom(kvp.Key, RoomType.KeyRoom);
                        Debug.LogWarning($"[MazeGenerator] 열쇠방 타입 누락 감지! ({kvp.Key.x},{kvp.Key.y}) 방을 열쇠방으로 강제 지정합니다.");
                        break;
                    }
                }
            }

            if (keyRoom != null)
            {
                keyRoom.EnsureKeyEliteSpawned();
            }
        }

        private RoomController CreateRoomInstance(MazeCell cell, Vector3 worldPos)
        {
            GameObject roomObj;
            RoomController roomCtrl;

            if (_roomPrefab != null)
            {
                roomObj = Instantiate(_roomPrefab, worldPos, Quaternion.identity, _dungeonRoot);
                roomCtrl = roomObj.GetComponent<RoomController>();
            }
            else
            {
                roomObj = BuildStandardRoomGeometry(cell, worldPos);
                roomCtrl = roomObj.GetComponent<RoomController>();
            }

            roomObj.transform.position = worldPos;
            roomCtrl.SetupRoom(cell.gridPos, cell.roomType);

            var floorTransform = roomObj.transform.Find("Floor");
            if (floorTransform != null && floorTransform.TryGetComponent<Renderer>(out var floorRend))
            {
                Color floorCol = cell.roomType switch
                {
                    RoomType.Start => new Color(0.18f, 0.35f, 0.22f),
                    RoomType.Boss => new Color(0.45f, 0.15f, 0.15f),
                    RoomType.KeyRoom => new Color(0.45f, 0.38f, 0.12f),
                    RoomType.Trap => new Color(0.32f, 0.18f, 0.38f),
                    _ => new Color(0.22f, 0.24f, 0.28f)
                };
                floorRend.material.color = floorCol;
                if (floorRend.material.HasProperty("_BaseColor")) floorRend.material.SetColor("_BaseColor", floorCol);
            }

            // 각 문 소켓 상태 설정
            foreach (Direction dir in System.Enum.GetValues(typeof(Direction)))
            {
                bool isOpen = cell.IsOpen(dir);
                DoorState state = DoorState.Wall;

                if (isOpen)
                {
                    Vector2Int neighborPos = cell.gridPos + MazeGrid.GetOffset(dir);
                    MazeCell neighbor = _grid.GetCell(neighborPos);

                    if (neighbor != null && neighbor.roomType == RoomType.Boss)
                    {
                        state = DoorState.BossLock;
                    }
                    else
                    {
                        state = DoorState.Open;
                    }
                }

                roomCtrl.SetSocketState(dir, state);
            }

            return roomCtrl;
        }

        private GameObject BuildStandardRoomGeometry(MazeCell cell, Vector3 worldPos)
        {
            GameObject room = new GameObject($"Room_{cell.gridPos.x}_{cell.gridPos.y}_{cell.roomType}");
            room.transform.SetPositionAndRotation(worldPos, Quaternion.identity);
            room.transform.SetParent(_dungeonRoot);

            var ctrl = room.AddComponent<RoomController>();

            // 1. 바닥 (Floor)
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.SetParent(room.transform, false);
            floor.transform.localPosition = Vector3.zero;
            floor.transform.localRotation = Quaternion.identity;
            floor.transform.localScale = new Vector3(_roomSize / 10f, 1f, _roomSize / 10f);

            var floorRend = floor.GetComponent<Renderer>();
            if (floorRend != null)
            {
                Color floorCol = cell.roomType switch
                {
                    RoomType.Start => new Color(0.18f, 0.35f, 0.22f), // 시작방(안전 녹색)
                    RoomType.Boss => new Color(0.45f, 0.15f, 0.15f),  // 보스방(위험 암적색)
                    RoomType.KeyRoom => new Color(0.45f, 0.38f, 0.12f),// 열쇠방(황금빛)
                    RoomType.Trap => new Color(0.32f, 0.18f, 0.38f),  // 함정방(자주색)
                    _ => new Color(0.22f, 0.24f, 0.28f)              // 일반 미로방(석재 회색)
                };
                floorRend.material.color = floorCol;
                if (floorRend.material.HasProperty("_BaseColor")) floorRend.material.SetColor("_BaseColor", floorCol);
            }

            // 2. 방 트리거 콜라이더
            var boxCol = room.AddComponent<BoxCollider>();
            boxCol.isTrigger = true;
            boxCol.center = new Vector3(0f, 2f, 0f);
            boxCol.size = new Vector3(_roomSize * 0.9f, 4f, _roomSize * 0.9f);

            // 3. 4방향 문 소켓 빌드 (North: 0°, East: 90°, South: 180°, West: 270°)
            BuildDoorSocket(room, ctrl, Direction.North, new Vector3(0f, 0f, _roomSize * 0.5f), Quaternion.identity);
            BuildDoorSocket(room, ctrl, Direction.East, new Vector3(_roomSize * 0.5f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f));
            BuildDoorSocket(room, ctrl, Direction.South, new Vector3(0f, 0f, -_roomSize * 0.5f), Quaternion.Euler(0f, 180f, 0f));
            BuildDoorSocket(room, ctrl, Direction.West, new Vector3(-_roomSize * 0.5f, 0f, 0f), Quaternion.Euler(0f, 270f, 0f));

            return room;
        }

        private void BuildDoorSocket(GameObject room, RoomController ctrl, Direction dir, Vector3 localPos, Quaternion localRot)
        {
            GameObject socketObj = new GameObject($"Socket_{dir}");
            socketObj.transform.SetParent(room.transform, false);
            socketObj.transform.localPosition = localPos;
            socketObj.transform.localRotation = localRot;

            var socket = socketObj.AddComponent<DoorSocket>();

            float doorWidth = _doorWidth; // 출입문 개방 통로 너비 (2배 확대: 12.0m)
            float sideWallLen = (_roomSize - doorWidth) * 0.5f; // (26 - 6) / 2 = 10m
            float sideOffset = (doorWidth + sideWallLen) * 0.5f; // (6 + 10) / 2 = 8m

            Color wallColor = new Color(0.28f, 0.3f, 0.35f); // 어두운 청회색 석벽

            // 1. 좌측 코너 고정 벽체 (항상 존재)
            GameObject leftWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftWall.name = "SideWall_Left";
            leftWall.transform.SetParent(socketObj.transform, false);
            leftWall.transform.localPosition = new Vector3(-sideOffset, _wallHeight * 0.5f, 0f);
            leftWall.transform.localRotation = Quaternion.identity;
            leftWall.transform.localScale = new Vector3(sideWallLen, _wallHeight, 0.9f);
            SetObjColor(leftWall, wallColor);

            // 2. 우측 코너 고정 벽체 (항상 존재)
            GameObject rightWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightWall.name = "SideWall_Right";
            rightWall.transform.SetParent(socketObj.transform, false);
            rightWall.transform.localPosition = new Vector3(sideOffset, _wallHeight * 0.5f, 0f);
            rightWall.transform.localRotation = Quaternion.identity;
            rightWall.transform.localScale = new Vector3(sideWallLen, _wallHeight, 0.9f);
            SetObjColor(rightWall, wallColor);

            // 3. 중앙 통로 폐쇄 벽체 (DoorState.Wall 일 때만 활성화)
            GameObject centerWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            centerWall.name = "SolidCenterWall";
            centerWall.transform.SetParent(socketObj.transform, false);
            centerWall.transform.localPosition = new Vector3(0f, _wallHeight * 0.5f, 0f);
            centerWall.transform.localRotation = Quaternion.identity;
            centerWall.transform.localScale = new Vector3(doorWidth + 0.1f, _wallHeight, 0.9f);
            SetObjColor(centerWall, wallColor);

            // 4. 중앙 잠금 봉인 배리어 (CombatLock/BossLock 시 차단)
            GameObject barrier = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barrier.name = "BarrierVisual";
            barrier.transform.SetParent(socketObj.transform, false);
            barrier.transform.localPosition = new Vector3(0f, _wallHeight * 0.5f, 0f);
            barrier.transform.localRotation = Quaternion.identity;
            barrier.transform.localScale = new Vector3(doorWidth + 0.2f, _wallHeight * 0.95f, 0.6f);

            // 5. 문 기둥 (Door Pillars)
            GameObject pillars = new GameObject("DoorPillars");
            pillars.transform.SetParent(socketObj.transform, false);
            pillars.transform.localPosition = Vector3.zero;
            pillars.transform.localRotation = Quaternion.identity;

            GameObject pLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pLeft.name = "Pillar_Left";
            pLeft.transform.SetParent(pillars.transform, false);
            pLeft.transform.localPosition = new Vector3(-doorWidth * 0.5f, _wallHeight * 0.5f, 0f);
            pLeft.transform.localRotation = Quaternion.identity;
            pLeft.transform.localScale = new Vector3(0.7f, _wallHeight * 1.05f, 1.2f);
            SetObjColor(pLeft, new Color(0.18f, 0.2f, 0.24f));

            GameObject pRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pRight.name = "Pillar_Right";
            pRight.transform.SetParent(pillars.transform, false);
            pRight.transform.localPosition = new Vector3(doorWidth * 0.5f, _wallHeight * 0.5f, 0f);
            pRight.transform.localRotation = Quaternion.identity;
            pRight.transform.localScale = new Vector3(0.7f, _wallHeight * 1.05f, 1.2f);
            SetObjColor(pRight, new Color(0.18f, 0.2f, 0.24f));

            socket.InitializeComponents(dir, centerWall, barrier, pillars);
            SetRoomSocketPrivateField(ctrl, dir, socket);
        }

        private void SetObjColor(GameObject go, Color color)
        {
            var rend = go.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = color;
                if (rend.material.HasProperty("_BaseColor"))
                {
                    rend.material.SetColor("_BaseColor", color);
                }
            }
        }

        private void SetRoomSocketPrivateField(RoomController ctrl, Direction dir, DoorSocket socket)
        {
            string fieldName = dir switch
            {
                Direction.North => "_socketNorth",
                Direction.East => "_socketEast",
                Direction.South => "_socketSouth",
                Direction.West => "_socketWest",
                _ => ""
            };
            var field = typeof(RoomController).GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(ctrl, socket);
        }

        private void PositionPlayerAtStart()
        {
            var player = FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                var cc = player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                player.transform.position = new Vector3(0f, 1f, 0f);
                Physics.SyncTransforms();
                if (cc != null) cc.enabled = true;
            }

            Core.HelperManager.Instance?.RepositionBehindPlayer();
        }

        /// <summary>
        /// 보스방 진입 시 인접 방의 연결 소켓도 함께 봉인하여 퇴로 차단
        /// </summary>
        public void LockBossRoomNeighborSockets(Vector2Int bossPos)
        {
            foreach (Direction dir in System.Enum.GetValues(typeof(Direction)))
            {
                Vector2Int neighborPos = bossPos + MazeGrid.GetOffset(dir);
                if (_instantiatedRooms.TryGetValue(neighborPos, out var neighborRoom) && neighborRoom != null)
                {
                    Direction oppositeDir = MazeGrid.GetOpposite(dir);
                    var socket = neighborRoom.GetSocket(oppositeDir);
                    if (socket != null && socket.CurrentState != DoorState.Wall)
                    {
                        socket.LockBossEncounter();
                    }
                }
            }
        }

        public void UnlockBossDoors()
        {
            Vector2Int bossPos = new Vector2Int(_gridSize - 1, _gridSize - 1);
            if (_instantiatedRooms.TryGetValue(bossPos, out var bossRoom) && bossRoom != null)
            {
                foreach (Direction dir in System.Enum.GetValues(typeof(Direction)))
                {
                    var socket = bossRoom.GetSocket(dir);
                    if (socket != null && socket.CurrentState != DoorState.Wall)
                    {
                        socket.UnlockWithKey();
                    }
                }
            }

            // 보스방과 맞닿아 있는 인접 방의 봉인 배리어 소켓 및 자동문도 함께 영구 개방
            foreach (Direction dir in System.Enum.GetValues(typeof(Direction)))
            {
                Vector2Int neighborPos = bossPos + MazeGrid.GetOffset(dir);
                if (_instantiatedRooms.TryGetValue(neighborPos, out var neighborRoom) && neighborRoom != null)
                {
                    Direction oppositeDir = MazeGrid.GetOpposite(dir);
                    var socket = neighborRoom.GetSocket(oppositeDir);
                    if (socket != null && (socket.CurrentState == DoorState.BossLock || socket.CurrentState == DoorState.Open))
                    {
                        socket.UnlockWithKey();
                    }
                }
            }

            Debug.Log("<color=gold>[MazeGenerator] 보스방 문이 열쇠로 영구 개방되었습니다!</color>");
        }

        /// <summary>
        /// Stage Clear 후 시작룸 재출발: 기존 잔존 요괴 정리, 플레이어 HP 및 위치 초기화, 새 미로 생성
        /// </summary>
        public void RestartStage()
        {
            Debug.Log("<color=cyan>[MazeGenerator] 스테이지 초기화 및 시작룸 재출발 시작!</color>");

            // 1. 플레이어 체력 및 입력 잠금 해제
            var player = FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                player.Heal(player.MaxHp);
                player.SetInputLock(false);
            }

            // 2. 새 던전 미로 생성 (ClearExistingDungeon이 잔존 요괴, 영혼석, 하트, 투사체, UI를 100% 정리 후 재생성)
            GenerateDungeon();
        }

        // ========================================================
        // 몬스터 길찾기 및 네비게이션 지원 메서드
        // ========================================================

        public Vector2Int GetRoomCoordsAtWorldPos(Vector3 worldPos)
        {
            int x = Mathf.Clamp(Mathf.RoundToInt(worldPos.x / _roomSize), 0, _gridSize - 1);
            int y = Mathf.Clamp(Mathf.RoundToInt(worldPos.z / _roomSize), 0, _gridSize - 1);
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
            return new Vector3(center.x + offset.x * _roomSize * 0.5f, 1.0f, center.z + offset.y * _roomSize * 0.5f);
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

                foreach (Direction dir in System.Enum.GetValues(typeof(Direction)))
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

            // targetRoom에서 역추적하여 currentRoom 바로 다음 방(firstStep) 도출
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
    }
}
