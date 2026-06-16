import re

with open('Assets/Scenes/Demo.unity', 'r', encoding='utf-8') as f:
    content = f.read()

# Find all RectTransforms with sizeDelta > 1000
for match in re.finditer(r'--- !u!224 &(\d+)\nRectTransform:\n(.*?)(?=---)', content, re.DOTALL):
    rt_text = match.group(2)
    size_match = re.search(r'm_SizeDelta: \{x: ([\d.-]+), y: ([\d.-]+)\}', rt_text)
    if size_match:
        x, y = float(size_match.group(1)), float(size_match.group(2))
        if x > 1500 or y > 1500:
            # find corresponding GameObject
            go_match = re.search(r'm_GameObject: \{fileID: (\d+)\}', rt_text)
            if go_match:
                go_id = go_match.group(1)
                go_obj_match = re.search(fr'--- !u!1 &{go_id}\nGameObject:\n(.*?)m_Name: (.*?)\n', content, re.DOTALL)
                name = go_obj_match.group(2) if go_obj_match else "Unknown"
                print(f"Name: {name} | SizeDelta: {x} x {y}")
