using UnityEngine;
using UnityEngine.UI;

namespace ElectricalSim.UI
{
    public static class MainUiTheme
    {
        public enum UiTextRole
        {
            BrandTitle,
            NavText,
            NavTextSelected,
            ToolbarPrimaryButton,
            ToolbarButton,
            ToolbarDangerButton,
            ToolbarLabel,
            ToolbarFileButton,
            DeveloperToolButton,
            SectionTitle,
            SubsectionTitle,
            FilterText,
            FilterTextSelected,
            PaletteCardTitle,
            LogTitle,
            LogBody,
            InspectorTitle,
            InspectorButton,
            InspectorCardTitle,
            InspectorBody
        }

        public const float NavBarHeight = 72f;
        public const float ToolbarHeight = 64f;
        public const float MainContentTop = NavBarHeight + ToolbarHeight;
        public const float LeftPanelWidth = 380f;
        public const float RightPanelWidth = 320f;
        public const float ToolbarButtonHeight = 40f;

        public static readonly Color PageBackground = Hex("F8FBFF");
        public static readonly Color PanelBackground = Color.white;
        public static readonly Color Divider = Hex("E5E7EB");
        public static readonly Color PrimaryBlue = Hex("2563EB");
        public static readonly Color SelectedBlue = Hex("EAF2FF");
        public static readonly Color DeepText = Hex("111827");
        public static readonly Color NormalText = Hex("1F2937");
        public static readonly Color SecondaryText = Hex("334155");
        public static readonly Color MutedText = Hex("64748B");
        public static readonly Color DangerRed = Hex("DC2626");
        public static readonly Color DangerBorder = Hex("FCA5A5");
        public static readonly Color SuccessGreen = Hex("16A34A");
        public static readonly Color ToolbarButton = Hex("F8FAFC");
        public static readonly Color FilterButton = Hex("F1F5F9");
        public static readonly Color GridMinor = Hex("EAF0F7");
        public static readonly Color GridMajor = Hex("D7E2F0");

        private static Font titleFont;
        public static Font TitleFont
        {
            get
            {
                if (titleFont == null)
                {
                    titleFont = Resources.Load<Font>("Fonts/maoken_fengyasong") ??
                                Resources.Load<Font>("Fonts/MaokenFengyasong") ??
                                Resources.Load<Font>("Fonts/MaokenFengyaSong") ??
                                Resources.Load<Font>("Fonts/maoken") ??
                                Resources.Load<Font>("Fonts/Maoken") ??
                                Resources.Load<Font>("maoken_fengyasong") ??
                                Resources.Load<Font>("MaokenFengyasong") ??
                                Resources.Load<Font>("maoken") ??
                                Resources.Load<Font>("Maoken") ??
                                Resources.Load<Font>("UI/Fonts/Maoken");
                    if (titleFont == null)
                    {
                        titleFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    }
                }
                return titleFont;
            }
        }

        private static Font uiFont;
        public static Font UiFont
        {
            get
            {
                if (uiFont == null)
                {
                    uiFont = Resources.Load<Font>("Fonts/OppoSans-Regular");
                    if (uiFont == null)
                    {
                        var names = new[]
                        {
                            "Microsoft YaHei UI",
                            "Microsoft YaHei",
                            "Source Han Sans SC",
                            "Noto Sans CJK SC",
                            "Arial"
                        };
                        uiFont = Font.CreateDynamicFontFromOSFont(names, 16);
                    }
                    if (uiFont == null)
                    {
                        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    }
                }
                return uiFont;
            }
        }

        private static Font uiFontBold;
        public static Font UiFontBold
        {
            get
            {
                if (uiFontBold == null)
                {
                    uiFontBold = Resources.Load<Font>("Fonts/OppoSans-Bold");
                    if (uiFontBold == null)
                    {
                        uiFontBold = UiFont;
                    }
                }
                return uiFontBold;
            }
        }

        private static Font bodyFont;
        public static Font BodyFont
        {
            get
            {
                if (bodyFont == null)
                {
                    bodyFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }
                return bodyFont;
            }
        }

        public static void ApplyText(Text text, int size, FontStyle style, Color color, TextAnchor alignment, bool isTitle = false)
        {
            if (text == null)
            {
                return;
            }

            text.font = isTitle ? TitleFont : BodyFont;
            text.fontSize = isTitle ? size + 2 : size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.verticalOverflow = VerticalWrapMode.Overflow;
        }

        public static void ApplyTextRole(Text text, UiTextRole role)
        {
            ApplyTextRole(text, role, null);
        }

        public static void ApplyTextRole(Text text, UiTextRole role, Color? colorOverride)
        {
            if (text == null)
            {
                return;
            }

            var font = UiFont;
            var fontSize = 13;
            var fontStyle = FontStyle.Normal;
            var alignment = text.alignment;
            var lineSpacing = 1f;
            var horizontalOverflow = HorizontalWrapMode.Overflow;
            var verticalOverflow = VerticalWrapMode.Overflow;
            var resizeTextForBestFit = false;
            var resizeTextMinSize = 0;
            var resizeTextMaxSize = 0;

            switch (role)
            {
                case UiTextRole.BrandTitle:
                    font = TitleFont;
                    fontSize = 19;
                    fontStyle = FontStyle.Bold;
                    alignment = TextAnchor.MiddleLeft;
                    break;
                case UiTextRole.NavText:
                    font = UiFont;
                    fontSize = 15;
                    fontStyle = FontStyle.Normal;
                    alignment = TextAnchor.MiddleCenter;
                    break;
                case UiTextRole.NavTextSelected:
                    font = UiFontBold;
                    fontSize = 15;
                    fontStyle = FontStyle.Normal;
                    alignment = TextAnchor.MiddleCenter;
                    break;
                case UiTextRole.ToolbarPrimaryButton:
                    font = UiFontBold;
                    fontSize = 14;
                    fontStyle = FontStyle.Normal;
                    alignment = TextAnchor.MiddleCenter;
                    break;
                case UiTextRole.ToolbarButton:
                    font = UiFont;
                    fontSize = 13;
                    fontStyle = FontStyle.Normal;
                    alignment = TextAnchor.MiddleCenter;
                    break;
                case UiTextRole.ToolbarDangerButton:
                    font = UiFontBold;
                    fontSize = 13;
                    fontStyle = FontStyle.Normal;
                    alignment = TextAnchor.MiddleCenter;
                    break;
                case UiTextRole.ToolbarLabel:
                    font = UiFont;
                    fontSize = 13;
                    fontStyle = FontStyle.Normal;
                    alignment = TextAnchor.MiddleLeft;
                    break;
                case UiTextRole.ToolbarFileButton:
                    font = UiFont;
                    fontSize = 13;
                    fontStyle = FontStyle.Normal;
                    alignment = TextAnchor.MiddleCenter;
                    break;
                case UiTextRole.DeveloperToolButton:
                    font = UiFont;
                    fontSize = 13;
                    fontStyle = FontStyle.Normal;
                    alignment = TextAnchor.MiddleCenter;
                    break;
                case UiTextRole.SectionTitle:
                    font = UiFontBold;
                    fontSize = 17;
                    fontStyle = FontStyle.Normal;
                    alignment = TextAnchor.MiddleLeft;
                    break;
                case UiTextRole.SubsectionTitle:
                    font = UiFontBold;
                    fontSize = 14;
                    fontStyle = FontStyle.Normal;
                    alignment = TextAnchor.MiddleLeft;
                    break;
                case UiTextRole.FilterText:
                    font = UiFont;
                    fontSize = 13;
                    fontStyle = FontStyle.Normal;
                    break;
                case UiTextRole.FilterTextSelected:
                    font = UiFontBold;
                    fontSize = 13;
                    fontStyle = FontStyle.Normal;
                    break;
                case UiTextRole.PaletteCardTitle:
                    font = UiFont;
                    fontSize = 13;
                    fontStyle = FontStyle.Normal;
                    alignment = TextAnchor.MiddleCenter;
                    horizontalOverflow = HorizontalWrapMode.Wrap;
                    verticalOverflow = VerticalWrapMode.Truncate;
                    resizeTextForBestFit = true;
                    resizeTextMinSize = 12;
                    resizeTextMaxSize = 13;
                    break;
                case UiTextRole.LogTitle:
                    font = UiFontBold;
                    fontSize = 16;
                    fontStyle = FontStyle.Normal;
                    alignment = TextAnchor.MiddleLeft;
                    break;
                case UiTextRole.LogBody:
                    font = UiFont;
                    fontSize = 13;
                    fontStyle = FontStyle.Normal;
                    alignment = TextAnchor.UpperLeft;
                    lineSpacing = 1.25f;
                    horizontalOverflow = HorizontalWrapMode.Wrap;
                    verticalOverflow = VerticalWrapMode.Overflow;
                    break;
                case UiTextRole.InspectorTitle:
                    font = UiFontBold;
                    fontSize = 16;
                    fontStyle = FontStyle.Normal;
                    alignment = TextAnchor.MiddleLeft;
                    break;
                case UiTextRole.InspectorButton:
                    font = UiFontBold;
                    fontSize = 13;
                    fontStyle = FontStyle.Normal;
                    alignment = TextAnchor.MiddleCenter;
                    break;
                case UiTextRole.InspectorCardTitle:
                    font = UiFontBold;
                    fontSize = 15;
                    fontStyle = FontStyle.Normal;
                    alignment = TextAnchor.UpperLeft;
                    break;
                case UiTextRole.InspectorBody:
                    font = UiFont;
                    fontSize = 13;
                    fontStyle = FontStyle.Normal;
                    alignment = TextAnchor.UpperLeft;
                    lineSpacing = 1.3f;
                    horizontalOverflow = HorizontalWrapMode.Wrap;
                    verticalOverflow = VerticalWrapMode.Overflow;
                    break;
            }

            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.lineSpacing = lineSpacing;
            text.alignment = alignment;
            text.horizontalOverflow = horizontalOverflow;
            text.verticalOverflow = verticalOverflow;
            text.resizeTextForBestFit = resizeTextForBestFit;
            text.resizeTextMinSize = resizeTextMinSize > 0 ? resizeTextMinSize : fontSize;
            text.resizeTextMaxSize = resizeTextMaxSize > 0 ? resizeTextMaxSize : fontSize;

            if (colorOverride.HasValue)
            {
                text.color = colorOverride.Value;
            }
        }

        public static void StyleButton(Button button, Color background, Color textColor, Color borderColor, bool bold = false)
        {
            if (button == null)
            {
                return;
            }

            var image = button.GetComponent<Image>() ?? button.gameObject.AddComponent<Image>();
            image.sprite = UiThemeTokens.GetRoundedSprite(8);
            image.type = Image.Type.Sliced;
            image.color = background;
            image.raycastTarget = true;

            var outline = button.GetComponent<Outline>() ?? button.gameObject.AddComponent<Outline>();
            outline.effectColor = borderColor;
            outline.effectDistance = new Vector2(1f, -1f);
            outline.enabled = borderColor.a > 0f;

            var text = button.GetComponentInChildren<Text>(true);
            if (text != null)
            {
                ApplyText(text, 14, bold ? FontStyle.Bold : FontStyle.Normal, textColor, TextAnchor.MiddleCenter);
                text.resizeTextForBestFit = false;
                text.fontSize = 15;
            }

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.96f, 0.98f, 1f, 1f);
            colors.pressedColor = new Color(0.90f, 0.94f, 1f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.88f, 0.91f, 0.95f, 0.8f);
            button.colors = colors;
        }

        public static Color Hex(string hex)
        {
            if (ColorUtility.TryParseHtmlString("#" + hex, out var color))
            {
                return color;
            }

            return Color.white;
        }
    }
}
