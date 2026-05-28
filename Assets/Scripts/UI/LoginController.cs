using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
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

        private const string AccountPrefix = "ElectricalSim.Account.";
        private const string LastAccountKey = "ElectricalSim.LastAccount";
        private const string LegacySessionKey = "ElectricalSim.SessionUser";
        private const string DemoAccount = "admin";
        private const string DemoPassword = "123456";
        private const string DemoAnswer = "demo";

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

            loginButton?.onClick.AddListener(HandleLogin);
            showRegisterButton?.onClick.AddListener(() => ShowForm(registerPanel, "\u6ce8\u518c\u65b0\u8d26\u53f7\uff0c\u5b89\u5168\u7b54\u6848\u7528\u4e8e\u6f14\u793a\u7248\u627e\u56de\u5bc6\u7801\u3002"));
            showForgotButton?.onClick.AddListener(() => ShowForm(forgotPanel, "\u8f93\u5165\u8d26\u53f7\u3001\u5b89\u5168\u7b54\u6848\u548c\u65b0\u5bc6\u7801\u5373\u53ef\u91cd\u7f6e\u3002"));
            registerButton?.onClick.AddListener(HandleRegister);
            registerBackButton?.onClick.AddListener(() => ShowForm(loginPanel, "\u8bf7\u8f93\u5165\u8d26\u53f7\u5bc6\u7801\u767b\u5f55\u7cfb\u7edf\u3002"));
            resetPasswordButton?.onClick.AddListener(HandleResetPassword);
            forgotBackButton?.onClick.AddListener(() => ShowForm(loginPanel, "\u8bf7\u8f93\u5165\u8d26\u53f7\u5bc6\u7801\u767b\u5f55\u7cfb\u7edf\u3002"));
            logoutButton?.onClick.AddListener(Logout);
        }

        private void Start()
        {
            if (!IsLoginScene)
            {
                return;
            }

            EnsureDemoAccount();
            PlayerPrefs.DeleteKey(LegacySessionKey);
            PlayerPrefs.Save();
            ShowLogin();
        }

        private void HandleLogin()
        {
            var account = NormalizeAccount(loginAccountInput);
            var password = Read(loginPasswordInput);
            if (string.IsNullOrWhiteSpace(account) || string.IsNullOrWhiteSpace(password))
            {
                SetMessage("\u8bf7\u8f93\u5165\u8d26\u53f7\u548c\u5bc6\u7801\u3002", true);
                return;
            }

            if (!HasAccount(account))
            {
                SetMessage("\u8d26\u53f7\u4e0d\u5b58\u5728\uff0c\u53ef\u4ee5\u5148\u6ce8\u518c\u3002", true);
                return;
            }

            if (!HashesMatch(password, GetStored(account, "PasswordHash")))
            {
                SetMessage("\u5bc6\u7801\u4e0d\u6b63\u786e\u3002", true);
                return;
            }

            PlayerPrefs.SetString(LastAccountKey, account);
            PlayerPrefs.DeleteKey(LegacySessionKey);
            PlayerPrefs.Save();
            AppSession.Login(account);
            SceneManager.LoadScene(demoSceneName);
        }

        private void HandleRegister()
        {
            var account = NormalizeAccount(registerAccountInput);
            var password = Read(registerPasswordInput);
            var confirm = Read(registerConfirmInput);
            var answer = Read(registerAnswerInput);

            if (string.IsNullOrWhiteSpace(account) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(answer))
            {
                SetMessage("\u8d26\u53f7\u3001\u5bc6\u7801\u548c\u5b89\u5168\u7b54\u6848\u90fd\u9700\u8981\u586b\u5199\u3002", true);
                return;
            }

            if (account.Length < 3)
            {
                SetMessage("\u8d26\u53f7\u81f3\u5c11 3 \u4e2a\u5b57\u7b26\u3002", true);
                return;
            }

            if (password.Length < 6)
            {
                SetMessage("\u5bc6\u7801\u81f3\u5c11 6 \u4f4d\u3002", true);
                return;
            }

            if (password != confirm)
            {
                SetMessage("\u4e24\u6b21\u8f93\u5165\u7684\u5bc6\u7801\u4e0d\u4e00\u81f4\u3002", true);
                return;
            }

            if (HasAccount(account))
            {
                SetMessage("\u8d26\u53f7\u5df2\u5b58\u5728\uff0c\u8bf7\u6362\u4e00\u4e2a\u8d26\u53f7\u3002", true);
                return;
            }

            SaveAccount(account, password, answer);
            PlayerPrefs.SetString(LastAccountKey, account);
            PlayerPrefs.Save();
            loginAccountInput.text = account;
            loginPasswordInput.text = string.Empty;
            ShowForm(loginPanel, "\u6ce8\u518c\u6210\u529f\uff0c\u8bf7\u8f93\u5165\u5bc6\u7801\u767b\u5f55\u3002");
        }

        private void HandleResetPassword()
        {
            var account = NormalizeAccount(forgotAccountInput);
            var answer = Read(forgotAnswerInput);
            var newPassword = Read(forgotNewPasswordInput);

            if (string.IsNullOrWhiteSpace(account) || string.IsNullOrWhiteSpace(answer) || string.IsNullOrWhiteSpace(newPassword))
            {
                SetMessage("\u8d26\u53f7\u3001\u5b89\u5168\u7b54\u6848\u548c\u65b0\u5bc6\u7801\u90fd\u9700\u8981\u586b\u5199\u3002", true);
                return;
            }

            if (!HasAccount(account))
            {
                SetMessage("\u8d26\u53f7\u4e0d\u5b58\u5728\u3002", true);
                return;
            }

            if (!HashesMatch(answer, GetStored(account, "AnswerHash")))
            {
                SetMessage("\u5b89\u5168\u7b54\u6848\u4e0d\u6b63\u786e\u3002", true);
                return;
            }

            if (newPassword.Length < 6)
            {
                SetMessage("\u65b0\u5bc6\u7801\u81f3\u5c11 6 \u4f4d\u3002", true);
                return;
            }

            PlayerPrefs.SetString(Key(account, "PasswordHash"), Hash(newPassword));
            PlayerPrefs.SetString(LastAccountKey, account);
            PlayerPrefs.Save();
            loginAccountInput.text = account;
            loginPasswordInput.text = string.Empty;
            ShowForm(loginPanel, "\u5bc6\u7801\u5df2\u91cd\u7f6e\uff0c\u8bf7\u4f7f\u7528\u65b0\u5bc6\u7801\u767b\u5f55\u3002");
        }

        private void Logout()
        {
            AppSession.Logout();
            SceneManager.LoadScene(loginSceneName);
        }

        private void ShowLogin()
        {
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
                loginAccountInput.text = PlayerPrefs.GetString(LastAccountKey, string.Empty);
            }

            if (loginPasswordInput != null)
            {
                loginPasswordInput.text = string.Empty;
            }

            ShowForm(loginPanel, "\u8bf7\u8f93\u5165\u8d26\u53f7\u5bc6\u7801\u767b\u5f55\u7cfb\u7edf\u3002\u6f14\u793a\u8d26\u53f7\uff1aadmin / 123456");
        }

        private void ShowForm(GameObject panel, string message)
        {
            if (loginPanel != null)
            {
                loginPanel.SetActive(panel == loginPanel);
            }

            if (registerPanel != null)
            {
                registerPanel.SetActive(panel == registerPanel);
            }

            if (forgotPanel != null)
            {
                forgotPanel.SetActive(panel == forgotPanel);
            }

            SetMessage(message, false);
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

        private void EnsureDemoAccount()
        {
            if (!HasAccount(DemoAccount))
            {
                SaveAccount(DemoAccount, DemoPassword, DemoAnswer);
            }
        }

        private static void SaveAccount(string account, string password, string answer)
        {
            PlayerPrefs.SetString(Key(account, "Exists"), "1");
            PlayerPrefs.SetString(Key(account, "PasswordHash"), Hash(password));
            PlayerPrefs.SetString(Key(account, "AnswerHash"), Hash(answer));
            PlayerPrefs.Save();
        }

        private static bool HasAccount(string account)
        {
            return PlayerPrefs.GetString(Key(account, "Exists"), string.Empty) == "1";
        }

        private static bool HashesMatch(string raw, string storedHash)
        {
            return !string.IsNullOrEmpty(storedHash) && Hash(raw) == storedHash;
        }

        private static string GetStored(string account, string field)
        {
            return PlayerPrefs.GetString(Key(account, field), string.Empty);
        }

        private static string Key(string account, string field)
        {
            var bytes = Encoding.UTF8.GetBytes(account.Trim().ToLowerInvariant());
            return AccountPrefix + Convert.ToBase64String(bytes) + "." + field;
        }

        private static string Hash(string value)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value.Trim()));
                return Convert.ToBase64String(bytes);
            }
        }

        private static string NormalizeAccount(InputField input)
        {
            return Read(input).Trim().ToLowerInvariant();
        }

        private static string Read(InputField input)
        {
            return input != null ? input.text : string.Empty;
        }
    }
}
