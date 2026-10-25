using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace IVRAR.Group02.Telemetry
{
    /// <summary>
    /// High-precision millisecond telemetry logger for shoot/don't-shoot reaction time,
    /// target categorization accuracy, and head kinematics jitter.
    /// Exports empirical trial logs to disk in standard CSV format for statistical analysis.
    /// </summary>
    public sealed class ReactionLatencyTelemetryLogger : MonoBehaviour
    {
        [Header("Participant Metadata")]
        [Tooltip("Pseudonymous participant ID (e.g., P01, P02). Do not use direct identifiers.")]
        public string participantCode = "P01_TEST";
        public string sessionId = "S01_BLOCK1";
        public string conditionLabel = "SensoryStress_Night";
        public string dataStatus = "unverified";

        [Header("File Export Configuration")]
        public string exportFolder = "telemetry";
        public string exportFileName = "reaction_latency_trial_log.csv";
        public string assessmentFileName = "post_trial_assessments.csv";

        [Header("Kinematics & Head Motion Proxy")]
        public Transform headTransform;
        [Range(60, 120)] public int sampleRateHz = 90;
        [Min(10)] public int windowSamples = 90;

        private readonly StringBuilder _trialBuffer = new StringBuilder(16384);
        private readonly StringBuilder _assessmentBuffer = new StringBuilder(4096);

        private float[] _yawDeltas;
        private int _sampleCursor;
        private int _sampleCount;
        private float _sampleAccumulator;
        private Quaternion _previousHeadRotation;
        private bool _hasPreviousRotation;
        private float _currentYawVariance;

        private int _hits;
        private int _misses;
        private int _falseAlarms;
        private int _correctRejections;

        public int TotalHits => _hits;
        public int TotalMisses => _misses;
        public int TotalFalseAlarms => _falseAlarms;
        public int TotalCorrectRejections => _correctRejections;

        [Header("Environmental Parameters (Phase 03)")]
        [Tooltip("Simulated illuminance target in lux.")]
        public float simulatedLuxTarget = 0.5f;
        [Tooltip("Active fog density extinction coefficient.")]
        public float fogDensity = 0.065f;
        [Tooltip("Target luminance contrast multiplier.")]
        public float targetContrast = 0.35f;
        [Tooltip("Ambient illumination intensity.")]
        public float ambientIntensity = 0.06f;

        private void Awake()
        {
            _yawDeltas = new float[Mathf.Max(120, windowSamples)];

            _trialBuffer.AppendLine("TimestampUtc,ParticipantID,SessionID,Condition,DataStatus,SimulatedLuxTarget,FogDensity,TargetContrast,AmbientIntensity,TrialID,TargetType,TraineeAction,LatencyMs,Hit,Miss,FalseAlarm,CorrectRejection,DistanceMeters,PresentationAngleDeg,HeadYawJitterVarianceDeg2,DPrime,BetaCriterion");
            _assessmentBuffer.AppendLine("TimestampUtc,ParticipantID,SessionID,TrialID,NasaTlxMental,NasaTlxPhysical,NasaTlxTemporal,NasaTlxPerformance,NasaTlxEffort,NasaTlxFrustration,RawTlxMean,SagatRecallCorrect,SagatRecallTotal");

            if (headTransform == null && Camera.main != null)
                headTransform = Camera.main.transform;

            if (headTransform != null)
            {
                _previousHeadRotation = headTransform.rotation;
                _hasPreviousRotation = true;
            }
        }

        private void Update()
        {
            if (headTransform == null) return;

            float interval = 1f / Mathf.Clamp(sampleRateHz, 60, 120);
            _sampleAccumulator += Time.unscaledDeltaTime;

            while (_sampleAccumulator >= interval)
            {
                _sampleAccumulator -= interval;
                Quaternion currentRot = headTransform.rotation;

                if (_hasPreviousRotation)
                {
                    float deltaYaw = Mathf.DeltaAngle(_previousHeadRotation.eulerAngles.y, currentRot.eulerAngles.y);
                    _yawDeltas[_sampleCursor] = deltaYaw;
                    _sampleCursor = (_sampleCursor + 1) % _yawDeltas.Length;
                    if (_sampleCount < _yawDeltas.Length) _sampleCount++;

                    _currentYawVariance = CalculateYawVariance();
                }

                _previousHeadRotation = currentRot;
                _hasPreviousRotation = true;
            }
        }

        private float CalculateYawVariance()
        {
            if (_sampleCount < 2) return 0f;
            float sum = 0f;
            for (int i = 0; i < _sampleCount; i++) sum += _yawDeltas[i];
            float mean = sum / _sampleCount;

            float sumSq = 0f;
            for (int i = 0; i < _sampleCount; i++)
            {
                float diff = _yawDeltas[i] - mean;
                sumSq += diff * diff;
            }
            return sumSq / (_sampleCount - 1);
        }

        public void SetEnvironmentalTelemetry(float simulatedLux, float activeFogDensity, float activeContrast, float activeAmbient)
        {
            simulatedLuxTarget = simulatedLux;
            fogDensity = activeFogDensity;
            targetContrast = activeContrast;
            ambientIntensity = activeAmbient;
        }

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

            _trialBuffer.AppendLine(line);
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

            _assessmentBuffer.AppendLine(line);
            ExportTelemetryToDisk();
        }

        public void ExportTelemetryToDisk()
        {
            try
            {
                string targetDir = Path.Combine(Application.dataPath, "..", exportFolder);
                if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

                string trialPath = Path.Combine(targetDir, exportFileName);
                File.AppendAllText(trialPath, _trialBuffer.ToString());
                _trialBuffer.Clear();

                string assessmentPath = Path.Combine(targetDir, assessmentFileName);
                if (_assessmentBuffer.Length > 0)
                {
                    File.AppendAllText(assessmentPath, _assessmentBuffer.ToString());
                    _assessmentBuffer.Clear();
                }

                Debug.Log("[ReactionLatencyTelemetryLogger] Successfully flushed trial logs to: " + targetDir);
            }
            catch (Exception ex)
            {
                Debug.LogError("[ReactionLatencyTelemetryLogger] Failed to write telemetry: " + ex.Message);
            }
        }

        private void OnApplicationQuit()
        {
            ExportTelemetryToDisk();
        }
    }
}
