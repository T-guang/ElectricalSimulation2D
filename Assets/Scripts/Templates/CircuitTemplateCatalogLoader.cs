using UnityEngine;

namespace ElectricalSim.Templates
{
    public static class CircuitTemplateCatalogLoader
    {
        public static bool TryLoad(string resourcesPath, out CircuitTemplateCatalogDto catalog, out string error)
        {
            catalog = null;
            error = null;

            if (string.IsNullOrWhiteSpace(resourcesPath))
            {
                error = "模板目录路径为空。";
                return false;
            }

            var asset = Resources.Load<TextAsset>(resourcesPath);
            if (asset == null)
            {
                error = "模板目录读取失败。";
                return false;
            }

            try
            {
                catalog = JsonUtility.FromJson<CircuitTemplateCatalogDto>(asset.text);
            }
            catch (System.Exception exception)
            {
                error = "模板目录解析失败：" + exception.Message;
                return false;
            }

            if (catalog == null || catalog.templates == null)
            {
                error = "模板目录数据为空。";
                return false;
            }

            return true;
        }
    }
}
