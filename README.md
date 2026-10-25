# IVRAR GROUP 02
## Immersive VR Sensory-Stress Defensive Security Training under Low Visibility

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Unity](https://img.shields.io/badge/Unity-2020.3.25f1%20LTS-blue.svg)](https://unity.com/)
[![Render Pipeline](https://img.shields.io/badge/Render%20Pipeline-Universal%20RP-orange.svg)](https://unity.com/srp/universal-render-pipeline)
[![XR](https://img.shields.io/badge/XR-OpenXR%20%7C%20SteamVR%20%7C%20Oculus-green.svg)](https://www.khronos.org/openxr/)

---

## 🎯 Executive Summary & Research Domain

**Authorized Research Question:**  
> *"How can an immersive VR sensory-stress simulation improve target prioritization and reaction latency for defensive security trainees under simulated low-visibility nighttime conditions?"*

This repository houses the Phase 01 architecture, software systems, and experimental testbed for **IVRAR Group 02** (SVKM's NMIMS MPSTME, AY2026–27). The system evaluates cognitive workload, situational awareness, and shoot/don't-shoot decision dynamics under combined mesopic visual degradation (distance fog, ambient light dimming) and sudden acute sensory stressors (peripheral 6Hz strobe bursts, spatialized gunshot/alarm audio cues).

---

## 👥 IVRAR Group 02: Team Roster & Technical Responsibilities

| Roll No | Name | Role | Primary Technical Responsibilities |
|---|---|---|---|
| **N024** | **Ranish Devadiga** | Lead XR Systems Architect | Unity OpenXR nighttime mesopic rendering, volumetric low-light fog shaders, dynamic flashlight cone inverse-square falloff, peripheral strobe startle cues, and 6-DoF defensive weapon rig. |
| **N042** | **Hriday Jain** | Human Factors & Usability Engineer | Multi-dimensional NASA-TLX cognitive workload assessment, Situational Awareness Global Assessment Technique (SAGAT), Kennedy SSQ cybersickness monitoring, and stress inoculation training (SIT) efficacy. |
| **N047** | **Vedika Kaki** | Spatial Telemetry & Data Lead | Signal Detection Theory (SDT) sensitivity index ($d'$) and decision criterion ($\beta$), shoot/don't-shoot millisecond reaction latency logging, false alarm error rate reduction, and paired t-test hypothesis testing. |
| **N062** | **Medha Mishra** | Techno-Managerial Product Manager | Defensive training operational economics, live-fire ammunition consumption substitution (350 rounds/trainee), physical shoot-house facility rental replacement, operational cost parity ($\kappa = 0.042$), and capital payback horizon (10.9 months). |

---

## 🖥️ In-VR Startup Menu System

The dedicated Group 02 entry scene (`Assets/Scenes/Group02_MainScene.unity`) features a world-space holographic UI menu displaying:

```
========================================================================================
                                     IVRAR GROUP 02
             Immersive VR Sensory-Stress Defensive Security Training under Low Visibility
========================================================================================
  [ ▶ START SIMULATION ]       - Initiates active Poisson trial sequence & stress envelope
  [ ⚙ PARTICIPANT SETUP ]      - Configures pseudonymous Participant ID, Session & Cohort
  [ 🔬 RESEARCH CONFIGURATION] - Adjusts ScriptableObject fog, strobe, and trial parameters
  [ 👥 CREDITS ]               - Detailed team breakdown and engineering responsibilities
  [ 📜 ATTRIBUTION ]           - Original author credit, license notices, and base heritage
  [ ✕ EXIT ]                   - Gracefully terminates application & flushes telemetry
========================================================================================
```

### Menu Subsystems
1. **Start Simulation**: Transitions from idle into the active defense scenario. Sets mesopic lighting, initiates randomized target presentation across left/center/right anchors, triggers sensory stress envelopes, and launches the post-trial survey upon completion.
2. **Participant Setup**: Dedicated in-VR input panel ensuring research ethics compliance by restricting participant identifiers strictly to pseudonymous codes (e.g. `P01`, `P02`).
3. **Research Configuration**: Exposes runtime tuning for `ExperimentConfig` ScriptableObject variables:
   - Fog extinction per meter ($\alpha \in [0.0, 0.15]$)
   - Strobe pulse frequency ($1.0 - 6.0\text{ Hz}$ safe physiological boundary)
   - Total experimental block trials ($5 - 50$)
4. **Credits & Attribution**: Displays technical responsibilities for the four group engineers and provides explicit MIT License attribution to the original author.

---

## 🏛️ Project Architecture & Modular Directory Structure

The project has been restructured into discrete, decoupled layers separating simulation logic, environmental rendering, sensory stress induction, target management, telemetry, human factors, and UI:

```
Assets/
  Scenes/
    Group02_MainScene.unity         <-- [Active Scene 0] Group 02 dedicated research & menu scene
    ShootingRange/
      ShootingRange.unity           <-- [Scene 1] Original baseline scene (100% preserved)
      ShootingRange_OriginalBackup.unity <-- [Scene 2] Explicit preserved backup of baseline scene
  Scripts/
    Core/                           <-- SimulationManager, ExperimentConfig (ScriptableObject), SimulationState
    Environment/                    <-- LowVisibilityEnvironmentController, FlashlightController
    SensoryStress/                  <-- SensoryStressManager, StrobeStressController, AcousticStressController
    TargetManagement/               <-- SensoryStressTargetManager, TacticalTarget, TargetTrialMetadata
    Telemetry/                      <-- ReactionLatencyTelemetryLogger, SignalDetectionMetrics, HeadKinematicsLogger
    HumanFactors/                   <-- NasaTlxAssessment, KennedySsqAssessment, SagatAssessment, PostTrialAssessmentController
    UI/                             <-- MainMenuController, ParticipantSetupUI, ResearchConfigUI, HUDController
  Prefabs/
    Group02/                        <-- HostileTarget.prefab, CivilianTarget.prefab
  Materials/                        <-- Target and environment shaders/materials
  Audio/                            <-- Spatialized gunfire, siren, alarm audio assets
  Lighting/                         <-- Volume profiles and mesopic lighting assets
  ScriptableObjects/                <-- Config_Baseline, Config_LowVisibility_SensoryStress, Config_DefaultResearch
  Tests/
    Editor/                         <-- SignalDetectionMetricsTests, ExperimentConfigTests
    Runtime/                        <-- TelemetryLoggerTests, SimulationLifecycleTests

telemetry/                          <-- Empirical trial CSV logs, Python statistical scripts, benchmark datasets
docs/                               <-- Technical dossier, literature reviews, empirical protocol, architecture specs
```

---

## 🔬 Subsystem Responsibilities

### 1. Simulation Core (`IVRAR.Group02.Core`)
- **`SimulationManager.cs`**: Primary finite state machine governing session states (`Idle`, `RunningTrial`, `SensoryStressActive`, `TargetPresented`, `PostTrialAssessment`, `SessionCompleted`).
- **`ExperimentConfig.cs`**: `ScriptableObject` enabling researchers to save, load, and hot-swap experimental parameters (ambient lux, strobe frequency, cue volumes, target speed, trial count) without modifying scenes.

### 2. Low-Visibility Environment (`IVRAR.Group02.Environment`)
- **`LowVisibilityEnvironmentController.cs`**: Dynamically adjusts directional moonlight (0.08 intensity) and exponential-squared distance fog ($\alpha = 0.045\text{ m}^{-1}$) to reproduce mesopic low-visibility conditions.
- **`FlashlightController.cs`**: Evaluates target incident illuminance ($E = \frac{I_0 \cos\theta}{r^2} e^{-\alpha r}$) based on trainee orientation and distance.

### 3. Sensory Stress System (`IVRAR.Group02.SensoryStress`)
- **`StrobeStressController.cs`**: Generates peripheral visual startle flashes capped strictly at 6.0 Hz and $\le 6$ pulses to maintain participant photic comfort while testing vigilance.
- **`AcousticStressController.cs`**: Emits spatialized 3D audio cues (gunshot cracks, security sirens) with safe volume caps (<85 dB SPL equivalent).
- **`SensoryStressManager.cs`**: Integrates visual and auditory stress envelopes with target presentations.

### 4. Target Management (`IVRAR.Group02.TargetManagement`)
- **`TacticalTarget.cs`**: Hostile (weapon-wielding) vs. Civilian (unarmed neutral) target model implementing `Interfaces.IDamageable` for full interoperability with baseline ballistics.
- **`TargetTrialMetadata.cs`**: Records presentation timestamps, target distance, azimuth angle, and calculates millisecond reaction latency.
- **`SensoryStressTargetManager.cs`**: Governs target spawning using a Poisson inter-arrival distribution ($t = -\ln(U) \cdot \mu$) across spatial anchors.

### 5. Telemetry & Analytics (`IVRAR.Group02.Telemetry`)
- **`ReactionLatencyTelemetryLogger.cs`**: Logs millisecond-precision decision latency, Hit, Miss, False Alarm, Correct Rejection, distance, angle, and 90Hz head yaw jitter to `telemetry/reaction_latency_trial_log.csv`.
- **`SignalDetectionMetrics.cs`**: Computes Signal Detection Theory sensitivity index ($d'$), decision criterion ($\beta$), response bias ($c$), and non-parametric $A'$ using the Hautus (1995) log-linear correction.
- **`HeadKinematicsLogger.cs`**: Tracks rotational velocity and angular jitter as exploratory stress proxies.

### 6. Human Factors Assessment (`IVRAR.Group02.HumanFactors`)
- **`NasaTlxAssessment.cs`**: Captures Mental Demand, Physical Demand, Temporal Demand, Performance, Effort, and Frustration.
- **`KennedySsqAssessment.cs`**: Tracks Nausea, Oculomotor, and Disorientation symptoms.
- **`SagatAssessment.cs`**: Tests Situational Awareness (target type perception and spatial localization).
- **`PostTrialAssessmentController.cs`**: Displays interactive in-VR assessment sliders immediately following experimental blocks.

---

## 🛡️ Attribution & Baseline Compatibility

### Original Author Attribution
This project is an academic research extension of **ShootingRangeVR**:
- **Original Author**: Anton Pokidov ([kaayran](https://github.com/kaayran))
- **Original Repository**: [https://github.com/kaayran/ShootingRangeVR](https://github.com/kaayran/ShootingRangeVR)
- **License**: MIT License (Copyright 2022 Anton Pokidov)

### Baseline Mechanics Retained
All original weapon handling, cartridge cycling, and equipment mechanics remain **100% functional and intact**:
- **Glock 17**: Slide rack, magazine insertion/ejection, striker pin firing, realistic cartridge chambering, and bullet ejection.
- **F-1 Fragmentation Grenade**: Pull ring, release lever, fuse detonation, and explosive fragment physics.
- **Tactical Equipment**: Altyn helmet with audio-dampening visor and Gzhel ceramic plate body armor.
- **Original Scene**: `Assets/Scenes/ShootingRange/ShootingRange.unity` is completely intact and backed up at `Assets/Scenes/ShootingRange/ShootingRange_OriginalBackup.unity`.

---

## 🚀 Getting Started

### Prerequisites
- **Unity Editor**: `2020.3.25f1 LTS`
- **Render Pipeline**: Universal Render Pipeline (URP 10.7.0)
- **VR Runtimes**: SteamVR / OpenVR / Oculus XR Plugin
- **Python**: Python 3.8+ (for telemetry processing in `telemetry/`)

### Setup Instructions
1. Clone the repository:
   ```bash
   git clone https://github.com/hridayjain13/ShootingRangeVR.git
   ```
2. Open the project in **Unity 2020.3.25f1**.
3. In the Project window, open:
   - **`Assets/Scenes/Group02_MainScene.unity`** for the IVRAR Group 02 Main Menu and Sensory-Stress Simulation.
   - **`Assets/Scenes/ShootingRange/ShootingRange.unity`** for the original baseline shooting range.
4. Press **Play** in the Unity Editor or build for PCVR/Quest.

---

## 📄 License
This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.
All baseline code copyright (c) 2022 Anton Pokidov.
IVRAR Group 02 extensions copyright (c) 2026 Ranish Devadiga, Hriday Jain, Vedika Kaki, Medha Mishra.
