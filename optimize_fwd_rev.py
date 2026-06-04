import json

path = "Assets/Resources/Blueprints/Templates/motor_forward_reverse_control_template.json"
with open(path, "r", encoding="utf-8") as f:
    data = json.load(f)

# 1. Update component positions (方案A: 上主回路, 下控制回路)
comp_coords = {
    "power_3p_1": (-500.0, 280.0),
    "breaker_3p_1": (-200.0, 280.0),
    "fuse_3p_1": (100.0, 280.0),
    
    "km_forward": (-100.0, 40.0),
    "km_reverse": (250.0, 40.0),
    
    "motor_1": (75.0, -200.0),
    
    "stop_1": (-400.0, -280.0),
    "forward_button_1": (-100.0, -280.0),
    "reverse_button_1": (150.0, -280.0)
}

for c in data.get("components", []):
    cid = c.get("instanceId")
    if cid in comp_coords:
        c["x"], c["y"] = comp_coords[cid]

# 2. Add manual route points to wires
def set_route(wire, points):
    wire["manualRoutePoints"] = [{"x": p[0], "y": p[1]} for p in points]
    if len(points) >= 2:
        wire["manualRouteHorizontal"] = (abs(points[0][1] - points[1][1]) < 1)
        wire["manualRouteAxis"] = points[0][1] if wire["manualRouteHorizontal"] else points[0][0]

for w in data.get("wires", []):
    start_c = w.get("startComponentId")
    start_t = w.get("startTerminalId")
    end_c = w.get("endComponentId")
    end_t = w.get("endTerminalId")

    # Clear existing routes first to avoid mess
    if "manualRoutePoints" in w:
        w["manualRoutePoints"] = []
    
    # 1. power to breaker
    if start_c == "power_3p_1" and end_c == "breaker_3p_1":
        y_bus = 220
        if start_t == "L1": set_route(w, [(-520, y_bus), (-220, y_bus)])
        elif start_t == "L2": set_route(w, [(-500, y_bus - 10), (-200, y_bus - 10)])
        elif start_t == "L3": set_route(w, [(-480, y_bus - 20), (-180, y_bus - 20)])
    
    # 2. breaker to fuse
    elif start_c == "breaker_3p_1" and end_c == "fuse_3p_1":
        y_bus = 200
        if start_t == "P1_OUT": set_route(w, [(-220, y_bus), (80, y_bus)])
        elif start_t == "P2_OUT": set_route(w, [(-200, y_bus - 10), (100, y_bus - 10)])
        elif start_t == "P3_OUT": set_route(w, [(-180, y_bus - 20), (120, y_bus - 20)])
        
    # 3. fuse to km_forward
    elif start_c == "fuse_3p_1" and end_c == "km_forward":
        y_bus = 140
        if start_t == "L1_OUT": set_route(w, [(80, y_bus), (-120, y_bus)])
        elif start_t == "L2_OUT": set_route(w, [(100, y_bus - 10), (-100, y_bus - 10)])
        elif start_t == "L3_OUT": set_route(w, [(120, y_bus - 20), (-80, y_bus - 20)])

    # 4. fuse to km_reverse
    elif start_c == "fuse_3p_1" and end_c == "km_reverse":
        y_bus = 160
        if start_t == "L1_OUT": set_route(w, [(80, y_bus), (230, y_bus)])
        elif start_t == "L2_OUT": set_route(w, [(100, y_bus - 10), (250, y_bus - 10)])
        elif start_t == "L3_OUT": set_route(w, [(120, y_bus - 20), (270, y_bus - 20)])

    # 5. km_forward to motor
    elif start_c == "km_forward" and end_c == "motor_1":
        y_bus = -60
        if start_t == "T1": set_route(w, [(-120, y_bus), (55, y_bus)])
        elif start_t == "T2": set_route(w, [(-100, y_bus - 10), (75, y_bus - 10)])
        elif start_t == "T3": set_route(w, [(-80, y_bus - 20), (95, y_bus - 20)])

    # 6. km_reverse to motor (Reverse L1 & L3)
    elif start_c == "km_reverse" and end_c == "motor_1":
        y_bus = -80
        if start_t == "T1": set_route(w, [(230, y_bus), (95, y_bus)]) # L1 to W
        elif start_t == "T2": set_route(w, [(250, y_bus - 10), (75, y_bus - 10)]) # L2 to V
        elif start_t == "T3": set_route(w, [(270, y_bus - 20), (55, y_bus - 20)]) # L3 to U

    # 7. Control Circuit Route Points
    elif start_c == "power_3p_1" and end_c == "stop_1" and start_t == "L1":
        set_route(w, [(-560, -280)])
    
    elif start_c == "power_3p_1" and end_t == "A2":
        set_route(w, [(-460, 20), (10, 20)])

    # PE Line
    elif start_c == "power_3p_1" and end_c == "motor_1" and start_t == "PE":
        set_route(w, [(-600, 360), (-600, -200), (0, -200)])

with open(path, "w", encoding="utf-8") as f:
    json.dump(data, f, indent=4, ensure_ascii=False)

print("Optimization complete.")
