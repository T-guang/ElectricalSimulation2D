namespace ElectricalSim.Core
{
    public enum VisualPrefabStateMode
    {
        Static,
        IsClosed,
        IsEnergized,
        ContactorEnergized,
        TimerPhase,
        MotorRunning
    }

    public sealed class VisualPrefabConfig
    {
        public string DefinitionName { get; }
        public string PrefabPath { get; }
        public string DefaultSpritePath { get; }
        public string ActiveSpritePath { get; }
        public VisualPrefabStateMode StateMode { get; }
        public bool ActiveWhenClosed { get; }
        public bool UseTransparentTerminalView { get; }
        public bool HideLegacyTerminalLabel { get; }
        public bool DisableLegacyTerminalOffset { get; }
        public bool HasOperationHitArea { get; }
        public bool ShowTerminalDebugMarkers { get; }

        public VisualPrefabConfig(
            string definitionName,
            string prefabPath,
            string defaultSpritePath = null,
            string activeSpritePath = null,
            VisualPrefabStateMode stateMode = VisualPrefabStateMode.Static,
            bool activeWhenClosed = true,
            bool useTransparentTerminalView = true,
            bool hideLegacyTerminalLabel = true,
            bool disableLegacyTerminalOffset = true,
            bool hasOperationHitArea = false,
            bool showTerminalDebugMarkers = false)
        {
            DefinitionName = definitionName;
            PrefabPath = prefabPath;
            DefaultSpritePath = defaultSpritePath;
            ActiveSpritePath = activeSpritePath;
            StateMode = stateMode;
            ActiveWhenClosed = activeWhenClosed;
            UseTransparentTerminalView = useTransparentTerminalView;
            HideLegacyTerminalLabel = hideLegacyTerminalLabel;
            DisableLegacyTerminalOffset = disableLegacyTerminalOffset;
            HasOperationHitArea = hasOperationHitArea;
            ShowTerminalDebugMarkers = showTerminalDebugMarkers;
        }
    }
}
