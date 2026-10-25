using NUnit.Framework;
using UnityEngine;
using IVRAR.Group02.Core;

namespace IVRAR.Group02.Tests
{
    public class SimulationLifecycleTests
    {
        [Test]
        public void SimulationManager_InitializesInIdleState()
        {
            var go = new GameObject("SimulationManagerTest");
            var manager = go.AddComponent<SimulationManager>();

            Assert.AreEqual(SimulationState.Idle, manager.CurrentState);

            Object.DestroyImmediate(go);
        }
    }
}
