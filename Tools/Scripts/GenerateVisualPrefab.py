# ====================================================================
# Visual Prefab YAML 生成辅助脚本
# 当前由 Fuse Prefab 生成脚本归档而来，后续可扩展为通用 Visual Prefab 生成器。
#
# 注意：
# - 本脚本不属于 Unity Runtime 代码。
# - 本脚本不应放入 Assets 目录（避免被 Unity 强行生成 .meta）。
# ====================================================================
import os
import random

def generate_file_id():
    return random.randint(1000000, 9999999)

def create_prefab(template_path, out_path, image_guid, width, height, terminals):
    with open(template_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    root_id = generate_file_id()
    root_go_id = generate_file_id()
    
    body_id = generate_file_id()
    body_go_id = generate_file_id()
    body_renderer_id = generate_file_id()
    body_mono_id = generate_file_id()
    
    anchors_id = generate_file_id()
    anchors_go_id = generate_file_id()
    
    prefab_str = f'''%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!1 &{root_go_id}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {root_id}}}
  m_Layer: 5
  m_Name: VisualRoot
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &{root_id}
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {root_go_id}}}
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children:
  - {{fileID: {body_id}}}
  - {{fileID: {anchors_id}}}
  m_Father: {{fileID: 0}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
  m_AnchorMin: {{x: 0.5, y: 0.5}}
  m_AnchorMax: {{x: 0.5, y: 0.5}}
  m_AnchoredPosition: {{x: 0, y: 0}}
  m_SizeDelta: {{x: {width}, y: {height}}}
  m_Pivot: {{x: 0.5, y: 0.5}}
--- !u!1 &{body_go_id}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {body_id}}}
  - component: {{fileID: {body_renderer_id}}}
  - component: {{fileID: {body_mono_id}}}
  m_Layer: 5
  m_Name: Body
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &{body_id}
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {body_go_id}}}
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: {root_id}}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
  m_AnchorMin: {{x: 0, y: 0}}
  m_AnchorMax: {{x: 1, y: 1}}
  m_AnchoredPosition: {{x: 0, y: 0}}
  m_SizeDelta: {{x: 0, y: 0}}
  m_Pivot: {{x: 0.5, y: 0.5}}
--- !u!222 &{body_renderer_id}
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {body_go_id}}}
  m_CullTransparentMesh: 1
--- !u!114 &{body_mono_id}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {body_go_id}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: fe87c0e1cc204ed48ad3b37840f39efc, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  m_Material: {{fileID: 0}}
  m_Color: {{r: 1, g: 1, b: 1, a: 1}}
  m_RaycastTarget: 0
  m_RaycastPadding: {{x: 0, y: 0, z: 0, w: 0}}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Sprite: {{fileID: 21300000, guid: {image_guid}, type: 3}}
  m_Type: 0
  m_PreserveAspect: 0
  m_FillCenter: 1
  m_FillMethod: 4
  m_FillAmount: 1
  m_FillClockwise: 1
  m_FillOrigin: 0
  m_UseSpriteMesh: 0
  m_PixelsPerUnitMultiplier: 1
--- !u!1 &{anchors_go_id}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {anchors_id}}}
  m_Layer: 5
  m_Name: TerminalAnchors
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &{anchors_id}
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {anchors_go_id}}}
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children:
'''
    
    terminal_blocks = []
    for term in terminals:
        t_id = generate_file_id()
        t_go_id = generate_file_id()
        prefab_str += f"  - {{fileID: {t_id}}}\n"
        
        terminal_blocks.append(f'''--- !u!1 &{t_go_id}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {t_id}}}
  m_Layer: 5
  m_Name: Terminal_{term['name']}
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &{t_id}
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {t_go_id}}}
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: {anchors_id}}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
  m_AnchorMin: {{x: 0.5, y: 0.5}}
  m_AnchorMax: {{x: 0.5, y: 0.5}}
  m_AnchoredPosition: {{x: {term['x']:.2f}, y: {term['y']:.2f}}}
  m_SizeDelta: {{x: 28, y: 28}}
  m_Pivot: {{x: 0.5, y: 0.5}}
''')

    prefab_str += f'''  m_Father: {{fileID: {root_id}}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
  m_AnchorMin: {{x: 0.5, y: 0.5}}
  m_AnchorMax: {{x: 0.5, y: 0.5}}
  m_AnchoredPosition: {{x: 0, y: 0}}
  m_SizeDelta: {{x: {width}, y: {height}}}
  m_Pivot: {{x: 0.5, y: 0.5}}
'''
    prefab_str += "".join(terminal_blocks)

    with open(out_path, 'w', encoding='utf-8') as f:
        f.write(prefab_str)

import yaml
def read_guid(path):
    with open(path, 'r', encoding='utf-8') as f:
        for line in f:
            if line.startswith('guid:'):
                return line.split(':')[1].strip()
    return None

fuse1_guid = read_guid('Assets/Art/Components/Fuse_1P_Visual.png.meta')
fuse3_guid = read_guid('Assets/Art/Components/Fuse_3P_Visual.png.meta')

# Create 1P: 72 x 205
terms_1p = [
    {'name': 'IN', 'x': 0.0, 'y': 80.39},
    {'name': 'OUT', 'x': 0.0, 'y': -79.28}
]
create_prefab('Assets/Prefab/AC_ThreePhase_Power_Visual.prefab', 'Assets/Prefab/Fuse_1P_Visual.prefab', fuse1_guid, 72, 205, terms_1p)

# Create 3P: 216 x 205
terms_3p = [
    {'name': 'L1_IN', 'x': -58.00, 'y': 80.05},
    {'name': 'L1_OUT', 'x': -58.00, 'y': -80.05},
    {'name': 'L2_IN', 'x': 0.20, 'y': 80.05},
    {'name': 'L2_OUT', 'x': 0.20, 'y': -80.05},
    {'name': 'L3_IN', 'x': 58.98, 'y': 80.05},
    {'name': 'L3_OUT', 'x': 58.58, 'y': -80.05}
]
create_prefab('Assets/Prefab/AC_ThreePhase_Power_Visual.prefab', 'Assets/Prefab/Fuse_3P_Visual.prefab', fuse3_guid, 216, 205, terms_3p)

print("Prefabs regenerated with new sizes successfully.")

