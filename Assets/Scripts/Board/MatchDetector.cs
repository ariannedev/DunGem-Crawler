using System.Collections.Generic;
using UnityEngine;

namespace DunGemCrawler
{
    public class MatchDetector
    {
        // Find matches resulting from swapping cells a and b.
        // Only scans the affected cells (O(N+M)).
        public List<MatchResult> FindMatchesFromSwap(BoardData data, Vector2Int a, Vector2Int b)
        {
            var results = new List<MatchResult>();
            ScanCell(data, a, results);
            ScanCell(data, b, results);
            MergeOverlapping(results);
            return results;
        }

        // Full board scan — used for cascade detection.
        public List<MatchResult> FindAllMatches(BoardData data)
        {
            var results = new List<MatchResult>();
            for (int col = 0; col < data.Columns; col++)
                for (int row = 0; row < data.Rows; row++)
                    ScanCell(data, new Vector2Int(col, row), results);
            MergeOverlapping(results);
            return results;
        }

        // Determine if any match is adjacent to the player and compute move target.
        public bool CheckPlayerMove(BoardData data, List<MatchResult> matches,
            Vector2Int swapDir, out Vector2Int moveTarget)
        {
            Vector2Int playerCell = data.PlayerCell;
            foreach (var match in matches)
            {
                foreach (var cell in match.MatchedCells)
                {
                    if (GridUtils.IsAdjacent(cell, playerCell))
                    {
                        // Move player in the direction from playerCell toward the matched cell
                        Vector2Int dir = cell - playerCell;
                        Vector2Int target = playerCell + dir;
                        if (data.InBounds(target))
                        {
                            moveTarget = target;
                            return true;
                        }
                    }
                }
            }
            moveTarget = playerCell;
            return false;
        }

        // ── Special gem processing ────────────────────────────────────────────────
        //
        // Call this after FindMatchesFromSwap / FindAllMatches.
        // It:
        //   • Converts one cell to a special gem when match size >= 4 (no existing specials
        //     in that match).
        //   • Excludes the newly created special gem from the removal list so it stays on board.
        //   • Records which LineClear gems should clear BOTH axes (activating match >= 4 cells).
        //
        // Returns: the final list of cells to remove.
        // swapA/swapB  — pass null for cascade calls (no preferred pivot).
        public List<Vector2Int> ProcessMatches(
            BoardData data,
            List<MatchResult> matches,
            Vector2Int? swapA, Vector2Int? swapB,
            HashSet<Vector2Int> lineClearBothCells)
        {
            var toRemove = new HashSet<Vector2Int>();

            foreach (var match in matches)
            {
                var cells = match.MatchedCells;

                // Separate existing specials from normal gems in this match
                var existingSpecials = new List<Vector2Int>();
                var normals          = new List<Vector2Int>();
                foreach (var c in cells)
                {
                    var g = data.GetGem(c);
                    if (g != null && g.Type != GemType.Normal)
                        existingSpecials.Add(c);
                    else
                        normals.Add(c);
                }

                // Existing specials always activate (get removed + trigger expansion in RemovalState)
                foreach (var c in existingSpecials)
                    toRemove.Add(c);

                // If an existing LineClear is activated by a 4+ cell match, flag "clear both"
                if (cells.Count >= 4)
                    foreach (var c in existingSpecials)
                        if (data.GetGem(c)?.Type == GemType.LineClear)
                            lineClearBothCells.Add(c);

                // Create a new special only when there are no existing specials in this match
                if (existingSpecials.Count == 0 && cells.Count >= 4)
                {
                    Vector2Int specialCell = PickSpecialCell(cells, swapA, swapB);

                    if (cells.Count >= 5)
                    {
                        ConvertToColorBomb(data, specialCell);
                    }
                    else // exactly 4
                    {
                        bool horizontal = IsHorizontalMatch(cells);
                        ConvertToLineClear(data, specialCell, horizontal);
                    }

                    // All cells EXCEPT the newly created special get removed
                    foreach (var c in cells)
                        if (c != specialCell) toRemove.Add(c);
                }
                else
                {
                    // Normal 3-match (or specials already handled above)
                    foreach (var c in normals)
                        toRemove.Add(c);
                }
            }

            return new List<Vector2Int>(toRemove);
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private Vector2Int PickSpecialCell(List<Vector2Int> cells,
            Vector2Int? swapA, Vector2Int? swapB)
        {
            // Prefer the initiating swap cells (they feel most "earned")
            if (swapA.HasValue || swapB.HasValue)
                foreach (var c in cells)
                    if (c == swapA || c == swapB) return c;

            // Cascade: pick the cell closest to the centre of the match
            float avgCol = 0, avgRow = 0;
            foreach (var c in cells) { avgCol += c.x; avgRow += c.y; }
            avgCol /= cells.Count; avgRow /= cells.Count;
            Vector2Int best = cells[0];
            float bestDist = float.MaxValue;
            foreach (var c in cells)
            {
                float d = (c.x - avgCol) * (c.x - avgCol) + (c.y - avgRow) * (c.y - avgRow);
                if (d < bestDist) { bestDist = d; best = c; }
            }
            return best;
        }

        private bool IsHorizontalMatch(List<Vector2Int> cells)
        {
            // Count distinct rows vs distinct columns — whichever is fewer indicates the axis
            int rows = 0, cols = 0;
            var seenRows = new HashSet<int>(); var seenCols = new HashSet<int>();
            foreach (var c in cells) { seenRows.Add(c.y); seenCols.Add(c.x); }
            rows = seenRows.Count; cols = seenCols.Count;
            return rows <= cols; // horizontal if mostly same row
        }

        private void ConvertToLineClear(BoardData data, Vector2Int cell, bool horizontal)
        {
            var gem = data.GetGem(cell);
            if (gem == null) return;
            gem.Type = GemType.LineClear;
            gem.LineClearHorizontal = horizontal;
            gem.View?.SetGemType(GemType.LineClear, horizontal);
        }

        private void ConvertToColorBomb(BoardData data, Vector2Int cell)
        {
            var gem = data.GetGem(cell);
            if (gem == null) return;
            gem.Type = GemType.ColorBomb;
            gem.View?.SetGemType(GemType.ColorBomb);
        }

        // ── ─────────────────────────────────────────────────────────────────────

        private void ScanCell(BoardData data, Vector2Int cell, List<MatchResult> results)
        {
            GemData gem = data.GetGem(cell);
            if (gem == null || gem.Modifier?.Type == GemModifierType.Frozen) return;
            GemColor color = gem.Color;

            // Horizontal run
            var hRun = BuildRun(data, cell, color, Vector2Int.right);
            if (hRun.Count >= 3)
                AddOrMerge(results, hRun);

            // Vertical run
            var vRun = BuildRun(data, cell, color, Vector2Int.up);
            if (vRun.Count >= 3)
                AddOrMerge(results, vRun);
        }

        private List<Vector2Int> BuildRun(BoardData data, Vector2Int origin,
            GemColor color, Vector2Int axis)
        {
            var run = new List<Vector2Int> { origin };

            // Extend in positive direction
            for (int i = 1; ; i++)
            {
                Vector2Int next = origin + axis * i;
                if (!data.InBounds(next)) break;
                GemData g = data.GetGem(next);
                if (g == null || g.Color != color || g.Modifier?.Type == GemModifierType.Frozen) break;
                run.Add(next);
            }
            // Extend in negative direction
            for (int i = 1; ; i++)
            {
                Vector2Int next = origin - axis * i;
                if (!data.InBounds(next)) break;
                GemData g = data.GetGem(next);
                if (g == null || g.Color != color || g.Modifier?.Type == GemModifierType.Frozen) break;
                run.Add(next);
            }

            return run;
        }

        private void AddOrMerge(List<MatchResult> results, List<Vector2Int> cells)
        {
            // Try to find an existing result that shares any cell (for L/T shapes)
            foreach (var existing in results)
            {
                foreach (var c in cells)
                {
                    if (existing.MatchedCells.Contains(c))
                    {
                        foreach (var nc in cells)
                            if (!existing.MatchedCells.Contains(nc))
                                existing.MatchedCells.Add(nc);
                        return;
                    }
                }
            }
            var result = new MatchResult();
            result.MatchedCells.AddRange(cells);
            results.Add(result);
        }

        // Second pass: merge any results that still share cells (handles cross shapes).
        private void MergeOverlapping(List<MatchResult> results)
        {
            bool merged = true;
            while (merged)
            {
                merged = false;
                for (int i = 0; i < results.Count; i++)
                {
                    for (int j = i + 1; j < results.Count; j++)
                    {
                        if (SharesCell(results[i], results[j]))
                        {
                            foreach (var c in results[j].MatchedCells)
                                if (!results[i].MatchedCells.Contains(c))
                                    results[i].MatchedCells.Add(c);
                            results.RemoveAt(j);
                            merged = true;
                            break;
                        }
                    }
                    if (merged) break;
                }
            }
        }

        private bool SharesCell(MatchResult a, MatchResult b)
        {
            foreach (var c in a.MatchedCells)
                if (b.MatchedCells.Contains(c)) return true;
            return false;
        }
    }
}
