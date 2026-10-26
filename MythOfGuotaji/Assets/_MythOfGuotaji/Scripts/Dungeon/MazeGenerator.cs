using System;
using System.Collections.Generic;
using MythOfGuotaji.Actor;
using MythOfGuotaji.Core;
using UnityEngine;
using Random = UnityEngine.Random;

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
        [SerializeField] private Data.MapSkinTableSO _mapSkinTable;

        private Data.StageDungeonConfig _currentStageConfig;
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

            if (_mapSkinTable == null)
            {
                _mapSkinTable = Data.GameDataManager.Instance?.MapSkinTable ?? Resources.Load<Data.MapSkinTableSO>("Data/MapSkinTable");
            }
        }

        /// <summary>
        /// 전체 던전 절차적 생성: 2블럭 선형 흐름 + 최소 칸 수 보장 + 샛길 미궁 방 체계
        /// </summary>
        public void GenerateDungeon()
        {
            ApplyConfigFromTable();
            ClearExistingDungeon();

            // 스테이지별 던전 생성 파라미터 로드
            int currentStage = GameManager.Instance != null ? GameManager.Instance.CurrentStage : 1;
            var stageTable = Data.GameDataManager.Instance?.DungeonStages ?? Resources.Load<Data.DungeonStageTableSO>("Data/DungeonStageTable");
            Data.StageDungeonConfig stageCfg = stageTable != null ? stageTable.GetConfigForStage(currentStage) : new Data.StageDungeonConfig();
            _currentStageConfig = stageCfg;
            if (stageCfg.roomSize > 0f) _roomSize = stageCfg.roomSize;

            const int maxRetries = 100;
            bool generationSuccess = false;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                _grid = new MazeGrid(); // 유기적 비정방형 그리드

                if (GenerateTwoBlockLinearMaze(_grid, stageCfg, out string validationMsg))
                {
                    generationSuccess = true;
                    Debug.Log($"<color=lime>[MazeGenerator] 2블록 선형 미궁 던전 검증 통과 (시도 {attempt}회): {validationMsg}</color>");
                    break;
                }
            }

            if (!generationSuccess)
            {
                Debug.LogError("[MazeGenerator] 최대 재시도 초과! 안전 비상 2블록 던전을 생성합니다.");
                _grid = new MazeGrid();
                BuildEmergencyTwoBlockDungeon(_grid, stageCfg);
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

            Debug.Log($"<color=green>[MazeGenerator] [스테이지 {currentStage}] 2블록 선형 미궁 던전 (총 {_grid.CellCount}개 방, 각 {_roomSize}m) 생성 완료!</color>");
        }

        #region Two-Block Linear & Maze Generation Pipeline

        private List<Direction> GetAvailableDirections(MazeGrid grid, Vector2Int pos)
        {
            List<Direction> available = new();
            foreach (Direction dir in Enum.GetValues(typeof(Direction)))
            {
                Vector2Int next = pos + MazeGrid.GetOffset(dir);
                if (!grid.ContainsCell(next))
                {
                    available.Add(dir);
                }
            }
            return available;
        }

        private bool GenerateTwoBlockLinearMaze(MazeGrid grid, Data.StageDungeonConfig cfg, out string log)
        {
            Vector2Int startPos = Vector2Int.zero;
            MazeCell startCell = new MazeCell(startPos.x, startPos.y)
            {
                roomType = RoomType.Start,
                blockIndex = 1,
                isMainTrunk = true,
                visited = true
            };
            grid.AddCell(startCell);

            // 1. 블록 1 메인 경로 (Start -> Rooms -> MidBoss)
            int block1Count = UnityEngine.Random.Range(cfg.minRoomsBlock1, cfg.maxRoomsBlock1 + 1);
            Vector2Int currentPos = startPos;
            List<MazeCell> b1MainCells = new List<MazeCell> { startCell };

            for (int i = 0; i < block1Count; i++)
            {
                var dirs = GetAvailableDirections(grid, currentPos);
                if (dirs.Count == 0) { log = "Block 1 path stuck"; return false; }
                Direction dir = dirs[UnityEngine.Random.Range(0, dirs.Count)];
                Vector2Int nextPos = currentPos + MazeGrid.GetOffset(dir);

                MazeCell nextCell = new MazeCell(nextPos.x, nextPos.y)
                {
                    roomType = RoomType.Normal,
                    blockIndex = 1,
                    isMainTrunk = true,
                    visited = true
                };
                grid.AddCell(nextCell);
                grid.GetCell(currentPos).SetOpen(dir, true);
                nextCell.SetOpen(MazeGrid.GetOpposite(dir), true);

                b1MainCells.Add(nextCell);
                currentPos = nextPos;
            }

            // 중간보스룸 (KeyRoom / Elite) 배치
            var midBossDirs = GetAvailableDirections(grid, currentPos);
            if (midBossDirs.Count == 0) { log = "Cannot place MidBoss"; return false; }
            Direction toMidBoss = midBossDirs[UnityEngine.Random.Range(0, midBossDirs.Count)];
            Vector2Int midBossPos = currentPos + MazeGrid.GetOffset(toMidBoss);

            MazeCell midBossCell = new MazeCell(midBossPos.x, midBossPos.y)
            {
                roomType = RoomType.KeyRoom,
                blockIndex = 1, // 1블록의 종착점이자 2블록의 유일한 관문!
                isMainTrunk = true,
                visited = true
            };
            grid.AddCell(midBossCell);
            grid.GetCell(currentPos).SetOpen(toMidBoss, true);
            midBossCell.SetOpen(MazeGrid.GetOpposite(toMidBoss), true);

            // 2. 블록 1 샛길(미궁 막다른 길) 방 생성
            for (int b = 0; b < cfg.branchRoomsBlock1; b++)
            {
                var candidates = b1MainCells.FindAll(c => c.roomType != RoomType.KeyRoom && GetAvailableDirections(grid, c.gridPos).Count > 0);
                if (candidates.Count > 0)
                {
                    var parent = candidates[UnityEngine.Random.Range(0, candidates.Count)];
                    var dirs = GetAvailableDirections(grid, parent.gridPos);
                    Direction bDir = dirs[UnityEngine.Random.Range(0, dirs.Count)];
                    Vector2Int bPos = parent.gridPos + MazeGrid.GetOffset(bDir);

                    MazeCell branch = new MazeCell(bPos.x, bPos.y)
                    {
                        roomType = (UnityEngine.Random.value < 0.25f ? RoomType.Trap : RoomType.Normal),
                        blockIndex = 1,
                        isMainTrunk = false,
                        visited = true
                    };
                    grid.AddCell(branch);
                    parent.SetOpen(bDir, true);
                    branch.SetOpen(MazeGrid.GetOpposite(bDir), true);
                }
            }

            // 3. 블록 2 메인 경로 (중간보스룸에서만 출발!)
            var b2StartDirs = GetAvailableDirections(grid, midBossPos);
            if (b2StartDirs.Count == 0) { log = "Cannot exit MidBoss into Block 2"; return false; }
            Direction toB2 = b2StartDirs[UnityEngine.Random.Range(0, b2StartDirs.Count)];
            Vector2Int b2StartPos = midBossPos + MazeGrid.GetOffset(toB2);

            MazeCell b2FirstCell = new MazeCell(b2StartPos.x, b2StartPos.y)
            {
                roomType = RoomType.Normal,
                blockIndex = 2,
                isMainTrunk = true,
                visited = true
            };
            grid.AddCell(b2FirstCell);
            midBossCell.SetOpen(toB2, true);
            b2FirstCell.SetOpen(MazeGrid.GetOpposite(toB2), true);

            int block2Count = UnityEngine.Random.Range(cfg.minRoomsBlock2, cfg.maxRoomsBlock2 + 1);
            currentPos = b2StartPos;
            List<MazeCell> b2MainCells = new List<MazeCell> { b2FirstCell };

            for (int i = 1; i < block2Count; i++)
            {
                var dirs = GetAvailableDirections(grid, currentPos);
                if (dirs.Count == 0) { log = "Block 2 path stuck"; return false; }
                Direction dir = dirs[UnityEngine.Random.Range(0, dirs.Count)];
                Vector2Int nextPos = currentPos + MazeGrid.GetOffset(dir);

                MazeCell nextCell = new MazeCell(nextPos.x, nextPos.y)
                {
                    roomType = RoomType.Normal,
                    blockIndex = 2,
                    isMainTrunk = true,
                    visited = true
                };
                grid.AddCell(nextCell);
                grid.GetCell(currentPos).SetOpen(dir, true);
                nextCell.SetOpen(MazeGrid.GetOpposite(dir), true);

                b2MainCells.Add(nextCell);
                currentPos = nextPos;
            }

            // 최종보스룸 (Boss) 배치 (단일 진입로)
            var bossDirs = GetAvailableDirections(grid, currentPos);
            if (bossDirs.Count == 0) { log = "Cannot place Boss"; return false; }
            Direction toBoss = bossDirs[UnityEngine.Random.Range(0, bossDirs.Count)];
            Vector2Int bossPos = currentPos + MazeGrid.GetOffset(toBoss);

            MazeCell bossCell = new MazeCell(bossPos.x, bossPos.y)
            {
                roomType = RoomType.Boss,
                blockIndex = 2,
                isMainTrunk = true,
                visited = true
            };
            grid.AddCell(bossCell);
            grid.GetCell(currentPos).SetOpen(toBoss, true);
            bossCell.SetOpen(MazeGrid.GetOpposite(toBoss), true);

            // 4. 블록 2 샛길(미궁 막다른 길) 방 생성
            for (int b = 0; b < cfg.branchRoomsBlock2; b++)
            {
                var candidates = b2MainCells.FindAll(c => c.roomType != RoomType.Boss && GetAvailableDirections(grid, c.gridPos).Count > 0);
                if (candidates.Count > 0)
                {
                    var parent = candidates[UnityEngine.Random.Range(0, candidates.Count)];
                    var dirs = GetAvailableDirections(grid, parent.gridPos);
                    Direction bDir = dirs[UnityEngine.Random.Range(0, dirs.Count)];
                    Vector2Int bPos = parent.gridPos + MazeGrid.GetOffset(bDir);

                    MazeCell branch = new MazeCell(bPos.x, bPos.y)
                    {
                        roomType = (UnityEngine.Random.value < 0.25f ? RoomType.Trap : RoomType.Normal),
                        blockIndex = 2,
                        isMainTrunk = false,
                        visited = true
                    };
                    grid.AddCell(branch);
                    parent.SetOpen(bDir, true);
                    branch.SetOpen(MazeGrid.GetOpposite(bDir), true);
                }
            }

            // 5. 검증 (BFS)
            int d1 = CalculateShortestPathLength(grid, startPos, midBossPos);
            int d2 = CalculateShortestPathLength(grid, midBossPos, bossPos);

            if (d1 - 1 < cfg.minRoomsBlock1)
            {
                log = $"Block 1 rooms ({d1 - 1}) < min ({cfg.minRoomsBlock1})";
                return false;
            }
            if (d2 - 1 < cfg.minRoomsBlock2)
            {
                log = $"Block 2 rooms ({d2 - 1}) < min ({cfg.minRoomsBlock2})";
                return false;
            }

            // 병목 검증: 중간보스룸을 통과하지 않고 최종보스로 갈 수 없어야 함
            if (IsPathExistsExcluding(grid, startPos, bossPos, midBossPos))
            {
                log = "Bottleneck violated! MidBoss was bypassed";
                return false;
            }

            // [규칙 4 검증]: 중간보스방(KeyRoom)은 반드시 들어오는 문 1개(1블록), 나가는 문 1개(2블록) 총 2개의 문으로 고정되어야 함
            int midBossDoorCount = 0;
            int midBossBlock1Doors = 0;
            int midBossBlock2Doors = 0;
            foreach (Direction dir in Enum.GetValues(typeof(Direction)))
            {
                if (midBossCell.IsOpen(dir))
                {
                    midBossDoorCount++;
                    var nCell = grid.GetCell(midBossPos + MazeGrid.GetOffset(dir));
                    if (nCell != null)
                    {
                        if (nCell.blockIndex == 1) midBossBlock1Doors++;
                        else if (nCell.blockIndex == 2) midBossBlock2Doors++;
                    }
                }
            }

            if (midBossDoorCount != 2 || midBossBlock1Doors != 1 || midBossBlock2Doors != 1)
            {
                log = $"MidBoss gate rule violated! Doors={midBossDoorCount} (B1={midBossBlock1Doors}, B2={midBossBlock2Doors})";
                return false;
            }

            log = $"성공! 블록1: {d1 - 1}개 방 (최소 {cfg.minRoomsBlock1}), 블록2: {d2 - 1}개 방 (최소 {cfg.minRoomsBlock2}), 중간보스 관문(입구1/출구1), 총 {grid.CellCount}개 방";
            return true;
        }

        private int CalculateShortestPathLength(MazeGrid grid, Vector2Int start, Vector2Int target)
        {
            Queue<(Vector2Int pos, int dist)> q = new();
            HashSet<Vector2Int> visited = new();
            q.Enqueue((start, 0));
            visited.Add(start);

            while (q.Count > 0)
            {
                var (curr, dist) = q.Dequeue();
                if (curr == target) return dist;

                var cell = grid.GetCell(curr);
                if (cell == null) continue;

                foreach (Direction dir in Enum.GetValues(typeof(Direction)))
                {
                    if (cell.IsOpen(dir))
                    {
                        Vector2Int next = curr + MazeGrid.GetOffset(dir);
                        if (!visited.Contains(next))
                        {
                            visited.Add(next);
                            q.Enqueue((next, dist + 1));
                        }
                    }
                }
            }
            return 999;
        }

        private bool IsPathExistsExcluding(MazeGrid grid, Vector2Int start, Vector2Int target, Vector2Int excluded)
        {
            Queue<Vector2Int> q = new();
            HashSet<Vector2Int> visited = new();
            q.Enqueue(start);
            visited.Add(start);

            while (q.Count > 0)
            {
                var curr = q.Dequeue();
                if (curr == target) return true;

                var cell = grid.GetCell(curr);
                if (cell == null) continue;

                foreach (Direction dir in Enum.GetValues(typeof(Direction)))
                {
                    if (cell.IsOpen(dir))
                    {
                        Vector2Int next = curr + MazeGrid.GetOffset(dir);
                        if (next != excluded && !visited.Contains(next))
                        {
                            visited.Add(next);
                            q.Enqueue(next);
                        }
                    }
                }
            }
            return false;
        }

        private void BuildEmergencyTwoBlockDungeon(MazeGrid grid, Data.StageDungeonConfig cfg)
        {
            grid.AddCell(new MazeCell(0, 0) { roomType = RoomType.Start, blockIndex = 1, isMainTrunk = true, visited = true });

            int b1Count = Mathf.Max(1, cfg.minRoomsBlock1);
            for (int x = 1; x <= b1Count; x++)
            {
                var c = new MazeCell(x, 0) { roomType = RoomType.Normal, blockIndex = 1, isMainTrunk = true, visited = true };
                grid.AddCell(c);
                grid.GetCell(x - 1, 0).SetOpen(Direction.East, true);
                c.SetOpen(Direction.West, true);
            }

            int midX = b1Count + 1;
            var mid = new MazeCell(midX, 0) { roomType = RoomType.KeyRoom, blockIndex = 1, isMainTrunk = true, visited = true };
            grid.AddCell(mid);
            grid.GetCell(midX - 1, 0).SetOpen(Direction.East, true);
            mid.SetOpen(Direction.West, true);

            int b2Count = Mathf.Max(3, cfg.minRoomsBlock2);
            for (int i = 1; i <= b2Count; i++)
            {
                int curX = midX + i;
                var c = new MazeCell(curX, 0) { roomType = RoomType.Normal, blockIndex = 2, isMainTrunk = true, visited = true };
                grid.AddCell(c);
                grid.GetCell(curX - 1, 0).SetOpen(Direction.East, true);
                c.SetOpen(Direction.West, true);
            }

            int bossX = midX + b2Count + 1;
            var boss = new MazeCell(bossX, 0) { roomType = RoomType.Boss, blockIndex = 2, isMainTrunk = true, visited = true };
            grid.AddCell(boss);
            grid.GetCell(bossX - 1, 0).SetOpen(Direction.East, true);
            boss.SetOpen(Direction.West, true);
        }

        #endregion

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
            foreach (MazeCell cell in grid.GetAllCells())
            {
                if (cell == null) continue;
                // [규칙 1 절대 보장]: 시작 좌표 (0,0)은 100% RoomType.Start
                if (cell.gridPos == Vector2Int.zero)
                {
                    cell.roomType = RoomType.Start;
                }
                Vector3 worldPos = new Vector3(cell.gridPos.x * _roomSize, 0f, cell.gridPos.y * _roomSize);
                RoomController roomCtrl = CreateRoomInstance(cell, worldPos);
                _instantiatedRooms[cell.gridPos] = roomCtrl;
            }
        }

        /// <summary>
        /// [규칙 3 & 4]: 중간보스방(KeyRoom) -> 후반부 던전(Block 2)으로 연결되는 출구 관문인지 판별
        /// </summary>
        public static bool IsMidBossExitDoor(MazeCell cellA, MazeCell cellB)
        {
            if (cellA == null || cellB == null) return false;
            return (cellA.roomType == RoomType.KeyRoom && cellB.blockIndex == 2) ||
                   (cellB.roomType == RoomType.KeyRoom && cellA.blockIndex == 2);
        }

        /// <summary>
        /// [규칙 5, 7, 8]: 문 앞에 직접 다가가야만 열리는 문인지 판별
        /// (스타트 룸 첫 문, 중간보스방 입구/출구, 최종보스방 입구)
        /// </summary>
        public static bool IsManualApproachOnlyDoor(MazeCell cellA, MazeCell cellB)
        {
            if (cellA == null || cellB == null) return false;
            return cellA.roomType == RoomType.Start || cellB.roomType == RoomType.Start ||
                   cellA.roomType == RoomType.KeyRoom || cellB.roomType == RoomType.KeyRoom ||
                   cellA.roomType == RoomType.Boss || cellB.roomType == RoomType.Boss;
        }

        private void SetupDungeonFogAndAutoDoors(MazeGrid grid)
        {
            HashSet<string> installedDoorPairs = new();

            foreach (var kvp in _instantiatedRooms)
            {
                Vector2Int pos = kvp.Key;
                RoomController room = kvp.Value;
                MazeCell cell = grid.GetCell(pos);
                if (room == null || cell == null) continue;

                // 1. 방 포그 오버레이 초기화
                room.InitializeFogOverlay(_roomSize);

                // 2. 자동 개폐 문짝(AutoDoor) 및 잠긴 관문(LockedDoor) 설치 + 양방향 소켓 연동
                foreach (Direction dir in System.Enum.GetValues(typeof(Direction)))
                {
                    if (cell.IsOpen(dir))
                    {
                        Vector2Int neighborPos = pos + MazeGrid.GetOffset(dir);
                        MazeCell neighborCell = grid.GetCell(neighborPos);
                        RoomController neighborRoom = GetRoomAt(neighborPos);

                        string pairKey = GetDoorPairKey(pos, neighborPos);

                        var socket = room.GetSocket(dir);
                        if (socket == null || (socket.CurrentState != DoorState.Open && socket.CurrentState != DoorState.BossLock)) continue;

                        if (!installedDoorPairs.Contains(pairKey))
                        {
                            DungeonDoorType doorType = IsMidBossExitDoor(cell, neighborCell)
                                ? DungeonDoorType.LockedDoor
                                : DungeonDoorType.AutoDoor;
                            bool manualOnly = IsManualApproachOnlyDoor(cell, neighborCell);

                            var autoDoor = InstallAutoDoorOnSocket(socket);
                            if (autoDoor != null)
                            {
                                autoDoor.ConfigureDoorPolicy(doorType, manualOnly);
                            }

                            DoorSocket oppSocket = neighborRoom != null ? neighborRoom.GetSocket(MazeGrid.GetOpposite(dir)) : null;
                            socket.LinkOppositeSocket(oppSocket, autoDoor, doorType);
                            if (oppSocket != null)
                            {
                                oppSocket.LinkOppositeSocket(socket, autoDoor, doorType);
                            }

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

        private DungeonAutoDoor InstallAutoDoorOnSocket(DoorSocket socket)
        {
            var existing = socket.GetComponentInChildren<DungeonAutoDoor>();
            if (existing != null) return existing;

            GameObject autoDoorPrefab = null;
            if (Data.GameDataManager.Instance != null && Data.GameDataManager.Instance.Prefabs != null)
            {
                autoDoorPrefab = Data.PrefabDatabaseSO.ResolvePrefab(ref Data.GameDataManager.Instance.Prefabs.autoDoorPrefab, "Prefabs/DungeonAutoDoor");
            }
            else
            {
                autoDoorPrefab = Resources.Load<GameObject>("Prefabs/DungeonAutoDoor");
            }

            GameObject autoDoorObj;
            if (autoDoorPrefab != null)
            {
                autoDoorObj = Instantiate(autoDoorPrefab, socket.transform, false);
                autoDoorObj.name = "AutoDoor";
            }
            else
            {
                autoDoorObj = new GameObject("AutoDoor");
                autoDoorObj.transform.SetParent(socket.transform, false);
            }

            autoDoorObj.transform.localPosition = Vector3.zero;
            autoDoorObj.transform.localRotation = Quaternion.identity;

            var doorComp = autoDoorObj.GetComponent<DungeonAutoDoor>();
            if (doorComp == null)
            {
                doorComp = autoDoorObj.AddComponent<DungeonAutoDoor>();
            }
            return doorComp;
        }

        private void PreSpawnAllRoomMonsters()
        {
            foreach (var kvp in _instantiatedRooms)
            {
                // [규칙 1 절대 보장]: 시작방 (0,0)은 절대 안전지대 (사전 스폰 완전 배제)
                if (kvp.Key == Vector2Int.zero) continue;
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

            if (keyRoom == null)
            {
                // 중간보스방이 없는 경우, 시작방과 보스방을 제외한 방 중 하나를 강제로 KeyRoom 지정
                foreach (var kvp in _instantiatedRooms)
                {
                    if (kvp.Key != Vector2Int.zero && (bossRoom == null || kvp.Key != bossRoom.GridPosition) && kvp.Value != null)
                    {
                        keyRoom = kvp.Value;
                        keyRoom.SetupRoom(kvp.Key, RoomType.KeyRoom);
                        Debug.LogWarning($"[MazeGenerator] 열쇠방 타입 누락 감지! ({kvp.Key.x},{kvp.Key.y}) 방을 열쇠방으로 강제 지정합니다.");
                        break;
                    }
                }
            }

            // [기획 규칙]: 보스와 중간보스는 최초 방 생성 시 소환되지 않고, 
            // 해당 방에 진입하여 1차 웨이브를 소탕한 후 2차 웨이브에서 소환됩니다.
        }

        private RoomController CreateRoomInstance(MazeCell cell, Vector3 worldPos)
        {
            GameObject roomObj;
            RoomController roomCtrl;

            // 1. 맵 스킨 테이블에서 현재 스테이지 스킨 카테고리의 룸 프리팹 무작위 선택
            if (_mapSkinTable == null)
            {
                _mapSkinTable = Data.GameDataManager.Instance?.MapSkinTable ?? Resources.Load<Data.MapSkinTableSO>("Data/MapSkinTable");
            }

            int skinCategory = _currentStageConfig != null ? _currentStageConfig.skinCategoryIndex : 0;
            GameObject selectedPrefab = _mapSkinTable != null ? _mapSkinTable.GetRandomRoomPrefab(skinCategory) : null;
            if (selectedPrefab == null)
            {
                selectedPrefab = _roomPrefab;
            }

            if (selectedPrefab != null)
            {
                roomObj = Instantiate(selectedPrefab, worldPos, Quaternion.identity, _dungeonRoot);
                roomCtrl = roomObj.GetComponent<RoomController>();
                if (roomCtrl == null)
                {
                    roomCtrl = roomObj.AddComponent<RoomController>();
                }
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
            // [규칙 2 & 3]: 던전의 모든 문은 일단 자동으로 열리는 문(Open)으로 세팅하고,
            // 오직 중간보스(KeyRoom) -> 후반부 던전(Block 2)으로 연결되는 문만 잠긴 문(BossLock)으로 세팅!
            // (최종보스방 문은 자동으로 열리는 문으로 세팅)
            foreach (Direction dir in System.Enum.GetValues(typeof(Direction)))
            {
                bool isOpen = cell.IsOpen(dir);
                DoorState state = DoorState.Wall;

                if (isOpen)
                {
                    Vector2Int neighborPos = cell.gridPos + MazeGrid.GetOffset(dir);
                    MazeCell neighbor = _grid.GetCell(neighborPos);

                    if (IsMidBossExitDoor(cell, neighbor))
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
                player.ResetAnimation();
                player.SetInCombat(false); // 재시작/스폰 시 비전투 상태 보장!
            }

            _playerCurrentRoom = Vector2Int.zero;
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

        /// <summary>
        /// [규칙 3 & 8]: 중간보스방(KeyRoom) -> 후반부 던전(Block 2)으로 연결되는 잠긴 문을 열쇠로 봉인 해제
        /// </summary>
        public void UnlockBossDoors()
        {
            foreach (var kvp in _instantiatedRooms)
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

            Debug.Log("<color=gold>[MazeGenerator] 중간보스 출구 관문이 열쇠로 봉인 해제되었습니다!</color>");
        }

        /// <summary>
        /// Stage Clear 후 시작룸 재출발: 기존 잔존 요괴 정리, 플레이어 HP 및 위치 초기화, 새 미로 생성
        /// </summary>
        public void RestartStage()
        {
            Debug.Log("<color=cyan>[MazeGenerator] 스테이지 초기화 및 시작룸 재출발 시작!</color>");

            // 1. 플레이어 부활 및 애니메이션 리셋
            var player = FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                player.Respawn();
            }

            // 2. 새 던전 미로 생성 (ClearExistingDungeon이 잔존 요괴, 영혼석, 하트, 투사체, UI를 100% 정리 후 재생성)
            GenerateDungeon();
        }

        // ========================================================
        // 몬스터 길찾기 및 네비게이션 지원 메서드
        // ========================================================

        private Vector2Int _playerCurrentRoom = Vector2Int.zero;

        /// <summary>
        /// 플레이어가 현재 머물고 있는 방을 안정적으로 판정 (문턱 히스테리시스 적용)
        /// - 문턱(Doorway) 근처(16.3m ~ 22.7m)에 있을 때는 이전 방 소속을 유지하여 경계선 오인식 방지
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
            foreach (var kvp in _instantiatedRooms)
            {
                Vector3 rCenter = GetRoomCenterWorldPos(kvp.Key);
                if (Mathf.Abs(pPos.x - rCenter.x) <= halfInterior && Mathf.Abs(pPos.z - rCenter.z) <= halfInterior)
                {
                    _playerCurrentRoom = kvp.Key;
                    return _playerCurrentRoom;
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

        #region Ten Dungeon Door Policies Engine (던전 문 10대 정책 관리 엔진)

        private float _doorPolicyCheckTimer = 0f;
        private readonly HashSet<string> _activeCombatTriggerDoors = new();

        /// <summary>
        /// [사용자 핵심 규칙]: 문이 열리고 전투가 시작되면, 해당 전투가 끝날 때까지 방금 열린 문은 절대로 닫히면 안 됨!
        /// </summary>
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
        }

        public bool IsCombatTriggerDoor(string pairKey)
        {
            return _activeCombatTriggerDoors.Contains(pairKey);
        }

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
        }

        /// <summary>
        /// 던전 내에 전투 중인 방(인카운터 활성 및 살아있는 몬스터/남은 웨이브 존재)이 있는지 확인
        /// </summary>
        public bool HasAnyActiveCombat()
        {
            foreach (var room in _instantiatedRooms.Values)
            {
                if (room != null && room.IsEncounterActive && (!room.WavesCompleted || room.LivingMonsterCount > 0))
                {
                    return true;
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

        /// <summary>
        /// 특정 방 클리어 시 호출: 던전 내 모든 전투 종료 여부 판별 후 비전투 전환 및 문 일괄 재개방 (규칙 10)
        /// </summary>
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

        /// <summary>
        /// [규칙 10]: 전투가 종료된 뒤에는 한번 열었던 문은 모두 다시 열려있어야 함
        /// </summary>
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

        public static string GetDoorPairKey(Vector2Int a, Vector2Int b)
        {
            return a.x < b.x || (a.x == b.x && a.y < b.y)
                ? $"{a.x}_{a.y}:{b.x}_{b.y}"
                : $"{b.x}_{b.y}:{a.x}_{a.y}";
        }

        /// <summary>
        /// 플레이어 위치에서 전투 중인 모든 몬스터 방으로 향하는 개방 경로(통로 문 쌍) 집합 계산
        /// - [규칙 9]: 전투 중에 문이 열리거나 닫혀서 몬스터나 캐릭터가 서로 단절되는 일이 없도록,
        ///   플레이어가 위치한 방과 전투 중인 몬스터가 위치한 방을 잇는 모든 통로 문은 열림 상태를 유지한다.
        /// </summary>
        private HashSet<string> GetDoorPairsConnectingPlayerToMonsters(Vector2Int playerPos, HashSet<Vector2Int> combatRooms)
        {
            HashSet<string> connectingPairs = new();
            if (_grid == null || combatRooms.Count == 0) return connectingPairs;

            Vector3 playerWorldPos = PlayerController.Instance != null ? PlayerController.Instance.transform.position : Vector3.zero;

            // [안전망 1]: 플레이어가 어떤 문 근처(5.0m 이내)에 있고, 그 문이 교전 방과 연결되어 있다면 즉시 보호
            foreach (var kvp in _instantiatedRooms)
            {
                var room = kvp.Value;
                if (room == null) continue;

                foreach (Direction dir in System.Enum.GetValues(typeof(Direction)))
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
                // 플레이어와 요괴가 같은 방에 있다면 둘 사이를 가로막는 문은 없음
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

                    foreach (Direction dir in System.Enum.GetValues(typeof(Direction)))
                    {
                        if (cell.IsOpen(dir))
                        {
                            Vector2Int nextPos = curr + MazeGrid.GetOffset(dir);
                            if (!visited.Contains(nextPos) && _grid.GetCell(nextPos) != null)
                            {
                                // 문이 실제로 한 번이라도 열린 적이 있거나 현재 열려있는지 확인
                                bool doorOpened = false;
                                if (currRoom != null)
                                {
                                    var socket = currRoom.GetSocket(dir);
                                    if (socket != null && socket.AutoDoor != null && (socket.AutoDoor.HasEverBeenOpened || socket.AutoDoor.IsOpen))
                                    {
                                        doorOpened = true;
                                    }
                                }

                                if (doorOpened)
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

        /// <summary>
        /// [던전 문 10대 정책 실시간 평가 엔진]
        /// - 규칙 9a: 새로운 방에 진입해서 전투가 시작되면 그 방에서 나가는 퇴로는 전투 중에는 막힘.
        /// - 규칙 9b: 전투 중에 문이 열리거나 닫혀서 몬스터나 캐릭터가 서로 단절되는 일은 없음.
        /// - 규칙 10: 전투가 종료된 뒤에는 한번 열었던 문은 모두 다시 열림.
        /// </summary>
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

            // 1. 주인공과 교전 중인 몬스터들의 물리적 위치 및 추가 웨이브가 남은 교전 방 수집
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

            foreach (var room in _instantiatedRooms.Values)
            {
                if (room != null && room.IsEncounterActive && !room.WavesCompleted)
                {
                    combatRooms.Add(room.GridPosition);
                }
            }

            // 던전 내에 전투 중인 요괴나 웨이브가 없다면 비전투 전환 및 일괄 재개방 (규칙 10)
            if (combatRooms.Count == 0 && !anyActiveCombat)
            {
                PlayerController.Instance.SetInCombat(false);
                ReopenAllOpenedDoorsAfterCombat();
                return;
            }

            PlayerController.Instance.SetInCombat(true);

            // 플레이어에서 각 전투 몬스터 위치로 통하는 경로상의 문 쌍 수집 (열린 문 기반)
            HashSet<string> pathDoorPairs = GetDoorPairsConnectingPlayerToMonsters(playerPos, combatRooms);

            // 2. 던전 내 모든 자동문 순회 평가
            var allDoors = FindObjectsByType<DungeonAutoDoor>(FindObjectsInactive.Include);
            foreach (var door in allDoors)
            {
                if (door == null) continue;
                var socket = door.GetComponentInParent<DoorSocket>();
                if (socket == null) continue;
                var room = socket.GetComponentInParent<RoomController>();
                if (room == null) continue;

                // 아직 열쇠로 봉인 해제되지 않은 중간보스 출구 잠긴 문은 건드리지 않음 (규칙 3 & 8)
                if (door.DoorType == DungeonDoorType.LockedDoor && !door.IsKeyUnlocked)
                {
                    continue;
                }

                // 한번도 플레이어가 열지 않은 문은 닫힌 상태 유지
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

                float distToPlayer = Vector3.Distance(door.transform.position, playerWorldPos);
                bool isPlayerNearThisDoor = distToPlayer <= 6.0f;
                bool isRecentlyOpened = (Time.time - door.LastOpenedTime < 3.0f);

                // [사용자 핵심 규칙]: 문이 열리고 전투가 시작되면, 해당 전투가 끝날 때까지 방금 열린 문은 절대로 닫히면 안 됨!
                bool isCombatTriggerDoor = _activeCombatTriggerDoors.Contains(pairKey);

                // [규칙 9b & 10]: 플레이어와 전투 중인 몬스터 사이를 잇는 문은 단절 방지를 위해 열림 유지
                bool isBetweenPlayerAndMonsters = pathDoorPairs.Contains(pairKey);

                // [규칙 6 & 9]: 플레이어가 있는 방과 교전 방을 직접 잇는 전방 관문 문은 진입 통로이므로 열림 유지
                bool isGatewayToCombat = (posA == playerPos && combatRooms.Contains(posB)) || (posB == playerPos && combatRooms.Contains(posA));

                if (isCombatTriggerDoor || isBetweenPlayerAndMonsters || isGatewayToCombat || isRecentlyOpened || isPlayerNearThisDoor)
                {
                    door.ForceOpen();
                }
                else
                {
                    // [규칙 9]: 새로운 방에 진입해서 전투가 시작되면 그 방에서 나가는 퇴로(이번 전투 이전에 열었던 문)는 전투 중에는 막힘!
                    // 단, 문턱 한가운데에 캐릭터나 몬스터가 걸쳐 있을 때는 완전히 통과할 때까지 대기
                    if (door.IsOpen && !door.IsActorInDoorway(3.0f))
                    {
                        door.CloseByCombat();
                    }
                }
            }
        }

        #endregion
    }
}
