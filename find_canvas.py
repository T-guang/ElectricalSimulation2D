import re

with open('Assets/Scenes/Demo.unity', 'r', encoding='utf-8') as f:
    content = f.read()

# Find GameObjects with name Workspace, Content, Canvas, Simulation
for match in re.finditer(r'--- !u!1 &(\d+)\nGameObject:\n.*?m_Name: (.*?\n.*?)(?=---)', content, re.DOTALL):
    name = re.search(r'm_Name: (.*)', match.group(2)).group(1)
    if any(k in name for k in ['Content', 'Workspace', 'Canvas', 'Simulation', 'Area']):
        print(f"ID: {match.group(1)}, Name: {name}")

