using System;
using System.Collections.Generic;

namespace ElectricalSim.Core
{
    public static class VisualPrefabRegistry
    {
        private static readonly Dictionary<string, VisualPrefabConfig> Configs = new Dictionary<string, VisualPrefabConfig>(StringComparer.Ordinal)
        {
            {
                "Button_Start_NO",
                new VisualPrefabConfig(
                    "Button_Start_NO",
                    "Assets/Prefab/Button_Start_NO_Visual.prefab",
                    "Assets/Art/Components/Button_Start_NO_Default.png",
                    "Assets/Art/Components/Button_Start_NO_Pressed.png",
                    VisualPrefabStateMode.IsClosed,
                    activeWhenClosed: true,
                    hasOperationHitArea: true)
            },
            {
                "Button_Stop_NC",
                new VisualPrefabConfig(
                    "Button_Stop_NC",
                    "Assets/Prefab/Button_Stop_NC_Visual.prefab",
                    "Assets/Art/Components/Button_Stop_NC_Default.png",
                    "Assets/Art/Components/Button_Stop_NC_Pressed.png",
                    VisualPrefabStateMode.IsClosed,
                    activeWhenClosed: false,
                    hasOperationHitArea: true)
            },
            {
                "Button_Compound_SB",
                new VisualPrefabConfig(
                    "Button_Compound_SB",
                    "Assets/Prefab/Button_Compound_SB_Visual.prefab",
                    "Assets/Art/Components/Button_Compound_SB_Default.png",
                    "Assets/Art/Components/Button_Compound_SB_Pressed.png",
                    VisualPrefabStateMode.IsClosed,
                    activeWhenClosed: true,
                    hasOperationHitArea: true)
            },
            {
                "Button_Compound_Green_SB",
                new VisualPrefabConfig(
                    "Button_Compound_Green_SB",
                    "Assets/Prefab/Button_Compound_Green_SB_Visual.prefab",
                    "Assets/Art/Components/Button_Compound_Green_SB_Default.png",
                    "Assets/Art/Components/Button_Compound_Green_SB_Pressed.png",
                    VisualPrefabStateMode.IsClosed,
                    activeWhenClosed: true,
                    hasOperationHitArea: true)
            },
            {
                "Button_SelfLock_SB",
                new VisualPrefabConfig(
                    "Button_SelfLock_SB",
                    "Assets/Prefab/Button_SelfLock_SB_Visual.prefab",
                    "Assets/Art/Components/Button_SelfLock_SB_Default.png",
                    "Assets/Art/Components/Button_SelfLock_SB_Locked.png",
                    VisualPrefabStateMode.IsClosed,
                    activeWhenClosed: true,
                    hasOperationHitArea: true)
            },
            {
                "Button_SelfLock_Green_SB",
                new VisualPrefabConfig(
                    "Button_SelfLock_Green_SB",
                    "Assets/Prefab/Button_SelfLock_Green_SB_Visual.prefab",
                    "Assets/Art/Components/Button_SelfLock_Green_SB_Default.png",
                    "Assets/Art/Components/Button_SelfLock_Green_SB_Locked.png",
                    VisualPrefabStateMode.IsClosed,
                    activeWhenClosed: true,
                    hasOperationHitArea: true)
            },
            {
                "AC_ThreePhase_Power",
                new VisualPrefabConfig(
                    "AC_ThreePhase_Power",
                    "Assets/Prefab/AC_ThreePhase_Power_Visual.prefab",
                    "Assets/Art/Components/AC_ThreePhase_Power_Visual.png")
            },
            {
                "Fuse_1P",
                new VisualPrefabConfig(
                    "Fuse_1P",
                    "Assets/Prefab/Fuse_1P_Visual.prefab")
            },
            {
                "Fuse_3P",
                new VisualPrefabConfig(
                    "Fuse_3P",
                    "Assets/Prefab/Fuse_3P_Visual.prefab")
            },
            {
                "Indicator_Green_220V",
                new VisualPrefabConfig(
                    "Indicator_Green_220V",
                    "Assets/Prefab/Indicator_Green_Visual.prefab",
                    "Assets/Art/Components/Indicator_Green_Off.png",
                    "Assets/Art/Components/Indicator_Green_On.png",
                    VisualPrefabStateMode.IsEnergized)
            },
            {
                "Indicator_Red_220V",
                new VisualPrefabConfig(
                    "Indicator_Red_220V",
                    "Assets/Prefab/Indicator_Red_Visual.prefab",
                    "Assets/Art/Components/Indicator_Red_Off.png",
                    "Assets/Art/Components/Indicator_Red_On.png",
                    VisualPrefabStateMode.IsEnergized)
            },
            {
                "Indicator_Yellow_220V",
                new VisualPrefabConfig(
                    "Indicator_Yellow_220V",
                    "Assets/Prefab/Indicator_Yellow_Visual.prefab",
                    "Assets/Art/Components/Indicator_Yellow_Off.png",
                    "Assets/Art/Components/Indicator_Yellow_On.png",
                    VisualPrefabStateMode.IsEnergized)
            },
            {
                "Indicator_Green_380V",
                new VisualPrefabConfig(
                    "Indicator_Green_380V",
                    "Assets/Prefab/Indicator_Green_Visual.prefab",
                    "Assets/Art/Components/Indicator_Green_Off.png",
                    "Assets/Art/Components/Indicator_Green_On.png",
                    VisualPrefabStateMode.IsEnergized)
            },
            {
                "Indicator_Red_380V",
                new VisualPrefabConfig(
                    "Indicator_Red_380V",
                    "Assets/Prefab/Indicator_Red_Visual.prefab",
                    "Assets/Art/Components/Indicator_Red_Off.png",
                    "Assets/Art/Components/Indicator_Red_On.png",
                    VisualPrefabStateMode.IsEnergized)
            },
            {
                "Indicator_Yellow_380V",
                new VisualPrefabConfig(
                    "Indicator_Yellow_380V",
                    "Assets/Prefab/Indicator_Yellow_Visual.prefab",
                    "Assets/Art/Components/Indicator_Yellow_Off.png",
                    "Assets/Art/Components/Indicator_Yellow_On.png",
                    VisualPrefabStateMode.IsEnergized)
            },
            {
                "ThermalRelay_FR_380V",
                new VisualPrefabConfig(
                    "ThermalRelay_FR_380V",
                    "Assets/Prefab/ThermalRelay_FR_Visual.prefab",
                    "Assets/Art/Components/ThermalRelay_FR_Normal.png")
            }
        };

        public static bool TryGetConfig(string definitionName, out VisualPrefabConfig config)
        {
            if (string.IsNullOrWhiteSpace(definitionName))
            {
                config = null;
                return false;
            }

            return Configs.TryGetValue(definitionName, out config);
        }
    }
}
