using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using B01.DebrisPlayground001;
using B01.ReattachClearance001;
using B01.DockHardening001;

namespace B01.CodeXRegressionQA.PublicReattach
{
    /// <summary>
    /// Independent W10 receiver: an external component directly calls frozen W07 public API
    /// even after the new W09 protected demo is armed. RED expected on #471 source.
    /// This test is a REAL Unity 6 PlayMode fixture, but requires licensed Unity to RUN.
    /// </summary>
    public sealed class PublicReattachApiPlayModeTests
    {
        private GameObject host;
        private DebrisRigPlayground legacy;
        private DebrisSafeReattachGate v08;
        private HardenedDockDemo v09;
        private GameObject blocker;

        [SetUp]
        public void CreateRealDemo()
        {
            host = new GameObject("B01-61 W10 external API bypass test only");
            legacy = host.AddComponent<DebrisRigPlayground>();
            v08 = host.AddComponent<DebrisSafeReattachGate>();
            v09 = host.AddComponent<HardenedDockDemo>();
            Assert.IsTrue(v09.Arm(), "W09 hardening must be active");
            Assert.IsFalse(legacy.enabled, "Legacy GUI should have been disabled");
            Assert.IsFalse(v08.enabled, "Old W08 GUI should have been disabled");
        }

        [TearDown]
        public void Cleanup()
        {
            if (blocker != null) Object.DestroyImmediate(blocker);
            if (host != null) Object.DestroyImmediate(host);
        }

        private void PutSolidBlockerAtMount(int index)
        {
            RigDemoDefinition spec = DebrisRigDemoCatalog.ByIndex(index);
            blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blocker.name = "W10 physical dock blocker";
            blocker.transform.position = new Vector3(
                (float)(spec.RootX + spec.PartOffsetX), 1.2f, 0f);
            blocker.transform.localScale = Vector3.one * 0.7f;
            blocker.layer = 8; // Non-default layer to test actual all-layer W09 query.
            Physics.SyncTransforms();
        }

        [UnityTest]
        public IEnumerator ExternalPublicApiCannotDockThroughPhysicalBlocker()
        {
            const int rig = 0;
            Assert.IsTrue(legacy.Detach(rig));
            Rigidbody body = legacy.PartBody(rig);
            PutSolidBlockerAtMount(rig);
            yield return new WaitForFixedUpdate();

            Assert.IsFalse(v09.TryDock(rig), "W09 protected API should refuse blocker");
            Assert.IsTrue(legacy.IsDetached(rig));
            Assert.AreEqual(1, legacy.AppliedCount(rig));

            // Deliberately invoke original W07 API as a different external component,
            // not through W07 GUI or W08/W09 buttons. Currently #471 returns TRUE.
            bool accepted = legacy.Reattach(rig);
            Assert.IsFalse(accepted, "P0 SOURCE RED: legacy public API bypasses active W09 safety gate");
            Assert.IsTrue(legacy.IsDetached(rig), "Rejected dock must preserve detached state");
            Assert.IsFalse(body.isKinematic, "Rejected dock must not freeze Rigidbody");
            Assert.AreEqual(1, legacy.AppliedCount(rig), "Rejected dock must preserve consumed ID");
        }

        [UnityTest]
        public IEnumerator ExternalPublicApiCannotTeleportRemoteHeavyRig()
        {
            const int rig = 1;
            Assert.IsTrue(legacy.Detach(rig));
            Rigidbody body = legacy.PartBody(rig);
            body.position = new Vector3(0f, 40f, 0f);
            Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();

            Assert.IsFalse(v09.TryDock(rig), "W09 must reject far-away body");
            Vector3 before = body.transform.position;
            bool accepted = legacy.Reattach(rig);
            Assert.IsFalse(accepted, "P0 SOURCE RED: remote original public Reattach bypasses reach validation");
            Assert.IsTrue(legacy.IsDetached(rig));
            Assert.That(Vector3.Distance(before, body.transform.position), Is.LessThan(0.001f));
            Assert.AreEqual(1, legacy.AppliedCount(rig));
        }
    }
}
