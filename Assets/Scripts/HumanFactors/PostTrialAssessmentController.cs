using System;
using UnityEngine;
using UnityEngine.UI;
using IVRAR.Group02.Telemetry;

namespace IVRAR.Group02.HumanFactors
{
    /// <summary>
    /// In-VR World Space assessment interface for collecting NASA-TLX cognitive workload
    /// and SAGAT situational awareness responses after each experimental block.
    /// </summary>
    public sealed class PostTrialAssessmentController : MonoBehaviour
    {
        [Header("UI Component References")]
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private Text _categoryLabel;
        [SerializeField] private Text _questionLabel;
        [SerializeField] private Text _valueLabel;
        [SerializeField] private Slider _responseSlider;
        [SerializeField] private Button _nextButton;
        [SerializeField] private Text _nextButtonLabel;

        [Header("Telemetry Link")]
        [SerializeField] private ReactionLatencyTelemetryLogger _logger;

        public event Action OnAssessmentCompleted;

        private readonly float[] _tlxScores = new float[6];
        private readonly string[] _questionTitles = {
            "NASA-TLX: Mental Demand",
            "NASA-TLX: Physical Demand",
            "NASA-TLX: Temporal Demand",
            "NASA-TLX: Performance Rating",
            "NASA-TLX: Effort Exerted",
            "NASA-TLX: Frustration Level",
            "SAGAT Recall: Target Type",
            "SAGAT Recall: Target Position"
        };

        private readonly string[] _questionDescriptions = {
            "How much mental and perceptual activity was required (thinking, deciding, searching)? (0=Low, 100=High)",
            "How much physical activity was required (moving, aiming, controlling)? (0=Low, 100=High)",
            "How much time pressure did you feel due to the pace of the scenario? (0=Low, 100=High)",
            "How successful do you think you were in accomplishing the defense goals? (0=Good, 100=Poor)",
            "How hard did you have to work to accomplish your level of performance? (0=Low, 100=High)",
            "How insecure, discouraged, irritated, or stressed did you feel? (0=Low, 100=High)",
            "Was the last presented target HOSTILE? (0 = Civilian/Unarmed, 1 = Hostile Threat)",
            "Was the target located on your LEFT side? (0 = Right/Center, 1 = Left)"
        };

        private int _currentQuestionIndex;
        private int _sagatCorrect;
        private bool _actualHostile;
        private bool _actualLeft;
        private string _activeTrialId;
        private bool _isAssessing;

        public bool IsAssessing => _isAssessing;

        private void Awake()
        {
            if (_logger == null) _logger = FindObjectOfType<ReactionLatencyTelemetryLogger>();
            if (_panelRoot != null) _panelRoot.SetActive(false);
            if (_responseSlider != null) _responseSlider.onValueChanged.AddListener(OnSliderChanged);
            if (_nextButton != null) _nextButton.onClick.AddListener(AdvanceQuestion);
        }

        public void BeginAssessment(string trialId, bool actualHostile, bool actualLeft)
        {
            if (_isAssessing) return;

            _activeTrialId = trialId;
            _actualHostile = actualHostile;
            _actualLeft = actualLeft;
            _currentQuestionIndex = 0;
            _sagatCorrect = 0;
            _isAssessing = true;

            if (_panelRoot != null) _panelRoot.SetActive(true);
            DisplayCurrentQuestion();
        }

        private void DisplayCurrentQuestion()
        {
            if (_questionLabel == null || _responseSlider == null) return;

            if (_categoryLabel != null)
                _categoryLabel.text = string.Format("Item {0} of {1}", _currentQuestionIndex + 1, _questionTitles.Length);

            _questionLabel.text = _questionTitles[_currentQuestionIndex] + "\n\n" + _questionDescriptions[_currentQuestionIndex];

            bool isBinaryProbe = _currentQuestionIndex >= 6;
            _responseSlider.minValue = 0;
            _responseSlider.maxValue = isBinaryProbe ? 1 : 100;
            _responseSlider.wholeNumbers = true;
            _responseSlider.value = isBinaryProbe ? 0 : 50;

            if (_nextButtonLabel != null)
                _nextButtonLabel.text = (_currentQuestionIndex == _questionTitles.Length - 1) ? "Submit & Save" : "Next";

            OnSliderChanged(_responseSlider.value);
        }

        private void OnSliderChanged(float val)
        {
            if (_valueLabel == null) return;
            int intVal = Mathf.RoundToInt(val);

            if (_currentQuestionIndex >= 6)
            {
                if (_currentQuestionIndex == 6)
                    _valueLabel.text = (intVal == 1) ? "1 (Hostile Threat)" : "0 (Civilian / Neutral)";
                else
                    _valueLabel.text = (intVal == 1) ? "1 (Left Side)" : "0 (Right / Center)";
            }
            else
            {
                _valueLabel.text = intVal.ToString();
            }
        }

        public void AdvanceQuestion()
        {
            if (!_isAssessing) return;

            int val = Mathf.RoundToInt(_responseSlider.value);

            if (_currentQuestionIndex < 6)
            {
                _tlxScores[_currentQuestionIndex] = val;
            }
            else
            {
                bool answerTrue = (val == 1);
                bool truth = (_currentQuestionIndex == 6) ? _actualHostile : _actualLeft;
                if (answerTrue == truth) _sagatCorrect++;
            }

            _currentQuestionIndex++;

            if (_currentQuestionIndex < _questionTitles.Length)
            {
                DisplayCurrentQuestion();
            }
            else
            {
                // Finalize assessment
                _isAssessing = false;
                if (_panelRoot != null) _panelRoot.SetActive(false);

                if (_logger != null)
                {
                    _logger.RecordAssessment(
                        _logger.participantCode,
                        _logger.sessionId,
                        _activeTrialId,
                        _tlxScores,
                        _sagatCorrect,
                        2
                    );
                }

                OnAssessmentCompleted?.Invoke();
            }
        }
    }
}
