# IVRAR Group 02: Headset-Specific Manual Testing Guide

## Purpose
This document specifies the verification protocols, hardware checks, and manual test procedures required for **Phase 02: OpenXR and VR Interaction Integration**.

While desktop emulation mode (`DesktopFallbackController.cs`) enables standalone syntax, interaction, and telemetry testing without VR hardware, **headset-specific perceptual, optical, and kinematic properties must still be validated manually on a physical 6-DoF OpenXR headset**.

---

## 1. Supported Hardware Rigs & OpenXR Runtimes

| Headset Target | Connection Method | Target Refresh Rate | OpenXR Active Runtime Provider |
|---|---|---|---|
| **Meta Quest 2** | Oculus Link (USB-C 3.0) / AirLink | 72 Hz / 90 Hz | Meta Quest OpenXR Runtime (`oculus_openxr`) |
| **Meta Quest 3** | Oculus Link (USB-C 3.0) / AirLink | 90 Hz / 120 Hz | Meta Quest OpenXR Runtime (`oculus_openxr`) |
| **Valve Index** | DisplayPort + USB 3.0 | 90 Hz / 120 Hz / 144 Hz | SteamVR OpenXR Runtime (`steamvr`) |
| **HTC Vive Pro / Cosmos** | DisplayPort + USB 3.0 | 90 Hz | SteamVR OpenXR Runtime (`steamvr`) |
| **Windows Mixed Reality** | HDMI/DP + USB 3.0 | 60 Hz / 90 Hz | Windows Mixed Reality OpenXR Runtime |

---

## 2. Pre-Flight Verification Checklist

Before launching a participant training or data collection session:

- [ ] **XR Loader Initialization**:
  - Open `Assets/Scenes/Group02_MainScene.unity`.
  - Confirm the on-screen diagnostics panel (`F3`) reports `Status: INITIALIZED` and the active loader matches your connected runtime (e.g. `OpenXR Loader` or `Oculus Loader`).
  - **Truthful Hardware Rule**: Confirm that `Headset: VERIFIED` is `TRUE` and that `Reported Refresh Rate` reflects the actual physical panel frequency (e.g. `90.0 Hz` or `72.0 Hz`). If unverified or running in desktop fallback, do not certify participant data as verified.
- [ ] **Guardian / Chaperone Boundary**:
  - Confirm a clear $2.5\text{ m} \times 2.5\text{ m}$ minimum tracking volume.
- [ ] **Controller Battery Levels**:
  - Ensure both Left and Right Touch/Index controllers have $\ge 50\%$ battery.

---

## 3. Manual Headset Verification Protocols

### Protocol A: 6-DoF Head Pose Tracking & Kinematics Jitter
1. Don the VR headset.
2. Slowly rotate head in yaw, pitch, and roll axes:
   - Verify smooth 1:1 motion without angular swimming, judder, or reprojection hitching.
3. Rapidly perform lateral head tilts and small tremors:
   - Check the diagnostic panel / telemetry log (`HeadYawJitterVarianceDeg2`).
   - Confirm variance scales dynamically with trainee agitation without sensor drift.

### Protocol B: Symmetrical Left & Right Controller Input Bindings
1. **Primary Trigger (Index Finger)**:
   - Aim ray at **[Start Simulation]** button and pull index trigger.
   - Confirm button click event fires and haptic vibration pulse is felt on the controller.
2. **Primary Button (A on Right / X on Left)**:
   - When a hostile or civilian target appears, press Primary Button.
   - Confirm immediate simulated response firing and hit registration in telemetry.
3. **Thumbstick Click (Stick Depression)**:
   - Move off-center and press the thumbstick in.
   - Verify the participant's view instantaneously recenters to directly face the shooting range target corridor.
4. **Secondary Button (B on Right / Y on Left)**:
   - Press Secondary Button during active simulation.
   - Confirm simulation pauses immediately (`Time.timeScale = 0`), target timer freezes, and the in-VR Pause modal opens.
   - Press **[Resume Simulation]** to confirm smooth unpausing.

### Protocol C: Low-Visibility Mesopic Optical Quality
1. Verify that the mesopic night sky and ambient illuminance match low-light conditions (~0.05–0.15 lux).
2. Point the weapon-mounted tactical flashlight towards near (3m), mid (8m), and far (14m) target anchors:
   - Confirm smooth inverse-square luminous attenuation ($1/r^2$).
   - Confirm distance fog progressively occludes targets beyond 15 meters.

### Protocol D: Sensory Stressor Safety & Audio Capping
1. **Peripheral Strobe Flash**:
   - Observe the peripheral strobe pulses during hostile presentation.
   - Confirm flashes are confined strictly to peripheral vision and flash rate never exceeds 6.0 Hz ($\le 4$ pulses per train).
   - Ensure no direct central photic blinding occurs.
2. **Acoustic Startle Cue**:
   - Verify gunshot startle cues and security sirens play with full 3D spatialization.
   - Confirm peak loudness is safe and comfortable (<85 dB SPL equivalent).

---

## 4. Desktop Fallback Testing Protocol (Offline / Non-VR)

When testing without a connected VR headset:
1. Open the project in Unity Editor and press **Play**.
2. If no headset is detected, `DesktopFallbackController` activates automatically.
3. Use the following controls:
   - **Mouse Movement**: 360-degree head look.
   - **WASD**: Forward, left, backward, right translation.
   - **Left Click**: Fire weapon / activate UI button targeted by laser pointer.
   - **Spacebar**: Immediate simulated response trigger.
   - **Right Click / F**: Toggle tactical flashlight.
   - **C or R**: Recenter camera to target corridor.
   - **Escape or P**: Toggle pause menu.
   - **F12 or Ctrl+Q**: Immediate application termination.
   - **F1 / Tab**: Toggle Desktop Emulation on/off.
   - **F3**: Toggle Hardware Diagnostics HUD.
