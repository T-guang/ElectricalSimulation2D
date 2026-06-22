using System.Collections.Generic;

namespace ElectricalSim.Core.Validation
{
    public sealed class CircuitValidationReport
    {
        public readonly List<CircuitValidationIssue> Issues = new List<CircuitValidationIssue>();

        public bool HasErrors
        {
            get
            {
                return HasSeverity(CircuitValidationSeverity.Error);
            }
        }

        public bool HasWarnings
        {
            get
            {
                return HasSeverity(CircuitValidationSeverity.Warning);
            }
        }

        private bool HasSeverity(CircuitValidationSeverity severity)
        {
            for (var i = 0; i < Issues.Count; i++)
            {
                if (Issues[i] != null && Issues[i].Severity == severity)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
