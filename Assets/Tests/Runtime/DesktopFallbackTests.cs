using NUnit.Framework;
using UnityEngine;
using IVRAR.Group02.XR;

namespace IVRAR.Group02.Tests
{
    public class DesktopFallbackTests
    {
        [Test]
        public void DesktopFallback_ToggleMode_SwitchesActiveState()
        {
            var go = new GameObject("TestDesktopFallback");
            var fallback = go.AddComponent<DesktopFallbackController>();

            fallback.SetDesktopMode(true);
            Assert.IsTrue(fallback.IsDesktopModeActive);

            fallback.SetDesktopMode(false);
            Assert.IsFalse(fallback.IsDesktopModeActive);

            Object.DestroyImmediate(go);
        }
    }
}
