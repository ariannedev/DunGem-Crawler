using UnityEngine;

namespace DunGemCrawler
{
    public class PlayerController
    {
        public PlayerData Data { get; }
        public PlayerView View { get; }

        public PlayerController(PlayerData data, PlayerView view)
        {
            Data = data;
            View = view;
        }
    }
}
