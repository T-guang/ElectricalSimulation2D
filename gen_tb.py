import random

def gen_id(): return str(random.randint(100000000000000000, 999999999999999999))

prefabs_data = [
    {
        'name': 'TerminalBlock_2H_Visual',
        'guid': 'eaa4e17096824363bb0301704086ea74',
        'img_w': 468, 'img_h': 445, 'pref_w': 140, 'pref_h': 133,
        'terminals': [
            {'id': 'T1', 'x': 146.5, 'y': 150.5}, {'id': 'T2', 'x': 327, 'y': 150.5},
            {'id': 'B1', 'x': 146.5, 'y': 296.5}, {'id': 'B2', 'x': 327, 'y': 296.5}
        ]
    },
    {
        'name': 'TerminalBlock_3H_Visual',
        'guid': '8f439d6b0147405890fcad5017364319',
        'img_w': 701, 'img_h': 445, 'pref_w': 210, 'pref_h': 133,
        'terminals': [
            {'id': 'T1', 'x': 170.5, 'y': 150.5}, {'id': 'T2', 'x': 353.5, 'y': 150.5}, {'id': 'T3', 'x': 536, 'y': 150.5},
            {'id': 'B1', 'x': 170.5, 'y': 297}, {'id': 'B2', 'x': 353.5, 'y': 297}, {'id': 'B3', 'x': 536, 'y': 297}
        ]
    },
    {
        'name': 'TerminalBlock_6H_Visual',
        'guid': '993f6137a64344a9875fb19ea167d8ef',
        'img_w': 1117, 'img_h': 445, 'pref_w': 330, 'pref_h': 132,
        'terminals': [
            {'id': 'T1', 'x': 108, 'y': 147.5}, {'id': 'T2', 'x': 287.5, 'y': 149.5}, {'id': 'T3', 'x': 469, 'y': 148.5},
            {'id': 'T4', 'x': 650.5, 'y': 149.5}, {'id': 'T5', 'x': 830.5, 'y': 150.5}, {'id': 'T6', 'x': 1011, 'y': 149.5},
            {'id': 'B1', 'x': 108, 'y': 295}, {'id': 'B2', 'x': 287.5, 'y': 296}, {'id': 'B3', 'x': 469, 'y': 296},
            {'id': 'B4', 'x': 650, 'y': 296}, {'id': 'B5', 'x': 830.5, 'y': 296}, {'id': 'B6', 'x': 1011, 'y': 296}
        ]
    },
    {
        'name': 'TerminalBlock_2V_Visual',
        'guid': '79316235739f4ef0a7fd7badc1f86cbb',
        'img_w': 445, 'img_h': 468, 'pref_w': 133, 'pref_h': 140,
        'terminals': [
            {'id': 'L1', 'x': 148.5, 'y': 146.5}, {'id': 'L2', 'x': 148.5, 'y': 327},
            {'id': 'R1', 'x': 294.5, 'y': 146.5}, {'id': 'R2', 'x': 294.5, 'y': 327}
        ]
    },
    {
        'name': 'TerminalBlock_3V_Visual',
        'guid': 'd4063f316380462185ba1253b18b8343',
        'img_w': 445, 'img_h': 701, 'pref_w': 133, 'pref_h': 210,
        'terminals': [
            {'id': 'L1', 'x': 149, 'y': 170.5}, {'id': 'L2', 'x': 149, 'y': 353.5}, {'id': 'L3', 'x': 149, 'y': 536},
            {'id': 'R1', 'x': 295.5, 'y': 170.5}, {'id': 'R2', 'x': 295.5, 'y': 353.5}, {'id': 'R3', 'x': 295.5, 'y': 536}
        ]
    },
    {
        'name': 'TerminalBlock_6V_Visual',
        'guid': 'a5b0eba739544bf4957ea3b68d3cce92',
        'img_w': 445, 'img_h': 1117, 'pref_w': 132, 'pref_h': 330,
        'terminals': [
            {'id': 'L1', 'x': 150, 'y': 109}, {'id': 'L2', 'x': 149, 'y': 288.5}, {'id': 'L3', 'x': 149, 'y': 470},
            {'id': 'L4', 'x': 149, 'y': 651}, {'id': 'L5', 'x': 149, 'y': 831.5}, {'id': 'L6', 'x': 149, 'y': 1012},
            {'id': 'R1', 'x': 297.5, 'y': 109}, {'id': 'R2', 'x': 295.5, 'y': 288.5}, {'id': 'R3', 'x': 296.5, 'y': 470},
            {'id': 'R4', 'x': 295.5, 'y': 651.5}, {'id': 'R5', 'x': 294.5, 'y': 831.5}, {'id': 'R6', 'x': 295.5, 'y': 1012}
        ]
    }
]

for pdata in prefabs_data:
    root_id, root_go_id = gen_id(), gen_id()
    bb_id, bb_go_id, bb_rend, bb_mono = gen_id(), gen_id(), gen_id(), gen_id()
    ta_id, ta_go_id = gen_id(), gen_id()
    
    term_children_ids = []
    terms_yaml = ''
    for t in pdata['terminals']:
        t_id, t_go_id = gen_id(), gen_id()
        term_children_ids.append(t_id)
        
        px = round((t['x'] / pdata['img_w'] - 0.5) * pdata['pref_w'], 3)
        py = round((0.5 - t['y'] / pdata['img_h']) * pdata['pref_h'], 3)
        
        terms_yaml += f'''--- !u!1 &{t_go_id}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {t_id}}}
  m_Layer: 5
  m_Name: Terminal_{t['id']}
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
  m_Father: {{fileID: {ta_id}}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
  m_AnchorMin: {{x: 0.5, y: 0.5}}
  m_AnchorMax: {{x: 0.5, y: 0.5}}
  m_AnchoredPosition: {{x: {px}, y: {py}}}
  m_SizeDelta: {{x: 12, y: 12}}
  m_Pivot: {{x: 0.5, y: 0.5}}
'''
    
    ta_children = '\n  - '.join([f'{{fileID: {cid}}}' for cid in term_children_ids])
    if ta_children: ta_children = '\n  - ' + ta_children
    
    yaml = f'''%YAML 1.1
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
  m_Name: {pdata['name']}
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
  - {{fileID: {bb_id}}}
  - {{fileID: {ta_id}}}
  m_Father: {{fileID: 0}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
  m_AnchorMin: {{x: 0.5, y: 0.5}}
  m_AnchorMax: {{x: 0.5, y: 0.5}}
  m_AnchoredPosition: {{x: 0, y: 0}}
  m_SizeDelta: {{x: {pdata['pref_w']}, y: {pdata['pref_h']}}}
  m_Pivot: {{x: 0.5, y: 0.5}}
--- !u!1 &{bb_go_id}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {bb_id}}}
  - component: {{fileID: {bb_rend}}}
  - component: {{fileID: {bb_mono}}}
  m_Layer: 5
  m_Name: Body
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &{bb_id}
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {bb_go_id}}}
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
--- !u!222 &{bb_rend}
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {bb_go_id}}}
  m_CullTransparentMesh: 1
--- !u!114 &{bb_mono}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {bb_go_id}}}
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
  m_Sprite: {{fileID: 21300000, guid: {pdata['guid']}, type: 3}}
  m_Type: 0
  m_PreserveAspect: 0
  m_FillCenter: 1
  m_FillMethod: 4
  m_FillAmount: 1
  m_FillClockwise: 1
  m_FillOrigin: 0
  m_UseSpriteMesh: 0
  m_PixelsPerUnitMultiplier: 1
--- !u!1 &{ta_go_id}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {ta_id}}}
  m_Layer: 5
  m_Name: TerminalAnchors
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &{ta_id}
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {ta_go_id}}}
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: {ta_children}
  m_Father: {{fileID: {root_id}}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
  m_AnchorMin: {{x: 0, y: 0}}
  m_AnchorMax: {{x: 1, y: 1}}
  m_AnchoredPosition: {{x: 0, y: 0}}
  m_SizeDelta: {{x: 0, y: 0}}
  m_Pivot: {{x: 0.5, y: 0.5}}
'''
    yaml += terms_yaml
    
    with open(f"Assets/Prefab/{pdata['name']}.prefab", 'w', encoding='utf-8') as f:
        f.write(yaml)
        
print('Generated 6 prefabs!')
