using UnityEngine;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using Firebase;
using System.Threading.Tasks;

namespace QCDC.Auth
{
    /// <summary>
    /// Communicates with the online database to handle user accounts and save progress.
    /// </summary>
    public class AuthManager : MonoBehaviour
    {
        public static AuthManager Instance { get; private set; }

        private FirebaseAuth auth;
        private FirebaseFirestore db;
        private FirebaseUser user;
        private bool isInitialized;
        private const float FirebaseInitTimeoutSeconds = 20f;

        public static event Action<string, string> OnLoginSuccess;
        public static event Action<string> OnAuthError;
        public static event Action<string> OnResetEmailSent;
        public static event Action OnSessionChecked;
        public static event Action OnLoggedOut;

        // Keeps this manager alive across all screens
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else Destroy(gameObject);
        }

        // Starts connecting to the server right away
        private void Start()
        {
            StartCoroutine(Initialize());
        }

        // Sets up the database and checks if a user is already logged in
        private IEnumerator Initialize()
        {
            FirebaseManager firebaseManager = FirebaseManager.EnsureInstance();

            float elapsed = 0f;
            while (!firebaseManager.IsFirebaseReady && !firebaseManager.HasInitializationFailed)
            {
                elapsed += Time.unscaledDeltaTime;
                if (elapsed >= FirebaseInitTimeoutSeconds)
                {
                    OnAuthError?.Invoke("Initialization timed out. Check connection and retry.");
                    OnSessionChecked?.Invoke();
                    yield break;
                }

                yield return null;
            }

            if (firebaseManager.HasInitializationFailed)
            {
                string error = string.IsNullOrWhiteSpace(firebaseManager.InitializationError)
                    ? "Could not initialize Firebase."
                    : firebaseManager.InitializationError;

                OnAuthError?.Invoke(error);
                OnSessionChecked?.Invoke();
                yield break;
            }

            auth = FirebaseAuth.DefaultInstance;
            db = FirebaseFirestore.DefaultInstance;
            isInitialized = true;

            if (auth.CurrentUser != null)
            {
                user = auth.CurrentUser;
                FetchUserTrainingData();
            }
            else
            {
                OnSessionChecked?.Invoke();
            }
        }

        // Creates a brand new user account
        public void RegisterUser(string name, string email, string password)
        {
            if (!CanRunAuthOperation()) return;

            auth.CreateUserWithEmailAndPasswordAsync(email, password)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    HandleFirebaseError(task.Exception);
                    return;
                }

                user = task.Result.User;

                SaveUserDataAsync(name).ContinueWithOnMainThread(saveTask =>
                {
                    if (saveTask.IsCanceled || saveTask.IsFaulted)
                    {
                        HandleFirebaseError(saveTask.Exception);
                        return;
                    }

                    UpdateTrainingDataAsync("0").ContinueWithOnMainThread(progressTask =>
                    {
                        if (progressTask.IsCanceled || progressTask.IsFaulted)
                        {
                            HandleFirebaseError(progressTask.Exception);
                            return;
                        }

                        OnLoginSuccess?.Invoke(name, "0");
                    });
                });
            });
        }

        // Saves the new user's profile details to the database
        private Task SaveUserDataAsync(string name)
        {
            if (user == null) return Task.CompletedTask;

            DocumentReference docRef = db.Collection("Users").Document(user.UserId);

            Dictionary<string, object> data = new Dictionary<string, object>
            {
                { "Name", name },
                { "Email", user.Email },
                { "CreatedAt", FieldValue.ServerTimestamp }
            };

            return docRef.SetAsync(data, SetOptions.MergeAll);
        }

        // Logs an existing user back in
        public void LoginUser(string email, string password)
        {
            if (!CanRunAuthOperation()) return;

            auth.SignInWithEmailAndPasswordAsync(email, password)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    HandleFirebaseError(task.Exception);
                    return;
                }

                user = task.Result.User;
                FetchUserTrainingData();
            });
        }

        // Logs the user out
        public void SignOut()
        {
            if (auth == null) return;

            auth.SignOut();
            user = null;
            OnLoggedOut?.Invoke();
        }

        // Sends an email for the user to pick a new password
        public void ResetPassword(string email)
        {
            if (!CanRunAuthOperation()) return;

            auth.SendPasswordResetEmailAsync(email)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogWarning("Reset failed: " + task.Exception);
                    OnAuthError?.Invoke("Something went wrong. Try again.");
                    return;
                }

                OnResetEmailSent?.Invoke("If this email exists, a reset link has been sent.");
            });
        }

        // Triggers the background process to update progress
        public void UpdateTrainingData(string progressValue)
        {
            UpdateTrainingDataAsync(progressValue).ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogWarning("Failed to update training progress.");
                }
            });
        }

        // Saves the user's simulation score securely to the server
        public Task UpdateTrainingDataAsync(string progressValue)
        {
            if (user == null || !isInitialized) return Task.CompletedTask;

            string encryptedProgress = SecurityManager.Encrypt(progressValue);

            DocumentReference docRef = db.Collection("Users").Document(user.UserId);

            Dictionary<string, object> data = new Dictionary<string, object>
            {
                { "Progress", encryptedProgress },
                { "LastUpdated", FieldValue.ServerTimestamp }
            };

            return docRef.SetAsync(data, SetOptions.MergeAll);
        }

        // Downloads the user's progress history from the server
        public void FetchUserTrainingData()
        {
            if (user == null) return;

            db.Collection("Users").Document(user.UserId).GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    OnAuthError?.Invoke("Failed to fetch data");
                    OnSessionChecked?.Invoke();
                    return;
                }

                DocumentSnapshot snapshot = task.Result;

                string name = "User";
                string progress = "0";

                if (snapshot.Exists)
                {
                    if (snapshot.ContainsField("Name"))
                        name = snapshot.GetValue<string>("Name");

                    if (snapshot.ContainsField("Progress"))
                    {
                        string encrypted = snapshot.GetValue<string>("Progress");
                        try
                        {
                            progress = SecurityManager.Decrypt(encrypted);
                        }
                        catch (Exception)
                        {
                            Debug.LogWarning("Progress could not be decrypted. Falling back to 0.");
                            progress = "0";
                        }
                    }
                }

                OnLoginSuccess?.Invoke(name, progress);
            });
        }

        // Translates technical database errors into plain English messages
        private void HandleFirebaseError(Exception exception)
        {
            if (exception == null)
            {
                OnAuthError?.Invoke("Unknown error occurred");
                return;
            }

            FirebaseException firebaseEx = exception.GetBaseException() as FirebaseException;

            if (firebaseEx != null)
            {
                AuthError errorCode = (AuthError)firebaseEx.ErrorCode;

                switch (errorCode)
                {
                    case AuthError.EmailAlreadyInUse:
                        OnAuthError?.Invoke("Email already registered");
                        break;
                    case AuthError.InvalidEmail:
                        OnAuthError?.Invoke("Invalid email format");
                        break;
                    case AuthError.WeakPassword:
                        OnAuthError?.Invoke("Password too weak (min 6 characters)");
                        break;
                    case AuthError.WrongPassword:
                        OnAuthError?.Invoke("Incorrect password");
                        break;
                    case AuthError.UserNotFound:
                        OnAuthError?.Invoke("No account found. Please register");
                        break;
                    default:
                        OnAuthError?.Invoke("Authentication failed");
                        break;
                }
            }
            else
            {
                OnAuthError?.Invoke("Unexpected error occurred");
            }
        }

        // Checks if the database is fully booted up before trying to use it
        private bool CanRunAuthOperation()
        {
            if (!isInitialized || auth == null || db == null)
            {
                OnAuthError?.Invoke("Authentication service is still initializing.");
                return false;
            }

            return true;
        }

        // Manually re-triggers the session check when returning from other scenes
        public void RefreshSession()
        {
            if (user != null)
            {
                FetchUserTrainingData();
            }
            else
            {
                OnSessionChecked?.Invoke();
            }
        }
    }
}