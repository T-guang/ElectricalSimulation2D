import re

file_path = 'Assets/Scripts/UI/VisualPrefab/VisualPrefabRegistry.cs'
with open(file_path, 'r', encoding='utf-8') as f:
    content = f.read()

target = '''            {
                "Button_Stop_NC",
                new VisualPrefabConfig(
                    "Button_Stop_NC",
                    "Assets/Prefab/Button_Stop_NC_Visual.prefab",
                    "Assets/Art/Components/Button_Stop_NC_Default.png",
                    "Assets/Art/Components/Button_Stop_NC_Pressed.png",
                    VisualPrefabStateMode.IsClosed,
                    activeWhenClosed: false,
                    hasOperationHitArea: true)
            },'''

replacement = '''            {
                "EmergencyStop_NC",
                new VisualPrefabConfig(
                    "EmergencyStop_NC",
                    "Assets/Prefab/EmergencyStopButton_Visual.prefab",
                    "Assets/Art/Components/EmergencyStop/EmergencyStopButton_Default.png",
                    "Assets/Art/Components/EmergencyStop/EmergencyStopButton_Pressed.png",
                    VisualPrefabStateMode.IsClosed,
                    activeWhenClosed: false,
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
            },'''

if target in content:
    content = content.replace(target, replacement)
    with open(file_path, 'w', encoding='utf-8') as f:
        f.write(content)
    print("Injected successfully!")
else:
    print("Could not find target!")
