using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace IVRAR.Group02.XR
{
    /// <summary>
    /// Coordinates the XR Origin, Camera Offset, Head tracking pose,
    /// and spatial recentering offsets for the VR participant.
    /// </summary>
    public class XROriginController : MonoBehaviour
    {
        [Header("Hierarchy Transforms")]
        [SerializeField] private Transform _cameraOffset;
        [SerializeField] private Camera _headCamera;
        [SerializeField] private Transform _leftControllerAnchor;
        [SerializeField] private Transform _rightControllerAnchor;

        [Header("Ergonomics & Eye Height")]
        [SerializeField] private float _defaultEyeHeightMeters = 1.6f;
        [SerializeField] private bool _useDeviceHeight = true;

        private readonly List<InputDevice> _hmdDevices = new List<InputDevice>();
        private Vector3 _recenteredPositionOffset = Vector3.zero;
        private float _recenteredYawOffset = 0f;

        public Camera HeadCamera => _headCamera;
        public Transform LeftControllerAnchor => _leftControllerAnchor;
        public Transform RightControllerAnchor => _rightControllerAnchor;

        private void Awake()
        {
            if (_headCamera == null) _headCamera = GetComponentInChildren<Camera>();
            if (_cameraOffset == null && _headCamera != null) _cameraOffset = _headCamera.transform.parent;

            if (_cameraOffset != null && !_useDeviceHeight)
            {
                _cameraOffset.localPosition = new Vector3(0f, _defaultEyeHeightMeters, 0f);
            }
        }

        private void Update()
        {
            TrackHeadPose();
        }

        private void TrackHeadPose()
        {
            _hmdDevices.Clear();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted, _hmdDevices);

            if (_hmdDevices.Count > 0 && _hmdDevices[0].isValid && _headCamera != null)
            {
                if (_hmdDevices[0].TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 rawPos))
                {
                    _headCamera.transform.localPosition = rawPos;
                }

                if (_hmdDevices[0].TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rawRot))
                {
                    _headCamera.transform.localRotation = rawRot;
                }
            }
        }

        /// <summary>
        /// Recenter participant head pose so current position maps to origin and forward faces +Z.
        /// </summary>
        public void Recenter(Vector3 targetCenter, Vector3 targetForward)
        {
            if (_headCamera == null) return;

            // 1. Hardware tracking recenter call
            InputTracking.Recenter();

            // 2. Software origin alignment
            float currentHeadYaw = _headCamera.transform.eulerAngles.y;
            float targetYaw = Quaternion.LookRotation(targetForward, Vector3.up).eulerAngles.y;
            float yawDiff = targetYaw - currentHeadYaw;

            transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y + yawDiff, 0f);

            Vector3 currentHeadWorldPos = _headCamera.transform.position;
            Vector3 posDiff = targetCenter - new Vector3(currentHeadWorldPos.x, targetCenter.y, currentHeadWorldPos.z);
            transform.position += posDiff;

            Debug.Log("[XROriginController] Participant view recentered to target orientation.");
        }
    }
}
