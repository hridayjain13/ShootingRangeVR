using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace IVRAR.Group02.XR
{
    public enum ControllerHand
    {
        Left,
        Right
    }

    /// <summary>
    /// Reads and dispatches controller events (trigger, grip, primary/secondary buttons, thumbstick)
    /// across left and right 6-DoF OpenXR / SteamVR controllers.
    /// Provides accessible symmetrical mappings and tactile haptic pulse triggers.
    /// </summary>
    public class XRControllerInputHandler : MonoBehaviour
    {
        [Header("Controller Configuration")]
        [SerializeField] private ControllerHand _hand = ControllerHand.Right;
        [SerializeField] private Transform _controllerAnchor;

        [Header("Action Thresholds")]
        [SerializeField, Range(0.1f, 0.9f)] private float _triggerPressThreshold = 0.5f;
        [SerializeField, Range(0.1f, 0.9f)] private float _gripPressThreshold = 0.5f;

        // Events
        public event Action OnTriggerPressed;
        public event Action OnTriggerReleased;
        public event Action OnPrimaryButtonPressed;     // A or X
        public event Action OnSecondaryButtonPressed;   // B or Y
        public event Action OnGripPressed;
        public event Action OnThumbstickClicked;
        public event Action<Vector2> OnThumbstickMoved;

        // State properties
        public float TriggerValue { get; private set; }
        public float GripValue { get; private set; }
        public Vector2 ThumbstickAxis { get; private set; }
        public bool IsPrimaryPressed { get; private set; }
        public bool IsSecondaryPressed { get; private set; }
        public bool IsTriggerPressed { get; private set; }
        public bool IsGripPressed { get; private set; }
        public bool IsConnected => _device.isValid;

        private InputDevice _device;
        private readonly List<InputDevice> _deviceBuffer = new List<InputDevice>();

        private bool _prevTriggerState;
        private bool _prevGripState;
        private bool _prevPrimaryState;
        private bool _prevSecondaryState;
        private bool _prevThumbstickClickState;

        private void Update()
        {
            EnsureDeviceValid();
            PollInputs();
            TrackControllerTransform();
        }

        private void EnsureDeviceValid()
        {
            if (_device.isValid) return;

            _deviceBuffer.Clear();
            var charac = (_hand == ControllerHand.Left)
                ? (InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Left)
                : (InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Right);

            InputDevices.GetDevicesWithCharacteristics(charac, _deviceBuffer);
            if (_deviceBuffer.Count > 0 && _deviceBuffer[0].isValid)
            {
                _device = _deviceBuffer[0];
                Debug.Log(string.Format("[XRControllerInputHandler] Connected {0} controller: {1}", _hand, _device.name));
            }
        }

        private void PollInputs()
        {
            if (!_device.isValid) return;

            // 1. Trigger
            if (_device.TryGetFeatureValue(CommonUsages.trigger, out float trigVal))
            {
                TriggerValue = trigVal;
                IsTriggerPressed = trigVal >= _triggerPressThreshold;

                if (IsTriggerPressed && !_prevTriggerState) OnTriggerPressed?.Invoke();
                else if (!IsTriggerPressed && _prevTriggerState) OnTriggerReleased?.Invoke();
                _prevTriggerState = IsTriggerPressed;
            }

            // 2. Grip
            if (_device.TryGetFeatureValue(CommonUsages.grip, out float gripVal))
            {
                GripValue = gripVal;
                IsGripPressed = gripVal >= _gripPressThreshold;

                if (IsGripPressed && !_prevGripState) OnGripPressed?.Invoke();
                _prevGripState = IsGripPressed;
            }

            // 3. Primary Button (A / X)
            if (_device.TryGetFeatureValue(CommonUsages.primaryButton, out bool primVal))
            {
                IsPrimaryPressed = primVal;
                if (primVal && !_prevPrimaryState) OnPrimaryButtonPressed?.Invoke();
                _prevPrimaryState = primVal;
            }

            // 4. Secondary Button (B / Y)
            if (_device.TryGetFeatureValue(CommonUsages.secondaryButton, out bool secVal))
            {
                IsSecondaryPressed = secVal;
                if (secVal && !_prevSecondaryState) OnSecondaryButtonPressed?.Invoke();
                _prevSecondaryState = secVal;
            }

            // 5. Thumbstick Click
            if (_device.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out bool thumbClick))
            {
                if (thumbClick && !_prevThumbstickClickState) OnThumbstickClicked?.Invoke();
                _prevThumbstickClickState = thumbClick;
            }

            // 6. Thumbstick 2D Axis
            if (_device.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 axis))
            {
                ThumbstickAxis = axis;
                if (axis.sqrMagnitude > 0.01f)
                {
                    OnThumbstickMoved?.Invoke(axis);
                }
            }
        }

        private void TrackControllerTransform()
        {
            if (!_device.isValid || _controllerAnchor == null) return;

            if (_device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 pos))
            {
                _controllerAnchor.localPosition = pos;
            }

            if (_device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rot))
            {
                _controllerAnchor.localRotation = rot;
            }
        }

        public void SendHapticImpulse(float amplitude = 0.5f, float duration = 0.1f)
        {
            if (!_device.isValid) return;

            uint channel = 0;
            _device.SendHapticImpulse(channel, Mathf.Clamp01(amplitude), Mathf.Max(0.01f, duration));
        }
    }
}
