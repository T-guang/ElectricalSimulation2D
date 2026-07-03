using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace ElectricalSim.Platform
{
    public static class NativeFileBrowser
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void ImportFileWebGL();
#endif

        public static void RequestImportBlueprint(Action<string> onJsonReceived, Action<string> onError)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            NativeFileBrowserReceiver.Instance.Prepare(onJsonReceived, onError);
            ImportFileWebGL();
#elif UNITY_STANDALONE_WIN || UNITY_EDITOR
            try
            {
                var saveDir = Path.Combine(Application.persistentDataPath, "SavedBlueprints");
                if (!Directory.Exists(saveDir))
                {
                    Directory.CreateDirectory(saveDir);
                }
                var normalizedPath = Path.GetFullPath(saveDir).Replace('/', '\\');
                System.Diagnostics.Process.Start("explorer.exe", normalizedPath);
                onError?.Invoke("请把外部 JSON 拷入此文件夹，然后点上方【刷新列表】即可载入。");
            }
            catch (Exception e)
            {
                onError?.Invoke("无法打开文件夹：" + e.Message);
            }
#else
            onError?.Invoke("当前平台暂不支持外部导入。");
#endif
        }
    }
}
