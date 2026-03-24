using System.Collections;
using UnityEngine;

namespace DunGemCrawler
{
    public class EnemyView : MonoBehaviour
    {
        private Animator       _animator;
        private SpriteRenderer _body;
        private SpriteRenderer _hpFill;
        private Transform      _hpFillTransform;

        private static readonly int AttackTrigger = Animator.StringToHash("Attack");

        private static Sprite _bodySprite;
        private static Sprite _barSprite;

        private void Awake()
        {
            _animator = GetComponent<Animator>();

            // ── Body ─────────────────────────────────────────────────────────────────
            // Reuse a "Body" child if the prefab has one (e.g. with a SpriteRenderer
            // already set up for the Animator). Otherwise create one procedurally.
            var bodyT = transform.Find("Body");
            if (bodyT != null)
            {
                _body = bodyT.GetComponent<SpriteRenderer>();
            }
            else
            {
                var bodyGO = new GameObject("Body");
                bodyGO.transform.SetParent(transform, false);
                _body = bodyGO.AddComponent<SpriteRenderer>();
                _body.sortingLayerName = "Player";
                _body.sortingOrder     = 0;

                EnsureFallbackSprites();
                _body.sprite = _bodySprite;
            }

            // ── HP bar ────────────────────────────────────────────────────────────────
            EnsureFallbackSprites();

            var bgGO = new GameObject("HPBarBg");
            bgGO.transform.SetParent(transform, false);
            bgGO.transform.localPosition = new Vector3(0f, -0.42f, 0f);
            var bgSr = bgGO.AddComponent<SpriteRenderer>();
            bgSr.sortingLayerName    = "Player";
            bgSr.sortingOrder        = 1;
            bgSr.color               = new Color(0.2f, 0.2f, 0.2f);
            bgSr.sprite              = _barSprite;
            bgSr.transform.localScale = new Vector3(0.7f, 0.1f, 1f);

            var fillGO = new GameObject("HPBarFill");
            fillGO.transform.SetParent(transform, false);
            fillGO.transform.localPosition = new Vector3(0f, -0.42f, 0f);
            _hpFill = fillGO.AddComponent<SpriteRenderer>();
            _hpFill.sortingLayerName   = "Player";
            _hpFill.sortingOrder       = 2;
            _hpFill.color              = new Color(0.2f, 0.85f, 0.2f);
            _hpFill.sprite             = _barSprite;
            _hpFillTransform           = fillGO.transform;
            _hpFillTransform.localScale = new Vector3(0.7f, 0.1f, 1f);
        }

        // ── Animation ─────────────────────────────────────────────────────────────

        public IEnumerator MoveTo(Vector2 target, float speed = 5f)
        {
            while ((Vector2)transform.position != target)
            {
                transform.position = Vector2.MoveTowards(
                    transform.position, target, speed * Time.deltaTime);
                yield return null;
            }
        }

        public void PlayAttack()
        {
            _animator?.SetTrigger(AttackTrigger);
        }

        // ── HP / damage ───────────────────────────────────────────────────────────

        public void SetHealth(int current, int max)
        {
            float ratio = max > 0 ? (float)current / max : 0f;
            _hpFillTransform.localScale    = new Vector3(0.7f * ratio, 0.1f, 1f);
            _hpFillTransform.localPosition = new Vector3(-0.35f * (1f - ratio), -0.42f, 0f);
            _hpFill.color = Color.Lerp(new Color(0.9f, 0.2f, 0.1f), new Color(0.2f, 0.85f, 0.2f), ratio);
        }

        public void FlashDamage()
        {
            if (gameObject.activeSelf)
                StartCoroutine(DamageFlash());
        }

        private IEnumerator DamageFlash()
        {
            _body.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            _body.color = Color.red;
        }

        // ── Procedural fallback sprites ───────────────────────────────────────────

        private static void EnsureFallbackSprites()
        {
            if (_bodySprite == null) _bodySprite = MakeDiamondSprite();
            if (_barSprite  == null) _barSprite  = MakePixelSprite();
        }

        private static Sprite MakeDiamondSprite()
        {
            const int size = 40;
            var tex    = new Texture2D(size, size) { filterMode = FilterMode.Point };
            var pixels = new Color[size * size];
            float half = size * 0.5f;
            var fill   = new Color(0.85f, 0.1f, 0.1f);

            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx    = Mathf.Abs(x + 0.5f - half) / (half - 1f);
                    float dy    = Mathf.Abs(y + 0.5f - half) / (half - 1f);
                    float alpha = (dx + dy <= 1f) ? 1f : 0f;
                    pixels[y * size + x] = new Color(fill.r, fill.g, fill.b, alpha);
                }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static Sprite MakePixelSprite()
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
        }
    }
}
