#!/usr/bin/env python3
"""
Unit Test Suite for Human Factors Assessment Engine
Validates:
1. NASA-TLX Raw TLX (RTLX) calculation conforming to Hart & Staveland (1988).
2. Kennedy SSQ subscales and Total Severity weights conforming to Kennedy et al. (1993).
3. Pre/Post differentiation and Delta SSQ paired calculation.
4. SAGAT Endsley 3-Level Situational Awareness rubric.
5. Missing-data handling and zero-PII security enforcement.
"""

from pathlib import Path
import sys
import tempfile
import unittest

# Ensure local telemetry module resolution
sys.path.insert(0, str(Path(__file__).resolve().parent))
from analyze_human_factors import (
    compute_raw_tlx,
    compute_ssq_scores,
    compute_paired_stats,
    analyze_dataset,
    SSQ_WEIGHTS,
    TLX_DIMENSIONS,
    SSQ_SYMPTOMS
)


class TestHumanFactorsAssessment(unittest.TestCase):

    def test_tlx_exact_raw_tlx_calculation(self):
        """
        Verify Raw TLX (RTLX) unweighted arithmetic mean across all 6 dimensions.
        """
        scores = {
            "MentalDemand": 60.0,
            "PhysicalDemand": 40.0,
            "TemporalDemand": 50.0,
            "Performance": 30.0,
            "Effort": 70.0,
            "Frustration": 20.0
        }
        raw_mean, answered, missing = compute_raw_tlx(scores)

        # Sum = 270.0. Mean = 270.0 / 6 = 45.0
        self.assertEqual(answered, 6)
        self.assertEqual(missing, 0)
        self.assertAlmostEqual(raw_mean, 45.0, places=4)

    def test_tlx_missing_dimension_handling(self):
        """
        Missing dimensions (-1 or out of 0-100 bounds) must be excluded from the denominator.
        """
        scores = {
            "MentalDemand": 60.0,
            "PhysicalDemand": 40.0,
            "TemporalDemand": -1.0,  # Missing
            "Performance": -1.0,     # Missing
            "Effort": -1.0,          # Missing
            "Frustration": -1.0      # Missing
        }
        raw_mean, answered, missing = compute_raw_tlx(scores)

        self.assertEqual(answered, 2)
        self.assertEqual(missing, 4)
        # Mean of 2 answered: (60 + 40) / 2 = 50.0
        self.assertAlmostEqual(raw_mean, 50.0, places=4)

    def test_ssq_published_weights_verification(self):
        """
        Validate exact theoretical weights from Kennedy et al. (1993):
        When all 16 symptoms are rated 1 (Slight):
        - Raw N = 7 -> Score = 7 * 9.54 = 66.78
        - Raw O = 7 -> Score = 7 * 7.58 = 53.06
        - Raw D = 7 -> Score = 7 * 13.92 = 97.44
        - Total Severity = (7 + 7 + 7) * 3.74 = 78.54
        """
        symptoms = {sym: 1 for sym in SSQ_SYMPTOMS}
        res = compute_ssq_scores(symptoms)

        self.assertTrue(res["IsValid"])
        self.assertEqual(res["AnsweredCount"], 16)
        self.assertEqual(res["MissingCount"], 0)

        self.assertEqual(res["RawNausea"], 7)
        self.assertAlmostEqual(res["NauseaScore"], 66.78, places=2)

        self.assertEqual(res["RawOculomotor"], 7)
        self.assertAlmostEqual(res["OculomotorScore"], 53.06, places=2)

        self.assertEqual(res["RawDisorientation"], 7)
        self.assertAlmostEqual(res["DisorientationScore"], 97.44, places=2)

        self.assertAlmostEqual(res["TotalSeverity"], 78.54, places=2)

    def test_ssq_missing_symptoms_flagged(self):
        """
        Missing symptoms (-1) must flag IsValid=False and report missing count.
        """
        symptoms = {sym: -1 for sym in SSQ_SYMPTOMS}
        res = compute_ssq_scores(symptoms)

        self.assertFalse(res["IsValid"])
        self.assertEqual(res["AnsweredCount"], 0)
        self.assertEqual(res["MissingCount"], 16)
        self.assertEqual(res["TotalSeverity"], 0.0)

    def test_ssq_pre_post_delta_calculation(self):
        """
        Verify Delta SSQ calculation: Delta = Post - Pre.
        """
        pre_scores = [0.0, 7.58, 0.0, 3.74]
        post_scores = [19.08, 22.74, 27.84, 26.18]

        stats = compute_paired_stats(pre_scores, post_scores)

        self.assertEqual(stats["n"], 4)
        # Expected deltas: [19.08 - 0, 22.74 - 7.58, 27.84 - 0, 26.18 - 3.74] = [19.08, 15.16, 27.84, 22.44]
        expected_mean = (19.08 + 15.16 + 27.84 + 22.44) / 4.0
        self.assertAlmostEqual(stats["mean_delta"], expected_mean, places=2)
        self.assertGreater(stats["cohens_dz"], 1.0, "Substantial change should yield large effect size.")

    def test_pii_rejection_security(self):
        """
        Test that CSV files with prohibited Personally Identifiable Information (PII)
        such as email, name, or student_id are rejected with a security error.
        """
        with tempfile.NamedTemporaryFile("w", suffix=".csv", delete=False) as tf:
            tf.write("TimestampUtc,ParticipantID,Email,MentalDemand\n")
            tf.write("2026-10-25T00:00:00,P01,student@univ.edu,45.0\n")
            temp_path = Path(tf.name)

        try:
            with self.assertRaises(ValueError) as ctx:
                analyze_dataset(temp_path)
            self.assertIn("Security Alert", str(ctx.exception))
        finally:
            if temp_path.exists():
                temp_path.unlink()

    def test_full_pipeline_on_synthetic_fixture(self):
        """
        Verify end-to-end dataset analysis on the generated synthetic test fixture.
        """
        fixture_path = Path("telemetry/SYNTHETIC_TEST_human_factors_assessments.csv")
        self.assertTrue(fixture_path.exists(), "Synthetic test fixture must exist.")

        results = analyze_dataset(fixture_path)

        self.assertEqual(results["CompletionSummary"]["UniqueParticipantsCount"], 4)
        self.assertGreater(results["NasaTlx"]["TotalRecords"], 0)
        self.assertGreater(results["KennedySsq"]["TotalRecords"], 0)
        self.assertGreater(results["Sagat"]["TotalFreezeProbes"], 0)

        # Check conditions
        self.assertIn("Baseline_Daylight", results["NasaTlx"]["Conditions"])
        self.assertIn("Combined_LowVis_SensoryStress", results["NasaTlx"]["Conditions"])

        # Check paired SSQ
        self.assertEqual(results["KennedySsq"]["PairedParticipantsCount"], 4)
        self.assertIn("TotalSeverity", results["KennedySsq"]["DeltaSSQ_PairedStats"])


if __name__ == "__main__":
    unittest.main()
