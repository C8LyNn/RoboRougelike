using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using B0125.SweepOrb.R2;

namespace B0125.SweepOrb.R2.Tests
{
    public sealed class SweepOrbUnityPlayModeTests
    {
        [UnityTest]
        public IEnumerator CreatesActualColliderAndRigidbody()
        {
            var root = new GameObject("isolated-native-real-physics");
            var demo = root.AddComponent<SweepOrbUnityDemo>();
            yield return new WaitForFixedUpdate();
            Assert.NotNull(demo.ActiveRigidbody);
            Assert.IsTrue(demo.ActiveRigidbody.GetComponent<CapsuleCollider>().enabled);
            Assert.IsFalse(demo.ActiveRigidbody.isKinematic);
            Assert.AreEqual(1.5f, demo.ActiveRigidbody.mass, 0.01f);
            Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator TwoPhysicalEnvelopeAndMassClasses()
        {
            var root = new GameObject("isolation-light-heavy");
            var demo = root.AddComponent<SweepOrbUnityDemo>();
            yield return new WaitForFixedUpdate();
            float lightWidth = demo.ActiveRigidbody.GetComponent<Collider>().bounds.size.x;
            demo.SetVehicle(true);
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(12f, demo.ActiveRigidbody.mass, 0.01f);
            Assert.Greater(demo.ActiveRigidbody.GetComponent<Collider>().bounds.size.x, lightWidth);
            demo.SetVehicle(false);
            Assert.AreEqual(1.5f, demo.ActiveRigidbody.mass, 0.01f);
            Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator RealFixedUpdatesApplyOnlyOneDamageGrantPerSweep()
        {
            var root = new GameObject("isolation-real-fixed-clock");
            var demo = root.AddComponent<SweepOrbUnityDemo>();
            yield return new WaitForFixedUpdate();
            for (int i = 0; i < 65; i++) yield return new WaitForFixedUpdate();
            Assert.Greater(demo.Elapsed, 1.0);
            Assert.AreEqual(1, demo.Hits);
            Assert.AreEqual(90, demo.Health);
            Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator PauseAndRestartFreezeAndResetRealClock()
        {
            var root = new GameObject("isolation-pause");
            var demo = root.AddComponent<SweepOrbUnityDemo>();
            yield return new WaitForFixedUpdate();
            demo.SetPaused(true);
            double pausedAt = demo.Elapsed;
            for (int i = 0; i < 8; i++) yield return new WaitForFixedUpdate();
            Assert.AreEqual(pausedAt, demo.Elapsed, 1e-9);
            demo.SetPaused(false);
            yield return new WaitForFixedUpdate();
            Assert.Greater(demo.Elapsed, pausedAt);
            demo.Restart();
            Assert.AreEqual(0, demo.Elapsed, 1e-9);
            Assert.AreEqual(100, demo.Health);
            Object.Destroy(root);
        }
    }
}
