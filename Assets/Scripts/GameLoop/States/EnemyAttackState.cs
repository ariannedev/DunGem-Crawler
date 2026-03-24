using System.Collections;

namespace DunGemCrawler
{
    public class EnemyAttackState : GameStateBase
    {
        public override void OnEnter()
        {
            Board.StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            // 1. Melee damage from all adjacent enemies
            int damage = Board.Enemies.AttackPlayer(Board.Data.PlayerCell);
            if (damage > 0)
            {
                Board.Player.TakeDamage(damage);
                Board.RefreshUI();
            }

            // 2. Per-type enemy skills (may animate)
            yield return Board.StartCoroutine(Board.Enemies.ExecuteAllSkills(Board));

            FSM.Enter<ObjectiveCheckState>();
        }
    }
}
