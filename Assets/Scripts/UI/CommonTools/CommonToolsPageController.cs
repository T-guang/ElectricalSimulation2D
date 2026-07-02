using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace ElectricalSim.UI.CommonTools
{
    public sealed class CommonToolsPageController : MonoBehaviour
    {
        private enum ToolTab
        {
            Resistor,
            Formula,
            Article
        }

        private readonly List<Button> tabButtons = new List<Button>();
        private readonly List<Text> tabLabels = new List<Text>();
        private readonly List<Button> bandButtons = new List<Button>();
        private readonly List<Text> bandLabels = new List<Text>();
        private readonly List<Button> colorButtons = new List<Button>();
        private readonly List<Outline> bandOutlines = new List<Outline>();

        private RectTransform contentRoot;
        private RectTransform resistorPanel;
        private RectTransform formulaPanel;
        private RectTransform articlePanel;
        private RectTransform resistorPreview;
        private Text resistorResultText;
        private Text bandSummaryText;
        private RectTransform formulaDetailContent;
        private RectTransform articleDetailContent;

        private List<ResistorColorEntry> resistorColors;
        private List<CommonFormulaEntry> formulas;
        private List<CommonArticleEntry> articles;

        private readonly int[] bandColorIndices = { 1, 2, 0, 10, 10 };
        private bool fiveBandMode;
        private int selectedBandIndex;
        private bool built;

        private static readonly Color PageBackground = new Color(0.96f, 0.98f, 1f);
        private static readonly Color CardBackground = Color.white;
        private static readonly Color PrimaryBlue = new Color(0.15f, 0.39f, 0.92f);
        private static readonly Color TextDark = new Color(0.07f, 0.11f, 0.18f);
        private static readonly Color TextMuted = new Color(0.35f, 0.42f, 0.52f);

        private void OnEnable()
        {
            if (!built)
            {
                BuildPage();
                built = true;
            }
        }

        public void BuildPage()
        {
            ClearChildren();
            resistorColors = CommonToolsSeedData.GetResistorColors();
            formulas = CommonToolsSeedData.GetFormulas();
            articles = CommonToolsSeedData.GetArticles();

            var background = gameObject.GetComponent<Image>();
            if (background == null)
            {
                background = gameObject.AddComponent<Image>();
            }

            background.color = PageBackground;
            background.raycastTarget = true;

            BuildHeader();
            BuildSidebar();
            BuildContentRoot();
            BuildResistorPanel();
            BuildFormulaPanel();
            BuildArticlePanel();
            SelectTool(ToolTab.Resistor);
        }

        private void BuildHeader()
        {
            var title = CreateText("ToolsTitle", transform, "常用工具", 32, FontStyle.Bold, TextDark);
            title.alignment = TextAnchor.MiddleLeft;
            SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(34f, -30f), new Vector2(260f, 54f));

            var desc = CreateText("ToolsDescription", transform, "提供常见电工计算、色环识别和基础资料速查。", 18, FontStyle.Normal, TextMuted);
            desc.alignment = TextAnchor.MiddleLeft;
            SetRect(desc.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(300f, -34f), new Vector2(-80f, 48f));
        }

        private void BuildSidebar()
        {
            var sidebar = CreatePanel("ToolSidebar", transform, CardBackground);
            SetRect(sidebar, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(34f, -104f), new Vector2(230f, -132f));
            AddSoftOutline(sidebar);

            var title = CreateText("SidebarTitle", sidebar, "工具分类", 22, FontStyle.Bold, TextDark);
            title.alignment = TextAnchor.MiddleLeft;
            SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(20f, -18f), new Vector2(-40f, 40f));

            var categoryList = CreateRect("CategoryList", sidebar);
            SetRect(categoryList, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(0f, -60f));

            var layout = categoryList.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 8f;
            layout.padding = new RectOffset(14, 14, 16, 16);

            AddToolTab(categoryList, ToolTab.Resistor, "电阻色环识别");
            AddToolTab(categoryList, ToolTab.Formula, "电路公式");
            AddToolTab(categoryList, ToolTab.Article, "基础资料");
        }

        private void AddToolTab(Transform parent, ToolTab tab, string label)
        {
            var buttonRect = CreatePanel("ToolTab_" + tab, parent, new Color(0.96f, 0.98f, 1f));
            var layoutElement = buttonRect.gameObject.AddComponent<LayoutElement>();
            layoutElement.minHeight = 44f;
            layoutElement.preferredHeight = 44f;

            var button = buttonRect.gameObject.AddComponent<Button>();
            var text = CreateText("Text", buttonRect, label, 17, FontStyle.Normal, new Color(0.18f, 0.24f, 0.32f));
            text.alignment = TextAnchor.MiddleCenter;
            SetRect(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            button.onClick.AddListener(() => SelectTool(tab));
            tabButtons.Add(button);
            tabLabels.Add(text);
        }

        private void BuildContentRoot()
        {
            contentRoot = CreatePanel("ToolContentRoot", transform, CardBackground);
            StretchTo(contentRoot, 292f, 104f, 34f, 32f);
            AddSoftOutline(contentRoot);
        }

        private void BuildResistorPanel()
        {
            resistorPanel = CreateRect("ResistorColorPanel", contentRoot);
            StretchTo(resistorPanel, 0f, 0f, 0f, 0f);

            var modeTitle = CreateText("ModeTitle", resistorPanel, "电阻色环识别", 24, FontStyle.Bold, TextDark);
            modeTitle.alignment = TextAnchor.MiddleLeft;
            SetRect(modeTitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -22f), new Vector2(220f, 44f));

            var fourButton = CreateButton("FourBandButton", resistorPanel, "四色环", new Color(0.89f, 0.94f, 1f), PrimaryBlue, 16);
            SetRect(fourButton.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-204f, -24f), new Vector2(86f, 36f));
            fourButton.onClick.AddListener(() => SetBandMode(false));

            var fiveButton = CreateButton("FiveBandButton", resistorPanel, "五色环", new Color(0.94f, 0.97f, 1f), TextDark, 16);
            SetRect(fiveButton.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-106f, -24f), new Vector2(86f, 36f));
            fiveButton.onClick.AddListener(() => SetBandMode(true));

            resistorPreview = CreatePanel("ResistorPreview", resistorPanel, new Color(0.97f, 0.98f, 1f));
            SetRect(resistorPreview, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(28f, -76f), new Vector2(-28f, 180f));
            AddSoftOutline(resistorPreview);

            BuildResistorPreview();

            resistorResultText = CreateText("ResultText", resistorPanel, string.Empty, 24, FontStyle.Bold, PrimaryBlue);
            resistorResultText.alignment = TextAnchor.MiddleCenter;
            resistorResultText.verticalOverflow = VerticalWrapMode.Overflow;
            SetRect(resistorResultText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(28f, -268f), new Vector2(-28f, 86f));

            BuildBandSelector();
            BuildExampleButtons();
            BuildColorSelector();
            RefreshResistorTool();
        }

        private void BuildResistorPreview()
        {
            CreateLead("LeadLeft", new Vector2(-380f, 0f), new Vector2(230f, 8f));
            CreateLead("LeadRight", new Vector2(380f, 0f), new Vector2(230f, 8f));

            var body = CreatePanel("ResistorBody", resistorPreview, new Color(0.86f, 0.72f, 0.48f));
            SetRect(body, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(540f, 86f));
            AddSoftOutline(body);

            var leftCap = CreatePanel("CapLeft", resistorPreview, new Color(0.78f, 0.62f, 0.38f));
            SetRect(leftCap, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-272f, 0f), new Vector2(46f, 104f));

            var rightCap = CreatePanel("CapRight", resistorPreview, new Color(0.78f, 0.62f, 0.38f));
            SetRect(rightCap, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(272f, 0f), new Vector2(46f, 104f));

            var highlight = CreatePanel("BodyHighlight", resistorPreview, new Color(1f, 0.92f, 0.70f, 0.72f));
            SetRect(highlight, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 22f), new Vector2(470f, 16f));

            for (var i = 0; i < 5; i++)
            {
                var band = CreatePanel("Band" + (i + 1), resistorPreview, Color.black);
                SetRect(band, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-165f + i * 78f, 0f), new Vector2(24f, 104f));
                var outline = band.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0.15f, 0.39f, 0.92f, 0f);
                outline.effectDistance = new Vector2(3f, -3f);
                bandOutlines.Add(outline);
            }
        }

        private void CreateLead(string name, Vector2 position, Vector2 size)
        {
            var lead = CreatePanel(name, resistorPreview, new Color(0.70f, 0.74f, 0.80f));
            SetRect(lead, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);
        }

        private void BuildBandSelector()
        {
            var bandTitle = CreateText("BandSelectorTitle", resistorPanel, "选择要编辑的色环", 17, FontStyle.Bold, TextDark);
            bandTitle.alignment = TextAnchor.MiddleLeft;
            SetRect(bandTitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(34f, -362f), new Vector2(220f, 34f));

            for (var i = 0; i < 5; i++)
            {
                var captured = i;
                var button = CreateButton("BandSelector_" + i, resistorPanel, "第" + (i + 1) + "环", new Color(0.94f, 0.97f, 1f), TextDark, 15);
                SetRect(button.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(34f + i * 96f, -398f), new Vector2(86f, 36f));
                button.onClick.AddListener(() =>
                {
                    selectedBandIndex = captured;
                    RefreshResistorTool();
                });
                bandButtons.Add(button);
                bandLabels.Add(button.GetComponentInChildren<Text>());
            }

            bandSummaryText = CreateText("BandSummary", resistorPanel, string.Empty, 15, FontStyle.Normal, TextMuted);
            bandSummaryText.alignment = TextAnchor.MiddleLeft;
            bandSummaryText.horizontalOverflow = HorizontalWrapMode.Wrap;
            SetRect(bandSummaryText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(34f, -438f), new Vector2(-68f, 34f));
        }

        private void BuildExampleButtons()
        {
            var title = CreateText("ExampleTitle", resistorPanel, "常用示例", 17, FontStyle.Bold, TextDark);
            title.alignment = TextAnchor.MiddleLeft;
            SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(34f, -478f), new Vector2(120f, 32f));

            AddExampleButton("10 Ω", 0, 1, 0, 0);
            AddExampleButton("100 Ω", 1, 1, 0, 1);
            AddExampleButton("220 Ω", 2, 2, 2, 1);
            AddExampleButton("1 kΩ", 3, 1, 0, 2);
            AddExampleButton("4.7 kΩ", 4, 4, 7, 2);
            AddExampleButton("10 kΩ", 5, 1, 0, 3);
            AddExampleButton("100 kΩ", 6, 1, 0, 4);
            AddExampleButton("1 MΩ", 7, 1, 0, 5);
        }

        private void AddExampleButton(string label, int index, int band1, int band2, int multiplier)
        {
            var button = CreateButton("Example_" + index, resistorPanel, label, new Color(0.96f, 0.98f, 1f), TextDark, 14);
            SetRect(button.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(34f + index * 82f, -514f), new Vector2(74f, 30f));
            button.onClick.AddListener(() =>
            {
                fiveBandMode = false;
                selectedBandIndex = 0;
                bandColorIndices[0] = band1;
                bandColorIndices[1] = band2;
                bandColorIndices[2] = multiplier;
                bandColorIndices[3] = 10;
                bandColorIndices[4] = 10;
                RefreshResistorTool();
            });
        }

        private void BuildColorSelector()
        {
            var title = CreateText("ColorSelectorTitle", resistorPanel, "颜色", 17, FontStyle.Bold, TextDark);
            title.alignment = TextAnchor.MiddleLeft;
            SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(34f, -558f), new Vector2(120f, 34f));

            for (var i = 0; i < resistorColors.Count; i++)
            {
                var entry = resistorColors[i];
                var button = CreateButton("Color_" + entry.Name, resistorPanel, entry.Name, entry.Color, GetReadableTextColor(entry.Color), 15);
                SetRect(button.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(104f, 42f));
                var captured = i;
                button.onClick.AddListener(() => SetSelectedBandColor(captured));
                colorButtons.Add(button);
            }
        }

        private void BuildFormulaPanel()
        {
            formulaPanel = CreateRect("FormulaPanel", contentRoot);
            StretchTo(formulaPanel, 0f, 0f, 0f, 0f);

            var listPanel = BuildListPanel(formulaPanel, "FormulaList", "公式列表");
            var detailPanel = BuildDetailPanel(formulaPanel, "FormulaDetail", 300f);
            formulaDetailContent = BuildScrollableContent(detailPanel, "FormulaDetailContent", 760f);

            for (var i = 0; i < formulas.Count; i++)
            {
                var captured = formulas[i];
                var button = CreateListButton(listPanel, captured.Title, i);
                button.onClick.AddListener(() => ShowFormula(captured));
            }

            ShowFormula(formulas.Count > 0 ? formulas[0] : null);
        }

        private void BuildArticlePanel()
        {
            articlePanel = CreateRect("ArticlePanel", contentRoot);
            StretchTo(articlePanel, 0f, 0f, 0f, 0f);

            var listPanel = BuildListPanel(articlePanel, "ArticleList", "资料目录");
            var detailPanel = BuildDetailPanel(articlePanel, "ArticleDetail", 300f);
            articleDetailContent = BuildScrollableContent(detailPanel, "ArticleDetailContent", 900f);

            for (var i = 0; i < articles.Count; i++)
            {
                var captured = articles[i];
                var button = CreateListButton(listPanel, captured.Title, i);
                button.onClick.AddListener(() => ShowArticle(captured));
            }

            ShowArticle(articles.Count > 0 ? articles[0] : null);
        }

        private RectTransform BuildListPanel(RectTransform parent, string name, string title)
        {
            var panel = CreatePanel(name, parent, new Color(0.97f, 0.98f, 1f));
            SetRect(panel, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(28f, -24f), new Vector2(250f, -48f));
            AddSoftOutline(panel);
            var titleText = CreateText("Title", panel, title, 20, FontStyle.Bold, TextDark);
            titleText.alignment = TextAnchor.MiddleLeft;
            SetRect(titleText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(18f, -12f), new Vector2(-36f, 36f));
            return panel;
        }

        private RectTransform BuildDetailPanel(RectTransform parent, string name, float left)
        {
            var panel = CreatePanel(name, parent, CardBackground);
            StretchTo(panel, left, 24f, 28f, 24f);
            AddSoftOutline(panel);
            return panel;
        }

        private RectTransform BuildScrollableContent(RectTransform parent, string name, float contentHeight)
        {
            var viewport = CreatePanel(name + "Viewport", parent, new Color(1f, 1f, 1f, 0.01f));
            StretchTo(viewport, 0f, 0f, 0f, 0f);
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;

            var content = CreateRect(name, viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, contentHeight);

            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 14f;
            layout.padding = new RectOffset(18, 18, 18, 24);

            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = parent.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28f;
            return content;
        }

        private Button CreateListButton(RectTransform parent, string title, int index)
        {
            var button = CreateButton("ListItem_" + index, parent, title, Color.white, TextDark, 15);
            SetRect(button.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(14f, -64f - index * 44f), new Vector2(-28f, 36f));
            var label = button.GetComponentInChildren<Text>();
            label.alignment = TextAnchor.MiddleLeft;
            return button;
        }

        private Text CreateDetailText(RectTransform parent, string name, int fontSize, FontStyle style, Color color, float topY, float height)
        {
            var text = CreateText(name, parent, string.Empty, fontSize, style, color);
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            SetRect(text.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(28f, topY), new Vector2(-56f, height));
            return text;
        }

        private void SelectTool(ToolTab tab)
        {
            resistorPanel.gameObject.SetActive(tab == ToolTab.Resistor);
            formulaPanel.gameObject.SetActive(tab == ToolTab.Formula);
            articlePanel.gameObject.SetActive(tab == ToolTab.Article);

            for (var i = 0; i < tabButtons.Count; i++)
            {
                var active = (int)tab == i;
                var image = tabButtons[i].GetComponent<Image>();
                image.color = active ? new Color(0.89f, 0.94f, 1f) : new Color(0.96f, 0.98f, 1f);
                tabLabels[i].fontStyle = active ? FontStyle.Bold : FontStyle.Normal;
                tabLabels[i].color = active ? PrimaryBlue : new Color(0.18f, 0.24f, 0.32f);
            }
        }

        private void SetBandMode(bool useFiveBands)
        {
            fiveBandMode = useFiveBands;
            if (!fiveBandMode && selectedBandIndex > 3)
            {
                selectedBandIndex = 3;
            }

            RefreshResistorTool();
        }

        private void SetSelectedBandColor(int colorIndex)
        {
            if (!IsColorAllowedForBand(colorIndex, selectedBandIndex))
            {
                return;
            }

            bandColorIndices[selectedBandIndex] = colorIndex;
            RefreshResistorTool();
        }

        private void RefreshResistorTool()
        {
            var visibleBands = fiveBandMode ? 5 : 4;
            for (var i = 0; i < 5; i++)
            {
                var band = resistorPreview.Find("Band" + (i + 1));
                if (band != null)
                {
                    band.gameObject.SetActive(i < visibleBands);
                    band.GetComponent<Image>().color = resistorColors[bandColorIndices[i]].Color;
                }

                if (i < bandOutlines.Count)
                {
                    bandOutlines[i].effectColor = selectedBandIndex == i && i < visibleBands
                        ? new Color(0.15f, 0.39f, 0.92f, 0.85f)
                        : new Color(0.15f, 0.39f, 0.92f, 0f);
                }

                if (i < bandButtons.Count)
                {
                    bandButtons[i].gameObject.SetActive(i < visibleBands);
                    var active = selectedBandIndex == i;
                    bandButtons[i].GetComponent<Image>().color = active ? new Color(0.89f, 0.94f, 1f) : new Color(0.96f, 0.98f, 1f);
                    bandLabels[i].color = active ? PrimaryBlue : TextDark;
                    bandLabels[i].fontStyle = active ? FontStyle.Bold : FontStyle.Normal;
                    bandLabels[i].text = "第" + (i + 1) + "环\n" + GetBandRoleName(i);
                }
            }

            var visibleColorIndex = 0;
            for (var i = 0; i < colorButtons.Count; i++)
            {
                var allowed = IsColorAllowedForBand(i, selectedBandIndex);
                var button = colorButtons[i];
                button.gameObject.SetActive(allowed);
                button.interactable = allowed;
                if (allowed)
                {
                    var row = visibleColorIndex / 6;
                    var col = visibleColorIndex % 6;
                    SetRect(button.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(34f + col * 104f, -594f - row * 46f), new Vector2(94f, 38f));
                    var label = button.GetComponentInChildren<Text>();
                    label.text = GetColorButtonLabel(resistorColors[i], selectedBandIndex);
                    label.fontSize = 13;
                    visibleColorIndex++;
                }
            }

            if (bandSummaryText != null)
            {
                bandSummaryText.text = GetBandSummary();
            }

            resistorResultText.text = CalculateResistanceText();
        }

        private bool IsColorAllowedForBand(int colorIndex, int bandIndex)
        {
            var color = resistorColors[colorIndex];
            var multiplierBand = fiveBandMode ? 3 : 2;
            var toleranceBand = fiveBandMode ? 4 : 3;
            if (bandIndex == toleranceBand)
            {
                return color.HasTolerance;
            }

            if (bandIndex == multiplierBand)
            {
                return color.HasMultiplier;
            }

            return color.HasDigit;
        }

        private string CalculateResistanceText()
        {
            double significant;
            double multiplier;
            string tolerance;

            if (fiveBandMode)
            {
                significant = resistorColors[bandColorIndices[0]].Digit.Value * 100 +
                              resistorColors[bandColorIndices[1]].Digit.Value * 10 +
                              resistorColors[bandColorIndices[2]].Digit.Value;
                multiplier = resistorColors[bandColorIndices[3]].Multiplier.Value;
                tolerance = resistorColors[bandColorIndices[4]].Tolerance;
            }
            else
            {
                significant = resistorColors[bandColorIndices[0]].Digit.Value * 10 +
                              resistorColors[bandColorIndices[1]].Digit.Value;
                multiplier = resistorColors[bandColorIndices[2]].Multiplier.Value;
                tolerance = resistorColors[bandColorIndices[3]].Tolerance;
            }

            var ohms = significant * multiplier;
            return "阻值：" + FormatResistance(ohms) + " " + tolerance + "\n" +
                   "计算：" + FormatNumber(significant) + " × " + FormatMultiplier(multiplier) + " = " + FormatRawOhms(ohms) + "\n" +
                   "色环：" + GetBandColorNames();
        }

        private static string FormatResistance(double value)
        {
            if (value >= 1000000d)
            {
                return FormatNumber(value / 1000000d) + " MΩ";
            }

            if (value >= 1000d)
            {
                return FormatNumber(value / 1000d) + " kΩ";
            }

            return FormatNumber(value) + " Ω";
        }

        private static string FormatRawOhms(double value)
        {
            return FormatNumber(value) + " Ω";
        }

        private static string FormatMultiplier(double value)
        {
            return value >= 1d ? FormatNumber(value) : value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private static string FormatNumber(double value)
        {
            return value.ToString(value >= 10d || Math.Abs(value - Math.Round(value)) < 0.0001d ? "0.##" : "0.###", CultureInfo.InvariantCulture);
        }

        private string GetBandRoleName(int bandIndex)
        {
            var multiplierBand = fiveBandMode ? 3 : 2;
            var toleranceBand = fiveBandMode ? 4 : 3;
            if (bandIndex == toleranceBand)
            {
                return "误差";
            }

            if (bandIndex == multiplierBand)
            {
                return "倍率";
            }

            return "数字";
        }

        private string GetColorButtonLabel(ResistorColorEntry color, int bandIndex)
        {
            var role = GetBandRoleName(bandIndex);
            if (role == "误差")
            {
                return color.Name + "\n" + color.Tolerance;
            }

            if (role == "倍率")
            {
                return color.Name + "\n×" + FormatMultiplier(color.Multiplier.Value);
            }

            return color.Name + "\n数字 " + color.Digit.Value;
        }

        private string GetBandSummary()
        {
            var visibleBands = fiveBandMode ? 5 : 4;
            var parts = new List<string>();
            for (var i = 0; i < visibleBands; i++)
            {
                parts.Add("第" + (i + 1) + "环：" + resistorColors[bandColorIndices[i]].Name + "（" + GetBandRoleName(i) + "）");
            }

            return string.Join("    ", parts.ToArray());
        }

        private string GetBandColorNames()
        {
            var visibleBands = fiveBandMode ? 5 : 4;
            var names = new List<string>();
            for (var i = 0; i < visibleBands; i++)
            {
                names.Add(resistorColors[bandColorIndices[i]].Name);
            }

            return string.Join("、", names.ToArray());
        }

        private void ShowFormula(CommonFormulaEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            ClearChildren(formulaDetailContent);
            CreateInfoCard(formulaDetailContent, entry.Title, "分类：" + entry.Category, false, 22);
            CreateInfoCard(formulaDetailContent, "核心公式", JoinLines(entry.Expressions), true, 20);
            CreateInfoCard(formulaDetailContent, "常见变形", JoinLines(entry.Variants), true, 18);
            CreateInfoCard(formulaDetailContent, "变量说明", entry.Variables, false, 16);
            CreateInfoCard(formulaDetailContent, "单位说明", entry.UnitDescription, false, 16);
            CreateInfoCard(formulaDetailContent, "适用场景", entry.UseCase, false, 16);
            CreateInfoCard(formulaDetailContent, "计算示例", entry.Example, false, 16);
            CreateInfoCard(formulaDetailContent, "常见错误", entry.CommonMistakes, false, 16);
            CreateInfoCard(formulaDetailContent, "注意事项", entry.Note, false, 16);
        }

        private void ShowArticle(CommonArticleEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            ClearChildren(articleDetailContent);
            CreateInfoCard(articleDetailContent, entry.Title, "分类：" + entry.Category + "\n学习目标：" + entry.LearningGoal, false, 22);
            for (var i = 0; i < entry.Sections.Count; i++)
            {
                CreateInfoCard(articleDetailContent, entry.Sections[i].Heading, entry.Sections[i].Body, false, 16);
            }

            CreateInfoCard(articleDetailContent, "关键概念", entry.KeyPoints, false, 16);
            CreateInfoCard(articleDetailContent, "常见误区", entry.CommonMistakes, false, 16);
            CreateInfoCard(articleDetailContent, "与本系统的关系", entry.RelationToSystem, false, 16);
        }

        private RectTransform CreateInfoCard(RectTransform parent, string title, string body, bool formulaStyle, int titleSize)
        {
            var card = CreatePanel("InfoCard_" + title, parent, Color.white);
            AddSoftOutline(card);

            var layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 8f;
            layout.padding = new RectOffset(20, 20, 14, 16);

            var fitter = card.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var layoutElement = card.gameObject.AddComponent<LayoutElement>();
            layoutElement.minHeight = formulaStyle ? 92f : 110f;

            var titleText = CreateText("Title", card, title, titleSize, FontStyle.Bold, TextDark);
            titleText.alignment = TextAnchor.UpperLeft;
            titleText.verticalOverflow = VerticalWrapMode.Overflow;
            titleText.gameObject.AddComponent<LayoutElement>().preferredHeight = titleSize + 10f;

            var bodyText = CreateText("Body", card, string.IsNullOrWhiteSpace(body) ? "该部分内容正在完善中。" : body, formulaStyle ? 19 : 15, formulaStyle ? FontStyle.Bold : FontStyle.Normal, formulaStyle ? PrimaryBlue : TextDark);
            bodyText.alignment = TextAnchor.UpperLeft;
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyText.verticalOverflow = VerticalWrapMode.Overflow;
            var bodyElement = bodyText.gameObject.AddComponent<LayoutElement>();
            bodyElement.minHeight = formulaStyle ? 44f : 56f;

            return card;
        }

        private static string JoinLines(List<string> values)
        {
            if (values == null || values.Count == 0)
            {
                return string.Empty;
            }

            return string.Join("\n", values.ToArray());
        }

        private void ClearChildren()
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }

            tabButtons.Clear();
            tabLabels.Clear();
            bandButtons.Clear();
            bandLabels.Clear();
            colorButtons.Clear();
            bandOutlines.Clear();
        }

        private static void ClearChildren(RectTransform root)
        {
            if (root == null)
            {
                return;
            }

            for (var i = root.childCount - 1; i >= 0; i--)
            {
                Destroy(root.GetChild(i).gameObject);
            }
        }

        private static Color GetReadableTextColor(Color background)
        {
            var luminance = background.r * 0.299f + background.g * 0.587f + background.b * 0.114f;
            return luminance > 0.62f ? TextDark : Color.white;
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
    }
}
