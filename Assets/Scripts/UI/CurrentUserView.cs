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

            if (logoutButton != null)
            {
                logoutButton.gameObject.SetActive(true);
                var label = logoutButton.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.text = "切换本地用户";
                }
            }

            logoutButton?.onClick.AddListener(LogoutToLoginScene);
        }

        private void Start()
        {
            RefreshLocalUserText();
        }

        private void OnDestroy()
        {
            logoutButton?.onClick.RemoveListener(LogoutToLoginScene);
        }

        private void RefreshLocalUserText()
        {
            if (currentUserText == null)
            {
                return;
            }

            var userName = AppSession.IsLoggedIn
                ? AppSession.CurrentUser
                : PlayerPrefs.GetString(LoginController.LastUserNameKey, string.Empty);
            currentUserText.text = string.IsNullOrWhiteSpace(userName)
                ? "当前状态：单机本地模式"
                : "当前状态：单机本地模式  当前用户：" + userName;
        }

        private void Refresh()
        {
            if (currentUserText == null)
            {
                return;
            }

            currentUserText.text = "当前状态：单机本地模式";
        }

        public void LogoutToLoginScene()
        {
            AppSession.Logout();
            SceneManager.LoadScene(loginSceneName);
        }
    }
}
