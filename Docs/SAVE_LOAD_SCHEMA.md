# 图纸保存与加载结构说明 (Save/Load Schema)

## 1. 核心类职责
**`SaveLoadService`** 的职责是项目的序列化与反序列化中枢：
1. 提供 `SaveAs` 方法，从 `WorkspaceController` 获取画布数据，转换为可序列化的 `DrawingDto`。
2. 将 `DrawingDto` 使用 `JsonUtility` 转换为 JSON 文本存入本地磁盘。
3. 提供 `ListSavedBlueprints` 方法，遍历读取本地 JSON 文件，快速构建 `SavedBlueprintInfo` 列表用于 UI 展示。
4. 提供 `LoadFromFile` 方法，读取对应 JSON 文本并恢复画板内容。
5. 提供 `DeleteSavedBlueprint` 物理删除 JSON 文件。

## 2. 核心数据结构说明

### `DrawingDto` 的整体作用
用于封装整个图纸文件的根对象。它包括了元数据（文档ID、文档名、保存时间）以及具体的物理内容清单（元件列表和接线列表）。这是一个严格用来对接 `JsonUtility` 的序列化对象。

### 子数据结构
- **`ComponentDto`**: 代表画布上的一个元件实例。包含 `instanceId`（全局唯一ID）、`definitionName`（关联哪一种模板类型）、`x/y`（坐标位置）、`isClosed`（当前开关状态）、`parameters`（内部参数信息）。
- **`WireDto`**: 代表一根导线。记录连接的两端信息（`startComponentId`, `startTerminalId`, `endComponentId`, `endTerminalId`）以及视觉信息（`color`, `style`, 是否有手动弯折点 `manualRoutePoints` 等）。
- **`SavedBlueprintInfo`**: 此对象不直接用于 JSON 序列化存储，而是 `SaveLoadService` 在读取 JSON 文件后，向上层 UI 传递的简要展示信息集合（包括文件路径、生成的文件名、最后修改时间等）。

## 3. 当前的完整工作流

- **保存流程 (Save)**
  1. UI 层触发（`DemoUIController` -> `SaveBlueprintDialog`），用户确定“图纸名称”。
  2. 弹窗调用 `SaveLoadService.SaveAs(documentName)`。
  3. 服务调用 `CreateDrawingDto()`，遍历 `WorkspaceController` 中的所有元件（转为 `ComponentDto`）和 `WireManager` 中的导线（转为 `WireDto`）。
  4. 使用 `JsonUtility.ToJson(drawing)`，并写入到 `Application.persistentDataPath/SavedBlueprints/` 目录，文件名为过滤后的安全字符名 `.json`。

- **导入流程 (Load)**
  1. UI 层触发（`ImportBlueprintPanel` 列表点击某一项的导入按钮）。
  2. 面板拿到对应的文件路径，调用 `SaveLoadService.LoadFromFile(filePath)`。
  3. 读取文件转为 JSON 并反序列化得到 `DrawingDto`。
  4. 清空 `WorkspaceController` 的画板。
  5. 遍历 `components`，通过名称找回对应的 `ComponentDefinition` 并 Spawn 出新的 `CircuitComponent`。
  6. 遍历 `wires`，通过记录的端点 ID 生成 `WireView` 并恢复弯折信息。
  7. 刷新整个画板的计算状态并写入历史栈初始态。

- **删除流程 (Delete)**
  1. `ImportBlueprintPanel` 触发删除逻辑（经过用户弹窗确认）。
  2. 将选中的文件路径传入 `SaveLoadService.DeleteSavedBlueprint(filePath)`。
  3. 服务做安全校验，确保仅删除目标目录下的 `.json` 文件。
  4. 物理删除文件 `File.Delete()`。
  5. UI 面板主动调用刷新，重新拉取最新文件列表。

## 4. 后续扩展字段与维护注意事项

后续在做 UI 和图纸管理功能增强时，可能会涉及扩展诸如：图纸分类、备注说明、封面缩略图等。此时请务必遵守以下原则：

1. **不要随意更改原有 JSON 字段名**
   Unity 自带的 `JsonUtility` 强依赖变量命名。若更改 `DrawingDto` 等类中的现有变量名，将会导致旧版 JSON 存档文件无法被反序列化，读档失败。
2. **新增字段必须给默认值或处理空值**
   若给 `DrawingDto` 新增例如 `public string category;`。读取老存档时，该字段为空。在恢复 UI 时需要有向下兼容逻辑，例如 `string.IsNullOrEmpty(drawing.category) ? "未分类" : drawing.category`。
3. **扩充缩略图等重度数据时的考量**
   如果需要保存缩略图，建议不要把图片 Base64 写在 `DrawingDto` 中（避免 JSON 文件过大影响解析性能）。推荐的方法是保存一张同名图片文件（例如 `blueprint_1.png`），并在 UI 加载时使用路径匹配的策略。
4. **扩展图纸列表展现**
   列表展示用的是 `SavedBlueprintInfo` 数据，它是由 `DrawingDto` 解析得来。如果 `DrawingDto` 增加了“分类”字段，记得同步把该字段传给 `SavedBlueprintInfo` 以便列表 UI（`SavedBlueprintListItem`）能够直接读取展示，不需要重新解析文件。
