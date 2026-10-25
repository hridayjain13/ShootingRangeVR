using System;
using UnityEngine;
using IVRAR.Group02.Environment;

namespace IVRAR.Group02.Core
{
    public enum ExperimentalCondition
    {
        Baseline_ClearDaylight,
        LowVisibility_NoStress,
        LowVisibility_SensoryStress
    }

    /// <summary>
    /// ScriptableObject defining independent variables and parameters for research trials.
    /// Allows rapid experiment switching without modifying scene objects or recompiling code.
    /// </summary>
    [CreateAssetMenu(fileName = "ExperimentConfig", menuName = "IVRAR/Experiment Configuration", order = 1)]
    public class ExperimentConfig : ScriptableObject
    {
        [Header("Experiment Identity")]
        public string configurationName = "Default Research Condition";
        public ExperimentalCondition condition = ExperimentalCondition.LowVisibility_SensoryStress;
        [TextArea(2, 4)]
        public string description = "Mesopic nighttime low visibility (0.05-0.15 lux) with peripheral strobe cues (6Hz) and spatialized acoustic gunshot/alarm stressors.";

        [Header("Phase 03 Low-Visibility Experimental Condition")]
        [Tooltip("Standardized experimental condition preset.")]
        public LowVisibilityCondition lowVisibilityCondition = LowVisibilityCondition.ConditionC_ReducedWithFog;

        [Tooltip("Simulated research illuminance target in lux (0.5 to 5.0 lux). NOTE: Unity light intensity is an uncalibrated simulation target, not a verified physical measurement at the eye.")]
        [Range(0.5f, 5.0f)]
        public float simulatedIlluminationTargetLux = 0.5f;

        [Tooltip("True only if physical lux meter measurement has been verified at the headset lens aperture.")]
        public bool isPhysicallyCalibrated = false;

        [Tooltip("Target visual luminance contrast multiplier (0.1 = low silhouette contrast, 1.0 = baseline contrast).")]
        [Range(0.1f, 1.0f)]
        public float targetContrastMultiplier = 0.35f;

        [Tooltip("Real-time shadow strength (0 = no shadow, 1 = full darkness shadow).")]
        [Range(0f, 1f)]
        public float shadowStrength = 0.35f;

        [Header("Low-Visibility & Optical Environment")]
        [Tooltip("Target ambient illuminance in lux (photopic/mesopic boundary is ~0.01-3.0 lux).")]
        [Range(0.01f, 50f)]
        public float ambientLux = 0.12f;

        [Tooltip("Extinction coefficient for distance fog (per meter).")]
        [Range(0f, 0.2f)]
        public float fogExtinctionPerMeter = 0.045f;

        [Tooltip("Directional moonlight/ambient light intensity factor.")]
        [Range(0.01f, 1f)]
        public float directionalIntensity = 0.08f;

        [Tooltip("Tactical flashlight cone angle in degrees.")]
        [Range(15f, 75f)]
        public float flashlightSpotAngle = 35f;

        [Tooltip("Tactical flashlight effective beam range in meters.")]
        [Range(5f, 60f)]
        public float flashlightRange = 30f;

        [Tooltip("Tactical flashlight base luminous intensity.")]
        [Range(0.1f, 5f)]
        public float flashlightIntensity = 1.6f;

        [Header("Sensory Stressor Configuration")]
        [Tooltip("Strobe flash frequency in Hz. Capped at 6.0 Hz to prevent photosensitive epileptic discomfort.")]
        [Range(1f, 6f)]
        public float strobeFrequencyHz = 6.0f;

        [Tooltip("Maximum consecutive strobe pulses in a single burst (capped at 6 pulses).")]
        [Range(1, 6)]
        public int maxStrobePulses = 4;

        [Tooltip("Maximum strobe light intensity.")]
        [Range(0.05f, 0.5f)]
        public float strobeIntensity = 0.20f;

        [Tooltip("Maximum volume multiplier for gunshot acoustic cue (safety capped).")]
        [Range(0.1f, 1.0f)]
        public float maxGunshotCueGain = 0.75f;

        [Tooltip("Maximum volume multiplier for alarm acoustic cue (safety capped).")]
        [Range(0.1f, 1.0f)]
        public float maxAlarmCueGain = 0.60f;

        [Tooltip("Probability of triggering an acoustic startle during target presentation.")]
        [Range(0f, 1f)]
        public float acousticStartleProbability = 0.40f;

        [Header("Target Presentation & Protocol")]
        [Tooltip("Probability that a spawned target is Hostile (1 - P is Civilian).")]
        [Range(0.1f, 0.9f)]
        public float hostileProbability = 0.50f;

        [Tooltip("Average inter-target arrival interval in seconds (Poisson distribution mean).")]
        [Min(0.5f)]
        public float meanInterTargetIntervalSeconds = 3.0f;

        [Tooltip("Time window in seconds before an unengaged target is marked as Miss / Correct Rejection.")]
        [Min(0.5f)]
        public float exposureDurationSeconds = 2.5f;

        [Tooltip("Target presentation movement speed in meters per second.")]
        [Min(0f)]
        public float targetMovementSpeed = 1.2f;

        [Tooltip("Total trials in this experimental block.")]
        [Range(5, 100)]
        public int totalTrialsInBlock = 20;

        [Header("Telemetry & Session Defaults")]
        public string defaultExportDirectory = "telemetry";
        public string trialLogFileName = "reaction_latency_trial_log.csv";
        public string assessmentLogFileName = "post_trial_assessments.csv";
        [Range(60, 120)]
        public int headKinematicsSampleRateHz = 90;
        public int headJitterWindowSize = 90;

        private void OnValidate()
        {
            // Safety constraints
            strobeFrequencyHz = Mathf.Clamp(strobeFrequencyHz, 1f, 6f);
            maxStrobePulses = Mathf.Clamp(maxStrobePulses, 1, 6);
            strobeIntensity = Mathf.Clamp(strobeIntensity, 0.05f, 0.35f);
            maxGunshotCueGain = Mathf.Clamp01(maxGunshotCueGain);
            maxAlarmCueGain = Mathf.Clamp01(maxAlarmCueGain);
            hostileProbability = Mathf.Clamp(hostileProbability, 0.1f, 0.9f);
            if (exposureDurationSeconds < 0.5f) exposureDurationSeconds = 0.5f;
            if (meanInterTargetIntervalSeconds < 0.5f) meanInterTargetIntervalSeconds = 0.5f;
            if (totalTrialsInBlock < 5) totalTrialsInBlock = 5;
        }
    }
}
