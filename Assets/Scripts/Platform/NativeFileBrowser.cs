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
#elif UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            try
            {
                string path = WindowsFileDialog.OpenFile("选择外部 JSON 图纸", "JSON 图纸文件 (*.json)|*.json", "json");
                if (!string.IsNullOrEmpty(path))
                {
                    string json = System.IO.File.ReadAllText(path);
                    onJsonReceived?.Invoke(json);
                }
            }
            catch (Exception e)
            {
                onError?.Invoke("无法读取图纸文件：" + e.Message);
            }
#else
            onError?.Invoke("当前平台暂不支持外部导入。");
#endif
        }
    }
}
