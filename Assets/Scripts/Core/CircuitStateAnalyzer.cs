using System;
using System.Collections.Generic;
using System.Text;

namespace ElectricalSim.Core
{
    public sealed class CircuitStateAnalyzer
    {
        public const string VoltageNone = "None";
        public const string VoltageL = "L";
        public const string VoltageN = "N";
        public const string VoltagePE = "PE";
        public const string VoltageConflict = "Conflict";

        private readonly Dictionary<string, TerminalView> terminalsByKey = new Dictionary<string, TerminalView>();
        private readonly Dictionary<string, string> friendlyNamesByInstanceId = new Dictionary<string, string>();
        private readonly HashSet<string> wiredTerminalKeys = new HashSet<string>();
        private readonly Dictionary<string, HashSet<string>> connectionGraph = new Dictionary<string, HashSet<string>>();

        public CircuitStateResult Analyze(
            IReadOnlyList<CircuitComponent> components,
            IReadOnlyList<WireView> wires)
        {
            terminalsByKey.Clear();
            friendlyNamesByInstanceId.Clear();
            wiredTerminalKeys.Clear();
            connectionGraph.Clear();

            var result = new CircuitStateResult();
            BuildFriendlyNames(components);
            var unionFind = new TerminalUnionFind();
            RegisterTerminals(components, unionFind, result);
            AddWireConnections(wires, unionFind, result);
            AddInternalConnections(components, unionFind);

            var rootLabels = new Dictionary<string, HashSet<string>>();
            var rootSources = new Dictionary<string, List<string>>();
            AddPowerLabels(components, unionFind, rootLabels, rootSources, result);
            DetectPowerConflicts(rootLabels, result);
            BuildComponentStates(components, unionFind, rootLabels, rootSources, result);
            BuildLoadPathExplanations(components, result);
            FinalizeBreakerControlAnalysis(result);
            return result;
        }

        private void RegisterTerminals(
            IReadOnlyList<CircuitComponent> components,
            TerminalUnionFind unionFind,
            CircuitStateResult result)
        {
            if (components == null)
            {
                result.Warnings.Add("当前画布元件列表不可用。");
                return;
            }

            for (var i = 0; i < components.Count; i++)
            {
                var component = components[i];
                if (component == null || component.Definition == null)
                {
                    continue;
                }

                if (component.Terminals == null || component.Terminals.Count == 0)
                {
                    result.Warnings.Add(DisplayName(component) + " 没有可分析的端子。");
                    continue;
                }

                for (var terminalIndex = 0; terminalIndex < component.Terminals.Count; terminalIndex++)
                {
                    var terminal = component.Terminals[terminalIndex];
                    if (terminal == null || string.IsNullOrWhiteSpace(terminal.TerminalId))
                    {
                        result.Warnings.Add(DisplayName(component) + " 存在无法识别的端子。");
                        continue;
                    }

                    var key = TerminalKey(terminal);
                    if (terminalsByKey.ContainsKey(key))
                    {
                        result.Warnings.Add("检测到重复端子标识：" + FriendlyTerminalKey(key) + "。");
                        continue;
                    }

                    terminalsByKey.Add(key, terminal);
                    unionFind.Add(key);
                }
            }
        }

        private void AddWireConnections(
            IReadOnlyList<WireView> wires,
            TerminalUnionFind unionFind,
            CircuitStateResult result)
        {
            if (wires == null)
            {
                result.Warnings.Add("当前画布导线列表不可用。");
                return;
            }

            for (var i = 0; i < wires.Count; i++)
            {
                var wire = wires[i];
                if (wire == null || wire.StartTerminal == null || wire.EndTerminal == null)
                {
                    result.Warnings.Add("发现端点不完整的导线，已跳过。");
                    continue;
                }

                var startKey = TerminalKey(wire.StartTerminal);
                var endKey = TerminalKey(wire.EndTerminal);
                if (!terminalsByKey.ContainsKey(startKey) || !terminalsByKey.ContainsKey(endKey))
                {
                    result.Warnings.Add("导线端点无法对应到当前画布端子：" + FriendlyTerminalKey(startKey) + " -> " + FriendlyTerminalKey(endKey) + "。");
                    continue;
                }

                ConnectTerminalKeys(startKey, endKey, unionFind);
                wiredTerminalKeys.Add(startKey);
                wiredTerminalKeys.Add(endKey);
            }
        }

        private void AddInternalConnections(
            IReadOnlyList<CircuitComponent> components,
            TerminalUnionFind unionFind)
        {
            if (components == null)
            {
                return;
            }

            for (var i = 0; i < components.Count; i++)
            {
                var component = components[i];
                if (component == null || component.Definition == null)
                {
                    continue;
                }

                if (component.Definition.kind == ComponentKind.EnergyMeter || IsEnergyMeter(component))
                {
                    ConnectIfExists(component, "L_IN", "L_OUT", unionFind);
                    ConnectIfExists(component, "N_IN", "N_OUT", unionFind);
                    continue;
                }

                if (component.Definition.kind == ComponentKind.TwoWaySwitch || DefinitionContains(component, "TwoWay", "DoubleThrow", "双控"))
                {
                    ConnectIfExists(component, "L", component.IsClosed ? "L1" : "L2", unionFind);
                    ConnectIfExists(component, "COM", component.IsClosed ? "L1" : "L2", unionFind);
                    continue;
                }

                if (component.Definition.kind == ComponentKind.Breaker || IsBreaker(component))
                {
                    if (component.IsClosed)
                    {
                        ConnectKnownPairs(component, unionFind, true);
                    }

                    continue;
                }

                if (component.Definition.kind == ComponentKind.Fuse || DefinitionContains(component, "Fuse", "熔断器", "保险"))
                {
                    ConnectKnownPairs(component, unionFind, true);
                    continue;
                }

                if (IsHouseholdSwitch(component))
                {
                    if (component.IsClosed)
                    {
                        ConnectKnownPairs(component, unionFind, true);
                    }
                }
            }
        }

        private void AddPowerLabels(
            IReadOnlyList<CircuitComponent> components,
            TerminalUnionFind unionFind,
            Dictionary<string, HashSet<string>> rootLabels,
            Dictionary<string, List<string>> rootSources,
            CircuitStateResult result)
        {
            if (components == null)
            {
                return;
            }

            var reportedThreePhase = false;
            for (var i = 0; i < components.Count; i++)
            {
                var component = components[i];
                if (component == null || component.Definition == null || component.Definition.kind != ComponentKind.PowerSource)
                {
                    continue;
                }

                if (component.GetTerminal("L1") != null || component.Definition.sourcePhaseCount >= 3)
                {
                    result.ContainsUnsupportedThreePhaseCircuit = true;
                    if (!reportedThreePhase)
                    {
                        result.Infos.Add("当前电路包含三相工业元件，V0 暂不进行三相工业现象分析。");
                        reportedThreePhase = true;
                    }

                    continue;
                }

                var line = component.GetTerminal("L");
                var neutral = component.GetTerminal("N");
                if (line == null || neutral == null)
                {
                    result.Warnings.Add(DisplayName(component) + " 缺少 L 或 N 端子，无法传播单相电压标签。");
                    continue;
                }

                AddPowerLabel(line, VoltageL, unionFind, rootLabels, rootSources);
                AddPowerLabel(neutral, VoltageN, unionFind, rootLabels, rootSources);
                AddPowerLabel(component.GetTerminal("PE"), VoltagePE, unionFind, rootLabels, rootSources);
            }
        }

        private static void AddPowerLabel(
            TerminalView terminal,
            string label,
            TerminalUnionFind unionFind,
            Dictionary<string, HashSet<string>> rootLabels,
            Dictionary<string, List<string>> rootSources)
        {
            if (terminal == null)
            {
                return;
            }

            var terminalKey = TerminalKey(terminal);
            var root = unionFind.Find(terminalKey);
            if (root == null)
            {
                return;
            }

            if (!rootLabels.TryGetValue(root, out var labels))
            {
                labels = new HashSet<string>();
                rootLabels.Add(root, labels);
            }

            labels.Add(label);
            if (!rootSources.TryGetValue(root, out var sources))
            {
                sources = new List<string>();
                rootSources.Add(root, sources);
            }

            if (!sources.Contains(terminalKey))
            {
                sources.Add(terminalKey);
            }
        }

        private static void DetectPowerConflicts(
            Dictionary<string, HashSet<string>> rootLabels,
            CircuitStateResult result)
        {
            foreach (var pair in rootLabels)
            {
                var labels = pair.Value;
                if (labels.Contains(VoltageL) && labels.Contains(VoltageN))
                {
                    result.HasShortCircuit = true;
                    result.HasPowerConflict = true;
                    AddUnique(result.Errors, "检测到 L 与 N 位于同一导通节点，存在短路风险。");
                }

                if ((labels.Contains(VoltageL) || labels.Contains(VoltageN)) && labels.Contains(VoltagePE))
                {
                    result.HasPowerConflict = true;
                    AddUnique(result.Warnings, "检测到电源导体与 PE 存在连接，请检查接地或短路风险。");
                }
            }
        }

        private void BuildComponentStates(
            IReadOnlyList<CircuitComponent> components,
            TerminalUnionFind unionFind,
            Dictionary<string, HashSet<string>> rootLabels,
            Dictionary<string, List<string>> rootSources,
            CircuitStateResult result)
        {
            if (components == null)
            {
                return;
            }

            for (var i = 0; i < components.Count; i++)
            {
                var component = components[i];
                if (component == null || component.Definition == null)
                {
                    continue;
                }

                var info = new ComponentStateInfo
                {
                    InstanceId = SafeInstanceId(component),
                    DefinitionName = component.Definition.name,
                    DisplayName = friendlyNamesByInstanceId.TryGetValue(SafeInstanceId(component), out var friendlyName)
                        ? friendlyName
                        : DisplayName(component),
                    State = component.Definition.togglable ? (component.IsClosed ? "On" : "Off") : "Unknown"
                };

                for (var terminalIndex = 0; terminalIndex < component.Terminals.Count; terminalIndex++)
                {
                    var terminal = component.Terminals[terminalIndex];
                    if (terminal == null)
                    {
                        continue;
                    }

                    WriteActualTerminalState(terminal, info, unionFind, rootLabels, rootSources);
                }

                if (IsLamp(component))
                {
                    info.SummaryGroup = ComponentStateInfo.GroupLoad;
                    AnalyzeSinglePhaseLoad(component, info, result, false);
                }
                else if (IsFan(component))
                {
                    info.SummaryGroup = ComponentStateInfo.GroupLoad;
                    AnalyzeSinglePhaseLoad(component, info, result, true);
                }
                else if (component.Definition.kind == ComponentKind.TwoWaySwitch || DefinitionContains(component, "TwoWay", "DoubleThrow", "双控"))
                {
                    info.SummaryGroup = ComponentStateInfo.GroupControl;
                    info.IsTwoWaySwitch = true;
                    info.State = component.IsClosed ? "TwoWayL1" : "TwoWayL2";
                    info.ConductionExplanation = component.IsClosed ? "当前导通：COM → L1" : "当前导通：COM → L2";
                }
                else if (IsHouseholdSwitch(component))
                {
                    info.SummaryGroup = ComponentStateInfo.GroupControl;
                    info.IsHouseholdSwitch = true;
                    info.State = component.IsClosed ? "Closed" : "Open";
                    info.ConductionExplanation = component.IsClosed
                        ? "当前导通：" + FindFirstTerminalPair(component, " → ")
                        : "当前状态：" + FindFirstTerminalPair(component, " 与 ") + " 不导通";
                }
                else if (component.Definition.kind == ComponentKind.Breaker || IsBreaker(component))
                {
                    info.SummaryGroup = ComponentStateInfo.GroupControl;
                    info.IsBreaker = true;
                    info.State = component.IsClosed ? "Closed" : "Open";
                    info.ConductionExplanation = component.IsClosed
                        ? "当前导通：" + FindFirstTerminalPair(component, " → ")
                        : "当前状态：" + FindFirstTerminalPair(component, " 与 ") + " 不导通";
                    AnalyzeBreaker(component, info, result);
                }
                else if (component.Definition.kind == ComponentKind.Fuse || DefinitionContains(component, "Fuse", "熔断器", "保险"))
                {
                    info.SummaryGroup = ComponentStateInfo.GroupControl;
                    info.State = "Conducting";
                }
                else if (component.Definition.kind == ComponentKind.PowerSource)
                {
                    info.SummaryGroup = ComponentStateInfo.GroupOther;
                    info.State = "Normal";
                }
                else if (component.Definition.kind == ComponentKind.EnergyMeter || IsEnergyMeter(component))
                {
                    info.SummaryGroup = ComponentStateInfo.GroupOther;
                    info.State = AreMeterTerminalsWired(component) ? "Conducting" : "Incomplete";
                    info.Judgement = "V0.1.1 简化认为电能表 L_IN/L_OUT、N_IN/N_OUT 内部导通，不计算电能。";
                }

                result.Components.Add(info);
            }
        }

        private bool AreMeterTerminalsWired(CircuitComponent component)
        {
            var terminalIds = new[] { "L_IN", "L_OUT", "N_IN", "N_OUT" };
            for (var i = 0; i < terminalIds.Length; i++)
            {
                var terminal = component.GetTerminal(terminalIds[i]);
                if (terminal == null || !wiredTerminalKeys.Contains(TerminalKey(terminal)))
                {
                    return false;
                }
            }

            return true;
        }

        private void AnalyzeBreaker(CircuitComponent component, ComponentStateInfo info, CircuitStateResult result)
        {
            var inputIds = new[] { "IN", "L_IN", "P_IN", "P1_IN", "P2_IN", "P3_IN", "P4_IN" };
            var outputIds = new[] { "OUT", "L_OUT", "P_OUT", "P1_OUT", "P2_OUT", "P3_OUT", "P4_OUT" };
            info.BreakerInputHasSupply = HasVoltage(info, inputIds, VoltageL) || HasVoltage(info, inputIds, VoltageN);
            info.BreakerOutputHasSupply = HasVoltage(info, outputIds, VoltageL) || HasVoltage(info, outputIds, VoltageN);
            info.BreakerAllInputsHaveSupply = AllExistingTerminalsHaveVoltage(component, info, inputIds);
            info.BreakerHasCompleteExternalConnections =
                AllExistingTerminalsAreWired(component, inputIds) &&
                AllExistingTerminalsAreWired(component, outputIds);

            if (!info.BreakerInputHasSupply)
            {
                AddComponentWarning(info, result, "空开输入端未获得电源标签，请检查空开进线。");
            }
            else if (!component.IsClosed)
            {
                AddComponentInfo(info, result, "空气开关当前断开，输入侧电源标签不会传播到输出侧。");
            }
        }

        private bool AllExistingTerminalsAreWired(CircuitComponent component, string[] terminalIds)
        {
            var found = false;
            for (var i = 0; i < terminalIds.Length; i++)
            {
                var terminal = component.GetTerminal(terminalIds[i]);
                if (terminal == null)
                {
                    continue;
                }

                found = true;
                if (!wiredTerminalKeys.Contains(TerminalKey(terminal)))
                {
                    return false;
                }
            }

            return found;
        }

        private static bool AllExistingTerminalsHaveVoltage(
            CircuitComponent component,
            ComponentStateInfo info,
            string[] terminalIds)
        {
            var found = false;
            for (var i = 0; i < terminalIds.Length; i++)
            {
                if (component.GetTerminal(terminalIds[i]) == null)
                {
                    continue;
                }

                found = true;
                if (!info.TerminalVoltages.TryGetValue(terminalIds[i], out var voltage) ||
                    voltage == VoltageNone ||
                    voltage == VoltageConflict)
                {
                    return false;
                }
            }

            return found;
        }

        private static bool HasVoltage(ComponentStateInfo info, string[] terminalIds, string voltage)
        {
            for (var i = 0; i < terminalIds.Length; i++)
            {
                if (info.TerminalVoltages.TryGetValue(terminalIds[i], out var value) && value == voltage)
                {
                    return true;
                }
            }

            return false;
        }

        private static void FinalizeBreakerControlAnalysis(CircuitStateResult result)
        {
            var hasHouseholdSwitch = false;
            var hasValidClosedBreaker = false;
            var hasProperlyConnectedBreaker = false;
            var hasBreakerWithSuppliedInputs = false;
            var hasOpenSuppliedBreaker = false;
            var hasRunningLoad = false;
            var hasStoppedLoadWithoutLine = false;

            for (var i = 0; i < result.Components.Count; i++)
            {
                var component = result.Components[i];
                hasHouseholdSwitch |= component.IsHouseholdSwitch;
                hasValidClosedBreaker |= component.IsBreaker && component.State == "Closed" &&
                    component.BreakerInputHasSupply && component.BreakerOutputHasSupply;
                hasProperlyConnectedBreaker |= component.IsBreaker &&
                    component.BreakerHasCompleteExternalConnections &&
                    component.BreakerInputHasSupply;
                hasBreakerWithSuppliedInputs |= component.IsBreaker &&
                    component.BreakerAllInputsHaveSupply;
                hasOpenSuppliedBreaker |= component.IsBreaker && component.State == "Open" &&
                    component.BreakerInputHasSupply;
                hasRunningLoad |= component.SummaryGroup == ComponentStateInfo.GroupLoad &&
                    (component.State == "On" || component.State == "Running");
                hasStoppedLoadWithoutLine |= component.SummaryGroup == ComponentStateInfo.GroupLoad &&
                    component.State != "On" && component.State != "Running" &&
                    !HasVoltage(component, new[] { "L", "N" }, VoltageL);
            }

            result.HasHouseholdControlSwitch = hasHouseholdSwitch;
            result.HasValidClosedBreakerControl = hasValidClosedBreaker && hasRunningLoad;
            result.HasProperlyConnectedBreaker = hasProperlyConnectedBreaker;
            result.HasBreakerWithSuppliedInputs = hasBreakerWithSuppliedInputs;
            if (result.HasValidClosedBreakerControl && !hasHouseholdSwitch)
            {
                AddUnique(result.Infos, "当前负载由空气开关直接控制，可以实现通断。若本题是空开控制照明电路，该接法是合理的。");
            }
            else if (hasOpenSuppliedBreaker && hasStoppedLoadWithoutLine)
            {
                AddUnique(result.Warnings, "空气开关当前断开，火线 L 无法传播到负载 L 端，因此负载未通电。");
            }
        }

        private void WriteActualTerminalState(
            TerminalView terminal,
            ComponentStateInfo info,
            TerminalUnionFind unionFind,
            Dictionary<string, HashSet<string>> rootLabels,
            Dictionary<string, List<string>> rootSources)
        {
            var terminalKey = TerminalKey(terminal);
            var root = unionFind.Find(terminalKey);
            var sources = ResolveSources(root, rootSources);
            var voltage = ResolveRootVoltage(root, rootLabels);
            info.TerminalVoltages[terminal.TerminalId] = ResolveVoltageFromSources(voltage, sources);
            info.VoltageSources[terminal.TerminalId] = sources;
        }

        private static string ResolveVoltageFromSources(string voltage, string sources)
        {
            if (voltage == VoltageConflict || voltage == VoltageNone || string.IsNullOrWhiteSpace(sources))
            {
                return voltage;
            }

            var sourceLabels = new HashSet<string>();
            var sourceParts = sources.Split(',');
            for (var i = 0; i < sourceParts.Length; i++)
            {
                var source = sourceParts[i].Trim();
                if (source.EndsWith(".L", StringComparison.OrdinalIgnoreCase))
                {
                    sourceLabels.Add(VoltageL);
                }
                else if (source.EndsWith(".N", StringComparison.OrdinalIgnoreCase))
                {
                    sourceLabels.Add(VoltageN);
                }
                else if (source.EndsWith(".PE", StringComparison.OrdinalIgnoreCase))
                {
                    sourceLabels.Add(VoltagePE);
                }
            }

            if (sourceLabels.Count == 1)
            {
                foreach (var sourceLabel in sourceLabels)
                {
                    return sourceLabel;
                }
            }

            return sourceLabels.Count > 1 ? VoltageConflict : voltage;
        }

        private static void AnalyzeSinglePhaseLoad(
            CircuitComponent component,
            ComponentStateInfo info,
            CircuitStateResult result,
            bool isFan)
        {
            var line = VoltageAt(info, "L");
            var neutral = VoltageAt(info, "N");
            var runningState = isFan ? "Running" : "On";
            var stoppedState = isFan ? "Stopped" : "Off";
            var loadName = isFan ? "风扇" : "灯泡";

            if (component.GetTerminal("L") == null || component.GetTerminal("N") == null)
            {
                info.State = stoppedState;
                AddComponentError(info, result, loadName + "缺少 L 或 N 端子，无法判断供电状态。");
                return;
            }

            if (line == VoltageConflict || neutral == VoltageConflict)
            {
                info.State = stoppedState;
                info.Judgement = loadName + "所在回路存在电压冲突或短路风险。";
                AddComponentError(info, result, loadName + "所在回路存在电压冲突或短路风险。");
                return;
            }

            if (line == VoltageL && neutral == VoltageN)
            {
                info.State = runningState;
                info.Judgement = loadName + "获得有效 L-N 供电。";
                return;
            }

            if (line == VoltageN && neutral == VoltageL)
            {
                info.State = stoppedState;
                info.Judgement = loadName + " L/N 接反，当前系统按规范端子判断为" + (isFan ? "停止" : "不亮") + "。";
                AddComponentWarning(info, result, loadName + " L/N 接反。当前系统按规范端子判断，L 端应接火线、N 端应接零线，因此判断为" + (isFan ? "停止" : "不亮") + "。");
                AddComponentInfo(info, result, isFan
                    ? "风扇应按规范连接 L/N 端子。"
                    : "真实普通灯泡在部分情况下可能仍会发光，但接线不规范，实训系统按规范端子进行判定。");
                return;
            }

            info.State = stoppedState;
            var hasLine = line == VoltageL || neutral == VoltageL;
            var hasNeutral = line == VoltageN || neutral == VoltageN;
            if (!hasLine)
            {
                AddComponentWarning(info, result, loadName + "未获得火线 L。");
            }

            if (!hasNeutral)
            {
                AddComponentWarning(info, result, loadName + "缺少零线 N 回路。");
            }

            if (line != VoltageNone && line == neutral)
            {
                AddComponentWarning(info, result, loadName + "两端没有形成有效 L-N 电压差。");
            }

            if (!hasLine && !hasNeutral)
            {
                info.Judgement = loadName + "未获得完整 L-N 供电。";
            }
            else if (!hasLine)
            {
                info.Judgement = loadName + "缺少火线 L。";
            }
            else if (!hasNeutral)
            {
                info.Judgement = loadName + "缺少零线 N 回路。";
            }
            else
            {
                info.Judgement = loadName + "两端没有形成有效 L-N 电压差。";
            }
        }

        private static void AddComponentWarning(ComponentStateInfo info, CircuitStateResult result, string message)
        {
            AddUnique(info.Warnings, message);
            AddUnique(result.Warnings, info.DisplayName + "：" + message);
        }

        private static void AddComponentError(ComponentStateInfo info, CircuitStateResult result, string message)
        {
            AddUnique(info.Errors, message);
            AddUnique(result.Errors, info.DisplayName + "：" + message);
        }

        private static void AddComponentInfo(ComponentStateInfo info, CircuitStateResult result, string message)
        {
            AddUnique(info.Infos, message);
            AddUnique(result.Infos, info.DisplayName + "：" + message);
        }

        private static string ResolveRootVoltage(string root, Dictionary<string, HashSet<string>> rootLabels)
        {
            if (root == null || !rootLabels.TryGetValue(root, out var labels) || labels.Count == 0)
            {
                return VoltageNone;
            }

            if (labels.Count > 1)
            {
                return VoltageConflict;
            }

            foreach (var label in labels)
            {
                return label;
            }

            return VoltageNone;
        }

        private string ResolveSources(string root, Dictionary<string, List<string>> rootSources)
        {
            if (root == null || !rootSources.TryGetValue(root, out var sources))
            {
                return string.Empty;
            }

            var readableSources = new List<string>();
            for (var i = 0; i < sources.Count; i++)
            {
                var source = sources[i];
                var separator = source.LastIndexOf('.');
                var instanceId = separator > 0 ? source.Substring(0, separator) : source;
                var terminalId = separator > 0 ? source.Substring(separator + 1) : string.Empty;
                var readableName = friendlyNamesByInstanceId.TryGetValue(instanceId, out var value) ? value : ShortId(instanceId);
                readableSources.Add(string.IsNullOrWhiteSpace(terminalId) ? readableName : readableName + "." + terminalId);
            }

            return string.Join(", ", readableSources.ToArray());
        }

        private string FriendlyTerminalKey(string terminalKey)
        {
            if (string.IsNullOrWhiteSpace(terminalKey))
            {
                return "未知端子";
            }

            var separator = terminalKey.LastIndexOf('.');
            var instanceId = separator > 0 ? terminalKey.Substring(0, separator) : terminalKey;
            var terminalId = separator > 0 ? terminalKey.Substring(separator + 1) : string.Empty;
            var readableName = friendlyNamesByInstanceId.TryGetValue(instanceId, out var value) ? value : ShortId(instanceId);
            return string.IsNullOrWhiteSpace(terminalId) ? readableName : readableName + "." + terminalId;
        }

        private static string VoltageAt(ComponentStateInfo info, string terminalId)
        {
            return info.TerminalVoltages.TryGetValue(terminalId, out var value) ? value : VoltageNone;
        }

        private static bool IsHouseholdSwitch(CircuitComponent component)
        {
            return component.Definition.category == ComponentCategory.Household &&
                component.Definition.kind == ComponentKind.Switch &&
                !DefinitionContains(component, "TwoWay", "DoubleThrow", "双控");
        }

        private static bool IsBreaker(CircuitComponent component)
        {
            return DefinitionContains(component, "Breaker", "AirSwitch", "CircuitBreaker", "空开", "空气开关", "断路器");
        }

        private static bool IsEnergyMeter(CircuitComponent component)
        {
            return DefinitionContains(component, "EnergyMeter", "ElectricMeter", "电能表", "电表", "单相电能表");
        }

        private static bool IsLamp(CircuitComponent component)
        {
            return component.Definition.kind == ComponentKind.Lamp ||
                DefinitionContains(component, "Lamp", "Bulb", "Light", "灯泡", "照明");
        }

        private static bool IsFan(CircuitComponent component)
        {
            return component.Definition.kind == ComponentKind.Fan ||
                DefinitionContains(component, "Fan", "风扇", "电风扇");
        }

        private static bool DefinitionContains(CircuitComponent component, params string[] keywords)
        {
            if (component == null || keywords == null)
            {
                return false;
            }

            var definitionName = component.Definition != null ? component.Definition.name : string.Empty;
            var displayName = component.Definition != null ? component.Definition.displayName : string.Empty;
            var combined = definitionName + " " + displayName;

            for (var i = 0; i < keywords.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(keywords[i]) &&
                    combined.IndexOf(keywords[i], StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private void BuildLoadPathExplanations(
            IReadOnlyList<CircuitComponent> components,
            CircuitStateResult result)
        {
            if (components == null)
            {
                return;
            }

            var lineSources = new List<string>();
            var neutralSources = new List<string>();
            for (var i = 0; i < components.Count; i++)
            {
                var component = components[i];
                if (component == null || component.Definition == null ||
                    component.Definition.kind != ComponentKind.PowerSource ||
                    component.GetTerminal("L1") != null ||
                    component.Definition.sourcePhaseCount >= 3)
                {
                    continue;
                }

                AddTerminalKey(component.GetTerminal("L"), lineSources);
                AddTerminalKey(component.GetTerminal("N"), neutralSources);
            }

            for (var i = 0; i < components.Count; i++)
            {
                var component = components[i];
                if (component == null || (!IsLamp(component) && !IsFan(component)))
                {
                    continue;
                }

                var info = result.FindComponent(SafeInstanceId(component));
                if (info == null)
                {
                    continue;
                }

                var lineTerminal = component.GetTerminal("L");
                var neutralTerminal = component.GetTerminal("N");
                info.LinePathExplanation = BuildPathExplanation(lineSources, TerminalKey(lineTerminal), "火线 L", info.DisplayName + ".L", false);
                info.NeutralPathExplanation = BuildPathExplanation(neutralSources, TerminalKey(neutralTerminal), "零线 N", info.DisplayName + ".N", true);
            }
        }

        private string BuildPathExplanation(
            List<string> sourceKeys,
            string targetKey,
            string label,
            string targetName,
            bool reverseDisplay)
        {
            var path = FindShortestPath(sourceKeys, targetKey);
            if (path == null || path.Count == 0)
            {
                return label + "：未到达 " + targetName;
            }

            if (reverseDisplay)
            {
                path.Reverse();
            }

            return label + "：" + FormatPath(path);
        }

        private List<string> FindShortestPath(List<string> sourceKeys, string targetKey)
        {
            if (sourceKeys == null || string.IsNullOrWhiteSpace(targetKey))
            {
                return null;
            }

            var queue = new Queue<string>();
            var previous = new Dictionary<string, string>();
            for (var i = 0; i < sourceKeys.Count; i++)
            {
                var source = sourceKeys[i];
                if (string.IsNullOrWhiteSpace(source) || previous.ContainsKey(source))
                {
                    continue;
                }

                previous.Add(source, null);
                queue.Enqueue(source);
            }

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current == targetKey)
                {
                    var path = new List<string>();
                    while (current != null)
                    {
                        path.Add(current);
                        current = previous[current];
                    }

                    path.Reverse();
                    return path;
                }

                if (!connectionGraph.TryGetValue(current, out var neighbors))
                {
                    continue;
                }

                foreach (var neighbor in neighbors)
                {
                    if (previous.ContainsKey(neighbor))
                    {
                        continue;
                    }

                    previous.Add(neighbor, current);
                    queue.Enqueue(neighbor);
                }
            }

            return null;
        }

        private string FormatPath(List<string> terminalPath)
        {
            var parts = new List<string>();
            for (var i = 0; i < terminalPath.Count; i++)
            {
                var key = terminalPath[i];
                var separator = key.LastIndexOf('.');
                var instanceId = separator > 0 ? key.Substring(0, separator) : key;
                var isEndpoint = i == 0 || i == terminalPath.Count - 1;
                var text = isEndpoint ? FriendlyTerminalKey(key) :
                    friendlyNamesByInstanceId.TryGetValue(instanceId, out var name) ? name : ShortId(instanceId);
                if (parts.Count == 0 || parts[parts.Count - 1] != text)
                {
                    parts.Add(text);
                }
            }

            return string.Join(" → ", parts);
        }

        private static void AddTerminalKey(TerminalView terminal, List<string> keys)
        {
            if (terminal != null)
            {
                keys.Add(TerminalKey(terminal));
            }
        }

        private static string FindFirstTerminalPair(CircuitComponent component, string separator)
        {
            var pairs = new[]
            {
                new[] { "L", "L1" },
                new[] { "11", "12" },
                new[] { "IN", "OUT" },
                new[] { "L_IN", "L_OUT" },
                new[] { "P_IN", "P_OUT" },
                new[] { "P1_IN", "P1_OUT" }
            };

            for (var i = 0; i < pairs.Length; i++)
            {
                if (component.GetTerminal(pairs[i][0]) != null && component.GetTerminal(pairs[i][1]) != null)
                {
                    return pairs[i][0] + separator + pairs[i][1];
                }
            }

            return "输入端" + separator + "输出端";
        }

        private void BuildFriendlyNames(IReadOnlyList<CircuitComponent> components)
        {
            if (components == null)
            {
                return;
            }

            var totals = new Dictionary<string, int>();
            for (var i = 0; i < components.Count; i++)
            {
                var component = components[i];
                if (component == null || component.Definition == null)
                {
                    continue;
                }

                var name = DisplayName(component);
                totals[name] = totals.TryGetValue(name, out var count) ? count + 1 : 1;
            }

            var indexes = new Dictionary<string, int>();
            for (var i = 0; i < components.Count; i++)
            {
                var component = components[i];
                if (component == null || component.Definition == null)
                {
                    continue;
                }

                var name = DisplayName(component);
                indexes[name] = indexes.TryGetValue(name, out var index) ? index + 1 : 1;
                friendlyNamesByInstanceId[SafeInstanceId(component)] = totals[name] > 1 ? name + " #" + indexes[name] : name;
            }
        }

        private static string ShortId(string instanceId)
        {
            if (string.IsNullOrWhiteSpace(instanceId))
            {
                return "未知元件";
            }

            return instanceId.Length <= 8 ? instanceId : instanceId.Substring(0, 8);
        }

        private bool ConnectKnownPairs(CircuitComponent component, TerminalUnionFind unionFind, bool fallbackToFirstPair)
        {
            var connected = false;
            connected |= ConnectIfExists(component, "L", "L1", unionFind);
            connected |= ConnectIfExists(component, "11", "12", unionFind);
            connected |= ConnectIfExists(component, "IN", "OUT", unionFind);
            connected |= ConnectIfExists(component, "L_IN", "L_OUT", unionFind);
            connected |= ConnectIfExists(component, "N_IN", "N_OUT", unionFind);
            connected |= ConnectIfExists(component, "P_IN", "P_OUT", unionFind);
            connected |= ConnectIfExists(component, "P1_IN", "P1_OUT", unionFind);
            connected |= ConnectIfExists(component, "P2_IN", "P2_OUT", unionFind);
            connected |= ConnectIfExists(component, "P3_IN", "P3_OUT", unionFind);
            connected |= ConnectIfExists(component, "P4_IN", "P4_OUT", unionFind);

            if (!connected && fallbackToFirstPair && component.Terminals != null && component.Terminals.Count == 2)
            {
                ConnectTerminalKeys(TerminalKey(component.Terminals[0]), TerminalKey(component.Terminals[1]), unionFind);
                connected = true;
            }

            return connected;
        }

        private bool ConnectIfExists(
            CircuitComponent component,
            string terminalA,
            string terminalB,
            TerminalUnionFind unionFind)
        {
            var a = component != null ? component.GetTerminal(terminalA) : null;
            var b = component != null ? component.GetTerminal(terminalB) : null;
            if (a == null || b == null)
            {
                return false;
            }

            ConnectTerminalKeys(TerminalKey(a), TerminalKey(b), unionFind);
            return true;
        }

        private void ConnectTerminalKeys(string a, string b, TerminalUnionFind unionFind)
        {
            unionFind.Union(a, b);
            AddGraphEdge(a, b);
        }

        private void AddGraphEdge(string a, string b)
        {
            if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
            {
                return;
            }

            if (!connectionGraph.TryGetValue(a, out var from))
            {
                from = new HashSet<string>();
                connectionGraph.Add(a, from);
            }

            if (!connectionGraph.TryGetValue(b, out var to))
            {
                to = new HashSet<string>();
                connectionGraph.Add(b, to);
            }

            from.Add(b);
            to.Add(a);
        }

        private static string TerminalKey(TerminalView terminal)
        {
            return terminal == null
                ? string.Empty
                : SafeInstanceId(terminal.Owner) + "." + terminal.TerminalId;
        }

        private static string SafeInstanceId(CircuitComponent component)
        {
            if (component == null)
            {
                return "unknown";
            }

            return string.IsNullOrWhiteSpace(component.InstanceId)
                ? component.GetInstanceID().ToString()
                : component.InstanceId;
        }

        private static string DisplayName(CircuitComponent component)
        {
            if (component == null || component.Definition == null)
            {
                return "未知元件";
            }

            return string.IsNullOrWhiteSpace(component.Definition.displayName)
                ? component.Definition.name
                : component.Definition.displayName.Replace("\n", " ");
        }

        private static void AddUnique(List<string> list, string value)
        {
            if (list != null && !string.IsNullOrWhiteSpace(value) && !list.Contains(value))
            {
                list.Add(value);
            }
        }
    }

    public sealed class CircuitStateResult
    {
        public readonly List<ComponentStateInfo> Components = new List<ComponentStateInfo>();
        public readonly List<string> Errors = new List<string>();
        public readonly List<string> Warnings = new List<string>();
        public readonly List<string> Infos = new List<string>();

        public bool HasShortCircuit;
        public bool HasPowerConflict;
        public bool ContainsUnsupportedThreePhaseCircuit;
        public bool HasHouseholdControlSwitch;
        public bool HasValidClosedBreakerControl;
        public bool HasProperlyConnectedBreaker;
        public bool HasBreakerWithSuppliedInputs;

        public ComponentStateInfo FindComponent(string instanceId)
        {
            return Components.Find(c => c.InstanceId == instanceId);
        }

        public string ToReadableText(bool includeDebug = false)
        {
            var builder = new StringBuilder();
            builder.AppendLine("【通用现象分析 V0.1.1】");

            if (ContainsUnsupportedThreePhaseCircuit)
            {
                builder.AppendLine("当前电路包含三相工业元件，V0.1.1 暂不进行三相工业现象分析。");
                return builder.ToString().TrimEnd();
            }

            var loadCount = CountGroup(ComponentStateInfo.GroupLoad);
            var controlCount = CountGroup(ComponentStateInfo.GroupControl);
            var workingLoadCount = CountWorkingLoads();
            builder.AppendLine();
            builder.AppendLine("一、总体结论");
            builder.AppendLine(HasShortCircuit ? "- 检测到 L/N 短路风险。" : "- 未发现 L/N 短路。");
            builder.AppendLine("- 共识别 " + loadCount + " 个负载、" + controlCount + " 个控制/保护元件。");
            builder.AppendLine("- 当前有 " + workingLoadCount + " 个负载正常工作，" + (loadCount - workingLoadCount) + " 个负载未工作。");

            AppendComponentGroup(builder, "二、负载状态", ComponentStateInfo.GroupLoad);
            AppendComponentGroup(builder, "三、控制与保护元件", ComponentStateInfo.GroupControl);
            AppendComponentGroup(builder, "四、电源与其他元件", ComponentStateInfo.GroupOther);

            AppendMessages(builder, "五、错误", Errors);
            AppendMessages(builder, "六、警告", Warnings);
            AppendMessages(builder, "七、教学提示", Infos);
            if (includeDebug)
            {
                AppendDebugTerminalLabels(builder);
            }

            return builder.ToString().TrimEnd();
        }

        private int CountGroup(string group)
        {
            var count = 0;
            for (var i = 0; i < Components.Count; i++)
            {
                if (Components[i].SummaryGroup == group)
                {
                    count++;
                }
            }

            return count;
        }

        private int CountWorkingLoads()
        {
            var count = 0;
            for (var i = 0; i < Components.Count; i++)
            {
                if (Components[i].SummaryGroup == ComponentStateInfo.GroupLoad &&
                    (Components[i].State == "On" || Components[i].State == "Running"))
                {
                    count++;
                }
            }

            return count;
        }

        private void AppendComponentGroup(StringBuilder builder, string title, string group)
        {
            var index = 1;
            builder.AppendLine();
            builder.AppendLine(title);
            for (var i = 0; i < Components.Count; i++)
            {
                var component = Components[i];
                if (component.SummaryGroup != group)
                {
                    continue;
                }

                builder.AppendLine(index + ". " + component.DisplayName + "：" + ReadableState(component.State));
                if (!string.IsNullOrWhiteSpace(component.ConductionExplanation))
                {
                    builder.AppendLine("   - " + component.ConductionExplanation);
                }

                if (component.IsTwoWaySwitch)
                {
                    AppendTerminal(builder, component, "L");
                    AppendTerminal(builder, component, "L1");
                    AppendTerminal(builder, component, "L2");
                }
                else
                {
                    foreach (var terminal in component.TerminalVoltages)
                    {
                        AppendTerminal(builder, component, terminal.Key);
                    }
                }

                if (!string.IsNullOrWhiteSpace(component.Judgement))
                {
                    builder.AppendLine("   - 判断：" + component.Judgement);
                }

                if (component.SummaryGroup == ComponentStateInfo.GroupLoad)
                {
                    builder.AppendLine("   - 供电路径：");
                    builder.AppendLine("     " + component.LinePathExplanation);
                    builder.AppendLine("     " + component.NeutralPathExplanation);
                }

                index++;
            }

            if (index == 1)
            {
                builder.AppendLine("- 无");
            }
        }

        private static void AppendTerminal(StringBuilder builder, ComponentStateInfo component, string terminalId)
        {
            if (!component.TerminalVoltages.TryGetValue(terminalId, out var voltage))
            {
                return;
            }

            component.VoltageSources.TryGetValue(terminalId, out var source);
            var displayTerminalId = component.IsTwoWaySwitch && terminalId == "L"
                ? "COM"
                : component.IsBreaker
                    ? BreakerTerminalLabel(terminalId)
                    : terminalId;
            builder.Append("   - ").Append(displayTerminalId).Append("端：").Append(voltage);
            if (!string.IsNullOrWhiteSpace(source))
            {
                builder.Append("，来源：").Append(source);
            }

            builder.AppendLine();
        }

        private static string BreakerTerminalLabel(string terminalId)
        {
            if (terminalId == "IN" || terminalId.EndsWith("_IN", StringComparison.Ordinal))
            {
                return terminalId == "IN" ? "输入" : terminalId.Replace("_IN", " 输入");
            }

            if (terminalId == "OUT" || terminalId.EndsWith("_OUT", StringComparison.Ordinal))
            {
                return terminalId == "OUT" ? "输出" : terminalId.Replace("_OUT", " 输出");
            }

            return terminalId;
        }

        private static void AppendMessages(StringBuilder builder, string title, List<string> messages)
        {
            builder.AppendLine();
            builder.AppendLine(title);
            if (messages == null || messages.Count == 0)
            {
                builder.AppendLine("- 无");
                return;
            }

            for (var i = 0; i < messages.Count; i++)
            {
                builder.AppendLine("- " + messages[i]);
            }
        }

        private void AppendDebugTerminalLabels(StringBuilder builder)
        {
            builder.AppendLine();
            builder.AppendLine("【端子标签调试】");
            for (var i = 0; i < Components.Count; i++)
            {
                var component = Components[i];
                foreach (var terminal in component.TerminalVoltages)
                {
                    builder.AppendLine(component.DisplayName + "." + terminal.Key + " = " + terminal.Value);
                }
            }
        }

        private static string ReadableState(string state)
        {
            switch (state)
            {
                case "On":
                    return "亮";
                case "Off":
                    return "不亮";
                case "Running":
                    return "运行";
                case "Stopped":
                    return "停止";
                case "Closed":
                    return "闭合";
                case "Open":
                    return "断开";
                case "Normal":
                    return "正常";
                case "Conducting":
                    return "导通";
                case "Incomplete":
                    return "接线不完整";
                case "TwoWayL1":
                    return "COM→L1 导通";
                case "TwoWayL2":
                    return "COM→L2 导通";
                default:
                    return state;
            }
        }
    }

    public sealed class ComponentStateInfo
    {
        public const string GroupLoad = "Load";
        public const string GroupControl = "Control";
        public const string GroupOther = "Other";

        public string InstanceId;
        public string DefinitionName;
        public string DisplayName;
        public string State;
        public string SummaryGroup;
        public string Judgement;
        public string ConductionExplanation;
        public string LinePathExplanation;
        public string NeutralPathExplanation;
        public bool IsTwoWaySwitch;
        public bool IsHouseholdSwitch;
        public bool IsBreaker;
        public bool BreakerInputHasSupply;
        public bool BreakerOutputHasSupply;
        public bool BreakerHasCompleteExternalConnections;
        public bool BreakerAllInputsHaveSupply;

        public readonly Dictionary<string, string> TerminalVoltages = new Dictionary<string, string>();
        public readonly Dictionary<string, string> VoltageSources = new Dictionary<string, string>();
        public readonly List<string> Warnings = new List<string>();
        public readonly List<string> Errors = new List<string>();
        public readonly List<string> Infos = new List<string>();
    }

    internal sealed class TerminalUnionFind
    {
        private readonly Dictionary<string, string> parents = new Dictionary<string, string>();
        private readonly Dictionary<string, int> ranks = new Dictionary<string, int>();

        public void Add(string item)
        {
            if (string.IsNullOrWhiteSpace(item) || parents.ContainsKey(item))
            {
                return;
            }

            parents.Add(item, item);
            ranks.Add(item, 0);
        }

        public string Find(string item)
        {
            if (string.IsNullOrWhiteSpace(item) || !parents.ContainsKey(item))
            {
                return null;
            }

            var parent = parents[item];
            if (parent != item)
            {
                parents[item] = Find(parent);
            }

            return parents[item];
        }

        public void Union(string a, string b)
        {
            var rootA = Find(a);
            var rootB = Find(b);
            if (rootA == null || rootB == null || rootA == rootB)
            {
                return;
            }

            var rankA = ranks[rootA];
            var rankB = ranks[rootB];
            if (rankA < rankB)
            {
                parents[rootA] = rootB;
            }
            else if (rankA > rankB)
            {
                parents[rootB] = rootA;
            }
            else
            {
                parents[rootB] = rootA;
                ranks[rootA] = rankA + 1;
            }
        }
    }
}
