using System.Collections.Generic;
using UnityEngine;

namespace DunGemCrawler
{
    public class MatchResult
    {
        public List<Vector2Int> MatchedCells = new List<Vector2Int>();
        public bool TriggersPlayerMove;
        public Vector2Int PlayerMoveTarget;
    }
}
