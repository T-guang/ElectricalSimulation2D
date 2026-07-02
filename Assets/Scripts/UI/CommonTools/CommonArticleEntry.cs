using System;
using System.Collections.Generic;

namespace ElectricalSim.UI.CommonTools
{
    [Serializable]
    public sealed class CommonArticleEntry
    {
        public string Id;
        public string Title;
        public string Category;
        public string LearningGoal;
        public string Content;
        public List<ArticleSection> Sections = new List<ArticleSection>();
        public string KeyPoints;
        public string CommonMistakes;
        public string RelationToSystem;
        public string[] Aliases;
    }

    [Serializable]
    public sealed class ArticleSection
    {
        public string Heading;
        public string Body;
    }
}
