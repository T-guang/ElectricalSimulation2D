# 脚本映射与功能分类 (Script Map)

本文档整理了 `Assets/Scripts` 目录下所有的 C# 脚本，方便快速定位对应的功能。

## 1. 脚本分类与作用总结

### 核心控制器与系统 (Core)
- **`WorkspaceController.cs`**: 核心画板控制器。负责管理元件列表、导线列表、画布交互（平移/缩放）、撤销/重做、调用仿真服务等。
- **`SimulationEngine.cs`**: 简易电路仿真引擎逻辑。
- **`DemoRuntimeBootstrap.cs`**: 启动引导类，自动生成内置的“家庭照明示例电路”。
- **`AppSession.cs`**: 全局运行时状态管理，记录用户登录信息等。

### 持久化与图纸管理 (Save/Load)
- **`SaveLoadService.cs`**: 文件读写核心服务。处理 JSON 文件的持久化读写。
- **`SavedBlueprintInfo.cs`**: 保存的图纸数据模型（ID、名称、时间、文件路径等展示所需信息）。

### 画布基础元素 (Canvas Elements)
- **`CircuitComponent.cs`**: 元件基类。负责在画板中的坐标渲染、被选中状态、双击交互切换及引脚初始化。
- **`TerminalView.cs`**: 端子（引脚）视图类。绑定用户点击事件以触发接线操作。
- **`WireManager.cs`**: 导线管理器。统筹生成、删除、刷新导线位置。
- **`WireView.cs`**: 单根导线的视图类，处理高亮与路由显示。
- **`WireBendHandle.cs`**: 导线弯折点的手柄组件，支持手动拖拽路由控制。
- **`WorkspaceGrid.cs`**: 画布网格的背景绘制。

### 核心数据结构 (Data Structures)
- **`ComponentDefinition.cs`**: 元器件的预设模板定义，如长宽尺寸、引脚分布、默认颜色等。
- **`ComponentParameter.cs` / `ComponentParameterSet.cs`**: 元件的扩展参数及集合。
- **`ElectricalEnums.cs`**: 核心枚举类（如导线路由类型）。
- **`PageId.cs`**: 定义系统中的各个 UI 页面标签的枚举。
- **`PaletteItem.cs`**: 元件选择区中单一元件数据。

### UI 层 (UI Panels & Controllers)
- **`DemoUIController.cs`**: 主界面 UI 逻辑，负责将保存、加载、运行仿真等按钮绑定到底层服务。
- **`SaveBlueprintDialog.cs`**: 点击保存时弹出的命名窗口。
- **`ImportBlueprintPanel.cs`**: 点击导入时弹出的列表面板。
- **`SavedBlueprintListItem.cs`**: 导入面板中单个图纸项的 UI 预制体控制。
- **`BlueprintController.cs`** / **`BlueprintReferencePanel.cs`**: 内置预设图纸集的浏览和预览逻辑。
- **`PaletteController.cs`**: 左侧栏工具箱（元件库）管理。
- **`TopNavigationController.cs`**: 顶栏导航按钮。
- **`PageRouter.cs`**: 面板切换路由，控制界面大类的显隐。
- **`TemplateListItem.cs`** / **`TemplateLoadController.cs`** / **`TemplateSelectionPanel.cs`**: 标准模板相关 UI 逻辑。
- **`ComponentParameterView.cs`**: 右侧元件参数调节栏。
- **`MeasurementPanel.cs`** / **`OscilloscopeWaveform.cs`**: 测试仪表数据显示面板。
- **`EncyclopediaController.cs`**: 百科页面。
- **`CurrentUserView.cs`** / **`LoginController.cs`**: 用户系统与登录 UI。

---

## 2. 大致调用关系

- **保存数据**: `DemoUIController` (监听点击) -> `SaveBlueprintDialog` (UI输入) -> `SaveLoadService` (读取逻辑) -> 访问 `WorkspaceController` 中持有的列表数据 -> 序列化存盘。
- **加载数据**: `DemoUIController` -> `ImportBlueprintPanel` -> `SaveLoadService` (读取本地文件) -> `WorkspaceController` (清空并重新 Instantiate 元件和 `WireManager` 重新接线)。
- **操作画板**: `TerminalView` 响应点击 -> 发送到 `WorkspaceController` 确认选中状态 -> 如果是两个端子都选中 -> 调用 `WireManager.CreateWire` 生成一根 `WireView`。

---

## 3. 按核心业务划分的关联脚本

### 图纸保存、导入、删除涉及的脚本
- `DemoUIController.cs` (入口按钮)
- `SaveLoadService.cs` (I/O 和读写逻辑核心)
- `SaveBlueprintDialog.cs` (保存弹窗)
- `ImportBlueprintPanel.cs` (导入和删除列表)
- `SavedBlueprintListItem.cs` (列表项目)
- `SavedBlueprintInfo.cs` (列表项对应的数据结构)

### 元件、端子、接线涉及的脚本
- **元件**: `CircuitComponent.cs`, `ComponentDefinition.cs`
- **端子**: `TerminalView.cs`
- **接线**: `WireManager.cs`, `WireView.cs`, `WireBendHandle.cs`
- **中枢管理**: `WorkspaceController.cs` (负责协调选中状态、通知连线刷新)

### UI 页面切换涉及的脚本
- `PageId.cs` (定义有哪些页面)
- `PageRouter.cs` (处理具体的 GameObject 切换控制)
- `TopNavigationController.cs` (处理顶部按钮与样式的绑定)
