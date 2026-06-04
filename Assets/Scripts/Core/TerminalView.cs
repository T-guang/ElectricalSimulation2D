using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ElectricalSim.Core
{
    public sealed class TerminalView : MonoBehaviour, IPointerClickHandler
    {
        public string TerminalId { get; private set; }
        public string Label { get; private set; }
        public TerminalRole Role { get; private set; }
        public CircuitComponent Owner { get; private set; }
        public Color TerminalColor { get; private set; }

        private WorkspaceController workspace;
        private Image image;
        private bool selected;
        private bool wireEndpointHighlighted;

        public void Initialize(CircuitComponent owner, TerminalDefinition definition, WorkspaceController ownerWorkspace)
        {
            Owner = owner;
            TerminalId = definition.id;
            Label = definition.label;
            Role = definition.role;
            TerminalColor = definition.color;
            workspace = ownerWorkspace;
            image = GetComponent<Image>();
            SetSelected(false);
        }

        public Vector3 WorldPosition => transform.position;

        public void SetSelected(bool isSelected)
        {
            selected = isSelected;
            ApplyVisualState();
        }

        public void SetWireEndpointHighlight(bool highlighted)
        {
            wireEndpointHighlighted = highlighted;
            ApplyVisualState();
        }

        private void ApplyVisualState()
        {
            if (image == null)
            {
                image = GetComponent<Image>();
            }

            if (image != null)
            {
                image.raycastTarget = true;
            }

            var scale = wireEndpointHighlighted ? 1.45f : selected ? 1.25f : 1f;
            transform.localScale = Vector3.one * scale;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            workspace?.HandleTerminalClicked(this);
            eventData.Use();
        }
    }
}

