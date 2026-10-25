using System.Collections;
using UnityEngine;

namespace IVRAR.Group02.SensoryStress
{
    /// <summary>
    /// Controls peripheral strobe visual startle cues with strict safety guards.
    /// Frequency is capped at 6.0 Hz and pulse trains are capped at 6 pulses to eliminate seizure risk.
    /// </summary>
    public class StrobeStressController : MonoBehaviour
    {
        [Header("Strobe Light Source")]
        [SerializeField] private Light _strobeLight;

        [Header("Safety Boundaries")]
        [SerializeField, Range(1f, 6f)] private float _maxFrequencyHz = 6.0f;
        [SerializeField, Range(1, 6)] private int _maxPulses = 4;
        [SerializeField, Range(0.05f, 0.35f)] private float _maxIntensity = 0.25f;

        private bool _isStrobeRunning;
        private Coroutine _strobeCoroutine;

        private void Awake()
        {
            if (_strobeLight == null) _strobeLight = GetComponent<Light>();
            if (_strobeLight != null) _strobeLight.enabled = false;
        }

        public void TriggerStrobeBurst(float frequencyHz, int pulseCount, float intensity)
        {
            if (_isStrobeRunning || _strobeLight == null) return;

            float safeFreq = Mathf.Clamp(frequencyHz, 1f, _maxFrequencyHz);
            int safePulses = Mathf.Clamp(pulseCount, 1, _maxPulses);
            float safeIntensity = Mathf.Clamp(intensity, 0.05f, _maxIntensity);

            _strobeCoroutine = StartCoroutine(StrobeSequence(safeFreq, safePulses, safeIntensity));
        }

        private IEnumerator StrobeSequence(float frequency, int pulses, float intensity)
        {
            _isStrobeRunning = true;
            _strobeLight.intensity = intensity;
            float halfPeriod = 0.5f / frequency;

            for (int i = 0; i < pulses; i++)
            {
                _strobeLight.enabled = true;
                yield return new WaitForSeconds(halfPeriod);
                _strobeLight.enabled = false;
                yield return new WaitForSeconds(halfPeriod);
            }

            _isStrobeRunning = false;
            _strobeLight.enabled = false;
        }

        public void StopStrobe()
        {
            if (_strobeCoroutine != null) StopCoroutine(_strobeCoroutine);
            _isStrobeRunning = false;
            if (_strobeLight != null) _strobeLight.enabled = false;
        }

        private void OnDisable()
        {
            StopStrobe();
        }
    }
}
