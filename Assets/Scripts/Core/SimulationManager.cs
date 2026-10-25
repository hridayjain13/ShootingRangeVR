using System;
using UnityEngine;
using IVRAR.Group02.Environment;
using IVRAR.Group02.SensoryStress;
using IVRAR.Group02.TargetManagement;
using IVRAR.Group02.Telemetry;
using IVRAR.Group02.HumanFactors;
using IVRAR.Group02.UI;

namespace IVRAR.Group02.Core
{
    /// <summary>
    /// Master coordinator for IVRAR Group 02 VR training simulation.
    /// Orchestrates environment lighting, sensory stress triggers, target presentation,
    /// telemetry logging, post-trial assessment, and user interface transitions.
    /// </summary>
    public class SimulationManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private ExperimentConfig _activeConfig;

        [Header("Subsystem References")]
        [SerializeField] private LowVisibilityEnvironmentController _environmentController;
        [SerializeField] private SensoryStressManager _sensoryStressManager;
        [SerializeField] private SensoryStressTargetManager _targetManager;
        [SerializeField] private ReactionLatencyTelemetryLogger _telemetryLogger;
        [SerializeField] private PostTrialAssessmentController _assessmentController;
        [SerializeField] private MainMenuController _mainMenuController;
        [SerializeField] private HUDController _hudController;

        public event Action<SimulationState> OnStateChanged;

        private SimulationState _currentState = SimulationState.Idle;
        public SimulationState CurrentState => _currentState;

        private void Awake()
        {
            // Auto-resolve references if not explicitly assigned
            if (_environmentController == null) _environmentController = FindObjectOfType<LowVisibilityEnvironmentController>();
            if (_sensoryStressManager == null) _sensoryStressManager = FindObjectOfType<SensoryStressManager>();
            if (_targetManager == null) _targetManager = FindObjectOfType<SensoryStressTargetManager>();
            if (_telemetryLogger == null) _telemetryLogger = FindObjectOfType<ReactionLatencyTelemetryLogger>();
            if (_assessmentController == null) _assessmentController = FindObjectOfType<PostTrialAssessmentController>();
            if (_mainMenuController == null) _mainMenuController = FindObjectOfType<MainMenuController>();
            if (_hudController == null) _hudController = FindObjectOfType<HUDController>();
        }

        private void Start()
        {
            SetState(SimulationState.Idle);

            if (_targetManager != null)
            {
                _targetManager.OnAllTrialsCompleted += HandleAllTrialsCompleted;
                _targetManager.OnTargetPresented += HandleTargetPresented;
            }

            if (_assessmentController != null)
            {
                _assessmentController.OnAssessmentCompleted += HandleAssessmentCompleted;
            }
        }

        public void SetParticipantMetadata(string code, string session, string condition)
        {
            if (_telemetryLogger != null)
            {
                _telemetryLogger.participantCode = code;
                _telemetryLogger.sessionId = session;
                _telemetryLogger.conditionLabel = condition;
            }
        }

        public void StartSimulation()
        {
            SetState(SimulationState.RunningTrial);

            // 1. Initialize environment
            if (_environmentController != null && _activeConfig != null)
                _environmentController.ApplyConfiguration(_activeConfig);

            // 2. Initialize stressors
            if (_sensoryStressManager != null && _activeConfig != null)
                _sensoryStressManager.Initialize(_activeConfig);

            // 3. Initialize targets & begin sequence
            if (_targetManager != null)
            {
                _targetManager.Initialize(_activeConfig);
                _targetManager.StartTrialSequence();
            }

            if (_hudController != null && _activeConfig != null)
                _hudController.UpdateProgress(1, _activeConfig.totalTrialsInBlock);
        }

        private void HandleTargetPresented(int trialIndex, bool isHostile, float distance, float angle)
        {
            SetState(SimulationState.TargetPresented);

            if (_hudController != null && _activeConfig != null)
                _hudController.UpdateProgress(trialIndex, _activeConfig.totalTrialsInBlock);
        }

        private void HandleAllTrialsCompleted()
        {
            SetState(SimulationState.PostTrialAssessment);

            // Initiate post-trial survey
            if (_assessmentController != null)
            {
                _assessmentController.BeginAssessment("FINAL_SESSION", true, false);
            }
            else
            {
                FinishSession();
            }
        }

        private void HandleAssessmentCompleted()
        {
            FinishSession();
        }

        public void FinishSession()
        {
            SetState(SimulationState.SessionCompleted);

            // Export telemetry
            if (_telemetryLogger != null)
                _telemetryLogger.ExportTelemetryToDisk();

            // Restore baseline lighting
            if (_environmentController != null)
                _environmentController.RestoreBaselineEnvironment();

            // Return to main menu
            if (_mainMenuController != null)
                _mainMenuController.ShowMainMenu();

            Debug.Log("[SimulationManager] Session complete. Telemetry exported.");
        }

        public void StopSimulation()
        {
            if (_targetManager != null) _targetManager.StopTrialSequence();
            if (_sensoryStressManager != null) _sensoryStressManager.StopAllStressors();
            if (_environmentController != null) _environmentController.RestoreBaselineEnvironment();
            if (_mainMenuController != null) _mainMenuController.ShowMainMenu();

            SetState(SimulationState.Idle);
        }

        private void SetState(SimulationState newState)
        {
            _currentState = newState;
            OnStateChanged?.Invoke(_currentState);
            if (_hudController != null) _hudController.UpdateState(_currentState);
        }

        private void OnDestroy()
        {
            if (_targetManager != null)
            {
                _targetManager.OnAllTrialsCompleted -= HandleAllTrialsCompleted;
                _targetManager.OnTargetPresented -= HandleTargetPresented;
            }

            if (_assessmentController != null)
            {
                _assessmentController.OnAssessmentCompleted -= HandleAssessmentCompleted;
            }
        }
    }
}
