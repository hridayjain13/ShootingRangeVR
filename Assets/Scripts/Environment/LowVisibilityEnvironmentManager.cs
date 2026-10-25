using System;
using System.Collections.Generic;
using UnityEngine;
using IVRAR.Group02.Core;
using IVRAR.Group02.TargetManagement;
using IVRAR.Group02.Telemetry;

namespace IVRAR.Group02.Environment
{
    /// <summary>
    /// Experimental condition presets for low-visibility nighttime visual identification.
    /// </summary>
    public enum LowVisibilityCondition
    {
        ConditionA_Baseline,
        ConditionB_ReducedNighttime,
        ConditionC_ReducedWithFog,
        Custom
    }

    /// <summary>
    /// Serializable profile defining all lighting, fog, shadow, and target contrast parameters
    /// for a reproducible experimental condition.
    /// </summary>
    [Serializable]
    public struct LowVisibilityProfile
    {
        [Tooltip("Human-readable condition title.")]
        public string conditionName;

        [Tooltip("Simulated research illuminance target in lux (photopic/mesopic boundary is ~0.5-5.0 lux). NOTE: Unity light intensity is an uncalibrated simulation target, not a verified physical measurement at the eye.")]
        [Range(0.5f, 5.0f)]
        public float simulatedTargetLux;

        [Tooltip("True only if physical lux meter measurement has been verified at the headset lens aperture.")]
        public bool isPhysicallyCalibrated;

        [Tooltip("Directional moonlight/ambient illumination intensity factor.")]
        [Range(0.01f, 2.0f)]
        public float directionalIntensity;

        [Tooltip("Color tint of the primary directional illumination source.")]
        public Color directionalColor;

        [Tooltip("Real-time shadow casting type for directional light.")]
        public LightShadows shadowType;

        [Tooltip("Real-time shadow strength (0 = no shadow, 1 = full darkness shadow).")]
        [Range(0f, 1f)]
        public float shadowStrength;

        [Tooltip("Ambient lighting intensity multiplier.")]
        [Range(0.01f, 1.5f)]
        public float ambientIntensity;

        [Tooltip("Ambient trilight sky color.")]
        public Color ambientSkyColor;

        [Tooltip("Ambient trilight equator color.")]
        public Color ambientEquatorColor;

        [Tooltip("Ambient trilight ground color.")]
        public Color ambientGroundColor;

        [Tooltip("Whether distance atmospheric fog is active.")]
        public bool fogEnabled;

        [Tooltip("Mathematical fog attenuation mode.")]
        public FogMode fogMode;

        [Tooltip("Extinction coefficient for distance fog (per meter).")]
        [Range(0f, 0.2f)]
        public float fogDensity;

        [Tooltip("Atmospheric fog scattering color tint.")]
        public Color fogColor;

        [Tooltip("Target visual luminance contrast multiplier (0.1 = low silhouette contrast, 1.0 = baseline contrast).")]
        [Range(0.1f, 1.0f)]
        public float targetContrastMultiplier;

        [Tooltip("Maximum distance in meters at which targets can be visually resolved before blending into fog/darkness.")]
        [Min(2f)]
        public float targetVisibilityMaxDistance;
    }

    /// <summary>
    /// Transforms the Unity environment into a controlled, reproducible nighttime visual-identification experiment.
    /// Manages illumination targets (0.5 to 5.0 simulated lux), atmospheric fog density, ambient light,
    /// shadows, and target visual contrast across experimental conditions A, B, and C.
    /// </summary>
    [DisallowMultipleComponent]
    public class LowVisibilityEnvironmentManager : MonoBehaviour
    {
        [Header("Active Condition Selection")]
        [SerializeField] private LowVisibilityCondition _activeCondition = LowVisibilityCondition.ConditionC_ReducedWithFog;

        [Header("Scene Illumination References")]
        [SerializeField] private Light _directionalLight;
        [SerializeField] private Light _ambientFillLight;
        [SerializeField] private FlashlightController _traineeFlashlight;

        [Header("System Links")]
        [SerializeField] private ReactionLatencyTelemetryLogger _telemetryLogger;

        [Header("Calibration Metadata")]
        [SerializeField] private bool _isPhysicallyCalibrated = false;
        [SerializeField] private string _calibrationStatus = "Uncalibrated Simulated Illumination Target";

        [Header("Preset Profiles")]
        [SerializeField] private LowVisibilityProfile _profileA_Baseline;
        [SerializeField] private LowVisibilityProfile _profileB_ReducedNighttime;
        [SerializeField] private LowVisibilityProfile _profileC_ReducedWithFog;
        [SerializeField] private LowVisibilityProfile _customProfile;

        [Header("Runtime Hotkey Overrides")]
        [SerializeField] private bool _enableKeyboardShortcuts = true;

        // Cached baseline settings for reset-to-baseline function
        private bool _originalFogEnabled;
        private FogMode _originalFogMode;
        private float _originalFogDensity;
        private Color _originalFogColor;
        private float _originalAmbientIntensity;
        private Color _originalAmbientSkyColor;
        private Color _originalAmbientEquatorColor;
        private Color _originalAmbientGroundColor;
        private UnityEngine.Rendering.AmbientMode _originalAmbientMode;

        private float _originalDirectionalIntensity;
        private Color _originalDirectionalColor;
        private LightShadows _originalShadowType;
        private float _originalShadowStrength;

        private bool _isBaselineCaptured = false;

        // Public accessors for research telemetry and UI
        public LowVisibilityCondition ActiveCondition => _activeCondition;
        public float CurrentSimulatedTargetLux => GetCurrentProfile().simulatedTargetLux;
        public bool IsPhysicallyCalibrated => _isPhysicallyCalibrated;
        public string CalibrationStatus => _calibrationStatus;
        public float CurrentFogDensity => RenderSettings.fog ? RenderSettings.fogDensity : 0f;
        public float CurrentTargetContrast => GetCurrentProfile().targetContrastMultiplier;
        public float CurrentAmbientIntensity => RenderSettings.ambientIntensity;
        public float CurrentDirectionalIntensity => _directionalLight != null ? _directionalLight.intensity : 0f;
        public ReactionLatencyTelemetryLogger TelemetryLogger { get => _telemetryLogger; set => _telemetryLogger = value; }

        private void Awake()
        {
            InitializeDefaultProfiles();
            CaptureBaselineEnvironment();

            if (_directionalLight == null)
            {
                // Find primary directional light in scene
                Light[] lights = FindObjectsOfType<Light>();
                foreach (var l in lights)
                {
                    if (l.type == LightType.Directional)
                    {
                        _directionalLight = l;
                        break;
                    }
                }
            }

            if (_telemetryLogger == null)
            {
                _telemetryLogger = FindObjectOfType<ReactionLatencyTelemetryLogger>();
            }

            if (_traineeFlashlight == null)
            {
                _traineeFlashlight = FindObjectOfType<FlashlightController>();
            }
        }

        private void Start()
        {
            ApplyCondition(_activeCondition);
        }

        private void Update()
        {
            if (_enableKeyboardShortcuts)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1)) ApplyCondition(LowVisibilityCondition.ConditionA_Baseline);
                if (Input.GetKeyDown(KeyCode.Alpha2)) ApplyCondition(LowVisibilityCondition.ConditionB_ReducedNighttime);
                if (Input.GetKeyDown(KeyCode.Alpha3)) ApplyCondition(LowVisibilityCondition.ConditionC_ReducedWithFog);
                if (Input.GetKeyDown(KeyCode.Alpha0)) ResetToBaseline();
            }
        }

        /// <summary>
        /// Captures the initial scene environment settings so they can be faithfully restored.
        /// </summary>
        public void CaptureBaselineEnvironment()
        {
            if (_isBaselineCaptured) return;

            _originalFogEnabled = RenderSettings.fog;
            _originalFogMode = RenderSettings.fogMode;
            _originalFogDensity = RenderSettings.fogDensity;
            _originalFogColor = RenderSettings.fogColor;

            _originalAmbientMode = RenderSettings.ambientMode;
            _originalAmbientIntensity = RenderSettings.ambientIntensity;
            _originalAmbientSkyColor = RenderSettings.ambientSkyColor;
            _originalAmbientEquatorColor = RenderSettings.ambientEquatorColor;
            _originalAmbientGroundColor = RenderSettings.ambientGroundColor;

            if (_directionalLight != null)
            {
                _originalDirectionalIntensity = _directionalLight.intensity;
                _originalDirectionalColor = _directionalLight.color;
                _originalShadowType = _directionalLight.shadows;
                _originalShadowStrength = _directionalLight.shadowStrength;
            }

            _isBaselineCaptured = true;
        }

        /// <summary>
        /// Restores the environment to the initial baseline scene configuration.
        /// </summary>
        public void ResetToBaseline()
        {
            if (!_isBaselineCaptured) CaptureBaselineEnvironment();

            RenderSettings.fog = _originalFogEnabled;
            RenderSettings.fogMode = _originalFogMode;
            RenderSettings.fogDensity = _originalFogDensity;
            RenderSettings.fogColor = _originalFogColor;

            RenderSettings.ambientMode = _originalAmbientMode;
            RenderSettings.ambientIntensity = _originalAmbientIntensity;
            RenderSettings.ambientSkyColor = _originalAmbientSkyColor;
            RenderSettings.ambientEquatorColor = _originalAmbientEquatorColor;
            RenderSettings.ambientGroundColor = _originalAmbientGroundColor;

            if (_directionalLight != null)
            {
                _directionalLight.intensity = _originalDirectionalIntensity;
                _directionalLight.color = _originalDirectionalColor;
                _directionalLight.shadows = _originalShadowType;
                _directionalLight.shadowStrength = _originalShadowStrength;
            }

            _activeCondition = LowVisibilityCondition.ConditionA_Baseline;
            UpdateTargetVisibility(1.0f);
            NotifyTelemetry();

            Debug.Log("[LowVisibilityEnvironmentManager] Environment reset to baseline configuration.");
        }

        /// <summary>
        /// Selects and applies one of the experimental conditions (Condition A, B, or C).
        /// </summary>
        public void ApplyCondition(LowVisibilityCondition condition)
        {
            CaptureBaselineEnvironment();
            _activeCondition = condition;

            LowVisibilityProfile profile = GetProfileForCondition(condition);
            ApplyProfile(profile);
        }

        /// <summary>
        /// Applies an experimental configuration ScriptableObject.
        /// </summary>
        public void ApplyConfiguration(ExperimentConfig config)
        {
            if (config == null) return;
            CaptureBaselineEnvironment();

            // Select condition based on config settings
            if (config.condition == ExperimentalCondition.Baseline_ClearDaylight)
            {
                ApplyCondition(LowVisibilityCondition.ConditionA_Baseline);
            }
            else if (config.fogExtinctionPerMeter > 0.03f)
            {
                ApplyCondition(LowVisibilityCondition.ConditionC_ReducedWithFog);
            }
            else
            {
                ApplyCondition(LowVisibilityCondition.ConditionB_ReducedNighttime);
            }
        }

        /// <summary>
        /// Applies a concrete LowVisibilityProfile to the Unity rendering engine.
        /// </summary>
        public void ApplyProfile(LowVisibilityProfile profile)
        {
            // 1. Ambient Lighting
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = profile.ambientSkyColor;
            RenderSettings.ambientEquatorColor = profile.ambientEquatorColor;
            RenderSettings.ambientGroundColor = profile.ambientGroundColor;
            RenderSettings.ambientIntensity = profile.ambientIntensity;

            // 2. Directional Nighttime Illumination & Shadows
            if (_directionalLight != null)
            {
                _directionalLight.enabled = true;
                _directionalLight.intensity = profile.directionalIntensity;
                _directionalLight.color = profile.directionalColor;
                _directionalLight.shadows = profile.shadowType;
                _directionalLight.shadowStrength = profile.shadowStrength;
                _directionalLight.shadowResolution = UnityEngine.Rendering.LightShadowResolution.High;
                _directionalLight.shadowBias = 0.05f;
                _directionalLight.shadowNormalBias = 0.4f;
            }

            if (_ambientFillLight != null)
            {
                _ambientFillLight.intensity = profile.directionalIntensity * 0.4f;
                _ambientFillLight.color = profile.ambientSkyColor;
            }

            // 3. Distance Atmospheric Fog
            RenderSettings.fog = profile.fogEnabled;
            if (profile.fogEnabled)
            {
                RenderSettings.fogMode = profile.fogMode;
                RenderSettings.fogDensity = profile.fogDensity;
                RenderSettings.fogColor = profile.fogColor;
            }

            // 4. Target Visibility and Silhouette Contrast
            UpdateTargetVisibility(profile.targetContrastMultiplier);

            // 5. Update Telemetry
            NotifyTelemetry();

            Debug.Log(string.Format(
                "[LowVisibilityEnvironmentManager] Applied Condition: {0} | Simulated Target: {1:F1} lux (Calibrated: {2}) | Fog Density: {3:F4} | Ambient: {4:F2}",
                profile.conditionName, profile.simulatedTargetLux, profile.isPhysicallyCalibrated, profile.fogDensity, profile.ambientIntensity
            ));
        }

        private void UpdateTargetVisibility(float contrastMultiplier)
        {
            // Propagate contrast factor to all active tactical targets
            var targets = FindObjectsOfType<TacticalTarget>();
            foreach (var target in targets)
            {
                var renderers = target.GetComponentsInChildren<Renderer>();
                foreach (var r in renderers)
                {
                    if (r.material != null)
                    {
                        // Modulate material tint towards low-contrast mesopic threshold
                        Color baseCol = r.material.color;
                        Color targetCol = Color.Lerp(Color.black, baseCol, contrastMultiplier);
                        r.material.color = targetCol;
                    }
                }
            }
        }

        private void NotifyTelemetry()
        {
            if (_telemetryLogger != null)
            {
                var profile = GetCurrentProfile();
                _telemetryLogger.conditionLabel = string.Format("{0}_{1:F1}luxSimulated", _activeCondition, profile.simulatedTargetLux);
                _telemetryLogger.SetEnvironmentalTelemetry(
                    profile.simulatedTargetLux,
                    profile.fogEnabled ? profile.fogDensity : 0f,
                    profile.targetContrastMultiplier,
                    profile.ambientIntensity
                );
            }
        }

        public LowVisibilityProfile GetCurrentProfile()
        {
            return GetProfileForCondition(_activeCondition);
        }

        public LowVisibilityProfile GetProfileForCondition(LowVisibilityCondition condition)
        {
            switch (condition)
            {
                case LowVisibilityCondition.ConditionA_Baseline:
                    return _profileA_Baseline;
                case LowVisibilityCondition.ConditionB_ReducedNighttime:
                    return _profileB_ReducedNighttime;
                case LowVisibilityCondition.ConditionC_ReducedWithFog:
                    return _profileC_ReducedWithFog;
                case LowVisibilityCondition.Custom:
                    return _customProfile;
                default:
                    return _profileA_Baseline;
            }
        }

        private void InitializeDefaultProfiles()
        {
            // Condition A: Baseline Visibility (clear daylight / bright training baseline, ~5.0 simulated lux target)
            _profileA_Baseline = new LowVisibilityProfile
            {
                conditionName = "Condition A: Baseline Visibility",
                simulatedTargetLux = 5.0f,
                isPhysicallyCalibrated = false,
                directionalIntensity = 0.85f,
                directionalColor = new Color(0.95f, 0.95f, 0.90f),
                shadowType = LightShadows.Soft,
                shadowStrength = 0.85f,
                ambientIntensity = 0.80f,
                ambientSkyColor = new Color(0.70f, 0.75f, 0.82f),
                ambientEquatorColor = new Color(0.50f, 0.55f, 0.60f),
                ambientGroundColor = new Color(0.30f, 0.32f, 0.35f),
                fogEnabled = false,
                fogMode = FogMode.ExponentialSquared,
                fogDensity = 0.001f,
                fogColor = new Color(0.7f, 0.75f, 0.8f),
                targetContrastMultiplier = 1.0f,
                targetVisibilityMaxDistance = 50f
            };

            // Condition B: Reduced Nighttime Visibility (clear night mesopic moonlight, ~1.5 simulated lux target)
            _profileB_ReducedNighttime = new LowVisibilityProfile
            {
                conditionName = "Condition B: Reduced Nighttime Visibility",
                simulatedTargetLux = 1.5f,
                isPhysicallyCalibrated = false,
                directionalIntensity = 0.12f,
                directionalColor = new Color(0.48f, 0.60f, 0.82f), // Moonlight hue
                shadowType = LightShadows.Soft,
                shadowStrength = 0.60f,
                ambientIntensity = 0.18f,
                ambientSkyColor = new Color(0.06f, 0.08f, 0.14f),
                ambientEquatorColor = new Color(0.03f, 0.04f, 0.06f),
                ambientGroundColor = Color.black,
                fogEnabled = true,
                fogMode = FogMode.ExponentialSquared,
                fogDensity = 0.018f, // Mild atmospheric haze
                fogColor = new Color(0.05f, 0.07f, 0.11f),
                targetContrastMultiplier = 0.65f,
                targetVisibilityMaxDistance = 25f
            };

            // Condition C: Reduced Visibility with Fog (deep mesopic nighttime with dense fog, ~0.5 simulated lux target)
            _profileC_ReducedWithFog = new LowVisibilityProfile
            {
                conditionName = "Condition C: Reduced Visibility with Fog",
                simulatedTargetLux = 0.5f,
                isPhysicallyCalibrated = false,
                directionalIntensity = 0.035f,
                directionalColor = new Color(0.35f, 0.45f, 0.65f), // Dim, scattered moonlight
                shadowType = LightShadows.Soft,
                shadowStrength = 0.35f, // Highly diffused shadows in dense fog
                ambientIntensity = 0.06f,
                ambientSkyColor = new Color(0.02f, 0.03f, 0.05f),
                ambientEquatorColor = new Color(0.01f, 0.015f, 0.025f),
                ambientGroundColor = Color.black,
                fogEnabled = true,
                fogMode = FogMode.ExponentialSquared,
                fogDensity = 0.065f, // Heavy fog: targets beyond 12-15m severely attenuated
                fogColor = new Color(0.04f, 0.05f, 0.08f),
                targetContrastMultiplier = 0.35f,
                targetVisibilityMaxDistance = 12f
            };

            // Custom profile defaults to Condition C
            _customProfile = _profileC_ReducedWithFog;
            _customProfile.conditionName = "Custom Low-Visibility Profile";
        }
    }
}
