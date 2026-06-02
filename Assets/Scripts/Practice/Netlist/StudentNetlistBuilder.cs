using ElectricalSim.Core;

namespace ElectricalSim.Practice.Netlist
{
    public static class StudentNetlistBuilder
    {
        public static PracticeNetlist Build(WorkspaceController workspace)
        {
            var netlist = new PracticeNetlist();
            if (workspace == null)
            {
                return netlist;
            }

            foreach (var component in workspace.Components)
            {
                if (component == null || component.Definition == null)
                {
                    continue;
                }

                netlist.AddComponent(component.InstanceId, component.Definition.name, CleanDisplayName(component.Definition.displayName, component.Definition.name));
                foreach (var terminal in component.Terminals)
                {
                    if (terminal != null)
                    {
                        netlist.AddTerminal(component.InstanceId, terminal.TerminalId);
                    }
                }
            }

            if (workspace.WireManager != null)
            {
                foreach (var wire in workspace.WireManager.Wires)
                {
                    if (wire == null || wire.StartTerminal == null || wire.EndTerminal == null)
                    {
                        continue;
                    }

                    var startOwner = wire.StartTerminal.Owner;
                    var endOwner = wire.EndTerminal.Owner;
                    if (startOwner == null || endOwner == null)
                    {
                        continue;
                    }

                    netlist.AddConnection(startOwner.InstanceId, wire.StartTerminal.TerminalId, endOwner.InstanceId, wire.EndTerminal.TerminalId);
                }
            }

            return netlist;
        }

        private static string CleanDisplayName(string displayName, string fallback)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                return fallback;
            }

            return displayName.Replace("\n", "").Replace("\r", "");
        }
    }
}
