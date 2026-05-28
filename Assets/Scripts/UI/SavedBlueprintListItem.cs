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
            rect.sizeDelta = new Vector2(560f, 90f);
            itemObject.GetComponent<Image>().color = new Color(0.96f, 0.98f, 1f, 1f);

            var item = itemObject.GetComponent<SavedBlueprintListItem>();
            item.nameText = CreateText("NameText", rect, 17, TextAnchor.MiddleLeft, new Vector2(16f, -10f), new Vector2(320f, 28f));
            item.timeText = CreateText("TimeText", rect, 13, TextAnchor.MiddleLeft, new Vector2(16f, -40f), new Vector2(280f, 22f));
            item.fileText = CreateText("FileText", rect, 12, TextAnchor.MiddleLeft, new Vector2(16f, -64f), new Vector2(330f, 20f));
            item.importButton = CreateButton(rect, "ImportButton", "导入", new Vector2(374f, -24f), new Vector2(76f, 36f), new Color(0.16f, 0.45f, 0.96f), Color.white);
            item.deleteButton = CreateButton(rect, "DeleteButton", "删除", new Vector2(462f, -24f), new Vector2(76f, 36f), new Color(1f, 0.94f, 0.94f), new Color(0.9f, 0.12f, 0.08f));
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
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = new Color(0.05f, 0.12f, 0.24f);
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
            buttonObject.GetComponent<Image>().color = background;

            var label = CreateText("Text", rect, 14, TextAnchor.MiddleCenter, Vector2.zero, size);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;
            label.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            label.color = textColor;
            return buttonObject.GetComponent<Button>();
        }
    }
}
