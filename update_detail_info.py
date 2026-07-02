import re

file_path = 'Assets/Scripts/UI/EncyclopediaController.cs'
with open(file_path, 'r', encoding='utf-8') as f:
    content = f.read()

old_build_detail_basic_info = '''        private static string BuildDetailBasicInfo(ComponentEncyclopediaEntry entry)
        {
            return "名称：" + entry.DisplayName +
                   "\\n分类：" + entry.Category +
                   "\\n适用电路：" + entry.CircuitType +
                   "\\n额定电压：" + Fallback(entry.RatedVoltage) +
                   "\\n额定电流：" + Fallback(entry.RatedCurrent) +
                   "\\n端子：" + BuildTerminalSummary(entry.Definition, entry.Terminals);
        }'''

# Note: The original encoding might have broken characters like `名称?`. Let me use regex to match it.

old_pattern = r'private static string BuildDetailBasicInfo\(ComponentEncyclopediaEntry entry\)[\s\S]*?\{[\s\S]*?return "名称.*? \+ entry\.DisplayName \+[\s\S]*?"\\n分类.*? \+ entry\.Category \+[\s\S]*?"\\n适用电路.*? \+ entry\.CircuitType \+[\s\S]*?"\\n额定电压.*? \+ Fallback\(entry\.RatedVoltage\) \+[\s\S]*?"\\n额定电流.*? \+ Fallback\(entry\.RatedCurrent\) \+[\s\S]*?"\\n端子.*? \+ BuildTerminalSummary\(entry\.Definition, entry\.Terminals\);[\s\S]*?\}'

new_build_detail_basic_info = '''        private static string BuildDetailBasicInfo(ComponentEncyclopediaEntry entry)
        {
            return "名称：" + entry.DisplayName +
                   "\\n分类：" + entry.Category +
                   "\\n适用电路：" + entry.CircuitType +
                   "\\n核心参数：" + BuildBasicSummary(entry) +
                   "\\n引脚端子：" + BuildTerminalSummary(entry.Definition, entry.Terminals);
        }'''

match = re.search(old_pattern, content)
if match:
    content = content.replace(match.group(0), new_build_detail_basic_info)
    with open(file_path, 'w', encoding='utf-8') as f:
        f.write(content)
    print("SUCCESS: BuildDetailBasicInfo updated.")
else:
    print("ERROR: BuildDetailBasicInfo not found. Let's try exact replace with weird chars.")
    
    # In case regex failed due to some weirdness
    old_exact = '''        private static string BuildDetailBasicInfo(ComponentEncyclopediaEntry entry)
        {
            return "名称：" + entry.DisplayName +
                   "\\n分类：" + entry.Category +
                   "\\n适用电路：" + entry.CircuitType +
                   "\\n额定电压：" + Fallback(entry.RatedVoltage) +
                   "\\n额定电流：" + Fallback(entry.RatedCurrent) +
                   "\\n端子：" + BuildTerminalSummary(entry.Definition, entry.Terminals);
        }'''
    # Actually, the output in console had `名称?` because the console encoding didn't decode the colon (：) correctly, but the file is UTF-8. So regex should have matched if we just look for `名称` and `Fallback`.
