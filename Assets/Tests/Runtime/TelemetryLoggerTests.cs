using NUnit.Framework;
using UnityEngine;
using IVRAR.Group02.Telemetry;

namespace IVRAR.Group02.Tests
{
    public class TelemetryLoggerTests
    {
        [Test]
        public void TelemetryLogger_RecordsHitsAndComputesCountsCorrectly()
        {
            var go = new GameObject("TestLogger");
            var logger = go.AddComponent<ReactionLatencyTelemetryLogger>();

            logger.RecordEngagementEvent("TRIAL-0001", isHostile: true, engaged: true, latencyMs: 420.5f, distance: 10f, angle: 5f);
            logger.RecordEngagementEvent("TRIAL-0002", isHostile: false, engaged: false, latencyMs: 2500f, distance: 12f, angle: -8f);
            logger.RecordEngagementEvent("TRIAL-0003", isHostile: false, engaged: true, latencyMs: 380f, distance: 8f, angle: 0f);
            logger.RecordEngagementEvent("TRIAL-0004", isHostile: true, engaged: false, latencyMs: 2500f, distance: 15f, angle: 12f);

            Assert.AreEqual(1, logger.TotalHits);
            Assert.AreEqual(1, logger.TotalCorrectRejections);
            Assert.AreEqual(1, logger.TotalFalseAlarms);
            Assert.AreEqual(1, logger.TotalMisses);

            Object.DestroyImmediate(go);
        }
    }
}
