using System.Collections;
using TMPro;
using UnityEngine;

namespace QCDC.UI
{
    /// <summary>
    /// Controls the text and animations shown on screen while the user scans the floor.
    /// </summary>
    public class ARPlacementStatusUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private CanvasGroup statusCanvasGroup;
        [SerializeField] private GameObject optionalScanningIndicator;

        [Header("Messages")]
        [SerializeField] private string scanningMessage = "Scanning floor";
        [SerializeField] private string placementReadyMessage = "Tap to place object";

        private Coroutine scanningDotsRoutine;
        private bool isCurrentlyReady = false;
        private bool isVisible = false;

        // Ensures the UI is hidden as soon as the app starts
        private void Awake()
        {
            HideUI();
        }

        // Turns on the scanning messages
        public void ShowUI()
        {
            isVisible = true;
            isCurrentlyReady = false;

            if (statusCanvasGroup != null) statusCanvasGroup.alpha = 1f;
            if (optionalScanningIndicator != null) optionalScanningIndicator.SetActive(true);

            StartScanningDots();
        }

        // completely hides the scanning messages
        public void HideUI()
        {
            isVisible = false;

            if (statusCanvasGroup != null) statusCanvasGroup.alpha = 0f;
            if (optionalScanningIndicator != null) optionalScanningIndicator.SetActive(false);
            if (statusText != null) statusText.text = string.Empty;

            StopScanningDots();
        }

        // Swaps the text from scanning to ready when the camera finds the floor
        public void UpdateStatus(bool isReady)
        {
            if (!isVisible) return;

            if (isCurrentlyReady != isReady)
            {
                isCurrentlyReady = isReady;

                if (isCurrentlyReady)
                {
                    StopScanningDots();
                    if (statusText != null) statusText.text = placementReadyMessage;
                    if (optionalScanningIndicator != null) optionalScanningIndicator.SetActive(false);
                }
                else
                {
                    StartScanningDots();
                    if (optionalScanningIndicator != null) optionalScanningIndicator.SetActive(true);
                }
            }
        }

        // Starts the animated periods next to the word scanning
        private void StartScanningDots()
        {
            StopScanningDots();
            scanningDotsRoutine = StartCoroutine(AnimateScanningDots());
        }

        // Stops the animated periods
        private void StopScanningDots()
        {
            if (scanningDotsRoutine != null)
            {
                StopCoroutine(scanningDotsRoutine);
                scanningDotsRoutine = null;
            }
        }

        // Loops the text animation to make it look like the app is thinking
        private IEnumerator AnimateScanningDots()
        {
            int dotCount = 0;
            while (!isCurrentlyReady && isVisible)
            {
                dotCount = (dotCount % 3) + 1;
                string dots = new string('.', dotCount);

                if (statusText != null)
                    statusText.text = $"{scanningMessage}{dots}\nMove your phone slowly";

                yield return new WaitForSeconds(0.35f);
            }
        }
    }
}