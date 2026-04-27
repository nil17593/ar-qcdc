using UnityEngine;
using TMPro;
using QCDC.Core;

namespace QCDC.UI
{
    /// <summary>
    /// Controls the on-screen information panel that displays details about a selected 3D model part.
    /// </summary>
    public class ModelPartUI : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;

        // Ensures the information panel is hidden when the app first loads
        private void Awake()
        {
            Hide();
        }

        // Opens the panel and updates the text with the name and description of the tapped piece
        public void Show(ModelPart part)
        {
            if (part == null)
            {
                Hide();
                return;
            }

            panel.SetActive(true);
            titleText.text = part.GetPartName();
            descriptionText.text = part.GetDescription();
        }

        // Closes the information panel
        public void Hide()
        {
            panel.SetActive(false);
        }
    }
}