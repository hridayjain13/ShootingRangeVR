# IVRAR Group 02 Telemetry Pipeline

## Overview
This directory houses the quantitative experimental data collection, processing tools, and statistical analysis scripts for **IVRAR Group 02: Immersive VR Sensory-Stress Defensive Security Training under Low Visibility**.

## Data Schemas

### 1. `reaction_latency_trial_log.csv`
Records trial-level shoot/don't-shoot decision performance, millisecond latency, accuracy, target position, and head movement jitter.

| Column | Type | Description |
|---|---|---|
| `TimestampUtc` | ISO-8601 String | Coordinated Universal Time of target resolution |
| `ParticipantID` | String | Pseudonymous participant code (e.g. `P01`, `P02`) |
| `SessionID` | String | Experimental session block code (e.g. `S01_BLOCK1`) |
| `Condition` | String | Experimental condition (`Baseline_Daylight`, `LowVisibility_SensoryStress`) |
| `DataStatus` | String | Data validity verification status |
| `TrialID` | String | Sequential trial identifier |
| `TargetType` | String | Target classification (`Hostile` vs. `Civilian`) |
| `TraineeAction` | String | Trainee response (`Shoot` vs. `Hold`) |
| `LatencyMs` | Float | Millisecond elapsed time between target presentation and decision |
| `Hit` | 0 or 1 | Correct engagement of hostile threat |
| `Miss` | 0 or 1 | Failure to engage hostile threat before exposure timeout |
| `FalseAlarm` | 0 or 1 | Inappropriate engagement of civilian / unarmed neutral |
| `CorrectRejection` | 0 or 1 | Appropriate non-engagement of civilian |
| `DistanceMeters` | Float | Radial distance from trainee origin to target anchor |
| `PresentationAngleDeg` | Float | Horizontal azimuth angle relative to trainee forward vector |
| `HeadYawJitterVarianceDeg2` | Float | 90 Hz rolling variance of angular head rotational speed ($\text{deg}^2$) |
| `DPrime` | Float | Signal Detection Theory sensitivity index ($d'$) with Hautus correction |
| `BetaCriterion` | Float | Signal Detection Theory decision criterion / likelihood ratio ($\beta$) |

### 2. `post_trial_assessments.csv`
Captures multi-dimensional psychological assessments administered in-headset post-block:
- **NASA Task Load Index (NASA-TLX)**: Mental Demand, Physical Demand, Temporal Demand, Performance, Effort, Frustration (0–100 scale).
- **Situational Awareness Global Assessment Technique (SAGAT)**: Level 1 target perception and Level 2 spatial localization accuracy.

## Python Analysis Tools
- `generate_paper_figures.py`: Generates publication-ready boxplots, latency distributions, and ROC curves.
- `nasa_tlx_calculator.py`: Computes subscale distributions and Raw TLX (RTLX) means.
- `kennedy_ssq_calculator.py`: Evaluates Nausea, Oculomotor, and Disorientation indices.
- `sensory_stress_defense_roi.py`: Evaluates economic training ROI (ammunition substitution and facility cost offsets).
- `sus_calculator.py`: System Usability Scale (SUS) score processor.
- `test_evaluation_tools.py`: Unit test suite verifying calculation validity.
