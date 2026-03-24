using UnityEngine;

namespace DunGemCrawler
{
    public class GameUI : MonoBehaviour
    {
        public int Health = 10;
        public int MaxHealth = 10;
        public int Floor = 1;
        public int Score = 0;

        private GUIStyle _style;

        private void OnGUI()
        {
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label);
                _style.fontSize = 22;
                _style.normal.textColor = Color.white;
            }

            GUI.Label(new Rect(10, 10, 250, 30), $"Floor  {Floor}", _style);
            GUI.Label(new Rect(10, 40, 250, 30), $"HP  {Health} / {MaxHealth}", _style);
            GUI.Label(new Rect(10, 70, 250, 30), $"Score  {Score}", _style);
        }

        public void UpdateHealth(int current, int max)
        {
            Health = current;
            MaxHealth = max;
        }

        public void UpdateFloor(int floor) => Floor = floor;
        public void UpdateScore(int score) => Score = score;
    }
}
