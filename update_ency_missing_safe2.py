import re

file_path = 'Assets/Scripts/UI/EncyclopediaController.cs'
with open(file_path, 'r', encoding='utf-8') as f:
    content = f.read()

new_entries = '''                {
                    "Timer_OffDelay",
                    new ComponentEncyclopediaEntry
                    {
                        DisplayName = "断电延时时间继电器", Category = "控制元件", CircuitType = "工业电路",
                        Purpose = "在电路中实现断电延时控制，即线圈失电后，经过设定的延时时间，触点才复位。",
                        TerminalDescription = "通常包含线圈端子（如 2、7）和带有延时闭合/延时断开功能的触点（如 15、16、18）。",
                        WorkingState = "线圈得电时，触点瞬间动作；线圈失电时，触点保持当前状态并开始计时，计时到达设定值后触点复位。",
                        WiringUsage = "常用于需要设备在主电源切断后继续运行一段时间的场合（如冷却风扇延时停止）。",
                        CommonMistakes = "与通电延时时间继电器混淆；误将控制电源切断导致无法计时。",
                        SimulationRule = "当前版本该元件主要用于百科展示，运行仿真能力仍在完善中。",
                        SafetyTips = "实际使用时应注意断电延时继电器的储能机制（如内置电容），切断总电源后触点可能依然会动作。",
                        ContentCompleted = true
                    }
                },
                {
                    "SwitchPower",
                    new ComponentEncyclopediaEntry
                    {
                        DisplayName = "开关电源", Category = "电源仪表", CircuitType = "工业电路",
                        Purpose = "将 220V 交流电转换为低压直流电（如 24V），为 PLC、传感器和继电器等低压控制设备提供工作电源。",
                        TerminalDescription = "包含交流进线端（L、N、PE）和直流出线端（V+、V-）。",
                        WorkingState = "接入有效 220V 交流电源后，直流端持续输出稳定的 24V 直流电势差。",
                        WiringUsage = "通常安装在控制柜内，L/N 端子接市电，V+/V- 端子作为控制回路的 DC24V 电源。",
                        CommonMistakes = "将交直流进出线接反，导致设备瞬间烧毁；过载短接 V+ 与 V-。",
                        SimulationRule = "当前版本该元件主要用于百科展示，运行仿真能力仍在完善中。",
                        SafetyTips = "开关电源外壳通常为金属并带有接线端子排，通电后严禁触摸裸露端子和内部电路板。",
                        ContentCompleted = true
                    }
                },
                {
                    "PLC_",
                    new ComponentEncyclopediaEntry
                    {
                        DisplayName = "PLC可编程控制器", Category = "控制元件", CircuitType = "工业电路",
                        Purpose = "工业自动化系统的核心大脑，通过内部程序逻辑控制输出端，实现复杂的时序和逻辑控制。",
                        TerminalDescription = "通常包含电源端子、输入端子（X）和输出端子（Y），以及公共端（COM）。",
                        WorkingState = "根据输入端口的状态和内部梯形图程序，驱动输出端口导通或闭合。",
                        WiringUsage = "输入端接按钮、传感器；输出端接继电器、接触器线圈或电磁阀。",
                        CommonMistakes = "输入输出的 NPN/PNP 类型接错；公共端 COM 接错电压导致烧毁端口。",
                        SimulationRule = "当前版本该元件主要用于百科展示，梯形图编程与运行仿真能力仍在完善中。",
                        SafetyTips = "PLC 属于精密电子设备，接线时必须严格区分交直流电源和信号线，避免高压串入信号端。",
                        ContentCompleted = true
                    }
                },
                {
                    "SolenoidValve",
                    new ComponentEncyclopediaEntry
                    {
                        DisplayName = "电磁阀", Category = "用电设备", CircuitType = "工业电路",
                        Purpose = "利用电磁铁的吸力控制流体（如压缩空气或液压油）的流动方向，是气动/液压系统的核心执行元件。",
                        TerminalDescription = "通常包含电磁线圈的两个接线端（如正负极或 L/N 端）。",
                        WorkingState = "线圈得电时产生磁场吸动阀芯，改变气路或液路状态；失电时依靠弹簧复位。",
                        WiringUsage = "常由 PLC 输出端口或中间继电器控制线圈得失电，进而控制气缸伸缩。",
                        CommonMistakes = "线圈电压等级接错（如 24V 误接 220V）；安装时流体进出口方向装反。",
                        SimulationRule = "当前版本该元件主要用于百科展示，运行仿真能力仍在完善中。",
                        SafetyTips = "气动系统带有高压能量，维修电磁阀前必须先切断气源并排空残余压力。",
                        ContentCompleted = true
                    }
                },
                {
                    "StepperDriver",
                    new ComponentEncyclopediaEntry
                    {
                        DisplayName = "步进电机驱动器", Category = "控制元件", CircuitType = "工业电路",
                        Purpose = "接收来自 PLC 或控制器的脉冲和方向信号，将其转换为控制步进电机绕组的电流，驱动电机按指定步距角转动。",
                        TerminalDescription = "包含电源输入（V+、V-）、控制信号输入（PUL、DIR、ENA）以及电机绕组输出（A+、A-、B+、B-）。",
                        WorkingState = "得电且使能后，每接收到一个脉冲信号，输出对应的电流时序，驱动电机转动一步。",
                        WiringUsage = "控制信号端接 PLC 输出口；电机绕组输出端对应接步进电机的两相线圈。",
                        CommonMistakes = "脉冲和方向信号接反；绕组 A 相与 B 相线序接错导致电机震动不转。",
                        SimulationRule = "当前版本该元件主要用于百科展示，脉冲时序仿真能力仍在完善中。",
                        SafetyTips = "驱动器工作时会产生较高热量，需保证良好散热；电机运行期间严禁插拔电机接线。",
                        ContentCompleted = true
                    }
                },
                {
                    "StepperMotor",
                    new ComponentEncyclopediaEntry
                    {
                        DisplayName = "步进电动机", Category = "用电设备", CircuitType = "工业电路",
                        Purpose = "将电脉冲信号转换为角位移或线位移，用于需要精确控制位置和速度的自动化机构。",
                        TerminalDescription = "通常为四线或六线引出，代表内部的两组定子绕组（如 A 相和 B 相）。",
                        WorkingState = "在驱动器给定的时序电流下，转子按固定的步距角（如 1.8 度）逐点转动。",
                        WiringUsage = "与专用的步进电机驱动器配合使用，四根引出线分别接驱动器的 A+、A-、B+、B-。",
                        CommonMistakes = "未使用驱动器而直接接入直流电源；将不同相的导线短接。",
                        SimulationRule = "当前版本该元件主要用于百科展示，运行仿真能力仍在完善中。",
                        SafetyTips = "步进电机在高速运转时可能产生较大反电动势，断电后电机轴可能仍有惯性旋转，请注意安全。",
                        ContentCompleted = true
                    }
                },
                {
                    "Tool_Oscilloscope",
                    new ComponentEncyclopediaEntry
                    {
                        DisplayName = "示波器", Category = "电源仪表", CircuitType = "维修测量",
                        Purpose = "用于观察和测量电路中电压信号随时间变化的波形，是分析高频信号和瞬态过程的高级测试仪器。",
                        TerminalDescription = "包含多个测量通道探头接口（CH1、CH2）及接地夹。",
                        WorkingState = "探头接入被测电路后，屏幕实时显示电压幅度与时间的二维函数波形。",
                        WiringUsage = "探头针接触被测信号点，接地夹连接电路的地线参考点。",
                        CommonMistakes = "测量高压市电时未使用隔离探头导致示波器炸机；接地夹接错位置造成短路。",
                        SimulationRule = "当前版本该元件主要用于百科展示，完整信号波形捕获能力仍在完善中。",
                        SafetyTips = "示波器探头接地夹通常与外壳及大地相连，测量非隔离电源时极易引发对地短路危险。",
                        ContentCompleted = true
                    }
                }'''

old_text = '''                        SafetyTips = "双控接线线路较多，布线前务必明确公共端和控制线，切忌混淆零火线。",
                        ContentCompleted = true
                    }
                }
            };
        }'''

new_text = '''                        SafetyTips = "双控接线线路较多，布线前务必明确公共端和控制线，切忌混淆零火线。",
                        ContentCompleted = true
                    }
                },
''' + new_entries + '''
            };
        }'''

if old_text in content:
    content = content.replace(old_text, new_text)
else:
    print("WARNING: Could not find exact dict end pattern!")

# Now we also need to update GetDisplayName
get_display_name_additions = '''            if (name.Contains("SwitchPower_220V")) return "220V开关电源（待完善）";
            if (name.Contains("PLC_Output_24V")) return "PLC可编程控制器（待完善）";
            if (name.Contains("SolenoidValve_24V")) return "电磁阀（待完善）";
            if (name.Contains("StepperDriver_24V")) return "步进电机驱动器（待完善）";
            if (name.Contains("StepperMotor_24V")) return "步进电动机（待完善）";
            if (name.Contains("Tool_Oscilloscope")) return "示波器（无图）";'''

if 'if (name.Contains("Tool_Multimeter")) return "万用表（无图）";' in content:
    content = content.replace('if (name.Contains("Tool_Multimeter")) return "万用表（无图）";', 
                              'if (name.Contains("Tool_Multimeter")) return "万用表（无图）";\n' + get_display_name_additions)
else:
    print("WARNING: Could not find Tool_Multimeter block in GetDisplayName!")

# Update BuildBasicSummary
build_basic_summary_additions = '''            if (defName.Contains("SwitchPower")) return "直流供电 / 待完善";
            if (defName.Contains("PLC_")) return "逻辑控制 / 待完善";
            if (defName.Contains("SolenoidValve")) return "气液控制 / 待完善";
            if (defName.Contains("StepperDriver")) return "脉冲驱动 / 待完善";
            if (defName.Contains("StepperMotor")) return "精确步进 / 待完善";
            if (defName.Contains("Tool_Oscilloscope")) return "波形分析 / 图片待补充";'''

if 'if (defName.Contains("Tool_Multimeter")) return "测量工具 / 图片待补充";' in content:
    content = content.replace('if (defName.Contains("Tool_Multimeter")) return "测量工具 / 图片待补充";',
                              'if (defName.Contains("Tool_Multimeter")) return "测量工具 / 图片待补充";\n' + build_basic_summary_additions)
else:
    print("WARNING: Could not find Tool_Multimeter block in BuildBasicSummary!")


# Also inject the correct Aliases checking logic inside MatchesSearch
# Currently MatchesSearch is:
#         private bool MatchesSearch(ComponentEncyclopediaEntry entry)
#         {
#             if (string.IsNullOrWhiteSpace(searchText))
# ...
#             return Contains(entry.DisplayName, query) ||
#                    Contains(entry.Category, query) ||
# ...

matches_search_old = '''        private bool MatchesSearch(ComponentEncyclopediaEntry entry)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return true;
            }

            var query = searchText.Trim();
            return Contains(entry.DisplayName, query) ||
                   Contains(entry.Category, query) ||
                   Contains(entry.Purpose, query) ||
                   Contains(entry.TerminalDescription, query) ||
                   Contains(BuildTerminalSummary(entry.Definition, entry.Terminals), query);
        }'''

matches_search_new = '''        private bool MatchesSearch(ComponentEncyclopediaEntry entry)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return true;
            }

            var query = searchText.Trim();
            
            var defName = entry.DefinitionName ?? "";
            var aliases = "";
            if (defName.Contains("Contactor")) aliases = "接触器 KM Contactor";
            else if (defName.Contains("Timer_")) aliases = "KT 时间继电器 延时继电器";
            else if (defName.Contains("ThermalRelay")) aliases = "FR 热继 过载保护";
            else if (defName.Contains("LimitSwitch")) aliases = "SQ 限位开关 行程开关";
            else if (defName.Contains("EmergencyStop")) aliases = "急停 急停开关 Emergency Stop";
            else if (defName.Contains("Motor_")) aliases = "电机 三相电动机 Motor";
            else if (defName.Contains("TerminalBlock")) aliases = "端子 接线端子 Terminal Block";
            else if (defName.Contains("Indicator_")) aliases = "信号灯 运行灯 报警灯";
            else if (defName.Contains("SwitchPower")) aliases = "开关电源 稳压电源";
            else if (defName.Contains("PLC_")) aliases = "PLC 可编程控制器";
            else if (defName.Contains("SolenoidValve")) aliases = "电磁阀 气动";
            else if (defName.Contains("Stepper")) aliases = "步进 驱动器";
            else if (defName.Contains("Tool_Oscilloscope")) aliases = "示波器 测量";
            
            return Contains(entry.DisplayName, query) ||
                   Contains(entry.Category, query) ||
                   Contains(entry.Purpose, query) ||
                   Contains(entry.TerminalDescription, query) ||
                   Contains(aliases, query) ||
                   Contains(BuildTerminalSummary(entry.Definition, entry.Terminals), query);
        }'''

if matches_search_old in content:
    content = content.replace(matches_search_old, matches_search_new)
else:
    print("WARNING: Could not find MatchesSearch block! Searching loosely...")
    # fallback to regex
    content = re.sub(r'private bool MatchesSearch\(ComponentEncyclopediaEntry entry\)[\s\S]*?\}', matches_search_new, content)

with open(file_path, 'w', encoding='utf-8') as f:
    f.write(content)
print("SUCCESS: Code correctly patched.")
