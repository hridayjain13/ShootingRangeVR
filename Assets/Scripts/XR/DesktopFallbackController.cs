using UnityEngine;
using UnityEngine.XR;

namespace IVRAR.Group02.XR
{
    /// <summary>
    /// Desktop emulation and testing mode for developing and evaluating without a physical VR headset.
    /// Provides first-person mouse look, WASD locomotion, and simulated controller interactions.
    /// </summary>
    public class DesktopFallbackController : MonoBehaviour
    {
        [Header("Camera & Transforms")]
        [SerializeField] private Camera _targetCamera;
        [SerializeField] private Transform _playerOrigin;

        [Header("Locomotion & Look Tuning")]
        [SerializeField] private float _moveSpeed = 3.5f;
        [SerializeField] private float _mouseSensitivity = 2.5f;
        [SerializeField] private bool _invertY = false;

        [Header("System Links")]
        [SerializeField] private SimulatedResponseMechanism _responseMechanism;
        [SerializeField] private XRRayInteractorPointer _rayPointer;

        [Header("Mode Configuration")]
        [SerializeField] private bool _autoEnableIfNoHmd = true;
        [SerializeField] private KeyCode _toggleModeKey = KeyCode.F1;
        [SerializeField] private KeyCode _altToggleKey = KeyCode.Tab;

        public bool IsDesktopModeActive { get; private set; }

        private float _yaw;
        private float _pitch;
        private bool _cursorLocked = true;

        private void Awake()
        {
            if (_targetCamera == null) _targetCamera = Camera.main;
            if (_playerOrigin == null) _playerOrigin = transform;
            if (_responseMechanism == null) _responseMechanism = FindObjectOfType<SimulatedResponseMechanism>();
            if (_rayPointer == null) _rayPointer = FindObjectOfType<XRRayInteractorPointer>();
        }

        private void Start()
        {
            if (_autoEnableIfNoHmd)
            {
                // If XR is not active or no physical HMD is connected, engage desktop testing
                bool isHmdActive = XRSettings.isDeviceActive;
                SetDesktopMode(!isHmdActive);
            }

            if (_targetCamera != null)
            {
                _yaw = _targetCamera.transform.eulerAngles.y;
                _pitch = _targetCamera.transform.eulerAngles.x;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(_toggleModeKey) || Input.GetKeyDown(_altToggleKey))
            {
                SetDesktopMode(!IsDesktopModeActive);
            }

            if (!IsDesktopModeActive) return;

            HandleCursorLock();
            HandleMouseLook();
            HandleLocomotion();
            HandleSimulatedInteractions();
        }

        public void SetDesktopMode(bool active)
        {
            IsDesktopModeActive = active;
            _cursorLocked = active;
            ApplyCursorLock(_cursorLocked);

            Debug.Log("[DesktopFallbackController] Desktop Mode: " + (IsDesktopModeActive ? "ENABLED" : "DISABLED"));
        }

        private void HandleCursorLock()
        {
            if (Input.GetKeyDown(KeyCode.LeftAlt) || Input.GetKeyDown(KeyCode.RightAlt))
            {
                _cursorLocked = !_cursorLocked;
                ApplyCursorLock(_cursorLocked);
            }
        }

        private void ApplyCursorLock(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private void HandleMouseLook()
        {
            if (!_cursorLocked || _targetCamera == null) return;

            float mouseX = Input.GetAxis("Mouse X") * _mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * _mouseSensitivity * (_invertY ? 1f : -1f);

            _yaw += mouseX;
            _pitch = Mathf.Clamp(_pitch + mouseY, -80f, 80f);

            _targetCamera.transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }

        private void HandleLocomotion()
        {
            if (_playerOrigin == null || _targetCamera == null) return;

            float horizontal = Input.GetAxis("Horizontal"); // A/D
            float vertical = Input.GetAxis("Vertical");     // W/S

            if (Mathf.Abs(horizontal) > 0.05f || Mathf.Abs(vertical) > 0.05f)
            {
                Vector3 forward = _targetCamera.transform.forward;
                forward.y = 0f;
                forward.Normalize();

                Vector3 right = _targetCamera.transform.right;
                right.y = 0f;
                right.Normalize();

                Vector3 moveDir = (forward * vertical + right * horizontal).normalized;
                _playerOrigin.position += moveDir * (_moveSpeed * Time.unscaledDeltaTime);
            }
        }

        private void HandleSimulatedInteractions()
        {
            // Left click: trigger simulated controller click / shoot response
            if (Input.GetMouseButtonDown(0))
            {
                if (_responseMechanism != null)
                {
                    _responseMechanism.ProcessResponse(null);
                }
                else if (_rayPointer != null)
                {
                    _rayPointer.TriggerClick();
                }
            }

            // Spacebar: immediate target engagement trigger
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (_responseMechanism != null)
                {
                    _responseMechanism.ProcessResponse(null);
                }
            }
        }

        private void OnGUI()
        {
            if (!IsDesktopModeActive) return;

            GUI.color = new Color(0f, 0f, 0f, 0.7f);
            GUI.Box(new Rect(Screen.width - 290, 10, 280, 100), GUIContent.none);

            GUI.color = Color.cyan;
            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };

            string instructions =
                "<b>DESKTOP TEST MODE ACTIVE (F1/Tab to toggle)</b>\n" +
                "- Mouse: 360° Look | Alt: Toggle Cursor\n" +
                "- WASD: Walk / Move Trainee Position\n" +
                "- Left Click / Space: Shoot / Interact\n" +
                "- C / R: Recenter | Esc: Pause Simulation";

            GUI.Label(new Rect(Screen.width - 280, 15, 260, 90), instructions, style);
        }

        private void OnDisable()
        {
            ApplyCursorLock(false);
        }
    }
}
