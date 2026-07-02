import re

file_path = 'Assets/Scripts/UI/EncyclopediaController.cs'
with open(file_path, 'r', encoding='utf-8') as f:
    content = f.read()

# Locate the AddDetailLine block. We can just search for "AddDetailLine(infoPanel,"
pattern = r'AddDetailLine\(infoPanel,\s*"分类.*?\);\s*AddDetailLine\(infoPanel,\s*"适用电路.*?\);\s*AddDetailLine\(infoPanel,\s*"额定电压.*?\);\s*AddDetailLine\(infoPanel,\s*"额定电流.*?\);\s*AddDetailLine\(infoPanel,\s*"端子.*?\);'

new_block = '''            AddDetailLine(infoPanel, "分类：" + entry.Category);
            AddDetailLine(infoPanel, "适用电路：" + entry.CircuitType);
            AddDetailLine(infoPanel, "核心参数：" + BuildBasicSummary(entry));
            AddDetailLine(infoPanel, "端子：" + BuildTerminalSummary(entry.Definition, entry.Terminals), 52f);'''

match = re.search(pattern, content)
if match:
    content = content.replace(match.group(0), new_block)
    with open(file_path, 'w', encoding='utf-8') as f:
        f.write(content)
    print("SUCCESS: CreateDetailHeader AddDetailLine block replaced.")
else:
    print("ERROR: Regex did not match.")
