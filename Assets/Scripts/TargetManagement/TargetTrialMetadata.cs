using System.Collections;
using UnityEngine;
using IVRAR.Group02.Telemetry;

namespace IVRAR.Group02.TargetManagement
{
    /// <summary>
    /// Attached to spawned targets to track trial lifecycle, measure response latency,
    /// and log engagement outcomes.
    /// </summary>
    public sealed class TargetTrialMetadata : MonoBehaviour
    {
        public string TrialId { get; private set; }
        public bool IsHostile { get; private set; }
        public float DistanceMeters { get; private set; }
        public float PresentationAngle { get; private set; }

        private ReactionLatencyTelemetryLogger _logger;
        private double _spawnTime;
        private bool _reported;
        private Coroutine _timeoutCoroutine;

        public void Initialize(string trialId, bool isHostile, float distanceMeters, float presentationAngle,
            ReactionLatencyTelemetryLogger telemetryLogger, double presentedAt)
        {
            TrialId = trialId;
            IsHostile = isHostile;
            DistanceMeters = distanceMeters;
            PresentationAngle = presentationAngle;
            _logger = telemetryLogger;
            _spawnTime = presentedAt;
            _reported = false;
        }

        public void StartExposureTimeout(float durationSeconds)
        {
            if (_timeoutCoroutine != null) StopCoroutine(_timeoutCoroutine);
            _timeoutCoroutine = StartCoroutine(TimeoutSequence(durationSeconds));
        }

        private IEnumerator TimeoutSequence(float durationSeconds)
        {
            yield return new WaitForSeconds(Mathf.Max(0.2f, durationSeconds));
            RegisterDecision(false);
        }

        public void RegisterDecision(bool engaged)
        {
            if (_reported) return;
            _reported = true;

            if (_timeoutCoroutine != null) StopCoroutine(_timeoutCoroutine);

            float latencyMs = Mathf.Max(0f, (float)((Time.realtimeSinceStartupAsDouble - _spawnTime) * 1000.0));

            if (_logger != null)
            {
                _logger.RecordEngagementEvent(TrialId, IsHostile, engaged, latencyMs, DistanceMeters, PresentationAngle);
            }

            // Visual feedback on destruction
            Destroy(gameObject, 0.05f);
        }
    }
}
