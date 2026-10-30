using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using B01.DebrisPlayground001;
using B01.ReattachClearance001;

namespace B01.DockHardening001.Tests
{
    public sealed class W09ProtectedDockPlayModeTests
    {
        private GameObject host;
        private DebrisRigPlayground demo;
        private DebrisSafeReattachGate oldGate;
        private HardenedDockDemo gate;
        private GameObject blocker;

        [SetUp]
        public void Setup()
        {
            host=new GameObject("W09 isolated hardened test");
            demo=host.AddComponent<DebrisRigPlayground>();
            oldGate=host.AddComponent<DebrisSafeReattachGate>();
            gate=host.AddComponent<HardenedDockDemo>();
            Assert.IsTrue(gate.Arm());
        }
        [TearDown]
        public void Cleanup()
        {
            if (blocker!=null) Object.DestroyImmediate(blocker);
            if (host!=null) Object.DestroyImmediate(host);
        }
        private void MakeBlocker(int index,int layer)
        {
            RigDemoDefinition spec=DebrisRigDemoCatalog.ByIndex(index);
            blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);
            blocker.transform.position=new Vector3((float)(spec.RootX+spec.PartOffsetX),1.2f,0);
            blocker.transform.localScale=Vector3.one*.7f;
            blocker.layer=layer;
            Physics.SyncTransforms();
        }
        [Test]
        public void OriginalBypassInterfacesDisabledOnArming()
        {
            Assert.IsTrue(gate.Armed);
            Assert.IsFalse(demo.enabled);
            Assert.IsFalse(oldGate.enabled);
        }
        [Test]
        public void NonDetachedPartNeverDocks()
        {
            Assert.IsFalse(gate.TryDock(0));
            Assert.IsTrue(demo.PartBody(0).isKinematic);
        }
        [UnityTest]
        public IEnumerator AllLayerAuditRejectsBlockerHiddenFromW08NarrowedMask()
        {
            Assert.IsTrue(demo.Detach(0));
            // Simulate exactly the W08 inspector mask omission: old gate sees no layer 8.
            FieldInfo field=typeof(DebrisSafeReattachGate)
                .GetField("collisionLayers",BindingFlags.Instance|BindingFlags.NonPublic);
            Assert.IsNotNull(field);
            field.SetValue(oldGate,(LayerMask)0);
            MakeBlocker(0,8);
            yield return new WaitForFixedUpdate();
            Assert.IsFalse(gate.TryDock(0));
            Assert.AreEqual(HardenedDockVerdict.NewAuditRejected,gate.LastVerdict);
            Assert.IsTrue(demo.IsDetached(0));
            Assert.AreEqual(1,demo.AppliedCount(0));
        }
        [UnityTest]
        public IEnumerator MovingBlockerAwayPermitsActualOriginalReattach()
        {
            Assert.IsTrue(demo.Detach(0));
            MakeBlocker(0,8);
            yield return new WaitForFixedUpdate();
            Assert.IsFalse(gate.TryDock(0));
            blocker.transform.position=new Vector3(0,30,0);
            Physics.SyncTransforms();
            Assert.IsTrue(gate.TryDock(0));
            Assert.IsFalse(demo.IsDetached(0));
            Assert.AreEqual(0,demo.AppliedCount(0));
        }
        [Test]
        public void RestoredUnsafeGuiIsRejectedRatherThanDocking()
        {
            Assert.IsTrue(demo.Detach(1));
            demo.enabled=true;
            Assert.IsFalse(gate.TryDock(1));
            Assert.AreEqual(HardenedDockVerdict.UnsafeLegacyUi,gate.LastVerdict);
            Assert.IsTrue(demo.IsDetached(1));
        }
        [Test]
        public void RemotePartRemainsDetachedAndLedgerUntouched()
        {
            Assert.IsTrue(demo.Detach(0));
            demo.PartBody(0).position=new Vector3(0,40,0);
            Assert.IsFalse(gate.TryDock(0));
            Assert.IsTrue(demo.IsDetached(0));
            Assert.AreEqual(1,demo.AppliedCount(0));
        }
        [Test]
        public void BadIndexCannotModifyEitherRig()
        {
            Assert.IsFalse(gate.TryDock(-1));
            Assert.IsFalse(gate.TryDock(2));
            Assert.IsFalse(demo.IsDetached(0));
            Assert.IsFalse(demo.IsDetached(1));
        }
    }
}
