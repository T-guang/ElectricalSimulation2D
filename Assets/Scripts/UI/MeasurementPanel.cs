using ElectricalSim.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ElectricalSim.UI
{
    public sealed class MeasurementPanel : MonoBehaviour
    {
        [SerializeField] private Text multimeterText;
        [SerializeField] private Text oscilloscopeText;
        [SerializeField] private OscilloscopeWaveform waveform;

        public void ShowComponent(CircuitComponent component, bool simulationRunning)
        {
            if (component == null || component.Definition == null)
            {
                SetIdle();
                return;
            }

            var definition = component.Definition;
            var isSource = definition.kind == ComponentKind.PowerSource;
            var active = simulationRunning && (component.IsEnergized || isSource);
            var voltage = active ? GetDisplayVoltage(definition, component.MeasuredVoltage) : 0f;
            var current = active && !isSource ? component.MeasuredCurrent : 0f;
            var power = active && !isSource ? component.MeasuredPower : 0f;

            if (multimeterText != null)
            {
                multimeterText.text =
                    "万用表\n" +
                    $"对象：{definition.displayName.Replace("\n", " ")}\n" +
                    $"状态：{GetStateText(definition, simulationRunning, component.IsEnergized)}\n" +
                    $"测量电压 U：{FormatValue(voltage, "V")}\n" +
                    $"测量电流 I：{FormatValue(current, "A")}\n" +
                    $"估算功率 P：{FormatValue(power, "W")}\n" +
                    $"额定参数：{BuildRatedLine(component)}";
            }

            var signalVoltage = active ? GetSignalVoltage(definition, voltage) : 0f;
            if (oscilloscopeText != null)
            {
                oscilloscopeText.text =
                    "简易示波器\n" +
                    $"{GetSignalName(definition, simulationRunning, component.IsEnergized)}\n" +
                    $"参考峰值：{FormatValue(signalVoltage, "V")}\n" +
                    "频率：50 Hz 教学参考";
            }

            if (waveform != null)
            {
                waveform.SetSignal(signalVoltage, IsAlternatingSignal(definition), active);
            }
        }

        private void SetIdle()
        {
            if (multimeterText != null)
            {
                multimeterText.text =
                    "万用表\n" +
                    "未选择元件\n" +
                    "点击画布中的元件查看测量值。\n" +
                    "测量电压 U：-- V\n" +
                    "测量电流 I：-- A\n" +
                    "估算功率 P：-- W";
            }

            if (oscilloscopeText != null)
            {
                oscilloscopeText.text =
                    "简易示波器\n" +
                    "等待选择元件\n" +
                    "未开始仿真时显示为零线。";
            }

            if (waveform != null)
            {
                waveform.SetSignal(0f, false, false);
            }
        }

        private static string BuildRatedLine(CircuitComponent component)
        {
            if (component == null || component.Definition == null)
            {
                return "暂未配置";
            }

            var definition = component.Definition;
            var voltage = ResolveVoltage(component, definition);
            var power = ResolvePower(component, definition);
            var current = ResolveCurrent(component, definition);

            if (voltage > 0f && power > 0f)
            {
                current = power / voltage;
            }

            if (definition.kind == ComponentKind.PowerSource)
            {
                if (definition.sourcePhaseCount >= 3)
                {
                    var lineVoltage = definition.sourceLineVoltage > 0f ? definition.sourceLineVoltage : voltage;
                    return $"{voltage:0}V 相电压 / {lineVoltage:0}V 线电压";
                }

                return voltage > 0f ? $"{voltage:0}V 输出" : "未配置";
            }

            if (voltage <= 0f && power <= 0f && current <= 0f)
            {
                return "暂未配置";
            }

            return $"{voltage:0}V / {power:0.#}W / {current:0.###}A";
        }

        private static float ResolveVoltage(CircuitComponent component, ComponentDefinition definition)
        {
            if (TryGetParameterValue(component, "ratedVoltage", out var value))
            {
                return value;
            }

            if (TryGetParameterValue(component, "voltage", out value))
            {
                return value;
            }

            return definition.ratedVoltage > 0f ? definition.ratedVoltage : definition.sourceVoltage;
        }

        private static float ResolvePower(CircuitComponent component, ComponentDefinition definition)
        {
            if (TryGetParameterValue(component, "power", out var value))
            {
                return value;
            }

            if (TryGetParameterValue(component, "ratedPower", out value))
            {
                return value;
            }

            return definition.ratedPower;
        }

        private static float ResolveCurrent(CircuitComponent component, ComponentDefinition definition)
        {
            if (TryGetParameterValue(component, "current", out var value))
            {
                return value;
            }

            if (TryGetParameterValue(component, "ratedCurrent", out value))
            {
                return value;
            }

            return definition.ratedCurrent;
        }

        private static bool TryGetParameterValue(CircuitComponent component, string key, out float value)
        {
            value = 0f;
            var parameter = component != null ? component.GetParameter(key) : null;
            if (parameter == null)
            {
                return false;
            }

            value = parameter.value;
            return true;
        }

        private static float GetDisplayVoltage(ComponentDefinition definition, float measuredVoltage)
        {
            if (definition.kind == ComponentKind.PowerSource)
            {
                return definition.sourceLineVoltage > 0f ? definition.sourceLineVoltage : definition.sourceVoltage;
            }

            return measuredVoltage;
        }

        private static float GetSignalVoltage(ComponentDefinition definition, float measuredVoltage)
        {
            if (definition.kind == ComponentKind.PowerSource)
            {
                return definition.sourceLineVoltage > 0f ? definition.sourceLineVoltage : definition.sourceVoltage;
            }

            return measuredVoltage;
        }

        private static string GetSignalName(ComponentDefinition definition, bool simulationRunning, bool energized)
        {
            if (!simulationRunning)
            {
                return "仿真未开始，无有效信号";
            }

            if (definition.kind == ComponentKind.PowerSource)
            {
                return definition.sourcePhaseCount >= 3 ? "三相交流参考波形" : "单相交流参考波形";
            }

            return energized ? "检测到运行信号" : "元件未通电，无有效信号";
        }

        private static string GetStateText(ComponentDefinition definition, bool simulationRunning, bool energized)
        {
            if (!simulationRunning)
            {
                return "未开始仿真";
            }

            if (definition.kind == ComponentKind.PowerSource)
            {
                return "电源输出参考";
            }

            return energized ? "RUN" : "未通电";
        }

        private static bool IsAlternatingSignal(ComponentDefinition definition)
        {
            return definition.kind != ComponentKind.TerminalBlock && !definition.name.Contains("24V");
        }

        private static string FormatValue(float value, string unit)
        {
            return value <= 0f ? $"0 {unit}" : $"{value:0.###} {unit}";
        }
    }
}
