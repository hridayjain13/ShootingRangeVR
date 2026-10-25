using System;
using System.Collections;
using UnityEngine;
using IVRAR.Group02.Core;
using IVRAR.Group02.SensoryStress;
using IVRAR.Group02.Telemetry;

namespace IVRAR.Group02.TargetManagement
{
    /// <summary>
    /// Spawns and manages hostile vs. civilian tactical targets across designated positions
    /// under Poisson inter-arrival distributions and synchronizes with the sensory stress envelope.
    /// </summary>
    public class SensoryStressTargetManager : MonoBehaviour
    {
        [Header("Target Prefabs")]
        [SerializeField] private GameObject _hostileTargetPrefab;
        [SerializeField] private GameObject _civilianTargetPrefab;

        [Header("Spawn Locations")]
        [SerializeField] private Transform[] _spawnAnchors;

        [Header("System References")]
        [SerializeField] private ReactionLatencyTelemetryLogger _telemetryLogger;
        [SerializeField] private SensoryStressManager _stressManager;

        public event Action<int, bool, float, float> OnTargetPresented; // trialIndex, isHostile, distance, angle
        public event Action OnAllTrialsCompleted;

        private ExperimentConfig _config;
        private bool _isSequenceActive;
        private Coroutine _spawnCoroutine;
        private System.Random _random = new System.Random();
        private int _currentTrialIndex;
        private GameObject _activeTarget;

        public bool IsSequenceActive => _isSequenceActive;
        public int CurrentTrialIndex => _currentTrialIndex;

        private void Awake()
        {
            if (_telemetryLogger == null) _telemetryLogger = FindObjectOfType<ReactionLatencyTelemetryLogger>();
            if (_stressManager == null) _stressManager = FindObjectOfType<SensoryStressManager>();
        }

        public void Initialize(ExperimentConfig config)
        {
            _config = config;
        }

        public void StartTrialSequence()
        {
            if (_isSequenceActive) return;
            _isSequenceActive = true;
            _currentTrialIndex = 0;
            _spawnCoroutine = StartCoroutine(SequenceLoop());
        }

        public void StopTrialSequence()
        {
            _isSequenceActive = false;
            if (_spawnCoroutine != null) StopCoroutine(_spawnCoroutine);
            if (_activeTarget != null) Destroy(_activeTarget);
            if (_stressManager != null) _stressManager.StopAllStressors();
        }

        private IEnumerator SequenceLoop()
        {
            int maxTrials = (_config != null) ? _config.totalTrialsInBlock : 20;
            float meanInterval = (_config != null) ? _config.meanInterTargetIntervalSeconds : 3.0f;

            while (_isSequenceActive && _currentTrialIndex < maxTrials)
            {
                // Poisson process wait time: t = -ln(U) * mean
                double u = Math.Max(_random.NextDouble(), 1e-9);
                float waitSeconds = (float)(-Math.Log(u) * Mathf.Max(0.5f, meanInterval));
                yield return new WaitForSeconds(waitSeconds);

                if (!_isSequenceActive) yield break;

                _currentTrialIndex++;
                SpawnTrialTarget(_currentTrialIndex);

                float exposureDur = (_config != null) ? _config.exposureDurationSeconds : 2.5f;
                yield return new WaitForSeconds(exposureDur + 0.5f);
            }

            _isSequenceActive = false;
            OnAllTrialsCompleted?.Invoke();
        }

        public void SpawnTrialTarget(int trialIndex)
        {
            if (_spawnAnchors == null || _spawnAnchors.Length == 0) return;

            int anchorIdx = _random.Next(_spawnAnchors.Length);
            Transform anchor = _spawnAnchors[anchorIdx];
            if (anchor == null) return;

            float pHostile = (_config != null) ? _config.hostileProbability : 0.5f;
            bool isHostile = _random.NextDouble() < pHostile;

            GameObject prefab = isHostile ? _hostileTargetPrefab : _civilianTargetPrefab;
            if (prefab == null) return;

            if (_activeTarget != null) Destroy(_activeTarget);

            _activeTarget = Instantiate(prefab, anchor.position, anchor.rotation);

            string trialId = "TRIAL-" + trialIndex.ToString("D4");
            float distance = Vector3.Distance(transform.position, anchor.position);
            Vector3 relative = anchor.position - transform.position;
            float angle = Vector3.SignedAngle(transform.forward, relative, Vector3.up);

            // Setup metadata
            TargetTrialMetadata metadata = _activeTarget.GetComponent<TargetTrialMetadata>();
            if (metadata == null) metadata = _activeTarget.AddComponent<TargetTrialMetadata>();

            metadata.Initialize(trialId, isHostile, distance, angle, _telemetryLogger, Time.realtimeSinceStartupAsDouble);
            float exposureDur = (_config != null) ? _config.exposureDurationSeconds : 2.5f;
            metadata.StartExposureTimeout(exposureDur);

            // Tactical target component check
            TacticalTarget tactical = _activeTarget.GetComponent<TacticalTarget>();
            if (tactical != null) tactical.SetTargetType(isHostile);

            // Trigger sensory stressors
            if (_stressManager != null)
            {
                _stressManager.TriggerSensoryStressorEnvelope(anchor.position, 1.0f);
            }

            OnTargetPresented?.Invoke(trialIndex, isHostile, distance, angle);
        }
    }
}
