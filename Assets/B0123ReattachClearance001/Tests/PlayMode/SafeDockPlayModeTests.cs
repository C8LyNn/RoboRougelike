using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using B01.DebrisPlayground001;

namespace B01.ReattachClearance001.Tests
{
    public sealed class SafeDockPlayModeTests
    {
        private GameObject host;
        private DebrisRigPlayground demo;
        private DebrisSafeReattachGate gate;
        private GameObject blocker;

        [SetUp]
        public void Prepare()
        {
            host = new GameObject("W08 isolated safe reattach test");
            demo = host.AddComponent<DebrisRigPlayground>();
            gate = host.AddComponent<DebrisSafeReattachGate>();
            demo.BuildDemo();
        }

        [TearDown]
        public void Finish()
        {
            if (blocker != null) Object.DestroyImmediate(blocker);
            if (host != null) Object.DestroyImmediate(host);
        }

        [UnityTest]
        public IEnumerator ActualObstacleBlocksDockWithoutConsumingDetach()
        {
            Assert.IsTrue(demo.Detach(0));
            blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blocker.transform.position = new Vector3(
                (float)(DebrisRigDemoCatalog.Light.RootX + DebrisRigDemoCatalog.Light.PartOffsetX), 1.2f, 0f);
            blocker.transform.localScale = Vector3.one * 0.7f;
            yield return new WaitForFixedUpdate();
            Assert.IsFalse(gate.TrySafeReattach(demo, 0));
            Assert.AreEqual(DockVerdict.Blocked, gate.LastVerdict);
            Assert.IsTrue(demo.IsDetached(0));
            Assert.AreEqual(1, demo.AppliedCount(0));
        }

        [UnityTest]
        public IEnumerator ObstacleRemovalAllowsSubsequentDock()
        {
            Assert.IsTrue(demo.Detach(0));
            blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blocker.transform.position = new Vector3(
                (float)(DebrisRigDemoCatalog.Light.RootX + DebrisRigDemoCatalog.Light.PartOffsetX), 1.2f, 0f);
            blocker.transform.localScale = Vector3.one * 0.7f;
            yield return new WaitForFixedUpdate();
            Assert.IsFalse(gate.TrySafeReattach(demo, 0));
            blocker.transform.position = new Vector3(0f, 40f, 0f);
            Physics.SyncTransforms();
            Assert.IsTrue(gate.TrySafeReattach(demo, 0));
            Assert.IsFalse(demo.IsDetached(0));
            Assert.AreEqual(0, demo.AppliedCount(0));
        }

        [Test]
        public void NotDetachedIsRejectedWithoutSideEffects()
        {
            Assert.IsFalse(gate.TrySafeReattach(demo, 1));
            Assert.AreEqual(DockVerdict.NotDetached, gate.LastVerdict);
            Assert.AreEqual(0, demo.AppliedCount(1));
        }

        [Test]
        public void ExcessiveTravelDistanceIsRejected()
        {
            Assert.IsTrue(demo.Detach(0));
            Rigidbody body = demo.PartBody(0);
            body.position = new Vector3(0f, 20f, 0f);
            Physics.SyncTransforms();
            Assert.IsFalse(gate.TrySafeReattach(demo, 0));
            Assert.AreEqual(DockVerdict.OutOfReach, gate.LastVerdict);
        }

        [Test]
        public void TriggerDoesNotBlockSafeReattach()
        {
            Assert.IsTrue(demo.Detach(1));
            blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blocker.transform.position = new Vector3(
                (float)(DebrisRigDemoCatalog.Heavy.RootX + DebrisRigDemoCatalog.Heavy.PartOffsetX), 1.2f, 0f);
            blocker.transform.localScale = Vector3.one * 0.5f;
            blocker.GetComponent<BoxCollider>().isTrigger = true;
            Physics.SyncTransforms();
            Assert.IsTrue(gate.TrySafeReattach(demo, 1));
            Assert.IsFalse(demo.IsDetached(1));
        }

        [Test]
        public void InvalidRigIndexFailsClosed()
        {
            Assert.IsFalse(gate.TrySafeReattach(demo, -1));
            Assert.AreEqual(DockVerdict.InvalidInput, gate.LastVerdict);
        }
    }
}
