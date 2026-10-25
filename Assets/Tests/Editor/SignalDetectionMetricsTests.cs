using NUnit.Framework;
using IVRAR.Group02.Telemetry;

namespace IVRAR.Group02.Tests
{
    [TestFixture]
    public class SignalDetectionMetricsTests
    {
        [Test]
        public void Calculate_WithBalancedResponses_ReturnsValidDPrimeAndBeta()
        {
            // 20 hits, 5 misses, 3 false alarms, 22 correct rejections
            int hits = 20;
            int misses = 5;
            int falseAlarms = 3;
            int correctRejections = 22;

            var res = SignalDetectionMetrics.Calculate(hits, misses, falseAlarms, correctRejections);

            Assert.IsFalse(double.IsNaN(res.DPrime));
            Assert.IsFalse(double.IsNaN(res.Beta));
            Assert.Greater(res.DPrime, 1.0, "High sensitivity expected for low false alarm and high hit count");
            Assert.Greater(res.APrime, 0.5, "Non-parametric A' must be greater than chance (0.5)");
        }

        [Test]
        public void Calculate_WithZeroFalseAlarms_HautusCorrectionPreventsInfinity()
        {
            int hits = 10;
            int misses = 0;
            int falseAlarms = 0;
            int correctRejections = 10;

            var res = SignalDetectionMetrics.Calculate(hits, misses, falseAlarms, correctRejections);

            Assert.IsFalse(double.IsInfinity(res.DPrime));
            Assert.IsFalse(double.IsInfinity(res.Beta));
            Assert.Greater(res.DPrime, 0.0);
        }

        [Test]
        public void InverseNormalCDF_MatchesStandardZValues()
        {
            // Z(0.5) = 0.0
            double z05 = SignalDetectionMetrics.InverseNormalCDF(0.5);
            Assert.AreEqual(0.0, z05, 1e-4);

            // Z(0.8413) ~= 1.0
            double z84 = SignalDetectionMetrics.InverseNormalCDF(0.8413447);
            Assert.AreEqual(1.0, z84, 1e-3);

            // Z(0.1587) ~= -1.0
            double z15 = SignalDetectionMetrics.InverseNormalCDF(0.1586553);
            Assert.AreEqual(-1.0, z15, 1e-3);
        }
    }
}
