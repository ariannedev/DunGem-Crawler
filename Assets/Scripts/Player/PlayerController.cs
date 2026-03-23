using System;
using UnityEngine;

namespace DunGemCrawler
{
    public class PlayerController
    {
        public PlayerData Data { get; }
        public PlayerView View { get; }

        public event Action OnDeath;

        public PlayerController(PlayerData data, PlayerView view)
        {
            Data = data;
            View = view;
        }

        public void TakeDamage(int amount)
        {
            Data.Health = Mathf.Max(0, Data.Health - amount);
            if (Data.Health == 0)
                OnDeath?.Invoke();
        }

        public void Heal(int amount)
        {
            Data.Health = Mathf.Min(Data.MaxHealth, Data.Health + amount);
        }
    }
}
