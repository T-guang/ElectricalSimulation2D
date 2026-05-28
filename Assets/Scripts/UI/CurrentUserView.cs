using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ElectricalSim.UI
{
    public sealed class CurrentUserView : MonoBehaviour
    {
        [SerializeField] private Text currentUserText;
        [SerializeField] private Button logoutButton;
        [SerializeField] private string loginSceneName = "LoginScene";

        private void Awake()
        {
            if (currentUserText == null)
            {
                var userObject = GameObject.Find("CurrentUser");
                currentUserText = userObject != null ? userObject.GetComponent<Text>() : null;
            }

            if (logoutButton == null)
            {
                var logoutObject = GameObject.Find("LogoutButton");
                logoutButton = logoutObject != null ? logoutObject.GetComponent<Button>() : null;
            }

            logoutButton?.onClick.AddListener(LogoutToLoginScene);
        }

        private void Start()
        {
            Refresh();
        }

        private void OnDestroy()
        {
            logoutButton?.onClick.RemoveListener(LogoutToLoginScene);
        }

        private void Refresh()
        {
            if (currentUserText == null)
            {
                return;
            }

            currentUserText.text = AppSession.IsLoggedIn
                ? "\u5f53\u524d\u7528\u6237\uff1a" + AppSession.CurrentUser
                : "\u5f53\u524d\u7528\u6237\uff1a\u672a\u767b\u5f55";
        }

        public void LogoutToLoginScene()
        {
            AppSession.Logout();
            SceneManager.LoadScene(loginSceneName);
        }
    }
}
