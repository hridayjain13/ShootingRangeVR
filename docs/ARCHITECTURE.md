# IVRAR Group 02: Software Architecture & Subsystem Specification

## System Overview
The **IVRAR Group 02** project restructures and extends the baseline VR shooting range into an empirical research and training simulator: **"Immersive VR Sensory-Stress Defensive Security Training under Low Visibility"**.

```
+-----------------------------------------------------------------------------------+
|                                  MAIN MENU UI                                     |
|  - Start Simulation   - Participant Setup   - Research Config   - Credits / Exit  |
+-----------------------------------------+-----------------------------------------+
                                          |
                                          v
+-----------------------------------------------------------------------------------+
|                               SIMULATION MANAGER                                  |
|   Coordinates trial lifecycle, state transitions, timers, and subsystem events    |
+-----------------------------------------+-----------------------------------------+
       |                     |                         |                      |
       v                     v                         v                      v
+--------------+     +---------------+        +------------------+    +-------------+
| ENVIRONMENT  |     | SENSORY STRESS|        | TARGET MANAGEMENT|    |  TELEMETRY  |
| - Low Light  |     | - Strobe 6Hz  |        | - Hostile/Civ    |    | - Reaction  |
| - Dist Fog   |     | - Acoustic    |        | - Poisson Timing |    |   Latency   |
| - Flashlight |     |   Gunfire/Alm |        | - Exposure Window|    | - SDT d'/b  |
+--------------+     +---------------+        +------------------+    | - Head Yaw  |
                                                                      +------+------+
                                                                             |
                                                                             v
                                                                      +-------------+
                                                                      |HUMAN FACTORS|
                                                                      | - NASA-TLX  |
                                                                      | - SAGAT     |
                                                                      | - SSQ       |
                                                                      +-------------+
```

## Subsystem Breakdown

### 1. Core (`IVRAR.Group02.Core`)
- `SimulationState.cs`: Finite state machine (`Idle`, `RunningTrial`, `SensoryStressActive`, `TargetPresented`, `PostTrialAssessment`, `SessionCompleted`, etc.).
- `ExperimentConfig.cs`: ScriptableObject defining independent research variables (ambient lux, fog density, strobe rate, target speed, trial count). Enables instant condition switching without recompilation.
- `SimulationManager.cs`: Primary orchestrator coordinating environment modifiers, stress triggers, target presentation, and telemetry recording.

### 2. Environment System (`IVRAR.Group02.Environment`)
- `LowVisibilityEnvironmentController.cs`: Simulates realistic nighttime mesopic conditions (0.05–0.15 lux) and exponential-squared distance fog.
- `FlashlightController.cs`: Tactical weapon-mounted/handheld light implementing inverse-square falloff $E = \frac{I_0 \cos\theta}{r^2} e^{-\alpha r}$ and target illuminance estimation.

### 3. Sensory Stress System (`IVRAR.Group02.SensoryStress`)
- `StrobeStressController.cs`: Peripheral strobe visual cues capped strictly at 6.0 Hz and $\le 6$ pulses to prevent photic discomfort.
- `AcousticStressController.cs`: Spatialized 3D gunfire and alarm cues with gain limiters for trainee hearing safety.
- `SensoryStressManager.cs`: Synchronizes stressor bursts with target presentation envelopes.

### 4. Target Management (`IVRAR.Group02.TargetManagement`)
- `TacticalTarget.cs`: Target component distinguishing between Hostile (shoot) and Civilian (don't-shoot) threats. Implements `Interfaces.IDamageable` for seamless compatibility with legacy Glock 17 ballistics.
- `TargetTrialMetadata.cs`: Records presentation timestamps, target distance, azimuth angle, and calculates millisecond decision latency.
- `SensoryStressTargetManager.cs`: Spawns randomized targets according to Poisson inter-arrival intervals and drives presentation timeouts.

### 5. Telemetry & Data Logging (`IVRAR.Group02.Telemetry`)
- `ReactionLatencyTelemetryLogger.cs`: High-precision CSV logger capturing trial-by-trial outcomes (Hit, Miss, False Alarm, Correct Rejection), latency (ms), and rolling 90Hz head yaw jitter.
- `SignalDetectionMetrics.cs`: Pure mathematical library implementing Hautus log-linear Signal Detection Theory ($d'$, $\beta$, $c$, $A'$).
- `HeadKinematicsLogger.cs`: 90Hz headset rotation and tremor sampler providing physiological stress proxies.

### 6. Human Factors & Ergonomics (`IVRAR.Group02.HumanFactors`)
- `NasaTlxAssessment.cs`: Captures Mental Demand, Physical Demand, Temporal Demand, Performance, Effort, and Frustration.
- `KennedySsqAssessment.cs`: Evaluates Nausea, Oculomotor, and Disorientation symptoms.
- `SagatAssessment.cs`: Measures Situational Awareness (target perception, localization, threat recognition).
- `PostTrialAssessmentController.cs`: Drives an in-VR world-space canvas for immediate post-trial participant responses.

### 7. User Interface (`IVRAR.Group02.UI`)
- `MainMenuController.cs`: Start Menu displaying:
  - Title: **IVRAR GROUP 02**
  - Subtitle: **Immersive VR Sensory-Stress Defensive Security Training under Low Visibility**
  - Navigation: Start Simulation, Participant Setup, Research Configuration, Exit.
  - Team Credits: Detailed technical roles for Ranish Devadiga, Hriday Jain, Vedika Kaki, and Medha Mishra.
  - Attribution: Credit to original ShootingRangeVR author Anton Pokidov under MIT License.
- `ParticipantSetupUI.cs`: Inputs pseudonymous participant and session identifiers.
- `ResearchConfigUI.cs`: In-VR tuning panel for experimental ScriptableObject parameters.
- `HUDController.cs`: In-headset feedback displaying latency, score, and trial count.

## Compatibility & Integrity
- **Unity Version**: Fully compatible with Unity 2020.3.25f1 and newer.
- **Dependencies**: Retains all existing URP and XR package dependencies without modification.
- **Legacy Functionality**: The original working scene `ShootingRange.unity` is preserved intact along with an explicit backup `ShootingRange_OriginalBackup.unity`. All original weapon, cartridge, and magazine scripts continue to function in their original namespaces.
