using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using ElectricalSim.Core;

namespace ElectricalSim.UI
{
    public sealed class SaveLoadService : MonoBehaviour
    {
        [SerializeField] private WorkspaceController workspace;
        [SerializeField] private List<ComponentDefinition> catalog = new List<ComponentDefinition>();

        public IReadOnlyList<ComponentDefinition> Catalog => catalog;

        public string SavedBlueprintDirectory => Path.Combine(Application.persistentDataPath, "SavedBlueprints");

        private string LegacySavePath => Path.Combine(Application.persistentDataPath, "electrical_demo_drawing.json");

        public void Initialize(WorkspaceController targetWorkspace, List<ComponentDefinition> definitions)
        {
            workspace = targetWorkspace;
            catalog = definitions;
        }

        public void Save()
        {
            SaveAs("未命名图纸");
        }

        public bool SaveAs(string documentName)
        {
            return SaveAs(documentName, false, out _, out _, out _);
        }

        public bool SaveAs(string documentName, bool overwrite, out SavedBlueprintInfo savedInfo, out bool exists, out string error)
        {
            savedInfo = null;
            error = null;
            exists = false;
            if (workspace == null)
            {
                error = "保存失败：工作区未初始化。";
                return false;
            }

            var safeName = SanitizeDocumentName(documentName);
            if (string.IsNullOrWhiteSpace(safeName))
            {
                error = "图纸名称不能为空。";
                return false;
            }

            try
            {
                EnsureSaveDirectory();
                var path = CreateUniqueSavePath(safeName);
                
                if (!overwrite && File.Exists(path))
                {
                    exists = true;
                    error = "已存在同名文件";
                    return false;
                }

                var drawing = CreateDrawingDto();
                drawing.documentId = Guid.NewGuid().ToString("N");
                drawing.documentName = safeName;
                drawing.savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                File.WriteAllText(path, JsonUtility.ToJson(drawing, true));
                savedInfo = CreateInfo(path, drawing);
                workspace.SetStatus(overwrite ? "已覆盖保存图纸：" + safeName : "已保存图纸：" + safeName);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                error = "保存失败：" + exception.Message;
                workspace.SetStatus(error);
                return false;
            }
        }

        public void Load()
        {
            if (!File.Exists(LegacySavePath))
            {
                workspace.SetStatus("未找到旧版保存图纸。请使用导入图纸选择已保存文件。");
                return;
            }

            LoadFromFile(LegacySavePath);
        }

        public bool LoadFromFile(string filePath)
        {
            return LoadFromFile(filePath, out _);
        }

        public bool LoadFromFile(string filePath, out string error)
        {
            error = null;
            if (workspace == null)
            {
                error = "导入失败：工作区未初始化。";
                return false;
            }

            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                error = "导入失败：图纸文件不存在。";
                workspace.SetStatus(error);
                return false;
            }

            try
            {
                var drawing = JsonUtility.FromJson<DrawingDto>(File.ReadAllText(filePath));
                ApplyDrawingDto(drawing);
                workspace.SetStatus("图纸已加载：" + ResolveDocumentName(drawing, filePath));
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                error = "导入失败：" + exception.Message;
                workspace.SetStatus(error);
                return false;
            }
        }

        public List<SavedBlueprintInfo> ListSavedBlueprints()
        {
            EnsureSaveDirectory();
            var result = new List<SavedBlueprintInfo>();
            foreach (var filePath in Directory.GetFiles(SavedBlueprintDirectory, "*.json"))
            {
                try
                {
                    result.Add(CreateInfoFast(filePath));
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"读取保存图纸失败：{filePath}\n{exception.Message}");
                }
            }

            return result.OrderByDescending(item => item.lastWriteTime).ToList();
        }

        public bool DeleteSavedBlueprint(string filePath)
        {
            return DeleteSavedBlueprint(filePath, out _);
        }

        public bool DeleteSavedBlueprint(string filePath, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(filePath))
            {
                error = "删除失败：图纸文件路径为空。";
                workspace?.SetStatus(error);
                return false;
            }

            try
            {
                EnsureSaveDirectory();
                var saveDirectory = Path.GetFullPath(SavedBlueprintDirectory)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    + Path.DirectorySeparatorChar;
                var targetPath = Path.GetFullPath(filePath);

                if (!targetPath.StartsWith(saveDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    error = "删除失败：只能删除用户保存的图纸。";
                    workspace?.SetStatus(error);
                    return false;
                }

                if (!string.Equals(Path.GetExtension(targetPath), ".json", StringComparison.OrdinalIgnoreCase))
                {
                    error = "删除失败：只能删除 json 图纸文件。";
                    workspace?.SetStatus(error);
                    return false;
                }

                if (!File.Exists(targetPath))
                {
                    error = "删除失败：图纸文件不存在。";
                    workspace?.SetStatus(error);
                    return false;
                }

                DrawingDto drawing = null;
                try
                {
                    drawing = JsonUtility.FromJson<DrawingDto>(File.ReadAllText(targetPath));
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"读取待删除图纸信息失败：{targetPath}\n{exception.Message}");
                }

                var documentName = ResolveDocumentName(drawing, targetPath);
                File.Delete(targetPath);
                workspace?.SetStatus("已删除图纸：" + documentName);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                error = "删除失败：" + exception.Message;
                workspace?.SetStatus(error);
                return false;
            }
        }

        private DrawingDto CreateDrawingDto()
        {
            var drawing = new DrawingDto();

            foreach (var component in workspace.Components)
            {
                drawing.components.Add(new ComponentDto
                {
                    instanceId = component.InstanceId,
                    definitionName = component.Definition.name,
                    x = ((RectTransform)component.transform).anchoredPosition.x,
                    y = ((RectTransform)component.transform).anchoredPosition.y,
                    isClosed = component.IsClosed,
                    parameters = component.CloneParameters()
                });
            }

            foreach (var wire in workspace.WireManager.Wires)
            {
                drawing.wires.Add(new WireDto
                {
                    startComponentId = wire.StartTerminal.Owner.InstanceId,
                    startTerminalId = wire.StartTerminal.TerminalId,
                    endComponentId = wire.EndTerminal.Owner.InstanceId,
                    endTerminalId = wire.EndTerminal.TerminalId,
                    color = ColorUtility.ToHtmlStringRGBA(wire.WireColor),
                    style = wire.Style.ToString(),
                    hasManualRoute = wire.HasManualRoute,
                    manualRouteHorizontal = wire.ManualRouteHorizontal,
                    manualRouteAxis = wire.ManualRouteAxis,
                    manualRoutePoints = new List<Vector2>(wire.ManualRoutePoints)
                });
            }

            return drawing;
        }

        private void ApplyDrawingDto(DrawingDto drawing)
        {
            if (drawing == null)
            {
                return;
            }

            workspace.ClearDrawing();

            foreach (var item in drawing.components ?? new List<ComponentDto>())
            {
                var definition = catalog.Find(d => d.name == item.definitionName);
                if (definition == null)
                {
                    continue;
                }

                var component = workspace.SpawnComponent(definition, new Vector2(item.x, item.y), item.instanceId, false);
                component.SetClosed(item.isClosed);
                component.SetParameters(item.parameters);
            }

            foreach (var item in drawing.wires ?? new List<WireDto>())
            {
                var start = workspace.FindComponent(item.startComponentId)?.GetTerminal(item.startTerminalId);
                var end = workspace.FindComponent(item.endComponentId)?.GetTerminal(item.endTerminalId);
                var color = Color.white;
                ColorUtility.TryParseHtmlString("#" + item.color, out color);
                var style = WireStyle.Orthogonal;
                Enum.TryParse(item.style, out style);
                var wire = workspace.WireManager.CreateWire(start, end, color, style);
                if (wire != null && item.hasManualRoute)
                {
                    if (item.manualRoutePoints != null && item.manualRoutePoints.Count >= 2)
                    {
                        wire.SetManualRoutePoints(item.manualRoutePoints);
                    }
                    else
                    {
                        wire.SetManualRoute(item.manualRouteHorizontal, item.manualRouteAxis);
                    }
                }
            }

            workspace.WireManager.RefreshAll();
            workspace.MarkSimulationDirty();
            workspace.ClearHistory();
        }

        private void EnsureSaveDirectory()
        {
            if (!Directory.Exists(SavedBlueprintDirectory))
            {
                Directory.CreateDirectory(SavedBlueprintDirectory);
            }
        }

        private string CreateUniqueSavePath(string safeName)
        {
            return Path.Combine(SavedBlueprintDirectory, safeName + ".json");
        }

        private static string SanitizeDocumentName(string documentName)
        {
            if (string.IsNullOrWhiteSpace(documentName))
            {
                return string.Empty;
            }

            var invalid = new HashSet<char>(Path.GetInvalidFileNameChars().Concat(new[] { '\\', '/', ':', '*', '?', '"', '<', '>', '|' }));
            var chars = documentName.Trim().Where(c => !invalid.Contains(c)).ToArray();
            return new string(chars).Trim();
        }

        private static SavedBlueprintInfo CreateInfo(string filePath, DrawingDto drawing)
        {
            return new SavedBlueprintInfo
            {
                documentId = drawing != null ? drawing.documentId : string.Empty,
                documentName = ResolveDocumentName(drawing, filePath),
                savedAt = drawing != null && !string.IsNullOrWhiteSpace(drawing.savedAt) ? drawing.savedAt : "未知时间",
                fileName = Path.GetFileName(filePath),
                filePath = filePath,
                lastWriteTime = File.Exists(filePath) ? File.GetLastWriteTime(filePath) : DateTime.MinValue
            };
        }

        private static SavedBlueprintInfo CreateInfoFast(string filePath)
        {
            var writeTime = File.Exists(filePath) ? File.GetLastWriteTime(filePath) : DateTime.MinValue;
            return new SavedBlueprintInfo
            {
                documentId = string.Empty,
                documentName = Path.GetFileNameWithoutExtension(filePath),
                savedAt = writeTime.ToString("yyyy-MM-dd HH:mm:ss"),
                fileName = Path.GetFileName(filePath),
                filePath = filePath,
                lastWriteTime = writeTime
            };
        }

        private static string ResolveDocumentName(DrawingDto drawing, string filePath)
        {
            if (drawing != null && !string.IsNullOrWhiteSpace(drawing.documentName))
            {
                return drawing.documentName;
            }

            return Path.GetFileNameWithoutExtension(filePath);
        }

        [Serializable]
        private sealed class DrawingDto
        {
            public string documentId;
            public string documentName;
            public string savedAt;
            public List<ComponentDto> components = new List<ComponentDto>();
            public List<WireDto> wires = new List<WireDto>();
        }

        [Serializable]
        private sealed class ComponentDto
        {
            public string instanceId;
            public string definitionName;
            public float x;
            public float y;
            public bool isClosed;
            public List<ComponentParameter> parameters = new List<ComponentParameter>();
        }

        [Serializable]
        private sealed class WireDto
        {
            public string startComponentId;
            public string startTerminalId;
            public string endComponentId;
            public string endTerminalId;
            public string color;
            public string style;
            public bool hasManualRoute;
            public bool manualRouteHorizontal;
            public float manualRouteAxis;
            public List<Vector2> manualRoutePoints = new List<Vector2>();
        }
    }
}

