using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace B01.SecureReattachV2.Tests
{
    public sealed class SecureReattachV2PlayModeTests
    {
        private GameObject host;
        private SecureDebrisPlaygroundV2 demo;
        private GameObject obstruction;

        [SetUp]
        public void Prepare()
        {
            host=new GameObject("B01-23 SAFE V2 independent scene test");
            demo=host.AddComponent<SecureDebrisPlaygroundV2>();
            demo.BuildDemo();
        }
        [TearDown]
        public void Finish()
        {
            if(obstruction!=null)Object.DestroyImmediate(obstruction);
            if(host!=null)Object.DestroyImmediate(host);
        }
        private void Block(int index,int layer)
        {
            SecureRigSpec rig=SecureRigCatalog.At(index);
            obstruction=GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstruction.transform.position=new Vector3((float)(rig.RootX+rig.PartOffsetX),1.2f,0);
            obstruction.transform.localScale=Vector3.one*.7f;
            obstruction.layer=layer;
            Physics.SyncTransforms();
        }
        [UnityTest]
        public IEnumerator OriginalNamedPublicReattachCannotBypassPhysics()
        {
            Assert.IsTrue(demo.Detach(0));
            Block(0,8);
            yield return new WaitForFixedUpdate();
            Assert.IsFalse(demo.Reattach(0));
            Assert.AreEqual(DockResult.Obstructed,demo.LastDockResult);
            Assert.IsTrue(demo.IsDetached(0));
            Assert.AreEqual(1,demo.AppliedCount(0));
        }
        [Test]
        public void AllPublicAliasesRejectBlockedOrInvalidRequest()
        {
            Assert.IsTrue(demo.Detach(1));
            Block(1,8);
            Assert.IsFalse(demo.Reattach(1));
            Assert.IsFalse(demo.TrySafeReattach(1));
            Assert.IsFalse(demo.ReattachFromGui(1));
            Assert.IsTrue(demo.IsDetached(1));
            Assert.AreEqual(1,demo.AppliedCount(1));
        }
        [UnityTest]
        public IEnumerator PhysicalBlockerRemovedThenAllRoutesCanUseGuard()
        {
            Assert.IsTrue(demo.Detach(0));
            Block(0,8);
            yield return new WaitForFixedUpdate();
            Assert.IsFalse(demo.Reattach(0));
            obstruction.transform.position=new Vector3(0,35,0);
            Physics.SyncTransforms();
            Assert.IsTrue(demo.Reattach(0));
            Assert.IsFalse(demo.IsDetached(0));
            Assert.AreEqual(0,demo.AppliedCount(0));
        }

        [Test]
        public void LostConsumedIdRejectsEveryPublicAliasWithoutPhysicalMutation()
        {
            Assert.IsTrue(demo.Detach(0));
            Rigidbody body=demo.PartBody(0);
            Vector3 velocity=body.linearVelocity;
            bool kinematic=body.isKinematic;
            Transform parent=body.transform.parent;
            Vector3 worldPosition=body.transform.position;
            // Test-only reflection: the R2 ledger is private, not a public UI API.
            var rigsField=typeof(SecureDebrisPlaygroundV2).GetField("rigs",
                System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(rigsField);
            var rigs=(System.Array)rigsField.GetValue(demo);
            object rig=rigs.GetValue(0);
            var ledgerField=rig.GetType().GetField("Ledger");
            var ledger=(AtomicDockLedger)ledgerField.GetValue(rig);
            Assert.AreEqual(1,ledger.Count);
            ledger.ClearForNewRun(); // reproduce #486 out-of-sequence invalidation
            Assert.AreEqual(0,ledger.Count);
            Assert.IsFalse(demo.Reattach(0));
            Assert.IsFalse(demo.TrySafeReattach(0));
            Assert.IsFalse(demo.ReattachFromGui(0));
            Assert.IsTrue(demo.IsDetached(0));
            Assert.AreEqual(0,demo.AppliedCount(0));
            Assert.AreEqual(kinematic,body.isKinematic);
            Assert.AreEqual(parent,body.transform.parent);
            Assert.AreEqual(worldPosition,body.transform.position);
            Assert.AreEqual(velocity,body.linearVelocity);
        }

        [Test]
        public void NotDetachedOrBadIndexDoesNotMoveParts()
        {
            Assert.IsFalse(demo.Reattach(1));
            Assert.AreEqual(DockResult.NotDetached,demo.LastDockResult);
            Assert.IsFalse(demo.Reattach(-1));
            Assert.IsFalse(demo.Reattach(2));
            Assert.IsFalse(demo.IsDetached(0));
        }
        [Test]
        public void RemotePartCannotUseAnyDockEntryPoint()
        {
            Assert.IsTrue(demo.Detach(1));
            demo.PartBody(1).position=new Vector3(0,30,0);
            Physics.SyncTransforms();
            Assert.IsFalse(demo.Reattach(1));
            Assert.IsFalse(demo.TrySafeReattach(1));
            Assert.IsFalse(demo.ReattachFromGui(1));
            Assert.IsTrue(demo.IsDetached(1));
        }
        [Test]
        public void TriggerColliderIsNotAnObstacle()
        {
            Assert.IsTrue(demo.Detach(0));
            Block(0,8);
            obstruction.GetComponent<BoxCollider>().isTrigger=true;
            Physics.SyncTransforms();
            Assert.IsTrue(demo.ReattachFromGui(0));
            Assert.IsFalse(demo.IsDetached(0));
        }
        [Test]
        public void AllLayersRejectDefaultAndHighLayerBlockers()
        {
            Assert.IsTrue(demo.Detach(0));
            Block(0,31);
            Assert.IsFalse(demo.Reattach(0));
            Assert.AreEqual(DockResult.Obstructed,demo.LastDockResult);
        }
    }
}
