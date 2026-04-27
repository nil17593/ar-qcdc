using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using QCDC.Auth;

namespace QCDC.UI
{
    /// <summary>
    /// Controls the main menu screen where users can see their progress and start training.
    /// </summary>
    public class DashboardController : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private TextMeshProUGUI userNameText;
        [SerializeField] private TextMeshProUGUI progressText;

        [SerializeField] private GameObject continueButton;
        [SerializeField] private GameObject dashboardPanel;

        private int currentProgress = 0;

        // Starts listening for successful logins when this screen is active
        private void OnEnable()
        {
            AuthManager.OnLoginSuccess += SetupDashboard;
        }

        // Stops listening when this screen is inactive
        private void OnDisable()
        {
            AuthManager.OnLoginSuccess -= SetupDashboard;
        }

        // Updates the text to show the user's name and current progress
        private void SetupDashboard(string name, string progress)
        {
            dashboardPanel.SetActive(true);

            userNameText.text = $"Welcome, {name}";

            if (!int.TryParse(progress, out currentProgress))
            {
                currentProgress = 0;
            }
            currentProgress = Mathf.Clamp(currentProgress, 0, 100);
            progressText.text = $"Progress: {currentProgress}%";

            continueButton.SetActive(currentProgress > 0);
        }

        // Loads the AR training experience
        public void OnStartTraining()
        {
            AudioManager.Instance.Play(SoundType.UIClick);
            SceneManager.LoadScene("TrainingScene");
        }

        // Loads the AR training experience for returning users
        public void OnContinueTraining()
        {
            Debug.Log("Continue Training Clicked");
            AudioManager.Instance.Play(SoundType.UIClick);
            SceneManager.LoadScene("TrainingScene");
        }

        // Wipes the user's progress and updates the server
        public void OnResetProgress()
        {
            currentProgress = 0;
            AudioManager.Instance.Play(SoundType.UIClick);
            if (AuthManager.Instance != null)
            {
                AuthManager.Instance.UpdateTrainingData("0");
            }

            progressText.text = "Progress: 0%";
            continueButton.SetActive(false);
        }

        // Logs the user out and hides the dashboard
        public void OnLogout()
        {
            AudioManager.Instance.Play(SoundType.UIClick);
            if (AuthManager.Instance != null)
            {
                AuthManager.Instance.SignOut();
            }

            dashboardPanel.SetActive(false);
        }
    }
}