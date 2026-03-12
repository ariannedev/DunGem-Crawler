using System.Collections.Generic;
using UnityEngine;

namespace DunGemCrawler
{
    public static class GridUtils
    {
        public static Vector2 GridToWorld(int col, int row, Vector2 origin, float cellSize) =>
            new Vector2(origin.x + col * cellSize + cellSize * 0.5f,
                        origin.y + row * cellSize + cellSize * 0.5f);

        public static Vector2 GridToWorld(Vector2Int cell, Vector2 origin, float cellSize) =>
            GridToWorld(cell.x, cell.y, origin, cellSize);

        public static Vector2Int WorldToGrid(Vector2 worldPos, Vector2 origin, float cellSize) =>
            new Vector2Int(
                Mathf.FloorToInt((worldPos.x - origin.x) / cellSize),
                Mathf.FloorToInt((worldPos.y - origin.y) / cellSize));

        public static bool IsAdjacent(Vector2Int a, Vector2Int b)
        {
            int dx = Mathf.Abs(a.x - b.x);
            int dy = Mathf.Abs(a.y - b.y);
            return (dx == 1 && dy == 0) || (dx == 0 && dy == 1);
        }

        public static List<Vector2Int> GetNeighbors(Vector2Int cell, int cols, int rows)
        {
            var result = new List<Vector2Int>(4);
            TryAdd(result, cell.x + 1, cell.y, cols, rows);
            TryAdd(result, cell.x - 1, cell.y, cols, rows);
            TryAdd(result, cell.x, cell.y + 1, cols, rows);
            TryAdd(result, cell.x, cell.y - 1, cols, rows);
            return result;
        }

        private static void TryAdd(List<Vector2Int> list, int col, int row, int cols, int rows)
        {
            if (col >= 0 && col < cols && row >= 0 && row < rows)
                list.Add(new Vector2Int(col, row));
        }
    }
}
