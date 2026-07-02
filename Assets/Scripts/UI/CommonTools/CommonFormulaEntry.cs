using System;
using System.Collections.Generic;

namespace ElectricalSim.UI.CommonTools
{
    [Serializable]
    public sealed class CommonFormulaEntry
    {
        public string Title;
        public string Category;
        public string Expression;
        public List<string> Expressions = new List<string>();
        public List<string> Variants = new List<string>();
        public string Variables;
        public string UnitDescription;
        public string UseCase;
        public string Example;
        public string CommonMistakes;
        public string Note;
        public string[] Aliases;
    }
}
