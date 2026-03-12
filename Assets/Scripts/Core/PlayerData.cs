using UnityEngine;

namespace DunGemCrawler
{
    public class PlayerData
    {
        public Vector2Int Cell;
        public int Health = 10;

        public PlayerData(Vector2Int startCell)
        {
            Cell = startCell;
        }
    }
}
