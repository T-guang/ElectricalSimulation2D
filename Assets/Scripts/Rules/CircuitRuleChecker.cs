using System.Collections.Generic;
using ElectricalSim.Core;

namespace ElectricalSim.Rules
{
    public sealed class CircuitRuleChecker
    {
        private readonly WorkspaceController workspace;
        private readonly CircuitCheckResult result = new CircuitCheckResult();
        private readonly Dictionary<string, HashSet<string>> structuralGraph = new Dictionary<string, HashSet<string>>();
        private readonly Dictionary<string, HashSet<string>> liveGraph = new Dictionary<string, HashSet<string>>();
        private readonly List<CircuitComponent> powers = new List<CircuitComponent>();
        private readonly List<CircuitComponent> loads = new List<CircuitComponent>();
        private readonly List<CircuitComponent> switches = new List<CircuitComponent>();
        private readonly List<CircuitComponent> breakers = new List<CircuitComponent>();
        private readonly List<CircuitComponent> meters = new List<CircuitComponent>();

        public CircuitRuleChecker(WorkspaceController workspace)
        {
            this.workspace = workspace;
        }

        public CircuitCheckResult Check()
        {
            if (workspace == null)
            {
                AddIssue(CircuitIssueSeverity.Error, "WORKSPACE_MISSING", "未能读取当前画布。", "规则检测器没有拿到工作区对象。", null, null);
                return result;
            }

            ClassifyComponents();
            BuildGraphs();
            CheckBasicCompleteness();
            CheckLoadConnections();
            CheckPhaseAndNeutralPaths();
            CheckSwitchOnPhaseBranch();
            CheckBreakers();
            CheckMeters();
            CheckIsolatedComponents();
            CheckOpenDevicesAffectLoads();
            return result;
        }

        private void ClassifyComponents()
        {
            var components = workspace.Components;
            if (components == null)
            {
                return;
            }

            foreach (var component in components)
            {
                if (component == null || component.Definition == null)
                {
                    continue;
                }

                var definition = component.Definition;
                if (definition.kind == ComponentKind.PowerSource || definition.name.Contains("Power"))
                {
                    powers.Add(component);
                }

                if (definition.kind == ComponentKind.Lamp || definition.kind == ComponentKind.Fan)
                {
                    loads.Add(component);
                }

                if (definition.kind == ComponentKind.Switch || definition.kind == ComponentKind.TwoWaySwitch || definition.kind == ComponentKind.PushButton)
                {
                    switches.Add(component);
                }

                if (definition.kind == ComponentKind.Breaker)
                {
                    breakers.Add(component);
                }

                if (definition.kind == ComponentKind.EnergyMeter)
                {
                    meters.Add(component);
                }
            }
        }

        private void BuildGraphs()
        {
            var components = workspace.Components;
            if (components != null)
            {
                foreach (var component in components)
                {
                    if (component == null || component.Terminals == null)
                    {
                        continue;
                    }

                    foreach (var terminal in component.Terminals)
                    {
                        AddNode(structuralGraph, Node(terminal));
                        AddNode(liveGraph, Node(terminal));
                    }

                    AddInternalEdges(component, structuralGraph, true);
                    AddInternalEdges(component, liveGraph, false);
                }
            }

            var wires = workspace.WireManager != null ? workspace.WireManager.Wires : null;
            if (wires == null)
            {
                return;
            }

            foreach (var wire in wires)
            {
                if (wire == null || wire.StartTerminal == null || wire.EndTerminal == null)
                {
                    continue;
                }

                AddEdge(structuralGraph, Node(wire.StartTerminal), Node(wire.EndTerminal));
                AddEdge(liveGraph, Node(wire.StartTerminal), Node(wire.EndTerminal));
            }
        }

        private void AddInternalEdges(CircuitComponent component, Dictionary<string, HashSet<string>> graph, bool structural)
        {
            if (component == null || component.Definition == null)
            {
                return;
            }

            switch (component.Definition.kind)
            {
                case ComponentKind.Switch:
                case ComponentKind.PushButton:
                    if (structural || component.IsClosed)
                    {
                        AddTerminalEdge(graph, component, "L", "L1");
                    }
                    break;
                case ComponentKind.TwoWaySwitch:
                    if (structural)
                    {
                        AddTerminalEdge(graph, component, "L", "L1");
                        AddTerminalEdge(graph, component, "L", "L2");
                    }
                    else
                    {
                        AddTerminalEdge(graph, component, "L", component.IsClosed ? "L1" : "L2");
                    }
                    break;
                case ComponentKind.Breaker:
                    if (structural || component.IsClosed)
                    {
                        AddTerminalEdge(graph, component, "P1_IN", "P1_OUT");
                        AddTerminalEdge(graph, component, "P2_IN", "P2_OUT");
                        AddTerminalEdge(graph, component, "L_IN", "L_OUT");
                        AddTerminalEdge(graph, component, "N_IN", "N_OUT");
                    }
                    break;
                case ComponentKind.EnergyMeter:
                    AddTerminalEdge(graph, component, "L_IN", "L_OUT");
                    AddTerminalEdge(graph, component, "N_IN", "N_OUT");
                    break;
            }
        }

        private void CheckBasicCompleteness()
        {
            if (powers.Count == 0)
            {
                AddIssue(CircuitIssueSeverity.Error, "NO_POWER", "未检测到电源元件。", "当前画布中没有 220V 电源或交流电源。", "请先放置一个电源元件。");
            }

            if (loads.Count == 0)
            {
                AddIssue(CircuitIssueSeverity.Error, "NO_LOAD", "未检测到负载元件。", "当前画布中没有灯泡或电风扇等负载。", "请至少放置一个负载元件。");
            }

            var wires = workspace.WireManager != null ? workspace.WireManager.Wires : null;
            if (wires == null || wires.Count == 0)
            {
                AddIssue(CircuitIssueSeverity.Error, "NO_WIRE", "当前电路没有导线连接。", "元件之间还没有形成任何接线关系。", "请用导线连接电源、开关和负载端子。");
            }
        }

        private void CheckLoadConnections()
        {
            foreach (var load in loads)
            {
                var lTerminal = FindPhaseTerminal(load);
                var nTerminal = FindNeutralTerminal(load);

                if (lTerminal == null || !HasAnyWire(lTerminal))
                {
                    AddIssue(CircuitIssueSeverity.Error, "LOAD_L_MISSING", LoadName(load) + "缺少 L 端连接。", "负载火线端没有连接导线。", "请检查 " + LoadName(load) + " 的 L 端是否接入火线支路。", load);
                }

                if (nTerminal == null || !HasAnyWire(nTerminal))
                {
                    AddIssue(CircuitIssueSeverity.Error, "LOAD_N_MISSING", LoadName(load) + "缺少 N 端连接。", "负载零线端没有连接导线。", "请检查 " + LoadName(load) + " 的 N 端是否接回电源 N。", load);
                }
            }
        }

        private void CheckPhaseAndNeutralPaths()
        {
            foreach (var load in loads)
            {
                var loadL = FindPhaseTerminal(load);
                var loadN = FindNeutralTerminal(load);

                if (loadL != null && !CanReachAnyPowerTerminal(loadL, TerminalRole.Phase, structuralGraph))
                {
                    AddIssue(CircuitIssueSeverity.Error, "PHASE_PATH_MISSING", "未检测到从电源 L 到 " + LoadName(load) + " L 端的完整火线路径。", "该负载火线支路没有和电源 L 形成连通。", "请检查电源 L、空开、开关和负载 L 端之间的连接。", load);
                }

                if (loadN != null && !CanReachAnyPowerTerminal(loadN, TerminalRole.Neutral, structuralGraph))
                {
                    AddIssue(CircuitIssueSeverity.Error, "NEUTRAL_PATH_MISSING", "未检测到 " + LoadName(load) + " N 端回到电源 N 的零线回路。", "该负载零线端没有和电源 N 连通。", "请检查负载 N 端是否接回电源 N。", load);
                }
            }
        }

        private void CheckSwitchOnPhaseBranch()
        {
            foreach (var load in loads)
            {
                var loadL = FindPhaseTerminal(load);
                if (loadL == null || !CanReachAnyPowerTerminal(loadL, TerminalRole.Phase, structuralGraph))
                {
                    continue;
                }

                if (switches.Count == 0)
                {
                    AddIssue(CircuitIssueSeverity.Warning, "NO_SWITCH", LoadName(load) + "火线支路可能未经过开关控制。", "当前电路没有检测到家庭照明开关。", "家庭照明中通常应让开关控制火线。", load);
                    continue;
                }

                var switchOnPhasePath = false;
                foreach (var sw in switches)
                {
                    var input = sw.GetTerminal("L");
                    var output = sw.GetTerminal("L1") ?? sw.GetTerminal("L2");
                    if (input != null && output != null &&
                        CanReachAnyPowerTerminal(input, TerminalRole.Phase, structuralGraph) &&
                        AreConnected(output, loadL, structuralGraph))
                    {
                        switchOnPhasePath = true;
                        break;
                    }
                }

                if (!switchOnPhasePath)
                {
                    AddIssue(CircuitIssueSeverity.Warning, "SWITCH_NOT_ON_PHASE", LoadName(load) + "火线支路可能未经过开关控制。", "未检测到开关位于电源 L 到负载 L 的控制路径上。", "请确认开关串联在火线支路，而不是只接在旁路或零线支路。", load);
                }
            }

            foreach (var sw in switches)
            {
                var l = sw.GetTerminal("L");
                if (l != null && CanReachAnyPowerTerminal(l, TerminalRole.Neutral, structuralGraph))
                {
                    AddIssue(CircuitIssueSeverity.Warning, "SWITCH_ON_NEUTRAL", "开关疑似接在零线支路上。", sw.Definition.displayName + " 的 L 端与电源 N 连通。", "家庭照明中通常应优先控制火线。", sw);
                }
            }
        }

        private void CheckBreakers()
        {
            if (breakers.Count == 0)
            {
                AddIssue(CircuitIssueSeverity.Info, "NO_BREAKER", "当前电路未检测到 2P 空气开关。", "基础演示电路可以运行，但实际家庭电路通常需要保护开关。", "后续可加入 2P 空气开关，让电源 L/N 先进入空开输入端。");
                return;
            }

            foreach (var breaker in breakers)
            {
                var p1In = breaker.GetTerminal("P1_IN");
                var p2In = breaker.GetTerminal("P2_IN");
                var p1Out = breaker.GetTerminal("P1_OUT");
                var p2Out = breaker.GetTerminal("P2_OUT");
                var incomplete = p1In == null || p2In == null || p1Out == null || p2Out == null ||
                    !CanReachAnyPowerTerminal(p1In, TerminalRole.Phase, structuralGraph) ||
                    !CanReachAnyPowerTerminal(p2In, TerminalRole.Neutral, structuralGraph) ||
                    !HasAnyWire(p1Out) ||
                    !HasAnyWire(p2Out);

                if (incomplete)
                {
                    AddIssue(CircuitIssueSeverity.Warning, "BREAKER_INCOMPLETE", "空气开关进线或出线不完整。", breaker.Definition.displayName + " 没有形成完整的 L/N 输入输出结构。", "请确认电源 L/N 先进入空开输入端，再由空开输出端连接后续负载。", breaker);
                }
            }
        }

        private void CheckMeters()
        {
            foreach (var meter in meters)
            {
                var missing = new List<string>();
                if (!HasAnyWire(meter.GetTerminal("L_IN"))) missing.Add("L_IN");
                if (!HasAnyWire(meter.GetTerminal("L_OUT"))) missing.Add("L_OUT");
                if (!HasAnyWire(meter.GetTerminal("N_IN"))) missing.Add("N_IN");
                if (!HasAnyWire(meter.GetTerminal("N_OUT"))) missing.Add("N_OUT");

                if (missing.Count > 0)
                {
                    AddIssue(CircuitIssueSeverity.Warning, "METER_INCOMPLETE", "单相电能表进线或出线不完整。", "缺少连接端子：" + string.Join("、", missing.ToArray()) + "。", "请检查 L_IN、L_OUT、N_IN、N_OUT 是否按进出线关系连接。", meter);
                }
            }
        }

        private void CheckIsolatedComponents()
        {
            var components = workspace.Components;
            if (components == null)
            {
                return;
            }

            foreach (var component in components)
            {
                if (component == null || component.Terminals == null)
                {
                    continue;
                }

                var hasWire = false;
                foreach (var terminal in component.Terminals)
                {
                    if (HasAnyWire(terminal))
                    {
                        hasWire = true;
                        break;
                    }
                }

                if (!hasWire)
                {
                    AddIssue(CircuitIssueSeverity.Info, "ISOLATED_COMPONENT", "元件 " + ComponentName(component) + " 当前没有任何导线连接。", "该元件暂时没有参与当前电路。", "如果它是多余元件，可以删除；如果需要参与电路，请接入对应端子。", component);
                }
            }
        }

        private void CheckOpenDevicesAffectLoads()
        {
            foreach (var load in loads)
            {
                var loadL = FindPhaseTerminal(load);
                var loadN = FindNeutralTerminal(load);
                if (loadL == null || loadN == null)
                {
                    continue;
                }

                var structuralOk = CanReachAnyPowerTerminal(loadL, TerminalRole.Phase, structuralGraph) &&
                    CanReachAnyPowerTerminal(loadN, TerminalRole.Neutral, structuralGraph);
                var liveOk = CanReachAnyPowerTerminal(loadL, TerminalRole.Phase, liveGraph) &&
                    CanReachAnyPowerTerminal(loadN, TerminalRole.Neutral, liveGraph);
                if (structuralOk && !liveOk)
                {
                    var offDevices = new System.Collections.Generic.List<string>();
                    foreach (var b in breakers) { if (!b.IsClosed) offDevices.Add(ComponentName(b)); }
                    foreach (var s in switches) { if (!s.IsClosed) offDevices.Add(ComponentName(s)); }

                    if (offDevices.Count == 1)
                    {
                        AddIssue(CircuitIssueSeverity.Info, "OPEN_DEVICE", 
                            LoadName(load) + "当前可能因" + offDevices[0] + "处于 OFF 状态而未通电。", 
                            "物理结构已接线，但实时计算状态下回路断开", 
                            "建议：请先将" + offDevices[0] + "切换为 ON，再运行仿真观察负载状态。", load);
                    }
                    else if (offDevices.Count > 1)
                    {
                        var sb = new System.Text.StringBuilder();
                        sb.AppendLine(LoadName(load) + "当前可能因以下控制元件处于 OFF 状态而未通电：");
                        for (int i = 0; i < offDevices.Count; i++)
                        {
                            sb.AppendLine((i + 1) + ". " + offDevices[i]);
                        }
                        AddIssue(CircuitIssueSeverity.Info, "OPEN_DEVICE", 
                            sb.ToString().TrimEnd(), 
                            "物理结构已接线，但实时计算状态下回路断开", 
                            "建议：请依次确认这些控制元件是否需要切换为 ON。", load);
                    }
                    else
                    {
                        AddIssue(CircuitIssueSeverity.Info, "OPEN_DEVICE", 
                            LoadName(load) + "当前可能因开关或空开断开而未通电。", 
                            "物理结构已接线，但实时计算状态下回路断开", 
                            "请将双控或单控开关切换为 ON/OFF 进行观察负载状态。", load);
                    }
                }
            }
        }

                var structuralOk = CanReachAnyPowerTerminal(loadL, TerminalRole.Phase, structuralGraph) &&
                    CanReachAnyPowerTerminal(loadN, TerminalRole.Neutral, structuralGraph);
                var liveOk = CanReachAnyPowerTerminal(loadL, TerminalRole.Phase, liveGraph) &&
                    CanReachAnyPowerTerminal(loadN, TerminalRole.Neutral, liveGraph);
                if (structuralOk && !liveOk)
                {
                    AddIssue(CircuitIssueSeverity.Info, "OPEN_DEVICE", LoadName(load) + "当前可能因开关或空开断开而未通电。", "接线结构基本存在，但实时开关状态让回路断开。", "双击开关或空开切换 ON/OFF 后，再运行仿真观察负载状态。", load);
                }
            }
        }

        private bool CanReachAnyPowerTerminal(TerminalView from, TerminalRole powerRole, Dictionary<string, HashSet<string>> graph)
        {
            if (from == null)
            {
                return false;
            }

            foreach (var power in powers)
            {
                TerminalView target = powerRole == TerminalRole.Neutral ? FindNeutralTerminal(power) : FindPhaseTerminal(power);
                if (target != null && AreConnected(from, target, graph))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool AreConnected(TerminalView a, TerminalView b, Dictionary<string, HashSet<string>> graph)
        {
            if (a == null || b == null)
            {
                return false;
            }

            return AreConnected(Node(a), Node(b), graph);
        }

        private static bool AreConnected(string start, string end, Dictionary<string, HashSet<string>> graph)
        {
            if (string.IsNullOrEmpty(start) || string.IsNullOrEmpty(end) || graph == null || !graph.ContainsKey(start) || !graph.ContainsKey(end))
            {
                return false;
            }

            var visited = new HashSet<string>();
            var queue = new Queue<string>();
            visited.Add(start);
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current == end)
                {
                    return true;
                }

                foreach (var next in graph[current])
                {
                    if (visited.Add(next))
                    {
                        queue.Enqueue(next);
                    }
                }
            }

            return false;
        }

        private bool HasAnyWire(TerminalView terminal)
        {
            if (terminal == null || workspace.WireManager == null || workspace.WireManager.Wires == null)
            {
                return false;
            }

            foreach (var wire in workspace.WireManager.Wires)
            {
                if (wire != null && (wire.StartTerminal == terminal || wire.EndTerminal == terminal))
                {
                    return true;
                }
            }

            return false;
        }

        private static TerminalView FindPhaseTerminal(CircuitComponent component)
        {
            return FindTerminal(component, "L") ?? FindTerminal(component, "L_IN") ?? FindByRole(component, TerminalRole.Phase) ?? FindByRole(component, TerminalRole.Input);
        }

        private static TerminalView FindNeutralTerminal(CircuitComponent component)
        {
            return FindTerminal(component, "N") ?? FindTerminal(component, "N_IN") ?? FindByRole(component, TerminalRole.Neutral);
        }

        private static TerminalView FindTerminal(CircuitComponent component, string terminalId)
        {
            return component != null ? component.GetTerminal(terminalId) : null;
        }

        private static TerminalView FindByRole(CircuitComponent component, TerminalRole role)
        {
            if (component == null || component.Terminals == null)
            {
                return null;
            }

            foreach (var terminal in component.Terminals)
            {
                if (terminal != null && terminal.Role == role)
                {
                    return terminal;
                }
            }

            return null;
        }

        private static void AddTerminalEdge(Dictionary<string, HashSet<string>> graph, CircuitComponent component, string a, string b)
        {
            var first = component.GetTerminal(a);
            var second = component.GetTerminal(b);
            if (first != null && second != null)
            {
                AddEdge(graph, Node(first), Node(second));
            }
        }

        private static void AddNode(Dictionary<string, HashSet<string>> graph, string node)
        {
            if (!string.IsNullOrEmpty(node) && !graph.ContainsKey(node))
            {
                graph[node] = new HashSet<string>();
            }
        }

        private static void AddEdge(Dictionary<string, HashSet<string>> graph, string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
            {
                return;
            }

            AddNode(graph, a);
            AddNode(graph, b);
            graph[a].Add(b);
            graph[b].Add(a);
        }

        private static string Node(TerminalView terminal)
        {
            if (terminal == null || terminal.Owner == null)
            {
                return string.Empty;
            }

            return terminal.Owner.InstanceId + "." + terminal.TerminalId;
        }

        private void AddIssue(CircuitIssueSeverity severity, string code, string title, string message, string suggestion, CircuitComponent component = null)
        {
            result.Add(new CircuitIssue
            {
                code = code,
                severity = severity,
                title = title,
                message = message,
                suggestion = suggestion,
                componentId = component != null ? component.InstanceId : string.Empty,
                componentName = component != null ? ComponentName(component) : string.Empty
            });
        }

        private static string ComponentName(CircuitComponent component)
        {
            return component != null && component.Definition != null ? component.Definition.displayName : "未知元件";
        }

        private static string LoadName(CircuitComponent component)
        {
            return ComponentName(component);
        }
    }
}

