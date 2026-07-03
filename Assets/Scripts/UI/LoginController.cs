using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ElectricalSim.UI
{
    public sealed class LoginController : MonoBehaviour
    {
        [SerializeField] private GameObject loginRoot;
        [SerializeField] private List<GameObject> appRoots = new List<GameObject>();
        [SerializeField] private GameObject loginPanel;
        [SerializeField] private GameObject registerPanel;
        [SerializeField] private GameObject forgotPanel;
        [SerializeField] private InputField loginAccountInput;
        [SerializeField] private InputField loginPasswordInput;
        [SerializeField] private InputField registerAccountInput;
        [SerializeField] private InputField registerPasswordInput;
        [SerializeField] private InputField registerConfirmInput;
        [SerializeField] private InputField registerAnswerInput;
        [SerializeField] private InputField forgotAccountInput;
        [SerializeField] private InputField forgotAnswerInput;
        [SerializeField] private InputField forgotNewPasswordInput;
        [SerializeField] private Text messageText;
        [SerializeField] private Text currentUserText;
        [SerializeField] private Button loginButton;
        [SerializeField] private Button showRegisterButton;
        [SerializeField] private Button showForgotButton;
        [SerializeField] private Button registerButton;
        [SerializeField] private Button registerBackButton;
        [SerializeField] private Button resetPasswordButton;
        [SerializeField] private Button forgotBackButton;
        [SerializeField] private Button logoutButton;
        [SerializeField] private string loginSceneName = "LoginScene";
        [SerializeField] private string demoSceneName = "Demo";

        public const string LastUserNameKey = "ElectricalSim.Local.LastUserName";
        public const string LastLoginTimeKey = "ElectricalSim.Local.LastLoginTime";
        private const string LegacyLastAccountKey = "ElectricalSim.LastAccount";
        private const string LegacySessionKey = "ElectricalSim.SessionUser";

        private bool IsLoginScene => SceneManager.GetActiveScene().name == loginSceneName;

        private void Awake()
        {
            if (!IsLoginScene)
            {
                if (loginRoot != null)
                {
                    loginRoot.SetActive(false);
                }

                enabled = false;
                return;
            }

            ClearButtonEvents();
            ConfigureLocalLoginView();
            loginButton?.onClick.AddListener(HandleLogin);
            showRegisterButton?.onClick.AddListener(HandleContinueLastUser);
            registerBackButton?.onClick.AddListener(ShowLogin);
            forgotBackButton?.onClick.AddListener(ShowLogin);
            logoutButton?.onClick.AddListener(Logout);
        }

        private void Start()
        {
            if (!IsLoginScene)
            {
                return;
            }

            ShowLogin();
        }

        private void HandleLogin()
        {
            var userName = Read(loginAccountInput).Trim();
            if (string.IsNullOrWhiteSpace(userName))
            {
                SetMessage("请输入姓名或学号。", true);
                return;
            }

            SaveLocalUser(userName);
            AppSession.Login(userName);
            SceneManager.LoadScene(demoSceneName);
        }

        private void HandleContinueLastUser()
        {
            var userName = PlayerPrefs.GetString(LastUserNameKey, string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(userName))
            {
                SetMessage("没有找到上次本地用户，请输入姓名或学号。", true);
                return;
            }

            SaveLocalUser(userName);
            AppSession.Login(userName);
            SceneManager.LoadScene(demoSceneName);
        }

        private static void SaveLocalUser(string userName)
        {
            PlayerPrefs.SetString(LastUserNameKey, userName);
            PlayerPrefs.SetString(LastLoginTimeKey, System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            PlayerPrefs.DeleteKey(LegacyLastAccountKey);
            PlayerPrefs.DeleteKey(LegacySessionKey);
            PlayerPrefs.Save();
        }

        private void Logout()
        {
            AppSession.Logout();
            SceneManager.LoadScene(loginSceneName);
        }

        private void ShowLogin()
        {
            if (!IsLoginScene)
            {
                return;
            }

            if (loginRoot != null)
            {
                loginRoot.SetActive(true);
            }

            foreach (var root in appRoots)
            {
                if (root != null)
                {
                    root.SetActive(false);
                }
            }

            if (loginAccountInput != null)
            {
                loginAccountInput.text = PlayerPrefs.GetString(LastUserNameKey, string.Empty);
            }

            ConfigureLocalLoginView();
            ShowForm(loginPanel, "单机本地模式，输入姓名或学号即可进入系统。");
        }

        private void ShowForm(GameObject panel, string message)
        {
            if (loginPanel != null)
            {
                loginPanel.SetActive(panel == loginPanel);
            }

            HideAccountSystemObjects();
            SetMessage(message, false);
        }

        private void ConfigureLocalLoginView()
        {
            HideAccountSystemObjects();
            SetText("LoginTitle", "电工数字学生仿真系统");
            SetText("LoginSubtitle", "单机本地模式，输入姓名或学号即可进入系统。");
            SetPlaceholder(loginAccountInput, "请输入姓名或学号");
            ConfigureButtonText(loginButton, "进入系统");

            if (showRegisterButton != null)
            {
                showRegisterButton.gameObject.SetActive(true);
                ConfigureButtonText(showRegisterButton, "继续上次用户");
            }

            if (currentUserText != null)
            {
                currentUserText.text = "当前状态：单机本地模式";
            }

            if (logoutButton != null)
            {
                ConfigureButtonText(logoutButton, "切换本地用户");
            }
        }

        private void ClearButtonEvents()
        {
            loginButton?.onClick.RemoveAllListeners();
            showRegisterButton?.onClick.RemoveAllListeners();
            showForgotButton?.onClick.RemoveAllListeners();
            registerButton?.onClick.RemoveAllListeners();
            registerBackButton?.onClick.RemoveAllListeners();
            resetPasswordButton?.onClick.RemoveAllListeners();
            forgotBackButton?.onClick.RemoveAllListeners();
            logoutButton?.onClick.RemoveAllListeners();
        }

        private void HideAccountSystemObjects()
        {
            SetActive(registerPanel, false);
            SetActive(forgotPanel, false);
            SetActive(loginPasswordInput, false);
            SetActive(registerAccountInput, false);
            SetActive(registerPasswordInput, false);
            SetActive(registerConfirmInput, false);
            SetActive(registerAnswerInput, false);
            SetActive(forgotAccountInput, false);
            SetActive(forgotAnswerInput, false);
            SetActive(forgotNewPasswordInput, false);
            SetActive(showForgotButton, false);
            SetActive(registerButton, false);
            SetActive(registerBackButton, false);
            SetActive(resetPasswordButton, false);
            SetActive(forgotBackButton, false);
        }

        private void SetMessage(string message, bool isError)
        {
            if (messageText == null)
            {
                return;
            }

            messageText.text = message;
            messageText.color = isError ? new Color(0.9f, 0.12f, 0.12f) : new Color(0.12f, 0.32f, 0.64f);
        }

        private static void SetActive(Component component, bool active)
        {
            if (component != null)
            {
                component.gameObject.SetActive(active);
            }
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null)
            {
                target.SetActive(active);
            }
        }

        private static void SetText(string objectName, string value)
        {
            var target = GameObject.Find(objectName);
            var text = target != null ? target.GetComponent<Text>() : null;
            if (text != null)
            {
                text.text = value;
            }
        }

        private static void SetPlaceholder(InputField input, string value)
        {
            if (input == null)
            {
                return;
            }

            var placeholder = input.placeholder as Text;
            if (placeholder != null)
            {
                placeholder.text = value;
            }
        }

        private static void ConfigureButtonText(Button button, string value)
        {
            if (button == null)
            {
                return;
            }

            var label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = value;
            }
        }

        private static string Read(InputField input)
        {
            return input != null ? input.text : string.Empty;
        }
    }
}
