using System;
using System.Collections.Generic;
using ElectricalSim.Core;

namespace ElectricalSim.Core.Validation
{
    public sealed class CircuitValidationService
    {
        public CircuitValidationReport Validate(
            IReadOnlyList<CircuitComponent> components,
            IReadOnlyList<WireView> wires,
            CircuitStateResult analysisResult)
        {
            var report = new CircuitValidationReport();
            var phaseHelper = new MotorPhaseValidationHelper(components, wires);
            AddMotorIssues(report, components, analysisResult, phaseHelper);
            AddUnsupportedComponentIssues(report, components);
            return report;
        }

        private static void AddMotorIssues(
            CircuitValidationReport report,
            IReadOnlyList<CircuitComponent> components,
            CircuitStateResult analysisResult,
            MotorPhaseValidationHelper phaseHelper)
        {
            if (report == null || analysisResult == null || analysisResult.Components == null)
            {
                return;
            }

            for (var i = 0; i < analysisResult.Components.Count; i++)
            {
                var info = analysisResult.Components[i];
                if (info == null)
                {
                    continue;
                }

                var component = FindComponent(components, info.InstanceId);
                if (info.IsStarDeltaMotor)
                {
                    AddStarDeltaIssue(report, component, info, phaseHelper);
                }
                else if (info.IsThreePhaseMotor)
                {
                    AddThreePhaseMotorIssue(report, component, info, phaseHelper);
                }
            }
        }

        private static void AddThreePhaseMotorIssue(
            CircuitValidationReport report,
            CircuitComponent component,
            ComponentStateInfo info,
            MotorPhaseValidationHelper phaseHelper)
        {
            var phaseResult = phaseHelper != null ? phaseHelper.Validate(component, info, false) : null;
            if (phaseResult == null || !phaseResult.ShouldEvaluate)
            {
                return;
            }

            if (phaseResult.HasDuplicatePhase)
            {
                AddIssue(
                    report,
                    "MOTOR_DUPLICATE_PHASE",
                    CircuitValidationSeverity.Error,
                    CircuitValidationCategory.Motor,
                    "三相电机重复相",
                    "三相电机存在重复相接入，当前接线不满足正常三相运行条件。",
                    component,
                    TerminalConstants.U,
                    TerminalConstants.V,
                    TerminalConstants.W);
                return;
            }

            if (phaseResult.HasMissingPhase)
            {
                AddIssue(
                    report,
                    "MOTOR_MISSING_PHASE",
                    CircuitValidationSeverity.Error,
                    CircuitValidationCategory.Motor,
                    "三相电机缺相",
                    "三相电机缺少有效三相供电，当前不满足正常运行条件。",
                    component,
                    TerminalConstants.U,
                    TerminalConstants.V,
                    TerminalConstants.W);
            }
        }

        private static void AddStarDeltaIssue(
            CircuitValidationReport report,
            CircuitComponent component,
            ComponentStateInfo info,
            MotorPhaseValidationHelper phaseHelper)
        {
            if (string.Equals(info.State, "StarDeltaConflict", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(info.StarDeltaConnectionMode, "Conflict", StringComparison.OrdinalIgnoreCase))
            {
                AddIssue(
                    report,
                    "STAR_DELTA_CONFLICT",
                    CircuitValidationSeverity.Error,
                    CircuitValidationCategory.StarDelta,
                    "星三角冲突",
                    "检测到星形连接与三角连接同时存在，存在星三角冲突风险，系统不输出正常电机估算。",
                    component,
                    TerminalConstants.U1,
                    TerminalConstants.V1,
                    TerminalConstants.W1,
                    TerminalConstants.U2,
                    TerminalConstants.V2,
                    TerminalConstants.W2);
                return;
            }

            var phaseResult = phaseHelper != null ? phaseHelper.Validate(component, info, true) : null;
            if (phaseResult == null || !phaseResult.ShouldEvaluate)
            {
                return;
            }

            if (phaseResult.HasDuplicatePhase)
            {
                AddIssue(
                    report,
                    "STAR_DELTA_DUPLICATE_PHASE",
                    CircuitValidationSeverity.Error,
                    CircuitValidationCategory.StarDelta,
                    "星三角电机重复相",
                    "星三角电机存在重复相接入，当前接线不满足正常三相运行条件。",
                    component,
                    TerminalConstants.U1,
                    TerminalConstants.V1,
                    TerminalConstants.W1);
                return;
            }

            if (phaseResult.HasMissingPhase)
            {
                AddIssue(
                    report,
                    "STAR_DELTA_MISSING_PHASE",
                    CircuitValidationSeverity.Error,
                    CircuitValidationCategory.StarDelta,
                    "星三角电机缺相",
                    "星三角电机缺少有效三相供电，当前不满足正常运行条件。",
                    component,
                    TerminalConstants.U1,
                    TerminalConstants.V1,
                    TerminalConstants.W1);
            }
        }

        private static void AddUnsupportedComponentIssues(
            CircuitValidationReport report,
            IReadOnlyList<CircuitComponent> components)
        {
            if (report == null || components == null)
            {
                return;
            }

            for (var i = 0; i < components.Count; i++)
            {
                var component = components[i];
                var definition = component != null ? component.Definition : null;
                if (definition == null ||
                    (definition.supportLevel != ComponentSupportLevel.VisualOnly && definition.canParticipateInRuntime))
                {
                    continue;
                }

                var reason = string.IsNullOrWhiteSpace(definition.unsupportedReason)
                    ? "该元件当前暂未支持完整仿真判断。"
                    : definition.unsupportedReason;

                AddIssue(
                    report,
                    "UNSUPPORTED_COMPONENT",
                    CircuitValidationSeverity.Warning,
                    CircuitValidationCategory.UnsupportedComponent,
                    "暂未支持元件",
                    reason,
                    component);
            }
        }

        private static CircuitComponent FindComponent(IReadOnlyList<CircuitComponent> components, string instanceId)
        {
            if (components == null || string.IsNullOrWhiteSpace(instanceId))
            {
                return null;
            }

            for (var i = 0; i < components.Count; i++)
            {
                var component = components[i];
                if (component != null && string.Equals(component.InstanceId, instanceId, StringComparison.OrdinalIgnoreCase))
                {
                    return component;
                }
            }

            return null;
        }

        private static void AddIssue(
            CircuitValidationReport report,
            string ruleId,
            CircuitValidationSeverity severity,
            CircuitValidationCategory category,
            string title,
            string message,
            CircuitComponent component,
            params string[] relatedTerminals)
        {
            if (report == null || HasIssue(report, ruleId, component))
            {
                return;
            }

            var issue = new CircuitValidationIssue
            {
                RuleId = ruleId,
                Severity = severity,
                Category = category,
                Title = title,
                Message = message,
                Component = component
            };

            if (relatedTerminals != null)
            {
                for (var i = 0; i < relatedTerminals.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(relatedTerminals[i]))
                    {
                        issue.RelatedTerminals.Add(relatedTerminals[i]);
                    }
                }
            }

            report.Issues.Add(issue);
        }

        private static bool HasIssue(CircuitValidationReport report, string ruleId, CircuitComponent component)
        {
            var instanceId = component != null ? component.InstanceId : string.Empty;
            for (var i = 0; i < report.Issues.Count; i++)
            {
                var issue = report.Issues[i];
                var issueInstanceId = issue != null && issue.Component != null ? issue.Component.InstanceId : string.Empty;
                if (issue != null &&
                    string.Equals(issue.RuleId, ruleId, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(issueInstanceId, instanceId, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
