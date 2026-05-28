using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ElectricalSim.Core;

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
            EnsureBlueprintPanels();
            startButton?.onClick.AddListener(ToggleSimulation);
            clearWiresButton?.onClick.AddListener(workspace.ClearWires);
            clearAllButton?.onClick.AddListener(workspace.ClearDrawing);
            saveButton?.onClick.AddListener(OpenSaveDialog);
            loadButton?.onClick.AddListener(OpenImportPanel);
            undoButton?.onClick.AddListener(workspace.Undo);
            redoButton?.onClick.AddListener(workspace.Redo);
            quickDeleteButton?.onClick.AddListener(workspace.DeleteSelection);
            quickClearWiresButton?.onClick.AddListener(workspace.ClearWires);
            quickClearAllButton?.onClick.AddListener(workspace.ClearDrawing);
            lockButton?.onClick.AddListener(ToggleLock);

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
            var label = lockButton != null ? lockButton.GetComponentInChildren<Text>() : null;
            if (label != null)
            {
                label.text = workspace.IsInteractionLocked ? "解锁" : "锁定";
            }
        }
    }
}

