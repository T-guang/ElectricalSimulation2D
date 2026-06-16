import re

with open('Assets/Scenes/Demo.unity', 'r', encoding='utf-8') as f:
    content = f.read()

blocks = content.split('\n--- ')

canvas_content_id = None
canvas_rect_id = None

# Find CanvasContent GameObj
for block in blocks:
    if block.startswith('!u!1 '):
        if 'm_Name: CanvasContent' in block:
            obj_id = block.split('\n')[0].split('&')[1].strip()
            canvas_content_id = obj_id
            print(f"Found CanvasContent GameObject ID: {canvas_content_id}")
            break

# Find its RectTransform
for block in blocks:
    if block.startswith('!u!224 '):
        if f'm_GameObject: {{fileID: {canvas_content_id}}}' in block:
            rect_id = block.split('\n')[0].split('&')[1].strip()
            canvas_rect_id = rect_id
            print(f"Found CanvasContent RectTransform ID: {canvas_rect_id}")
            
            # modify size
            new_block = re.sub(r'm_SizeDelta: \{x: ([\d.-]+), y: ([\d.-]+)\}', 'm_SizeDelta: {x: 4200, y: 2700}', block)
            if new_block != block:
                content = content.replace('\n--- ' + block, '\n--- ' + new_block)
                print("Successfully updated CanvasContent RectTransform.")
            break

with open('Assets/Scenes/Demo.unity', 'w', encoding='utf-8') as f:
    f.write(content)

