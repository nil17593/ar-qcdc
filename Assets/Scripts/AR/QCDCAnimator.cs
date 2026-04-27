using UnityEngine;
using System.Collections.Generic;

namespace QCDC.Mechanics
{
    /// <summary>
    /// Moves the individual parts of the 3D model and manages localized 3D sound effects.
    /// </summary>
    public class QCDCAnimator : MonoBehaviour
    {
        [Header("Assemblies")]
        [SerializeField] private Transform femaleAssembly;
        [SerializeField] private Transform handle;

        [Header("Mechanics")]
        [SerializeField] private Transform femaleFlap;
        [SerializeField] private Transform maleFlap;
        [SerializeField] private Transform maleSpring;

        [Header("VFX & SFX")]
        [SerializeField] private ParticleSystem fluidFlowParticles;
        [SerializeField] private List<MeshRenderer> localizedFadeRenderers;
        [SerializeField] private Material xRayMaterial;

        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip slideSound;
        [SerializeField] private AudioClip lockSound;
        [SerializeField] private AudioClip flowSound;

        private Dictionary<MeshRenderer, Material> originalMaterials = new();

        [Header("Settings")]
        [SerializeField] private Vector3 connectedPosition;
        [SerializeField] private Vector3 handleLockedRotation;
        [SerializeField] private Vector3 flapOpenOffset;

        private Vector3 disconnectedPosition;
        private Quaternion initialHandleRotation;
        private Vector3 initialFemaleFlapPos;
        private Vector3 initialMaleFlapPos;
        private Vector3 initialSpringScale;

        // Saves starting positions and converts the attached audio source to 3D spatial audio
        private void Awake()
        {
            disconnectedPosition = femaleAssembly.localPosition;
            initialHandleRotation = handle.localRotation;

            initialFemaleFlapPos = femaleFlap.localPosition;
            initialMaleFlapPos = maleFlap.localPosition;
            initialSpringScale = maleSpring.localScale;

            foreach (var r in localizedFadeRenderers)
                originalMaterials[r] = r.material;

            fluidFlowParticles.Stop();

            if (audioSource != null)
            {
                // Forces the attached audio to physically exist in 3D space
                audioSource.spatialBlend = 1f;
                audioSource.rolloffMode = AudioRolloffMode.Linear;
                audioSource.minDistance = 0.2f;
                audioSource.maxDistance = 4.0f;
            }
        }

        // Slides the main body together
        public void SetInsertion(float t)
        {
            femaleAssembly.localPosition = Vector3.Lerp(disconnectedPosition, connectedPosition, t);
        }

        // Plays the metal sliding sound
        public void PlaySlideSound()
        {
            if (audioSource != null && slideSound != null)
            {
                audioSource.Stop();
                audioSource.clip = slideSound;
                audioSource.loop = false;
                audioSource.Play();
            }
        }

        // Plays the heavy locking clank sound
        public void PlayLockSound()
        {
            if (audioSource != null && lockSound != null)
            {
                audioSource.Stop();
                audioSource.clip = lockSound;
                audioSource.loop = false;
                audioSource.Play();
            }
        }

        // Turns the metal transparent
        public void EnableXRay()
        {
            foreach (var r in localizedFadeRenderers)
                r.material = xRayMaterial;
        }

        // Turns the transparent metal back to solid
        public void DisableXRay()
        {
            foreach (var kvp in originalMaterials)
                kvp.Key.material = kvp.Value;
        }

        // Rotates the handle and physically pushes the internal springs
        public void SetHandleRotation(float t)
        {
            Quaternion targetRot = initialHandleRotation * Quaternion.Euler(handleLockedRotation);
            handle.localRotation = Quaternion.Lerp(initialHandleRotation, targetRot, t);

            Vector3 targetFemale = initialFemaleFlapPos + flapOpenOffset;
            Vector3 targetMale = initialMaleFlapPos + flapOpenOffset;
            Vector3 springCompressed = new Vector3(initialSpringScale.x, initialSpringScale.y, initialSpringScale.z * 0.4f);

            femaleFlap.localPosition = Vector3.Lerp(initialFemaleFlapPos, targetFemale, t);
            maleFlap.localPosition = Vector3.Lerp(initialMaleFlapPos, targetMale, t);
            maleSpring.localScale = Vector3.Lerp(initialSpringScale, springCompressed, t);
        }

        // Shoots the wind effect out of the pipe and loops the flow sound

        public void StartFlow()
        {
            fluidFlowParticles.Play();
            if (audioSource != null && flowSound != null)
            {
                audioSource.Stop();
                audioSource.clip = flowSound;
                audioSource.loop = true;
                audioSource.Play();
            }
        }

        // Shuts off the wind effect and stops the looping sound
        public void StopFlow()
        {
            fluidFlowParticles.Stop();
            StopAudio();
        }

        // Stops any currently playing sound to prevent overlaps
        public void StopAudio()
        {
            if (audioSource != null) audioSource.Stop();
        }
    }
}