namespace ElectricalSim.Core
{
    public static class ParameterKeys
    {
        public const string SourceVoltage = "sourceVoltage";
        public const string SourceLineVoltage = "sourceLineVoltage";
        public const string Voltage = "voltage";
        public const string LineVoltage = "lineVoltage";

        public const string RatedVoltage = "ratedVoltage";
        public const string RatedPower = "ratedPower";
        public const string RatedCurrent = "ratedCurrent";

        public const string Efficiency = "efficiency";
        public const string PowerFactor = "powerFactor";

        public const string SettingCurrent = "settingCurrent";
        public const string DelaySeconds = "delaySeconds";

        public const string Power = "power";
        public const string Current = "current";
    }

    public static class ParameterAliases
    {
        public static readonly string[] SourceVoltage =
        {
            ParameterKeys.SourceVoltage,
            ParameterKeys.Voltage
        };

        public static readonly string[] SourceLineVoltage =
        {
            ParameterKeys.SourceLineVoltage,
            ParameterKeys.LineVoltage
        };

        public static string GetCanonicalKey(string key)
        {
            if (IsAliasOf(key, SourceVoltage))
            {
                return ParameterKeys.SourceVoltage;
            }

            if (IsAliasOf(key, SourceLineVoltage))
            {
                return ParameterKeys.SourceLineVoltage;
            }

            return key;
        }

        public static string[] GetAliasesIncludingSelf(string key)
        {
            if (IsAliasOf(key, SourceVoltage))
            {
                return SourceVoltage;
            }

            if (IsAliasOf(key, SourceLineVoltage))
            {
                return SourceLineVoltage;
            }

            return new[] { key };
        }

        private static bool IsAliasOf(string key, string[] aliases)
        {
            if (string.IsNullOrWhiteSpace(key) || aliases == null)
            {
                return false;
            }

            for (var i = 0; i < aliases.Length; i++)
            {
                if (string.Equals(key, aliases[i], System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
