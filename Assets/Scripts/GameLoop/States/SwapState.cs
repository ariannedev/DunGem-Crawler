using System.Collections;
using UnityEngine;

namespace DunGemCrawler
{
    public class SwapState : GameStateBase
    {
        public override void OnEnter()
        {
            Board.StartCoroutine(RunSwap());
        }

        private IEnumerator RunSwap()
        {
            Vector2Int a = Board.PendingSwapA;
            Vector2Int b = Board.PendingSwapB;
            GemData gemA = Board.Data.GetGem(a);
            GemData gemB = Board.Data.GetGem(b);

            Vector2 posA = Board.GridToWorld(a);
            Vector2 posB = Board.GridToWorld(b);

            // Swap data immediately
            Board.Data.SwapGems(a, b);

            // Animate simultaneously
            int[] remaining = { 2 };
            Board.StartCoroutine(AnimOne(gemA?.View, posB, remaining));
            Board.StartCoroutine(AnimOne(gemB?.View, posA, remaining));

            while (remaining[0] > 0)
                yield return null;

            FSM.Enter<MatchCheckState>();
        }

        private IEnumerator AnimOne(GemView view, Vector2 target, int[] remaining)
        {
            if (view != null)
                yield return view.StartCoroutine(view.SwapTo(target, Board.Config.SwapAnimDuration));
            remaining[0]--;
        }
    }
}
