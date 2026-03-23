using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DunGemCrawler
{
    public struct GemFallMove
    {
        public int  Col;         // destination column
        public int  FromRow;     // source row (vertical/spawn); same as ToRow for laterals
        public int  ToRow;       // destination row
        public bool IsNewSpawn;  // gem created fresh
        public bool IsLateral;   // gem slides sideways
        public int  FromCol;     // source column (lateral only)
    }

    public class GravityResolver
    {
        public List<GemFallMove> ResolveGravity(BoardData data, Vector2Int? blockedCell = null)
        {
            var moves = new List<GemFallMove>();

            // Phase 1 — compact existing gems downward within each section (no spawns yet)
            for (int col = 0; col < data.Columns; col++)
                CompactColumn(data, col, moves, blockedCell);

            // Phase 2 — lateral fills: a gem from the row ABOVE the gap in an adjacent
            // column slides diagonally down into the gap, mimicking gravity flowing around
            // the player obstacle.
            var donorCols = new HashSet<int>();
            FillBelowObstacleLaterally(data, data.PlayerCell, moves, donorCols, blockedCell);
            if (blockedCell.HasValue)
                FillBelowObstacleLaterally(data, blockedCell.Value, moves, donorCols, blockedCell);

            // Phase 1b — re-compact donor columns: stealing a gem from the middle of a
            // column leaves a gap there; shift all gems above it downward so the gap
            // rises to the top row, ready for a single top spawn.
            foreach (int col in donorCols)
                CompactColumn(data, col, moves, blockedCell);

            // Phase 3 — spawn new gems from off-screen top for remaining empty cells
            for (int col = 0; col < data.Columns; col++)
                SpawnForTopSection(data, col, moves, blockedCell);

            return moves;
        }

        // ── Phase 1 ─────────────────────────────────────────────────────────────────

        private void CompactColumn(BoardData data, int col, List<GemFallMove> moves,
            Vector2Int? blockedCell)
        {
            var obstacles = GetObstacles(data, col, blockedCell);

            int sectionStart = 0;
            foreach (int obstacleRow in obstacles)
            {
                CompactSection(data, col, sectionStart, obstacleRow - 1, moves);
                sectionStart = obstacleRow + 1;
            }
            CompactSection(data, col, sectionStart, data.Rows - 1, moves);
        }

        private void CompactSection(BoardData data, int col, int minRow, int maxRow,
            List<GemFallMove> moves)
        {
            if (minRow > maxRow) return;

            int writeRow = minRow;
            for (int readRow = minRow; readRow <= maxRow; readRow++)
            {
                GemData gem = data.GetGem(col, readRow);
                if (gem == null) continue;

                if (readRow != writeRow)
                {
                    moves.Add(new GemFallMove { Col = col, FromRow = readRow, ToRow = writeRow });
                    data.SetGem(col, writeRow, gem);
                    data.SetGem(col, readRow, null);
                }
                writeRow++;
            }
            // Empty slots at top of section (writeRow..maxRow) are left null for Phase 2/3
        }

        // ── Phase 2 ─────────────────────────────────────────────────────────────────

        // Cells below an obstacle can't receive gems falling from above, so we slide
        // a gem in from the left or right neighbour instead.
        private void FillBelowObstacleLaterally(BoardData data, Vector2Int obstacle,
            List<GemFallMove> moves, HashSet<int> donorCols, Vector2Int? blockedCell)
        {
            int col = obstacle.x;
            int obstacleRow = obstacle.y;

            for (int r = obstacleRow - 1; r >= 0; r--)
            {
                if (data.IsPlayerCell(col, r)) continue; // never fill the player's current cell
                if (blockedCell.HasValue && blockedCell.Value.x == col
                    && blockedCell.Value.y == r) continue; // never fill the player's destination
                if (data.GetGem(col, r) != null) continue; // already occupied

                // Try left neighbour first, then right.
                // Search upward from r+1 so the gem falls diagonally downward.
                // We scan past r+1 in case Phase 1 already compacted that row away
                // (e.g. a horizontal match cleared gems in both this column and the
                // adjacent column at the same row, so the r+1 slot is now empty).
                bool filled = false;
                foreach (int adjCol in new[] { col - 1, col + 1 })
                {
                    for (int donorRow = r + 1; donorRow < data.Rows; donorRow++)
                    {
                        if (!data.InBounds(adjCol, donorRow)) break;
                        if (data.IsPlayerCell(adjCol, donorRow)) break; // don't cross the player
                        GemData gem = data.GetGem(adjCol, donorRow);
                        if (gem == null) continue;

                        data.SetGem(col, r, gem);
                        data.SetGem(adjCol, donorRow, null);
                        moves.Add(new GemFallMove
                        {
                            Col = col, ToRow = r, FromRow = donorRow,
                            IsLateral = true, FromCol = adjCol
                        });
                        donorCols.Add(adjCol);
                        filled = true;
                        break;
                    }
                    if (filled) break;
                }

                // If no neighbour had a gem the cell stays empty;
                // Phase 3 won't reach it (blocked from above), so it fills next turn
                // when a match clears adjacent gems.
                _ = filled;
            }
        }

        // ── Phase 3 ─────────────────────────────────────────────────────────────────

        // Spawn new gems (off-screen above board) for every empty cell in the topmost
        // unobstructed section of each column.
        private void SpawnForTopSection(BoardData data, int col, List<GemFallMove> moves,
            Vector2Int? blockedCell)
        {
            int topSectionMin = GetTopSectionMin(data, col, blockedCell);

            // Scan bottom-to-top so the lowest empty row gets fromRow = data.Rows,
            // the next gets data.Rows+1, etc. — all gems travel the same distance
            // and arrive simultaneously, stacked like a column dropping into place.
            int spawnOffset = 0;
            for (int r = topSectionMin; r < data.Rows; r++)
            {
                if (data.GetGem(col, r) != null) continue;

                moves.Add(new GemFallMove
                {
                    Col        = col,
                    FromRow    = data.Rows + spawnOffset,
                    ToRow      = r,
                    IsNewSpawn = true
                });
                spawnOffset++;
            }
        }

        // ── Helpers ─────────────────────────────────────────────────────────────────

        private int GetTopSectionMin(BoardData data, int col, Vector2Int? blockedCell)
        {
            int highestObstacle = -1;
            if (data.PlayerCell.x == col)
                highestObstacle = Mathf.Max(highestObstacle, data.PlayerCell.y);
            if (blockedCell.HasValue && blockedCell.Value.x == col)
                highestObstacle = Mathf.Max(highestObstacle, blockedCell.Value.y);
            return highestObstacle + 1;
        }

        private List<int> GetObstacles(BoardData data, int col, Vector2Int? blockedCell)
        {
            var obstacles = new List<int>();
            if (data.PlayerCell.x == col)
                obstacles.Add(data.PlayerCell.y);
            if (blockedCell.HasValue && blockedCell.Value.x == col
                && blockedCell.Value.y != data.PlayerCell.y)
                obstacles.Add(blockedCell.Value.y);
            obstacles.Sort();
            return obstacles;
        }

        // ── ApplyMoves ───────────────────────────────────────────────────────────────

        public IEnumerator ApplyMoves(BoardData data, List<GemFallMove> moves,
            BoardConfig config, GemPool pool, Vector2 boardOrigin, MonoBehaviour host)
        {
            // Create gem data + views for spawns (safe colours so no instant cascade)
            foreach (var move in moves)
            {
                if (!move.IsNewSpawn) continue;

                GemColor safeColor = PickSafeSpawnColor(data, move.Col, move.ToRow,
                    Mathf.Clamp(config.GemColorCount, 1, 5));
                var gemData = new GemData(safeColor, move.Col, move.ToRow);
                data.SetGem(move.Col, move.ToRow, gemData);

                Vector2 spawnPos = GridUtils.GridToWorld(
                    move.Col, move.FromRow, boardOrigin, config.CellSize);
                pool.Get(gemData, spawnPos);
                // View starts off-screen and MoveTo will bring it down
            }

            // Animate all moves simultaneously:
            //   • vertical falls  — view is already at (col, fromRow), animate to (col, toRow)
            //   • lateral slides  — view is already at (fromCol, row), animate to (col, row)
            //   • spawns          — view is already off-screen, animate to (col, toRow)
            int[] remaining = { 0 };
            foreach (var move in moves)
            {
                GemData gem = data.GetGem(move.Col, move.ToRow);
                if (gem?.View == null) continue;

                Vector2 target = GridUtils.GridToWorld(
                    move.Col, move.ToRow, boardOrigin, config.CellSize);

                remaining[0]++;
                host.StartCoroutine(FallOne(gem.View, target, config.GemFallSpeed, remaining));
            }

            while (remaining[0] > 0)
                yield return null;
        }

        // ── Safe colour picker ───────────────────────────────────────────────────────

        private GemColor PickSafeSpawnColor(BoardData data, int col, int row, int colorCount)
        {
            var forbidden = new HashSet<GemColor>();

            ForbidIfPair(data, col - 2, row,     col - 1, row,     forbidden);
            ForbidIfPair(data, col + 1, row,     col + 2, row,     forbidden);
            ForbidIfPair(data, col - 1, row,     col + 1, row,     forbidden);
            ForbidIfPair(data, col,     row - 2, col,     row - 1, forbidden);
            ForbidIfPair(data, col,     row + 1, col,     row + 2, forbidden);
            ForbidIfPair(data, col,     row - 1, col,     row + 1, forbidden);

            var available = new List<GemColor>();
            for (int i = 0; i < colorCount; i++)
            {
                var c = (GemColor)i;
                if (!forbidden.Contains(c)) available.Add(c);
            }

            return available.Count > 0
                ? available[Random.Range(0, available.Count)]
                : (GemColor)Random.Range(0, colorCount);
        }

        private void ForbidIfPair(BoardData data, int c1, int r1, int c2, int r2,
            HashSet<GemColor> forbidden)
        {
            if (!data.InBounds(c1, r1) || !data.InBounds(c2, r2)) return;
            var g1 = data.GetGem(c1, r1);
            var g2 = data.GetGem(c2, r2);
            if (g1 != null && g2 != null && g1.Color == g2.Color)
                forbidden.Add(g1.Color);
        }

        private IEnumerator FallOne(GemView view, Vector2 target, float speed, int[] remaining)
        {
            yield return view.StartCoroutine(view.MoveTo(target, speed));
            remaining[0]--;
        }
    }
}
