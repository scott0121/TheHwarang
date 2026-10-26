using System;
using System.Collections.Generic;
using UnityEngine;

namespace MythOfGuotaji.Dungeon
{
    public class MazeCell
    {
        public Vector2Int gridPos;
        public RoomType roomType = RoomType.Normal;
        public bool[] openDirections = new bool[4]; // North=0, East=1, South=2, West=3
        public bool visited = false;
        public int distanceFromStart = -1;

        // [신규 2블럭 아키텍처 지원]
        public int blockIndex = 1;      // 1 = 스타트~중간보스 블록, 2 = 중간보스~최종보스 블록
        public bool isMainTrunk = false; // 메인 선형 경로인가? (false면 탐험형 샛길/미궁 방)

        public MazeCell(int x, int y)
        {
            gridPos = new Vector2Int(x, y);
            for (int i = 0; i < 4; i++) openDirections[i] = false;
        }

        public bool IsOpen(Direction dir) => openDirections[(int)dir];
        public void SetOpen(Direction dir, bool open) => openDirections[(int)dir] = open;
    }

    /// <summary>
    /// 유기적 미궁 던전 그리드 데이터 구조:
    /// - 정사각형 고정 크기에 구애받지 않고 유연한 좌표 배치를 지원하는 딕셔너리 기반
    /// - 2블럭(스타트->중간보스, 중간보스->최종보스) 및 샛길 미궁 방 체계 지원
    /// </summary>
    public class MazeGrid
    {
        public readonly int size;
        private readonly Dictionary<Vector2Int, MazeCell> _cellsDict = new();
        private readonly MazeCell[,] _cells; // 레거시 호환용

        public int CellCount => _cellsDict.Count;

        public MazeGrid(int size)
        {
            this.size = size;
            _cells = new MazeCell[size, size];

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    var cell = new MazeCell(x, y);
                    _cells[x, y] = cell;
                    _cellsDict[new Vector2Int(x, y)] = cell;
                }
            }
        }

        /// <summary>
        /// 유기적 비정방형 던전을 위한 생성자
        /// </summary>
        public MazeGrid()
        {
            this.size = 30; // 넉넉한 가상 바운드
            _cells = new MazeCell[0, 0];
        }

        public MazeCell GetCell(int x, int y) => GetCell(new Vector2Int(x, y));

        public MazeCell GetCell(Vector2Int pos)
        {
            if (_cellsDict.TryGetValue(pos, out var cell))
            {
                return cell;
            }
            return null;
        }

        public void AddCell(MazeCell cell)
        {
            if (cell == null) return;
            _cellsDict[cell.gridPos] = cell;
        }

        public bool ContainsCell(Vector2Int pos) => _cellsDict.ContainsKey(pos);

        public IEnumerable<MazeCell> GetAllCells() => _cellsDict.Values;

        public static Vector2Int GetOffset(Direction dir)
        {
            return dir switch
            {
                Direction.North => new Vector2Int(0, 1),
                Direction.East => new Vector2Int(1, 0),
                Direction.South => new Vector2Int(0, -1),
                Direction.West => new Vector2Int(-1, 0),
                _ => Vector2Int.zero
            };
        }

        public static Direction GetOffsetDirection(Vector2Int diff)
        {
            if (diff == new Vector2Int(0, 1)) return Direction.North;
            if (diff == new Vector2Int(1, 0)) return Direction.East;
            if (diff == new Vector2Int(0, -1)) return Direction.South;
            if (diff == new Vector2Int(-1, 0)) return Direction.West;
            return Direction.North;
        }

        public static Direction GetOpposite(Direction dir)
        {
            return dir switch
            {
                Direction.North => Direction.South,
                Direction.East => Direction.West,
                Direction.South => Direction.North,
                Direction.West => Direction.East,
                _ => Direction.North
            };
        }
    }
}
