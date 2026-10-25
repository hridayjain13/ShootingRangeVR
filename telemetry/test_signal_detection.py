import math
import sys
from pathlib import Path
import unittest
import pandas as pd

# Ensure local telemetry module resolution
sys.path.insert(0, str(Path(__file__).resolve().parent))
from analyze_signal_detection import compute_sdt_metrics, compute_reaction_time_stats, analyze_dataset


class TestSignalDetectionMetrics(unittest.TestCase):

    def test_standard_sdt_values(self):
        """
        Verify known theoretical SDT values:
        When Hit Rate = 0.8413447 (Z = +1.0) and FA Rate = 0.1586553 (Z = -1.0):
        d' should be exactly 2.0, beta should be 1.0, and c should be 0.0.
        """
        # 841 hits, 159 misses (84.1%), 159 FAs, 841 CRs (15.9%)
        sdt = compute_sdt_metrics(841, 159, 159, 841, correction_method="macmillan")

        self.assertAlmostEqual(sdt["d_prime"], 2.0, places=1)
        self.assertAlmostEqual(sdt["beta"], 1.0, places=1)
        self.assertAlmostEqual(sdt["criterion_c"], 0.0, places=1)

    def test_chance_discrimination(self):
        """
        When trainee cannot discriminate signal from noise (Hit Rate == FA Rate):
        d' must equal 0.0, and beta must equal 1.0.
        """
        sdt = compute_sdt_metrics(50, 50, 50, 50, correction_method="hautus")

        self.assertAlmostEqual(sdt["d_prime"], 0.0, places=4)
        self.assertAlmostEqual(sdt["beta"], 1.0, places=4)
        self.assertAlmostEqual(sdt["criterion_c"], 0.0, places=4)

    def test_extreme_rates_correction_hautus(self):
        """
        100% Hit Rate (0 misses) and 0% FA Rate (0 false alarms):
        Hautus log-linear correction must prevent infinite Z-scores and return finite d'.
        """
        sdt = compute_sdt_metrics(hits=20, misses=0, false_alarms=0, correct_rejections=20, correction_method="hautus")

        self.assertFalse(math.isinf(sdt["d_prime"]), "d' must not be infinite under Hautus correction.")
        self.assertFalse(math.isnan(sdt["d_prime"]), "d' must not be NaN.")
        self.assertGreater(sdt["d_prime"], 2.5, "Perfect discrimination with 20 trials should yield d' > 2.5.")
        self.assertFalse(math.isinf(sdt["beta"]))

    def test_extreme_rates_correction_macmillan(self):
        """
        Macmillan & Kaplan 1/(2N) correction must also yield finite, bounded d' and beta.
        """
        sdt = compute_sdt_metrics(hits=20, misses=0, false_alarms=0, correct_rejections=20, correction_method="macmillan")

        self.assertFalse(math.isinf(sdt["d_prime"]))
        self.assertFalse(math.isnan(sdt["d_prime"]))
        self.assertGreater(sdt["d_prime"], 3.0)

    def test_zero_trials_handled_gracefully(self):
        """
        Zero trials should return safe neutral defaults rather than ZeroDivisionError.
        """
        sdt = compute_sdt_metrics(0, 0, 0, 0)
        self.assertEqual(sdt["total_trials"], 0)
        self.assertEqual(sdt["d_prime"], 0.0)
        self.assertEqual(sdt["beta"], 1.0)


class TestReactionTimeStats(unittest.TestCase):

    def test_descriptive_latency_statistics(self):
        """
        Sample [400.0, 500.0, 600.0]:
        Mean = 500.0 ms, Median = 500.0 ms, Std Dev = 100.0 ms.
        """
        latencies = [400.0, 500.0, 600.0]
        stats = compute_reaction_time_stats(latencies)

        self.assertEqual(stats["count"], 3)
        self.assertAlmostEqual(stats["mean_ms"], 500.0)
        self.assertAlmostEqual(stats["median_ms"], 500.0)
        self.assertAlmostEqual(stats["std_dev_ms"], 100.0)
        self.assertAlmostEqual(stats["min_ms"], 400.0)
        self.assertAlmostEqual(stats["max_ms"], 600.0)

    def test_negative_and_nan_values_filtered(self):
        """
        Negative latencies or NaNs must be safely filtered out.
        """
        dirty_latencies = [-100.0, float("nan"), 450.0, 550.0]
        stats = compute_reaction_time_stats(dirty_latencies)

        self.assertEqual(stats["count"], 2)
        self.assertAlmostEqual(stats["mean_ms"], 500.0)


class TestDatasetAnalysis(unittest.TestCase):

    def test_full_dataframe_analysis(self):
        """
        Tests end-to-end processing of a trial dataset across two conditions.
        """
        data = {
            "ParticipantID": ["P01", "P01", "P01", "P01", "P01", "P01"],
            "Condition": ["Baseline", "Baseline", "Baseline", "SensoryStress", "SensoryStress", "SensoryStress"],
            "LatencyMs": [420.0, 480.0, 510.0, 580.0, 620.0, 650.0],
            "Hit": [1, 1, 0, 1, 0, 0],
            "Miss": [0, 0, 0, 0, 1, 0],
            "FalseAlarm": [0, 0, 0, 0, 0, 1],
            "CorrectRejection": [0, 0, 1, 0, 0, 0]
        }
        df = pd.DataFrame(data)
        results = analyze_dataset(df)

        self.assertEqual(results["total_trials"], 6)
        self.assertIn("Baseline", results["condition_comparisons"])
        self.assertIn("SensoryStress", results["condition_comparisons"])

        base_acc = results["condition_comparisons"]["Baseline"]["accuracy"]
        self.assertEqual(base_acc, 1.0, "All 3 baseline decisions were correct (2 Hits, 1 CR).")

        stress_acc = results["condition_comparisons"]["SensoryStress"]["accuracy"]
        self.assertAlmostEqual(stress_acc, 1.0 / 3.0, places=2)


if __name__ == "__main__":
    unittest.main()
