using System.Collections.Generic;
using UnityEngine;

namespace DunGemCrawler
{
    public class BoardInitializer
    {
        public void Initialize(BoardData data, BoardConfig config,
            GemPool pool, DungeonTileView tileViewPrefab,
            Transform tileParent, Vector2 boardOrigin, PlayerData player)
        {
            data.PlayerCell = player.Cell;
            PlaceDungeonTiles(data, config, tileViewPrefab, tileParent, boardOrigin);
            EnsureDoorAtBottom(data, config);
            PlaceGems(data, config, pool, boardOrigin);
        }

        // Doors are only valid on the three non-entry edges: bottom, left, and right.
        // The top row is the player's entry row, so doors are excluded there.
        private bool IsValidDoorCell(int col, int row, int cols, int rows)
        {
            if (row == rows - 1) return false;           // top row = player entry, no exit here
            return row == 0 || col == 0 || col == cols - 1;
        }

        private void PlaceDungeonTiles(BoardData data, BoardConfig config,
            DungeonTileView tileViewPrefab, Transform parent, Vector2 origin)
        {
            float totalWeight = config.WeightFloor + config.WeightWall + config.WeightDoor
                              + config.WeightTreasure + config.WeightEnemy;

            for (int col = 0; col < config.Columns; col++)
            {
                for (int row = 0; row < config.Rows; row++)
                {
                    TileType type = RollTileType(config, totalWeight);

                    // Doors are restricted to edge walls; demote to Floor elsewhere
                    if (type == TileType.Door && !IsValidDoorCell(col, row, config.Columns, config.Rows))
                        type = TileType.Floor;

                    var tileData = new DungeonTileData(type, col, row);

                    Vector2 worldPos = GridUtils.GridToWorld(col, row, origin, config.CellSize);
                    var view = Object.Instantiate(
                        tileViewPrefab, worldPos, Quaternion.identity, parent);
                    view.SetType(type);
                    tileData.View = view;

                    data.SetTile(col, row, tileData);
                }
            }
        }

        private TileType RollTileType(BoardConfig config, float totalWeight)
        {
            float roll = Random.Range(0f, totalWeight);
            if (roll < config.WeightFloor) return TileType.Floor;
            roll -= config.WeightFloor;
            if (roll < config.WeightWall) return TileType.Wall;
            roll -= config.WeightWall;
            if (roll < config.WeightDoor) return TileType.Door;
            roll -= config.WeightDoor;
            if (roll < config.WeightTreasure) return TileType.Treasure;
            return TileType.Enemy;
        }

        // Guarantee at least one door exists on the valid door edges.
        private void EnsureDoorAtBottom(BoardData data, BoardConfig config)
        {
            for (int col = 0; col < config.Columns; col++)
                for (int row = 0; row < config.Rows; row++)
                    if (IsValidDoorCell(col, row, config.Columns, config.Rows)
                        && data.GetTile(col, row)?.Type == TileType.Door) return;

            // No door found anywhere on the edges — place one at a random bottom-row cell
            int doorCol = Random.Range(0, config.Columns);
            DungeonTileData tile = data.GetTile(doorCol, 0);
            tile.Type = TileType.Door;
            tile.View.SetType(TileType.Door);
        }

        private void PlaceGems(BoardData data, BoardConfig config,
            GemPool pool, Vector2 origin)
        {
            int colorCount = Mathf.Clamp(config.GemColorCount, 1, 5);

            for (int row = 0; row < config.Rows; row++)
            {
                for (int col = 0; col < config.Columns; col++)
                {
                    if (data.IsPlayerCell(col, row)) continue;

                    GemColor color = PickNoMatchColor(data, col, row, colorCount);
                    var gemData = new GemData(color, col, row);
                    data.SetGem(col, row, gemData);

                    Vector2 worldPos = GridUtils.GridToWorld(col, row, origin, config.CellSize);
                    pool.Get(gemData, worldPos);
                }
            }
        }

        // Pick a color that won't create a 3-in-a-row left or below.
        private GemColor PickNoMatchColor(BoardData data, int col, int row, int colorCount)
        {
            var forbidden = new HashSet<GemColor>();

            // Check left: if two to the left are same color, forbid it
            if (col >= 2)
            {
                GemData left1 = data.GetGem(col - 1, row);
                GemData left2 = data.GetGem(col - 2, row);
                if (left1 != null && left2 != null && left1.Color == left2.Color)
                    forbidden.Add(left1.Color);
            }

            // Check below: if two below are same color, forbid it
            if (row >= 2)
            {
                GemData below1 = data.GetGem(col, row - 1);
                GemData below2 = data.GetGem(col, row - 2);
                if (below1 != null && below2 != null && below1.Color == below2.Color)
                    forbidden.Add(below1.Color);
            }

            // Build available colors
            var available = new List<GemColor>();
            for (int i = 0; i < colorCount; i++)
            {
                var c = (GemColor)i;
                if (!forbidden.Contains(c)) available.Add(c);
            }

            if (available.Count == 0)
            {
                // Fallback: should never happen with 5 colors and at most 2 forbidden
                return (GemColor)Random.Range(0, colorCount);
            }

            return available[Random.Range(0, available.Count)];
        }
    }
}
