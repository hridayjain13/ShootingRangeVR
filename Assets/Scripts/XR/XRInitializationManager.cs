using System.Collections;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;

namespace IVRAR.Group02.XR
{
    /// <summary>
    /// OpenXR and XR Subsystem Manager.
    /// Manages the XR Loader lifecycle, initializes subsystems on launch,
    /// and configures appropriate fallback modes when no headset is present.
    /// </summary>
    public class XRInitializationManager : MonoBehaviour
    {
        [Header("XR Configuration")]
        [SerializeField] private bool _autoInitializeOnAwake = true;
        [SerializeField] private TrackingSpaceType _desiredTrackingSpace = TrackingSpaceType.RoomScale;

        [Header("Diagnostic Reference")]
        [SerializeField] private XRInitializationDiagnostics _diagnostics;

        public bool IsXRActive => XRSettings.isDeviceActive;

        private void Awake()
        {
            if (_diagnostics == null) _diagnostics = GetComponent<XRInitializationDiagnostics>();

            if (_autoInitializeOnAwake)
            {
                StartCoroutine(InitializeXRLifecycle());
            }
        }

        private IEnumerator InitializeXRLifecycle()
        {
            Debug.Log("[XRInitializationManager] Starting XR Subsystem Initialization...");

            var generalSettings = XRGeneralSettings.Instance;
            if (generalSettings != null && generalSettings.Manager != null)
            {
                if (generalSettings.Manager.activeLoader == null)
                {
                    Debug.Log("[XRInitializationManager] Initializing XR Loader synchronously...");
                    yield return generalSettings.Manager.InitializeLoader();
                }

                if (generalSettings.Manager.activeLoader != null)
                {
                    Debug.Log("[XRInitializationManager] Starting XR Subsystems with loader: " +
                              generalSettings.Manager.activeLoader.name);
                    generalSettings.Manager.StartSubsystems();
                }
                else
                {
                    Debug.LogWarning("[XRInitializationManager] No XR Loader initialized. Defaulting to Desktop Fallback mode.");
                }
            }

            // Configure tracking space
            SetTrackingSpace(_desiredTrackingSpace);

            yield return null;

            if (_diagnostics != null)
            {
                _diagnostics.PollXRHardware();
            }
        }

        public void SetTrackingSpace(TrackingSpaceType spaceType)
        {
            if (XRDevice.SetTrackingSpaceType(spaceType))
            {
                Debug.Log("[XRInitializationManager] Tracking space set to: " + spaceType);
            }
            else
            {
                Debug.Log("[XRInitializationManager] Tracking space " + spaceType + " not supported on active device. Fallback to Stationary.");
                XRDevice.SetTrackingSpaceType(TrackingSpaceType.Stationary);
            }
        }

        private void OnDestroy()
        {
            var generalSettings = XRGeneralSettings.Instance;
            if (generalSettings != null && generalSettings.Manager != null && generalSettings.Manager.isInitializationComplete)
            {
                generalSettings.Manager.StopSubsystems();
            }
        }
    }
}
