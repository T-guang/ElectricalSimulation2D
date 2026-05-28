using System;
using System.Collections.Generic;
using ElectricalSim.Core;
using UnityEngine;

namespace ElectricalSim.Templates
{
    [Serializable]
    public sealed class CircuitTemplateDto
    {
        public string templateId;
        public string templateName;
        public string category;
        public string difficulty;
        public string description;
        public List<TemplateComponentDto> components = new List<TemplateComponentDto>();
        public List<TemplateWireDto> wires = new List<TemplateWireDto>();
    }

    [Serializable]
    public sealed class TemplateComponentDto
    {
        public string instanceId;
        public string definitionName;
        public float x;
        public float y;
        public bool isClosed;
        public List<ComponentParameter> parameters = new List<ComponentParameter>();
    }

    [Serializable]
    public sealed class TemplateWireDto
    {
        public string startComponentId;
        public string startTerminalId;
        public string endComponentId;
        public string endTerminalId;
        public string color;
        public string style;
        
        // 布局信息
        public List<Vector2> manualRoutePoints;
        public bool manualRouteHorizontal;
        public float manualRouteAxis;
    }
}
