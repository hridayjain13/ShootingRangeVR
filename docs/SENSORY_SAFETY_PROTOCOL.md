# IVRAR Group 02: Sensory Stress Safety Protocol & Ethical Compliance

## Purpose & Scope
This protocol establishes the human factors safety boundaries, acoustic limits, photosensitivity safeguards, and participant autonomy procedures for **Phase 04: Controlled Sensory-Stress System** in the *Immersive VR Sensory-Stress Defensive Security Training under Low Visibility* project.

---

## 1. Acoustic Safety & SPL Reality vs. Digital Gain

> [!IMPORTANT]
> **Digital audio amplitude values ($0.0 - 1.0$, or $\text{dBFS}$) do NOT establish physical sound pressure levels ($\text{dB SPL}$) at the participant's tympanic membrane.**
>
> The project proposal outlines experimental acoustic startle cues between $85 - 95\text{ dB SPL}$. **This range is NOT implemented as a default headset volume.** The default digital gain is conservatively capped at **$0.25$ ($-12\text{ dBFS}$)**. Outputting $85 - 95\text{ dB SPL}$ physically requires independent calibration, ethics committee (IRB) approval, and dosimetric monitoring.

### Why Digital Gain Does Not Equal Physical SPL
1. **Headset DAC / Amplifier Variance**: Audio output circuitry across different headsets (e.g., Meta Quest 2/3 integrated pipe drivers vs. Valve Index off-ear near-field speakers vs. external over-ear monitoring headphones) produces wildly varying acoustic outputs for identical digital samples.
2. **OS Volume Settings**: Windows system mixer and headset hardware volume buttons scale output unpredictably.
3. **Physical Verification Requirement**:
   - Any physical acoustic exposure approaching or exceeding $85\text{ dBA}$ must be measured using an **artificial ear / ear simulator fixture** (e.g., IEC 60318-4 / ITU-T Rec. P.57 Type 3.3 pinna simulator) with a calibrated Type 1 sound level meter.
   - Without such measurement, running higher digital volumes risks acoustic trauma or temporary threshold shift (TTS).

### Regulatory Exposure Limits (NIOSH & OSHA)
- **NIOSH Recommended Exposure Limit (REL)**: $85\text{ dBA}$ for an 8-hour time-weighted average (TWA) with a $3\text{ dB}$ exchange rate ($88\text{ dBA} \le 4\text{ hours}$, $91\text{ dBA} \le 2\text{ hours}$, $94\text{ dBA} \le 1\text{ hour}$).
- **Peak Impulse Noise Limit**: C-weighted peak sound pressure must never exceed $140\text{ dB Peak SPL}$ ($120\text{ dB Peak SPL}$ recommended for civilian research protocols).
- **Default System Cap**: Software hard ceiling `_absoluteAudioSafetyLimit = 0.40`, strictly preventing sudden digital clipping or dangerous volume spikes.

---

## 2. Photosensitivity Safety & Safe Visual Distraction

> [!CAUTION]
> Rapid optical flashing between $3\text{ Hz}$ and $30\text{ Hz}$ presents an elevated risk of triggering photosensitive epileptic seizures or visual discomfort.
> Standard experimental protocols must utilize **Gradual Peripheral Glare** rather than abrupt strobing.

### Photosensitivity Safeguards (WCAG 2.1 & ISO 9241-391 Compliance)
1. **Default Mode: Gradual Peripheral Glare**:
   - Instead of binary on/off flashing, the visual distractor smoothly modulates a peripheral illumination source using a continuous sinusoidal bell curve ($f \le 1.2\text{ Hz}$).
   - Peak intensity eases in over $250\text{ ms}$ and decays over $250\text{ ms}$, eliminating sharp photic edges.
2. **Subtle Ambient Pulse Mode**:
   - A non-flashing ambient illumination transition that gently shifts scene trilight intensity without any localized high-contrast flicker.
3. **Controlled Low-Frequency Strobe (Restricted Research Mode)**:
   - When strobing is explicitly required by an approved study design, frequency is strictly capped at **$f \le 3.0\text{ Hz}$** (under the internationally recognized $3\text{ Hz}$ safety threshold), with bursts restricted to $\le 3$ pulses.

---

## 3. Four Standardized Experimental Conditions

The system coordinates four distinct sensory distraction regimes via [`SensoryStressTargetManager.cs`](file:///c:/Users/hrida/Downloads/ShootingRangeVR/Assets/Scripts/SensoryStress/SensoryStressTargetManager.cs):

| Condition | Auditory Distraction | Peripheral Visual Distraction | Research Objective |
|---|---|---|---|
| **Condition 1: No-Distraction Baseline** | Disabled ($0\text{ dBFS}$) | Disabled ($0\text{ lux}$) | Baseline decision reaction time and visual identification accuracy under low visibility alone. |
| **Condition 2: Controlled Auditory** | Active (Spatial gunshot/alarm, gain $\le 0.25$) | Disabled | Isolates cognitive auditory distraction and acoustic startle on motor latency. |
| **Condition 3: Controlled Visual** | Disabled | Active (Gradual peripheral glare $\le 1.5\text{ Hz}$) | Isolates peripheral optical glare and attentional capture during visual target search. |
| **Condition 4: Combined Auditory & Visual** | Active (Spatial gunshot/alarm, gain $\le 0.25$) | Active (Gradual peripheral glare $\le 1.5\text{ Hz}$) | Evaluates multi-modal sensory stress overload on shoot/don't-shoot accuracy. |

---

## 4. Deterministic Reproducibility & Random Scheduling

1. **Independence of Events**:
   - Stressor events are scheduled **independently of participant performance** (no punishment or reward cues), preserving true independent variable validity.
2. **Deterministic Seed Support**:
   - Set `useDeterministicSeed = true` and specify `randomSeed` (default: `42`).
   - Every participant in a given experimental cohort experiences identical distractor onset delays (e.g. $t = 0.45\text{ s}$ post-target presentation) and identical audio/visual sequencing.
   - For between-subject randomization, unique seeds can be assigned per participant ID (e.g. `seed = participantCode.GetHashCode()`).

---

## 5. Participant Autonomy & Comfort Controls

1. **Participant In-VR Pause**:
   - Pressing controller Secondary Button (`B` on Right / `Y` on Left) or keyboard `Escape` / `P` instantly pauses the simulation (`Time.timeScale = 0`), halts all active auditory and visual stressors, and opens the in-VR pause modal.
2. **Immediate Termination Without Penalty**:
   - Participants may exit the simulation at any point by selecting **[Exit Simulation]** or pressing `F12` / `Ctrl+Q`.
   - Data recorded up to that point is preserved with status `terminated_early`, and no penalty or coercion is applied.

---

## 6. Researcher Safety Override

In the event of trainee distress, disorientation, or hardware abnormality:

1. **Emergency Cutoff Key (`F9`)**:
   - Pressing **`F9`** immediately triggers `TriggerResearcherSafetyOverride()`.
2. **Immediate System Reaction**:
   - Instantly silences all audio sources (`AudioSource.Stop()`).
   - Instantly turns off peripheral glare and strobe lights.
   - Cancels all active distraction coroutines.
   - Flags `isSafetyOverridden = true` in memory and records `SafetyOverridden = TRUE` in [`telemetry/sensory_distraction_event_log.csv`](file:///c:/Users/hrida/Downloads/ShootingRangeVR/telemetry/sensory_distraction_event_log.csv).
3. **Session Resumption**:
   - Safety override remains engaged until the researcher explicitly clicks **Reset Safety Override** after verifying participant readiness.

---

## 7. Event Telemetry Logging Format

All scheduled and executed distraction events are logged to `telemetry/sensory_distraction_event_log.csv`:

```csv
TimestampUtc,ParticipantID,SessionID,Condition,TrialID,EventType,ScheduledDelaySec,ActualOnsetSec,DurationSec,AudioGain,VisualIntensity,SafetyOverridden
2026-10-25T17:50:00Z,P01_TEST,S01_BLOCK1,Condition4_CombinedAuditoryVisual,T01,Combined,0.650,0.652,0.500,0.25,0.20,FALSE
2026-10-25T17:50:05Z,P01_TEST,S01_BLOCK1,Condition4_CombinedAuditoryVisual,T02,Combined,0.820,0.821,0.500,0.25,0.20,FALSE
```
