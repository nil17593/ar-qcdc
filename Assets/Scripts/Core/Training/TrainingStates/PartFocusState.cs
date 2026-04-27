using UnityEngine;
using QCDC.Core;
using QCDC.Mechanics;

namespace QCDC.States
{
    /// <summary>
    /// Zooms in on a specific part of the model and displays its details.
    /// </summary>
    public class PartFocusState : ITrainingState
    {
        private ModelPart currentPart;
        private ModelPartHighlighter highlighter;

        // Highlights the tapped part and brings up the text description panel
        public void Enter(ARTrainingManager context)
        {
            currentPart = context.PartSelectionManager.GetCurrentPart();

            if (currentPart != null)
            {
                highlighter = currentPart.GetComponent<ModelPartHighlighter>();
                if (highlighter != null) highlighter.Highlight();

                GameObject spawnedModel = context.PlacementManager.GetPlacedObject();
                if (spawnedModel != null)
                    context.InteractionController.SetTarget(spawnedModel.transform);

                context.PartUI.Show(currentPart);
            }
        }

        // Watches for a tap outside the part to close the information panel
        public void Update(ARTrainingManager context)
        {
            context.InteractionController.HandleInteraction();

            bool inputDetected = Input.GetMouseButtonDown(0) ||
                                (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);

            if (inputDetected)
            {
                ModelPart tappedPart = context.PartSelectionManager.TrySelectPart();

                if (tappedPart != null && tappedPart == currentPart) return;

                context.SetState(new ExplorationState());
            }
        }

        // Removes the highlight and hides the text panel before returning to exploration
        public void Exit(ARTrainingManager context)
        {
            if (highlighter != null) highlighter.ResetHighlight();
            context.InteractionController.ClearTarget();
            context.PartUI.Hide();
        }
    }
}