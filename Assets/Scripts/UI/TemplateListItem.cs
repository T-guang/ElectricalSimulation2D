using ElectricalSim.Templates;
using UnityEngine;
using UnityEngine.UI;

namespace ElectricalSim.UI
{
    public sealed class TemplateListItem : MonoBehaviour
    {
        [SerializeField] private Text titleText;
        [SerializeField] private Text metaText;
        [SerializeField] private Text descriptionText;
        [SerializeField] private Button button;

        private CircuitTemplateCatalogItemDto item;
        private System.Action<CircuitTemplateCatalogItemDto> onSelected;

        public static TemplateListItem Create(RectTransform parent)
        {
            var itemObject = new GameObject("TemplateListItem", typeof(RectTransform), typeof(Image), typeof(Button), typeof(TemplateListItem));
            itemObject.transform.SetParent(parent, false);

            var rect = itemObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, 86f);

            var image = itemObject.GetComponent<Image>();
            image.color = new Color(0.96f, 0.98f, 1f, 1f);

            var view = itemObject.GetComponent<TemplateListItem>();
            view.button = itemObject.GetComponent<Button>();
            view.titleText = CreateText("Title", rect, 17, FontStyle.Bold, new Color(0.05f, 0.08f, 0.14f));
            view.titleText.rectTransform.anchorMin = new Vector2(0f, 1f);
            view.titleText.rectTransform.anchorMax = new Vector2(1f, 1f);
            view.titleText.rectTransform.anchoredPosition = new Vector2(12f, -8f);
            view.titleText.rectTransform.sizeDelta = new Vector2(-24f, 24f);

            view.metaText = CreateText("Meta", rect, 14, FontStyle.Normal, new Color(0.12f, 0.32f, 0.62f));
            view.metaText.rectTransform.anchorMin = new Vector2(0f, 1f);
            view.metaText.rectTransform.anchorMax = new Vector2(1f, 1f);
            view.metaText.rectTransform.anchoredPosition = new Vector2(12f, -34f);
            view.metaText.rectTransform.sizeDelta = new Vector2(-24f, 20f);

            view.descriptionText = CreateText("Description", rect, 14, FontStyle.Normal, new Color(0.26f, 0.32f, 0.42f));
            view.descriptionText.rectTransform.anchorMin = new Vector2(0f, 0f);
            view.descriptionText.rectTransform.anchorMax = new Vector2(1f, 1f);
            view.descriptionText.rectTransform.offsetMin = new Vector2(12f, 8f);
            view.descriptionText.rectTransform.offsetMax = new Vector2(-12f, -56f);

            view.button.onClick.AddListener(view.HandleClick);
            return view;
        }

        public void Initialize(CircuitTemplateCatalogItemDto templateItem, System.Action<CircuitTemplateCatalogItemDto> selectedCallback)
        {
            item = templateItem;
            onSelected = selectedCallback;

            titleText.text = string.IsNullOrWhiteSpace(item.templateName) ? item.templateId : item.templateName;
            metaText.text = item.category + " / " + item.difficulty;
            descriptionText.text = item.description;
        }

        private void HandleClick()
        {
            onSelected?.Invoke(item);
        }

        private static Text CreateText(string name, Transform parent, int size, FontStyle style, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }
    }
}
