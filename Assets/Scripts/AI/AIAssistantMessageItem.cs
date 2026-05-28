using UnityEngine;
using UnityEngine.UI;

namespace ElectricalSim.AI
{
    public sealed class AIAssistantMessageItem : MonoBehaviour
    {
        [SerializeField] private Text messageText;
        [SerializeField] private Image background;
        [SerializeField] private LayoutElement layoutElement;

        public static AIAssistantMessageItem Create(Transform parent)
        {
            var go = new GameObject("AIAssistantMessageItem", typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(AIAssistantMessageItem));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);

            var item = go.GetComponent<AIAssistantMessageItem>();
            item.background = go.GetComponent<Image>();
            item.layoutElement = go.GetComponent<LayoutElement>();
            item.layoutElement.minHeight = 56f;
            item.layoutElement.flexibleWidth = 1f;

            var layout = go.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 8, 8);
            layout.spacing = 0f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = go.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var textGo = new GameObject("MessageText", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            textGo.transform.SetParent(go.transform, false);
            item.messageText = textGo.GetComponent<Text>();
            item.messageText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            item.messageText.fontSize = 14;
            item.messageText.alignment = TextAnchor.UpperLeft;
            item.messageText.horizontalOverflow = HorizontalWrapMode.Wrap;
            item.messageText.verticalOverflow = VerticalWrapMode.Overflow;
            item.messageText.color = new Color(0.06f, 0.08f, 0.12f);
            item.messageText.raycastTarget = false;

            var textLayout = textGo.GetComponent<LayoutElement>();
            textLayout.flexibleWidth = 1f;
            return item;
        }

        public void SetMessage(string sender, string message, bool fromUser)
        {
            if (background != null)
            {
                background.color = fromUser ? new Color(0.86f, 0.93f, 1f, 1f) : new Color(0.96f, 0.98f, 0.96f, 1f);
                background.raycastTarget = true;
            }

            if (messageText != null)
            {
                messageText.text = sender + "：\n" + message;
            }

            if (layoutElement != null)
            {
                layoutElement.minHeight = 56f;
                layoutElement.preferredHeight = -1f;
            }
        }
    }
}
