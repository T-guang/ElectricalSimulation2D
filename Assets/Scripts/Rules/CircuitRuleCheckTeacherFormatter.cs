using System.Text;
using System.Collections.Generic;

namespace ElectricalSim.Rules
{
    public static class CircuitRuleCheckTeacherFormatter
    {
        public static string FormatForTeaching(CircuitCheckResult result)
        {
            if (result == null)
            {
                return "电路检查失败：未能获取检查结果。";
            }

            var builder = new StringBuilder();
            builder.AppendLine("电路检查结果");
            builder.AppendLine();

            if (result.issues.Count == 0)
            {
                builder.AppendLine("未发现明显接线错误。");
                builder.AppendLine();
                builder.AppendLine("当前电路包含电源、保护元件、控制元件和负载，火线与零线回路基本完整。");
                builder.AppendLine("你可以点击“开始仿真”，观察灯泡 RUN 状态和万用表读数。");
                builder.AppendLine();
                builder.AppendLine("提示：");
                builder.AppendLine("本检查为基础教学规则检查，主要用于帮助发现常见接线问题，不替代真实电工规范验收。");
                return builder.ToString().TrimEnd();
            }

            builder.AppendLine("本次检查发现：");
            int errorCount = result.ErrorCount;
            int warningCount = result.WarningCount;
            int infoCount = result.InfoCount;

            if (errorCount > 0) builder.AppendLine($"严重问题：{errorCount} 个");
            if (warningCount > 0) builder.AppendLine($"提醒：{warningCount} 个");
            if (infoCount > 0) builder.AppendLine($"信息：{infoCount} 条");
            builder.AppendLine();

            AppendGroup(builder, result, CircuitIssueSeverity.Error, "一、需要优先处理的问题");
            AppendGroup(builder, result, CircuitIssueSeverity.Warning, errorCount > 0 ? "二、提醒" : "一、提醒");
            AppendGroup(builder, result, CircuitIssueSeverity.Info, errorCount > 0 && warningCount > 0 ? "三、说明" : (errorCount > 0 || warningCount > 0 ? "二、说明" : "一、说明"));

            builder.AppendLine();
            builder.AppendLine("本检查只读取当前画布，不会修改元件、导线或仿真结果。");
            builder.AppendLine("本功能用于教学辅助，不替代真实电工规范验收。");

            return builder.ToString().TrimEnd();
        }

        private static void AppendGroup(StringBuilder builder, CircuitCheckResult result, CircuitIssueSeverity severity, string title)
        {
            var issues = new List<CircuitIssue>();
            foreach (var issue in result.issues)
            {
                if (issue != null && issue.severity == severity)
                {
                    issues.Add(issue);
                }
            }

            if (issues.Count == 0)
            {
                return;
            }

            builder.AppendLine(title);
            builder.AppendLine();

            for (int i = 0; i < issues.Count; i++)
            {
                var issue = issues[i];
                builder.AppendLine($"{i + 1}. {issue.title}");
                
                var teacherExplanation = GetTeacherExplanation(issue);
                
                builder.AppendLine($"   原因：{teacherExplanation.Reason}");
                builder.AppendLine($"   建议：{teacherExplanation.Suggestion}");
                builder.AppendLine();
            }
        }

        private class TeacherExplanation
        {
            public string Reason { get; set; }
            public string Suggestion { get; set; }
        }

        private static TeacherExplanation GetTeacherExplanation(CircuitIssue issue)
        {
            var explanation = new TeacherExplanation();

            switch (issue.code)
            {
                case "NO_POWER":
                    explanation.Reason = "电源是电路能量来源。没有电源时，即使有灯泡、开关和导线，电路也无法工作。";
                    explanation.Suggestion = "请从左侧元件池拖入 220V 电源，再连接火线 L 和零线 N。";
                    break;
                case "NO_LOAD":
                    explanation.Reason = "负载是消耗电能并产生效果的元件，例如灯泡或电风扇。没有负载时，电路没有实际工作对象。";
                    explanation.Suggestion = "请添加电灯泡或电风扇等负载元件。";
                    break;
                case "NO_WIRE":
                    explanation.Reason = "元件必须通过导线连接成回路，电流才能流动。";
                    explanation.Suggestion = "请使用红色导线连接火线 L 支路，使用蓝色导线连接零线 N 支路。";
                    break;
                case "LOAD_L_MISSING":
                    explanation.Reason = "L 端通常接火线，是负载获得电源的一侧。如果 L 端没有连接到电源火线支路，负载不会通电。";
                    explanation.Suggestion = "检查负载红色端子是否连接到开关或空开输出的火线支路。";
                    break;
                case "LOAD_N_MISSING":
                    explanation.Reason = "N 端需要回到电源零线，形成完整回路。如果 N 端断开，电流没有返回路径。";
                    explanation.Suggestion = "检查负载蓝色端子是否接回电源 N。";
                    break;
                case "PHASE_PATH_MISSING":
                    explanation.Reason = "火线 L 应从电源出发，经过保护元件和控制元件后到达负载 L 端。如果中间任意一段断开，负载就不能获得电源。";
                    explanation.Suggestion = "沿红色导线从电源 L 开始检查，依次查看空开、开关、负载 L 端是否连通。";
                    break;
                case "NEUTRAL_PATH_MISSING":
                    explanation.Reason = "零线 N 是电流返回电源的路径。负载 N 端没有回到电源 N 时，电路无法形成闭合回路。";
                    explanation.Suggestion = "沿蓝色导线从负载 N 端检查，确认是否最终回到电源 N。";
                    break;
                case "NO_SWITCH":
                case "SWITCH_NOT_ON_PHASE":
                case "SWITCH_ON_NEUTRAL":
                    explanation.Reason = "家庭照明电路中，开关通常应控制火线。如果开关没有串在火线路径上，可能导致灯具维护时仍带电，存在安全隐患。";
                    explanation.Suggestion = "请确认电源 L 经过开关后再进入灯泡 L 端。";
                    break;
                case "BREAKER_INCOMPLETE":
                    explanation.Reason = "空气开关用于保护后级电路。通常电源 L/N 先进入空开输入端，再由空开输出端连接到后续电路。如果进线或出线缺失，保护和供电都可能不正常。";
                    explanation.Suggestion = "检查电源 L/N 是否接到空开输入端，空开输出端是否接到后续开关、灯泡或风扇回路。";
                    break;
                case "METER_INCOMPLETE":
                    explanation.Reason = "单相电能表通常需要 L_IN、L_OUT、N_IN、N_OUT 形成完整进出线关系。进线或出线缺失会导致后级电路不能正确供电或计量。";
                    explanation.Suggestion = "请检查电源 L/N 是否进入电表输入端，电表输出端是否连接到后续电路。";
                    break;
                case "ISOLATED_COMPONENT":
                    explanation.Reason = "孤立元件没有任何导线连接，当前不会参与电路工作。";
                    explanation.Suggestion = "如果该元件是本次练习需要的，请将它接入电路；如果不是需要的元件，可以删除以保持画布清晰。";
                    break;
                case "OPEN_DEVICE":
                    if (issue.title != null && issue.title.Contains("以下控制元件"))
                    {
                        explanation.Reason = "多个控制元件断开时，会切断负载的火线控制支路。接线结构可能正确，但当前状态下负载不会运行。";
                        explanation.Suggestion = "请依次确认这些控制元件是否需要切换为 ON，再观察负载状态。";
                    }
                    else if (issue.title != null && issue.title.Contains("空气开关"))
                    {
                        explanation.Reason = "空气开关处于 OFF 时，会切断后级电路。即使导线连接正确，负载也可能不会通电。";
                        explanation.Suggestion = "请确认是否需要将空气开关切换为 ON，再运行仿真观察结果。";
                    }
                    else if (issue.title != null && issue.title.Contains("单开单控开关"))
                    {
                        explanation.Reason = "单控开关处于 OFF 时，会切断灯泡或风扇的火线控制支路。接线结构可能正确，但当前状态下负载不会运行。";
                        explanation.Suggestion = "请将对应单控开关切换为 ON，再观察灯泡或风扇状态。";
                    }
                    else
                    {
                        explanation.Reason = "控制元件断开时，即使接线结构正确，负载也不会通电。";
                        explanation.Suggestion = "请检查控制元件的 ON/OFF 状态。";
                    }
                    break;
                default:
                    explanation.Reason = issue.message ?? "电路存在影响运行的结构或状态问题。";
                    explanation.Suggestion = issue.suggestion ?? "请检查相关元件和接线。";
                    break;
            }

            return explanation;
        }
    }
}
