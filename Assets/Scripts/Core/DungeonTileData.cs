namespace DunGemCrawler
{
    public enum TileType { Floor, Wall, Door, Treasure, Enemy, Goal }

    public class DungeonTileData
    {
        public TileType Type;
        public int Col;
        public int Row;
        public bool HasBeenRevealed;
        public DungeonTileView View;

        public DungeonTileData(TileType type, int col, int row)
        {
            Type = type;
            Col = col;
            Row = row;
        }
    }
}
