using System.Collections;
using UnityEngine;

namespace DunGemCrawler
{
    public class GravityState : GameStateBase
    {
        public override void OnEnter()
        {
            Board.StartCoroutine(RunGravity());
        }

        private IEnumerator RunGravity()
        {
            Vector2Int? blocked = Board.HasPendingPlayerMove
                ? Board.PendingPlayerMove
                : (Vector2Int?)null;

            var moves = Board.Gravity.ResolveGravity(Board.Data, blocked);
            yield return Board.StartCoroutine(
                Board.Gravity.ApplyMoves(Board.Data, moves, Board.Config,
                    Board.Pool, Board.BoardOrigin, Board));

            FSM.Enter<CascadeCheckState>();
        }
    }
}
