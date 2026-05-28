using System;

namespace ElectricalSim.AI
{
    public sealed class AIAssistantConfig
    {
        private const string DefaultModel = "gpt-4o-mini";

        public string Endpoint { get; }
        public string ApiKey { get; }
        public string Model { get; }

        public bool IsConfigured => !string.IsNullOrWhiteSpace(Endpoint) && !string.IsNullOrWhiteSpace(ApiKey);

        public AIAssistantConfig(string endpoint, string apiKey, string model)
        {
            Endpoint = NormalizeEndpoint(endpoint);
            ApiKey = apiKey ?? string.Empty;
            Model = NormalizeModel(model);
        }

        public static AIAssistantConfig LoadDefault()
        {
            // Do not hard-code API keys in the Unity client. Use environment variables for local testing only.
            var endpoint = ReadEnvironmentValue("ELECTRICAL_AI_ENDPOINT");
            var apiKey = ReadEnvironmentValue("ELECTRICAL_AI_API_KEY");
            var model = ReadEnvironmentValue("ELECTRICAL_AI_MODEL");
            return new AIAssistantConfig(endpoint, apiKey, model);
        }

        private static string ReadEnvironmentValue(string name)
        {
            var value = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            value = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            return Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Machine) ?? string.Empty;
        }

        private static string NormalizeEndpoint(string endpoint)
        {
            endpoint = (endpoint ?? string.Empty).Trim().TrimEnd('/');
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                return string.Empty;
            }

            if (endpoint.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
            {
                return endpoint;
            }

            return endpoint + "/chat/completions";
        }

        private static string NormalizeModel(string model)
        {
            model = (model ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(model))
            {
                return DefaultModel;
            }

            var separators = new[] { '?', ',', ';', '?', '?', '\n', '\r', '\t' };
            var first = model.Split(separators, StringSplitOptions.RemoveEmptyEntries)[0].Trim();
            return string.IsNullOrWhiteSpace(first) ? DefaultModel : first;
        }
    }
}
