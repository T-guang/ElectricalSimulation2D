using UnityEngine;
using UnityEngine.EventSystems;

namespace ElectricalSim.Core
{
    public sealed class KTTimerHitArea : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private System.Action clickAction;

        public void Configure(System.Action action)
        {
            clickAction = action;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            clickAction?.Invoke();
            eventData.Use();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            eventData.Use();
        }

        public void OnDrag(PointerEventData eventData)
        {
            eventData.Use();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            eventData.Use();
        }
    }
}
