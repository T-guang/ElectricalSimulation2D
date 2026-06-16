import re

with open('Assets/Scenes/Demo.unity', 'r', encoding='utf-8') as f:
    content = f.read()

ws_match = re.search(r'--- !u!1 &(\d+)\nGameObject:\n(.*?m_Name: Workspace\n.*?(?=---))', content, re.DOTALL)
if ws_match:
    comp_ids = re.findall(r'- component: \{fileID: (\d+)\}', ws_match.group(2))
    for cid in comp_ids:
        # Find RectTransform for this component ID
        rt_match = re.search(fr'--- !u!224 &{cid}\nRectTransform:\n(.*?)(?=---)', content, re.DOTALL)
        if rt_match:
            rt_text = rt_match.group(0)
            size_match = re.search(r'm_SizeDelta: \{x: ([\d.-]+), y: ([\d.-]+)\}', rt_text)
            if size_match:
                x = float(size_match.group(1))
                y = float(size_match.group(2))
                new_x = x * 1.5
                new_y = y * 1.5
                print(f"Old size: {x} x {y}")
                print(f"New size: {new_x} x {new_y}")
                
                new_rt_text = re.sub(r'm_SizeDelta: \{x: [\d.-]+, y: [\d.-]+\}', f'm_SizeDelta: {{x: {new_x}, y: {new_y}}}', rt_text)
                content = content.replace(rt_text, new_rt_text)
                
                with open('Assets/Scenes/Demo.unity', 'w', encoding='utf-8') as fw:
                    fw.write(content)
                print("Successfully updated Workspace size!")
                break
