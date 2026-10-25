using UnityEngine;
using IVRAR.Group02.TargetManagement;
using IVRAR.Group02.Telemetry;

namespace IVRAR.Group02.XR
{
    /// <summary>
    /// Controller-based simulated response mechanism.
    /// Captures trainee shoot/don't-shoot decisions from controller triggers,
    /// primary buttons (A/X), and desktop clicks, delivering haptic feedback upon firing.
    /// </summary>
    public class SimulatedResponseMechanism : MonoBehaviour
    {
        [Header("Controller Links")]
        [SerializeField] private XRControllerInputHandler _rightController;
        [SerializeField] private XRControllerInputHandler _leftController;
        [SerializeField] private XRRayInteractorPointer _rayPointer;

        [Header("Haptic Feedback Settings")]
        [SerializeField, Range(0.1f, 1.0f)] private float _hapticAmplitude = 0.7f;
        [SerializeField, Range(0.02f, 0.5f)] private float _hapticDuration = 0.08f;

        [Header("Audio Feedback")]
        [SerializeField] private AudioSource _responseAudioSource;
        [SerializeField] private AudioClip _simulatedShotClip;

        private void Start()
        {
            if (_rightController != null)
            {
                _rightController.OnTriggerPressed += HandleRightTrigger;
                _rightController.OnPrimaryButtonPressed += HandleRightPrimary;
            }

            if (_leftController != null)
            {
                _leftController.OnTriggerPressed += HandleLeftTrigger;
                _leftController.OnPrimaryButtonPressed += HandleLeftPrimary;
            }
        }

        private void HandleRightTrigger()
        {
            ProcessResponse(_rightController);
        }

        private void HandleRightPrimary()
        {
            ProcessResponse(_rightController);
        }

        private void HandleLeftTrigger()
        {
            ProcessResponse(_leftController);
        }

        private void HandleLeftPrimary()
        {
            ProcessResponse(_leftController);
        }

        public void ProcessResponse(XRControllerInputHandler activeController = null)
        {
            // 1. Deliver tactile feedback to the triggering controller
            if (activeController != null)
            {
                activeController.SendHapticImpulse(_hapticAmplitude, _hapticDuration);
            }

            // 2. Play simulated shot audio if assigned
            if (_responseAudioSource != null && _simulatedShotClip != null)
            {
                _responseAudioSource.PlayOneShot(_simulatedShotClip);
            }

            // 3. Check if ray pointer is aiming directly at a target or UI button
            if (_rayPointer != null)
            {
                bool clicked = _rayPointer.TriggerClick();
                if (clicked) return;
            }

            // 4. Fallback: Check if there is an active tactical target in the scene
            var activeTarget = FindObjectOfType<TacticalTarget>();
            if (activeTarget != null)
            {
                activeTarget.OnTargetHit();
                return;
            }

            var activeMetadata = FindObjectOfType<TargetTrialMetadata>();
            if (activeMetadata != null)
            {
                activeMetadata.RegisterDecision(true);
            }
        }

        private void OnDestroy()
        {
            if (_rightController != null)
            {
                _rightController.OnTriggerPressed -= HandleRightTrigger;
                _rightController.OnPrimaryButtonPressed -= HandleRightPrimary;
            }

            if (_leftController != null)
            {
                _leftController.OnTriggerPressed -= HandleLeftTrigger;
                _leftController.OnPrimaryButtonPressed -= HandleLeftPrimary;
            }
        }
    }
}
