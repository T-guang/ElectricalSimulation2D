namespace ElectricalSim.Practice.Netlist
{
    public sealed class PracticeNetlistTerminal
    {
        public string ComponentId { get; }
        public string DefinitionName { get; }
        public string DisplayName { get; }
        public string TerminalId { get; }
        public string TerminalKey { get; }

        public PracticeNetlistTerminal(string componentId, string definitionName, string displayName, string terminalId)
        {
            ComponentId = componentId;
            DefinitionName = definitionName;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? definitionName : displayName;
            TerminalId = terminalId;
            TerminalKey = MakeKey(componentId, terminalId);
        }

        public static string MakeKey(string componentId, string terminalId)
        {
            return (componentId ?? string.Empty) + "." + (terminalId ?? string.Empty);
        }
    }
}
