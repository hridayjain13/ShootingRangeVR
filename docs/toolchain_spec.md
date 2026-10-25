# Toolchain & Hardware Specification
**Group ID:** IVRAR_GROUP_02  
**Project:** How can an immersive VR sensory-stress simulation improve target prioritization and reaction latency for defensive security trainees under simulated low-visibility nighttime conditions?  

## Recommended Software Stack
* **Game Engine:** Unity 2022.3 LTS (Long Term Support)
* **XR Plugin Architecture:** OpenXR Plugin (>= 1.8.0)
* **Toolkit:** XR Interaction Toolkit (XRI >= 2.5.2 or 3.0.0)
* **Graphics Pipeline:** Universal Render Pipeline (URP - Mobile/Standalone Profile)
* **Code Editor:** Visual Studio 2022 / VS Code with C# Dev Kit

## Target Deployment Platforms
* Meta Quest 2 / Quest 3 / Quest Pro (via Android OpenXR)
* Standalone PC VR (OpenXR Link / AirLink)
* WebXR (via WebGL 2.0 / Babylon.js / Three.js where applicable)

## Telemetry & Data Collection Protocol
* Sampling Rate: >= 60 Hz head/hand transform recording
* Output Format: Structured CSV (`/telemetry/trial_<id>.csv`)
* Metrics: Completion Time (s), Path Trajectory Deviation (m), Error Rates, Collision Events
