import json

path = "Assets/Resources/Blueprints/Templates/motor_jog_continuous_template.json"
with open(path, "r", encoding="utf-8") as f:
    data = json.load(f)

print(f"Template ID: {data.get('templateId')}")
print(f"Template Name: {data.get('templateName')}")

print("\n--- Components ---")
comp_map = {}
for c in data.get("components", []):
    cid = c.get("instanceId")
    cdef = c.get("definitionName")
    comp_map[cid] = cdef
    print(f"- {cid} ({cdef})")

print("\n--- Wires ---")
for w in data.get("wires", []):
    sc = w.get("startComponentId")
    st = w.get("startTerminalId")
    ec = w.get("endComponentId")
    et = w.get("endTerminalId")
    sc_def = comp_map.get(sc, sc)
    ec_def = comp_map.get(ec, ec)
    print(f"{sc_def} ({sc}).{st}  ->  {ec_def} ({ec}).{et}")

