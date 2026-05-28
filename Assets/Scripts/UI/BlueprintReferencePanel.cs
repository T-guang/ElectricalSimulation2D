using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ElectricalSim.UI
{
    public sealed class BlueprintReferencePanel : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        [SerializeField] private RectTransform panelRect;
        [SerializeField] private Button zoomInButton;
        [SerializeField] private Button zoomOutButton;
        [SerializeField] private Button resetButton;

        private Vector2 dragStartPointer;
        private Vector2 dragStartPosition;
        private Vector2 baseSize;
        private float zoom = 1f;

        private void Awake()
        {
            if (panelRect == null)
            {
                panelRect = GetComponent<RectTransform>();
            }

            baseSize = panelRect != null ? panelRect.sizeDelta : new Vector2(560f, 560f);
            zoomInButton?.onClick.AddListener(() => SetZoom(zoom + 0.15f));
            zoomOutButton?.onClick.AddListener(() => SetZoom(zoom - 0.15f));
            resetButton?.onClick.AddListener(ResetView);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (panelRect == null)
            {
                return;
            }

            dragStartPointer = eventData.position;
            dragStartPosition = panelRect.anchoredPosition;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (panelRect == null)
            {
                return;
            }

            var canvas = panelRect.GetComponentInParent<Canvas>();
            var scale = canvas != null ? canvas.scaleFactor : 1f;
            panelRect.anchoredPosition = dragStartPosition + (eventData.position - dragStartPointer) / Mathf.Max(0.01f, scale);
        }

        private void SetZoom(float value)
        {
            zoom = Mathf.Clamp(value, 0.65f, 1.65f);
            if (panelRect != null)
            {
                panelRect.sizeDelta = baseSize * zoom;
            }
        }

        private void ResetView()
        {
            zoom = 1f;
            if (panelRect != null)
            {
                panelRect.sizeDelta = baseSize;
                panelRect.anchoredPosition = new Vector2(-22f, -40f);
            }
        }
    }
}
