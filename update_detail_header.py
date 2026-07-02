import re

file_path = 'Assets/Scripts/UI/EncyclopediaController.cs'
with open(file_path, 'r', encoding='utf-8') as f:
    content = f.read()

# Instead of relying on specific character encodings (like `分类：`), I will use regex to find AddDetailLine calls
# after title.gameObject.AddComponent...

old_pattern = r'AddDetailLine\(infoPanel,\s*".*?".*?entry\.Category\);\s*AddDetailLine\(infoPanel,\s*".*?".*?entry\.CircuitType\);\s*AddDetailLine\(infoPanel,\s*".*?".*?Fallback\(entry\.RatedVoltage\)\);\s*AddDetailLine\(infoPanel,\s*".*?".*?Fallback\(entry\.RatedCurrent\)\);\s*AddDetailLine\(infoPanel,\s*".*?".*?BuildTerminalSummary\(entry\.Definition, entry\.Terminals\)\);'

new_lines = '''            AddDetailLine(infoPanel, "分类：" + entry.Category);
            AddDetailLine(infoPanel, "适用电路：" + entry.CircuitType);
            AddDetailLine(infoPanel, "核心参数：" + BuildBasicSummary(entry));
            AddDetailLine(infoPanel, "端子：" + BuildTerminalSummary(entry.Definition, entry.Terminals));'''

match = re.search(old_pattern, content)
if match:
    content = content.replace(match.group(0), new_lines)
    with open(file_path, 'w', encoding='utf-8') as f:
        f.write(content)
    print("SUCCESS: CreateDetailHeader updated.")
else:
    print("ERROR: CreateDetailHeader AddDetailLine block not found.")
