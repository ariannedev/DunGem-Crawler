using System.Collections;
using UnityEngine;

namespace DunGemCrawler
{
    public class RoomExitState : GameStateBase
    {
        public override void OnEnter()
        {
            Board.StartCoroutine(RunRoomExit());
        }

        private IEnumerator RunRoomExit()
        {
            Board.FloorNumber++;

            // Return all gems to the pool and wipe gem grid
            for (int col = 0; col < Board.Data.Columns; col++)
            {
                for (int row = 0; row < Board.Data.Rows; row++)
                {
                    GemData gem = Board.Data.GetGem(col, row);
                    if (gem?.View != null)
                        Board.Pool.Return(gem.View);
                    Board.Data.SetGem(col, row, null);
                }
            }

            // Destroy old dungeon tile views
            foreach (Transform child in Board.TileParent)
                Object.Destroy(child.gameObject);

            // Brief pause so the board clears visually before the next one appears
            yield return new WaitForSeconds(0.25f);

            // Reset player to top-center
            Vector2Int startCell = new Vector2Int(
                Board.Config.Columns / 2, Board.Config.Rows - 1);
            Board.Data.PlayerCell = startCell;
            Board.Player.Data.Cell = startCell;
            Board.Player.View.transform.position = Board.GridToWorld(startCell);

            // Clear state machine blackboard
            Board.HasPendingPlayerMove = false;
            Board.PendingRemovals.Clear();
            Board.CascadeDepth = 0;

            // Generate new room
            var initializer = new BoardInitializer();
            initializer.Initialize(Board.Data, Board.Config, Board.Pool,
                Board.DungeonTilePrefab, Board.TileParent, Board.BoardOrigin,
                Board.Player.Data);

            Board.RefreshUI();
            FSM.Enter<IdleState>();
        }
    }
}
