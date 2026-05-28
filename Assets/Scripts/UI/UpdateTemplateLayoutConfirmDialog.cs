using System;
using UnityEngine;
using UnityEngine.UI;

namespace ElectricalSim.UI
{
    public sealed class UpdateTemplateLayoutConfirmDialog : MonoBehaviour
    {
        private Action onConfirm;

        public static void Show(string templateName, Action onConfirmCallback)
        {
            var go = new GameObject("UpdateTemplateConfirmDialog", typeof(RectTransform), typeof(Image), typeof(UpdateTemplateLayoutConfirmDialog));
            var canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                go.transform.SetParent(canvas.transform, false);
            }

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var bg = go.GetComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.6f);
            bg.raycastTarget = true;

            var dialog = go.GetComponent<UpdateTemplateLayoutConfirmDialog>();
            dialog.onConfirm = onConfirmCallback;
            dialog.BuildUI(templateName);
        }

        private void BuildUI(string templateName)
        {
            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(transform, false);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(400f, 260f);
            panelRect.anchoredPosition = Vector2.zero;
            panel.GetComponent<Image>().color = Color.white;

            var title = CreateText(panel.transform, "更新模板布局确认", 18, FontStyle.Bold, TextAnchor.MiddleCenter);
            title.rectTransform.anchorMin = new Vector2(0f, 1f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.anchoredPosition = new Vector2(0f, -30f);

            var message = CreateText(
                panel.transform,
                $"是否将当前画布布局保存回系统模板“{templateName}”？\n\n此操作只会覆盖该模板的元件位置和导线路径。\n系统会自动备份原模板 JSON。\n元件参数不会被覆盖。",
                14,
                FontStyle.Normal,
                TextAnchor.UpperLeft);
            message.rectTransform.anchorMin = new Vector2(0f, 0f);
            message.rectTransform.anchorMax = new Vector2(1f, 1f);
            message.rectTransform.offsetMin = new Vector2(24f, 80f);
            message.rectTransform.offsetMax = new Vector2(-24f, -70f);
            message.color = new Color(0.2f, 0.2f, 0.2f);

            var cancelButton = CreateButton(panel.transform, "取消", new Color(0.8f, 0.8f, 0.8f), new Color(0.2f, 0.2f, 0.2f));
            var cancelRect = cancelButton.GetComponent<RectTransform>();
            cancelRect.anchorMin = new Vector2(0.5f, 0f);
            cancelRect.anchorMax = new Vector2(0.5f, 0f);
            cancelRect.anchoredPosition = new Vector2(-100f, 40f);
            cancelRect.sizeDelta = new Vector2(140f, 40f);
            cancelButton.onClick.AddListener(() => Destroy(gameObject));

            var confirmButton = CreateButton(panel.transform, "确认更新", new Color(0.85f, 0.2f, 0.2f), Color.white);
            var confirmRect = confirmButton.GetComponent<RectTransform>();
            confirmRect.anchorMin = new Vector2(0.5f, 0f);
            confirmRect.anchorMax = new Vector2(0.5f, 0f);
            confirmRect.anchoredPosition = new Vector2(100f, 40f);
            confirmRect.sizeDelta = new Vector2(140f, 40f);
            confirmButton.onClick.AddListener(() =>
            {
                Debug.Log("[UpdateTemplateLayoutConfirmDialog] 确认更新按钮被点击。");
                onConfirm?.Invoke();
                Destroy(gameObject);
            });
        }

        private Text CreateText(Transform parent, string content, int size, FontStyle style, TextAnchor align)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.text = content;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = align;
            text.color = new Color(0.1f, 0.1f, 0.1f);
            return text;
        }

        private Button CreateButton(Transform parent, string label, Color bgColor, Color textColor)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = bgColor;

            var text = CreateText(go.transform, label, 16, FontStyle.Bold, TextAnchor.MiddleCenter);
            text.color = textColor;
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            return button;
        }
    }
}
