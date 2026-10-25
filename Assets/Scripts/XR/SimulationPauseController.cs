using System;
using UnityEngine;
using UnityEngine.UI;
using IVRAR.Group02.Core;

namespace IVRAR.Group02.XR
{
    /// <summary>
    /// Manages simulation pause, resume, participant recentering, and emergency exit controls.
    /// Accessible via VR secondary buttons (B/Y), in-VR pause modal, or keyboard (Escape, P, F12).
    /// </summary>
    public class SimulationPauseController : MonoBehaviour
    {
        [Header("Pause Modal UI")]
        [SerializeField] private GameObject _pausePanelRoot;
        [SerializeField] private Button _btnResume;
        [SerializeField] private Button _btnRecenter;
        [SerializeField] private Button _btnReturnToMenu;
        [SerializeField] private Button _btnImmediateExit;

        [Header("Controller Links")]
        [SerializeField] private XRControllerInputHandler _leftController;
        [SerializeField] private XRControllerInputHandler _rightController;

        [Header("System References")]
        [SerializeField] private SimulationManager _simulationManager;
        [SerializeField] private XRRecenteringController _recenteringController;

        public bool IsPaused { get; private set; }

        private void Awake()
        {
            if (_simulationManager == null) _simulationManager = FindObjectOfType<SimulationManager>();
            if (_recenteringController == null) _recenteringController = FindObjectOfType<XRRecenteringController>();

            if (_btnResume != null) _btnResume.onClick.AddListener(ResumeSimulation);
            if (_btnRecenter != null) _btnRecenter.onClick.AddListener(OnRecenterClicked);
            if (_btnReturnToMenu != null) _btnReturnToMenu.onClick.AddListener(ReturnToMainMenu);
            if (_btnImmediateExit != null) _btnImmediateExit.onClick.AddListener(ImmediateExit);

            if (_pausePanelRoot != null) _pausePanelRoot.SetActive(false);
        }

        private void Start()
        {
            if (_leftController != null) _leftController.OnSecondaryButtonPressed += TogglePause;
            if (_rightController != null) _rightController.OnSecondaryButtonPressed += TogglePause;
        }

        private void Update()
        {
            // Keyboard shortcuts
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
            {
                TogglePause();
            }

            // Immediate emergency exit shortcuts
            if (Input.GetKeyDown(KeyCode.F12) || (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Q)))
            {
                ImmediateExit();
            }
        }

        public void TogglePause()
        {
            if (IsPaused) ResumeSimulation();
            else PauseSimulation();
        }

        public void PauseSimulation()
        {
            IsPaused = true;
            Time.timeScale = 0f;

            if (_pausePanelRoot != null) _pausePanelRoot.SetActive(true);
            Debug.Log("[SimulationPauseController] Simulation PAUSED.");
        }

        public void ResumeSimulation()
        {
            IsPaused = false;
            Time.timeScale = 1f;

            if (_pausePanelRoot != null) _pausePanelRoot.SetActive(false);
            Debug.Log("[SimulationPauseController] Simulation RESUMED.");
        }

        private void OnRecenterClicked()
        {
            if (_recenteringController != null)
            {
                _recenteringController.Recenter();
            }
        }

        public void ReturnToMainMenu()
        {
            ResumeSimulation();
            if (_simulationManager != null)
            {
                _simulationManager.StopSimulation();
            }
        }

        public void ImmediateExit()
        {
            Debug.Log("[SimulationPauseController] IMMEDIATE EXIT triggered.");
            Time.timeScale = 1f;

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
            if (_leftController != null) _leftController.OnSecondaryButtonPressed -= TogglePause;
            if (_rightController != null) _rightController.OnSecondaryButtonPressed -= TogglePause;
        }
    }
}
