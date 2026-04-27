using UnityEngine;
using QCDC.Core;
using QCDC.Mechanics;
using QCDC.UI;

namespace QCDC.States
{
    /// <summary>
    /// Manages the initial phase where the user scans the floor to place the 3D model.
    /// </summary>
    public class PlacementState : ITrainingState
    {
        // Activates the floor tracking tools and scanning screens
        public void Enter(ARTrainingManager context)
        {
            Debug.Log("<color=green>Entered Placement State</color>");

            if (context.PartUI != null) context.PartUI.Hide();

            context.PlacementManager.EnablePlacement();

            if (context.PlaneStateController != null)
                context.PlaneStateController.EnableController();

            if (context.PlacementUI != null)
                context.PlacementUI.ShowUI();
        }

        // Constantly checks if the floor is ready and waits for the user to tap to place the model
        public void Update(ARTrainingManager context)
        {
            if (context.PlaneStateController != null && context.PlacementUI != null)
            {
                bool isReady = context.PlaneStateController.IsPlacementReady;
                context.PlacementUI.UpdateStatus(isReady);
            }

            bool placed = context.PlacementManager.TryPlaceObject();
            if (placed)
            {
                context.SetState(new ExplorationState());
            }
        }

        // Turns off the floor scanning tools and UI once the model is safely placed
        public void Exit(ARTrainingManager context)
        {
            context.PlacementManager.DisablePlacement();

            if (context.PlaneStateController != null)
                context.PlaneStateController.DisableController();

            if (context.PlacementUI != null)
                context.PlacementUI.HideUI();
        }
    }
}