using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using ElectricalSim.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ElectricalSim.Diagnostics
{
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    public sealed class UiTypographyRuntimeAuditRunner : MonoBehaviour
    {
        private const string ReportRelativeDirectory = "Reports/UI/TypographyRuntimeAudit";

        private static readonly int[] AuditPageIndices = { 0, 1, 2, 3, 4, 5 };
        private static readonly string[] AuditPageNames =
        {
            "模拟电路",
            "图纸集",
            "仿真广场",
            "元器件百科",
            "常用工具",
            "系统信息"
        };

        private static readonly Dictionary<PageId, string> PageRootFieldMap = new Dictionary<PageId, string>
        {
            { PageId.Simulation, "simulationRoot" },
            { PageId.Blueprint, "blueprintRoot" },
            { PageId.Square, "squareRoot" },
            { PageId.Encyclopedia, "encyclopediaRoot" },
            { PageId.Tools, "toolsRoot" },
            { PageId.Profile, "profileRoot" }
        };

        private readonly List<AuditRow> rows = new List<AuditRow>(512);
        private readonly List<CanvasInfo> canvasInfos = new List<CanvasInfo>(8);
        private readonly List<CanvasScalerInfo> canvasScalerInfos = new List<CanvasScalerInfo>(8);
        private readonly List<string> screenshotPaths = new List<string>(8);
        private readonly HashSet<string> uniqueKeys = new HashSet<string>();

        private bool isRunning;

        public void BeginAudit()
        {
            if (isRunning)
            {
                Debug.LogWarning("UI 字体运行时审计已在执行中。");
                return;
            }

            isRunning = true;
            StartCoroutine(RunAudit());
        }

        private IEnumerator RunAudit()
        {
            rows.Clear();
            canvasInfos.Clear();
            canvasScalerInfos.Clear();
            screenshotPaths.Clear();
            uniqueKeys.Clear();

            var navigation = FindObjectOfType<TopNavigationController>();
            var pageRouter = FindObjectOfType<PageRouter>();

            if (navigation == null)
            {
                Debug.LogError("UI 字体运行时审计失败：未找到 TopNavigationController。");
                Finish();
                yield break;
            }

            if (pageRouter == null)
            {
                Debug.LogError("UI 字体运行时审计失败：未找到 PageRouter。");
                Finish();
                yield break;
            }

            var originalPageIndex = PageIdToIndex(pageRouter.CurrentPage);
            CollectEnvironmentInfo();

            for (var i = 0; i < AuditPageIndices.Length; i++)
            {
                var pageIndex = AuditPageIndices[i];
                var pageName = AuditPageNames[i];

                navigation.SelectTab(pageIndex);
                yield return null;
                yield return null;

                Canvas.ForceUpdateCanvases();
                ForceRebuildCurrentPage(pageRouter);
                Canvas.ForceUpdateCanvases();
                yield return null;

                yield return CapturePageScreenshot(i, pageName);
                CollectTextRowsForCurrentPage(pageIndex, pageName);
            }

            navigation.SelectTab(originalPageIndex);
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();
            ForceRebuildCurrentPage(pageRouter);
            Canvas.ForceUpdateCanvases();
            yield return null;

            WriteOutputs();
            Finish();
        }

        private void Finish()
        {
            isRunning = false;
            if (this != null && gameObject != null)
            {
                Destroy(gameObject);
            }
        }

        private void CollectEnvironmentInfo()
        {
            canvasInfos.Clear();
            canvasScalerInfos.Clear();

            foreach (var canvas in Resources.FindObjectsOfTypeAll<Canvas>())
            {
                if (canvas == null || !canvas.gameObject.scene.IsValid())
                {
                    continue;
                }

                canvasInfos.Add(new CanvasInfo
                {
                    Name = canvas.name,
                    RenderMode = canvas.renderMode.ToString(),
                    ScaleFactor = canvas.scaleFactor,
                    PixelRect = RectToString(canvas.pixelRect)
                });

                var scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler == null)
                {
                    continue;
                }

                canvasScalerInfos.Add(new CanvasScalerInfo
                {
                    Name = canvas.name,
                    UiScaleMode = scaler.uiScaleMode.ToString(),
                    ReferenceResolution = Vector2ToString(scaler.referenceResolution),
                    ScreenMatchMode = scaler.screenMatchMode.ToString(),
                    MatchWidthOrHeight = scaler.matchWidthOrHeight,
                    ReferencePixelsPerUnit = scaler.referencePixelsPerUnit
                });
            }
        }

        private IEnumerator CapturePageScreenshot(int pageOrder, string pageName)
        {
            var outputDir = GetOutputDirectory();
            Directory.CreateDirectory(outputDir);

            var fileName = string.Format(
                CultureInfo.InvariantCulture,
                "{0:D2}_{1}_{2}x{3}.png",
                pageOrder,
                pageName,
                Screen.width,
                Screen.height);
            var path = Path.Combine(outputDir, fileName);

            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(path);
            yield return new WaitForEndOfFrame();

            screenshotPaths.Add(path);
        }

        private void CollectTextRowsForCurrentPage(int pageIndex, string pageName)
        {
            foreach (var text in Resources.FindObjectsOfTypeAll<Text>())
            {
                if (text == null)
                {
                    continue;
                }

                var go = text.gameObject;
                if (go == null || !go.scene.IsValid())
                {
                    continue;
                }

                if (!go.activeInHierarchy)
                {
                    continue;
                }

                var uniqueKey = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}_{1}",
                    pageIndex,
                    text.GetInstanceID());
                if (!uniqueKeys.Add(uniqueKey))
                {
                    continue;
                }

                try
                {
                    rows.Add(BuildAuditRow(text, pageIndex, pageName));
                }
                catch (Exception ex)
                {
                    Debug.LogWarningFormat(
                        "UI 字体运行时审计：采集控件失败。Page={0} Object={1} Error={2}",
                        pageName,
                        go.name,
                        ex.Message);
                }
            }
        }

        private AuditRow BuildAuditRow(Text text, int pageIndex, string pageName)
        {
            var rectTransform = text.rectTransform;
            var textRectWidth = rectTransform != null ? rectTransform.rect.width : 0f;
            var textRectHeight = rectTransform != null ? rectTransform.rect.height : 0f;
            var content = text.text ?? string.Empty;
            var hierarchyPath = BuildHierarchyPath(text.transform);
            var currentFont = text.font;
            var candidateFont = MainUiTheme.UiFont;

            var currentMeasurement = MeasureText(text, currentFont, textRectWidth, textRectHeight);
            var candidateMeasurement = MeasureText(text, candidateFont, textRectWidth, textRectHeight);

            var currentWidthUsageRatio = SafeRatio(currentMeasurement.PreferredWidth, textRectWidth);
            var currentHeightUsageRatio = SafeRatio(currentMeasurement.PreferredHeight, textRectHeight);
            var candidateWidthUsageRatio = SafeRatio(candidateMeasurement.PreferredWidth, textRectWidth);
            var candidateHeightUsageRatio = SafeRatio(candidateMeasurement.PreferredHeight, textRectHeight);

            var lineCountIncreased = candidateMeasurement.LineCount > currentMeasurement.LineCount;
            var currentOverflow = IsOverflowing(currentWidthUsageRatio) || IsOverflowing(currentHeightUsageRatio);

            var button = FindNearestParentButton(text);
            var buttonInfo = button != null ? BuildButtonInfo(button, text) : ButtonInfo.Empty;
            var layoutContext = BuildLayoutContext(text);

            var category = ResolveAuditCategory(pageName, hierarchyPath, content, button != null);
            var controlType = ResolveControlType(category, button != null, content);
            var fontRole = ResolveSuggestedFontRole(category, button != null);
            var parentRect = rectTransform != null && rectTransform.parent is RectTransform parentRt
                ? parentRt
                : null;

            var row = new AuditRow
            {
                PageIndex = pageIndex,
                PageName = pageName,
                SceneName = text.gameObject.scene.name,
                HierarchyPath = hierarchyPath,
                GameObjectName = text.gameObject.name,
                ActiveSelf = text.gameObject.activeSelf,
                ActiveInHierarchy = text.gameObject.activeInHierarchy,
                InstanceID = text.GetInstanceID(),
                TextContent = EscapeTextContent(content),
                TextLength = content.Length,
                ChineseCharacterCount = CountChineseCharacters(content),
                HasNewLine = content.Contains("\n"),
                CurrentFontName = currentFont != null ? currentFont.name : "(null)",
                FontSize = text.fontSize,
                FontStyle = text.fontStyle.ToString(),
                LineSpacing = text.lineSpacing,
                Alignment = text.alignment.ToString(),
                HorizontalOverflow = text.horizontalOverflow.ToString(),
                VerticalOverflow = text.verticalOverflow.ToString(),
                ResizeTextForBestFit = text.resizeTextForBestFit,
                ResizeTextMinSize = text.resizeTextMinSize,
                ResizeTextMaxSize = text.resizeTextMaxSize,
                FontSizeUsedForBestFit = ResolveBestFitFontSize(text, textRectWidth, textRectHeight),
                SupportRichText = text.supportRichText,
                TextRectWidth = textRectWidth,
                TextRectHeight = textRectHeight,
                AnchorMin = Vector2ToString(rectTransform.anchorMin),
                AnchorMax = Vector2ToString(rectTransform.anchorMax),
                Pivot = Vector2ToString(rectTransform.pivot),
                SizeDelta = Vector2ToString(rectTransform.sizeDelta),
                AnchoredPosition = Vector2ToString(rectTransform.anchoredPosition),
                OffsetMin = Vector2ToString(rectTransform.offsetMin),
                OffsetMax = Vector2ToString(rectTransform.offsetMax),
                LocalScale = Vector3ToString(rectTransform.localScale),
                ParentRectWidth = parentRect != null ? parentRect.rect.width : 0f,
                ParentRectHeight = parentRect != null ? parentRect.rect.height : 0f,
                ParentLayoutGroupType = layoutContext.ParentLayoutGroupType,
                HasContentSizeFitter = layoutContext.HasContentSizeFitter,
                HasLayoutElement = layoutContext.HasLayoutElement,
                IsUnderMask = layoutContext.IsUnderMask,
                IsUnderRectMask2D = layoutContext.IsUnderRectMask2D,
                IsUnderScrollRect = layoutContext.IsUnderScrollRect,
                CurrentPreferredWidth = currentMeasurement.PreferredWidth,
                CurrentPreferredHeight = currentMeasurement.PreferredHeight,
                CurrentGeneratedLineCount = currentMeasurement.LineCount,
                CurrentWidthUsageRatio = currentWidthUsageRatio,
                CurrentHeightUsageRatio = currentHeightUsageRatio,
                CandidateFontName = candidateFont != null ? candidateFont.name : "(null)",
                CandidatePreferredWidth = candidateMeasurement.PreferredWidth,
                CandidatePreferredHeight = candidateMeasurement.PreferredHeight,
                CandidateGeneratedLineCount = candidateMeasurement.LineCount,
                CandidateWidthUsageRatio = candidateWidthUsageRatio,
                CandidateHeightUsageRatio = candidateHeightUsageRatio,
                CandidateWidthDeltaPercent = SafeDeltaPercent(candidateMeasurement.PreferredWidth, currentMeasurement.PreferredWidth),
                CandidateHeightDeltaPercent = SafeDeltaPercent(candidateMeasurement.PreferredHeight, currentMeasurement.PreferredHeight),
                ParentButtonPath = buttonInfo.Path,
                ButtonRectWidth = buttonInfo.RectWidth,
                ButtonRectHeight = buttonInfo.RectHeight,
                ButtonHasLayoutElement = buttonInfo.HasLayoutElement,
                ButtonLayoutMinWidth = buttonInfo.LayoutMinWidth,
                ButtonLayoutPreferredWidth = buttonInfo.LayoutPreferredWidth,
                ButtonLayoutFlexibleWidth = buttonInfo.LayoutFlexibleWidth,
                ButtonLayoutMinHeight = buttonInfo.LayoutMinHeight,
                ButtonLayoutPreferredHeight = buttonInfo.LayoutPreferredHeight,
                ButtonLayoutFlexibleHeight = buttonInfo.LayoutFlexibleHeight,
                ButtonInteractable = buttonInfo.Interactable,
                IconCount = buttonInfo.IconCount,
                IconNames = buttonInfo.IconNames,
                MaximumIconWidth = buttonInfo.MaximumIconWidth,
                SumIconWidths = buttonInfo.SumIconWidths,
                TextAvailableWidth = textRectWidth,
                AuditCategory = category,
                ControlType = controlType,
                SuggestedFontRole = fontRole,
                AllowsWrapping = text.horizontalOverflow == HorizontalWrapMode.Wrap,
                LineCountIncreased = lineCountIncreased,
                CurrentOverflow = currentOverflow
            };

            ApplyRiskAssessment(row);
            return row;
        }

        private Button FindNearestParentButton(Text text)
        {
            if (text == null)
            {
                return null;
            }

            var current = text.transform;
            while (current != null)
            {
                var button = current.GetComponent<Button>();
                if (button != null)
                {
                    return button;
                }

                current = current.parent;
            }

            return null;
        }

        private ButtonInfo BuildButtonInfo(Button button, Text text)
        {
            var rectTransform = button.GetComponent<RectTransform>();
            var layout = button.GetComponent<LayoutElement>();
            var buttonWidth = rectTransform != null ? rectTransform.rect.width : 0f;
            var buttonHeight = rectTransform != null ? rectTransform.rect.height : 0f;

            var iconCandidates = new List<Image>();
            foreach (var image in button.GetComponentsInChildren<Image>(true))
            {
                if (image == null || image.gameObject == button.gameObject || image.gameObject == text.gameObject)
                {
                    continue;
                }

                if (!image.gameObject.activeInHierarchy)
                {
                    continue;
                }

                var imageRect = image.rectTransform;
                if (imageRect == null)
                {
                    continue;
                }

                var width = imageRect.rect.width;
                var height = imageRect.rect.height;
                if (width <= 0f || height <= 0f)
                {
                    continue;
                }

                if (buttonWidth > 0f && buttonHeight > 0f && width >= buttonWidth * 0.8f && height >= buttonHeight * 0.8f)
                {
                    continue;
                }

                iconCandidates.Add(image);
            }

            var prioritizedIcons = iconCandidates
                .OrderByDescending(img => HasIconLikeName(img.gameObject.name))
                .ThenBy(img => img.gameObject.name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return new ButtonInfo
            {
                Path = BuildHierarchyPath(button.transform),
                RectWidth = buttonWidth,
                RectHeight = buttonHeight,
                HasLayoutElement = layout != null,
                LayoutMinWidth = layout != null ? layout.minWidth : 0f,
                LayoutPreferredWidth = layout != null ? layout.preferredWidth : 0f,
                LayoutFlexibleWidth = layout != null ? layout.flexibleWidth : 0f,
                LayoutMinHeight = layout != null ? layout.minHeight : 0f,
                LayoutPreferredHeight = layout != null ? layout.preferredHeight : 0f,
                LayoutFlexibleHeight = layout != null ? layout.flexibleHeight : 0f,
                Interactable = button.interactable,
                IconCount = prioritizedIcons.Count,
                IconNames = string.Join("|", prioritizedIcons.Select(img => img.gameObject.name).ToArray()),
                MaximumIconWidth = prioritizedIcons.Count == 0 ? 0f : prioritizedIcons.Max(img => img.rectTransform.rect.width),
                SumIconWidths = prioritizedIcons.Sum(img => img.rectTransform.rect.width)
            };
        }

        private LayoutContext BuildLayoutContext(Text text)
        {
            var context = new LayoutContext();
            var current = text.transform.parent;

            while (current != null)
            {
                if (string.IsNullOrEmpty(context.ParentLayoutGroupType))
                {
                    if (current.GetComponent<HorizontalLayoutGroup>() != null)
                    {
                        context.ParentLayoutGroupType = nameof(HorizontalLayoutGroup);
                    }
                    else if (current.GetComponent<VerticalLayoutGroup>() != null)
                    {
                        context.ParentLayoutGroupType = nameof(VerticalLayoutGroup);
                    }
                    else if (current.GetComponent<GridLayoutGroup>() != null)
                    {
                        context.ParentLayoutGroupType = nameof(GridLayoutGroup);
                    }
                }

                context.HasContentSizeFitter |= current.GetComponent<ContentSizeFitter>() != null;
                context.HasLayoutElement |= current.GetComponent<LayoutElement>() != null;
                context.IsUnderMask |= current.GetComponent<Mask>() != null;
                context.IsUnderRectMask2D |= current.GetComponent<RectMask2D>() != null;
                context.IsUnderScrollRect |= current.GetComponent<ScrollRect>() != null;

                current = current.parent;
            }

            return context;
        }

        private MeasurementResult MeasureText(Text text, Font font, float width, float height)
        {
            var result = new MeasurementResult();
            if (text == null || font == null)
            {
                return result;
            }

            var extents = new Vector2(Mathf.Max(width, 0f), Mathf.Max(height, 0f));
            if (extents.x <= 0f)
            {
                extents.x = 8192f;
            }

            if (extents.y <= 0f)
            {
                extents.y = 8192f;
            }

            var settings = text.GetGenerationSettings(extents);
            settings.font = font;
            settings.resizeTextForBestFit = text.resizeTextForBestFit;
            settings.resizeTextMinSize = text.resizeTextMinSize;
            settings.resizeTextMaxSize = text.resizeTextMaxSize;
            settings.fontSize = text.fontSize;
            settings.fontStyle = text.fontStyle;
            settings.lineSpacing = text.lineSpacing;
            settings.richText = text.supportRichText;
            settings.horizontalOverflow = text.horizontalOverflow;
            settings.verticalOverflow = text.verticalOverflow;
            settings.scaleFactor = 1f;
            settings.pivot = text.rectTransform != null ? text.rectTransform.pivot : new Vector2(0.5f, 0.5f);

            var content = text.text ?? string.Empty;
            var generator = new TextGenerator(content.Length + 32);
            generator.Populate(content, settings);

            result.PreferredWidth = generator.GetPreferredWidth(content, settings);
            result.PreferredHeight = generator.GetPreferredHeight(content, settings);
            result.LineCount = generator.lineCount;
            return result;
        }

        private string ResolveBestFitFontSize(Text text, float width, float height)
        {
            if (text == null)
            {
                return "N/A";
            }

            try
            {
                var extents = new Vector2(Mathf.Max(width, 1f), Mathf.Max(height, 1f));
                var settings = text.GetGenerationSettings(extents);
                text.cachedTextGenerator.Populate(text.text ?? string.Empty, settings);

                var property = typeof(TextGenerator).GetProperty("fontSizeUsedForBestFit", BindingFlags.Instance | BindingFlags.Public);
                if (property == null)
                {
                    return "N/A";
                }

                var value = property.GetValue(text.cachedTextGenerator, null);
                return value != null ? Convert.ToString(value, CultureInfo.InvariantCulture) : "N/A";
            }
            catch
            {
                return "N/A";
            }
        }

        private void ApplyRiskAssessment(AuditRow row)
        {
            var isSingleLine = IsSingleLineCategory(row.AuditCategory, row.ControlType);
            var riskScore = 1;
            var reasons = new List<string>();

            if (row.CurrentOverflow)
            {
                riskScore = Mathf.Max(riskScore, 3);
                reasons.Add("当前字体已接近或超过容器边界");
            }

            if (row.ResizeTextForBestFit && TryParseFloat(row.FontSizeUsedForBestFit, out var bestFitSize) && bestFitSize <= row.FontSize - 2f)
            {
                riskScore = Mathf.Max(riskScore, 3);
                reasons.Add("当前控件依赖 BestFit 压缩字号");
            }

            if (isSingleLine)
            {
                if (row.CandidateWidthUsageRatio >= 1f || row.CandidateHeightUsageRatio >= 1f)
                {
                    riskScore = Mathf.Max(riskScore, 3);
                    reasons.Add("候选字体会直接顶满按钮或标签容器");
                }
                else if (row.IsUnderRectMask2D && row.CandidateWidthUsageRatio >= 0.95f)
                {
                    riskScore = Mathf.Max(riskScore, 3);
                    reasons.Add("遮罩区域中的单行文本接近裁切阈值");
                }
                else if (row.CandidateWidthUsageRatio >= 0.90f)
                {
                    riskScore = Mathf.Max(riskScore, 3);
                    reasons.Add("候选字体宽度占用率已达高风险区间");
                }
                else if (row.CandidateWidthUsageRatio >= 0.80f)
                {
                    riskScore = Mathf.Max(riskScore, 2);
                    reasons.Add("候选字体宽度占用率已进入中风险区间");
                }
            }
            else
            {
                if (row.CandidateHeightUsageRatio >= 1f)
                {
                    riskScore = Mathf.Max(riskScore, 3);
                    reasons.Add("候选字体高度会超出正文或标题容器");
                }
                else if (row.CandidateHeightUsageRatio >= 0.90f)
                {
                    riskScore = Mathf.Max(riskScore, 3);
                    reasons.Add("候选字体高度占用率已达高风险区间");
                }
                else if (row.CandidateHeightUsageRatio >= 0.80f)
                {
                    riskScore = Mathf.Max(riskScore, 2);
                    reasons.Add("候选字体高度占用率已进入中风险区间");
                }

                if (row.LineCountIncreased)
                {
                    riskScore = Mathf.Min(3, riskScore + 1);
                    reasons.Add("候选字体会增加换行行数");
                }
            }

            row.RiskScore = riskScore;
            row.RiskLevel = RiskScoreToText(riskScore);
            row.RiskReason = reasons.Count == 0 ? "候选字体替换后边界仍较安全" : string.Join("；", reasons.ToArray());
        }

        private string ResolveAuditCategory(string pageName, string hierarchyPath, string content, bool hasParentButton)
        {
            var path = hierarchyPath.ToLowerInvariant();

            if (content.Contains("电工数字学生仿真系统"))
            {
                return "BrandTitle";
            }

            if (pageName == "模拟电路")
            {
                if (path.Contains("topnavigation") || path.Contains("navbar") || path.Contains("nav"))
                {
                    return "TopNavigation";
                }

                if (content.Contains("导线颜色"))
                {
                    return "WireColorLabel";
                }

                if (path.Contains("toolbar") || path.Contains("tool"))
                {
                    return "SimulationToolbar";
                }

                if (path.Contains("actionlog") || content.Contains("操作记录"))
                {
                    return "ActionLog";
                }

                if (path.Contains("palette") || content.Contains("电工控件池"))
                {
                    return hasParentButton ? "PaletteFilter" : "PaletteTitle";
                }

                if (path.Contains("inspector") || content.Contains("检查助手"))
                {
                    if (hasParentButton)
                    {
                        return "InspectorButton";
                    }

                    return content.Contains("检查助手") ? "InspectorTitle" : "InspectorBody";
                }
            }

            if (pageName == "图纸集")
            {
                if (hasParentButton && (content.Contains("进入练习") || content.Contains("上一页") || content.Contains("下一页")))
                {
                    return "BlueprintCardButton";
                }

                if (content.Contains("搜索") || path.Contains("search"))
                {
                    return "BlueprintSearch";
                }

                if (hasParentButton)
                {
                    return "BlueprintFilter";
                }

                if (path.Contains("card") || path.Contains("item"))
                {
                    return "BlueprintCardTitle";
                }

                if (content.Contains("图纸集"))
                {
                    return "PageTitle";
                }
            }

            if (pageName == "仿真广场")
            {
                if (content.Contains("查看详情") || content.Contains("加载案例") || content.Contains("返回广场") || content.Contains("加载到画布"))
                {
                    return "GalleryCardButton";
                }

                if (content.Contains("搜索") || path.Contains("search"))
                {
                    return "GallerySearch";
                }

                if (path.Contains("dropdown") || content.Contains("排序"))
                {
                    return "GallerySort";
                }

                if (hasParentButton)
                {
                    return "GalleryFilter";
                }

                if (path.Contains("card") || path.Contains("item"))
                {
                    return content.Length <= 20 ? "GalleryCardTitle" : "GalleryCardMeta";
                }

                if (content.Contains("仿真广场"))
                {
                    return "GalleryPageTitle";
                }
            }

            if (pageName == "元器件百科")
            {
                return "Encyclopedia";
            }

            if (pageName == "常用工具")
            {
                return "CommonTools";
            }

            if (pageName == "系统信息")
            {
                return "Profile";
            }

            if (hasParentButton)
            {
                return "Dialog";
            }

            return "Other";
        }

        private string ResolveControlType(string category, bool hasParentButton, string content)
        {
            if (hasParentButton)
            {
                return "Button";
            }

            if (category.EndsWith("Title", StringComparison.Ordinal))
            {
                return "PanelTitle";
            }

            if (category.EndsWith("Body", StringComparison.Ordinal))
            {
                return "CardBody";
            }

            if (content.Contains("搜索"))
            {
                return "SearchInput";
            }

            return "Label";
        }

        private string ResolveSuggestedFontRole(string category, bool hasParentButton)
        {
            if (category == "BrandTitle")
            {
                return "AppTitle";
            }

            if (category == "TopNavigation")
            {
                return "NavText";
            }

            if (category == "SimulationToolbar")
            {
                return hasParentButton ? "ToolbarButtonText" : "SectionTitle";
            }

            if (category == "WireColorLabel")
            {
                return "SectionTitle";
            }

            if (category == "PaletteTitle")
            {
                return "SectionTitle";
            }

            if (category == "PaletteFilter")
            {
                return "NavText";
            }

            if (category == "ActionLog")
            {
                return "LogBody";
            }

            if (category == "InspectorTitle")
            {
                return "InspectorTitle";
            }

            if (category == "InspectorButton")
            {
                return "InspectorButton";
            }

            if (category == "InspectorBody")
            {
                return "InspectorBody";
            }

            if (category == "BlueprintCardTitle" || category == "GalleryCardTitle")
            {
                return "CardTitle";
            }

            if (category == "BlueprintCardButton" || category == "GalleryCardButton")
            {
                return "DialogButton";
            }

            if (category == "BlueprintSearch" || category == "GallerySearch" || category == "GallerySort")
            {
                return "Caption";
            }

            if (category == "GalleryPageTitle")
            {
                return "PageTitle";
            }

            if (category == "Encyclopedia" || category == "CommonTools" || category == "Profile")
            {
                return "CardBody";
            }

            return hasParentButton ? "DialogButton" : "CardBody";
        }

        private bool IsSingleLineCategory(string category, string controlType)
        {
            if (controlType == "Button")
            {
                return true;
            }

            switch (category)
            {
                case "BrandTitle":
                case "TopNavigation":
                case "SimulationToolbar":
                case "WireColorLabel":
                case "PaletteTitle":
                case "PaletteFilter":
                case "InspectorTitle":
                case "InspectorButton":
                case "BlueprintFilter":
                case "BlueprintSearch":
                case "BlueprintCardButton":
                case "BlueprintPagination":
                case "GalleryFilter":
                case "GallerySearch":
                case "GallerySort":
                case "GalleryCardButton":
                case "PageTitle":
                    return true;
                default:
                    return false;
            }
        }

        private void ForceRebuildCurrentPage(PageRouter pageRouter)
        {
            if (pageRouter == null)
            {
                return;
            }

            var root = ResolveCurrentPageRoot(pageRouter);
            if (root == null)
            {
                return;
            }

            var rootRect = root.GetComponent<RectTransform>();
            if (rootRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);
            }

            foreach (var layoutGroup in root.GetComponentsInChildren<LayoutGroup>(true))
            {
                if (layoutGroup == null || !layoutGroup.gameObject.activeInHierarchy)
                {
                    continue;
                }

                var rect = layoutGroup.GetComponent<RectTransform>();
                if (rect != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
                }
            }
        }

        private GameObject ResolveCurrentPageRoot(PageRouter pageRouter)
        {
            if (!PageRootFieldMap.TryGetValue(pageRouter.CurrentPage, out var fieldName))
            {
                return null;
            }

            var field = typeof(PageRouter).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            return field != null ? field.GetValue(pageRouter) as GameObject : null;
        }

        private void WriteOutputs()
        {
            var outputDir = GetOutputDirectory();
            Directory.CreateDirectory(outputDir);

            var resolutionToken = string.Format(CultureInfo.InvariantCulture, "{0}x{1}", Screen.width, Screen.height);
            var csvPath = Path.Combine(outputDir, "UI_Font_Runtime_Audit_" + resolutionToken + ".csv");
            var summaryPath = Path.Combine(outputDir, "UI_Font_Runtime_Audit_" + resolutionToken + "_Summary.md");
            var highRiskCsvPath = Path.Combine(outputDir, "UI_Font_Runtime_Audit_" + resolutionToken + "_HighRiskOnly.csv");
            var highRiskSummaryPath = Path.Combine(outputDir, "UI_Font_Runtime_Audit_" + resolutionToken + "_HighRiskOnly_Summary.md");

            WriteCsv(csvPath, rows);
            WriteSummary(summaryPath, rows, "完整字体运行时审计");

            var highRiskRows = rows.Where(row => row.RiskLevel == "高").ToList();
            WriteCsv(highRiskCsvPath, highRiskRows);
            WriteSummary(highRiskSummaryPath, highRiskRows, "高风险控件二次筛选导出");

            Debug.LogFormat(
                "UI 字体运行时审计已导出。目录：{0}，文件数量：{1}",
                outputDir,
                4 + screenshotPaths.Count);
        }

        private void WriteCsv(string path, IList<AuditRow> auditRows)
        {
            var columns = AuditRow.CsvColumns;
            var builder = new StringBuilder(128 * (auditRows.Count + 2));
            builder.AppendLine(string.Join(",", columns.Select(CsvEscape).ToArray()));

            foreach (var row in auditRows)
            {
                builder.AppendLine(row.ToCsvLine());
            }

            File.WriteAllText(path, builder.ToString(), new UTF8Encoding(true));
        }

        private void WriteSummary(string summaryPath, IList<AuditRow> auditRows, string title)
        {
            var builder = new StringBuilder(4096);
            builder.AppendLine("# " + title);
            builder.AppendLine();
            builder.AppendLine("## 运行环境");
            builder.AppendLine();
            builder.AppendLine("- 导出时间：" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            builder.AppendLine("- Unity 版本：" + Application.unityVersion);
            builder.AppendLine("- 平台：" + Application.platform);
            builder.AppendLine("- Scene：" + string.Join(", ", GetLoadedSceneNames().ToArray()));
            builder.AppendLine("- Screen.width：" + Screen.width.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine("- Screen.height：" + Screen.height.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine("- Screen.dpi：" + Screen.dpi.ToString("0.###", CultureInfo.InvariantCulture));
            builder.AppendLine("- Game View：" + Screen.width.ToString(CultureInfo.InvariantCulture) + " × " + Screen.height.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine("- MainUiTheme.TitleFont：" + SafeFontName(MainUiTheme.TitleFont));
            builder.AppendLine("- MainUiTheme.UiFont：" + SafeFontName(MainUiTheme.UiFont));
            builder.AppendLine("- MainUiTheme.BodyFont：" + SafeFontName(MainUiTheme.BodyFont));
            builder.AppendLine();

            builder.AppendLine("## Canvas 信息");
            builder.AppendLine();
            foreach (var canvasInfo in canvasInfos)
            {
                builder.AppendLine(string.Format(
                    CultureInfo.InvariantCulture,
                    "- {0} | renderMode={1} | scaleFactor={2:0.###} | pixelRect={3}",
                    canvasInfo.Name,
                    canvasInfo.RenderMode,
                    canvasInfo.ScaleFactor,
                    canvasInfo.PixelRect));
            }
            builder.AppendLine();

            builder.AppendLine("## CanvasScaler 信息");
            builder.AppendLine();
            foreach (var scalerInfo in canvasScalerInfos)
            {
                builder.AppendLine(string.Format(
                    CultureInfo.InvariantCulture,
                    "- {0} | mode={1} | referenceResolution={2} | screenMatchMode={3} | matchWidthOrHeight={4:0.###} | referencePixelsPerUnit={5:0.###}",
                    scalerInfo.Name,
                    scalerInfo.UiScaleMode,
                    scalerInfo.ReferenceResolution,
                    scalerInfo.ScreenMatchMode,
                    scalerInfo.MatchWidthOrHeight,
                    scalerInfo.ReferencePixelsPerUnit));
            }
            builder.AppendLine();

            var highRiskCount = auditRows.Count(row => row.RiskLevel == "高");
            var mediumRiskCount = auditRows.Count(row => row.RiskLevel == "中");
            var lowRiskCount = auditRows.Count(row => row.RiskLevel == "低");

            builder.AppendLine("## 风险统计");
            builder.AppendLine();
            builder.AppendLine("- Text 总数：" + auditRows.Count.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine("- 高风险：" + highRiskCount.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine("- 中风险：" + mediumRiskCount.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine("- 低风险：" + lowRiskCount.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine();

            builder.AppendLine("## 按页面统计");
            builder.AppendLine();
            foreach (var group in auditRows.GroupBy(row => row.PageName).OrderBy(group => group.Key, StringComparer.Ordinal))
            {
                builder.AppendLine(string.Format(
                    CultureInfo.InvariantCulture,
                    "- {0}: 总数={1}，高={2}，中={3}，低={4}",
                    group.Key,
                    group.Count(),
                    group.Count(row => row.RiskLevel == "高"),
                    group.Count(row => row.RiskLevel == "中"),
                    group.Count(row => row.RiskLevel == "低")));
            }
            builder.AppendLine();

            builder.AppendLine("## 按 AuditCategory 统计");
            builder.AppendLine();
            foreach (var group in auditRows.GroupBy(row => row.AuditCategory).OrderBy(group => group.Key, StringComparer.Ordinal))
            {
                builder.AppendLine(string.Format(
                    CultureInfo.InvariantCulture,
                    "- {0}: 总数={1}，高={2}，中={3}，低={4}",
                    group.Key,
                    group.Count(),
                    group.Count(row => row.RiskLevel == "高"),
                    group.Count(row => row.RiskLevel == "中"),
                    group.Count(row => row.RiskLevel == "低")));
            }
            builder.AppendLine();

            AppendTopList(
                builder,
                "风险最高的前 30 项",
                auditRows.OrderByDescending(row => row.RiskScore)
                    .ThenByDescending(row => row.CandidateWidthUsageRatio)
                    .ThenByDescending(row => row.CandidateHeightUsageRatio)
                    .Take(30));

            AppendTopList(
                builder,
                "候选字体宽度增长最大的前 20 项",
                auditRows.Where(row => !float.IsNaN(row.CandidateWidthDeltaPercent))
                    .OrderByDescending(row => row.CandidateWidthDeltaPercent)
                    .Take(20));

            AppendTopList(
                builder,
                "候选字体导致行数增加的项目",
                auditRows.Where(row => row.LineCountIncreased));

            AppendTopList(
                builder,
                "BestFit 实际缩小字号的项目",
                auditRows.Where(row =>
                {
                    if (!row.ResizeTextForBestFit || !TryParseFloat(row.FontSizeUsedForBestFit, out var actualSize))
                    {
                        return false;
                    }

                    return actualSize < row.FontSize;
                }));

            AppendTopList(
                builder,
                "当前已经发生溢出的项目",
                auditRows.Where(row => row.CurrentOverflow));

            builder.AppendLine("## 建议优先检查的页面顺序");
            builder.AppendLine();
            foreach (var page in auditRows
                         .GroupBy(row => row.PageName)
                         .Select(group => new
                         {
                             PageName = group.Key,
                             WeightedScore = group.Sum(row => ScoreRisk(row.RiskLevel)),
                             HighCount = group.Count(row => row.RiskLevel == "高"),
                             MediumCount = group.Count(row => row.RiskLevel == "中")
                         })
                         .OrderByDescending(item => item.WeightedScore)
                         .ThenByDescending(item => item.HighCount)
                         .ThenByDescending(item => item.MediumCount))
            {
                builder.AppendLine(string.Format(
                    CultureInfo.InvariantCulture,
                    "- {0}: 加权风险={1}，高={2}，中={3}",
                    page.PageName,
                    page.WeightedScore,
                    page.HighCount,
                    page.MediumCount));
            }
            builder.AppendLine();

            builder.AppendLine("## 截图输出");
            builder.AppendLine();
            foreach (var screenshotPath in screenshotPaths)
            {
                builder.AppendLine("- " + screenshotPath);
            }

            File.WriteAllText(summaryPath, builder.ToString(), new UTF8Encoding(true));
        }

        private void AppendTopList(StringBuilder builder, string title, IEnumerable<AuditRow> source)
        {
            builder.AppendLine("## " + title);
            builder.AppendLine();

            var any = false;
            foreach (var row in source)
            {
                any = true;
                builder.AppendLine(string.Format(
                    CultureInfo.InvariantCulture,
                    "- [{0}] {1} | {2} | 风险={3} | 当前占用={4:0.###}/{5:0.###} | 候选占用={6:0.###}/{7:0.###} | 文本={8}",
                    row.PageName,
                    row.AuditCategory,
                    row.HierarchyPath,
                    row.RiskLevel,
                    row.CurrentWidthUsageRatio,
                    row.CurrentHeightUsageRatio,
                    row.CandidateWidthUsageRatio,
                    row.CandidateHeightUsageRatio,
                    row.TextContent));
            }

            if (!any)
            {
                builder.AppendLine("- 无");
            }

            builder.AppendLine();
        }

        private static int ScoreRisk(string riskLevel)
        {
            switch (riskLevel)
            {
                case "高":
                    return 3;
                case "中":
                    return 2;
                default:
                    return 1;
            }
        }

        private static string BuildHierarchyPath(Transform transform)
        {
            if (transform == null)
            {
                return string.Empty;
            }

            var names = new List<string>(16);
            var current = transform;
            while (current != null)
            {
                names.Add(current.name);
                current = current.parent;
            }

            names.Reverse();
            return string.Join("/", names.ToArray());
        }

        private static string EscapeTextContent(string content)
        {
            return (content ?? string.Empty).Replace("\r", "\\r").Replace("\n", "\\n");
        }

        private static int CountChineseCharacters(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return 0;
            }

            var count = 0;
            foreach (var ch in content)
            {
                if ((ch >= 0x3400 && ch <= 0x4DBF) || (ch >= 0x4E00 && ch <= 0x9FFF) || (ch >= 0xF900 && ch <= 0xFAFF))
                {
                    count++;
                }
            }

            return count;
        }

        private static bool HasIconLikeName(string objectName)
        {
            if (string.IsNullOrEmpty(objectName))
            {
                return false;
            }

            var name = objectName.ToLowerInvariant();
            return name.Contains("icon") || name.Contains("image") || name.Contains("symbol");
        }

        private static float SafeRatio(float numerator, float denominator)
        {
            return denominator > 0f ? numerator / denominator : float.NaN;
        }

        private static float SafeDeltaPercent(float candidateValue, float currentValue)
        {
            return currentValue > 0f ? ((candidateValue / currentValue) - 1f) * 100f : float.NaN;
        }

        private static bool IsOverflowing(float ratio)
        {
            return !float.IsNaN(ratio) && ratio >= 1f;
        }

        private static string RiskScoreToText(int score)
        {
            if (score >= 3)
            {
                return "高";
            }

            if (score == 2)
            {
                return "中";
            }

            return "低";
        }

        private static IEnumerable<string> GetLoadedSceneNames()
        {
            var sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCount;
            for (var i = 0; i < sceneCount; i++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (scene.IsValid())
                {
                    yield return scene.name;
                }
            }
        }

        private static bool TryParseFloat(string text, out float value)
        {
            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private static string SafeFontName(Font font)
        {
            return font != null ? font.name : "(null)";
        }

        private static string Vector2ToString(Vector2 value)
        {
            return string.Format(CultureInfo.InvariantCulture, "({0:0.###},{1:0.###})", value.x, value.y);
        }

        private static string Vector3ToString(Vector3 value)
        {
            return string.Format(CultureInfo.InvariantCulture, "({0:0.###},{1:0.###},{2:0.###})", value.x, value.y, value.z);
        }

        private static string RectToString(Rect value)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "x={0:0.###},y={1:0.###},w={2:0.###},h={3:0.###}",
                value.x,
                value.y,
                value.width,
                value.height);
        }

        private static int PageIdToIndex(PageId pageId)
        {
            switch (pageId)
            {
                case PageId.Simulation:
                    return 0;
                case PageId.Blueprint:
                    return 1;
                case PageId.Square:
                    return 2;
                case PageId.Encyclopedia:
                    return 3;
                case PageId.Tools:
                    return 4;
                case PageId.Profile:
                    return 5;
                default:
                    return 0;
            }
        }

        private static string CsvEscape(string value)
        {
            if (value == null)
            {
                return "\"\"";
            }

            var escaped = value.Replace("\"", "\"\"");
            return "\"" + escaped + "\"";
        }

        private static string FormatFloat(float value)
        {
            return float.IsNaN(value) ? "N/A" : value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string FormatBool(bool value)
        {
            return value ? "true" : "false";
        }

        private static string GetOutputDirectory()
        {
            var projectRoot = Directory.GetParent(Application.dataPath);
            return projectRoot == null
                ? Path.Combine(Application.dataPath, "Reports", "UI", "TypographyRuntimeAudit")
                : Path.Combine(projectRoot.FullName, ReportRelativeDirectory);
        }

        private sealed class AuditRow
        {
            public static readonly string[] CsvColumns =
            {
                "PageIndex",
                "PageName",
                "SceneName",
                "HierarchyPath",
                "GameObjectName",
                "ActiveSelf",
                "ActiveInHierarchy",
                "InstanceID",
                "TextContent",
                "TextLength",
                "ChineseCharacterCount",
                "HasNewLine",
                "CurrentFontName",
                "FontSize",
                "FontStyle",
                "LineSpacing",
                "Alignment",
                "HorizontalOverflow",
                "VerticalOverflow",
                "ResizeTextForBestFit",
                "ResizeTextMinSize",
                "ResizeTextMaxSize",
                "FontSizeUsedForBestFit",
                "SupportRichText",
                "TextRectWidth",
                "TextRectHeight",
                "AnchorMin",
                "AnchorMax",
                "Pivot",
                "SizeDelta",
                "AnchoredPosition",
                "OffsetMin",
                "OffsetMax",
                "LocalScale",
                "ParentRectWidth",
                "ParentRectHeight",
                "ParentLayoutGroupType",
                "HasContentSizeFitter",
                "HasLayoutElement",
                "IsUnderMask",
                "IsUnderRectMask2D",
                "IsUnderScrollRect",
                "CurrentPreferredWidth",
                "CurrentPreferredHeight",
                "CurrentGeneratedLineCount",
                "CurrentWidthUsageRatio",
                "CurrentHeightUsageRatio",
                "CandidateFontName",
                "CandidatePreferredWidth",
                "CandidatePreferredHeight",
                "CandidateGeneratedLineCount",
                "CandidateWidthUsageRatio",
                "CandidateHeightUsageRatio",
                "CandidateWidthDeltaPercent",
                "CandidateHeightDeltaPercent",
                "ParentButtonPath",
                "ButtonRectWidth",
                "ButtonRectHeight",
                "ButtonHasLayoutElement",
                "ButtonLayoutMinWidth",
                "ButtonLayoutPreferredWidth",
                "ButtonLayoutFlexibleWidth",
                "ButtonLayoutMinHeight",
                "ButtonLayoutPreferredHeight",
                "ButtonLayoutFlexibleHeight",
                "ButtonInteractable",
                "IconCount",
                "IconNames",
                "MaximumIconWidth",
                "SumIconWidths",
                "TextAvailableWidth",
                "AuditCategory",
                "ControlType",
                "SuggestedFontRole",
                "AllowsWrapping",
                "LineCountIncreased",
                "CurrentOverflow",
                "RiskLevel",
                "RiskScore",
                "RiskReason"
            };

            public int PageIndex;
            public string PageName;
            public string SceneName;
            public string HierarchyPath;
            public string GameObjectName;
            public bool ActiveSelf;
            public bool ActiveInHierarchy;
            public int InstanceID;
            public string TextContent;
            public int TextLength;
            public int ChineseCharacterCount;
            public bool HasNewLine;
            public string CurrentFontName;
            public int FontSize;
            public string FontStyle;
            public float LineSpacing;
            public string Alignment;
            public string HorizontalOverflow;
            public string VerticalOverflow;
            public bool ResizeTextForBestFit;
            public int ResizeTextMinSize;
            public int ResizeTextMaxSize;
            public string FontSizeUsedForBestFit;
            public bool SupportRichText;
            public float TextRectWidth;
            public float TextRectHeight;
            public string AnchorMin;
            public string AnchorMax;
            public string Pivot;
            public string SizeDelta;
            public string AnchoredPosition;
            public string OffsetMin;
            public string OffsetMax;
            public string LocalScale;
            public float ParentRectWidth;
            public float ParentRectHeight;
            public string ParentLayoutGroupType;
            public bool HasContentSizeFitter;
            public bool HasLayoutElement;
            public bool IsUnderMask;
            public bool IsUnderRectMask2D;
            public bool IsUnderScrollRect;
            public float CurrentPreferredWidth;
            public float CurrentPreferredHeight;
            public int CurrentGeneratedLineCount;
            public float CurrentWidthUsageRatio;
            public float CurrentHeightUsageRatio;
            public string CandidateFontName;
            public float CandidatePreferredWidth;
            public float CandidatePreferredHeight;
            public int CandidateGeneratedLineCount;
            public float CandidateWidthUsageRatio;
            public float CandidateHeightUsageRatio;
            public float CandidateWidthDeltaPercent;
            public float CandidateHeightDeltaPercent;
            public string ParentButtonPath;
            public float ButtonRectWidth;
            public float ButtonRectHeight;
            public bool ButtonHasLayoutElement;
            public float ButtonLayoutMinWidth;
            public float ButtonLayoutPreferredWidth;
            public float ButtonLayoutFlexibleWidth;
            public float ButtonLayoutMinHeight;
            public float ButtonLayoutPreferredHeight;
            public float ButtonLayoutFlexibleHeight;
            public bool ButtonInteractable;
            public int IconCount;
            public string IconNames;
            public float MaximumIconWidth;
            public float SumIconWidths;
            public float TextAvailableWidth;
            public string AuditCategory;
            public string ControlType;
            public string SuggestedFontRole;
            public bool AllowsWrapping;
            public bool LineCountIncreased;
            public bool CurrentOverflow;
            public string RiskLevel;
            public int RiskScore;
            public string RiskReason;

            public string ToCsvLine()
            {
                var values = new[]
                {
                    PageIndex.ToString(CultureInfo.InvariantCulture),
                    PageName,
                    SceneName,
                    HierarchyPath,
                    GameObjectName,
                    FormatBool(ActiveSelf),
                    FormatBool(ActiveInHierarchy),
                    InstanceID.ToString(CultureInfo.InvariantCulture),
                    TextContent,
                    TextLength.ToString(CultureInfo.InvariantCulture),
                    ChineseCharacterCount.ToString(CultureInfo.InvariantCulture),
                    FormatBool(HasNewLine),
                    CurrentFontName,
                    FontSize.ToString(CultureInfo.InvariantCulture),
                    FontStyle,
                    LineSpacing.ToString("0.###", CultureInfo.InvariantCulture),
                    Alignment,
                    HorizontalOverflow,
                    VerticalOverflow,
                    FormatBool(ResizeTextForBestFit),
                    ResizeTextMinSize.ToString(CultureInfo.InvariantCulture),
                    ResizeTextMaxSize.ToString(CultureInfo.InvariantCulture),
                    FontSizeUsedForBestFit,
                    FormatBool(SupportRichText),
                    FormatFloat(TextRectWidth),
                    FormatFloat(TextRectHeight),
                    AnchorMin,
                    AnchorMax,
                    Pivot,
                    SizeDelta,
                    AnchoredPosition,
                    OffsetMin,
                    OffsetMax,
                    LocalScale,
                    FormatFloat(ParentRectWidth),
                    FormatFloat(ParentRectHeight),
                    ParentLayoutGroupType ?? string.Empty,
                    FormatBool(HasContentSizeFitter),
                    FormatBool(HasLayoutElement),
                    FormatBool(IsUnderMask),
                    FormatBool(IsUnderRectMask2D),
                    FormatBool(IsUnderScrollRect),
                    FormatFloat(CurrentPreferredWidth),
                    FormatFloat(CurrentPreferredHeight),
                    CurrentGeneratedLineCount.ToString(CultureInfo.InvariantCulture),
                    FormatFloat(CurrentWidthUsageRatio),
                    FormatFloat(CurrentHeightUsageRatio),
                    CandidateFontName,
                    FormatFloat(CandidatePreferredWidth),
                    FormatFloat(CandidatePreferredHeight),
                    CandidateGeneratedLineCount.ToString(CultureInfo.InvariantCulture),
                    FormatFloat(CandidateWidthUsageRatio),
                    FormatFloat(CandidateHeightUsageRatio),
                    FormatFloat(CandidateWidthDeltaPercent),
                    FormatFloat(CandidateHeightDeltaPercent),
                    ParentButtonPath ?? string.Empty,
                    FormatFloat(ButtonRectWidth),
                    FormatFloat(ButtonRectHeight),
                    FormatBool(ButtonHasLayoutElement),
                    FormatFloat(ButtonLayoutMinWidth),
                    FormatFloat(ButtonLayoutPreferredWidth),
                    FormatFloat(ButtonLayoutFlexibleWidth),
                    FormatFloat(ButtonLayoutMinHeight),
                    FormatFloat(ButtonLayoutPreferredHeight),
                    FormatFloat(ButtonLayoutFlexibleHeight),
                    FormatBool(ButtonInteractable),
                    IconCount.ToString(CultureInfo.InvariantCulture),
                    IconNames ?? string.Empty,
                    FormatFloat(MaximumIconWidth),
                    FormatFloat(SumIconWidths),
                    FormatFloat(TextAvailableWidth),
                    AuditCategory,
                    ControlType,
                    SuggestedFontRole,
                    FormatBool(AllowsWrapping),
                    FormatBool(LineCountIncreased),
                    FormatBool(CurrentOverflow),
                    RiskLevel,
                    RiskScore.ToString(CultureInfo.InvariantCulture),
                    RiskReason
                };

                return string.Join(",", values.Select(CsvEscape).ToArray());
            }
        }

        private struct MeasurementResult
        {
            public float PreferredWidth;
            public float PreferredHeight;
            public int LineCount;
        }

        private struct CanvasInfo
        {
            public string Name;
            public string RenderMode;
            public float ScaleFactor;
            public string PixelRect;
        }

        private struct CanvasScalerInfo
        {
            public string Name;
            public string UiScaleMode;
            public string ReferenceResolution;
            public string ScreenMatchMode;
            public float MatchWidthOrHeight;
            public float ReferencePixelsPerUnit;
        }

        private struct LayoutContext
        {
            public string ParentLayoutGroupType;
            public bool HasContentSizeFitter;
            public bool HasLayoutElement;
            public bool IsUnderMask;
            public bool IsUnderRectMask2D;
            public bool IsUnderScrollRect;
        }

        private struct ButtonInfo
        {
            public static readonly ButtonInfo Empty = new ButtonInfo
            {
                Path = string.Empty,
                IconNames = string.Empty
            };

            public string Path;
            public float RectWidth;
            public float RectHeight;
            public bool HasLayoutElement;
            public float LayoutMinWidth;
            public float LayoutPreferredWidth;
            public float LayoutFlexibleWidth;
            public float LayoutMinHeight;
            public float LayoutPreferredHeight;
            public float LayoutFlexibleHeight;
            public bool Interactable;
            public int IconCount;
            public string IconNames;
            public float MaximumIconWidth;
            public float SumIconWidths;
        }
    }
}
