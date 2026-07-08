using System;
using UnityEngine;
using UnityEngine.UI;

namespace ElectricalSim.UI
{
    public sealed class SavedBlueprintListItem : MonoBehaviour
    {
        [SerializeField] private Text nameText;
        [SerializeField] private Text timeText;
        [SerializeField] private Text fileText;
        [SerializeField] private Button importButton;
        [SerializeField] private Button deleteButton;

        private SavedBlueprintInfo info;
        private Action<SavedBlueprintInfo> onImportClicked;
        private Action<SavedBlueprintInfo> onDeleteClicked;

        public static SavedBlueprintListItem Create(RectTransform parent)
        {
            var itemObject = new GameObject("SavedBlueprintListItem", typeof(RectTransform), typeof(Image), typeof(SavedBlueprintListItem));
            itemObject.transform.SetParent(parent, false);
            var rect = itemObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(720f, 100f);
            var img = itemObject.GetComponent<Image>();
            img.color = new Color(0.973f, 0.98f, 0.988f);
            img.sprite = UiThemeTokens.GetRoundedSprite(8);
            img.type = Image.Type.Sliced;

            var item = itemObject.GetComponent<SavedBlueprintListItem>();
            item.nameText = CreateText("NameText", rect, 18, TextAnchor.MiddleLeft, new Vector2(16f, -14f), new Vector2(400f, 28f));
            item.nameText.fontStyle = FontStyle.Bold;
            item.nameText.color = new Color(0.2f, 0.255f, 0.333f);
            
            item.timeText = CreateText("TimeText", rect, 14, TextAnchor.MiddleLeft, new Vector2(16f, -44f), new Vector2(400f, 24f));
            item.timeText.color = new Color(0.392f, 0.455f, 0.545f);
            
            item.fileText = CreateText("FileText", rect, 14, TextAnchor.MiddleLeft, new Vector2(16f, -70f), new Vector2(400f, 24f));
            item.fileText.color = new Color(0.392f, 0.455f, 0.545f);

            item.importButton = CreateButton(rect, "ImportButton", "导入", new Vector2(508f, -30f), new Vector2(90f, 40f), new Color(0.145f, 0.388f, 0.922f), Color.white);
            item.deleteButton = CreateButton(rect, "DeleteButton", "删除", new Vector2(614f, -30f), new Vector2(90f, 40f), new Color(0.996f, 0.886f, 0.886f), new Color(0.863f, 0.149f, 0.149f));
            item.importButton.onClick.AddListener(item.HandleImportClick);
            item.deleteButton.onClick.AddListener(item.HandleDeleteClick);
            return item;
        }

        public void Initialize(SavedBlueprintInfo blueprintInfo, Action<SavedBlueprintInfo> importHandler, Action<SavedBlueprintInfo> deleteHandler)
        {
            info = blueprintInfo;
            onImportClicked = importHandler;
            onDeleteClicked = deleteHandler;

            if (nameText != null)
            {
                nameText.text = blueprintInfo != null ? blueprintInfo.documentName : "未命名图纸";
            }

            if (timeText != null)
            {
                timeText.text = blueprintInfo != null ? "保存时间：" + blueprintInfo.savedAt : "保存时间：未知时间";
            }

            if (fileText != null)
            {
                fileText.text = blueprintInfo != null ? blueprintInfo.fileName : string.Empty;
            }
        }

        private void HandleImportClick()
        {
            onImportClicked?.Invoke(info);
        }

        private void HandleDeleteClick()
        {
            onDeleteClicked?.Invoke(info);
        }

        private static Text CreateText(string name, RectTransform parent, int fontSize, TextAnchor alignment, Vector2 position, Vector2 size)
        {
            var label = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            label.transform.SetParent(parent, false);
            label.rectTransform.anchorMin = new Vector2(0f, 1f);
            label.rectTransform.anchorMax = new Vector2(0f, 1f);
            label.rectTransform.pivot = new Vector2(0f, 1f);
            label.rectTransform.anchoredPosition = position;
            label.rectTransform.sizeDelta = size;
            label.font = MainUiTheme.BodyFont;
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = new Color(0.2f, 0.255f, 0.333f);
            label.raycastTarget = false;
            return label;
        }

        private static Button CreateButton(RectTransform parent, string name, string text, Vector2 position, Vector2 size, Color background, Color textColor)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var img = buttonObject.GetComponent<Image>();
            img.color = background;
            img.sprite = UiThemeTokens.GetRoundedSprite(8);
            img.type = Image.Type.Sliced;

            var label = CreateText("Text", rect, 14, TextAnchor.MiddleCenter, Vector2.zero, size);
            label.text = text;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;
            label.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            label.color = textColor;
            var nav = buttonObject.GetComponent<Button>().navigation;
            nav.mode = Navigation.Mode.None;
            buttonObject.GetComponent<Button>().navigation = nav;
            return buttonObject.GetComponent<Button>();
        }
    }
}
