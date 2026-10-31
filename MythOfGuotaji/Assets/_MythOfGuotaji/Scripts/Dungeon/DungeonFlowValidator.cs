using System;
using System.Collections.Generic;
using UnityEngine;

namespace MythOfGuotaji.Dungeon
{
    /// <summary>
    /// 던전 생성 흐름 및 유효성(보스방, 열쇠방, 경로 연결성, 최단 거리 등) 검증 엔진
    /// </summary>
    public static class DungeonFlowValidator
    {
        /// <summary>
        /// BFS를 이용하여 두 방 좌표 사이의 최단 경로 방 개수를 계산 (통로 연결 기준)
        /// </summary>
        public static int CalculateShortestPathLength(MazeGrid grid, Vector2Int start, Vector2Int target)
        {
            if (grid == null || start == target) return 0;

            Queue<Vector2Int> queue = new();
            Dictionary<Vector2Int, int> dist = new();

            queue.Enqueue(start);
            dist[start] = 0;

            while (queue.Count > 0)
            {
                var curr = queue.Dequeue();
                int currentDist = dist[curr];
                if (curr == target) return currentDist;

                var cell = grid.GetCell(curr);
                if (cell == null) continue;

                foreach (Direction dir in Enum.GetValues(typeof(Direction)))
                {
                    if (cell.IsOpen(dir))
                    {
                        Vector2Int next = curr + MazeGrid.GetOffset(dir);
                        if (!dist.ContainsKey(next) && grid.GetCell(next) != null)
                        {
                            dist[next] = currentDist + 1;
                            queue.Enqueue(next);
                        }
                    }
                }
            }

            return -1; // 연결되지 않음
        }

        /// <summary>
        /// 특정 방(exclude)을 거치지 않고 start에서 target으로 가는 경로가 존재하는지 검사
        /// </summary>
        public static bool IsPathExistsExcluding(MazeGrid grid, Vector2Int start, Vector2Int target, Vector2Int exclude)
        {
            if (grid == null) return false;
            if (start == target) return true;
            if (start == exclude || target == exclude) return false;

            Queue<Vector2Int> queue = new();
            HashSet<Vector2Int> visited = new();

            queue.Enqueue(start);
            visited.Add(start);

            while (queue.Count > 0)
            {
                var curr = queue.Dequeue();
                if (curr == target) return true;

                var cell = grid.GetCell(curr);
                if (cell == null) continue;

                foreach (Direction dir in Enum.GetValues(typeof(Direction)))
                {
                    if (cell.IsOpen(dir))
                    {
                        Vector2Int next = curr + MazeGrid.GetOffset(dir);
                        if (next != exclude && !visited.Contains(next) && grid.GetCell(next) != null)
                        {
                            visited.Add(next);
                            queue.Enqueue(next);
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 생성된 미로 구조가 2블록 선형 흐름 및 기획 규칙을 완벽히 만족하는지 종합 검증
        /// - 1. 시작방(Start), 중간보스방(Key/MidBoss), 보스방(Boss) 존재 여부
        /// - 2. 시작방 -> 중간보스방 경로 연결성 및 최소 거리 검증
        /// - 3. 중간보스방 -> 보스방 경로 연결성 검증
        /// - 4. 중간보스방을 통과하지 않고는 시작방에서 보스방에 도달할 수 없어야 함 (게이트 차단 원칙)
        /// - 5. 모든 방의 완전한 연결성(고립 방 없음) 검증
        /// </summary>
        public static bool ValidateDungeonFlow(MazeGrid grid, Data.StageDungeonConfig stageCfg, out string failureReason)
        {
            if (grid == null || grid.CellCount == 0)
            {
                failureReason = "Grid is empty.";
                return false;
            }

            MazeCell startCell = null;
            MazeCell midBossCell = null;
            MazeCell bossCell = null;

            foreach (var cell in grid.GetAllCells())
            {
                if (cell.roomType == RoomType.Start) startCell = cell;
                else if (cell.roomType == RoomType.KeyRoom) midBossCell = cell;
                else if (cell.roomType == RoomType.Boss) bossCell = cell;
            }

            if (startCell == null)
            {
                failureReason = "Start room not found.";
                return false;
            }
            if (midBossCell == null)
            {
                failureReason = "MidBoss (KeyRoom) not found.";
                return false;
            }
            if (bossCell == null)
            {
                failureReason = "Boss room not found.";
                return false;
            }

            Vector2Int startPos = startCell.gridPos;
            Vector2Int midBossPos = midBossCell.gridPos;
            Vector2Int bossPos = bossCell.gridPos;

            // 1. 시작방 -> 중간보스방 거리 검증
            int startToMidDist = CalculateShortestPathLength(grid, startPos, midBossPos);
            if (startToMidDist < 0)
            {
                failureReason = "Path from Start to MidBoss is disconnected.";
                return false;
            }

            int minRequiredB1 = stageCfg != null ? stageCfg.minRoomsBlock1 : 3;
            if (startToMidDist < minRequiredB1)
            {
                failureReason = $"Start to MidBoss path length ({startToMidDist}) is shorter than required minimum ({minRequiredB1}).";
                return false;
            }

            // 2. 중간보스방 -> 보스방 거리 검증
            int midToBossDist = CalculateShortestPathLength(grid, midBossPos, bossPos);
            if (midToBossDist < 0)
            {
                failureReason = "Path from MidBoss to Boss is disconnected.";
                return false;
            }

            int minRequiredB2 = stageCfg != null ? stageCfg.minRoomsBlock2 : 3;
            if (midToBossDist < minRequiredB2)
            {
                failureReason = $"MidBoss to Boss path length ({midToBossDist}) is shorter than required minimum ({minRequiredB2}).";
                return false;
            }

            // 3. 우회로 방지: 중간보스방을 통과하지 않고 보스방에 갈 수 있는 경로가 존재하면 실패
            if (IsPathExistsExcluding(grid, startPos, bossPos, midBossPos))
            {
                failureReason = "Found an invalid shortcut to Boss that bypasses the MidBoss (Key) room.";
                return false;
            }

            // 4. 고립된 방이 없는지 전수 검사 (시작방에서 모든 방으로 도달 가능해야 함)
            HashSet<Vector2Int> reachable = new();
            Queue<Vector2Int> q = new();
            q.Enqueue(startPos);
            reachable.Add(startPos);

            while (q.Count > 0)
            {
                var curr = q.Dequeue();
                var cell = grid.GetCell(curr);
                if (cell == null) continue;

                foreach (Direction dir in Enum.GetValues(typeof(Direction)))
                {
                    if (cell.IsOpen(dir))
                    {
                        Vector2Int next = curr + MazeGrid.GetOffset(dir);
                        if (!reachable.Contains(next) && grid.GetCell(next) != null)
                        {
                            reachable.Add(next);
                            q.Enqueue(next);
                        }
                    }
                }
            }

            if (reachable.Count != grid.CellCount)
            {
                failureReason = $"Dungeon contains isolated unreachable rooms ({reachable.Count}/{grid.CellCount}).";
                return false;
            }

            failureReason = string.Empty;
            return true;
        }
    }
}
