using UnityEngine;

namespace QCDC.Mechanics
{
    /// <summary>
    /// Changes the color of a model part when the user taps on it.
    /// </summary>
    public class ModelPartHighlighter : MonoBehaviour
    {
        private Material originalMaterial;
        [SerializeField] private Material highlightMaterial;

        private Renderer partRenderer;

        // Saves the original color of the part when it loads
        private void Awake()
        {
            partRenderer = GetComponent<Renderer>();
            if (partRenderer != null)
                originalMaterial = partRenderer.material;
        }

        // Swaps the color to a highlighted material
        public void Highlight()
        {
            if (partRenderer != null && highlightMaterial != null)
                partRenderer.material = highlightMaterial;
        }

        // Reverts the color back to normal
        public void ResetHighlight()
        {
            if (partRenderer != null && originalMaterial != null)
                partRenderer.material = originalMaterial;
        }
    }
}