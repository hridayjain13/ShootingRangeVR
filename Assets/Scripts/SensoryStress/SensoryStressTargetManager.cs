using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using IVRAR.Group02.Core;
using IVRAR.Group02.Telemetry;

namespace IVRAR.Group02.SensoryStress
{
    /// <summary>
    /// Experimental sensory distraction condition presets.
    /// </summary>
    public enum SensoryDistractionCondition
    {
        Condition1_NoDistractionBaseline,
        Condition2_ControlledAuditory,
        Condition3_ControlledPeripheralVisual,
        Condition4_CombinedAuditoryVisual
    }

    /// <summary>
    /// Visual distraction presentation modes with photosensitivity-safe alternatives.
    /// </summary>
    public enum VisualDistractionMode
    {
        GradualPeripheralGlare,     // Recommended / Photosensitivity-safe: smooth, gentle sinusoidal peripheral glow (<= 1.5 Hz)
        SubtleAmbientPulse,         // Non-flashing: soft breathing ambient lighting change
        ControlledLowFrequencyStrobe // Capped at <= 3.0 Hz to eliminate seizure risk under WCAG 2.1 / ISO 9241-391
    }

    /// <summary>
    /// Record of an individual scheduled sensory distraction event.
    /// </summary>
    [Serializable]
    public struct DistractionEventRecord
    {
        public string timestampUtc;
        public float sessionTimeSec;
        public string trialId;
        public SensoryDistractionCondition condition;
        public string eventType;
        public float scheduledDelaySec;
        public float actualOnsetTimeSec;
        public float durationSec;
        public float audioGain;
        public float visualIntensity;
        public bool isSafetyOverridden;
    }

    /// <summary>
    /// Manages controlled, reproducible sensory-stress distractors (auditory and peripheral visual)
    /// across four experimental conditions for defensive reaction research.
    /// Integrates deterministic seed scheduling, strict acoustic volume caps, photosensitivity-safe
    /// glare modes, event timestamp logging, participant comfort pauses, and immediate researcher safety overrides.
    /// </summary>
    [DisallowMultipleComponent]
    public class SensoryStressTargetManager : MonoBehaviour
    {
        [Header("Active Distraction Condition")]
        [SerializeField] private SensoryDistractionCondition _activeCondition = SensoryDistractionCondition.Condition4_CombinedAuditoryVisual;
        [SerializeField] private VisualDistractionMode _visualMode = VisualDistractionMode.GradualPeripheralGlare;

        [Header("Acoustic Intensity & Safety Controls")]
        [Tooltip("Digital audio gain (0.0 to 1.0). Conservative default is 0.25 (-12 dBFS). NOTE: Digital gain does NOT equal physical dB SPL. 85-95 dB requires sound meter calibration, IRB approval, and dosimetric monitoring.")]
        [Range(0.05f, 0.50f)]
        [SerializeField] private float _audioGain = 0.25f;

        [Tooltip("Hard safety ceiling on digital gain (audio will NEVER exceed this factor).")]
        [Range(0.10f, 0.60f)]
        [SerializeField] private float _absoluteAudioSafetyLimit = 0.40f;

        [Header("Peripheral Visual Settings")]
        [Tooltip("Peak visual glare intensity.")]
        [Range(0.05f, 0.40f)]
        [SerializeField] private float _visualIntensity = 0.20f;

        [Tooltip("Frequency in Hz for gradual peripheral glare or strobe. Capped strictly at 3.0 Hz for photosensitivity safety.")]
        [Range(0.5f, 3.0f)]
        [SerializeField] private float _visualFrequencyHz = 1.2f;

        [Header("Event Timing & Randomization")]
        [Tooltip("Minimum delay after trial onset before distraction fires.")]
        [Range(0.1f, 2.0f)]
        [SerializeField] private float _minOnsetDelaySec = 0.3f;

        [Tooltip("Maximum delay after trial onset before distraction fires.")]
        [Range(0.5f, 3.0f)]
        [SerializeField] private float _maxOnsetDelaySec = 1.2f;

        [Tooltip("Duration of distraction cue in seconds.")]
        [Range(0.1f, 1.5f)]
        [SerializeField] private float _eventDurationSec = 0.5f;

        [Header("Deterministic Reproducibility")]
        [SerializeField] private bool _useDeterministicSeed = true;
        [SerializeField] private int _randomSeed = 42;

        [Header("Subsystem References")]
        [SerializeField] private AcousticStressController _acousticController;
        [SerializeField] private StrobeStressController _strobeController;
        [SerializeField] private Light _peripheralGlareLight;
        [SerializeField] private ReactionLatencyTelemetryLogger _telemetryLogger;

        [Header("Telemetry Log Export")]
        [SerializeField] private string _exportDirectory = "telemetry";
        [SerializeField] private string _logFileName = "sensory_distraction_event_log.csv";

        [Header("Safety Hotkeys")]
        [SerializeField] private KeyCode _safetyOverrideKey = KeyCode.F9;

        private System.Random _random;
        private bool _isSafetyOverridden;
        private bool _isPaused;
        private Coroutine _activeDistractionRoutine;
        private Coroutine _visualEffectRoutine;
        private readonly List<DistractionEventRecord> _eventHistory = new List<DistractionEventRecord>();
        private readonly StringBuilder _csvBuffer = new StringBuilder(4096);
        private float _sessionStartTime;

        public SensoryDistractionCondition ActiveCondition => _activeCondition;
        public VisualDistractionMode VisualMode => _visualMode;
        public float AudioGain => _audioGain;
        public float VisualIntensity => _visualIntensity;
        public bool IsSafetyOverridden => _isSafetyOverridden;
        public bool IsPaused => _isPaused;
        public IReadOnlyList<DistractionEventRecord> EventHistory => _eventHistory;

        private void Awake()
        {
            if (_acousticController == null) _acousticController = FindObjectOfType<AcousticStressController>();
            if (_strobeController == null) _strobeController = FindObjectOfType<StrobeStressController>();
            if (_telemetryLogger == null) _telemetryLogger = FindObjectOfType<ReactionLatencyTelemetryLogger>();

            if (_peripheralGlareLight == null)
            {
                // Create dedicated peripheral glare light if not assigned
                var glareObj = new GameObject("PeripheralGlareLight_Safe");
                glareObj.transform.SetParent(transform);
                glareObj.transform.localPosition = new Vector3(1.5f, 1.5f, 1.0f);
                _peripheralGlareLight = glareObj.AddComponent<Light>();
                _peripheralGlareLight.type = LightType.Point;
                _peripheralGlareLight.range = 10f;
                _peripheralGlareLight.color = new Color(0.9f, 0.95f, 1.0f);
                _peripheralGlareLight.intensity = 0f;
                _peripheralGlareLight.enabled = false;
            }

            _sessionStartTime = Time.time;
            InitializeRandomSeed();

            _csvBuffer.AppendLine("TimestampUtc,ParticipantID,SessionID,Condition,TrialID,EventType,ScheduledDelaySec,ActualOnsetSec,DurationSec,AudioGain,VisualIntensity,SafetyOverridden");
        }

        private void Update()
        {
            // Researcher Emergency Safety Stop
            if (Input.GetKeyDown(_safetyOverrideKey))
            {
                TriggerResearcherSafetyOverride();
            }
        }

        public void InitializeRandomSeed()
        {
            _random = _useDeterministicSeed ? new System.Random(_randomSeed) : new System.Random();
        }

        /// <summary>
        /// Configures the active experimental condition (1, 2, 3, or 4).
        /// </summary>
        public void SetCondition(SensoryDistractionCondition condition)
        {
            _activeCondition = condition;
            Debug.Log("[SensoryStressTargetManager] Active Condition set to: " + condition);
        }

        /// <summary>
        /// Schedules a sensory distraction envelope for an active trial.
        /// Events are scheduled independently of participant performance.
        /// </summary>
        public void ScheduleDistractionForTrial(string trialId, Vector3 targetWorldPosition)
        {
            if (_activeCondition == SensoryDistractionCondition.Condition1_NoDistractionBaseline)
            {
                return; // Baseline: no distractions scheduled
            }

            if (_isSafetyOverridden || _isPaused) return;

            if (_activeDistractionRoutine != null)
            {
                StopCoroutine(_activeDistractionRoutine);
            }

            // Calculate deterministic or randomized onset delay within trial window
            float delay = (float)(_minOnsetDelaySec + _random.NextDouble() * (_maxOnsetDelaySec - _minOnsetDelaySec));
            _activeDistractionRoutine = StartCoroutine(DistractionSequence(trialId, targetWorldPosition, delay));
        }

        private IEnumerator DistractionSequence(string trialId, Vector3 targetPos, float delay)
        {
            float trialStartTime = Time.time;
            yield return new WaitForSeconds(delay);

            if (_isSafetyOverridden || _isPaused) yield break;

            float actualOnsetTime = Time.time - trialStartTime;
            float sessionElapsed = Time.time - _sessionStartTime;
            string nowUtc = DateTime.UtcNow.ToString("o");

            bool fireAudio = (_activeCondition == SensoryDistractionCondition.Condition2_ControlledAuditory ||
                              _activeCondition == SensoryDistractionCondition.Condition4_CombinedAuditoryVisual);

            bool fireVisual = (_activeCondition == SensoryDistractionCondition.Condition3_ControlledPeripheralVisual ||
                               _activeCondition == SensoryDistractionCondition.Condition4_CombinedAuditoryVisual);

            string eventType = fireAudio && fireVisual ? "Combined" : (fireAudio ? "Auditory" : "Visual");

            // 1. Fire Auditory Cue with conservative gain capping
            float appliedAudioGain = 0f;
            if (fireAudio && _acousticController != null)
            {
                appliedAudioGain = Mathf.Min(_audioGain, _absoluteAudioSafetyLimit);
                bool gunshot = _random.NextDouble() < 0.6;
                if (gunshot)
                {
                    _acousticController.PlayGunshotCue(appliedAudioGain, targetPos);
                }
                else
                {
                    _acousticController.PlayAlarmCue(appliedAudioGain, targetPos);
                }
            }

            // 2. Fire Peripheral Visual Distraction (Photosensitivity-Safe)
            float appliedVisualIntensity = 0f;
            if (fireVisual)
            {
                appliedVisualIntensity = _visualIntensity;
                TriggerVisualDistraction(_visualMode, _visualFrequencyHz, _eventDurationSec, appliedVisualIntensity);
            }

            // 3. Log Event Telemetry
            var record = new DistractionEventRecord
            {
                timestampUtc = nowUtc,
                sessionTimeSec = sessionElapsed,
                trialId = trialId,
                condition = _activeCondition,
                eventType = eventType,
                scheduledDelaySec = delay,
                actualOnsetTimeSec = actualOnsetTime,
                durationSec = _eventDurationSec,
                audioGain = appliedAudioGain,
                visualIntensity = appliedVisualIntensity,
                isSafetyOverridden = _isSafetyOverridden
            };

            _eventHistory.Add(record);
            AppendEventToCsv(record);

            yield return new WaitForSeconds(_eventDurationSec);
        }

        private void TriggerVisualDistraction(VisualDistractionMode mode, float freqHz, float duration, float intensity)
        {
            if (_visualEffectRoutine != null) StopCoroutine(_visualEffectRoutine);

            switch (mode)
            {
                case VisualDistractionMode.GradualPeripheralGlare:
                    _visualEffectRoutine = StartCoroutine(GradualGlareRoutine(freqHz, duration, intensity));
                    break;
                case VisualDistractionMode.SubtleAmbientPulse:
                    _visualEffectRoutine = StartCoroutine(AmbientPulseRoutine(duration, intensity));
                    break;
                case VisualDistractionMode.ControlledLowFrequencyStrobe:
                    if (_strobeController != null)
                    {
                        float safeFreq = Mathf.Min(freqHz, 3.0f); // Strictly <= 3.0 Hz
                        int pulses = Mathf.Clamp(Mathf.RoundToInt(duration * safeFreq), 1, 3);
                        _strobeController.TriggerStrobeBurst(safeFreq, pulses, intensity);
                    }
                    break;
            }
        }

        private IEnumerator GradualGlareRoutine(float frequency, float duration, float peakIntensity)
        {
            if (_peripheralGlareLight == null) yield break;

            _peripheralGlareLight.enabled = true;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (_isSafetyOverridden || _isPaused) break;

                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                // Smooth sinusoidal bell-curve pulse: no sudden flashing edges
                float sineWave = Mathf.Sin(progress * Mathf.PI);
                _peripheralGlareLight.intensity = sineWave * peakIntensity;
                yield return null;
            }

            _peripheralGlareLight.intensity = 0f;
            _peripheralGlareLight.enabled = false;
        }

        private IEnumerator AmbientPulseRoutine(float duration, float peakIntensity)
        {
            if (_peripheralGlareLight == null) yield break;

            _peripheralGlareLight.enabled = true;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (_isSafetyOverridden || _isPaused) break;

                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                // Very gentle linear rise and decay
                float factor = (progress < 0.5f) ? (progress * 2f) : ((1f - progress) * 2f);
                _peripheralGlareLight.intensity = factor * peakIntensity * 0.75f;
                yield return null;
            }

            _peripheralGlareLight.intensity = 0f;
            _peripheralGlareLight.enabled = false;
        }

        /// <summary>
        /// Researcher safety override. Immediately silences audio, shuts off lights, and aborts stressors.
        /// </summary>
        public void TriggerResearcherSafetyOverride()
        {
            _isSafetyOverridden = true;
            StopAllDistractions();

            Debug.LogWarning("[SensoryStressTargetManager] SAFETY OVERRIDE TRIGGERED. All sensory stressors disabled immediately.");
            FlushTelemetryLog();
        }

        /// <summary>
        /// Resets the safety override state after inspection.
        /// </summary>
        public void ResetSafetyOverride()
        {
            _isSafetyOverridden = false;
            Debug.Log("[SensoryStressTargetManager] Safety override reset. Ready for trials.");
        }

        /// <summary>
        /// Participant-controlled pause.
        /// </summary>
        public void PauseDistraction()
        {
            _isPaused = true;
            StopAllDistractions();
        }

        /// <summary>
        /// Resumes distraction scheduling.
        /// </summary>
        public void ResumeDistraction()
        {
            _isPaused = false;
        }

        /// <summary>
        /// Halts all active audio, visual coroutines, and lights immediately.
        /// </summary>
        public void StopAllDistractions()
        {
            if (_activeDistractionRoutine != null) StopCoroutine(_activeDistractionRoutine);
            if (_visualEffectRoutine != null) StopCoroutine(_visualEffectRoutine);

            if (_acousticController != null) _acousticController.StopAllAudio();
            if (_strobeController != null) _strobeController.StopStrobe();

            if (_peripheralGlareLight != null)
            {
                _peripheralGlareLight.intensity = 0f;
                _peripheralGlareLight.enabled = false;
            }
        }

        private void AppendEventToCsv(DistractionEventRecord r)
        {
            string participant = (_telemetryLogger != null) ? _telemetryLogger.participantCode : "P01_TEST";
            string session = (_telemetryLogger != null) ? _telemetryLogger.sessionId : "S01_BLOCK1";

            string line = string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "{0},{1},{2},{3},{4},{5},{6:F3},{7:F3},{8:F3},{9:F2},{10:F2},{11}",
                r.timestampUtc, participant, session, r.condition, r.trialId,
                r.eventType, r.scheduledDelaySec, r.actualOnsetTimeSec, r.durationSec,
                r.audioGain, r.visualIntensity, r.isSafetyOverridden ? "TRUE" : "FALSE"
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

                Debug.Log("[SensoryStressTargetManager] Flushed distraction events to: " + filePath);
            }
            catch (Exception ex)
            {
                Debug.LogError("[SensoryStressTargetManager] Error flushing telemetry: " + ex.Message);
            }
        }

        private void OnApplicationQuit()
        {
            StopAllDistractions();
            FlushTelemetryLog();
        }

        private void OnDisable()
        {
            StopAllDistractions();
        }
    }
}
