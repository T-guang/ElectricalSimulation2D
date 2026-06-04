import json

path = "Assets/Resources/Blueprints/Templates/motor_thermal_protection_template.json"
with open(path, "r", encoding="utf-8") as f:
    data = json.load(f)

# 1. Component Positions (Ultra Wide Layout)
comp_coords = {
    "power_3p_1": (-600.0, 300.0),
    "breaker_3p_1": (-300.0, 300.0),
    "fuse_3p_1": (0.0, 300.0),
    "km_1": (250.0, 50.0),
    "fr_1": (500.0, 50.0),
    "motor_1": (500.0, -200.0),
    "stop_1": (-400.0, -300.0),
    "start_1": (-100.0, -300.0)
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
    # power to breaker
    if start_c == "power_3p_1" and end_c == "breaker_3p_1":
        y_bus = 250
        if start_t == "L1": set_route(w, [(-620, y_bus), (-320, y_bus)])
        elif start_t == "L2": set_route(w, [(-600, y_bus - 15), (-300, y_bus - 15)])
        elif start_t == "L3": set_route(w, [(-580, y_bus - 30), (-280, y_bus - 30)])
    
    # breaker to fuse
    elif start_c == "breaker_3p_1" and end_c == "fuse_3p_1":
        y_bus = 200
        if start_t == "P1_OUT": set_route(w, [(-320, y_bus), (-20, y_bus)])
        elif start_t == "P2_OUT": set_route(w, [(-300, y_bus - 15), (0, y_bus - 15)])
        elif start_t == "P3_OUT": set_route(w, [(-280, y_bus - 30), (20, y_bus - 30)])
        
    # fuse to km_1 (Down and right)
    elif start_c == "fuse_3p_1" and end_c == "km_1":
        y_drop = 150
        if start_t == "L1_OUT": set_route(w, [(-20, y_drop), (230, y_drop)])
        elif start_t == "L2_OUT": set_route(w, [(0, y_drop - 15), (250, y_drop - 15)])
        elif start_t == "L3_OUT": set_route(w, [(20, y_drop - 30), (270, y_drop - 30)])
        
    # km_1 to fr_1 (Straight right)
    elif start_c == "km_1" and end_c == "fr_1":
        y_bus = -10
        if start_t == "T1": set_route(w, [(230, y_bus), (480, y_bus)])
        elif start_t == "T2": set_route(w, [(250, y_bus - 15), (500, y_bus - 15)])
        elif start_t == "T3": set_route(w, [(270, y_bus - 30), (520, y_bus - 30)])
        
    # fr_1 to motor_1 (Straight down, auto-route is fine but let's give precise drops)
    elif start_c == "fr_1" and end_c == "motor_1":
        y_drop = -100
        if start_t == "T1": set_route(w, [(480, y_drop), (480, y_drop-50)])
        elif start_t == "T2": set_route(w, [(500, y_drop), (500, y_drop-50)])
        elif start_t == "T3": set_route(w, [(520, y_drop), (520, y_drop-50)])
        
    # PE Line (power to motor)
    elif start_c == "power_3p_1" and end_c == "motor_1" and start_t == "PE":
        set_route(w, [(-680, 300), (-680, -400), (450, -400)])

    # --- Control Circuit ---
    elif start_c == "power_3p_1" and start_t == "L1" and end_c == "stop_1":
        set_route(w, [(-650, 300), (-650, -250), (-400, -250)])
        w["color"] = "#E74C3C" # Red
        
    elif start_c == "stop_1" and end_c == "fr_1" and end_t == "95":
        # From stop_1 to fr_1.95 (Right then up)
        set_route(w, [(-300, -300), (550, -300)])
        w["color"] = "#E74C3C"

    elif start_c == "fr_1" and start_t == "96" and end_c == "start_1":
        # From fr_1.96 to start_1.23
        set_route(w, [(580, 50), (580, -250), (-100, -250)])
        w["color"] = "#E74C3C"

    elif start_c == "start_1" and end_c == "km_1" and end_t == "A1":
        # From start_1.24 to km_1.A1
        set_route(w, [(0, -300), (300, -300), (300, 50)])
        w["color"] = "#E74C3C"

    elif start_c == "km_1" and start_t == "A2" and end_c == "power_3p_1":
        # From km_1.A2 to power L2
        set_route(w, [(320, 50), (320, 380), (-550, 380)])
        w["color"] = "#E74C3C"

    # --- Self-Locking Circuit ---
    elif start_c == "fr_1" and start_t == "96" and end_c == "km_1" and end_t == "13":
        set_route(w, [(570, 50), (570, 100), (300, 100)])
        w["color"] = "#9B59B6" # Purple
        
    elif start_c == "km_1" and start_t == "14" and end_c == "start_1":
        set_route(w, [(200, 50), (200, -230), (-50, -230)])
        w["color"] = "#9B59B6"

with open(path, "w", encoding="utf-8") as f:
    json.dump(data, f, indent=4, ensure_ascii=False)

print("Optimization complete.")
