using UnityEngine;
using System;
using QCDC.States;
using QCDC.Mechanics;
using QCDC.UI;
using QCDC.Auth;

namespace QCDC.Core
{
    /// <summary>
    /// The main brain of the app that switches between different phases of the training.
    /// </summary>
    public class ARTrainingManager : MonoBehaviour
    {
        private ITrainingState currentState;
        public ITrainingState CurrentState => currentState;

        [Header("Core Sub-Systems")]
        public ARPlacementManager PlacementManager;
        public ARPlanePlacementStateController PlaneStateController;
        public ModelInteractionController InteractionController;
        public ModelPartSelectionManager PartSelectionManager;

        [Header("UI Managers")]
        public ModelPartUI PartUI;
        public ARPlacementStatusUI PlacementUI;
        public GameObject connectButton;
        public GameObject instructionText;
        public GameObject completionPanel;
        public ARChecklistUI ChecklistUI;

        public event Action<ITrainingState> OnStateChanged;

        // Starts the app by asking the user to place the model
        private void Start()
        {
            SetState(new PlacementState());
        }

        // Keeps running the logic for whatever state the user is currently in
        private void Update()
        {
            currentState?.Update(this);
        }

        // Safely closes the old state and starts up the new state
        public void SetState(ITrainingState newState)
        {
            currentState?.Exit(this);

            currentState = newState;
            currentState.Enter(this);

            if (connectButton != null)
            {
                bool shouldShowButton = (currentState is ExplorationState) || (currentState is PartFocusState);
                connectButton.SetActive(shouldShowButton);
            }

            OnStateChanged?.Invoke(currentState);
        }

        // Moves the app forward when the user places the 3D model
        public void OnPlacementDone()
        {
            if (currentState is PlacementState)
                SetState(new ExplorationState());
        }

        // Zooms in when the user taps a specific piece
        public void OnPartSelected()
        {
            if (currentState is ExplorationState)
                SetState(new PartFocusState());
        }

        // Zooms out when the user taps away from a piece
        public void OnClosePart()
        {
            if (currentState is PartFocusState)
                SetState(new ExplorationState());
        }

        // Triggers the end sequence
        public void OnTrainingCompleted()
        {
            SetState(new CompletedState());
        }

        // Triggers the connection animation
        public void StartConnectionSequence()
        {
            AudioManager.Instance?.Play(SoundType.UIClick);
            if (currentState is ExplorationState || currentState is PartFocusState)
                SetState(new ConnectionState());
        }

        // Turns text directions on or off
        public void ShowInstruction(bool show)
        {
            if (instructionText != null)
                instructionText.SetActive(show);
        }

        // Turns the success panel on or off
        public void ShowCompletionUI(bool show)
        {
            if (completionPanel != null)
                completionPanel.SetActive(show);
        }

        // Pings the server to save the user's score
        public void SaveTrainingData()
        {
            if (AuthManager.Instance == null)
            {
                Debug.LogWarning("AuthManager missing. Simulating save to backend...");
                return;
            }
            AuthManager.Instance.UpdateTrainingData("100");
        }

        // Resets the model and starts the exploration over
        public void RestartSimulation()
        {
            AudioManager.Instance?.Play(SoundType.UIClick);
            GameObject spawnedModel = PlacementManager.GetPlacedObject();
            if (spawnedModel != null) spawnedModel.transform.rotation = Quaternion.identity;
            SetState(new ExplorationState());
        }

        // Leaves the 3D view and returns to the main menu
        public void ReturnToMainMenu()
        {
            AudioManager.Instance?.Play(SoundType.UIClick);
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenuScene");
        }
    }
}