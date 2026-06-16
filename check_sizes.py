import re

with open('Assets/Scenes/Demo.unity', 'r', encoding='utf-8') as f:
    content = f.read()

def find_size(name):
    print(f"--- Checking {name} ---")
    match = re.search(fr'--- !u!1 &(\d+)\nGameObject:\n(.*?(?<=m_Name: ){name}\n.*?(?=---))', content, re.DOTALL)
    if not match:
        print("Not found")
        return
    comp_ids = re.findall(r'- component: \{fileID: (\d+)\}', match.group(2))
    for cid in comp_ids:
        rt_match = re.search(fr'--- !u!224 &{cid}\nRectTransform:\n(.*?)(?=---)', content, re.DOTALL)
        if rt_match:
            rt_text = rt_match.group(1)
            size_match = re.search(r'm_SizeDelta: \{x: ([\d.-]+), y: ([\d.-]+)\}', rt_text)
            anchor_min = re.search(r'm_AnchorMin: \{x: ([\d.-]+), y: ([\d.-]+)\}', rt_text)
            anchor_max = re.search(r'm_AnchorMax: \{x: ([\d.-]+), y: ([\d.-]+)\}', rt_text)
            if size_match:
                print(f"SizeDelta: {size_match.group(1)} x {size_match.group(2)}")
                if anchor_min: print(f"AnchorMin: {anchor_min.group(1)}, {anchor_min.group(2)}")
                if anchor_max: print(f"AnchorMax: {anchor_max.group(1)}, {anchor_max.group(2)}")

find_size("ComponentLayer")
find_size("WireLayer")
find_size("SimulationPage")
find_size("Workspace")
find_size("MainAppRoot")
