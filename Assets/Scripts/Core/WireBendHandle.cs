using UnityEngine;
using UnityEngine.EventSystems;

namespace ElectricalSim.Core
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class WireBendHandle : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private WireView owner;
        private int segmentIndex;

        public RectTransform RectTransform { get; private set; }

        public void Initialize(WireView wire, int editableSegmentIndex)
        {
            owner = wire;
            segmentIndex = editableSegmentIndex;
            RectTransform = GetComponent<RectTransform>();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            owner?.SelectFromBendHandle();
            eventData.Use();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            owner?.BeginBendDrag(segmentIndex, eventData);
            eventData.Use();
        }

        public void OnDrag(PointerEventData eventData)
        {
            owner?.DragActiveSegment(eventData);
            eventData.Use();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            owner?.EndBendDrag(eventData);
            eventData.Use();
        }
    }
}
