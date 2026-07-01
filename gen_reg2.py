import re

with open('Assets/Scripts/UI/VisualPrefab/VisualPrefabRegistry.cs', 'r', encoding='utf-8') as f:
    text = f.read()

new_configs = '''            {
                "AC_220V_Power",
                new VisualPrefabConfig(
                    "AC_220V_Power",
                    "Assets/Prefab/AC_220V_Power_Visual.prefab",
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
