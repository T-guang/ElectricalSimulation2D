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

            FixLayout();
        }

        private void FixLayout()
        {
            var titleText = GetComponentInChildren<Text>();
            if (titleText != null)
            {
                var rt = titleText.rectTransform;
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.offsetMin = new Vector2(10f, -40f);
                rt.offsetMax = new Vector2(-160f, 0f);
                titleText.alignment = TextAnchor.MiddleLeft;
                titleText.horizontalOverflow = HorizontalWrapMode.Wrap;
            }

            var buttons = GetComponentsInChildren<Button>();
            float currentX = -5f;
            for (int i = buttons.Length - 1; i >= 0; i--)
            {
                var btn = buttons[i];
                var brt = btn.GetComponent<RectTransform>();
                if (brt.anchorMin.y > 0.5f)
                {
                    brt.anchorMin = new Vector2(1f, 1f);
                    brt.anchorMax = new Vector2(1f, 1f);
                    brt.pivot = new Vector2(1f, 1f);
                    brt.anchoredPosition = new Vector2(currentX, -5f);
                    currentX -= (brt.sizeDelta.x + 5f);
                }
            }
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
