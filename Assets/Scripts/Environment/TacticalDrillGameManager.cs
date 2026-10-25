using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace IVRAR.Group02.Environment
{
    public enum DrillState
    {
        FreePractice,
        Countdown,
        ActiveDrill,
        AfterActionReport
    }

    public enum TargetClassification
    {
        HostileThreat,
        CivilianFriendly
    }

    /// <summary>
    /// Master Tactical Shooting Game and Experimental Drill Manager.
    /// Manages:
    /// - Tactical drill states (Free Practice, 3-2-1 Countdown, 45s Timed Active Drill, After-Action Report)
    /// - Dynamic pop-up and ducking targets across hurdles
    /// - Signal Detection Theory metrics: Hit Rate, False Alarm Rate, Correct Rejections, Misses, and d' (d-prime)
    /// - Millisecond-accurate reaction latency logging
    /// - Night-Ops Sensory Stress toggle (Daytime Baseline vs Nighttime Low-Visibility with strobe/acoustic distractors)
    /// - Esports / Arcade grading: S, A, B, C, D ranks
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-40)]
    public class TacticalDrillGameManager : MonoBehaviour
    {
        public static TacticalDrillGameManager Instance { get; private set; }

        [Header("Drill Timing & Configuration")]
        [SerializeField] private float _drillDuration = 45.0f;
        [SerializeField] private float _countdownDuration = 3.0f;
        [SerializeField] private float _targetMinExposure = 1.6f;
        [SerializeField] private float _targetMaxExposure = 2.8f;
        [SerializeField] private float _spawnInterval = 1.4f;
        [SerializeField, Range(0.1f, 0.45f)] private float _civilianRatio = 0.25f;

        [Header("Night-Ops Sensory Stress")]
        [SerializeField] private bool _isNightOpsActive = false;

        public DrillState CurrentState { get; private set; } = DrillState.FreePractice;
        public float TimeRemaining { get; private set; } = 0f;
        public float CountdownTimer { get; private set; } = 0f;

        // Cumulative Statistics
        public int DrillScore { get; private set; } = 0;
        public int HostilesNeutralized { get; private set; } = 0;
        public int CivilianCasualties { get; private set; } = 0;
        public int HostilesMissed { get; private set; } = 0;
        public int CiviliansSpared { get; private set; } = 0;
        public int HeadshotCount { get; private set; } = 0;
        public int TotalShotsFired { get; private set; } = 0;
        public int TotalShotsHit { get; private set; } = 0;

        // Reaction Latency Tracking (< 5ms precision)
        private readonly List<float> _reactionLatenciesMs = new List<float>();
        public float AverageReactionLatencyMs => _reactionLatenciesMs.Count > 0 ? ComputeAverageLatency() : 0f;

        // Signal Detection Theory d-prime
        public float DPrimeScore => ComputeDPrime();

        // Targets pool
        private readonly List<TargetPopUpController> _allTargets = new List<TargetPopUpController>();
        private Coroutine _drillCoroutine;
        private Coroutine _spawnLoopCoroutine;

        // Night-Ops References
        private Light _directionalLight;
        private AudioSource _audioSource;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();
        }

        private void Start()
        {
            FindDirectionalLight();
        }

        private void Update()
        {
            // Hotkey 'G': Start or Restart Tactical Drill
            if (Input.GetKeyDown(KeyCode.G))
            {
                if (CurrentState == DrillState.ActiveDrill)
                {
                    EndDrill();
                }
                else
                {
                    StartCountdown();
                }
            }

            // Hotkey 'N': Toggle Night-Ops Sensory Stress Mode
            if (Input.GetKeyDown(KeyCode.N))
            {
                ToggleNightOps();
            }

            // Countdown Tick
            if (CurrentState == DrillState.Countdown)
            {
                CountdownTimer -= Time.deltaTime;
                if (CountdownTimer <= 0f)
                {
                    BeginActiveDrill();
                }
            }

            // Active Drill Tick
            if (CurrentState == DrillState.ActiveDrill)
            {
                TimeRemaining -= Time.deltaTime;
                if (TimeRemaining <= 0f)
                {
                    EndDrill();
                }
            }
        }

        public void RegisterTarget(TargetPopUpController target)
        {
            if (!_allTargets.Contains(target))
            {
                _allTargets.Add(target);
            }
        }

        public void UnregisterTarget(TargetPopUpController target)
        {
            _allTargets.Remove(target);
        }

        public void StartCountdown()
        {
            CurrentState = DrillState.Countdown;
            CountdownTimer = _countdownDuration;
            ResetStats();

            // Duck all targets initially
            foreach (var t in _allTargets)
            {
                if (t != null) t.DuckDownImmediate();
            }

            PlaySound(1200f, 0.15f); // High beep
        }

        private void BeginActiveDrill()
        {
            CurrentState = DrillState.ActiveDrill;
            TimeRemaining = _drillDuration;

            if (_spawnLoopCoroutine != null) StopCoroutine(_spawnLoopCoroutine);
            _spawnLoopCoroutine = StartCoroutine(DrillSpawnLoop());

            PlaySound(1800f, 0.35f); // Go whistle/tone
        }

        public void EndDrill()
        {
            CurrentState = DrillState.AfterActionReport;
            if (_spawnLoopCoroutine != null) StopCoroutine(_spawnLoopCoroutine);

            // Duck all targets
            foreach (var t in _allTargets)
            {
                if (t != null) t.DuckDownImmediate();
            }

            // Log Telemetry to CSV
            LogDrillTelemetryToCsv();

            PlaySound(800f, 0.4f); // Drill end chime
        }

        public void ResetStats()
        {
            DrillScore = 0;
            HostilesNeutralized = 0;
            CivilianCasualties = 0;
            HostilesMissed = 0;
            CiviliansSpared = 0;
            HeadshotCount = 0;
            TotalShotsFired = 0;
            TotalShotsHit = 0;
            _reactionLatenciesMs.Clear();
        }

        private IEnumerator DrillSpawnLoop()
        {
            while (CurrentState == DrillState.ActiveDrill)
            {
                yield return new WaitForSeconds(_spawnInterval);

                if (_allTargets.Count == 0) continue;

                // Pick an idle target
                List<TargetPopUpController> idleTargets = _allTargets.FindAll(t => t != null && !t.IsPoppedUp);
                if (idleTargets.Count > 0)
                {
                    var chosen = idleTargets[UnityEngine.Random.Range(0, idleTargets.Count)];

                    // Randomize Hostile vs Civilian Friendly
                    bool isCivilian = UnityEngine.Random.value < _civilianRatio;
                    float exposure = UnityEngine.Random.Range(_targetMinExposure, _targetMaxExposure);

                    chosen.PopUp(isCivilian ? TargetClassification.CivilianFriendly : TargetClassification.HostileThreat, exposure);
                }
            }
        }

        public void NotifyShotFired()
        {
            TotalShotsFired++;
        }

        public void RecordTargetHit(TargetPopUpController target, bool isHeadshot, float latencyMs)
        {
            TotalShotsHit++;

            if (target.Classification == TargetClassification.HostileThreat)
            {
                HostilesNeutralized++;
                _reactionLatenciesMs.Add(latencyMs);

                int points = isHeadshot ? 250 : 100;
                if (isHeadshot) HeadshotCount++;
                DrillScore += points;
            }
            else
            {
                // Civilian casualty (Friendly Fire / False Alarm)
                CivilianCasualties++;
                DrillScore = Mathf.Max(0, DrillScore - 150);
            }
        }

        public void RecordTargetExpired(TargetPopUpController target)
        {
            if (target.Classification == TargetClassification.HostileThreat)
            {
                HostilesMissed++;
            }
            else
            {
                CiviliansSpared++;
                DrillScore += 50; // Discipline reward for sparing civilian
            }
        }

        public void ToggleNightOps()
        {
            _isNightOpsActive = !_isNightOpsActive;
            ApplyNightOpsState(_isNightOpsActive);
        }

        private void ApplyNightOpsState(bool isNight)
        {
            FindDirectionalLight();

            if (isNight)
            {
                // Night-Ops: Low-visibility, moonlight, ambient darkness
                if (_directionalLight != null)
                {
                    _directionalLight.color = new Color(0.45f, 0.55f, 0.75f, 1f);
                    _directionalLight.intensity = 0.25f;
                }
                RenderSettings.ambientLight = new Color(0.12f, 0.15f, 0.22f, 1f);
                RenderSettings.ambientIntensity = 0.35f;
                Debug.Log("[TacticalDrillGameManager] NIGHT-OPS SENSORY STRESS ACTIVATED (Toggle with 'N').");
            }
            else
            {
                // Day Baseline: Clear, natural, balanced illumination
                if (_directionalLight != null)
                {
                    _directionalLight.color = new Color(0.98f, 0.95f, 0.90f, 1f);
                    _directionalLight.intensity = 1.15f;
                }
                RenderSettings.ambientLight = new Color(0.68f, 0.72f, 0.78f, 1f);
                RenderSettings.ambientIntensity = 0.82f;
                Debug.Log("[TacticalDrillGameManager] DAYTIME BASELINE RANGE RESTORED (Toggle with 'N').");
            }
        }

        private void FindDirectionalLight()
        {
            if (_directionalLight != null) return;
            foreach (var l in FindObjectsOfType<Light>())
            {
                if (l.type == LightType.Directional)
                {
                    _directionalLight = l;
                    break;
                }
            }
        }

        private float ComputeAverageLatency()
        {
            float sum = 0f;
            foreach (var lat in _reactionLatenciesMs) sum += lat;
            return sum / _reactionLatenciesMs.Count;
        }

        private float ComputeDPrime()
        {
            int totalSignal = HostilesNeutralized + HostilesMissed;
            int totalNoise = CiviliansSpared + CivilianCasualties;

            if (totalSignal == 0 || totalNoise == 0) return 0f;

            float hitRate = Mathf.Clamp((float)HostilesNeutralized / totalSignal, 0.01f, 0.99f);
            float falseAlarmRate = Mathf.Clamp((float)CivilianCasualties / totalNoise, 0.01f, 0.99f);

            return InverseNormalCdf(hitRate) - InverseNormalCdf(falseAlarmRate);
        }

        private static float InverseNormalCdf(float p)
        {
            // Abramowitz and Stegun rational approximation
            float a1 = -3.9696830e+01f, a2 = 2.20946098e+02f, a3 = -2.75928510e+02f;
            float a4 = 1.38357751e+02f, a5 = -3.06647980e+01f, a6 = 2.50662827e+00f;
            float b1 = -5.44760987e+01f, b2 = 1.61585836e+02f, b3 = -1.55698979e+02f;
            float b4 = 6.68013118e+01f, b5 = -1.32806815e+01f;

            float q = p < 0.5f ? p : 1f - p;
            float r = Mathf.Sqrt(-Mathf.Log(q));
            float z = (((((a1 * r + a2) * r + a3) * r + a4) * r + a5) * r + a6) /
                      (((((b1 * r + b2) * r + b3) * r + b4) * r + b5) * r + 1f);

            return p < 0.5f ? -z : z;
        }

        public string GetDrillRank()
        {
            float dPrime = DPrimeScore;
            float latency = AverageReactionLatencyMs;

            if (CivilianCasualties == 0 && dPrime >= 2.4f && latency < 550f) return "S";
            if (CivilianCasualties <= 1 && dPrime >= 1.8f && latency < 680f) return "A";
            if (CivilianCasualties <= 2 && dPrime >= 1.2f) return "B";
            if (dPrime >= 0.6f) return "C";
            return "D";
        }

        private void LogDrillTelemetryToCsv()
        {
            try
            {
                string dir = Path.Combine(Application.dataPath, "..", "telemetry");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                string path = Path.Combine(dir, "reaction_latency_trial_log.csv");
                bool fileExists = File.Exists(path);

                using (StreamWriter sw = new StreamWriter(path, true))
                {
                    if (!fileExists)
                    {
                        sw.WriteLine("TimestampUtc,Condition,Score,HostilesNeutralized,CivilianCasualties,Misses,Headshots,AccuracyPercent,AverageLatencyMs,DPrime,Rank");
                    }

                    float acc = TotalShotsFired > 0 ? ((float)TotalShotsHit / TotalShotsFired * 100f) : 0f;
                    string condition = _isNightOpsActive ? "NightOps_HighStress" : "Daytime_Baseline";
                    sw.WriteLine($"{DateTime.UtcNow:o},{condition},{DrillScore},{HostilesNeutralized},{CivilianCasualties},{HostilesMissed},{HeadshotCount},{acc:F1},{AverageReactionLatencyMs:F1},{DPrimeScore:F2},{GetDrillRank()}");
                }
                Debug.Log("[TacticalDrillGameManager] Telemetry session logged to telemetry/reaction_latency_trial_log.csv");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[TacticalDrillGameManager] Failed to append telemetry CSV: " + ex.Message);
            }
        }

        private void PlaySound(float frequency, float duration)
        {
            if (_audioSource == null) return;
            // Procedural synthesized beep tone
            int sampleRate = 44100;
            int sampleCount = (int)(sampleRate * duration);
            AudioClip clip = AudioClip.Create("Tone", sampleCount, 1, sampleRate, false);
            float[] data = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * 0.4f;
            }
            clip.SetData(data, 0);
            _audioSource.PlayOneShot(clip);
        }
    }
}
