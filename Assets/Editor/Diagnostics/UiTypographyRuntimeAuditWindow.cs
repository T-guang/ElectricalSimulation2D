using ElectricalSim.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace ElectricalSim.Editor.Diagnostics
{
    public sealed class UiTypographyRuntimeAuditWindow : EditorWindow
    {
        private const string MenuPath = "Tools/UI/导出字体运行时审计";

        [MenuItem(MenuPath)]
        private static void ExportTypographyAudit()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "UI 字体运行时审计",
                    "请先进入 Play Mode，并等待主界面加载完成。",
                    "确定");
                return;
            }

            var existingRunner = Object.FindObjectOfType<UiTypographyRuntimeAuditRunner>();
            if (existingRunner != null)
            {
                Debug.LogWarning("UI 字体运行时审计已在执行中，请等待当前任务完成。");
                return;
            }

            var host = new GameObject("UiTypographyRuntimeAuditRunner");
            host.hideFlags = HideFlags.HideAndDontSave;
            var runner = host.AddComponent<UiTypographyRuntimeAuditRunner>();
            runner.BeginAudit();
        }
    }
}
