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
        [SerializeField] private AIAssistantPanel aiAssistantPanel;
        [SerializeField] private Button undoButton;
        [SerializeField] private Button redoButton;
        [SerializeField] private Button quickDeleteButton;
        [SerializeField] private Button quickClearWiresButton;
        [SerializeField] private Button quickClearAllButton;
        [SerializeField] private Button lockButton;
        [SerializeField] private Dropdown wireStyleDropdown;
        [SerializeField] private List<Button> colorButtons = new List<Button>();

        private void Awake()
        {
            EnsureToolbarLayout();
            EnsureBlueprintPanels();
            EnsureAIAssistantPanel();
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

            var colors = new[]
            {
                new Color(0.95f, 0.15f, 0.12f),
                new Color(0.10f, 0.45f, 0.95f),
                new Color(0.08f, 0.65f, 0.25f),
                new Color(0.95f, 0.78f, 0.12f)
            };

            for (var i = 0; i < colorButtons.Count && i < colors.Length; i++)
            {
                var color = colors[i];
                colorButtons[i].onClick.AddListener(() => workspace.CurrentWireColor = color);
            }

            RefreshSimulationButtonLabel();
            RefreshLockButtonLabel();
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

        private void EnsureAIAssistantPanel()
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

            aiAssistantPanel = AIAssistantPanel.Create(parent, workspace);
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
