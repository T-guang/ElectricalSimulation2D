using System.Collections.Generic;
using UnityEngine;

namespace ElectricalSim.Core
{
    public sealed class TeachingParameterCalculationResult
    {
        public bool HasEffectiveSinglePhaseVoltage;
        public string LineLabel;
        public float MeasuredVoltage;
        public float MeasuredCurrent;
        public float MeasuredPower;
        public float RatedPower;
    }

    public sealed class ThreePhaseMotorEstimate
    {
        public bool IsRunning;
        public float LineVoltage;
        public float RatedPower;
        public float Efficiency;
        public float PowerFactor;
        public float EstimatedCurrent;
    }

    public enum StarDeltaMotorEstimateStage
    {
        Stopped,
        Star,
        Delta,
        Conflict,
        SupplyFault,
        Unknown
    }

    public sealed class StarDeltaMotorEstimate
    {
        public StarDeltaMotorEstimateStage Stage;
        public float LineVoltage;
        public float RatedPower;
        public float Efficiency;
        public float PowerFactor;
        public float DeltaEstimatedCurrent;
        public float StarEstimatedCurrent;
        public float EstimatedCurrent;
        public bool HasNormalEstimate;
    }

    public sealed class ControlCircuitLoadEstimate
    {
        public string DisplayName;
        public string LoadType;
        public float RatedVoltage;
        public float RatedPower;
        public float RatedCurrent;
        public float EstimatedCurrent;
        public bool HasEnoughParameters;
    }

    public static class TeachingParameterCalculationService
    {
        public delegate IReadOnlyCollection<string> PhaseResolver(TerminalView terminal);
        public delegate bool NeutralResolver(TerminalView terminal);

        public static bool TryCalculateSinglePhaseLoad(
            CircuitComponent component,
            bool circuitActive,
            float sourceVoltage,
            PhaseResolver resolvePhases,
            NeutralResolver canReachNeutral,
            out TeachingParameterCalculationResult result)
        {
            result = null;
            if (!IsSinglePhaseTeachingLoad(component))
            {
                return false;
            }

            result = new TeachingParameterCalculationResult();
            if (!circuitActive || resolvePhases == null || canReachNeutral == null)
            {
                return true;
            }

            var firstTerminal = ResolveFirstTerminal(component);
            var secondTerminal = ResolveSecondTerminal(component);
            if (firstTerminal == null || secondTerminal == null)
            {
                return true;
            }

            var firstLine = ResolveSingleLineLabel(resolvePhases(firstTerminal));
            var secondLine = ResolveSingleLineLabel(resolvePhases(secondTerminal));
            var firstNeutral = canReachNeutral(firstTerminal);
            var secondNeutral = canReachNeutral(secondTerminal);

            var valid = false;
            var lineLabel = string.Empty;
            if (!string.IsNullOrWhiteSpace(firstLine) && secondNeutral && string.IsNullOrWhiteSpace(secondLine))
            {
                valid = true;
                lineLabel = firstLine;
            }
            else if (!string.IsNullOrWhiteSpace(secondLine) && firstNeutral && string.IsNullOrWhiteSpace(firstLine))
            {
                valid = true;
                lineLabel = secondLine;
            }

            if (!valid)
            {
                return true;
            }

            var voltage = ResolvePositive(sourceVoltage, ResolveParameterValue(component, "ratedVoltage", component.Definition.ratedVoltage));
            var power = Mathf.Max(0f, ResolveParameterValue(component, "ratedPower", component.Definition.ratedPower));
            result.HasEffectiveSinglePhaseVoltage = voltage > 0f;
            result.LineLabel = lineLabel;
            result.MeasuredVoltage = result.HasEffectiveSinglePhaseVoltage ? voltage : 0f;
            result.MeasuredPower = result.HasEffectiveSinglePhaseVoltage ? power : 0f;
            result.MeasuredCurrent = result.MeasuredVoltage > 0f ? power / result.MeasuredVoltage : 0f;
            result.RatedPower = power;
            return true;
        }

        public static bool TryCalculateThreePhaseMotor(
            CircuitComponent component,
            bool circuitActive,
            float sourceLineVoltage,
            out ThreePhaseMotorEstimate result)
        {
            result = null;
            if (!IsThreePhaseTeachingMotor(component))
            {
                return false;
            }

            var lineVoltage = ResolvePositive(sourceLineVoltage, ResolveParameterValue(component, "ratedVoltage", component.Definition.ratedVoltage));
            var ratedPower = Mathf.Max(0f, ResolveParameterValue(component, "ratedPower", component.Definition.ratedPower));
            var efficiency = Mathf.Max(0.01f, ResolveParameterValue(component, "efficiency", 0.85f));
            var powerFactor = Mathf.Max(0.01f, ResolveParameterValue(component, "powerFactor", 0.8f));

            result = new ThreePhaseMotorEstimate
            {
                IsRunning = circuitActive,
                LineVoltage = circuitActive ? lineVoltage : 0f,
                RatedPower = ratedPower,
                Efficiency = efficiency,
                PowerFactor = powerFactor,
                EstimatedCurrent = circuitActive && lineVoltage > 0f && ratedPower > 0f
                    ? ratedPower / (Mathf.Sqrt(3f) * lineVoltage * efficiency * powerFactor)
                    : 0f
            };

            return true;
        }

        public static bool TryCalculateStarDeltaMotor(
            CircuitComponent component,
            StarDeltaMotorEstimateStage stage,
            float sourceLineVoltage,
            out StarDeltaMotorEstimate result)
        {
            result = null;
            if (!IsStarDeltaTeachingMotor(component))
            {
                return false;
            }

            var lineVoltage = ResolvePositive(sourceLineVoltage, ResolveParameterValue(component, "ratedVoltage", component.Definition.ratedVoltage));
            var ratedPower = Mathf.Max(0f, ResolveParameterValue(component, "ratedPower", component.Definition.ratedPower));
            var efficiency = Mathf.Max(0.01f, ResolveParameterValue(component, "efficiency", 0.85f));
            var powerFactor = Mathf.Max(0.01f, ResolveParameterValue(component, "powerFactor", 0.8f));
            var deltaCurrent = lineVoltage > 0f && ratedPower > 0f
                ? ratedPower / (Mathf.Sqrt(3f) * lineVoltage * efficiency * powerFactor)
                : 0f;
            var starCurrent = deltaCurrent / 3f;
            var normal = stage == StarDeltaMotorEstimateStage.Star ||
                stage == StarDeltaMotorEstimateStage.Delta;

            result = new StarDeltaMotorEstimate
            {
                Stage = stage,
                LineVoltage = normal ? lineVoltage : 0f,
                RatedPower = ratedPower,
                Efficiency = efficiency,
                PowerFactor = powerFactor,
                DeltaEstimatedCurrent = deltaCurrent,
                StarEstimatedCurrent = starCurrent,
                EstimatedCurrent = stage == StarDeltaMotorEstimateStage.Star
                    ? starCurrent
                    : stage == StarDeltaMotorEstimateStage.Delta
                        ? deltaCurrent
                        : 0f,
                HasNormalEstimate = normal && lineVoltage > 0f && ratedPower > 0f
            };

            return true;
        }

        public static bool TryEstimateControlCircuitLoad(
            CircuitComponent component,
            out ControlCircuitLoadEstimate result)
        {
            result = null;
            if (!IsControlCircuitTeachingLoad(component))
            {
                return false;
            }

            var ratedVoltage = ResolveParameterValue(component, "ratedVoltage", component.Definition.ratedVoltage);
            if (ratedVoltage <= 0f)
            {
                ratedVoltage = ResolveParameterValue(component, "sourceVoltage", component.Definition.sourceVoltage);
            }

            var ratedPower = ResolveParameterValue(component, "ratedPower", component.Definition.ratedPower);
            var ratedCurrent = ResolveParameterValue(component, "ratedCurrent", component.Definition.ratedCurrent);
            var estimatedCurrent = ratedPower > 0f && ratedVoltage > 0f ? ratedPower / ratedVoltage : ratedCurrent;

            result = new ControlCircuitLoadEstimate
            {
                DisplayName = component.Definition.displayName,
                LoadType = ControlLoadTypeName(component),
                RatedVoltage = Mathf.Max(0f, ratedVoltage),
                RatedPower = Mathf.Max(0f, ratedPower),
                RatedCurrent = Mathf.Max(0f, ratedCurrent),
                EstimatedCurrent = Mathf.Max(0f, estimatedCurrent),
                HasEnoughParameters = ratedVoltage > 0f && (ratedPower > 0f || ratedCurrent > 0f)
            };

            return true;
        }

        public static bool IsSinglePhaseTeachingLoad(CircuitComponent component)
        {
            if (component == null || component.Definition == null ||
                !component.Definition.canParticipateInParameterCalculation)
            {
                return false;
            }

            if (component.Definition.kind == ComponentKind.Lamp ||
                component.Definition.kind == ComponentKind.Fan)
            {
                return true;
            }

            if (component.Definition.kind != ComponentKind.Indicator)
            {
                return false;
            }

            var ratedVoltage = ResolveParameterValue(component, "ratedVoltage", component.Definition.ratedVoltage);
            return ratedVoltage > 0f && ratedVoltage < 300f;
        }

        public static bool IsThreePhaseTeachingMotor(CircuitComponent component)
        {
            return component != null &&
                component.Definition != null &&
                component.Definition.canParticipateInParameterCalculation &&
                component.Definition.kind == ComponentKind.Motor &&
                component.GetTerminal("U") != null &&
                component.GetTerminal("V") != null &&
                component.GetTerminal("W") != null &&
                component.GetTerminal("U1") == null &&
                component.GetTerminal("V1") == null &&
                component.GetTerminal("W1") == null;
        }

        public static bool IsStarDeltaTeachingMotor(CircuitComponent component)
        {
            return component != null &&
                component.Definition != null &&
                component.Definition.canParticipateInParameterCalculation &&
                component.Definition.kind == ComponentKind.Motor &&
                component.GetTerminal("U1") != null &&
                component.GetTerminal("V1") != null &&
                component.GetTerminal("W1") != null &&
                component.GetTerminal("U2") != null &&
                component.GetTerminal("V2") != null &&
                component.GetTerminal("W2") != null;
        }

        public static bool IsControlCircuitTeachingLoad(CircuitComponent component)
        {
            if (component == null || component.Definition == null ||
                component.Definition.supportLevel == ComponentSupportLevel.VisualOnly ||
                !component.Definition.canParticipateInRuntime)
            {
                return false;
            }

            if (component.Definition.kind == ComponentKind.Indicator)
            {
                return component.Definition.canParticipateInParameterCalculation;
            }

            if (IsTimerRelay(component))
            {
                return true;
            }

            return IsContactor(component);
        }

        public static float ResolveParameterValue(CircuitComponent component, string key, float fallback)
        {
            var parameter = component != null ? component.GetParameter(key) : null;
            return parameter != null ? parameter.value : fallback;
        }

        public static string LoadDisplayName(CircuitComponent component)
        {
            if (component == null || component.Definition == null)
            {
                return "负载";
            }

            if (component.Definition.kind == ComponentKind.Fan)
            {
                return "电风扇";
            }

            if (component.Definition.kind == ComponentKind.Indicator)
            {
                return "220V 指示灯";
            }

            return "灯泡";
        }

        private static string ControlLoadTypeName(CircuitComponent component)
        {
            if (component.Definition.kind == ComponentKind.Indicator)
            {
                return "指示灯";
            }

            if (IsTimerRelay(component))
            {
                return "时间继电器 KT 线圈";
            }

            return "接触器线圈";
        }

        private static bool IsContactor(CircuitComponent component)
        {
            if (component == null || component.Definition == null)
            {
                return false;
            }

            if (IsTimerRelay(component))
            {
                return false;
            }

            return component.Definition.kind == ComponentKind.ContactorCoil ||
                component.GetTerminal("A1") != null &&
                component.GetTerminal("A2") != null &&
                component.GetTerminal("L1") != null &&
                component.GetTerminal("T1") != null;
        }

        private static bool IsTimerRelay(CircuitComponent component)
        {
            if (component == null || component.Definition == null ||
                component.GetTerminal("A1") == null || component.GetTerminal("A2") == null)
            {
                return false;
            }

            var id = component.Definition.name ?? string.Empty;
            var displayName = component.Definition.displayName ?? string.Empty;
            return id.IndexOf("Timer_", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                id.IndexOf("TimerRelay", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                displayName.Contains("时间继电器");
        }

        private static TerminalView ResolveFirstTerminal(CircuitComponent component)
        {
            return component.GetTerminal("L") ?? component.GetTerminal("A1");
        }

        private static TerminalView ResolveSecondTerminal(CircuitComponent component)
        {
            return component.GetTerminal("N") ?? component.GetTerminal("A2");
        }

        private static float ResolvePositive(float preferred, float fallback)
        {
            return preferred > 0f ? preferred : Mathf.Max(0f, fallback);
        }

        private static string ResolveSingleLineLabel(IReadOnlyCollection<string> phaseKeys)
        {
            if (phaseKeys == null || phaseKeys.Count != 1)
            {
                return string.Empty;
            }

            foreach (var key in phaseKeys)
            {
                return NormalizeLineLabel(key);
            }

            return string.Empty;
        }

        private static string NormalizeLineLabel(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            var dot = key.LastIndexOf('.');
            return dot >= 0 && dot < key.Length - 1 ? key.Substring(dot + 1) : key;
        }
    }
}
