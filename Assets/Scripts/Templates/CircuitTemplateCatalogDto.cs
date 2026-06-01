using System;
using System.Collections.Generic;

namespace ElectricalSim.Templates
{
    [Serializable]
    public sealed class CircuitTemplateCatalogDto
    {
        public List<CircuitTemplateCatalogItemDto> templates = new List<CircuitTemplateCatalogItemDto>();
    }

    [Serializable]
    public sealed class CircuitTemplateCatalogItemDto
    {
        public string templateId;
        public string templateName;
        public string category;
        public string difficulty;
        public string description;
        public string resourcePath;
        public string thumbnailPath;
        public int sortOrder;
    }
}

