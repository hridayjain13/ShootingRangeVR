# Techno-Economic Evaluation & Analytics Model

**Document Version:** 1.0.0  
**Project:** IVRAR Group 02 — Immersive VR Sensory-Stress Defensive Security Training under Low Visibility  
**Author:** Group 02 Techno-Managerial Evaluation Team  
**Scope:** Mathematical formulation and operational analysis comparing Conventional Facility-Based Training against VR-Assisted Training.

---

## 1. Executive Summary & Methodological Boundary

This document formalizes the dimensionless techno-economic evaluation model and research analytics toolchain for the IVRAR Group 02 platform.

### Academic & Methodological Boundary
> [!IMPORTANT]
> **Strict Separation of Telemetry and Monetary Assumptions:**
> Empirical experimental telemetry ($RT$, $d'$, $\beta$, NASA-TLX, Kennedy SSQ, SAGAT) measures participant performance and psychometrics.
> All financial values (facility rental, ammunition substitution, CapEx, OpEx) represent parameterized techno-economic assumptions for feasibility modeling.
> **No empirical claims of monetary savings or munitions substitution are asserted without externally validated institutional accounting receipts.**

---

## 2. Dimensionless Techno-Economic Model Formulation

### 2.1 Parameter Definitions

Let:
- $N_s$: Number of completed training sessions.
- $N_p$: Number of participating trainees.
- $p$: Number of concurrent participants per session ($p = N_p / N_s$).

#### Conventional Live-Fire Facility Cost Function ($C_{\text{conv}}$)
Conventional training relies on leased tactical shoot-houses, ammunition consumables, and on-site range safety officers:

$$C_{\text{conv}}(N_s) = N_s \cdot \text{OpEx}_{\text{conv}}$$

$$\text{OpEx}_{\text{conv}} = C_{\text{facility}} + (p \cdot C_{\text{consumables}}) + C_{\text{inst,conv}} + C_{\text{logistics}}$$

Where:
- $C_{\text{facility}}$: Range / facility rental per session.
- $C_{\text{consumables}}$: Consumable ammunition, targets, and PPE wear per participant.
- $C_{\text{inst,conv}}$: Certified range safety officers and tactical instructors.
- $C_{\text{logistics}}$: Range safety maintenance, target reset, and cleanup.

#### VR-Assisted Training Cost Function ($C_{\text{vr}}$)
VR-assisted training introduces fixed capital equipment and software expenditures ($\text{CapEx}_{\text{vr}}$), offset by substantially lower per-session marginal operational costs ($\text{OpEx}_{\text{vr}}$):

$$C_{\text{vr}}(N_s) = \text{CapEx}_{\text{vr}} + N_s \cdot \text{OpEx}_{\text{vr}}$$

$$\text{CapEx}_{\text{vr}} = C_{\text{hw,initial}} + C_{\text{sw,dev}} + C_{\text{replacement}}$$

$$\text{OpEx}_{\text{vr}} = C_{\text{vr,power/net}} + C_{\text{inst,vr}} + \frac{C_{\text{maint,annual}}}{N_s}$$

Where:
- $C_{\text{hw,initial}}$: HMD headsets, optical/inside-out tracking, controllers, and GPU workstations.
- $C_{\text{sw,dev}}$: Initial custom scenario architecture and software engineering.
- $C_{\text{replacement}}$: Annual depreciation / hardware replacement allowance ($20\%$).
- $C_{\text{maint,annual}}$: Scheduled sanitation, sensor recalibration, and software maintenance.
- $C_{\text{inst,vr}}$: VR operator / simulation instructor supervision per session.

---

### 2.2 Dimensionless Normalization

To evaluate the operational viability independently of local currency or fluctuating price levels, we define three fundamental dimensionless ratios:

1. **Operating Cost Ratio ($\kappa$):**
   $$\kappa = \frac{\text{OpEx}_{\text{vr}}}{\text{OpEx}_{\text{conv}}}$$
   Where $\kappa < 1$ is a mandatory thermodynamic/economic prerequisite for economic viability. (If $\kappa \ge 1$, VR operating expenses equal or exceed conventional costs, rendering break-even impossible).

2. **Capital Intensity Ratio ($\theta$):**
   $$\theta = \frac{\text{CapEx}_{\text{vr}}}{\text{OpEx}_{\text{conv}}}$$
   Measures the initial capital barrier scaled by the operational cost of a single conventional session.

3. **Dimensionless Break-Even Index ($\eta^*$):**
   Setting $C_{\text{vr}}(N_s^*) = C_{\text{conv}}(N_s^*)$:
   $$\text{CapEx}_{\text{vr}} + N_s^* \cdot \text{OpEx}_{\text{vr}} = N_s^* \cdot \text{OpEx}_{\text{conv}}$$
   $$N_s^* = \frac{\text{CapEx}_{\text{vr}}}{\text{OpEx}_{\text{conv}} - \text{OpEx}_{\text{vr}}} = \frac{\theta}{1 - \kappa} = \eta^*$$
   Therefore, the break-even session count $N_s^*$ is identically equal to the dimensionless index $\eta^*$.

4. **Normalized Cost Function ($\Phi(N_s)$):**
   $$\Phi(N_s) = \frac{C_{\text{vr}}(N_s)}{C_{\text{conv}}(N_s)} = \frac{\text{CapEx}_{\text{vr}} + N_s \cdot \text{OpEx}_{\text{vr}}}{N_s \cdot \text{OpEx}_{\text{conv}}} = \kappa + \frac{\theta}{N_s}$$
   - When $N_s < \eta^* \implies \Phi(N_s) > 1.0$ (VR is more expensive than conventional training).
   - At break-even $N_s = \eta^* \implies \Phi(\eta^*) = 1.0$.
   - As $N_s \to \infty \implies \Phi(N_s) \to \kappa$ (Asymptotic minimum cost ratio).

5. **Normalized Economic Savings Function ($\Psi(N_s)$):**
   $$\Psi(N_s) = 1 - \Phi(N_s) = (1 - \kappa) - \frac{\theta}{N_s}$$
   Maximum asymptotic economic substitution is strictly bounded by:
   $$\lim_{N_s \to \infty} \Psi(N_s) = (1 - \kappa)$$

---

### 2.3 Return on Investment (ROI) & Unit Cost

#### Return on Investment (ROI)
$$\text{ROI} = \left( \frac{C_{\text{conv}}(N_s) - C_{\text{vr}}(N_s)}{\text{CapEx}_{\text{vr}}} \right) \times 100\% = \left( \frac{N_s \cdot (1 - \kappa)}{\theta} - 1 \right) \times 100\%$$

#### Cost per Participant ($c_p$)
$$c_{p,\text{conv}} = \frac{C_{\text{conv}}(N_s)}{N_p}$$

$$c_{p,\text{vr}} = \frac{C_{\text{vr}}(N_s)}{N_p} = \frac{\text{CapEx}_{\text{vr}}}{N_p} + \frac{N_s \cdot \text{OpEx}_{\text{vr}}}{N_p}$$

---

## 3. Parametric Sensitivity Analysis

The model evaluates elasticity across two critical vectors:
1. **Cohort Volume Elasticity ($N_p$):** As trainee volume increases, fixed VR CapEx is amortized over more individuals, causing $c_{p,\text{vr}}$ to drop hyperbolically, while conventional live-fire training exhibits nearly flat unit costs due to linear ammunition and consumable scaling.
2. **CapEx & OpEx Perturbation Grid ($\pm 10\% \text{ to } \pm 50\%$):** Evaluates how hardware price shifts or facility rental escalation alters break-even point $\eta^*$ and overall ROI.

---

## 4. Research Analytics Dashboard

The platform automatically compiles and visualizes experimental telemetry and techno-economic dynamics into a 9-panel publication dashboard:

| Panel | Metric | Methodological Source |
| :---: | :--- | :--- |
| **1** | Reaction Latency by Condition | Monotonic clock latency (Mean $\pm$ SE, Median) |
| **2** | Signal Detection Theory ($d'$, False Alarms) | Log-linear Hautus correction over Hit/FA proportions |
| **3** | Identification Accuracy (%) | Correct decisions over total tactical presentations |
| **4** | Cognitive Workload (NASA-TLX) | All 6 dimensions and unweighted Raw TLX (RTLX) |
| **5** | Cybersickness (Kennedy SSQ) | Pre-Session baseline vs. Post-Session exposure & $\Delta \text{SSQ}$ |
| **6** | Situational Awareness (SAGAT) | Endsley Level 1, 2, and 3 probe accuracy during freezes |
| **7** | Cumulative Expenditure & Break-Even | Break-even crossover ($N_s^*$) and cost substitution zone |
| **8** | Dimensionless Operational Dynamics | $\Phi(N_s) = \kappa + \theta / N_s$ trajectory and $\kappa$ asymptote |
| **9** | Techno-Economic Sensitivity Analysis | Parameter variation ($\pm 30\%$) vs. projected ROI |

---

## 5. Execution Commands

```bash
# Run Techno-Economic Evaluation & Generate Publication Dashboard
python telemetry/sensory_stress_defense_roi.py \
  --trials-csv telemetry/SYNTHETIC_TEST_reaction_latency_trial_log.csv \
  --hf-csv telemetry/SYNTHETIC_TEST_human_factors_assessments.csv \
  --output-dir telemetry/figures

# Run Comprehensive Unit Test Suite
python -m unittest telemetry/test_evaluation_tools.py

# Run all telemetry unit tests
python -m unittest discover -s telemetry -p "test_*.py"
```
