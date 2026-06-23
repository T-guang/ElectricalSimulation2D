using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ElectricalSim.Core
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class CircuitComponent : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        [SerializeField] private Image body;
        [SerializeField] private Text title;
        [SerializeField] private Text stateLabel;
        [SerializeField] private ComponentParameterSet parameterSet = new ComponentParameterSet();

        public string InstanceId { get; private set; }
        public ComponentDefinition Definition { get; private set; }
        public bool IsClosed { get; private set; }
        public bool IsEnergized { get; private set; }
        public float MeasuredVoltage { get; private set; }
        public float MeasuredCurrent { get; private set; }
        public float MeasuredPower { get; private set; }
        public IReadOnlyList<TerminalView> Terminals => terminals;

        private readonly List<TerminalView> terminals = new List<TerminalView>();
        private WorkspaceController workspace;
        private RectTransform rectTransform;
        private Vector2 dragOffset;
        private bool selected;

        public void Initialize(ComponentDefinition definition, WorkspaceController owner, string instanceId = null)
        {
            InstanceId = string.IsNullOrWhiteSpace(instanceId) ? System.Guid.NewGuid().ToString("N") : instanceId;
            Definition = definition;
            workspace = owner;
            rectTransform = GetComponent<RectTransform>();
            IsClosed = definition.startsClosed;
            parameterSet.SetParameters(definition.parameters);
            EnsureInstanceParametersFromDefinition();

            if (body != null)
            {
                body.color = definition.bodyColor;
            }

            if (title != null)
            {
                title.text = ResolveInstanceDisplayName(definition.displayName, InstanceId);
            }

            BuildTerminals();
            RefreshVisual();
        }

        private static string ResolveInstanceDisplayName(string defaultName, string instanceId)
        {
            if (string.Equals(instanceId, "km_main", System.StringComparison.OrdinalIgnoreCase))
            {
                return "主接触器 KM";
            }

            if (string.Equals(instanceId, "km_star", System.StringComparison.OrdinalIgnoreCase))
            {
                return "星形接触器 KMY";
            }

            if (string.Equals(instanceId, "km_delta", System.StringComparison.OrdinalIgnoreCase))
            {
                return "三角形接触器 KMD";
            }

            return defaultName;
        }

        public TerminalView GetTerminal(string terminalId)
        {
            return terminals.Find(t => t.TerminalId == terminalId);
        }

        public void SetParameters(IEnumerable<ComponentParameter> parameters)
        {
            if (!HasAnyParameter(parameters) &&
                parameterSet.parameters != null &&
                parameterSet.parameters.Count > 0)
            {
                EnsureInstanceParametersFromDefinition();
                return;
            }

            parameterSet.SetParameters(parameters);
            EnsureInstanceParametersFromDefinition();
        }

        public void EnsureInstanceParametersFromDefinition()
        {
            if (Definition == null || Definition.parameters == null || Definition.parameters.Count == 0)
            {
                return;
            }

            if (parameterSet.parameters == null)
            {
                parameterSet.parameters = new List<ComponentParameter>();
            }

            var existingKeys = new HashSet<string>();
            var existingCanonicalKeys = new HashSet<string>();
            for (var i = 0; i < parameterSet.parameters.Count; i++)
            {
                var parameter = parameterSet.parameters[i];
                if (parameter != null && !string.IsNullOrWhiteSpace(parameter.key))
                {
                    existingKeys.Add(parameter.key);
                    existingCanonicalKeys.Add(ParameterAliases.GetCanonicalKey(parameter.key));
                }
            }

            for (var i = 0; i < Definition.parameters.Count; i++)
            {
                var definitionParameter = Definition.parameters[i];
                var canonicalKey = definitionParameter != null
                    ? ParameterAliases.GetCanonicalKey(definitionParameter.key)
                    : null;
                if (definitionParameter == null ||
                    string.IsNullOrWhiteSpace(definitionParameter.key) ||
                    existingKeys.Contains(definitionParameter.key) ||
                    existingCanonicalKeys.Contains(canonicalKey))
                {
                    continue;
                }

                var clone = definitionParameter.Clone();
                clone.ClampValue();
                parameterSet.parameters.Add(clone);
                existingKeys.Add(clone.key);
                existingCanonicalKeys.Add(ParameterAliases.GetCanonicalKey(clone.key));
            }
        }

        private static bool HasAnyParameter(IEnumerable<ComponentParameter> parameters)
        {
            if (parameters == null)
            {
                return false;
            }

            foreach (var parameter in parameters)
            {
                if (parameter != null && !string.IsNullOrWhiteSpace(parameter.key))
                {
                    return true;
                }
            }

            return false;
        }

        public ComponentParameter GetParameter(string key)
        {
            return parameterSet.GetParameter(key);
        }

        public bool SetParameterValue(string key, float value)
        {
            return parameterSet.SetParameterValue(key, value);
        }

        public IReadOnlyList<ComponentParameter> GetAllParameters()
        {
            return parameterSet.parameters;
        }

        public List<ComponentParameter> CloneParameters()
        {
            return parameterSet.CloneList();
        }

        public void SetEnergized(bool energized)
        {
            IsEnergized = energized;
            RefreshVisual();
        }

        public void SetMeasurement(float voltage, float current, float power)
        {
            MeasuredVoltage = voltage;
            MeasuredCurrent = current;
            MeasuredPower = power;
        }

        public void ClearMeasurement()
        {
            SetMeasurement(0f, 0f, 0f);
        }

        public void SetClosed(bool closed)
        {
            IsClosed = closed;
            RefreshVisual();
        }

        public void SetSelected(bool isSelected)
        {
            selected = isSelected;
            RefreshVisual();
        }

        public void Toggle()
        {
            if (workspace != null && workspace.IsInteractionLocked)
            {
                workspace.SetStatus("画布已锁定，解锁后再切换元件状态。");
                return;
            }

            if (!Definition.togglable)
            {
                return;
            }

            workspace?.RecordHistoryCheckpoint();
            IsClosed = !IsClosed;
            RefreshVisual();
            var statusMessage = IsOnDelayTimerRelay()
                ? "时间继电器兼容状态已切换：该字段仅用于旧图纸兼容；当前延时触点以 KT 运行态计时为准。"
                : "开关状态已改变，点击开始仿真刷新结果。";
            workspace?.MarkSimulationDirty(statusMessage);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (workspace != null && workspace.IsInteractionLocked)
            {
                workspace.SetStatus("画布已锁定，当前不能选择元件。");
                return;
            }

            if (eventData.clickCount >= 2)
            {
                Toggle();
            }
            else
            {
                workspace?.SelectComponent(this);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (workspace != null && workspace.IsInteractionLocked)
            {
                workspace.SetStatus("画布已锁定，解锁后再移动元件。");
                return;
            }

            workspace?.RecordHistoryCheckpoint();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out dragOffset);
            workspace?.SelectComponent(this);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (workspace == null || workspace.IsInteractionLocked)
            {
                return;
            }

            if (workspace.TryScreenToCanvasLocal(eventData.position, eventData.pressEventCamera, out var localPoint))
            {
                rectTransform.anchoredPosition = workspace.Snap(localPoint - dragOffset);
                workspace.RefreshWiresFor(this);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            workspace?.MarkSimulationDirty("元件位置已调整，点击开始仿真刷新结果。");
        }

        private void BuildTerminals()
        {
            foreach (var terminal in terminals)
            {
                if (terminal != null)
                {
                    Destroy(terminal.gameObject);
                }
            }

            terminals.Clear();

            foreach (var terminalDefinition in Definition.terminals)
            {
                var terminalObject = new GameObject("Terminal_" + terminalDefinition.id, typeof(RectTransform), typeof(Image), typeof(Button), typeof(TerminalView));
                terminalObject.transform.SetParent(transform, false);

                var terminalRect = terminalObject.GetComponent<RectTransform>();
                terminalRect.sizeDelta = new Vector2(18f, 18f);
                terminalRect.anchorMin = terminalDefinition.normalizedPosition;
                terminalRect.anchorMax = terminalDefinition.normalizedPosition;
                terminalRect.anchoredPosition = Vector2.zero;

                var terminalImage = terminalObject.GetComponent<Image>();
                terminalImage.color = terminalDefinition.color;

                var terminal = terminalObject.GetComponent<TerminalView>();
                terminal.Initialize(this, terminalDefinition, workspace);
                terminals.Add(terminal);

                if (Definition.sprite != null)
                {
                    var labelObject = new GameObject("Label_" + terminalDefinition.id, typeof(RectTransform), typeof(Text));
                    labelObject.transform.SetParent(transform, false);
                    var labelRect = labelObject.GetComponent<RectTransform>();
                    labelRect.sizeDelta = new Vector2(44f, 18f);
                    labelRect.anchorMin = terminalDefinition.normalizedPosition;
                    labelRect.anchorMax = terminalDefinition.normalizedPosition;
                    labelRect.anchoredPosition = GetTerminalLabelOffset(terminalDefinition.normalizedPosition);

                    var label = labelObject.GetComponent<Text>();
                    label.text = terminalDefinition.label;
                    label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    label.fontSize = 15;
                    label.fontStyle = FontStyle.Bold;
                    label.alignment = TextAnchor.MiddleCenter;
                    label.color = new Color(0.04f, 0.06f, 0.1f);
                    label.raycastTarget = false;
                }
            }
        }

        private static Vector2 GetTerminalLabelOffset(Vector2 normalizedPosition)
        {
            if (normalizedPosition.y > 0.75f)
            {
                return new Vector2(0f, -24f);
            }

            if (normalizedPosition.y < 0.25f)
            {
                return new Vector2(0f, 24f);
            }

            return new Vector2(0f, 28f);
        }

        private void RefreshVisual()
        {
            if (body != null && Definition != null)
            {
                var baseColor = Definition.sprite != null ? Color.white : Definition.bodyColor;
                var color = IsEnergized ? Color.Lerp(baseColor, Definition.accentColor, 0.45f) : baseColor;
                body.color = selected ? Color.Lerp(color, Color.white, 0.35f) : color;
            }

            if (stateLabel != null && Definition != null)
            {
                if (Definition.kind == ComponentKind.TwoWaySwitch)
                {
                    stateLabel.text = IsClosed ? "L-L1" : "L-L2";
                    stateLabel.color = new Color(0.05f, 0.42f, 0.9f);
                }
                else if (IsOnDelayTimerRelay())
                {
                    ConfigureOnDelayTimerStateLabel(stateLabel);
                    stateLabel.text = GetOnDelayTimerRuntimeText();
                    stateLabel.color = GetOnDelayTimerRuntimeColor();
                }
                else if (Definition.togglable)
                {
                    ConfigureDefaultStateLabel(stateLabel);
                    stateLabel.fontSize = 18;
                    stateLabel.text = IsClosed ? "ON" : "OFF";
                    stateLabel.color = IsClosed ? new Color(0.05f, 0.55f, 0.24f) : new Color(0.65f, 0.1f, 0.1f);
                }
                else if (IsAutoReciprocatingMotionMotor())
                {
                    ConfigureMotionStateLabel(stateLabel);
                    stateLabel.text = GetMotionRuntimeText();
                    stateLabel.color = IsEnergized ? new Color(0.05f, 0.45f, 0.95f) : new Color(0.32f, 0.36f, 0.42f);
                }
                else
                {
                    ConfigureDefaultStateLabel(stateLabel);
                    stateLabel.fontSize = 18;
                    stateLabel.text = IsEnergized ? GetRunStateText() : "";
                    stateLabel.color = new Color(0.05f, 0.45f, 0.95f);
                }
            }
        }

        private static void ConfigureMotionStateLabel(Text label)
        {
            if (label == null)
            {
                return;
            }

            label.fontSize = 11;
            label.lineSpacing = 0.88f;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 8;
            label.resizeTextMaxSize = 11;

            var rect = label.rectTransform;
            if (rect != null)
            {
                rect.anchorMin = new Vector2(-0.18f, 0.08f);
                rect.anchorMax = new Vector2(1.18f, 0.92f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
        }

        private static void ConfigureDefaultStateLabel(Text label)
        {
            if (label == null)
            {
                return;
            }

            label.lineSpacing = 1f;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.resizeTextForBestFit = false;
        }

        private static void ConfigureOnDelayTimerStateLabel(Text label)
        {
            if (label == null)
            {
                return;
            }

            label.fontSize = 11;
            label.lineSpacing = 0.85f;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 8;
            label.resizeTextMaxSize = 11;

            var rect = label.rectTransform;
            if (rect != null)
            {
                rect.anchorMin = new Vector2(-0.15f, 0.2f);
                rect.anchorMax = new Vector2(1.15f, 0.8f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
        }

        private string GetOnDelayTimerRuntimeText()
        {
            if (!RuntimeStateManager.Shared.TryGetTimerState(InstanceId, out var timerState) || timerState == null)
            {
                return "KT: Reset 0.0 / " + ResolveDelaySeconds().ToString("0.0") + "s\n线圈未得电";
            }

            var coilText = timerState.IsCoilEnergized ? "得电" : "未得电";
            return "KT: " + timerState.Phase + " " +
                   timerState.ElapsedSeconds.ToString("0.0") + " / " +
                   timerState.DelaySeconds.ToString("0.0") + "s，线圈" +
                   coilText;
        }

        private float ResolveDelaySeconds()
        {
            var parameter = GetParameter("delaySeconds");
            return parameter != null ? Mathf.Max(0f, parameter.value) : 3f;
        }

        private Color GetOnDelayTimerRuntimeColor()
        {
            if (!RuntimeStateManager.Shared.TryGetTimerState(InstanceId, out var timerState) || timerState == null)
            {
                return IsClosed ? new Color(0.05f, 0.55f, 0.24f) : new Color(0.65f, 0.1f, 0.1f);
            }

            switch (timerState.Phase)
            {
                case TimerRuntimePhase.Elapsed:
                    return new Color(0.05f, 0.55f, 0.24f);
                case TimerRuntimePhase.Timing:
                    return new Color(0.85f, 0.46f, 0.08f);
                default:
                    return new Color(0.65f, 0.1f, 0.1f);
            }
        }

        private bool IsOnDelayTimerRelay()
        {
            return Definition != null &&
                   !string.IsNullOrWhiteSpace(Definition.name) &&
                   Definition.name.IndexOf("Timer_OnDelay", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private bool IsAutoReciprocatingMotionMotor()
        {
            return Definition != null &&
                   Definition.kind == ComponentKind.Motor &&
                   string.Equals(InstanceId, "motor_1", System.StringComparison.OrdinalIgnoreCase) &&
                   GetTerminal("U") != null &&
                   GetTerminal("V") != null &&
                   GetTerminal("W") != null &&
                   HasWorkspaceComponent("sq_left") &&
                   HasWorkspaceComponent("sq_right") &&
                   HasWorkspaceComponent("km_forward") &&
                   HasWorkspaceComponent("km_reverse");
        }

        private bool HasWorkspaceComponent(string instanceId)
        {
            if (workspace == null || workspace.Components == null || string.IsNullOrWhiteSpace(instanceId))
            {
                return false;
            }

            for (var i = 0; i < workspace.Components.Count; i++)
            {
                var component = workspace.Components[i];
                if (component != null &&
                    string.Equals(component.InstanceId, instanceId, System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private string GetMotionRuntimeText()
        {
            var motionState = RuntimeStateManager.Shared.GetOrCreateMotionState(InstanceId);
            var electricalState = IsEnergized ? GetRunStateText() : "停止";
            if (motionState == null)
            {
                return "电气：" + electricalState + "\n虚拟运动：停止\n虚拟位置：50 / 100\n虚拟左限位：未触发\n虚拟右限位：未触发";
            }

            return "电气：" + electricalState +
                   "\n虚拟运动：" + MotionDirectionText(motionState.Direction) +
                   "\n虚拟位置：" + motionState.Position.ToString("0") + " / 100" +
                   "\n虚拟左限位：" + (motionState.LeftLimitTriggered ? "已触发" : "未触发") +
                   "\n虚拟右限位：" + (motionState.RightLimitTriggered ? "已触发" : "未触发") +
                   "\n速度：" + motionState.Speed.ToString("0") + " / s";
        }

        private static string MotionDirectionText(MotionDirection direction)
        {
            switch (direction)
            {
                case MotionDirection.Forward:
                    return "正向";
                case MotionDirection.Reverse:
                    return "反向";
                default:
                    return "停止";
            }
        }

        private string GetRunStateText()
        {
            if (Definition != null && Definition.kind == ComponentKind.Motor && GetTerminal("U") != null && GetTerminal("V") != null && GetTerminal("W") != null)
            {
                var direction = GetParameter("rotationDirection");
                if (direction != null)
                {
                    if (direction.value > 0.5f)
                    {
                        return "正转";
                    }

                    if (direction.value < -0.5f)
                    {
                        return "反转";
                    }
                }
            }

            return "RUN";
        }
    }
}

