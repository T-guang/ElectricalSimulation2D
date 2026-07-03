using System;
using UnityEngine;

namespace ElectricalSim.Platform
{
    public sealed class NativeFileBrowserReceiver : MonoBehaviour
    {
        private static NativeFileBrowserReceiver instance;
        private Action<string> onJsonReceived;
        private Action<string> onError;

        public static NativeFileBrowserReceiver Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("NativeFileBrowserReceiver");
                    instance = go.AddComponent<NativeFileBrowserReceiver>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        public void Prepare(Action<string> onJsonReceived, Action<string> onError)
        {
            this.onJsonReceived = onJsonReceived;
            this.onError = onError;
        }

        public void OnWebGLFileLoaded(string json)
        {
            onJsonReceived?.Invoke(json);
            ClearCallbacks();
        }

        public void OnWebGLFileError(string error)
        {
            onError?.Invoke(error);
            ClearCallbacks();
        }

        private void ClearCallbacks()
        {
            onJsonReceived = null;
            onError = null;
        }
    }
}
