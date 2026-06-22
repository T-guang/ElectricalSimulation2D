using System.Collections.Generic;

namespace ElectricalSim.Core
{
    public enum ActualSupplyVoltageKind
    {
        Unknown,
        SinglePhase,
        ThreePhaseLine
    }

    public readonly struct ActualSupplyVoltageResult
    {
        public ActualSupplyVoltageResult(
            bool resolved,
            float voltage,
            ActualSupplyVoltageKind kind,
            string reason)
        {
            Resolved = resolved;
            Voltage = voltage;
            Kind = kind;
            Reason = reason;
        }

        public bool Resolved { get; }
        public float Voltage { get; }
        public ActualSupplyVoltageKind Kind { get; }
        public string Reason { get; }
    }

    public static class ActualSupplyVoltageResolver
    {
        public static float ResolveSinglePhaseVoltage(
            IReadOnlyList<CircuitComponent> components,
            float fallback = 220f)
        {
            if (components == null)
            {
                return fallback;
            }

            for (var i = 0; i < components.Count; i++)
            {
                var component = components[i];
                if (!IsPowerSource(component))
                {
                    continue;
                }

                var voltage = ResolveParameterValueByKeys(
                    component,
                    component.Definition.sourceVoltage,
                    fallback,
                    "sourceVoltage",
                    "voltage");
                if (voltage > 0f)
                {
                    return voltage;
                }
            }

            return fallback;
        }

        public static float ResolveThreePhaseLineVoltage(
            IReadOnlyList<CircuitComponent> components,
            float fallback = 380f)
        {
            if (components == null)
            {
                return fallback;
            }

            for (var i = 0; i < components.Count; i++)
            {
                var component = components[i];
                if (!IsPowerSource(component) || !HasThreePhaseOutputTerminals(component))
                {
                    continue;
                }

                var lineVoltage = ResolvePowerSourceLineVoltage(component, fallback);
                if (lineVoltage > 0f)
                {
                    return lineVoltage;
                }
            }

            for (var i = 0; i < components.Count; i++)
            {
                var component = components[i];
                if (!IsPowerSource(component))
                {
                    continue;
                }

                var lineVoltage = ResolvePowerSourceLineVoltage(component, 0f);
                if (lineVoltage > 0f)
                {
                    return lineVoltage;
                }
            }

            return fallback;
        }

        public static ActualSupplyVoltageResult ResolveAcrossVoltageLabels(
            IReadOnlyList<CircuitComponent> components,
            string firstVoltage,
            string secondVoltage)
        {
            if (string.IsNullOrWhiteSpace(firstVoltage) || string.IsNullOrWhiteSpace(secondVoltage))
            {
                return new ActualSupplyVoltageResult(false, 0f, ActualSupplyVoltageKind.Unknown, "Terminal voltage label is empty.");
            }

            if ((IsLineOrPhase(firstVoltage) && secondVoltage == "N") ||
                (IsLineOrPhase(secondVoltage) && firstVoltage == "N"))
            {
                var voltage = ResolveSinglePhaseVoltage(components);
                return new ActualSupplyVoltageResult(
                    voltage > 0f,
                    voltage,
                    ActualSupplyVoltageKind.SinglePhase,
                    "Resolved as line-neutral voltage.");
            }

            if (IsThreePhaseLine(firstVoltage) &&
                IsThreePhaseLine(secondVoltage) &&
                firstVoltage != secondVoltage)
            {
                var voltage = ResolveThreePhaseLineVoltage(components);
                return new ActualSupplyVoltageResult(
                    voltage > 0f,
                    voltage,
                    ActualSupplyVoltageKind.ThreePhaseLine,
                    "Resolved as phase-phase line voltage.");
            }

            return new ActualSupplyVoltageResult(false, 0f, ActualSupplyVoltageKind.Unknown, "Terminal voltage labels do not form a supported voltage pair.");
        }

        public static ActualSupplyVoltageResult ResolveForEnergizedControlLoad(
            IReadOnlyList<CircuitComponent> components,
            CircuitComponent component)
        {
            if (component == null || !component.IsEnergized)
            {
                return new ActualSupplyVoltageResult(false, 0f, ActualSupplyVoltageKind.Unknown, "Control load is not energized.");
            }

            var ratedVoltage = ResolveParameterValueByKeys(
                component,
                component.Definition != null ? component.Definition.ratedVoltage : 0f,
                0f,
                "ratedVoltage",
                "sourceVoltage",
                "voltage");
            if (ratedVoltage <= 0f)
            {
                return new ActualSupplyVoltageResult(false, 0f, ActualSupplyVoltageKind.Unknown, "Rated voltage is not available.");
            }

            if (ratedVoltage >= 300f)
            {
                var lineVoltage = ResolveThreePhaseLineVoltage(components);
                return new ActualSupplyVoltageResult(
                    lineVoltage > 0f,
                    lineVoltage,
                    ActualSupplyVoltageKind.ThreePhaseLine,
                    "Resolved from energized 380V-rated control load.");
            }

            var phaseVoltage = ResolveSinglePhaseVoltage(components);
            return new ActualSupplyVoltageResult(
                phaseVoltage > 0f,
                phaseVoltage,
                ActualSupplyVoltageKind.SinglePhase,
                "Resolved from energized single-phase control load.");
        }

        private static bool IsPowerSource(CircuitComponent component)
        {
            return component != null &&
                component.Definition != null &&
                component.Definition.kind == ComponentKind.PowerSource;
        }

        private static bool HasThreePhaseOutputTerminals(CircuitComponent component)
        {
            return component != null &&
                component.GetTerminal("L1") != null &&
                component.GetTerminal("L2") != null &&
                component.GetTerminal("L3") != null;
        }

        private static float ResolvePowerSourceLineVoltage(CircuitComponent component, float fallback)
        {
            if (component == null || component.Definition == null)
            {
                return fallback;
            }

            return ResolveParameterValueByKeys(
                component,
                component.Definition.sourceLineVoltage,
                fallback,
                "sourceLineVoltage",
                "lineVoltage");
        }

        private static float ResolveParameterValueByKeys(
            CircuitComponent component,
            float definitionFallback,
            float hardFallback,
            params string[] keys)
        {
            if (component != null && keys != null)
            {
                for (var i = 0; i < keys.Length; i++)
                {
                    var parameter = component.GetParameter(keys[i]);
                    if (parameter != null && parameter.value > 0f)
                    {
                        return parameter.value;
                    }
                }
            }

            return definitionFallback > 0f ? definitionFallback : hardFallback;
        }

        private static bool IsLineOrPhase(string voltage)
        {
            return voltage == "L" || IsThreePhaseLine(voltage);
        }

        private static bool IsThreePhaseLine(string voltage)
        {
            return voltage == "L1" || voltage == "L2" || voltage == "L3";
        }
    }
}
