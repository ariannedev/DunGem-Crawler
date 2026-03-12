namespace DunGemCrawler
{
    public enum GemColor { Red, Blue, Green, Yellow, Purple }

    public class GemData
    {
        public GemColor Color;
        public int Col;
        public int Row;
        public GemView View;

        public GemData(GemColor color, int col, int row)
        {
            Color = color;
            Col = col;
            Row = row;
        }
    }
}
