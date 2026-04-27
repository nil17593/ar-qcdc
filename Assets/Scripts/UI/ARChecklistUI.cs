using UnityEngine;
using TMPro;

namespace QCDC.UI
{
    /// <summary>
    /// Controls the floating holographic checklist that guides the user through the AR connection steps.
    /// </summary>
    public class ARChecklistUI : MonoBehaviour
    {
        [SerializeField] private GameObject panel;

        [Header("Checklist Items")]
        [SerializeField] private TextMeshProUGUI step1Text;
        [SerializeField] private TextMeshProUGUI step2Text;
        [SerializeField] private TextMeshProUGUI step3Text;
        [SerializeField] private TextMeshProUGUI step4Text;

        // Hides the checklist when the app starts
        private void Awake()
        {
            Hide();
        }

        // Displays the checklist canvas
        public void Show()
        {
            if (panel != null) panel.SetActive(true);
        }

        // Hides the checklist canvas
        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
        }

        // Resets all text items to their incomplete state
        public void ResetChecklist()
        {
            SetStepStatus(step1Text, "Awaiting Alignment", false);
            SetStepStatus(step2Text, "Coupling Inserted", false);
            SetStepStatus(step3Text, "Handles Locked", false);
            SetStepStatus(step4Text, "Fluid Flowing", false);
        }

        // Updates the checklist based on the current step index
        public void MarkStepComplete(int stepIndex)
        {
            if (stepIndex >= 1) SetStepStatus(step1Text, "Awaiting Alignment", true);
            if (stepIndex >= 2) SetStepStatus(step2Text, "Coupling Inserted", true);
            if (stepIndex >= 3) SetStepStatus(step3Text, "Handles Locked", true);
            if (stepIndex >= 4) SetStepStatus(step4Text, "Fluid Flowing", true);
        }

        // Formats the text to look like a checked or unchecked box
        private void SetStepStatus(TextMeshProUGUI textElement, string taskName, bool isComplete)
        {
            if (textElement == null) return;

            if (isComplete)
            {
                textElement.text = $"<color=green>[X] {taskName}</color>";
            }
            else
            {
                textElement.text = $"<color=white>[ ] {taskName}</color>";
            }
        }
    }
}