using ElectricalSim.Core;
using ElectricalSim.Templates;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ElectricalSim.UI
{
    public sealed class TemplateLoadController : MonoBehaviour
    {
        [SerializeField] private WorkspaceController workspace;
        [SerializeField] private SaveLoadService saveLoadService;
        [SerializeField] private string catalogPath = "Blueprints/Templates/template_catalog";

        private TemplateSelectionPanel selectionPanel;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallAfterSceneLoad()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureController();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureController();
        }

        private static void EnsureController()
        {
            if (FindObjectOfType<WorkspaceController>() == null || FindObjectOfType<TemplateLoadController>() != null)
            {
                return;
            }

            new GameObject("TemplateLoadController").AddComponent<TemplateLoadController>();
        }

        private void Start()
        {
            if (workspace == null)
            {
                workspace = FindObjectOfType<WorkspaceController>();
            }

            if (saveLoadService == null)
            {
                saveLoadService = FindObjectOfType<SaveLoadService>();
            }

            CreateButtonIfNeeded();

            if (FindObjectOfType<UpdateTemplateLayoutController>() == null)
            {
                UpdateTemplateLayoutController.Create(workspace);
            }
        }

        public void ShowTemplateSelection()
        {
            if (workspace == null)
            {
                return;
            }

            if (!CircuitTemplateCatalogLoader.TryLoad(catalogPath, out var catalog, out var error))
            {
                workspace.SetStatus(error ?? "模板目录读取失败。");
                return;
            }

            EnsureSelectionPanel();
            if (selectionPanel == null)
            {
                workspace.SetStatus("模板目录读取失败。");
                return;
            }

            selectionPanel.Show(catalog.templates);
        }

        private void LoadTemplate(CircuitTemplateCatalogItemDto item)
        {
            if (workspace == null || item == null)
            {
                return;
            }

            if (!CircuitTemplateLoader.TryLoad(item.resourcePath, out var template, out var error))
            {
                workspace.SetStatus("模板读取失败：" + item.templateId);
                if (!string.IsNullOrEmpty(error))
                {
                    Debug.LogWarning(error);
                }

                return;
            }

            var catalog = saveLoadService != null ? saveLoadService.Catalog : null;
            if (!CircuitTemplateSpawnService.Spawn(template, workspace, catalog, out var message))
            {
                workspace.SetStatus(string.IsNullOrWhiteSpace(message) ? "模板生成失败：" + item.templateId : message);
                return;
            }

            TemplateEditSession.RecordTemplate(item.templateId, template.templateName, item.resourcePath);
            workspace.SetStatus("已加载标准图纸：" + template.templateName);
        }

        private void CreateButtonIfNeeded()
        {
            if (workspace == null)
            {
                return;
            }

            var saveButton = GameObject.Find("Save");
            var importButton = GameObject.Find("Load");
            var toolbarParent = saveButton != null ? saveButton.transform.parent as RectTransform : null;
            if (toolbarParent == null && workspace.WorkspaceRect != null)
            {
                toolbarParent = workspace.WorkspaceRect.parent as RectTransform;
            }

            if (toolbarParent == null)
            {
                return;
            }

            var oldButton = GameObject.Find("LoadTemplateButton");
            if (oldButton != null)
            {
                Destroy(oldButton);
            }

            var fileActionGroup = EnsureFileActionGroup(toolbarParent);
            if (fileActionGroup == null)
            {
                return;
            }

            var buttonObject = GameObject.Find("LoadBlueprintButton");
            if (buttonObject == null)
            {
                buttonObject = new GameObject("LoadBlueprintButton", typeof(RectTransform), typeof(Image), typeof(Button));
            }

            buttonObject.transform.SetParent(fileActionGroup, false);
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(138f, 48f);

            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.98f);

            var button = buttonObject.GetComponent<Button>();
            button.onClick.RemoveListener(ShowTemplateSelection);
            button.onClick.AddListener(ShowTemplateSelection);

            var label = buttonObject.GetComponentInChildren<Text>();
            if (label == null)
            {
                label = new GameObject("Text", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
                label.transform.SetParent(buttonObject.transform, false);
            }

            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;
            label.text = "加载图纸";
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 16;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(0.05f, 0.12f, 0.24f);
            label.raycastTarget = false;

            PrepareFileButton(saveButton);
            PrepareFileButton(importButton);
            if (saveButton != null)
            {
                saveButton.transform.SetParent(fileActionGroup, false);
            }

            if (importButton != null)
            {
                importButton.transform.SetParent(fileActionGroup, false);
            }

            buttonObject.transform.SetSiblingIndex(0);
            if (saveButton != null)
            {
                saveButton.transform.SetSiblingIndex(1);
            }

            if (importButton != null)
            {
                importButton.transform.SetSiblingIndex(2);
            }
        }

        private static RectTransform EnsureFileActionGroup(RectTransform toolbarParent)
        {
            var existing = toolbarParent.Find("FileActionGroup");
            var groupObject = existing != null ? existing.gameObject : new GameObject("FileActionGroup", typeof(RectTransform));
            groupObject.transform.SetParent(toolbarParent, false);

            var rect = groupObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-48f, 0f);
            rect.sizeDelta = new Vector2(442f, 48f);

            var layout = groupObject.GetComponent<HorizontalLayoutGroup>();
            if (layout == null)
            {
                layout = groupObject.AddComponent<HorizontalLayoutGroup>();
            }

            layout.childAlignment = TextAnchor.MiddleRight;
            layout.spacing = 14f;
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            return rect;
        }

        private static void PrepareFileButton(GameObject buttonObject)
        {
            if (buttonObject == null)
            {
                return;
            }

            var rect = buttonObject.GetComponent<RectTransform>();
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(138f, 48f);
        }

        private void EnsureSelectionPanel()
        {
            if (selectionPanel != null)
            {
                return;
            }

            RectTransform parent = null;
            if (workspace != null && workspace.WorkspaceRect != null)
            {
                parent = workspace.WorkspaceRect.root as RectTransform;
                if (parent == null)
                {
                    parent = workspace.WorkspaceRect.parent as RectTransform;
                }
            }

            if (parent != null)
            {
                selectionPanel = TemplateSelectionPanel.Create(parent, LoadTemplate);
            }
        }
    }
}
