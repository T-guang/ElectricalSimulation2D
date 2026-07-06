using UnityEngine;
using UnityEngine.UI;

namespace ElectricalSim.UI
{
    public static class UiIconLibrary
    {
        public static Sprite Load(string relativePath)
        {
            return string.IsNullOrWhiteSpace(relativePath) ? null : Resources.Load<Sprite>("UI/Icons/" + relativePath);
        }

        public static Image EnsureButtonIcon(Button button, string relativePath, Vector2 size, Vector2 anchoredPosition, Color? color = null)
        {
            if (button == null)
            {
                return null;
            }

            var sprite = Load(relativePath);
            if (sprite == null)
            {
                return null;
            }

            var rect = button.transform.Find("Icon") as RectTransform;
            if (rect == null)
            {
                var iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconObject.transform.SetParent(button.transform, false);
                rect = iconObject.GetComponent<RectTransform>();
            }

            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            var image = rect.GetComponent<Image>() ?? rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = color ?? Color.white;
            rect.SetAsFirstSibling();
            return image;
        }

        public static Image EnsureCenteredIcon(Transform parent, string relativePath, Vector2 size, Color? color = null)
        {
            if (parent == null)
            {
                return null;
            }

            var sprite = Load(relativePath);
            if (sprite == null)
            {
                return null;
            }

            var rect = parent.Find("Icon") as RectTransform;
            if (rect == null)
            {
                var iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconObject.transform.SetParent(parent, false);
                rect = iconObject.GetComponent<RectTransform>();
            }

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;

            var image = rect.GetComponent<Image>() ?? rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = color ?? Color.white;
            rect.SetAsLastSibling();
            return image;
        }
    }
}
