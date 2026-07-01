import re

file_path = 'Assets/Scripts/Core/CircuitComponent.cs'
with open(file_path, 'r', encoding='utf-8') as f:
    content = f.read()

# Replace in TryApplyExperimentalKmVisualPrefab
old_str_1 = '''            if (!useExperimentalKmVisualPrefab ||
                Definition == null ||
                !string.Equals(Definition.name, experimentalKmVisualDefinitionName, System.StringComparison.Ordinal))'''

new_str_1 = '''            if (!useExperimentalKmVisualPrefab ||
                Definition == null ||
                !(string.Equals(Definition.name, experimentalKmVisualDefinitionName, System.StringComparison.Ordinal) || string.Equals(Definition.name, "Contactor_KM_220V", System.StringComparison.Ordinal)))'''

# Replace in IsExperimentalKmVisualActive
old_str_2 = '''        private bool IsExperimentalKmVisualActive()
        {
            return useExperimentalKmVisualPrefab &&
                   experimentalKmVisualRoot != null &&
                   Definition != null &&
                   string.Equals(Definition.name, experimentalKmVisualDefinitionName, System.StringComparison.Ordinal);
        }'''

new_str_2 = '''        private bool IsExperimentalKmVisualActive()
        {
            return useExperimentalKmVisualPrefab &&
                   experimentalKmVisualRoot != null &&
                   Definition != null &&
                   (string.Equals(Definition.name, experimentalKmVisualDefinitionName, System.StringComparison.Ordinal) || string.Equals(Definition.name, "Contactor_KM_220V", System.StringComparison.Ordinal));
        }'''

if old_str_1 in content:
    content = content.replace(old_str_1, new_str_1)
    print("Replaced 1")
else:
    print("Could not find old_str_1")

if old_str_2 in content:
    content = content.replace(old_str_2, new_str_2)
    print("Replaced 2")
else:
    print("Could not find old_str_2")

with open(file_path, 'w', encoding='utf-8') as f:
    f.write(content)
