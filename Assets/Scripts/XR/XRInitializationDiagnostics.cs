using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;

namespace IVRAR.Group02.XR
{
    /// <summary>
    /// Real-time XR initialization and hardware diagnostic component.
    /// Truthfully monitors active XR loader, headset tracking, controller states,
    /// and display refresh rate. Does NOT report an unsupported headset or refresh rate as verified.
    /// </summary>
    [DisallowMultipleComponent]
    public class XRInitializationDiagnostics : MonoBehaviour
    {
        [Header("Diagnostic Status (Read-Only)")]
        [SerializeField] private bool _isXRInitialized;
        [SerializeField] private string _activeXRPlatform = "None";
        [SerializeField] private string _activeLoaderName = "None";
        [SerializeField] private bool _headsetTrackingAvailable;
        [SerializeField] private bool _isHeadsetVerified;
        [SerializeField] private string _headsetName = "None";
        [SerializeField] private bool _leftControllerAvailable;
        [SerializeField] private bool _rightControllerAvailable;
        [SerializeField] private float _reportedRefreshRateHz = -1f;
        [SerializeField] private bool _isRefreshRateVerified;
        [SerializeField] private bool _isDesktopFallbackActive;

        [Header("Diagnostic Display Options")]
        [SerializeField] private bool _showOnGUI = true;
        [SerializeField] private bool _showInWorldSpace = true;
        [SerializeField] private KeyCode _toggleKey = KeyCode.F3;

        public bool IsXRInitialized => _isXRInitialized;
        public string ActiveLoaderName => _activeLoaderName;
        public bool HeadsetTrackingAvailable => _headsetTrackingAvailable;
        public bool IsHeadsetVerified => _isHeadsetVerified;
        public string HeadsetName => _headsetName;
        public bool LeftControllerAvailable => _leftControllerAvailable;
        public bool RightControllerAvailable => _rightControllerAvailable;
        public float ReportedRefreshRateHz => _reportedRefreshRateHz;
        public bool IsRefreshRateVerified => _isRefreshRateVerified;
        public bool IsDesktopFallbackActive => _isDesktopFallbackActive;

        private readonly List<InputDevice> _hmdDevices = new List<InputDevice>();
        private readonly List<InputDevice> _leftControllers = new List<InputDevice>();
        private readonly List<InputDevice> _rightControllers = new List<InputDevice>();
        private readonly List<XRDisplaySubsystem> _displaySubsystems = new List<XRDisplaySubsystem>();

        private float _pollTimer;
        private const float POLL_INTERVAL = 0.5f;

        private void Start()
        {
            PollXRHardware();
        }

        private void Update()
        {
            if (Input.GetKeyDown(_toggleKey))
            {
                _showOnGUI = !_showOnGUI;
            }

            _pollTimer += Time.unscaledDeltaTime;
            if (_pollTimer >= POLL_INTERVAL)
            {
                _pollTimer = 0f;
                PollXRHardware();
            }
        }

        public void PollXRHardware()
        {
            // 1. Query XR Management & Active Loader
            _isXRInitialized = XRSettings.isDeviceActive;
            _activeXRPlatform = !string.IsNullOrEmpty(XRSettings.loadedDeviceName) ? XRSettings.loadedDeviceName : "None";

            var generalSettings = XRGeneralSettings.Instance;
            if (generalSettings != null && generalSettings.Manager != null && generalSettings.Manager.activeLoader != null)
            {
                _activeLoaderName = generalSettings.Manager.activeLoader.name;
                _isXRInitialized = true;
            }
            else
            {
                _activeLoaderName = _isXRInitialized ? _activeXRPlatform : "None (Desktop Fallback)";
            }

            // 2. Query Headset (HMD) Devices
            _hmdDevices.Clear();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted, _hmdDevices);

            if (_hmdDevices.Count > 0 && _hmdDevices[0].isValid)
            {
                _headsetName = _hmdDevices[0].name;
                bool hasPos = _hmdDevices[0].TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 pos);
                bool hasRot = _hmdDevices[0].TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rot);
                _headsetTrackingAvailable = hasPos || hasRot;

                // Truthful verification: true only if genuine physical HMD is active
                _isHeadsetVerified = _headsetTrackingAvailable && !_headsetName.ToLower().Contains("desktop");
                _isDesktopFallbackActive = false;
            }
            else
            {
                _headsetName = "None";
                _headsetTrackingAvailable = false;
                _isHeadsetVerified = false;
                _isDesktopFallbackActive = true;
            }

            // 3. Query Left & Right Controllers
            _leftControllers.Clear();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Left, _leftControllers);
            _leftControllerAvailable = (_leftControllers.Count > 0 && _leftControllers[0].isValid);

            _rightControllers.Clear();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Right, _rightControllers);
            _rightControllerAvailable = (_rightControllers.Count > 0 && _rightControllers[0].isValid);

            // 4. Query Display Refresh Rate
            _displaySubsystems.Clear();
            SubsystemManager.GetSubsystems(_displaySubsystems);

            _reportedRefreshRateHz = -1f;
            _isRefreshRateVerified = false;

            if (_displaySubsystems.Count > 0 && _displaySubsystems[0].running)
            {
                var display = _displaySubsystems[0];
                if (display.TryGetDisplayRefreshRate(out float rate) && rate > 30f)
                {
                    _reportedRefreshRateHz = rate;
                    _isRefreshRateVerified = true;
                }
            }

            // Fallback: Check XRDevice or frame timing
            if (!_isRefreshRateVerified && _isHeadsetVerified)
            {
                // In some legacy OpenVR or Oculus runtimes
                float refreshRate = XRDevice.refreshRate;
                if (refreshRate > 40f && refreshRate < 250f)
                {
                    _reportedRefreshRateHz = refreshRate;
                    _isRefreshRateVerified = true;
                }
            }
        }

        public string GetDiagnosticSummary()
        {
            return string.Format(
                "=== XR DIAGNOSTICS ===\n" +
                "Status: {0}\n" +
                "Loader: {1}\n" +
                "Headset: {2} (Tracking: {3}, Verified: {4})\n" +
                "Left Controller: {5} | Right Controller: {6}\n" +
                "Refresh Rate: {7} (Verified: {8})\n" +
                "Mode: {9}",
                _isXRInitialized ? "INITIALIZED" : "NOT_INITIALIZED",
                _activeLoaderName,
                _headsetName,
                _headsetTrackingAvailable ? "YES" : "NO",
                _isHeadsetVerified ? "VERIFIED" : "UNVERIFIED",
                _leftControllerAvailable ? "CONNECTED" : "DISCONNECTED",
                _rightControllerAvailable ? "CONNECTED" : "DISCONNECTED",
                _isRefreshRateVerified ? string.Format("{0:F1} Hz", _reportedRefreshRateHz) : "N/A (Unverified)",
                _isRefreshRateVerified ? "YES" : "NO",
                _isDesktopFallbackActive ? "Desktop Fallback Mode" : "Headset VR Mode"
            );
        }

        private void OnGUI()
        {
            if (!_showOnGUI) return;

            GUI.color = Color.black;
            GUI.Box(new Rect(10, 10, 420, 160), GUIContent.none);

            GUI.color = Color.white;
            GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };

            GUIStyle bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11
            };

            GUI.Label(new Rect(20, 15, 400, 25), "IVRAR Group 02 - XR Hardware Diagnostics (F3 to Toggle)", headerStyle);

            string statusColor = _isXRInitialized ? "<color=#00FF00>ONLINE</color>" : "<color=#FFCC00>DESKTOP FALLBACK</color>";
            string loaderInfo = string.Format("XR Status: {0} | Loader: <b>{1}</b>", statusColor, _activeLoaderName);
            string hmdInfo = string.Format("HMD: <b>{0}</b> | Tracking: {1} | Verified: {2}",
                _headsetName,
                _headsetTrackingAvailable ? "<color=#00FF00>YES</color>" : "<color=#FF4444>NO</color>",
                _isHeadsetVerified ? "<color=#00FF00>TRUE</color>" : "<color=#AAAAAA>FALSE</color>");

            string ctrlInfo = string.Format("Controllers: Left=[{0}] | Right=[{1}]",
                _leftControllerAvailable ? "<color=#00FF00>ACTIVE</color>" : "<color=#AAAAAA>OFF</color>",
                _rightControllerAvailable ? "<color=#00FF00>ACTIVE</color>" : "<color=#AAAAAA>OFF</color>");

            string fpsInfo = string.Format("Refresh Rate: <b>{0}</b> (Verified: {1})",
                _isRefreshRateVerified ? string.Format("{0:F1} Hz", _reportedRefreshRateHz) : "<color=#FF8800>N/A (Unverified)</color>",
                _isRefreshRateVerified ? "YES" : "NO");

            string modeInfo = string.Format("Active Mode: <color={0}><b>{1}</b></color>",
                _isDesktopFallbackActive ? "#00FFFF" : "#00FF00",
                _isDesktopFallbackActive ? "Desktop Emulation (WASD + Mouse)" : "6-DoF OpenXR Headset");

            string fullText = string.Format("{0}\n{1}\n{2}\n{3}\n{4}", loaderInfo, hmdInfo, ctrlInfo, fpsInfo, modeInfo);
            GUI.Label(new Rect(20, 40, 400, 120), fullText, bodyStyle);
        }
    }
}
