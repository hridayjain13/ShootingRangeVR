using UnityEngine;
using UnityEngine.UI;
using IVRAR.Group02.Core;
using IVRAR.Group02.Telemetry;

namespace IVRAR.Group02.UI
{
    /// <summary>
    /// In-VR trainee HUD displaying trial countdown, current reaction latency feedback,
    /// accuracy score, and active sensory stress state.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("HUD Labels")]
        [SerializeField] private Text _lblSimulationState;
        [SerializeField] private Text _lblTrialProgress;
        [SerializeField] private Text _lblLatencyFeedback;
        [SerializeField] private Text _lblAccuracyStats;

        [Header("System References")]
        [SerializeField] private SimulationManager _simulationManager;
        [SerializeField] private ReactionLatencyTelemetryLogger _telemetryLogger;

        private void Awake()
        {
            if (_simulationManager == null) _simulationManager = FindObjectOfType<SimulationManager>();
            if (_telemetryLogger == null) _telemetryLogger = FindObjectOfType<ReactionLatencyTelemetryLogger>();
        }

        public void UpdateState(SimulationState state)
        {
            if (_lblSimulationState != null)
                _lblSimulationState.text = "State: " + state.ToString();
        }

        public void UpdateProgress(int currentTrial, int totalTrials)
        {
            if (_lblTrialProgress != null)
                _lblTrialProgress.text = string.Format("Trial: {0} / {1}", currentTrial, totalTrials);
        }

        public void DisplayLatencyFeedback(float latencyMs, bool wasHit, bool wasHostile)
        {
            if (_lblLatencyFeedback == null) return;

            string verdict;
            Color col;

            if (wasHostile && wasHit)
            {
                verdict = string.Format("HIT! {0:F0} ms", latencyMs);
                col = Color.green;
            }
            else if (!wasHostile && wasHit)
            {
                verdict = string.Format("FALSE ALARM! (Civilian Shot) {0:F0} ms", latencyMs);
                col = Color.red;
            }
            else if (wasHostile && !wasHit)
            {
                verdict = "MISS! (Hostile Escaped)";
                col = Color.yellow;
            }
            else
            {
                verdict = "CORRECT REJECTION (Civilian Spared)";
                col = Color.cyan;
            }

            _lblLatencyFeedback.text = verdict;
            _lblLatencyFeedback.color = col;

            UpdateAccuracyStats();
        }

        private void UpdateAccuracyStats()
        {
            if (_lblAccuracyStats == null || _telemetryLogger == null) return;

            int hits = _telemetryLogger.TotalHits;
            int misses = _telemetryLogger.TotalMisses;
            int falseAlarms = _telemetryLogger.TotalFalseAlarms;
            int correctRejections = _telemetryLogger.TotalCorrectRejections;

            int total = hits + misses + falseAlarms + correctRejections;
            int correct = hits + correctRejections;
            float acc = total > 0 ? (correct / (float)total) * 100f : 100f;

            _lblAccuracyStats.text = string.Format("Accuracy: {0:F1}% (H:{1} M:{2} FA:{3} CR:{4})",
                acc, hits, misses, falseAlarms, correctRejections);
        }
    }
}
