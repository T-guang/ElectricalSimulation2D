import re

file_path = 'Assets/Scripts/UI/EncyclopediaController.cs'
with open(file_path, 'r', encoding='utf-8') as f:
    content = f.read()

# Pattern to replace the entire BuildOverrides method
old_method_pattern = r'private static Dictionary<string, ComponentEncyclopediaEntry> BuildOverrides\(\)[\s\S]*?return new Dictionary<string, ComponentEncyclopediaEntry>\(StringComparer\.OrdinalIgnoreCase\)\s*\{[\s\S]*?\}\s*;\s*\}'

new_method = '''private static Dictionary<string, ComponentEncyclopediaEntry> BuildOverrides()
        {
            return new Dictionary<string, ComponentEncyclopediaEntry>(StringComparer.OrdinalIgnoreCase)
            {
                {
                    "Contactor",
                    new ComponentEncyclopediaEntry
                    {
                        DisplayName = "交流接触器",
                        Category = "控制元件",
                        CircuitType = "工业电路",
                        Purpose = "交流接触器用于通过控制回路控制主回路通断，常用于电动机启停、连续运行、正反转和星三角启动等工业控制电路。",
                        TerminalDescription = "A1、A2 是线圈端子；1/L1、3/L2、5/L3 是主触点进线端；2/T1、4/T2、6/T3 是主触点出线端；13NO、14NO 是常开辅助触点；21NC、22NC 是常闭辅助触点。",
                        WorkingState = "线圈未得电时，主触点断开，13-14 断开，21-22 导通。线圈得电后，主触点闭合，13-14 闭合，21-22 断开。",
                        WiringUsage = "连续运行控制中，13-14 常用于自锁；正反转控制中，21-22 常用于互锁；主触点用于控制电动机主回路。",
                        CommonMistakes = "不要把 A1/A2 当作主回路端子；不要把 13-14 和 21-22 混用；正反转电路中不能让正转和反转接触器同时吸合。",
                        SimulationRule = "当 A1/A2 线圈形成有效控制回路时，接触器吸合，主触点和辅助触点会随线圈状态自动切换。",
                        SafetyTips = "实际接线时应区分主回路和控制回路，并确认线圈电压与电源电压一致。",
                        ContentCompleted = true
                    }
                },
                {
                    "ThermalRelay",
                    new ComponentEncyclopediaEntry
                    {
                        DisplayName = "热继电器 (FR)",
                        Category = "保护设备",
                        CircuitType = "工业电路",
                        Purpose = "热继电器主要用于电动机的过载保护和断相保护，防止电动机因长期过载发热而烧毁。",
                        TerminalDescription = "1/L1、3/L2、5/L3 是主回路进线端；2/T1、4/T2、6/T3 是主回路出线端（常连接电动机）；95、96 是常闭辅助触点；97、98 是常开辅助触点。",
                        WorkingState = "正常工作时，主触点和 95-96 保持导通，97-98 断开。当主回路电流长时间过载导致双金属片受热弯曲后，继电器动作，95-96 断开，97-98 闭合。",
                        WiringUsage = "主触点串联在电动机主回路中；95-96 常闭触点串联在控制回路（如接触器线圈回路上），用于过载时切断控制电路；97-98 常用于报警指示灯。",
                        CommonMistakes = "错误地将 97-98 常开触点串联在控制回路中导致无法启动；忘记将热继电器主触点与接触器出线端连接。",
                        SimulationRule = "本系统中，当流经主触点的电流超过设定电流并持续一定时间后，热继电器会触发过载动作，切断 95-96 触点。可点击复位按钮恢复。",
                        SafetyTips = "发生过载跳闸后，应先查明过载原因，排除故障并等热元件冷却后，再进行手动复位。",
                        ContentCompleted = true
                    }
                },
                {
                    "Timer_OnDelay",
                    new ComponentEncyclopediaEntry
                    {
                        DisplayName = "通电延时时间继电器 (KT)",
                        Category = "控制元件",
                        CircuitType = "工业电路",
                        Purpose = "用于在电路中实现时间控制，通电后经过设定的延时时间才改变触点状态，常用于星三角降压启动、顺序控制等。",
                        TerminalDescription = "2、7 是线圈端子；8、5 是延时断开常闭触点；8、6 是延时闭合常开触点；1、3、4 根据具体型号可能带有瞬时触点。",
                        WorkingState = "线圈未得电时，8-5 导通，8-6 断开。线圈得电并开始计时，计时期间状态不变；计时到达设定值后，8-5 断开，8-6 导通。断电后立即恢复初始状态。",
                        WiringUsage = "在星三角启动中，常开触点 8-6 用于触发角接接触器，常闭触点 8-5 用于切断星接接触器。",
                        CommonMistakes = "将线圈端子错接为其他触点；混淆延时触点与瞬时触点；忘记设定延时时间导致动作过快或不动作。",
                        SimulationRule = "当 2、7 线圈通电后启动内部计时器。计时到达设定的延迟时间时，触发触点状态切换。断电则瞬间重置计时与触点。",
                        SafetyTips = "在星三角启动接线中，应确保时间继电器的延时设定合理，避免电动机长时间处于星接状态或过早切换。",
                        ContentCompleted = true
                    }
                },
                {
                    "LimitSwitch",
                    new ComponentEncyclopediaEntry
                    {
                        DisplayName = "行程开关 (SQ)",
                        Category = "传感元件",
                        CircuitType = "工业电路",
                        Purpose = "用于检测机械运动部件的位置，并将机械位移信号转换为电信号，实现行程控制或限位保护。",
                        TerminalDescription = "通常包含一对常开触点（NO）和一对常闭触点（NC）。具体端子如 11-12 为常闭，13-14 为常开。",
                        WorkingState = "未受外力触碰时，常闭导通、常开断开。当机械部件压下开关连杆时，常闭断开、常开导通。外力消失后自动复位。",
                        WiringUsage = "常闭触点串联在控制回路中作为限位停止信号（如行车终点停止）；常开触点可作为到达某位置后的启动信号。",
                        CommonMistakes = "作为限位保护时错接了常开触点，导致到达极限位置时无法自动切断电路。",
                        SimulationRule = "在画布中点击行程开关的触碰区域（OperationHitArea）模拟机械压下，松开鼠标即可模拟复位。",
                        SafetyTips = "在关键的安全限位应用中（如起重机上限位），必须强制使用行程开关的常闭触点串联在主控回路中，以保证线路断开时也能安全停车。",
                        ContentCompleted = true
                    }
                },
                {
                    "EmergencyStop",
                    new ComponentEncyclopediaEntry
                    {
                        DisplayName = "急停按钮",
                        Category = "开关按钮",
                        CircuitType = "工业/家庭电路",
                        Purpose = "在遇到紧急情况时，操作人员可以通过迅速按下该按钮切断设备的控制总电源，实现紧急停车保护设备和人员安全。",
                        TerminalDescription = "通常提供 11、12 或 1、2 等常闭（NC）触点端子。",
                        WorkingState = "正常状态下（未按下），常闭触点保持导通。按下后，触点断开并机械锁定。必须旋转或拉出按钮才能复位重新导通。",
                        WiringUsage = "急停按钮的常闭触点必须串联在控制回路的总进线端，确保按下时能切断所有控制线圈的电源。",
                        CommonMistakes = "将急停按钮错接为常开触点；或者未串联在控制总线上，导致按下后部分设备仍在运行。",
                        SimulationRule = "点击急停按钮即可按下并锁定，再次点击可模拟旋转复位释放。状态变化立即切断或连通常闭触点。",
                        SafetyTips = "急停按钮是关键的安全装置，严禁短接。必须确保其安装在操作人员极易触及的醒目位置。",
                        ContentCompleted = true
                    }
                },
                {
                    "Button_Start_NO",
                    new ComponentEncyclopediaEntry
                    {
                        DisplayName = "启动按钮 (NO)",
                        Category = "开关按钮",
                        CircuitType = "工业电路",
                        Purpose = "用于发送启动控制信号，通常为绿色按钮，按下时接通电路，松手后自动复位断开。",
                        TerminalDescription = "提供 13、14 或 3、4 等常开（NO）触点端子。",
                        WorkingState = "未操作时触点断开（不导通）。按下按钮期间，触点闭合导通。松开手后弹簧使按钮复位，触点再次断开。",
                        WiringUsage = "常用于与接触器的辅助常开触点并联，实现启动信号的输入与自锁逻辑。",
                        CommonMistakes = "接线时未使用接触器自锁，导致松开启动按钮后电动机立刻停止运行。",
                        SimulationRule = "鼠标按下时触点导通，松开鼠标时触点断开。适合用来测试瞬时导通逻辑。",
                        SafetyTips = "操作时应明确按钮控制的设备对象，启动大型设备前需确认周围环境安全。",
                        ContentCompleted = true
                    }
                },
                {
                    "Button_Stop_NC",
                    new ComponentEncyclopediaEntry
                    {
                        DisplayName = "停止按钮 (NC)",
                        Category = "开关按钮",
                        CircuitType = "工业电路",
                        Purpose = "用于发送停止控制信号，通常为红色按钮，按下时切断控制电路，松手后自动复位导通。",
                        TerminalDescription = "提供 11、12 或 1、2 等常闭（NC）触点端子。",
                        WorkingState = "未操作时触点导通。按下按钮期间，触点断开。松开手后弹簧使按钮复位，触点再次导通。",
                        WiringUsage = "常串联在接触器线圈的控制回路中，按下时破坏自锁状态，使接触器断电释放。",
                        CommonMistakes = "误将其与启动按钮并联，导致按下停止按钮时造成控制线路短路或误动作。",
                        SimulationRule = "鼠标按下时触点断开，松开鼠标时触点恢复导通，用于破坏回路自锁状态。",
                        SafetyTips = "停止按钮的可靠性至关重要，接线时确保连接牢固，避免因接触不良导致设备意外停机。",
                        ContentCompleted = true
                    }
                },
                {
                    "Button_Compound",
                    new ComponentEncyclopediaEntry
                    {
                        DisplayName = "复合按钮 (SB)",
                        Category = "开关按钮",
                        CircuitType = "工业电路",
                        Purpose = "内部机械联动的双触点按钮，同时包含常开和常闭触点，按下时两个触点会联动翻转，常用于正反转控制的按钮互锁。",
                        TerminalDescription = "通常包含两组端子：11-12 为常闭触点（NC），13-14 为常开触点（NO）。",
                        WorkingState = "未按下时，11-12 导通，13-14 断开。按下瞬间，11-12 先断开，随后 13-14 闭合；松手复位时，13-14 先断开，11-12 再闭合。",
                        WiringUsage = "在正反转控制中，将常闭触点串联在反方向控制回路中，常开触点串联在正方向启动回路中，实现按下启动的同时切断另一方向的作用。",
                        CommonMistakes = "错误理解联动顺序，导致按下瞬间产生短路；或者只接了一组触点，没有发挥复合按钮的互锁优势。",
                        SimulationRule = "点击操作时，系统会自动处理内部的触点联动顺序，保证常闭先断开、常开后闭合的物理规律。",
                        SafetyTips = "使用复合按钮互锁虽然方便，但在容易产生电弧的高压大电流接触器控制中，仍需辅以接触器辅助触点互锁实现双重保护。",
                        ContentCompleted = true
                    }
                },
                {
                    "Button_SelfLock",
                    new ComponentEncyclopediaEntry
                    {
                        DisplayName = "自锁开关/按钮",
                        Category = "开关按钮",
                        CircuitType = "家庭/工业电路",
                        Purpose = "按下后机械锁定保持导通，再按一次解锁断开，不需要通过接触器就能维持导通状态，常用于低功率设备的直接控制。",
                        TerminalDescription = "常见有 L、L1（单控），或带有 11-12、23-24 等多组同步开闭的端子。",
                        WorkingState = "操作一次，触点状态翻转并保持（如闭合）。再次操作，触点状态再次翻转并保持（如断开）。",
                        WiringUsage = "用于直接控制照明灯泡、小型风扇等设备的电源通断，不需要复杂的控制回路。",
                        CommonMistakes = "在需要频繁启停的大功率电动机控制中错误使用了自锁开关直接控制主回路，导致触点烧毁。",
                        SimulationRule = "点击即可切换并保持锁定状态，无需一直按住鼠标。再次点击释放。",
                        SafetyTips = "自锁开关触点容量有限，严禁超载使用；在断电后若未手动复位，来电时设备会自动启动，存在一定安全隐患。",
                        ContentCompleted = true
                    }
                },
                {
                    "KnifeSwitch",
                    new ComponentEncyclopediaEntry
                    {
                        DisplayName = "刀开关 (QS)",
                        Category = "保护设备",
                        CircuitType = "工业电路",
                        Purpose = "用作电气设备隔离或不频繁接通和分断容量较小的低压电路，确保检修时有明显的断开点。",
                        TerminalDescription = "包含三组或多组对称的进线和出线端子，如 L1、L2、L3 和对应的 T1、T2、T3。",
                        WorkingState = "合闸时，动触刀与静触座咬合，电路接通；分闸时，动触刀拔出，电路切断，形成可见的安全断口。",
                        WiringUsage = "通常作为工业控制柜的总电源隔离关开，安装在电源进线的最前端。",
                        CommonMistakes = "带负荷分断或合闸大功率设备，导致产生强烈电弧烧毁触点甚至伤人。",
                        SimulationRule = "点击刀柄实现合闸与分闸操作，三相触点同步动作。",
                        SafetyTips = "严禁带大负荷拉合刀开关！断电维修时必须先分断刀开关并确认有明显断开点。",
                        ContentCompleted = true
                    }
                },
                {
                    "AC_ThreePhase_Power",
                    new ComponentEncyclopediaEntry
                    {
                        DisplayName = "三相交流电源",
                        Category = "电源仪表",
                        CircuitType = "工业电路",
                        Purpose = "为工业控制系统提供三相 380V 的交流动力电源，是电动机和各种工业负载的能量来源。",
                        TerminalDescription = "L1 (U)、L2 (V)、L3 (W) 为三相火线，N 为零线，PE 为保护接地线。",
                        WorkingState = "提供相位依次相差 120 度的交流电势。L1、L2、L3 任意两根火线间的线电压为 380V，任意一根火线与零线 N 之间的相电压为 220V。",
                        WiringUsage = "三根火线接入主回路控制电动机；控制回路可取 L1 与 L2 获得 380V 控制电源，或取 L1 与 N 获得 220V 控制电源。",
                        CommonMistakes = "将 L1/L2 接入 220V 额定电压的接触器线圈导致线圈烧毁；漏接保护接地线 (PE)。",
                        SimulationRule = "系统始终认为电源接通，为整个拓扑网络提供基准电势，支持跨相短路检测。",
                        SafetyTips = "380V 具有极高危险性，仿真中若发生相间短路（如 L1 直通 L2），系统将立即判定违规并切断仿真。",
                        ContentCompleted = true
                    }
                },
                {
                    "AC_220V_Power",
                    new ComponentEncyclopediaEntry
                    {
                        DisplayName = "220V 交流电源",
                        Category = "电源仪表",
                        CircuitType = "家庭电路",
                        Purpose = "为普通家庭照明、插座及小型单相用电设备提供标准单相 220V 交流电源。",
                        TerminalDescription = "L 为火线 (Live)，N 为零线 (Neutral)。有些带有 L2、N2 扩展端子用于多路并联。",
                        WorkingState = "L 极持续提供与 N 极之间具有 220V 交流电势差的电能。",
                        WiringUsage = "所有开关通常只控制火线 L，零线 N 直接接入用电设备的另一端。",
                        CommonMistakes = "接线时将火线和零线直接短接；或者开关错接在零线上，导致电器断开开关后依然带电。",
                        SimulationRule = "系统会从 L 出发，顺着导线寻找回到 N 的回路。如果 L 直接无阻抗回到 N，将触发短路报错。",
                        SafetyTips = "必须遵循“火线进开关，零线进灯头”的接线规范，保证检修电器时的绝对安全。",
                        ContentCompleted = true
                    }
                },
                {
                    "Motor_ThreePhase",
                    new ComponentEncyclopediaEntry
                    {
                        DisplayName = "三相异步电动机",
                        Category = "用电设备",
                        CircuitType = "工业电路",
                        Purpose = "将三相交流电能转化为机械能的旋转电机，是工业拖动系统的绝对核心动力设备。",
                        TerminalDescription = "U1、V1、W1 和 U2、V2、W2 是定子绕组接线端，PE 为外壳接地端。通过端子的不同短接方式可实现星形 (Y) 或三角形 (△) 运行。",
                        WorkingState = "三相绕组通入三相交流电后产生旋转磁场，带动转子旋转。相序改变（如 L1、L2 对调）会使旋转磁场反向，从而实现电动机反转。",
                        WiringUsage = "主回路通过接触器将 L1、L2、L3 接入电机的三个端子。Y接法将 U2/V2/W2 短接；△接法将 U1-W2, V1-U2, W1-V2 连接。",
                        CommonMistakes = "缺相运行导致电机烧毁；电机外壳未接地；星三角接线中绕组头尾接错导致内部短路。",
                        SimulationRule = "系统会实时检测加在电机端子上的三相电压。只有检测到完整的三相电势差且相序合法时，电机才会旋转，并显示转向动画。",
                        SafetyTips = "必须确保电机外壳牢固连接 PE 地线。实际操作中，大功率电机启动电流极大，需要降压启动。",
                        ContentCompleted = true
                    }
                },
                {
                    "Motor_StarDelta",
                    new ComponentEncyclopediaEntry
                    {
                        DisplayName = "星三角电动机",
                        Category = "用电设备",
                        CircuitType = "工业电路",
                        Purpose = "专用于星三角降压启动电路的电机，通过启动时星形连接、运行后三角连接来大幅降低启动电流。",
                        TerminalDescription = "U1、V1、W1 为绕组首端，U2、V2、W2 为绕组尾端。六个端子全部引出。",
                        WorkingState = "星形阶段通常将 U2、V2、W2 短接；三角阶段按 U1-W2、V1-U2、W1-V2 形成三角连接运行。",
                        WiringUsage = "星三角启动电路中，主接触器、星形接触器和三角接触器配合时间继电器自动完成连接切换。",
                        CommonMistakes = "不要同时形成星形和三角连接。不要接错 U1/V1/W1 与 U2/V2/W2 的对应关系，否则导致内部短路。",
                        SimulationRule = "本系统会识别星形、三角和星三角冲突状态，冲突或缺相时电机不会输出正常运行估算。",
                        SafetyTips = "实际星三角接线必须确认电机铭牌支持三角运行电压（如 380V-△），否则容易烧毁绕组。",
                        ContentCompleted = true
                    }
                },
                {
                    "Indicator_",
                    new ComponentEncyclopediaEntry
                    {
                        DisplayName = "指示灯",
                        Category = "传感元件",
                        CircuitType = "工业电路",
                        Purpose = "用于指示设备的工作状态（如运行、停止、故障），方便操作人员远距离监控系统运行情况。",
                        TerminalDescription = "通常提供 X1、X2 两个接线端子。",
                        WorkingState = "当 X1 和 X2 两端存在符合额定要求的电压差时，指示灯亮起；否则熄灭。",
                        WiringUsage = "绿灯常与接触器的常开触点串联指示“运行”；红灯常与常闭触点串联指示“停止”；黄灯常与热继电器 97-98 串联指示“故障过载”。",
                        CommonMistakes = "将 220V 的指示灯错误接入 380V 的相间电压，导致灯泡瞬间烧毁。",
                        SimulationRule = "只要 X1、X2 之间形成有效闭合回路且电压差符合元件额定设定，指示灯就会发光。",
                        SafetyTips = "更换指示灯时应注意电压等级。指示灯仅起提示作用，设备是否真正断电必须以电压测量为准。",
                        ContentCompleted = true
                    }
                },
                {
                    "Breaker_",
                    new ComponentEncyclopediaEntry
                    {
                        DisplayName = "空气开关/断路器 (QF)",
                        Category = "保护设备",
                        CircuitType = "家庭/工业电路",
                        Purpose = "作为总电源开关，并在电路发生严重过载或短路时自动跳闸切断电源，保护线路和设备安全。",
                        TerminalDescription = "1P提供一对进出线；3P提供三对进出线（如 1、3、5 进线，2、4、6 出线）。",
                        WorkingState = "手动合闸后内部触点连通。当检测到电流超过瞬时脱扣或过载脱扣阈值时，机械结构自动解扣，瞬间断开所有极。",
                        WiringUsage = "安装在电源引入侧，所有用电和控制回路均从空气开关的出线端取电。",
                        CommonMistakes = "上下级断路器额定电流配置倒置，导致越级跳闸；进出线接反。",
                        SimulationRule = "合闸后导通。若仿真中发生硬短路，系统判定短路的同时可模拟断路器跳闸保护动作。",
                        SafetyTips = "跳闸后绝不能强行立刻合闸，必须彻底排查短路点或烧毁元件后才能恢复送电。",
                        ContentCompleted = true
                    }
                },
                {
                    "Fuse_",
                    new ComponentEncyclopediaEntry
                    {
                        DisplayName = "熔断器 (FU)",
                        Category = "保护设备",
                        CircuitType = "工业电路",
                        Purpose = "用于短路保护。当电路中发生严重短路产生巨大电流时，内部熔体自身发热熔断，切断电路。",
                        TerminalDescription = "通常提供对称的进出线端子。单极熔断器有一对端子。",
                        WorkingState = "正常情况下作为导线导通。短路电流经过时，熔体在极短时间内烧毁断裂，物理切断回路。",
                        WiringUsage = "主回路熔断器（FU1）通常选额定电流较大者，控制回路熔断器（FU2）选额定电流较小者，串联在对应回路最前端。",
                        CommonMistakes = "随意用铜丝铁丝代替熔体，导致短路时无法熔断，引发火灾。",
                        SimulationRule = "作为常规阻抗极小的导通元件。若发生严重短路，熔断器可作为切断点之一。",
                        SafetyTips = "更换熔断器熔体时必须先切断前端电源！绝不能带电插拔或更换。",
                        ContentCompleted = true
                    }
                },
                {
                    "Tool_Multimeter",
                    new ComponentEncyclopediaEntry
                    {
                        DisplayName = "万用表",
                        Category = "电源仪表",
                        CircuitType = "维修测量",
                        Purpose = "用于测量电路中的电压、电流、电阻及导通状态等电气参数，是电工最基础的排故工具。",
                        TerminalDescription = "通常包含红表笔端子和黑表笔端子。",
                        WorkingState = "选择不同的测量档位（如交流电压档）后，表笔接触测量点，屏幕显示测量数值。",
                        WiringUsage = "电压测量并联在被测元件两端；电流测量串联在回路中。",
                        CommonMistakes = "用电流档或电阻档去测量带电电压，导致万用表内部烧毁；不明确被测电压大小选错量程。",
                        SimulationRule = "本系统中主要作为教学展示和接线体验，不具备完整的动态测量功能。",
                        SafetyTips = "测量未知高压时，应选择最高量程；操作时手不可触碰表笔金属部分。",
                        ContentCompleted = true
                    }
                },
                {
                    "Single_Phase_Meter",
                    new ComponentEncyclopediaEntry
                    {
                        DisplayName = "单相电能表",
                        Category = "电源仪表",
                        CircuitType = "家庭电路",
                        Purpose = "用于计量单相用电回路所消耗的电能（度数），广泛用于家庭用电计量。",
                        TerminalDescription = "一般有四个接线端子：1进火线、2出火线、3进零线、4出零线。",
                        WorkingState = "正确接入电源和负载后，表盘或液晶屏记录累计耗电量。",
                        WiringUsage = "通常安装在家庭总电源进线处，所有家用电器的耗电均经过此表。",
                        CommonMistakes = "进出线接反；火线零线位置接错导致短路或不走字。",
                        SimulationRule = "本系统中主要用于模拟家庭电路的标准接线实训，暂不支持电量数值累加计算。",
                        SafetyTips = "电能表接线端子带电，安装或检修必须拉开上级电源开关。",
                        ContentCompleted = true
                    }
                },
                {
                    "TerminalBlock_",
                    new ComponentEncyclopediaEntry
                    {
                        DisplayName = "端子排",
                        Category = "端子与模块",
                        CircuitType = "通用",
                        Purpose = "用于整理和转接线路，方便多个导线之间的规范连接，使控制柜内接线清晰整齐。",
                        TerminalDescription = "端子排按位导通，每一位上下或左右对应端子互通，不同位之间默认不互通。",
                        WorkingState = "端子排本身不产生电源，也不改变电压，只提供纯物理导线连接和转接功能。",
                        WiringUsage = "常用于把电源、按钮、接触器线圈和外部设备接线整理到统一位置过渡连接。",
                        CommonMistakes = "不要误以为端子排所有端子都互相导通。不同位端子不能随意当作同一节点使用。",
                        SimulationRule = "本系统中，端子排按对应位进行电气连通，例如 1A 与 1B 导通，2A 与 2B 导通，但 1A 与 2A 不默认导通。",
                        SafetyTips = "实际接线中端子排应压接牢固，并保持端子编号清晰以防错接。",
                        ContentCompleted = true
                    }
                },
                {
                    "Lamp_",
                    new ComponentEncyclopediaEntry
                    {
                        DisplayName = "灯泡",
                        Category = "用电设备",
                        CircuitType = "家庭电路",
                        Purpose = "用于将电能转换为光能，是家庭照明和简单电路中最直观的负载器件。",
                        TerminalDescription = "通常提供 L（火线进线）和 N（零线出线）两个螺纹或卡口端子。",
                        WorkingState = "接入火线和零线形成完整闭合回路后点亮，发光发热。",
                        WiringUsage = "火线通常应经过单控开关或双控开关后接入灯泡的中心触点，零线直接接入螺纹套管。",
                        CommonMistakes = "将零线接开关而火线直通灯泡，导致开关断开后灯泡仍带有高压电，极度危险。",
                        SimulationRule = "本系统中只要两端存在符合额定要求的电压差，灯泡即刻发光显示高亮状态。",
                        SafetyTips = "更换灯泡时应首先确认开关处于断开状态，切忌用湿手接触灯泡及灯座金属部分。",
                        ContentCompleted = true
                    }
                },
                {
                    "Fan_",
                    new ComponentEncyclopediaEntry
                    {
                        DisplayName = "电风扇",
                        Category = "用电设备",
                        CircuitType = "家庭电路",
                        Purpose = "将电能转换为机械风能的单相用电设备，用于模拟家庭电路中的感性负载。",
                        TerminalDescription = "提供 L（火线端）和 N（零线端），有时带 PE 端子。",
                        WorkingState = "接入有效 220V 单相电源回路后内部单相电机运转驱动扇叶旋转。",
                        WiringUsage = "常见接线为火线经墙壁开关控制后接入电风扇 L 端，N 端直接接零线。",
                        CommonMistakes = "接线时短路火线与零线；遗漏 PE 保护接地线的连接。",
                        SimulationRule = "接入标准 220V 电源并形成闭合回路后，系统渲染扇叶旋转动画。",
                        SafetyTips = "电风扇带有旋转部件，接线测试时应保持安全距离；金属外壳风扇必须接地。",
                        ContentCompleted = true
                    }
                },
                {
                    "Single_Control_Switch",
                    new ComponentEncyclopediaEntry
                    {
                        DisplayName = "单开单控开关",
                        Category = "开关按钮",
                        CircuitType = "家庭电路",
                        Purpose = "用于控制一盏灯或一路单相负载的简单开关，按下即保持状态，最为常见。",
                        TerminalDescription = "通常提供 L（进线端）和 L1（出线端）两个端子。",
                        WorkingState = "闭合时 L-L1 导通，负载得电；断开时 L-L1 不导通，负载失电。",
                        WiringUsage = "必须串联在火线 (L) 回路中，不允许接在零线上。",
                        CommonMistakes = "把火线和零线分别接在开关的两个端子上，一按开关直接引起爆燃短路。",
                        SimulationRule = "点击开关面板切换并保持导通/断开状态，状态改变立即重算拓扑回路。",
                        SafetyTips = "火线必须先进开关，保证断开开关后负载端不带电，保障人员检修安全。",
                        ContentCompleted = true
                    }
                },
                {
                    "Two_Way_Switch",
                    new ComponentEncyclopediaEntry
                    {
                        DisplayName = "单开双控开关",
                        Category = "开关按钮",
                        CircuitType = "家庭电路",
                        Purpose = "用于在两个不同位置（如楼上和楼下、床头和门口）独立控制同一盏灯的亮灭。",
                        TerminalDescription = "提供 L（公共端）、L1（第一常开/常闭端）和 L2（第二常开/常闭端）三个端子。",
                        WorkingState = "拨动开关时，公共端 L 会在与 L1 导通和与 L2 导通两种状态之间切换。",
                        WiringUsage = "两个双控开关的 L1 和 L2 分别通过两根控制线直连；电源火线进第一个开关的 L，第二个开关的 L 接负载灯泡。",
                        CommonMistakes = "错把火线接到了 L1 或 L2 端；或者双控线与火线零线混接，导致只能单控甚至短路。",
                        SimulationRule = "点击面板切换 L 与 L1 或 L2 的连接关系，支持完整的异地双控逻辑解算。",
                        SafetyTips = "双控接线线路较多，布线前务必明确公共端和控制线，切忌混淆零火线。",
                        ContentCompleted = true
                    }
                }
            };
        }'''

match = re.search(old_method_pattern, content)
if match:
    content = content.replace(match.group(0), new_method)
    with open(file_path, 'w', encoding='utf-8') as f:
        f.write(content)
    print("SUCCESS: BuildOverrides replaced with 24 components.")
else:
    print("ERROR: BuildOverrides method not found.")
