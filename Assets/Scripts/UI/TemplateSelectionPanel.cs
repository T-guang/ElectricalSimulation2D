using System.Collections.Generic;
using ElectricalSim.Templates;
using UnityEngine;
using UnityEngine.UI;

namespace ElectricalSim.UI
{
    public sealed class TemplateSelectionPanel : MonoBehaviour
    {
        [SerializeField] private RectTransform content;
        [SerializeField] private Button closeButton;
        [SerializeField] private Text emptyText;

        private System.Action<CircuitTemplateCatalogItemDto> onSelected;

        public static TemplateSelectionPanel Create(RectTransform parent, System.Action<CircuitTemplateCatalogItemDto> selectedCallback)
        {
            var panelObject = new GameObject("TemplateSelectionPanel", typeof(RectTransform), typeof(Image), typeof(TemplateSelectionPanel));
            panelObject.transform.SetParent(parent, false);

            var rect = panelObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(620f, 500f);

            var image = panelObject.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.97f);
            image.raycastTarget = true;

            var panel = panelObject.GetComponent<TemplateSelectionPanel>();
            panel.onSelected = selectedCallback;
            panel.Build(rect);
            panel.Hide();
            return panel;
        }

        public void Show(IReadOnlyList<CircuitTemplateCatalogItemDto> templates)
        {
            gameObject.SetActive(true);
            RebuildList(templates);
            transform.SetAsLastSibling();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void Build(RectTransform parent)
        {
            var title = CreateText("Title", parent, "加载标准图纸", 22, FontStyle.Bold, new Color(0.05f, 0.08f, 0.14f));
            title.rectTransform.anchorMin = new Vector2(0f, 1f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.anchoredPosition = new Vector2(24f, -18f);
            title.rectTransform.sizeDelta = new Vector2(-90f, 34f);

            closeButton = CreateButton(parent, "关闭", new Color(0.94f, 0.96f, 0.98f), new Color(0.1f, 0.14f, 0.22f));
            var closeRect = closeButton.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-18f, -16f);
            closeRect.sizeDelta = new Vector2(68f, 34f);
            closeButton.onClick.AddListener(Hide);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask)).GetComponent<RectTransform>();
            viewport.SetParent(parent, false);
            viewport.anchorMin = new Vector2(0f, 0f);
            viewport.anchorMax = new Vector2(1f, 1f);
            viewport.offsetMin = new Vector2(24f, 24f);
            viewport.offsetMax = new Vector2(-24f, -70f);
            viewport.GetComponent<Image>().color = new Color(0.98f, 0.99f, 1f, 1f);
            viewport.GetComponent<Mask>().showMaskGraphic = true;

            content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(viewport, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(-16f, 0f);

            emptyText = CreateText("EmptyText", parent, "暂无标准图纸。", 16, FontStyle.Normal, new Color(0.32f, 0.38f, 0.48f));
            emptyText.alignment = TextAnchor.MiddleCenter;
            emptyText.rectTransform.anchorMin = new Vector2(0f, 0f);
            emptyText.rectTransform.anchorMax = new Vector2(1f, 1f);
            emptyText.rectTransform.offsetMin = new Vector2(24f, 24f);
            emptyText.rectTransform.offsetMax = new Vector2(-24f, -70f);
            emptyText.gameObject.SetActive(false);

            var scrollRect = parent.gameObject.AddComponent<ScrollRect>();
            scrollRect.viewport = viewport;
            scrollRect.content = content;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
        }

        private void RebuildList(IReadOnlyList<CircuitTemplateCatalogItemDto> templates)
        {
            for (var i = content.childCount - 1; i >= 0; i--)
            {
                Destroy(content.GetChild(i).gameObject);
            }

            var hasTemplates = templates != null && templates.Count > 0;
            if (emptyText != null)
            {
                emptyText.gameObject.SetActive(!hasTemplates);
            }

            var y = -8f;
            if (templates != null)
            {
                foreach (var template in templates)
                {
                    var item = TemplateListItem.Create(content);
                    var rect = item.GetComponent<RectTransform>();
                    rect.anchoredPosition = new Vector2(0f, y);
                    item.Initialize(template, HandleSelected);
                    y -= 94f;
                }
            }

            content.sizeDelta = new Vector2(-16f, Mathf.Max(0f, -y + 8f));
        }

        private void HandleSelected(CircuitTemplateCatalogItemDto item)
        {
            Hide();
            onSelected?.Invoke(item);
        }

        private static Text CreateText(string name, Transform parent, string text, int size, FontStyle style, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var label = go.GetComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = size;
            label.fontStyle = style;
            label.alignment = TextAnchor.UpperLeft;
            label.color = color;
            label.raycastTarget = false;
            return label;
        }

        private static Button CreateButton(Transform parent, string text, Color color, Color textColor)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            var label = CreateText("Text", go.transform, text, 15, FontStyle.Normal, textColor);
            label.alignment = TextAnchor.MiddleCenter;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;
            return go.GetComponent<Button>();
        }
    }
}
