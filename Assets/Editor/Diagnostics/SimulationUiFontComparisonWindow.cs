using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using ElectricalSim.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ElectricalSim.Editor.Diagnostics
{
    /// <summary>
    /// Editor-only diagnostic. In Play Mode it creates a HideFlags.DontSave UGUI canvas,
    /// measures only that temporary canvas, and never writes UI state back to scenes or prefabs.
    /// </summary>
    public sealed class SimulationUiFontComparisonWindow : EditorWindow
    {
        private const string OverlayName = "__SimulationUiFontComparisonOverlay";
        private const float MeasureWidth = 1200f;
        private const float SingleLineHeight = 64f;
        private const float LongLineHeight = 96f;
        private static readonly Vector2Int[] Resolutions = { new Vector2Int(1920, 1080), new Vector2Int(3840, 2160), new Vector2Int(1366, 768) };
        private static readonly string[] PageNames = { "Brand_Navigation", "Toolbar", "Palette_Log", "Inspector" };
        private static readonly Dictionary<string, int> EditSizes = new Dictionary<string, int>();
        private static GameObject overlay;
        private static Surface surface;

        private static readonly List<Sample> Samples = new List<Sample>
        {
            new Sample("Brand_Navigation", "品牌名称", "电工数字学生仿真系统", 22, 32, 27, false),
            new Sample("Brand_Navigation", "顶部导航", "模拟电路　图纸集　仿真广场　元器件百科　常用工具　系统信息", 18, 19, 19, false),
            new Sample("Toolbar", "开始仿真", "开始仿真", 16, 18, 17, false),
            new Sample("Toolbar", "工具栏按钮", "撤销　重做　删除　清线　清空　锁定", 15, 16, 16, false),
            new Sample("Palette_Log", "控件池标题", "电工控件池", 18, 24, 22, false),
            new Sample("Palette_Log", "分区标题", "家庭电路组件", 15, 22, 17, false),
            new Sample("Palette_Log", "元件名称", "三相交流电源", 14, 0, 15, false),
            new Sample("Palette_Log", "元件名称", "单相电能表（220V）", 14, 0, 15, false),
            new Sample("Palette_Log", "元件名称", "通电延时时间继电器（380V）", 14, 0, 15, false),
            new Sample("Palette_Log", "操作记录标题", "操作记录", 17, 20, 19, false),
            new Sample("Palette_Log", "操作记录正文", "13:39:36 默认家庭照明示例：电能表经过 2P 空开", 14, 0, 15, true),
            new Sample("Inspector", "检查助手", "检查助手", 16, 0, 18, false),
            new Sample("Inspector", "检查按钮", "检查当前电路", 13, 0, 15, false),
            new Sample("Inspector", "检查说明", "当前画布不是系统模板，无法更新模板布局。", 14, 0, 15, true)
        };

        private int resolutionIndex;
        private int pageIndex;
        private string status = "先采集 Edit Mode 旧场景字号；再在 Play Mode 打开临时 UGUI 对照。";

        [MenuItem("Tools/UI/模拟电路字体字号 A-B 对照")]
        private static void Open() => GetWindow<SimulationUiFontComparisonWindow>("模拟电路字体字号 A-B 对照").Show();

        private void OnEnable() => EditorApplication.playModeStateChanged += OnPlayModeChanged;
        private void OnDisable() => EditorApplication.playModeStateChanged -= OnPlayModeChanged;

        private void OnGUI()
        {
            EditorGUILayout.LabelField("模拟电路字体字号 A/B 对照（运行时 UGUI）", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("EditorWindow 只负责控制；对照主体只在 Play Mode 的临时 UGUI Canvas 中渲染。停止运行或关闭面板后会销毁，不会保存场景或正式 UI 参数。", MessageType.Info);
            resolutionIndex = EditorGUILayout.Popup("目标 Game View", resolutionIndex, Resolutions.Select(item => item.x + " × " + item.y).ToArray());
            pageIndex = EditorGUILayout.Popup("当前截图分组", pageIndex, PageNames);
            var gameViewSize = GetSelectedGameViewSize();
            EditorGUILayout.LabelField("Game View 选择分辨率", gameViewSize.x + " × " + gameViewSize.y);
            EditorGUILayout.LabelField("Screen API（诊断）", Screen.width + " × " + Screen.height);
            EditorGUILayout.LabelField("已采集旧字号", EditSizes.Count + " 项");
            using (new EditorGUI.DisabledScope(Application.isPlaying))
                if (GUILayout.Button("1. 采集 Edit Mode 旧场景字号")) CaptureEditSizes();
            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (GUILayout.Button("2. 显示当前分组并导出有效测量")) ShowPage(Resolutions[resolutionIndex], PageNames[pageIndex]);
                if (GUILayout.Button("导出当前分组 Game View 截图")) CaptureScreenshot(Resolutions[resolutionIndex]);
                if (GUILayout.Button("关闭临时对照面板")) DestroyOverlay();
            }
            EditorGUILayout.HelpBox(status, MessageType.None);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("实际 Font.name", EditorStyles.boldLabel);
            foreach (var candidate in Candidates()) EditorGUILayout.LabelField(candidate.label + " -> " + FontName(candidate.font));
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.EnteredEditMode) DestroyOverlay();
        }

        private static void CaptureEditSizes()
        {
            EditSizes.Clear();
            foreach (var text in Resources.FindObjectsOfTypeAll<Text>())
            {
                if (text == null || !text.gameObject.scene.IsValid()) continue;
                if (Samples.Any(sample => sample.editSize == 0 && sample.text == text.text)) EditSizes[text.text] = text.fontSize;
            }
            SetStatus("已从 Edit Mode 静态场景采集 " + EditSizes.Count + " 项字号。");
        }

        private static void ShowPage(Vector2Int expected, string page)
        {
            if (!Validate(expected)) return;
            DestroyOverlay();
            var sourceScaler = FindProjectScaler();
            surface = CreateSurface(sourceScaler, page);
            BuildHeader(surface, expected, page);
            BuildPage(surface, page);
            ForceLayout(surface);
            var rows = Measure(surface, page);
            WriteReport(rows, expected, page, sourceScaler);
            SetStatus("已显示 " + page + " 运行时 UGUI 对照。报告只记录有效 Rect；当前页面可直接截图。");
        }

        private static bool Validate(Vector2Int expected)
        {
            if (!Application.isPlaying)
            {
                SetStatus("请先进入 Play Mode。工具不会使用 EditorWindow 字体渲染作为结果。");
                return false;
            }
            var selected = GetSelectedGameViewSize();
            if (selected.x != expected.x || selected.y != expected.y)
            {
                SetStatus("当前 Game View 实际分辨率不匹配，请调整后重试。当前选择 " + selected.x + "×" + selected.y + "，目标 " + expected.x + "×" + expected.y + "。");
                return false;
            }
            return true;
        }

        private static Surface CreateSurface(CanvasScaler source, string page)
        {
            overlay = new GameObject(OverlayName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            overlay.hideFlags = HideFlags.DontSave;
            var canvas = overlay.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;
            var scaler = overlay.GetComponent<CanvasScaler>();
            CopyScaler(source, scaler);
            var root = overlay.GetComponent<RectTransform>();
            Stretch(root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var shell = Panel("Shell", overlay.transform, ColorOf("F8FAFC"));
            Stretch(shell.GetComponent<RectTransform>(), new Vector2(.01f, .02f), new Vector2(.99f, .98f), Vector2.zero, Vector2.zero);
            var scroll = ScrollView(shell.transform, out var content);
            Stretch(scroll.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(12f, 12f), new Vector2(-12f, -104f));
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return new Surface { canvas = canvas, scaler = scaler, shell = shell.GetComponent<RectTransform>(), content = content, page = page };
        }

        private static void BuildHeader(Surface value, Vector2Int expected, string page)
        {
            var title = Text("Title", value.shell, "模拟电路字体字号 A/B 对照 - " + page, MainUiTheme.UiFontBold, 22, ColorOf("111827"), TextAnchor.MiddleLeft);
            Stretch(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -50f), new Vector2(-150f, -12f));
            var info = Text("Info", value.shell, "Screen " + expected.x + "×" + expected.y + "  |  Canvas scale=" + value.canvas.scaleFactor.ToString("0.###") + "  |  A 当前 / B 静态 / C 建议", MainUiTheme.UiFont, 13, ColorOf("475569"), TextAnchor.MiddleLeft);
            Stretch(info.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -84f), new Vector2(-20f, -50f));
            var close = Button("Close", value.shell, "关闭", MainUiTheme.UiFontBold, 14, Color.white, ColorOf("2563EB"));
            SetRect(close.GetComponent<RectTransform>(), new Vector2(110f, 36f), new Vector2(1f, 1f), new Vector2(-18f, -18f));
            close.onClick.AddListener(DestroyOverlay);
        }

        private static void BuildPage(Surface value, string page)
        {
            var candidates = Candidates();
            var header = Row("Candidates", value.content, 44f);
            Cell(header.transform, "角色 / 文案", MainUiTheme.UiFontBold, 14, ColorOf("334155"), 230f, 44f, true);
            foreach (var candidate in candidates) Cell(header.transform, candidate.label + "\n" + FontName(candidate.font), MainUiTheme.UiFontBold, 12, ColorOf("334155"), 250f, 44f, true);
            var bold = BoldCandidate();
            if (bold.font != null)
            {
                var boldRow = Row("EntityBoldAuxiliary", value.content, 74f);
                Cell(boldRow.transform, "实体粗体辅助对照\nOppoSans-Bold + FontStyle.Normal", MainUiTheme.UiFontBold, 13, ColorOf("334155"), 230f, 74f, true);
                ComparisonCell(boldRow.transform, bold, new Sample(page, "实体粗体", "开始仿真　检查当前电路", 16, 18, 17, false), 74f);
            }
            foreach (var sample in Samples.Where(item => item.page == page))
            {
                var height = sample.longText ? 210f : 170f;
                var row = Row("Role", value.content, height);
                Cell(row.transform, sample.role + "\n" + sample.text + "\nA " + sample.currentSize + " / B " + EditSize(sample) + " / C " + sample.recommendedSize, MainUiTheme.UiFontBold, 13, ColorOf("334155"), 230f, height, true);
                foreach (var candidate in candidates) ComparisonCell(row.transform, candidate, sample, height);
            }
        }

        private static void ComparisonCell(Transform parent, Candidate candidate, Sample sample, float height)
        {
            var panel = Panel("Font_" + candidate.label.Replace(" ", "_"), parent, Color.white);
            LayoutSize(panel, 250f, height);
            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.spacing = 4;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            if (candidate.font == null) { Cell(panel.transform, "当前机器不可用", MainUiTheme.UiFont, 13, ColorOf("64748B"), 234f, 30f, true); return; }
            var color = sample.longText ? ColorOf("475569") : ColorOf("111827");
            Variant(panel.transform, candidate.font, "A " + sample.currentSize, sample.text, sample.currentSize, color, sample.longText);
            Variant(panel.transform, candidate.font, "B " + EditSize(sample), sample.text, EditSize(sample), color, sample.longText);
            Variant(panel.transform, candidate.font, "C " + sample.recommendedSize, sample.text, sample.recommendedSize, color, sample.longText);
        }

        private static void Variant(Transform parent, Font font, string prefix, string text, int size, Color color, bool longText)
        {
            var line = Text("Variant", parent, prefix + "  " + text, font, size, color, TextAnchor.UpperLeft);
            line.horizontalOverflow = HorizontalWrapMode.Wrap;
            line.verticalOverflow = VerticalWrapMode.Overflow;
            line.resizeTextForBestFit = false;
            LayoutHeight(line.gameObject, longText ? 56f : 42f);
        }

        private static List<Measurement> Measure(Surface value, string page)
        {
            var root = new GameObject("MeasurementRoot", typeof(RectTransform));
            root.hideFlags = HideFlags.DontSave;
            root.transform.SetParent(value.shell, false);
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = rootRect.anchorMax = new Vector2(0f, 1f);
            rootRect.pivot = new Vector2(0f, 1f);
            rootRect.anchoredPosition = new Vector2(-3000f, 0f);
            rootRect.sizeDelta = new Vector2(MeasureWidth, 1f);
            var pending = new List<MeasurementSource>();
            foreach (var candidate in Candidates().Where(item => item.font != null))
            foreach (var sample in Samples.Where(item => item.page == page))
            {
                pending.Add(CreateMeasureText(root.transform, candidate, sample, "A 当前", sample.currentSize));
                pending.Add(CreateMeasureText(root.transform, candidate, sample, "B 静态", EditSize(sample)));
                pending.Add(CreateMeasureText(root.transform, candidate, sample, "C 建议", sample.recommendedSize));
            }
            var bold = BoldCandidate();
            if (bold.font != null)
            {
                var sample = new Sample(page, "实体粗体", "开始仿真　检查当前电路", 16, 18, 17, false);
                pending.Add(CreateMeasureText(root.transform, bold, sample, "A 当前", sample.currentSize));
                pending.Add(CreateMeasureText(root.transform, bold, sample, "B 静态", sample.editSize));
                pending.Add(CreateMeasureText(root.transform, bold, sample, "C 建议", sample.recommendedSize));
            }
            ForceLayout(value);
            return pending.Select(Capture).ToList();
        }

        private static MeasurementSource CreateMeasureText(Transform parent, Candidate candidate, Sample sample, string tier, int size)
        {
            var host = new GameObject("Measure", typeof(RectTransform));
            host.transform.SetParent(parent, false);
            var rect = host.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(MeasureWidth, sample.longText ? LongLineHeight : SingleLineHeight);
            var color = sample.longText ? ColorOf("475569") : ColorOf("111827");
            var text = Text("Text", host.transform, sample.text, candidate.font, size, color, TextAnchor.UpperLeft);
            Stretch(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            text.horizontalOverflow = sample.longText ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.resizeTextForBestFit = false;
            text.lineSpacing = 1f;
            return new MeasurementSource { candidate = candidate, sample = sample, tier = tier, fontSize = size, text = text };
        }

        private static Measurement Capture(MeasurementSource source)
        {
            var text = source.text;
            var rect = text.rectTransform.rect;
            if (rect.width <= 0f || rect.height <= 0f) return Measurement.Invalid(source, rect);
            var settings = text.GetGenerationSettings(rect.size);
            var generator = new TextGenerator();
            generator.Populate(text.text, settings);
            var preferredWidth = text.preferredWidth;
            var preferredHeight = text.preferredHeight;
            var widthRatio = preferredWidth / rect.width;
            var heightRatio = preferredHeight / rect.height;
            var riskRatio = text.horizontalOverflow == HorizontalWrapMode.Wrap ? heightRatio : widthRatio;
            return new Measurement
            {
                valid = true, candidate = source.candidate.label, actualFontName = FontName(source.candidate.font), role = source.sample.role, tier = source.tier, fontSize = source.fontSize,
                rectWidth = rect.width, rectHeight = rect.height, preferredWidth = preferredWidth, preferredHeight = preferredHeight, widthUsage = widthRatio, heightUsage = heightRatio,
                lineCount = generator.lineCount, bestFitSize = "N/A (BestFit=false)", risk = Risk(riskRatio), screenWidth = Screen.width, screenHeight = Screen.height, scaleFactor = surface.canvas.scaleFactor
            };
        }

        private static string Risk(float ratio) => ratio >= 1f ? "High" : ratio >= .9f ? "Medium" : "Low";

        private static void ForceLayout(Surface value)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(value.shell);
            LayoutRebuilder.ForceRebuildLayoutImmediate(value.content);
            Canvas.ForceUpdateCanvases();
        }

        private static void WriteReport(List<Measurement> rows, Vector2Int expected, string page, CanvasScaler sourceScaler)
        {
            var output = OutputDir(expected);
            Directory.CreateDirectory(output);
            var builder = new StringBuilder();
            builder.AppendLine("# 模拟电路字体字号 A/B 对照（修正测量）");
            builder.AppendLine();
            builder.AppendLine("- Page: " + page);
            builder.AppendLine("- Game View selected resolution: " + expected.x + "x" + expected.y);
            builder.AppendLine("- UnityEngine.Screen API: " + Screen.width + "x" + Screen.height + " (recorded for Editor-context diagnostics)");
            builder.AppendLine("- Canvas scaleFactor: " + surface.canvas.scaleFactor.ToString("0.###"));
            builder.AppendLine("- CanvasScaler referenceResolution: " + surface.scaler.referenceResolution.x + "x" + surface.scaler.referenceResolution.y);
            builder.AppendLine("- CanvasScaler screenMatchMode/matchWidthOrHeight: " + surface.scaler.screenMatchMode + " / " + surface.scaler.matchWidthOrHeight.ToString("0.###"));
            builder.AppendLine("- Source CanvasScaler: " + (sourceScaler == null ? "not found (fallback 1920x1080, 0.5)" : sourceScaler.gameObject.name));
            builder.AppendLine("- Measurement Rect: " + MeasureWidth + "x" + SingleLineHeight + " (long text " + MeasureWidth + "x" + LongLineHeight + ")。仅输出有效 Rect。\n");
            builder.AppendLine("| FontCandidate | ActualFontName | Role | SizeTier | FontSize | RectWidth | RectHeight | PreferredWidth | PreferredHeight | WidthUsageRatio | HeightUsageRatio | GeneratedLineCount | FontSizeUsedForBestFit | Risk | ScreenWidth | ScreenHeight | CanvasScaleFactor |");
            builder.AppendLine("| --- | --- | --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- | --- | ---: | ---: | ---: |");
            foreach (var row in rows.Where(item => item.valid))
                builder.AppendLine("| " + Esc(row.candidate) + " | " + Esc(row.actualFontName) + " | " + Esc(row.role) + " | " + row.tier + " | " + row.fontSize + " | " + Num(row.rectWidth) + " | " + Num(row.rectHeight) + " | " + Num(row.preferredWidth) + " | " + Num(row.preferredHeight) + " | " + Num(row.widthUsage) + " | " + Num(row.heightUsage) + " | " + row.lineCount + " | " + row.bestFitSize + " | " + row.risk + " | " + row.screenWidth + " | " + row.screenHeight + " | " + Num(row.scaleFactor) + " |");
            builder.AppendLine("\n截图请在同一分辨率下依次选择 Brand_Navigation、Toolbar、Palette_Log、Inspector 后点击导出。每个页面均同屏横向显示全部字体候选。");
            File.WriteAllText(Path.Combine(output, "SimulationFontComparisonFixed.md"), builder.ToString(), new UTF8Encoding(true));
        }

        private static void CaptureScreenshot(Vector2Int expected)
        {
            if (!Validate(expected)) return;
            if (surface == null) { SetStatus("请先显示分组后再截图。"); return; }
            var output = OutputDir(expected);
            Directory.CreateDirectory(output);
            var path = Path.Combine(output, surface.page + ".png");
            ScreenCapture.CaptureScreenshot(path, 1);
            SetStatus("已请求 Game View 截图：" + path + "。Unity 会在当前帧结束后写入。");
        }

        private static int EditSize(Sample sample) => sample.editSize > 0 ? sample.editSize : EditSizes.TryGetValue(sample.text, out var size) ? size : sample.currentSize;
        private static Vector2Int GetSelectedGameViewSize()
        {
            try
            {
                var gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
                var getMain = gameViewType?.GetMethod("GetMainGameView", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                var gameView = getMain?.Invoke(null, null);
                if (gameView == null && gameViewType != null)
                {
                    gameView = Resources.FindObjectsOfTypeAll(gameViewType).FirstOrDefault();
                }
                if (gameView == null) return new Vector2Int(Screen.width, Screen.height);
                var sizeProperty = gameViewType.GetProperty("currentGameViewSize", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var sizeField = gameViewType.GetField("currentGameViewSize", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var size = sizeProperty?.GetValue(gameView, null) ?? sizeField?.GetValue(gameView);
                if (size == null) return new Vector2Int(Screen.width, Screen.height);
                var sizeType = size.GetType();
                var width = Convert.ToInt32(sizeType.GetProperty("width", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(size, null));
                var height = Convert.ToInt32(sizeType.GetProperty("height", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(size, null));
                return width > 0 && height > 0 ? new Vector2Int(width, height) : new Vector2Int(Screen.width, Screen.height);
            }
            catch
            {
                return new Vector2Int(Screen.width, Screen.height);
            }
        }
        private static List<Candidate> Candidates() => new List<Candidate>
        {
            new Candidate("LegacyRuntime", Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")), new Candidate("OppoSans-Regular", MainUiTheme.UiFont),
            new Candidate("Microsoft YaHei UI", OsFont("Microsoft YaHei UI")), new Candidate("Microsoft YaHei", OsFont("Microsoft YaHei")),
            new Candidate("Source Han Sans SC", OsFont("Source Han Sans SC")), new Candidate("Noto Sans CJK SC", OsFont("Noto Sans CJK SC"))
        };
        private static Candidate BoldCandidate() => new Candidate("OppoSans-Bold (实体粗体)", MainUiTheme.UiFontBold);
        private static Font OsFont(string name) { try { return Font.CreateDynamicFontFromOSFont(name, 16); } catch { return null; } }
        private static CanvasScaler FindProjectScaler() => Resources.FindObjectsOfTypeAll<CanvasScaler>().FirstOrDefault(item => item != null && item.gameObject.scene.IsValid() && item.gameObject.name != OverlayName);
        private static void CopyScaler(CanvasScaler source, CanvasScaler target)
        {
            target.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; target.referenceResolution = new Vector2(1920f, 1080f); target.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; target.matchWidthOrHeight = .5f; target.referencePixelsPerUnit = 100f;
            if (source == null) return;
            target.referenceResolution = source.referenceResolution; target.screenMatchMode = source.screenMatchMode; target.matchWidthOrHeight = source.matchWidthOrHeight; target.referencePixelsPerUnit = source.referencePixelsPerUnit;
        }

        private static ScrollRect ScrollView(Transform parent, out RectTransform content)
        {
            var root = Panel("ScrollView", parent, ColorOf("F8FAFC")); root.AddComponent<ScrollRect>();
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask)); viewport.transform.SetParent(root.transform, false); viewport.GetComponent<Mask>().showMaskGraphic = false; Stretch(viewport.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var contentObject = new GameObject("Content", typeof(RectTransform)); contentObject.transform.SetParent(viewport.transform, false); content = contentObject.GetComponent<RectTransform>(); content.anchorMin = new Vector2(0f, 1f); content.anchorMax = new Vector2(1f, 1f); content.pivot = new Vector2(.5f, 1f); content.anchoredPosition = Vector2.zero; content.sizeDelta = new Vector2(0f, 1f);
            var scroll = root.GetComponent<ScrollRect>(); scroll.viewport = viewport.GetComponent<RectTransform>(); scroll.content = content; scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped; return scroll;
        }
        private static GameObject Row(string name, Transform parent, float height) { var item = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement)); item.transform.SetParent(parent, false); LayoutSize(item, 0f, height); var layout = item.GetComponent<HorizontalLayoutGroup>(); layout.spacing = 8; layout.childControlWidth = false; layout.childControlHeight = true; layout.childForceExpandWidth = false; layout.childForceExpandHeight = false; return item; }
        private static GameObject Panel(string name, Transform parent, Color color) { var item = new GameObject(name, typeof(RectTransform), typeof(Image)); item.transform.SetParent(parent, false); item.GetComponent<Image>().color = color; return item; }
        private static Text Cell(Transform parent, string value, Font font, int size, Color color, float width, float height, bool wrap) { var item = new GameObject("Cell", typeof(RectTransform), typeof(Text), typeof(LayoutElement)); item.transform.SetParent(parent, false); LayoutSize(item, width, height); var text = item.GetComponent<Text>(); text.text = value; text.font = font; text.fontSize = size; text.fontStyle = FontStyle.Normal; text.color = color; text.alignment = TextAnchor.UpperLeft; text.lineSpacing = 1f; text.horizontalOverflow = wrap ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow; text.verticalOverflow = VerticalWrapMode.Overflow; text.resizeTextForBestFit = false; return text; }
        private static Text Text(string name, Transform parent, string value, Font font, int size, Color color, TextAnchor anchor) { var item = new GameObject(name, typeof(RectTransform), typeof(Text)); item.transform.SetParent(parent, false); var text = item.GetComponent<Text>(); text.text = value; text.font = font; text.fontSize = size; text.fontStyle = FontStyle.Normal; text.color = color; text.alignment = anchor; text.lineSpacing = 1f; text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Overflow; text.resizeTextForBestFit = false; return text; }
        private static Button Button(string name, Transform parent, string label, Font font, int size, Color foreground, Color background) { var item = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)); item.transform.SetParent(parent, false); item.GetComponent<Image>().color = background; var text = Text("Text", item.transform, label, font, size, foreground, TextAnchor.MiddleCenter); Stretch(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(8f, 4f), new Vector2(-8f, -4f)); return item.GetComponent<Button>(); }
        private static void LayoutSize(GameObject item, float width, float height) { var e = item.GetComponent<LayoutElement>() ?? item.AddComponent<LayoutElement>(); if (width > 0f) { e.minWidth = width; e.preferredWidth = width; e.flexibleWidth = 0f; } e.minHeight = height; e.preferredHeight = height; e.flexibleHeight = 0f; }
        private static void LayoutHeight(GameObject item, float height) { var e = item.GetComponent<LayoutElement>() ?? item.AddComponent<LayoutElement>(); e.minHeight = height; e.preferredHeight = height; e.flexibleHeight = 0f; }
        private static void Stretch(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax) { rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = offsetMin; rect.offsetMax = offsetMax; }
        private static void SetRect(RectTransform rect, Vector2 size, Vector2 anchor, Vector2 position) { rect.anchorMin = anchor; rect.anchorMax = anchor; rect.pivot = new Vector2(1f, 1f); rect.sizeDelta = size; rect.anchoredPosition = position; }
        private static void DestroyOverlay() { if (overlay == null) return; DestroyImmediate(overlay); overlay = null; surface = null; }
        private static string OutputDir(Vector2Int size) => Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Reports", "UI", "SimulationFontComparisonFixed", size.x + "x" + size.y);
        private static void SetStatus(string text) { var window = GetWindow<SimulationUiFontComparisonWindow>(); window.status = text; window.Repaint(); Debug.Log("[SimulationUiFontComparison] " + text); }
        private static string FontName(Font font) => font == null ? "<unavailable>" : font.name;
        private static Color ColorOf(string html) { ColorUtility.TryParseHtmlString("#" + html, out var color); return color; }
        private static string Esc(string value) => (value ?? string.Empty).Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
        private static string Num(float value) => value.ToString("0.###");

        private sealed class Sample { public readonly string page, role, text; public readonly int currentSize, editSize, recommendedSize; public readonly bool longText; public Sample(string page, string role, string text, int currentSize, int editSize, int recommendedSize, bool longText) { this.page = page; this.role = role; this.text = text; this.currentSize = currentSize; this.editSize = editSize; this.recommendedSize = recommendedSize; this.longText = longText; } }
        private sealed class Candidate { public readonly string label; public readonly Font font; public Candidate(string label, Font font) { this.label = label; this.font = font; } }
        private sealed class Surface { public Canvas canvas; public CanvasScaler scaler; public RectTransform shell, content; public string page; }
        private sealed class MeasurementSource { public Candidate candidate; public Sample sample; public string tier; public int fontSize; public Text text; }
        private sealed class Measurement
        {
            public bool valid; public string candidate, actualFontName, role, tier, bestFitSize, risk; public int fontSize, lineCount, screenWidth, screenHeight; public float rectWidth, rectHeight, preferredWidth, preferredHeight, widthUsage, heightUsage, scaleFactor;
            public static Measurement Invalid(MeasurementSource source, Rect rect) => new Measurement { valid = false, candidate = source.candidate.label, actualFontName = FontName(source.candidate.font), role = source.sample.role, tier = source.tier, fontSize = source.fontSize, rectWidth = rect.width, rectHeight = rect.height, bestFitSize = "N/A (BestFit=false)", risk = "InvalidRect", screenWidth = Screen.width, screenHeight = Screen.height, scaleFactor = surface == null ? 0f : surface.canvas.scaleFactor };
        }
    }
}
