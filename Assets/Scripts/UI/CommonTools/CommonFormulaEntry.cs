using System;

namespace ElectricalSim.UI.CommonTools
{
    [Serializable]
    public sealed class CommonFormulaEntry
    {
        public string Title;
        public string Expression;
        public string Variables;
        public string UseCase;
        public string Example;
        public string Note;
        public string[] Aliases;
    }
}
