using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DunGemCrawler
{
    public class RemovalState : GameStateBase
    {
        private static readonly Vector2Int[] Dirs =
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
        };

        public override void OnEnter()
        {
            Board.StartCoroutine(RunRemoval());
        }

        private IEnumerator RunRemoval()
        {
            // Expand PendingRemovals to include special gem effects, with chain reactions.
            ExpandSpecialGems(Board.PendingRemovals, Board.LineClearBothCells, Board.Data);

            // Award score: scales with match size and cascade depth
            int scoreGain = Board.PendingRemovals.Count * 50 * (1 + Board.CascadeDepth);
            Board.Score += scoreGain;

            var hitEnemies = new HashSet<Vector2Int>();
            var chippedIce = new HashSet<Vector2Int>(); // avoid double-chipping per batch

            foreach (var cell in Board.PendingRemovals)
            {
                GemData gem = Board.Data.GetGem(cell);

                // Frozen gems resist direct removal — they can only be freed by adjacent matches.
                if (gem?.Modifier?.Type == GemModifierType.Frozen) continue;

                if (gem?.View != null)
                    Board.Pool.Return(gem.View);

                Board.Data.SetGem(cell, null);
                Board.Dungeon.RevealCell(cell);

                foreach (var dir in Dirs)
                {
                    Vector2Int adj = cell + dir;

                    // Damage enemies adjacent to this removal
                    if (!hitEnemies.Contains(adj) && Board.Enemies.HasEnemy(adj))
                    {
                        bool killed = Board.Enemies.DamageEnemy(adj, 1);
                        hitEnemies.Add(adj);
                        if (killed) Board.Score += 500;
                    }

                    // Chip ice on adjacent frozen gems
                    if (!chippedIce.Contains(adj) && Board.Data.IsFrozenCell(adj))
                    {
                        GemData frozen = Board.Data.GetGem(adj);
                        if (frozen?.Modifier != null)
                        {
                            chippedIce.Add(adj);
                            frozen.Modifier.IceLayers--;
                            if (frozen.Modifier.IceLayers <= 0)
                            {
                                frozen.Modifier = null;
                                Board.Data.FrozenCells.Remove(adj);
                                frozen.View?.SetFrozen(0);
                            }
                            else
                            {
                                frozen.View?.SetFrozen(frozen.Modifier.IceLayers);
                            }
                        }
                    }
                }
            }

            Board.RefreshUI();

            yield return new WaitForSeconds(Board.Config.FlashDuration * 0.1f);

            FSM.Enter<GravityState>();
        }

        // Iteratively expand the removal list for LineClear and ColorBomb gems.
        // Frozen gems are immune to special-gem expansion.
        private void ExpandSpecialGems(List<Vector2Int> removals,
            HashSet<Vector2Int> lineClearBothCells, BoardData data)
        {
            var inRemovalSet = new HashSet<Vector2Int>(removals);
            var toProcess    = new Queue<Vector2Int>(removals);

            while (toProcess.Count > 0)
            {
                Vector2Int cell = toProcess.Dequeue();
                GemData gem = data.GetGem(cell);
                if (gem == null || gem.Type == GemType.Normal) continue;

                var expansion = new List<Vector2Int>();

                if (gem.Type == GemType.LineClear)
                {
                    bool clearBoth = lineClearBothCells.Contains(cell);
                    if (gem.LineClearHorizontal || clearBoth)
                        AddRow(data, cell.y, expansion);
                    if (!gem.LineClearHorizontal || clearBoth)
                        AddColumn(data, cell.x, expansion);
                }
                else if (gem.Type == GemType.ColorBomb)
                {
                    AddAllOfColor(data, gem.Color, expansion);
                }

                foreach (var c in expansion)
                {
                    if (inRemovalSet.Add(c))
                    {
                        removals.Add(c);
                        toProcess.Enqueue(c);
                    }
                }
            }
        }

        // Frozen gems are excluded from special-effect expansion.
        private void AddRow(BoardData data, int row, List<Vector2Int> list)
        {
            for (int col = 0; col < data.Columns; col++)
            {
                var cell = new Vector2Int(col, row);
                if (data.GetGem(cell) != null && !data.IsFrozenCell(cell))
                    list.Add(cell);
            }
        }

        private void AddColumn(BoardData data, int col, List<Vector2Int> list)
        {
            for (int row = 0; row < data.Rows; row++)
            {
                var cell = new Vector2Int(col, row);
                if (data.GetGem(cell) != null && !data.IsFrozenCell(cell))
                    list.Add(cell);
            }
        }

        private void AddAllOfColor(BoardData data, GemColor color, List<Vector2Int> list)
        {
            for (int col = 0; col < data.Columns; col++)
                for (int row = 0; row < data.Rows; row++)
                {
                    var cell = new Vector2Int(col, row);
                    GemData g = data.GetGem(cell);
                    if (g != null && g.Color == color && !data.IsFrozenCell(cell))
                        list.Add(cell);
                }
        }
    }
}
