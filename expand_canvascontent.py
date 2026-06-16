import re

with open('Assets/Scenes/Demo.unity', 'r', encoding='utf-8') as f:
    content = f.read()

# 1. Find CanvasContent GameObject
go_match = re.search(r'--- !u!1 &(\d+)\nGameObject:\n(.*?m_Name: CanvasContent\n.*?(?=---))', content, re.DOTALL)
if go_match:
    comp_ids = re.findall(r'- component: \{fileID: (\d+)\}', go_match.group(2))
    for cid in comp_ids:
        # Find RectTransform
        rt_match = re.search(fr'--- !u!224 &{cid}\nRectTransform:\n(.*?)(?=---)', content, re.DOTALL)
        if rt_match:
            rt_text = rt_match.group(0)
            if 'm_SizeDelta:' in rt_text:
                new_rt_text = re.sub(r'm_SizeDelta: \{x: [\d.-]+, y: [\d.-]+\}', 'm_SizeDelta: {x: 4200, y: 2700}', rt_text)
                content = content.replace(rt_text, new_rt_text)
                print("Replaced sizeDelta for CanvasContent to 4200 x 2700")

with open('Assets/Scenes/Demo.unity', 'w', encoding='utf-8') as f:
    f.write(content)
print("Done.")
