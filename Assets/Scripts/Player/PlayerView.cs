using System.Collections;
using UnityEngine;

namespace DunGemCrawler
{
    public class PlayerView : MonoBehaviour
    {
        private Animator       _animator;
        private SpriteRenderer _sr;

        private static readonly int IsWalking = Animator.StringToHash("IsWalking");

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _sr       = GetComponent<SpriteRenderer>();
        }

        public IEnumerator MoveTo(Vector2 target, float duration)
        {
            _animator?.SetBool(IsWalking, true);

            // Flip to face direction of travel
            if (_sr != null)
            {
                float dx = target.x - ((Vector2)transform.position).x;
                if (Mathf.Abs(dx) > 0.01f)
                    _sr.flipX = dx < 0;
            }

            Vector2 start = transform.position;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                transform.position = Vector2.Lerp(start, target, t / duration);
                yield return null;
            }
            transform.position = target;

            _animator?.SetBool(IsWalking, false);
        }
    }
}
