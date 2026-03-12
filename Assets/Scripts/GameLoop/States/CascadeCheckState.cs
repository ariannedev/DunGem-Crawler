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

            // Collect removals
            var toRemove = new List<Vector2Int>();
            foreach (var m in matches)
                foreach (var c in m.MatchedCells)
                    if (!toRemove.Contains(c)) toRemove.Add(c);
            Board.PendingRemovals = toRemove;

            // Check if any cascade match is adjacent to player
            if (!Board.HasPendingPlayerMove)
            {
                if (Board.Detector.CheckPlayerMove(Board.Data, matches,
                    Vector2Int.zero, out Vector2Int moveTarget))
                {
                    Board.HasPendingPlayerMove = true;
                    Board.PendingPlayerMove = moveTarget;
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
                FSM.Enter<IdleState>();
            }
        }
    }
}
