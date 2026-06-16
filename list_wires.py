import json

with open("Assets/Resources/Blueprints/Templates/motor_forward_reverse_interlock_template.json", "r", encoding="utf-8") as f:
    data = json.load(f)

for w in data.get('wires', []):
    sc = w.get("startComponentId")
    st = w.get("startTerminalId")
    ec = w.get("endComponentId")
    et = w.get("endTerminalId")
    print(f"{sc}.{st} -> {ec}.{et}")

