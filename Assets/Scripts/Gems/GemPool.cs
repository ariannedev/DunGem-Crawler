using System.Collections.Generic;
using UnityEngine;

namespace DunGemCrawler
{
    public class GemPool : MonoBehaviour
    {
        [SerializeField] private GemView gemPrefab;
        [SerializeField] private int prewarmCount = 80;

        // One controller per GemColor enum value: Red, Blue, Green, Yellow, Purple
        [SerializeField] private RuntimeAnimatorController[] gemControllers = new RuntimeAnimatorController[5];

        private Queue<GemView> _pool = new Queue<GemView>();
        private bool _initialized;

        private void Awake() => EnsureInitialized();

        public void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;
            for (int i = 0; i < prewarmCount; i++)
                _pool.Enqueue(CreateInstance());
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
            RuntimeAnimatorController controller = colorIndex < gemControllers.Length
                ? gemControllers[colorIndex]
                : null;

            view.Initialize(data, controller);
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
    }
}
