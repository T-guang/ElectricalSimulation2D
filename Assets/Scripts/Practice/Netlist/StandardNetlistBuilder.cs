using ElectricalSim.Templates;

namespace ElectricalSim.Practice.Netlist
{
    public static class StandardNetlistBuilder
    {
        public static PracticeNetlist Build(CircuitTemplateDto template)
        {
            var netlist = new PracticeNetlist();
            if (template == null)
            {
                return netlist;
            }

            if (template.components != null)
            {
                foreach (var component in template.components)
                {
                    if (component == null)
                    {
                        continue;
                    }

                    netlist.AddComponent(component.instanceId, component.definitionName, component.definitionName);
                }
            }

            if (template.wires != null)
            {
                foreach (var wire in template.wires)
                {
                    if (wire == null)
                    {
                        continue;
                    }

                    netlist.AddConnection(wire.startComponentId, wire.startTerminalId, wire.endComponentId, wire.endTerminalId);
                }
            }

            return netlist;
        }
    }
}
