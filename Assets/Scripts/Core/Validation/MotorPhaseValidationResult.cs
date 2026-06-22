using System.Collections.Generic;
using ElectricalSim.Core;

namespace ElectricalSim.Core.Validation
{
    public sealed class MotorPhaseValidationResult
    {
        public CircuitComponent Motor;
        public bool ShouldEvaluate;
        public bool HasMissingPhase;
        public bool HasDuplicatePhase;
        public readonly Dictionary<string, string> TerminalPhaseMap = new Dictionary<string, string>();
        public readonly List<string> MissingTerminals = new List<string>();
        public readonly List<string> DuplicatePhases = new List<string>();
        public string SourceDescription;
    }
}
