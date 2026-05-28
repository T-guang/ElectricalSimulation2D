namespace ElectricalSim.UI
{
    public static class TemplateEditSession
    {
        public static string CurrentTemplateId { get; private set; }
        public static string CurrentTemplateName { get; private set; }
        public static string CurrentResourcePath { get; private set; }

        public static bool HasSystemTemplateLoaded => !string.IsNullOrEmpty(CurrentTemplateId);

        public static void RecordTemplate(string templateId, string templateName, string resourcePath)
        {
            CurrentTemplateId = templateId;
            CurrentTemplateName = templateName;
            CurrentResourcePath = resourcePath;
        }

        public static void Clear()
        {
            CurrentTemplateId = null;
            CurrentTemplateName = null;
            CurrentResourcePath = null;
        }
    }
}
