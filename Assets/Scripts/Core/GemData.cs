namespace DunGemCrawler
{
    public enum GemColor { Red, Blue, Green, Yellow, Purple }
    public enum GemType  { Normal, LineClear, ColorBomb }

    public class GemData
    {
        public GemColor   Color;
        public GemType    Type = GemType.Normal;
        public bool       LineClearHorizontal = true; // true = row, false = column
        public GemModifier Modifier;                  // null = no modifier
        public int Col;
        public int Row;
        public GemView View;

        public GemData(GemColor color, int col, int row)
        {
            Color = color;
            Col   = col;
            Row   = row;
        }
    }
}
