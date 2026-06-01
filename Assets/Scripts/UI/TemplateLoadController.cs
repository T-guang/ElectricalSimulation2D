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
                workspace.SetStatus(error ?? "标准图纸目录读取失败。");
                return;
            }

            EnsureSelectionPanel();
            if (selectionPanel == null)
            {
                workspace.SetStatus("标准图纸面板创建失败。");
                return;
            }

            selectionPanel.Show(catalog.templates);
        }

        public void RequestLoadTemplateFromGallery(ElectricalSim.Templates.CircuitTemplateCatalogItemDto item)
        {
            if (item != null)
            {
                LoadTemplate(item);
            }
        }

        private void LoadTemplate(ElectricalSim.Templates.CircuitTemplateCatalogItemDto item)
        {
            if (workspace == null || item == null)
            {
                return;
            }

            if (HasWorkspaceContent())
            {
                ShowLoadConfirm(item, () => LoadTemplateNow(item));
                return;
            }

            LoadTemplateNow(item);
        }

        private void LoadTemplateNow(CircuitTemplateCatalogItemDto item)
        {
            if (workspace == null || item == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(item.resourcePath))
            {
                workspace.SetStatus("模板路径为空：" + item.templateId);
                return;
            }

            if (!CircuitTemplateLoader.TryLoad(item.resourcePath, out var template, out var error))
            {
                workspace.SetStatus(string.IsNullOrWhiteSpace(error) ? "模板读取失败：" + item.templateId : error);
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

        private bool HasWorkspaceContent()
        {
            var hasComponents = workspace != null && workspace.Components != null && workspace.Components.Count > 0;
            var hasWires = workspace != null && workspace.WireManager != null && workspace.WireManager.Wires != null && workspace.WireManager.Wires.Count > 0;
            return hasComponents || hasWires;
        }

        private void ShowLoadConfirm(CircuitTemplateCatalogItemDto item, System.Action onConfirm)
        {
            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                onConfirm?.Invoke();
                return;
            }

            var overlay = new GameObject("LoadTemplateConfirmDialog", typeof(RectTransform), typeof(Image));
            overlay.transform.SetParent(canvas.transform, false);
            overlay.transform.SetAsLastSibling();

            var overlayRect = overlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            var overlayImage = overlay.GetComponent<Image>();
            overlayImage.color = new Color(0f, 0f, 0f, 0.45f);
            overlayImage.raycastTarget = true;

            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(overlay.transform, false);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(460f, 240f);
            panel.GetComponent<Image>().color = Color.white;

            var title = CreateText("Title", panel.transform, "加载标准图纸", 20, FontStyle.Bold, new Color(0.05f, 0.08f, 0.14f));
            title.alignment = TextAnchor.MiddleCenter;
            title.rectTransform.anchorMin = new Vector2(0f, 1f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.anchoredPosition = new Vector2(0f, -28f);
            title.rectTransform.sizeDelta = new Vector2(-40f, 32f);

            var templateName = string.IsNullOrWhiteSpace(item.templateName) ? item.templateId : item.templateName;
            var message = CreateText("Message", panel.transform, $"当前画布将被清空并加载标准图纸“{templateName}”，是否继续？", 16, FontStyle.Normal, new Color(0.22f, 0.27f, 0.36f));
            message.alignment = TextAnchor.MiddleCenter;
            message.horizontalOverflow = HorizontalWrapMode.Wrap;
            message.verticalOverflow = VerticalWrapMode.Overflow;
            message.rectTransform.anchorMin = new Vector2(0f, 0f);
            message.rectTransform.anchorMax = new Vector2(1f, 1f);
            message.rectTransform.offsetMin = new Vector2(42f, 74f);
            message.rectTransform.offsetMax = new Vector2(-42f, -72f);

            var cancel = CreateButton(panel.transform, "取消", new Color(0.94f, 0.96f, 0.98f), new Color(0.1f, 0.14f, 0.22f));
            var cancelRect = cancel.GetComponent<RectTransform>();
            cancelRect.anchorMin = new Vector2(0.5f, 0f);
            cancelRect.anchorMax = new Vector2(0.5f, 0f);
            cancelRect.anchoredPosition = new Vector2(-95f, 36f);
            cancelRect.sizeDelta = new Vector2(130f, 42f);
            cancel.onClick.AddListener(() => Destroy(overlay));

            var confirm = CreateButton(panel.transform, "确认加载", new Color(0.12f, 0.45f, 1f), Color.white);
            var confirmRect = confirm.GetComponent<RectTransform>();
            confirmRect.anchorMin = new Vector2(0.5f, 0f);
            confirmRect.anchorMax = new Vector2(0.5f, 0f);
            confirmRect.anchoredPosition = new Vector2(95f, 36f);
            confirmRect.sizeDelta = new Vector2(130f, 42f);
            confirm.onClick.AddListener(() =>
            {
                Destroy(overlay);
                onConfirm?.Invoke();
            });
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

        private static Text CreateText(string name, Transform parent, string text, int size, FontStyle style, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var label = go.GetComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = size;
            label.fontStyle = style;
            label.alignment = TextAnchor.UpperLeft;
            label.color = color;
            label.raycastTarget = false;
            return label;
        }

        private static Button CreateButton(Transform parent, string text, Color color, Color textColor)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            var label = CreateText("Text", go.transform, text, 15, FontStyle.Normal, textColor);
            label.alignment = TextAnchor.MiddleCenter;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;
            return go.GetComponent<Button>();
        }
    }
}

