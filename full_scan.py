import os
import json

def count_lines(filepath):
    try:
        with open(filepath, 'r', encoding='utf-8') as f:
            return sum(1 for line in f)
    except:
        return 0

def get_size(filepath):
    try:
        return os.path.getsize(filepath)
    except:
        return 0

stats = {}

ignore_dirs = {'.git', 'Library', 'Logs', 'Temp', 'obj', 'UserSettings', 'Build'}

total_size = 0
total_files = 0

for root, dirs, files in os.walk('.'):
    # filter ignored directories
    dirs[:] = [d for d in dirs if d not in ignore_dirs]
    
    for file in files:
        if file.startswith('.'): continue
        ext = file.split('.')[-1].lower() if '.' in file else 'unknown'
        filepath = os.path.join(root, file)
        
        lines = 0
        if ext in ['cs', 'py', 'json', 'txt', 'shader', 'md', 'cginc']:
            lines = count_lines(filepath)
            
        size = get_size(filepath)
        
        total_size += size
        total_files += 1
        
        if ext not in stats:
            stats[ext] = {'count': 0, 'lines': 0, 'size': 0}
            
        stats[ext]['count'] += 1
        stats[ext]['lines'] += lines
        stats[ext]['size'] += size

# Output JSON so it's easy to read for the agent
with open('project_stats_output.json', 'w', encoding='utf-8') as f:
    json.dump({'total_files': total_files, 'total_size': total_size, 'stats': stats}, f, ensure_ascii=False, indent=2)
