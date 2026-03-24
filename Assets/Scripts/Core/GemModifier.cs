using System;

namespace DunGemCrawler
{
    public enum GemModifierType { Frozen }

    [Serializable]
    public class GemModifier
    {
        public GemModifierType Type;
        public int IceLayers;

        public GemModifier(GemModifierType type, int iceLayers)
        {
            Type      = type;
            IceLayers = iceLayers;
        }
    }
}
