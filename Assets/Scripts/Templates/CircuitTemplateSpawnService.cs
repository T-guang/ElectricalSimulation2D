using System.Collections.Generic;
using ElectricalSim.Core;
using UnityEngine;

namespace ElectricalSim.Templates
{
    public static class CircuitTemplateSpawnService
    {
        public static bool Spawn(
            CircuitTemplateDto template,
            WorkspaceController workspace,
            IReadOnlyList<ComponentDefinition> catalog,
            out string message)
        {
            message = string.Empty;
            if (template == null)
            {
                message = "模板为空。";
                return false;
            }

            if (workspace == null)
            {
                message = "模板加载失败：未绑定工作区。";
                return false;
            }

            if (catalog == null || catalog.Count == 0)
            {
                message = "模板加载失败：元件库为空。";
                return false;
            }

            workspace.RecordHistoryCheckpoint();
            workspace.ClearDrawing(false);

            var spawned = new Dictionary<string, CircuitComponent>();
            foreach (var item in template.components)
            {
                var definition = FindDefinition(catalog, item.definitionName);
                if (definition == null)
                {
                    workspace.SetStatus("模板缺少元件定义：" + item.definitionName);
                    continue;
                }

                var component = workspace.SpawnComponent(definition, new Vector2(item.x, item.y), item.instanceId, false);
                if (component == null)
                {
                    workspace.SetStatus("模板元件生成失败：" + item.instanceId);
                    continue;
                }

                component.SetClosed(item.isClosed);
                component.SetParameters(item.parameters);
                spawned[item.instanceId] = component;
            }

            foreach (var item in template.wires)
            {
                if (!spawned.TryGetValue(item.startComponentId, out var startComponent) ||
                    !spawned.TryGetValue(item.endComponentId, out var endComponent))
                {
                    workspace.SetStatus("模板导线生成失败：找不到元件 " + item.startComponentId + " -> " + item.endComponentId);
                    continue;
                }

                var start = startComponent.GetTerminal(item.startTerminalId);
                var end = endComponent.GetTerminal(item.endTerminalId);
                if (start == null || end == null)
                {
                    workspace.SetStatus("模板导线生成失败：找不到端子 " + item.startComponentId + "." + item.startTerminalId + " -> " + item.endComponentId + "." + item.endTerminalId);
                    continue;
                }

                var wire = workspace.WireManager.CreateWire(start, end, ParseColor(item.color), ParseStyle(item.style));
                if (wire == null)
                {
                    continue;
                }

                if (item.manualRoutePoints != null && item.manualRoutePoints.Count >= 2)
                {
                    wire.SetManualRoutePoints(item.manualRoutePoints);
                }
                else if (!Mathf.Approximately(item.manualRouteAxis, 0f))
                {
                    wire.SetManualRoute(item.manualRouteHorizontal, item.manualRouteAxis);
                }
            }

            workspace.WireManager.RefreshAll();
            workspace.MarkSimulationDirty();
            message = "已加载模板：" + template.templateName;
            workspace.SetStatus(message);
            return true;
        }

        private static ComponentDefinition FindDefinition(IReadOnlyList<ComponentDefinition> catalog, string definitionName)
        {
            if (string.IsNullOrWhiteSpace(definitionName))
            {
                return null;
            }

            for (var i = 0; i < catalog.Count; i++)
            {
                var item = catalog[i];
                if (item != null && item.name == definitionName)
                {
                    return item;
                }
            }

            return null;
        }

        private static WireStyle ParseStyle(string style)
        {
            if (!string.IsNullOrWhiteSpace(style) && System.Enum.TryParse(style, out WireStyle parsed))
            {
                return parsed;
            }

            return WireStyle.Orthogonal;
        }

        private static Color ParseColor(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return new Color(0.95f, 0.12f, 0.12f);
            }

            switch (value.Trim().ToLowerInvariant())
            {
                case "blue":
                    return new Color(0.1f, 0.35f, 0.95f);
                case "green":
                    return new Color(0.08f, 0.65f, 0.25f);
                case "yellow":
                    return new Color(0.95f, 0.78f, 0.12f);
                case "red":
                    return new Color(0.95f, 0.12f, 0.12f);
            }

            if (ColorUtility.TryParseHtmlString(value.StartsWith("#") ? value : "#" + value, out var color))
            {
                return color;
            }

            return new Color(0.95f, 0.12f, 0.12f);
        }
    }
}
