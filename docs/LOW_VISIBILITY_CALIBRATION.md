# IVRAR Group 02: Low-Visibility Nighttime Simulation & Photometric Calibration Guide

## Purpose & Scope
This document specifies the optical modeling, experimental conditions, calibration procedures, and performance constraints for **Phase 03: Low-Visibility Nighttime Simulation** in the *Immersive VR Sensory-Stress Defensive Security Training under Low Visibility* project.

---

## 1. Photometric Reality vs. Engine Illumination Units

> [!IMPORTANT]
> **Unity light intensity values (scalar shader arbitrary units) are NOT automatically equivalent to measured physical illuminance (photometric lux, $\text{lm/m}^2$) incident at a human participant's cornea.**
>
> In software and experimental protocols, all uncalibrated conditions are formally designated as **Simulated Illumination Targets**. No claim of empirical photometric accuracy is made until verified through physical measurement at the headset display aperture.

### The Display Pipeline Disconnect
In real-world tactical environments, illuminance ($E$, in lux) directly governs retinal ganglion cell excitation and the photopic-to-mesopic transition:
1. **Physical World**: Real moonlit sky provides $\approx 0.01 - 0.2\text{ lux}$; street-lit security corridors provide $\approx 1.0 - 5.0\text{ lux}$.
2. **Unity Engine World**: Light components use unitless scalar multipliers (e.g. `intensity = 0.08`), rendered through arbitrary Universal Render Pipeline (URP) shaders.
3. **Headset Display Hardware**: Target color buffers pass through GPU tone-mapping, color space transformation (sRGB), display panel luminance profiles ($L_{\text{peak}} \approx 100 - 150\text{ cd/m}^2$ for typical Fast-LCD VR panels), and optical Fresnel/pancake lens attenuation.

Therefore, achieving a target mesopic range requires **empirical spectroradiometric or photodiode calibration** directly at the headset eye relief plane.

---

## 2. Experimental Condition Definitions

Three standardized, reproducible experimental conditions are implemented via [`LowVisibilityEnvironmentManager.cs`](file:///c:/Users/hrida/Downloads/ShootingRangeVR/Assets/Scripts/Environment/LowVisibilityEnvironmentManager.cs):

| Parameter | Condition A: Baseline Visibility | Condition B: Reduced Nighttime Visibility | Condition C: Reduced Visibility with Fog |
|---|---|---|---|
| **Formally Certified Title** | Baseline Training Range | Mesopic Nighttime Surveillance | Deep Mesopic Low-Visibility Defense |
| **Simulated Target Illuminance** | **$5.0\text{ lux}$ (Simulated)** | **$1.5\text{ lux}$ (Simulated)** | **$0.5\text{ lux}$ (Simulated)** |
| **Physical Calibration State** | *Uncalibrated Simulation Target* | *Uncalibrated Simulation Target* | *Uncalibrated Simulation Target* |
| **Directional Moonlight Intensity** | $0.85$ (Daylight White) | $0.12$ (Mesopic Moonlight, $4800\text{ K}$) | $0.035$ (Diffused Low Moonlight) |
| **Realtime Shadows** | Soft Shadows (Strength: $0.85$) | Soft Shadows (Strength: $0.60$) | Diffuse Soft Shadows (Strength: $0.35$) |
| **Ambient Trilight Intensity** | $0.80$ | $0.18$ | $0.06$ |
| **Atmospheric Fog Active** | `false` | `true` | `true` |
| **Fog Mathematical Model** | Linear / Minimal ($0.001\text{ m}^{-1}$) | Exponential Squared ($\text{Exp}^2$) | Exponential Squared ($\text{Exp}^2$) |
| **Fog Extinction Coeff ($\beta$)** | $\beta = 0.001\text{ m}^{-1}$ | $\beta = 0.018\text{ m}^{-1}$ (Mild Haze) | $\beta = 0.065\text{ m}^{-1}$ (Heavy Fog) |
| **Visual Target Contrast Multiplier** | $1.00$ ($100\%$ High Contrast) | $0.65$ (Reduced Contrast) | $0.35$ (Low Silhouette Contrast) |
| **Effective Target Resolvability** | $> 50\text{ m}$ | $\approx 25\text{ m}$ | $\le 12\text{ m}$ |
| **Tactical Flashlight Role** | Optional / Secondary | Informative target identification | Critical search illumination |

---

## 3. Optical Attenuation & Beer-Lambert Law

Atmospheric light extinction in Condition B and Condition C follows the exponential Beer-Lambert formulation:

$$I(d) = I_0 \cdot e^{-\beta \cdot d}$$

Where:
- $I_0$ is the unattenuated target luminance.
- $\beta$ is the atmospheric extinction coefficient per meter (`fogDensity`).
- $d$ is the Euclidean distance from trainee camera to tactical target ($3\text{ m}$, $8\text{ m}$, $14\text{ m}$).

### Visual Range Attenuation Table ($\beta = 0.065\text{ m}^{-1}$)
- **At $3.0\text{ m}$ (Close Target Anchor)**: Transmission $T = e^{-0.065 \times 3} \approx 82.3\%$. Target clearly identifiable.
- **At $8.0\text{ m}$ (Mid Target Anchor)**: Transmission $T = e^{-0.065 \times 8} \approx 59.5\%$. Visual identification requires active weapon flashlight focus.
- **At $14.0\text{ m}$ (Far Target Anchor)**: Transmission $T = e^{-0.065 \times 14} \approx 40.3\%$. Target blends into background noise, requiring rapid identification under sensory stress.

---

## 4. Physical Photometric Calibration Procedure

To elevate any condition from *Simulated Illumination Target* to *Empirically Calibrated Lux*:

### Required Hardware Instrumentation
1. **Calibrated Illuminance Meter / Photometer**:
   - E.g., Konica Minolta T-10A, Thorlabs PM100D with optical power head, or equivalent cosine-corrected illuminance sensor.
2. **Custom VR Lens Fixture**:
   - 3D-printed light-tight adapter ring securing the sensor head perpendicular to the VR headset display lens at standard eye-relief distance ($12 - 15\text{ mm}$).
3. **Neutral Uniform Target Field**:
   - Standard 18% photographic gray card visual scene rendered at infinite focal depth.

### Step-by-Step Measurement Protocol
1. **Darkroom Isolation**:
   - Perform all measurements in a darkroom with ambient room illuminance $< 0.005\text{ lux}$.
2. **Display Panel Warm-Up**:
   - Power on the VR headset and keep display active for a minimum of 15 minutes to stabilize thermal backlight emission.
3. **Baseline Sensor Zeroing**:
   - Cap the photometer sensor, zero the instrument, and verify dark current $< 0.001\text{ lux}$.
4. **Per-Eye Optical Acquisition**:
   - Mount sensor fixture on the left lens aperture; record 10 consecutive readings at 1 Hz. Repeat for right lens aperture.
5. **Unity Scalar Adjustments**:
   - Adjust `directionalIntensity` and `ambientIntensity` until measured physical illuminance at the lens matches the desired target:
     - Target A: $5.00 \pm 0.25\text{ lux}$
     - Target B: $1.50 \pm 0.10\text{ lux}$
     - Target C: $0.50 \pm 0.05\text{ lux}$
6. **Provenance Recording**:
   - When verified, set `isPhysicallyCalibrated = true` in [`LowVisibilityEnvironmentManager.cs`](file:///c:/Users/hrida/Downloads/ShootingRangeVR/Assets/Scripts/Environment/LowVisibilityEnvironmentManager.cs), enter the calibration serial number, and change `dataStatus` from `unverified` to `empirical` in telemetry exports.

---

## 5. VR Frame Performance & Rendering Constraints

Low-visibility mesopic training requires strict adherence to VR performance limits to avoid motion sickness and reprojection artifacts:

1. **Target Frame Rate**:
   - Maintain steady **$90\text{ FPS}$** ($11.1\text{ ms}$ frame budget) on Meta Quest Link / SteamVR.
2. **No Heavy Post-Processing**:
   - Screen-space reflections, multi-pass bloom, motion blur, and depth-of-field are **disabled**.
   - These post-effects degrade edge contrast, cause temporal shimmer in dark environments, and introduce GPU frame spikes that break VR tracking stability.
3. **Single Directional Shadow Caster**:
   - Real-time shadows are limited strictly to the primary directional moonlight.
   - Flashlight spot light uses per-pixel forward rendering without dynamic shadow cascades to preserve draw-call budgets.
4. **Fog Performance**:
   - Hardware-accelerated native URP vertex/pixel fog (`RenderSettings.fog = true`, `FogMode.ExponentialSquared`) is used rather than expensive volumetric marching shaders.

---

## 6. Telemetry Recording Verification

All environmental parameters applied during a trial session are logged to [`telemetry/reaction_latency_trial_log.csv`](file:///c:/Users/hrida/Downloads/ShootingRangeVR/telemetry/reaction_latency_trial_log.csv):

```csv
TimestampUtc,ParticipantID,SessionID,Condition,DataStatus,SimulatedLuxTarget,FogDensity,TargetContrast,AmbientIntensity,TrialID,TargetType,TraineeAction,LatencyMs,Hit,Miss,FalseAlarm,CorrectRejection,DistanceMeters,PresentationAngleDeg,HeadYawJitterVarianceDeg2,DPrime,BetaCriterion
2026-10-25T17:40:00Z,P01_TEST,S01_BLOCK1,ConditionC_ReducedWithFog_0.5luxSimulated,unverified,0.50,0.0650,0.35,0.06,T01,Hostile,Shoot,462.4,1,0,0,0,8.00,0.00,0.0142,2.450,0.920
```

This guarantees full scientific provenance: every recorded reaction time and signal detection metric ($d'$, $\beta$) is directly correlated with the exact simulated illuminance target and atmospheric fog density present during that specific trial.
