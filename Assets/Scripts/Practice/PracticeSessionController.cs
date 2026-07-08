using UnityEngine;
using UnityEngine.UI;
using ElectricalSim.Templates;
using ElectricalSim.Core;
using ElectricalSim.UI;
using ElectricalSim.AI;

namespace ElectricalSim.Practice
{
    public class PracticeSessionController : MonoBehaviour
    {
        private static PracticeSessionController _instance;
        public static PracticeSessionController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<PracticeSessionController>(true);
                    if (_instance == null)
                    {
                        var go = new GameObject("PracticeSessionController");
                        _instance = go.AddComponent<PracticeSessionController>();
                    }
                }

                return _instance;
            }
        }

        public bool IsPracticeActive { get; private set; }
        public CircuitTemplateCatalogItemDto CurrentTemplateItem { get; private set; }
        public CircuitTemplateDto CurrentTemplateData { get; private set; }

        private WorkspaceController workspace;
        private BlueprintReferencePanel referencePanel;
        private LocalInspectorPanel inspectorPanel;
        private TopNavigationController navigation;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            EnsureReferences();
        }

        private void EnsureReferences()
        {
            if (workspace == null)
            {
                workspace = FindObjectOfType<WorkspaceController>(true);
            }

            if (referencePanel == null)
            {
                referencePanel = FindObjectOfType<BlueprintReferencePanel>(true);
            }

            if (inspectorPanel == null)
            {
                inspectorPanel = FindObjectOfType<LocalInspectorPanel>(true);
            }

            if (navigation == null)
            {
                navigation = FindObjectOfType<TopNavigationController>(true);
            }
        }

        public void StartPractice(CircuitTemplateCatalogItemDto templateItem)
        {
            StartPractice(templateItem, null);
        }

        public void StartPractice(CircuitTemplateCatalogItemDto templateItem, System.Action onEntered)
        {
            EnsureReferences();

            if (HasWorkspaceContent())
            {
                ShowPracticeConfirm(templateItem, () =>
                {
                    if (EnterPracticeMode(templateItem))
                    {
                        onEntered?.Invoke();
                    }
                });
                return;
            }

            if (EnterPracticeMode(templateItem))
            {
                onEntered?.Invoke();
            }
        }

        private bool EnterPracticeMode(CircuitTemplateCatalogItemDto templateItem)
        {
            EnsureReferences();

            CircuitTemplateDto templateDto = null;
            string loadError = null;
            var loaded = templateItem != null && CircuitTemplateLoader.TryLoad(templateItem.resourcePath, out templateDto, out loadError);
            if (!loaded)
            {
                var templateId = templateItem != null ? templateItem.templateId : "未知模板";
                var message = string.IsNullOrWhiteSpace(loadError)
                    ? $"练习模板 {templateId} 加载失败。"
                    : $"练习模板 {templateId} 加载失败：{loadError}";
                workspace?.SetStatus(message);
                return false;
            }

            workspace?.ClearDrawing(true);

            IsPracticeActive = true;
            CurrentTemplateItem = templateItem;
            CurrentTemplateData = templateDto;

            navigation?.SelectTab(0);

            if (referencePanel != null)
            {
                referencePanel.gameObject.SetActive(true);
                UpdateReferencePanel(templateItem);
            }

            inspectorPanel?.RefreshPracticeState();
            return true;
        }

        public void ClearPracticeState()
        {
            EnsureReferences();

            IsPracticeActive = false;
            CurrentTemplateItem = null;
            CurrentTemplateData = null;

            inspectorPanel?.RefreshPracticeState();

            if (referencePanel != null)
            {
                referencePanel.gameObject.SetActive(false);
            }
        }

        public void EndPractice()
        {
            ClearPracticeState();
            workspace?.ClearDrawing(true);
            navigation?.SelectTab(0);
        }

        public void UpdateReferencePanel(CircuitTemplateCatalogItemDto item)
        {
            EnsureReferences();

            if (referencePanel == null || item == null)
            {
                return;
            }

            var targetText = FindReferenceTitleText(referencePanel);
            if (targetText != null)
            {
                targetText.text = item.templateName;
            }
        }

        public void SubmitPractice()
        {
            EnsureReferences();

            if (!IsPracticeActive)
            {
                workspace?.SetStatus("当前没有正在进行的练习，请先从图纸集进入练习。");
                return;
            }

            if (workspace == null)
            {
                Debug.LogWarning("[PracticeSessionController] WorkspaceController not found; cannot submit practice.");
                return;
            }

            var connectionResult = ElectricalSim.Practice.Netlist.PracticeConnectionChecker.Check(workspace, CurrentTemplateData);
            var summary = PracticeFeedbackFormatter.Format(CurrentTemplateItem, connectionResult);

            inspectorPanel?.AddAssistantMessage(summary);
            workspace.SetStatus(connectionResult.Passed ? "练习检测已提交：接线通过。" : "练习检测已提交：接线需要修改。");
        }

        private bool HasWorkspaceContent()
        {
            EnsureReferences();

            var hasComponents = workspace != null && workspace.Components != null && workspace.Components.Count > 0;
            var hasWires = workspace != null && workspace.WireManager != null && workspace.WireManager.Wires != null && workspace.WireManager.Wires.Count > 0;
            return hasComponents || hasWires;
        }

        private static Text FindReferenceTitleText(BlueprintReferencePanel panel)
        {
            var texts = panel.GetComponentsInChildren<Text>(true);
            foreach (var text in texts)
            {
                if (text != null && (text.name.Contains("Title") || text.name.Contains("Recommendation")))
                {
                    return text;
                }
            }

            return texts.Length > 0 ? texts[0] : null;
        }

        private void ShowPracticeConfirm(CircuitTemplateCatalogItemDto item, System.Action onConfirm)
        {
            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                onConfirm?.Invoke();
                return;
            }

            var overlay = new GameObject("PracticeConfirmDialog", typeof(RectTransform), typeof(Image));
            overlay.transform.SetParent(canvas.transform, false);
            overlay.transform.SetAsLastSibling();

            var overlayRect = overlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            var overlayImage = overlay.GetComponent<Image>();
            overlayImage.color = new Color(0f, 0f, 0f, 0.42f);
            overlayImage.raycastTarget = true;

            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(overlay.transform, false);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(500f, 270f);

            var panelImage = panel.GetComponent<Image>();
            panelImage.sprite = UiThemeTokens.GetRoundedSprite(16, 64);
            panelImage.type = Image.Type.Sliced;
            panelImage.color = Color.white;

            var outline = panel.AddComponent<Outline>();
            outline.effectColor = MainUiTheme.Hex("E5E7EB");
            outline.effectDistance = new Vector2(1f, -1f);

            var shadow = panel.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.05f);
            shadow.effectDistance = new Vector2(0f, -4f);

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var titleObj = new GameObject("Title", typeof(RectTransform), typeof(Text));
            titleObj.transform.SetParent(panel.transform, false);
            var titleText = titleObj.GetComponent<Text>();
            titleText.text = "进入练习确认";
            titleText.font = font;
            titleText.fontSize = 20;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = MainUiTheme.Hex("111827");
            var titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.offsetMin = new Vector2(20f, -60f);
            titleRect.offsetMax = new Vector2(-20f, -28f);

            var msgObj = new GameObject("Message", typeof(RectTransform), typeof(Text));
            msgObj.transform.SetParent(panel.transform, false);
            var msgText = msgObj.GetComponent<Text>();
            var templateName = item != null && !string.IsNullOrWhiteSpace(item.templateName) ? item.templateName : "当前练习";
            msgText.text = $"进入练习会清空当前画布，并显示参考图纸“{templateName}”。是否继续？";
            msgText.font = font;
            msgText.fontSize = 15;
            msgText.alignment = TextAnchor.MiddleCenter;
            msgText.color = MainUiTheme.Hex("475569");
            msgText.lineSpacing = 1.3f;
            var msgRect = msgObj.GetComponent<RectTransform>();
            msgRect.anchorMin = new Vector2(0f, 0f);
            msgRect.anchorMax = new Vector2(1f, 1f);
            msgRect.offsetMin = new Vector2(48f, 85f);
            msgRect.offsetMax = new Vector2(-48f, -90f);

            var cancelBtn = CreateDialogButton(panel.transform, "CancelButton", "取消", MainUiTheme.Hex("F1F5F9"), MainUiTheme.Hex("334155"), new Vector2(-74f, 48f));
            var confirmBtn = CreateDialogButton(panel.transform, "ConfirmButton", "开始练习", MainUiTheme.Hex("2563EB"), Color.white, new Vector2(74f, 48f));

            var confirmText = confirmBtn.transform.Find("Text")?.GetComponent<Text>();
            if (confirmText != null)
            {
                confirmText.fontStyle = FontStyle.Bold;
            }

            cancelBtn.onClick.AddListener(() => Destroy(overlay));
            confirmBtn.onClick.AddListener(() =>
            {
                Destroy(overlay);
                onConfirm?.Invoke();
            });
        }

        private static Button CreateDialogButton(Transform parent, string name, string label, Color background, Color textColor, Vector2 anchoredPosition)
        {
            var buttonObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObj.transform.SetParent(parent, false);

            var buttonImage = buttonObj.GetComponent<Image>();
            buttonImage.sprite = UiThemeTokens.GetRoundedSprite(8, 64);
            buttonImage.type = Image.Type.Sliced;
            buttonImage.color = background;

            var button = buttonObj.GetComponent<Button>();
            button.targetGraphic = buttonImage;

            var buttonRect = buttonObj.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0f);
            buttonRect.anchorMax = new Vector2(0.5f, 0f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.sizeDelta = new Vector2(118f, 38f);
            buttonRect.anchoredPosition = anchoredPosition;

            var textObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textObj.transform.SetParent(buttonObj.transform, false);
            var text = textObj.GetComponent<Text>();
            text.text = label;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 15;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = textColor;

            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return button;
        }
    }
}

