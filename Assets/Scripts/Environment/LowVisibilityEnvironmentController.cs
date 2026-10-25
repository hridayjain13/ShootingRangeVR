using UnityEngine;
using IVRAR.Group02.Core;

namespace IVRAR.Group02.Environment
{
    /// <summary>
    /// Controls mesopic nighttime lighting, distance fog density, and ambient illumination
    /// to simulate low-visibility conditions according to experimental parameters.
    /// </summary>
    public class LowVisibilityEnvironmentController : MonoBehaviour
    {
        [Header("Scene Light References")]
        [SerializeField] private Light _directionalLight;
        [SerializeField] private Light _ambientFillLight;

        [Header("Default Mesopic Profile")]
        [SerializeField] private Color _nighttimeSkyColor = new Color(0.04f, 0.05f, 0.08f, 1f);
        [SerializeField] private Color _nighttimeEquatorColor = new Color(0.02f, 0.03f, 0.04f, 1f);
        [SerializeField] private Color _nighttimeFogColor = new Color(0.05f, 0.06f, 0.09f, 1f);

        private bool _originalFog;
        private FogMode _originalFogMode;
        private float _originalFogDensity;
        private Color _originalFogColor;
        private float _originalAmbientIntensity;
        private float _originalDirectionalIntensity;
        private bool _isInitialized;

        private void Awake()
        {
            CaptureOriginalSettings();
        }

        public void CaptureOriginalSettings()
        {
            if (_isInitialized) return;
            _originalFog = RenderSettings.fog;
            _originalFogMode = RenderSettings.fogMode;
            _originalFogDensity = RenderSettings.fogDensity;
            _originalFogColor = RenderSettings.fogColor;
            _originalAmbientIntensity = RenderSettings.ambientIntensity;
            if (_directionalLight != null)
                _originalDirectionalIntensity = _directionalLight.intensity;
            _isInitialized = true;
        }

        public void ApplyConfiguration(ExperimentConfig config)
        {
            if (config == null) return;
            CaptureOriginalSettings();

            if (config.condition == ExperimentalCondition.Baseline_ClearDaylight)
            {
                RestoreBaselineEnvironment();
                return;
            }

            // Apply Mesopic Low Visibility
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = _nighttimeFogColor;
            RenderSettings.fogDensity = config.fogExtinctionPerMeter;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = _nighttimeSkyColor;
            RenderSettings.ambientEquatorColor = _nighttimeEquatorColor;
            RenderSettings.ambientGroundColor = Color.black;
            RenderSettings.ambientIntensity = Mathf.Clamp(config.ambientLux / 5f, 0.02f, 0.5f);

            if (_directionalLight != null)
            {
                _directionalLight.enabled = true;
                _directionalLight.color = new Color(0.6f, 0.7f, 0.9f); // Moonlight hue
                _directionalLight.intensity = config.directionalIntensity;
            }

            if (_ambientFillLight != null)
            {
                _ambientFillLight.intensity = config.directionalIntensity * 0.5f;
            }
        }

        public void RestoreBaselineEnvironment()
        {
            if (!_isInitialized) return;
            RenderSettings.fog = _originalFog;
            RenderSettings.fogMode = _originalFogMode;
            RenderSettings.fogDensity = _originalFogDensity;
            RenderSettings.fogColor = _originalFogColor;
            RenderSettings.ambientIntensity = _originalAmbientIntensity;

            if (_directionalLight != null)
            {
                _directionalLight.intensity = _originalDirectionalIntensity;
                _directionalLight.color = Color.white;
            }
        }

        public void SetFogDensity(float density)
        {
            RenderSettings.fog = true;
            RenderSettings.fogDensity = Mathf.Clamp(density, 0f, 0.2f);
        }

        private void OnDisable()
        {
            RestoreBaselineEnvironment();
        }
    }
}
