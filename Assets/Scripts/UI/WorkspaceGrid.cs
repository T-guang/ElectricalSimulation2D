using UnityEngine;
using UnityEngine.UI;

namespace ElectricalSim.UI
{
    public sealed class WorkspaceGrid : MaskableGraphic
    {
        [SerializeField] private float spacing = 20f;
        [SerializeField] private Color lineColor = new Color(0.918f, 0.941f, 0.969f, 1f); // EAF0F7
        [SerializeField] private Color majorLineColor = new Color(0.843f, 0.886f, 0.941f, 1f); // D7E2F0
        [SerializeField] private int majorLineEvery = 5;
        [SerializeField] private float lineWidth = 1f;
        [SerializeField] private float majorLineWidth = 1.35f;

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
            ApplyDesignDefaults();
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            raycastTarget = false;
            ApplyDesignDefaults();
        }

        private void ApplyDesignDefaults()
        {
            spacing = 20f;
            lineColor = MainUiTheme.Hex("EAF0F7");
            majorLineColor = MainUiTheme.Hex("D7E2F0");
            majorLineEvery = 5;
            lineWidth = 1.0f;
            majorLineWidth = 1.35f;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var rect = rectTransform.rect;
            var index = 0;
            var verticalIndex = 0;

            for (var x = rect.xMin; x <= rect.xMax; x += spacing)
            {
                var isMajor = majorLineEvery > 0 && verticalIndex % majorLineEvery == 0;
                AddLine(vh, ref index, new Vector2(x, rect.yMin), new Vector2(x, rect.yMax), isMajor);
                verticalIndex++;
            }

            var horizontalIndex = 0;
            for (var y = rect.yMin; y <= rect.yMax; y += spacing)
            {
                var isMajor = majorLineEvery > 0 && horizontalIndex % majorLineEvery == 0;
                AddLine(vh, ref index, new Vector2(rect.xMin, y), new Vector2(rect.xMax, y), isMajor);
                horizontalIndex++;
            }
        }

        private void AddLine(VertexHelper vh, ref int index, Vector2 a, Vector2 b, bool isMajor)
        {
            var direction = (b - a).normalized;
            var width = isMajor ? majorLineWidth : lineWidth;
            var normal = new Vector2(-direction.y, direction.x) * width;
            var color = isMajor ? majorLineColor : lineColor;
            vh.AddVert(a - normal, color, Vector2.zero);
            vh.AddVert(a + normal, color, Vector2.zero);
            vh.AddVert(b + normal, color, Vector2.zero);
            vh.AddVert(b - normal, color, Vector2.zero);
            vh.AddTriangle(index, index + 1, index + 2);
            vh.AddTriangle(index + 2, index + 3, index);
            index += 4;
        }
    }
}

