# V1 风险与低风险整理建议

## 1. 结论

当前 V1 结构检查和编译未发现阻塞封版的问题。建议以“文档补齐、测试固化、避免核心重构”为主要整理策略。

## 2. 可立即做的低风险项

| 建议 | 依据 | 风险 |
|---|---|---|
| 每次新增/修改模板后运行 `Tools/电工仿真/校验图纸模板完整性` | 当前工具已存在，能提前发现孤立元件、缺失端子和 catalog 错误 | 低 |
| 将本次 V1 报告加入封版检查流程 | 便于交接和人工验收 | 低 |
| 修正连续运行模板 catalog 描述中的“占位模板”文案 | 模板已完成，但描述仍过时 | 低，仅文案 |
| 为静态检查面板与画布历史 RUN 差异保留统一说明 | 已是明确 V1 边界，可减少误解 | 低，仅文案 |
| 为 KT 手动模拟延时说明保持统一短文案 | 防止把 KT ON/OFF 当普通开关 | 低，仅文案 |
| 为 `TemplateIntegrityChecker` 报告增加当前 Git commit/Unity 版本 | 方便报告追溯 | 低，Editor-only |
| 将临时根目录 Python 调试脚本归档到明确的 `Tools/` 或外部工作目录 | 当前 Git 工作树有多份未跟踪辅助脚本 | 低；先确认用途，不直接删除 |
| 补充高风险脚本顶部职责注释和回归范围 | 降低后续误改概率 | 低，不改变行为 |

## 3. 暂缓的中风险项

| 建议 | 原因 | 安全前提 |
|---|---|---|
| 统一模板 `templateId` 命名是否带 `_template` | 当前家庭模板存在两种风格，但运行正常 | 先做兼容映射和 catalog 回归 |
| 统一“连锁/联锁”等教学文案 | 仅影响显示，但散落于模板和输出 | 建立术语表后分批修改 |
| 整理 UI 中动态创建控件的重复代码 | 多个面板自行创建按钮/文本 | 先做视觉回归基线 |
| 评估两套 `PracticeConnectionChecker` 的职责重叠 | 可能是旧实现与 Netlist 新实现并存 | 先确认实际调用链和练习测试覆盖 |
| 拆分 `DemoSceneBuilder` 的资产、场景、UI 生成职责 | 文件约 1,858 行，维护成本高 | 仅在能验证重新生成结果时进行 |
| 拆分 `AIAssistantPanel` 的 UI 构建与检查结果展示 | 文件较大且承载多入口 | 需 AI/家庭/工业检查回归 |
| 清理 `WorkspaceController.simulationDirty` CS0414 警告 | 当前字段赋值但编译器认为未读取 | 先确认设计意图，避免影响刷新语义 |

## 4. 不建议当前做的高风险项

1. 不重构或拆分 `CircuitStateAnalyzer`。其已覆盖 V1.0-V1.8，多轮动态状态和输出优先级耦合较深。
2. 不重写 `SimulationEngine`，尤其不要把静态检查和历史 RUN 混为同一状态源。
3. 不重写 `WorkspaceController`；它协调画布、历史、仿真、测量和拓扑变化。
4. 不全局修改 `WireManager` 接线规则；星三角同元件跳线必须继续保持白名单。
5. 不修改模板 DTO、保存 DTO 的已有字段名，避免破坏系统模板和用户旧图纸。
6. 不批量格式化模板 JSON、asset 或核心脚本，避免产生无法审查的大面积 diff。
7. 不让检查面板读取 `SimulationEngine.IsEnergized` 或画布 RUN 历史。
8. 不在 V1 封版阶段加入真实 KT 计时、参数计算或工业级物理模型。

## 5. 疑似重复或可读性问题

### 疑似重复代码

- `Practice/PracticeConnectionChecker.cs` 与 `Practice/Netlist/PracticeConnectionChecker.cs` 名称和职责接近。建议先确认生产调用链，V1 不删除、不合并。
- 工业静态分析和画布运行层分别实现动态器件规则，这是刻意的“双状态源”架构边界；可以补文档，但不应为了去重强行合并。
- 多个 UI 脚本包含动态创建 Text/Button 的辅助方法，可在 V2 评估统一 UI 工厂，但当前不动。

### 命名与文案

- `motor_self_hold_control` catalog 描述仍为“占位模板”，建议仅更新说明。
- 模板 ID 有的包含 `_template`，有的不包含；当前不影响加载，但应在后续规范中说明。
- “联锁/连锁”存在混用，建议建立教学术语表后再统一。

### 可补充注释的位置

- `CircuitStateAnalyzer.Analyze`：说明多轮迭代、静态历史边界和危险状态优先保留。
- `SimulationEngine.ResetRuntimeState`：说明只清历史 latch，不清按钮/KT 手动状态。
- `WorkspaceController.MarkTopologyDirty`：说明拓扑变化为何必须清理旧 RUN。
- `WireManager.CanCreateWire`：说明星三角电机是唯一同元件跳线白名单。
- `SaveLoadService` 和模板 DTO：说明字段名兼容性约束。

## 6. 已知边界与非 Bug 项

- 检查面板是静态拓扑分析，不读取画布历史 RUN。
- 连续/热继/正反转中，启动按钮松开后画布可能通过自锁保持，静态检查可能给出冷启动视角。
- KT 是手动模拟延时，不是真实秒级计时。
- 当前参数与测量值属于教学近似，不是工业保护整定或物理仿真。
- OffDelay 和真实机械运动状态机尚未完整支持。

## 7. 未来重构的安全顺序

1. 先建立自动化回归夹具：模板加载、结构检查、关键 Analyzer 状态和 Simulation RUN 状态。
2. 固化 18 张模板人工验收截图/结果基线。
3. 先整理低风险 DTO 文档、文案和 Editor 工具。
4. 再整理 UI 重复代码，保持核心状态逻辑不动。
5. 再评估 Practice 两套检查器的调用与迁移。
6. 最后才考虑拆分 `WorkspaceController`、`CircuitStateAnalyzer` 或 `SimulationEngine`；每次仅移动一个职责并保持行为等价。

## 8. 封版前建议

- 运行 Template Integrity Checker 并保存当前 18 模板报告。
- 在 Unity Editor 手工执行 `V1_Acceptance_Report.md` 清单。
- 确认停止按钮 OFF、KT 未得电防呆、星三角冲突三类安全场景。
- 清理或归档未跟踪的临时辅助脚本后再提交。
- 人工验收通过后打 `v1.0-teaching-circuit-state-analysis` 标签。

