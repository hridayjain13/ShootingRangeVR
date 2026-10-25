using UnityEngine;
using IVRAR.Group02.Core;

namespace IVRAR.Group02.Environment
{
    /// <summary>
    /// Tactical flashlight model implementing realistic inverse-square decay and atmospheric fog attenuation.
    /// E = (I0 * cos(theta) / r^2) * exp(-alpha * r)
    /// </summary>
    public class FlashlightController : MonoBehaviour
    {
        [Header("Light Components")]
        [SerializeField] private Light _spotLight;
        [SerializeField] private AudioSource _toggleAudioSource;
        [SerializeField] private AudioClip _toggleClip;

        [Header("Flashlight Parameters")]
        [SerializeField] private float _referenceIlluminanceLux = 100f;
        [SerializeField] private float _referenceDistanceMeters = 1.0f;
        [SerializeField] private float _fogExtinctionAlpha = 0.045f;

        private bool _isOn = true;
        private float _baseIntensity = 1.5f;

        private void Awake()
        {
            if (_spotLight == null) _spotLight = GetComponent<Light>();
            if (_spotLight != null) _baseIntensity = _spotLight.intensity;
        }

        public void ApplyConfiguration(ExperimentConfig config)
        {
            if (config == null || _spotLight == null) return;
            _spotLight.spotAngle = config.flashlightSpotAngle;
            _spotLight.range = config.flashlightRange;
            _spotLight.intensity = config.flashlightIntensity;
            _baseIntensity = config.flashlightIntensity;
            _fogExtinctionAlpha = config.fogExtinctionPerMeter;
        }

        public void Toggle()
        {
            SetState(!_isOn);
        }

        public void SetState(bool on)
        {
            _isOn = on;
            if (_spotLight != null) _spotLight.enabled = _isOn;
            if (_toggleAudioSource != null && _toggleClip != null)
                _toggleAudioSource.PlayOneShot(_toggleClip);
        }

        /// <summary>
        /// Calculates target incident illuminance (in lux) at a specific 3D point in the environment.
        /// </summary>
        public float CalculateIlluminanceAtTarget(Vector3 targetPosition)
        {
            if (!_isOn || _spotLight == null) return 0f;

            Vector3 lightPos = _spotLight.transform.position;
            Vector3 lightForward = _spotLight.transform.forward;
            Vector3 toTarget = targetPosition - lightPos;
            float distance = toTarget.magnitude;

            if (distance > _spotLight.range || distance < 0.1f) return 0f;

            Vector3 dirToTarget = toTarget.normalized;
            float cosTheta = Mathf.Max(0f, Vector3.Dot(lightForward, dirToTarget));
            float angleDeg = Mathf.Acos(cosTheta) * Mathf.Rad2Deg;

            if (angleDeg > _spotLight.spotAngle * 0.5f) return 0f;

            float r = Mathf.Max(0.2f, distance);
            float geometricFalloff = (_referenceIlluminanceLux * Mathf.Pow(_referenceDistanceMeters, 2f) * cosTheta) / (r * r);
            float atmosphericAttenuation = Mathf.Exp(-_fogExtinctionAlpha * r);

            return geometricFalloff * atmosphericAttenuation;
        }
    }
}
