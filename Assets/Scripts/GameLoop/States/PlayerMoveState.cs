using System.Collections;
using UnityEngine;

namespace DunGemCrawler
{
    public class PlayerMoveState : GameStateBase
    {
        public override void OnEnter()
        {
            Board.StartCoroutine(RunMove());
        }

        private IEnumerator RunMove()
        {
            Vector2Int target = Board.PendingPlayerMove;

            // Remove any gem that may have ended up at the destination (safety check)
            GemData existing = Board.Data.GetGem(target);
            if (existing?.View != null)
                Board.Pool.Return(existing.View);
            Board.Data.SetGem(target, null);

            // Update player position in data
            Board.Data.PlayerCell = target;
            Board.Player.Data.Cell = target;

            // Animate
            Vector2 worldTarget = Board.GridToWorld(target);
            yield return Board.StartCoroutine(
                Board.Player.View.MoveTo(worldTarget, Board.Config.PlayerMoveDuration));

            // Check tile at destination
            TileType tile = Board.Dungeon.GetType(target.x, target.y);
            if (tile == TileType.Door)
            {
                FSM.Enter<RoomExitState>();
                yield break;
            }

            // Run gravity to fill the cell the player vacated
            FSM.Enter<GravityState>();
        }
    }
}
