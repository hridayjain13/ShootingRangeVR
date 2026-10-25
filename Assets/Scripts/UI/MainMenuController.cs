using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using IVRAR.Group02.Core;

namespace IVRAR.Group02.UI
{
    /// <summary>
    /// Master UI controller for the IVRAR Group 02 VR training simulation startup menu.
    /// Manages navigation between Main Menu, Participant Setup, Research Configuration,
    /// Team Credits, Original Project Attribution, and Simulation Execution.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Menu Panels")]
        [SerializeField] private GameObject _mainMenuPanel;
        [SerializeField] private GameObject _participantSetupPanel;
        [SerializeField] private GameObject _researchConfigPanel;
        [SerializeField] private GameObject _creditsPanel;
        [SerializeField] private GameObject _attributionPanel;

        [Header("Main Menu Navigation Buttons")]
        [SerializeField] private Button _btnStartSimulation;
        [SerializeField] private Button _btnParticipantSetup;
        [SerializeField] private Button _btnResearchConfig;
        [SerializeField] private Button _btnCredits;
        [SerializeField] private Button _btnAttribution;
        [SerializeField] private Button _btnExit;

        [Header("Back Buttons")]
        [SerializeField] private Button _btnBackFromParticipant;
        [SerializeField] private Button _btnBackFromConfig;
        [SerializeField] private Button _btnBackFromCredits;
        [SerializeField] private Button _btnBackFromAttribution;

        [Header("Simulation Coordinator Reference")]
        [SerializeField] private SimulationManager _simulationManager;

        [Header("Title & Branding Labels")]
        [SerializeField] private Text _lblProjectTitle;
        [SerializeField] private Text _lblProjectSubtitle;

        public const string TITLE_TEXT = "IVRAR GROUP 02";
        public const string SUBTITLE_TEXT = "Immersive VR Sensory-Stress Defensive Security Training under Low Visibility";

        private void Awake()
        {
            if (_simulationManager == null) _simulationManager = FindObjectOfType<SimulationManager>();

            // Setup branding labels
            if (_lblProjectTitle != null) _lblProjectTitle.text = TITLE_TEXT;
            if (_lblProjectSubtitle != null) _lblProjectSubtitle.text = SUBTITLE_TEXT;

            // Wire up navigation listeners
            if (_btnStartSimulation != null) _btnStartSimulation.onClick.AddListener(OnStartSimulationClicked);
            if (_btnParticipantSetup != null) _btnParticipantSetup.onClick.AddListener(ShowParticipantSetup);
            if (_btnResearchConfig != null) _btnResearchConfig.onClick.AddListener(ShowResearchConfiguration);
            if (_btnCredits != null) _btnCredits.onClick.AddListener(ShowCredits);
            if (_btnAttribution != null) _btnAttribution.onClick.AddListener(ShowAttribution);
            if (_btnExit != null) _btnExit.onClick.AddListener(OnExitClicked);

            if (_btnBackFromParticipant != null) _btnBackFromParticipant.onClick.AddListener(ShowMainMenu);
            if (_btnBackFromConfig != null) _btnBackFromConfig.onClick.AddListener(ShowMainMenu);
            if (_btnBackFromCredits != null) _btnBackFromCredits.onClick.AddListener(ShowMainMenu);
            if (_btnBackFromAttribution != null) _btnBackFromAttribution.onClick.AddListener(ShowMainMenu);

            ShowMainMenu();
        }

        public void ShowMainMenu()
        {
            SetPanelState(main: true, participant: false, config: false, credits: false, attribution: false);
        }

        public void ShowParticipantSetup()
        {
            SetPanelState(main: false, participant: true, config: false, credits: false, attribution: false);
        }

        public void ShowResearchConfiguration()
        {
            SetPanelState(main: false, participant: false, config: true, credits: false, attribution: false);
        }

        public void ShowCredits()
        {
            SetPanelState(main: false, participant: false, config: false, credits: true, attribution: false);
        }

        public void ShowAttribution()
        {
            SetPanelState(main: false, participant: false, config: false, credits: false, attribution: true);
        }

        private void SetPanelState(bool main, bool participant, bool config, bool credits, bool attribution)
        {
            if (_mainMenuPanel != null) _mainMenuPanel.SetActive(main);
            if (_participantSetupPanel != null) _participantSetupPanel.SetActive(participant);
            if (_researchConfigPanel != null) _researchConfigPanel.SetActive(config);
            if (_creditsPanel != null) _creditsPanel.SetActive(credits);
            if (_attributionPanel != null) _attributionPanel.SetActive(attribution);
        }

        public void OnStartSimulationClicked()
        {
            if (_simulationManager != null)
            {
                // Start in-scene simulation trial sequence
                SetPanelState(false, false, false, false, false);
                _simulationManager.StartSimulation();
            }
            else
            {
                // Fallback: If original scene is available in build settings, can load it
                if (SceneManager.sceneCountInBuildSettings > 1)
                {
                    SceneManager.LoadScene(1);
                }
                else
                {
                    Debug.Log("[MainMenuController] Starting simulation sequence.");
                }
            }
        }

        public void OnExitClicked()
        {
            Debug.Log("[MainMenuController] Exiting IVRAR Group 02 simulation.");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
