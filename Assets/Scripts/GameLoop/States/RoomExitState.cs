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

            // Return all gems to the pool and wipe gem grid (use old dimensions)
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

            // Clear active enemies
            Board.Enemies.ClearAll();

            // Destroy old dungeon tile views
            foreach (Transform child in Board.TileParent)
                Object.Destroy(child.gameObject);

            yield return new WaitForSeconds(0.25f);

            // Load new level config and rebuild data/systems
            LevelConfig nextLevel = Board.Progression.Get(Board.FloorNumber);
            Board.LoadLevel(nextLevel);

            // Reset player to top-centre of the new grid
            Vector2Int startCell = new Vector2Int(
                nextLevel.Columns / 2, nextLevel.Rows - 1);
            Board.Data.PlayerCell    = startCell;
            Board.Player.Data.Cell   = startCell;
            Board.Player.View.transform.position = Board.GridToWorld(startCell);

            // Clear state machine blackboard
            Board.HasPendingPlayerMove = false;
            Board.PendingRemovals.Clear();
            Board.LineClearBothCells.Clear();
            Board.CascadeDepth = 0;

            // Generate new room
            var initializer = new BoardInitializer();
            initializer.Initialize(Board.Data, nextLevel, Board.Pool,
                Board.DungeonTilePrefab, Board.TileParent, Board.BoardOrigin,
                Board.Player.Data);

            Board.RefreshUI();
            FSM.Enter<IdleState>();
        }
    }
}
