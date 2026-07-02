using System;

namespace ElectricalSim.UI.CommonTools
{
    [Serializable]
    public sealed class CommonArticleEntry
    {
        public string Title;
        public string Category;
        public string Content;
        public string[] Aliases;
    }
}
