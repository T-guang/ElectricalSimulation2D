using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ElectricalSim.Core;
using ElectricalSim.AI;

namespace ElectricalSim.UI
{
    public sealed class DemoUIController : MonoBehaviour
    {
        [SerializeField] private WorkspaceController workspace;
        [SerializeField] private SaveLoadService saveLoadService;
        [SerializeField] private Button startButton;
        [SerializeField] private Button clearWiresButton;
        [SerializeField] private Button clearAllButton;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button loadButton;
        [SerializeField] private Button importButton;
        [SerializeField] private SaveBlueprintDialog saveDialog;
        [SerializeField] private ImportBlueprintPanel importPanel;
        [SerializeField] private LocalInspectorPanel localInspectorPanel;
        [SerializeField] private Button undoButton;
        [SerializeField] private Button redoButton;
        [SerializeField] private Button quickDeleteButton;
        [SerializeField] private Button quickClearWiresButton;
        [SerializeField] private Button quickClearAllButton;
        [SerializeField] private Button lockButton;
        [SerializeField] private Dropdown wireStyleDropdown;
        [SerializeField] private List<Button> colorButtons = new List<Button>();

        private readonly List<Outline> colorButtonOutlines = new List<Outline>();
        private Color[] wirePaletteColors;
        private Color lastActiveWireColor;
        private bool lastActiveWireSelectionState;

        private void Awake()
        {
            ApplyMainFrameLayout();
            EnsureToolbarLayout();
            EnsureMainLogo();
            EnsureBlueprintPanels();
            EnsureLocalInspectorPanel();
            BindButton(startButton, ToggleSimulation);
            BindButton(clearWiresButton, workspace.ClearWires);
            BindButton(clearAllButton, workspace.ClearDrawing);
            BindButton(saveButton, OpenSaveDialog);
            BindButton(loadButton, OpenImportPanel);
            BindButton(undoButton, workspace.Undo);
            BindButton(redoButton, workspace.Redo);
            BindButton(quickDeleteButton, workspace.DeleteSelection);
            BindButton(lockButton, ToggleLock);

            if (wireStyleDropdown != null)
            {
                wireStyleDropdown.onValueChanged.AddListener(value => workspace.CurrentWireStyle = value == 0 ? WireStyle.Orthogonal : WireStyle.Straight);
            }

            wirePaletteColors = new[]
            {
                MainUiTheme.Hex("EF4444"),
                MainUiTheme.PrimaryBlue,
                MainUiTheme.SuccessGreen,
                MainUiTheme.Hex("FACC15")
            };

            colorButtonOutlines.Clear();
            for (var i = 0; i < colorButtons.Count && i < wirePaletteColors.Length; i++)
            {
                var color = wirePaletteColors[i];
                ConfigureWireColorButton(colorButtons[i], color);
                colorButtons[i].onClick.AddListener(() =>
                {
                    workspace.ApplyWirePaletteColor(color);
                    RefreshWireColorButtons(true);
                });
            }

            RefreshSimulationButtonLabel();
            RefreshLockButtonLabel();
            RefreshWireColorButtons(true);
        }

        private void LateUpdate()
        {
            RefreshWireColorButtons(false);
        }

        private void BindButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null || action == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        private void EnsureToolbarLayout()
        {
            var toolbar = startButton != null ? startButton.transform.parent as RectTransform : null;
            if (toolbar == null)
            {
                toolbar = transform as RectTransform;
            }

            if (toolbar == null)
            {
                return;
            }

            var toolbarImage = toolbar.GetComponent<Image>() ?? toolbar.gameObject.AddComponent<Image>();
            toolbarImage.color = MainUiTheme.PanelBackground;
            toolbarImage.raycastTarget = true;
            var toolbarOutline = toolbar.GetComponent<Outline>() ?? toolbar.gameObject.AddComponent<Outline>();
            toolbarOutline.effectColor = MainUiTheme.Divider;
            toolbarOutline.effectDistance = new Vector2(0f, -1f);

            var quickRoot = FindQuickToolRoot();

            undoButton = EnsureButton(toolbar, undoButton, "UndoButton", "撤销");
            redoButton = EnsureButton(toolbar, redoButton, "RedoButton", "重做");
            quickDeleteButton = EnsureButton(toolbar, quickDeleteButton, "DeleteSelectionButton", "删除");
            lockButton = EnsureButton(toolbar, lockButton, "InteractionLockButton", "锁定");

            var leftGroup = EnsureGroup(toolbar, "LeftActionGroup", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(790f, 60f), TextAnchor.MiddleLeft, 14f);
            var colorGroup = EnsureGroup(toolbar, "WireColorGroup", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(830f, 0f), new Vector2(260f, 60f), TextAnchor.MiddleLeft, 14f);
            var rightGroup = EnsureGroup(toolbar, "RightActionGroup", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-24f, 0f), new Vector2(400f, 60f), TextAnchor.MiddleRight, 14f);

            MoveButtonToGroup(startButton, leftGroup, new Vector2(128f, 40f), "开始仿真");
            MoveButtonToGroup(undoButton, leftGroup, new Vector2(92f, 40f), "撤销");
            MoveButtonToGroup(redoButton, leftGroup, new Vector2(92f, 40f), "重做");
            MoveButtonToGroup(quickDeleteButton, leftGroup, new Vector2(92f, 40f), "删除");
            MoveButtonToGroup(clearWiresButton, leftGroup, new Vector2(92f, 40f), "清线");
            MoveButtonToGroup(clearAllButton, leftGroup, new Vector2(92f, 40f), "清空");
            MoveButtonToGroup(lockButton, leftGroup, new Vector2(92f, 40f), "锁定");

            StyleToolbarButton(startButton, true, false);
            StyleToolbarButton(undoButton, false, false);
            StyleToolbarButton(redoButton, false, false);
            StyleToolbarButton(quickDeleteButton, false, true);
            StyleToolbarButton(clearWiresButton, false, true);
            StyleToolbarButton(clearAllButton, false, true);
            StyleToolbarButton(lockButton, false, false);

            SetToolbarButtonSize(startButton, 128f);
            SetToolbarButtonSize(undoButton, 92f);
            SetToolbarButtonSize(redoButton, 92f);
            SetToolbarButtonSize(quickDeleteButton, 92f);
            SetToolbarButtonSize(clearWiresButton, 92f);
            SetToolbarButtonSize(clearAllButton, 92f);
            SetToolbarButtonSize(lockButton, 92f);
            SetToolbarButtonSize(saveButton, 118f);
            SetToolbarButtonSize(loadButton, 118f);

            ApplyToolbarIcon(startButton, "ui_toolbar_start_32", 24f, false, true);
            ApplyToolbarIcon(undoButton, "ui_toolbar_undo_24", 24f);
            ApplyToolbarIcon(redoButton, "ui_toolbar_redo_24", 24f);
            ApplyToolbarIcon(quickDeleteButton, "ui_toolbar_delete_24", 24f, true);
            ApplyToolbarIcon(clearWiresButton, "ui_toolbar_clear_wire_24", 24f, true);
            ApplyToolbarIcon(clearAllButton, "ui_toolbar_clear_all_24", 24f, true);
            ApplyToolbarIcon(lockButton, "ui_toolbar_lock_24", 24f);
            
            if (saveButton != null)
            {
                MoveButtonToGroup(saveButton, rightGroup, new Vector2(118f, 40f), null); // null to keep original text "保存图纸"
                StyleToolbarButton(saveButton, false, false);
                ApplyToolbarIcon(saveButton, "ui_toolbar_save_blueprint_24", 24f);
                SetToolbarButtonSize(saveButton, 118f);
            }

            if (importButton == null)
            {
                var importGo = GameObject.Find("ImportButton") ?? GameObject.Find("ImportBlueprintButton") ?? GameObject.Find("ImportDrawingButton") ?? GameObject.Find("导入图纸");
                if (importGo != null) importButton = importGo.GetComponent<Button>();
            }

            if (importButton != null)
            {
                MoveButtonToGroup(importButton, rightGroup, new Vector2(118f, 40f), "导入图纸");
                StyleToolbarButton(importButton, false, false);
                ApplyToolbarIcon(importButton, "ui_toolbar_import_blueprint_24", 24f);
                SetToolbarButtonSize(importButton, 118f);
            }
            else if (loadButton != null)
            {
                // Fallback: restore previous logic but keep original text "导入图纸" and correct icon
                MoveButtonToGroup(loadButton, rightGroup, new Vector2(118f, 40f), null); 
                StyleToolbarButton(loadButton, false, false);
                ApplyToolbarIcon(loadButton, "ui_toolbar_import_blueprint_24", 24f);
                SetToolbarButtonSize(loadButton, 118f);
            }

            if (colorGroup.Find("WireColorLabel") == null)
            {
                var labelGo = new GameObject("WireColorLabel", typeof(RectTransform), typeof(Text));
                labelGo.transform.SetParent(colorGroup, false);
                labelGo.transform.SetAsFirstSibling();
                var labelText = labelGo.GetComponent<Text>();
                labelText.text = "导线颜色";
                MainUiTheme.ApplyText(labelText, 14, FontStyle.Bold, MainUiTheme.DeepText, TextAnchor.MiddleCenter, false);
                labelText.horizontalOverflow = HorizontalWrapMode.Overflow;
                var rt = labelText.rectTransform;
                rt.sizeDelta = new Vector2(72f, 28f);
            }

            for (var i = 0; i < colorButtons.Count; i++)
            {
                var button = colorButtons[i];
                if (button == null)
                {
                    continue;
                }

                MoveButtonToGroup(button, colorGroup, new Vector2(28f, 28f), null);
                var img = button.GetComponent<Image>();
                if (img != null)
                {
                    img.sprite = UiThemeTokens.GetRoundedSprite(6, 32);
                    img.type = Image.Type.Sliced;
                }
            }

            EnsureVerticalDivider(toolbar, "LeftToolbarDivider", 814f);
            EnsureVerticalDivider(toolbar, "ColorToolbarDivider", 1100f);

            if (quickRoot != null)
            {
                quickRoot.gameObject.SetActive(false);
            }

            if (quickClearWiresButton != null)
            {
                quickClearWiresButton.gameObject.SetActive(false);
            }

            if (quickClearAllButton != null)
            {
                quickClearAllButton.gameObject.SetActive(false);
            }
        }

        private void ApplyMainFrameLayout()
        {
            var navBar = GameObject.Find("NavBar")?.GetComponent<RectTransform>();
            if (navBar != null)
            {
                navBar.anchorMin = new Vector2(0f, 1f);
                navBar.anchorMax = new Vector2(1f, 1f);
                navBar.pivot = new Vector2(0.5f, 1f);
                navBar.anchoredPosition = Vector2.zero;
                navBar.sizeDelta = new Vector2(0f, MainUiTheme.NavBarHeight);
                var navImage = navBar.GetComponent<Image>() ?? navBar.gameObject.AddComponent<Image>();
                navImage.color = MainUiTheme.PanelBackground;
                var navOutline = navBar.GetComponent<Outline>() ?? navBar.gameObject.AddComponent<Outline>();
                navOutline.effectColor = MainUiTheme.Divider;
                navOutline.effectDistance = new Vector2(0f, -1f);
                ApplyFontsToChildren(navBar, 14);
                ApplyNavigationVisuals(navBar);
            }

            var topBar = GameObject.Find("TopBar")?.GetComponent<RectTransform>();
            if (topBar != null)
            {
                topBar.anchorMin = new Vector2(0f, 1f);
                topBar.anchorMax = new Vector2(1f, 1f);
                topBar.pivot = new Vector2(0.5f, 1f);
                topBar.anchoredPosition = new Vector2(0f, -MainUiTheme.NavBarHeight);
                topBar.sizeDelta = new Vector2(0f, 60f);
            }

            var workspaceRect = workspace != null ? workspace.WorkspaceRect : null;
            if (workspaceRect != null)
            {
                workspaceRect.offsetMax = new Vector2(workspaceRect.offsetMax.x, -(MainUiTheme.NavBarHeight + 60f));
                workspaceRect.offsetMin = new Vector2(workspaceRect.offsetMin.x, 0f);
                var workspaceImage = workspaceRect.GetComponent<Image>() ?? workspaceRect.gameObject.AddComponent<Image>();
                workspaceImage.color = MainUiTheme.PageBackground;
            }

            var palette = GameObject.Find("Palette")?.GetComponent<RectTransform>();
            if (palette != null)
            {
                palette.anchorMin = new Vector2(0f, 0f);
                palette.anchorMax = new Vector2(0f, 1f);
                palette.pivot = new Vector2(0f, 0.5f);
                palette.offsetMin = new Vector2(0f, 0f);
                palette.offsetMax = new Vector2(MainUiTheme.LeftPanelWidth, -(MainUiTheme.NavBarHeight + 60f));
            }
        }

        private void EnsureMainLogo()
        {
            var logoText = GameObject.Find("Logo")?.GetComponent<Text>();
            if (logoText == null)
            {
                return;
            }

            var sprite = UiIconLibrary.Load("Logo/ui_sidebar_yalong_logo_320") ?? UiIconLibrary.Load("ui_sidebar_yalong_logo_320");
            if (sprite == null)
            {
                return;
            }

            var titleValue = string.IsNullOrWhiteSpace(logoText.text) ? "电工数字学生仿真系统" : logoText.text;
            logoText.text = string.Empty;
            logoText.raycastTarget = false;

            var iconRect = logoText.transform.Find("LogoIcon") as RectTransform;
            if (iconRect == null)
            {
                var iconObject = new GameObject("LogoIcon", typeof(RectTransform), typeof(Image));
                iconObject.transform.SetParent(logoText.transform, false);
                iconRect = iconObject.GetComponent<RectTransform>();
            }

            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(18f, 10f);
            iconRect.sizeDelta = new Vector2(36f, 36f);

            var image = iconRect.GetComponent<Image>() ?? iconRect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.color = Color.white;
            image.raycastTarget = false;

            var titleRect = logoText.transform.Find("LogoTitle") as RectTransform;
            if (titleRect == null)
            {
                var titleObject = new GameObject("LogoTitle", typeof(RectTransform), typeof(Text));
                titleObject.transform.SetParent(logoText.transform, false);
                titleRect = titleObject.GetComponent<RectTransform>();
            }

            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = Vector2.one;
            titleRect.offsetMin = new Vector2(64f, 10f);
            titleRect.offsetMax = new Vector2(0f, 10f);

            var titleText = titleRect.GetComponent<Text>() ?? titleRect.gameObject.AddComponent<Text>();
            titleText.text = titleValue;
            MainUiTheme.ApplyText(titleText, 20, FontStyle.Bold, MainUiTheme.DeepText, TextAnchor.MiddleLeft, true);
            titleText.resizeTextForBestFit = false;
            titleText.raycastTarget = false;

            iconRect.SetAsFirstSibling();
        }

        private RectTransform FindQuickToolRoot()
        {
            var candidates = new[]
            {
                undoButton,
                redoButton,
                quickDeleteButton,
                quickClearWiresButton,
                quickClearAllButton,
                lockButton
            };

            foreach (var candidate in candidates)
            {
                var parent = candidate != null ? candidate.transform.parent as RectTransform : null;
                if (parent != null && parent.name.Contains("Quick"))
                {
                    return parent;
                }
            }

            return null;
        }

        private void ApplyToolbarIcon(Button button, string iconPath, float iconSize, bool danger = false, bool primary = false)
        {
            if (button == null)
            {
                return;
            }

            Color? color = null;
            if (primary) color = Color.white;
            else color = MainUiTheme.Hex("64748B");
            
            var icon = UiIconLibrary.EnsureButtonIcon(button, iconPath, new Vector2(iconSize, iconSize), new Vector2(18f, 0f), color);
            if (icon == null)
            {
                return;
            }

            var label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.rectTransform.anchorMin = Vector2.zero;
                label.rectTransform.anchorMax = Vector2.one;
                label.rectTransform.offsetMin = new Vector2(46f, 0f);
                label.rectTransform.offsetMax = new Vector2(-12f, 0f);
                label.alignment = TextAnchor.MiddleCenter;
                label.verticalOverflow = VerticalWrapMode.Overflow;
            }
        }

        private static void StyleToolbarButton(Button button, bool primary, bool danger)
        {
            if (button == null) return;

            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = UiThemeTokens.GetRoundedSprite(10);
                image.type = Image.Type.Sliced;
                
                if (primary) image.color = MainUiTheme.Hex("2563EB");
                else if (danger) image.color = Color.white;
                else image.color = Color.white;
            }

            var outline = button.GetComponent<Outline>();
            if (outline == null) outline = button.gameObject.AddComponent<Outline>();
            
            if (primary) outline.effectColor = MainUiTheme.Hex("2563EB");
            else if (danger) outline.effectColor = MainUiTheme.Hex("FCA5A5");
            else outline.effectColor = MainUiTheme.Hex("D8DEE8");
            
            outline.effectDistance = new Vector2(1f, -1f);

            var label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.fontSize = 14;
                label.fontStyle = primary ? FontStyle.Bold : FontStyle.Normal;
                if (primary) label.color = Color.white;
                else label.color = MainUiTheme.Hex("334155");
            }
        }

        private RectTransform EnsureGroup(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta, TextAnchor childAlignment, float spacing)
        {
            var existing = parent.Find(name) as RectTransform;
            var rect = existing != null ? existing : CreateRect(name, parent);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            var layout = rect.GetComponent<HorizontalLayoutGroup>();
            if (layout == null)
            {
                layout = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            }

            layout.childAlignment = childAlignment;
            layout.spacing = spacing;
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            return rect;
        }

        private Button EnsureButton(RectTransform fallbackParent, Button button, string name, string label)
        {
            if (button != null)
            {
                return button;
            }

            var rect = CreateRect(name, fallbackParent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = Color.white;
            button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            var text = CreateText("Text", rect, label);
            text.color = new Color(0.05f, 0.08f, 0.14f);
            return button;
        }

        private RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            return rect;
        }

        private Text CreateText(string name, Transform parent, string value)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.text = value;
            MainUiTheme.ApplyText(text, 14, FontStyle.Normal, MainUiTheme.SecondaryText, TextAnchor.MiddleCenter, false);
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 10;
            text.resizeTextMaxSize = 14;
            text.raycastTarget = false;

            var rect = text.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(-12f, -4f);
            return text;
        }

        private void MoveButtonToGroup(Button button, RectTransform group, Vector2 size, string label)
        {
            if (button == null || group == null)
            {
                return;
            }

            button.transform.SetParent(group, false);
            button.gameObject.SetActive(true);

            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;

            var layout = button.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = button.gameObject.AddComponent<LayoutElement>();
            }

            layout.preferredWidth = size.x;
            layout.preferredHeight = size.y;
            layout.flexibleWidth = 0f;
            layout.flexibleHeight = 0f;

            if (!string.IsNullOrEmpty(label))
            {
                var text = button.GetComponentInChildren<Text>();
                if (text == null)
                {
                    text = CreateText("Text", button.transform, label);
                }

                text.text = label;
                text.alignment = TextAnchor.MiddleCenter;
                text.resizeTextForBestFit = true;
                text.resizeTextMinSize = 10;
                text.resizeTextMaxSize = 14;
                text.font = MainUiTheme.BodyFont;
            }
        }

        private static void SetToolbarButtonSize(Button button, float width)
        {
            if (button == null)
            {
                return;
            }

            var rect = button.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.sizeDelta = new Vector2(width, 40f);
            }

            var layout = button.GetComponent<LayoutElement>();
            if (layout != null)
            {
                layout.preferredWidth = width;
                layout.preferredHeight = 40f;
                layout.flexibleWidth = 0f;
                layout.flexibleHeight = 0f;
            }
        }

        private void ConfigureWireColorButton(Button button, Color color)
        {
            if (button == null)
            {
                return;
            }

            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = color;
            }

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
            colors.pressedColor = new Color(0.86f, 0.92f, 1f, 1f);
            colors.selectedColor = Color.white;
            button.colors = colors;

            var outline = button.GetComponent<Outline>();
            if (outline == null)
            {
                outline = button.gameObject.AddComponent<Outline>();
            }

            outline.effectColor = new Color(0f, 0f, 0f, 0.16f);
            outline.effectDistance = new Vector2(1f, -1f);
            if (!colorButtonOutlines.Contains(outline))
            {
                colorButtonOutlines.Add(outline);
            }
        }

        private void RefreshWireColorButtons(bool force)
        {
            if (workspace == null || wirePaletteColors == null)
            {
                return;
            }

            var activeColor = workspace.ActiveWirePaletteColor;
            var hasSelectedWire = workspace.HasSelectedWire;
            if (!force && hasSelectedWire == lastActiveWireSelectionState && IsSamePaletteColor(activeColor, lastActiveWireColor))
            {
                return;
            }

            lastActiveWireColor = activeColor;
            lastActiveWireSelectionState = hasSelectedWire;

            for (var i = 0; i < colorButtons.Count && i < wirePaletteColors.Length; i++)
            {
                var button = colorButtons[i];
                if (button == null)
                {
                    continue;
                }

                var outline = button.GetComponent<Outline>();
                var active = IsSamePaletteColor(activeColor, wirePaletteColors[i]);
                if (outline != null)
                {
                    outline.effectColor = active ? MainUiTheme.Hex("1D4ED8") : new Color(0f, 0f, 0f, 0.16f);
                    outline.effectDistance = active ? new Vector2(2f, -2f) : new Vector2(1f, -1f);
                }
            }
        }

        private static void ApplyFontsToChildren(RectTransform root, int defaultSize)
        {
            if (root == null)
            {
                return;
            }

            var texts = root.GetComponentsInChildren<Text>(true);
            for (var i = 0; i < texts.Length; i++)
            {
                var text = texts[i];
                if (text == null)
                {
                    continue;
                }

                text.font = MainUiTheme.BodyFont;
                if (text.fontSize <= 0 || text.fontSize == 16)
                {
                    text.fontSize = defaultSize;
                }
            }
        }

        private static void ApplyNavigationVisuals(RectTransform navBar)
        {
            if (navBar == null)
            {
                return;
            }

            var buttons = navBar.GetComponentsInChildren<Button>(true);
            for (var i = 0; i < buttons.Length; i++)
            {
                var button = buttons[i];
                if (button == null)
                {
                    continue;
                }

                var label = button.GetComponentInChildren<Text>(true);
                var labelText = label != null ? label.text : string.Empty;
                var selected = labelText.Contains("模拟电路");

                var image = button.GetComponent<Image>() ?? button.gameObject.AddComponent<Image>();
                image.color = selected ? MainUiTheme.SelectedBlue : Color.white;

                var outline = button.GetComponent<Outline>();
                if (outline != null)
                {
                    outline.effectColor = selected ? MainUiTheme.SelectedBlue : Color.clear;
                    outline.effectDistance = Vector2.zero;
                }

                if (label != null)
                {
                    MainUiTheme.ApplyText(label, 15, selected ? FontStyle.Bold : FontStyle.Normal, selected ? MainUiTheme.PrimaryBlue : MainUiTheme.NormalText, TextAnchor.MiddleCenter, true);
                }
            }
        }

        private static bool IsSamePaletteColor(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b) < 0.35f;
        }

        private void EnsureLocalInspectorPanel()
        {
            if (workspace == null)
            {
                return;
            }

            var parent = workspace.WorkspaceRect != null ? workspace.WorkspaceRect.parent as RectTransform : null;
            if (parent == null)
            {
                parent = startButton != null && startButton.transform.parent != null ? startButton.transform.parent.parent as RectTransform : null;
            }

            if (parent == null)
            {
                return;
            }

            localInspectorPanel = LocalInspectorPanel.Create(parent, workspace);
        }
        private void EnsureBlueprintPanels()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = FindObjectOfType<Canvas>();
            }

            var parent = canvas != null ? canvas.transform as RectTransform : transform.root as RectTransform;
            if (parent == null)
            {
                return;
            }

            if (saveDialog == null)
            {
                saveDialog = FindObjectOfType<SaveBlueprintDialog>();
            }

            if (saveDialog == null)
            {
                saveDialog = SaveBlueprintDialog.Create(parent, saveLoadService);
            }
            else
            {
                saveDialog.Initialize(saveLoadService);
            }

            if (importPanel == null)
            {
                importPanel = ImportBlueprintPanel.Create(parent, saveLoadService);
            }
            else
            {
                importPanel.Initialize(saveLoadService);
            }
        }

        private void EnsureVerticalDivider(RectTransform parent, string name, float x)
        {
            var divider = parent.Find(name) as RectTransform;
            if (divider == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(parent, false);
                divider = go.GetComponent<RectTransform>();
            }

            divider.anchorMin = new Vector2(0f, 0.5f);
            divider.anchorMax = new Vector2(0f, 0.5f);
            divider.pivot = new Vector2(0.5f, 0.5f);
            divider.anchoredPosition = new Vector2(x, 0f);
            divider.sizeDelta = new Vector2(1f, 32f);

            var img = divider.GetComponent<Image>();
            img.color = MainUiTheme.Hex("E5E7EB");
            img.raycastTarget = false;
        }

        private void OpenSaveDialog()
        {
            if (saveDialog != null)
            {
                saveDialog.Show();
                return;
            }

            saveLoadService.Save();
        }

        private void OpenImportPanel()
        {
            if (importPanel != null)
            {
                importPanel.Show();
                return;
            }

            saveLoadService.Load();
        }

        private void ToggleSimulation()
        {
            workspace.ToggleSimulation();
            RefreshSimulationButtonLabel();
        }

        private void RefreshSimulationButtonLabel()
        {
            var label = startButton != null ? startButton.GetComponentInChildren<Text>() : null;
            if (label != null)
            {
                label.text = workspace != null && workspace.IsSimulationRunning ? "结束仿真" : "开始仿真";
            }
        }

        private void ToggleLock()
        {
            workspace.ToggleInteractionLock();
            RefreshLockButtonLabel();
        }

        private void RefreshLockButtonLabel()
        {
            var label = lockButton != null ? lockButton.GetComponentInChildren<Text>() : null;
            if (label != null)
            {
                label.text = workspace != null && workspace.IsInteractionLocked ? "解锁" : "锁定";
            }
        }
    }
}
