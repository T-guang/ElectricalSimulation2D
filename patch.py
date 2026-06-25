import re

with open('Assets/Scripts/Core/CircuitComponent.cs', 'r', encoding='utf-8') as f:
    content = f.read()

# 1. Add constants
constants_target = '        private const string experimentalCompoundGreenButtonPressedSpritePath = "Assets/Art/Components/Button_Compound_Green_SB_Pressed.png";\n'
constants_repl = '''        // Temporary self-lock button visual pilot. Set to false to restore the default rectangular appearance.
        private const bool useExperimentalSelfLockButtonVisualPrefab = true;
        private const bool showExperimentalSelfLockButtonTerminalDebugMarkers = false;
        private const string experimentalSelfLockRedButtonDefinitionName = "Button_SelfLock_SB";
        private const string experimentalSelfLockGreenButtonDefinitionName = "Button_SelfLock_Green_SB";
        private const string experimentalSelfLockRedButtonVisualAssetPath = "Assets/Prefab/Button_SelfLock_SB_Visual.prefab";
        private const string experimentalSelfLockGreenButtonVisualAssetPath = "Assets/Prefab/Button_SelfLock_Green_SB_Visual.prefab";
        private const string experimentalSelfLockRedButtonDefaultSpritePath = "Assets/Art/Components/Button_SelfLock_SB_Default.png";
        private const string experimentalSelfLockRedButtonPressedSpritePath = "Assets/Art/Components/Button_SelfLock_SB_Locked.png";
        private const string experimentalSelfLockGreenButtonDefaultSpritePath = "Assets/Art/Components/Button_SelfLock_Green_SB_Default.png";
        private const string experimentalSelfLockGreenButtonPressedSpritePath = "Assets/Art/Components/Button_SelfLock_Green_SB_Locked.png";\n'''
content = content.replace(constants_target, constants_target + constants_repl)

# 2. Add fields
fields_target = '        private readonly Dictionary<string, RectTransform> experimentalCompoundButtonTerminalAnchors = new Dictionary<string, RectTransform>(System.StringComparer.OrdinalIgnoreCase);\n'
fields_repl = '''        private RectTransform experimentalSelfLockButtonVisualRoot;
        private Image experimentalSelfLockButtonBodyImage;
        private Sprite experimentalSelfLockButtonDefaultSprite;
        private Sprite experimentalSelfLockButtonPressedSprite;
        private readonly Dictionary<string, RectTransform> experimentalSelfLockButtonTerminalAnchors = new Dictionary<string, RectTransform>(System.StringComparer.OrdinalIgnoreCase);\n'''
content = content.replace(fields_target, fields_target + fields_repl)

# 3. Add to TryApply... calls in Initialize
init_target = '            TryApplyExperimentalCompoundButtonVisualPrefab();\n'
init_repl = '            TryApplyExperimentalSelfLockButtonVisualPrefab();\n'
content = content.replace(init_target, init_target + init_repl)

# 4. Add Update... in RefreshVisual
refresh_target = '                    UpdateExperimentalCompoundButtonBodySprite();\n'
refresh_repl = '                    UpdateExperimentalSelfLockButtonBodySprite();\n'
content = content.replace(refresh_target, refresh_target + refresh_repl)

# 5. Add to TryGetTerminalPosition
pos_target = '''            if (TryGetExperimentalCompoundButtonTerminalPosition(terminalId, out localPosition))
            {
                showDebugMarker = showExperimentalCompoundButtonTerminalDebugMarkers;
                return true;
            }\n'''
pos_repl = '''            if (TryGetExperimentalSelfLockButtonTerminalPosition(terminalId, out localPosition))
            {
                showDebugMarker = showExperimentalSelfLockButtonTerminalDebugMarkers;
                return true;
            }\n'''
content = content.replace(pos_target, pos_target + pos_repl)

# 6. Add IsExperimentalVisualActive
is_active_target = '                   IsExperimentalCompoundButtonVisualActive() ||\n'
is_active_repl = '                   IsExperimentalSelfLockButtonVisualActive() ||\n'
content = content.replace(is_active_target, is_active_target + is_active_repl)

# 7. Add definition/active methods
def_target = '''        private bool IsExperimentalCompoundButtonDefinition()
        {
            return string.Equals(Definition.name, experimentalCompoundRedButtonDefinitionName, System.StringComparison.Ordinal) ||
                   string.Equals(Definition.name, experimentalCompoundGreenButtonDefinitionName, System.StringComparison.Ordinal);
        }\n'''
def_repl = '''
        private bool IsExperimentalSelfLockButtonVisualActive()
        {
            return useExperimentalSelfLockButtonVisualPrefab &&
                   experimentalSelfLockButtonVisualRoot != null &&
                   Definition != null &&
                   IsExperimentalSelfLockButtonDefinition();
        }

        private bool IsExperimentalSelfLockButtonDefinition()
        {
            return string.Equals(Definition.name, experimentalSelfLockRedButtonDefinitionName, System.StringComparison.Ordinal) ||
                   string.Equals(Definition.name, experimentalSelfLockGreenButtonDefinitionName, System.StringComparison.Ordinal);
        }\n'''
content = content.replace(def_target, def_target + def_repl)

# 8. Duplicate TryApplyExperimentalCompoundButtonVisualPrefab block
m = re.search(r'(        private void TryApplyExperimentalCompoundButtonVisualPrefab\(\).*?)(        private void TryApplyExperimentalThreePhasePowerVisualPrefab\(\))', content, re.DOTALL)
if m:
    compound_block = m.group(1)
    # Replace compound specific names with selflock specific names
    selflock_block = compound_block.replace('CompoundButton', 'SelfLockButton')
    selflock_block = selflock_block.replace('CompoundGreenButton', 'SelfLockGreenButton')
    selflock_block = selflock_block.replace('CompoundRedButton', 'SelfLockRedButton')
    
    content = content[:m.start()] + compound_block + selflock_block + m.group(2) + content[m.end():]

with open('Assets/Scripts/Core/CircuitComponent.cs', 'w', encoding='utf-8') as f:
    f.write(content)

print("Patch applied")
