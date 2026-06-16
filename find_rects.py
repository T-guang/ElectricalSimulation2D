import yaml
import sys

def parse_unity_yaml():
    with open('Assets/Scenes/Demo.unity', 'r', encoding='utf-8') as f:
        content = f.read()

    blocks = content.split('\n--- ')
    
    game_objects = {}
    rect_transforms = {}

    for block in blocks:
        lines = block.split('\n')
        if len(lines) < 2: continue
        
        header = lines[0]
        if 'GameObject:' in block:
            obj_id = header.split('&')[1].strip() if '&' in header else None
            name = None
            components = []
            for line in lines:
                if 'm_Name:' in line:
                    name = line.split('m_Name:')[1].strip()
                if '- component:' in line:
                    try:
                        comp_id = line.split('fileID:')[1].split('}')[0].strip()
                        components.append(comp_id)
                    except: pass
            if obj_id:
                game_objects[obj_id] = {'name': name, 'components': components}
                
        elif 'RectTransform:' in block:
            obj_id = header.split('&')[1].strip() if '&' in header else None
            size_delta = None
            game_obj_id = None
            for line in lines:
                if 'm_GameObject:' in line:
                    try:
                        game_obj_id = line.split('fileID:')[1].split('}')[0].strip()
                    except: pass
                if 'm_SizeDelta:' in line:
                    size_delta = line
            if obj_id:
                rect_transforms[obj_id] = {'game_obj_id': game_obj_id, 'size_delta': size_delta, 'block': block}

    for rid, rect in rect_transforms.items():
        gid = rect['game_obj_id']
        if gid in game_objects:
            name = game_objects[gid]['name']
            if any(k in name.lower() for k in ['content', 'workspace', 'canvas', 'simulation', 'grid', 'viewport']):
                print(f"Name: {name}, SizeDelta: {rect['size_delta']}, RectID: {rid}")

parse_unity_yaml()
