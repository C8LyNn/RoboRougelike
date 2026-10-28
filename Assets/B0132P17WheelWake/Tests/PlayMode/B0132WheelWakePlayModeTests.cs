using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RoboRougelike.B0132P17.Tests
{
    public sealed class B0132WheelWakePlayModeTests
    {
        [UnityTest]
        public IEnumerator ActiveCruise_CreatesTwoCosmeticEmitters_WithoutPhysics()
        {
            var root = new GameObject("WheelWake source scope test");
            var emitter = root.AddComponent<B0132WheelWakeEmitter>();
            emitter.SetVisualInput(WheelWakeMode.Cruise, 9f, .35f, true, true);
            Assert.AreEqual(2, emitter.SystemCount);
            Assert.IsTrue(emitter.IsEmitting);
            Assert.AreEqual(2, root.GetComponentsInChildren<ParticleSystem>().Length);
            Assert.AreEqual(0, root.GetComponentsInChildren<Rigidbody>(true).Length);
            Assert.AreEqual(0, root.GetComponentsInChildren<Collider>(true).Length);
            emitter.ClearAll();
            Assert.IsFalse(emitter.IsEmitting);
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ContactLost_ImmediatelyClearsBothParticleSystems()
        {
            var root = new GameObject("WheelWake grounded gate");
            var emitter = root.AddComponent<B0132WheelWakeEmitter>();
            emitter.SetVisualInput(WheelWakeMode.Drift, 11f, .8f, true, true);
            Assert.IsTrue(emitter.IsEmitting);
            emitter.SetVisualInput(WheelWakeMode.Drift, 11f, .8f, false, true);
            Assert.IsFalse(emitter.IsEmitting);
            Assert.AreEqual(0f, emitter.LastSample.RatePerSecond);
            foreach (var system in root.GetComponentsInChildren<ParticleSystem>(true))
                Assert.IsFalse(system.isPlaying);
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator OffAndDisable_DoNotResumeStaleEmission()
        {
            var root = new GameObject("WheelWake teardown gate");
            var emitter = root.AddComponent<B0132WheelWakeEmitter>();
            emitter.SetVisualInput(WheelWakeMode.Brake, 8f, .9f, true, true);
            Assert.IsTrue(emitter.IsEmitting);
            emitter.SetVisualInput(WheelWakeMode.Brake, 8f, .9f, true, false);
            Assert.IsFalse(emitter.IsEmitting);
            root.SetActive(false);
            emitter.SetVisualInput(WheelWakeMode.Cruise, 8f, .2f, true, true);
            Assert.IsFalse(emitter.IsEmitting);
            root.SetActive(true);
            Assert.IsFalse(emitter.IsEmitting);
            Object.Destroy(root);
            yield return null;
        }
    }
}
