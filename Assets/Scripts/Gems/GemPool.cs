using System.Collections.Generic;
using UnityEngine;

namespace DunGemCrawler
{
    public class GemPool : MonoBehaviour
    {
        [SerializeField] private GemView gemPrefab;
        [SerializeField] private int prewarmCount = 80;

        private Queue<GemView> _pool = new Queue<GemView>();

        // Runtime-generated sprites and colors (one per GemColor)
        private Sprite[] _sprites;
        private Color[] _colors;

        private static readonly Color[] GemColors =
        {
            new Color(0.9f, 0.2f, 0.2f), // Red
            new Color(0.2f, 0.4f, 0.9f), // Blue
            new Color(0.2f, 0.8f, 0.3f), // Green
            new Color(0.95f, 0.85f, 0.1f), // Yellow
            new Color(0.7f, 0.2f, 0.9f), // Purple
        };

        private bool _initialized;

        private void Awake() => EnsureInitialized();

        public void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;
            BuildSprites();
            for (int i = 0; i < prewarmCount; i++)
                _pool.Enqueue(CreateInstance());
        }

        private void BuildSprites()
        {
            _sprites = new Sprite[GemColors.Length];
            _colors = GemColors;
            for (int i = 0; i < GemColors.Length; i++)
                _sprites[i] = MakeSprite(GemColors[i]);
        }

        private static Sprite MakeSprite(Color color)
        {
            const int size = 48;
            var tex = new Texture2D(size, size)
            {
                filterMode = FilterMode.Point
            };
            var pixels = new Color[size * size];

            float radius = size * 0.45f;
            float center = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - center;
                    float dy = y + 0.5f - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = 1f - Mathf.Clamp01((dist - (radius - 1f)) / 1f);
                    pixels[y * size + x] = new Color(color.r, color.g, color.b, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private GemView CreateInstance()
        {
            var go = Instantiate(gemPrefab, transform);
            go.gameObject.SetActive(false);
            return go;
        }

        public GemView Get(GemData data, Vector2 worldPos)
        {
            GemView view = _pool.Count > 0 ? _pool.Dequeue() : CreateInstance();
            view.gameObject.SetActive(true);
            view.transform.position = worldPos;
            int colorIndex = (int)data.Color;
            view.Initialize(data, _sprites[colorIndex], _colors[colorIndex]);
            data.View = view;
            return view;
        }

        public void Return(GemView view)
        {
            view.SetGemType(GemType.Normal); // clear special overlay
            view.SetFrozen(0);               // clear ice overlay
            view.gameObject.SetActive(false);
            _pool.Enqueue(view);
        }

        public Sprite GetSprite(GemColor color) => _sprites[(int)color];
        public Color GetColor(GemColor color) => _colors[(int)color];
    }
}
