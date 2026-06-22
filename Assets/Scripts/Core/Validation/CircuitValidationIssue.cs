using System.Collections.Generic;
using ElectricalSim.Core;

namespace ElectricalSim.Core.Validation
{
    public sealed class CircuitValidationIssue
    {
        public string RuleId;
        public CircuitValidationSeverity Severity;
        public CircuitValidationCategory Category;
        public string Title;
        public string Message;
        public CircuitComponent Component;
        public List<CircuitComponent> RelatedComponents = new List<CircuitComponent>();
        public List<string> RelatedTerminals = new List<string>();
    }
}
