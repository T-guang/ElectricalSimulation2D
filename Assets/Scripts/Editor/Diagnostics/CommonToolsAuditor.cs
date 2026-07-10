using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using ElectricalSim.UI.CommonTools;

public static class CommonToolsAuditor
{
    [MenuItem("Electrical/Diagnostics/Audit Common Tools Page")]
    public static void RunAudit()
    {
        var controller = Object.FindObjectOfType<CommonToolsPageController>(true);
        if (controller == null)
        {
            var go = new GameObject("TestCommonTools");
            controller = go.AddComponent<CommonToolsPageController>();
        }

        // Force rebuild to get latest UI elements
        controller.gameObject.SetActive(true);
        controller.BuildPage();

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("=== Runtime Component Audit for Common Tools ===");

        int shadowCount = 0;
        int outlineCount = 0;
        int suspectNameCount = 0;

        void Traverse(Transform t, string path)
        {
            var currentPath = path + "/" + t.name;
            
            bool hasShadow = t.GetComponent<Shadow>() != null;
            bool hasOutline = t.GetComponent<Outline>() != null;
            
            bool isSuspectName = t.name.Contains("Shadow") || t.name.Contains("Outline") || t.name.Contains("Border") || t.name.Contains("Glow");

            var img = t.GetComponent<Image>();
            
            var mask = t.GetComponent<Mask>();
            var rectMask = t.GetComponent<RectMask2D>();
            bool parentHasMask = t.parent != null && (t.parent.GetComponent<Mask>() != null || t.parent.GetComponent<RectMask2D>() != null);
            
            if (hasShadow) shadowCount++;
            if (hasOutline) outlineCount++;
            if (isSuspectName) suspectNameCount++;

            if (hasShadow || hasOutline || isSuspectName || img != null)
            {
                sb.AppendLine($"- Path: {currentPath}");
                if (hasShadow) sb.AppendLine("  - Has Shadow: Yes");
                if (hasOutline) sb.AppendLine("  - Has Outline: Yes");
                if (isSuspectName) sb.AppendLine("  - Suspect Name: Yes");
                if (parentHasMask) sb.AppendLine("  - Parent has Mask/RectMask2D: Yes");
                if (mask != null) sb.AppendLine("  - Has Mask: Yes");
                if (rectMask != null) sb.AppendLine("  - Has RectMask2D: Yes");
                
                if (img != null)
                {
                    sb.AppendLine($"  - Image Sprite: {(img.sprite != null ? img.sprite.name : "null")}");
                    sb.AppendLine($"  - Image Type: {img.type}");
                    sb.AppendLine($"  - Image Color: #{ColorUtility.ToHtmlStringRGBA(img.color)}");
                }
                var rt = t.GetComponent<RectTransform>();
                if (rt != null)
                {
                    sb.AppendLine($"  - Rect Size: {rt.rect.width}x{rt.rect.height}");
                }
                sb.AppendLine();
            }

            foreach (Transform child in t)
            {
                Traverse(child, currentPath);
            }
        }

        Traverse(controller.transform, controller.name);

        sb.AppendLine("=== Summary ===");
        sb.AppendLine($"Shadow components: {shadowCount}");
        sb.AppendLine($"Outline components: {outlineCount}");
        sb.AppendLine($"Suspect names (Shadow/Outline/Border/Glow): {suspectNameCount}");

        string path = "CommonToolsAuditResult.txt";
        File.WriteAllText(path, sb.ToString());
        Debug.Log("Audit completed. Result written to " + path);
    }
}
