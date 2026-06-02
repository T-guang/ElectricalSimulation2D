using ElectricalSim.Rules;
using System.Linq;

namespace ElectricalSim.Practice
{
    public class PracticeScore
    {
        public int TotalScore { get; set; }
        public string Grade { get; set; }
    }

    public static class PracticeScoreCalculator
    {
        public static PracticeScore Calculate(CircuitCheckResult ruleResult, ConnectionCheckResult connectionResult)
        {
            if (ruleResult == null || connectionResult == null)
            {
                return new PracticeScore { TotalScore = 0, Grade = "未知状态" };
            }

            // Fatal issues checking
            bool hasFatalError = ruleResult.issues.Any(e => 
                (e.severity == CircuitIssueSeverity.Error) && 
                (e.message.Contains("未检测到有效电源") || 
                 e.message.Contains("未检测到有效负载") || 
                 e.message.Contains("未检测到任何导线")));

            if (hasFatalError || connectionResult.MatchScore == 0)
            {
                return new PracticeScore { TotalScore = 0, Grade = "未通过" };
            }

            int score = connectionResult.MatchScore;
            score -= ruleResult.ErrorCount * 15;
            score -= ruleResult.WarningCount * 5;

            if (score < 0) score = 0;

            string grade;
            if (score >= 90) grade = "通过";
            else if (score >= 75) grade = "基本通过";
            else if (score >= 60) grade = "需要修改";
            else grade = "未通过";

            return new PracticeScore { TotalScore = score, Grade = grade };
        }
    }
}
