using NUnit.Framework;
using UnityEngine;
using IVRAR.Group02.SensoryStress;

namespace IVRAR.Group02.Tests
{
    [TestFixture]
    public class SensoryStressDistractionTests
    {
        private GameObject _testRoot;
        private SensoryStressTargetManager _distractionManager;

        [SetUp]
        public void SetUp()
        {
            _testRoot = new GameObject("TestDistractionRoot");
            _distractionManager = _testRoot.AddComponent<SensoryStressTargetManager>();
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
        public void DistractionManager_SupportsAllFourConditions()
        {
            _distractionManager.SetCondition(SensoryDistractionCondition.Condition1_NoDistractionBaseline);
            Assert.AreEqual(SensoryDistractionCondition.Condition1_NoDistractionBaseline, _distractionManager.ActiveCondition);

            _distractionManager.SetCondition(SensoryDistractionCondition.Condition2_ControlledAuditory);
            Assert.AreEqual(SensoryDistractionCondition.Condition2_ControlledAuditory, _distractionManager.ActiveCondition);

            _distractionManager.SetCondition(SensoryDistractionCondition.Condition3_ControlledPeripheralVisual);
            Assert.AreEqual(SensoryDistractionCondition.Condition3_ControlledPeripheralVisual, _distractionManager.ActiveCondition);

            _distractionManager.SetCondition(SensoryDistractionCondition.Condition4_CombinedAuditoryVisual);
            Assert.AreEqual(SensoryDistractionCondition.Condition4_CombinedAuditoryVisual, _distractionManager.ActiveCondition);
        }

        [Test]
        public void DistractionManager_AudioGain_DefaultsConservativelyBelowSafetyCeiling()
        {
            // Must not default to extreme 85-95 dB digital equivalent
            Assert.LessOrEqual(_distractionManager.AudioGain, 0.25f, "Default digital gain must be conservative (<= 0.25 / -12 dBFS).");
        }

        [Test]
        public void DistractionManager_PhotosensitivitySafety_DefaultModeIsGradualGlare()
        {
            Assert.AreEqual(VisualDistractionMode.GradualPeripheralGlare, _distractionManager.VisualMode,
                "Photosensitivity safety protocol requires GradualPeripheralGlare as preferred default.");
        }

        [Test]
        public void DistractionManager_ResearcherSafetyOverride_DisablesImmediately()
        {
            Assert.IsFalse(_distractionManager.IsSafetyOverridden);

            _distractionManager.TriggerResearcherSafetyOverride();

            Assert.IsTrue(_distractionManager.IsSafetyOverridden, "Safety override flag must be true after trigger.");

            // Resetting
            _distractionManager.ResetSafetyOverride();
            Assert.IsFalse(_distractionManager.IsSafetyOverridden);
        }

        [Test]
        public void DistractionManager_ParticipantComfortPause_HaltsDistraction()
        {
            _distractionManager.PauseDistraction();
            Assert.IsTrue(_distractionManager.IsPaused);

            _distractionManager.ResumeDistraction();
            Assert.IsFalse(_distractionManager.IsPaused);
        }
    }
}
