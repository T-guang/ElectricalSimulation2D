using System;

namespace ElectricalSim.UI
{
    [Serializable]
    public sealed class SavedBlueprintInfo
    {
        public string documentId;
        public string documentName;
        public string savedAt;
        public string fileName;
        public string filePath;
        public DateTime lastWriteTime;
    }
}
