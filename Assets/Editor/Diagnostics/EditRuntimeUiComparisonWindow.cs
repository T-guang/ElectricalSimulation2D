using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using ElectricalSim.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ElectricalSim.Editor.Diagnostics
{
    /// <summary>
    /// Read-only UI audit. It never marks scenes dirty, records Undo, or changes UI objects.
    /// </summary>
    public sealed class EditRuntimeUiComparisonWindow : EditorWindow
    {
        private const string OutputRelativePath = "Reports/UI/EditRuntimeComparison";
        private static readonly Vector2Int[] SupportedResolutions =
        {
            new Vector2Int(1920, 1080),
            new Vector2Int(3840, 2160)
        };

        private int resolutionIndex;
        private int playFrameCount;
        private string status = "先在 Edit Mode 采集，然后进入 Play Mode 并等待主界面稳定。";

        [MenuItem("Tools/UI/Edit-Play UI 对照审计")]
        private static void Open()
        {
            GetWindow<EditRuntimeUiComparisonWindow>("Edit-Play UI 对照审计").Show();
        }

        private void OnEnable()
        {
            EditorApplication.update += TrackPlayFrames;
        }

        private void OnDisable()
        {
            EditorApplication.update -= TrackPlayFrames;
        }

        private void TrackPlayFrames()
        {
            if (Application.isPlaying)
            {
                playFrameCount++;
            }
            else
            {
                playFrameCount = 0;
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Edit / Play UI 对照审计", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("本工具只读取当前场景 UI 并导出报告；不会保存场景、修改 Prefab 或重建正式界面。", MessageType.Info);

            resolutionIndex = EditorGUILayout.Popup(
                "当前 Game View 分辨率",
                resolutionIndex,
                SupportedResolutions.Select(item => item.x + " × " + item.y).ToArray());

            var target = SupportedResolutions[resolutionIndex];
            EditorGUILayout.LabelField("Screen", Screen.width + " × " + Screen.height);
            EditorGUILayout.LabelField("Play Mode 稳定帧", playFrameCount.ToString());

            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                if (GUILayout.Button("1. 采集 Edit Mode"))
                {
                    CaptureEdit(target);
                }
            }

            using (new EditorGUI.DisabledScope(!Application.isPlaying || playFrameCount < 3))
            {
                if (GUILayout.Button("2. 采集 Play Mode 并生成差异"))
                {
                    CapturePlay(target);
                }
            }

            if (Application.isPlaying && playFrameCount < 3)
            {
                EditorGUILayout.HelpBox("等待至少 3 帧后才能采集 Play Mode。", MessageType.None);
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(status, MessageType.None);
            if (GUILayout.Button("打开导出目录"))
            {
                var path = GetOutputDirectory();
                Directory.CreateDirectory(path);
                EditorUtility.RevealInFinder(path);
            }
        }

        private static void CaptureEdit(Vector2Int target)
        {
            var output = GetOutputDirectory();
            Directory.CreateDirectory(output);
            var snapshot = CaptureSnapshot("EditMode", target);
            WriteSnapshot(snapshot, output, "EditMode_UI", target);
            TryCaptureGameView(output, "Edit", target, out var screenshotMessage);
            WriteCaptureNote(output, "Edit", target, screenshotMessage);
            Debug.Log("[EditRuntimeUiComparison] Edit Mode 采集完成：" + output);
        }

        private static void CapturePlay(Vector2Int target)
        {
            var output = GetOutputDirectory();
            Directory.CreateDirectory(output);
            var snapshot = CaptureSnapshot("PlayMode", target);
            WriteSnapshot(snapshot, output, "PlayMode_UI", target);
            TryCaptureGameView(output, "Play", target, out var screenshotMessage);
            WriteCaptureNote(output, "Play", target, screenshotMessage);

            var editPath = GetSnapshotPath(output, "EditMode_UI", target);
            if (!File.Exists(editPath))
            {
                Debug.LogWarning("[EditRuntimeUiComparison] 未找到同分辨率 Edit Mode 快照：" + editPath);
                return;
            }

            var edit = JsonUtility.FromJson<UiSnapshot>(File.ReadAllText(editPath, Encoding.UTF8));
            var diff = BuildDiff(edit, snapshot);
            WriteDiff(diff, output, target);
            WriteComparisonReport(edit, snapshot, diff, output, target, screenshotMessage);
            Debug.Log("[EditRuntimeUiComparison] Play Mode 采集和差异报告完成：" + output);
        }

        private static UiSnapshot CaptureSnapshot(string mode, Vector2Int target)
        {
            Canvas.ForceUpdateCanvases();
            var snapshot = new UiSnapshot
            {
                mode = mode,
                capturedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                unityVersion = Application.unityVersion,
                platform = Application.platform.ToString(),
                sceneNames = string.Join(" | ", GetOpenSceneNames()),
                screenWidth = Screen.width,
                screenHeight = Screen.height,
                screenDpi = Screen.dpi,
                targetWidth = target.x,
                targetHeight = target.y,
                titleFont = SafeFontName(MainUiTheme.TitleFont),
                uiFont = SafeFontName(MainUiTheme.UiFont),
                uiFontBold = SafeFontName(MainUiTheme.UiFontBold),
                bodyFont = SafeFontName(MainUiTheme.BodyFont)
            };

            var canvases = Resources.FindObjectsOfTypeAll<Canvas>()
                .Where(IsSceneObject)
                .OrderBy(canvas => GetHierarchyPath(canvas.transform))
                .ToArray();

            foreach (var canvas in canvases)
            {
                var rect = canvas.transform as RectTransform;
                if (rect != null)
                {
                    try
                    {
                        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
                    }
                    catch (Exception exception)
                    {
                        snapshot.errors.Add("Layout rebuild: " + GetHierarchyPath(canvas.transform) + " - " + exception.Message);
                    }
                }

                snapshot.canvases.Add(CaptureCanvas(canvas));
            }

            Canvas.ForceUpdateCanvases();
            var texts = Resources.FindObjectsOfTypeAll<Text>()
                .Where(IsSceneObject)
                .OrderBy(text => GetHierarchyPath(text.transform))
                .ThenBy(text => text.GetInstanceID())
                .ToArray();

            foreach (var text in texts)
            {
                try
                {
                    snapshot.texts.Add(CaptureText(text));
                }
                catch (Exception exception)
                {
                    snapshot.errors.Add("Text: " + SafeObjectName(text) + " - " + exception.Message);
                }
            }

            return snapshot;
        }

        private static CanvasRecord CaptureCanvas(Canvas canvas)
        {
            var scaler = canvas.GetComponent<CanvasScaler>();
            var rect = canvas.transform as RectTransform;
            return new CanvasRecord
            {
                sceneName = canvas.gameObject.scene.name,
                hierarchyPath = GetHierarchyPath(canvas.transform),
                name = canvas.name,
                renderMode = canvas.renderMode.ToString(),
                scaleFactor = canvas.scaleFactor,
                pixelRect = FormatRect(canvas.pixelRect),
                rectWidth = rect != null ? rect.rect.width : 0f,
                rectHeight = rect != null ? rect.rect.height : 0f,
                hasScaler = scaler != null,
                uiScaleMode = scaler != null ? scaler.uiScaleMode.ToString() : string.Empty,
                referenceResolution = scaler != null ? FormatVector(scaler.referenceResolution) : string.Empty,
                screenMatchMode = scaler != null ? scaler.screenMatchMode.ToString() : string.Empty,
                matchWidthOrHeight = scaler != null ? scaler.matchWidthOrHeight : 0f,
                referencePixelsPerUnit = scaler != null ? scaler.referencePixelsPerUnit : 0f
            };
        }

        private static TextRecord CaptureText(Text text)
        {
            var rect = text.rectTransform;
            var button = FindParentButton(text.transform);
            return new TextRecord
            {
                sceneName = text.gameObject.scene.name,
                hierarchyPath = GetHierarchyPath(text.transform),
                gameObjectName = text.gameObject.name,
                instanceId = text.GetInstanceID(),
                textContent = text.text ?? string.Empty,
                fontName = SafeFontName(text.font),
                fontSize = text.fontSize,
                fontStyle = text.fontStyle.ToString(),
                color = ColorUtility.ToHtmlStringRGBA(text.color),
                alignment = text.alignment.ToString(),
                lineSpacing = text.lineSpacing,
                horizontalOverflow = text.horizontalOverflow.ToString(),
                verticalOverflow = text.verticalOverflow.ToString(),
                bestFit = text.resizeTextForBestFit,
                bestFitMin = text.resizeTextMinSize,
                bestFitMax = text.resizeTextMaxSize,
                rectWidth = rect.rect.width,
                rectHeight = rect.rect.height,
                localScale = FormatVector(rect.localScale),
                activeSelf = text.gameObject.activeSelf,
                activeInHierarchy = text.gameObject.activeInHierarchy,
                parentButtonPath = button != null ? GetHierarchyPath(button.transform) : string.Empty,
                parentLayoutGroup = FindNearestLayoutGroup(text.transform)
            };
        }

        private static List<DiffRecord> BuildDiff(UiSnapshot edit, UiSnapshot play)
        {
            var result = new List<DiffRecord>();
            var editLookup = BuildLookup(edit.texts);
            var playLookup = BuildLookup(play.texts);
            var keys = new HashSet<string>(editLookup.Keys);
            keys.UnionWith(playLookup.Keys);

            foreach (var key in keys.OrderBy(item => item))
            {
                editLookup.TryGetValue(key, out var editText);
                playLookup.TryGetValue(key, out var playText);
                var diff = new DiffRecord
                {
                    key = key,
                    editPath = editText != null ? editText.hierarchyPath : string.Empty,
                    playPath = playText != null ? playText.hierarchyPath : string.Empty,
                    textContent = playText != null ? playText.textContent : editText != null ? editText.textContent : string.Empty,
                    editOnlyObject = editText != null && playText == null,
                    runtimeOnlyObject = editText == null && playText != null
                };

                if (editText != null && playText != null)
                {
                    diff.fontChanged = editText.fontName != playText.fontName;
                    diff.fontSizeChanged = editText.fontSize != playText.fontSize;
                    diff.fontStyleChanged = editText.fontStyle != playText.fontStyle;
                    diff.colorChanged = editText.color != playText.color;
                    diff.rectChanged = !Approximately(editText.rectWidth, playText.rectWidth) || !Approximately(editText.rectHeight, playText.rectHeight);
                    diff.scaleChanged = editText.localScale != playText.localScale;
                    diff.parentChanged = editText.parentButtonPath != playText.parentButtonPath || editText.parentLayoutGroup != playText.parentLayoutGroup;
                    diff.activeStateChanged = editText.activeSelf != playText.activeSelf || editText.activeInHierarchy != playText.activeInHierarchy;
                    diff.editFont = editText.fontName;
                    diff.playFont = playText.fontName;
                    diff.editFontSize = editText.fontSize;
                    diff.playFontSize = playText.fontSize;
                    diff.editColor = editText.color;
                    diff.playColor = playText.color;
                    diff.editRect = FormatSize(editText.rectWidth, editText.rectHeight);
                    diff.playRect = FormatSize(playText.rectWidth, playText.rectHeight);
                }

                result.Add(diff);
            }

            return result;
        }

        private static Dictionary<string, TextRecord> BuildLookup(List<TextRecord> texts)
        {
            var lookup = new Dictionary<string, TextRecord>();
            foreach (var group in texts.GroupBy(item => item.hierarchyPath + "|" + item.textContent))
            {
                var index = 0;
                foreach (var record in group.OrderBy(item => item.instanceId))
                {
                    lookup[group.Key + "|" + index++] = record;
                }
            }

            return lookup;
        }

        private static void WriteSnapshot(UiSnapshot snapshot, string output, string prefix, Vector2Int target)
        {
            var json = JsonUtility.ToJson(snapshot, true);
            File.WriteAllText(GetSnapshotPath(output, prefix, target), json, new UTF8Encoding(false));
            WriteTextCsv(snapshot, Path.Combine(output, prefix + "_" + target.x + "x" + target.y + ".csv"));
            WriteTextCsv(snapshot, Path.Combine(output, prefix + ".csv"));
        }

        private static void WriteTextCsv(UiSnapshot snapshot, string path)
        {
            var rows = new List<string>
            {
                Csv("RecordType", "SceneName", "HierarchyPath", "GameObjectName", "InstanceID", "TextContent", "FontName", "FontSize", "FontStyle", "Color", "Alignment", "LineSpacing", "HorizontalOverflow", "VerticalOverflow", "BestFit", "BestFitMin", "BestFitMax", "RectWidth", "RectHeight", "LocalScale", "ActiveSelf", "ActiveInHierarchy", "ParentButtonPath", "ParentLayoutGroup", "CanvasRenderMode", "CanvasScaleFactor", "CanvasPixelRect", "CanvasUiScaleMode", "CanvasReferenceResolution", "CanvasScreenMatchMode", "CanvasMatchWidthOrHeight", "CanvasReferencePixelsPerUnit")
            };

            foreach (var text in snapshot.texts)
            {
                rows.Add(Csv("Text", text.sceneName, text.hierarchyPath, text.gameObjectName, text.instanceId, text.textContent, text.fontName, text.fontSize, text.fontStyle, text.color, text.alignment, text.lineSpacing, text.horizontalOverflow, text.verticalOverflow, text.bestFit, text.bestFitMin, text.bestFitMax, text.rectWidth, text.rectHeight, text.localScale, text.activeSelf, text.activeInHierarchy, text.parentButtonPath, text.parentLayoutGroup));
            }

            foreach (var canvas in snapshot.canvases)
            {
                rows.Add(Csv("Canvas", canvas.sceneName, canvas.hierarchyPath, canvas.name, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, canvas.rectWidth, canvas.rectHeight, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, canvas.renderMode, canvas.scaleFactor, canvas.pixelRect, canvas.uiScaleMode, canvas.referenceResolution, canvas.screenMatchMode, canvas.matchWidthOrHeight, canvas.referencePixelsPerUnit));
            }

            File.WriteAllText(path, string.Join("\r\n", rows), new UTF8Encoding(true));
        }

        private static void WriteDiff(List<DiffRecord> records, string output, Vector2Int target)
        {
            var rows = new List<string>
            {
                Csv("Key", "TextContent", "EditPath", "PlayPath", "FontChanged", "FontSizeChanged", "FontStyleChanged", "ColorChanged", "RectChanged", "ScaleChanged", "ParentChanged", "ActiveStateChanged", "RuntimeOnlyObject", "EditOnlyObject", "EditFont", "PlayFont", "EditFontSize", "PlayFontSize", "EditColor", "PlayColor", "EditRect", "PlayRect")
            };

            foreach (var record in records)
            {
                rows.Add(Csv(record.key, record.textContent, record.editPath, record.playPath, record.fontChanged, record.fontSizeChanged, record.fontStyleChanged, record.colorChanged, record.rectChanged, record.scaleChanged, record.parentChanged, record.activeStateChanged, record.runtimeOnlyObject, record.editOnlyObject, record.editFont, record.playFont, record.editFontSize, record.playFontSize, record.editColor, record.playColor, record.editRect, record.playRect));
            }

            var namedPath = Path.Combine(output, "Edit_Play_UI_Diff_" + target.x + "x" + target.y + ".csv");
            File.WriteAllText(namedPath, string.Join("\r\n", rows), new UTF8Encoding(true));
            File.WriteAllText(Path.Combine(output, "Edit_Play_UI_Diff.csv"), string.Join("\r\n", rows), new UTF8Encoding(true));
        }

        private static void WriteComparisonReport(UiSnapshot edit, UiSnapshot play, List<DiffRecord> diff, string output, Vector2Int target, string screenshotMessage)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Edit Mode 与 Play Mode UI 对照审计");
            builder.AppendLine();
            builder.AppendLine("- 目标 Game View：" + target.x + " × " + target.y);
            builder.AppendLine("- Edit 场景：" + edit.sceneNames);
            builder.AppendLine("- Play 场景：" + play.sceneNames);
            builder.AppendLine("- Edit 文本数：" + edit.texts.Count);
            builder.AppendLine("- Play 文本数：" + play.texts.Count);
            builder.AppendLine("- Edit 字体：Title=" + edit.titleFont + "，Ui=" + edit.uiFont + "，UiBold=" + edit.uiFontBold + "，Body=" + edit.bodyFont);
            builder.AppendLine("- Play 字体：Title=" + play.titleFont + "，Ui=" + play.uiFont + "，UiBold=" + play.uiFontBold + "，Body=" + play.bodyFont);
            builder.AppendLine("- 截图：" + screenshotMessage);
            builder.AppendLine();
            builder.AppendLine("## 差异统计");
            builder.AppendLine();
            builder.AppendLine("| 项目 | 数量 |");
            builder.AppendLine("| --- | ---: |");
            builder.AppendLine("| 字体变化 | " + diff.Count(item => item.fontChanged) + " |");
            builder.AppendLine("| 字号变化 | " + diff.Count(item => item.fontSizeChanged) + " |");
            builder.AppendLine("| 字重变化 | " + diff.Count(item => item.fontStyleChanged) + " |");
            builder.AppendLine("| 颜色变化 | " + diff.Count(item => item.colorChanged) + " |");
            builder.AppendLine("| Rect 变化 | " + diff.Count(item => item.rectChanged) + " |");
            builder.AppendLine("| Parent 变化 | " + diff.Count(item => item.parentChanged) + " |");
            builder.AppendLine("| Runtime Only | " + diff.Count(item => item.runtimeOnlyObject) + " |");
            builder.AppendLine("| Edit Only | " + diff.Count(item => item.editOnlyObject) + " |");
            builder.AppendLine();
            builder.AppendLine("## Canvas 与 CanvasScaler");
            builder.AppendLine();
            foreach (var canvas in play.canvases)
            {
                builder.AppendLine("- " + canvas.hierarchyPath + "：" + canvas.renderMode + "，scale=" + canvas.scaleFactor + "，pixelRect=" + canvas.pixelRect + "，scaler=" + canvas.uiScaleMode + " / " + canvas.referenceResolution + " / match=" + canvas.matchWidthOrHeight);
            }

            builder.AppendLine();
            builder.AppendLine("## 重点对象差异");
            builder.AppendLine();
            foreach (var item in diff.Where(IsFocusText).Take(80))
            {
                builder.AppendLine("- " + item.textContent + " | Font " + item.editFont + " → " + item.playFont + " | Size " + item.editFontSize + " → " + item.playFontSize + " | Rect " + item.editRect + " → " + item.playRect);
            }

            builder.AppendLine();
            builder.AppendLine("## 运行时覆盖来源（静态扫描）");
            builder.AppendLine();
            foreach (var source in ScanUiOverrides())
            {
                builder.AppendLine("- " + source);
            }

            builder.AppendLine();
            builder.AppendLine("## 解释建议");
            builder.AppendLine();
            builder.AppendLine("本报告以同一层级路径和文本内容匹配 Edit / Play UI。字体、字号、颜色、Rect、父级与激活状态发生变化的项目应优先检查运行时覆盖来源。截图仅在当前 Game View 实际分辨率等于目标值且 Unity 内部 Game View 捕获接口可用时写入；失败会保留原因，不会以 Scene View 截图替代。");

            File.WriteAllText(Path.Combine(output, "Edit_Play_UI_Comparison_" + target.x + "x" + target.y + ".md"), builder.ToString(), new UTF8Encoding(true));
            File.WriteAllText(Path.Combine(output, "Edit_Play_UI_Comparison.md"), builder.ToString(), new UTF8Encoding(true));
        }

        private static IEnumerable<string> ScanUiOverrides()
        {
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var scriptsRoot = Path.Combine(projectRoot, "Assets", "Scripts");
            if (!Directory.Exists(scriptsRoot))
            {
                yield return "未找到 Assets/Scripts。";
                yield break;
            }

            var needles = new[]
            {
                "ApplyMainFrameLayout", "EnsureToolbarLayout", "ApplyThemeToNavBar", "EnsureCardPaletteShell", "BuildUi", "CreateText", "CreateButton", "ApplyTextRole", "text.font =", "text.fontSize =", "text.fontStyle =", "sizeDelta =", "offsetMin =", "offsetMax =", "SetParent(", "AddComponent<Text>", "AddComponent<Button>"
            };

            foreach (var path in Directory.GetFiles(scriptsRoot, "*.cs", SearchOption.AllDirectories))
            {
                var lines = File.ReadAllLines(path);
                var currentMethod = string.Empty;
                for (var index = 0; index < lines.Length; index++)
                {
                    var line = lines[index];
                    if (line.Contains("(" ) && (line.Contains("private ") || line.Contains("public ") || line.Contains("protected ")) && line.Contains(")"))
                    {
                        currentMethod = line.Trim();
                    }

                    if (needles.Any(needle => line.Contains(needle)))
                    {
                        yield return Path.GetRelativePath(projectRoot, path).Replace('\\', '/') + ":" + (index + 1) + " | " + currentMethod + " | " + line.Trim();
                    }
                }
            }
        }

        private static bool TryCaptureGameView(string output, string mode, Vector2Int target, out string message)
        {
            message = string.Empty;
            if (Screen.width != target.x || Screen.height != target.y)
            {
                message = "未截图：当前 Game View 为 " + Screen.width + " × " + Screen.height + "，目标为 " + target.x + " × " + target.y + "。";
                return false;
            }

            var gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            var gameView = gameViewType == null ? null : Resources.FindObjectsOfTypeAll(gameViewType).FirstOrDefault();
            if (gameView == null)
            {
                message = "未截图：未找到 Game View 窗口。";
                return false;
            }

            var path = Path.Combine(output, mode + "_" + target.x + "x" + target.y + ".png");
            var captureMethods = gameViewType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(method => method.Name.IndexOf("CaptureScreenshot", StringComparison.OrdinalIgnoreCase) >= 0)
                .ToArray();

            foreach (var method in captureMethods)
            {
                try
                {
                    var args = BuildCaptureArguments(method.GetParameters(), path);
                    if (args == null)
                    {
                        continue;
                    }

                    method.Invoke(gameView, args);
                    if (File.Exists(path))
                    {
                        message = "已通过 Game View 捕获：" + Path.GetFileName(path);
                        return true;
                    }
                }
                catch (Exception exception)
                {
                    message = "Game View 截图接口失败：" + exception.GetBaseException().Message;
                }
            }

            if (Application.isPlaying)
            {
                try
                {
                    ScreenCapture.CaptureScreenshot(path, 1);
                    message = "已请求 Play Mode Game View 截图：" + Path.GetFileName(path) + "。";
                    return true;
                }
                catch (Exception exception)
                {
                    message = "Play Mode 截图失败：" + exception.Message;
                }
            }

            if (string.IsNullOrEmpty(message))
            {
                message = "未截图：当前 Unity 版本未暴露可调用的 Game View 捕获接口。";
            }

            return false;
        }

        private static object[] BuildCaptureArguments(ParameterInfo[] parameters, string path)
        {
            var result = new object[parameters.Length];
            for (var index = 0; index < parameters.Length; index++)
            {
                var type = parameters[index].ParameterType;
                if (type == typeof(string)) result[index] = path;
                else if (type == typeof(int)) result[index] = 1;
                else if (type == typeof(bool)) result[index] = false;
                else if (type.IsEnum) result[index] = Activator.CreateInstance(type);
                else if (parameters[index].HasDefaultValue) result[index] = parameters[index].DefaultValue;
                else return null;
            }

            return result;
        }

        private static void WriteCaptureNote(string output, string mode, Vector2Int target, string message)
        {
            File.WriteAllText(Path.Combine(output, mode + "_" + target.x + "x" + target.y + "_Capture.txt"), message, new UTF8Encoding(true));
        }

        private static bool IsSceneObject(UnityEngine.Object item)
        {
            if (item == null) return false;
            var component = item as Component;
            return component != null && component.gameObject.scene.IsValid();
        }

        private static Button FindParentButton(Transform transform)
        {
            var current = transform;
            while (current != null)
            {
                var button = current.GetComponent<Button>();
                if (button != null) return button;
                current = current.parent;
            }

            return null;
        }

        private static string FindNearestLayoutGroup(Transform transform)
        {
            var current = transform.parent;
            while (current != null)
            {
                if (current.GetComponent<HorizontalLayoutGroup>() != null) return "HorizontalLayoutGroup:" + GetHierarchyPath(current);
                if (current.GetComponent<VerticalLayoutGroup>() != null) return "VerticalLayoutGroup:" + GetHierarchyPath(current);
                if (current.GetComponent<GridLayoutGroup>() != null) return "GridLayoutGroup:" + GetHierarchyPath(current);
                current = current.parent;
            }

            return string.Empty;
        }

        private static IEnumerable<string> GetOpenSceneNames()
        {
            for (var index = 0; index < SceneManager.sceneCount; index++)
            {
                var scene = SceneManager.GetSceneAt(index);
                if (scene.IsValid()) yield return scene.name;
            }
        }

        private static bool IsFocusText(DiffRecord record)
        {
            var content = record.textContent ?? string.Empty;
            return content.Contains("电工数字学生仿真系统")
                   || content.Contains("模拟电路")
                   || content.Contains("图纸集")
                   || content.Contains("仿真广场")
                   || content.Contains("元器件百科")
                   || content.Contains("常用工具")
                   || content.Contains("系统信息")
                   || content.Contains("开始仿真")
                   || content.Contains("撤销")
                   || content.Contains("电工控件池")
                   || content.Contains("操作记录")
                   || content.Contains("检查助手");
        }

        private static string GetHierarchyPath(Transform transform)
        {
            if (transform == null) return string.Empty;
            var names = new Stack<string>();
            var current = transform;
            while (current != null)
            {
                names.Push(current.name);
                current = current.parent;
            }

            return string.Join("/", names.ToArray());
        }

        private static string SafeFontName(Font font)
        {
            return font != null ? font.name : "<null>";
        }

        private static string SafeObjectName(UnityEngine.Object item)
        {
            return item != null ? item.name : "<null>";
        }

        private static bool Approximately(float left, float right)
        {
            return Mathf.Abs(left - right) < 0.1f;
        }

        private static string FormatVector(Vector2 value)
        {
            return value.x.ToString("0.##") + "," + value.y.ToString("0.##");
        }

        private static string FormatVector(Vector3 value)
        {
            return value.x.ToString("0.##") + "," + value.y.ToString("0.##") + "," + value.z.ToString("0.##");
        }

        private static string FormatRect(Rect value)
        {
            return value.x.ToString("0.##") + "," + value.y.ToString("0.##") + "," + value.width.ToString("0.##") + "," + value.height.ToString("0.##");
        }

        private static string FormatSize(float width, float height)
        {
            return width.ToString("0.##") + "×" + height.ToString("0.##");
        }

        private static string Csv(params object[] values)
        {
            return string.Join(",", values.Select(value => "\"" + ((value ?? string.Empty).ToString().Replace("\"", "\"\"").Replace("\r", "\\r").Replace("\n", "\\n")) + "\""));
        }

        private static string GetOutputDirectory()
        {
            return Path.Combine(Directory.GetParent(Application.dataPath).FullName, OutputRelativePath);
        }

        private static string GetSnapshotPath(string output, string prefix, Vector2Int target)
        {
            return Path.Combine(output, prefix + "_" + target.x + "x" + target.y + ".json");
        }

        [Serializable]
        private sealed class UiSnapshot
        {
            public string mode;
            public string capturedAt;
            public string unityVersion;
            public string platform;
            public string sceneNames;
            public int screenWidth;
            public int screenHeight;
            public float screenDpi;
            public int targetWidth;
            public int targetHeight;
            public string titleFont;
            public string uiFont;
            public string uiFontBold;
            public string bodyFont;
            public List<CanvasRecord> canvases = new List<CanvasRecord>();
            public List<TextRecord> texts = new List<TextRecord>();
            public List<string> errors = new List<string>();
        }

        [Serializable]
        private sealed class CanvasRecord
        {
            public string sceneName;
            public string hierarchyPath;
            public string name;
            public string renderMode;
            public float scaleFactor;
            public string pixelRect;
            public float rectWidth;
            public float rectHeight;
            public bool hasScaler;
            public string uiScaleMode;
            public string referenceResolution;
            public string screenMatchMode;
            public float matchWidthOrHeight;
            public float referencePixelsPerUnit;
        }

        [Serializable]
        private sealed class TextRecord
        {
            public string sceneName;
            public string hierarchyPath;
            public string gameObjectName;
            public int instanceId;
            public string textContent;
            public string fontName;
            public int fontSize;
            public string fontStyle;
            public string color;
            public string alignment;
            public float lineSpacing;
            public string horizontalOverflow;
            public string verticalOverflow;
            public bool bestFit;
            public int bestFitMin;
            public int bestFitMax;
            public float rectWidth;
            public float rectHeight;
            public string localScale;
            public bool activeSelf;
            public bool activeInHierarchy;
            public string parentButtonPath;
            public string parentLayoutGroup;
        }

        [Serializable]
        private sealed class DiffRecord
        {
            public string key;
            public string textContent;
            public string editPath;
            public string playPath;
            public bool fontChanged;
            public bool fontSizeChanged;
            public bool fontStyleChanged;
            public bool colorChanged;
            public bool rectChanged;
            public bool scaleChanged;
            public bool parentChanged;
            public bool activeStateChanged;
            public bool runtimeOnlyObject;
            public bool editOnlyObject;
            public string editFont;
            public string playFont;
            public int editFontSize;
            public int playFontSize;
            public string editColor;
            public string playColor;
            public string editRect;
            public string playRect;
        }
    }
}
