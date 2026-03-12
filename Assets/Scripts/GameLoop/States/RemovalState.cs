using System.Collections;
using UnityEngine;

namespace DunGemCrawler
{
    public class RemovalState : GameStateBase
    {
        public override void OnEnter()
        {
            Board.StartCoroutine(RunRemoval());
        }

        private IEnumerator RunRemoval()
        {
            foreach (var cell in Board.PendingRemovals)
            {
                GemData gem = Board.Data.GetGem(cell);
                if (gem?.View != null)
                    Board.Pool.Return(gem.View);

                Board.Data.SetGem(cell, null);

                // Flash the dungeon tile beneath
                Board.Dungeon.RevealCell(cell);
            }

            // Wait for flash animations to be visible (brief pause; flashes run independently)
            yield return new WaitForSeconds(Board.Config.FlashDuration * 0.1f);

            FSM.Enter<GravityState>();
        }
    }
}
