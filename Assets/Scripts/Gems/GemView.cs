using System.Collections;
using UnityEngine;

namespace DunGemCrawler
{
    public class GemView : MonoBehaviour
    {
        private SpriteRenderer _sr;
        public GemData Data { get; private set; }

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
        }

        public void Initialize(GemData data, Sprite sprite, Color color)
        {
            Data = data;
            _sr.sprite = sprite;
            _sr.color = color;
        }

        public void SetHighlight(bool on)
        {
            _sr.color = on
                ? Color.Lerp(_sr.color, Color.white, 0.4f)
                : GetBaseColor();
        }

        private Color GetBaseColor()
        {
            // Re-derive tint from data; GemPool stores colors by index
            return _sr.color; // pool re-sets color on Get(), highlight is cosmetic only
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
