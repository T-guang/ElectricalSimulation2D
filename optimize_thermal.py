import json

path = "Assets/Resources/Blueprints/Templates/motor_thermal_protection_template.json"
with open(path, "r", encoding="utf-8") as f:
    data = json.load(f)

# 1. Component Positions
comp_coords = {
    "power_3p_1": (-400.0, 250.0),
    "breaker_3p_1": (-100.0, 250.0),
    "fuse_3p_1": (200.0, 250.0),
    "km_1": (200.0, 50.0),
    "fr_1": (200.0, -100.0),
    "motor_1": (200.0, -250.0),
    "stop_1": (-150.0, -100.0),
    "start_1": (0.0, -100.0)
}

for c in data.get("components", []):
    cid = c.get("instanceId")
    if cid in comp_coords:
        c["x"], c["y"] = comp_coords[cid]

# 2. Wire Routing
def set_route(wire, points):
    wire["manualRoutePoints"] = [{"x": float(p[0]), "y": float(p[1])} for p in points]
    if len(points) >= 2:
        # Determine if the first segment is horizontal
        is_horizontal = abs(points[0][1] - points[1][1]) < 1.0
        wire["manualRouteHorizontal"] = is_horizontal
        wire["manualRouteAxis"] = float(points[0][1]) if is_horizontal else float(points[0][0])

for w in data.get("wires", []):
    start_c = w.get("startComponentId")
    start_t = w.get("startTerminalId")
    end_c = w.get("endComponentId")
    end_t = w.get("endTerminalId")

    # Remove manualRoutePoints completely first to avoid the [] bug
    w.pop("manualRoutePoints", None)
    w.pop("manualRouteHorizontal", None)
    w.pop("manualRouteAxis", None)
    
    # --- Main Circuit ---
    # power to breaker
    if start_c == "power_3p_1" and end_c == "breaker_3p_1":
        y_bus = 200
        if start_t == "L1": set_route(w, [(-420, y_bus), (-120, y_bus)])
        elif start_t == "L2": set_route(w, [(-400, y_bus - 10), (-100, y_bus - 10)])
        elif start_t == "L3": set_route(w, [(-380, y_bus - 20), (-80, y_bus - 20)])
    
    # breaker to fuse
    elif start_c == "breaker_3p_1" and end_c == "fuse_3p_1":
        y_bus = 160
        if start_t == "P1_OUT": set_route(w, [(-120, y_bus), (180, y_bus)])
        elif start_t == "P2_OUT": set_route(w, [(-100, y_bus - 10), (200, y_bus - 10)])
        elif start_t == "P3_OUT": set_route(w, [(-80, y_bus - 20), (220, y_bus - 20)])
        
    # fuse to km_1 (straight down)
    elif start_c == "fuse_3p_1" and end_c == "km_1":
        pass # Let the engine auto-route the straight drop
        
    # km_1 to fr_1 (straight down)
    elif start_c == "km_1" and end_c == "fr_1":
        pass # Let the engine auto-route
        
    # fr_1 to motor_1 (straight down)
    elif start_c == "fr_1" and end_c == "motor_1":
        pass # Let the engine auto-route
        
    # PE Line (power to motor)
    elif start_c == "power_3p_1" and end_c == "motor_1" and start_t == "PE":
        set_route(w, [(-480, 250), (-480, -250), (100, -250)])

    # --- Control Circuit ---
    elif start_c == "power_3p_1" and start_t == "L1" and end_c == "stop_1":
        set_route(w, [(-460, 250), (-460, -100), (-200, -100)])
        w["color"] = "#E74C3C" # Red for control
        
    elif start_c == "stop_1" and end_c == "fr_1" and end_t == "95":
        set_route(w, [(-100, -100), (140, -100)])
        w["color"] = "#E74C3C"

    elif start_c == "fr_1" and start_t == "96" and end_c == "start_1":
        set_route(w, [(260, -100), (260, -140), (50, -140)])
        w["color"] = "#E74C3C"

    elif start_c == "start_1" and end_c == "km_1" and end_t == "A1":
        set_route(w, [(0, -50), (0, 50), (140, 50)])
        w["color"] = "#E74C3C"

    elif start_c == "km_1" and start_t == "A2" and end_c == "power_3p_1":
        set_route(w, [(260, 50), (320, 50), (320, 320), (-400, 320)])
        w["color"] = "#E74C3C"

    # --- Self-Locking Circuit ---
    elif start_c == "fr_1" and start_t == "96" and end_c == "km_1" and end_t == "13":
        set_route(w, [(280, -100), (280, 50)])
        w["color"] = "#9B59B6" # Purple for self-lock
        
    elif start_c == "km_1" and start_t == "14" and end_c == "start_1":
        set_route(w, [(150, 50), (150, 10), (20, 10), (20, -50)])
        w["color"] = "#9B59B6"

with open(path, "w", encoding="utf-8") as f:
    json.dump(data, f, indent=4, ensure_ascii=False)

print("Optimization complete.")
