using UnityEngine;
using QCDC.Core;

namespace QCDC.Mechanics
{
    /// <summary>
    /// Detects when the user taps on a specific part of the 3D model.
    /// </summary>
    public class ModelPartSelectionManager : MonoBehaviour
    {
        private Camera mainCamera;
        private ModelPart currentSelected;

        // Finds the main camera to use for detecting taps
        private void Awake()
        {
            mainCamera = Camera.main;
        }

        // Checks exactly where the user tapped to see if it hit a model piece
        public ModelPart TrySelectPart()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    return null;
                }
            }

            Vector3 inputPosition = Vector3.zero;
            bool inputDetected = false;

#if UNITY_EDITOR
            if (Input.GetMouseButtonDown(0))
            {
                inputPosition = Input.mousePosition;
                inputDetected = true;
            }
#else
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                inputPosition = Input.GetTouch(0).position;
                inputDetected = true;
            }
#endif

            if (!inputDetected) return null;

            Ray ray = mainCamera.ScreenPointToRay(inputPosition);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                ModelPart part = hit.collider.GetComponent<ModelPart>();
                if (part != null)
                {
                    currentSelected = part;
                    return part;
                }
            }
            return null;
        }

        // Returns the piece that is currently tapped on
        public ModelPart GetCurrentPart()
        {
            return currentSelected;
        }
    }
}