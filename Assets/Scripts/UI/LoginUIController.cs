using QCDC.Auth;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace QCDC.UI
{
    /// <summary>
    /// Manages the user interface for logging in, registering, and resetting passwords.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class LoginUIController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject loginPanel;
        [SerializeField] private GameObject registerPanel;
        [SerializeField] private GameObject resetPanel;
        [SerializeField] private GameObject loadingPanel;

        [Header("Login")]
        [SerializeField] private TMP_InputField loginEmail;
        [SerializeField] private TMP_InputField loginPassword;

        [Header("Register")]
        [SerializeField] private TMP_InputField registerName;
        [SerializeField] private TMP_InputField registerEmail;
        [SerializeField] private TMP_InputField registerPassword;
        [SerializeField] private TMP_InputField confirmPassword;
        [SerializeField] private Toggle termsToggle;

        [Header("Reset")]
        [SerializeField] private TMP_InputField resetEmail;

        [Header("UI Notifications")]
        [SerializeField] private TextMeshProUGUI errorText;

        private CanvasGroup canvasGroup;
        private Coroutine fadeRoutine;
        private Coroutine errorRoutine;

        // Sets up the components needed to fade the screens
        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        // Links the text boxes so errors disappear when the user starts typing
        private void Start()
        {
            ShowLoading();

            // If AuthManager survived from a previous scene, force it to report its state
            if (AuthManager.Instance != null)
            {
                AuthManager.Instance.RefreshSession();
            }

            loginEmail.onValueChanged.AddListener(_ => ClearError());
            loginPassword.onValueChanged.AddListener(_ => ClearError());

            registerName.onValueChanged.AddListener(_ => ClearError());
            registerEmail.onValueChanged.AddListener(_ => ClearError());
            registerPassword.onValueChanged.AddListener(_ => ClearError());
            confirmPassword.onValueChanged.AddListener(_ => ClearError());

            resetEmail.onValueChanged.AddListener(_ => ClearError());
        }

        // Listens for updates from the server when this screen is active
        private void OnEnable()
        {
            AuthManager.OnLoginSuccess += HandleLoginSuccess;
            AuthManager.OnAuthError += ShowError;
            AuthManager.OnResetEmailSent += HandleReset;
            AuthManager.OnSessionChecked += HandleSessionCheck;
            AuthManager.OnLoggedOut += HandleLogout;
        }

        // Stops listening to the server when this screen is inactive
        private void OnDisable()
        {
            AuthManager.OnLoginSuccess -= HandleLoginSuccess;
            AuthManager.OnAuthError -= ShowError;
            AuthManager.OnResetEmailSent -= HandleReset;
            AuthManager.OnSessionChecked -= HandleSessionCheck;
            AuthManager.OnLoggedOut -= HandleLogout;
        }

        // Displays the loading screen
        private void ShowLoading()
        {
            if (loadingPanel != null) loadingPanel.SetActive(true);
        }

        // Hides the loading screen
        private void HideLoading()
        {
            if (loadingPanel != null) loadingPanel.SetActive(false);
        }

        // Drops the user at the login screen after checking their session
        private void HandleSessionCheck()
        {
            HideLoading();
            ShowLogin();
        }

        // Clears the screen to move to the dashboard after a successful login
        private void HandleLoginSuccess(string name, string progress)
        {
            HideLoading();
            SetPanels(false, false, false);
        }

        // Shows a success message and returns to the login screen
        private void HandleReset(string msg)
        {
            HideLoading();
            resetEmail.text = "";
            ShowLogin();
            ShowSuccess("Reset email sent! Please check your Spam/Junk folder and follow the instructions to reset your password.");
        }

        // Returns the user to the login screen when they log out
        private void HandleLogout()
        {
            ShowLogin();
        }

        // Public buttons to switch between different screens
        public void ShowLogin() { ClearError(); FadeTo(true, false, false); AudioManager.Instance.Play(SoundType.UIClick); }
        public void ShowRegister() { ClearError(); FadeTo(false, true, false); AudioManager.Instance.Play(SoundType.UIClick); }
        public void ShowReset() { ClearError(); FadeTo(false, false, true); AudioManager.Instance.Play(SoundType.UIClick); }

        // Turns specific panels on or off
        private void SetPanels(bool l, bool r, bool rs)
        {
            loginPanel.SetActive(l);
            registerPanel.SetActive(r);
            resetPanel.SetActive(rs);
        }

        // Starts the visual fade transition between screens
        private void FadeTo(bool l, bool r, bool rs)
        {
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeRoutine(l, r, rs));
        }

        // Smoothly hides the current screen and reveals the next one
        private IEnumerator FadeRoutine(bool l, bool r, bool rs)
        {
            float duration = 0.15f;
            float t = 0;

            while (t < duration)
            {
                t += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(1, 0, t / duration);
                yield return null;
            }

            SetPanels(l, r, rs);

            t = 0;
            while (t < duration)
            {
                t += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(0, 1, t / duration);
                yield return null;
            }

            canvasGroup.alpha = 1;
        }

        // Sends the user's login details to the server
        public void OnLoginClick()
        {
            AudioManager.Instance.Play(SoundType.UIClick);
            if (!ValidateLogin()) return;
            if (AuthManager.Instance == null)
            {
                ShowError("Auth service unavailable");
                return;
            }

            ShowLoading();
            AuthManager.Instance.LoginUser(loginEmail.text.Trim(), loginPassword.text);
        }

        // Sends the user's registration details to the server
        public void OnRegisterClick()
        {
            AudioManager.Instance.Play(SoundType.UIClick);
            if (!ValidateRegister()) return;
            if (AuthManager.Instance == null)
            {
                ShowError("Auth service unavailable");
                return;
            }

            ShowLoading();
            AuthManager.Instance.RegisterUser(
                registerName.text.Trim(),
                registerEmail.text.Trim(),
                registerPassword.text
            );
        }

        // Sends a request to reset the user's password
        public void OnResetClick()
        {
            string emailToReset = resetEmail.text.Trim();
            AudioManager.Instance.Play(SoundType.UIClick);
            if (string.IsNullOrEmpty(emailToReset))
            {
                ShowError("Enter email");
                return;
            }

            if (!IsValidEmail(emailToReset))
            {
                ShowError("Invalid email format");
                return;
            }

            if (AuthManager.Instance == null)
            {
                ShowError("Auth service unavailable");
                return;
            }

            ShowLoading();
            AuthManager.Instance.ResetPassword(emailToReset);
        }

        // Checks if the login fields are filled out correctly
        private bool ValidateLogin()
        {
            if (IsEmpty(loginEmail.text, loginPassword.text))
                return ShowErrorReturn("Fields cannot be empty");

            if (!IsValidEmail(loginEmail.text))
                return ShowErrorReturn("Invalid email");

            return true;
        }

        // Checks if the registration fields are filled out correctly
        private bool ValidateRegister()
        {
            if (string.IsNullOrEmpty(registerName.text))
                return ShowErrorReturn("Enter your name");

            if (termsToggle != null && !termsToggle.isOn)
                return ShowErrorReturn("Accept terms to continue");

            if (!IsValidEmail(registerEmail.text))
                return ShowErrorReturn("Invalid email");

            if (registerPassword.text.Length < 6)
                return ShowErrorReturn("Password must be at least 6 characters");

            if (registerPassword.text != confirmPassword.text)
                return ShowErrorReturn("Passwords do not match");

            return true;
        }

        // Displays an error message on the screen and stops the process
        private bool ShowErrorReturn(string msg)
        {
            ShowError(msg);
            return false;
        }

        // Checks if any text boxes were left empty
        private bool IsEmpty(params string[] fields)
        {
            foreach (var f in fields)
                if (string.IsNullOrEmpty(f)) return true;

            return false;
        }

        // Makes sure the email looks like a real email address
        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;

            try
            {
                _ = new System.Net.Mail.MailAddress(email);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        // Displays a red error message on the screen
        private void ShowError(string msg)
        {
            HideLoading();
            errorText.text = $"<color=red>{msg}</color>";

            if (errorRoutine != null) StopCoroutine(errorRoutine);

            errorRoutine = StartCoroutine(HideNotification(3f));
        }

        // Displays a green success message on the screen
        private void ShowSuccess(string msg)
        {
            HideLoading();
            errorText.text = $"<color=green>{msg}</color>";

            if (errorRoutine != null) StopCoroutine(errorRoutine);

            errorRoutine = StartCoroutine(HideNotification(5f));
        }

        // Clears any notifications after a set number of seconds
        private IEnumerator HideNotification(float delay)
        {
            yield return new WaitForSeconds(delay);
            ClearError();
        }

        // Empties the notification text box
        private void ClearError()
        {
            errorText.text = "";
        }
    }
}