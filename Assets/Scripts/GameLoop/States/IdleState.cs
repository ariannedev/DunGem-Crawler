using UnityEngine;

namespace DunGemCrawler
{
    public class IdleState : GameStateBase
    {
        public override void OnEnter()
        {
            Board.Input.OnSwapRequested += HandleSwapRequest;
            Board.Input.OnCellPressed += HandleCellPressed;
        }

        public override void OnExit()
        {
            Board.Input.OnSwapRequested -= HandleSwapRequest;
            Board.Input.OnCellPressed -= HandleCellPressed;
        }

        private void HandleCellPressed(Vector2Int cell)
        {
            // Highlight selected gem
            if (Board.Data.InBounds(cell))
            {
                var gem = Board.Data.GetGem(cell);
                gem?.View?.SetHighlight(true);
            }
        }

        private void HandleSwapRequest(Vector2Int a, Vector2Int b)
        {
            // Clear any highlight
            Board.Data.GetGem(a)?.View?.SetHighlight(false);

            // Reject if either cell is out of bounds
            if (!Board.Data.InBounds(a) || !Board.Data.InBounds(b)) return;

            // Reject if a cell is the player
            if (Board.Data.IsPlayerCell(a) || Board.Data.IsPlayerCell(b)) return;

            // Reject if either cell is empty
            if (Board.Data.GetGem(a) == null || Board.Data.GetGem(b) == null) return;

            Board.PendingSwapA = a;
            Board.PendingSwapB = b;
            FSM.Enter<SwapState>();
        }
    }
}
