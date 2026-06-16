import json

path = "Assets/Resources/Blueprints/Templates/motor_self_hold_control_template.json"
with open(path, "r", encoding="utf-8") as f:
    data = json.load(f)

# Component Positions (Ultra-Wide Waterfall)
comp_coords = {
    "power_3p_1": (-800.0, 450.0),   # Pushed very far left to account for its massive width
    "breaker_3p_1": (-300.0, 300.0), # 500px gap from power center
    "fuse_3p_1": (100.0, 150.0),     # 400px gap
    "km_1": (500.0, 0.0),            # 400px gap
    "motor_1": (500.0, -250.0),      # Directly below KM
    "stop_1": (-300.0, -250.0),
    "start_1": (100.0, -250.0)
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
        if start_t == "L1": set_route(w, [(-850, y_bus), (-320, y_bus)])
        elif start_t == "L2": set_route(w, [(-800, y_bus-15), (-300, y_bus-15)])
        elif start_t == "L3": set_route(w, [(-750, y_bus-30), (-280, y_bus-30)])

    # Breaker to Fuse
    elif start_c == "breaker_3p_1" and end_c == "fuse_3p_1":
        y_bus = 230
        if start_t == "P1_OUT": set_route(w, [(-320, y_bus), (80, y_bus)])
        elif start_t == "P2_OUT": set_route(w, [(-300, y_bus-15), (100, y_bus-15)])
        elif start_t == "P3_OUT": set_route(w, [(-280, y_bus-30), (120, y_bus-30)])

    # Fuse to KM
    elif start_c == "fuse_3p_1" and end_c == "km_1":
        y_bus = 80
        if start_t == "L1_OUT": set_route(w, [(80, y_bus), (480, y_bus)])
        elif start_t == "L2_OUT": set_route(w, [(100, y_bus-15), (500, y_bus-15)])
        elif start_t == "L3_OUT": set_route(w, [(120, y_bus-30), (520, y_bus-30)])

    # KM to Motor
    elif start_c == "km_1" and end_c == "motor_1":
        y_bus = -100
        if start_t == "T1": set_route(w, [(480, y_bus), (420, y_bus), (420, -220)])
        elif start_t == "T2": set_route(w, [(500, y_bus-15), (440, y_bus-15), (440, -250)])
        elif start_t == "T3": set_route(w, [(520, y_bus-30), (460, y_bus-30), (460, -280)])

    # PE
    elif start_c == "power_3p_1" and start_t == "PE" and end_c == "motor_1":
        set_route(w, [(-650, 400), (-650, -350), (480, -350)])
        w["color"] = "#14A640"

    # --- Control Circuit ---
    elif start_c == "power_3p_1" and start_t == "L1" and end_c == "stop_1":
        set_route(w, [(-900, 380), (-900, -250), (-350, -250)])
        w["color"] = "#E74C3C"

    elif start_c == "stop_1" and start_t == "12" and end_c == "start_1" and end_t == "23":
        set_route(w, [(-250, -250), (50, -250)])
        w["color"] = "#E74C3C"

    elif start_c == "start_1" and start_t == "24" and end_c == "km_1" and end_t == "A1":
        set_route(w, [(150, -250), (380, -250), (380, 20)])
        w["color"] = "#E74C3C"

    elif start_c == "km_1" and start_t == "A2" and end_c == "power_3p_1":
        set_route(w, [(600, 20), (650, 20), (650, 550), (-800, 550)])
        w["color"] = "#E74C3C"

    # --- Self Lock Circuit ---
    elif start_c == "stop_1" and start_t == "12" and end_c == "km_1" and end_t == "13":
        set_route(w, [(-250, -280), (360, -280), (360, 0)])
        w["color"] = "#9B59B6"

    elif start_c == "km_1" and start_t == "14" and end_c == "start_1" and end_t == "24":
        set_route(w, [(600, 0), (620, 0), (620, -300), (150, -300)])
        w["color"] = "#9B59B6"

with open(path, "w", encoding="utf-8") as f:
    json.dump(data, f, indent=4, ensure_ascii=False)

print("Optimization complete.")
