using UnityEngine;
using Firebase;
using Firebase.Extensions;
using System;

namespace QCDC.Auth
{
    /// <summary>
    /// Starts up the connection to the Google Firebase servers.
    /// </summary>
    public class FirebaseManager : MonoBehaviour
    {
        public static FirebaseManager Instance { get; private set; }

        public DependencyStatus dependencyStatus { get; private set; }
        public FirebaseApp app { get; private set; }

        public bool IsFirebaseReady { get; private set; } = false;
        public bool HasInitializationFailed { get; private set; }
        public string InitializationError { get; private set; } = string.Empty;

        public static event Action<bool, string> OnInitializationCompleted;

        private bool initializationStarted;

        // Keeps the connection object alive
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        // Starts the boot sequence
        private void Start()
        {
            InitializeFirebase();
        }

        // Creates a new connection manager if one doesn't exist yet
        public static FirebaseManager EnsureInstance()
        {
            if (Instance != null)
            {
                return Instance;
            }

            FirebaseManager existingManager = FindObjectOfType<FirebaseManager>();
            if (existingManager != null)
            {
                return existingManager;
            }

            GameObject managerObject = new GameObject("FirebaseManager");
            return managerObject.AddComponent<FirebaseManager>();
        }

        // Checks the device for the right files and boots the database
        private void InitializeFirebase()
        {
            if (initializationStarted || IsFirebaseReady)
            {
                return;
            }

            initializationStarted = true;

            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    HandleInitializationFailure("Firebase dependency check failed.");
                    return;
                }

                dependencyStatus = task.Result;

                if (dependencyStatus == DependencyStatus.Available)
                {
                    InitializeDefaultApp();
                }
                else
                {
                    HandleInitializationFailure($"Could not resolve Firebase dependencies: {dependencyStatus}");
                }
            });
        }

        // Successfully logs onto the database servers
        private void InitializeDefaultApp()
        {
            try
            {
                app = FirebaseApp.DefaultInstance;
                IsFirebaseReady = true;
                HasInitializationFailed = false;
                InitializationError = string.Empty;
                Debug.Log("Firebase Initialized Successfully.");
                OnInitializationCompleted?.Invoke(true, string.Empty);
            }
            catch (Exception exception)
            {
                HandleInitializationFailure($"Firebase init exception: {exception.Message}");
            }
        }

        // Logs a warning if the server connection fails
        private void HandleInitializationFailure(string error)
        {
            HasInitializationFailed = true;
            IsFirebaseReady = false;
            InitializationError = error;
            Debug.LogError(error);
            OnInitializationCompleted?.Invoke(false, error);
        }

        // Cleans up the connection when the app closes
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}