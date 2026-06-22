using System;
using System.Collections.Generic;
using System.Text;
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
            titleText = CreateText("Title", header, "检查助手", 18, TextAnchor.MiddleLeft);
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
            switchModeButton = CreateButton("SwitchModeButton", quickActions, "切换检查模式", new Color(0.92f, 0.95f, 0.98f), new Color(0.05f, 0.08f, 0.14f), 30f);
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
                AddAssistantMessage("已切换到真实检查助手。当前版本仅保留远程服务结构，真实请求将在后续接入后端。");
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
                industrialExplanation = ApplyCurrentCircuitName(industrialExplanation);
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
                    var currentCircuitName = ResolveCurrentCircuitName();
                    if (!string.IsNullOrWhiteSpace(currentCircuitName))
                    {
                        industrialResult.CircuitType = currentCircuitName;
                    }

                    var industrialStateResult = AnalyzeCircuitState();
                    ApplyRuntimeDisplayOverrides(industrialStateResult);
                    var industrialDebugDetails = BuildRuntimeDisplaySummary(industrialStateResult) +
                        "\n\n" + industrialResult.FormatForAssistant() +
                        "\n\n" + industrialStateResult.ToReadableText();
                    AddAssistantMessage(PrependCheckPanelRuntimeNotices(
                        TeachingCheckReportFormatter.Format(industrialStateResult, industrialResult, industrialDebugDetails),
                        industrialStateResult));
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
                ApplyRuntimeDisplayOverrides(stateResult);
                var displayResult = FilterCheckPanelFalsePositives(result, stateResult);
                var debugDetails = BuildRuntimeDisplaySummary(stateResult) +
                    "\n\n" + CircuitRuleCheckTeacherFormatter.FormatForTeaching(displayResult) +
                    "\n\n" + stateResult.ToReadableText();
                AddAssistantMessage(PrependCheckPanelRuntimeNotices(
                    TeachingCheckReportFormatter.Format(stateResult, displayResult, debugDetails),
                    stateResult));
                
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

        private string ApplyCurrentCircuitName(string text)
        {
            var currentCircuitName = ResolveCurrentCircuitName();
            if (string.IsNullOrWhiteSpace(currentCircuitName) || string.IsNullOrWhiteSpace(text))
            {
                return text;
            }

            const string marker = "电路类型：";
            var markerIndex = text.IndexOf(marker, StringComparison.Ordinal);
            if (markerIndex < 0)
            {
                return text;
            }

            var lineEnd = text.IndexOf('\n', markerIndex);
            return lineEnd < 0
                ? text.Substring(0, markerIndex) + marker + currentCircuitName
                : text.Substring(0, markerIndex) + marker + currentCircuitName + text.Substring(lineEnd);
        }

        private static string ResolveCurrentCircuitName()
        {
            if (ElectricalSim.UI.TemplateEditSession.HasSystemTemplateLoaded &&
                !string.IsNullOrWhiteSpace(ElectricalSim.UI.TemplateEditSession.CurrentTemplateName))
            {
                return ElectricalSim.UI.TemplateEditSession.CurrentTemplateName;
            }

            var practiceController = ElectricalSim.Practice.PracticeSessionController.Instance;
            if (practiceController != null &&
                practiceController.IsPracticeActive &&
                practiceController.CurrentTemplateItem != null &&
                !string.IsNullOrWhiteSpace(practiceController.CurrentTemplateItem.templateName))
            {
                return practiceController.CurrentTemplateItem.templateName;
            }

            return string.Empty;
        }

        private void AppendCircuitStateAnalysis()
        {
            try
            {
                var result = AnalyzeCircuitState();
                ApplyRuntimeDisplayOverrides(result);
                AddAssistantMessage(PrependUnsupportedComponentNotice(
                    BuildRuntimeDisplaySummary(result) + "\n\n" + result.ToReadableText()));
            }
            catch (Exception exception)
            {
                Debug.LogWarning("CircuitStateAnalyzer failed: " + exception.Message);
                AddAssistantMessage("【通用现象分析】\n通用现象分析暂时无法完成：" + exception.Message);
            }
        }

        private CircuitStateResult AnalyzeCircuitState()
        {
            var analyzer = new CircuitStateAnalyzer();
            return analyzer.Analyze(workspace.Components, workspace.WireManager != null ? workspace.WireManager.Wires : null);
        }

        private void ApplyRuntimeDisplayOverrides(CircuitStateResult stateResult)
        {
            if (stateResult == null || workspace == null || workspace.Components == null)
            {
                return;
            }

            var liveComponents = new Dictionary<string, CircuitComponent>();
            for (var i = 0; i < workspace.Components.Count; i++)
            {
                var component = workspace.Components[i];
                if (component != null && !string.IsNullOrWhiteSpace(component.InstanceId))
                {
                    liveComponents[component.InstanceId] = component;
                }
            }

            var hasStarDeltaMotor = HasStarDeltaMotor(stateResult);
            var starContactorEnergized = false;
            var deltaContactorEnergized = false;

            for (var i = 0; i < stateResult.Components.Count; i++)
            {
                var info = stateResult.Components[i];
                if (info == null ||
                    !info.IsContactor ||
                    string.IsNullOrWhiteSpace(info.InstanceId) ||
                    !liveComponents.TryGetValue(info.InstanceId, out var liveComponent))
                {
                    continue;
                }

                if (IsStarContactor(info) && liveComponent.IsEnergized)
                {
                    starContactorEnergized = true;
                }
                else if (IsDeltaContactor(info) && liveComponent.IsEnergized)
                {
                    deltaContactorEnergized = true;
                }
            }

            for (var i = 0; i < stateResult.Components.Count; i++)
            {
                var info = stateResult.Components[i];
                if (info == null ||
                    string.IsNullOrWhiteSpace(info.InstanceId) ||
                    !liveComponents.TryGetValue(info.InstanceId, out var liveComponent))
                {
                    continue;
                }

                ApplyTimerRuntimeDisplayOverride(info, liveComponent);
                ApplyCanvasRunDisplayOverride(info, liveComponent);
                if (hasStarDeltaMotor && info.IsStarDeltaMotor)
                {
                    ApplyStarDeltaRuntimeStageOverride(info, starContactorEnergized, deltaContactorEnergized);
                }
            }
        }

        private static bool HasStarDeltaMotor(CircuitStateResult stateResult)
        {
            if (stateResult == null)
            {
                return false;
            }

            for (var i = 0; i < stateResult.Components.Count; i++)
            {
                if (stateResult.Components[i] != null && stateResult.Components[i].IsStarDeltaMotor)
                {
                    return true;
                }
            }

            return false;
        }

        private static void ApplyStarDeltaRuntimeStageOverride(
            ComponentStateInfo info,
            bool starContactorEnergized,
            bool deltaContactorEnergized)
        {
            if (info == null || !info.IsStarDeltaMotor)
            {
                return;
            }

            if (info.State == "StarDeltaConflict" || (starContactorEnergized && deltaContactorEnergized))
            {
                info.State = "StarDeltaConflict";
                info.StarDeltaConnectionMode = "Conflict";
                if (string.IsNullOrWhiteSpace(info.Judgement))
                {
                    info.Judgement = "危险：星形接触器 KMY 与三角形接触器 KMD 同时闭合，存在星三角短接风险。";
                }
                return;
            }

            if (deltaContactorEnergized && !starContactorEnergized)
            {
                info.State = "DeltaConnected";
                info.StarDeltaConnectionMode = "Delta";
                info.Judgement = "当前 KMY 失电、KMD 得电，电机处于三角运行阶段。";
                return;
            }

            if (starContactorEnergized && !deltaContactorEnergized)
            {
                info.State = "StarConnected";
                info.StarDeltaConnectionMode = "Star";
                info.Judgement = "当前 KMY 得电、KMD 失电，电机处于星形启动阶段。";
            }
        }

        private static bool IsStarContactor(ComponentStateInfo info)
        {
            return ContainsAny(info.InstanceId, "km_star", "kmy", "star", "星形") ||
                ContainsAny(info.DisplayName, "KMY", "star", "星形");
        }

        private static bool IsDeltaContactor(ComponentStateInfo info)
        {
            return ContainsAny(info.InstanceId, "km_delta", "kmd", "delta", "三角") ||
                ContainsAny(info.DisplayName, "KMD", "delta", "三角");
        }

        private static bool ContainsAny(string text, params string[] values)
        {
            if (string.IsNullOrWhiteSpace(text) || values == null)
            {
                return false;
            }

            for (var i = 0; i < values.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(values[i]) &&
                    text.IndexOf(values[i], StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static void ApplyTimerRuntimeDisplayOverride(ComponentStateInfo info, CircuitComponent component)
        {
            if (!info.IsOnDelayTimerRelay || component == null)
            {
                return;
            }

            var hasRuntimeState = RuntimeStateManager.Shared.TryGetTimerState(component.InstanceId, out var timerState) &&
                timerState != null;
            var phase = hasRuntimeState ? timerState.Phase : TimerRuntimePhase.Reset;
            var elapsedSeconds = hasRuntimeState ? timerState.ElapsedSeconds : 0f;
            var delaySeconds = hasRuntimeState ? timerState.DelaySeconds : ResolveDelaySeconds(component);
            var coilEnergized = hasRuntimeState && timerState.IsCoilEnergized;
            var elapsed = coilEnergized && phase == TimerRuntimePhase.Elapsed;

            info.IsTimerRelayCoilEnergizedByAnalyzer = coilEnergized;
            info.IsTimerDelayElapsed = elapsed;
            info.IsTimerDelayedNoClosed = elapsed;
            info.IsTimerDelayedNcClosed = !elapsed;
            info.TimerDelayStatus = phase.ToString();
            info.State = coilEnergized ? "CoilEnergized" : "CoilOff";
            info.CoilStatus = info.State;
            info.TimerContactDescription =
                TimerPhaseDisplayText(phase) + " " + elapsedSeconds.ToString("0.0") + " / " +
                delaySeconds.ToString("0.0") + "s，线圈" + (coilEnergized ? "得电" : "未得电") +
                "；15/16 " + (info.IsTimerDelayedNcClosed ? "导通" : "断开") +
                "，15/18 " + (info.IsTimerDelayedNoClosed ? "导通" : "断开") + "。";
        }

        private static void ApplyCanvasRunDisplayOverride(ComponentStateInfo info, CircuitComponent component)
        {
            if (component == null || info.IsOnDelayTimerRelay)
            {
                return;
            }

            if (info.IsContactor && info.State != "InterlockConflict")
            {
                info.IsContactorCoilEnergizedByAnalyzer = component.IsEnergized;
                info.IsContactorMainContactsClosedByAnalyzer = component.IsEnergized;
                info.CoilStatus = component.IsEnergized ? "CoilEnergized" : "CoilOff";
                info.MainContactStatus = component.IsEnergized ? "Closed" : "Open";
                return;
            }

            if (info.IsStarDeltaMotor)
            {
                if (info.State == "StarDeltaConflict")
                {
                    return;
                }

                if (!component.IsEnergized)
                {
                    info.State = "Stopped";
                    return;
                }

                if (string.Equals(info.StarDeltaConnectionMode, "Delta", StringComparison.OrdinalIgnoreCase))
                {
                    info.State = "DeltaConnected";
                }
                else if (string.Equals(info.StarDeltaConnectionMode, "Star", StringComparison.OrdinalIgnoreCase))
                {
                    info.State = "StarConnected";
                }
                else
                {
                    info.State = "Running";
                }
                return;
            }

            if (info.IsThreePhaseMotor || info.SummaryGroup == ComponentStateInfo.GroupLoad)
            {
                info.State = component.IsEnergized ? "Running" : "Stopped";
            }
        }

        private static float ResolveDelaySeconds(CircuitComponent component)
        {
            const float defaultDelaySeconds = 3f;
            if (component == null)
            {
                return defaultDelaySeconds;
            }

            var parameter = component.GetParameter("delaySeconds");
            return parameter != null ? Mathf.Max(0f, parameter.value) : defaultDelaySeconds;
        }

        private string BuildRuntimeDisplaySummary(CircuitStateResult stateResult)
        {
            if (stateResult == null)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            builder.AppendLine("【当前画布运行态】");
            var count = 0;
            if (TryAppendAutoReciprocatingRuntimeSummary(builder))
            {
                count++;
            }

            for (var i = 0; i < stateResult.Components.Count; i++)
            {
                var component = stateResult.Components[i];
                if (component == null)
                {
                    continue;
                }

                if (component.IsOnDelayTimerRelay)
                {
                    builder.AppendLine("- " + component.DisplayName + "：" + component.TimerContactDescription);
                    count++;
                    continue;
                }

                if (component.IsContactor)
                {
                    builder.AppendLine("- " + component.DisplayName + "：线圈" +
                        (component.IsContactorCoilEnergizedByAnalyzer ? "得电" : "未得电") +
                        "，主触点" + (component.IsContactorMainContactsClosedByAnalyzer ? "闭合" : "断开") + "。");
                    count++;
                    continue;
                }

                if (component.IsThreePhaseMotor || component.IsStarDeltaMotor || component.SummaryGroup == ComponentStateInfo.GroupLoad)
                {
                    builder.AppendLine("- " + component.DisplayName + "：" + RuntimeDisplayState(component) + "。");
                    count++;
                }
            }

            if (count == 0)
            {
                builder.AppendLine("- 当前没有可叠加显示的运行态元件。");
            }

            builder.AppendLine("- 说明：本段用于展示当前画布运行态；结构分析仍由下方调试详情给出。");
            return builder.ToString().TrimEnd();
        }

        private string PrependCheckPanelRuntimeNotices(string report, CircuitStateResult stateResult)
        {
            return PrependUnsupportedComponentNotice(
                PrependParameterEstimationSummary(
                    PrependIndustrialParameterEstimationSummary(
                        PrependAutoReciprocatingRuntimeSummary(report),
                        stateResult),
                    stateResult));
        }

        private string PrependParameterEstimationSummary(string report, CircuitStateResult stateResult)
        {
            var summary = BuildParameterEstimationSummary(stateResult);
            if (string.IsNullOrWhiteSpace(summary))
            {
                return report;
            }

            if (string.IsNullOrWhiteSpace(report))
            {
                return summary;
            }

            return summary + "\n\n" + report;
        }

        private string PrependIndustrialParameterEstimationSummary(string report, CircuitStateResult stateResult)
        {
            var summary = BuildIndustrialParameterEstimationSummary(stateResult);
            if (string.IsNullOrWhiteSpace(summary))
            {
                return report;
            }

            if (string.IsNullOrWhiteSpace(report))
            {
                return summary;
            }

            return summary + "\n\n" + report;
        }

        private string BuildIndustrialParameterEstimationSummary(CircuitStateResult stateResult)
        {
            if (stateResult == null || workspace == null || workspace.Components == null)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            var motorCount = AppendThreePhaseMotorEstimates(builder, stateResult);
            var starDeltaCount = AppendStarDeltaMotorEstimates(builder, stateResult);
            var thermalCount = AppendThermalRelaySettingEstimates(builder);
            var controlCount = AppendControlCircuitLoadEstimates(builder, stateResult);
            return motorCount == 0 && starDeltaCount == 0 && thermalCount == 0 && controlCount == 0 ? string.Empty : builder.ToString().TrimEnd();
        }

        private int AppendThreePhaseMotorEstimates(StringBuilder builder, CircuitStateResult stateResult)
        {
            var count = 0;
            for (var i = 0; i < stateResult.Components.Count; i++)
            {
                var info = stateResult.Components[i];
                var component = FindWorkspaceComponent(info.InstanceId);
                if (component == null || !TeachingParameterCalculationService.IsThreePhaseTeachingMotor(component))
                {
                    continue;
                }

                if (count == 0)
                {
                    builder.AppendLine("【三相电机参数估算】");
                }

                var lineVoltage = ResolveThreePhaseLineVoltage();
                if (!TeachingParameterCalculationService.TryCalculateThreePhaseMotor(
                    component,
                    component.IsEnergized,
                    lineVoltage,
                    out var estimate))
                {
                    continue;
                }

                if (!component.IsEnergized)
                {
                    builder.AppendLine("- " + component.Definition.displayName + "当前未运行，估算运行电流为 0A。");
                    count++;
                    continue;
                }

                builder.AppendLine("- " + component.Definition.displayName + "当前运行。线电压：" +
                    estimate.LineVoltage.ToString("0.#") + "V；额定功率：" +
                    estimate.RatedPower.ToString("0.#") + "W；效率：" +
                    estimate.Efficiency.ToString("0.##") + "；功率因数：" +
                    estimate.PowerFactor.ToString("0.##") + "；估算运行电流：约 " +
                    estimate.EstimatedCurrent.ToString("0.###") + "A。");
                count++;
            }

            if (count > 0)
            {
                builder.AppendLine("- 说明：该结果为教学估算值，按 I≈P/(√3×U×η×cosφ) 计算。");
            }

            return count;
        }

        private int AppendStarDeltaMotorEstimates(StringBuilder builder, CircuitStateResult stateResult)
        {
            var count = 0;
            for (var i = 0; i < stateResult.Components.Count; i++)
            {
                var info = stateResult.Components[i];
                if (info == null || !info.IsStarDeltaMotor)
                {
                    continue;
                }

                var component = FindWorkspaceComponent(info.InstanceId);
                if (component == null ||
                    !TeachingParameterCalculationService.IsStarDeltaTeachingMotor(component))
                {
                    continue;
                }

                var stage = ResolveStarDeltaEstimateStage(info, component);
                if (!TeachingParameterCalculationService.TryCalculateStarDeltaMotor(
                    component,
                    stage,
                    ResolveThreePhaseLineVoltage(),
                    out var estimate))
                {
                    continue;
                }

                if (count == 0)
                {
                    if (builder.Length > 0)
                    {
                        builder.AppendLine();
                    }

                    builder.AppendLine("【星三角参数估算】");
                }

                AppendStarDeltaEstimateLine(builder, component, estimate);
                count++;
            }

            return count;
        }

        private static void AppendStarDeltaEstimateLine(
            StringBuilder builder,
            CircuitComponent component,
            StarDeltaMotorEstimate estimate)
        {
            var name = component != null && component.Definition != null
                ? NormalizeComponentDisplayName(component.Definition.displayName)
                : "星三角电机";

            switch (estimate.Stage)
            {
                case StarDeltaMotorEstimateStage.Conflict:
                    builder.AppendLine("- " + name + "：当前存在星形与三角同时接入风险，属于危险接线状态。系统不输出正常运行电流估算，请先排除星三角冲突。");
                    return;
                case StarDeltaMotorEstimateStage.SupplyFault:
                    builder.AppendLine("- " + name + "：当前三相供电异常，系统不输出正常运行电流估算。");
                    return;
                case StarDeltaMotorEstimateStage.Star:
                    builder.AppendLine("- 说明：星形启动电流按三角运行估算电流的 1/3 进行教学近似。");
                    builder.AppendLine("- " + name + "当前阶段：星形启动。线电压：" +
                        estimate.LineVoltage.ToString("0.#") + "V；额定功率：" +
                        estimate.RatedPower.ToString("0.#") + "W；效率：" +
                        estimate.Efficiency.ToString("0.##") + "；功率因数：" +
                        estimate.PowerFactor.ToString("0.##") + "；三角运行估算电流：约 " +
                        estimate.DeltaEstimatedCurrent.ToString("0.###") + "A；星形启动估算电流：约 " +
                        estimate.StarEstimatedCurrent.ToString("0.###") + "A。");
                    builder.AppendLine("- 说明：星形启动时绕组电压降低，启动电流约为三角运行电流的 1/3。本结果为教学估算值，不代表真实启动暂态曲线。");
                    return;
                case StarDeltaMotorEstimateStage.Delta:
                    builder.AppendLine("- 说明：当前电机已切换至三角运行，按三相电机公式 I=P/(√3×U×η×cosφ) 进行教学估算。");
                    builder.AppendLine("- " + name + "当前阶段：三角运行。线电压：" +
                        estimate.LineVoltage.ToString("0.#") + "V；额定功率：" +
                        estimate.RatedPower.ToString("0.#") + "W；效率：" +
                        estimate.Efficiency.ToString("0.##") + "；功率因数：" +
                        estimate.PowerFactor.ToString("0.##") + "；估算运行电流：约 " +
                        estimate.EstimatedCurrent.ToString("0.###") + "A。");
                    builder.AppendLine("- 说明：当前电机已切换至三角运行，按三相电机额定运行公式进行教学估算。");
                    return;
                default:
                    builder.AppendLine("- " + name + "当前未运行，估算运行电流为 0A。");
                    return;
            }
        }

        private static StarDeltaMotorEstimateStage ResolveStarDeltaEstimateStage(
            ComponentStateInfo info,
            CircuitComponent component)
        {
            if (info == null)
            {
                return StarDeltaMotorEstimateStage.Unknown;
            }

            if (string.Equals(info.State, "StarDeltaConflict", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(info.StarDeltaConnectionMode, "Conflict", StringComparison.OrdinalIgnoreCase) ||
                (info.IsStarPointConnected && info.IsDeltaConnectionDetected))
            {
                return StarDeltaMotorEstimateStage.Conflict;
            }

            if (string.Equals(info.State, "Fault", StringComparison.OrdinalIgnoreCase))
            {
                return StarDeltaMotorEstimateStage.SupplyFault;
            }

            if (component == null || !component.IsEnergized)
            {
                return StarDeltaMotorEstimateStage.Stopped;
            }

            if (string.Equals(info.State, "StarConnected", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(info.StarDeltaConnectionMode, "Star", StringComparison.OrdinalIgnoreCase))
            {
                return StarDeltaMotorEstimateStage.Star;
            }

            if (string.Equals(info.State, "DeltaConnected", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(info.StarDeltaConnectionMode, "Delta", StringComparison.OrdinalIgnoreCase))
            {
                return StarDeltaMotorEstimateStage.Delta;
            }

            return StarDeltaMotorEstimateStage.SupplyFault;
        }

        private int AppendThermalRelaySettingEstimates(StringBuilder builder)
        {
            if (workspace == null || workspace.Components == null)
            {
                return 0;
            }

            var displayCounts = BuildComponentDisplayNameCounts();
            var displayOrdinals = BuildComponentDisplayOrdinals();
            var count = 0;
            for (var i = 0; i < workspace.Components.Count; i++)
            {
                var relay = workspace.Components[i];
                if (!TeachingParameterCalculationService.IsThermalRelay(relay))
                {
                    continue;
                }

                if (count == 0)
                {
                    if (builder.Length > 0)
                    {
                        builder.AppendLine();
                    }

                    builder.AppendLine("【热继整定匹配估算】");
                }

                var relayName = ResolveComponentDisplayName(relay, displayCounts, displayOrdinals);
                if (!TryFindProtectedThreePhaseMotor(relay, out var motor))
                {
                    builder.AppendLine("- " + relayName + "：暂未能可靠关联被保护电机，暂不进行整定匹配判断。");
                    count++;
                    continue;
                }

                var lineVoltage = ResolveThreePhaseLineVoltage();
                var useStaticReference = !motor.IsEnergized;
                if (!TeachingParameterCalculationService.TryCalculateThreePhaseMotor(
                    motor,
                    true,
                    lineVoltage,
                    out var motorEstimate) ||
                    !TeachingParameterCalculationService.TryEstimateThermalRelaySetting(
                        relay,
                        motorEstimate.EstimatedCurrent,
                        useStaticReference,
                        out var relayEstimate))
                {
                    builder.AppendLine("- " + relayName + "：暂未获得被保护电机的有效估算电流，无法进行整定匹配判断。");
                    count++;
                    continue;
                }

                var motorName = ResolveComponentDisplayName(motor, displayCounts, displayOrdinals);
                builder.AppendLine("- " + relayName + "：");
                if (!relay.IsClosed)
                {
                    builder.AppendLine("  当前热继处于 OFF 状态，控制回路已断开，电机当前运行电流为 0A。");
                    builder.AppendLine("  以下判断基于被保护电机的额定估算电流，仅作为整定参考，不表示当前正在过载。");
                }

                builder.AppendLine("  整定电流：" + relayEstimate.SettingCurrent.ToString("0.###") + "A；");
                builder.AppendLine("  被保护电机：" + motorName + "；");
                builder.AppendLine("  " + (relayEstimate.UsesStaticReference ? "电机额定估算电流：" : "电机估算运行电流：") +
                    relayEstimate.MotorCurrent.ToString("0.###") + "A；");
                builder.AppendLine("  判断：" + ThermalRelayJudgementText(relayEstimate.Judgement));
                count++;
            }

            if (count > 0)
            {
                builder.AppendLine("- 说明：该结果为教学估算，用于理解热继整定与电机电流的匹配关系，不代表真实热继动作曲线。");
            }

            return count;
        }

        private bool TryFindProtectedThreePhaseMotor(CircuitComponent relay, out CircuitComponent motor)
        {
            motor = null;
            if (relay == null || workspace == null || workspace.Components == null)
            {
                return false;
            }

            var graph = BuildWireTerminalGraph();
            for (var i = 0; i < workspace.Components.Count; i++)
            {
                var candidate = workspace.Components[i];
                if (!TeachingParameterCalculationService.IsThreePhaseTeachingMotor(candidate))
                {
                    continue;
                }

                if (AreThermalRelayOutputsConnectedToMotor(relay, candidate, graph))
                {
                    motor = candidate;
                    return true;
                }
            }

            return false;
        }

        private Dictionary<TerminalView, List<TerminalView>> BuildWireTerminalGraph()
        {
            var graph = new Dictionary<TerminalView, List<TerminalView>>();
            var wires = workspace != null && workspace.WireManager != null ? workspace.WireManager.Wires : null;
            if (wires == null)
            {
                return graph;
            }

            for (var i = 0; i < wires.Count; i++)
            {
                var wire = wires[i];
                if (wire == null || wire.StartTerminal == null || wire.EndTerminal == null)
                {
                    continue;
                }

                AddTerminalEdge(graph, wire.StartTerminal, wire.EndTerminal);
                AddTerminalEdge(graph, wire.EndTerminal, wire.StartTerminal);
            }

            return graph;
        }

        private static void AddTerminalEdge(
            Dictionary<TerminalView, List<TerminalView>> graph,
            TerminalView from,
            TerminalView to)
        {
            if (!graph.TryGetValue(from, out var list))
            {
                list = new List<TerminalView>();
                graph[from] = list;
            }

            if (!list.Contains(to))
            {
                list.Add(to);
            }
        }

        private bool AreThermalRelayOutputsConnectedToMotor(
            CircuitComponent relay,
            CircuitComponent motor,
            Dictionary<TerminalView, List<TerminalView>> graph)
        {
            return AreTerminalsConnectedByWires(relay.GetTerminal(TerminalConstants.T1), motor.GetTerminal(TerminalConstants.U), graph) &&
                AreTerminalsConnectedByWires(relay.GetTerminal(TerminalConstants.T2), motor.GetTerminal(TerminalConstants.V), graph) &&
                AreTerminalsConnectedByWires(relay.GetTerminal(TerminalConstants.T3), motor.GetTerminal(TerminalConstants.W), graph);
        }

        private static bool AreTerminalsConnectedByWires(
            TerminalView start,
            TerminalView target,
            Dictionary<TerminalView, List<TerminalView>> graph)
        {
            if (start == null || target == null)
            {
                return false;
            }

            if (start == target)
            {
                return true;
            }

            var visited = new HashSet<TerminalView>();
            var queue = new Queue<TerminalView>();
            visited.Add(start);
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (!graph.TryGetValue(current, out var next))
                {
                    continue;
                }

                for (var i = 0; i < next.Count; i++)
                {
                    var terminal = next[i];
                    if (terminal == target)
                    {
                        return true;
                    }

                    if (visited.Add(terminal))
                    {
                        queue.Enqueue(terminal);
                    }
                }
            }

            return false;
        }

        private static string ThermalRelayJudgementText(ThermalRelaySettingJudgement judgement)
        {
            switch (judgement)
            {
                case ThermalRelaySettingJudgement.TooLow:
                    return "整定偏低，可能导致电机正常运行时误动作。";
                case ThermalRelaySettingJudgement.Reasonable:
                    return "整定值接近电机运行电流，较合理。";
                case ThermalRelaySettingJudgement.High:
                    return "整定偏高，保护灵敏度降低。";
                case ThermalRelaySettingJudgement.TooHigh:
                    return "整定明显偏高，保护可能不足。";
                default:
                    return "暂未获得足够参数，无法判断整定匹配。";
            }
        }

        private int AppendControlCircuitLoadEstimates(StringBuilder builder, CircuitStateResult stateResult)
        {
            var hasCandidate = false;
            var count = 0;
            var totalCurrent = 0f;
            var displayCounts = BuildComponentDisplayNameCounts();
            var displayOrdinals = BuildComponentDisplayOrdinals();
            var stateMap = BuildComponentStateMap(stateResult);
            for (var i = 0; i < workspace.Components.Count; i++)
            {
                var component = workspace.Components[i];
                if (component == null ||
                    !TeachingParameterCalculationService.IsControlCircuitTeachingLoad(component))
                {
                    continue;
                }

                hasCandidate = true;
                if (!component.IsEnergized)
                {
                    continue;
                }

                stateMap.TryGetValue(component.InstanceId ?? string.Empty, out var info);
                var hasActualVoltage = TryResolveActualVoltageAcrossComponent(
                    component,
                    info,
                    out var actualSupplyVoltage,
                    out _);

                if (!TeachingParameterCalculationService.TryEstimateControlCircuitLoad(
                    component,
                    actualSupplyVoltage,
                    hasActualVoltage,
                    out var estimate))
                {
                    continue;
                }

                if (count == 0)
                {
                    builder.AppendLine();
                    builder.AppendLine("【控制回路负载估算】");
                    builder.AppendLine("当前得电控制负载：");
                }

                if (estimate.HasEnoughParameters)
                {
                    builder.AppendLine("- " + ResolveControlLoadDisplayName(component, estimate, displayCounts, displayOrdinals) + "（" + estimate.LoadType + "）：");
                    builder.AppendLine("  额定电压：" + estimate.RatedVoltage.ToString("0.#") + "V；");
                    builder.AppendLine("  当前供电电压：" + FormatControlLoadActualSupplyVoltage(estimate) + "；");
                    builder.AppendLine("  额定功率：" + estimate.RatedPower.ToString("0.###") + "W；");
                    builder.AppendLine("  当前估算电流：" +
                        estimate.EstimatedCurrent.ToString("0.###") + "A。");
                    totalCurrent += estimate.EstimatedCurrent;
                }
                else
                {
                    builder.AppendLine("- " + ResolveControlLoadDisplayName(component, estimate, displayCounts, displayOrdinals) + "（" + estimate.LoadType + "）：参数缺失，暂不估算电流。");
                }

                count++;
            }

            if (hasCandidate && count == 0)
            {
                builder.AppendLine();
                builder.AppendLine("【控制回路负载估算】");
                builder.AppendLine("当前未检测到得电的控制回路负载。");
                return 1;
            }

            if (count == 0)
            {
                return 0;
            }

            builder.AppendLine("控制回路估算总电流：" + totalCurrent.ToString("0.###") + "A。");
            builder.AppendLine("- 说明：该结果为教学估算值；若能识别当前供电电压，则按当前电压估算，否则按额定电压估算。本阶段不判断控制电源容量。");
            return count;
        }

        private Dictionary<string, ComponentStateInfo> BuildComponentStateMap(CircuitStateResult stateResult)
        {
            var result = new Dictionary<string, ComponentStateInfo>();
            if (stateResult == null || stateResult.Components == null)
            {
                return result;
            }

            for (var i = 0; i < stateResult.Components.Count; i++)
            {
                var info = stateResult.Components[i];
                if (info == null || string.IsNullOrWhiteSpace(info.InstanceId))
                {
                    continue;
                }

                result[info.InstanceId] = info;
            }

            return result;
        }

        private bool TryResolveActualVoltageAcrossComponent(
            CircuitComponent component,
            ComponentStateInfo info,
            out float voltage,
            out string reason)
        {
            voltage = 0f;
            reason = string.Empty;
            if (component == null || info == null)
            {
                reason = "缺少元件或检查状态。";
                return false;
            }

            var firstTerminal = ResolveControlLoadFirstTerminal(component);
            var secondTerminal = ResolveControlLoadSecondTerminal(component);
            if (firstTerminal == null || secondTerminal == null)
            {
                reason = "未找到可识别的供电端子。";
                return false;
            }

            var energizedResult = ActualSupplyVoltageResolver.ResolveForEnergizedControlLoad(
                workspace != null ? workspace.Components : null,
                component);
            if (energizedResult.Resolved)
            {
                voltage = energizedResult.Voltage;
                reason = energizedResult.Reason;
                return true;
            }

            var firstVoltage = VoltageAt(info, firstTerminal.TerminalId);
            var secondVoltage = VoltageAt(info, secondTerminal.TerminalId);
            if (string.IsNullOrWhiteSpace(firstVoltage) || string.IsNullOrWhiteSpace(secondVoltage))
            {
                reason = "端子电压标签为空。";
                return false;
            }

            var result = ActualSupplyVoltageResolver.ResolveAcrossVoltageLabels(
                workspace != null ? workspace.Components : null,
                firstVoltage,
                secondVoltage);
            voltage = result.Voltage;
            reason = result.Reason;
            return result.Resolved;
        }

        private static TerminalView ResolveControlLoadFirstTerminal(CircuitComponent component)
        {
            return component.GetTerminal(TerminalConstants.A1) ??
                component.GetTerminal(TerminalConstants.L) ??
                component.GetTerminal("1") ??
                component.GetTerminal("11");
        }

        private static TerminalView ResolveControlLoadSecondTerminal(CircuitComponent component)
        {
            return component.GetTerminal(TerminalConstants.A2) ??
                component.GetTerminal(TerminalConstants.N) ??
                component.GetTerminal("2") ??
                component.GetTerminal("12");
        }

        private static string FormatControlLoadActualSupplyVoltage(ControlCircuitLoadEstimate estimate)
        {
            if (estimate == null)
            {
                return "未能可靠识别，按额定电压估算";
            }

            if (estimate.UsesRatedVoltageFallback)
            {
                return "未能可靠识别，按额定电压 " + estimate.RatedVoltage.ToString("0.#") + "V 估算";
            }

            return estimate.ActualSupplyVoltage.ToString("0.#") + "V";
        }

        private Dictionary<string, int> BuildComponentDisplayNameCounts()
        {
            var result = new Dictionary<string, int>();
            if (workspace == null || workspace.Components == null)
            {
                return result;
            }

            for (var i = 0; i < workspace.Components.Count; i++)
            {
                var component = workspace.Components[i];
                if (component == null || component.Definition == null)
                {
                    continue;
                }

                var displayName = NormalizeComponentDisplayName(component.Definition.displayName);
                if (result.ContainsKey(displayName))
                {
                    result[displayName]++;
                }
                else
                {
                    result[displayName] = 1;
                }
            }

            return result;
        }

        private Dictionary<CircuitComponent, int> BuildComponentDisplayOrdinals()
        {
            var result = new Dictionary<CircuitComponent, int>();
            var indexes = new Dictionary<string, int>();
            if (workspace == null || workspace.Components == null)
            {
                return result;
            }

            for (var i = 0; i < workspace.Components.Count; i++)
            {
                var component = workspace.Components[i];
                if (component == null || component.Definition == null)
                {
                    continue;
                }

                var displayName = NormalizeComponentDisplayName(component.Definition.displayName);
                var next = 1;
                if (indexes.TryGetValue(displayName, out var current))
                {
                    next = current + 1;
                }

                indexes[displayName] = next;
                result[component] = next;
            }

            return result;
        }

        private static string ResolveControlLoadDisplayName(
            CircuitComponent component,
            ControlCircuitLoadEstimate estimate,
            Dictionary<string, int> displayCounts,
            Dictionary<CircuitComponent, int> displayOrdinals)
        {
            var fallback = component != null && component.Definition != null
                ? component.Definition.displayName
                : "控制负载";
            var displayName = NormalizeComponentDisplayName(
                estimate != null && !string.IsNullOrWhiteSpace(estimate.DisplayName)
                    ? estimate.DisplayName
                    : fallback);

            var shouldNumber = displayCounts != null &&
                displayCounts.TryGetValue(displayName, out var total) &&
                total > 1;
            if (!shouldNumber)
            {
                return displayName;
            }

            var ordinal = 1;
            if (component != null && displayOrdinals != null &&
                displayOrdinals.TryGetValue(component, out var componentOrdinal))
            {
                ordinal = componentOrdinal;
            }

            return displayName + " #" + ordinal.ToString();
        }

        private static string ResolveComponentDisplayName(
            CircuitComponent component,
            Dictionary<string, int> displayCounts,
            Dictionary<CircuitComponent, int> displayOrdinals)
        {
            var displayName = NormalizeComponentDisplayName(
                component != null && component.Definition != null
                    ? component.Definition.displayName
                    : "元件");

            var shouldNumber = displayCounts != null &&
                displayCounts.TryGetValue(displayName, out var total) &&
                total > 1;
            if (!shouldNumber)
            {
                return displayName;
            }

            var ordinal = 1;
            if (component != null && displayOrdinals != null &&
                displayOrdinals.TryGetValue(component, out var componentOrdinal))
            {
                ordinal = componentOrdinal;
            }

            return displayName + " #" + ordinal.ToString();
        }

        private static string NormalizeComponentDisplayName(string displayName)
        {
            return string.IsNullOrWhiteSpace(displayName)
                ? "元件"
                : displayName.Replace("\r", " ").Replace("\n", " ").Trim();
        }

        private float ResolveThreePhaseLineVoltage()
        {
            return ActualSupplyVoltageResolver.ResolveThreePhaseLineVoltage(
                workspace != null ? workspace.Components : null);
        }

        private string BuildParameterEstimationSummary(CircuitStateResult stateResult)
        {
            if (stateResult == null || workspace == null || workspace.Components == null)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            var count = 0;
            for (var i = 0; i < stateResult.Components.Count; i++)
            {
                var info = stateResult.Components[i];
                var component = FindWorkspaceComponent(info.InstanceId);
                if (component == null || !TeachingParameterCalculationService.IsSinglePhaseTeachingLoad(component))
                {
                    continue;
                }

                if (count == 0)
                {
                    builder.AppendLine("【参数估算】");
                }

                AppendSinglePhaseLoadEstimate(builder, component, info);
                count++;
            }

            if (count == 0)
            {
                return string.Empty;
            }

            builder.AppendLine("- 说明：以上为教学估算值，按 I = P / U 计算；本阶段不做多负载总电流、空开过载或工业级精确仿真。");
            return builder.ToString().TrimEnd();
        }

        private void AppendSinglePhaseLoadEstimate(
            StringBuilder builder,
            CircuitComponent component,
            ComponentStateInfo info)
        {
            var loadName = TeachingParameterCalculationService.LoadDisplayName(component);
            var firstTerminalId = component.GetTerminal(TerminalConstants.L) != null ? TerminalConstants.L : TerminalConstants.A1;
            var secondTerminalId = component.GetTerminal(TerminalConstants.N) != null ? TerminalConstants.N : TerminalConstants.A2;
            var firstVoltage = VoltageAt(info, firstTerminalId);
            var secondVoltage = VoltageAt(info, secondTerminalId);
            var lineLabel = string.Empty;

            if (IsLineOrPhase(firstVoltage) && secondVoltage == TerminalConstants.N)
            {
                lineLabel = firstVoltage;
            }
            else if (IsLineOrPhase(secondVoltage) && firstVoltage == TerminalConstants.N)
            {
                lineLabel = secondVoltage;
            }

            var ratedPower = Mathf.Max(0f, TeachingParameterCalculationService.ResolveParameterValue(
                component,
                ParameterKeys.RatedPower,
                component.Definition.ratedPower));

            if (string.IsNullOrWhiteSpace(lineLabel))
            {
                builder.AppendLine("- " + loadName + "当前未获得有效单相电压，估算电流为 0A。");
                return;
            }

            var voltage = ResolveSinglePhaseSourceVoltage(lineLabel);
            var current = voltage > 0f ? ratedPower / voltage : 0f;
            builder.AppendLine("- " + loadName + "获得 " + voltage.ToString("0.#") + "V 单相电压（" +
                lineLabel + "-N）；额定功率：" + ratedPower.ToString("0.#") +
                "W；估算电流：" + current.ToString("0.###") + "A。");
        }

        private float ResolveSinglePhaseSourceVoltage(string lineLabel)
        {
            return ActualSupplyVoltageResolver.ResolveSinglePhaseVoltage(
                workspace != null ? workspace.Components : null);
        }

        private static string VoltageAt(ComponentStateInfo info, string terminalId)
        {
            if (info == null || string.IsNullOrWhiteSpace(terminalId))
            {
                return string.Empty;
            }

            return info.TerminalVoltages.TryGetValue(terminalId, out var voltage) ? voltage : string.Empty;
        }

        private static bool IsLineOrPhase(string voltage)
        {
            return voltage == TerminalConstants.L ||
                voltage == TerminalConstants.L1 ||
                voltage == TerminalConstants.L2 ||
                voltage == TerminalConstants.L3;
        }

        private static bool IsThreePhaseLine(string voltage)
        {
            return voltage == TerminalConstants.L1 ||
                voltage == TerminalConstants.L2 ||
                voltage == TerminalConstants.L3;
        }

        private string PrependUnsupportedComponentNotice(string report)
        {
            var notice = BuildUnsupportedComponentNotice();
            if (string.IsNullOrWhiteSpace(notice))
            {
                return report;
            }

            if (string.IsNullOrWhiteSpace(report))
            {
                return notice;
            }

            return notice + "\n\n" + report;
        }

        private string BuildUnsupportedComponentNotice()
        {
            var unsupportedComponents = CollectUnsupportedRuntimeComponents();
            if (unsupportedComponents.Count == 0)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            builder.AppendLine("【未支持元件提示】");
            builder.AppendLine("当前画布包含暂未支持仿真判断的元件：" + JoinUnsupportedComponentNames(unsupportedComponents) + "。");
            builder.AppendLine("这些元件可以保存和显示接线，但当前不会参与完整运行判定。");
            builder.AppendLine("已支持部分未发现明显问题，但由于画布中存在暂未支持元件，系统无法给出完整运行结论。");

            for (var i = 0; i < unsupportedComponents.Count; i++)
            {
                var component = unsupportedComponents[i];
                var reason = component != null && component.Definition != null
                    ? component.Definition.unsupportedReason
                    : string.Empty;
                if (string.IsNullOrWhiteSpace(reason))
                {
                    continue;
                }

                builder.AppendLine("- " + UnsupportedComponentDisplayName(component) + "：" + reason);
            }

            return builder.ToString().TrimEnd();
        }

        private List<CircuitComponent> CollectUnsupportedRuntimeComponents()
        {
            var unsupported = new List<CircuitComponent>();
            if (workspace == null || workspace.Components == null)
            {
                return unsupported;
            }

            for (var i = 0; i < workspace.Components.Count; i++)
            {
                var component = workspace.Components[i];
                var definition = component != null ? component.Definition : null;
                if (definition == null)
                {
                    continue;
                }

                if (definition.supportLevel == ComponentSupportLevel.VisualOnly ||
                    !definition.canParticipateInRuntime)
                {
                    unsupported.Add(component);
                }
            }

            return unsupported;
        }

        private static string JoinUnsupportedComponentNames(List<CircuitComponent> components)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < components.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append("、");
                }

                builder.Append(UnsupportedComponentDisplayName(components[i]));
            }

            return builder.ToString();
        }

        private static string UnsupportedComponentDisplayName(CircuitComponent component)
        {
            if (component == null)
            {
                return "未知元件";
            }

            var definition = component.Definition;
            if (definition != null && !string.IsNullOrWhiteSpace(definition.displayName))
            {
                return definition.displayName.Replace("\r", string.Empty).Replace("\n", string.Empty);
            }

            if (definition != null && !string.IsNullOrWhiteSpace(definition.name))
            {
                return definition.name;
            }

            return string.IsNullOrWhiteSpace(component.InstanceId) ? "未知元件" : component.InstanceId;
        }

        private string PrependAutoReciprocatingRuntimeSummary(string report)
        {
            var summary = BuildAutoReciprocatingMainRuntimeSummary();
            if (string.IsNullOrWhiteSpace(summary))
            {
                return report;
            }

            if (string.IsNullOrWhiteSpace(report))
            {
                return summary;
            }

            return summary + "\n\n" + report;
        }

        private string BuildAutoReciprocatingMainRuntimeSummary()
        {
            var motor = FindWorkspaceComponent("motor_1");
            var forwardContactor = FindWorkspaceComponent("km_forward");
            var reverseContactor = FindWorkspaceComponent("km_reverse");
            var leftLimit = FindWorkspaceComponent("sq_left");
            var rightLimit = FindWorkspaceComponent("sq_right");
            if (motor == null || forwardContactor == null || reverseContactor == null ||
                leftLimit == null || rightLimit == null)
            {
                return string.Empty;
            }

            var motionState = RuntimeStateManager.Shared.GetOrCreateMotionState("motor_1");
            if (motionState == null)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            builder.AppendLine("【当前自动往返运行态】");
            if (motionState.Direction == MotionDirection.Stopped)
            {
                builder.AppendLine("- 虚拟运动：停止");
                builder.AppendLine("- 虚拟位置保持在 " + motionState.Position.ToString("0") + " / 100");
            }
            else
            {
                builder.AppendLine("- 虚拟位置：" + motionState.Position.ToString("0") + " / 100");
                builder.AppendLine("- 虚拟运动：" + MotionDirectionDisplayText(motionState.Direction));
            }

            builder.AppendLine("- " + ContactorPairDisplayText(forwardContactor, reverseContactor));
            builder.AppendLine("- 电机" + AutoReciprocatingMotorDisplayText(motor, motionState));
            builder.AppendLine("- " + AutoReciprocatingLimitTeachingText(motionState));
            builder.AppendLine("- SQ 手动状态表示人工触发；自动往返使用虚拟限位触发参与 effectiveTriggered 判断，虚拟触发不会写回 SQ 手动状态。");
            return builder.ToString().TrimEnd();
        }

        private bool TryAppendAutoReciprocatingRuntimeSummary(StringBuilder builder)
        {
            var motor = FindWorkspaceComponent("motor_1");
            var forwardContactor = FindWorkspaceComponent("km_forward");
            var reverseContactor = FindWorkspaceComponent("km_reverse");
            var leftLimit = FindWorkspaceComponent("sq_left");
            var rightLimit = FindWorkspaceComponent("sq_right");
            if (motor == null || forwardContactor == null || reverseContactor == null ||
                leftLimit == null || rightLimit == null)
            {
                return false;
            }

            var motionState = RuntimeStateManager.Shared.GetOrCreateMotionState("motor_1");
            if (motionState == null)
            {
                return false;
            }

            var leftEffectiveTriggered = leftLimit.IsClosed || motionState.LeftLimitTriggered;
            var rightEffectiveTriggered = rightLimit.IsClosed || motionState.RightLimitTriggered;

            builder.AppendLine();
            builder.AppendLine("【当前自动往返运行态】");
            builder.AppendLine("- 虚拟位置：" + motionState.Position.ToString("0") + " / 100");
            builder.AppendLine("- 虚拟运动：" + MotionDirectionDisplayText(motionState.Direction));
            builder.AppendLine("- 虚拟左限位：" + (motionState.LeftLimitTriggered ? "触发" : "未触发"));
            builder.AppendLine("- 虚拟右限位：" + (motionState.RightLimitTriggered ? "触发" : "未触发"));
            builder.AppendLine("- SQ 手动状态：左 SQ " + (leftLimit.IsClosed ? "人工触发" : "未人工触发") +
                "，右 SQ " + (rightLimit.IsClosed ? "人工触发" : "未人工触发") + "。");
            builder.AppendLine("- SQ 有效触发：左 SQ " + (leftEffectiveTriggered ? "触发" : "未触发") +
                "，右 SQ " + (rightEffectiveTriggered ? "触发" : "未触发") + "。");
            builder.AppendLine("- 正转接触器 KM_forward：" + (forwardContactor.IsEnergized ? "得电" : "未得电"));
            builder.AppendLine("- 反转接触器 KM_reverse：" + (reverseContactor.IsEnergized ? "得电" : "未得电"));
            builder.AppendLine("- 电机：" + AutoReciprocatingMotorDisplayText(motor, motionState));

            if (motionState.RightLimitTriggered)
            {
                builder.AppendLine("- 说明：到达右端后，右限位的有效触发状态使正转支路断开、反转支路接通，电机切换为反向运行。");
            }
            else if (motionState.LeftLimitTriggered)
            {
                builder.AppendLine("- 说明：到达左端后，左限位的有效触发状态使反转支路断开、正转支路接通，电机切换为正向运行。");
            }
            else if (motionState.Direction == MotionDirection.Forward)
            {
                builder.AppendLine("- 说明：正转时，电机带动机构向右移动；尚未到达右限位。");
            }
            else if (motionState.Direction == MotionDirection.Reverse)
            {
                builder.AppendLine("- 说明：反转时，电机带动机构向左移动；尚未到达左限位。");
            }
            else
            {
                builder.AppendLine("- 说明：当前控制回路未驱动自动往返电机，虚拟位置保持不变。");
            }

            builder.AppendLine("- 注意：SQ 元件本身的手动状态表示人工触发；自动往返中的左/右限位来自虚拟位置到达边界后的运行态触发。两者都会参与有效触发判断，但虚拟触发不会覆盖手动状态。");
            builder.AppendLine();
            return true;
        }

        private CircuitComponent FindWorkspaceComponent(string instanceId)
        {
            if (workspace == null || workspace.Components == null || string.IsNullOrWhiteSpace(instanceId))
            {
                return null;
            }

            for (var i = 0; i < workspace.Components.Count; i++)
            {
                var component = workspace.Components[i];
                if (component != null &&
                    string.Equals(component.InstanceId, instanceId, StringComparison.OrdinalIgnoreCase))
                {
                    return component;
                }
            }

            return null;
        }

        private static string MotionDirectionDisplayText(MotionDirection direction)
        {
            switch (direction)
            {
                case MotionDirection.Forward:
                    return "正向";
                case MotionDirection.Reverse:
                    return "反向";
                default:
                    return "停止";
            }
        }

        private static string AutoReciprocatingMotorDisplayText(CircuitComponent motor, MotionRuntimeState motionState)
        {
            if (motor == null || !motor.IsEnergized)
            {
                return "停止";
            }

            if (motionState != null && motionState.Direction == MotionDirection.Forward)
            {
                return "正向运行";
            }

            if (motionState != null && motionState.Direction == MotionDirection.Reverse)
            {
                return "反向运行";
            }

            return "运行中";
        }

        private static string ContactorPairDisplayText(CircuitComponent forwardContactor, CircuitComponent reverseContactor)
        {
            var forwardText = forwardContactor != null && forwardContactor.IsEnergized ? "得电" : "未得电";
            var reverseText = reverseContactor != null && reverseContactor.IsEnergized ? "得电" : "未得电";
            return "正转接触器" + forwardText + "，反转接触器" + reverseText;
        }

        private static string AutoReciprocatingLimitTeachingText(MotionRuntimeState motionState)
        {
            if (motionState == null)
            {
                return "当前未读取到虚拟运动状态。";
            }

            if (motionState.RightLimitTriggered)
            {
                return "已到达右限位；虚拟右限位使正转支路断开，并切换为反向运行。";
            }

            if (motionState.LeftLimitTriggered)
            {
                return "已到达左限位；虚拟左限位使反转支路断开，并切换为正向运行。";
            }

            if (motionState.Direction == MotionDirection.Forward)
            {
                return "尚未到达右限位；到达右端后将通过虚拟右限位切换为反向运行。";
            }

            if (motionState.Direction == MotionDirection.Reverse)
            {
                return "尚未到达左限位；到达左端后将通过虚拟左限位切换为正向运行。";
            }

            return "当前电机停止，虚拟位置保持不变。";
        }

        private static string RuntimeDisplayState(ComponentStateInfo component)
        {
            switch (component.State)
            {
                case "Running":
                    return "运行中";
                case "Stopped":
                    return "停止";
                case "StarConnected":
                    return "星形启动条件成立";
                case "DeltaConnected":
                    return "三角运行条件成立";
                case "StarDeltaConflict":
                    return "危险：星三角冲突";
                default:
                    return string.IsNullOrWhiteSpace(component.State) ? "状态未明确" : component.State;
            }
        }

        private static string TimerPhaseDisplayText(TimerRuntimePhase phase)
        {
            switch (phase)
            {
                case TimerRuntimePhase.Timing:
                    return "Timing / 计时中";
                case TimerRuntimePhase.Elapsed:
                    return "Elapsed";
                default:
                    return "Reset";
            }
        }

        private static CircuitCheckResult FilterCheckPanelFalsePositives(
            CircuitCheckResult ruleResult,
            CircuitStateResult stateResult)
        {
            if (ruleResult == null || stateResult == null)
            {
                return ruleResult;
            }

            var filtered = new CircuitCheckResult();
            var hasTwoWaySwitch = HasTwoWaySwitch(stateResult);
            var requireBreaker = ShouldRequireBreakerForCurrentTemplate();
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
                var isOptionalBreakerReminder = issue != null &&
                    issue.code == "NO_BREAKER" &&
                    !requireBreaker;
                var isTwoWayControlFalsePositive = issue != null &&
                    hasTwoWaySwitch &&
                    (issue.code == "LoadLivePathWithoutSwitch" ||
                     issue.code == "ParallelLoadBypassedControl" ||
                     issue.code == "SwitchBypassed" ||
                     issue.code == "SingleSwitchTerminalMiswired" ||
                     issue.code == "OPEN_DEVICE");
                var isStoppedLoadControlFalsePositive = issue != null &&
                    (issue.code == "LoadLivePathWithoutSwitch" ||
                     issue.code == "ParallelLoadBypassedControl") &&
                    IsStoppedLoadIssue(issue, stateResult);
                if (isBreakerCompletenessFalsePositive ||
                    isBreakerDirectControlFalsePositive ||
                    isOptionalBreakerReminder ||
                    isTwoWayControlFalsePositive ||
                    isStoppedLoadControlFalsePositive)
                {
                    continue;
                }

                filtered.Add(issue);
            }

            return filtered;
        }

        private static bool HasTwoWaySwitch(CircuitStateResult stateResult)
        {
            for (var i = 0; i < stateResult.Components.Count; i++)
            {
                if (stateResult.Components[i].IsTwoWaySwitch)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsStoppedLoadIssue(CircuitIssue issue, CircuitStateResult stateResult)
        {
            if (issue == null || string.IsNullOrWhiteSpace(issue.componentId))
            {
                return false;
            }

            var component = stateResult.FindComponent(issue.componentId);
            if (component == null || component.SummaryGroup != ComponentStateInfo.GroupLoad)
            {
                return false;
            }

            return component.State != "On" &&
                component.State != "Running" &&
                component.State != "Forward" &&
                component.State != "Reverse" &&
                component.State != "StarConnected" &&
                component.State != "DeltaConnected";
        }

        private static bool ShouldRequireBreakerForCurrentTemplate()
        {
            var identity = ElectricalSim.UI.TemplateEditSession.CurrentTemplateId + " " +
                ElectricalSim.UI.TemplateEditSession.CurrentTemplateName;
            var practiceController = ElectricalSim.Practice.PracticeSessionController.Instance;
            if (practiceController != null &&
                practiceController.IsPracticeActive &&
                practiceController.CurrentTemplateItem != null)
            {
                identity += " " + practiceController.CurrentTemplateItem.templateId +
                    " " + practiceController.CurrentTemplateItem.templateName;
            }

            return identity.IndexOf("breaker", StringComparison.OrdinalIgnoreCase) >= 0 ||
                identity.Contains("空气开关") ||
                identity.Contains("空开");
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
            service.Ask(question, summary, AddAssistantMessage, error => AddAssistantMessage(string.IsNullOrWhiteSpace(error) ? "检查助手暂时不可用，请稍后再试。" : error));
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
            AddMessage("检查助手", message, false);
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
