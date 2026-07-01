import os

def count_lines(filepath):
    try:
        with open(filepath, 'r', encoding='utf-8') as f:
            return sum(1 for line in f)
    except:
        return 0

stats = {
    'cs': {'count': 0, 'lines': 0},
    'prefab': {'count': 0},
    'asset': {'count': 0},
    'png': {'count': 0},
    'mat': {'count': 0},
    'unity': {'count': 0},
    'json': {'count': 0, 'lines': 0}
}

for root, dirs, files in os.walk('Assets'):
    for file in files:
        ext = file.split('.')[-1].lower()
        filepath = os.path.join(root, file)
        
        if ext == 'cs':
            stats['cs']['count'] += 1
            stats['cs']['lines'] += count_lines(filepath)
        elif ext == 'json':
            stats['json']['count'] += 1
            stats['json']['lines'] += count_lines(filepath)
        elif ext in stats:
            stats[ext]['count'] += 1

print('--- 项目代码统计 ---')
print(f"C# 脚本数量: {stats['cs']['count']} 个")
print(f"C# 代码行数: {stats['cs']['lines']} 行")
print('\n--- 资源文件统计 ---')
print(f"Prefab 预制体数量: {stats['prefab']['count']} 个")
print(f"Asset 数据文件数量: {stats['asset']['count']} 个")
print(f"PNG 图片数量: {stats['png']['count']} 张")
print(f"Material 材质数量: {stats['mat']['count']} 个")
print(f"Unity 场景数量: {stats['unity']['count']} 个")
print(f"JSON 模板数据: {stats['json']['count']} 个 (共 {stats['json']['lines']} 行)")
