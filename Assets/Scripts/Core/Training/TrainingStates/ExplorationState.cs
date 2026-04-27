using UnityEngine;
using QCDC.Core;
using QCDC.Mechanics;

namespace QCDC.States
{
    /// <summary>
    /// Allows the user to freely view and interact with the placed 3D model.
    /// </summary>
    public class ExplorationState : ITrainingState
    {
        // Sets up the environment for the user to inspect the entire model
        public void Enter(ARTrainingManager context)
        {
            Debug.Log("<color=green>Entered Exploration State</color>");

            if (context.PartUI != null) context.PartUI.Hide();

            GameObject spawnedModel = context.PlacementManager.GetPlacedObject();
            if (spawnedModel != null)
            {
                context.InteractionController.SetTarget(spawnedModel.transform);
            }
        }

        // Listens for user screen taps to select specific parts
        public void Update(ARTrainingManager context)
        {
            context.InteractionController.HandleInteraction();

            ModelPart tappedPart = context.PartSelectionManager.TrySelectPart();

            if (tappedPart != null)
            {
                context.OnPartSelected();
            }
        }

        // Clears the selected model from memory before moving to the next phase
        public void Exit(ARTrainingManager context)
        {
            context.InteractionController.ClearTarget();
        }
    }
}