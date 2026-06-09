using System;
using ElectricalSim.Core;
using ElectricalSim.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace ElectricalSim.AI
{
    public sealed class AIAssistantPanel : MonoBehaviour
    {
        private const float PanelWidth = 340f;
        private const float PanelMargin = 12f;
        private const float HeaderHeight = 42f;
        private const float QuickActionsHeight = 208f;
        private const float InputAreaHeight = 58f;

        [SerializeField] private WorkspaceController workspace;
        [SerializeField] private Text titleText;
        [SerializeField] private Text modeText;
        [SerializeField] private Button switchModeButton;
        [SerializeField] private Button explainButton;
        [SerializeField] private Button checkButton;
        [SerializeField] private Button submitPracticeButton;
        [SerializeField] private Button exitPracticeButton;
        [SerializeField] private Button clearChatButton;
        [SerializeField] private ScrollRect chatScrollRect;
        [SerializeField] private RectTransform chatContent;
        [SerializeField] private InputField questionInput;
        [SerializeField] private Button sendButton;

        private IAIAssistantService assistantService;
        private AIAssistantMode currentMode = AIAssistantMode.LocalMock;
        private CircuitSummaryBuilder summaryBuilder;

        public static AIAssistantPanel Create(RectTransform parent, WorkspaceController workspace)
        {
            if (parent == null)
            {
                return null;
            }

            var existing = parent.Find("AIAssistantPanel");
            AIAssistantPanel panel;
            RectTransform rect;
            Image image;

            if (existing != null)
            {
                panel = existing.GetComponent<AIAssistantPanel>() ?? existing.gameObject.AddComponent<AIAssistantPanel>();
                rect = existing.GetComponent<RectTransform>();
                image = existing.GetComponent<Image>() ?? existing.gameObject.AddComponent<Image>();
                panel.ClearGeneratedChildren();
            }
            else
            {
                var root = new GameObject("AIAssistantPanel", typeof(RectTransform), typeof(Image), typeof(AIAssistantPanel));
                root.transform.SetParent(parent, false);
                rect = root.GetComponent<RectTransform>();
                image = root.GetComponent<Image>();
                panel = root.GetComponent<AIAssistantPanel>();
            }

            image.color = new Color(0.97f, 0.98f, 1f, 1f);
            image.raycastTarget = true;

            panel.BuildUi(rect);
            panel.Initialize(workspace);
            panel.AdjustWorkspaceForPanel(parent, workspace);
            panel.transform.SetAsLastSibling();
            return panel;
        }

        public void Initialize(WorkspaceController workspaceController)
        {
            workspace = workspaceController;
            summaryBuilder = new CircuitSummaryBuilder(workspace);
            SetAssistantMode(AIAssistantMode.LocalMock, false);

            BindButton(sendButton, SendQuestion);
            BindButton(switchModeButton, ToggleAssistantMode);
            BindButton(explainButton, ExplainCurrentCircuit);
            BindButton(checkButton, CheckCurrentCircuit);
            BindButton(submitPracticeButton, SubmitPracticeCheck);
            BindButton(exitPracticeButton, ExitPractice);
            BindButton(clearChatButton, ClearChat);
        }

        private void ClearGeneratedChildren()
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
        }

        private void BuildUi(RectTransform root)
        {
            root.anchorMin = new Vector2(1f, 0f);
            root.anchorMax = new Vector2(1f, 1f);
            root.pivot = new Vector2(1f, 0.5f);
            root.offsetMin = new Vector2(-PanelWidth - PanelMargin, PanelMargin);
            root.offsetMax = new Vector2(-PanelMargin, -PanelMargin);

            var rootLayout = root.GetComponent<VerticalLayoutGroup>() ?? root.gameObject.AddComponent<VerticalLayoutGroup>();
            rootLayout.padding = new RectOffset(12, 12, 12, 12);
            rootLayout.spacing = 10f;
            rootLayout.childAlignment = TextAnchor.UpperCenter;
            rootLayout.childControlWidth = true;
            rootLayout.childControlHeight = true;
            rootLayout.childForceExpandWidth = true;
            rootLayout.childForceExpandHeight = false;

            var header = CreatePanelSection("Header", root, HeaderHeight, 0f, new Color(0.90f, 0.94f, 1f, 1f));
            titleText = CreateText("Title", header, "AI 助教", 18, TextAnchor.MiddleLeft);
            titleText.fontStyle = FontStyle.Bold;
            titleText.rectTransform.offsetMin = new Vector2(14f, 0f);
            titleText.rectTransform.offsetMax = new Vector2(-14f, 0f);

            var quickActions = CreatePanelSection("QuickActions", root, QuickActionsHeight, 0f, new Color(0.97f, 0.98f, 1f, 1f));
            var actionLayout = quickActions.gameObject.AddComponent<VerticalLayoutGroup>();
            actionLayout.padding = new RectOffset(0, 0, 0, 0);
            actionLayout.spacing = 7f;
            actionLayout.childAlignment = TextAnchor.UpperCenter;
            actionLayout.childControlWidth = true;
            actionLayout.childControlHeight = true;
            actionLayout.childForceExpandWidth = true;
            actionLayout.childForceExpandHeight = false;

            modeText = CreateLayoutText("ModeText", quickActions, "当前模式：本地助教", 13, TextAnchor.MiddleLeft, 22f);
            switchModeButton = CreateButton("SwitchModeButton", quickActions, "切换AI模式", new Color(0.92f, 0.95f, 0.98f), new Color(0.05f, 0.08f, 0.14f), 30f);
            explainButton = CreateButton("ExplainCircuitButton", quickActions, "当前电路解释", new Color(0.16f, 0.45f, 0.95f), Color.white, 30f);
            checkButton = CreateButton("CheckCircuitButton", quickActions, "检查当前电路", new Color(0.92f, 0.95f, 0.98f), new Color(0.05f, 0.08f, 0.14f), 30f);
            submitPracticeButton = CreateButton("SubmitPracticeButton", quickActions, "提交练习检测", new Color(0.12f, 0.65f, 0.25f), Color.white, 30f);
            submitPracticeButton.gameObject.SetActive(false);
            exitPracticeButton = CreateButton("ExitPracticeButton", quickActions, "退出练习", new Color(0.85f, 0.18f, 0.16f), Color.white, 30f);
            exitPracticeButton.gameObject.SetActive(false);
            clearChatButton = CreateButton("ClearChatButton", quickActions, "清空对话", new Color(0.92f, 0.95f, 0.98f), new Color(0.05f, 0.08f, 0.14f), 30f);

            var chatRoot = CreatePanelSection("ChatScrollView", root, 0f, 1f, new Color(0.94f, 0.97f, 1f, 1f));
            chatScrollRect = chatRoot.gameObject.AddComponent<ScrollRect>();
            chatScrollRect.horizontal = false;
            chatScrollRect.vertical = true;
            chatScrollRect.movementType = ScrollRect.MovementType.Clamped;

            var viewport = CreateRect("Viewport", chatRoot);
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = Vector2.zero;
            viewport.offsetMax = Vector2.zero;
            var viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(0.94f, 0.97f, 1f, 1f);
            var mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = true;

            chatContent = CreateRect("Content", viewport);
            chatContent.anchorMin = new Vector2(0f, 1f);
            chatContent.anchorMax = new Vector2(1f, 1f);
            chatContent.pivot = new Vector2(0.5f, 1f);
            chatContent.offsetMin = new Vector2(8f, 0f);
            chatContent.offsetMax = new Vector2(-8f, 0f);
            var contentLayout = chatContent.gameObject.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(0, 0, 8, 8);
            contentLayout.spacing = 8f;
            contentLayout.childAlignment = TextAnchor.UpperCenter;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            var fitter = chatContent.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            chatScrollRect.viewport = viewport;
            chatScrollRect.content = chatContent;

            var inputArea = CreatePanelSection("InputArea", root, InputAreaHeight, 0f, new Color(0.97f, 0.98f, 1f, 1f));
            var inputLayout = inputArea.gameObject.AddComponent<HorizontalLayoutGroup>();
            inputLayout.padding = new RectOffset(0, 0, 0, 0);
            inputLayout.spacing = 8f;
            inputLayout.childAlignment = TextAnchor.MiddleCenter;
            inputLayout.childControlWidth = true;
            inputLayout.childControlHeight = true;
            inputLayout.childForceExpandWidth = false;
            inputLayout.childForceExpandHeight = true;

            questionInput = CreateInputField("QuestionInputField", inputArea);
            var inputLayoutElement = questionInput.gameObject.AddComponent<LayoutElement>();
            inputLayoutElement.flexibleWidth = 1f;
            inputLayoutElement.minHeight = 46f;
            inputLayoutElement.preferredHeight = 46f;

            sendButton = CreateButton("SendButton", inputArea, "发送", new Color(0.16f, 0.45f, 0.95f), Color.white, 46f);
            var sendLayout = sendButton.GetComponent<LayoutElement>();
            sendLayout.minWidth = 68f;
            sendLayout.preferredWidth = 68f;
        }

        private void AdjustWorkspaceForPanel(RectTransform parent, WorkspaceController workspaceController)
        {
            var workspaceRect = workspaceController != null ? workspaceController.WorkspaceRect : null;
            if (workspaceRect == null || workspaceRect.parent != parent)
            {
                return;
            }

            var rect = GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, workspaceRect.anchorMin.y);
            rect.anchorMax = new Vector2(1f, workspaceRect.anchorMax.y);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.offsetMin = new Vector2(-PanelWidth - PanelMargin, workspaceRect.offsetMin.y + PanelMargin);
            rect.offsetMax = new Vector2(-PanelMargin, workspaceRect.offsetMax.y - PanelMargin);

            var targetRight = -(PanelWidth + PanelMargin * 2f);
            if (workspaceRect.anchorMax.x > 0.98f && workspaceRect.offsetMax.x > targetRight)
            {
                workspaceRect.offsetMax = new Vector2(targetRight, workspaceRect.offsetMax.y);
            }
        }

        private void ToggleAssistantMode()
        {
            if (currentMode == AIAssistantMode.LocalMock)
            {
                SetAssistantMode(AIAssistantMode.RemoteApi, true);
            }
            else
            {
                SetAssistantMode(AIAssistantMode.LocalMock, true);
            }
        }

        private void SetAssistantMode(AIAssistantMode mode, bool notify)
        {
            if (mode == AIAssistantMode.RemoteApi)
            {
                var remoteService = new RealAIAssistantService(AIAssistantConfig.LoadDefault(), this);
                if (!remoteService.IsConfigured)
                {
                    currentMode = AIAssistantMode.LocalMock;
                    assistantService = new MockAIAssistantService();
                    RefreshModeLabel();
                    if (notify)
                    {
                        AddAssistantMessage("真实 AI API 尚未配置，已继续使用本地 Mock 助教。");
                    }
                    return;
                }

                currentMode = AIAssistantMode.RemoteApi;
                assistantService = remoteService;
                RefreshModeLabel();
                if (notify)
                {
                    AddAssistantMessage("已切换到真实 AI 助教。当前版本仅保留远程服务结构，真实请求将在后续接入后端。");
                }
                return;
            }

            currentMode = AIAssistantMode.LocalMock;
            assistantService = new MockAIAssistantService();
            RefreshModeLabel();
            if (notify)
            {
                AddAssistantMessage("已切换到本地 Mock 助教。");
            }
        }

        private void RefreshModeLabel()
        {
            if (modeText != null)
            {
                modeText.text = currentMode == AIAssistantMode.RemoteApi ? "当前模式：真实AI" : "当前模式：本地助教";
            }
        }

        private void SendQuestion()
        {
            var question = questionInput != null ? questionInput.text.Trim() : string.Empty;
            if (string.IsNullOrEmpty(question))
            {
                AddAssistantMessage("请输入问题。");
                return;
            }

            if (questionInput != null)
            {
                questionInput.text = string.Empty;
            }

            AddUserMessage(question);
            AskAssistant(question);
        }

        private void ExplainCurrentCircuit()
        {
            if (IndustrialCircuitExplainer.TryExplain(workspace, out var industrialExplanation))
            {
                AddAssistantMessage(industrialExplanation);
                return;
            }

            AskAssistant("当前电路解释");
        }

        private void CheckCurrentCircuit()
        {
            if (workspace == null)
            {
                AddAssistantMessage("电路检查失败：未能读取当前画布。");
                return;
            }

            try
            {
                if (IndustrialCircuitRuleAnalyzer.TryAnalyze(workspace, out var industrialResult) && industrialResult.IsIndustrial)
                {
                    AddAssistantMessage(industrialResult.FormatForAssistant());
                    AppendCircuitStateAnalysis();
                    var industrialSummary = "工业电路检查完成：";
                    if (industrialResult.ErrorCount > 0)
                    {
                        industrialSummary += "发现 " + industrialResult.ErrorCount + " 个严重问题。";
                    }
                    else if (industrialResult.WarningCount > 0)
                    {
                        industrialSummary += "发现 " + industrialResult.WarningCount + " 个提醒。";
                    }
                    else
                    {
                        industrialSummary += "未发现严重错误。";
                    }
                    workspace.SetStatus(industrialSummary);
                    return;
                }

                var checker = new CircuitRuleChecker(workspace);
                var result = checker.Check();
                var stateResult = AnalyzeCircuitState();
                var displayResult = FilterBreakerDirectControlFalsePositives(result, stateResult);
                AddAssistantMessage(CircuitRuleCheckTeacherFormatter.FormatForTeaching(displayResult));
                AddAssistantMessage(stateResult.ToReadableText());
                
                string summary = "电路检查完成：";
                if (displayResult.ErrorCount > 0 || displayResult.WarningCount > 0)
                {
                    summary += "发现 " + displayResult.ErrorCount + " 个严重问题，" + displayResult.WarningCount + " 个提醒。";
                }
                else
                {
                    summary += "未发现明显接线错误。";
                }
                workspace.SetStatus(summary);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                AddAssistantMessage("电路检查时出错，请稍后重试。");
            }
        }

        private void AppendCircuitStateAnalysis()
        {
            try
            {
                AddAssistantMessage(AnalyzeCircuitState().ToReadableText());
            }
            catch (Exception exception)
            {
                Debug.LogWarning("CircuitStateAnalyzer V0 failed: " + exception.Message);
                AddAssistantMessage("【通用现象分析 V0】\n通用现象分析 V0 暂时无法完成：" + exception.Message);
            }
        }

        private CircuitStateResult AnalyzeCircuitState()
        {
            var analyzer = new CircuitStateAnalyzer();
            return analyzer.Analyze(workspace.Components, workspace.WireManager != null ? workspace.WireManager.Wires : null);
        }

        private static CircuitCheckResult FilterBreakerDirectControlFalsePositives(
            CircuitCheckResult ruleResult,
            CircuitStateResult stateResult)
        {
            if (ruleResult == null || stateResult == null)
            {
                return ruleResult;
            }

            var filtered = new CircuitCheckResult();
            for (var i = 0; i < ruleResult.issues.Count; i++)
            {
                var issue = ruleResult.issues[i];
                var isBreakerCompletenessFalsePositive = issue != null &&
                    issue.code == "BREAKER_INCOMPLETE" &&
                    stateResult.HasBreakerWithSuppliedInputs;
                var isBreakerDirectControlFalsePositive = issue != null &&
                    stateResult.HasValidClosedBreakerControl &&
                    !stateResult.HasHouseholdControlSwitch &&
                    (issue.code == "LoadLivePathWithoutSwitch" ||
                     issue.code == "ParallelLoadBypassedControl");
                if (isBreakerCompletenessFalsePositive || isBreakerDirectControlFalsePositive)
                {
                    continue;
                }

                filtered.Add(issue);
            }

            return filtered;
        }
        private void SubmitPracticeCheck()
        {
            var practiceController = ElectricalSim.Practice.PracticeSessionController.Instance;
            if (practiceController != null && practiceController.IsPracticeActive)
            {
                practiceController.SubmitPractice();
            }
            else
            {
                AddAssistantMessage("当前未处于练习模式。");
            }
        }

        private void ExitPractice()
        {
            var practiceController = ElectricalSim.Practice.PracticeSessionController.Instance;
            if (practiceController != null && practiceController.IsPracticeActive)
            {
                practiceController.EndPractice();
            }
        }

        public void RefreshPracticeState()
        {
            var practiceController = ElectricalSim.Practice.PracticeSessionController.Instance;
            bool isPractice = practiceController != null && practiceController.IsPracticeActive;
            if (submitPracticeButton != null)
            {
                submitPracticeButton.gameObject.SetActive(isPractice);
            }
            if (exitPracticeButton != null)
            {
                exitPracticeButton.gameObject.SetActive(isPractice);
            }
        }

        private void AskAssistant(string question)
        {
            var summary = summaryBuilder != null ? summaryBuilder.BuildDetailedSummary() : "当前画布为空，请先搭建或加载一个电路。";
            var service = assistantService ?? new MockAIAssistantService();
            service.Ask(question, summary, AddAssistantMessage, error => AddAssistantMessage(string.IsNullOrWhiteSpace(error) ? "AI 助教暂时不可用，请稍后再试。" : error));
        }

        private void ClearChat()
        {
            if (chatContent == null)
            {
                return;
            }

            for (var i = chatContent.childCount - 1; i >= 0; i--)
            {
                Destroy(chatContent.GetChild(i).gameObject);
            }
        }

        private void AddUserMessage(string message)
        {
            AddMessage("我", message, true);
        }

        public void AddAssistantMessage(string message)
        {
            AddMessage("AI 助教", message, false);
        }

        private void AddMessage(string sender, string message, bool fromUser)
        {
            if (chatContent == null)
            {
                return;
            }

            var item = AIAssistantMessageItem.Create(chatContent);
            item.SetMessage(sender, message, fromUser);
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(chatContent);
            Canvas.ForceUpdateCanvases();
            if (chatScrollRect != null)
            {
                chatScrollRect.verticalNormalizedPosition = 0f;
            }
        }

        private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null || action == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        private static RectTransform CreatePanelSection(string name, Transform parent, float preferredHeight, float flexibleHeight, Color backgroundColor)
        {
            var rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = backgroundColor;
            image.raycastTarget = true;
            var layout = rect.gameObject.AddComponent<LayoutElement>();
            if (preferredHeight > 0f)
            {
                layout.minHeight = preferredHeight;
                layout.preferredHeight = preferredHeight;
            }

            layout.flexibleHeight = flexibleHeight;
            return rect;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static Text CreateText(string name, Transform parent, string text, int fontSize, TextAnchor alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var label = go.GetComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = new Color(0.05f, 0.08f, 0.14f);
            label.raycastTarget = false;

            var rect = label.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return label;
        }

        private static Text CreateLayoutText(string name, Transform parent, string text, int fontSize, TextAnchor alignment, float preferredHeight)
        {
            var label = CreateText(name, parent, text, fontSize, alignment);
            label.rectTransform.offsetMin = new Vector2(8f, 0f);
            label.rectTransform.offsetMax = new Vector2(-8f, 0f);
            var layout = label.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = preferredHeight;
            layout.preferredHeight = preferredHeight;
            layout.flexibleWidth = 1f;
            return label;
        }

        private static Button CreateButton(string name, Transform parent, string label, Color backgroundColor, Color textColor, float preferredHeight)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = backgroundColor;
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;

            var layout = go.GetComponent<LayoutElement>();
            layout.minHeight = preferredHeight;
            layout.preferredHeight = preferredHeight;
            layout.flexibleWidth = 1f;

            var text = CreateText("Text", go.transform, label, 14, TextAnchor.MiddleCenter);
            text.color = textColor;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 10;
            text.resizeTextMaxSize = 14;
            text.rectTransform.offsetMin = new Vector2(8f, 0f);
            text.rectTransform.offsetMax = new Vector2(-8f, 0f);
            return button;
        }

        private static InputField CreateInputField(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(InputField));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = Color.white;
            image.raycastTarget = true;

            var input = go.GetComponent<InputField>();
            var text = CreateText("Text", go.transform, string.Empty, 14, TextAnchor.UpperLeft);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = true;
            text.rectTransform.offsetMin = new Vector2(10f, 7f);
            text.rectTransform.offsetMax = new Vector2(-10f, -7f);

            var placeholder = CreateText("Placeholder", go.transform, "请输入问题", 14, TextAnchor.MiddleLeft);
            placeholder.color = new Color(0.45f, 0.52f, 0.62f, 0.8f);
            placeholder.rectTransform.offsetMin = new Vector2(10f, 6f);
            placeholder.rectTransform.offsetMax = new Vector2(-10f, -6f);

            input.textComponent = text;
            input.placeholder = placeholder;
            input.lineType = InputField.LineType.MultiLineNewline;
            return input;
        }
    }
}
