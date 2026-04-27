using UnityEngine;
using QCDC.Core;

namespace QCDC.States
{
    /// <summary>
    /// Handles the final screen of the training, locking interactions and saving the user's progress.
    /// </summary>
    public class CompletedState : ITrainingState
    {
        // Prepares the final screen when the user finishes the simulation
        public void Enter(ARTrainingManager ctx)
        {
            Debug.Log("<color=green>Training Completed State Entered</color>");

            ctx.InteractionController.ClearTarget();
            ctx.PartUI.Hide();
            ctx.ShowInstruction(false);

            ctx.ShowCompletionUI(true);

            ctx.SaveTrainingData();
        }

        // Waits for the user to make a choice on the final screen
        public void Update(ARTrainingManager ctx)
        {
        }

        // Hides the final screen when leaving this phase
        public void Exit(ARTrainingManager ctx)
        {
            ctx.ShowCompletionUI(false);
        }
    }
}