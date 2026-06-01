using System.Collections.Generic;
using ElectricalSim.Templates;
using UnityEngine;
using UnityEngine.UI;

namespace ElectricalSim.UI
{
    public sealed class TemplateSelectionPanel : MonoBehaviour
    {
        private const string FamilyCategory = "家庭电路";
        private const string IndustrialCategory = "工业电路";

        [SerializeField] private RectTransform content;
        [SerializeField] private Button closeButton;
        [SerializeField] private Text emptyText;
        [SerializeField] private Button familyButton;
        [SerializeField] private Button industrialButton;

        private readonly List<CircuitTemplateCatalogItemDto> allTemplates = new List<CircuitTemplateCatalogItemDto>();
        private System.Action<CircuitTemplateCatalogItemDto> onSelected;
        private string currentCategory = FamilyCategory;

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
            allTemplates.Clear();
            if (templates != null)
            {
                allTemplates.AddRange(templates);
            }

            currentCategory = FamilyCategory;
            gameObject.SetActive(true);
            RefreshTemplateListByCategory(currentCategory);
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

            familyButton = CreateButton(parent, FamilyCategory, new Color(0.12f, 0.45f, 1f), Color.white);
            var familyRect = familyButton.GetComponent<RectTransform>();
            familyRect.anchorMin = new Vector2(0f, 1f);
            familyRect.anchorMax = new Vector2(0f, 1f);
            familyRect.pivot = new Vector2(0f, 1f);
            familyRect.anchoredPosition = new Vector2(24f, -58f);
            familyRect.sizeDelta = new Vector2(112f, 34f);
            familyButton.onClick.AddListener(ShowFamilyTemplates);

            industrialButton = CreateButton(parent, IndustrialCategory, new Color(0.94f, 0.96f, 0.98f), new Color(0.26f, 0.34f, 0.45f));
            var industrialRect = industrialButton.GetComponent<RectTransform>();
            industrialRect.anchorMin = new Vector2(0f, 1f);
            industrialRect.anchorMax = new Vector2(0f, 1f);
            industrialRect.pivot = new Vector2(0f, 1f);
            industrialRect.anchoredPosition = new Vector2(148f, -58f);
            industrialRect.sizeDelta = new Vector2(112f, 34f);
            industrialButton.onClick.AddListener(ShowIndustrialTemplates);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask)).GetComponent<RectTransform>();
            viewport.SetParent(parent, false);
            viewport.anchorMin = new Vector2(0f, 0f);
            viewport.anchorMax = new Vector2(1f, 1f);
            viewport.offsetMin = new Vector2(24f, 24f);
            viewport.offsetMax = new Vector2(-24f, -110f);
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
            emptyText.rectTransform.offsetMax = new Vector2(-24f, -110f);
            emptyText.gameObject.SetActive(false);

            var scrollRect = parent.gameObject.AddComponent<ScrollRect>();
            scrollRect.viewport = viewport;
            scrollRect.content = content;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
        }

        private void ShowFamilyTemplates()
        {
            RefreshTemplateListByCategory(FamilyCategory);
        }

        private void ShowIndustrialTemplates()
        {
            RefreshTemplateListByCategory(IndustrialCategory);
        }

        private void RefreshTemplateListByCategory(string category)
        {
            currentCategory = category;
            UpdateCategoryButtonState();

            var filteredTemplates = new List<CircuitTemplateCatalogItemDto>();
            foreach (var template in allTemplates)
            {
                if (template != null && template.category == category)
                {
                    filteredTemplates.Add(template);
                }
            }

            RebuildList(filteredTemplates);
            if (emptyText != null && filteredTemplates.Count == 0)
            {
                emptyText.text = category == IndustrialCategory ? "暂无工业电路模板。" : "暂无该分类模板。";
            }
        }

        private void UpdateCategoryButtonState()
        {
            ApplyCategoryButtonStyle(familyButton, currentCategory == FamilyCategory);
            ApplyCategoryButtonStyle(industrialButton, currentCategory == IndustrialCategory);
        }

        private static void ApplyCategoryButtonStyle(Button button, bool selected)
        {
            if (button == null)
            {
                return;
            }

            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = selected ? new Color(0.12f, 0.45f, 1f) : new Color(0.94f, 0.96f, 0.98f);
            }

            var label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.color = selected ? Color.white : new Color(0.26f, 0.34f, 0.45f);
            }
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
