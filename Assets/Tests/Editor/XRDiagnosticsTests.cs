using NUnit.Framework;
using UnityEngine;
using IVRAR.Group02.XR;

namespace IVRAR.Group02.Tests
{
    [TestFixture]
    public class XRDiagnosticsTests
    {
        [Test]
        public void Diagnostics_DoesNotReportUnsupportedHardwareAsVerified()
        {
            var go = new GameObject("TestDiagnostics");
            var diag = go.AddComponent<XRInitializationDiagnostics>();

            diag.PollXRHardware();

            // When running in headless editor without a physical HMD attached:
            if (!diag.HeadsetTrackingAvailable)
            {
                Assert.IsFalse(diag.IsHeadsetVerified, "An untracked/disconnected headset must never be reported as verified.");
                Assert.AreEqual(-1f, diag.ReportedRefreshRateHz, "Refresh rate must report -1 when unverified.");
                Assert.IsFalse(diag.IsRefreshRateVerified, "Refresh rate verification flag must be false when no hardware rate is queried.");
            }

            Object.DestroyImmediate(go);
        }

        [Test]
        public void Diagnostics_GeneratesFormattedSummaryString()
        {
            var go = new GameObject("TestDiagnostics");
            var diag = go.AddComponent<XRInitializationDiagnostics>();

            diag.PollXRHardware();
            string summary = diag.GetDiagnosticSummary();

            Assert.IsNotNull(summary);
            Assert.IsTrue(summary.Contains("XR DIAGNOSTICS"));
            Assert.IsTrue(summary.Contains("Loader:"));
            Assert.IsTrue(summary.Contains("Headset:"));

            Object.DestroyImmediate(go);
        }
    }
}
