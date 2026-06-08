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
        private readonly float simulationDeltaTime;
        private const int MaxContactorStabilizationIterations = 4;
        private static readonly Dictionary<int, bool> selfHoldEligibleContactors = new Dictionary<int, bool>();

        public SimulationEngine(List<CircuitComponent> components, IReadOnlyList<WireView> wires, float simulationDeltaTime = 0f)
        {
            this.components = components;
            this.wires = wires;
            this.simulationDeltaTime = Mathf.Max(0f, simulationDeltaTime);
        }

        public string Run()
        {
            closedContactors.Clear();
            SeedClosedContactorsFromRuntimeState();
            BuildGraph();

            var phaseRoots = GetPhaseRoots();
            var neutralRoots = GetNeutralRoots();

            for (var i = 0; i < MaxContactorStabilizationIterations; i++)
            {
                var previous = new HashSet<CircuitComponent>(closedContactors);
                UpdateClosedContactors();
                if (previous.SetEquals(closedContactors))
                {
                    break;
                }

                BuildGraph();
            }

            UpdateSelfHoldEligibility();

            var powered = Flood(phaseRoots);
            var neutral = Flood(neutralRoots);
            var shorted = powered.Overlaps(neutral);
            var energizedCount = 0;

            var systemVoltage = 220f;
            var powerSource = components.FirstOrDefault(c => c.Definition.kind == ComponentKind.PowerSource);
            if (powerSource != null)
            {
                systemVoltage = ResolveVoltage(powerSource, powerSource.Definition);
            }

            foreach (var component in components)
            {
                var energized = IsLoadEnergized(component, powered, neutral);
                var active = !shorted && energized;
                UpdateMotorDirection(component, active);
                component.SetEnergized(active);
                ApplyMeasurement(component, active, systemVoltage);
                if (active)
                {
                    energizedCount++;
                }
            }

            if (UpdateThermalRelays())
            {
                closedContactors.Clear();
                SeedClosedContactorsFromRuntimeState();
                BuildGraph();

                for (var i = 0; i < MaxContactorStabilizationIterations; i++)
                {
                    var previous = new HashSet<CircuitComponent>(closedContactors);
                    UpdateClosedContactors();
                    if (previous.SetEquals(closedContactors))
                    {
                        break;
                    }

                    BuildGraph();
                }

                UpdateSelfHoldEligibility();

                powered = Flood(phaseRoots);
                neutral = Flood(neutralRoots);
                shorted = powered.Overlaps(neutral);
                energizedCount = 0;

                foreach (var component in components)
                {
                    var energized = IsLoadEnergized(component, powered, neutral);
                    var active = !shorted && energized;
                    UpdateMotorDirection(component, active);
                    component.SetEnergized(active);
                    ApplyMeasurement(component, active, systemVoltage);
                    if (active)
                    {
                        energizedCount++;
                    }
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

        private static void ApplyMeasurement(CircuitComponent component, bool active, float systemVoltage)
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

            var voltage = definition.kind == ComponentKind.PowerSource ? ResolveVoltage(component, definition) : (systemVoltage > 0f ? systemVoltage : ResolveVoltage(component, definition));
            var power = ResolvePower(component, definition);
            var current = ResolveCurrent(component, definition);

            if (!IsThreePhaseMotorComponent(component) && voltage > 0f && power > 0f)
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

            var ratedCurrent = definition.ratedCurrent;
            if (TryGetParameterValue(component, "ratedCurrent", out value))
            {
                ratedCurrent = value;
            }

            if (TryGetParameterValue(component, "loadFactor", out var loadFactor))
            {
                return ratedCurrent * Mathf.Max(0f, loadFactor);
            }

            return ratedCurrent;
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

        private void SeedClosedContactorsFromRuntimeState()
        {
            foreach (var component in components)
            {
                if (IsContactorComponent(component) && component.IsEnergized && IsSelfHoldEligible(component))
                {
                    closedContactors.Add(component);
                }
            }
        }

        private bool IsLoadEnergized(CircuitComponent component, HashSet<TerminalView> powered, HashSet<TerminalView> neutral)
        {
            if (component.Definition.kind == ComponentKind.PowerSource || component.Definition.kind == ComponentKind.EnergyMeter || component.Definition.kind == ComponentKind.Switch || component.Definition.kind == ComponentKind.TwoWaySwitch || component.Definition.kind == ComponentKind.Fuse || component.Definition.kind == ComponentKind.Breaker || component.Definition.kind == ComponentKind.PushButton || component.Definition.kind == ComponentKind.TerminalBlock || component.Definition.kind == ComponentKind.Instrument)
            {
                return false;
            }

            if (component.Definition.kind == ComponentKind.Motor)
            {
                if (component.GetTerminal("U") != null && component.GetTerminal("V") != null && component.GetTerminal("W") != null)
                {
                    return IsThreePhaseMotorEnergized(component);
                }

                return HasPhaseAndNeutral(component, powered, neutral);
            }

            if (component.Definition.kind == ComponentKind.ContactorCoil)
            {
                return closedContactors.Contains(component);
            }

            return HasPhaseAndNeutral(component, powered, neutral);
        }

        private static bool HasPhaseAndNeutral(CircuitComponent component, HashSet<TerminalView> powered, HashSet<TerminalView> neutral)
        {
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

        private void UpdateClosedContactors()
        {
            closedContactors.Clear();
            foreach (var component in components)
            {
                if (!IsContactorComponent(component))
                {
                    continue;
                }

                if (IsContactorCoilEnergized(component))
                {
                    closedContactors.Add(component);
                }
            }

            ResolveMutualInterlockConflicts();
        }

        private static bool IsContactorComponent(CircuitComponent component)
        {
            if (component == null || component.Definition == null)
            {
                return false;
            }

            return component.Definition.kind == ComponentKind.ContactorCoil ||
                component.GetTerminal("A1") != null &&
                component.GetTerminal("A2") != null &&
                component.GetTerminal("L1") != null &&
                component.GetTerminal("T1") != null;
        }

        private bool IsContactorCoilEnergized(CircuitComponent contactor)
        {
            var coilA1 = contactor.GetTerminal("A1");
            var coilA2 = contactor.GetTerminal("A2");
            if (coilA1 == null || coilA2 == null || AreConnected(coilA1, coilA2))
            {
                return false;
            }

            var a1Phases = GetReachablePowerPhaseKeys(coilA1);
            var a2Phases = GetReachablePowerPhaseKeys(coilA2);

            if (a1Phases.Any(a => a2Phases.Any(b => b != a)))
            {
                return true;
            }

            return a1Phases.Count > 0 && CanReachPowerNeutral(coilA2) ||
                a2Phases.Count > 0 && CanReachPowerNeutral(coilA1);
        }

        private void UpdateSelfHoldEligibility()
        {
            foreach (var component in components)
            {
                if (!IsContactorComponent(component))
                {
                    continue;
                }

                var id = component.GetInstanceID();
                if (!closedContactors.Contains(component))
                {
                    selfHoldEligibleContactors[id] = false;
                    continue;
                }

                if (IsSelfHoldBrokenByJogButton(component))
                {
                    selfHoldEligibleContactors[id] = false;
                    continue;
                }

                selfHoldEligibleContactors[id] = IsSelfHoldEligible(component) || HasClosedContinuousStartPath(component);
            }
        }

        private static bool IsSelfHoldEligible(CircuitComponent component)
        {
            return component != null &&
                selfHoldEligibleContactors.TryGetValue(component.GetInstanceID(), out var eligible) &&
                eligible;
        }

        private bool HasClosedContinuousStartPath(CircuitComponent contactor)
        {
            var a1 = contactor.GetTerminal("A1");
            if (a1 == null)
            {
                return false;
            }

            foreach (var component in components)
            {
                if (!IsClosedContinuousStartComponent(component))
                {
                    continue;
                }

                if (IsJogStartButtonForContactor(component, contactor))
                {
                    continue;
                }

                if (AreConnected(component.GetTerminal("23"), a1) || AreConnected(component.GetTerminal("24"), a1))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsSelfHoldBrokenByJogButton(CircuitComponent contactor)
        {
            foreach (var component in components)
            {
                if (component == null || !component.IsClosed)
                {
                    continue;
                }

                if (IsJogStartButtonForContactor(component, contactor))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsJogStartButtonForContactor(CircuitComponent button, CircuitComponent contactor)
        {
            if (!IsCompoundPushButton(button) || contactor == null)
            {
                return false;
            }

            var a1 = contactor.GetTerminal("A1");
            var button23 = button.GetTerminal("23");
            var button24 = button.GetTerminal("24");
            if (a1 == null || button23 == null || button24 == null)
            {
                return false;
            }

            if (!AreConnected(button23, a1) && !AreConnected(button24, a1))
            {
                return false;
            }

            var instanceId = button.InstanceId ?? string.Empty;
            if (instanceId.IndexOf("jog", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                instanceId.IndexOf("点动", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            var button11 = button.GetTerminal("11");
            var button12 = button.GetTerminal("12");
            var contactor13 = contactor.GetTerminal("13");
            var contactor14 = contactor.GetTerminal("14");
            if (button11 == null || button12 == null || contactor13 == null || contactor14 == null)
            {
                return false;
            }

            return AreConnected(button11, contactor13) ||
                AreConnected(button11, contactor14) ||
                AreConnected(button12, contactor13) ||
                AreConnected(button12, contactor14);
        }
        private static bool IsClosedContinuousStartComponent(CircuitComponent component)
        {
            return component != null &&
                component.Definition != null &&
                component.Definition.kind == ComponentKind.PushButton &&
                component.IsClosed &&
                component.GetTerminal("23") != null &&
                component.GetTerminal("24") != null;
        }

        private void ResolveMutualInterlockConflicts()
        {
            if (closedContactors.Count < 2)
            {
                return;
            }

            var orderedContactors = closedContactors
                .OrderBy(GetComponentPriority)
                .ToList();
            var toRelease = new HashSet<CircuitComponent>();

            foreach (var contactor in orderedContactors)
            {
                if (toRelease.Contains(contactor))
                {
                    continue;
                }

                foreach (var other in orderedContactors)
                {
                    if (contactor == other || toRelease.Contains(other))
                    {
                        continue;
                    }

                    if (!CoilDependsOnNormallyClosedAuxiliary(contactor, other))
                    {
                        continue;
                    }

                    if (CoilDependsOnNormallyClosedAuxiliary(other, contactor))
                    {
                        var loser = GetComponentPriority(contactor) <= GetComponentPriority(other) ? other : contactor;
                        toRelease.Add(loser);
                    }
                    else
                    {
                        toRelease.Add(contactor);
                    }
                }
            }

            foreach (var contactor in toRelease)
            {
                closedContactors.Remove(contactor);
            }
        }

        private int GetComponentPriority(CircuitComponent component)
        {
            var index = components.IndexOf(component);
            return index >= 0 ? index : int.MaxValue;
        }

        private bool CoilDependsOnNormallyClosedAuxiliary(CircuitComponent contactor, CircuitComponent auxiliaryOwner)
        {
            var a1 = contactor.GetTerminal("A1");
            var a2 = contactor.GetTerminal("A2");
            var nc21 = auxiliaryOwner.GetTerminal("21");
            var nc22 = auxiliaryOwner.GetTerminal("22");
            if (a1 == null || a2 == null || nc21 == null || nc22 == null)
            {
                return false;
            }

            return AreConnected(a1, nc21) ||
                AreConnected(a1, nc22) ||
                AreConnected(a2, nc21) ||
                AreConnected(a2, nc22);
        }

        private bool IsThreePhaseMotorEnergized(CircuitComponent motor)
        {
            var u = motor.GetTerminal("U");
            var v = motor.GetTerminal("V");
            var w = motor.GetTerminal("W");
            if (u == null || v == null || w == null)
            {
                return false;
            }

            var uPhases = GetReachablePowerPhaseKeys(u);
            var vPhases = GetReachablePowerPhaseKeys(v);
            var wPhases = GetReachablePowerPhaseKeys(w);
            if (uPhases.Count == 0 || vPhases.Count == 0 || wPhases.Count == 0)
            {
                return false;
            }

            var allPhases = new HashSet<string>(uPhases);
            allPhases.UnionWith(vPhases);
            allPhases.UnionWith(wPhases);
            return allPhases.Count >= 3;
        }

        private void UpdateMotorDirection(CircuitComponent component, bool active)
        {
            if (!IsThreePhaseMotorComponent(component))
            {
                return;
            }

            SetParameterIfPresent(component, "rotationDirection", active ? ResolveThreePhaseMotorDirection(component) : 0f);
        }

        private float ResolveThreePhaseMotorDirection(CircuitComponent motor)
        {
            var u = SingleReachablePowerTerminalId(motor.GetTerminal("U"));
            var v = SingleReachablePowerTerminalId(motor.GetTerminal("V"));
            var w = SingleReachablePowerTerminalId(motor.GetTerminal("W"));

            if (u == "L1" && v == "L2" && w == "L3")
            {
                return 1f;
            }

            if (u == "L3" && v == "L2" && w == "L1")
            {
                return -1f;
            }

            return 0f;
        }

        private string SingleReachablePowerTerminalId(TerminalView terminal)
        {
            var ids = new HashSet<string>();
            foreach (var reachable in Flood(new List<TerminalView> { terminal }))
            {
                if (IsPowerTerminal(reachable, TerminalRole.Phase))
                {
                    ids.Add(reachable.TerminalId);
                }
            }

            return ids.Count == 1 ? ids.First() : string.Empty;
        }

        private bool UpdateThermalRelays()
        {
            var changed = false;
            foreach (var relay in components)
            {
                if (!IsThermalRelayComponent(relay))
                {
                    continue;
                }

                if (TryGetParameterValue(relay, "manualTrip", out var manualTrip) && manualTrip >= 0.5f)
                {
                    changed |= SetThermalRelayTripState(relay, true);
                    continue;
                }

                if (IsThermalRelayTripped(relay))
                {
                    continue;
                }

                var settingCurrent = ResolveThermalRelaySettingCurrent(relay);
                var motorCurrent = ResolveProtectedMotorCurrent(relay);
                if (settingCurrent <= 0f || motorCurrent <= settingCurrent)
                {
                    SetParameterIfPresent(relay, "overloadTimer", 0f);
                    continue;
                }

                var overloadTimer = ResolveParameterValue(relay, "overloadTimer", 0f);
                overloadTimer += simulationDeltaTime;
                SetParameterIfPresent(relay, "overloadTimer", overloadTimer);

                var tripDelay = Mathf.Max(0f, ResolveParameterValue(relay, "tripDelay", 3f));
                if (overloadTimer >= tripDelay)
                {
                    changed |= SetThermalRelayTripState(relay, true);
                }
            }

            return changed;
        }

        private float ResolveProtectedMotorCurrent(CircuitComponent relay)
        {
            var relayT1 = relay.GetTerminal("T1");
            var relayT2 = relay.GetTerminal("T2");
            var relayT3 = relay.GetTerminal("T3");
            var maxCurrent = 0f;

            foreach (var motor in components)
            {
                if (!IsThreePhaseMotorComponent(motor) || !motor.IsEnergized)
                {
                    continue;
                }

                if (AreConnected(relayT1, motor.GetTerminal("U")) &&
                    AreConnected(relayT2, motor.GetTerminal("V")) &&
                    AreConnected(relayT3, motor.GetTerminal("W")))
                {
                    maxCurrent = Mathf.Max(maxCurrent, ResolveCurrent(motor, motor.Definition));
                }
            }

            return maxCurrent;
        }

        private static float ResolveThermalRelaySettingCurrent(CircuitComponent relay)
        {
            if (TryGetParameterValue(relay, "settingCurrent", out var value))
            {
                return value;
            }

            if (TryGetParameterValue(relay, "ratedCurrent", out value))
            {
                return value;
            }

            return relay.Definition != null ? relay.Definition.ratedCurrent : 0f;
        }

        private static float ResolveParameterValue(CircuitComponent component, string key, float fallback)
        {
            return TryGetParameterValue(component, key, out var value) ? value : fallback;
        }

        private static bool SetThermalRelayTripState(CircuitComponent relay, bool tripped)
        {
            var oldState = IsThermalRelayTripped(relay);
            SetParameterIfPresent(relay, "tripState", tripped ? 1f : 0f);
            return oldState != tripped;
        }

        private static void SetParameterIfPresent(CircuitComponent component, string key, float value)
        {
            component?.SetParameterValue(key, value);
        }

        private static bool IsThreePhaseMotorComponent(CircuitComponent component)
        {
            return component != null &&
                component.Definition != null &&
                component.Definition.kind == ComponentKind.Motor &&
                component.GetTerminal("U") != null &&
                component.GetTerminal("V") != null &&
                component.GetTerminal("W") != null;
        }

        private HashSet<string> GetReachablePowerPhaseKeys(TerminalView terminal)
        {
            var phases = new HashSet<string>();
            foreach (var reachable in Flood(new List<TerminalView> { terminal }))
            {
                if (IsPowerTerminal(reachable, TerminalRole.Phase))
                {
                    phases.Add(PowerTerminalKey(reachable));
                }
            }

            return phases;
        }

        private bool CanReachPowerNeutral(TerminalView terminal)
        {
            return Flood(new List<TerminalView> { terminal }).Any(t => IsPowerTerminal(t, TerminalRole.Neutral));
        }

        private bool AreConnected(TerminalView a, TerminalView b)
        {
            return a != null && b != null && Flood(new List<TerminalView> { a }).Contains(b);
        }

        private static bool IsPowerTerminal(TerminalView terminal, TerminalRole role)
        {
            return terminal != null &&
                terminal.Role == role &&
                terminal.Owner != null &&
                terminal.Owner.Definition != null &&
                (terminal.Owner.Definition.kind == ComponentKind.PowerSource || terminal.Owner.Definition.kind == ComponentKind.EnergyMeter);
        }

        private static string PowerTerminalKey(TerminalView terminal)
        {
            var ownerId = terminal.Owner != null ? terminal.Owner.InstanceId : string.Empty;
            return ownerId + "." + terminal.TerminalId;
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

            if (IsThermalRelayComponent(component))
            {
                AddThermalRelayInternalConnections(component);
                return;
            }

            if (IsCompoundPushButton(component))
            {
                if (component.IsClosed)
                {
                    ConnectById(component, "23", "24");
                }
                else
                {
                    ConnectById(component, "11", "12");
                }

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
                        ConnectById(component, "13", "14");
                    }
                    else
                    {
                        ConnectById(component, "21", "22");
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

        private static bool IsCompoundPushButton(CircuitComponent component)
        {
            return component != null &&
                component.Definition != null &&
                component.GetTerminal("11") != null &&
                component.GetTerminal("12") != null &&
                component.GetTerminal("23") != null &&
                component.GetTerminal("24") != null &&
                component.Definition.name.IndexOf("Button_Compound", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void AddThermalRelayInternalConnections(CircuitComponent component)
        {
            if (component == null || !component.IsClosed)
            {
                return;
            }

            ConnectById(component, "L1", "T1");
            ConnectById(component, "L2", "T2");
            ConnectById(component, "L3", "T3");

            if (IsThermalRelayTripped(component))
            {
                ConnectById(component, "97", "98");
            }
            else
            {
                ConnectById(component, "95", "96");
            }
        }

        private static bool IsThermalRelayComponent(CircuitComponent component)
        {
            return component != null &&
                component.Definition != null &&
                component.GetTerminal("95") != null &&
                component.GetTerminal("96") != null &&
                component.GetTerminal("97") != null &&
                component.GetTerminal("98") != null &&
                component.GetTerminal("L1") != null &&
                component.GetTerminal("T1") != null;
        }

        private static bool IsThermalRelayTripped(CircuitComponent component)
        {
            return TryGetParameterValue(component, "tripState", out var value) && value >= 0.5f ||
                TryGetParameterValue(component, "isTripped", out value) && value >= 0.5f ||
                TryGetParameterValue(component, "tripped", out value) && value >= 0.5f;
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


