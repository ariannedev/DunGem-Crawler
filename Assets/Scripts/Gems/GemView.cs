using System.Collections;
using UnityEngine;

namespace DunGemCrawler
{
    public class GemView : MonoBehaviour
    {
        private SpriteRenderer _sr;
        private Color _baseColor;
        public GemData Data { get; private set; }

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
        }

        public void Initialize(GemData data, Sprite sprite, Color color)
        {
            Data = data;
            _baseColor = color;
            _sr.sprite = sprite;
            _sr.color = color;
        }

        public void SetHighlight(bool on)
        {
            _sr.color = on ? Color.Lerp(_baseColor, Color.white, 0.45f) : _baseColor;
        }

        // ── Special gem overlay ───────────────────────────────────────────────────

        private SpriteRenderer _overlay;

        private static Sprite _stripeH;
        private static Sprite _stripeV;
        private static Sprite _starSprite;

        public void SetGemType(GemType type, bool horizontal = true)
        {
            if (type == GemType.Normal)
            {
                if (_overlay != null)
                    _overlay.gameObject.SetActive(false);
                return;
            }

            EnsureOverlaySprites();

            if (_overlay == null)
            {
                var go = new GameObject("Overlay");
                go.transform.SetParent(transform, false);
                _overlay = go.AddComponent<SpriteRenderer>();
                _overlay.sortingLayerName = _sr.sortingLayerName;
                _overlay.sortingOrder    = _sr.sortingOrder + 1;
            }

            switch (type)
            {
                case GemType.LineClear:
                    _overlay.gameObject.SetActive(true);
                    _overlay.sprite = horizontal ? _stripeH : _stripeV;
                    _overlay.color  = new Color(1f, 1f, 1f, 0.85f);
                    break;
                case GemType.ColorBomb:
                    _overlay.gameObject.SetActive(true);
                    _overlay.sprite = _starSprite;
                    _overlay.color  = new Color(1f, 0.92f, 0.2f, 0.9f);
                    break;
            }
        }

        private static void EnsureOverlaySprites()
        {
            if (_stripeH == null) _stripeH = MakeStripe(true);
            if (_stripeV == null) _stripeV = MakeStripe(false);
            if (_starSprite == null) _starSprite = MakeStar();
        }

        private static Sprite MakeStripe(bool horizontal)
        {
            const int size = 48;
            var tex = new Texture2D(size, size) { filterMode = FilterMode.Point };
            var pixels = new Color[size * size];
            int lo = size / 2 - 3, hi = size / 2 + 3;

            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    bool inStripe = horizontal ? (y >= lo && y <= hi) : (x >= lo && x <= hi);
                    pixels[y * size + x] = inStripe
                        ? new Color(1f, 1f, 1f, 0.9f)
                        : new Color(0, 0, 0, 0);
                }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static Sprite MakeStar()
        {
            const int size = 48;
            var tex = new Texture2D(size, size) { filterMode = FilterMode.Point };
            var pixels = new Color[size * size];
            float c = size * 0.5f;
            float outerR = size * 0.42f, innerR = size * 0.18f;
            const int points = 5;

            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - c, dy = y + 0.5f - c;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float angle = Mathf.Atan2(dy, dx);
                    // Star: sample at this angle between inner/outer radius
                    float sectorAngle = (2f * Mathf.PI) / points;
                    float a = angle % sectorAngle;
                    if (a < 0) a += sectorAngle;
                    float t = a / sectorAngle;
                    float starR = Mathf.Lerp(outerR, innerR, Mathf.Abs(t - 0.5f) * 2f);
                    pixels[y * size + x] = dist <= starR
                        ? new Color(1f, 1f, 1f, 0.95f)
                        : new Color(0, 0, 0, 0);
                }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        // ── Frozen gem overlay ────────────────────────────────────────────────────

        private SpriteRenderer _iceOverlay;
        private static Sprite  _iceSprite;

        // layers == 0 hides the overlay; layers > 0 shows it with opacity scaled by layers.
        public void SetFrozen(int layers)
        {
            if (layers <= 0)
            {
                if (_iceOverlay != null)
                    _iceOverlay.gameObject.SetActive(false);
                return;
            }

            if (_iceOverlay == null)
            {
                var go = new GameObject("IceOverlay");
                go.transform.SetParent(transform, false);
                _iceOverlay = go.AddComponent<SpriteRenderer>();
                _iceOverlay.sortingLayerName = _sr.sortingLayerName;
                _iceOverlay.sortingOrder     = _sr.sortingOrder + 2;
            }

            EnsureIceSprite();
            _iceOverlay.sprite = _iceSprite;
            float opacity = Mathf.Clamp01(0.55f + (layers - 1) * 0.15f);
            _iceOverlay.color = new Color(0.6f, 0.88f, 1f, opacity);
            _iceOverlay.gameObject.SetActive(true);
        }

        private static void EnsureIceSprite()
        {
            if (_iceSprite == null) _iceSprite = MakeIceSprite();
        }

        private static Sprite MakeIceSprite()
        {
            const int size = 48;
            var tex    = new Texture2D(size, size) { filterMode = FilterMode.Point };
            var pixels = new Color[size * size];
            float c = size * 0.5f;
            float r = size * 0.44f;

            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx   = x + 0.5f - c;
                    float dy   = y + 0.5f - c;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    bool inRing  = dist >= r * 0.72f && dist <= r;
                    bool inCross = (Mathf.Abs(dx) < 2.5f || Mathf.Abs(dy) < 2.5f) && dist < r;

                    pixels[y * size + x] = (inRing || inCross)
                        ? new Color(0.85f, 0.97f, 1f, 0.92f)
                        : new Color(0.65f, 0.88f, 1f, 0.30f);
                }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        public IEnumerator SwapTo(Vector2 target, float duration)
        {
            Vector2 start = transform.position;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                transform.position = Vector2.Lerp(start, target, t / duration);
                yield return null;
            }
            transform.position = target;
        }

        public IEnumerator MoveTo(Vector2 target, float speed)
        {
            while ((Vector2)transform.position != target)
            {
                transform.position = Vector2.MoveTowards(
                    transform.position, target, speed * Time.deltaTime);
                yield return null;
            }
        }
    }
}
