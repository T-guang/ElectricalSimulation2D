import json

path = "Assets/Resources/Blueprints/Templates/motor_self_hold_control_template.json"
with open(path, "r", encoding="utf-8") as f:
    data = json.load(f)

# 1. Component Positions (Wide Layout)
comp_coords = {
    "power_3p_1": (-500.0, 300.0),
    "breaker_3p_1": (-250.0, 300.0),
    "fuse_3p_1": (0.0, 300.0),
    "km_1": (250.0, 100.0),
    "motor_1": (250.0, -150.0),
    "stop_1": (-250.0, -200.0),
    "start_1": (0.0, -200.0)
}

for c in data.get("components", []):
    cid = c.get("instanceId")
    if cid in comp_coords:
        c["x"], c["y"] = comp_coords[cid]

def set_route(wire, points):
    wire["manualRoutePoints"] = [{"x": float(p[0]), "y": float(p[1])} for p in points]

for w in data.get("wires", []):
    start_c = w.get("startComponentId")
    start_t = w.get("startTerminalId")
    end_c = w.get("endComponentId")
    end_t = w.get("endTerminalId")

    w.pop("manualRoutePoints", None)
    w.pop("manualRouteHorizontal", None)
    w.pop("manualRouteAxis", None)
    
    # --- Main Circuit ---
    if start_c == "power_3p_1" and end_c == "breaker_3p_1":
        y_bus = 250
        if start_t == "L1": set_route(w, [(-520, y_bus), (-270, y_bus)])
        elif start_t == "L2": set_route(w, [(-500, y_bus-15), (-250, y_bus-15)])
        elif start_t == "L3": set_route(w, [(-480, y_bus-30), (-230, y_bus-30)])

    elif start_c == "breaker_3p_1" and end_c == "fuse_3p_1":
        y_bus = 200
        if start_t == "P1_OUT": set_route(w, [(-270, y_bus), (-20, y_bus)])
        elif start_t == "P2_OUT": set_route(w, [(-250, y_bus-15), (0, y_bus-15)])
        elif start_t == "P3_OUT": set_route(w, [(-230, y_bus-30), (20, y_bus-30)])

    elif start_c == "fuse_3p_1" and end_c == "km_1":
        y_drop = 150
        if start_t == "L1_OUT": set_route(w, [(-20, y_drop), (230, y_drop)])
        elif start_t == "L2_OUT": set_route(w, [(0, y_drop-15), (250, y_drop-15)])
        elif start_t == "L3_OUT": set_route(w, [(20, y_drop-30), (270, y_drop-30)])

    elif start_c == "km_1" and end_c == "motor_1":
        pass # Straight drop

    elif start_c == "power_3p_1" and start_t == "PE" and end_c == "motor_1":
        set_route(w, [(-580, 300), (-580, -300), (200, -300)])
        w["color"] = "#14A640"

    # --- Control Circuit ---
    elif start_c == "power_3p_1" and start_t == "L1" and end_c == "stop_1":
        set_route(w, [(-550, 300), (-550, -150), (-300, -150)])
        w["color"] = "#E74C3C"

    elif start_c == "stop_1" and start_t == "12" and end_c == "start_1" and end_t == "23":
        set_route(w, [(-200, -200), (-50, -200)])
        w["color"] = "#E74C3C"

    elif start_c == "start_1" and start_t == "24" and end_c == "km_1" and end_t == "A1":
        set_route(w, [(50, -200), (150, -200), (150, 100)])
        w["color"] = "#E74C3C"

    elif start_c == "km_1" and start_t == "A2" and end_c == "power_3p_1":
        set_route(w, [(320, 100), (320, 380), (-500, 380)])
        w["color"] = "#E74C3C"

    # --- Self Lock Circuit ---
    elif start_c == "stop_1" and start_t == "12" and end_c == "km_1" and end_t == "13":
        set_route(w, [(-200, -220), (350, -220), (350, 100)])
        w["color"] = "#9B59B6"

    elif start_c == "km_1" and start_t == "14" and end_c == "start_1" and end_t == "24":
        set_route(w, [(380, 100), (380, -250), (50, -250)])
        w["color"] = "#9B59B6"

with open(path, "w", encoding="utf-8") as f:
    json.dump(data, f, indent=4, ensure_ascii=False)

print("Optimization complete.")
