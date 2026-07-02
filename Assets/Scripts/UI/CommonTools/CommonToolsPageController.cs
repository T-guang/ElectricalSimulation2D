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

        private RectTransform contentRoot;
        private RectTransform resistorPanel;
        private RectTransform formulaPanel;
        private RectTransform articlePanel;
        private RectTransform resistorPreview;
        private Text resistorResultText;
        private Text formulaTitleText;
        private Text formulaExpressionText;
        private Text formulaVariablesText;
        private Text formulaUseCaseText;
        private Text formulaExampleText;
        private Text formulaNoteText;
        private Text articleTitleText;
        private Text articleContentText;

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

            AddToolTab(sidebar, ToolTab.Resistor, "电阻色环识别", 0);
            AddToolTab(sidebar, ToolTab.Formula, "电路公式", 1);
            AddToolTab(sidebar, ToolTab.Article, "基础资料", 2);
        }

        private void AddToolTab(Transform parent, ToolTab tab, string label, int index)
        {
            var buttonRect = CreatePanel("ToolTab_" + tab, parent, new Color(0.96f, 0.98f, 1f));
            SetRect(buttonRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(14f, -76f - index * 56f), new Vector2(-28f, 44f));
            var button = buttonRect.gameObject.AddComponent<Button>();
            var text = CreateText("Text", buttonRect, label, 17, FontStyle.Normal, new Color(0.18f, 0.24f, 0.32f));
            text.alignment = TextAnchor.MiddleLeft;
            SetRect(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(16f, 0f), new Vector2(-24f, 0f));
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
            SetRect(resistorPreview, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(28f, -86f), new Vector2(-28f, 156f));
            AddSoftOutline(resistorPreview);

            BuildResistorPreview();

            resistorResultText = CreateText("ResultText", resistorPanel, string.Empty, 24, FontStyle.Bold, PrimaryBlue);
            resistorResultText.alignment = TextAnchor.MiddleCenter;
            SetRect(resistorResultText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(28f, -260f), new Vector2(-28f, 42f));

            BuildBandSelector();
            BuildColorSelector();
            RefreshResistorTool();
        }

        private void BuildResistorPreview()
        {
            var body = CreatePanel("ResistorBody", resistorPreview, new Color(0.87f, 0.74f, 0.50f));
            SetRect(body, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 68f));

            CreateLead("LeadLeft", new Vector2(-370f, 0f), new Vector2(210f, 8f));
            CreateLead("LeadRight", new Vector2(370f, 0f), new Vector2(210f, 8f));

            for (var i = 0; i < 5; i++)
            {
                var band = CreatePanel("Band" + (i + 1), resistorPreview, Color.black);
                SetRect(band, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-150f + i * 75f, 0f), new Vector2(28f, 86f));
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
            SetRect(bandTitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(34f, -326f), new Vector2(220f, 34f));

            for (var i = 0; i < 5; i++)
            {
                var captured = i;
                var button = CreateButton("BandSelector_" + i, resistorPanel, "第" + (i + 1) + "环", new Color(0.94f, 0.97f, 1f), TextDark, 15);
                SetRect(button.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(34f + i * 92f, -366f), new Vector2(82f, 34f));
                button.onClick.AddListener(() =>
                {
                    selectedBandIndex = captured;
                    RefreshResistorTool();
                });
                bandButtons.Add(button);
                bandLabels.Add(button.GetComponentInChildren<Text>());
            }
        }

        private void BuildColorSelector()
        {
            var title = CreateText("ColorSelectorTitle", resistorPanel, "颜色", 17, FontStyle.Bold, TextDark);
            title.alignment = TextAnchor.MiddleLeft;
            SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(34f, -430f), new Vector2(120f, 34f));

            for (var i = 0; i < resistorColors.Count; i++)
            {
                var entry = resistorColors[i];
                var row = i / 6;
                var col = i % 6;
                var button = CreateButton("Color_" + entry.Name, resistorPanel, entry.Name, entry.Color, GetReadableTextColor(entry.Color), 15);
                SetRect(button.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(34f + col * 92f, -472f - row * 48f), new Vector2(78f, 34f));
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
            var detailContent = BuildScrollableContent(detailPanel, "FormulaDetailContent", 760f);

            for (var i = 0; i < formulas.Count; i++)
            {
                var captured = formulas[i];
                var button = CreateListButton(listPanel, captured.Title, i);
                button.onClick.AddListener(() => ShowFormula(captured));
            }

            formulaTitleText = CreateDetailText(detailContent, "FormulaTitle", 26, FontStyle.Bold, TextDark, -24f, 48f);
            formulaExpressionText = CreateDetailText(detailContent, "FormulaExpression", 28, FontStyle.Bold, PrimaryBlue, -86f, 54f);
            formulaVariablesText = CreateDetailText(detailContent, "FormulaVariables", 16, FontStyle.Normal, TextDark, -158f, 90f);
            formulaUseCaseText = CreateDetailText(detailContent, "FormulaUseCase", 16, FontStyle.Normal, TextDark, -260f, 86f);
            formulaExampleText = CreateDetailText(detailContent, "FormulaExample", 16, FontStyle.Normal, TextDark, -360f, 86f);
            formulaNoteText = CreateDetailText(detailContent, "FormulaNote", 16, FontStyle.Normal, TextMuted, -460f, 86f);
            ShowFormula(formulas.Count > 0 ? formulas[0] : null);
        }

        private void BuildArticlePanel()
        {
            articlePanel = CreateRect("ArticlePanel", contentRoot);
            StretchTo(articlePanel, 0f, 0f, 0f, 0f);

            var listPanel = BuildListPanel(articlePanel, "ArticleList", "资料目录");
            var detailPanel = BuildDetailPanel(articlePanel, "ArticleDetail", 300f);
            var detailContent = BuildScrollableContent(detailPanel, "ArticleDetailContent", 900f);

            for (var i = 0; i < articles.Count; i++)
            {
                var captured = articles[i];
                var button = CreateListButton(listPanel, captured.Title, i);
                button.onClick.AddListener(() => ShowArticle(captured));
            }

            articleTitleText = CreateDetailText(detailContent, "ArticleTitle", 26, FontStyle.Bold, TextDark, -24f, 52f);
            articleContentText = CreateDetailText(detailContent, "ArticleContent", 17, FontStyle.Normal, TextDark, -94f, 740f);
            articleContentText.verticalOverflow = VerticalWrapMode.Overflow;
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

                if (i < bandButtons.Count)
                {
                    bandButtons[i].gameObject.SetActive(i < visibleBands);
                    var active = selectedBandIndex == i;
                    bandButtons[i].GetComponent<Image>().color = active ? new Color(0.89f, 0.94f, 1f) : new Color(0.96f, 0.98f, 1f);
                    bandLabels[i].color = active ? PrimaryBlue : TextDark;
                    bandLabels[i].fontStyle = active ? FontStyle.Bold : FontStyle.Normal;
                }
            }

            for (var i = 0; i < colorButtons.Count; i++)
            {
                colorButtons[i].interactable = IsColorAllowedForBand(i, selectedBandIndex);
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

            return FormatResistance(significant * multiplier) + " " + tolerance;
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

        private static string FormatNumber(double value)
        {
            return value.ToString(value >= 10d || Math.Abs(value - Math.Round(value)) < 0.0001d ? "0.##" : "0.###", CultureInfo.InvariantCulture);
        }

        private void ShowFormula(CommonFormulaEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            formulaTitleText.text = entry.Title;
            formulaExpressionText.text = entry.Expression;
            formulaVariablesText.text = "变量说明\n" + entry.Variables;
            formulaUseCaseText.text = "适用场景\n" + entry.UseCase;
            formulaExampleText.text = "示例\n" + entry.Example;
            formulaNoteText.text = "注意\n" + entry.Note;
        }

        private void ShowArticle(CommonArticleEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            articleTitleText.text = entry.Title + "  ·  " + entry.Category;
            articleContentText.text = entry.Content;
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
