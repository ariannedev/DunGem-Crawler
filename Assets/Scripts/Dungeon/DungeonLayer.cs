using System;
using UnityEngine;

namespace DunGemCrawler
{
    public class DungeonLayer
    {
        private readonly BoardData _data;
        private readonly float _flashDuration;
        private readonly MonoBehaviour _host;

        // Fires after a tile is revealed: (tileType, col, row)
        public event Action<TileType, int, int> OnTileRevealed;

        public DungeonLayer(BoardData data, float flashDuration, MonoBehaviour host)
        {
            _data = data;
            _flashDuration = flashDuration;
            _host = host;
        }

        public void RevealCell(int col, int row)
        {
            var tile = _data.GetTile(col, row);
            if (tile == null || tile.View == null) return;

            tile.HasBeenRevealed = true;

            if (tile.Type == TileType.Door)
                tile.View.SetPermanentlyVisible(true);
            else
                _host.StartCoroutine(tile.View.Flash(_flashDuration));

            OnTileRevealed?.Invoke(tile.Type, col, row);
        }

        public void RevealCell(Vector2Int cell) => RevealCell(cell.x, cell.y);

        public TileType GetType(int col, int row) => _data.GetTile(col, row)?.Type ?? TileType.Floor;
    }
}
