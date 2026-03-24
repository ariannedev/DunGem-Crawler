using System;
using System.Collections.Generic;
using UnityEngine;

namespace DunGemCrawler
{
    public enum LevelObjectiveType
    {
        ReachDoor,          // default — reach any Door tile
        ScoreThreshold,     // accumulate TargetScore before leaving
        KeystoneDelivery,   // move a gem of KeystoneColor onto GoalCell
    }

    [Serializable]
    public class CellOverride
    {
        public Vector2Int Cell;
        public TileType   Type;
    }

    [Serializable]
    public class FrozenGemPlacement
    {
        public Vector2Int Cell;
        public GemColor   Color;
        public int        IceLayers = 1; // adjacent matches required to break
    }

    [CreateAssetMenu(fileName = "LevelConfig", menuName = "DunGemCrawler/Level Config")]
    public class LevelConfig : ScriptableObject
    {
        [Header("Grid")]
        public int   Columns  = 8;
        public int   Rows     = 8;
        public float CellSize = 1f;

        [Header("Gems")]
        public int     GemColorCount = 5;
        // Relative weight per colour (index matches GemColor enum).
        // Array length must equal GemColorCount for weighted mode;
        // leave empty (or wrong length) for uniform distribution.
        public float[] GemColorWeights;

        [Header("Tile Weights (relative)")]
        public float WeightFloor    = 60f;
        public float WeightWall     = 15f;
        public float WeightDoor     =  5f;
        public float WeightTreasure = 10f;
        public float WeightEnemy    = 10f;

        [Header("Enemies")]
        // Weighted pool of enemy types that can spawn on this floor.
        // Leave empty to use default Melee enemies.
        public EnemyConfig[] EnemyPool;

        [Header("Cell Overrides")]
        // Specific tile types forced at specific cells before procedural fill.
        public List<CellOverride>      TileOverrides;
        // Frozen gems placed after tile generation, before normal gem fill.
        public List<FrozenGemPlacement> FrozenGems;

        [Header("Animation")]
        public float GemFallSpeed       =  8f;
        public float FlashDuration      = 0.4f;
        public float SwapAnimDuration   = 0.15f;
        public float PlayerMoveDuration = 0.2f;

        [Header("Objective")]
        public LevelObjectiveType Objective = LevelObjectiveType.ReachDoor;

        // ScoreThreshold
        public int TargetScore;

        // KeystoneDelivery — gem colour that must land on GoalCell
        public GemColor   KeystoneColor;
        public Vector2Int GoalCell;
    }
}
