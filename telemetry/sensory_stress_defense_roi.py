#!/usr/bin/env python3
"""
IVRAR Group 02: Dimensionless Techno-Economic Evaluation & Research Analytics Dashboard
Implements:
1. Dimensionless Techno-Economic Evaluation Model:
   - Compares Conventional Facility-Based Training vs. VR-Assisted Defensive Training.
   - Configurable capital investments (CapEx: hardware, software, replacement).
   - Configurable operational expenditures (OpEx: maintenance, consumables, instructor, facility).
   - Dimensionless parameters: Operating Expense Ratio (kappa), Capital Intensity Ratio (theta),
     Dimensionless Break-even (eta*), Normalized Cost Function (Phi), and Normalized Savings (Psi).
   - Sensitivity analyses for participant volume, hardware replacement, and OpEx variations.
   - Strict separation between empirical experimental data and hypothetical monetary assumptions.
   - Disclaimer: No empirical savings in ammunition, facility rentals, or operational effectiveness
     are claimed without externally validated cost records.

2. Research Analytics Dashboard:
   - Reads actual exported telemetry and human-factors assessment CSV files.
   - Calculates and visualizes:
     * Mean & Median reaction time by condition.
     * Accuracy and False-Alarm rate comparisons.
     * Signal detection d' (d-prime sensitivity index).
     * NASA-TLX cognitive workload across 6 subscales and Raw TLX.
     * Kennedy SSQ pre-session vs. post-session comparison & Delta SSQ.
     * SAGAT situational awareness across Endsley Levels 1, 2, and 3.
     * ROI and Break-even session crossover curves.
   - Exports publication-ready high-resolution figures.
"""

from dataclasses import dataclass, asdict
import argparse
import csv
from datetime import datetime, timezone
import json
import math
import os
from pathlib import Path
import sys
from typing import Any, Dict, List, Optional, Tuple

import numpy as np
import pandas as pd
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
import matplotlib.ticker as ticker
from scipy import stats


# ==============================================================================
# 1. TECHNO-ECONOMIC EVALUATION MODEL (DIMENSIONLESS & MONETARY)
# ==============================================================================

@dataclass
class TechnoEconomicAssumptions:
    """
    Structured monetary and operational assumptions for training cost comparison.
    All monetary units are in USD ($). These values represent parameterized baseline
    assumptions for techno-managerial modeling, strictly isolated from measured trial data.
    """
    # VR Scenario Capital Investment (CapEx)
    vr_hardware_investment: float = 8500.0       # HMDs, tracking base stations, haptics, PC rigs
    vr_software_development_cost: float = 12000.0 # Initial custom VR scenario design & development
    vr_hardware_replacement_rate: float = 0.20   # Annual depreciation/hardware replacement allowance (20%)

    # VR Scenario Operational Expenses (OpEx)
    vr_maintenance_annual: float = 1500.0        # System maintenance, sanitation, tracking recalibration
    vr_operating_cost_per_session: float = 15.0  # Electrical power, network, consumables
    vr_instructor_cost_per_session: float = 40.0 # VR technician / certified instructor supervision

    # Conventional Live-Fire / Facility-Based Training Scenario
    facility_rental_per_session: float = 220.0   # Live-fire range / tactical shoot-house rental
    consumables_per_participant_session: float = 35.0 # Blank/live ammunition, targets, PPE wear
    conventional_instructor_per_session: float = 90.0 # Range safety officers & tactical instructors
    conventional_operating_cost_per_session: float = 45.0 # Logistics, target reset, range cleanup

    # Operational Scale
    number_of_participants: int = 50             # Target trainee cohort size
    number_of_sessions: int = 100                # Planned annual training repetitions
    participants_per_session: int = 4            # Concurrent trainees per session


def validate_assumptions(assump: TechnoEconomicAssumptions) -> None:
    """Validates that assumptions contain physically meaningful non-negative values."""
    if assump.number_of_participants <= 0:
        raise ValueError("Number of participants must be greater than zero.")
    if assump.number_of_sessions <= 0:
        raise ValueError("Number of training sessions must be greater than zero.")
    if assump.participants_per_session <= 0:
        raise ValueError("Participants per session must be greater than zero.")

    for field, val in asdict(assump).items():
        if isinstance(val, (int, float)) and val < 0:
            raise ValueError(f"Assumption parameter '{field}' cannot be negative (found {val}).")


def compute_techno_economic_model(assump: TechnoEconomicAssumptions) -> Dict[str, Any]:
    """
    Computes absolute costs, unit participant costs, break-even thresholds, ROI,
    and dimensionless operational parameters.
    """
    validate_assumptions(assump)

    N_s = float(assump.number_of_sessions)
    N_p = float(assump.number_of_participants)
    p_per_sess = float(assump.participants_per_session)

    # 1. Total VR Capital Investment (CapEx)
    vr_capex_initial = assump.vr_hardware_investment + assump.vr_software_development_cost
    vr_annual_replacement = assump.vr_hardware_investment * assump.vr_hardware_replacement_rate
    vr_total_fixed_capex = vr_capex_initial + vr_annual_replacement

    # 2. VR Operational Cost per Session (OpEx_vr)
    # Annual maintenance amortized across planned sessions
    vr_maint_per_sess = assump.vr_maintenance_annual / max(1.0, N_s)
    opex_vr_per_session = (
        assump.vr_operating_cost_per_session +
        assump.vr_instructor_cost_per_session +
        vr_maint_per_sess
    )
    total_vr_cost = vr_total_fixed_capex + (opex_vr_per_session * N_s)

    # 3. Conventional Training Operational Cost per Session (OpEx_conv)
    # Consumables scale with participants per session
    consumables_session = assump.consumables_per_participant_session * p_per_sess
    opex_conv_per_session = (
        assump.facility_rental_per_session +
        consumables_session +
        assump.conventional_instructor_per_session +
        assump.conventional_operating_cost_per_session
    )
    # Conventional training assumes leased facilities without dedicated facility CapEx
    total_conventional_cost = opex_conv_per_session * N_s

    # 4. Cost Substitution & Net Savings
    cost_substitution = total_conventional_cost - total_vr_cost

    # 5. Unit Cost per Participant
    cost_per_participant_vr = total_vr_cost / max(1.0, N_p)
    cost_per_participant_conv = total_conventional_cost / max(1.0, N_p)

    # 6. Break-Even Session Count (N_s*)
    # C_vr(N_s*) = C_conv(N_s*) => CapEx + N_s* * OpEx_vr = N_s* * OpEx_conv
    # N_s* = CapEx / (OpEx_conv - OpEx_vr)
    marginal_session_savings = opex_conv_per_session - opex_vr_per_session
    if marginal_session_savings > 1e-4:
        break_even_sessions = vr_total_fixed_capex / marginal_session_savings
        is_break_even_achievable = True
    else:
        break_even_sessions = float("inf")
        is_break_even_achievable = False

    # 7. Return on Investment (ROI)
    # Net Economic Benefit relative to VR Fixed Capital Outlay
    if vr_total_fixed_capex > 0:
        roi_percent = (cost_substitution / vr_total_fixed_capex) * 100.0
    else:
        roi_percent = 0.0

    # 8. Dimensionless Operational Parameters
    # kappa: Operating Expense Ratio (OpEx_vr / OpEx_conv)
    # theta: Capital Intensity Ratio (CapEx_vr / OpEx_conv_session)
    # eta*: Dimensionless Break-even Index = theta / (1 - kappa)
    # Phi(N_s): Dimensionless Cost Ratio = C_vr / C_conv = kappa + theta / N_s
    # Psi(N_s): Dimensionless Savings Ratio = 1 - Phi(N_s) = (1 - kappa) - theta / N_s
    kappa = opex_vr_per_session / max(1e-4, opex_conv_per_session)
    theta = vr_total_fixed_capex / max(1e-4, opex_conv_per_session)

    if kappa < 1.0:
        dimensionless_breakeven_eta = theta / (1.0 - kappa)
    else:
        dimensionless_breakeven_eta = float("inf")

    phi_current = (total_vr_cost / max(1e-4, total_conventional_cost))
    psi_current = 1.0 - phi_current

    return {
        "Assumptions": asdict(assump),
        "TotalConventionalCost": total_conventional_cost,
        "TotalVrCost": total_vr_cost,
        "CostSubstitution": cost_substitution,
        "CostPerParticipantConv": cost_per_participant_conv,
        "CostPerParticipantVr": cost_per_participant_vr,
        "BreakEvenSessionCount": break_even_sessions,
        "IsBreakEvenAchievable": is_break_even_achievable,
        "RoiPercent": roi_percent,
        "OpExConvPerSession": opex_conv_per_session,
        "OpExVrPerSession": opex_vr_per_session,
        "CapExVrTotal": vr_total_fixed_capex,
        # Dimensionless Formulation
        "Dimensionless": {
            "OperatingCostRatio_Kappa": kappa,
            "CapitalIntensityRatio_Theta": theta,
            "DimensionlessBreakEven_Eta": dimensionless_breakeven_eta,
            "NormalizedCostRatio_Phi": phi_current,
            "NormalizedSavingsRatio_Psi": psi_current,
            "Formula_Phi": "Phi(N_s) = kappa + theta / N_s",
            "Formula_Psi": "Psi(N_s) = (1 - kappa) - theta / N_s",
            "AsymptoticSavingsLimit": (1.0 - kappa) * 100.0 # Maximum theoretical savings as N_s -> infinity
        }
    }


def compute_volume_sensitivity(assump: TechnoEconomicAssumptions, min_part: int = 10, max_part: int = 200, step: int = 10) -> Dict[str, Any]:
    """Evaluates sensitivity of unit cost and total expenditure to participant volume."""
    validate_assumptions(assump)
    volumes = list(range(min_part, max_part + step, step))
    vr_unit_costs = []
    conv_unit_costs = []
    savings = []

    for v in volumes:
        # Scale sessions proportionally to keep participant coverage invariant
        p_per_sess = max(1, assump.participants_per_session)
        needed_sessions = max(10, math.ceil(v * (assump.number_of_sessions / assump.number_of_participants)))

        test_assump = TechnoEconomicAssumptions(**asdict(assump))
        test_assump.number_of_participants = v
        test_assump.number_of_sessions = needed_sessions

        res = compute_techno_economic_model(test_assump)
        vr_unit_costs.append(res["CostPerParticipantVr"])
        conv_unit_costs.append(res["CostPerParticipantConv"])
        savings.append(res["CostSubstitution"])

    return {
        "ParticipantVolumes": volumes,
        "VrUnitCosts": vr_unit_costs,
        "ConvUnitCosts": conv_unit_costs,
        "NetSavings": savings
    }


def compute_cost_sensitivity(assump: TechnoEconomicAssumptions, variations: Optional[List[float]] = None) -> Dict[str, Any]:
    """
    Parametric sensitivity analysis across +/- variation in equipment CapEx and operational OpEx.
    """
    validate_assumptions(assump)
    if variations is None:
        variations = [-0.30, -0.15, 0.0, 0.15, 0.30]

    capex_base = assump.vr_hardware_investment + assump.vr_software_development_cost
    opex_base = assump.vr_operating_cost_per_session

    capex_sweep = []
    opex_sweep = []

    for delta in variations:
        # Vary CapEx
        mod_assump_c = TechnoEconomicAssumptions(**asdict(assump))
        mod_assump_c.vr_hardware_investment *= (1.0 + delta)
        res_c = compute_techno_economic_model(mod_assump_c)
        capex_sweep.append({
            "DeltaPercent": delta * 100.0,
            "CapExTotal": res_c["CapExVrTotal"],
            "BreakEvenSessions": res_c["BreakEvenSessionCount"],
            "RoiPercent": res_c["RoiPercent"]
        })

        # Vary VR OpEx
        mod_assump_o = TechnoEconomicAssumptions(**asdict(assump))
        mod_assump_o.vr_operating_cost_per_session *= (1.0 + delta)
        res_o = compute_techno_economic_model(mod_assump_o)
        opex_sweep.append({
            "DeltaPercent": delta * 100.0,
            "OpExVrPerSession": res_o["OpExVrPerSession"],
            "BreakEvenSessions": res_o["BreakEvenSessionCount"],
            "RoiPercent": res_o["RoiPercent"]
        })

    return {
        "Variations": [v * 100.0 for v in variations],
        "CapExSensitivity": capex_sweep,
        "OpExSensitivity": opex_sweep
    }


# ==============================================================================
# 2. EXPERIMENTAL DATA EXTRACTION (TELEMETRY & HUMAN FACTORS)
# ==============================================================================

def load_trial_telemetry(csv_path: Path) -> pd.DataFrame:
    """Loads and standardizes reaction latency and signal detection trial CSV."""
    if not csv_path.exists():
        raise FileNotFoundError(f"Trial telemetry CSV not found: {csv_path}")

    # Skip comment rows
    with csv_path.open("r", encoding="utf-8-sig") as f:
        valid_lines = [l for l in f if not l.strip().startswith("#")]

    import io
    df = pd.read_csv(io.StringIO("".join(valid_lines)))

    col_map = {c.strip(): c.strip() for c in df.columns}
    aliases = {
        "participantid": "ParticipantID", "sessionid": "SessionID", "condition": "Condition",
        "latencyms": "LatencyMs", "targettype": "TargetType", "traineeaction": "TraineeAction",
        "datastatus": "DataStatus", "trialid": "TrialID"
    }
    rename_dict = {}
    for c in df.columns:
        norm = c.replace("_", "").lower()
        if norm in aliases:
            rename_dict[c] = aliases[norm]

    df = df.rename(columns=rename_dict)
    if "LatencyMs" in df.columns:
        df["LatencyMs"] = pd.to_numeric(df["LatencyMs"], errors="coerce")

    return df


def load_human_factors_assessments(csv_path: Path) -> Dict[str, pd.DataFrame]:
    """Loads human factors CSV, splitting into NASA-TLX, Kennedy SSQ, and SAGAT tables."""
    if not csv_path.exists():
        raise FileNotFoundError(f"Human factors assessments CSV not found: {csv_path}")

    with csv_path.open("r", encoding="utf-8-sig") as f:
        valid_lines = [l for l in f if not l.strip().startswith("#")]

    import io
    df = pd.read_csv(io.StringIO("".join(valid_lines)))
    df.columns = [c.strip() for c in df.columns]

    inst_col = df.get("Instrument", pd.Series([""] * len(df))).astype(str).str.lower()

    # Split sub-tables
    tlx_df = df[inst_col.str.contains("tlx") | df["MentalDemand"].notna() & (df["MentalDemand"] != -1)].copy()
    ssq_df = df[inst_col.str.contains("ssq") | df["GeneralDiscomfort"].notna() & (df["GeneralDiscomfort"] != -1)].copy()
    sagat_df = df[inst_col.str.contains("sagat") | df.get("OverallSaScorePct", pd.Series([-1])).ne(-1)].copy()

    return {"TLX": tlx_df, "SSQ": ssq_df, "SAGAT": sagat_df, "Full": df}


def compute_sdt_metrics_from_df(df: pd.DataFrame) -> Dict[str, Any]:
    """Extracts signal detection theory metrics (Hits, FAs, d', criterion c) by condition."""
    if "TargetType" not in df.columns or "TraineeAction" not in df.columns:
        return {}

    cond_col = "Condition" if "Condition" in df.columns else None
    conditions = df[cond_col].unique() if cond_col else ["Overall"]

    sdt_results = {}
    for cond in conditions:
        sub = df[df[cond_col] == cond] if cond_col else df
        hostile = sub["TargetType"].astype(str).str.lower().eq("hostile")
        engaged = sub["TraineeAction"].astype(str).str.lower().isin(["engaged", "true", "1"])

        hits = int((hostile & engaged).sum())
        misses = int((hostile & ~engaged).sum())
        fas = int((~hostile & engaged).sum())
        crs = int((~hostile & ~engaged).sum())

        sig = hits + misses
        noi = fas + crs

        # Hautus log-linear correction
        hit_rate = (hits + 0.5) / (sig + 1.0) if sig >= 0 else 0.5
        fa_rate = (fas + 0.5) / (noi + 1.0) if noi >= 0 else 0.5

        z_hit = stats.norm.ppf(hit_rate)
        z_fa = stats.norm.ppf(fa_rate)
        d_prime = z_hit - z_fa
        c = -0.5 * (z_hit + z_fa)
        acc = (hits + crs) / max(1, sig + noi) * 100.0

        sdt_results[str(cond)] = {
            "Hits": hits, "Misses": misses, "FalseAlarms": fas, "CorrectRejections": crs,
            "SignalN": sig, "NoiseN": noi,
            "HitRate": hit_rate, "FalseAlarmRate": fa_rate,
            "DPrime": float(d_prime), "CriterionC": float(c), "AccuracyPct": float(acc)
        }

    return sdt_results


# ==============================================================================
# 3. PUBLICATION-READY DASHBOARD & FIGURE EXPORTS
# ==============================================================================

def setup_plot_style():
    """Sets a clean, publication-grade matplotlib visual aesthetic."""
    plt.style.use("seaborn-v0_8-whitegrid" if "seaborn-v0_8-whitegrid" in plt.style.available else "default")
    plt.rcParams.update({
        "font.family": "sans-serif",
        "font.size": 10,
        "axes.titlesize": 11,
        "axes.titleweight": "bold",
        "axes.labelsize": 10,
        "axes.labelweight": "semibold",
        "xtick.labelsize": 9,
        "ytick.labelsize": 9,
        "legend.fontsize": 9,
        "figure.titlesize": 14,
        "figure.titleweight": "bold",
        "figure.dpi": 300,
        "savefig.dpi": 300,
        "axes.edgecolor": "#333333",
        "axes.linewidth": 0.8,
        "grid.color": "#e0e0e0",
        "grid.linestyle": "--",
        "grid.alpha": 0.7
    })


def export_publication_dashboard(
    trial_df: pd.DataFrame,
    hf_dict: Dict[str, pd.DataFrame],
    roi_model: Dict[str, Any],
    output_dir: Path
) -> Path:
    """
    Renders and exports the multi-panel Research Analytics & Techno-Economic Dashboard.
    """
    setup_plot_style()
    output_dir.mkdir(parents=True, exist_ok=True)
    fig_path = output_dir / "research_analytics_techno_managerial_dashboard.png"

    fig, axes = plt.subplots(3, 3, figsize=(18, 14))
    fig.suptitle("IVRAR Group 02: Research Analytics & Techno-Managerial Evaluation Dashboard", fontsize=15, y=0.98)

    palette = ["#1f77b4", "#ff7f0e", "#2ca02c", "#d62728", "#9467bd", "#8c564b"]

    # --------------------------------------------------------------------------
    # Panel (0,0): Reaction Time by Condition (Mean & Median)
    # --------------------------------------------------------------------------
    ax = axes[0, 0]
    if "Condition" in trial_df.columns and "LatencyMs" in trial_df.columns:
        valid_df = trial_df.dropna(subset=["LatencyMs", "Condition"])
        cond_order = list(valid_df["Condition"].unique())
        means = [valid_df[valid_df["Condition"] == c]["LatencyMs"].mean() for c in cond_order]
        medians = [valid_df[valid_df["Condition"] == c]["LatencyMs"].median() for c in cond_order]
        sems = [valid_df[valid_df["Condition"] == c]["LatencyMs"].sem() for c in cond_order]

        x = np.arange(len(cond_order))
        width = 0.35
        ax.bar(x - width/2, means, width, yerr=sems, label="Mean RT (±SE)", color="#2b5c8f", capsize=4)
        ax.bar(x + width/2, medians, width, label="Median RT", color="#e67e22")
        ax.set_xticks(x)
        clean_labels = [c.replace("_", "\n") for c in cond_order]
        ax.set_xticklabels(clean_labels, rotation=15, ha="right")
        ax.set_ylabel("Latency (ms)")
        ax.set_title("1. Reaction Latency by Condition")
        ax.legend()
    else:
        ax.text(0.5, 0.5, "No Trial Latency Data Available", ha="center", va="center")
        ax.set_title("1. Reaction Latency")

    # --------------------------------------------------------------------------
    # Panel (0,1): Signal Detection d' & False-Alarm Rate
    # --------------------------------------------------------------------------
    ax = axes[0, 1]
    sdt_stats = compute_sdt_metrics_from_df(trial_df)
    if sdt_stats:
        conds = list(sdt_stats.keys())
        d_primes = [sdt_stats[c]["DPrime"] for c in conds]
        fa_rates = [sdt_stats[c]["FalseAlarmRate"] * 100.0 for c in conds]

        x = np.arange(len(conds))
        ax2 = ax.twinx()
        l1 = ax.plot(x, d_primes, "o-", color="#1b9e77", linewidth=2.2, markersize=7, label="Sensitivity (d')")
        l2 = ax2.plot(x, fa_rates, "s--", color="#d95f02", linewidth=2.0, markersize=7, label="False-Alarm Rate (%)")

        ax.set_xticks(x)
        ax.set_xticklabels([c.replace("_", "\n") for c in conds], rotation=15, ha="right")
        ax.set_ylabel("d' Sensitivity Index", color="#1b9e77")
        ax2.set_ylabel("False Alarm Rate (%)", color="#d95f02")
        ax.set_title("2. Signal Detection: d' & False Alarms")

        lines = l1 + l2
        labels = [l.get_label() for l in lines]
        ax.legend(lines, labels, loc="upper right")
    else:
        ax.text(0.5, 0.5, "No SDT Target Data", ha="center", va="center")
        ax.set_title("2. Signal Detection Theory")

    # --------------------------------------------------------------------------
    # Panel (0,2): Target Identification Accuracy (%)
    # --------------------------------------------------------------------------
    ax = axes[0, 2]
    if sdt_stats:
        conds = list(sdt_stats.keys())
        accuracies = [sdt_stats[c]["AccuracyPct"] for c in conds]
        bars = ax.bar(range(len(conds)), accuracies, color="#34495e", width=0.55)
        ax.set_xticks(range(len(conds)))
        ax.set_xticklabels([c.replace("_", "\n") for c in conds], rotation=15, ha="right")
        ax.set_ylabel("Overall Accuracy (%)")
        ax.set_ylim(0, 105)
        ax.set_title("3. Target Identification Accuracy")
        for b, val in zip(bars, accuracies):
            ax.text(b.get_x() + b.get_width()/2, val + 2, f"{val:.1f}%", ha="center", fontsize=8.5)
    else:
        ax.text(0.5, 0.5, "No Accuracy Data", ha="center", va="center")
        ax.set_title("3. Identification Accuracy")

    # --------------------------------------------------------------------------
    # Panel (1,0): NASA-TLX Cognitive Workload Across Conditions
    # --------------------------------------------------------------------------
    ax = axes[1, 0]
    tlx_df = hf_dict.get("TLX", pd.DataFrame())
    dims = ["MentalDemand", "PhysicalDemand", "TemporalDemand", "Performance", "Effort", "Frustration"]
    if not tlx_df.empty and "Condition" in tlx_df.columns:
        tlx_valid = tlx_df.copy()
        for d in dims:
            if d in tlx_valid.columns:
                tlx_valid[d] = pd.to_numeric(tlx_valid[d], errors="coerce")
        conds = [c for c in tlx_valid["Condition"].unique() if "pre" not in str(c).lower() and "summary" not in str(c).lower()]
        rtlx_means = []
        for c in conds:
            sub = tlx_valid[tlx_valid["Condition"] == c]
            row_means = sub[dims].replace(-1, np.nan).mean(axis=1)
            rtlx_means.append(row_means.mean())

        x = np.arange(len(conds))
        ax.bar(x, rtlx_means, color="#8e44ad", width=0.5)
        ax.set_xticks(x)
        ax.set_xticklabels([c.replace("_", "\n") for c in conds], rotation=15, ha="right")
        ax.set_ylabel("Raw TLX Score (0-100)")
        ax.set_ylim(0, 100)
        ax.set_title("4. NASA-TLX Cognitive Workload (RTLX)")
        for idx, val in enumerate(rtlx_means):
            if not np.isnan(val):
                ax.text(idx, val + 2, f"{val:.1f}", ha="center", fontsize=8.5)
    else:
        ax.text(0.5, 0.5, "No NASA-TLX Records", ha="center", va="center")
        ax.set_title("4. NASA-TLX Workload")

    # --------------------------------------------------------------------------
    # Panel (1,1): Kennedy SSQ Pre vs. Post Cybersickness & Delta
    # --------------------------------------------------------------------------
    ax = axes[1, 1]
    ssq_df = hf_dict.get("SSQ", pd.DataFrame())
    if not ssq_df.empty and "Timing" in ssq_df.columns:
        pre_sub = ssq_df[ssq_df["Timing"].astype(str).str.lower().str.contains("pre")]
        post_sub = ssq_df[ssq_df["Timing"].astype(str).str.lower().str.contains("post")]

        scales = ["NauseaScore", "OculomotorScore", "DisorientationScore", "TotalSeverityScore"]
        scale_names = ["Nausea", "Oculomotor", "Disorientation", "Total Severity"]

        pre_vals = [pd.to_numeric(pre_sub[s], errors="coerce").mean() if not pre_sub.empty else 0.0 for s in scales]
        post_vals = [pd.to_numeric(post_sub[s], errors="coerce").mean() if not post_sub.empty else 0.0 for s in scales]

        x = np.arange(len(scale_names))
        w = 0.35
        ax.bar(x - w/2, pre_vals, w, label="Pre-Session (Baseline)", color="#3498db")
        ax.bar(x + w/2, post_vals, w, label="Post-Session (Exposure)", color="#e74c3c")
        ax.set_xticks(x)
        ax.set_xticklabels(scale_names, rotation=15, ha="right")
        ax.set_ylabel("Kennedy SSQ Score")
        ax.set_title("5. Kennedy SSQ Cybersickness (Pre vs. Post)")
        ax.legend()
    else:
        ax.text(0.5, 0.5, "No Kennedy SSQ Records", ha="center", va="center")
        ax.set_title("5. Kennedy SSQ Pre/Post")

    # --------------------------------------------------------------------------
    # Panel (1,2): SAGAT Situational Awareness (Endsley Levels 1, 2, 3)
    # --------------------------------------------------------------------------
    ax = axes[1, 2]
    sagat_df = hf_dict.get("SAGAT", pd.DataFrame())
    if not sagat_df.empty and "Condition" in sagat_df.columns:
        for c in ["OverallSaScorePct", "L1ScorePct", "L2ScorePct", "L3ScorePct"]:
            sagat_df[c] = pd.to_numeric(sagat_df[c], errors="coerce")

        conds = list(sagat_df["Condition"].unique())
        l1_vals = [sagat_df[sagat_df["Condition"] == c]["L1ScorePct"].mean() for c in conds]
        l2_vals = [sagat_df[sagat_df["Condition"] == c]["L2ScorePct"].mean() for c in conds]
        l3_vals = [sagat_df[sagat_df["Condition"] == c]["L3ScorePct"].mean() for c in conds]

        x = np.arange(len(conds))
        w = 0.25
        ax.bar(x - w, l1_vals, w, label="L1: Perception", color="#27ae60")
        ax.bar(x, l2_vals, w, label="L2: Comprehension", color="#2980b9")
        ax.bar(x + w, l3_vals, w, label="L3: Projection", color="#8e44ad")

        ax.set_xticks(x)
        ax.set_xticklabels([c.replace("_", "\n") for c in conds], rotation=15, ha="right")
        ax.set_ylabel("Accuracy (%)")
        ax.set_ylim(0, 110)
        ax.set_title("6. SAGAT Situational Awareness (Levels 1-3)")
        ax.legend(loc="upper right")
    else:
        ax.text(0.5, 0.5, "No SAGAT Records", ha="center", va="center")
        ax.set_title("6. SAGAT Situational Awareness")

    # --------------------------------------------------------------------------
    # Panel (2,0): Techno-Economic Cost & Break-Even Trajectory
    # --------------------------------------------------------------------------
    ax = axes[2, 0]
    sessions_curve = np.linspace(1, max(150, int(roi_model["BreakEvenSessionCount"] * 1.5)), 100)
    capex_vr = roi_model["CapExVrTotal"]
    opex_vr = roi_model["OpExVrPerSession"]
    opex_conv = roi_model["OpExConvPerSession"]

    c_vr_curve = capex_vr + (opex_vr * sessions_curve)
    c_conv_curve = opex_conv * sessions_curve

    ax.plot(sessions_curve, c_conv_curve / 1000.0, "r--", linewidth=2.2, label="Conventional Facility Cost")
    ax.plot(sessions_curve, c_vr_curve / 1000.0, "b-", linewidth=2.2, label="VR-Assisted Training Cost")

    be_sess = roi_model["BreakEvenSessionCount"]
    if roi_model["IsBreakEvenAchievable"] and be_sess < sessions_curve[-1]:
        be_cost = (opex_conv * be_sess) / 1000.0
        ax.plot(be_sess, be_cost, "ko", markersize=8)
        ax.annotate(
            f"Break-Even: {be_sess:.0f} Sessions\n(${be_cost:.1f}k)",
            xy=(be_sess, be_cost),
            xytext=(be_sess * 0.7, be_cost * 1.25),
            arrowprops=dict(facecolor="black", shrink=0.08, width=1, headwidth=6),
            fontsize=9, weight="bold"
        )
        # Fill savings zone
        idx_cross = np.where(sessions_curve >= be_sess)[0]
        if len(idx_cross) > 0:
            ax.fill_between(
                sessions_curve[idx_cross],
                c_vr_curve[idx_cross] / 1000.0,
                c_conv_curve[idx_cross] / 1000.0,
                color="green", alpha=0.15, label="Net Cost Substitution Zone"
            )

    ax.set_xlabel("Number of Completed Sessions (N_s)")
    ax.set_ylabel("Cumulative Expenditure ($k USD)")
    ax.set_title("7. Cumulative Expenditure & Break-Even")
    ax.legend(loc="upper left")

    # --------------------------------------------------------------------------
    # Panel (2,1): Dimensionless Operational Model (Phi & Asymptote)
    # --------------------------------------------------------------------------
    ax = axes[2, 1]
    dim = roi_model["Dimensionless"]
    kappa = dim["OperatingCostRatio_Kappa"]
    theta = dim["CapitalIntensityRatio_Theta"]

    # Phi(N_s) = kappa + theta / N_s
    sess_range = np.linspace(5, max(150, int(roi_model["BreakEvenSessionCount"] * 1.8)), 100)
    phi_curve = kappa + (theta / sess_range)

    ax.plot(sess_range, phi_curve, color="#2c3e50", linewidth=2.2, label=r"$\Phi(N_s) = \kappa + \theta/N_s$")
    ax.axhline(1.0, color="red", linestyle=":", linewidth=1.5, label="Parity Threshold (Phi = 1.0)")
    ax.axhline(kappa, color="blue", linestyle="--", linewidth=1.5, label=f"OpEx Asymptote (kappa = {kappa:.2f})")

    ax.set_xlabel("Number of Completed Sessions (N_s)")
    ax.set_ylabel("Normalized Cost Ratio Phi = C_vr / C_conv")
    ax.set_title("8. Dimensionless Operational Dynamics")
    ax.set_ylim(0, min(3.5, max(phi_curve)))
    ax.legend(loc="upper right")

    # --------------------------------------------------------------------------
    # Panel (2,2): Sensitivity Analysis (ROI vs. CapEx / OpEx Variations)
    # --------------------------------------------------------------------------
    ax = axes[2, 2]
    variations = [-30, -15, 0, 15, 30]
    sens = compute_cost_sensitivity(TechnoEconomicAssumptions(**roi_model["Assumptions"]), [v/100.0 for v in variations])

    roi_capex = [s["RoiPercent"] for s in sens["CapExSensitivity"]]
    roi_opex = [s["RoiPercent"] for s in sens["OpExSensitivity"]]

    ax.plot(variations, roi_capex, "o-", color="#e74c3c", linewidth=2.0, label="CapEx Sensitivity (±30%)")
    ax.plot(variations, roi_opex, "s-", color="#2980b9", linewidth=2.0, label="VR OpEx Sensitivity (±30%)")
    ax.axvline(0, color="gray", linestyle="--", alpha=0.6)

    ax.set_xlabel("Parameter Variation (%)")
    ax.set_ylabel("Projected ROI (%)")
    ax.set_title("9. Techno-Economic Sensitivity Analysis")
    ax.legend()

    plt.tight_layout(rect=[0, 0.03, 1, 0.96])
    fig.savefig(fig_path, dpi=300)
    plt.close(fig)

    print(f"[Dashboard Exported] => {fig_path.resolve()}")
    return fig_path


# ==============================================================================
# 4. CLI AND EXECUTION ORCHESTRATION
# ==============================================================================

def main():
    parser = argparse.ArgumentParser(
        description="IVRAR Group 02 Techno-Managerial Evaluation & Research Analytics Dashboard"
    )
    # Telemetry data inputs
    parser.add_argument("--trials-csv", type=Path, default=Path("telemetry/SYNTHETIC_TEST_reaction_latency_trial_log.csv"),
                        help="Path to trial reaction latency and signal detection CSV")
    parser.add_argument("--hf-csv", type=Path, default=Path("telemetry/SYNTHETIC_TEST_human_factors_assessments.csv"),
                        help="Path to human factors assessments CSV (NASA-TLX, SSQ, SAGAT)")
    parser.add_argument("--output-dir", "-o", type=Path, default=Path("telemetry/figures"),
                        help="Output directory for generated publication charts and dashboard")

    # Techno-economic assumptions
    parser.add_argument("--vr-hw-capex", type=float, default=8500.0, help="Initial VR hardware capital cost ($)")
    parser.add_argument("--vr-sw-cost", type=float, default=12000.0, help="Software development & acquisition cost ($)")
    parser.add_argument("--vr-maint-annual", type=float, default=1500.0, help="Annual VR maintenance & calibration ($)")
    parser.add_argument("--vr-opex-session", type=float, default=15.0, help="VR operating cost per session ($)")
    parser.add_argument("--vr-instructor-session", type=float, default=40.0, help="VR technician/instructor cost per session ($)")
    parser.add_argument("--fac-rental-session", type=float, default=220.0, help="Conventional facility rental per session ($)")
    parser.add_argument("--consumables-participant", type=float, default=35.0, help="Conventional consumables per trainee ($)")
    parser.add_argument("--conv-instructor-session", type=float, default=90.0, help="Conventional instructor/safety officer per session ($)")
    parser.add_argument("--conv-opex-session", type=float, default=45.0, help="Conventional logistics & cleanup per session ($)")
    parser.add_argument("--participants", type=int, default=50, help="Total number of participants")
    parser.add_argument("--sessions", type=int, default=100, help="Total number of completed training sessions")
    parser.add_argument("--participants-per-session", type=int, default=4, help="Concurrent participants per training session")

    args = parser.parse_args()

    # 1. Instantiate Assumptions
    assump = TechnoEconomicAssumptions(
        vr_hardware_investment=args.vr_hw_capex,
        vr_software_development_cost=args.vr_sw_cost,
        vr_maintenance_annual=args.vr_maint_annual,
        vr_operating_cost_per_session=args.vr_opex_session,
        vr_instructor_cost_per_session=args.vr_instructor_session,
        facility_rental_per_session=args.fac_rental_session,
        consumables_per_participant_session=args.consumables_participant,
        conventional_instructor_per_session=args.conv_instructor_session,
        conventional_operating_cost_per_session=args.conv_opex_session,
        number_of_participants=args.participants,
        number_of_sessions=args.sessions,
        participants_per_session=args.participants_per_session
    )

    print("=" * 78)
    print("   IVRAR GROUP 02: TECHNO-ECONOMIC EVALUATION & RESEARCH ANALYTICS")
    print("=" * 78)

    # 2. Compute Techno-Economic Evaluation Model
    roi_model = compute_techno_economic_model(assump)

    print("\n[TECHNO-ECONOMIC EVALUATION RESULTS]")
    print(f"  Conventional Facility Cost:   ${roi_model['TotalConventionalCost']:,.2f}")
    print(f"  VR-Assisted Training Cost:    ${roi_model['TotalVrCost']:,.2f}")
    print(f"  Cost Substitution (Savings):  ${roi_model['CostSubstitution']:,.2f}")
    print(f"  Cost Per Trainee (Conv):      ${roi_model['CostPerParticipantConv']:,.2f}")
    print(f"  Cost Per Trainee (VR):        ${roi_model['CostPerParticipantVr']:,.2f}")
    print(f"  Break-Even Session Count:     {roi_model['BreakEvenSessionCount']:.1f} sessions")
    print(f"  Projected Return on Invest:   {roi_model['RoiPercent']:.1f}%")

    dim = roi_model["Dimensionless"]
    print("\n[DIMENSIONLESS OPERATIONAL METRICS]")
    print(f"  Operating Cost Ratio (kappa):        {dim['OperatingCostRatio_Kappa']:.3f}  (OpEx_VR / OpEx_Conv)")
    print(f"  Capital Intensity Ratio (theta):     {dim['CapitalIntensityRatio_Theta']:.3f}  (CapEx_VR / OpEx_Conv_session)")
    print(f"  Dimensionless Break-even (eta*):     {dim['DimensionlessBreakEven_Eta']:.2f}  (theta / (1 - kappa))")
    print(f"  Normalized Cost Ratio (Phi):         {dim['NormalizedCostRatio_Phi']:.3f}  (C_VR / C_Conv at N_s={args.sessions})")
    print(f"  Asymptotic Maximum Savings Limit:    {dim['AsymptoticSavingsLimit']:.1f}% as N_s -> infinity")

    # 3. Load Telemetry & Human Factors
    trial_df = pd.DataFrame()
    hf_dict = {}

    if args.trials_csv.exists():
        print(f"\n[Loading Trial Telemetry] => {args.trials_csv.resolve()}")
        trial_df = load_trial_telemetry(args.trials_csv)
        print(f"  Loaded {len(trial_df)} trial observation rows.")
    else:
        print(f"\n[Warning] Trial CSV not found: {args.trials_csv}")

    if args.hf_csv.exists():
        print(f"\n[Loading Human Factors Data] => {args.hf_csv.resolve()}")
        hf_dict = load_human_factors_assessments(args.hf_csv)
        print(f"  Loaded {len(hf_dict['Full'])} human factors records (TLX: {len(hf_dict['TLX'])}, SSQ: {len(hf_dict['SSQ'])}, SAGAT: {len(hf_dict['SAGAT'])}).")
    else:
        print(f"\n[Warning] Human Factors CSV not found: {args.hf_csv}")

    # 4. Generate Publication-Ready Dashboard
    dash_path = export_publication_dashboard(trial_df, hf_dict, roi_model, args.output_dir)

    print("\n" + "=" * 78)
    print("ACADEMIC & METHODOLOGICAL INTEGRITY NOTICE:")
    print("- All financial values represent parameterized model assumptions for research comparison.")
    print("- No empirical cost substitution in facility rentals, munitions, or training hours")
    print("  is claimed without externally certified institution receipts.")
    print("=" * 78)


if __name__ == "__main__":
    main()
