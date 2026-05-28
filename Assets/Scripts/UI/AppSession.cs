using UnityEngine;

namespace ElectricalSim.UI
{
    public sealed class AppSession : MonoBehaviour
    {
        private static AppSession instance;

        public static string CurrentUser { get; private set; } = string.Empty;
        public static bool IsLoggedIn => !string.IsNullOrWhiteSpace(CurrentUser);

        public static AppSession Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }

                var existing = FindObjectOfType<AppSession>();
                if (existing != null)
                {
                    instance = existing;
                    return instance;
                }

                var sessionObject = new GameObject("AppSession");
                instance = sessionObject.AddComponent<AppSession>();
                return instance;
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public static void Login(string account)
        {
            Instance.EnsureAlive();
            CurrentUser = account;
        }

        public static void Logout()
        {
            CurrentUser = string.Empty;
        }

        private void EnsureAlive()
        {
        }
    }
}
