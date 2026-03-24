using UnityEngine;

namespace DunGemCrawler
{
    public class EnemyData
    {
        public int         Col;
        public int         Row;
        public int         Health;
        public int         MaxHealth;
        public int         AttackDamage;
        public EnemyView   View;
        public EnemyConfig Config;

        public EnemyType   Type => Config?.Type ?? EnemyType.Melee;
        public Vector2Int  Cell => new Vector2Int(Col, Row);

        public EnemyData(int col, int row, EnemyConfig config = null)
        {
            Col          = col;
            Row          = row;
            Config       = config;
            MaxHealth    = config?.MaxHealth    ?? 3;
            Health       = MaxHealth;
            AttackDamage = config?.AttackDamage ?? 1;
        }
    }
}
