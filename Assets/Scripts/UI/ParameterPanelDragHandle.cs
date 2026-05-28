using UnityEngine;
using UnityEngine.EventSystems;

namespace ElectricalSim.UI
{
    public sealed class ParameterPanelDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        [SerializeField] private RectTransform target;

        private RectTransform parent;
        private Vector2 pointerStartLocal;
        private Vector2 targetStartPosition;

        public void Initialize(RectTransform targetRect)
        {
            target = targetRect;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (target == null)
            {
                return;
            }

            parent = target.parent as RectTransform;
            if (parent == null)
            {
                return;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, eventData.position, eventData.pressEventCamera, out pointerStartLocal);
            targetStartPosition = target.anchoredPosition;
            target.SetAsLastSibling();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (target == null || parent == null)
            {
                return;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, eventData.position, eventData.pressEventCamera, out var pointerLocal);
            target.anchoredPosition = ClampToParent(targetStartPosition + pointerLocal - pointerStartLocal);
        }

        private Vector2 ClampToParent(Vector2 candidate)
        {
            target.anchoredPosition = candidate;

            var parentRect = parent.rect;
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);

            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            for (var i = 0; i < corners.Length; i++)
            {
                var local = (Vector2)parent.InverseTransformPoint(corners[i]);
                min = Vector2.Min(min, local);
                max = Vector2.Max(max, local);
            }

            var adjustment = Vector2.zero;
            if (min.x < parentRect.xMin)
            {
                adjustment.x += parentRect.xMin - min.x;
            }
            else if (max.x > parentRect.xMax)
            {
                adjustment.x -= max.x - parentRect.xMax;
            }

            if (min.y < parentRect.yMin)
            {
                adjustment.y += parentRect.yMin - min.y;
            }
            else if (max.y > parentRect.yMax)
            {
                adjustment.y -= max.y - parentRect.yMax;
            }

            return candidate + adjustment;
        }
    }
}
