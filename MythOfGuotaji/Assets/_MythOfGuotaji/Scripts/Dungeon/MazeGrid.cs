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

        public MazeCell(int x, int y)
        {
            gridPos = new Vector2Int(x, y);
            for (int i = 0; i < 4; i++) openDirections[i] = false;
        }

        public bool IsOpen(Direction dir) => openDirections[(int)dir];
        public void SetOpen(Direction dir, bool open) => openDirections[(int)dir] = open;
    }

    /// <summary>
    /// N x N 논리적 미로 그리드 데이터 구조
    /// </summary>
    public class MazeGrid
    {
        public readonly int size;
        private readonly MazeCell[,] _cells;

        public MazeGrid(int size)
        {
            this.size = size;
            _cells = new MazeCell[size, size];

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    _cells[x, y] = new MazeCell(x, y);
                }
            }
        }

        public MazeCell GetCell(int x, int y)
        {
            if (x < 0 || x >= size || y < 0 || y >= size) return null;
            return _cells[x, y];
        }

        public MazeCell GetCell(Vector2Int pos) => GetCell(pos.x, pos.y);

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
