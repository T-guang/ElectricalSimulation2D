using System.Collections.Generic;
using System.Linq;

namespace ElectricalSim.Practice.Netlist
{
    public sealed class PracticeNetlistComponent
    {
        public string ComponentId { get; }
        public string DefinitionName { get; }
        public string DisplayName { get; }

        public PracticeNetlistComponent(string componentId, string definitionName, string displayName)
        {
            ComponentId = componentId;
            DefinitionName = definitionName;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? definitionName : displayName;
        }
    }

    public sealed class PracticeNetlist
    {
        private readonly Dictionary<string, PracticeNetlistComponent> components = new Dictionary<string, PracticeNetlistComponent>();
        private readonly Dictionary<string, PracticeNetlistTerminal> terminals = new Dictionary<string, PracticeNetlistTerminal>();
        private readonly List<PracticeNetlistConnection> directConnections = new List<PracticeNetlistConnection>();
        private readonly UnionFind<string> nodeUnion = new UnionFind<string>();

        public IReadOnlyDictionary<string, PracticeNetlistComponent> Components => components;
        public IReadOnlyDictionary<string, PracticeNetlistTerminal> Terminals => terminals;
        public IReadOnlyList<PracticeNetlistConnection> DirectConnections => directConnections;

        public void AddComponent(string componentId, string definitionName, string displayName)
        {
            if (string.IsNullOrWhiteSpace(componentId) || components.ContainsKey(componentId))
            {
                return;
            }

            components[componentId] = new PracticeNetlistComponent(componentId, definitionName, displayName);
        }

        public PracticeNetlistTerminal AddTerminal(string componentId, string terminalId)
        {
            if (string.IsNullOrWhiteSpace(componentId) || string.IsNullOrWhiteSpace(terminalId))
            {
                return null;
            }

            components.TryGetValue(componentId, out var component);
            var definitionName = component != null ? component.DefinitionName : string.Empty;
            var displayName = component != null ? component.DisplayName : definitionName;
            var terminal = new PracticeNetlistTerminal(componentId, definitionName, displayName, terminalId);
            terminals[terminal.TerminalKey] = terminal;
            nodeUnion.Add(terminal.TerminalKey);
            return terminal;
        }

        public void AddConnection(string startComponentId, string startTerminalId, string endComponentId, string endTerminalId)
        {
            var start = AddTerminal(startComponentId, startTerminalId);
            var end = AddTerminal(endComponentId, endTerminalId);
            if (start == null || end == null)
            {
                return;
            }

            var connection = new PracticeNetlistConnection(startComponentId, startTerminalId, endComponentId, endTerminalId);
            directConnections.Add(connection);
            nodeUnion.Union(connection.StartKey, connection.EndKey);
        }

        public bool AreConnected(string firstTerminalKey, string secondTerminalKey)
        {
            return nodeUnion.AreConnected(firstTerminalKey, secondTerminalKey);
        }

        public IReadOnlyList<IReadOnlyList<string>> GetEquivalentNodeGroups()
        {
            return nodeUnion.GetGroups();
        }

        public string DescribeTerminal(string terminalKey)
        {
            if (terminals.TryGetValue(terminalKey, out var terminal))
            {
                return terminal.DisplayName + " " + terminal.TerminalId;
            }

            return terminalKey;
        }

        public IEnumerable<IGrouping<string, PracticeNetlistComponent>> GroupComponentsByDefinition()
        {
            return components.Values.GroupBy(c => c.DefinitionName);
        }
    }
}
