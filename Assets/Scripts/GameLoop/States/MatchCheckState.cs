using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DunGemCrawler
{
    public class MatchCheckState : GameStateBase
    {
        public override void OnEnter()
        {
            Board.StartCoroutine(RunCheck());
        }

        private IEnumerator RunCheck()
        {
            var matches = Board.Detector.FindMatchesFromSwap(
                Board.Data, Board.PendingSwapA, Board.PendingSwapB);

            if (matches.Count == 0)
            {
                // No match — reverse the swap
                yield return Board.StartCoroutine(ReverseSwap());
                FSM.Enter<IdleState>();
                yield break;
            }

            // Collect all cells to remove
            var toRemove = new List<Vector2Int>();
            foreach (var m in matches)
                foreach (var c in m.MatchedCells)
                    if (!toRemove.Contains(c)) toRemove.Add(c);

            Board.PendingRemovals = toRemove;

            // Determine player move
            Board.HasPendingPlayerMove = Board.Detector.CheckPlayerMove(
                Board.Data, matches, Board.PendingSwapB - Board.PendingSwapA,
                out Vector2Int moveTarget);
            Board.PendingPlayerMove = moveTarget;

            FSM.Enter<RemovalState>();
        }

        private IEnumerator ReverseSwap()
        {
            Vector2Int a = Board.PendingSwapA;
            Vector2Int b = Board.PendingSwapB;
            GemData gemA = Board.Data.GetGem(a); // note: already swapped, so A is at b pos
            GemData gemB = Board.Data.GetGem(b);

            Vector2 posA = Board.GridToWorld(a);
            Vector2 posB = Board.GridToWorld(b);

            Board.Data.SwapGems(a, b); // swap back

            int[] remaining = { 2 };
            Board.StartCoroutine(AnimOne(Board.Data.GetGem(a)?.View, posA, remaining));
            Board.StartCoroutine(AnimOne(Board.Data.GetGem(b)?.View, posB, remaining));

            while (remaining[0] > 0)
                yield return null;
        }

        private IEnumerator AnimOne(GemView view, Vector2 target, int[] remaining)
        {
            if (view != null)
                yield return view.StartCoroutine(view.SwapTo(target, Board.Config.SwapAnimDuration));
            remaining[0]--;
        }
    }
}
