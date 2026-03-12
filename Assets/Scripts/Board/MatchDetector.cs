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

        private void ScanCell(BoardData data, Vector2Int cell, List<MatchResult> results)
        {
            GemData gem = data.GetGem(cell);
            if (gem == null) return;
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
                if (g == null || g.Color != color) break;
                run.Add(next);
            }
            // Extend in negative direction
            for (int i = 1; ; i++)
            {
                Vector2Int next = origin - axis * i;
                if (!data.InBounds(next)) break;
                GemData g = data.GetGem(next);
                if (g == null || g.Color != color) break;
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
