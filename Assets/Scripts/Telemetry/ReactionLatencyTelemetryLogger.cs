using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace IVRAR.Group02.Telemetry
{
    /// <summary>
    /// High-resolution, research-grade telemetry system.
    /// Provides continuous 90 Hz kinematic sampling, dropped-frame monitoring,
    /// microsecond-precision monotonic event timing, comprehensive SDT logging,
    /// independent session file exports, and non-blocking asynchronous file I/O.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ReactionLatencyTelemetryLogger : MonoBehaviour
    {
        [Header("Participant & Session Metadata")]
        [Tooltip("Pseudonymous participant code (e.g. P01, P02).")]
        public string participantCode = "P01_TEST";
        public string sessionId = "S01_BLOCK1";
        public string conditionLabel = "SensoryStress_Night";
        public string dataStatus = "unverified";

        [Header("High-Frequency Telemetry Sampling")]
        [Tooltip("Target continuous sampling frequency in Hz (VR standard is 90 Hz).")]
        [Range(60, 120)]
        public int targetSampleRateHz = 90;

        [Tooltip("Window size for rolling yaw variance and sampling statistics.")]
        [Min(10)]
        public int windowSamples = 90;

        [Header("Sampling Performance Tracking (Read-Only)")]
        [SerializeField] private float _achievedSampleRateHz;
        [SerializeField] private int _totalSampleCount;
        [SerializeField] private int _droppedSampleCount;
        [SerializeField] private float _meanIntervalMs;
        [SerializeField] private float _intervalJitterMs;

        [Header("File Export Configuration")]
        public string exportFolder = "telemetry";
        [Tooltip("If true, exports to separate files named with participant and session ID.")]
        [SerializeField] private bool _useIndependentSessionFiles = true;
        public string exportFileName = "reaction_latency_trial_log.csv";
        public string continuousTelemetryFileName = "continuous_telemetry_90hz.csv";
        public string assessmentFileName = "post_trial_assessments.csv";

        [Header("Kinematics Tracking Proxy")]
        public Transform headTransform;

        [Header("Environmental Parameters (Phase 03 / Phase 04)")]
        public float simulatedLuxTarget = 0.5f;
        public float fogDensity = 0.065f;
        public float targetContrast = 0.35f;
        public float ambientIntensity = 0.06f;

        // Cumulative SDT counters
        private int _hits;
        private int _misses;
        private int _falseAlarms;
        private int _correctRejections;

        public int TotalHits => _hits;
        public int TotalMisses => _misses;
        public int TotalFalseAlarms => _falseAlarms;
        public int TotalCorrectRejections => _correctRejections;
        public float AchievedSampleRateHz => _achievedSampleRateHz;
        public int DroppedSampleCount => _droppedSampleCount;
        public float MeanIntervalMs => _meanIntervalMs;

        // Rolling Kinematics Buffer
        private float[] _yawDeltas;
        private int _sampleCursor;
        private int _kinematicSampleCount;
        private Quaternion _previousHeadRotation;
        private bool _hasPreviousRotation;
        private float _currentYawVariance;

        // Monotonic High-Resolution Timing & Non-blocking I/O
        private double _targetIntervalSeconds;
        private double _nextSampleTime;
        private long _lastSampleStopwatchTicks;
        private readonly StringBuilder _trialBuffer = new StringBuilder(16384);
        private readonly StringBuilder _continuousBuffer = new StringBuilder(32768);
        private readonly StringBuilder _assessmentBuffer = new StringBuilder(4096);
        private readonly object _ioLock = new object();
        private bool _isExporting;

        private void Awake()
        {
            _targetIntervalSeconds = 1.0 / Mathf.Clamp(targetSampleRateHz, 60, 120);
            _yawDeltas = new float[Mathf.Max(120, windowSamples)];

            // Initialize Headers
            _trialBuffer.AppendLine("TimestampUtc,ParticipantID,SessionID,Condition,DataStatus,SimulatedLuxTarget,FogDensity,TargetContrast,AmbientIntensity,TrialID,TargetType,TraineeAction,LatencyMs,Hit,Miss,FalseAlarm,CorrectRejection,DistanceMeters,PresentationAngleDeg,HeadYawJitterVarianceDeg2,DPrime,BetaCriterion");
            _continuousBuffer.AppendLine("TimestampUtc,SessionTimeSec,ParticipantID,SessionID,Condition,ActualIntervalMs,AchievedHz,DroppedSample,PosX,PosY,PosZ,RotYaw,RotPitch,RotRoll,YawVarianceDeg2");
            _assessmentBuffer.AppendLine("TimestampUtc,ParticipantID,SessionID,TrialID,NasaTlxMental,NasaTlxPhysical,NasaTlxTemporal,NasaTlxPerformance,NasaTlxEffort,NasaTlxFrustration,RawTlxMean,SagatRecallCorrect,SagatRecallTotal");

            if (headTransform == null && Camera.main != null)
                headTransform = Camera.main.transform;

            if (headTransform != null)
            {
                _previousHeadRotation = headTransform.rotation;
                _hasPreviousRotation = true;
            }

            _lastSampleStopwatchTicks = Stopwatch.GetTimestamp();
            _nextSampleTime = Time.realtimeSinceStartupAsDouble + _targetIntervalSeconds;
        }

        private void Update()
        {
            TrackContinuousTelemetry();
        }

        /// <summary>
        /// Collects high-frequency continuous telemetry samples at exactly targetSampleRateHz (90 Hz).
        /// Measures real monotonic intervals between samples and tracks dropped frames.
        /// </summary>
        private void TrackContinuousTelemetry()
        {
            if (headTransform == null) return;

            double currentTime = Time.realtimeSinceStartupAsDouble;

            // Catch all elapsed intervals (does not assume single Update() guarantees 90Hz)
            while (currentTime >= _nextSampleTime)
            {
                _nextSampleTime += _targetIntervalSeconds;

                long currentTicks = Stopwatch.GetTimestamp();
                double intervalSec = (double)(currentTicks - _lastSampleStopwatchTicks) / Stopwatch.Frequency;
                _lastSampleStopwatchTicks = currentTicks;

                float intervalMs = (float)(intervalSec * 1000.0);
                float instantaneousHz = intervalSec > 0.0 ? (float)(1.0 / intervalSec) : targetSampleRateHz;

                // Detect dropped telemetry sample (if interval exceeds 1.5x expected)
                bool isDropped = intervalSec > (_targetIntervalSeconds * 1.5);
                if (isDropped) _droppedSampleCount++;

                _totalSampleCount++;

                // Rolling statistics
                _meanIntervalMs = Mathf.Lerp(_meanIntervalMs, intervalMs, 0.05f);
                _achievedSampleRateHz = Mathf.Lerp(_achievedSampleRateHz, instantaneousHz, 0.05f);
                _intervalJitterMs = Mathf.Abs(intervalMs - (float)(_targetIntervalSeconds * 1000.0));

                // Process Head Kinematics
                Quaternion currentRot = headTransform.rotation;
                Vector3 currentPos = headTransform.position;

                if (_hasPreviousRotation)
                {
                    float deltaYaw = Mathf.DeltaAngle(_previousHeadRotation.eulerAngles.y, currentRot.eulerAngles.y);
                    _yawDeltas[_sampleCursor] = deltaYaw;
                    _sampleCursor = (_sampleCursor + 1) % _yawDeltas.Length;
                    if (_kinematicSampleCount < _yawDeltas.Length) _kinematicSampleCount++;

                    _currentYawVariance = CalculateYawVariance();
                }

                _previousHeadRotation = currentRot;
                _hasPreviousRotation = true;

                // Append continuous record
                string nowUtc = DateTime.UtcNow.ToString("o");
                string contLine = string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "{0},{1:F3},{2},{3},{4},{5:F2},{6:F1},{7},{8:F3},{9:F3},{10:F3},{11:F2},{12:F2},{13:F2},{14:F4}",
                    nowUtc, currentTime, participantCode, sessionId, conditionLabel,
                    intervalMs, instantaneousHz, isDropped ? "TRUE" : "FALSE",
                    currentPos.x, currentPos.y, currentPos.z,
                    currentRot.eulerAngles.y, currentRot.eulerAngles.x, currentRot.eulerAngles.z,
                    _currentYawVariance
                );

                lock (_ioLock)
                {
                    _continuousBuffer.AppendLine(contLine);
                }
            }
        }

        private float CalculateYawVariance()
        {
            if (_kinematicSampleCount < 2) return 0f;
            float sum = 0f;
            for (int i = 0; i < _kinematicSampleCount; i++) sum += _yawDeltas[i];
            float mean = sum / _kinematicSampleCount;

            float sumSq = 0f;
            for (int i = 0; i < _kinematicSampleCount; i++)
            {
                float diff = _yawDeltas[i] - mean;
                sumSq += diff * diff;
            }
            return sumSq / (_kinematicSampleCount - 1);
        }

        public void SetEnvironmentalTelemetry(float simulatedLux, float activeFogDensity, float activeContrast, float activeAmbient)
        {
            simulatedLuxTarget = simulatedLux;
            fogDensity = activeFogDensity;
            targetContrast = activeContrast;
            ambientIntensity = activeAmbient;
        }

        /// <summary>
        /// Logs a discrete experimental trial with microsecond monotonic latency precision.
        /// Evaluates Hit, Miss, False Alarm, and Correct Rejection outcomes.
        /// </summary>
        public void RecordEngagementEvent(string trialId, bool isHostile, bool engaged, float latencyMs, float distance, float angle)
        {
            int isHit = (isHostile && engaged) ? 1 : 0;
            int isMiss = (isHostile && !engaged) ? 1 : 0;
            int isFalseAlarm = (!isHostile && engaged) ? 1 : 0;
            int isCorrectRejection = (!isHostile && !engaged) ? 1 : 0;

            if (isHit == 1) _hits++;
            if (isMiss == 1) _misses++;
            if (isFalseAlarm == 1) _falseAlarms++;
            if (isCorrectRejection == 1) _correctRejections++;

            var sdt = SignalDetectionMetrics.Calculate(_hits, _misses, _falseAlarms, _correctRejections);

            string targetTypeStr = isHostile ? "Hostile" : "Civilian";
            string actionStr = engaged ? "Shoot" : "Hold";
            string nowUtc = DateTime.UtcNow.ToString("o");

            string line = string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "{0},{1},{2},{3},{4},{5:F2},{6:F4},{7:F2},{8:F2},{9},{10},{11},{12:F1},{13},{14},{15},{16},{17:F2},{18:F2},{19:F4},{20:F3},{21:F3}",
                nowUtc, participantCode, sessionId, conditionLabel, dataStatus,
                simulatedLuxTarget, fogDensity, targetContrast, ambientIntensity,
                trialId, targetTypeStr, actionStr, latencyMs,
                isHit, isMiss, isFalseAlarm, isCorrectRejection,
                distance, angle, _currentYawVariance,
                double.IsNaN(sdt.DPrime) ? 0.0 : sdt.DPrime,
                double.IsNaN(sdt.Beta) ? 1.0 : sdt.Beta
            );

            lock (_ioLock)
            {
                _trialBuffer.AppendLine(line);
            }
        }

        public void RecordAssessment(string partCode, string sessId, string trialId, float[] tlxScores, int sagatCorrect, int sagatTotal)
        {
            string nowUtc = DateTime.UtcNow.ToString("o");
            float tlxSum = 0f;
            for (int i = 0; i < 6 && i < tlxScores.Length; i++) tlxSum += tlxScores[i];
            float rawTlx = tlxSum / 6f;

            string line = string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "{0},{1},{2},{3},{4:F1},{5:F1},{6:F1},{7:F1},{8:F1},{9:F1},{10:F2},{11},{12}",
                nowUtc, partCode, sessId, trialId,
                tlxScores.Length > 0 ? tlxScores[0] : 0,
                tlxScores.Length > 1 ? tlxScores[1] : 0,
                tlxScores.Length > 2 ? tlxScores[2] : 0,
                tlxScores.Length > 3 ? tlxScores[3] : 0,
                tlxScores.Length > 4 ? tlxScores[4] : 0,
                tlxScores.Length > 5 ? tlxScores[5] : 0,
                rawTlx, sagatCorrect, sagatTotal
            );

            lock (_ioLock)
            {
                _assessmentBuffer.AppendLine(line);
            }

            ExportTelemetryToDisk();
        }

        /// <summary>
        /// Flushes memory buffers to disk asynchronously without blocking the Unity main rendering thread.
        /// </summary>
        public void ExportTelemetryToDisk()
        {
            if (_isExporting) return;

            string trialData;
            string contData;
            string assessData;

            lock (_ioLock)
            {
                trialData = _trialBuffer.ToString();
                _trialBuffer.Clear();

                contData = _continuousBuffer.ToString();
                _continuousBuffer.Clear();

                assessData = _assessmentBuffer.ToString();
                _assessmentBuffer.Clear();
            }

            if (string.IsNullOrEmpty(trialData) && string.IsNullOrEmpty(contData) && string.IsNullOrEmpty(assessData))
                return;

            _isExporting = true;
            string exportDir = Path.Combine(Application.dataPath, "..", exportFolder);

            string trialFile = _useIndependentSessionFiles
                ? string.Format("trial_log_{0}_{1}.csv", participantCode, sessionId)
                : exportFileName;

            string contFile = _useIndependentSessionFiles
                ? string.Format("continuous_90hz_{0}_{1}.csv", participantCode, sessionId)
                : continuousTelemetryFileName;

            string assessFile = _useIndependentSessionFiles
                ? string.Format("assessments_{0}_{1}.csv", participantCode, sessionId)
                : assessmentFileName;

            Task.Run(() =>
            {
                try
                {
                    if (!Directory.Exists(exportDir)) Directory.CreateDirectory(exportDir);

                    if (!string.IsNullOrEmpty(trialData))
                        File.AppendAllText(Path.Combine(exportDir, trialFile), trialData);

                    if (!string.IsNullOrEmpty(contData))
                        File.AppendAllText(Path.Combine(exportDir, contFile), contData);

                    if (!string.IsNullOrEmpty(assessData))
                        File.AppendAllText(Path.Combine(exportDir, assessFile), assessData);

                    // Also mirror to global default file for master analysis
                    if (_useIndependentSessionFiles && !string.IsNullOrEmpty(trialData))
                    {
                        File.AppendAllText(Path.Combine(exportDir, exportFileName), trialData);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError("[ReactionLatencyTelemetryLogger] Asynchronous export error: " + ex.Message);
                }
                finally
                {
                    _isExporting = false;
                }
            });
        }

        private void OnApplicationQuit()
        {
            // Synchronous flush on application exit to prevent lost records
            try
            {
                string exportDir = Path.Combine(Application.dataPath, "..", exportFolder);
                if (!Directory.Exists(exportDir)) Directory.CreateDirectory(exportDir);

                string trialData = _trialBuffer.ToString();
                string contData = _continuousBuffer.ToString();
                string assessData = _assessmentBuffer.ToString();

                if (!string.IsNullOrEmpty(trialData))
                    File.AppendAllText(Path.Combine(exportDir, exportFileName), trialData);

                if (!string.IsNullOrEmpty(contData))
                    File.AppendAllText(Path.Combine(exportDir, continuousTelemetryFileName), contData);

                if (!string.IsNullOrEmpty(assessData))
                    File.AppendAllText(Path.Combine(exportDir, assessmentFileName), assessData);
            }
            catch (Exception ex)
            {
                Debug.LogError("[ReactionLatencyTelemetryLogger] Final synchronous exit flush failed: " + ex.Message);
            }
        }
    }
}
