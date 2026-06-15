# V1 脚本作用与风险地图

## 1. 总览

- 扫描范围：`Assets/Scripts`
- C# 脚本：81 个
- 总行数：约 21,829
- 模块：Core 12、UI 30、AI 11、Rules 6、Templates 5、Practice 15、Editor 2

风险定义：

- **高风险**：运行、分析、拓扑、保存加载或模板生成核心链路；修改必须全模板回归。
- **中风险**：面板、AI、规则展示、练习流程；修改应做对应功能回归。
- **低风险**：DTO、枚举、纯显示辅助、Editor-only 工具；仍应编译验证。

## 2. 高风险核心链路

| 脚本路径 | 主要职责 | 主要调用/依赖 | 风险与修改建议 |
|---|---|---|---|
| `Core/WorkspaceController.cs` | 画布中枢；元件/导线、选择、撤销重做、仿真启动、拓扑变化、运行态清理 | `WireManager`、`SimulationEngine`、参数/测量 UI、模板/保存加载 | 高；非必要不拆分，修改后全模板回归 |
| `Core/CircuitComponent.cs` | 元件实例、端子生成、手动状态、RUN 显示、拖动与双击 | `ComponentDefinition`、`TerminalView`、`WorkspaceController` | 高；关系到所有元件行为 |
| `Core/TerminalView.cs` | 端子显示和点击接线入口 | `CircuitComponent`、`WorkspaceController` | 高；修改会影响全部接线 |
| `Core/WireManager.cs` | 导线创建/删除/刷新及同元件跳线白名单 | `TerminalView`、`WireView`、`WorkspaceController` | 高；禁止全局放开同元件跳线 |
| `Core/WireView.cs` | 单根导线显示、路由、手动弯折和选择 | `WireManager`、`WireBendHandle` | 高；影响保存加载和可视化 |
| `Core/SimulationEngine.cs` | 画布 RUN、动态接触器/KT、历史自锁保持和测量值 | 元件、导线、`WorkspaceController` | 高；不能用来反推静态检查结论 |
| `Core/CircuitStateAnalyzer.cs` | 工业通用静态现象分析 V1.0-V1.8 和检查面板文本结果 | 元件、导线、UnionFind；由 AI 面板调用 | 高；非必要不重构，任何修改需全工业回归 |
| `Templates/CircuitTemplateSpawnService.cs` | 校验模板 DTO，生成元件和导线 | `CircuitTemplateDto`、`WorkspaceController`、`WireManager` | 高；模板加载核心 |
| `Templates/CircuitTemplateLoader.cs` | 从 Resources 加载并解析模板 JSON | `CircuitTemplateDto` | 高；字段变化会影响全部模板 |
| `Templates/CircuitTemplateCatalogLoader.cs` | 加载模板 catalog | Catalog DTO | 高；影响图纸集入口 |
| `UI/TemplateLoadController.cs` | 模板选择、加载确认和生成入口 | Catalog/Template Loader、SpawnService、SaveLoadService | 高；模板工作流核心 |
| `UI/SaveLoadService.cs` | 用户图纸保存、导入、删除和 DTO 恢复 | `WorkspaceController`、元件 catalog、JSON 文件 | 高；字段变更需兼容旧存档 |
| `Rules/CircuitRuleChecker.cs` | 家庭规则检查和结构/实时路径分析 | `WorkspaceController`、元件和导线 | 高；家庭 8 模板需整体回归 |
| `Core/ComponentDefinition.cs` | Data asset 的元件、电气、端子和能力字段定义 | 全部元件生成、接线和仿真 | 高；字段/端子改变影响所有资产 |

## 3. 数据定义与 DTO

| 脚本 | 职责 | 风险 |
|---|---|---|
| `Core/ElectricalEnums.cs` | 元件种类、端子角色、导线样式、端子定义 | 中；枚举值变化可能破坏 asset 序列化 |
| `Core/ComponentParameter.cs` | 单个可编辑参数 DTO | 低 |
| `Core/ComponentParameterSet.cs` | 元件参数集合、克隆和更新 | 中 |
| `Templates/CircuitTemplateDto.cs` | 模板、元件和导线 JSON DTO | 高；字段名不可随意改 |
| `Templates/CircuitTemplateCatalogDto.cs` | catalog DTO | 中；字段变化影响图纸集 |
| `UI/SavedBlueprintInfo.cs` | 用户图纸列表展示数据 | 低 |
| `UI/SaveLoadService.cs` 内部 DTO | 用户图纸 JSON schema | 高；必须向后兼容 |

## 4. UI、登录与场景管理

| 脚本 | 主要职责 | 依赖/调用 | 风险 |
|---|---|---|---|
| `UI/AppSession.cs` | 当前用户会话 | 登录/用户显示 | 低 |
| `UI/LoginController.cs` | 登录页面与会话初始化 | `AppSession`、页面路由 | 中 |
| `UI/CurrentUserView.cs` | 当前用户显示 | `AppSession` | 低 |
| `UI/PageId.cs` | 页面枚举 | 路由和导航 | 低 |
| `UI/PageRouter.cs` | 页面显隐路由 | `PageId` | 中 |
| `UI/TopNavigationController.cs` | 顶部导航和页面切换 | `PageRouter` | 中 |
| `UI/DemoUIController.cs` | 主界面按钮、保存加载、仿真等入口协调 | Workspace、保存加载、模板 UI | 中偏高 |
| `UI/DemoRuntimeBootstrap.cs` | 启动时构建默认家庭示例 | Workspace、Data catalog | 中 |
| `UI/EncyclopediaController.cs` | 元件百科页面 | UI | 低 |

## 5. 画布、元件、端子与显示辅助

| 脚本 | 主要职责 | 风险 |
|---|---|---|
| `Core/WireBendHandle.cs` | 导线弯折点拖拽手柄 | 中 |
| `UI/WorkspaceGrid.cs` | 画布网格绘制 | 低 |
| `UI/PaletteController.cs` | 元件库分类和列表 | 中 |
| `UI/PaletteItem.cs` | 元件拖入画布 | 中 |
| `UI/ParameterPanelDragHandle.cs` | 参数面板拖拽 | 低 |
| `UI/ComponentParameterView.cs` | 元件参数显示与编辑、标记仿真刷新 | 中 |

## 6. 图纸、模板与保存加载 UI

| 脚本 | 主要职责 | 风险 |
|---|---|---|
| `UI/BlueprintController.cs` | 图纸集 catalog 卡片、筛选和预览 | 中 |
| `UI/BlueprintReferencePanel.cs` | 图纸参考面板 | 低 |
| `UI/TemplateSelectionPanel.cs` | 模板选择面板 | 中 |
| `UI/TemplateListItem.cs` | 单个模板列表项 | 低 |
| `UI/SaveBlueprintDialog.cs` | 保存命名和覆盖确认 | 中 |
| `UI/ImportBlueprintPanel.cs` | 用户图纸导入/删除列表 | 中 |
| `UI/SavedBlueprintListItem.cs` | 用户图纸列表项 | 低 |
| `UI/TemplateEditSession.cs` | 当前系统模板编辑会话状态 | 中 |
| `UI/SystemTemplateLayoutUpdater.cs` | Editor 中更新系统模板布局 JSON | 高；会写模板，使用前备份 |
| `UI/UpdateTemplateLayoutController.cs` | 更新模板布局入口 | 中 |
| `UI/UpdateTemplateLayoutConfirmDialog.cs` | 更新确认弹窗 | 低 |

## 7. 家庭规则检测

| 脚本 | 主要职责 | 风险 |
|---|---|---|
| `Rules/CircuitCheckResult.cs` | 家庭规则结果集合与计数 | 低 |
| `Rules/CircuitIssue.cs` | 单条规则问题 DTO | 低 |
| `Rules/CircuitIssueSeverity.cs` | 严重级别枚举 | 低 |
| `Rules/CircuitRuleCheckFormatter.cs` | 简洁规则结果文本 | 低 |
| `Rules/CircuitRuleCheckTeacherFormatter.cs` | 教师解释、原因和建议 | 中 |

## 8. 工业通用现象分析与 AI 助教

| 脚本 | 主要职责 | 风险 |
|---|---|---|
| `AI/AIAssistantPanel.cs` | AI 助教 UI、检查当前电路入口、展示 Analyzer/规则结果 | 中偏高 |
| `AI/CircuitSummaryBuilder.cs` | 构建当前画布摘要 | 中 |
| `AI/IndustrialCircuitRuleAnalyzer.cs` | 工业规则事实与规则检查 | 中 |
| `AI/IndustrialCircuitExplainer.cs` | 工业解释文本 | 中 |
| `AI/CircuitAnalysisResult.cs` | AI 分析结果 DTO | 低 |
| `AI/AIAssistantConfig.cs` | AI 服务配置 | 中 |
| `AI/AIAssistantMode.cs` | 本地/远程模式枚举 | 低 |
| `AI/AIAssistantService.cs` | AI 服务接口 | 低 |
| `AI/MockAIAssistantService.cs` | 本地 Mock 助教 | 中 |
| `AI/RealAIAssistantService.cs` | 远程服务结构 | 中；当前真实后端能力有限 |
| `AI/AIAssistantMessageItem.cs` | 聊天消息显示 | 低 |

## 9. 参数面板与万用表

| 脚本 | 主要职责 | 风险 |
|---|---|---|
| `UI/MeasurementPanel.cs` | 万用表/示波器基础读数与教学估算 | 中 |
| `UI/OscilloscopeWaveform.cs` | 波形绘制 | 低 |
| `UI/ComponentParameterView.cs` | 参数编辑入口 | 中 |
| `Core/ComponentParameter*.cs` | 参数数据和集合 | 中 |

## 10. Practice 练习系统

| 脚本组 | 主要职责 | 风险 |
|---|---|---|
| `Practice/PracticeSessionController.cs` | 练习会话、提交和退出流程 | 中 |
| `Practice/PracticeConnectionChecker.cs` | 旧/直接连接检查实现 | 中 |
| `Practice/PracticeFeedbackFormatter.cs` | 练习反馈文本 | 低 |
| `Practice/PracticeScoreCalculator.cs` | 练习评分 | 中 |
| `Practice/Netlist/StandardNetlistBuilder.cs` | 从模板构建标准网表 | 中 |
| `Practice/Netlist/StudentNetlistBuilder.cs` | 从画布构建学生网表 | 中 |
| `Practice/Netlist/ComponentMappingSolver.cs` | 标准与学生元件映射 | 中偏高 |
| `Practice/Netlist/PracticeConnectionChecker.cs` | 网表连接比较 | 中偏高 |
| `Practice/Netlist/PracticeNetlist*.cs` | 网表、连接和端子 DTO | 低 |
| `Practice/Netlist/UnionFind.cs` | 练习系统连通集合 | 低 |
| `Practice/Netlist/*Result/Issue/Formatter.cs` | 结果、问题和文本 | 低 |

注意：当前存在两套名称相近的 `PracticeConnectionChecker`（`Practice/` 与 `Practice/Netlist/`），属于后续可评估的重复/迁移痕迹，V1 封版前不建议合并。

## 11. Editor-only 工具

| 脚本 | 主要职责 | 风险 |
|---|---|---|
| `Editor/DemoSceneBuilder.cs` | 生成 Demo 场景、Data asset 和 UI | 中偏高；重新生成可能覆盖手工资产调整 |
| `Editor/TemplateIntegrityChecker.cs` | 只读扫描 catalog、模板、typeId、端子和工业专项，生成报告 | 低；建议每次新增模板后运行 |

## 12. 关键调用链

### 画布运行

`WorkspaceController` → `SimulationEngine` → `CircuitComponent.SetEnergized` → 画布 RUN/测量显示。

### 检查当前电路

`AIAssistantPanel` → `CircuitRuleChecker`（家庭）/ `CircuitStateAnalyzer`（工业）→ 可读文本输出。

### 模板加载

`TemplateLoadController` → `CircuitTemplateCatalogLoader` / `CircuitTemplateLoader` → `CircuitTemplateSpawnService` → `WorkspaceController` / `WireManager`。

### 用户图纸保存与导入

`DemoUIController` / 保存导入面板 → `SaveLoadService` → `WorkspaceController` / `WireManager`。

## 13. 修改策略

- 高风险脚本：非必要不重构；一次只改一个行为面，并执行家庭 8 张、工业 10 张全回归。
- 中风险脚本：至少回归对应 UI/检查/练习流程。
- 低风险脚本：仍需 C# 编译和基本 UI 检查。

