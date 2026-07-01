import re

with open('Assets/Scripts/UI/VisualPrefab/VisualPrefabRegistry.cs', 'r', encoding='utf-8') as f:
    text = f.read()

new_configs = '''            {
                "TerminalBlock_2H",
                new VisualPrefabConfig(
                    "TerminalBlock_2H",
                    "Assets/Prefab/TerminalBlock_2H_Visual.prefab",
                    null, null,
                    VisualPrefabStateMode.Static,
                    activeWhenClosed: false,
                    useTransparentTerminalView: true,
                    hideLegacyTerminalLabel: true,
                    disableLegacyTerminalOffset: true,
                    hasOperationHitArea: false)
            },
            {
                "TerminalBlock_3H",
                new VisualPrefabConfig(
                    "TerminalBlock_3H",
                    "Assets/Prefab/TerminalBlock_3H_Visual.prefab",
                    null, null,
                    VisualPrefabStateMode.Static,
                    activeWhenClosed: false,
                    useTransparentTerminalView: true,
                    hideLegacyTerminalLabel: true,
                    disableLegacyTerminalOffset: true,
                    hasOperationHitArea: false)
            },
            {
                "TerminalBlock_6H",
                new VisualPrefabConfig(
                    "TerminalBlock_6H",
                    "Assets/Prefab/TerminalBlock_6H_Visual.prefab",
                    null, null,
                    VisualPrefabStateMode.Static,
                    activeWhenClosed: false,
                    useTransparentTerminalView: true,
                    hideLegacyTerminalLabel: true,
                    disableLegacyTerminalOffset: true,
                    hasOperationHitArea: false)
            },
            {
                "TerminalBlock_2V",
                new VisualPrefabConfig(
                    "TerminalBlock_2V",
                    "Assets/Prefab/TerminalBlock_2V_Visual.prefab",
                    null, null,
                    VisualPrefabStateMode.Static,
                    activeWhenClosed: false,
                    useTransparentTerminalView: true,
                    hideLegacyTerminalLabel: true,
                    disableLegacyTerminalOffset: true,
                    hasOperationHitArea: false)
            },
            {
                "TerminalBlock_3V",
                new VisualPrefabConfig(
                    "TerminalBlock_3V",
                    "Assets/Prefab/TerminalBlock_3V_Visual.prefab",
                    null, null,
                    VisualPrefabStateMode.Static,
                    activeWhenClosed: false,
                    useTransparentTerminalView: true,
                    hideLegacyTerminalLabel: true,
                    disableLegacyTerminalOffset: true,
                    hasOperationHitArea: false)
            },
            {
                "TerminalBlock_6V",
                new VisualPrefabConfig(
                    "TerminalBlock_6V",
                    "Assets/Prefab/TerminalBlock_6V_Visual.prefab",
                    null, null,
                    VisualPrefabStateMode.Static,
                    activeWhenClosed: false,
                    useTransparentTerminalView: true,
                    hideLegacyTerminalLabel: true,
                    disableLegacyTerminalOffset: true,
                    hasOperationHitArea: false)
            },
            {
                "KnifeSwitch_QS",'''

text = text.replace('            {\n                "KnifeSwitch_QS",', new_configs)

with open('Assets/Scripts/UI/VisualPrefab/VisualPrefabRegistry.cs', 'w', encoding='utf-8') as f:
    f.write(text)
