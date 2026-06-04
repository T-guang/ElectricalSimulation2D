namespace ElectricalSim.Rules
{
    public sealed class CircuitIssue
    {
        public string code;
        public CircuitIssueSeverity severity;
        public string title;
        public string message;
        public string componentId;
        public string componentName;
        public string suggestion;
    }
}
