import json

with open("Assets/Resources/Blueprints/Templates/motor_forward_reverse_interlock_template.json", "r", encoding="utf-8") as f:
    data = json.load(f)

print(f"Components count: {len(data.get('components', []))}")
for c in data.get('components', []):
    print(f"- {c['instanceId']} : {c['definitionName']}")

print(f"Wires count: {len(data.get('wires', []))}")
