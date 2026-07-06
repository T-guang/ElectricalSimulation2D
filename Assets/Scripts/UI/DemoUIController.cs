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
                new Color(0.95f, 0.15f, 0.12f),
                new Color(0.10f, 0.45f, 0.95f),
                new Color(0.08f, 0.65f, 0.25f),
                new Color(0.95f, 0.78f, 0.12f)
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

            var quickRoot = FindQuickToolRoot();

            undoButton = EnsureButton(toolbar, undoButton, "UndoButton", "撤销");
            redoButton = EnsureButton(toolbar, redoButton, "RedoButton", "重做");
            quickDeleteButton = EnsureButton(toolbar, quickDeleteButton, "DeleteSelectionButton", "删除");
            lockButton = EnsureButton(toolbar, lockButton, "InteractionLockButton", "锁定");

            var leftGroup = EnsureGroup(toolbar, "LeftActionGroup", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(40f, 0f), new Vector2(760f, 54f), TextAnchor.MiddleLeft, 10f);
            var colorGroup = EnsureGroup(toolbar, "WireColorGroup", new Vector2(0.52f, 0.5f), new Vector2(0.52f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(190f, 44f), TextAnchor.MiddleCenter, 12f);

            MoveButtonToGroup(startButton, leftGroup, new Vector2(110f, 44f), "开始仿真");
            MoveButtonToGroup(undoButton, leftGroup, new Vector2(72f, 44f), "撤销");
            MoveButtonToGroup(redoButton, leftGroup, new Vector2(72f, 44f), "重做");
            MoveButtonToGroup(quickDeleteButton, leftGroup, new Vector2(82f, 44f), "删除");
            MoveButtonToGroup(clearWiresButton, leftGroup, new Vector2(82f, 44f), "清线");
            MoveButtonToGroup(clearAllButton, leftGroup, new Vector2(82f, 44f), "清空");
            MoveButtonToGroup(lockButton, leftGroup, new Vector2(82f, 44f), "锁定");

            ApplyToolbarIcon(startButton, "Toolbar/ui_toolbar_start_32", 22f);
            ApplyToolbarIcon(undoButton, "Toolbar/ui_toolbar_undo_24", 18f);
            ApplyToolbarIcon(redoButton, "Toolbar/ui_toolbar_redo_24", 18f);
            ApplyToolbarIcon(quickDeleteButton, "Toolbar/ui_toolbar_delete_24", 18f);
            ApplyToolbarIcon(clearWiresButton, "Toolbar/ui_toolbar_clear_wire_24", 18f);
            ApplyToolbarIcon(clearAllButton, "Toolbar/ui_toolbar_clear_all_24", 18f);
            ApplyToolbarIcon(lockButton, "Toolbar/ui_toolbar_lock_24", 18f);

            for (var i = 0; i < colorButtons.Count; i++)
            {
                var button = colorButtons[i];
                if (button == null)
                {
                    continue;
                }

                MoveButtonToGroup(button, colorGroup, new Vector2(34f, 34f), null);
            }

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

        private void EnsureMainLogo()
        {
            var logoText = GameObject.Find("Logo")?.GetComponent<Text>();
            if (logoText == null)
            {
                return;
            }

            var sprite = UiIconLibrary.Load("Logo/ui_logo_main_320");
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
            iconRect.anchoredPosition = new Vector2(0f, 0f);
            iconRect.sizeDelta = new Vector2(132f, 42f);

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
            titleRect.offsetMin = new Vector2(146f, 0f);
            titleRect.offsetMax = Vector2.zero;

            var titleText = titleRect.GetComponent<Text>() ?? titleRect.gameObject.AddComponent<Text>();
            titleText.text = titleValue;
            titleText.font = logoText.font != null ? logoText.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 22;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleLeft;
            titleText.color = new Color(0.12f, 0.16f, 0.23f);
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

        private void ApplyToolbarIcon(Button button, string iconPath, float iconSize)
        {
            if (button == null)
            {
                return;
            }

            var icon = UiIconLibrary.EnsureButtonIcon(button, iconPath, new Vector2(iconSize, iconSize), new Vector2(18f, 0f));
            if (icon == null)
            {
                return;
            }

            var label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.rectTransform.anchorMin = Vector2.zero;
                label.rectTransform.anchorMax = Vector2.one;
                label.rectTransform.offsetMin = new Vector2(30f, 0f);
                label.rectTransform.offsetMax = new Vector2(-6f, 0f);
                label.alignment = TextAnchor.MiddleCenter;
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
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 16;
            text.alignment = TextAnchor.MiddleCenter;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 10;
            text.resizeTextMaxSize = 16;
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
                text.resizeTextMaxSize = 16;
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

            outline.effectColor = new Color(0f, 0f, 0f, 0.18f);
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
                    outline.effectColor = active ? new Color(0.1f, 0.24f, 0.65f, 0.9f) : new Color(0f, 0f, 0f, 0.18f);
                    outline.effectDistance = active ? new Vector2(3f, -3f) : new Vector2(1f, -1f);
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
                importPanel = FindObjectOfType<ImportBlueprintPanel>();
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
