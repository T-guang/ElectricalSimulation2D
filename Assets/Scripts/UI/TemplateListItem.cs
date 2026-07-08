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
            rect.sizeDelta = new Vector2(0f, 88f);

            var image = itemObject.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.01f);

            var view = itemObject.GetComponent<TemplateListItem>();
            view.button = itemObject.GetComponent<Button>();
            var nav = view.button.navigation;
            nav.mode = Navigation.Mode.None;
            view.button.navigation = nav;

            view.titleText = CreateText("Title", rect, 18, FontStyle.Bold, new Color(0.2f, 0.255f, 0.333f));
            view.titleText.rectTransform.anchorMin = new Vector2(0f, 1f);
            view.titleText.rectTransform.anchorMax = new Vector2(1f, 1f);
            view.titleText.rectTransform.pivot = new Vector2(0.5f, 1f);
            view.titleText.rectTransform.offsetMin = new Vector2(16f, -28f);
            view.titleText.rectTransform.offsetMax = new Vector2(-16f, -4f);

            view.metaText = CreateText("Meta", rect, 15, FontStyle.Bold, new Color(0.145f, 0.388f, 0.922f));
            view.metaText.rectTransform.anchorMin = new Vector2(0f, 1f);
            view.metaText.rectTransform.anchorMax = new Vector2(1f, 1f);
            view.metaText.rectTransform.pivot = new Vector2(0.5f, 1f);
            view.metaText.rectTransform.offsetMin = new Vector2(16f, -50f);
            view.metaText.rectTransform.offsetMax = new Vector2(-16f, -30f);

            view.descriptionText = CreateText("Description", rect, 15, FontStyle.Normal, new Color(0.392f, 0.455f, 0.545f));
            view.descriptionText.rectTransform.anchorMin = new Vector2(0f, 0f);
            view.descriptionText.rectTransform.anchorMax = new Vector2(1f, 1f);
            view.descriptionText.rectTransform.offsetMin = new Vector2(16f, 4f);
            view.descriptionText.rectTransform.offsetMax = new Vector2(-16f, -56f);

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
            text.font = MainUiTheme.BodyFont;
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
