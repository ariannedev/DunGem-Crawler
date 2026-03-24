using System.Collections.Generic;
using UnityEngine;

namespace DunGemCrawler
{
    public class BoardInitializer
    {
        public void Initialize(BoardData data, LevelConfig config,
            GemPool pool, DungeonTileView tileViewPrefab,
            Transform tileParent, Vector2 boardOrigin, PlayerData player)
        {
            data.PlayerCell = player.Cell;
            PlaceDungeonTiles(data, config, tileViewPrefab, tileParent, boardOrigin);
            EnsureDoorAtBottom(data, config);
            PlaceGems(data, config, pool, boardOrigin);
        }

        // ── Dungeon tiles ──────────────────────────────────────────────────────────

        private bool IsValidDoorCell(int col, int row, int cols, int rows)
        {
            if (row == rows - 1) return false;
            return row == 0 || col == 0 || col == cols - 1;
        }

        private void PlaceDungeonTiles(BoardData data, LevelConfig config,
            DungeonTileView tileViewPrefab, Transform parent, Vector2 origin)
        {
            // Build override lookup
            var overrides = new Dictionary<Vector2Int, TileType>();
            if (config.TileOverrides != null)
                foreach (var o in config.TileOverrides)
                    overrides[o.Cell] = o.Type;

            float totalWeight = config.WeightFloor + config.WeightWall + config.WeightDoor
                              + config.WeightTreasure + config.WeightEnemy;

            for (int col = 0; col < config.Columns; col++)
            {
                for (int row = 0; row < config.Rows; row++)
                {
                    TileType type;
                    var cell = new Vector2Int(col, row);

                    if (overrides.TryGetValue(cell, out TileType forced))
                        type = forced;
                    else
                        type = RollTileType(config, totalWeight);

                    if (type == TileType.Door && !IsValidDoorCell(col, row, config.Columns, config.Rows))
                        type = TileType.Floor;

                    var tileData = new DungeonTileData(type, col, row);

                    Vector2 worldPos = GridUtils.GridToWorld(col, row, origin, config.CellSize);
                    var view = Object.Instantiate(tileViewPrefab, worldPos, Quaternion.identity, parent);
                    view.SetType(type);

                    // Goal tiles are visible from the start
                    if (type == TileType.Goal)
                        view.SetPermanentlyVisible(true);

                    tileData.View = view;
                    data.SetTile(col, row, tileData);
                }
            }
        }

        private TileType RollTileType(LevelConfig config, float totalWeight)
        {
            float roll = Random.Range(0f, totalWeight);
            if (roll < config.WeightFloor)    return TileType.Floor;
            roll -= config.WeightFloor;
            if (roll < config.WeightWall)     return TileType.Wall;
            roll -= config.WeightWall;
            if (roll < config.WeightDoor)     return TileType.Door;
            roll -= config.WeightDoor;
            if (roll < config.WeightTreasure) return TileType.Treasure;
            return TileType.Enemy;
        }

        private void EnsureDoorAtBottom(BoardData data, LevelConfig config)
        {
            for (int col = 0; col < config.Columns; col++)
                for (int row = 0; row < config.Rows; row++)
                    if (IsValidDoorCell(col, row, config.Columns, config.Rows)
                        && data.GetTile(col, row)?.Type == TileType.Door) return;

            int doorCol = Random.Range(0, config.Columns);
            DungeonTileData tile = data.GetTile(doorCol, 0);
            tile.Type = TileType.Door;
            tile.View.SetType(TileType.Door);
        }

        // ── Gems ──────────────────────────────────────────────────────────────────

        private void PlaceGems(BoardData data, LevelConfig config,
            GemPool pool, Vector2 origin)
        {
            int colorCount = Mathf.Clamp(config.GemColorCount, 1, 5);

            // Build frozen gem lookup
            var frozenLookup = new Dictionary<Vector2Int, FrozenGemPlacement>();
            if (config.FrozenGems != null)
                foreach (var fp in config.FrozenGems)
                    frozenLookup[fp.Cell] = fp;

            for (int row = 0; row < config.Rows; row++)
            {
                for (int col = 0; col < config.Columns; col++)
                {
                    if (data.IsPlayerCell(col, row)) continue;

                    var cell = new Vector2Int(col, row);
                    GemColor   color;
                    GemModifier modifier = null;

                    if (frozenLookup.TryGetValue(cell, out FrozenGemPlacement fp))
                    {
                        color    = fp.Color;
                        modifier = new GemModifier(GemModifierType.Frozen, fp.IceLayers);
                        data.FrozenCells.Add(cell);
                    }
                    else
                    {
                        color = PickNoMatchColor(data, col, row, config, colorCount);
                    }

                    var gemData = new GemData(color, col, row) { Modifier = modifier };
                    data.SetGem(col, row, gemData);

                    Vector2 worldPos = GridUtils.GridToWorld(col, row, origin, config.CellSize);
                    GemView view = pool.Get(gemData, worldPos);

                    if (modifier?.Type == GemModifierType.Frozen)
                        view.SetFrozen(modifier.IceLayers);
                }
            }
        }

        // ── Colour helpers ────────────────────────────────────────────────────────

        private GemColor PickNoMatchColor(BoardData data, int col, int row,
            LevelConfig config, int colorCount)
        {
            var forbidden = new HashSet<GemColor>();

            if (col >= 2)
            {
                GemData l1 = data.GetGem(col - 1, row);
                GemData l2 = data.GetGem(col - 2, row);
                if (l1 != null && l2 != null && l1.Color == l2.Color)
                    forbidden.Add(l1.Color);
            }
            if (row >= 2)
            {
                GemData b1 = data.GetGem(col, row - 1);
                GemData b2 = data.GetGem(col, row - 2);
                if (b1 != null && b2 != null && b1.Color == b2.Color)
                    forbidden.Add(b1.Color);
            }

            var available = new List<GemColor>();
            for (int i = 0; i < colorCount; i++)
            {
                var c = (GemColor)i;
                if (!forbidden.Contains(c)) available.Add(c);
            }

            if (available.Count == 0)
                return (GemColor)Random.Range(0, colorCount);

            return PickWeightedColor(available, config, colorCount);
        }

        // Shared weighted colour picker (used by PickNoMatchColor and GravityResolver).
        public static GemColor PickWeightedColor(List<GemColor> available,
            LevelConfig config, int colorCount)
        {
            float[] weights = config.GemColorWeights;
            if (weights == null || weights.Length != colorCount)
                return available[Random.Range(0, available.Count)];

            float total = 0f;
            foreach (var c in available)
            {
                int idx = (int)c;
                if (idx < weights.Length) total += Mathf.Max(0f, weights[idx]);
            }

            if (total <= 0f)
                return available[Random.Range(0, available.Count)];

            float roll = Random.Range(0f, total);
            foreach (var c in available)
            {
                int   idx = (int)c;
                float w   = idx < weights.Length ? Mathf.Max(0f, weights[idx]) : 0f;
                if (roll < w) return c;
                roll -= w;
            }
            return available[available.Count - 1];
        }
    }
}
