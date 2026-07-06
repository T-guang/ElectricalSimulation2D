using UnityEngine;
using UnityEngine.UI;

namespace ElectricalSim.UI
{
    public static class MainUiTheme
    {
        public const float NavBarHeight = 60f;
        public const float ToolbarHeight = 60f;
        public const float LeftPanelWidth = 380f;
        public const float RightPanelWidth = 320f;
        public const float ToolbarButtonHeight = 36f;

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

        private static Font mainFont;

        public static Font MainFont
        {
            get
            {
                if (mainFont == null)
                {
                    mainFont = Resources.Load<Font>("Fonts/MaokenFengyaSong");
                    if (mainFont == null)
                    {
                        mainFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    }
                }

                return mainFont;
            }
        }

        public static void ApplyText(Text text, int size, FontStyle style, Color color, TextAnchor alignment)
        {
            if (text == null)
            {
                return;
            }

            text.font = MainFont;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
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
