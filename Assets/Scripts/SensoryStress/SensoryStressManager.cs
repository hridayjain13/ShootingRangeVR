using UnityEngine;
using IVRAR.Group02.Core;

namespace IVRAR.Group02.SensoryStress
{
    /// <summary>
    /// Central manager coordinating both optical (strobe) and acoustic stressors
    /// during active target presentation envelopes.
    /// </summary>
    public class SensoryStressManager : MonoBehaviour
    {
        [Header("Stressor Subsystems")]
        [SerializeField] private StrobeStressController _strobeController;
        [SerializeField] private AcousticStressController _acousticController;

        private ExperimentConfig _activeConfig;
        private System.Random _random = new System.Random();

        public void Initialize(ExperimentConfig config)
        {
            _activeConfig = config;
            if (_strobeController == null) _strobeController = FindObjectOfType<StrobeStressController>();
            if (_acousticController == null) _acousticController = FindObjectOfType<AcousticStressController>();
        }

        public void TriggerSensoryStressorEnvelope(Vector3 targetPosition, float intensityFactor = 1.0f)
        {
            if (_activeConfig == null) return;
            if (_activeConfig.condition == ExperimentalCondition.Baseline_ClearDaylight ||
                _activeConfig.condition == ExperimentalCondition.LowVisibility_NoStress)
            {
                return;
            }

            // Trigger Strobe startle
            if (_strobeController != null && _activeConfig.maxStrobePulses > 0)
            {
                int pulses = _random.Next(1, _activeConfig.maxStrobePulses + 1);
                _strobeController.TriggerStrobeBurst(_activeConfig.strobeFrequencyHz, pulses, _activeConfig.strobeIntensity * intensityFactor);
            }

            // Trigger Acoustic startle with configured probability
            if (_acousticController != null && _random.NextDouble() < _activeConfig.acousticStartleProbability)
            {
                bool gunshot = _random.NextDouble() < 0.6;
                if (gunshot)
                {
                    _acousticController.PlayGunshotCue(_activeConfig.maxGunshotCueGain * intensityFactor, targetPosition);
                }
                else
                {
                    _acousticController.PlayAlarmCue(_activeConfig.maxAlarmCueGain * intensityFactor, targetPosition);
                }
            }
        }

        public void StopAllStressors()
        {
            if (_strobeController != null) _strobeController.StopStrobe();
            if (_acousticController != null) _acousticController.StopAllAudio();
        }
    }
}
