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

        private const float PaletteWidth = 380f;
        private const float CollapsedPaletteWidth = 0f;
        private const float PalettePadding = 16f;
        private const float PaletteVerticalOffset = -83f;
        private const float CollapseHandleSize = 36f;
        private const float CardWidth = 100f;
        private const float CardHeight = 122f;
        private const float CardGapX = 10f;
        private const float CardGapY = 12f;
        private const float ContentLeft = 8f;
        private const float SectionTitleHeight = 30f;
        private const float SectionGap = 18f;
        private const float OperationLogHeight = 180f;
        private const float OperationLogMargin = 16f;
        private const float OperationLogWidth = PaletteWidth - PalettePadding * 2f;

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
        private Sprite collapseHandleSprite;
        private Button collapseHandleButton;
        private Text collapseHandleLabel;
        private bool isLeftPanelCollapsed;

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

            root.sizeDelta = new Vector2(PaletteWidth, root.sizeDelta.y);
            if (root.GetComponent<RectMask2D>() == null)
            {
                root.gameObject.AddComponent<RectMask2D>();
            }

            var rootImage = root.GetComponent<Image>() ?? root.gameObject.AddComponent<Image>();
            rootImage.color = Color.white;
            rootImage.raycastTarget = true;

            var rootOutline = root.GetComponent<Outline>() ?? root.gameObject.AddComponent<Outline>();
            rootOutline.effectColor = new Color(0.90f, 0.91f, 0.92f, 1f);
            rootOutline.effectDistance = new Vector2(1f, 0f);

            EnsureCollapseHandle(root);

            var title = transform.Find("PaletteTitle") as RectTransform;
            if (title != null)
            {
                title.anchorMin = new Vector2(0f, 1f);
                title.anchorMax = new Vector2(1f, 1f);
                title.pivot = new Vector2(0.5f, 1f);
                title.anchoredPosition = new Vector2(0f, -12f);
                title.sizeDelta = new Vector2(-PalettePadding * 2f, 38f);
                var titleText = title.GetComponent<Text>();
                if (titleText != null)
                {
                    titleText.fontSize = 18;
                    titleText.fontStyle = FontStyle.Bold;
                    titleText.color = new Color(0.12f, 0.16f, 0.22f);
                }
            }

            EnsureFilterButtons(root);
            HideLegacySearchBox();
            EnsureViewportPosition();
            EnsureActionLogLayout();
            EnsureSectionTitleObjects();
            ApplyLeftPanelLayout(PaletteWidth);
        }

        private void EnsureCollapseHandle(RectTransform root)
        {
            var parent = root.parent as RectTransform;
            if (parent == null)
            {
                return;
            }

            var handle = parent.Find("LeftPaletteCollapseHandle") as RectTransform;
            if (handle == null)
            {
                handle = CreateRect("LeftPaletteCollapseHandle", parent);
                handle.gameObject.AddComponent<Image>();
                handle.gameObject.AddComponent<Button>();

                var labelRect = CreateRect("Arrow", handle);
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;

                var label = labelRect.gameObject.AddComponent<Text>();
                label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                label.fontSize = 21;
                label.alignment = TextAnchor.MiddleCenter;
                label.raycastTarget = false;
            }

            handle.SetAsLastSibling();
            handle.anchorMin = new Vector2(0f, 0.5f);
            handle.anchorMax = new Vector2(0f, 0.5f);
            handle.pivot = new Vector2(0.5f, 0.5f);
            handle.sizeDelta = new Vector2(CollapseHandleSize, CollapseHandleSize);

            var image = handle.GetComponent<Image>() ?? handle.gameObject.AddComponent<Image>();
            image.sprite = GetCollapseHandleSprite();
            image.type = Image.Type.Simple;
            image.color = Color.white;
            image.raycastTarget = true;

            var outline = handle.GetComponent<Outline>() ?? handle.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.90f, 0.91f, 0.92f, 1f);
            outline.effectDistance = new Vector2(1f, -1f);

            var shadow = handle.GetComponent<Shadow>() ?? handle.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.14f);
            shadow.effectDistance = new Vector2(0f, -2f);

            collapseHandleButton = handle.GetComponent<Button>() ?? handle.gameObject.AddComponent<Button>();
            collapseHandleButton.onClick.RemoveListener(ToggleLeftPanelCollapsed);
            collapseHandleButton.onClick.AddListener(ToggleLeftPanelCollapsed);
            var colors = collapseHandleButton.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.97f, 0.98f, 1f, 1f);
            colors.pressedColor = new Color(0.93f, 0.96f, 1f, 1f);
            colors.selectedColor = Color.white;
            collapseHandleButton.colors = colors;

            collapseHandleLabel = handle.GetComponentInChildren<Text>(true);
            if (collapseHandleLabel != null)
            {
                collapseHandleLabel.color = new Color(0.58f, 0.64f, 0.72f, 1f);
            }
        }

        private void ToggleLeftPanelCollapsed()
        {
            isLeftPanelCollapsed = !isLeftPanelCollapsed;
            ApplyLeftPanelLayout(isLeftPanelCollapsed ? CollapsedPaletteWidth : PaletteWidth);
        }

        private void ApplyLeftPanelLayout(float width)
        {
            var root = transform as RectTransform;
            if (root != null)
            {
                root.sizeDelta = new Vector2(width, root.sizeDelta.y);
            }

            SetPaletteContentVisible(!isLeftPanelCollapsed);
            AlignWorkspaceToPalette(width);
            AlignActionLogToPalette();
            AlignCollapseHandle(width);
        }

        private void SetPaletteContentVisible(bool visible)
        {
            var title = transform.Find("PaletteTitle");
            if (title != null)
            {
                title.gameObject.SetActive(visible);
            }

            var filterRow = transform.Find("PaletteFilterRow");
            if (filterRow != null)
            {
                filterRow.gameObject.SetActive(visible);
            }

            var viewport = transform.Find("PaletteViewport");
            if (viewport != null)
            {
                viewport.gameObject.SetActive(visible);
            }

            var parent = transform.parent;
            var logPanel = parent != null ? parent.Find("ActionLogPanel") : null;
            if (logPanel != null)
            {
                logPanel.gameObject.SetActive(visible);
            }
        }

        private void AlignWorkspaceToPalette(float width)
        {
            var parent = transform.parent;
            if (parent == null)
            {
                return;
            }

            var workspace = parent.Find("Workspace") as RectTransform;
            if (workspace == null)
            {
                return;
            }

            workspace.offsetMin = new Vector2(width, workspace.offsetMin.y);
        }

        private void AlignActionLogToPalette()
        {
            var parent = transform.parent;
            var logPanel = parent != null ? parent.Find("ActionLogPanel") as RectTransform : null;
            if (logPanel == null)
            {
                return;
            }

            logPanel.anchoredPosition = new Vector2(OperationLogMargin, OperationLogMargin);
            logPanel.sizeDelta = new Vector2(OperationLogWidth, OperationLogHeight);
        }

        private void AlignCollapseHandle(float width)
        {
            if (collapseHandleButton == null)
            {
                return;
            }

            var handle = collapseHandleButton.transform as RectTransform;
            if (handle != null)
            {
                handle.SetAsLastSibling();
                var x = isLeftPanelCollapsed ? CollapseHandleSize * 0.5f : width;
                handle.anchoredPosition = new Vector2(x, PaletteVerticalOffset);
            }

            if (collapseHandleLabel != null)
            {
                collapseHandleLabel.text = isLeftPanelCollapsed ? ">" : "<";
            }
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
                row.sizeDelta = new Vector2(-PalettePadding * 2f, 34f);

                var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                layout.spacing = 8f;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = true;
                layout.childAlignment = TextAnchor.MiddleLeft;
            }

            allFilterButton = allFilterButton != null ? allFilterButton : EnsureFilterButton(row, "Filter_All", "全部", 56f);
            householdFilterButton = householdFilterButton != null ? householdFilterButton : EnsureFilterButton(row, "Filter_Household", "家庭电路组件", 118f);
            industrialFilterButton = industrialFilterButton != null ? industrialFilterButton : EnsureFilterButton(row, "Filter_Industrial", "工业电路组件", 118f);
        }

        private Button EnsureFilterButton(RectTransform row, string name, string label, float width)
        {
            var rect = row.Find(name) as RectTransform;
            if (rect == null)
            {
                rect = CreateRect(name, row);
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

            rect.sizeDelta = new Vector2(width, 32f);

            var image = rect.GetComponent<Image>() ?? rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.92f, 0.95f, 0.98f, 1f);

            var button = rect.GetComponent<Button>() ?? rect.gameObject.AddComponent<Button>();
            var labelText = rect.GetComponentInChildren<Text>();
            if (labelText != null)
            {
                labelText.text = label;
                labelText.color = new Color(0.16f, 0.22f, 0.32f);
            }

            var layout = rect.GetComponent<LayoutElement>() ?? rect.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.preferredHeight = 32f;
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
            viewport.offsetMin = new Vector2(PalettePadding, OperationLogHeight + OperationLogMargin + 16f);
            viewport.offsetMax = new Vector2(-PalettePadding, -104f);

            var viewportImage = viewport.GetComponent<Image>() ?? viewport.gameObject.AddComponent<Image>();
            viewportImage.color = Color.white;
            viewportImage.raycastTarget = true;
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
            logPanel.sizeDelta = new Vector2(OperationLogWidth, OperationLogHeight);
            logPanel.SetAsLastSibling();

            var panelImage = logPanel.GetComponent<Image>() ?? logPanel.gameObject.AddComponent<Image>();
            panelImage.color = Color.white;
            panelImage.raycastTarget = true;

            var outline = logPanel.GetComponent<Outline>() ?? logPanel.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.90f, 0.91f, 0.92f, 1f);
            outline.effectDistance = new Vector2(1f, -1f);

            var title = logPanel.Find("ActionLogTitle") as RectTransform;
            if (title != null)
            {
                title.anchorMin = new Vector2(0f, 1f);
                title.anchorMax = new Vector2(1f, 1f);
                title.pivot = new Vector2(0.5f, 1f);
                title.anchoredPosition = new Vector2(0f, -6f);
                title.sizeDelta = new Vector2(-20f, 28f);
                var titleText = title.GetComponent<Text>();
                if (titleText != null)
                {
                    titleText.fontSize = 16;
                    titleText.fontStyle = FontStyle.Bold;
                    titleText.color = new Color(0.12f, 0.16f, 0.22f);
                }
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
                viewport.offsetMin = new Vector2(10f, 10f);
                viewport.offsetMax = new Vector2(-10f, -74f);

                var viewportImage = viewport.GetComponent<Image>() ?? viewport.gameObject.AddComponent<Image>();
                viewportImage.color = new Color(0.98f, 0.99f, 1f, 1f);
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
                titleRect.sizeDelta = new Vector2(OperationLogWidth, 34f);

                var title = titleRect.gameObject.AddComponent<Text>();
                title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                title.fontSize = 17;
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
            background.color = Color.white;
            background.raycastTarget = true;

            var iconImage = EnsureChildImage(rect, "Icon");
            var iconRect = iconImage.rectTransform;
            iconRect.anchorMin = new Vector2(0.5f, 1f);
            iconRect.anchorMax = new Vector2(0.5f, 1f);
            iconRect.pivot = new Vector2(0.5f, 1f);
            iconRect.anchoredPosition = new Vector2(0f, -10f);
            iconRect.sizeDelta = GetPaletteIconSize(definition);
            var paletteSprite = ResolvePaletteIcon(definition);
            iconImage.sprite = paletteSprite != null ? paletteSprite : GetFallbackIcon();
            iconImage.color = paletteSprite != null ? Color.white : GetCategoryIconColor(definition.category);
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;

            var label = EnsureChildText(rect, "Label");
            label.text = definition.displayName;
            label.fontSize = 13;
            label.alignment = TextAnchor.UpperCenter;
            label.color = new Color(0.12f, 0.16f, 0.22f);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 10;
            label.resizeTextMaxSize = 13;
            label.raycastTarget = false;

            var labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 0f);
            labelRect.pivot = new Vector2(0.5f, 0f);
            labelRect.anchoredPosition = new Vector2(0f, 9f);
            labelRect.sizeDelta = new Vector2(-12f, 36f);

            var outline = rect.GetComponent<Outline>() ?? rect.gameObject.AddComponent<Outline>();
            var normalOutline = new Color(0.91f, 0.94f, 0.97f, 0.48f);
            var hoverOutline = new Color(0.58f, 0.77f, 0.99f, 1f);
            outline.effectColor = normalOutline;
            outline.effectDistance = new Vector2(1f, -1f);

            var paletteItem = rect.GetComponent<PaletteItem>();
            if (paletteItem != null)
            {
                paletteItem.ConfigureCardVisual(
                    background,
                    Color.white,
                    new Color(0.97f, 0.98f, 1f, 1f),
                    outline,
                    normalOutline,
                    hoverOutline);
            }
        }

        private static Vector2 GetPaletteIconSize(ComponentDefinition definition)
        {
            var name = definition != null ? definition.name : string.Empty;

            if (ContainsName(name, "AC_ThreePhase_Power") ||
                ContainsName(name, "TerminalBlock") ||
                ContainsName(name, "Fuse_1P") ||
                ContainsName(name, "Fuse_3P") ||
                ContainsName(name, "KnifeSwitch"))
            {
                return new Vector2(76f, 40f);
            }

            if (ContainsName(name, "AC_220V_Power") ||
                ContainsName(name, "Single_Phase_Meter") ||
                ContainsName(name, "Breaker_1P"))
            {
                return new Vector2(52f, 72f);
            }

            if (ContainsName(name, "Contactor_KM") ||
                ContainsName(name, "ThermalRelay") ||
                ContainsName(name, "Timer_") ||
                ContainsName(name, "TimerRelay") ||
                ContainsName(name, "Motor_") ||
                ContainsName(name, "LimitSwitch") ||
                ContainsName(name, "Breaker_2P") ||
                ContainsName(name, "Breaker_3P") ||
                ContainsName(name, "Breaker_4P"))
            {
                return new Vector2(72f, 72f);
            }

            return new Vector2(64f, 64f);
        }

        private static bool ContainsName(string value, string pattern)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   value.IndexOf(pattern, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static Sprite ResolvePaletteIcon(ComponentDefinition definition)
        {
            if (definition == null)
            {
                return null;
            }

#if UNITY_EDITOR
            var specialIcon = LoadSpecialPaletteIcon(definition.name);
            if (specialIcon != null)
            {
                return specialIcon;
            }

            if (VisualPrefabRegistry.TryGetConfig(definition.name, out var config) && config != null)
            {
                var sprite = LoadSpriteAtPath(config.DefaultSpritePath);
                if (sprite != null)
                {
                    return sprite;
                }

                sprite = LoadSpriteFromVisualPrefab(config.PrefabPath);
                if (sprite != null)
                {
                    return sprite;
                }
            }
#endif

            return definition.sprite;
        }

#if UNITY_EDITOR
        private static Sprite LoadSpecialPaletteIcon(string definitionName)
        {
            if (string.IsNullOrWhiteSpace(definitionName))
            {
                return null;
            }

            if (definitionName.IndexOf("Contactor_KM", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return LoadSpriteAtPath("Assets/Art/Components/Contactor_KM_380V_Default.png");
            }

            return null;
        }

        private static Sprite LoadSpriteAtPath(string path)
        {
            return string.IsNullOrWhiteSpace(path) ? null : UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Sprite LoadSpriteFromVisualPrefab(string prefabPath)
        {
            if (string.IsNullOrWhiteSpace(prefabPath))
            {
                return null;
            }

            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                return null;
            }

            var body = prefab.transform.Find("Body");
            var image = body != null ? body.GetComponent<Image>() : prefab.GetComponentInChildren<Image>(true);
            return image != null ? image.sprite : null;
        }
#endif

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

        private Sprite GetCollapseHandleSprite()
        {
            if (collapseHandleSprite != null)
            {
                return collapseHandleSprite;
            }

            var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            var clear = new Color(0f, 0f, 0f, 0f);
            var fill = Color.white;
            var center = new Vector2(31.5f, 31.5f);
            for (var y = 0; y < 64; y++)
            {
                for (var x = 0; x < 64; x++)
                {
                    var distance = Vector2.Distance(new Vector2(x, y), center);
                    texture.SetPixel(x, y, distance <= 30f ? fill : clear);
                }
            }

            texture.Apply();
            collapseHandleSprite = Sprite.Create(texture, new Rect(0f, 0f, 64f, 64f), new Vector2(0.5f, 0.5f), 64f);
            return collapseHandleSprite;
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
            var y = -14f;
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
                    title.anchoredPosition = new Vector2(18f, y);
                    var titleText = title.GetComponent<Text>();
                    if (titleText != null && sectionDisplayNames.TryGetValue(categoryEnum, out var sectionName))
                    {
                        titleText.text = sectionName;
                        titleText.fontSize = 17;
                    }
                }

                if (visibleCount == 0)
                {
                    HideCategory(categoryEnum);
                    continue;
                }

                y -= SectionTitleHeight + 12f;
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
