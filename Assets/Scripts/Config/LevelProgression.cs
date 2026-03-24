using System.Collections.Generic;
using UnityEngine;

namespace DunGemCrawler
{
    [CreateAssetMenu(fileName = "LevelProgression", menuName = "DunGemCrawler/Level Progression")]
    public class LevelProgression : ScriptableObject
    {
        public List<LevelConfig> Levels;

        // Fallback when FloorNumber exceeds the Levels list.
        // If null, the last entry in Levels is repeated.
        public LevelConfig DefaultConfig;

        public LevelConfig Get(int floorNumber)
        {
            int idx = floorNumber - 1;
            if (Levels != null && idx >= 0 && idx < Levels.Count)
                return Levels[idx];
            if (DefaultConfig != null)
                return DefaultConfig;
            if (Levels != null && Levels.Count > 0)
                return Levels[Levels.Count - 1];
            return null;
        }
    }
}
