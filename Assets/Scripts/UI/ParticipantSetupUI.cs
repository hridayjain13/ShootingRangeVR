using UnityEngine;
using UnityEngine.UI;
using IVRAR.Group02.Telemetry;
using IVRAR.Group02.Core;

namespace IVRAR.Group02.UI
{
    /// <summary>
    /// UI form for configuring participant codes, session IDs, and experimental cohort groups.
    /// Strictly adheres to institutional research ethics: uses pseudonymous codes only.
    /// </summary>
    public class ParticipantSetupUI : MonoBehaviour
    {
        [Header("Input Fields")]
        [SerializeField] private InputField _inputParticipantCode;
        [SerializeField] private InputField _inputSessionId;
        [SerializeField] private Dropdown _dropdownCondition;

        [Header("Status Feedback")]
        [SerializeField] private Text _lblStatus;
        [SerializeField] private Button _btnSave;

        [Header("System Links")]
        [SerializeField] private ReactionLatencyTelemetryLogger _telemetryLogger;
        [SerializeField] private SimulationManager _simulationManager;

        private void Awake()
        {
            if (_telemetryLogger == null) _telemetryLogger = FindObjectOfType<ReactionLatencyTelemetryLogger>();
            if (_simulationManager == null) _simulationManager = FindObjectOfType<SimulationManager>();

            if (_btnSave != null) _btnSave.onClick.AddListener(SaveParticipantDetails);

            // Populate defaults
            if (_inputParticipantCode != null && string.IsNullOrEmpty(_inputParticipantCode.text))
                _inputParticipantCode.text = "P01";

            if (_inputSessionId != null && string.IsNullOrEmpty(_inputSessionId.text))
                _inputSessionId.text = "S01_BLOCK1";

            if (_dropdownCondition != null)
            {
                _dropdownCondition.options.Clear();
                _dropdownCondition.options.Add(new Dropdown.OptionData("Low-Visibility Sensory Stress (Active)"));
                _dropdownCondition.options.Add(new Dropdown.OptionData("Low-Visibility Control (No Stress)"));
                _dropdownCondition.options.Add(new Dropdown.OptionData("Baseline Clear Daylight"));
                _dropdownCondition.value = 0;
            }
        }

        public void SaveParticipantDetails()
        {
            string code = (_inputParticipantCode != null && !string.IsNullOrWhiteSpace(_inputParticipantCode.text))
                ? _inputParticipantCode.text.Trim() : "P01";

            string session = (_inputSessionId != null && !string.IsNullOrWhiteSpace(_inputSessionId.text))
                ? _inputSessionId.text.Trim() : "S01";

            string cond = (_dropdownCondition != null)
                ? _dropdownCondition.options[_dropdownCondition.value].text : "Low-Visibility Sensory Stress";

            if (_telemetryLogger != null)
            {
                _telemetryLogger.participantCode = code;
                _telemetryLogger.sessionId = session;
                _telemetryLogger.conditionLabel = cond;
            }

            if (_simulationManager != null)
            {
                _simulationManager.SetParticipantMetadata(code, session, cond);
            }

            if (_lblStatus != null)
            {
                _lblStatus.text = string.Format("Saved! Participant: {0} | Session: {1}", code, session);
                _lblStatus.color = Color.green;
            }
        }
    }
}
