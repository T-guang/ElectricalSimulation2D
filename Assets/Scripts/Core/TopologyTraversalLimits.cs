using UnityEngine;

namespace ElectricalSim.Core
{
    public static class TopologyTraversalLimits
    {
        public const int MaxTraversalSteps = 4096;
        public const int MaxSearchDepth = 256;
        public const int MaxVisitedNodes = 2048;
        public const int MaxVisitedEdges = 8192;

        public const string ComplexTopologyRuleId = "COMPLEX_LOOP_OR_UNSUPPORTED_TOPOLOGY";
        public const string ComplexTopologyTitle = "复杂回路或暂不支持的嵌套结构";
        public const string ComplexTopologyMessage =
            "当前接线存在复杂回路或不受支持的嵌套结构，系统已停止继续追踪该路径，请检查自锁、互锁或短接支路。";

        public static bool IsTraversalBudgetExceeded(int steps, int visitedNodes, int visitedEdges)
        {
            return steps > MaxTraversalSteps ||
                visitedNodes > MaxVisitedNodes ||
                visitedEdges > MaxVisitedEdges;
        }

        public static void LogTraversalBudgetExceeded(string context)
        {
            Debug.LogWarning("[TopologyTraversal] " + ComplexTopologyMessage + " Context: " + context);
        }
    }
}
