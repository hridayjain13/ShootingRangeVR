using NUnit.Framework;
using UnityEngine;
using IVRAR.Group02.Environment;
using IVRAR.Group02.Telemetry;

namespace IVRAR.Group02.Tests
{
    [TestFixture]
    public class LowVisibilityEnvironmentTests
    {
        private GameObject _testRoot;
        private LowVisibilityEnvironmentManager _manager;
        private Light _testLight;

        [SetUp]
        public void SetUp()
        {
            _testRoot = new GameObject("TestEnvironmentRoot");
            _testLight = _testRoot.AddComponent<Light>();
            _testLight.type = LightType.Directional;
            _testLight.intensity = 1.0f;
            _testLight.color = Color.white;

            _manager = _testRoot.AddComponent<LowVisibilityEnvironmentManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_testRoot != null)
            {
                Object.DestroyImmediate(_testRoot);
            }
        }

        [Test]
        public void EnvironmentManager_SimulatedLuxTargets_BetweenHalfAndFiveLux()
        {
            var profileA = _manager.GetProfileForCondition(LowVisibilityCondition.ConditionA_Baseline);
            var profileB = _manager.GetProfileForCondition(LowVisibilityCondition.ConditionB_ReducedNighttime);
            var profileC = _manager.GetProfileForCondition(LowVisibilityCondition.ConditionC_ReducedWithFog);

            Assert.GreaterOrEqual(profileA.simulatedTargetLux, 0.5f);
            Assert.LessOrEqual(profileA.simulatedTargetLux, 5.0f);

            Assert.GreaterOrEqual(profileB.simulatedTargetLux, 0.5f);
            Assert.LessOrEqual(profileB.simulatedTargetLux, 5.0f);

            Assert.GreaterOrEqual(profileC.simulatedTargetLux, 0.5f);
            Assert.LessOrEqual(profileC.simulatedTargetLux, 5.0f);

            Assert.AreEqual(5.0f, profileA.simulatedTargetLux, "Condition A baseline target should be 5.0 lux.");
            Assert.AreEqual(1.5f, profileB.simulatedTargetLux, "Condition B nighttime target should be 1.5 lux.");
            Assert.AreEqual(0.5f, profileC.simulatedTargetLux, "Condition C dense fog target should be 0.5 lux.");
        }

        [Test]
        public void EnvironmentManager_DoesNotClaimPhysicalCalibrationByDefault()
        {
            Assert.IsFalse(_manager.IsPhysicallyCalibrated, "Uncalibrated simulation must not claim physical calibration.");
            Assert.IsTrue(_manager.CalibrationStatus.ToLower().Contains("uncalibrated"));

            var profileA = _manager.GetProfileForCondition(LowVisibilityCondition.ConditionA_Baseline);
            var profileB = _manager.GetProfileForCondition(LowVisibilityCondition.ConditionB_ReducedNighttime);
            var profileC = _manager.GetProfileForCondition(LowVisibilityCondition.ConditionC_ReducedWithFog);

            Assert.IsFalse(profileA.isPhysicallyCalibrated);
            Assert.IsFalse(profileB.isPhysicallyCalibrated);
            Assert.IsFalse(profileC.isPhysicallyCalibrated);
        }

        [Test]
        public void EnvironmentManager_AppliesConditionA_Reproducibly()
        {
            _manager.ApplyCondition(LowVisibilityCondition.ConditionA_Baseline);

            Assert.AreEqual(LowVisibilityCondition.ConditionA_Baseline, _manager.ActiveCondition);
            Assert.AreEqual(5.0f, _manager.CurrentSimulatedTargetLux);
            Assert.IsFalse(RenderSettings.fog, "Condition A baseline must disable fog.");
            Assert.AreEqual(1.0f, _manager.CurrentTargetContrast);
        }

        [Test]
        public void EnvironmentManager_AppliesConditionB_Reproducibly()
        {
            _manager.ApplyCondition(LowVisibilityCondition.ConditionB_ReducedNighttime);

            Assert.AreEqual(LowVisibilityCondition.ConditionB_ReducedNighttime, _manager.ActiveCondition);
            Assert.AreEqual(1.5f, _manager.CurrentSimulatedTargetLux);
            Assert.IsTrue(RenderSettings.fog, "Condition B must enable fog.");
            Assert.AreEqual(0.018f, RenderSettings.fogDensity, 0.001f);
            Assert.AreEqual(0.65f, _manager.CurrentTargetContrast, 0.01f);
        }

        [Test]
        public void EnvironmentManager_AppliesConditionC_Reproducibly()
        {
            _manager.ApplyCondition(LowVisibilityCondition.ConditionC_ReducedWithFog);

            Assert.AreEqual(LowVisibilityCondition.ConditionC_ReducedWithFog, _manager.ActiveCondition);
            Assert.AreEqual(0.5f, _manager.CurrentSimulatedTargetLux);
            Assert.IsTrue(RenderSettings.fog, "Condition C must enable heavy fog.");
            Assert.AreEqual(0.065f, RenderSettings.fogDensity, 0.001f);
            Assert.AreEqual(0.35f, _manager.CurrentTargetContrast, 0.01f);
        }

        [Test]
        public void EnvironmentManager_ResetToBaseline_RestoresOriginalSettings()
        {
            _manager.CaptureBaselineEnvironment();

            // Switch to heavy fog Condition C
            _manager.ApplyCondition(LowVisibilityCondition.ConditionC_ReducedWithFog);
            Assert.IsTrue(RenderSettings.fog);

            // Execute reset to baseline
            _manager.ResetToBaseline();

            // Verify original state restored
            Assert.AreEqual(LowVisibilityCondition.ConditionA_Baseline, _manager.ActiveCondition);
        }

        [Test]
        public void EnvironmentManager_UpdatesTelemetryLogger()
        {
            var loggerGo = new GameObject("TestLogger");
            var logger = loggerGo.AddComponent<ReactionLatencyTelemetryLogger>();
            _manager.TelemetryLogger = logger;

            _manager.ApplyCondition(LowVisibilityCondition.ConditionC_ReducedWithFog);

            Assert.AreEqual(0.5f, logger.simulatedLuxTarget);
            Assert.AreEqual(0.065f, logger.fogDensity, 0.001f);
            Assert.AreEqual(0.35f, logger.targetContrast, 0.01f);

            Object.DestroyImmediate(loggerGo);
        }
    }
}
