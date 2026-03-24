using UnityEngine;

namespace DunGemCrawler
{
    public enum EnemyType { Melee, Mixer, Freezer, Charger }

    [CreateAssetMenu(fileName = "EnemyConfig", menuName = "DunGemCrawler/Enemy Config")]
    public class EnemyConfig : ScriptableObject
    {
        [Header("Identity")]
        public EnemyType Type         = EnemyType.Melee;
        public int       MaxHealth    = 3;
        public int       AttackDamage = 1;
        public float     SpawnWeight  = 1f;

        [Header("Prefab")]
        // Leave null to use the procedural fallback body.
        public EnemyView Prefab;

        [Header("Mixer Settings")]
        public int MixSwapCount = 3;   // gem pairs swapped per turn

        [Header("Freezer Settings")]
        public int FreezeCount     = 1; // gems frozen per turn
        public int FreezeIceLayers = 1; // ice layers applied to each

        // Charger has no extra config — it always moves one cell/turn toward the player.
    }
}
