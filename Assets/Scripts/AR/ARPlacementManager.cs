using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace QCDC.Mechanics
{
    /// <summary>
    /// Handles spawning the 3D model onto a flat surface after safely decrypting it from memory.
    /// Supports Android APK architecture.
    /// </summary>
    public class ARPlacementManager : MonoBehaviour
    {
        [Header("Placement Setup")]
        [SerializeField] private ARPlanePlacementStateController placementStateController;
        [SerializeField] private string prefabName = "QCDC_Prefab";

        private ARRaycastManager arRaycastManager;
        private ARPlaneManager arPlaneManager;

        private GameObject qcdcPrefab;
        private GameObject spawnedModel;
        private AssetBundle loadedBundle;

        private static List<ARRaycastHit> hits = new List<ARRaycastHit>();
        private byte key = 123;

        // Prepares AR tracking tools and starts the unpacking process
        private void Awake()
        {
            arRaycastManager = GetComponent<ARRaycastManager>();
            arPlaneManager = GetComponent<ARPlaneManager>();

            // Start the loading as a Coroutine to handle Android architecture
            StartCoroutine(LoadEncryptedBundleRoutine());
        }

        // Fetches the file properly depending on the platform, then unlocks it in RAM
        private IEnumerator LoadEncryptedBundleRoutine()
        {
            string path = Path.Combine(Application.streamingAssetsPath, "qcdc_encrypted.bundle");
            byte[] encryptedData = null;

#if UNITY_ANDROID && !UNITY_EDITOR
            // ON ANDROID: StreamingAssets are inside the APK, so we MUST use UnityWebRequest
            using (UnityWebRequest webRequest = UnityWebRequest.Get(path))
            {
                yield return webRequest.SendWebRequest();

                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("Error loading bundle on Android: " + webRequest.error);
                    yield break;
                }
                
                encryptedData = webRequest.downloadHandler.data;
            }
#else
            // ON PC/MAC/EDITOR: We can use standard file reading
            if (!File.Exists(path))
            {
                Debug.LogError("Encrypted bundle not found in StreamingAssets!");
                yield break;
            }

            encryptedData = File.ReadAllBytes(path);
#endif

            // Decrypt the file bytes
            if (encryptedData != null)
            {
                for (int i = 0; i < encryptedData.Length; i++)
                {
                    encryptedData[i] ^= key;
                }

                loadedBundle = AssetBundle.LoadFromMemory(encryptedData);

                if (loadedBundle != null)
                {
                    qcdcPrefab = loadedBundle.LoadAsset<GameObject>(prefabName);
                }
            }
        }

        // Turns on floor detection
        public void EnablePlacement()
        {
            arPlaneManager.enabled = true;
            foreach (var plane in arPlaneManager.trackables)
                plane.gameObject.SetActive(true);
        }

        // Turns off floor detection
        public void DisablePlacement()
        {
            foreach (var plane in arPlaneManager.trackables)
                plane.gameObject.SetActive(false);
            arPlaneManager.enabled = false;
        }

        // Checks for screen taps and places the 3D model on the floor if valid
        public bool TryPlaceObject()
        {
            // If the Coroutine hasn't finished loading the prefab yet, ignore taps
            if (qcdcPrefab == null) return false;

#if UNITY_EDITOR
            if (Input.GetMouseButtonDown(0))
            {
                if (spawnedModel == null)
                {
                    Vector3 spawnPos = Camera.main.transform.position + Camera.main.transform.forward * 2.0f;
                    spawnPos.y -= 0.1f;
                    spawnedModel = Instantiate(qcdcPrefab, spawnPos, Quaternion.identity);
                    LockPlacement();
                    return true;
                }
            }
            return false;
#else
            if (Input.touchCount == 0) return false;
            Touch touch = Input.GetTouch(0);
            if (touch.phase != TouchPhase.Began) return false;

            if (spawnedModel == null && placementStateController != null && placementStateController.TryGetPlacementPose(out Pose centerPose))
            {
                spawnedModel = Instantiate(qcdcPrefab, centerPose.position, centerPose.rotation);
                LockPlacement();
                return true;
            }

            if (arRaycastManager.Raycast(touch.position, hits, TrackableType.PlaneWithinPolygon))
            {
                Pose hitPose = hits[0].pose;
                if (spawnedModel == null)
                {
                    spawnedModel = Instantiate(qcdcPrefab, hitPose.position, hitPose.rotation);
                    LockPlacement();
                    return true; 
                }
                else
                {
                    spawnedModel.transform.position = hitPose.position;
                    Vector3 lookPos = Camera.main.transform.position;
                    lookPos.y = spawnedModel.transform.position.y;
                    spawnedModel.transform.LookAt(lookPos);
                }
            }
            return false;
#endif
        }

        // Stops looking for new floors once the object is placed
        private void LockPlacement()
        {
            foreach (var plane in arPlaneManager.trackables)
                plane.gameObject.SetActive(false);
            arPlaneManager.enabled = false;
        }

        // Returns the 3D model that was spawned
        public GameObject GetPlacedObject()
        {
            return spawnedModel;
        }

        // Clean up the RAM safely when the AR Scene is closed or reset
        private void OnDestroy()
        {
            if (loadedBundle != null)
            {
                loadedBundle.Unload(false);
            }
        }
    }
}