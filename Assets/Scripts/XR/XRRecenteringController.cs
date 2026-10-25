using UnityEngine;
using UnityEngine.XR;

namespace IVRAR.Group02.XR
{
    /// <summary>
    /// Handles participant recentering. Realigns headset orientation and position
    /// to match the target shooting range line-of-sight.
    /// </summary>
    public class XRRecenteringController : MonoBehaviour
    {
        [Header("Target Alignment")]
        [SerializeField] private Vector3 _targetCenter = new Vector3(0f, 1.6f, 0f);
        [SerializeField] private Vector3 _targetForward = Vector3.forward;

        [Header("Origin Reference")]
        [SerializeField] private XROriginController _xrOrigin;

        [Header("Controller Links")]
        [SerializeField] private XRControllerInputHandler _leftController;
        [SerializeField] private XRControllerInputHandler _rightController;

        [Header("Keyboard Fallback")]
        [SerializeField] private KeyCode _recenterKey = KeyCode.C;
        [SerializeField] private KeyCode _altRecenterKey = KeyCode.R;

        private void Awake()
        {
            if (_xrOrigin == null) _xrOrigin = FindObjectOfType<XROriginController>();
        }

        private void Start()
        {
            if (_leftController != null) _leftController.OnThumbstickClicked += Recenter;
            if (_rightController != null) _rightController.OnThumbstickClicked += Recenter;
        }

        private void Update()
        {
            if (Input.GetKeyDown(_recenterKey) || Input.GetKeyDown(_altRecenterKey))
            {
                Recenter();
            }
        }

        public void Recenter()
        {
            Debug.Log("[XRRecenteringController] Recentering participant view...");

            // 1. Hardware OpenXR recenter
            InputTracking.Recenter();

            // 2. XR Origin alignment
            if (_xrOrigin != null)
            {
                _xrOrigin.Recenter(_targetCenter, _targetForward);
            }

            // 3. Controller haptic confirmation
            if (_rightController != null) _rightController.SendHapticImpulse(0.4f, 0.05f);
            if (_leftController != null) _leftController.SendHapticImpulse(0.4f, 0.05f);
        }

        private void OnDestroy()
        {
            if (_leftController != null) _leftController.OnThumbstickClicked -= Recenter;
            if (_rightController != null) _rightController.OnThumbstickClicked -= Recenter;
        }
    }
}
