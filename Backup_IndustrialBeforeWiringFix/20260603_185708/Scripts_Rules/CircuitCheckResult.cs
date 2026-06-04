using System.Collections.Generic;

namespace ElectricalSim.Rules
{
    public sealed class CircuitCheckResult
    {
        public readonly List<CircuitIssue> issues = new List<CircuitIssue>();

        public bool passed => ErrorCount == 0;
        public int ErrorCount { get; private set; }
        public int WarningCount { get; private set; }
        public int InfoCount { get; private set; }

        public string Summary
        {
            get
            {
                if (issues.Count == 0)
                {
                    return "未发现明显接线错误。";
                }

                return "发现 " + ErrorCount + " 个严重问题，" + WarningCount + " 个提醒，" + InfoCount + " 条信息。";
            }
        }

        public void Add(CircuitIssue issue)
        {
            if (issue == null)
            {
                return;
            }

            issues.Add(issue);
            switch (issue.severity)
            {
                case CircuitIssueSeverity.Error:
                    ErrorCount++;
                    break;
                case CircuitIssueSeverity.Warning:
                    WarningCount++;
                    break;
                case CircuitIssueSeverity.Info:
                    InfoCount++;
                    break;
            }
        }
    }
}
