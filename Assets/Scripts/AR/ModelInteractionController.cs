using UnityEngine;

namespace QCDC.Mechanics
{
    /// <summary>
    /// Allows the user to rotate (horizontally and vertically) and resize the 3D model using touch or mouse controls.
    /// </summary>
    public class ModelInteractionController : MonoBehaviour
    {
        [SerializeField] private float rotationSpeed = 0.2f;

        [Header("Zoom / Scale Settings")]
        [SerializeField] private float scaleSpeed = 0.1f;
        [SerializeField] private float minScaleMultiplier = 0.5f;
        [SerializeField] private float maxScaleMultiplier = 3.0f;

        private Transform target;
        private Vector3 initialScale;
        private Camera mainCamera;

        // Locks onto a specific 3D model to control it
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;

            if (target != null)
            {
                initialScale = target.localScale;
            }
        }

        // Releases the 3D model so it can no longer be controlled
        public void ClearTarget()
        {
            target = null;
        }

        // Processes screen swipes and pinches to rotate and scale the model
        public void HandleInteraction()
        {
            if (target == null) return;

            // Ensure we always have a reference to the main camera for relative vertical rotation
            if (mainCamera == null) mainCamera = Camera.main;

#if UNITY_EDITOR
            if (Input.GetMouseButton(0))
            {
                float rotY = Input.GetAxis("Mouse X") * rotationSpeed * 20f;
                float rotX = Input.GetAxis("Mouse Y") * rotationSpeed * 20f;

                // Horizontal rotation (Yaw) around the global Up axis
                target.Rotate(Vector3.up, -rotY, Space.World);
                // Vertical rotation (Pitch) relative to the camera's point of view
                target.Rotate(mainCamera.transform.right, rotX, Space.World);
            }

            float scroll = Input.mouseScrollDelta.y;
            if (scroll != 0)
            {
                float scaleFactor = 1f + (scroll * scaleSpeed);
                ApplyScale(scaleFactor);
            }
#else
            if (Input.touchCount == 1)
            {
                Touch touch = Input.GetTouch(0);

                if (touch.phase == TouchPhase.Moved)
                {
                    float rotY = touch.deltaPosition.x * rotationSpeed;
                    float rotX = touch.deltaPosition.y * rotationSpeed;

                    // Horizontal rotation (Yaw) around the global Up axis
                    target.Rotate(Vector3.up, -rotY, Space.World);
                    // Vertical rotation (Pitch) relative to the camera's point of view
                    target.Rotate(mainCamera.transform.right, rotX, Space.World);
                }
            }

            if (Input.touchCount == 2)
            {
                Touch t0 = Input.GetTouch(0);
                Touch t1 = Input.GetTouch(1);

                float prevDist = (t0.position - t0.deltaPosition - (t1.position - t1.deltaPosition)).magnitude;
                float currDist = (t0.position - t1.position).magnitude;

                float delta = currDist - prevDist;

                float scaleFactor = 1f + (delta * scaleSpeed * 0.05f);
                ApplyScale(scaleFactor);
            }
#endif
        }

        // Enlarges or shrinks the model safely within limits
        private void ApplyScale(float factor)
        {
            Vector3 newScale = target.localScale * factor;

            float minX = initialScale.x * minScaleMultiplier;
            float maxX = initialScale.x * maxScaleMultiplier;

            float clampedX = Mathf.Clamp(newScale.x, minX, maxX);

            target.localScale = new Vector3(clampedX, clampedX, clampedX);
        }
    }
}