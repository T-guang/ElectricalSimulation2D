using UnityEngine;
using UnityEngine.UI;

namespace ElectricalSim.Core
{
    public sealed class KTDelaySettingDialog : MonoBehaviour
    {
        private const string DelayParameterKey = "delaySeconds";

        private CircuitComponent component;
        private WorkspaceController workspace;
        private System.Action<float> applyDelay;
        private Text currentText;
        private Text errorText;
        private InputField inputField;

        public static void Show(CircuitComponent target, WorkspaceController owner, System.Action<float> applyCallback)
        {
            if (target == null)
            {
                return;
            }

            var existing = FindObjectOfType<KTDelaySettingDialog>();
            if (existing != null)
            {
                existing.Initialize(target, owner, applyCallback);
                existing.transform.SetAsLastSibling();
                return;
            }

            var canvas = target.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = FindObjectOfType<Canvas>();
            }

            if (canvas == null)
            {
                return;
            }

            var root = new GameObject("KTDelaySettingDialog", typeof(RectTransform), typeof(Image), typeof(KTDelaySettingDialog));
            root.transform.SetParent(canvas.transform, false);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var backdrop = root.GetComponent<Image>();
            backdrop.color = new Color(0f, 0f, 0f, 0.22f);
            backdrop.raycastTarget = true;

            var dialog = root.GetComponent<KTDelaySettingDialog>();
            dialog.BuildContent(rect);
            dialog.Initialize(target, owner, applyCallback);
        }

        private void Initialize(CircuitComponent target, WorkspaceController owner, System.Action<float> applyCallback)
        {
            component = target;
            workspace = owner;
            applyDelay = applyCallback;
            gameObject.SetActive(true);
            RefreshCurrentText();
        }

        private void BuildContent(RectTransform root)
        {
            var panel = CreateRect("Panel", root, new Vector2(0.5f, 0.5f), new Vector2(420f, 300f));
            var panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.color = new Color(0.98f, 0.99f, 1f, 1f);
            panelImage.raycastTarget = true;

            CreateText("Title", panel, "设置延时时间", 20, FontStyle.Bold, TextAnchor.MiddleLeft, new Vector2(24f, -28f), new Vector2(260f, 32f));
            var close = CreateButton(panel, "CloseButton", "X", new Vector2(372f, -28f), new Vector2(36f, 32f), new Color(0.94f, 0.96f, 0.98f), new Color(0.15f, 0.18f, 0.24f));
            close.onClick.AddListener(Close);

            currentText = CreateText("CurrentText", panel, string.Empty, 15, FontStyle.Normal, TextAnchor.MiddleLeft, new Vector2(24f, -70f), new Vector2(320f, 28f));

            CreateQuickButton(panel, "Quick1", "1秒", 1f, new Vector2(52f, -116f));
            CreateQuickButton(panel, "Quick3", "3秒", 3f, new Vector2(143f, -116f));
            CreateQuickButton(panel, "Quick5", "5秒", 5f, new Vector2(234f, -116f));
            CreateQuickButton(panel, "Quick10", "10秒", 10f, new Vector2(325f, -116f));

            CreateText("InputLabel", panel, "自定义时间", 14, FontStyle.Normal, TextAnchor.MiddleLeft, new Vector2(24f, -166f), new Vector2(100f, 28f));
            inputField = CreateInput(panel, new Vector2(190f, -166f), new Vector2(180f, 32f));

            errorText = CreateText("ErrorText", panel, string.Empty, 13, FontStyle.Normal, TextAnchor.MiddleLeft, new Vector2(24f, -205f), new Vector2(330f, 26f));
            errorText.color = new Color(0.85f, 0.12f, 0.08f);

            var apply = CreateButton(panel, "ApplyButton", "设置", new Vector2(242f, -256f), new Vector2(84f, 36f), new Color(0.12f, 0.42f, 0.95f), Color.white);
            apply.onClick.AddListener(ApplyCustomInput);
            var cancel = CreateButton(panel, "CancelButton", "关闭", new Vector2(340f, -256f), new Vector2(84f, 36f), new Color(0.94f, 0.96f, 0.98f), new Color(0.1f, 0.14f, 0.22f));
            cancel.onClick.AddListener(Close);
        }

        private void CreateQuickButton(RectTransform parent, string name, string label, float value, Vector2 position)
        {
            var button = CreateButton(parent, name, label, position, new Vector2(76f, 36f), new Color(0.88f, 0.93f, 1f), new Color(0.06f, 0.24f, 0.52f));
            button.onClick.AddListener(() =>
            {
                ApplyDelay(value);
                RefreshCurrentText();
            });
        }

        private void ApplyCustomInput()
        {
            if (inputField == null || !KTTimerVisualController.TryParseDelaySeconds(inputField.text, out var seconds))
            {
                if (errorText != null)
                {
                    errorText.text = "请输入 3、3秒、00:03 或 0:03 这样的格式。";
                }
                return;
            }

            ApplyDelay(seconds);
            Close();
        }

        private void ApplyDelay(float value)
        {
            applyDelay?.Invoke(value);
            workspace?.SetStatus("时间继电器延时时间已设置为 " + KTTimerVisualController.FormatSeconds(Mathf.RoundToInt(Mathf.Clamp(value, 0f, 10f))) + "。");
        }

        private void RefreshCurrentText()
        {
            var value = 3f;
            var parameter = component != null ? component.GetParameter(DelayParameterKey) : null;
            if (parameter != null)
            {
                value = Mathf.Clamp(parameter.value, 0f, 10f);
            }

            var seconds = Mathf.RoundToInt(value);
            if (currentText != null)
            {
                currentText.text = "当前设置：" + KTTimerVisualController.FormatSeconds(seconds) + "    总计 " + seconds + " 秒";
            }

            if (inputField != null)
            {
                inputField.text = KTTimerVisualController.FormatSeconds(seconds);
            }

            if (errorText != null)
            {
                errorText.text = string.Empty;
            }
        }

        private void Close()
        {
            Destroy(gameObject);
        }

        private static RectTransform CreateRect(string name, RectTransform parent, Vector2 anchor, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;
            return rect;
        }

        private static Text CreateText(string name, RectTransform parent, string text, int fontSize, FontStyle style, TextAnchor alignment, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var label = go.GetComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.alignment = alignment;
            label.color = new Color(0.08f, 0.1f, 0.16f);
            label.raycastTarget = false;
            return label;
        }

        private static InputField CreateInput(RectTransform parent, Vector2 position, Vector2 size)
        {
            var go = new GameObject("DelayInput", typeof(RectTransform), typeof(Image), typeof(InputField));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var image = go.GetComponent<Image>();
            image.color = Color.white;

            var text = CreateText("Text", rect, string.Empty, 15, FontStyle.Normal, TextAnchor.MiddleLeft, new Vector2(8f, 0f), new Vector2(size.x - 16f, size.y));
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            text.rectTransform.offsetMin = new Vector2(8f, 0f);
            text.rectTransform.offsetMax = new Vector2(-8f, 0f);

            var input = go.GetComponent<InputField>();
            input.textComponent = text;
            input.targetGraphic = image;
            input.contentType = InputField.ContentType.Standard;
            input.lineType = InputField.LineType.SingleLine;
            return input;
        }

        private static Button CreateButton(RectTransform parent, string name, string text, Vector2 position, Vector2 size, Color background, Color textColor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var image = go.GetComponent<Image>();
            image.color = background;

            var label = CreateText("Text", rect, text, 15, FontStyle.Bold, TextAnchor.MiddleCenter, Vector2.zero, size);
            label.color = textColor;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            return button;
        }
    }
}
