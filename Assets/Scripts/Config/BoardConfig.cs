using UnityEngine;

namespace DunGemCrawler
{
    [CreateAssetMenu(fileName = "BoardConfig", menuName = "DunGemCrawler/Board Config")]
    public class BoardConfig : ScriptableObject
    {
        [Header("Grid")]
        public int Columns = 8;
        public int Rows = 8;
        public float CellSize = 1f;

        [Header("Gems")]
        public int GemColorCount = 5;

        [Header("Animation")]
        public float GemFallSpeed = 8f;
        public float FlashDuration = 0.4f;
        public float SwapAnimDuration = 0.15f;
        public float PlayerMoveDuration = 0.2f;

        [Header("Dungeon Tile Weights (relative)")]
        public float WeightFloor = 60f;
        public float WeightWall = 15f;
        public float WeightDoor = 5f;
        public float WeightTreasure = 10f;
        public float WeightEnemy = 10f;
    }
}
