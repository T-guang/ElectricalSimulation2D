using System.Collections.Generic;
using ElectricalSim.UI.CommonTools;
using UnityEngine;
using UnityEngine.UI;

namespace ElectricalSim.UI
{
    public sealed class TopNavigationController : MonoBehaviour
    {
        [SerializeField] private PageRouter pageRouter;
        [SerializeField] private List<Button> tabButtons = new List<Button>();
        [SerializeField] private List<Text> tabLabels = new List<Text>();
        [SerializeField] private GameObject simulationRoot;
        [SerializeField] private GameObject blueprintRoot;
        [SerializeField] private GameObject encyclopediaRoot;
        [SerializeField] private GameObject toolsRoot;
        [SerializeField] private GameObject emptyPageRoot;
        [SerializeField] private Text emptyPageTitle;

        private void Awake()
        {
            if (pageRouter == null)
            {
                pageRouter = FindObjectOfType<PageRouter>();
            }

            if (pageRouter == null)
            {
                pageRouter = gameObject.AddComponent<PageRouter>();
            }

            pageRouter.Configure(
                simulationRoot,
                blueprintRoot,
                null,
                encyclopediaRoot,
                toolsRoot,
                null,
                emptyPageRoot,
                emptyPageTitle);

            if (toolsRoot != null && toolsRoot.GetComponent<CommonToolsPageController>() == null)
            {
                toolsRoot.AddComponent<CommonToolsPageController>();
            }

            if (simulationRoot != null)
            {
                var bg = simulationRoot.GetComponent<Image>();
                if (bg != null) bg.color = UiThemeTokens.Background;
            }

            for (var i = 0; i < tabButtons.Count; i++)
            {
                var index = i;
                tabButtons[i].onClick.AddListener(() => SelectTab(index));
            }

            ApplyThemeToNavBar();
            SelectTab(0);
        }

        private void ApplyThemeToNavBar()
        {
            var navRect = GetComponent<RectTransform>();
            if (navRect != null)
            {
                navRect.sizeDelta = new Vector2(navRect.sizeDelta.x, 60f);
                var bg = navRect.GetComponent<Image>();
                if (bg != null) bg.color = Color.white;
                
                if (navRect.Find("BottomBorder") == null)
                {
                    var border = new GameObject("BottomBorder", typeof(RectTransform), typeof(Image));
                    border.transform.SetParent(navRect, false);
                    var borderRt = border.GetComponent<RectTransform>();
                    borderRt.anchorMin = new Vector2(0, 0);
                    borderRt.anchorMax = new Vector2(1, 0);
                    borderRt.pivot = new Vector2(0.5f, 0f);
                    borderRt.sizeDelta = new Vector2(0, 1f);
                    borderRt.anchoredPosition = Vector2.zero;
                    border.GetComponent<Image>().color = UiThemeTokens.BorderColor;
                }
            }

            ApplyBrandGroupLayout();

            for (int i = 0; i < tabButtons.Count; i++)
            {
                var btnRect = tabButtons[i].GetComponent<RectTransform>();
                if (btnRect != null)
                {
                    btnRect.sizeDelta = new Vector2(btnRect.sizeDelta.x, 36f);
                    var img = tabButtons[i].GetComponent<Image>();
                    if (img != null)
                    {
                        img.sprite = UiThemeTokens.GetRoundedSprite(8);
                        img.type = Image.Type.Sliced;
                    }
                }
                if (i < tabLabels.Count && tabLabels[i] != null)
                {
                    tabLabels[i].fontSize = 18;
                    tabLabels[i].resizeTextForBestFit = false;
                }
            }
        }

        private void ApplyBrandGroupLayout()
        {
            var titleTransform = transform.Find("Logo") ?? transform.Find("Title");
            var brandRect = titleTransform as RectTransform;
            if (brandRect == null)
            {
                return;
            }

            brandRect.anchorMin = new Vector2(0f, 0.5f);
            brandRect.anchorMax = new Vector2(0f, 0.5f);
            brandRect.pivot = new Vector2(0f, 0.5f);
            brandRect.anchoredPosition = new Vector2(24f, 2f);
            brandRect.sizeDelta = new Vector2(390f, 44f);

            var layout = titleTransform.GetComponent<HorizontalLayoutGroup>() ?? titleTransform.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;
            layout.childControlHeight = false;
            layout.childControlWidth = false;

            var iconWrapper = titleTransform.Find("IconWrapper") as RectTransform;
            if (iconWrapper == null)
            {
                var wrapperObj = new GameObject("IconWrapper", typeof(RectTransform), typeof(LayoutElement));
                wrapperObj.transform.SetParent(titleTransform, false);
                wrapperObj.transform.SetAsFirstSibling();
                iconWrapper = wrapperObj.GetComponent<RectTransform>();
            }

            iconWrapper.sizeDelta = new Vector2(36f, 36f);
            var wrapperLayout = iconWrapper.GetComponent<LayoutElement>() ?? iconWrapper.gameObject.AddComponent<LayoutElement>();
            wrapperLayout.preferredWidth = 36f;
            wrapperLayout.preferredHeight = 36f;
            wrapperLayout.flexibleWidth = 0f;
            wrapperLayout.flexibleHeight = 0f;

            var icon = iconWrapper.Find("Icon") as RectTransform;
            if (icon == null)
            {
                var iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconObj.transform.SetParent(iconWrapper, false);
                icon = iconObj.GetComponent<RectTransform>();
            }

            icon.anchorMin = new Vector2(0.5f, 0.5f);
            icon.anchorMax = new Vector2(0.5f, 0.5f);
            icon.pivot = new Vector2(0.5f, 0.5f);
            icon.anchoredPosition = Vector2.zero;
            icon.sizeDelta = new Vector2(36f, 36f);

            var iconImage = icon.GetComponent<Image>() ?? icon.gameObject.AddComponent<Image>();
            iconImage.sprite = UiIconLibrary.Load("Logo/ui_logo_main_320");
            iconImage.preserveAspect = true;
            iconImage.color = Color.white;
            iconImage.raycastTarget = false;

            var titleText = titleTransform.Find("Text")?.GetComponent<Text>();
            if (titleText == null)
            {
                titleText = titleTransform.GetComponentInChildren<Text>(true);
            }

            if (titleText != null)
            {
                titleText.transform.SetAsLastSibling();
                titleText.color = UiThemeTokens.TextDark;
                titleText.font = MainUiTheme.TitleFont;
                titleText.fontSize = 20;
                titleText.fontStyle = FontStyle.Bold;
                titleText.alignment = TextAnchor.MiddleLeft;
                titleText.resizeTextForBestFit = false;
                titleText.verticalOverflow = VerticalWrapMode.Overflow;

                var textLayout = titleText.GetComponent<LayoutElement>() ?? titleText.gameObject.AddComponent<LayoutElement>();
                textLayout.preferredWidth = 300f;
                textLayout.preferredHeight = 38f;
            }
        }

        public void SelectTab(int index)
        {
            var page = ToPageId(index);
            if (pageRouter != null)
            {
                pageRouter.ShowPage(page);
            }

            RefreshTabStates(index);
        }

        private void RefreshTabStates(int activeIndex)
        {
            for (var i = 0; i < tabButtons.Count; i++)
            {
                var active = i == activeIndex;
                var image = tabButtons[i].GetComponent<Image>();
                if (image != null)
                {
                    image.color = active ? UiThemeTokens.PrimaryLight : Color.white;
                }

                if (i < tabLabels.Count && tabLabels[i] != null)
                {
                    tabLabels[i].color = active ? UiThemeTokens.PrimaryBlue : UiThemeTokens.TextMuted;
                    tabLabels[i].fontStyle = active ? FontStyle.Bold : FontStyle.Normal;
                }
            }
        }

        private static PageId ToPageId(int index)
        {
            switch (index)
            {
                case 0:
                    return PageId.Simulation;
                case 1:
                    return PageId.Blueprint;
                case 2:
                    return PageId.Square;
                case 3:
                    return PageId.Encyclopedia;
                case 4:
                    return PageId.Tools;
                case 5:
                    return PageId.Profile;
                default:
                    return PageId.Simulation;
            }
        }
    }

    /// <summary>
    /// 全局 UI 视觉主题规范 (V1.6 升级版)
    /// </summary>
    public static class UiThemeTokens
    {
        public static readonly Color Background = ParseColor("#F7F9FC", new Color(0.97f, 0.98f, 1f));
        public static readonly Color CardBackground = Color.white;
        
        public static readonly Color PrimaryBlue = ParseColor("#3B82F6", new Color(0.23f, 0.51f, 0.96f));
        public static readonly Color PrimaryHover = ParseColor("#2563EB", new Color(0.15f, 0.39f, 0.92f));
        public static readonly Color PrimaryLight = ParseColor("#EFF6FF", new Color(0.94f, 0.96f, 1f));
        
        public static readonly Color TextDark = ParseColor("#1E293B", new Color(0.12f, 0.16f, 0.23f));
        public static readonly Color TextMuted = ParseColor("#64748B", new Color(0.39f, 0.45f, 0.55f));
        
        public static readonly Color BorderColor = ParseColor("#E2E8F0", new Color(0.89f, 0.91f, 0.94f));
        public static readonly Color ErrorColor = ParseColor("#EF4444", new Color(0.94f, 0.27f, 0.27f));

        private static Color ParseColor(string hex, Color fallback)
        {
            return ColorUtility.TryParseHtmlString(hex, out var c) ? c : fallback;
        }

        private static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

        /// <summary>
        /// 获取或生成带有边缘抗锯齿的圆角 Sprite，带缓存机制以防内存泄漏
        /// </summary>
        public static Sprite GetRoundedSprite(int radius = 12, int size = 64)
        {
            string key = $"{radius}_{size}";
            if (spriteCache.TryGetValue(key, out var cachedSprite) && cachedSprite != null)
            {
                return cachedSprite;
            }

            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(0, Mathf.Abs(x - size / 2f) - (size / 2f - radius));
                    float dy = Mathf.Max(0, Mathf.Abs(y - size / 2f) - (size / 2f - radius));
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    float alpha = Mathf.Clamp01(radius - dist);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            int border = radius + 2;
            Sprite sprite = Sprite.Create(
                tex, 
                new Rect(0, 0, size, size), 
                new Vector2(0.5f, 0.5f), 
                100, 
                0, 
                SpriteMeshType.FullRect, 
                new Vector4(border, border, border, border)
            );

            spriteCache[key] = sprite;
            return sprite;
        }
    }
}
