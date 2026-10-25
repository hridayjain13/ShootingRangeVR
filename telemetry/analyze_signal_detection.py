#!/usr/bin/env python3
"""
IVRAR Group 02: Signal Detection Theory (SDT) & Reaction Latency Analysis Tool

Computes parametric Signal Detection Theory metrics (d', beta, criterion c),
extreme-rate corrections (Hautus 1995 / Macmillan & Kaplan 1985), and descriptive
reaction time statistics across experimental conditions.
"""

import argparse
import math
import os
import sys
from pathlib import Path
from statistics import NormalDist
import pandas as pd
import numpy as np


def compute_sdt_metrics(hits: int, misses: int, false_alarms: int, correct_rejections: int, correction_method: str = "hautus") -> dict:
    """
    Computes Signal Detection Theory parameters with documented correction for extreme rates.

    Parameters:
    - hits (H): Signal-present threat trials correctly engaged.
    - misses (M): Signal-present threat trials missed or withheld.
    - false_alarms (FA): Signal-absent or civilian trials incorrectly engaged.
    - correct_rejections (CR): Signal-absent or civilian trials correctly withheld.
    - correction_method: 'hautus' (log-linear +0.5 to all cells) or 'macmillan' (1/(2N) clamping).

    Returns dictionary containing Hit Rate, FA Rate, d', beta, criterion c, and adjusted rates.
    """
    signal_present_total = hits + misses
    signal_absent_total = false_alarms + correct_rejections
    total_trials = signal_present_total + signal_absent_total

    if total_trials == 0:
        return {
            "hits": 0, "misses": 0, "false_alarms": 0, "correct_rejections": 0,
            "signal_present_total": 0, "signal_absent_total": 0, "total_trials": 0,
            "raw_hit_rate": 0.0, "raw_fa_rate": 0.0,
            "adj_hit_rate": 0.5, "adj_fa_rate": 0.5,
            "d_prime": 0.0, "beta": 1.0, "criterion_c": 0.0,
            "correction_applied": "none_zero_trials"
        }

    raw_hit_rate = hits / signal_present_total if signal_present_total > 0 else 0.0
    raw_fa_rate = false_alarms / signal_absent_total if signal_absent_total > 0 else 0.0

    # Extreme rate correction to avoid infinite Z-scores
    normal = NormalDist()
    correction_applied = "none"

    if correction_method == "hautus":
        # Log-linear rule (Hautus, 1995): Add 0.5 to all 4 frequency cells
        # N_s becomes N_s + 1, N_n becomes N_n + 1
        adj_hit_rate = (hits + 0.5) / (signal_present_total + 1.0)
        adj_fa_rate = (false_alarms + 0.5) / (signal_absent_total + 1.0)
        correction_applied = "hautus_log_linear_1995"
    else:
        # Macmillan & Kaplan (1985) / 1/(2N) rule
        adj_hit_rate = raw_hit_rate
        adj_fa_rate = raw_fa_rate

        if signal_present_total > 0:
            if raw_hit_rate == 1.0:
                adj_hit_rate = 1.0 - (1.0 / (2.0 * signal_present_total))
                correction_applied = "macmillan_kaplan_1985"
            elif raw_hit_rate == 0.0:
                adj_hit_rate = 1.0 / (2.0 * signal_present_total)
                correction_applied = "macmillan_kaplan_1985"

        if signal_absent_total > 0:
            if raw_fa_rate == 1.0:
                adj_fa_rate = 1.0 - (1.0 / (2.0 * signal_absent_total))
                correction_applied = "macmillan_kaplan_1985"
            elif raw_fa_rate == 0.0:
                adj_fa_rate = 1.0 / (2.0 * signal_absent_total)
                correction_applied = "macmillan_kaplan_1985"

    # Clamp adjusted rates to strictly open interval (1e-6, 1 - 1e-6)
    adj_hit_rate = max(1e-6, min(1.0 - 1e-6, adj_hit_rate))
    adj_fa_rate = max(1e-6, min(1.0 - 1e-6, adj_fa_rate))

    z_hit = normal.inv_cdf(adj_hit_rate)
    z_fa = normal.inv_cdf(adj_fa_rate)

    # d' = Z(Hit) - Z(FA)
    d_prime = z_hit - z_fa

    # beta = exp((Z(FA)^2 - Z(Hit)^2) / 2)
    beta = math.exp((z_fa ** 2 - z_hit ** 2) / 2.0)

    # Criterion c = -0.5 * (Z(Hit) + Z(FA))
    criterion_c = -0.5 * (z_hit + z_fa)

    return {
        "hits": hits,
        "misses": misses,
        "false_alarms": false_alarms,
        "correct_rejections": correct_rejections,
        "signal_present_total": signal_present_total,
        "signal_absent_total": signal_absent_total,
        "total_trials": total_trials,
        "raw_hit_rate": raw_hit_rate,
        "raw_fa_rate": raw_fa_rate,
        "adj_hit_rate": adj_hit_rate,
        "adj_fa_rate": adj_fa_rate,
        "d_prime": d_prime,
        "beta": beta,
        "criterion_c": criterion_c,
        "correction_applied": correction_applied
    }


def compute_reaction_time_stats(latencies_ms: list) -> dict:
    """
    Computes descriptive reaction latency statistics from a list of millisecond values.
    Filters out invalid, negative, or NaN values.
    """
    valid = [float(x) for x in latencies_ms if pd.notna(x) and float(x) > 0.0]

    if not valid:
        return {
            "count": 0,
            "mean_ms": float("nan"),
            "median_ms": float("nan"),
            "std_dev_ms": float("nan"),
            "min_ms": float("nan"),
            "max_ms": float("nan")
        }

    arr = np.array(valid)
    return {
        "count": len(arr),
        "mean_ms": float(np.mean(arr)),
        "median_ms": float(np.median(arr)),
        "std_dev_ms": float(np.std(arr, ddof=1)) if len(arr) > 1 else 0.0,
        "min_ms": float(np.min(arr)),
        "max_ms": float(np.max(arr))
    }


def analyze_dataset(df: pd.DataFrame, correction_method: str = "hautus") -> dict:
    """
    Performs full SDT, reaction time, and condition-wise comparative analysis.
    Handles column name aliasing and invalid rows gracefully.
    """
    # Standardize column names
    aliases = {
        "participantcode": "participant_id",
        "participantid": "participant_id",
        "sessionid": "session_id",
        "conditionlabel": "condition",
        "condition": "condition",
        "experimentalcondition": "condition",
        "latencyms": "latency_ms",
        "presentationlatencyms": "latency_ms",
        "hit": "hit",
        "miss": "miss",
        "falsealarm": "false_alarm",
        "correctrejection": "correct_rejection",
        "outcome": "outcome",
        "targettype": "target_type",
        "primarycategory": "stimulus_category"
    }

    norm_df = df.copy()
    col_mapping = {}
    for col in norm_df.columns:
        clean = col.replace("_", "").lower().strip()
        if clean in aliases:
            col_mapping[col] = aliases[clean]
    norm_df.rename(columns=col_mapping, inplace=True)

    # Ensure required outcome indicators exist
    if "hit" not in norm_df.columns and "outcome" in norm_df.columns:
        norm_df["hit"] = (norm_df["outcome"].astype(str).str.lower() == "hit").astype(int)
        norm_df["miss"] = (norm_df["outcome"].astype(str).str.lower() == "miss").astype(int)
        norm_df["false_alarm"] = (norm_df["outcome"].astype(str).str.lower() == "falsealarm").astype(int)
        norm_df["correct_rejection"] = (norm_df["outcome"].astype(str).str.lower() == "correctrejection").astype(int)

    # Fallback to zero if missing
    for col in ["hit", "miss", "false_alarm", "correct_rejection"]:
        if col not in norm_df.columns:
            norm_df[col] = 0
        else:
            norm_df[col] = pd.to_numeric(norm_df[col], errors="coerce").fillna(0).astype(int)

    total_hits = int(norm_df["hit"].sum())
    total_misses = int(norm_df["miss"].sum())
    total_fas = int(norm_df["false_alarm"].sum())
    total_crs = int(norm_df["correct_rejection"].sum())

    overall_sdt = compute_sdt_metrics(total_hits, total_misses, total_fas, total_crs, correction_method)

    # Overall reaction time
    latencies = norm_df["latency_ms"].tolist() if "latency_ms" in norm_df.columns else []
    overall_rt = compute_reaction_time_stats(latencies)

    # Overall accuracy and rates
    total_trials = total_hits + total_misses + total_fas + total_crs
    accuracy = (total_hits + total_crs) / total_trials if total_trials > 0 else 0.0
    miss_rate = total_misses / (total_hits + total_misses) if (total_hits + total_misses) > 0 else 0.0
    fa_rate = total_fas / (total_fas + total_crs) if (total_fas + total_crs) > 0 else 0.0

    # Condition-wise comparisons
    condition_results = {}
    if "condition" in norm_df.columns:
        for cond_name, group in norm_df.groupby("condition"):
            cond_hits = int(group["hit"].sum())
            cond_misses = int(group["miss"].sum())
            cond_fas = int(group["false_alarm"].sum())
            cond_crs = int(group["correct_rejection"].sum())
            cond_total = cond_hits + cond_misses + cond_fas + cond_crs

            cond_sdt = compute_sdt_metrics(cond_hits, cond_misses, cond_fas, cond_crs, correction_method)
            cond_rt = compute_reaction_time_stats(group["latency_ms"].tolist() if "latency_ms" in group.columns else [])
            cond_acc = (cond_hits + cond_crs) / cond_total if cond_total > 0 else 0.0

            condition_results[str(cond_name)] = {
                "total_trials": cond_total,
                "hits": cond_hits, "misses": cond_misses, "false_alarms": cond_fas, "correct_rejections": cond_crs,
                "accuracy": cond_acc,
                "sdt": cond_sdt,
                "rt": cond_rt
            }

    return {
        "overall_sdt": overall_sdt,
        "overall_rt": overall_rt,
        "total_trials": total_trials,
        "accuracy": accuracy,
        "miss_rate": miss_rate,
        "fa_rate": fa_rate,
        "condition_comparisons": condition_results
    }


def print_formatted_summary(results: dict):
    """Prints a clear, scientific summary of SDT and reaction latency metrics."""
    sdt = results["overall_sdt"]
    rt = results["overall_rt"]

    print("=" * 68)
    print("      IVRAR GROUP 02: SIGNAL DETECTION & LATENCY ANALYSIS SUMMARY")
    print("=" * 68)
    print(f"Total Analyzed Trials: {results['total_trials']}")
    print(f"Accuracy Rate:         {results['accuracy'] * 100:.2f}%")
    print(f"Signal-Present Trials: {sdt['signal_present_total']} (Hits: {sdt['hits']}, Misses: {sdt['misses']})")
    print(f"Signal-Absent Trials:  {sdt['signal_absent_total']} (False Alarms: {sdt['false_alarms']}, Correct Rejections: {sdt['correct_rejections']})")
    print("-" * 68)
    print("SIGNAL DETECTION THEORY METRICS:")
    print(f"  Hit Rate:            {sdt['raw_hit_rate'] * 100:.2f}% (Adjusted: {sdt['adj_hit_rate'] * 100:.2f}%)")
    print(f"  False Alarm Rate:    {sdt['raw_fa_rate'] * 100:.2f}% (Adjusted: {sdt['adj_fa_rate'] * 100:.2f}%)")
    print(f"  Sensitivity (d'):    {sdt['d_prime']:.4f}")
    print(f"  Response Bias (beta):{sdt['beta']:.4f}")
    print(f"  Criterion (c):       {sdt['criterion_c']:.4f}")
    print(f"  Rate Correction:     {sdt['correction_applied']}")
    print("-" * 68)
    print("REACTION LATENCY METRICS:")
    print(f"  Valid RT Samples:    {rt['count']}")
    print(f"  Mean Latency:        {rt['mean_ms']:.1f} ms")
    print(f"  Median Latency:      {rt['median_ms']:.1f} ms")
    print(f"  Std Deviation:       {rt['std_dev_ms']:.1f} ms")
    print(f"  Range [Min, Max]:    [{rt['min_ms']:.1f} ms, {rt['max_ms']:.1f} ms]")

    conds = results.get("condition_comparisons", {})
    if conds:
        print("=" * 68)
        print("CONDITION-WISE COMPARISONS:")
        print(f"{'Condition':<32} {'Trials':<8} {'Acc%':<8} {'d-prime':<9} {'Mean RT':<10}")
        print("-" * 68)
        for cname, cdata in conds.items():
            acc_str = f"{cdata['accuracy'] * 100:.1f}%"
            dprime_str = f"{cdata['sdt']['d_prime']:.3f}"
            rt_str = f"{cdata['rt']['mean_ms']:.1f} ms" if not math.isnan(cdata['rt']['mean_ms']) else "N/A"
            print(f"{cname[:30]:<32} {cdata['total_trials']:<8} {acc_str:<8} {dprime_str:<9} {rt_str:<10}")
    print("=" * 68)


def main():
    parser = argparse.ArgumentParser(description="Signal Detection Theory (SDT) & Reaction Latency Analysis")
    parser.add_argument("--input", "-i", type=str, default="telemetry/reaction_latency_trial_log.csv",
                        help="Path to CSV trial log")
    parser.add_argument("--output", "-o", type=str, default=None,
                        help="Optional path to export summary JSON/CSV")
    parser.add_argument("--correction", "-c", choices=["hautus", "macmillan"], default="hautus",
                        help="Extreme rate correction method (default: hautus log-linear)")
    args = parser.parse_args()

    input_path = Path(args.input)
    if not input_path.exists():
        print(f"[Error] Input file not found: {input_path}")
        sys.exit(1)

    print(f"[Info] Reading dataset: {input_path.resolve()}")
    try:
        df = pd.read_csv(input_path, comment="#")
    except Exception as ex:
        print(f"[Error] Failed to parse CSV: {ex}")
        sys.exit(1)

    results = analyze_dataset(df, correction_method=args.correction)
    print_formatted_summary(results)


if __name__ == "__main__":
    main()
