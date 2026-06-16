import json

path = "Assets/Resources/Blueprints/Templates/motor_forward_reverse_interlock_template.json"
with open(path, "r", encoding="utf-8") as f:
    data = json.load(f)

# Ultra-wide Waterfall Layout
comp_coords = {
    "power_3p_1": (-800.0, 450.0),   
    "breaker_3p_1": (-300.0, 300.0), 
    "fuse_3p_1": (100.0, 150.0),     
    "km_forward": (400.0, 0.0),      
    "km_reverse": (900.0, 0.0),      
    "motor_1": (650.0, -350.0),      
    "stop_1": (-400.0, -250.0),
    "forward_button_1": (-100.0, -250.0),
    "reverse_button_1": (200.0, -250.0)
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
    w["color"] = "#E74C3C" # default control color
    
    # --- Main Circuit ---
    if start_c == "power_3p_1" and end_c == "breaker_3p_1":
        y_bus = 380
        if start_t == "L1": set_route(w, [(-850, y_bus), (-320, y_bus)]); w["color"] = "#F2C71F"
        elif start_t == "L2": set_route(w, [(-800, y_bus-15), (-300, y_bus-15)]); w["color"] = "#14A640"
        elif start_t == "L3": set_route(w, [(-750, y_bus-30), (-280, y_bus-30)]); w["color"] = "#E74C3C"

    elif start_c == "breaker_3p_1" and end_c == "fuse_3p_1":
        y_bus = 230
        if start_t == "P1_OUT": set_route(w, [(-320, y_bus), (80, y_bus)]); w["color"] = "#F2C71F"
        elif start_t == "P2_OUT": set_route(w, [(-300, y_bus-15), (100, y_bus-15)]); w["color"] = "#14A640"
        elif start_t == "P3_OUT": set_route(w, [(-280, y_bus-30), (120, y_bus-30)]); w["color"] = "#E74C3C"

    elif start_c == "fuse_3p_1" and end_c == "km_forward":
        y_bus = 80
        if start_t == "L1_OUT": set_route(w, [(80, y_bus), (380, y_bus)]); w["color"] = "#F2C71F"
        elif start_t == "L2_OUT": set_route(w, [(100, y_bus-15), (400, y_bus-15)]); w["color"] = "#14A640"
        elif start_t == "L3_OUT": set_route(w, [(120, y_bus-30), (420, y_bus-30)]); w["color"] = "#E74C3C"

    elif start_c == "fuse_3p_1" and end_c == "km_reverse":
        y_bus = 120 
        if start_t == "L1_OUT": set_route(w, [(80, y_bus), (880, y_bus)]); w["color"] = "#F2C71F"
        elif start_t == "L2_OUT": set_route(w, [(100, y_bus-15), (900, y_bus-15)]); w["color"] = "#14A640"
        elif start_t == "L3_OUT": set_route(w, [(120, y_bus-30), (920, y_bus-30)]); w["color"] = "#E74C3C"

    elif start_c == "km_forward" and end_c == "motor_1":
        y_bus = -100
        if start_t == "T1": set_route(w, [(380, y_bus), (550, y_bus), (550, -320)]); w["color"] = "#F2C71F"
        elif start_t == "T2": set_route(w, [(400, y_bus-15), (570, y_bus-15), (570, -320)]); w["color"] = "#14A640"
        elif start_t == "T3": set_route(w, [(420, y_bus-30), (590, y_bus-30), (590, -320)]); w["color"] = "#E74C3C"

    elif start_c == "km_reverse" and end_c == "motor_1":
        y_bus = -160
        if start_t == "T1" and end_t == "W": set_route(w, [(880, y_bus), (590, y_bus), (590, -300)]); w["color"] = "#F2C71F"
        elif start_t == "T2" and end_t == "V": set_route(w, [(900, y_bus-15), (570, y_bus-15), (570, -300)]); w["color"] = "#14A640"
        elif start_t == "T3" and end_t == "U": set_route(w, [(920, y_bus-30), (550, y_bus-30), (550, -300)]); w["color"] = "#E74C3C"

    elif start_c == "power_3p_1" and start_t == "PE" and end_c == "motor_1":
        set_route(w, [(-650, 400), (-650, -450), (650, -450)]); w["color"] = "#14A640"

    # --- Control Circuit ---
    elif start_c == "power_3p_1" and start_t == "L1" and end_c == "stop_1":
        set_route(w, [(-900, 380), (-900, -250), (-450, -250)])
        
    elif start_c == "stop_1" and start_t == "12" and end_c == "forward_button_1":
        set_route(w, [(-350, -250), (-150, -250)])
    
    elif start_c == "stop_1" and start_t == "12" and end_c == "reverse_button_1":
        set_route(w, [(-350, -250), (-350, -320), (150, -320), (150, -250)])
        
    elif start_c == "forward_button_1" and start_t == "24" and end_c == "km_reverse" and end_t == "21":
        set_route(w, [(-50, -250), (750, -250), (750, -20)])
        
    elif start_c == "km_reverse" and start_t == "22" and end_c == "km_forward" and end_t == "A1":
        set_route(w, [(980, -20), (1050, -20), (1050, 50), (280, 50), (280, 20)])
        
    elif start_c == "reverse_button_1" and start_t == "24" and end_c == "km_forward" and end_t == "21":
        set_route(w, [(250, -250), (250, -20)])
        
    elif start_c == "km_forward" and start_t == "22" and end_c == "km_reverse" and end_t == "A1":
        set_route(w, [(480, -20), (780, -20), (780, 20)])
        
    elif start_c == "stop_1" and start_t == "12" and end_c == "km_forward" and end_t == "13":
        set_route(w, [(-350, -250), (-350, -350), (200, -350), (200, 20)])
        
    elif start_c == "stop_1" and start_t == "12" and end_c == "km_reverse" and end_t == "13":
        set_route(w, [(-350, -250), (-350, -350), (700, -350), (700, 20)])
        
    elif start_c == "km_forward" and start_t == "14" and end_c == "forward_button_1" and end_t == "24":
        set_route(w, [(480, 20), (480, 70), (-50, 70), (-50, -200)])
        
    elif start_c == "km_reverse" and start_t == "14" and end_c == "reverse_button_1" and end_t == "24":
        set_route(w, [(980, 20), (980, 70), (250, 70), (250, -200)])

    elif start_t == "A2" and end_c == "power_3p_1":
        if start_c == "km_forward":
            set_route(w, [(500, 20), (500, 550), (-800, 550)])
        elif start_c == "km_reverse":
            set_route(w, [(1000, 20), (1000, 580), (-800, 580)])

with open(path, "w", encoding="utf-8") as f:
    json.dump(data, f, indent=4, ensure_ascii=False)

print("Optimization complete.")
