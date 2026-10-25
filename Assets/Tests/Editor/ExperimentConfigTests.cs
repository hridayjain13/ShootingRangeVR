using NUnit.Framework;
using UnityEngine;
using IVRAR.Group02.Core;

namespace IVRAR.Group02.Tests
{
    [TestFixture]
    public class ExperimentConfigTests
    {
        [Test]
        public void ExperimentConfig_InstantiationAndDefaults_AreValid()
        {
            var config = ScriptableObject.CreateInstance<ExperimentConfig>();

            Assert.IsNotNull(config);
            Assert.AreEqual(ExperimentalCondition.LowVisibility_SensoryStress, config.condition);
            Assert.LessOrEqual(config.strobeFrequencyHz, 6.0f, "Strobe frequency must be <= 6.0 Hz for photic safety");
            Assert.LessOrEqual(config.maxStrobePulses, 6, "Strobe pulse count must be <= 6 for participant safety");
            Assert.GreaterOrEqual(config.exposureDurationSeconds, 0.5f);
            Assert.GreaterOrEqual(config.totalTrialsInBlock, 5);

            Object.DestroyImmediate(config);
        }
    }
}
