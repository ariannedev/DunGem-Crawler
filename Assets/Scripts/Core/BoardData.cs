using System.Collections.Generic;
using UnityEngine;

namespace DunGemCrawler
{
    public class BoardData
    {
        public int Columns { get; }
        public int Rows { get; }
        public Vector2Int PlayerCell { get; set; }
        public HashSet<Vector2Int> ActiveEnemyCells { get; } = new HashSet<Vector2Int>();
        public HashSet<Vector2Int> FrozenCells      { get; } = new HashSet<Vector2Int>();

        public bool IsEnemyCell(int col, int row) =>
            ActiveEnemyCells.Contains(new Vector2Int(col, row));
        public bool IsEnemyCell(Vector2Int cell) =>
            ActiveEnemyCells.Contains(cell);

        public bool IsFrozenCell(int col, int row) =>
            FrozenCells.Contains(new Vector2Int(col, row));
        public bool IsFrozenCell(Vector2Int cell) =>
            FrozenCells.Contains(cell);

        private readonly GemData[,] _gems;
        private readonly DungeonTileData[,] _tiles;

        public BoardData(int columns, int rows)
        {
            Columns = columns;
            Rows = rows;
            _gems = new GemData[columns, rows];
            _tiles = new DungeonTileData[columns, rows];
        }

        public bool InBounds(int col, int row) =>
            col >= 0 && col < Columns && row >= 0 && row < Rows;

        public bool InBounds(Vector2Int cell) => InBounds(cell.x, cell.y);

        public GemData GetGem(int col, int row) => _gems[col, row];
        public GemData GetGem(Vector2Int cell) => _gems[cell.x, cell.y];

        public void SetGem(int col, int row, GemData gem)
        {
            _gems[col, row] = gem;
            if (gem != null)
            {
                gem.Col = col;
                gem.Row = row;
            }
        }

        public void SetGem(Vector2Int cell, GemData gem) => SetGem(cell.x, cell.y, gem);

        public DungeonTileData GetTile(int col, int row) => _tiles[col, row];
        public DungeonTileData GetTile(Vector2Int cell) => _tiles[cell.x, cell.y];

        public void SetTile(int col, int row, DungeonTileData tile)
        {
            _tiles[col, row] = tile;
        }

        public bool IsPlayerCell(int col, int row) =>
            PlayerCell.x == col && PlayerCell.y == row;

        public bool IsPlayerCell(Vector2Int cell) => IsPlayerCell(cell.x, cell.y);

        public void SwapGems(Vector2Int a, Vector2Int b)
        {
            GemData gemA = _gems[a.x, a.y];
            GemData gemB = _gems[b.x, b.y];
            _gems[a.x, a.y] = gemB;
            _gems[b.x, b.y] = gemA;
            if (gemA != null) { gemA.Col = b.x; gemA.Row = b.y; }
            if (gemB != null) { gemB.Col = a.x; gemB.Row = a.y; }
        }
    }
}
