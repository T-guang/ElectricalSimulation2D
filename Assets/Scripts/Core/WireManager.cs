using System.Collections.Generic;
using UnityEngine;

namespace ElectricalSim.Core
{
    public sealed class WireManager : MonoBehaviour
    {
        [SerializeField] private RectTransform wireLayer;

        public IReadOnlyList<WireView> Wires => wires;

        private readonly List<WireView> wires = new List<WireView>();
        private WorkspaceController workspace;

        public void Initialize(RectTransform layer, WorkspaceController owner)
        {
            wireLayer = layer;
            workspace = owner;
        }

        public WorkspaceController Workspace => workspace;

        public WireView CreateWire(TerminalView start, TerminalView end, Color color, WireStyle style)
        {
            if (start == null || end == null || start == end || start.Owner == end.Owner)
            {
                return null;
            }

            var existing = wires.Find(w => (w.StartTerminal == start && w.EndTerminal == end) || (w.StartTerminal == end && w.EndTerminal == start));
            if (existing != null)
            {
                return existing;
            }

            var go = new GameObject("Wire", typeof(RectTransform), typeof(WireView));
            go.transform.SetParent(wireLayer, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var wire = go.GetComponent<WireView>();
            wire.Initialize(start, end, color, style, workspace);
            wires.Add(wire);
            ReflowOffsets();
            return wire;
        }

        public void DeleteWire(WireView wire)
        {
            if (wire == null)
            {
                return;
            }

            wires.Remove(wire);
            Destroy(wire.gameObject);
            ReflowOffsets();
        }

        public void DeleteWiresFor(CircuitComponent component)
        {
            for (var i = wires.Count - 1; i >= 0; i--)
            {
                if (wires[i] != null && wires[i].Uses(component))
                {
                    DeleteWire(wires[i]);
                }
            }
        }

        public void Clear()
        {
            foreach (var wire in wires)
            {
                if (wire != null)
                {
                    Destroy(wire.gameObject);
                }
            }

            wires.Clear();
        }

        public void RefreshAll()
        {
            ReflowOffsets();
            foreach (var wire in wires)
            {
                wire.Refresh();
            }
        }

        public void RefreshFor(CircuitComponent component)
        {
            ReflowOffsets();
            foreach (var wire in wires)
            {
                if (wire.Uses(component))
                {
                    wire.Refresh();
                }
            }
        }

        private void ReflowOffsets()
        {
            var lanes = new[] { 0f, 22f, -22f, 44f, -44f, 66f, -66f, 88f, -88f };

            for (var i = 0; i < wires.Count; i++)
            {
                var lane = i % lanes.Length;
                var band = i / lanes.Length;
                var offset = lanes[lane];
                if (band > 0)
                {
                    var direction = Mathf.Abs(offset) < 0.1f || offset > 0f ? 1f : -1f;
                    offset += direction * band * 16f;
                }

                wires[i].SetRouteOffset(offset);
            }
        }
    }
}

