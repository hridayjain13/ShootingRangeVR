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

    print("=" * 68)
    print("      IVRAR GROUP 02: SYNTHETIC TEST DATA GENERATOR")
    print("      Strictly for software verification & offline analysis testing.")
    print("=" * 68)

    generate_synthetic_trial_dataset(trial_path, num_participants=args.participants, seed=args.seed)
    generate_synthetic_continuous_telemetry(cont_path, num_samples=600, seed=args.seed)


if __name__ == "__main__":
    main()
