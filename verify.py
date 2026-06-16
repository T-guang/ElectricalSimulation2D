import re

with open('Assets/Scenes/Demo.unity', 'r', encoding='utf-8') as f:
    content = f.read()

go_match = re.search(r'--- !u!1 &(\d+)\nGameObject:\n(.*?m_Name: CanvasContent\n.*?(?=---))', content, re.DOTALL)
if go_match:
    comp_ids = re.findall(r'- component: \{fileID: (\d+)\}', go_match.group(2))
    for cid in comp_ids:
        rt_match = re.search(fr'--- !u!224 &{cid}\nRectTransform:\n(.*?)(?=---)', content, re.DOTALL)
        if rt_match:
            rt_text = rt_match.group(0)
            if 'm_SizeDelta:' in rt_text:
                print(re.search(r'm_SizeDelta: (.*)', rt_text).group(1))

