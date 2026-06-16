import json

path = "Assets/Resources/Blueprints/Templates/motor_self_hold_control_template.json"
with open(path, "r", encoding="utf-8") as f:
    data = json.load(f)

# 1. Component Positions (Waterfall Layout)
comp_coords = {
    "power_3p_1": (-450.0, 450.0),
    "breaker_3p_1": (-200.0, 300.0),
    "fuse_3p_1": (50.0, 150.0),
    "km_1": (300.0, 0.0),
    "motor_1": (300.0, -250.0),
    "stop_1": (-200.0, -250.0),
    "start_1": (50.0, -250.0)
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
    # Power to Breaker
    if start_c == "power_3p_1" and end_c == "breaker_3p_1":
        y_bus = 380
        if start_t == "L1": set_route(w, [(-550, y_bus), (-220, y_bus)])
        elif start_t == "L2": set_route(w, [(-500, y_bus-15), (-200, y_bus-15)])
        elif start_t == "L3": set_route(w, [(-450, y_bus-30), (-180, y_bus-30)])

    # Breaker to Fuse
    elif start_c == "breaker_3p_1" and end_c == "fuse_3p_1":
        y_bus = 230
        if start_t == "P1_OUT": set_route(w, [(-220, y_bus), (30, y_bus)])
        elif start_t == "P2_OUT": set_route(w, [(-200, y_bus-15), (50, y_bus-15)])
        elif start_t == "P3_OUT": set_route(w, [(-180, y_bus-30), (70, y_bus-30)])

    # Fuse to KM
    elif start_c == "fuse_3p_1" and end_c == "km_1":
        y_bus = 80
        if start_t == "L1_OUT": set_route(w, [(30, y_bus), (280, y_bus)])
        elif start_t == "L2_OUT": set_route(w, [(50, y_bus-15), (300, y_bus-15)])
        elif start_t == "L3_OUT": set_route(w, [(70, y_bus-30), (320, y_bus-30)])

    # KM to Motor
    elif start_c == "km_1" and end_c == "motor_1":
        y_bus = -100
        # KM bottom terminals to Motor left terminals
        if start_t == "T1": set_route(w, [(280, y_bus), (220, y_bus), (220, -220)])
        elif start_t == "T2": set_route(w, [(300, y_bus-15), (240, y_bus-15), (240, -250)])
        elif start_t == "T3": set_route(w, [(320, y_bus-30), (260, y_bus-30), (260, -280)])

    # PE
    elif start_c == "power_3p_1" and start_t == "PE" and end_c == "motor_1":
        set_route(w, [(-350, 400), (-350, -320), (280, -320)])
        w["color"] = "#14A640"

    # --- Control Circuit ---
    elif start_c == "power_3p_1" and start_t == "L1" and end_c == "stop_1":
        set_route(w, [(-600, 380), (-600, -250), (-250, -250)])
        w["color"] = "#E74C3C"

    elif start_c == "stop_1" and start_t == "12" and end_c == "start_1" and end_t == "23":
        set_route(w, [(-150, -250), (0, -250)])
        w["color"] = "#E74C3C"

    elif start_c == "start_1" and start_t == "24" and end_c == "km_1" and end_t == "A1":
        set_route(w, [(100, -250), (180, -250), (180, 20)])
        w["color"] = "#E74C3C"

    elif start_c == "km_1" and start_t == "A2" and end_c == "power_3p_1":
        set_route(w, [(400, 20), (450, 20), (450, 500), (-500, 500)])
        w["color"] = "#E74C3C"

    # --- Self Lock Circuit ---
    elif start_c == "stop_1" and start_t == "12" and end_c == "km_1" and end_t == "13":
        set_route(w, [(-150, -280), (160, -280), (160, 0)])
        w["color"] = "#9B59B6"

    elif start_c == "km_1" and start_t == "14" and end_c == "start_1" and end_t == "24":
        set_route(w, [(400, 0), (420, 0), (420, -300), (100, -300)])
        w["color"] = "#9B59B6"

with open(path, "w", encoding="utf-8") as f:
    json.dump(data, f, indent=4, ensure_ascii=False)

print("Optimization complete.")
