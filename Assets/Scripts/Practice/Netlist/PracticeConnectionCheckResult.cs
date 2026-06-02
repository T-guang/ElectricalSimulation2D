using System.Collections.Generic;

namespace ElectricalSim.Practice.Netlist
{
    public sealed class PracticeConnectionCheckResult
    {
        public bool Passed { get; set; }
        public bool AmbiguousMapping { get; set; }

        public List<PracticeConnectionIssue> MissingComponents { get; } = new List<PracticeConnectionIssue>();
        public List<PracticeConnectionIssue> ExtraComponents { get; } = new List<PracticeConnectionIssue>();
        public List<PracticeConnectionIssue> MissingConnections { get; } = new List<PracticeConnectionIssue>();
        public List<PracticeConnectionIssue> WrongConnections { get; } = new List<PracticeConnectionIssue>();
        public List<PracticeConnectionIssue> ExtraConnections { get; } = new List<PracticeConnectionIssue>();
        public List<PracticeConnectionIssue> CorrectConnectionSuggestions { get; } = new List<PracticeConnectionIssue>();

        public bool HasIssues =>
            MissingComponents.Count > 0 ||
            ExtraComponents.Count > 0 ||
            MissingConnections.Count > 0 ||
            WrongConnections.Count > 0 ||
            ExtraConnections.Count > 0 ||
            AmbiguousMapping;
    }
}
