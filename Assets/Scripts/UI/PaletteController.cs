using System.Collections.Generic;
using ElectricalSim.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ElectricalSim.UI
{
    public sealed class PaletteController : MonoBehaviour
    {
        [SerializeField] private InputField searchInput;
        [SerializeField] private SaveLoadService saveLoadService;
        [SerializeField] private WorkspaceController workspace;
        [SerializeField] private RectTransform content;
        [SerializeField] private Button allFilterButton;
        [SerializeField] private Button householdFilterButton;
        [SerializeField] private Button industrialFilterButton;
        [SerializeField] private List<RectTransform> sectionTitles = new List<RectTransform>();
        [SerializeField] private List<RectTransform> itemRects = new List<RectTransform>();
        [SerializeField] private List<string> itemNames = new List<string>();
        [SerializeField] private List<int> itemCategories = new List<int>();

        private enum PaletteFilter
        {
            All,
            Household,
            Industrial
        }

        private const float CardWidth = 108f;
        private const float CardHeight = 124f;
        private const float CardGapX = 18f;
        private const float CardGapY = 16f;
        private const float ContentLeft = 18f;
        private const float SectionTitleHeight = 30f;
        private const float SectionGap = 18f;
        private const float OperationLogHeight = 176f;
        private const float OperationLogMargin = 10f;

        private readonly ComponentCategory[] categoryOrder =
        {
            ComponentCategory.Household,
            ComponentCategory.Industrial,
            ComponentCategory.Measurement
        };

        private readonly Dictionary<ComponentCategory, string> sectionDisplayNames = new Dictionary<ComponentCategory, string>
        {
            { ComponentCategory.Household, "家庭电路组件" },
            { ComponentCategory.Industrial, "工业电路组件" },
            { ComponentCategory.Measurement, "测量工具" }
        };

        private PaletteFilter currentFilter = PaletteFilter.All;
        private Sprite fallbackIcon;

        private void Awake()
        {
            EnsureCardPaletteShell();
            searchInput?.onValueChanged.AddListener(_ => ApplyFilter());
            allFilterButton?.onClick.AddListener(() => SetFilter(PaletteFilter.All));
            householdFilterButton?.onClick.AddListener(() => SetFilter(PaletteFilter.Household));
            industrialFilterButton?.onClick.AddListener(() => SetFilter(PaletteFilter.Industrial));
            AddMissingCatalogItems();
            UpgradeExistingItems();
            ApplyFilter();
        }

        private void EnsureCardPaletteShell()
        {
            var root = transform as RectTransform;
            if (root == null)
            {
                return;
            }

            var title = transform.Find("PaletteTitle") as RectTransform;
            if (title != null)
            {
                title.anchorMin = new Vector2(0f, 1f);
                title.anchorMax = new Vector2(1f, 1f);
                title.pivot = new Vector2(0.5f, 1f);
                title.anchoredPosition = new Vector2(0f, -16f);
                title.sizeDelta = new Vector2(-36f, 38f);
            }

            EnsureFilterButtons(root);
            HideLegacySearchBox();
            EnsureViewportPosition();
            EnsureActionLogLayout();
            EnsureSectionTitleObjects();
        }

        private void EnsureFilterButtons(RectTransform root)
        {
            var row = transform.Find("PaletteFilterRow") as RectTransform;
            if (row == null)
            {
                row = CreateRect("PaletteFilterRow", root);
                row.anchorMin = new Vector2(0f, 1f);
                row.anchorMax = new Vector2(1f, 1f);
                row.pivot = new Vector2(0.5f, 1f);
                row.anchoredPosition = new Vector2(0f, -58f);
                row.sizeDelta = new Vector2(-28f, 34f);

                var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                layout.spacing = 8f;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = true;
                layout.childAlignment = TextAnchor.MiddleLeft;
            }

            allFilterButton = allFilterButton != null ? allFilterButton : EnsureFilterButton(row, "Filter_All", "全部", 68f);
            householdFilterButton = householdFilterButton != null ? householdFilterButton : EnsureFilterButton(row, "Filter_Household", "家庭电路组件", 126f);
            industrialFilterButton = industrialFilterButton != null ? industrialFilterButton : EnsureFilterButton(row, "Filter_Industrial", "工业电路组件", 126f);
        }

        private Button EnsureFilterButton(RectTransform row, string name, string label, float width)
        {
            var rect = row.Find(name) as RectTransform;
            if (rect == null)
            {
                rect = CreateRect(name, row);
                rect.sizeDelta = new Vector2(width, 30f);
                rect.gameObject.AddComponent<Image>();
                rect.gameObject.AddComponent<Button>();

                var textRect = CreateRect("Text", rect);
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(8f, 2f);
                textRect.offsetMax = new Vector2(-8f, -2f);

                var text = textRect.gameObject.AddComponent<Text>();
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text.fontSize = 13;
                text.alignment = TextAnchor.MiddleCenter;
                text.horizontalOverflow = HorizontalWrapMode.Wrap;
                text.verticalOverflow = VerticalWrapMode.Truncate;
                text.raycastTarget = false;
            }

            var image = rect.GetComponent<Image>() ?? rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.91f, 0.94f, 0.98f, 1f);

            var button = rect.GetComponent<Button>() ?? rect.gameObject.AddComponent<Button>();
            var labelText = rect.GetComponentInChildren<Text>();
            if (labelText != null)
            {
                labelText.text = label;
                labelText.color = new Color(0.16f, 0.22f, 0.32f);
            }

            var layout = rect.GetComponent<LayoutElement>() ?? rect.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.preferredHeight = 30f;
            return button;
        }

        private void HideLegacySearchBox()
        {
            if (searchInput == null)
            {
                searchInput = GetComponentInChildren<InputField>(true);
            }

            if (searchInput == null)
            {
                return;
            }

            searchInput.text = string.Empty;
            searchInput.gameObject.SetActive(false);
        }

        private void EnsureViewportPosition()
        {
            var viewport = transform.Find("PaletteViewport") as RectTransform;
            if (viewport == null)
            {
                return;
            }

            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.pivot = new Vector2(0.5f, 0.5f);
            viewport.offsetMin = new Vector2(8f, OperationLogHeight + OperationLogMargin * 2f + 8f);
            viewport.offsetMax = new Vector2(-8f, -100f);
        }

        private void EnsureActionLogLayout()
        {
            var parent = transform.parent;
            if (parent == null)
            {
                return;
            }

            var logPanel = parent.Find("ActionLogPanel") as RectTransform;
            if (logPanel == null)
            {
                return;
            }

            logPanel.anchorMin = new Vector2(0f, 0f);
            logPanel.anchorMax = new Vector2(0f, 0f);
            logPanel.pivot = new Vector2(0f, 0f);
            logPanel.anchoredPosition = new Vector2(OperationLogMargin, OperationLogMargin);
            logPanel.sizeDelta = new Vector2(400f, OperationLogHeight);
            logPanel.SetAsLastSibling();

            var panelImage = logPanel.GetComponent<Image>() ?? logPanel.gameObject.AddComponent<Image>();
            panelImage.color = new Color(0.97f, 0.98f, 0.99f, 1f);
            panelImage.raycastTarget = true;

            var outline = logPanel.GetComponent<Outline>() ?? logPanel.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.68f, 0.76f, 0.86f, 0.7f);
            outline.effectDistance = new Vector2(1f, -1f);

            var title = logPanel.Find("ActionLogTitle") as RectTransform;
            if (title != null)
            {
                title.anchorMin = new Vector2(0f, 1f);
                title.anchorMax = new Vector2(1f, 1f);
                title.pivot = new Vector2(0.5f, 1f);
                title.anchoredPosition = new Vector2(0f, -6f);
                title.sizeDelta = new Vector2(-24f, 28f);
            }

            var status = logPanel.Find("CurrentStatus") as RectTransform;
            if (status != null)
            {
                status.anchorMin = new Vector2(0f, 1f);
                status.anchorMax = new Vector2(1f, 1f);
                status.pivot = new Vector2(0.5f, 1f);
                status.anchoredPosition = new Vector2(0f, -36f);
                status.sizeDelta = new Vector2(-24f, 32f);
            }

            var viewport = logPanel.Find("ActionLogViewport") as RectTransform;
            if (viewport != null)
            {
                viewport.anchorMin = Vector2.zero;
                viewport.anchorMax = Vector2.one;
                viewport.pivot = new Vector2(0.5f, 0.5f);
                viewport.offsetMin = new Vector2(12f, 10f);
                viewport.offsetMax = new Vector2(-12f, -74f);

                var viewportImage = viewport.GetComponent<Image>() ?? viewport.gameObject.AddComponent<Image>();
                viewportImage.color = Color.white;
                viewportImage.raycastTarget = true;
            }
        }

        private void EnsureSectionTitleObjects()
        {
            if (content == null)
            {
                return;
            }

            while (sectionTitles.Count < categoryOrder.Length)
            {
                var category = categoryOrder[sectionTitles.Count];
                var titleRect = CreateRect("Section_" + category, content);
                titleRect.anchorMin = new Vector2(0f, 1f);
                titleRect.anchorMax = new Vector2(0f, 1f);
                titleRect.pivot = new Vector2(0f, 1f);
                titleRect.sizeDelta = new Vector2(320f, 34f);

                var title = titleRect.gameObject.AddComponent<Text>();
                title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                title.fontSize = 19;
                title.alignment = TextAnchor.MiddleLeft;
                title.color = new Color(0.07f, 0.12f, 0.18f);
                title.raycastTarget = false;
                title.text = sectionDisplayNames.TryGetValue(category, out var displayName) ? displayName : category.ToString();

                sectionTitles.Add(titleRect);
            }
        }

        private void AddMissingCatalogItems()
        {
            if (content == null)
            {
                return;
            }

            if (saveLoadService == null)
            {
                saveLoadService = FindObjectOfType<SaveLoadService>();
            }

            if (workspace == null)
            {
                workspace = FindObjectOfType<WorkspaceController>();
            }

            var catalog = saveLoadService != null ? saveLoadService.Catalog : null;
            if (catalog == null || workspace == null)
            {
                return;
            }

            foreach (var definition in catalog)
            {
                if (definition == null || !definition.showInPalette || HasPaletteItem(definition))
                {
                    continue;
                }

                CreateRuntimePaletteItem(definition);
            }
        }

        private bool HasPaletteItem(ComponentDefinition definition)
        {
            for (var i = 0; i < itemRects.Count; i++)
            {
                var rect = itemRects[i];
                if (rect == null)
                {
                    continue;
                }

                var item = rect.GetComponent<PaletteItem>();
                if (item != null && item.Definition == definition)
                {
                    return true;
                }
            }

            return false;
        }

        private void CreateRuntimePaletteItem(ComponentDefinition definition)
        {
            var itemObject = new GameObject("Palette_" + definition.name, typeof(RectTransform), typeof(Image), typeof(PaletteItem));
            itemObject.transform.SetParent(content, false);

            var rect = itemObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(CardWidth, CardHeight);

            var item = itemObject.GetComponent<PaletteItem>();
            item.Initialize(definition, workspace);
            ConfigureCard(rect, definition);

            itemRects.Add(rect);
            itemNames.Add(definition.displayName);
            itemCategories.Add((int)definition.category);
        }

        private void UpgradeExistingItems()
        {
            for (var i = 0; i < itemRects.Count; i++)
            {
                var rect = itemRects[i];
                if (rect == null)
                {
                    continue;
                }

                var item = rect.GetComponent<PaletteItem>();
                var definition = item != null ? item.Definition : null;
                if (definition != null)
                {
                    ConfigureCard(rect, definition);
                }
            }
        }

        private void ConfigureCard(RectTransform rect, ComponentDefinition definition)
        {
            if (rect == null || definition == null)
            {
                return;
            }

            rect.sizeDelta = new Vector2(CardWidth, CardHeight);

            var background = rect.GetComponent<Image>() ?? rect.gameObject.AddComponent<Image>();
            background.color = new Color(0.96f, 0.98f, 1f, 1f);
            background.raycastTarget = true;

            var iconImage = EnsureChildImage(rect, "Icon");
            var iconRect = iconImage.rectTransform;
            iconRect.anchorMin = new Vector2(0.5f, 1f);
            iconRect.anchorMax = new Vector2(0.5f, 1f);
            iconRect.pivot = new Vector2(0.5f, 1f);
            iconRect.anchoredPosition = new Vector2(0f, -12f);
            iconRect.sizeDelta = new Vector2(64f, 58f);
            iconImage.sprite = definition.sprite != null ? definition.sprite : GetFallbackIcon();
            iconImage.color = definition.sprite != null ? Color.white : GetCategoryIconColor(definition.category);
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;

            var label = EnsureChildText(rect, "Label");
            label.text = definition.displayName;
            label.fontSize = 12;
            label.alignment = TextAnchor.UpperCenter;
            label.color = new Color(0.08f, 0.12f, 0.2f);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 9;
            label.resizeTextMaxSize = 12;
            label.raycastTarget = false;

            var labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 0f);
            labelRect.pivot = new Vector2(0.5f, 0f);
            labelRect.anchoredPosition = new Vector2(0f, 8f);
            labelRect.sizeDelta = new Vector2(-12f, 46f);

            var outline = rect.GetComponent<Outline>() ?? rect.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.68f, 0.76f, 0.86f, 0.7f);
            outline.effectDistance = new Vector2(1f, -1f);

            var paletteItem = rect.GetComponent<PaletteItem>();
            if (paletteItem != null)
            {
                paletteItem.ConfigureCardVisual(background, new Color(0.96f, 0.98f, 1f, 1f), new Color(0.88f, 0.93f, 1f, 1f));
            }
        }

        private static Image EnsureChildImage(RectTransform parent, string name)
        {
            var child = parent.Find(name) as RectTransform;
            if (child == null)
            {
                child = CreateRect(name, parent);
                child.gameObject.AddComponent<Image>();
            }

            return child.GetComponent<Image>() ?? child.gameObject.AddComponent<Image>();
        }

        private static Text EnsureChildText(RectTransform parent, string name)
        {
            var child = parent.Find(name) as RectTransform;
            if (child == null)
            {
                child = CreateRect(name, parent);
                child.gameObject.AddComponent<Text>();
            }

            var text = child.GetComponent<Text>() ?? child.gameObject.AddComponent<Text>();
            if (text.font == null)
            {
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            return text;
        }

        private Sprite GetFallbackIcon()
        {
            if (fallbackIcon != null)
            {
                return fallbackIcon;
            }

            var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            var clear = new Color(0f, 0f, 0f, 0f);
            var fill = new Color(0.64f, 0.72f, 0.84f, 1f);
            var dark = new Color(0.34f, 0.42f, 0.55f, 1f);
            var center = new Vector2(31.5f, 31.5f);
            for (var y = 0; y < 64; y++)
            {
                for (var x = 0; x < 64; x++)
                {
                    var distance = Vector2.Distance(new Vector2(x, y), center);
                    if (distance < 22f)
                    {
                        texture.SetPixel(x, y, fill);
                    }
                    else if (distance < 25f)
                    {
                        texture.SetPixel(x, y, dark);
                    }
                    else
                    {
                        texture.SetPixel(x, y, clear);
                    }
                }
            }

            texture.Apply();
            fallbackIcon = Sprite.Create(texture, new Rect(0f, 0f, 64f, 64f), new Vector2(0.5f, 0.5f), 64f);
            return fallbackIcon;
        }

        private static Color GetCategoryIconColor(ComponentCategory category)
        {
            switch (category)
            {
                case ComponentCategory.Household:
                    return new Color(0.35f, 0.58f, 0.95f, 1f);
                case ComponentCategory.Industrial:
                    return new Color(0.94f, 0.58f, 0.24f, 1f);
                case ComponentCategory.Measurement:
                    return new Color(0.37f, 0.68f, 0.48f, 1f);
                default:
                    return new Color(0.64f, 0.72f, 0.84f, 1f);
            }
        }

        private void ApplyFilter()
        {
            var query = searchInput != null ? searchInput.text.Trim() : string.Empty;
            var y = -8f;
            UpdateFilterButtonState();

            for (var sectionIndex = 0; sectionIndex < categoryOrder.Length; sectionIndex++)
            {
                var categoryEnum = categoryOrder[sectionIndex];
                if (!ShouldShowCategory(categoryEnum))
                {
                    HideCategory(categoryEnum);
                    var hiddenTitle = sectionIndex < sectionTitles.Count ? sectionTitles[sectionIndex] : null;
                    if (hiddenTitle != null)
                    {
                        hiddenTitle.gameObject.SetActive(false);
                    }

                    continue;
                }

                var category = (int)categoryEnum;
                var visibleCount = 0;
                for (var i = 0; i < itemRects.Count; i++)
                {
                    if (i >= itemCategories.Count || itemCategories[i] != category)
                    {
                        continue;
                    }

                    if (Matches(i, query))
                    {
                        visibleCount++;
                    }
                }

                var title = sectionIndex < sectionTitles.Count ? sectionTitles[sectionIndex] : null;
                if (title != null)
                {
                    title.gameObject.SetActive(visibleCount > 0);
                    title.anchoredPosition = new Vector2(20f, y);
                    var titleText = title.GetComponent<Text>();
                    if (titleText != null && sectionDisplayNames.TryGetValue(categoryEnum, out var sectionName))
                    {
                        titleText.text = sectionName;
                        titleText.fontSize = 19;
                    }
                }

                if (visibleCount == 0)
                {
                    HideCategory(categoryEnum);
                    continue;
                }

                y -= SectionTitleHeight + 8f;
                var visibleIndex = 0;
                for (var i = 0; i < itemRects.Count; i++)
                {
                    if (i >= itemCategories.Count || itemCategories[i] != category)
                    {
                        continue;
                    }

                    var visible = Matches(i, query);
                    itemRects[i].gameObject.SetActive(visible);
                    if (!visible)
                    {
                        continue;
                    }

                    var row = visibleIndex / 3;
                    var col = visibleIndex % 3;
                    itemRects[i].anchoredPosition = new Vector2(ContentLeft + col * (CardWidth + CardGapX), y - row * (CardHeight + CardGapY));
                    visibleIndex++;
                }

                var rows = Mathf.CeilToInt(visibleCount / 3f);
                y -= rows * (CardHeight + CardGapY) + SectionGap;
            }

            for (var i = 0; i < itemRects.Count; i++)
            {
                var categoryKnown = i < itemCategories.Count;
                if (!categoryKnown)
                {
                    itemRects[i].gameObject.SetActive(false);
                }
            }

            if (content != null)
            {
                content.sizeDelta = new Vector2(0f, Mathf.Max(900f, Mathf.Abs(y) + 24f));
                content.anchoredPosition = Vector2.zero;
            }
        }

        private bool ShouldShowCategory(ComponentCategory category)
        {
            switch (currentFilter)
            {
                case PaletteFilter.Household:
                    return category == ComponentCategory.Household;
                case PaletteFilter.Industrial:
                    return category == ComponentCategory.Industrial;
                default:
                    return category == ComponentCategory.Household || category == ComponentCategory.Industrial;
            }
        }

        private void HideCategory(ComponentCategory category)
        {
            var categoryValue = (int)category;
            for (var i = 0; i < itemRects.Count; i++)
            {
                if (i < itemCategories.Count && itemCategories[i] == categoryValue && itemRects[i] != null)
                {
                    itemRects[i].gameObject.SetActive(false);
                }
            }
        }

        private void SetFilter(PaletteFilter filter)
        {
            currentFilter = filter;
            ApplyFilter();
        }

        private void UpdateFilterButtonState()
        {
            SetFilterButtonVisual(allFilterButton, currentFilter == PaletteFilter.All);
            SetFilterButtonVisual(householdFilterButton, currentFilter == PaletteFilter.Household);
            SetFilterButtonVisual(industrialFilterButton, currentFilter == PaletteFilter.Industrial);
        }

        private static void SetFilterButtonVisual(Button button, bool selected)
        {
            if (button == null)
            {
                return;
            }

            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = selected ? new Color(0.20f, 0.45f, 0.95f, 1f) : new Color(0.91f, 0.94f, 0.98f, 1f);
            }

            var label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.color = selected ? Color.white : new Color(0.16f, 0.22f, 0.32f);
            }
        }

        private bool Matches(int index, string query)
        {
            if (!IsVisibleInPalette(index))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(query))
            {
                return true;
            }

            var name = index >= 0 && index < itemNames.Count ? itemNames[index] : string.Empty;
            return name.IndexOf(query, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private bool IsVisibleInPalette(int index)
        {
            if (index < 0 || index >= itemRects.Count || itemRects[index] == null)
            {
                return false;
            }

            var item = itemRects[index].GetComponent<PaletteItem>();
            return item == null || item.Definition == null || item.Definition.showInPalette;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }
    }
}
