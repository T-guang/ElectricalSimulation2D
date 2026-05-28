using System.IO;
using UnityEngine;

namespace ElectricalSim.Templates
{
    public static class CircuitTemplateLoader
    {
        public static bool TryLoad(string resourcesPath, out CircuitTemplateDto template, out string error)
        {
            template = null;
            error = null;

            if (string.IsNullOrWhiteSpace(resourcesPath))
            {
                error = "模板路径为空。";
                return false;
            }

            var json = ReadTemplateJson(resourcesPath, out error);
            if (string.IsNullOrWhiteSpace(json))
            {
                error = string.IsNullOrWhiteSpace(error) ? "模板读取失败：" + resourcesPath : error;
                return false;
            }

            try
            {
                template = JsonUtility.FromJson<CircuitTemplateDto>(json);
            }
            catch (System.Exception exception)
            {
                error = "模板解析失败：" + exception.Message;
                return false;
            }

            if (template == null || string.IsNullOrWhiteSpace(template.templateId))
            {
                error = "模板数据为空或缺少 templateId。";
                return false;
            }

            return true;
        }

        private static string ReadTemplateJson(string resourcesPath, out string error)
        {
            error = null;

#if UNITY_EDITOR
            var assetPath = Path.Combine(Application.dataPath, "Resources", resourcesPath + ".json").Replace("\\", "/");
            if (File.Exists(assetPath))
            {
                try
                {
                    return File.ReadAllText(assetPath);
                }
                catch (System.Exception exception)
                {
                    error = "模板磁盘读取失败：" + exception.Message;
                    return null;
                }
            }
#endif

            var asset = Resources.Load<TextAsset>(resourcesPath);
            if (asset == null)
            {
                error = "模板读取失败：" + resourcesPath;
                return null;
            }

            return asset.text;
        }
    }
}
