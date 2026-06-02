using UnityEngine;
using UnityEngine.UI;
using ElectricalSim.Templates;
using ElectricalSim.Core;
using ElectricalSim.UI;
using ElectricalSim.AI;
using System.Linq;

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
        private AIAssistantPanel aiPanel;
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

            if (aiPanel == null)
            {
                aiPanel = FindObjectOfType<AIAssistantPanel>(true);
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
            }
            else
            {
                if (EnterPracticeMode(templateItem))
                {
                    onEntered?.Invoke();
                }
            }
        }

        private bool EnterPracticeMode(CircuitTemplateCatalogItemDto templateItem)
        {
            EnsureReferences();

            if (!CircuitTemplateLoader.TryLoad(templateItem.resourcePath, out var templateDto, out _))
            {
                workspace?.SetStatus($"练习模板 {templateItem.templateId} 加载失败！");
                return false;
            }

            if (workspace != null)
            {
                workspace.ClearDrawing(true);
            }

            IsPracticeActive = true;
            CurrentTemplateItem = templateItem;
            CurrentTemplateData = templateDto;

            if (navigation != null)
            {
                navigation.SelectTab(0);
            }

            if (referencePanel != null)
            {
                referencePanel.gameObject.SetActive(true);
                UpdateReferencePanel(templateItem);
            }

            if (aiPanel != null)
            {
                aiPanel.RefreshPracticeState();
            }

            return true;
        }

        public void ClearPracticeState()
        {
            EnsureReferences();

            IsPracticeActive = false;
            CurrentTemplateItem = null;
            CurrentTemplateData = null;

            if (aiPanel != null)
            {
                aiPanel.RefreshPracticeState();
            }

            if (referencePanel != null)
            {
                referencePanel.gameObject.SetActive(false);
            }
        }

        public void EndPractice()
        {
            ClearPracticeState();

            if (workspace != null)
            {
                workspace.ClearDrawing(true);
            }

            if (navigation != null)
            {
                navigation.SelectTab(0); // Assuming 0 is the gallery tab
            }
        }

        public void UpdateReferencePanel(CircuitTemplateCatalogItemDto item)
        {
            EnsureReferences();

            if (referencePanel == null || item == null) return;
            var texts = referencePanel.GetComponentsInChildren<Text>(true);
            var titleText = texts.FirstOrDefault(t => t.name.Contains("Recommendation") || (t.text != null && t.text.Contains("推荐")));
            if (titleText != null)
            {
                titleText.text = item.templateName;
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

            if (aiPanel != null)
            {
                aiPanel.AddAssistantMessage(summary);
            }

            workspace.SetStatus(connectionResult.Passed ? "\u7ec3\u4e60\u68c0\u6d4b\u5df2\u63d0\u4ea4\uff1a\u63a5\u7ebf\u901a\u8fc7\u3002" : "\u7ec3\u4e60\u68c0\u6d4b\u5df2\u63d0\u4ea4\uff1a\u63a5\u7ebf\u9700\u8981\u4fee\u6539\u3002");
        }

        private bool HasWorkspaceContent()
        {
            EnsureReferences();

            var hasComponents = workspace != null && workspace.Components != null && workspace.Components.Count > 0;
            var hasWires = workspace != null && workspace.WireManager != null && workspace.WireManager.Wires != null && workspace.WireManager.Wires.Count > 0;
            return hasComponents || hasWires;
        }

        private string GetRecommendedComponents(string templateName)
        {
            if (string.IsNullOrEmpty(templateName)) return "暂无";
            if (templateName.Contains("单开单控")) return "220V电源、单开单控开关、电灯泡(220V)";
            if (templateName.Contains("双控")) return "220V电源、双控开关 ×2、电灯泡(220V)";
            if (templateName.Contains("空气开关控制照明")) return "220V电源、空气开关2P、单开单控开关、电灯泡(220V)";
            if (templateName.Contains("单相电能表")) return "220V电源、单相电能表、空气开关2P、单开单控开关、电灯泡(220V)";
            if (templateName.Contains("电灯泡与电风扇并联")) return "220V电源、单开单控开关 ×2、电灯泡(220V)、电风扇(220V)";
            if (templateName.Contains("单开控制双灯")) return "220V电源、空气开关2P、单开单控开关、电灯泡(220V) ×2";
            if (templateName.Contains("双开分别控制双灯")) return "220V电源、空气开关2P、单开单控开关 ×2、电灯泡(220V) ×2";
            if (templateName.Contains("空开控制灯泡与风扇")) return "220V电源、空气开关2P、单开单控开关 ×2、电灯泡(220V)、电风扇(220V)";
            return "220V电源、相关负载及开关";
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

            var panelImage = panel.GetComponent<Image>();
            panelImage.color = Color.white;

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var titleObj = new GameObject("Title", typeof(RectTransform), typeof(Text));
            titleObj.transform.SetParent(panel.transform, false);
            var titleText = titleObj.GetComponent<Text>();
            titleText.text = "进入练习确认";
            titleText.font = font;
            titleText.fontSize = 20;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = new Color(0.2f, 0.2f, 0.2f);
            var titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = new Vector2(20f, -50f);
            titleRect.offsetMax = new Vector2(-20f, -10f);

            var msgObj = new GameObject("Message", typeof(RectTransform), typeof(Text));
            msgObj.transform.SetParent(panel.transform, false);
            var msgText = msgObj.GetComponent<Text>();
            msgText.text = "开始练习将清空当前画布，并只显示参考图纸。是否继续？";
            msgText.font = font;
            msgText.fontSize = 16;
            msgText.alignment = TextAnchor.MiddleCenter;
            msgText.color = new Color(0.3f, 0.3f, 0.3f);
            var msgRect = msgObj.GetComponent<RectTransform>();
            msgRect.anchorMin = new Vector2(0f, 0f);
            msgRect.anchorMax = new Vector2(1f, 1f);
            msgRect.offsetMin = new Vector2(30f, 70f);
            msgRect.offsetMax = new Vector2(-30f, -50f);

            var cancelBtnObj = new GameObject("CancelButton", typeof(RectTransform), typeof(Image), typeof(Button));
            cancelBtnObj.transform.SetParent(panel.transform, false);
            var cancelBtnImage = cancelBtnObj.GetComponent<Image>();
            cancelBtnImage.color = new Color(0.9f, 0.9f, 0.9f);
            var cancelBtn = cancelBtnObj.GetComponent<Button>();
            cancelBtn.targetGraphic = cancelBtnImage;
            var cancelRect = cancelBtnObj.GetComponent<RectTransform>();
            cancelRect.anchorMin = new Vector2(0.5f, 0f);
            cancelRect.anchorMax = new Vector2(0.5f, 0f);
            cancelRect.sizeDelta = new Vector2(120f, 40f);
            cancelRect.anchoredPosition = new Vector2(-80f, 40f);

            var cancelTextObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            cancelTextObj.transform.SetParent(cancelBtnObj.transform, false);
            var cancelText = cancelTextObj.GetComponent<Text>();
            cancelText.text = "取消";
            cancelText.font = font;
            cancelText.fontSize = 16;
            cancelText.alignment = TextAnchor.MiddleCenter;
            cancelText.color = new Color(0.3f, 0.3f, 0.3f);
            var cancelTextRect = cancelTextObj.GetComponent<RectTransform>();
            cancelTextRect.anchorMin = Vector2.zero;
            cancelTextRect.anchorMax = Vector2.one;
            cancelTextRect.offsetMin = Vector2.zero;
            cancelTextRect.offsetMax = Vector2.zero;

            var confirmBtnObj = new GameObject("ConfirmButton", typeof(RectTransform), typeof(Image), typeof(Button));
            confirmBtnObj.transform.SetParent(panel.transform, false);
            var confirmBtnImage = confirmBtnObj.GetComponent<Image>();
            confirmBtnImage.color = new Color(0.12f, 0.45f, 0.95f);
            var confirmBtn = confirmBtnObj.GetComponent<Button>();
            confirmBtn.targetGraphic = confirmBtnImage;
            var confirmRect = confirmBtnObj.GetComponent<RectTransform>();
            confirmRect.anchorMin = new Vector2(0.5f, 0f);
            confirmRect.anchorMax = new Vector2(0.5f, 0f);
            confirmRect.sizeDelta = new Vector2(120f, 40f);
            confirmRect.anchoredPosition = new Vector2(80f, 40f);

            var confirmTextObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            confirmTextObj.transform.SetParent(confirmBtnObj.transform, false);
            var confirmText = confirmTextObj.GetComponent<Text>();
            confirmText.text = "开始练习";
            confirmText.font = font;
            confirmText.fontSize = 16;
            confirmText.alignment = TextAnchor.MiddleCenter;
            confirmText.color = Color.white;
            var confirmTextRect = confirmTextObj.GetComponent<RectTransform>();
            confirmTextRect.anchorMin = Vector2.zero;
            confirmTextRect.anchorMax = Vector2.one;
            confirmTextRect.offsetMin = Vector2.zero;
            confirmTextRect.offsetMax = Vector2.zero;

            cancelBtn.onClick.AddListener(() => Destroy(overlay));
            confirmBtn.onClick.AddListener(() =>
            {
                Destroy(overlay);
                onConfirm?.Invoke();
            });
        }
    }
}
