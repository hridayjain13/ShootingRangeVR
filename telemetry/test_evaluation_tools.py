#!/usr/bin/env python3
"""
Unit Test Suite for Techno-Economic Evaluation & Research Analytics Tools
Validates:
1. Dimensionless techno-economic model formulas, break-even, and ROI calculations.
2. Sensitivity analyses (participant volume scaling and cost elasticity).
3. Telemetry and human-factors CSV parsing, column normalization, and missing-value handling.
4. Signal detection theory (SDT) metrics and reaction latency aggregations.
5. Invalid input rejection and boundary condition handling.
6. Publication dashboard export and visualization generation.
"""

from dataclasses import asdict
from pathlib import Path
import tempfile
import unittest

import numpy as np
import pandas as pd

import sys
sys.path.insert(0, str(Path(__file__).resolve().parent))

from sensory_stress_defense_roi import (
    TechnoEconomicAssumptions,
    validate_assumptions,
    compute_techno_economic_model,
    compute_volume_sensitivity,
    compute_cost_sensitivity,
    load_trial_telemetry,
    load_human_factors_assessments,
    compute_sdt_metrics_from_df,
    export_publication_dashboard
)


class TestTechnoEconomicEvaluationModel(unittest.TestCase):

    def setUp(self):
        # Deterministic reference assumptions
        self.assumptions = TechnoEconomicAssumptions(
            vr_hardware_investment=10000.0,
            vr_software_development_cost=10000.0,
            vr_hardware_replacement_rate=0.20,
            vr_maintenance_annual=2000.0,
            vr_operating_cost_per_session=20.0,
            vr_instructor_cost_per_session=40.0,
            facility_rental_per_session=200.0,
            consumables_per_participant_session=40.0,
            conventional_instructor_per_session=100.0,
            conventional_operating_cost_per_session=50.0,
            number_of_participants=50,
            number_of_sessions=100,
            participants_per_session=4
        )

    def test_cost_calculations_exact_values(self):
        """
        Verify analytical formulas for CapEx, OpEx, total costs, and substitution:
        - CapEx VR = 10,000 + 10,000 + (10,000 * 0.20) = 22,000
        - OpEx VR / session = 20 + 40 + (2,000 / 100) = 80
        - Total VR Cost = 22,000 + (80 * 100) = 30,000
        - OpEx Conv / session = 200 + (40 * 4) + 100 + 50 = 510
        - Total Conv Cost = 510 * 100 = 51,000
        - Cost Substitution = 51,000 - 30,000 = 21,000
        - Cost per Participant: Conv = 51,000 / 50 = 1,020; VR = 30,000 / 50 = 600
        """
        res = compute_techno_economic_model(self.assumptions)

        self.assertAlmostEqual(res["CapExVrTotal"], 22000.0, places=2)
        self.assertAlmostEqual(res["OpExVrPerSession"], 80.0, places=2)
        self.assertAlmostEqual(res["TotalVrCost"], 30000.0, places=2)

        self.assertAlmostEqual(res["OpExConvPerSession"], 510.0, places=2)
        self.assertAlmostEqual(res["TotalConventionalCost"], 51000.0, places=2)

        self.assertAlmostEqual(res["CostSubstitution"], 21000.0, places=2)
        self.assertAlmostEqual(res["CostPerParticipantConv"], 1020.0, places=2)
        self.assertAlmostEqual(res["CostPerParticipantVr"], 600.0, places=2)

    def test_breakeven_and_roi_formulas(self):
        """
        Verify Break-Even Session Count and ROI:
        - Marginal Savings / session = 510 - 80 = 430
        - N_s* = CapEx / marginal savings = 22,000 / 430 = 51.1628 sessions
        - ROI = (Cost Substitution / CapEx) * 100% = (21,000 / 22,000) * 100% = 95.45%
        """
        res = compute_techno_economic_model(self.assumptions)

        expected_breakeven = 22000.0 / 430.0
        self.assertTrue(res["IsBreakEvenAchievable"])
        self.assertAlmostEqual(res["BreakEvenSessionCount"], expected_breakeven, places=3)

        expected_roi = (21000.0 / 22000.0) * 100.0
        self.assertAlmostEqual(res["RoiPercent"], expected_roi, places=2)

    def test_dimensionless_operational_formulation(self):
        """
        Validate dimensionless parameters:
        - kappa = OpEx_vr / OpEx_conv = 80 / 510 = 0.15686
        - theta = CapEx_vr / OpEx_conv = 22,000 / 510 = 43.13725
        - eta* = theta / (1 - kappa) = 43.13725 / (1 - 0.15686) = 51.1628
        - Phi(N_s=100) = kappa + theta / 100 = 0.15686 + 0.43137 = 0.58823 (TotalVr / TotalConv)
        - Psi(N_s=100) = 1 - Phi = 0.41176
        """
        res = compute_techno_economic_model(self.assumptions)
        dim = res["Dimensionless"]

        expected_kappa = 80.0 / 510.0
        expected_theta = 22000.0 / 510.0
        expected_eta = expected_theta / (1.0 - expected_kappa)

        self.assertAlmostEqual(dim["OperatingCostRatio_Kappa"], expected_kappa, places=4)
        self.assertAlmostEqual(dim["CapitalIntensityRatio_Theta"], expected_theta, places=4)
        self.assertAlmostEqual(dim["DimensionlessBreakEven_Eta"], expected_eta, places=3)
        self.assertAlmostEqual(dim["DimensionlessBreakEven_Eta"], res["BreakEvenSessionCount"], places=3)

        expected_phi = 30000.0 / 51000.0
        self.assertAlmostEqual(dim["NormalizedCostRatio_Phi"], expected_phi, places=4)
        self.assertAlmostEqual(dim["NormalizedSavingsRatio_Psi"], 1.0 - expected_phi, places=4)

    def test_unachievable_breakeven_boundary(self):
        """
        When VR OpEx per session exceeds Conventional OpEx per session,
        cost substitution is negative and break-even is infinite / impossible.
        """
        assump = TechnoEconomicAssumptions(**asdict(self.assumptions))
        # Inflate VR operating cost so VR OpEx > Conv OpEx
        assump.vr_operating_cost_per_session = 600.0
        res = compute_techno_economic_model(assump)

        self.assertFalse(res["IsBreakEvenAchievable"])
        self.assertEqual(res["BreakEvenSessionCount"], float("inf"))
        self.assertGreater(res["Dimensionless"]["OperatingCostRatio_Kappa"], 1.0)
        self.assertEqual(res["Dimensionless"]["DimensionlessBreakEven_Eta"], float("inf"))

    def test_invalid_input_validation(self):
        """Negative monetary or scale values must raise ValueError."""
        # Negative participant count
        with self.assertRaises(ValueError):
            bad = TechnoEconomicAssumptions(**asdict(self.assumptions))
            bad.number_of_participants = -5
            validate_assumptions(bad)

        # Zero sessions
        with self.assertRaises(ValueError):
            bad = TechnoEconomicAssumptions(**asdict(self.assumptions))
            bad.number_of_sessions = 0
            validate_assumptions(bad)

        # Negative CapEx
        with self.assertRaises(ValueError):
            bad = TechnoEconomicAssumptions(**asdict(self.assumptions))
            bad.vr_hardware_investment = -100.0
            validate_assumptions(bad)

    def test_volume_and_cost_sensitivities(self):
        """Verify sensitivity sweeps produce monotonically consistent outputs."""
        vol_res = compute_volume_sensitivity(self.assumptions, min_part=10, max_part=100, step=10)
        self.assertEqual(len(vol_res["ParticipantVolumes"]), 10)
        # Unit cost of VR should drop as cohort volume increases due to CapEx amortization
        self.assertGreater(vol_res["VrUnitCosts"][0], vol_res["VrUnitCosts"][-1])

        cost_res = compute_cost_sensitivity(self.assumptions, [-0.20, 0.0, 0.20])
        self.assertEqual(len(cost_res["CapExSensitivity"]), 3)
        self.assertEqual(len(cost_res["OpExSensitivity"]), 3)
        # Higher CapEx must increase break-even session count
        self.assertLess(cost_res["CapExSensitivity"][0]["BreakEvenSessions"], cost_res["CapExSensitivity"][2]["BreakEvenSessions"])


class TestCSVTelemetryAndHumanFactorsParsing(unittest.TestCase):

    def test_trial_telemetry_loading_and_sdt(self):
        """
        Verify that trial telemetry CSV loading parses correctly and extracts SDT metrics.
        """
        trial_path = Path("telemetry/SYNTHETIC_TEST_reaction_latency_trial_log.csv")
        self.assertTrue(trial_path.exists(), "Synthetic trial fixture must exist.")

        df = load_trial_telemetry(trial_path)
        self.assertFalse(df.empty)
        self.assertIn("LatencyMs", df.columns)
        self.assertIn("Condition", df.columns)
        self.assertIn("TargetType", df.columns)
        self.assertIn("TraineeAction", df.columns)

        sdt = compute_sdt_metrics_from_df(df)
        self.assertGreater(len(sdt), 0)
        for cond, metrics in sdt.items():
            self.assertIn("DPrime", metrics)
            self.assertIn("FalseAlarmRate", metrics)
            self.assertIn("AccuracyPct", metrics)
            self.assertTrue(0.0 <= metrics["FalseAlarmRate"] <= 1.0)
            self.assertTrue(0.0 <= metrics["AccuracyPct"] <= 100.0)

    def test_human_factors_assessments_loading(self):
        """
        Verify human factors CSV parser accurately segments TLX, SSQ, and SAGAT records.
        """
        hf_path = Path("telemetry/SYNTHETIC_TEST_human_factors_assessments.csv")
        self.assertTrue(hf_path.exists(), "Synthetic human factors fixture must exist.")

        tables = load_human_factors_assessments(hf_path)
        self.assertIn("TLX", tables)
        self.assertIn("SSQ", tables)
        self.assertIn("SAGAT", tables)

        self.assertGreater(len(tables["TLX"]), 0, "Must contain NASA-TLX rows.")
        self.assertGreater(len(tables["SSQ"]), 0, "Must contain Kennedy SSQ rows.")
        self.assertGreater(len(tables["SAGAT"]), 0, "Must contain SAGAT rows.")

    def test_dashboard_publication_export(self):
        """
        Verify export_publication_dashboard creates the high-res 9-panel PNG graphic.
        """
        trial_df = load_trial_telemetry(Path("telemetry/SYNTHETIC_TEST_reaction_latency_trial_log.csv"))
        hf_dict = load_human_factors_assessments(Path("telemetry/SYNTHETIC_TEST_human_factors_assessments.csv"))
        model = compute_techno_economic_model(TechnoEconomicAssumptions())

        with tempfile.TemporaryDirectory() as tmp_dir:
            out_dir = Path(tmp_dir)
            fig_path = export_publication_dashboard(trial_df, hf_dict, model, out_dir)
            self.assertTrue(fig_path.exists())
            self.assertGreater(fig_path.stat().st_size, 50000, "Figure PNG size must be substantive.")


# Backwards-compatible legacy runner
def test_all():
    """Sprint 0 Diagnostic Entry Point"""
    print("=== Testing IVRAR Evaluation Toolchains ===")
    try:
        import sus_calculator
        sus_calculator.evaluate_cohort()
        print("[OK] SUS Calculator operational.")
    except Exception as e:
        print(f"[FAIL] SUS Calculator failed: {e}")
        return False

    try:
        import nasa_tlx_calculator
        nasa_tlx_calculator.evaluate_tlx_comparison()
        print("[OK] NASA-TLX Calculator operational.")
    except Exception as e:
        print(f"[FAIL] NASA-TLX Calculator failed: {e}")
        return False

    try:
        import kennedy_ssq_calculator
        kennedy_ssq_calculator.evaluate_ssq_safety()
        print("[OK] Kennedy SSQ Calculator operational.")
    except Exception as e:
        print(f"[FAIL] Kennedy SSQ Calculator failed: {e}")
        return False

    suite = unittest.defaultTestLoader.loadTestsFromTestCase(TestTechnoEconomicEvaluationModel)
    suite.addTests(unittest.defaultTestLoader.loadTestsFromTestCase(TestCSVTelemetryAndHumanFactorsParsing))
    runner = unittest.TextTestRunner(verbosity=2)
    res = runner.run(suite)
    return res.wasSuccessful()


if __name__ == "__main__":
    unittest.main()
