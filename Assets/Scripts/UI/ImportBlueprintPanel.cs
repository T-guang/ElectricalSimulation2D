using UnityEngine;
using UnityEngine.UI;

namespace ElectricalSim.UI
{
    public sealed class ImportBlueprintPanel : MonoBehaviour
    {
        [SerializeField] private SaveLoadService saveLoadService;
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private Text emptyText;
        [SerializeField] private Text errorText;
        [SerializeField] private Button closeButton;
        [SerializeField] private GameObject confirmDeletePanel;
        [SerializeField] private Text confirmDeleteText;
        [SerializeField] private Button confirmDeleteButton;
        [SerializeField] private Button cancelDeleteButton;

        private SavedBlueprintInfo pendingDelete;

        public static ImportBlueprintPanel Create(RectTransform parent, SaveLoadService service)
        {
            var root = new GameObject("ImportBlueprintPanel", typeof(RectTransform), typeof(Image), typeof(ImportBlueprintPanel));
            root.transform.SetParent(parent, false);

            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.32f);

            var panel = root.GetComponent<ImportBlueprintPanel>();
            panel.saveLoadService = service;
            panel.BuildUi(rect);
            root.SetActive(false);
            return panel;
        }

        public void Initialize(SaveLoadService service)
        {
            saveLoadService = service;
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Hide);
                closeButton.onClick.AddListener(Hide);
            }

            if (confirmDeleteButton != null)
            {
                confirmDeleteButton.onClick.RemoveListener(ConfirmDelete);
                confirmDeleteButton.onClick.AddListener(ConfirmDelete);
            }

            if (cancelDeleteButton != null)
            {
                cancelDeleteButton.onClick.RemoveListener(CancelDelete);
                cancelDeleteButton.onClick.AddListener(CancelDelete);
            }
        }

        public void Show()
        {
            gameObject.SetActive(true);
            CancelDelete();
            RefreshList();
        }

        public void Hide()
        {
            CancelDelete();
            gameObject.SetActive(false);
        }

        private void RefreshList()
        {
            ClearList();
            SetError(string.Empty);

            if (saveLoadService == null)
            {
                SetEmpty(true, "保存服务未初始化。");
                return;
            }

            var blueprints = saveLoadService.ListSavedBlueprints();
            if (blueprints.Count == 0)
            {
                SetEmpty(true, "暂无已保存图纸。");
                return;
            }

            SetEmpty(false, string.Empty);
            foreach (var blueprint in blueprints)
            {
                var item = SavedBlueprintListItem.Create(contentRoot);
                item.Initialize(blueprint, LoadBlueprint, RequestDelete);
            }
        }

        private void LoadBlueprint(SavedBlueprintInfo blueprint)
        {
            if (blueprint == null || saveLoadService == null)
            {
                return;
            }

            if (saveLoadService.LoadFromFile(blueprint.filePath, out var error))
            {
                Hide();
                return;
            }

            SetError(string.IsNullOrWhiteSpace(error) ? "导入失败。" : error);
        }

        private void RequestDelete(SavedBlueprintInfo blueprint)
        {
            if (blueprint == null)
            {
                return;
            }

            pendingDelete = blueprint;
            if (confirmDeleteText != null)
            {
                confirmDeleteText.text = $"确定要删除图纸“{blueprint.documentName}”吗？此操作不可恢复。";
            }

            if (confirmDeletePanel != null)
            {
                confirmDeletePanel.SetActive(true);
                confirmDeletePanel.transform.SetAsLastSibling();
            }
        }

        private void ConfirmDelete()
        {
            if (pendingDelete == null || saveLoadService == null)
            {
                CancelDelete();
                return;
            }

            if (saveLoadService.DeleteSavedBlueprint(pendingDelete.filePath, out var error))
            {
                pendingDelete = null;
                if (confirmDeletePanel != null)
                {
                    confirmDeletePanel.SetActive(false);
                }

                RefreshList();
                return;
            }

            SetError(string.IsNullOrWhiteSpace(error) ? "删除失败。" : error);
            CancelDelete();
        }

        private void CancelDelete()
        {
            pendingDelete = null;
            if (confirmDeletePanel != null)
            {
                confirmDeletePanel.SetActive(false);
            }
        }

        private void BuildUi(RectTransform root)
        {
            var panel = CreateRect("Panel", root, new Vector2(0.5f, 0.5f), new Vector2(640f, 520f));
            panel.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.98f);

            CreateText("Title", panel, "导入图纸", 22, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(28f, -28f), new Vector2(420f, 36f));
            closeButton = CreateButton(panel, "CloseButton", "关闭", new Vector2(528f, -28f), new Vector2(84f, 36f), new Color(0.94f, 0.96f, 0.98f), new Color(0.05f, 0.12f, 0.24f));

            emptyText = CreateText("EmptyText", panel, "暂无已保存图纸。", 16, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420f, 36f));
            errorText = CreateText("ErrorText", panel, string.Empty, 14, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(28f, -468f), new Vector2(584f, 28f));
            errorText.color = new Color(0.82f, 0.08f, 0.08f);

            var scrollObject = new GameObject("ScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollObject.transform.SetParent(panel, false);
            var scrollRectTransform = scrollObject.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = new Vector2(0f, 1f);
            scrollRectTransform.anchorMax = new Vector2(0f, 1f);
            scrollRectTransform.pivot = new Vector2(0f, 1f);
            scrollRectTransform.anchoredPosition = new Vector2(28f, -82f);
            scrollRectTransform.sizeDelta = new Vector2(584f, 374f);
            scrollObject.GetComponent<Image>().color = new Color(0.92f, 0.95f, 0.99f, 1f);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollObject.transform, false);
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            contentRoot = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter)).GetComponent<RectTransform>();
            contentRoot.SetParent(viewport.transform, false);
            contentRoot.anchorMin = new Vector2(0f, 1f);
            contentRoot.anchorMax = new Vector2(1f, 1f);
            contentRoot.pivot = new Vector2(0.5f, 1f);
            contentRoot.offsetMin = new Vector2(12f, 0f);
            contentRoot.offsetMax = new Vector2(-12f, 0f);

            var layout = contentRoot.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.padding = new RectOffset(0, 0, 12, 12);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = contentRoot.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scrollRect = scrollObject.GetComponent<ScrollRect>();
            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRoot;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            BuildConfirmDeletePanel(panel);
            Initialize(saveLoadService);
        }

        private void BuildConfirmDeletePanel(RectTransform parent)
        {
            var overlay = CreateRect("ConfirmDeletePanel", parent, new Vector2(0.5f, 0.5f), new Vector2(420f, 190f));
            overlay.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 1f);
            confirmDeletePanel = overlay.gameObject;

            CreateText("Title", overlay, "删除图纸", 20, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(24f, -20f), new Vector2(240f, 32f));
            confirmDeleteText = CreateText("Message", overlay, string.Empty, 15, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(24f, -66f), new Vector2(372f, 64f));
            cancelDeleteButton = CreateButton(overlay, "CancelButton", "取消", new Vector2(202f, -136f), new Vector2(84f, 36f), new Color(0.94f, 0.96f, 0.98f), new Color(0.05f, 0.12f, 0.24f));
            confirmDeleteButton = CreateButton(overlay, "ConfirmButton", "确认删除", new Vector2(300f, -136f), new Vector2(96f, 36f), new Color(0.95f, 0.18f, 0.14f), Color.white);
            confirmDeletePanel.SetActive(false);
        }

        private void ClearList()
        {
            if (contentRoot == null)
            {
                return;
            }

            for (var i = contentRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(contentRoot.GetChild(i).gameObject);
            }
        }

        private void SetEmpty(bool visible, string message)
        {
            if (emptyText != null)
            {
                emptyText.gameObject.SetActive(visible);
                emptyText.text = message;
            }
        }

        private void SetError(string message)
        {
            if (errorText != null)
            {
                errorText.text = message;
            }
        }

        private static RectTransform CreateRect(string name, RectTransform parent, Vector2 anchor, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
            return rect;
        }

        private static Text CreateText(string name, RectTransform parent, string text, int fontSize, TextAnchor alignment, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var label = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            label.transform.SetParent(parent, false);
            label.rectTransform.anchorMin = anchor;
            label.rectTransform.anchorMax = anchor;
            label.rectTransform.pivot = new Vector2(anchor.x == 0f ? 0f : 0.5f, 1f);
            label.rectTransform.anchoredPosition = position;
            label.rectTransform.sizeDelta = size;
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = new Color(0.05f, 0.12f, 0.24f);
            label.raycastTarget = false;
            return label;
        }

        private static Button CreateButton(RectTransform parent, string name, string text, Vector2 position, Vector2 size, Color background, Color textColor)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            buttonObject.GetComponent<Image>().color = background;

            var label = CreateText("Text", rect, text, 15, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero, size);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;
            label.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            label.color = textColor;
            return buttonObject.GetComponent<Button>();
        }
    }
}
