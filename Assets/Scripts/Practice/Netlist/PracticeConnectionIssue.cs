namespace ElectricalSim.Practice.Netlist
{
    public enum PracticeConnectionIssueKind
    {
        MissingComponent,
        ExtraComponent,
        MissingConnection,
        WrongConnection,
        ExtraConnection,
        Suggestion,
        AmbiguousMapping
    }

    public sealed class PracticeConnectionIssue
    {
        public PracticeConnectionIssueKind Kind { get; }
        public string Message { get; }

        public PracticeConnectionIssue(PracticeConnectionIssueKind kind, string message)
        {
            Kind = kind;
            Message = message;
        }
    }
}
