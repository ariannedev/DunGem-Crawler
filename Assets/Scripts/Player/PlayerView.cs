using System.Collections;
using UnityEngine;

namespace DunGemCrawler
{
    public class PlayerView : MonoBehaviour
    {
        public IEnumerator MoveTo(Vector2 target, float duration)
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
    }
}
