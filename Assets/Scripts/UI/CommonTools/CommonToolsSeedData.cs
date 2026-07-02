using System.Collections.Generic;
using UnityEngine;

namespace ElectricalSim.UI.CommonTools
{
    public static class CommonToolsSeedData
    {
        public static List<ResistorColorEntry> GetResistorColors()
        {
            return new List<ResistorColorEntry>
            {
                ColorEntry("黑", 0, 1d, null, new Color(0.02f, 0.02f, 0.02f)),
                ColorEntry("棕", 1, 10d, "±1%", new Color(0.42f, 0.22f, 0.10f)),
                ColorEntry("红", 2, 100d, "±2%", new Color(0.86f, 0.12f, 0.12f)),
                ColorEntry("橙", 3, 1000d, null, new Color(0.96f, 0.42f, 0.08f)),
                ColorEntry("黄", 4, 10000d, null, new Color(0.96f, 0.78f, 0.10f)),
                ColorEntry("绿", 5, 100000d, "±0.5%", new Color(0.10f, 0.58f, 0.24f)),
                ColorEntry("蓝", 6, 1000000d, "±0.25%", new Color(0.10f, 0.33f, 0.86f)),
                ColorEntry("紫", 7, 10000000d, "±0.1%", new Color(0.45f, 0.20f, 0.75f)),
                ColorEntry("灰", 8, 100000000d, "±0.05%", new Color(0.50f, 0.55f, 0.62f)),
                ColorEntry("白", 9, 1000000000d, null, Color.white),
                ColorEntry("金", null, 0.1d, "±5%", new Color(0.95f, 0.66f, 0.18f)),
                ColorEntry("银", null, 0.01d, "±10%", new Color(0.72f, 0.76f, 0.80f))
            };
        }

        public static List<CommonFormulaEntry> GetFormulas()
        {
            return CommonToolsTeachingContent.GetFormulas();
        }

        public static List<CommonArticleEntry> GetArticles()
        {
            return CommonToolsTeachingContent.GetArticles();
        }

        private static ResistorColorEntry ColorEntry(string name, int? digit, double? multiplier, string tolerance, Color color)
        {
            return new ResistorColorEntry
            {
                Name = name,
                Digit = digit,
                Multiplier = multiplier,
                Tolerance = tolerance,
                Color = color
            };
        }

        private static CommonFormulaEntry Formula(string title, string category, string[] expressions, string[] variants, string variables, string unitDescription, string useCase, string example, string commonMistakes, string note, params string[] aliases)
        {
            return new CommonFormulaEntry
            {
                Title = title,
                Category = category,
                Expression = expressions != null && expressions.Length > 0 ? expressions[0] : string.Empty,
                Expressions = expressions != null ? new List<string>(expressions) : new List<string>(),
                Variants = variants != null ? new List<string>(variants) : new List<string>(),
                Variables = variables,
                UnitDescription = unitDescription,
                UseCase = useCase,
                Example = example,
                CommonMistakes = commonMistakes,
                Note = note,
                Aliases = aliases
            };
        }

        private static CommonArticleEntry Article(string title, string category, string learningGoal, string keyPoints, string commonMistakes, string relationToSystem, ArticleSection[] sections, params string[] aliases)
        {
            return new CommonArticleEntry
            {
                Title = title,
                Category = category,
                LearningGoal = learningGoal,
                Content = sections != null && sections.Length > 0 ? sections[0].Body : string.Empty,
                Sections = sections != null ? new List<ArticleSection>(sections) : new List<ArticleSection>(),
                KeyPoints = keyPoints,
                CommonMistakes = commonMistakes,
                RelationToSystem = relationToSystem,
                Aliases = aliases
            };
        }

        private static ArticleSection Section(string heading, string body)
        {
            return new ArticleSection
            {
                Heading = heading,
                Body = body
            };
        }
    }
}
