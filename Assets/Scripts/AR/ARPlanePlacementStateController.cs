using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace QCDC.Mechanics
{
    /// <summary>
    /// Checks the camera feed to find a valid floor and shows a target marker on it.
    /// </summary>
    public class ARPlanePlacementStateController : MonoBehaviour
    {
        [Header("AR References")]
        [SerializeField] private ARPlaneManager planeManager;
        [SerializeField] private ARRaycastManager raycastManager;
        [SerializeField] private Camera arCamera;

        [Header("Reticle")]
        [SerializeField] private Transform placementReticle;
        [SerializeField] private float reticleMoveSpeed = 15f;
        [SerializeField] private float reticleRotateSpeed = 20f;
        [SerializeField] private Vector2 screenCenterOffset;
        [SerializeField] private bool alignReticleFlatToFloor = true;
        [SerializeField] private float reticleUniformScale = 0.15f;
        [SerializeField] private Vector2 reticleScaleClampRange = new Vector2(0.05f, 0.6f);
        [SerializeField] private bool enableReticleDebugLogs = true;
        [SerializeField] private float debugLogIntervalSeconds = 0.5f;

        [Header("Plane Rules")]
        [SerializeField] private bool requireTrackedPlane = true;
        [SerializeField] private bool requireHorizontalUpPlane = true;
        [SerializeField] private bool forceHorizontalDetectionMode = true;

        public event Action<bool> OnPlaneDetectedStateChanged;
        public event Action<bool> OnPlacementReadyStateChanged;

        public bool HasDetectedHorizontalPlane { get; private set; }
        public bool IsPlacementPoseValid { get; private set; }
        public Pose CurrentPlacementPose { get; private set; }

        public bool IsPlacementReady => HasDetectedHorizontalPlane && IsPlacementPoseValid;
        public bool IsTrackingReady => ARSession.state == ARSessionState.SessionTracking;

        private static readonly List<ARRaycastHit> RaycastHits = new List<ARRaycastHit>(8);
        private bool previousPlaneDetectedState;
        private bool previousPlacementReadyState;
        private float nextReticleDebugLogTime;

        private bool isControllerActive = false;

        // Links default camera and tracking components when added to the scene
        private void Reset()
        {
            planeManager = GetComponent<ARPlaneManager>();
            raycastManager = GetComponent<ARRaycastManager>();
            arCamera = Camera.main;
        }

        // Ensures the camera is found right away
        private void Awake()
        {
            if (arCamera == null)
            {
                arCamera = Camera.main;
            }
        }

        // Starts checking the floor and moving the target marker
        private void OnEnable()
        {
            ApplyDetectionModeSettings();

            if (planeManager != null)
            {
                planeManager.planesChanged += OnPlanesChanged;
            }

            EvaluatePlaneDetectedState();
            EvaluatePlacementPose();
            UpdateReticleVisual(forceSnap: true);
        }

        // Stops checking the floor when disabled
        private void OnDisable()
        {
            if (planeManager != null)
            {
                planeManager.planesChanged -= OnPlanesChanged;
            }
        }

        // Turns on the controller logic
        public void EnableController()
        {
            isControllerActive = true;
        }

        // Turns off the controller logic and hides the target marker
        public void DisableController()
        {
            isControllerActive = false;

            HasDetectedHorizontalPlane = false;

            if (placementReticle != null && placementReticle.gameObject.activeSelf)
            {
                placementReticle.gameObject.SetActive(false);
            }

            if (IsPlacementPoseValid)
            {
                IsPlacementPoseValid = false;
                NotifyPlacementReadyIfChanged();
            }
        }

        // Updates the target marker position every frame if the controller is active
        private void Update()
        {
            if (!isControllerActive || !IsTrackingReady)
            {
                HasDetectedHorizontalPlane = false;

                if (placementReticle != null && placementReticle.gameObject.activeSelf)
                {
                    placementReticle.gameObject.SetActive(false);
                }

                if (IsPlacementPoseValid)
                {
                    IsPlacementPoseValid = false;
                    NotifyPlacementReadyIfChanged();
                }

                return;
            }

            EvaluatePlaneDetectedState();
            EvaluatePlacementPose();
            UpdateReticleVisual(forceSnap: false);
        }

        // Returns the final position where the object should be placed
        public bool TryGetPlacementPose(out Pose pose)
        {
            pose = CurrentPlacementPose;
            return IsPlacementReady;
        }

        // Reacts when the system finds new floors
        private void OnPlanesChanged(ARPlanesChangedEventArgs _)
        {
            ApplyDetectionModeSettings();
            EvaluatePlaneDetectedState();
        }

        // Tells the AR system to strictly look for horizontal floors
        private void ApplyDetectionModeSettings()
        {
            if (!forceHorizontalDetectionMode || planeManager == null) return;
            planeManager.requestedDetectionMode = PlaneDetectionMode.Horizontal;
        }

        // Checks the list of floors to see if any are flat and valid
        private void EvaluatePlaneDetectedState()
        {
            bool detected = false;

            if (planeManager != null)
            {
                foreach (ARPlane plane in planeManager.trackables)
                {
                    if (plane == null) continue;
                    if (requireTrackedPlane && plane.trackingState != TrackingState.Tracking) continue;
                    if (requireHorizontalUpPlane && plane.alignment != PlaneAlignment.HorizontalUp) continue;

                    detected = true;
                    break;
                }
            }

            HasDetectedHorizontalPlane = detected;

            if (previousPlaneDetectedState != detected)
            {
                previousPlaneDetectedState = detected;
                OnPlaneDetectedStateChanged?.Invoke(detected);
            }
        }

        // Points a laser straight down from the center of the screen to find the floor
        private void EvaluatePlacementPose()
        {
            IsPlacementPoseValid = false;

            if (!HasDetectedHorizontalPlane || raycastManager == null || arCamera == null)
            {
                NotifyPlacementReadyIfChanged();
                return;
            }

            Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f) + screenCenterOffset;
            if (!raycastManager.Raycast(center, RaycastHits, TrackableType.PlaneWithinPolygon))
            {
                NotifyPlacementReadyIfChanged();
                return;
            }

            for (int i = 0; i < RaycastHits.Count; i++)
            {
                ARRaycastHit hit = RaycastHits[i];
                ARPlane hitPlane = planeManager != null ? planeManager.GetPlane(hit.trackableId) : null;

                if (hitPlane == null) continue;
                if (requireTrackedPlane && hitPlane.trackingState != TrackingState.Tracking) continue;
                if (requireHorizontalUpPlane && hitPlane.alignment != PlaneAlignment.HorizontalUp) continue;

                CurrentPlacementPose = hit.pose;
                IsPlacementPoseValid = true;

                if (enableReticleDebugLogs && arCamera != null && Time.unscaledTime >= nextReticleDebugLogTime)
                {
                    float distance = Vector3.Distance(arCamera.transform.position, hit.pose.position);
                    Debug.Log($"[AR Reticle] Raycast hit floor. Distance: {distance:F2}m Position: {hit.pose.position}");
                    nextReticleDebugLogTime = Time.unscaledTime + debugLogIntervalSeconds;
                }

                break;
            }

            NotifyPlacementReadyIfChanged();
        }

        // Sends an alert if the placement readiness changes
        private void NotifyPlacementReadyIfChanged()
        {
            bool ready = IsPlacementReady;
            if (previousPlacementReadyState == ready) return;

            previousPlacementReadyState = ready;
            OnPlacementReadyStateChanged?.Invoke(ready);
        }

        // Moves and scales the target marker on the floor
        private void UpdateReticleVisual(bool forceSnap)
        {
            if (placementReticle == null) return;

            bool showReticle = IsPlacementReady;
            if (placementReticle.gameObject.activeSelf != showReticle)
            {
                placementReticle.gameObject.SetActive(showReticle);
            }

            if (!showReticle) return;

            Pose targetPose = CurrentPlacementPose;
            if (alignReticleFlatToFloor)
            {
                Vector3 euler = targetPose.rotation.eulerAngles;
                targetPose.rotation = Quaternion.Euler(0f, euler.y, 0f);
            }

            if (forceSnap)
            {
                placementReticle.SetPositionAndRotation(targetPose.position, targetPose.rotation);
            }
            else
            {
                float moveT = 1f - Mathf.Exp(-reticleMoveSpeed * Time.deltaTime);
                float rotateT = 1f - Mathf.Exp(-reticleRotateSpeed * Time.deltaTime);

                placementReticle.position = Vector3.Lerp(placementReticle.position, targetPose.position, moveT);
                //placementReticle.rotation = Quaternion.Slerp(placementReticle.rotation, targetPose.rotation, rotateT);
            }

            float clampedScale = Mathf.Clamp(reticleUniformScale, reticleScaleClampRange.x, reticleScaleClampRange.y);
            placementReticle.localScale = Vector3.one * clampedScale;
        }
    }
}