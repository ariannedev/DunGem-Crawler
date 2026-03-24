using System.Collections;
using UnityEngine;

namespace DunGemCrawler
{
    public class DungeonTileView : MonoBehaviour
    {
        private SpriteRenderer _sr;
        private TileType _type;
        private bool _permanentlyVisible;

        // Tile type colors for prototyping
        private static readonly Color ColorFloor    = new Color(0.4f, 0.3f, 0.2f);
        private static readonly Color ColorWall     = new Color(0.2f, 0.2f, 0.25f);
        private static readonly Color ColorDoor     = new Color(0.8f, 0.6f, 0.1f);
        private static readonly Color ColorTreasure = new Color(1.0f, 0.9f, 0.0f);
        private static readonly Color ColorEnemy    = new Color(0.8f, 0.1f, 0.1f);
        private static readonly Color ColorGoal     = new Color(0.2f, 0.9f, 0.5f);

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _sr.color = new Color(1f, 1f, 1f, 0f);
        }

        public void SetType(TileType type)
        {
            _type = type;
            _sr.color = GetTypeColor(type, 0f);
        }

        public void SetPermanentlyVisible(bool visible)
        {
            _permanentlyVisible = visible;
            _sr.color = GetTypeColor(_type, visible ? 1f : 0f);
        }

        public IEnumerator Flash(float duration)
        {
            if (_permanentlyVisible) yield break;

            float fadeIn = duration * 0.3f;
            float hold   = duration * 0.4f;
            float fadeOut = duration * 0.3f;

            // Fade in
            yield return Fade(0f, 1f, fadeIn);
            // Hold
            yield return new WaitForSeconds(hold);
            // Fade out (unless marked permanent mid-flash)
            if (!_permanentlyVisible)
                yield return Fade(1f, 0f, fadeOut);
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            float t = 0f;
            Color baseColor = GetTypeColor(_type, 1f);
            while (t < duration)
            {
                t += Time.deltaTime;
                float a = Mathf.Lerp(from, to, t / duration);
                _sr.color = new Color(baseColor.r, baseColor.g, baseColor.b, a);
                yield return null;
            }
            _sr.color = new Color(baseColor.r, baseColor.g, baseColor.b, to);
        }

        private static Color GetTypeColor(TileType type, float alpha)
        {
            Color c = type switch
            {
                TileType.Floor    => ColorFloor,
                TileType.Wall     => ColorWall,
                TileType.Door     => ColorDoor,
                TileType.Treasure => ColorTreasure,
                TileType.Enemy    => ColorEnemy,
                TileType.Goal     => ColorGoal,
                _                 => Color.grey
            };
            return new Color(c.r, c.g, c.b, alpha);
        }
    }
}
