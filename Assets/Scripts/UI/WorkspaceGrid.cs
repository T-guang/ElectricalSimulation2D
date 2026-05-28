using UnityEngine;
using UnityEngine.UI;

namespace ElectricalSim.UI
{
    public sealed class WorkspaceGrid : MaskableGraphic
    {
        [SerializeField] private float spacing = 32f;
        [SerializeField] private Color lineColor = new Color(0.70f, 0.76f, 0.84f, 0.26f);
        [SerializeField] private Color majorLineColor = new Color(0.62f, 0.69f, 0.78f, 0.34f);
        [SerializeField] private int majorLineEvery = 4;
        [SerializeField] private float lineWidth = 1f;
        [SerializeField] private float majorLineWidth = 1.15f;

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            raycastTarget = false;
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

