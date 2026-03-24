using System.Collections.Generic;
using UnityEngine;

namespace DunGemCrawler
{
    public class CascadeCheckState : GameStateBase
    {
        private const int MaxCascadeDepth = 20;

        public override void OnEnter()
        {
            if (Board.CascadeDepth >= MaxCascadeDepth)
            {
                Finish();
                return;
            }

            var matches = Board.Detector.FindAllMatches(Board.Data);

            if (matches.Count == 0)
            {
                Finish();
                return;
            }

            Board.CascadeDepth++;

            // Collect removals, creating special gems for 4/5+ matches
            Board.LineClearBothCells.Clear();
            Board.PendingRemovals = Board.Detector.ProcessMatches(
                Board.Data, matches,
                null, null,
                Board.LineClearBothCells);

            // Check if any cascade match is adjacent to player — only removed cells count
            if (!Board.HasPendingPlayerMove)
            {
                Vector2Int playerCell = Board.Data.PlayerCell;
                foreach (var cell in Board.PendingRemovals)
                {
                    if (GridUtils.IsAdjacent(cell, playerCell))
                    {
                        Board.HasPendingPlayerMove = true;
                        Board.PendingPlayerMove = cell;
                        break;
                    }
                }
            }

            FSM.Enter<RemovalState>();
        }

        private void Finish()
        {
            Board.CascadeDepth = 0;

            if (Board.HasPendingPlayerMove)
            {
                Board.HasPendingPlayerMove = false;
                FSM.Enter<PlayerMoveState>();
            }
            else
            {
                FSM.Enter<EnemyAttackState>();
            }
        }
    }
}
