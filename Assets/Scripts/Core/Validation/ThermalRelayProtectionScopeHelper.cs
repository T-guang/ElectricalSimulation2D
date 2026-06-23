using System;
using System.Collections.Generic;
using ElectricalSim.Core;

namespace ElectricalSim.Core.Validation
{
    public sealed class ThermalRelayProtectionScope
    {
        public ThermalRelayProtectionScope(
            CircuitComponent thermalRelay,
            CircuitComponent upstreamContactor,
            CircuitComponent protectedMotor,
            bool isReliable,
            string reason)
        {
            ThermalRelay = thermalRelay;
            UpstreamContactor = upstreamContactor;
            ProtectedMotor = protectedMotor;
            IsReliable = isReliable;
            Reason = reason ?? string.Empty;
        }

        public CircuitComponent ThermalRelay { get; }
        public CircuitComponent UpstreamContactor { get; }
        public CircuitComponent ProtectedMotor { get; }
        public bool IsReliable { get; }
        public string Reason { get; }
    }

    public sealed class ThermalRelayProtectionScopeHelper
    {
        private readonly Dictionary<TerminalView, HashSet<TerminalView>> staticWireGraph =
            new Dictionary<TerminalView, HashSet<TerminalView>>();

        public bool TryResolveProtectionScope(
            CircuitComponent relay,
            IReadOnlyList<CircuitComponent> components,
            IReadOnlyList<WireView> wires,
            out ThermalRelayProtectionScope scope)
        {
            BuildStaticMainCircuitGraph(components, wires);

            if (!IsSupportedThermalRelay(relay))
            {
                scope = CreateScope(relay, null, null, false, "Unsupported thermal relay.");
                return false;
            }

            if (!TryFindUniqueUpstreamContactor(relay, components, out var upstreamContactor, out var contactorReason))
            {
                scope = CreateScope(relay, null, null, false, contactorReason);
                return false;
            }

            if (!TryFindUniqueProtectedMotor(relay, components, out var protectedMotor, out var motorReason))
            {
                scope = CreateScope(relay, upstreamContactor, null, false, motorReason);
                return false;
            }

            scope = CreateScope(relay, upstreamContactor, protectedMotor, true, "Reliable thermal relay protection scope.");
            return true;
        }

        private bool TryFindUniqueUpstreamContactor(
            CircuitComponent relay,
            IReadOnlyList<CircuitComponent> components,
            out CircuitComponent contactor,
            out string reason)
        {
            contactor = null;
            var matchCount = 0;

            if (components != null)
            {
                for (var i = 0; i < components.Count; i++)
                {
                    var candidate = components[i];
                    if (!IsSupportedContactor(candidate) ||
                        !AreContactorOutputsConnectedToRelayInputs(candidate, relay))
                    {
                        continue;
                    }

                    matchCount++;
                    contactor = candidate;
                }
            }

            if (matchCount == 1)
            {
                reason = string.Empty;
                return true;
            }

            reason = matchCount == 0
                ? "No unique upstream contactor found for thermal relay."
                : "Multiple upstream contactors found for thermal relay.";
            contactor = null;
            return false;
        }

        private bool TryFindUniqueProtectedMotor(
            CircuitComponent relay,
            IReadOnlyList<CircuitComponent> components,
            out CircuitComponent motor,
            out string reason)
        {
            motor = null;
            var matchCount = 0;

            if (components != null)
            {
                for (var i = 0; i < components.Count; i++)
                {
                    var candidate = components[i];
                    if (!IsSupportedThreePhaseMotor(candidate) ||
                        !AreRelayOutputsConnectedToMotor(relay, candidate))
                    {
                        continue;
                    }

                    matchCount++;
                    motor = candidate;
                }
            }

            if (matchCount == 1)
            {
                reason = string.Empty;
                return true;
            }

            reason = matchCount == 0
                ? "No unique protected motor found for thermal relay."
                : "Multiple protected motors found for thermal relay.";
            motor = null;
            return false;
        }

        private bool AreContactorOutputsConnectedToRelayInputs(
            CircuitComponent contactor,
            CircuitComponent relay)
        {
            return AreConnectedByStaticWires(contactor.GetTerminal(TerminalConstants.T1), relay.GetTerminal(TerminalConstants.L1)) &&
                AreConnectedByStaticWires(contactor.GetTerminal(TerminalConstants.T2), relay.GetTerminal(TerminalConstants.L2)) &&
                AreConnectedByStaticWires(contactor.GetTerminal(TerminalConstants.T3), relay.GetTerminal(TerminalConstants.L3));
        }

        private bool AreRelayOutputsConnectedToMotor(
            CircuitComponent relay,
            CircuitComponent motor)
        {
            return AreConnectedByStaticWires(relay.GetTerminal(TerminalConstants.T1), motor.GetTerminal(TerminalConstants.U)) &&
                AreConnectedByStaticWires(relay.GetTerminal(TerminalConstants.T2), motor.GetTerminal(TerminalConstants.V)) &&
                AreConnectedByStaticWires(relay.GetTerminal(TerminalConstants.T3), motor.GetTerminal(TerminalConstants.W));
        }

        private void BuildStaticMainCircuitGraph(
            IReadOnlyList<CircuitComponent> components,
            IReadOnlyList<WireView> wires)
        {
            staticWireGraph.Clear();

            if (components != null)
            {
                for (var i = 0; i < components.Count; i++)
                {
                    var component = components[i];
                    if (component == null)
                    {
                        continue;
                    }

                    foreach (var terminal in component.Terminals)
                    {
                        EnsureStatic(terminal);
                    }
                }
            }

            if (wires == null)
            {
                return;
            }

            for (var i = 0; i < wires.Count; i++)
            {
                var wire = wires[i];
                if (wire == null)
                {
                    continue;
                }

                ConnectStatic(wire.StartTerminal, wire.EndTerminal);
            }
        }

        private bool AreConnectedByStaticWires(TerminalView first, TerminalView second)
        {
            if (first == null || second == null)
            {
                return false;
            }

            if (first == second)
            {
                return true;
            }

            var visited = new HashSet<TerminalView>();
            var queue = new Queue<TerminalView>();
            visited.Add(first);
            queue.Enqueue(first);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (!staticWireGraph.TryGetValue(current, out var next))
                {
                    continue;
                }

                foreach (var terminal in next)
                {
                    if (terminal == second)
                    {
                        return true;
                    }

                    if (visited.Add(terminal))
                    {
                        queue.Enqueue(terminal);
                    }
                }
            }

            return false;
        }

        private void EnsureStatic(TerminalView terminal)
        {
            if (terminal != null && !staticWireGraph.ContainsKey(terminal))
            {
                staticWireGraph[terminal] = new HashSet<TerminalView>();
            }
        }

        private void ConnectStatic(TerminalView first, TerminalView second)
        {
            if (first == null || second == null)
            {
                return;
            }

            EnsureStatic(first);
            EnsureStatic(second);
            staticWireGraph[first].Add(second);
            staticWireGraph[second].Add(first);
        }

        private static bool IsSupportedThermalRelay(CircuitComponent component)
        {
            if (component == null || component.Definition == null)
            {
                return false;
            }

            if (string.Equals(component.Definition.name, "ThermalRelay_FR_380V", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return component.GetTerminal(TerminalConstants.L1) != null &&
                component.GetTerminal(TerminalConstants.T1) != null &&
                component.GetTerminal(TerminalConstants.L2) != null &&
                component.GetTerminal(TerminalConstants.T2) != null &&
                component.GetTerminal(TerminalConstants.L3) != null &&
                component.GetTerminal(TerminalConstants.T3) != null &&
                component.GetTerminal(TerminalConstants.ThermalNC95) != null &&
                component.GetTerminal(TerminalConstants.ThermalNC96) != null &&
                component.GetTerminal(TerminalConstants.ThermalNO97) != null &&
                component.GetTerminal(TerminalConstants.ThermalNO98) != null;
        }

        private static bool IsSupportedContactor(CircuitComponent component)
        {
            if (component == null || component.Definition == null)
            {
                return false;
            }

            var definitionName = component.Definition.name ?? string.Empty;
            return string.Equals(definitionName, "Contactor_KM_380V", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(definitionName, "Contactor_KM_220V", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsSupportedThreePhaseMotor(CircuitComponent component)
        {
            return component != null &&
                component.Definition != null &&
                string.Equals(component.Definition.name, "Motor_ThreePhase_380V", StringComparison.OrdinalIgnoreCase) &&
                component.GetTerminal(TerminalConstants.U) != null &&
                component.GetTerminal(TerminalConstants.V) != null &&
                component.GetTerminal(TerminalConstants.W) != null;
        }

        private static ThermalRelayProtectionScope CreateScope(
            CircuitComponent relay,
            CircuitComponent upstreamContactor,
            CircuitComponent protectedMotor,
            bool reliable,
            string reason)
        {
            return new ThermalRelayProtectionScope(relay, upstreamContactor, protectedMotor, reliable, reason);
        }
    }
}
