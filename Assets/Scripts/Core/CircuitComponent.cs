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

            if (body != null)
            {
                body.color = definition.bodyColor;
            }

            if (title != null)
            {
                title.text = definition.displayName;
            }

            BuildTerminals();
            RefreshVisual();
        }

        public TerminalView GetTerminal(string terminalId)
        {
            return terminals.Find(t => t.TerminalId == terminalId);
        }

        public void SetParameters(IEnumerable<ComponentParameter> parameters)
        {
            parameterSet.SetParameters(parameters);
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
            workspace?.MarkSimulationDirty("开关状态已改变，点击开始仿真刷新结果。");
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
                else if (Definition.togglable)
                {
                    stateLabel.text = IsClosed ? "ON" : "OFF";
                    stateLabel.color = IsClosed ? new Color(0.05f, 0.55f, 0.24f) : new Color(0.65f, 0.1f, 0.1f);
                }
                else
                {
                    stateLabel.text = IsEnergized ? "RUN" : "";
                    stateLabel.color = new Color(0.05f, 0.45f, 0.95f);
                }
            }
        }
    }
}

