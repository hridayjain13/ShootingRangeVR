using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using IVRAR.Group02.Core;
using IVRAR.Group02.Environment;
using IVRAR.Group02.SensoryStress;
using IVRAR.Group02.Telemetry;

namespace IVRAR.Group02.TargetManagement
{
    public enum TrialType
    {
        SignalPresent_Single,
        SignalAbsent_CatchTrial,
        MultiTarget_Prioritization
    }

    public enum DecisionOutcome
    {
        Hit,                // Threat present, trainee shot
        Miss,               // Threat present, trainee withheld / timed out
        FalseAlarm,         // Non-threat or signal absent, trainee shot
        CorrectRejection    // Non-threat or signal absent, trainee withheld / pressed no-signal
    }

    public enum TaskDifficultyTier
    {
        Easy,       // High contrast (1.0), 3.0s window, 2 stimuli types
        Medium,     // Medium contrast (0.65), 2.2s window, 3 stimuli types
        Hard        // Low contrast (0.35), 1.5s window, 4 stimuli types + prioritization
    }

    public enum ConditionOrderMode
    {
        Sequential,                 // Baseline -> Low Visibility -> Sensory Distraction -> Combined
        CounterbalancedLatinSquare, // Latin square counterbalancing across 4 participant groups
        Randomized                  // Random block ordering
    }

    [Serializable]
    public struct SignalDetectionTrialRecord
    {
        public string trialId;
        public string participantId;
        public string experimentalCondition;
        public int blockIndex;
        public TrialType trialType;
        public StimulusCategory primaryCategory;
        public string stimulusPresentationTimestampUtc;
        public string responseTimestampUtc;
        public float presentationLatencyMs;
        public string correctResponse;
        public string participantResponse;
        public DecisionOutcome outcome;
        public bool isTimeout;
        public float prioritizationScore;
        public bool isPrioritizationOrderCorrect;
        public float stimulusDistanceMeters;
        public float stimulusContrast;
    }

    /// <summary>
    /// Master trial manager for the VR Visual Signal-Detection and Threat Prioritization experiment.
    /// Manages 4-condition sequences (Baseline, Low Visibility, Sensory Distraction, Combined),
    /// counterbalanced block ordering, Signal-Present vs Signal-Absent trials, abstract geometric stimuli,
    /// threat prioritization scoring rubric, explicit No-Signal responses, and comprehensive CSV logging.
    /// </summary>
    [DisallowMultipleComponent]
    public class VisualSignalDetectionTrialManager : MonoBehaviour
    {
        [Header("Participant Metadata")]
        [SerializeField] private string _participantId = "P01_TEST";
        [SerializeField] private int _latinSquareGroupIndex = 0; // 0, 1, 2, or 3 for counterbalancing

        [Header("Experiment Sequence & Counterbalancing")]
        [SerializeField] private ConditionOrderMode _conditionOrderMode = ConditionOrderMode.Sequential;
        [SerializeField] private TaskDifficultyTier _difficultyTier = TaskDifficultyTier.Medium;

        [Header("Trial Quantities per Block")]
        [SerializeField, Range(4, 50)] private int _trialsPerBlock = 12;
        [SerializeField, Range(0.1f, 0.4f)] private float _signalAbsentCatchTrialRatio = 0.25f;
        [SerializeField, Range(0.1f, 0.5f)] private float _prioritizationTrialRatio = 0.25f;

        [Header("Deterministic Seed Configuration")]
        [SerializeField] private bool _useDeterministicSeed = true;
        [SerializeField] private int _randomSeed = 1002;

        [Header("Spawn Anchors (Corridor Distance)")]
        [SerializeField] private Transform[] _spawnAnchors; // Near (3m), Mid (8m), Far (14m)

        [Header("Stimulus Prefab / Template")]
        [SerializeField] private GameObject _stimulusTemplatePrefab;

        [Header("Subsystem References")]
        [SerializeField] private LowVisibilityEnvironmentManager _environmentManager;
        [SerializeField] private IVRAR.Group02.SensoryStress.SensoryStressTargetManager _distractionManager;
        [SerializeField] private ReactionLatencyTelemetryLogger _telemetryLogger;

        [Header("Telemetry Log Export")]
        [SerializeField] private string _exportDirectory = "telemetry";
        [SerializeField] private string _logFileName = "visual_signal_detection_trial_log.csv";

        [Header("Keyboard Controls")]
        [SerializeField] private KeyCode _noSignalKey = KeyCode.Q;
        [SerializeField] private KeyCode _altNoSignalKey = KeyCode.X;

        public event Action<SignalDetectionTrialRecord> OnTrialCompleted;
        public event Action OnBlockCompleted;
        public event Action OnExperimentCompleted;

        private System.Random _random;
        private List<string> _blockSequence;
        private int _currentBlockIndex;
        private int _currentTrialIndexInBlock;
        private bool _isSequenceRunning;
        private Coroutine _sequenceCoroutine;

        private readonly List<SignalDetectionTrialRecord> _recordedTrials = new List<SignalDetectionTrialRecord>();
        private readonly List<AbstractVisualStimulus> _activeStimuli = new List<AbstractVisualStimulus>();
        private readonly List<int> _prioritizationEngagementHistory = new List<int>();
        private readonly StringBuilder _csvBuffer = new StringBuilder(8192);

        private bool _currentTrialAwaitingDecision;
        private double _currentTrialSpawnTime;
        private string _currentTrialId;
        private TrialType _currentTrialType;
        private StimulusCategory _currentPrimaryCategory;
        private float _currentTrialExposureLimit;
        private float _currentStimulusDistance;
        private float _currentStimulusContrast;
        private bool _currentTrialExplicitNoSignalFired;

        public bool IsSequenceRunning => _isSequenceRunning;
        public int CurrentBlockIndex => _currentBlockIndex;
        public int CurrentTrialIndex => _currentTrialIndexInBlock;
        public IReadOnlyList<SignalDetectionTrialRecord> RecordedTrials => _recordedTrials;

        private void Awake()
        {
            if (_environmentManager == null) _environmentManager = FindObjectOfType<LowVisibilityEnvironmentManager>();
            if (_distractionManager == null) _distractionManager = FindObjectOfType<IVRAR.Group02.SensoryStress.SensoryStressTargetManager>();
            if (_telemetryLogger == null) _telemetryLogger = FindObjectOfType<ReactionLatencyTelemetryLogger>();

            EnsureSpawnAnchors();
            InitializeRandomSeed();
            BuildConditionSequence();

            _csvBuffer.AppendLine("TrialID,ParticipantID,ExperimentalCondition,BlockIndex,TrialType,PrimaryCategory,StimulusOnsetUtc,ResponseOnsetUtc,LatencyMs,CorrectResponse,ParticipantResponse,Outcome,IsTimeout,PrioritizationScore,IsOrderCorrect,DistanceMeters,Contrast");
        }

        private void Update()
        {
            // Explicit No-Signal response shortcut
            if (_currentTrialAwaitingDecision)
            {
                if (Input.GetKeyDown(_noSignalKey) || Input.GetKeyDown(_altNoSignalKey))
                {
                    RegisterNoSignalDecision();
                }
            }
        }

        private void EnsureSpawnAnchors()
        {
            if (_spawnAnchors == null || _spawnAnchors.Length == 0)
            {
                // Create fallback near (3m), mid (8m), far (14m) anchors
                _spawnAnchors = new Transform[3];
                for (int i = 0; i < 3; i++)
                {
                    float dist = (i == 0) ? 3.5f : ((i == 1) ? 8.0f : 14.0f);
                    var anchorGo = new GameObject("Anchor_" + dist + "m");
                    anchorGo.transform.position = new Vector3((i - 1) * 1.5f, 1.4f, dist);
                    _spawnAnchors[i] = anchorGo.transform;
                }
            }
        }

        public void InitializeRandomSeed()
        {
            _random = _useDeterministicSeed ? new System.Random(_randomSeed) : new System.Random();
        }

        /// <summary>
        /// Generates the 4-block experimental sequence:
        /// Baseline -> Low Visibility -> Sensory Distraction -> Combined Condition.
        /// Supports counterbalanced Latin Square ordering to mitigate practice effects.
        /// </summary>
        public void BuildConditionSequence()
        {
            string[] baseConditions = new string[]
            {
                "Baseline_Daylight",
                "LowVisibility_Night",
                "SensoryDistraction_Daylight",
                "Combined_LowVis_SensoryStress"
            };

            _blockSequence = new List<string>();

            switch (_conditionOrderMode)
            {
                case ConditionOrderMode.Sequential:
                    _blockSequence.AddRange(baseConditions);
                    break;

                case ConditionOrderMode.CounterbalancedLatinSquare:
                    // 4x4 Balanced Latin Square
                    int shift = Mathf.Abs(_latinSquareGroupIndex) % 4;
                    for (int i = 0; i < 4; i++)
                    {
                        _blockSequence.Add(baseConditions[(i + shift) % 4]);
                    }
                    break;

                case ConditionOrderMode.Randomized:
                    var list = new List<string>(baseConditions);
                    while (list.Count > 0)
                    {
                        int idx = _random.Next(list.Count);
                        _blockSequence.Add(list[idx]);
                        list.RemoveAt(idx);
                    }
                    break;
            }
        }

        public void StartFullExperiment()
        {
            if (_isSequenceRunning) return;
            _isSequenceRunning = true;
            _currentBlockIndex = 0;
            _sequenceCoroutine = StartCoroutine(ExperimentExecutionLoop());
        }

        public void StopExperiment()
        {
            _isSequenceRunning = false;
            if (_sequenceCoroutine != null) StopCoroutine(_sequenceCoroutine);
            ClearActiveStimuli();
            FlushTelemetryLog();
        }

        private IEnumerator ExperimentExecutionLoop()
        {
            for (_currentBlockIndex = 0; _currentBlockIndex < _blockSequence.Count; _currentBlockIndex++)
            {
                string activeConditionName = _blockSequence[_currentBlockIndex];
                ApplyConditionEnvironment(activeConditionName);

                Debug.Log(string.Format("[VisualSignalDetectionTrialManager] Starting Block {0}/{1}: {2}",
                    _currentBlockIndex + 1, _blockSequence.Count, activeConditionName));

                // Execute trials in block
                for (_currentTrialIndexInBlock = 0; _currentTrialIndexInBlock < _trialsPerBlock; _currentTrialIndexInBlock++)
                {
                    yield return StartCoroutine(ExecuteSingleTrial(_currentBlockIndex, _currentTrialIndexInBlock, activeConditionName));
                    yield return new WaitForSeconds((float)(1.5 + _random.NextDouble() * 1.5)); // Inter-trial interval
                }

                OnBlockCompleted?.Invoke();
                yield return new WaitForSeconds(3.0f); // Rest pause between experimental blocks
            }

            _isSequenceRunning = false;
            FlushTelemetryLog();
            OnExperimentCompleted?.Invoke();
            Debug.Log("[VisualSignalDetectionTrialManager] Entire 4-block experiment sequence successfully completed!");
        }

        private void ApplyConditionEnvironment(string conditionName)
        {
            switch (conditionName)
            {
                case "Baseline_Daylight":
                    if (_environmentManager != null) _environmentManager.ApplyCondition(LowVisibilityCondition.ConditionA_Baseline);
                    if (_distractionManager != null) _distractionManager.SetCondition(SensoryDistractionCondition.Condition1_NoDistractionBaseline);
                    break;

                case "LowVisibility_Night":
                    if (_environmentManager != null) _environmentManager.ApplyCondition(LowVisibilityCondition.ConditionC_ReducedWithFog);
                    if (_distractionManager != null) _distractionManager.SetCondition(SensoryDistractionCondition.Condition1_NoDistractionBaseline);
                    break;

                case "SensoryDistraction_Daylight":
                    if (_environmentManager != null) _environmentManager.ApplyCondition(LowVisibilityCondition.ConditionA_Baseline);
                    if (_distractionManager != null) _distractionManager.SetCondition(SensoryDistractionCondition.Condition4_CombinedAuditoryVisual);
                    break;

                case "Combined_LowVis_SensoryStress":
                default:
                    if (_environmentManager != null) _environmentManager.ApplyCondition(LowVisibilityCondition.ConditionC_ReducedWithFog);
                    if (_distractionManager != null) _distractionManager.SetCondition(SensoryDistractionCondition.Condition4_CombinedAuditoryVisual);
                    break;
            }
        }

        private IEnumerator ExecuteSingleTrial(int blockIdx, int trialIdx, string conditionName)
        {
            ClearActiveStimuli();
            _prioritizationEngagementHistory.Clear();
            _currentTrialExplicitNoSignalFired = false;
            _currentTrialAwaitingDecision = true;

            _currentTrialId = string.Format("B{0:D2}_T{1:D3}", blockIdx + 1, trialIdx + 1);

            // Determine Trial Type: Signal-Present, Signal-Absent Catch Trial, or Multi-Target Prioritization
            double roll = _random.NextDouble();
            if (roll < _signalAbsentCatchTrialRatio)
            {
                _currentTrialType = TrialType.SignalAbsent_CatchTrial;
            }
            else if (roll < (_signalAbsentCatchTrialRatio + _prioritizationTrialRatio))
            {
                _currentTrialType = TrialType.MultiTarget_Prioritization;
            }
            else
            {
                _currentTrialType = TrialType.SignalPresent_Single;
            }

            // Determine Exposure Timeout based on difficulty
            switch (_difficultyTier)
            {
                case TaskDifficultyTier.Easy:
                    _currentTrialExposureLimit = 3.0f;
                    _currentStimulusContrast = 1.0f;
                    break;
                case TaskDifficultyTier.Medium:
                    _currentTrialExposureLimit = 2.2f;
                    _currentStimulusContrast = 0.65f;
                    break;
                case TaskDifficultyTier.Hard:
                    _currentTrialExposureLimit = 1.5f;
                    _currentStimulusContrast = 0.35f;
                    break;
            }

            // Select spawn anchor
            Transform anchor = _spawnAnchors[_random.Next(_spawnAnchors.Length)];
            _currentStimulusDistance = Vector3.Distance(Camera.main != null ? Camera.main.transform.position : Vector3.zero, anchor.position);

            string onsetUtc = DateTime.UtcNow.ToString("o");
            _currentTrialSpawnTime = Time.realtimeSinceStartupAsDouble;

            // Spawn Stimuli according to trial type
            if (_currentTrialType == TrialType.SignalAbsent_CatchTrial)
            {
                _currentPrimaryCategory = StimulusCategory.SignalAbsent_CatchTrial;
                // Signal absent: spawn empty noise marker or nothing
                SpawnStimulus(StimulusCategory.SignalAbsent_CatchTrial, anchor.position, _currentStimulusContrast);
            }
            else if (_currentTrialType == TrialType.SignalPresent_Single)
            {
                // 50% threat, 50% civilian/friendly
                bool isThreat = _random.NextDouble() < 0.5;
                _currentPrimaryCategory = isThreat
                    ? (_random.NextDouble() < 0.6 ? StimulusCategory.HighThreat_Tier1 : StimulusCategory.MediumThreat_Tier2)
                    : (_random.NextDouble() < 0.5 ? StimulusCategory.Neutral_Civilian : StimulusCategory.Friendly_Protected);

                SpawnStimulus(_currentPrimaryCategory, anchor.position, _currentStimulusContrast);
            }
            else // MultiTarget_Prioritization
            {
                _currentPrimaryCategory = StimulusCategory.HighThreat_Tier1;
                // Spawn High Threat, Medium Threat, and Civilian simultaneously across anchors
                if (_spawnAnchors.Length >= 3)
                {
                    SpawnStimulus(StimulusCategory.HighThreat_Tier1, _spawnAnchors[0].position, _currentStimulusContrast);
                    SpawnStimulus(StimulusCategory.Neutral_Civilian, _spawnAnchors[1].position, _currentStimulusContrast);
                    SpawnStimulus(StimulusCategory.MediumThreat_Tier2, _spawnAnchors[2].position, _currentStimulusContrast);
                }
            }

            // Notify distraction manager for trial sensory envelope
            if (_distractionManager != null)
            {
                _distractionManager.ScheduleDistractionForTrial(_currentTrialId, anchor.position);
            }

            // Await participant response or exposure timeout
            float elapsed = 0f;
            while (elapsed < _currentTrialExposureLimit && _currentTrialAwaitingDecision)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // If still awaiting decision after time limit, resolve as Timeout
            if (_currentTrialAwaitingDecision)
            {
                ResolveTrialOutcome(isTimeout: true, responseTimestampUtc: DateTime.UtcNow.ToString("o"), participantEngaged: false);
            }

            ClearActiveStimuli();
        }

        private AbstractVisualStimulus SpawnStimulus(StimulusCategory cat, Vector3 position, float contrast)
        {
            GameObject go;
            if (_stimulusTemplatePrefab != null)
            {
                go = Instantiate(_stimulusTemplatePrefab, position, Quaternion.identity);
            }
            else
            {
                go = new GameObject("Stimulus_" + cat);
                go.transform.position = position;
            }

            var stimulus = go.GetComponent<AbstractVisualStimulus>();
            if (stimulus == null) stimulus = go.AddComponent<AbstractVisualStimulus>();

            stimulus.Configure(cat, contrast);
            stimulus.OnStimulusEngaged += HandleStimulusEngaged;

            _activeStimuli.Add(stimulus);
            return stimulus;
        }

        private void HandleStimulusEngaged(AbstractVisualStimulus stimulus)
        {
            if (!_currentTrialAwaitingDecision) return;

            _prioritizationEngagementHistory.Add(stimulus.ThreatPriority);

            // In single trial, first shot completes the decision
            if (_currentTrialType != TrialType.MultiTarget_Prioritization)
            {
                ResolveTrialOutcome(isTimeout: false, responseTimestampUtc: DateTime.UtcNow.ToString("o"), participantEngaged: true);
            }
            else
            {
                // In prioritization trial, check if all threats are engaged or civilian hit
                if (stimulus.Category == StimulusCategory.Neutral_Civilian || stimulus.Category == StimulusCategory.Friendly_Protected)
                {
                    // Immediate civilian violation penalty
                    ResolveTrialOutcome(isTimeout: false, responseTimestampUtc: DateTime.UtcNow.ToString("o"), participantEngaged: true);
                }
                else if (_prioritizationEngagementHistory.Count >= 2)
                {
                    // Both threats engaged
                    ResolveTrialOutcome(isTimeout: false, responseTimestampUtc: DateTime.UtcNow.ToString("o"), participantEngaged: true);
                }
            }
        }

        /// <summary>
        /// Explicit participant No-Signal / Clear button response.
        /// Distinguishes deliberate correct rejection from passive timeout.
        /// </summary>
        public void RegisterNoSignalDecision()
        {
            if (!_currentTrialAwaitingDecision) return;
            _currentTrialExplicitNoSignalFired = true;

            ResolveTrialOutcome(isTimeout: false, responseTimestampUtc: DateTime.UtcNow.ToString("o"), participantEngaged: false);
        }

        private void ResolveTrialOutcome(bool isTimeout, string responseTimestampUtc, bool participantEngaged)
        {
            _currentTrialAwaitingDecision = false;
            double responseTime = Time.realtimeSinceStartupAsDouble;
            float latencyMs = Mathf.Max(0f, (float)((responseTime - _currentTrialSpawnTime) * 1000.0));

            DecisionOutcome outcome;
            string correctResponse;
            string participantResponseStr;
            float prioritizationScore = 0f;
            bool isOrderCorrect = false;

            // 1. Evaluate Decision Outcome
            if (_currentTrialType == TrialType.SignalAbsent_CatchTrial)
            {
                correctResponse = "Withhold_Or_NoSignal";
                if (participantEngaged)
                {
                    outcome = DecisionOutcome.FalseAlarm; // Shot at empty anchor
                    participantResponseStr = "Shoot";
                }
                else
                {
                    outcome = DecisionOutcome.CorrectRejection;
                    participantResponseStr = _currentTrialExplicitNoSignalFired ? "Explicit_NoSignal" : "Withhold_Timeout";
                }
            }
            else if (_currentTrialType == TrialType.SignalPresent_Single)
            {
                bool isThreat = (_currentPrimaryCategory == StimulusCategory.HighThreat_Tier1 ||
                                 _currentPrimaryCategory == StimulusCategory.MediumThreat_Tier2);

                correctResponse = isThreat ? "Shoot" : "Withhold_Or_NoSignal";

                if (isThreat)
                {
                    if (participantEngaged)
                    {
                        outcome = DecisionOutcome.Hit;
                        participantResponseStr = "Shoot";
                    }
                    else
                    {
                        outcome = DecisionOutcome.Miss;
                        participantResponseStr = _currentTrialExplicitNoSignalFired ? "Explicit_NoSignal" : "Withhold_Timeout";
                    }
                }
                else // Civilian or Friendly non-threat
                {
                    if (participantEngaged)
                    {
                        outcome = DecisionOutcome.FalseAlarm; // Shot non-combatant
                        participantResponseStr = "Shoot";
                    }
                    else
                    {
                        outcome = DecisionOutcome.CorrectRejection;
                        participantResponseStr = _currentTrialExplicitNoSignalFired ? "Explicit_NoSignal" : "Withhold_Timeout";
                    }
                }
            }
            else // MultiTarget_Prioritization
            {
                correctResponse = "Priority1_Then_Priority2";
                participantResponseStr = "Engaged_Priorities:" + string.Join("->", _prioritizationEngagementHistory);

                // Predefined Prioritization Scoring Rubric
                prioritizationScore = EvaluatePrioritizationRubric(out outcome, out isOrderCorrect, isTimeout);
            }

            // 2. Telemetry Record Creation
            string activeCondition = (_blockSequence != null && _currentBlockIndex < _blockSequence.Count)
                ? _blockSequence[_currentBlockIndex] : "DefaultCondition";

            var record = new SignalDetectionTrialRecord
            {
                trialId = _currentTrialId,
                participantId = _participantId,
                experimentalCondition = activeCondition,
                blockIndex = _currentBlockIndex + 1,
                trialType = _currentTrialType,
                primaryCategory = _currentPrimaryCategory,
                stimulusPresentationTimestampUtc = DateTime.UtcNow.ToString("o"),
                responseTimestampUtc = responseTimestampUtc,
                presentationLatencyMs = latencyMs,
                correctResponse = correctResponse,
                participantResponse = participantResponseStr,
                outcome = outcome,
                isTimeout = isTimeout,
                prioritizationScore = prioritizationScore,
                isPrioritizationOrderCorrect = isOrderCorrect,
                stimulusDistanceMeters = _currentStimulusDistance,
                stimulusContrast = _currentStimulusContrast
            };

            _recordedTrials.Add(record);
            AppendTrialToCsv(record);

            // Forward to standard telemetry logger
            if (_telemetryLogger != null)
            {
                bool isHostile = (_currentPrimaryCategory == StimulusCategory.HighThreat_Tier1 || _currentPrimaryCategory == StimulusCategory.MediumThreat_Tier2);
                _telemetryLogger.RecordEngagementEvent(record.trialId, isHostile, participantEngaged, latencyMs, _currentStimulusDistance, 0f);
            }

            OnTrialCompleted?.Invoke(record);

            Debug.Log(string.Format(
                "[VisualSignalDetectionTrialManager] Trial {0} [{1}] -> Outcome: {2} | RT: {3:F1}ms | Timeout: {4} | Prioritization: {5:F0} pts (OrderOK: {6})",
                record.trialId, record.trialType, record.outcome, record.presentationLatencyMs, record.isTimeout, record.prioritizationScore, record.isPrioritizationOrderCorrect
            ));
        }

        /// <summary>
        /// Predefined research scoring rubric for threat prioritization:
        /// +100 for Tier 1 first, +50 for Tier 2 second, -30 for inverted order, -100 for civilian engagement, -25 for timeout.
        /// </summary>
        public float EvaluatePrioritizationRubric(out DecisionOutcome outcome, out bool isOrderCorrect, bool isTimeout)
        {
            float score = 0f;
            isOrderCorrect = false;

            if (_prioritizationEngagementHistory.Contains(0)) // Hit civilian or friendly
            {
                score -= 100f;
                outcome = DecisionOutcome.FalseAlarm;
                return score;
            }

            if (isTimeout && _prioritizationEngagementHistory.Count == 0)
            {
                score -= 50f;
                outcome = DecisionOutcome.Miss;
                return score;
            }

            // Check order
            if (_prioritizationEngagementHistory.Count >= 2)
            {
                if (_prioritizationEngagementHistory[0] == 1 && _prioritizationEngagementHistory[1] == 2)
                {
                    // Perfect hierarchical prioritization
                    score += 150f; // 100 + 50
                    isOrderCorrect = true;
                    outcome = DecisionOutcome.Hit;
                }
                else if (_prioritizationEngagementHistory[0] == 2 && _prioritizationEngagementHistory[1] == 1)
                {
                    // Inverted order penalty
                    score += (150f - 30f); // 120 pts
                    isOrderCorrect = false;
                    outcome = DecisionOutcome.Hit;
                }
                else
                {
                    score += 50f;
                    outcome = DecisionOutcome.Hit;
                }
            }
            else if (_prioritizationEngagementHistory.Count == 1)
            {
                if (_prioritizationEngagementHistory[0] == 1) score += 100f;
                else score += 50f;
                outcome = DecisionOutcome.Hit;
            }
            else
            {
                outcome = DecisionOutcome.Miss;
            }

            // Reward for correctly withholding fire on neutral non-combatants (+50 pts)
            score += 50f;

            if (isTimeout) score -= 25f;

            return score;
        }

        private void AppendTrialToCsv(SignalDetectionTrialRecord r)
        {
            string line = string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "{0},{1},{2},{3},{4},{5},{6},{7},{8:F1},{9},{10},{11},{12},{13:F1},{14},{15:F2},{16:F2}",
                r.trialId, r.participantId, r.experimentalCondition, r.blockIndex,
                r.trialType, r.primaryCategory, r.stimulusPresentationTimestampUtc,
                r.responseTimestampUtc, r.presentationLatencyMs, r.correctResponse,
                r.participantResponse, r.outcome, r.isTimeout ? "TRUE" : "FALSE",
                r.prioritizationScore, r.isPrioritizationOrderCorrect ? "TRUE" : "FALSE",
                r.stimulusDistanceMeters, r.stimulusContrast
            );

            _csvBuffer.AppendLine(line);
        }

        public void FlushTelemetryLog()
        {
            try
            {
                string targetDir = Path.Combine(Application.dataPath, "..", _exportDirectory);
                if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

                string filePath = Path.Combine(targetDir, _logFileName);
                File.AppendAllText(filePath, _csvBuffer.ToString());
                _csvBuffer.Clear();

                Debug.Log("[VisualSignalDetectionTrialManager] Flushed trial records to: " + filePath);
            }
            catch (Exception ex)
            {
                Debug.LogError("[VisualSignalDetectionTrialManager] Error writing CSV: " + ex.Message);
            }
        }

        private void ClearActiveStimuli()
        {
            foreach (var s in _activeStimuli)
            {
                if (s != null) Destroy(s.gameObject);
            }
            _activeStimuli.Clear();
        }

        private void OnDestroy()
        {
            StopExperiment();
        }
    }
}
