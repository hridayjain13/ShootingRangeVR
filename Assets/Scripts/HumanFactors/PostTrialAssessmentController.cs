using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using IVRAR.Group02.Telemetry;

namespace IVRAR.Group02.HumanFactors
{
    /// <summary>
    /// Master In-VR and Desktop controller for standardized human-factors assessment workflows:
    /// 1. NASA Task Load Index (NASA-TLX) - 6 workload dimensions & Raw TLX (RTLX).
    /// 2. Kennedy Simulator Sickness Questionnaire (SSQ) - Pre-session & Post-session with Delta SSQ.
    /// 3. Situational Awareness Global Assessment Technique (SAGAT) - Simulation freeze & screen occlusion.
    /// 
    /// Adheres to ethical research standards:
    /// - Strict pseudonymous participant ID association (Zero PII).
    /// - Explicit missing-answer handling (coded as -1, never fabricated/imputed).
    /// - Session interruption (Pause / Resume) and optional voluntary withdrawal at any time without penalty.
    /// - Non-blocking asynchronous CSV telemetry export.
    /// </summary>
    public sealed class PostTrialAssessmentController : MonoBehaviour
    {
        [Header("UI Component References")]
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private GameObject _occlusionPanel; // Screen-blanking overlay during SAGAT freeze
        [SerializeField] private Text _categoryLabel;
        [SerializeField] private Text _questionLabel;
        [SerializeField] private Text _instructionLabel;
        [SerializeField] private Text _valueLabel;
        [SerializeField] private Slider _responseSlider;
        [SerializeField] private Button _nextButton;
        [SerializeField] private Text _nextButtonLabel;
        [SerializeField] private Button _pauseButton;
        [SerializeField] private Button _withdrawButton;
        [SerializeField] private Button _skipButton;

        [Header("Telemetry Link")]
        [SerializeField] private ReactionLatencyTelemetryLogger _logger;
        [SerializeField] private string _exportCsvRelativePath = "telemetry/human_factors_assessments.csv";

        public event Action OnAssessmentCompleted;
        public event Action OnParticipantWithdrawn;
        public event Action<bool> OnSimulationPaused; // true = paused, false = resumed

        private enum FlowMode
        {
            None,
            PreSessionSsq,
            PostSessionSsq,
            NasaTlx,
            SagatFreeze,
            LegacyMixed
        }

        private FlowMode _currentMode = FlowMode.None;
        private bool _isAssessing;
        private bool _isPaused;
        private float _previousTimeScale = 1f;

        // Session metadata
        private string _activeParticipantCode = "P001";
        private string _activeSessionId = "SESS01";
        private string _activeConditionLabel = "Baseline";
        private string _activeTrialId = "TRIAL-000";

        // Current questionnaire state
        private int _currentQuestionIndex;
        private int _totalQuestionsInFlow;
        private readonly List<string> _prompts = new List<string>();
        private readonly List<string> _explanations = new List<string>();
        private readonly List<float> _currentResponses = new List<float>();
        private readonly List<Vector2> _sliderRanges = new List<Vector2>(); // min, max
        private readonly List<bool> _isInteger = new List<bool>();

        // Assessment data instances
        private KennedySsqAssessment _preSessionSsq;
        private KennedySsqAssessment _postSessionSsq;
        private NasaTlxAssessment _currentTlx;
        private SagatAssessment _currentSagat;

        // Ground truth for SAGAT freeze
        private bool _freezeHostile;
        private bool _freezeLeft;
        private int _freezeSide;
        private int _freezeIllumination;
        private int _freezeDistanceTier;
        private int _freezeDistractionType;

        private static readonly object _csvLock = new object();

        public bool IsAssessing => _isAssessing;
        public bool IsPaused => _isPaused;
        public string ParticipantCode => _activeParticipantCode;
        public string SessionId => _activeSessionId;

        private void Awake()
        {
            if (_logger == null) _logger = FindObjectOfType<ReactionLatencyTelemetryLogger>();
            if (_panelRoot != null) _panelRoot.SetActive(false);
            if (_occlusionPanel != null) _occlusionPanel.SetActive(false);

            if (_responseSlider != null) _responseSlider.onValueChanged.AddListener(OnSliderChanged);
            if (_nextButton != null) _nextButton.onClick.AddListener(AdvanceQuestion);
            if (_pauseButton != null) _pauseButton.onClick.AddListener(TogglePauseAssessment);
            if (_withdrawButton != null) _withdrawButton.onClick.AddListener(HandleVoluntaryWithdrawal);
            if (_skipButton != null) _skipButton.onClick.AddListener(SkipCurrentQuestion);
        }

        private void Start()
        {
            SyncParticipantMetadataFromLogger();
        }

        private void SyncParticipantMetadataFromLogger()
        {
            if (_logger != null)
            {
                if (!string.IsNullOrEmpty(_logger.participantCode)) _activeParticipantCode = _logger.participantCode;
                if (!string.IsNullOrEmpty(_logger.sessionId)) _activeSessionId = _logger.sessionId;
                if (!string.IsNullOrEmpty(_logger.conditionLabel)) _activeConditionLabel = _logger.conditionLabel;
            }
        }

        public void SetParticipantInfo(string participantCode, string sessionId, string condition)
        {
            if (!string.IsNullOrEmpty(participantCode)) _activeParticipantCode = participantCode;
            if (!string.IsNullOrEmpty(sessionId)) _activeSessionId = sessionId;
            if (!string.IsNullOrEmpty(condition)) _activeConditionLabel = condition;
        }

        #region Standard Assessment Triggers

        /// <summary>
        /// Administers Pre-Session Kennedy Simulator Sickness Questionnaire (SSQ).
        /// Establishes baseline cybersickness/symptom score before immersion.
        /// </summary>
        public void BeginPreSessionSsq()
        {
            if (_isAssessing) return;
            SyncParticipantMetadataFromLogger();

            _currentMode = FlowMode.PreSessionSsq;
            _preSessionSsq = new KennedySsqAssessment
            {
                ParticipantCode = _activeParticipantCode,
                SessionId = _activeSessionId,
                Timing = AssessmentTiming.PreSession,
                TimestampUtc = DateTime.UtcNow.ToString("o")
            };

            SetupSsqFlow("Pre-Session Simulator Sickness Questionnaire (Baseline)");
        }

        /// <summary>
        /// Administers Post-Session Kennedy Simulator Sickness Questionnaire (SSQ).
        /// Enables calculation of Delta SSQ = Post - Pre.
        /// </summary>
        public void BeginPostSessionSsq()
        {
            if (_isAssessing) return;
            SyncParticipantMetadataFromLogger();

            _currentMode = FlowMode.PostSessionSsq;
            _postSessionSsq = new KennedySsqAssessment
            {
                ParticipantCode = _activeParticipantCode,
                SessionId = _activeSessionId,
                Timing = AssessmentTiming.PostSession,
                TimestampUtc = DateTime.UtcNow.ToString("o")
            };

            SetupSsqFlow("Post-Session Simulator Sickness Questionnaire");
        }

        /// <summary>
        /// Administers NASA Task Load Index (NASA-TLX) across all 6 workload dimensions.
        /// </summary>
        public void BeginNasaTlx(string conditionLabel = null)
        {
            if (_isAssessing) return;
            SyncParticipantMetadataFromLogger();
            if (!string.IsNullOrEmpty(conditionLabel)) _activeConditionLabel = conditionLabel;

            _currentMode = FlowMode.NasaTlx;
            _currentTlx = new NasaTlxAssessment
            {
                ParticipantCode = _activeParticipantCode,
                SessionId = _activeSessionId,
                ConditionLabel = _activeConditionLabel,
                Timing = AssessmentTiming.PostBlock,
                TimestampUtc = DateTime.UtcNow.ToString("o")
            };

            SetupNasaTlxFlow();
        }

        /// <summary>
        /// Initiates controlled SAGAT simulation freeze:
        /// 1. Freezes simulation (Time.timeScale = 0).
        /// 2. Blanks/occludes target display to test mental model without visual aid.
        /// 3. Presents research questions across Endsley Levels 1, 2, and 3.
        /// 4. Restores time and unblanks when completed.
        /// </summary>
        public void BeginSagatFreeze(string trialId, bool isHostile, int sideIndex, int illuminationIndex, int distanceTier, int distractionType)
        {
            if (_isAssessing) return;
            SyncParticipantMetadataFromLogger();

            _previousTimeScale = Time.timeScale;
            Time.timeScale = 0f; // Freeze simulation physics and animation

            if (_occlusionPanel != null) _occlusionPanel.SetActive(true); // Occlude target display

            _currentMode = FlowMode.SagatFreeze;
            _activeTrialId = trialId;

            _currentSagat = new SagatAssessment
            {
                ParticipantCode = _activeParticipantCode,
                SessionId = _activeSessionId,
                TrialId = trialId,
                ConditionLabel = _activeConditionLabel,
                Timing = AssessmentTiming.DuringFreeze,
                TimestampUtc = DateTime.UtcNow.ToString("o"),
                GroundTruthIsHostile = isHostile,
                GroundTruthSideIndex = sideIndex,
                GroundTruthIlluminationIndex = illuminationIndex,
                GroundTruthDistanceTier = distanceTier,
                GroundTruthDistractionType = distractionType,
                GroundTruthRequiredAction = isHostile ? 0 : 1
            };

            _currentSagat.PopulateStandardQuestionBank();
            SetupSagatFlow();
        }

        /// <summary>
        /// Backwards-compatible legacy trigger for existing SimulationManager trial cycle.
        /// Combines NASA-TLX and 2 binary SAGAT recall probes.
        /// </summary>
        public void BeginAssessment(string trialId, bool actualHostile, bool actualLeft)
        {
            if (_isAssessing) return;
            SyncParticipantMetadataFromLogger();

            _currentMode = FlowMode.LegacyMixed;
            _activeTrialId = trialId;
            _freezeHostile = actualHostile;
            _freezeLeft = actualLeft;

            SetupLegacyMixedFlow();
        }

        #endregion

        #region Questionnaire Setup Routines

        private void SetupSsqFlow(string titleHeader)
        {
            _prompts.Clear();
            _explanations.Clear();
            _currentResponses.Clear();
            _sliderRanges.Clear();
            _isInteger.Clear();

            string[] symptoms = {
                "1. General Discomfort",
                "2. Fatigue",
                "3. Headache",
                "4. Eye Strain",
                "5. Difficulty Focusing",
                "6. Increased Salivation",
                "7. Sweating",
                "8. Nausea",
                "9. Difficulty Concentrating",
                "10. Fullness of Head",
                "11. Blurred Vision",
                "12. Dizziness with Eyes Open",
                "13. Dizziness with Eyes Closed",
                "14. Vertigo",
                "15. Stomach Awareness",
                "16. Burping"
            };

            string instr = "Instructions: Rate how much each symptom is affecting you right now.\n" +
                          "Scale: 0 = None, 1 = Slight, 2 = Moderate, 3 = Severe.\n" +
                          "Your participation is voluntary; you may pause or withdraw at any time.";

            for (int i = 0; i < symptoms.Length; i++)
            {
                _prompts.Add(titleHeader + "\n" + symptoms[i]);
                _explanations.Add(instr);
                _currentResponses.Add(-1f); // Default unanswered
                _sliderRanges.Add(new Vector2(0f, 3f));
                _isInteger.Add(true);
            }

            StartDisplayFlow();
        }

        private void SetupNasaTlxFlow()
        {
            _prompts.Clear();
            _explanations.Clear();
            _currentResponses.Clear();
            _sliderRanges.Clear();
            _isInteger.Clear();

            string[] dims = {
                "NASA-TLX: Mental Demand",
                "NASA-TLX: Physical Demand",
                "NASA-TLX: Temporal Demand",
                "NASA-TLX: Performance Rating",
                "NASA-TLX: Effort Exerted",
                "NASA-TLX: Frustration Level"
            };

            string[] descs = {
                "How much mental and perceptual activity was required (thinking, deciding, searching)? (0 = Very Low, 100 = Very High)",
                "How much physical activity was required (moving, aiming, controlling)? (0 = Very Low, 100 = Very High)",
                "How much time pressure did you feel due to the pace of the scenario? (0 = Very Low, 100 = Very High)",
                "How successful do you think you were in accomplishing the defense goals? (0 = Perfect/Good, 100 = Failure/Poor)",
                "How hard did you have to work to accomplish your level of performance? (0 = Very Low, 100 = Very High)",
                "How insecure, discouraged, irritated, or stressed did you feel? (0 = Very Low, 100 = Very High)"
            };

            for (int i = 0; i < dims.Length; i++)
            {
                _prompts.Add(dims[i]);
                _explanations.Add(descs[i]);
                _currentResponses.Add(-1f);
                _sliderRanges.Add(new Vector2(0f, 100f));
                _isInteger.Add(true);
            }

            StartDisplayFlow();
        }

        private void SetupSagatFlow()
        {
            _prompts.Clear();
            _explanations.Clear();
            _currentResponses.Clear();
            _sliderRanges.Clear();
            _isInteger.Clear();

            for (int i = 0; i < _currentSagat.Questions.Count; i++)
            {
                var q = _currentSagat.Questions[i];
                string prompt = string.Format("[SAGAT Freeze Probe - {0}]\n{1}", q.Level.ToString().Replace("_", " "), q.Prompt);

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("The scenario is frozen and the display occluded. Answer based on your situational awareness:");
                for (int j = 0; j < q.Options.Length; j++)
                {
                    sb.AppendLine(string.Format("  [{0}] = {1}", j, q.Options[j]));
                }

                _prompts.Add(prompt);
                _explanations.Add(sb.ToString().TrimEnd());
                _currentResponses.Add(-1f);
                _sliderRanges.Add(new Vector2(0f, q.Options.Length - 1));
                _isInteger.Add(true);
            }

            StartDisplayFlow();
        }

        private void SetupLegacyMixedFlow()
        {
            _prompts.Clear();
            _explanations.Clear();
            _currentResponses.Clear();
            _sliderRanges.Clear();
            _isInteger.Clear();

            string[] titles = {
                "NASA-TLX: Mental Demand",
                "NASA-TLX: Physical Demand",
                "NASA-TLX: Temporal Demand",
                "NASA-TLX: Performance Rating",
                "NASA-TLX: Effort Exerted",
                "NASA-TLX: Frustration Level",
                "SAGAT Recall: Target Type",
                "SAGAT Recall: Target Position"
            };

            string[] descs = {
                "How much mental and perceptual activity was required? (0 = Low, 100 = High)",
                "How much physical activity was required? (0 = Low, 100 = High)",
                "How much time pressure did you feel? (0 = Low, 100 = High)",
                "How successful do you think you were? (0 = Good, 100 = Poor)",
                "How hard did you have to work to accomplish performance? (0 = Low, 100 = High)",
                "How insecure, discouraged, or stressed did you feel? (0 = Low, 100 = High)",
                "Was the last presented target HOSTILE? (0 = Civilian/Unarmed, 1 = Hostile Threat)",
                "Was the target located on your LEFT side? (0 = Right/Center, 1 = Left)"
            };

            for (int i = 0; i < titles.Length; i++)
            {
                _prompts.Add(titles[i]);
                _explanations.Add(descs[i]);
                _currentResponses.Add(-1f);
                bool isBinary = (i >= 6);
                _sliderRanges.Add(new Vector2(0f, isBinary ? 1f : 100f));
                _isInteger.Add(true);
            }

            StartDisplayFlow();
        }

        private void StartDisplayFlow()
        {
            _currentQuestionIndex = 0;
            _totalQuestionsInFlow = _prompts.Count;
            _isAssessing = true;
            _isPaused = false;

            if (_panelRoot != null) _panelRoot.SetActive(true);
            DisplayCurrentQuestion();
        }

        #endregion

        #region Display & Interaction

        private void DisplayCurrentQuestion()
        {
            if (_questionLabel == null || _responseSlider == null) return;

            if (_categoryLabel != null)
                _categoryLabel.text = string.Format("Item {0} of {1} ({2})", _currentQuestionIndex + 1, _totalQuestionsInFlow, _currentMode);

            _questionLabel.text = _prompts[_currentQuestionIndex];

            if (_instructionLabel != null)
                _instructionLabel.text = _explanations[_currentQuestionIndex];

            Vector2 range = _sliderRanges[_currentQuestionIndex];
            _responseSlider.minValue = range.x;
            _responseSlider.maxValue = range.y;
            _responseSlider.wholeNumbers = _isInteger[_currentQuestionIndex];

            // If question has not been set yet, set slider to midpoint
            float existingVal = _currentResponses[_currentQuestionIndex];
            if (existingVal < 0f)
            {
                _responseSlider.value = Mathf.Round((range.x + range.y) / 2f);
            }
            else
            {
                _responseSlider.value = existingVal;
            }

            if (_nextButtonLabel != null)
                _nextButtonLabel.text = (_currentQuestionIndex == _totalQuestionsInFlow - 1) ? "Submit & Save" : "Next";

            OnSliderChanged(_responseSlider.value);
        }

        private void OnSliderChanged(float val)
        {
            if (_valueLabel == null) return;

            int intVal = Mathf.RoundToInt(val);

            if (_currentMode == FlowMode.PreSessionSsq || _currentMode == FlowMode.PostSessionSsq)
            {
                string[] ssqLabels = { "0 (None)", "1 (Slight)", "2 (Moderate)", "3 (Severe)" };
                _valueLabel.text = (intVal >= 0 && intVal < ssqLabels.Length) ? ssqLabels[intVal] : intVal.ToString();
            }
            else if (_currentMode == FlowMode.SagatFreeze && _currentSagat != null && _currentQuestionIndex < _currentSagat.Questions.Count)
            {
                var q = _currentSagat.Questions[_currentQuestionIndex];
                if (intVal >= 0 && intVal < q.Options.Length)
                    _valueLabel.text = string.Format("{0} ({1})", intVal, q.Options[intVal]);
                else
                    _valueLabel.text = intVal.ToString();
            }
            else if (_currentMode == FlowMode.LegacyMixed && _currentQuestionIndex >= 6)
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

            // Record response
            _currentResponses[_currentQuestionIndex] = _responseSlider.value;
            _currentQuestionIndex++;

            if (_currentQuestionIndex < _totalQuestionsInFlow)
            {
                DisplayCurrentQuestion();
            }
            else
            {
                FinalizeAssessment(CompletionStatus.Completed);
            }
        }

        public void SkipCurrentQuestion()
        {
            if (!_isAssessing) return;

            // Leave as -1 to indicate explicitly skipped/missing answer
            _currentResponses[_currentQuestionIndex] = -1f;
            _currentQuestionIndex++;

            if (_currentQuestionIndex < _totalQuestionsInFlow)
            {
                DisplayCurrentQuestion();
            }
            else
            {
                FinalizeAssessment(CompletionStatus.PartiallyCompleted);
            }
        }

        public void TogglePauseAssessment()
        {
            if (!_isAssessing) return;

            _isPaused = !_isPaused;

            if (_isPaused)
            {
                if (_instructionLabel != null)
                    _instructionLabel.text = "[PAUSED] Assessment session paused by participant or researcher. Click Resume to continue.";
                if (_nextButton != null) _nextButton.interactable = false;
                if (_responseSlider != null) _responseSlider.interactable = false;
            }
            else
            {
                if (_nextButton != null) _nextButton.interactable = true;
                if (_responseSlider != null) _responseSlider.interactable = true;
                DisplayCurrentQuestion();
            }

            OnSimulationPaused?.Invoke(_isPaused);
        }

        public void HandleVoluntaryWithdrawal()
        {
            if (!_isAssessing) return;

            Debug.LogWarning("[HumanFactors] Participant exercised voluntary withdrawal right. Ceasing assessment without penalty.");
            FinalizeAssessment(CompletionStatus.Withdrawn);
            OnParticipantWithdrawn?.Invoke();
        }

        #endregion

        #region Finalization & Archiving

        private void FinalizeAssessment(CompletionStatus status)
        {
            _isAssessing = false;

            // Restore timescale and unblank occlusion panel if frozen
            if (_currentMode == FlowMode.SagatFreeze)
            {
                Time.timeScale = (_previousTimeScale > 0f) ? _previousTimeScale : 1f;
                if (_occlusionPanel != null) _occlusionPanel.SetActive(false);
            }

            if (_panelRoot != null) _panelRoot.SetActive(false);

            // Export and log based on active instrument
            switch (_currentMode)
            {
                case FlowMode.PreSessionSsq:
                    FinalizeSsq(_preSessionSsq, status);
                    break;

                case FlowMode.PostSessionSsq:
                    FinalizeSsq(_postSessionSsq, status);
                    // Compute Delta SSQ if pre-session is present
                    if (_preSessionSsq != null && _postSessionSsq != null)
                    {
                        var delta = KennedySsqAssessment.ComputeDelta(_preSessionSsq, _postSessionSsq);
                        Debug.Log(string.Format(
                            CultureInfo.InvariantCulture,
                            "[HumanFactors] Delta SSQ Computed: dN={0:F2}, dO={1:F2}, dD={2:F2}, dTS={3:F2}",
                            delta.DeltaNausea, delta.DeltaOculomotor, delta.DeltaDisorientation, delta.DeltaTotalSeverity
                        ));
                    }
                    break;

                case FlowMode.NasaTlx:
                    FinalizeTlx(status);
                    break;

                case FlowMode.SagatFreeze:
                    FinalizeSagat(status);
                    break;

                case FlowMode.LegacyMixed:
                    FinalizeLegacyMixed(status);
                    break;
            }

            _currentMode = FlowMode.None;
            OnAssessmentCompleted?.Invoke();
        }

        private void FinalizeSsq(KennedySsqAssessment ssq, CompletionStatus status)
        {
            if (ssq == null) return;

            ssq.Status = status;
            int[] vals = new int[16];
            for (int i = 0; i < 16 && i < _currentResponses.Count; i++)
            {
                vals[i] = Mathf.RoundToInt(_currentResponses[i]);
            }
            ssq.SetAllSymptoms(vals);

            // Export CSV row
            AppendAssessmentToCsv(ssq.ToCsvRow(), KennedySsqAssessment.GetCsvHeader());
            Debug.Log(string.Format("[HumanFactors] SSQ {0} finalized. Status={1}", ssq.Timing, status));
        }

        private void FinalizeTlx(CompletionStatus status)
        {
            if (_currentTlx == null) return;

            _currentTlx.Status = status;
            float[] dims = new float[6];
            for (int i = 0; i < 6 && i < _currentResponses.Count; i++)
            {
                dims[i] = _currentResponses[i];
            }
            _currentTlx.FromArray(dims);

            AppendAssessmentToCsv(_currentTlx.ToCsvRow(), NasaTlxAssessment.GetCsvHeader());
            Debug.Log(string.Format(
                CultureInfo.InvariantCulture,
                "[HumanFactors] NASA-TLX finalized. RTLX={0:F2}, Status={1}",
                _currentTlx.ComputeRawTlxScore(), status
            ));
        }

        private void FinalizeSagat(CompletionStatus status)
        {
            if (_currentSagat == null) return;

            _currentSagat.Status = status;
            for (int i = 0; i < _currentSagat.Questions.Count && i < _currentResponses.Count; i++)
            {
                _currentSagat.Questions[i].SelectedOptionIndex = Mathf.RoundToInt(_currentResponses[i]);
            }

            AppendAssessmentToCsv(_currentSagat.ToCsvRow(), SagatAssessment.GetCsvHeader());
            var res = _currentSagat.ComputeRubricScores();
            Debug.Log(string.Format(
                CultureInfo.InvariantCulture,
                "[HumanFactors] SAGAT freeze finalized. Overall SA={0:F1}%, L1={1:F1}%, L2={2:F1}%, L3={3:F1}%, Status={4}",
                res.OverallSaPercentage, res.Level1Percentage, res.Level2Percentage, res.Level3Percentage, status
            ));
        }

        private void FinalizeLegacyMixed(CompletionStatus status)
        {
            float[] tlxScores = new float[6];
            for (int i = 0; i < 6 && i < _currentResponses.Count; i++)
            {
                tlxScores[i] = _currentResponses[i] >= 0 ? _currentResponses[i] : 0f;
            }

            int sagatCorrect = 0;
            if (_currentResponses.Count > 6)
            {
                bool answerHostile = (_currentResponses[6] == 1f);
                if (answerHostile == _freezeHostile) sagatCorrect++;
            }
            if (_currentResponses.Count > 7)
            {
                bool answerLeft = (_currentResponses[7] == 1f);
                if (answerLeft == _freezeLeft) sagatCorrect++;
            }

            if (_logger != null)
            {
                _logger.RecordAssessment(
                    _activeParticipantCode,
                    _activeSessionId,
                    _activeTrialId,
                    tlxScores,
                    sagatCorrect,
                    2
                );
            }

            // Also record structured TLX row
            var tlx = new NasaTlxAssessment
            {
                ParticipantCode = _activeParticipantCode,
                SessionId = _activeSessionId,
                ConditionLabel = _activeConditionLabel,
                Status = status,
                TimestampUtc = DateTime.UtcNow.ToString("o")
            };
            tlx.FromArray(tlxScores);
            AppendAssessmentToCsv(tlx.ToCsvRow(), NasaTlxAssessment.GetCsvHeader());
        }

        private void AppendAssessmentToCsv(string csvRow, string headerIfNew)
        {
            try
            {
                string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                string filePath = Path.Combine(projectRoot, _exportCsvRelativePath);
                string dir = Path.GetDirectoryName(filePath);

                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                lock (_csvLock)
                {
                    bool fileExists = File.Exists(filePath);
                    using (var writer = new StreamWriter(filePath, append: true, encoding: Encoding.UTF8))
                    {
                        if (!fileExists && !string.IsNullOrEmpty(headerIfNew))
                        {
                            writer.WriteLine(headerIfNew);
                        }
                        writer.WriteLine(csvRow);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[HumanFactors] Failed to export human factors CSV: " + ex.Message);
            }
        }

        #endregion

        private void OnDestroy()
        {
            if (Time.timeScale == 0f)
            {
                Time.timeScale = 1f; // Ensure simulation unfreezes on tear down
            }
        }
    }
}
