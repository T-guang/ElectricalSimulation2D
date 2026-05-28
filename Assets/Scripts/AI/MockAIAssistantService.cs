using System;
using System.Collections.Generic;
using System.Text;

namespace ElectricalSim.AI
{
    public sealed class MockAIAssistantService : IAIAssistantService
    {
        private class CircuitContext
        {
            public int ComponentCount;
            public int WireCount;
            public bool HasPower;
            public bool HasBulb;
            public bool HasFan;
            public bool HasSwitch;
            public bool HasDoubleControl;
            public bool HasSingleControl;
            public bool HasMeter;
            public bool HasBreaker;
            public bool HasRun;
            public float Power = 60f;
            public float Voltage = 220f;
            public List<string> MainComponents = new List<string>();
        }

        public void Ask(string userQuestion, string circuitSummary, Action<string> onSuccess, Action<string> onError)
        {
            if (onSuccess == null) return;
            if (string.IsNullOrWhiteSpace(circuitSummary) || circuitSummary.Contains("当前画布为空"))
            {
                onSuccess("当前画布为空，请先搭建或加载一个电路。");
                return;
            }

            var q = userQuestion ?? string.Empty;
            var ctx = ParseContext(circuitSummary);
            var sb = new StringBuilder();

            if (q == "当前电路解释")
            {
                sb.AppendLine("结论：该电路是一个基础的电气控制回路。");
                sb.AppendLine();
                sb.AppendLine("原因：");
                sb.AppendLine($"1. 当前电路包含 {ctx.ComponentCount} 个元件和 {ctx.WireCount} 条导线。");
                
                var compList = ctx.MainComponents.Count > 0 ? string.Join("、", ctx.MainComponents) : "未检测到主要元件";
                sb.AppendLine($"2. 主要元件包括：{compList}等。");
                
                if (ctx.HasDoubleControl)
                    sb.AppendLine("3. 电路中包含双控开关，这是双控照明逻辑，可实现两地控制。");
                else if (ctx.HasSingleControl)
                    sb.AppendLine("3. 电路中包含单控开关，这是单控照明逻辑，用于简单通断。");
                    
                if (ctx.HasMeter || ctx.HasBreaker)
                    sb.AppendLine("4. 存在电能计量或保护元件（如电表/空气开关），使电路具备更完善的实际用电特征。");
                    
                sb.AppendLine("5. 基本工作过程：电源火线经过控制元件到达负载，负载通过零线回到电源，形成闭合回路后负载即可运行。");
                sb.AppendLine();
                sb.AppendLine("建议：");
                sb.AppendLine("1. 请点击“开始仿真”按钮。");
                sb.AppendLine("2. 观察负载是否显示 RUN，以验证接线是否正确。");
            }
            else if (q == "检查当前电路")
            {
                sb.AppendLine("结论：当前处于本地 Mock 检查模式。");
                sb.AppendLine();
                sb.AppendLine("原因：");
                if (!ctx.HasPower)
                {
                    sb.AppendLine("1. 缺失电源：当前电路没有检测到 220V 交流电源。");
                }
                else if (!ctx.HasBulb && !ctx.HasFan)
                {
                    sb.AppendLine("1. 缺失负载：当前电路有电源但没有灯泡或风扇等负载。");
                }
                else if (ctx.WireCount == 0)
                {
                    sb.AppendLine("1. 缺失导线：当前没有接线。");
                }
                else if (ctx.HasPower && (ctx.HasBulb || ctx.HasFan) && !ctx.HasRun)
                {
                    sb.AppendLine("1. 回路异常：已有电源和负载，但仿真显示没有通电（没有 RUN 状态）。");
                }
                else if (ctx.HasRun)
                {
                    sb.AppendLine("1. 回路正常：当前至少有一个负载通电显示 RUN，基础回路已形成。");
                }
                else
                {
                    sb.AppendLine("1. 未检测到明显的基础缺失。");
                }
                
                sb.AppendLine();
                sb.AppendLine("建议：");
                if (!ctx.HasPower) sb.AppendLine("1. 请先放置一个 220V 电源。");
                else if (!ctx.HasBulb && !ctx.HasFan) sb.AppendLine("1. 请放置灯泡或风扇作为负载。");
                else if (ctx.WireCount == 0) sb.AppendLine("1. 请使用导线连接各个端子。");
                else if (!ctx.HasRun)
                {
                    sb.AppendLine("1. 检查电源 L/N 是否接出。");
                    sb.AppendLine("2. 检查负载 L/N 是否形成回路。");
                    sb.AppendLine("3. 检查开关是否接在火线上且已闭合。");
                    sb.AppendLine("4. 检查是否存在未连接的端子。");
                }
                else
                {
                    sb.AppendLine("1. 当前接线基础正确，可进一步探索更多接线组合。");
                }
                sb.AppendLine("2. 下一阶段会加入更准确的 AI 电气规则检查。");
            }
            else if (ContainsAny(q, "是否完整", "连接完整", "有没有接好", "能不能运行", "有没有问题", "检查一下"))
            {
                if (ctx.HasRun)
                {
                    sb.AppendLine("结论：当前电路连接基本完整且已通电。");
                    sb.AppendLine();
                    sb.AppendLine("原因：");
                    sb.AppendLine($"1. 电路包含 {ctx.ComponentCount} 个元件和 {ctx.WireCount} 条导线。");
                    sb.AppendLine("2. 从当前仿真状态看，负载已经显示 RUN。");
                    sb.AppendLine("3. 这说明电源、开关和负载之间至少存在一条有效闭合工作回路。");
                    sb.AppendLine();
                    sb.AppendLine("建议：");
                    sb.AppendLine("1. 可以尝试断开开关，观察负载是否停止运行。");
                    sb.AppendLine("2. 验证各个开关对电路的控制逻辑是否符合预期。");
                }
                else
                {
                    sb.AppendLine("结论：当前电路可能不够完整，负载未正常工作。");
                    sb.AppendLine();
                    sb.AppendLine("原因：");
                    sb.AppendLine($"1. 电路包含 {ctx.ComponentCount} 个元件和 {ctx.WireCount} 条导线。");
                    sb.AppendLine("2. 当前没有检测到负载的 RUN（通电）状态。");
                    sb.AppendLine("3. 可能是回路未闭合、开关未闭合、零线未回到电源 N，或者导线断开。");
                    sb.AppendLine();
                    sb.AppendLine("建议：");
                    sb.AppendLine("1. 请确认电源 L 是否经过开关再到负载 L。");
                    sb.AppendLine("2. 请确认负载 N 是否回到电源 N。");
                    sb.AppendLine("3. 请确认开关处于闭合状态。");
                }
            }
            else if (ContainsAny(q, "功率", "电流", "电压", "瓦", "W", "A", "V", "多少电流", "功率多少"))
            {
                var i = ctx.Power / ctx.Voltage;
                sb.AppendLine("结论：电路中负载的参数设置会直接影响仿真运行结果。");
                sb.AppendLine();
                sb.AppendLine("原因：");
                sb.AppendLine("1. U 代表电压，I 代表电流，P 代表功率。");
                sb.AppendLine("2. 在阻性负载中可以使用近似公式：P = U × I，即 I = P / U。");
                sb.AppendLine($"3. 当前电路提取到负载功率约为 {ctx.Power}W，电压为 {ctx.Voltage}V。");
                sb.AppendLine();
                sb.AppendLine("建议：");
                sb.AppendLine($"1. 按照近似计算，当前 {ctx.Power}W 的负载在 {ctx.Voltage}V 下工作，电流约为 {i:0.##}A。");
                sb.AppendLine("2. 你可以通过修改元件参数面板中的功率值，观察仿真结果中电流的变化规律。");
            }
            else if (ContainsAny(q, "单开单控", "单控开关"))
            {
                sb.AppendLine("结论：单开单控开关是家庭照明中最基础的控制元件。");
                sb.AppendLine();
                sb.AppendLine("原因：");
                sb.AppendLine("1. 它通常串联在火线（L）中，用于控制火线的通断。");
                sb.AppendLine("2. 常用于一个开关控制一个灯的场景。");
                sb.AppendLine("3. 相比双控开关，它的接线端子更少，逻辑也更简单。");
                sb.AppendLine();
                sb.AppendLine("建议：");
                sb.AppendLine("1. 基本接线规范为：电源 L → 开关入端，开关出端 → 灯泡 L。");
                sb.AppendLine("2. 确保灯泡的 N 端直接回到电源 N，形成闭合回路。");
            }
            else if (ContainsAny(q, "双控", "单开双控", "双控开关", "两个开关控制一个灯"))
            {
                sb.AppendLine("结论：双控开关可实现两个不同位置控制同一盏灯。");
                sb.AppendLine();
                sb.AppendLine("原因：");
                sb.AppendLine("1. 它通常有三个端子（L、L1、L2）。");
                sb.AppendLine("2. 常用于楼梯上下、走廊两端或卧室门口和床头。");
                sb.AppendLine("3. 任意一个开关被切换，都可能改变整条照明回路的通断状态。");
                sb.AppendLine();
                sb.AppendLine("建议：");
                sb.AppendLine("1. 两个双控开关之间通常需要两根联络线（L1 连 L1，L2 连 L2）。");
                sb.AppendLine("2. 第一开关的 L 端接电源火线，第二开关的 L 端接灯泡火线。");
                sb.AppendLine("3. 仔细核对端子标号，接错容易导致短路或无法双控。");
            }
            else if (ContainsAny(q, "灯泡", "电灯泡", "负载", "为什么灯亮", "为什么灯不亮"))
            {
                sb.AppendLine("结论：灯泡是典型的电气负载，必须在闭合回路中才能工作。");
                sb.AppendLine();
                sb.AppendLine("原因：");
                sb.AppendLine("1. 灯泡需要火线和零线同时连通以形成完整回路。");
                sb.AppendLine("2. 只有在电源正常、导线完好且开关闭合时，灯泡才会显示 RUN 并发光。");
                if (ctx.HasRun) sb.AppendLine("3. 目前仿真状态显示已有负载 RUN，说明接线基本正确。");
                else sb.AppendLine("3. 目前没有负载 RUN，说明回路尚未接通。");
                sb.AppendLine();
                sb.AppendLine("建议：");
                sb.AppendLine("1. 如果灯不亮，依次检查：电源输出、所有开关状态、灯泡 L/N 接线。");
                sb.AppendLine("2. 确保没有任何一根导线处于断开（悬空）状态。");
            }
            else if (ContainsAny(q, "220V电源", "交流电源", "电源", "L", "N", "PE", "火线", "零线", "地线"))
            {
                sb.AppendLine("结论：220V 交流电源是整个电路工作的动力来源。");
                sb.AppendLine();
                sb.AppendLine("原因：");
                sb.AppendLine("1. L 代表火线（Live），带电且对地有电压。");
                sb.AppendLine("2. N 代表零线（Neutral），作为工作回路的回流线，理论上不带电。");
                sb.AppendLine("3. PE 是保护地线（Protective Earth），用于设备金属外壳保护接地，不参与正常工作电流。");
                sb.AppendLine();
                sb.AppendLine("建议：");
                sb.AppendLine("1. 家庭照明通常仅由 L 和 N 形成工作回路。");
                sb.AppendLine("2. 出于安全规范，火线 L 必须经过开关再进入负载，绝对不能反接。");
            }
            else if (ContainsAny(q, "电表", "单相电能表", "计量"))
            {
                sb.AppendLine("结论：单相电能表用于计量整个下游电路消耗的电能。");
                sb.AppendLine();
                sb.AppendLine("原因：");
                sb.AppendLine("1. 它记录负载随时间消耗的总电量。");
                sb.AppendLine("2. 通常包含明确的进线端和出线端。");
                sb.AppendLine();
                sb.AppendLine("建议：");
                sb.AppendLine("1. 接线时必须严格区分火线进出和零线进出。");
                sb.AppendLine("2. 按照实际家庭用电流程，电表一般位于最前端，其出线再接空气开关、普通控制开关和负载。");
            }
            else if (ContainsAny(q, "空气开关", "空开", "断路器", "1P", "2P"))
            {
                sb.AppendLine("结论：空气开关（断路器）是极其重要的电路保护装置。");
                sb.AppendLine();
                sb.AppendLine("原因：");
                sb.AppendLine("1. 它在发生过载或短路时会自动跳闸，切断电源。");
                sb.AppendLine("2. 1P 断路器通常只断开单根火线；2P 断路器可以同时断开火线和零线。");
                sb.AppendLine("3. 它不是用电设备，而是保护设备。");
                sb.AppendLine();
                sb.AppendLine("建议：");
                sb.AppendLine("1. 家庭入户总闸或各照明/插座回路，都必须配置合适容量的空开。");
                sb.AppendLine("2. 请勿将空开当做普通开关频繁操作，也不要把它串接成负载。");
            }
            else if (ContainsAny(q, "解释", "这个电路", "原理", "怎么工作", "说明"))
            {
                sb.AppendLine("结论：电路正常工作依赖于完整闭合的导电路径。");
                sb.AppendLine();
                sb.AppendLine("原因：");
                sb.AppendLine("1. 当前电路主要由电源、开关、导线和负载组成。");
                sb.AppendLine("2. 电源提供 220V 交流电压。");
                sb.AppendLine("3. 火线电流从电源出发，依次经过各类控制元件（如开关）到达负载。");
                sb.AppendLine("4. 最后通过零线从负载回流至电源。");
                sb.AppendLine();
                sb.AppendLine("建议：");
                sb.AppendLine("1. 闭合开关，形成回路，负载即可工作。");
                sb.AppendLine("2. 断开开关，回路截断，负载即刻停止运行。");
            }
            else
            {
                sb.AppendLine("结论：我现在是第一版 AI 助教模拟模式。");
                sb.AppendLine();
                sb.AppendLine("原因：");
                sb.AppendLine("1. 当前暂未接入真实的云端大语言模型。");
                sb.AppendLine("2. 但我已经可以根据当前电路摘要，结合专家规则为您提供基础指导。");
                sb.AppendLine();
                sb.AppendLine("建议：");
                sb.AppendLine("1. 您可以试着问我：“这个电路为什么灯不亮？”");
                sb.AppendLine("2. 或问：“什么是单开双控开关？”、“灯泡功率是什么意思？”");
            }

            sb.AppendLine();
            sb.AppendLine("当前为本地 Mock 助教，后续可接入真实 AI 获得更精确分析。");

            onSuccess(sb.ToString().TrimEnd());
        }

        private static CircuitContext ParseContext(string summary)
        {
            var ctx = new CircuitContext();
            
            int.TryParse(ExtractLineValue(summary, "元件数量："), out ctx.ComponentCount);
            int.TryParse(ExtractLineValue(summary, "导线数量："), out ctx.WireCount);
            
            ctx.HasPower = ContainsAny(summary, "220V交流电源", "220V");
            ctx.HasBulb = ContainsAny(summary, "白炽灯", "灯泡");
            ctx.HasFan = ContainsAny(summary, "排气扇", "风扇");
            ctx.HasSwitch = ContainsAny(summary, "开关");
            ctx.HasDoubleControl = ContainsAny(summary, "单开双控");
            ctx.HasSingleControl = ContainsAny(summary, "单开单控");
            ctx.HasMeter = ContainsAny(summary, "单相电能表", "电表");
            ctx.HasBreaker = ContainsAny(summary, "断路器", "空气开关", "漏保");
            ctx.HasRun = ContainsAny(summary, "通电=是");

            var powerStr = "60";
            var powerIdx = summary.IndexOf("W", StringComparison.Ordinal);
            if (powerIdx > 0)
            {
                var startIdx = powerIdx - 1;
                while (startIdx > 0 && char.IsDigit(summary[startIdx]))
                {
                    startIdx--;
                }
                if (startIdx < powerIdx - 1)
                {
                    powerStr = summary.Substring(startIdx + 1, powerIdx - startIdx - 1);
                }
            }
            if (float.TryParse(powerStr, out var p) && p > 0) ctx.Power = p;

            if (ctx.HasPower) ctx.MainComponents.Add("220V电源");
            if (ctx.HasBreaker) ctx.MainComponents.Add("空气开关");
            if (ctx.HasMeter) ctx.MainComponents.Add("电表");
            if (ctx.HasSwitch) ctx.MainComponents.Add("开关");
            if (ctx.HasBulb) ctx.MainComponents.Add("灯泡");
            else if (ctx.HasFan) ctx.MainComponents.Add("风扇");

            if (ctx.MainComponents.Count > 5)
            {
                ctx.MainComponents.RemoveRange(5, ctx.MainComponents.Count - 5);
            }

            return ctx;
        }

        private static bool ContainsAny(string text, params string[] keywords)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            foreach (var keyword in keywords)
            {
                if (text.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            return false;
        }

        private static string ExtractLineValue(string text, string prefix)
        {
            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (line.StartsWith(prefix, StringComparison.Ordinal))
                {
                    return line.Substring(prefix.Length).Trim();
                }
            }

            return "0";
        }
    }
}
