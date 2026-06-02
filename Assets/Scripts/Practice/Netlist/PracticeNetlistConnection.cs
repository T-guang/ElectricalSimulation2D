namespace ElectricalSim.Practice.Netlist
{
    public sealed class PracticeNetlistConnection
    {
        public string StartComponentId { get; }
        public string StartTerminalId { get; }
        public string EndComponentId { get; }
        public string EndTerminalId { get; }
        public string StartKey { get; }
        public string EndKey { get; }

        public PracticeNetlistConnection(string startComponentId, string startTerminalId, string endComponentId, string endTerminalId)
        {
            StartComponentId = startComponentId;
            StartTerminalId = startTerminalId;
            EndComponentId = endComponentId;
            EndTerminalId = endTerminalId;
            StartKey = PracticeNetlistTerminal.MakeKey(startComponentId, startTerminalId);
            EndKey = PracticeNetlistTerminal.MakeKey(endComponentId, endTerminalId);
        }

        public string GetUndirectedKey()
        {
            return string.CompareOrdinal(StartKey, EndKey) <= 0 ? StartKey + "<->" + EndKey : EndKey + "<->" + StartKey;
        }
    }
}
