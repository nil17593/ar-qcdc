using UnityEngine;
using System.Collections;
using QCDC.Core;
using QCDC.Mechanics;

namespace QCDC.States
{
    /// <summary>
    /// Controls the step-by-step mechanical connection process of the 3D model.
    /// </summary>
    public class ConnectionState : ITrainingState
    {
        private float autoInsertTimer = 0f;
        private float autoInsertDuration = 1.5f;
        private float handleProgress = 0f;

        private bool inserted = false;
        private bool handlesLocked = false;
        private bool xrayEnabled = false;
        private bool isDisconnecting = false;

        private QCDCAnimator animator;

        public void Enter(ARTrainingManager ctx)
        {
            GameObject spawnedModel = ctx.PlacementManager.GetPlacedObject();
            if (spawnedModel != null)
            {
                spawnedModel.transform.rotation = Quaternion.Euler(0, -90f, 0);
                animator = spawnedModel.GetComponent<QCDCAnimator>();
            }

            ctx.InteractionController.ClearTarget();
            ctx.PartUI.Hide();

            if (ctx.ChecklistUI != null)
            {
                ctx.ChecklistUI.Show();
                ctx.ChecklistUI.ResetChecklist();
                ctx.ChecklistUI.MarkStepComplete(1);
            }

            if (animator != null) animator.PlaySlideSound();
        }

        public void Update(ARTrainingManager ctx)
        {
            // If we are disconnecting, lock out the user input completely
            if (animator == null || isDisconnecting) return;

            if (!inserted)
            {
                autoInsertTimer += Time.deltaTime;
                float t = Mathf.Clamp01(autoInsertTimer / autoInsertDuration);
                animator.SetInsertion(t);

                if (t >= 1f)
                {
                    inserted = true;
                    animator.StopAudio(); // <--- FIX: Stop slide sound immediately
                    ctx.ShowInstruction(true);

                    if (ctx.ChecklistUI != null) ctx.ChecklistUI.MarkStepComplete(2);
                }
                return;
            }

            if (!xrayEnabled)
            {
                animator.EnableXRay();
                xrayEnabled = true;
                return;
            }

            if (handleProgress < 1f)
            {
                float dragAmount = 0f;

#if UNITY_EDITOR
                if (Input.GetMouseButton(0)) dragAmount = Input.GetAxis("Mouse Y");
#else
                if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Moved)
                    dragAmount = Input.GetTouch(0).deltaPosition.y * 0.02f;
#endif

                if (dragAmount != 0)
                {
                    ctx.ShowInstruction(false);

                    handleProgress -= dragAmount * Time.deltaTime * 5f;
                    handleProgress = Mathf.Clamp01(handleProgress);
                    animator.SetHandleRotation(handleProgress);
                }

                // Lock the handle and trigger the rest of the sequence
                if (handleProgress >= 1f && !handlesLocked)
                {
                    handlesLocked = true;
                    animator.PlayLockSound();

                    if (ctx.ChecklistUI != null) ctx.ChecklistUI.MarkStepComplete(3);

                    // <--- FIX: Trigger Flow via Coroutine so we can time the sounds perfectly
                    ctx.StartCoroutine(DisconnectionSequence(ctx, animator));
                }
            }
        }

        // Handles the fluid flow and the automated reversal
        private IEnumerator DisconnectionSequence(ARTrainingManager ctx, QCDCAnimator anim)
        {
            isDisconnecting = true; // Locks the Update loop so user can't drag anymore

            // 1. Wait a split second to let the Lock "Clank" sound finish echoing
            yield return new WaitForSeconds(0.6f);

            // 2. Start the fluid/wind flow
            anim.StartFlow();
            if (ctx.ChecklistUI != null) ctx.ChecklistUI.MarkStepComplete(4);

            // 3. Let it run for 3 seconds
            yield return new WaitForSeconds(3f);

            // 4. Stop the flow (This now automatically stops the audio too)
            anim.StopFlow();
            yield return new WaitForSeconds(0.5f);

            // 5. Reverse Handle Rotation
            float t = 1f;
            while (t > 0f)
            {
                t -= Time.deltaTime * 1.5f;
                t = Mathf.Clamp01(t);
                anim.SetHandleRotation(t);
                yield return null;
            }

            yield return new WaitForSeconds(0.3f);

            // 6. Reverse Insertion
            t = 1f;
            while (t > 0f)
            {
                t -= Time.deltaTime * 1.5f;
                t = Mathf.Clamp01(t);
                anim.SetInsertion(t);
                yield return null;
            }

            yield return new WaitForSeconds(0.3f);

            anim.DisableXRay();
            if (ctx.ChecklistUI != null) ctx.ChecklistUI.Hide();

            ctx.SetState(new CompletedState());
        }

        public void Exit(ARTrainingManager ctx) { }
    }
}