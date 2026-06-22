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
    }
}
