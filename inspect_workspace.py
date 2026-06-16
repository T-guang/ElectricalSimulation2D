import re

with open('Assets/Scenes/Demo.unity', 'r', encoding='utf-8') as f:
    content = f.read()

# 1. Find Workspace GameObject
ws_match = re.search(r'--- !u!1 &(\d+)\nGameObject:\n(.*?m_Name: Workspace\n.*?(?=---))', content, re.DOTALL)
if not ws_match:
    print("Workspace not found")
else:
    ws_id = ws_match.group(1)
    print(f"Workspace GameObject ID: {ws_id}")
    
    # Extract component IDs
    comp_ids = re.findall(r'- component: \{fileID: (\d+)\}', ws_match.group(2))
    print(f"Component IDs: {comp_ids}")
    
    # Search for RectTransform with one of these IDs
    for cid in comp_ids:
        rt_match = re.search(fr'--- !u!224 &{cid}\nRectTransform:\n(.*?)(?=---)', content, re.DOTALL)
        if rt_match:
            print("Found RectTransform:")
            print(rt_match.group(1)[:500])

