using System;
using System.Collections.Generic;
using ElectricalSim.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ElectricalSim.UI
{
    public sealed class EncyclopediaController : MonoBehaviour
    {
        private const string CategoryAll = "全部";
        private const float SidebarWidth = 220f;
        private const float CardWidth = 370f;
        private const float CardHeight = 160f;
        private const float CardGapX = 18f;
        private const float CardGapY = 18f;

        [SerializeField] private List<Button> categoryButtons = new List<Button>();
        [SerializeField] private List<Text> categoryLabels = new List<Text>();
        [SerializeField] private List<RectTransform> categorySections = new List<RectTransform>();
        [SerializeField] private RectTransform content;

        private readonly List<ComponentEncyclopediaEntry> entries = new List<ComponentEncyclopediaEntry>();
        private readonly List<RectTransform> cardRects = new List<RectTransform>();
        private readonly string[] categories =
        {
            CategoryAll,
            "开关按钮",
            "控制元件",
            "传感元件",
            "保护设备",
            "电源仪表",
            "用电设备",
            "端子与模块",
            "其他"
        };

        private RectTransform listViewRoot;
        private RectTransform detailViewRoot;
        private RectTransform cardContent;
        private RectTransform detailContent;
        private ScrollRect cardScrollRect;
        private GridLayoutGroup cardGridLayout;
        private ScrollRect detailScrollRect;
        private InputField searchInput;
        private Text emptyText;
        private Text detailHintText;
        private string selectedCategory = CategoryAll;
        private string searchText = string.Empty;
        private Sprite fallbackSprite;

        private void Start()
        {
            BuildPage();
        }

        private void BuildPage()
        {
            ClearExistingChildren();
            entries.Clear();
            entries.AddRange(ComponentEncyclopediaDatabase.Build(LoadDefinitions()));

            CreateHeader();
            CreateListView();
            CreateDetailView();
            ShowListView();
            RefreshCards();
        }

        private List<ComponentDefinition> LoadDefinitions()
        {
            var definitions = new List<ComponentDefinition>();
            var saveLoadService = FindObjectOfType<SaveLoadService>();
            if (saveLoadService != null && saveLoadService.Catalog != null)
            {
                foreach (var definition in saveLoadService.Catalog)
                {
                    if (definition != null && definition.showInPalette)
                    {
                        definitions.Add(definition);
                    }
                }
            }

#if UNITY_EDITOR
            if (definitions.Count == 0)
            {
                var guids = UnityEditor.AssetDatabase.FindAssets("t:ComponentDefinition", new[] { "Assets/Data" });
                for (var i = 0; i < guids.Length; i++)
                {
                    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
                    var definition = UnityEditor.AssetDatabase.LoadAssetAtPath<ComponentDefinition>(path);
                    if (definition != null && definition.showInPalette)
                    {
                        definitions.Add(definition);
                    }
                }
            }
#endif

            definitions.Sort((a, b) => string.Compare(GetDisplayName(a), GetDisplayName(b), StringComparison.Ordinal));
            return definitions;
        }

        private void ClearExistingChildren()
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }

            categoryButtons.Clear();
            categoryLabels.Clear();
            categorySections.Clear();
        }

        private void CreateHeader()
        {
            var title = CreateText("EncyclopediaTitle", transform, "元器件百科", 32, FontStyle.Bold, new Color(0.07f, 0.11f, 0.18f));
            title.alignment = TextAnchor.MiddleLeft;
            SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(34f, -30f), new Vector2(320f, 54f));

            var subtitle = CreateText("EncyclopediaSubtitle", transform, "按元件类型整理用途、端子、接线方式和仿真规则，点击卡片查看详情。", 18, FontStyle.Normal, new Color(0.35f, 0.42f, 0.52f));
            subtitle.alignment = TextAnchor.MiddleLeft;
            SetRect(subtitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(360f, -34f), new Vector2(-560f, 48f));

            var searchPanel = CreatePanel("SearchBox", transform, Color.white);
            SetRect(searchPanel, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-34f, -32f), new Vector2(300f, 42f));
            var outline = searchPanel.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.86f, 0.90f, 0.95f, 1f);
            outline.effectDistance = new Vector2(1f, -1f);

            searchInput = searchPanel.gameObject.AddComponent<InputField>();
            var searchTextLabel = CreateText("Text", searchPanel, string.Empty, 15, FontStyle.Normal, new Color(0.10f, 0.15f, 0.22f));
            searchTextLabel.alignment = TextAnchor.MiddleLeft;
            SetRect(searchTextLabel.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(16f, 0f), new Vector2(-16f, 0f));
            var placeholder = CreateText("Placeholder", searchPanel, "搜索名称、端子或用途", 15, FontStyle.Normal, new Color(0.52f, 0.58f, 0.66f));
            placeholder.alignment = TextAnchor.MiddleLeft;
            SetRect(placeholder.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(16f, 0f), new Vector2(-16f, 0f));
            searchInput.textComponent = searchTextLabel;
            searchInput.placeholder = placeholder;
            searchInput.onValueChanged.AddListener(value =>
            {
                searchText = value ?? string.Empty;
                RefreshCards();
            });
        }

        private void CreateListView()
        {
            listViewRoot = CreateRect("ListViewRoot", transform);
            SetRect(listViewRoot, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            var sidebar = CreatePanel("CategorySidebar", listViewRoot, Color.white);
            SetRect(sidebar, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(28f, -92f), new Vector2(SidebarWidth, -136f));
            var sidebarOutline = sidebar.gameObject.AddComponent<Outline>();
            sidebarOutline.effectColor = new Color(0.88f, 0.91f, 0.95f, 1f);
            sidebarOutline.effectDistance = new Vector2(1f, -1f);

            var sidebarTitle = CreateText("SidebarTitle", sidebar, "分类", 22, FontStyle.Bold, new Color(0.07f, 0.11f, 0.18f));
            sidebarTitle.alignment = TextAnchor.MiddleLeft;
            SetRect(sidebarTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(20f, -18f), new Vector2(-40f, 40f));

            for (var i = 0; i < categories.Length; i++)
            {
                var category = categories[i];
                var buttonRect = CreatePanel("Category_" + category, sidebar, new Color(0.96f, 0.98f, 1f, 1f));
                SetRect(buttonRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -72f - i * 48f), new Vector2(-32f, 38f));
                var button = buttonRect.gameObject.AddComponent<Button>();
                var label = CreateText("Text", buttonRect, category, 16, FontStyle.Normal, new Color(0.18f, 0.24f, 0.32f));
                label.alignment = TextAnchor.MiddleLeft;
                SetRect(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(16f, 0f), new Vector2(-16f, 0f));
                var captured = category;
                button.onClick.AddListener(() =>
                {
                    selectedCategory = captured;
                    RefreshCards();
                });
                categoryButtons.Add(button);
                categoryLabels.Add(label);
            }

            var scrollRoot = CreatePanel("CardScrollView", listViewRoot, new Color(1f, 1f, 1f, 0.01f));
            StretchTo(scrollRoot, 290f, 104f, 32f, 24f);

            var viewport = CreatePanel("Viewport", scrollRoot, new Color(1f, 1f, 1f, 0.01f));
            StretchTo(viewport, 0f, 0f, 0f, 0f);
            var mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            cardContent = CreateRect("CardGridContent", viewport);
            cardContent.anchorMin = new Vector2(0.5f, 1f);
            cardContent.anchorMax = new Vector2(0.5f, 1f);
            cardContent.pivot = new Vector2(0.5f, 1f);
            cardContent.anchoredPosition = Vector2.zero;
            cardContent.sizeDelta = Vector2.zero;

            cardGridLayout = cardContent.gameObject.AddComponent<GridLayoutGroup>();
            cardGridLayout.cellSize = new Vector2(CardWidth, CardHeight);
            cardGridLayout.spacing = new Vector2(CardGapX, CardGapY);
            cardGridLayout.padding = new RectOffset(0, 0, 0, 24);
            cardGridLayout.childAlignment = TextAnchor.UpperLeft;
            cardGridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            cardGridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            cardGridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            cardGridLayout.constraintCount = 3;

            var fitter = cardContent.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            cardScrollRect = scrollRoot.gameObject.AddComponent<ScrollRect>();
            cardScrollRect.viewport = viewport;
            cardScrollRect.content = cardContent;
            cardScrollRect.horizontal = false;
            cardScrollRect.vertical = true;
            cardScrollRect.movementType = ScrollRect.MovementType.Clamped;
            cardScrollRect.scrollSensitivity = 28f;

            emptyText = CreateText("EmptyText", listViewRoot, "未找到相关元器件", 18, FontStyle.Normal, new Color(0.40f, 0.46f, 0.55f));
            emptyText.alignment = TextAnchor.MiddleCenter;
            StretchTo(emptyText.rectTransform, 290f, 104f, 32f, 24f);
            emptyText.gameObject.SetActive(false);
        }

        private void CreateDetailView()
        {
            detailViewRoot = CreateRect("DetailViewRoot", transform);
            SetRect(detailViewRoot, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            detailViewRoot.gameObject.SetActive(false);

            var backButton = CreateButton("BackButton", detailViewRoot, "返回百科", new Color(0.90f, 0.94f, 1f), new Color(0.06f, 0.28f, 0.72f), 16);
            SetRect(backButton.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(34f, -92f), new Vector2(118f, 38f));
            backButton.onClick.AddListener(ShowListView);

            detailHintText = CreateText("DetailHint", detailViewRoot, string.Empty, 14, FontStyle.Normal, new Color(0.79f, 0.39f, 0.08f));
            detailHintText.alignment = TextAnchor.MiddleLeft;
            SetRect(detailHintText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(172f, -92f), new Vector2(-40f, 38f));

            var scrollRoot = CreatePanel("DetailScrollView", detailViewRoot, new Color(1f, 1f, 1f, 0.01f));
            StretchTo(scrollRoot, 34f, 144f, 34f, 38f);

            var viewport = CreatePanel("Viewport", scrollRoot, new Color(1f, 1f, 1f, 0.01f));
            StretchTo(viewport, 0f, 0f, 0f, 0f);
            var mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            detailContent = CreateRect("DetailContent", viewport);
            detailContent.anchorMin = new Vector2(0f, 1f);
            detailContent.anchorMax = new Vector2(1f, 1f);
            detailContent.pivot = new Vector2(0.5f, 1f);
            detailContent.anchoredPosition = Vector2.zero;
            detailContent.sizeDelta = Vector2.zero;

            var layout = detailContent.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 14f;
            layout.padding = new RectOffset(16, 16, 16, 24);

            var fitter = detailContent.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            detailScrollRect = scrollRoot.gameObject.AddComponent<ScrollRect>();
            detailScrollRect.viewport = viewport;
            detailScrollRect.content = detailContent;
            detailScrollRect.horizontal = false;
            detailScrollRect.vertical = true;
            detailScrollRect.movementType = ScrollRect.MovementType.Clamped;
            detailScrollRect.scrollSensitivity = 30f;
        }

        private void RefreshCards()
        {
            for (var i = cardContent.childCount - 1; i >= 0; i--)
            {
                Destroy(cardContent.GetChild(i).gameObject);
            }

            cardRects.Clear();
            UpdateCategoryButtonState();

            var filtered = new List<ComponentEncyclopediaEntry>();
            foreach (var entry in entries)
            {
                if (entry == null || !MatchesCategory(entry) || !MatchesSearch(entry))
                {
                    continue;
                }

                filtered.Add(entry);
            }

            var viewportWidth = cardScrollRect != null && cardScrollRect.viewport != null
                ? cardScrollRect.viewport.rect.width
                : 0f;
            if (viewportWidth <= 1f)
            {
                viewportWidth = Mathf.Max(CardWidth, Screen.width - SidebarWidth - 140f);
            }

            var columns = Mathf.Max(1, Mathf.FloorToInt((viewportWidth + CardGapX) / (CardWidth + CardGapX)));
            cardGridLayout.constraintCount = columns;

            for (var i = 0; i < filtered.Count; i++)
            {
                var card = CreateCard(filtered[i]);
                cardRects.Add(card);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(cardContent);
            cardContent.anchoredPosition = Vector2.zero;
            if (cardScrollRect != null)
            {
                cardScrollRect.verticalNormalizedPosition = 1f;
            }

            emptyText.gameObject.SetActive(filtered.Count == 0);
        }

        private RectTransform CreateCard(ComponentEncyclopediaEntry entry)
        {
            var card = CreatePanel("ComponentCard_" + entry.DefinitionName, cardContent, Color.white);
            card.sizeDelta = new Vector2(CardWidth, CardHeight);
            var cardLayout = card.gameObject.AddComponent<LayoutElement>();
            cardLayout.preferredWidth = CardWidth;
            cardLayout.preferredHeight = CardHeight;
            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card.GetComponent<Image>();
            var outline = card.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.88f, 0.91f, 0.95f, 1f);
            outline.effectDistance = new Vector2(1f, -1f);
            button.onClick.AddListener(() => ShowDetail(entry));

            var thumbnailArea = CreatePanel("ThumbnailArea", card, new Color(0.97f, 0.98f, 1f, 1f));
            SetRect(thumbnailArea, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(125f, -28f));
            var mask = thumbnailArea.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = true;

            var image = CreateRect("ThumbnailImage", thumbnailArea);
            var imageComponent = image.gameObject.AddComponent<Image>();
            var sprite = ResolveIcon(entry.Definition);
            imageComponent.sprite = sprite != null ? sprite : GetFallbackSprite();
            imageComponent.color = sprite != null ? Color.white : new Color(0.86f, 0.90f, 0.96f, 1f);
            imageComponent.preserveAspect = true;
            imageComponent.raycastTarget = false;
            
            SetRect(image, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(105f, 125f));

            var infoArea = CreateRect("InfoArea", card);
            SetRect(infoArea, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(0f, 0f));
            infoArea.offsetMin = new Vector2(150f, 16f);
            infoArea.offsetMax = new Vector2(-16f, -18f);

            var infoLayout = infoArea.gameObject.AddComponent<VerticalLayoutGroup>();
            infoLayout.childAlignment = TextAnchor.MiddleLeft;
            infoLayout.childControlWidth = true;
            infoLayout.childControlHeight = true;
            infoLayout.childForceExpandWidth = true;
            infoLayout.childForceExpandHeight = false;
            infoLayout.spacing = 6f;

            var name = CreateText("NameText", infoArea, entry.DisplayName, 16, FontStyle.Bold, new Color(0.07f, 0.11f, 0.18f));
            name.alignment = TextAnchor.MiddleLeft;
            name.horizontalOverflow = HorizontalWrapMode.Wrap;
            name.verticalOverflow = VerticalWrapMode.Overflow;

            var category = CreateText("CategoryText", infoArea, entry.Category, 12, FontStyle.Normal, new Color(0.35f, 0.42f, 0.52f));
            category.alignment = TextAnchor.MiddleLeft;
            category.horizontalOverflow = HorizontalWrapMode.Wrap;
            category.verticalOverflow = VerticalWrapMode.Overflow;

            var param = CreateText("ParamText", infoArea, BuildBasicSummary(entry), 12, FontStyle.Normal, new Color(0.08f, 0.32f, 0.60f));
            param.alignment = TextAnchor.MiddleLeft;
            param.horizontalOverflow = HorizontalWrapMode.Wrap;
            param.verticalOverflow = VerticalWrapMode.Overflow;

            var terminals = CreateText("TerminalText", infoArea, BuildTerminalSummary(entry.Definition, entry.Terminals), 12, FontStyle.Normal, new Color(0.18f, 0.24f, 0.32f));
            terminals.alignment = TextAnchor.MiddleLeft;
            terminals.horizontalOverflow = HorizontalWrapMode.Wrap;
            terminals.verticalOverflow = VerticalWrapMode.Overflow;

            return card;
        }

        private void ShowDetail(ComponentEncyclopediaEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            listViewRoot.gameObject.SetActive(false);
            detailViewRoot.gameObject.SetActive(true);
            detailHintText.text = entry.ContentCompleted ? string.Empty : "该元件百科内容仍在完善中。";

            for (var i = detailContent.childCount - 1; i >= 0; i--)
            {
                Destroy(detailContent.GetChild(i).gameObject);
            }

            CreateDetailHeader(entry);
            CreateSection("元件用途", entry.Purpose);
            CreateSection("端子说明", entry.TerminalDescription);
            CreateSection("工作状态", entry.WorkingState);
            CreateSection("常见接线方式", entry.WiringUsage);
            CreateSection("常见错误", entry.CommonMistakes);
            CreateSection("本系统中的仿真规则", entry.SimulationRule);
            CreateSection("安全提示", entry.SafetyTips);

            LayoutRebuilder.ForceRebuildLayoutImmediate(detailContent);
            detailContent.anchoredPosition = Vector2.zero;
            if (detailScrollRect != null)
            {
                detailScrollRect.verticalNormalizedPosition = 1f;
            }
        }

        private void ShowListView()
        {
            detailViewRoot.gameObject.SetActive(false);
            listViewRoot.gameObject.SetActive(true);
            RefreshCards();
        }

        private void CreateDetailHeader(ComponentEncyclopediaEntry entry)
        {
            var header = CreatePanel("HeaderCard", detailContent, Color.white);
            AddSoftOutline(header);
            var headerLayoutElement = header.gameObject.AddComponent<LayoutElement>();
            headerLayoutElement.preferredHeight = 258f;
            headerLayoutElement.minHeight = 238f;

            var headerLayout = header.gameObject.AddComponent<HorizontalLayoutGroup>();
            headerLayout.padding = new RectOffset(24, 24, 20, 20);
            headerLayout.spacing = 28f;
            headerLayout.childAlignment = TextAnchor.MiddleLeft;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = false;
            headerLayout.childForceExpandHeight = false;

            var imageArea = CreatePanel("ImagePanel", header, new Color(0.97f, 0.98f, 1f, 1f));
            var imageLayout = imageArea.gameObject.AddComponent<LayoutElement>();
            imageLayout.preferredWidth = 210f;
            imageLayout.preferredHeight = 210f;
            imageLayout.minWidth = 180f;
            imageLayout.minHeight = 180f;
            var image = imageArea.GetComponent<Image>();
            var sprite = ResolveIcon(entry.Definition);
            image.sprite = sprite != null ? sprite : GetFallbackSprite();
            image.color = sprite != null ? Color.white : new Color(0.86f, 0.90f, 0.96f, 1f);
            image.preserveAspect = true;
            image.raycastTarget = false;

            var infoPanel = CreateRect("BasicInfoPanel", header);
            var infoLayoutElement = infoPanel.gameObject.AddComponent<LayoutElement>();
            infoLayoutElement.flexibleWidth = 1f;
            infoLayoutElement.preferredHeight = 210f;
            var infoLayout = infoPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            infoLayout.childAlignment = TextAnchor.UpperLeft;
            infoLayout.childControlWidth = true;
            infoLayout.childControlHeight = true;
            infoLayout.childForceExpandWidth = true;
            infoLayout.childForceExpandHeight = false;
            infoLayout.spacing = 6f;

            var title = CreateText("NameText", infoPanel, entry.DisplayName, 24, FontStyle.Bold, new Color(0.07f, 0.11f, 0.18f));
            title.alignment = TextAnchor.MiddleLeft;
            title.verticalOverflow = VerticalWrapMode.Overflow;
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 38f;

            AddDetailLine(infoPanel, "分类：" + entry.Category);
            AddDetailLine(infoPanel, "适用电路：" + entry.CircuitType);
            AddDetailLine(infoPanel, "额定电压：" + Fallback(entry.RatedVoltage));
            AddDetailLine(infoPanel, "额定电流：" + Fallback(entry.RatedCurrent));
            AddDetailLine(infoPanel, "端子：" + BuildTerminalSummary(entry.Definition, entry.Terminals), 52f);
        }

        private void AddDetailLine(Transform parent, string text, float preferredHeight = 24f)
        {
            var line = CreateText("InfoLine", parent, text, 15, FontStyle.Normal, new Color(0.15f, 0.20f, 0.29f));
            line.alignment = TextAnchor.UpperLeft;
            line.verticalOverflow = VerticalWrapMode.Overflow;
            line.horizontalOverflow = HorizontalWrapMode.Wrap;
            line.gameObject.AddComponent<LayoutElement>().preferredHeight = preferredHeight;
        }

        private void CreateSection(string title, string body)
        {
            body = string.IsNullOrWhiteSpace(body) ? "该部分内容待补充。" : body;
            var card = CreatePanel("Section_" + title, detailContent, Color.white);
            AddSoftOutline(card);

            var cardLayoutElement = card.gameObject.AddComponent<LayoutElement>();
            cardLayoutElement.minHeight = 112f;
            cardLayoutElement.preferredHeight = Mathf.Clamp(96f + Mathf.CeilToInt(body.Length / 34f) * 22f, 128f, 260f);

            var layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 14, 16);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var titleText = CreateText("Title", card, title, 18, FontStyle.Bold, new Color(0.07f, 0.11f, 0.18f));
            titleText.alignment = TextAnchor.MiddleLeft;
            titleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;

            var bodyText = CreateText("Body", card, body, 15, FontStyle.Normal, new Color(0.20f, 0.26f, 0.35f));
            bodyText.alignment = TextAnchor.UpperLeft;
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyText.verticalOverflow = VerticalWrapMode.Overflow;
            bodyText.gameObject.AddComponent<LayoutElement>().preferredHeight = Mathf.Clamp(44f + Mathf.CeilToInt(body.Length / 34f) * 22f, 56f, 190f);
        }

        private bool MatchesCategory(ComponentEncyclopediaEntry entry)
        {
            if (entry == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(selectedCategory) ||
                string.Equals(selectedCategory, CategoryAll, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(selectedCategory, "All", StringComparison.OrdinalIgnoreCase) ||
                selectedCategory == "全部")
            {
                return true;
            }

            return string.Equals(entry.Category, selectedCategory, StringComparison.OrdinalIgnoreCase);
        }

        private bool MatchesSearch(ComponentEncyclopediaEntry entry)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return true;
            }

            var query = searchText.Trim();
            return Contains(entry.DisplayName, query) ||
                   Contains(entry.Category, query) ||
                   Contains(entry.Purpose, query) ||
                   Contains(entry.TerminalDescription, query) ||
                   Contains(BuildTerminalSummary(entry.Definition, entry.Terminals), query);
        }

        private void UpdateCategoryButtonState()
        {
            for (var i = 0; i < categoryButtons.Count; i++)
            {
                var active = i < categories.Length && categories[i] == selectedCategory;
                var image = categoryButtons[i].GetComponent<Image>();
                if (image != null)
                {
                    image.color = active ? new Color(0.89f, 0.94f, 1f) : new Color(0.96f, 0.98f, 1f);
                }

                if (i < categoryLabels.Count && categoryLabels[i] != null)
                {
                    categoryLabels[i].color = active ? new Color(0.06f, 0.38f, 0.95f) : new Color(0.18f, 0.24f, 0.32f);
                    categoryLabels[i].fontStyle = active ? FontStyle.Bold : FontStyle.Normal;
                }
            }
        }

        private static string BuildBasicSummary(ComponentEncyclopediaEntry entry)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(entry.RatedVoltage))
            {
                parts.Add("电压 " + entry.RatedVoltage);
            }

            if (!string.IsNullOrWhiteSpace(entry.RatedCurrent))
            {
                parts.Add("电流 " + entry.RatedCurrent);
            }

            return parts.Count > 0 ? string.Join(" / ", parts) : "参数：待补充";
        }

        private static string BuildDetailBasicInfo(ComponentEncyclopediaEntry entry)
        {
            return "名称：" + entry.DisplayName +
                   "\n分类：" + entry.Category +
                   "\n适用电路：" + entry.CircuitType +
                   "\n额定电压：" + Fallback(entry.RatedVoltage) +
                   "\n额定电流：" + Fallback(entry.RatedCurrent) +
                   "\n端子：" + BuildTerminalSummary(entry.Definition, entry.Terminals);
        }

        private static string BuildTerminalSummary(ComponentDefinition definition, string[] fallbackTerminals)
        {
            var parts = new List<string>();
            if (definition != null && definition.terminals != null)
            {
                for (var i = 0; i < definition.terminals.Count; i++)
                {
                    var terminal = definition.terminals[i];
                    if (terminal != null)
                    {
                        parts.Add(string.IsNullOrWhiteSpace(terminal.label) ? terminal.id : terminal.label);
                    }
                }
            }

            if (parts.Count == 0 && fallbackTerminals != null)
            {
                parts.AddRange(fallbackTerminals);
            }

            if (parts.Count == 0)
                return "端子：暂无端子";

            if (parts.Count <= 6)
                return "端子：" + string.Join("、", parts);

            var firstSix = new List<string>();
            for (var i = 0; i < 6; i++) firstSix.Add(parts[i]);
            
            return "端子：共 " + parts.Count + " 个，" + string.Join("、", firstSix) + " 等";
        }

        private static string Fallback(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "待补充" : value;
        }

        private static bool Contains(string value, string query)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   !string.IsNullOrWhiteSpace(query) &&
                   value.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string GetDisplayName(ComponentDefinition definition)
        {
            return definition == null || string.IsNullOrWhiteSpace(definition.displayName)
                ? definition != null ? definition.name : string.Empty
                : definition.displayName.Replace("\n", " ");
        }

        private Sprite ResolveIcon(ComponentDefinition definition)
        {
            if (definition == null)
            {
                return null;
            }

#if UNITY_EDITOR
            if (definition.name.IndexOf("Contactor_KM", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var kmSprite = LoadSpriteAtPath("Assets/Art/Components/Contactor_KM_380V_Default.png");
                if (kmSprite != null)
                {
                    return kmSprite;
                }
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

        private Sprite GetFallbackSprite()
        {
            if (fallbackSprite != null)
            {
                return fallbackSprite;
            }

            var texture = new Texture2D(96, 96, TextureFormat.RGBA32, false);
            var clear = new Color(0f, 0f, 0f, 0f);
            var fill = new Color(0.82f, 0.88f, 0.96f, 1f);
            var border = new Color(0.54f, 0.62f, 0.74f, 1f);
            for (var y = 0; y < 96; y++)
            {
                for (var x = 0; x < 96; x++)
                {
                    var inside = x > 12 && x < 84 && y > 12 && y < 84;
                    var edge = x > 8 && x < 88 && y > 8 && y < 88;
                    texture.SetPixel(x, y, inside ? fill : edge ? border : clear);
                }
            }

            texture.Apply();
            fallbackSprite = Sprite.Create(texture, new Rect(0f, 0f, 96f, 96f), new Vector2(0.5f, 0.5f), 96f);
            return fallbackSprite;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static RectTransform CreatePanel(string name, Transform parent, Color color)
        {
            var rect = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            var image = rect.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            return rect;
        }

        private static Button CreateButton(string name, Transform parent, string text, Color color, Color textColor, int fontSize)
        {
            var rect = CreatePanel(name, parent, color);
            var button = rect.gameObject.AddComponent<Button>();
            var label = CreateText("Text", rect, text, fontSize, FontStyle.Normal, textColor);
            label.alignment = TextAnchor.MiddleCenter;
            SetRect(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            return button;
        }

        private static Text CreateText(string name, Transform parent, string value, int size, FontStyle style, Color color)
        {
            var text = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            text.transform.SetParent(parent, false);
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static void AddSoftOutline(RectTransform rect)
        {
            var outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.88f, 0.91f, 0.95f, 1f);
            outline.effectDistance = new Vector2(1f, -1f);
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }

        private static void StretchTo(RectTransform rect, float left, float top, float right, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private sealed class ComponentEncyclopediaEntry
        {
            public ComponentDefinition Definition;
            public string DefinitionName;
            public string DisplayName;
            public string Category;
            public string CircuitType;
            public string RatedVoltage;
            public string RatedCurrent;
            public string[] Terminals;
            public string Purpose;
            public string TerminalDescription;
            public string WorkingState;
            public string WiringUsage;
            public string CommonMistakes;
            public string SimulationRule;
            public string SafetyTips;
            public bool ContentCompleted;
        }

        private static class ComponentEncyclopediaDatabase
        {
            public static List<ComponentEncyclopediaEntry> Build(List<ComponentDefinition> definitions)
            {
                var overrides = BuildOverrides();
                var result = new List<ComponentEncyclopediaEntry>();
                foreach (var definition in definitions)
                {
                    if (definition == null)
                    {
                        continue;
                    }

                    var entry = CreateDefaultEntry(definition);
                    foreach (var pair in overrides)
                    {
                        if (definition.name.IndexOf(pair.Key, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            ApplyOverride(entry, pair.Value);
                            break;
                        }
                    }

                    result.Add(entry);
                }

                result.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.Ordinal));
                return result;
            }

            private static ComponentEncyclopediaEntry CreateDefaultEntry(ComponentDefinition definition)
            {
                var category = ResolveCategory(definition);
                return new ComponentEncyclopediaEntry
                {
                    Definition = definition,
                    DefinitionName = definition.name,
                    DisplayName = GetDisplayName(definition),
                    Category = category,
                    CircuitType = ResolveCircuitType(definition.category),
                    RatedVoltage = ResolveRatedVoltage(definition),
                    RatedCurrent = definition.ratedCurrent > 0f ? definition.ratedCurrent.ToString("0.##") + "A" : string.Empty,
                    Terminals = null,
                    Purpose = "该部分内容待补充。",
                    TerminalDescription = "该元件端子以当前 Definition 配置为准，端子列表见基本信息。",
                    WorkingState = "该部分内容待补充。",
                    WiringUsage = "该部分内容待补充。",
                    CommonMistakes = "该部分内容待补充。",
                    SimulationRule = definition.canParticipateInRuntime ? "本系统中该元件可参与仿真运行，具体行为以当前电路连接和元件状态为准。" : "该元件当前主要用于展示或辅助操作。",
                    SafetyTips = "实际接线前应确认元件额定电压、电流和端子功能。",
                    ContentCompleted = false
                };
            }

            private static Dictionary<string, ComponentEncyclopediaEntry> BuildOverrides()
            {
                return new Dictionary<string, ComponentEncyclopediaEntry>(StringComparer.OrdinalIgnoreCase)
                {
                    {
                        "Contactor_KM",
                        new ComponentEncyclopediaEntry
                        {
                            DisplayName = "交流接触器",
                            Category = "控制元件",
                            CircuitType = "工业电路",
                            RatedVoltage = "220V / 380V",
                            RatedCurrent = "按元件参数设置",
                            Terminals = new[] { "1/L1", "3/L2", "5/L3", "2/T1", "4/T2", "6/T3", "A1", "A2", "13NO", "14NO", "21NC", "22NC" },
                            Purpose = "交流接触器用于通过控制回路控制主回路通断，常用于电动机启停、连续运行、正反转、星三角启动等工业控制电路。",
                            TerminalDescription = "A1、A2 是线圈端子；1/L1、3/L2、5/L3 是主触点进线端；2/T1、4/T2、6/T3 是主触点出线端；13NO、14NO 是常开辅助触点；21NC、22NC 是常闭辅助触点。",
                            WorkingState = "线圈未得电时，主触点断开，13-14 断开，21-22 导通。线圈得电后，主触点闭合，13-14 闭合，21-22 断开。",
                            WiringUsage = "在连续运行控制中，13-14 常用于自锁。在正反转控制中，21-22 常用于互锁。主触点用于控制电动机主回路。",
                            CommonMistakes = "不要把 A1/A2 当作主回路端子；不要把 13-14 和 21-22 混用；正反转电路中不能让正转和反转接触器同时吸合。",
                            SimulationRule = "本系统中，当 A1/A2 线圈形成有效控制回路时，接触器进入吸合状态，主触点和辅助触点会随线圈状态自动切换。",
                            SafetyTips = "接触器主回路和控制回路应区分接线，实际电气接线中必须确认电压等级和负载容量。",
                            ContentCompleted = true
                        }
                    },
                    {
                        "Timer_OnDelay",
                        new ComponentEncyclopediaEntry
                        {
                            DisplayName = "通电延时时间继电器",
                            Category = "控制元件",
                            CircuitType = "工业电路",
                            RatedVoltage = "220V / 380V",
                            Terminals = new[] { "A1", "A2", "15", "16", "18" },
                            Purpose = "时间继电器用于在控制回路中实现延时动作。通电延时时间继电器在线圈得电后开始计时，达到设定时间后触点状态发生变化。",
                            TerminalDescription = "A1、A2 是线圈端子；15 是公共端；16 是延时常闭端；18 是延时常开端。",
                            WorkingState = "未通电或未到设定时间时，15-16 导通，15-18 断开。达到设定延时时间后，15-16 断开，15-18 导通。",
                            WiringUsage = "常用于两台电机顺序启动、星三角降压启动等需要延时切换的控制电路。",
                            CommonMistakes = "不要把 16 和 18 接反；不要忽略时间继电器的延时过程；星三角电路中不能让星形接触器和三角接触器同时吸合。",
                            SimulationRule = "本系统中，时间继电器线圈得电后进入计时状态，显示屏会显示倒计时或运行时间，达到设定时间后触点自动切换。",
                            SafetyTips = "实际电路中应根据负载启动特性设置合理延时时间，避免切换过早或过晚。",
                            ContentCompleted = true
                        }
                    },
                    {
                        "EmergencyStop",
                        new ComponentEncyclopediaEntry
                        {
                            DisplayName = "急停按钮",
                            Category = "保护设备",
                            CircuitType = "工业电路",
                            RatedVoltage = "220V",
                            Terminals = new[] { "11", "12" },
                            Purpose = "急停按钮用于紧急情况下快速切断控制回路，防止设备继续运行，属于重要安全保护元件。",
                            TerminalDescription = "11、12 为常闭触点。正常状态下 11-12 导通，按下急停后 11-12 断开。",
                            WorkingState = "正常状态下急停按钮保持导通。按下后按钮锁定，控制回路断开，需要复位后才能恢复导通。",
                            WiringUsage = "急停按钮通常串联在控制回路电源入口或停止回路中，用于切断接触器线圈供电。",
                            CommonMistakes = "不要把急停按钮当作普通停止按钮。普通停止按钮松开后恢复，急停按钮按下后会锁定。不要把急停按钮接成常开启动功能。",
                            SimulationRule = "本系统中，急停按钮按下后 11-12 断开，相关控制回路失电，接触器线圈应释放，电机或负载停止运行。",
                            SafetyTips = "实际设备中急停按钮应安装在明显、易操作的位置，并定期检查复位和触点状态。",
                            ContentCompleted = true
                        }
                    },
                    {
                        "ThermalRelay",
                        new ComponentEncyclopediaEntry
                        {
                            DisplayName = "热继电器",
                            Category = "保护设备",
                            CircuitType = "工业电路",
                            RatedVoltage = "380V 主回路 / 220V 控制回路",
                            Terminals = new[] { "L1", "L2", "L3", "T1", "T2", "T3", "95", "96", "97", "98" },
                            Purpose = "热继电器用于电动机过载保护。当电机长时间过载时，热继电器动作，断开控制回路，保护电机。",
                            TerminalDescription = "L1/L2/L3 与 T1/T2/T3 是主回路端子；95-96 是常闭辅助触点；97-98 是常开辅助触点。",
                            WorkingState = "正常状态下主回路导通，95-96 导通，97-98 断开。过载动作后，95-96 断开，97-98 闭合。",
                            WiringUsage = "热继电器通常接在接触器之后、电动机之前。95-96 常串联在接触器线圈控制回路中。",
                            CommonMistakes = "不要只接主回路而忽略 95-96 控制回路保护；不要把 97-98 当作停止保护触点；不要把热继电器当作短路保护元件。",
                            SimulationRule = "本系统中，热继电器跳闸后 95-96 断开，控制回路失电，相关接触器释放。",
                            SafetyTips = "实际电路中热继电器整定电流应根据电机额定电流设置。",
                            ContentCompleted = true
                        }
                    },
                    {
                        "LimitSwitch",
                        new ComponentEncyclopediaEntry
                        {
                            DisplayName = "行程开关 SQ",
                            Category = "传感元件",
                            CircuitType = "工业电路",
                            RatedVoltage = "220V",
                            Terminals = new[] { "11", "12", "23", "24" },
                            Purpose = "行程开关用于检测机械位置或运动到位状态，常用于自动往返、限位保护等控制电路。",
                            TerminalDescription = "11-12 通常为常闭触点，23-24 通常为常开触点。",
                            WorkingState = "未触发时，11-12 导通，23-24 断开。触发后，11-12 断开，23-24 导通。",
                            WiringUsage = "自动往返电路中，行程开关用于在运动到端点时切换接触器状态，实现换向。",
                            CommonMistakes = "不要把行程开关理解为自锁按钮。行程开关由机械位置触发，离开触发位置后通常会复位。",
                            SimulationRule = "本系统中，自动往返模板会根据虚拟位置触发 SQ，SQ 触点状态随位置自动变化。",
                            SafetyTips = "实际设备中行程开关安装位置需要可靠，避免机械越位或误触发。",
                            ContentCompleted = true
                        }
                    }
                };
            }

            private static void ApplyOverride(ComponentEncyclopediaEntry target, ComponentEncyclopediaEntry source)
            {
                target.DisplayName = source.DisplayName;
                target.Category = source.Category;
                target.CircuitType = source.CircuitType;
                target.RatedVoltage = source.RatedVoltage;
                target.RatedCurrent = source.RatedCurrent;
                target.Terminals = source.Terminals;
                target.Purpose = source.Purpose;
                target.TerminalDescription = source.TerminalDescription;
                target.WorkingState = source.WorkingState;
                target.WiringUsage = source.WiringUsage;
                target.CommonMistakes = source.CommonMistakes;
                target.SimulationRule = source.SimulationRule;
                target.SafetyTips = source.SafetyTips;
                target.ContentCompleted = source.ContentCompleted;
            }

            private static string ResolveCategory(ComponentDefinition definition)
            {
                var name = definition.name ?? string.Empty;
                if (name.IndexOf("EmergencyStop", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    definition.kind == ComponentKind.Fuse ||
                    definition.kind == ComponentKind.Breaker)
                {
                    return "保护设备";
                }

                if (name.IndexOf("LimitSwitch", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return "传感元件";
                }

                switch (definition.kind)
                {
                    case ComponentKind.Switch:
                    case ComponentKind.TwoWaySwitch:
                    case ComponentKind.PushButton:
                        return "开关按钮";
                    case ComponentKind.PowerSource:
                    case ComponentKind.EnergyMeter:
                    case ComponentKind.Instrument:
                        return "电源仪表";
                    case ComponentKind.Lamp:
                    case ComponentKind.Fan:
                    case ComponentKind.Motor:
                    case ComponentKind.Indicator:
                        return "用电设备";
                    case ComponentKind.ContactorCoil:
                        return "控制元件";
                    case ComponentKind.TerminalBlock:
                        return "端子与模块";
                    default:
                        return "其他";
                }
            }

            private static string ResolveCircuitType(ComponentCategory category)
            {
                switch (category)
                {
                    case ComponentCategory.Household:
                        return "家庭电路";
                    case ComponentCategory.Industrial:
                        return "工业电路";
                    case ComponentCategory.Measurement:
                        return "通用 / 测量";
                    default:
                        return "通用";
                }
            }

            private static string ResolveRatedVoltage(ComponentDefinition definition)
            {
                if (definition.sourcePhaseCount >= 3 && definition.sourceLineVoltage > 0f)
                {
                    return definition.sourceLineVoltage.ToString("0") + "V";
                }

                if (definition.sourceVoltage > 0f)
                {
                    return definition.sourceVoltage.ToString("0") + "V";
                }

                if (definition.ratedVoltage > 0f)
                {
                    return definition.ratedVoltage.ToString("0") + "V";
                }

                return string.Empty;
            }
        }
    }
}
