using System.Collections.Generic;
using System.Text;

namespace ElectricalSim.AI
{
    public sealed class CircuitAnalysisResult
    {
        public bool IsIndustrial { get; set; }
        public string CircuitType { get; set; } = "工业控制电路";
        public List<string> Errors { get; } = new List<string>();
        public List<string> Warnings { get; } = new List<string>();
        public List<string> TeachingTips { get; } = new List<string>();

        public int ErrorCount => Errors.Count;
        public int WarningCount => Warnings.Count;

        public string FormatForAssistant()
        {
            var builder = new StringBuilder();
            builder.AppendLine("AI 助教：");
            builder.AppendLine("当前电路检查结果");
            builder.AppendLine();
            builder.AppendLine("电路类型：" + (string.IsNullOrWhiteSpace(CircuitType) ? "工业控制电路" : CircuitType));
            builder.AppendLine();

            if (Errors.Count > 0)
            {
                builder.AppendLine("结论：当前电路存在 " + Errors.Count + " 个严重问题。");
            }
            else if (Warnings.Count > 0)
            {
                builder.AppendLine("结论：当前电路未发现严重错误，但有 " + Warnings.Count + " 个提醒。");
            }
            else
            {
                builder.AppendLine("结论：当前电路未发现严重错误。");
            }

            AppendSection(builder, "错误", Errors);
            AppendSection(builder, "警告", Warnings);
            AppendSection(builder, "教学提示", TeachingTips);

            builder.AppendLine();
            builder.AppendLine("本检查只读取当前画布，不会修改元件、导线或仿真结果。");
            builder.AppendLine("本功能用于教学辅助，不替代真实电工规范验收。");
            return builder.ToString().TrimEnd();
        }

        private static void AppendSection(StringBuilder builder, string title, List<string> items)
        {
            if (items == null || items.Count == 0)
            {
                return;
            }

            builder.AppendLine();
            builder.AppendLine(title + "：");
            for (var i = 0; i < items.Count; i++)
            {
                builder.AppendLine((i + 1) + ". " + items[i]);
            }
        }
    }
}
