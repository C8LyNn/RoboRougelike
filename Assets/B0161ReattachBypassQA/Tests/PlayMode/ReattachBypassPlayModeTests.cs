// B01-61 W09 native regression SPEC for original W07+W08; authored but Unity NOT_RUN.
// Expected RED on frozen original: legacy public Reattach bypasses W08 clearance.
using System.Collections;
using B01.DebrisPlayground001;
using B01.ReattachClearance001;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace B01.ReattachBypassQA
{
    public sealed class ReattachBypassPlayModeTests
    {
        private GameObject host;
        private GameObject blocker;
        private DebrisRigPlayground demo;
        private DebrisSafeReattachGate gate;

        [SetUp]
        public void SetUp()
        {
            host = new GameObject("B01-61 isolated legacy dock bypass test");
            demo = host.AddComponent<DebrisRigPlayground>();
            gate = host.AddComponent<DebrisSafeReattachGate>();
            demo.BuildDemo();
        }

        [TearDown]
        public void TearDown()
        {
            if (blocker != null) Object.DestroyImmediate(blocker);
            if (host != null) Object.DestroyImmediate(host);
        }

        [UnityTest]
        public IEnumerator ExistingLegacyEntryPointMustNotBypassBlockedDock()
        {
            Assert.IsTrue(demo.Detach(0), "Original W07 detach must succeed");
            var position = new Vector3(
                (float)(DebrisRigDemoCatalog.Light.RootX + DebrisRigDemoCatalog.Light.PartOffsetX),
                1.2f, 0f);
            Rigidbody body = demo.PartBody(0);
            body.position = position + Vector3.forward * 0.5f;
            body.linearVelocity = Vector3.zero;

            blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blocker.transform.position = position;
            blocker.transform.localScale = Vector3.one * 0.8f;
            Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();

            Assert.IsFalse(gate.TrySafeReattach(demo, 0),
                "Actual W08 safe path must reject the physical obstruction");
            Assert.AreEqual(DockVerdict.Blocked, gate.LastVerdict);
            Assert.IsTrue(demo.IsDetached(0));
            Assert.AreEqual(1, demo.AppliedCount(0));

            // CURRENT ORIGINAL W07 IS EXPECTED TO FAIL HERE:
            // public method bypasses gate and reattaches the same obstructed part.
            Assert.IsFalse(demo.Reattach(0),
                "SECURITY RED: original public W07 Reattach bypasses W08 physical gate");
            Assert.IsTrue(demo.IsDetached(0),
                "Blocked reattach must not change original stable part state");
            Assert.AreEqual(1, demo.AppliedCount(0),
                "Blocked reattach must not rearm consumed part identity");
        }
    }
}
