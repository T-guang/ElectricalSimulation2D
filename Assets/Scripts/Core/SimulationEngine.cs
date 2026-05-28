using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ElectricalSim.Core
{
    public sealed class SimulationEngine
    {
        private readonly List<CircuitComponent> components;
        private readonly IReadOnlyList<WireView> wires;
        private readonly Dictionary<TerminalView, List<TerminalView>> graph = new Dictionary<TerminalView, List<TerminalView>>();
        private readonly HashSet<CircuitComponent> closedContactors = new HashSet<CircuitComponent>();

        public SimulationEngine(List<CircuitComponent> components, IReadOnlyList<WireView> wires)
        {
            this.components = components;
            this.wires = wires;
        }

        public string Run()
        {
            BuildGraph();

            var phaseRoots = GetPhaseRoots();
            var neutralRoots = GetNeutralRoots();

            var powered = Flood(phaseRoots);
            var neutral = Flood(neutralRoots);
            UpdateClosedContactors(powered, neutral);
            if (closedContactors.Count > 0)
            {
                BuildGraph();
                powered = Flood(phaseRoots);
                neutral = Flood(neutralRoots);
            }

            var shorted = powered.Overlaps(neutral);
            var energizedCount = 0;

            foreach (var component in components)
            {
                var energized = IsLoadEnergized(component, powered, neutral);
                var active = !shorted && energized;
                component.SetEnergized(active);
                ApplyMeasurement(component, active);
                if (active)
                {
                    energizedCount++;
                }
            }

            if (phaseRoots.Count == 0 || neutralRoots.Count == 0)
            {
                return "缺少电源，请先放置 220V 电源。";
            }

            if (shorted)
            {
                return "检测到短路：火线与零线直接连通。";
            }

            return energizedCount > 0 ? $"仿真完成：{energizedCount} 个负载/线圈已动作。" : "线路未形成完整回路。";
        }

        private static void ApplyMeasurement(CircuitComponent component, bool active)
        {
            if (component == null)
            {
                return;
            }

            var definition = component.Definition;
            if (!active || definition == null)
            {
                component.ClearMeasurement();
                return;
            }

            var voltage = ResolveVoltage(component, definition);
            var power = ResolvePower(component, definition);
            var current = ResolveCurrent(component, definition);

            if (voltage > 0f && power > 0f)
            {
                current = power / voltage;
            }

            if (power <= 0f && voltage > 0f && current > 0f)
            {
                power = voltage * current;
            }

            component.SetMeasurement(voltage, current, power);
        }

        private static float ResolveVoltage(CircuitComponent component, ComponentDefinition definition)
        {
            if (TryGetParameterValue(component, "ratedVoltage", out var value))
            {
                return value;
            }

            if (TryGetParameterValue(component, "voltage", out value))
            {
                return value;
            }

            return definition.ratedVoltage > 0f ? definition.ratedVoltage : definition.sourceVoltage;
        }

        private static float ResolvePower(CircuitComponent component, ComponentDefinition definition)
        {
            if (TryGetParameterValue(component, "power", out var value))
            {
                return value;
            }

            if (TryGetParameterValue(component, "ratedPower", out value))
            {
                return value;
            }

            return definition.ratedPower;
        }

        private static float ResolveCurrent(CircuitComponent component, ComponentDefinition definition)
        {
            if (TryGetParameterValue(component, "current", out var value))
            {
                return value;
            }

            if (TryGetParameterValue(component, "ratedCurrent", out value))
            {
                return value;
            }

            return definition.ratedCurrent;
        }

        private static bool TryGetParameterValue(CircuitComponent component, string key, out float value)
        {
            value = 0f;
            var parameter = component != null ? component.GetParameter(key) : null;
            if (parameter == null)
            {
                return false;
            }

            value = parameter.value;
            return true;
        }

        private bool IsLoadEnergized(CircuitComponent component, HashSet<TerminalView> powered, HashSet<TerminalView> neutral)
        {
            if (component.Definition.kind == ComponentKind.PowerSource || component.Definition.kind == ComponentKind.EnergyMeter || component.Definition.kind == ComponentKind.Switch || component.Definition.kind == ComponentKind.TwoWaySwitch || component.Definition.kind == ComponentKind.Fuse || component.Definition.kind == ComponentKind.Breaker || component.Definition.kind == ComponentKind.PushButton || component.Definition.kind == ComponentKind.TerminalBlock || component.Definition.kind == ComponentKind.Instrument)
            {
                return false;
            }

            if (component.Definition.kind == ComponentKind.Motor)
            {
                return component.Terminals.Count(t => t.Role == TerminalRole.Input && powered.Contains(t)) >= 3;
            }

            if (component.Definition.kind == ComponentKind.ContactorCoil)
            {
                var coilA1 = component.Terminals.FirstOrDefault(t => t.Role == TerminalRole.CoilA1);
                var coilA2 = component.Terminals.FirstOrDefault(t => t.Role == TerminalRole.CoilA2);
                return coilA1 != null && coilA2 != null && powered.Contains(coilA1) && neutral.Contains(coilA2);
            }

            var hasPhase = component.Terminals.Any(t => powered.Contains(t) && t.Role != TerminalRole.Neutral && t.Role != TerminalRole.CoilA2);
            var hasNeutral = component.Terminals.Any(t => neutral.Contains(t) || t.Role == TerminalRole.CoilA2 && neutral.Contains(t));
            return hasPhase && hasNeutral;
        }

        private List<TerminalView> GetPhaseRoots()
        {
            return components
                .Where(c => c.Definition.kind == ComponentKind.PowerSource || c.Definition.kind == ComponentKind.EnergyMeter)
                .SelectMany(c => c.Terminals)
                .Where(t => t.Role == TerminalRole.Phase)
                .ToList();
        }

        private List<TerminalView> GetNeutralRoots()
        {
            return components
                .Where(c => c.Definition.kind == ComponentKind.PowerSource || c.Definition.kind == ComponentKind.EnergyMeter)
                .SelectMany(c => c.Terminals)
                .Where(t => t.Role == TerminalRole.Neutral)
                .ToList();
        }

        private void UpdateClosedContactors(HashSet<TerminalView> powered, HashSet<TerminalView> neutral)
        {
            closedContactors.Clear();
            foreach (var component in components)
            {
                if (component.Definition.kind != ComponentKind.ContactorCoil)
                {
                    continue;
                }

                var coilA1 = component.Terminals.FirstOrDefault(t => t.Role == TerminalRole.CoilA1);
                var coilA2 = component.Terminals.FirstOrDefault(t => t.Role == TerminalRole.CoilA2);
                if (coilA1 != null && coilA2 != null && powered.Contains(coilA1) && neutral.Contains(coilA2))
                {
                    closedContactors.Add(component);
                }
            }
        }

        private void BuildGraph()
        {
            graph.Clear();

            foreach (var component in components)
            {
                foreach (var terminal in component.Terminals)
                {
                    Ensure(terminal);
                }
            }

            foreach (var wire in wires)
            {
                Connect(wire.StartTerminal, wire.EndTerminal);
            }

            foreach (var component in components)
            {
                AddInternalConnections(component);
            }
        }

        private void AddInternalConnections(CircuitComponent component)
        {
            var terms = component.Terminals.ToList();
            if (terms.Count < 2)
            {
                return;
            }

            switch (component.Definition.kind)
            {
                case ComponentKind.TwoWaySwitch:
                    Connect(terms[0], component.IsClosed ? terms[1] : terms[2]);
                    break;
                case ComponentKind.EnergyMeter:
                    ConnectPairs(terms, true);
                    break;
                case ComponentKind.ContactorCoil:
                    if (closedContactors.Contains(component))
                    {
                        ConnectById(component, "L1", "T1");
                        ConnectById(component, "L2", "T2");
                        ConnectById(component, "L3", "T3");
                    }
                    break;
                case ComponentKind.Switch:
                case ComponentKind.PushButton:
                case ComponentKind.Fuse:
                case ComponentKind.Breaker:
                case ComponentKind.TerminalBlock:
                    ConnectPairs(terms, component.IsClosed || component.Definition.kind == ComponentKind.TerminalBlock);
                    break;
            }
        }

        private void ConnectById(CircuitComponent component, string a, string b)
        {
            Connect(component.GetTerminal(a), component.GetTerminal(b));
        }

        private void ConnectPairs(List<TerminalView> terms, bool enabled)
        {
            if (!enabled)
            {
                return;
            }

            for (var i = 0; i + 1 < terms.Count; i += 2)
            {
                Connect(terms[i], terms[i + 1]);
            }
        }

        private HashSet<TerminalView> Flood(List<TerminalView> roots)
        {
            var visited = new HashSet<TerminalView>();
            var queue = new Queue<TerminalView>();

            foreach (var root in roots)
            {
                visited.Add(root);
                queue.Enqueue(root);
            }

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (!graph.TryGetValue(current, out var next))
                {
                    continue;
                }

                foreach (var terminal in next)
                {
                    if (visited.Add(terminal))
                    {
                        queue.Enqueue(terminal);
                    }
                }
            }

            return visited;
        }

        private void Ensure(TerminalView terminal)
        {
            if (terminal != null && !graph.ContainsKey(terminal))
            {
                graph.Add(terminal, new List<TerminalView>());
            }
        }

        private void Connect(TerminalView a, TerminalView b)
        {
            if (a == null || b == null)
            {
                return;
            }

            Ensure(a);
            Ensure(b);

            if (!graph[a].Contains(b))
            {
                graph[a].Add(b);
            }

            if (!graph[b].Contains(a))
            {
                graph[b].Add(a);
            }
        }
    }
}

