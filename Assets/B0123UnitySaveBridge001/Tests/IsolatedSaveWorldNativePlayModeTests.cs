using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using B01.DetachedSaveContract001;
using B01.UnitySaveBridge001;
namespace B01.UnitySaveBridge001.Tests
{
    public sealed class IsolatedSaveWorldNativePlayModeTests
    {
        [UnityTest]
        public IEnumerator ColdLoad_RealRigidbody_RoundTripLightHeavy()
        {
            GameObject game=new GameObject("W15 native save test host");
            try
            {
                var host=game.AddComponent<IsolatedTwinRigSaveHost>();
                Assert.IsTrue(host.TrySelectPersistentRunSlot(31,System.Guid.NewGuid()));
                byte[] save=IsolatedTwinRigSaveHost.MakeFixture(31,1,true,true);
                Assert.IsTrue(host.TryRestoreBytes(save,31,1));
                Assert.IsTrue(host.IsConsumed(0,2310001));
                Assert.IsTrue(host.IsConsumed(1,2310002));
                // Actual native activeInHierarchy: unlike inactive staging,
                // both rigidbodies must become real physics participants on publish.
                var nativeBodies=game.GetComponentsInChildren<Rigidbody>(true);
                Assert.AreEqual(2,nativeBodies.Length);
                foreach(var body in nativeBodies)
                {
                    Assert.IsTrue(body.gameObject.activeInHierarchy);
                    Assert.IsFalse(body.isKinematic);
                }
                Assert.IsTrue(host.TrySaveBytes(out byte[] bytes));
                Assert.IsTrue(SnapshotCodec.TryDecode(bytes,out RunSnapshot snapshot));
                Assert.AreEqual(2,snapshot.Parts.Length);
                Assert.AreEqual(1.5,snapshot.Parts[0].MassKg,.0001);
                Assert.AreEqual(12,snapshot.Parts[1].MassKg,.0001);
                yield return null;
            }
            finally {Object.Destroy(game);}
        }
        [UnityTest]
        public IEnumerator CorruptLoad_LeavesPreviouslyActiveRigidbodyAndIdsUntouched()
        {
            GameObject game=new GameObject("W15 fail closed native host");
            try
            {
                var host=game.AddComponent<IsolatedTwinRigSaveHost>();
                Assert.IsTrue(host.TrySelectPersistentRunSlot(31,System.Guid.NewGuid()));
                Assert.IsTrue(host.TryRestoreBytes(
                    IsolatedTwinRigSaveHost.MakeFixture(31,1,true,false),31,1));
                byte[] corrupt=IsolatedTwinRigSaveHost.MakeFixture(31,2,false,true);
                corrupt[117]^=1;
                Assert.IsFalse(host.TryRestoreBytes(corrupt,31,1));
                Assert.IsTrue(host.IsConsumed(0,2310001));
                Assert.IsFalse(host.IsConsumed(1,2310002));
                Assert.AreEqual(1,host.Generation);
                yield return null;
            }
            finally {Object.Destroy(game);}
        }
        [UnityTest]
        public IEnumerator ColdRestart_RestoresRealRigidbodyAndConsumedIds()
        {
            GameObject prior=new GameObject("W15 old separate process");
            GameObject next=null;
            try
            {
                System.Guid slot=System.Guid.NewGuid();
                var original=prior.AddComponent<IsolatedTwinRigSaveHost>();
                Assert.IsTrue(original.TrySelectPersistentRunSlot(41,slot));
                Assert.IsTrue(original.TryRestoreBytes(
                    IsolatedTwinRigSaveHost.MakeFixture(41,4,true,false),41,4));
                Assert.IsTrue(original.TrySaveBytes(out byte[] saved));
                // Disable MUST NOT release the same-world slot to a newer host.
                prior.SetActive(false);
                Object.Destroy(prior);
                yield return null; // native Unity destruction and OnDestroy lease release
                next=new GameObject("W19 restarted owner");
                var restored=next.AddComponent<IsolatedTwinRigSaveHost>();
                Assert.IsTrue(restored.TrySelectPersistentRunSlot(41,slot));
                Assert.IsTrue(restored.TryRestoreBytes(saved,41,4));
                Assert.IsTrue(restored.IsConsumed(0,2310001));
                Assert.IsFalse(restored.IsConsumed(1,2310002));
                Assert.AreEqual(4,restored.Generation);
                yield return null;
            }
            finally
            {
                if(prior!=null)Object.Destroy(prior);
                if(next!=null)Object.Destroy(next);
            }
        }
        [UnityTest]
        public IEnumerator WrongGeneration_CannotPartiallyPublishRealPhysicalWorld()
        {
            var go=new GameObject("W15 generation native test");
            try
            {
                var host=go.AddComponent<IsolatedTwinRigSaveHost>();
                Assert.IsTrue(host.TrySelectPersistentRunSlot(9,System.Guid.NewGuid()));
                Assert.IsTrue(host.TryRestoreBytes(
                    IsolatedTwinRigSaveHost.MakeFixture(9,5,false,true),9,5));
                Assert.IsFalse(host.TryRestoreBytes(
                    IsolatedTwinRigSaveHost.MakeFixture(9,3,true,false),9,3));
                Assert.IsFalse(host.IsConsumed(0,2310001));
                Assert.IsTrue(host.IsConsumed(1,2310002));
                Assert.AreEqual(5,host.Generation);
                yield return null;
            }
            finally {Object.Destroy(go);}
        }

        [UnityTest]
        public IEnumerator SeparateRunSlots_CannotPoisonLaterRunOrReuseForeignOwner()
        {
            System.Guid slot=System.Guid.NewGuid();
            GameObject first=new GameObject("W18 run51 owner");
            GameObject second=new GameObject("W18 run52 owner");
            try
            {
                var a=first.AddComponent<IsolatedTwinRigSaveHost>();
                Assert.IsTrue(a.TrySelectPersistentRunSlot(51,slot));
                Assert.IsTrue(a.TryRestoreBytes(
                    IsolatedTwinRigSaveHost.MakeFixture(51,1,true,false),51,1));
                Assert.IsTrue(a.IsConsumed(0,2310001));
                first.SetActive(false); // owner lease retained; different run has different file
                var b=second.AddComponent<IsolatedTwinRigSaveHost>();
                Assert.IsTrue(b.TrySelectPersistentRunSlot(52,slot));
                Assert.IsTrue(b.TryRestoreBytes(
                    IsolatedTwinRigSaveHost.MakeFixture(52,1,false,true),52,1));
                Assert.IsTrue(b.IsConsumed(1,2310002));
                Assert.IsFalse(b.IsConsumed(0,2310001));
            }
            finally {Object.Destroy(first);Object.Destroy(second);}
            yield return null;
        }

        [UnityTest]
        public IEnumerator DisabledSameSlotHost_CannotStealOwnerUntilDestroyed()
        {
            System.Guid slot=System.Guid.NewGuid();
            GameObject first=new GameObject("W19 real slot owner");
            GameObject competing=null;
            try
            {
                var a=first.AddComponent<IsolatedTwinRigSaveHost>();
                Assert.IsTrue(a.TrySelectPersistentRunSlot(77,slot));
                Assert.IsTrue(a.TryRestoreBytes(
                    IsolatedTwinRigSaveHost.MakeFixture(77,1,true,false),77,1));
                first.SetActive(false); // NOT permission to hand over live state
                competing=new GameObject("W19 blocked second owner");
                var b=competing.AddComponent<IsolatedTwinRigSaveHost>();
                Assert.IsFalse(b.TrySelectPersistentRunSlot(77,slot));
                Assert.IsFalse(b.TryRestoreBytes(
                    IsolatedTwinRigSaveHost.MakeFixture(77,2,false,true),77,1));
                Object.Destroy(first);
                yield return null; // true Unity OnDestroy must close the slot lease
                Assert.IsTrue(b.TrySelectPersistentRunSlot(77,slot));
                Assert.IsTrue(b.TryRestoreBytes(
                    IsolatedTwinRigSaveHost.MakeFixture(77,2,false,true),77,1));
                Assert.IsTrue(b.IsConsumed(1,2310002));
            }
            finally
            {
                if(first!=null)Object.Destroy(first);
                if(competing!=null)Object.Destroy(competing);
            }
        }

        [UnityTest]
        public IEnumerator DestroyOnlyHostComponent_StopsPhysicsBeforeLeaseHandoff()
        {
            System.Guid slot=System.Guid.NewGuid();
            var aRoot=new GameObject("R5 standalone component owner");
            var bRoot=new GameObject("R5 rejected contender");
            try
            {
                var a=aRoot.AddComponent<IsolatedTwinRigSaveHost>();
                Assert.IsTrue(a.TrySelectPersistentRunSlot(91,slot));
                Assert.IsTrue(a.TryRestoreBytes(
                    IsolatedTwinRigSaveHost.MakeFixture(91,10,true,false),91,10));
                Assert.AreEqual(2,aRoot.GetComponentsInChildren<Rigidbody>(true).Length);
                var b=bRoot.AddComponent<IsolatedTwinRigSaveHost>();
                Assert.IsFalse(b.TrySelectPersistentRunSlot(91,slot));
                UnityEngine.Object.Destroy(a); // ONLY component, old root survives
                yield return null; // scheduled old physics world deletion + escrow
                Assert.AreEqual(0,aRoot.GetComponentsInChildren<Rigidbody>(true).Length);
                Assert.IsTrue(b.TrySelectPersistentRunSlot(91,slot));
                Assert.IsTrue(b.TryRestoreBytes(
                    IsolatedTwinRigSaveHost.MakeFixture(91,11,false,true),91,11));
                Assert.IsTrue(b.IsConsumed(1,2310002));
            }
            finally
            {
                UnityEngine.Object.Destroy(aRoot);
                UnityEngine.Object.Destroy(bRoot);
            }
        }
    }
}
