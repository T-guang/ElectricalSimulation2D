using System;

namespace ElectricalSim.AI
{
    public interface IAIAssistantService
    {
        void Ask(string userQuestion, string circuitSummary, Action<string> onSuccess, Action<string> onError);
    }
}
