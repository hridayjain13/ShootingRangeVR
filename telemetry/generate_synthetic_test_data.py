#!/usr/bin/env python3
"""
IVRAR Group 02: Synthetic Test-Data Generator for Development Only

NOTICE:
Synthetic data produced by this script is strictly for software pipeline verification,
unit testing, and visualization prototyping. It must NEVER be presented as actual
participant empirical results. All rows are explicitly tagged with data_status=synthetic_test_fixture.
"""

import argparse
from datetime import datetime, timezone
import math
from pathlib import Path
import random
import sys


def generate_synthetic_trial_dataset(output_path: Path, num_participants: int = 4, trials_per_block: int = 12, seed: int = 42):
    """
    Generates synthetic trial-level signal detection records across 4 conditions.
    """
    random.seed(seed)
    conditions = [
        ("Baseline_Daylight", 380.0, 45.0, 0.92, 0.05),
        ("LowVisibility_Night", 480.0, 60.0, 0.82, 0.12),
        ("SensoryDistraction_Daylight", 540.0, 75.0, 0.78, 0.15),
        ("Combined_LowVis_SensoryStress", 630.0, 95.0, 0.68, 0.22)
    ]

    header_notice = [
        "# NOTICE: SYNTHETIC TEST DATA GENERATED FOR SOFTWARE VERIFICATION ONLY.",
        "# DO NOT USE AS EMPIRICAL PARTICIPANT RESULTS.",
        "# PROVENANCE: SYNTHETIC / GENERATED VIA generate_synthetic_test_data.py",
        f"# GENERATION_TIMESTAMP_UTC: {datetime.now(timezone.utc).isoformat()}",
        f"# RANDOM_SEED: {seed}"
    ]

    columns = [
        "TimestampUtc", "ParticipantID", "SessionID", "Condition", "DataStatus",
        "SimulatedLuxTarget", "FogDensity", "TargetContrast", "AmbientIntensity",
        "TrialID", "TargetType", "TraineeAction", "LatencyMs",
        "Hit", "Miss", "FalseAlarm", "CorrectRejection",
        "DistanceMeters", "PresentationAngleDeg", "HeadYawJitterVarianceDeg2",
        "DPrime", "BetaCriterion"
    ]

    lines = list(header_notice)
    lines.append(",".join(columns))

    for p_idx in range(1, num_participants + 1):
        p_code = f"P{p_idx:02d}_SYNTHETIC"
        session_id = "S01_TEST"

        trial_counter = 0
        cum_hits = 0
        cum_misses = 0
        cum_fas = 0
        cum_crs = 0

        for cond_name, mean_rt, std_rt, p_hit, p_fa in conditions:
            # Set environmental simulation parameters
            sim_lux = 5.0 if "Baseline" in cond_name else (1.5 if "Daylight" in cond_name else 0.5)
            fog_density = 0.001 if "Baseline" in cond_name else (0.018 if "Night" in cond_name else 0.065)
            target_contrast = 1.0 if "Baseline" in cond_name else (0.65 if "Daylight" in cond_name else 0.35)
            ambient_intensity = 0.8 if "Baseline" in cond_name else (0.18 if "Daylight" in cond_name else 0.06)

            for t_idx in range(1, trials_per_block + 1):
                trial_counter += 1
                trial_id = f"T{trial_counter:03d}"
                now_utc = datetime.now(timezone.utc).isoformat()

                # 70% Signal-Present (Threat), 30% Signal-Absent (Civilian / Catch)
                is_signal_present = random.random() < 0.70

                if is_signal_present:
                    target_type = "Hostile"
                    engaged = random.random() < p_hit
                    is_hit = 1 if engaged else 0
                    is_miss = 1 if not engaged else 0
                    is_fa = 0
                    is_cr = 0
                else:
                    target_type = "Civilian"
                    engaged = random.random() < p_fa
                    is_hit = 0
                    is_miss = 0
                    is_fa = 1 if engaged else 0
                    is_cr = 1 if not engaged else 0

                cum_hits += is_hit
                cum_misses += is_miss
                cum_fas += is_fa
                cum_crs += is_cr

                action = "Shoot" if engaged else "Hold"
                distance = random.choice([3.5, 8.0, 14.0])
                angle = random.uniform(-25.0, 25.0)

                # Generate latency with log-normal or Gaussian right skew
                raw_latency = random.gauss(mean_rt, std_rt) + (distance * 6.0)
                latency_ms = max(220.0, raw_latency) if engaged else 0.0

                yaw_variance = random.uniform(0.002, 0.035)

                # Parametric d' calculation (with Hautus correction)
                adj_h = (cum_hits + 0.5) / (cum_hits + cum_misses + 1.0)
                adj_fa = (cum_fas + 0.5) / (cum_fas + cum_crs + 1.0)

                # Approximate d'
                z_h = math.sqrt(2.0) * _approx_erfinv(2.0 * adj_h - 1.0)
                z_fa = math.sqrt(2.0) * _approx_erfinv(2.0 * adj_fa - 1.0)
                d_prime = z_h - z_fa
                beta = math.exp((z_fa ** 2 - z_h ** 2) / 2.0)

                row = [
                    now_utc, p_code, session_id, cond_name, "synthetic_test_fixture",
                    f"{sim_lux:.2f}", f"{fog_density:.4f}", f"{target_contrast:.2f}", f"{ambient_intensity:.2f}",
                    trial_id, target_type, action, f"{latency_ms:.1f}",
                    str(is_hit), str(is_miss), str(is_fa), str(is_cr),
                    f"{distance:.2f}", f"{angle:.2f}", f"{yaw_variance:.4f}",
                    f"{d_prime:.3f}", f"{beta:.3f}"
                ]
                lines.append(",".join(row))

    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(f"[Synthetic Generator] Generated {len(lines) - len(header_notice) - 1} trial rows at: {output_path.resolve()}")


def generate_synthetic_continuous_telemetry(output_path: Path, num_samples: int = 500, target_hz: float = 90.0, seed: int = 42):
    """
    Generates synthetic 90 Hz continuous head tracking telemetry samples.
    """
    random.seed(seed)
    ideal_interval_ms = 1000.0 / target_hz

    header_notice = [
        "# NOTICE: SYNTHETIC TEST DATA GENERATED FOR SOFTWARE VERIFICATION ONLY.",
        "# DO NOT USE AS EMPIRICAL PARTICIPANT RESULTS.",
        f"# TARGET_SAMPLING_RATE_HZ: {target_hz}",
        f"# GENERATION_TIMESTAMP_UTC: {datetime.now(timezone.utc).isoformat()}"
    ]

    columns = [
        "TimestampUtc", "SessionTimeSec", "ParticipantID", "SessionID", "Condition",
        "ActualIntervalMs", "AchievedHz", "DroppedSample",
        "PosX", "PosY", "PosZ", "RotYaw", "RotPitch", "RotRoll", "YawVarianceDeg2"
    ]

    lines = list(header_notice)
    lines.append(",".join(columns))

    t_sec = 0.0
    for i in range(num_samples):
        jitter_ms = random.gauss(0.0, 0.4)
        is_dropped = random.random() < 0.01
        if is_dropped:
            jitter_ms += ideal_interval_ms * 0.8

        interval_ms = max(5.0, ideal_interval_ms + jitter_ms)
        achieved_hz = 1000.0 / interval_ms
        t_sec += interval_ms / 1000.0

        now_utc = datetime.now(timezone.utc).isoformat()
        pos_x = random.gauss(0.0, 0.02)
        pos_y = 1.60 + random.gauss(0.0, 0.01)
        pos_z = random.gauss(0.0, 0.02)
        yaw = random.uniform(-15.0, 15.0)
        pitch = random.uniform(-8.0, 8.0)
        roll = random.uniform(-2.0, 2.0)
        variance = random.uniform(0.005, 0.025)

        row = [
            now_utc, f"{t_sec:.3f}", "P01_SYNTHETIC", "S01_TEST", "Combined_LowVis_SensoryStress",
            f"{interval_ms:.2f}", f"{achieved_hz:.1f}", "TRUE" if is_dropped else "FALSE",
            f"{pos_x:.3f}", f"{pos_y:.3f}", f"{pos_z:.3f}",
            f"{yaw:.2f}", f"{pitch:.2f}", f"{roll:.2f}", f"{variance:.4f}"
        ]
        lines.append(",".join(row))

    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(f"[Synthetic Generator] Generated {num_samples} 90Hz telemetry samples at: {output_path.resolve()}")


def _approx_erfinv(x: float) -> float:
    """Winitzki approximation of inverse error function."""
    a = 0.147
    x = max(-0.999999, min(0.999999, x))
    log_term = math.log(1.0 - x * x)
    term1 = 2.0 / (math.pi * a) + log_term / 2.0
    term2 = log_term / a
    inner = term1 * term1 - term2
    sign = 1.0 if x >= 0 else -1.0
    return sign * math.sqrt(math.sqrt(inner) - term1)


def generate_synthetic_human_factors_dataset(output_path: Path, num_participants: int = 4, seed: int = 42):
    """
    Generates synthetic human factors assessment data (NASA-TLX, Kennedy SSQ, SAGAT)
    for offline testing of telemetry/analyze_human_factors.py.
    """
    random.seed(seed)

    header_notice = [
        "# NOTICE: SYNTHETIC TEST FIXTURE FOR SOFTWARE VERIFICATION ONLY.",
        "# DO NOT USE AS EMPIRICAL PARTICIPANT RESULTS.",
        "# PROVENANCE: SYNTHETIC / GENERATED VIA generate_synthetic_test_data.py",
        f"# GENERATION_TIMESTAMP_UTC: {datetime.now(timezone.utc).isoformat()}",
        f"# RANDOM_SEED: {seed}"
    ]

    # Universal schema accommodating TLX, SSQ, and SAGAT records
    columns = [
        "TimestampUtc", "ParticipantID", "SessionID", "Condition", "TrialID",
        "Instrument", "Timing", "CompletionStatus",
        # NASA-TLX columns
        "MentalDemand", "PhysicalDemand", "TemporalDemand", "Performance", "Effort", "Frustration", "RawTlxMean",
        # Kennedy SSQ columns
        "GeneralDiscomfort", "Fatigue", "Headache", "EyeStrain", "DifficultyFocusing",
        "SalivationIncreasing", "Sweating", "Nausea", "DifficultyConcentrating",
        "FullnessOfHead", "BlurredVision", "DizzinessEyesOpen", "DizzinessEyesClosed",
        "Vertigo", "StomachAwareness", "Burping",
        "NauseaScore", "OculomotorScore", "DisorientationScore", "TotalSeverityScore",
        # SAGAT columns
        "TotalProbes", "TotalCorrect", "OverallSaScorePct",
        "L1Correct", "L1Total", "L1ScorePct",
        "L2Correct", "L2Total", "L2ScorePct",
        "L3Correct", "L3Total", "L3ScorePct"
    ]

    lines = list(header_notice)
    lines.append(",".join(columns))

    conditions = [
        ("Baseline_Daylight", 28.0, 10.0, 92.0),
        ("LowVisibility_Night", 48.0, 15.0, 80.0),
        ("SensoryDistraction_Daylight", 58.0, 18.0, 75.0),
        ("Combined_LowVis_SensoryStress", 72.0, 20.0, 62.0)
    ]

    for p_idx in range(1, num_participants + 1):
        p_code = f"P{p_idx:02d}_SYNTHETIC"
        session_id = "S01_TEST"
        now_utc = datetime.now(timezone.utc).isoformat()

        # 1. Pre-Session Kennedy SSQ (Baseline)
        pre_symptoms = [0] * 16
        # Slight baseline tiredness or discomfort in a few participants
        if random.random() < 0.4:
            pre_symptoms[0] = 1 # Discomfort
        if random.random() < 0.5:
            pre_symptoms[1] = 1 # Fatigue

        n_raw = pre_symptoms[0] + pre_symptoms[5] + pre_symptoms[6] + pre_symptoms[7] + pre_symptoms[8] + pre_symptoms[14] + pre_symptoms[15]
        o_raw = pre_symptoms[0] + pre_symptoms[1] + pre_symptoms[2] + pre_symptoms[3] + pre_symptoms[4] + pre_symptoms[8] + pre_symptoms[10]
        d_raw = pre_symptoms[4] + pre_symptoms[7] + pre_symptoms[9] + pre_symptoms[10] + pre_symptoms[11] + pre_symptoms[12] + pre_symptoms[13]

        pre_n_score = n_raw * 9.54
        pre_o_score = o_raw * 7.58
        pre_d_score = d_raw * 13.92
        pre_ts_score = (n_raw + o_raw + d_raw) * 3.74

        row_pre_ssq = [
            now_utc, p_code, session_id, "Baseline_Pre", "PRE-01",
            "KennedySSQ", "PreSession", "Completed",
            "-1", "-1", "-1", "-1", "-1", "-1", "-1"
        ] + [str(s) for s in pre_symptoms] + [
            f"{pre_n_score:.2f}", f"{pre_o_score:.2f}", f"{pre_d_score:.2f}", f"{pre_ts_score:.2f}",
            "-1", "-1", "-1", "-1", "-1", "-1", "-1", "-1", "-1", "-1", "-1", "-1"
        ]
        lines.append(",".join(row_pre_ssq))

        # 2. Block-level NASA-TLX and SAGAT freezes across conditions
        for cond_idx, (cond_name, base_tlx, tlx_sd, sa_base) in enumerate(conditions):
            # NASA-TLX
            m = max(5.0, min(95.0, random.gauss(base_tlx * 1.05, tlx_sd)))
            p = max(5.0, min(95.0, random.gauss(base_tlx * 0.85, tlx_sd)))
            t = max(5.0, min(95.0, random.gauss(base_tlx * 1.10, tlx_sd)))
            perf = max(5.0, min(95.0, random.gauss(base_tlx * 0.90, tlx_sd)))
            eff = max(5.0, min(95.0, random.gauss(base_tlx * 1.08, tlx_sd)))
            fru = max(5.0, min(95.0, random.gauss(base_tlx * 0.95, tlx_sd)))
            rtlx_mean = (m + p + t + perf + eff + fru) / 6.0

            row_tlx = [
                now_utc, p_code, session_id, cond_name, f"BLOCK-TLX-{cond_idx+1:02d}",
                "NasaTlx", "PostBlock", "Completed",
                f"{m:.1f}", f"{p:.1f}", f"{t:.1f}", f"{perf:.1f}", f"{eff:.1f}", f"{fru:.1f}", f"{rtlx_mean:.2f}"
            ] + ["-1"] * 20 + [
                "-1", "-1", "-1", "-1", "-1", "-1", "-1", "-1", "-1", "-1", "-1", "-1"
            ]
            lines.append(",".join(row_tlx))

            # SAGAT Freeze
            l1_tot, l2_tot, l3_tot = 3, 2, 1
            tot_probes = l1_tot + l2_tot + l3_tot
            p_sa = max(0.40, min(0.98, sa_base / 100.0 + random.uniform(-0.08, 0.08)))
            l1_corr = sum(1 for _ in range(l1_tot) if random.random() < p_sa)
            l2_corr = sum(1 for _ in range(l2_tot) if random.random() < p_sa)
            l3_corr = sum(1 for _ in range(l3_tot) if random.random() < p_sa)
            tot_corr = l1_corr + l2_corr + l3_corr

            ov_pct = (tot_corr / tot_probes) * 100.0
            l1_pct = (l1_corr / l1_tot) * 100.0
            l2_pct = (l2_corr / l2_tot) * 100.0
            l3_pct = (l3_corr / l3_tot) * 100.0

            row_sagat = [
                now_utc, p_code, session_id, cond_name, f"FREEZE-{cond_idx+1:02d}",
                "Sagat", "DuringFreeze", "Completed",
                "-1", "-1", "-1", "-1", "-1", "-1", "-1"
            ] + ["-1"] * 20 + [
                str(tot_probes), str(tot_corr), f"{ov_pct:.1f}",
                str(l1_corr), str(l1_tot), f"{l1_pct:.1f}",
                str(l2_corr), str(l2_tot), f"{l2_pct:.1f}",
                str(l3_corr), str(l3_tot), f"{l3_pct:.1f}"
            ]
            lines.append(",".join(row_sagat))

        # 3. Post-Session Kennedy SSQ
        post_symptoms = list(pre_symptoms)
        # Moderate simulator sickness elevation in eye strain, slight nausea, sweating
        if random.random() < 0.6: post_symptoms[3] = min(3, post_symptoms[3] + 1) # Eye strain
        if random.random() < 0.5: post_symptoms[4] = min(3, post_symptoms[4] + 1) # Focusing
        if random.random() < 0.35: post_symptoms[6] = min(3, post_symptoms[6] + 1) # Sweating
        if random.random() < 0.3: post_symptoms[7] = min(3, post_symptoms[7] + 1) # Nausea
        if random.random() < 0.4: post_symptoms[10] = min(3, post_symptoms[10] + 1) # Blurred vision

        post_n_raw = post_symptoms[0] + post_symptoms[5] + post_symptoms[6] + post_symptoms[7] + post_symptoms[8] + post_symptoms[14] + post_symptoms[15]
        post_o_raw = post_symptoms[0] + post_symptoms[1] + post_symptoms[2] + post_symptoms[3] + post_symptoms[4] + post_symptoms[8] + post_symptoms[10]
        post_d_raw = post_symptoms[4] + post_symptoms[7] + post_symptoms[9] + post_symptoms[10] + post_symptoms[11] + post_symptoms[12] + post_symptoms[13]

        post_n_score = post_n_raw * 9.54
        post_o_score = post_o_raw * 7.58
        post_d_score = post_d_raw * 13.92
        post_ts_score = (post_n_raw + post_o_raw + post_d_raw) * 3.74

        row_post_ssq = [
            now_utc, p_code, session_id, "PostSession_Summary", "POST-01",
            "KennedySSQ", "PostSession", "Completed",
            "-1", "-1", "-1", "-1", "-1", "-1", "-1"
        ] + [str(s) for s in post_symptoms] + [
            f"{post_n_score:.2f}", f"{post_o_score:.2f}", f"{post_d_score:.2f}", f"{post_ts_score:.2f}",
            "-1", "-1", "-1", "-1", "-1", "-1", "-1", "-1", "-1", "-1", "-1", "-1"
        ]
        lines.append(",".join(row_post_ssq))

    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(f"[Synthetic Generator] Generated synthetic human factors assessments at: {output_path.resolve()}")


def main():
    parser = argparse.ArgumentParser(description="Synthetic Test-Data Generator for Development Only")
    parser.add_argument("--outdir", "-o", type=Path, default=Path("telemetry"),
                        help="Output directory for synthetic fixtures")
    parser.add_argument("--participants", "-p", type=int, default=4,
                        help="Number of synthetic participant blocks to generate")
    parser.add_argument("--seed", "-s", type=int, default=42,
                        help="Random seed for deterministic generation")
    args = parser.parse_args()

    trial_path = args.outdir / "SYNTHETIC_TEST_reaction_latency_trial_log.csv"
    cont_path = args.outdir / "SYNTHETIC_TEST_continuous_telemetry_90hz.csv"
    hf_path = args.outdir / "SYNTHETIC_TEST_human_factors_assessments.csv"

    print("=" * 68)
    print("      IVRAR GROUP 02: SYNTHETIC TEST DATA GENERATOR")
    print("      Strictly for software verification & offline analysis testing.")
    print("=" * 68)

    generate_synthetic_trial_dataset(trial_path, num_participants=args.participants, seed=args.seed)
    generate_synthetic_continuous_telemetry(cont_path, num_samples=600, seed=args.seed)
    generate_synthetic_human_factors_dataset(hf_path, num_participants=args.participants, seed=args.seed)


if __name__ == "__main__":
    main()

