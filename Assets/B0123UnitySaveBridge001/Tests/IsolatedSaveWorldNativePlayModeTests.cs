using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using B01.DetachedSaveContract001;
using B01.UnitySaveBridge001;

namespace B01.UnitySaveBridge001.Tests
{
    // Authored true Unity Editor/PhysX PlayMode tests; NATIVE_G1_NOT_RUN until
    // real licensed Unity6000.3.14f1 executes and produces Editor.log + XML.
    public sealed class IsolatedSaveWorldNativePlayModeTests
    {
        private static string PrivateSlot()=>"W18T_"+Guid.NewGuid().ToString("N");
        private static (GameObject,IsolatedTwinRigSaveHost) Open(string label,string slot)
        {
            var root=new GameObject(label);
            root.SetActive(false); // Awake MUST NOT run until slot configured
            var host=root.AddComponent<IsolatedTwinRigSaveHost>();
            Assert.IsTrue(host.ConfigureCheckpointSlotBeforeAwake(slot));
            root.SetActive(true);
            Assert.IsTrue(host.OwnsCheckpointSlot);
            return (root,host);
        }
        private static void CleanOwnTestSlot(string slot)
        {
            string p=IsolatedTwinRigSaveHost.CheckpointPathForSlot(slot);
            // Only exact unique GUID-owned slot filenames, NEVER a shared or
            // player/default checkpoint, NEVER delete an occupied directory.
            if(!slot.StartsWith("W18T_",StringComparison.Ordinal))return;
            foreach(string ext in new[]{"",".lock",".ownerlock"})
                if(File.Exists(p+ext))File.Delete(p+ext);
        }
        [UnityTest]
        public IEnumerator ColdLoad_RealRigidbody_RoundTripLightHeavy()
        {
            string slot=PrivateSlot();
            var pair=Open("W18 native real bodies",slot);
            try
            {
                var host=pair.Item2;
                Assert.IsTrue(host.TryRestoreBytes(
                    IsolatedTwinRigSaveHost.MakeFixture(31,1,true,true),31,1));
                Assert.IsTrue(host.IsConsumed(0,2310001));
                Assert.IsTrue(host.IsConsumed(1,2310002));
                Rigidbody[] bodies=pair.Item1.GetComponentsInChildren<Rigidbody>(true);
                Assert.AreEqual(2,bodies.Length);
                foreach(var body in bodies)
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
            finally {UnityEngine.Object.Destroy(pair.Item1);}
            yield return null;CleanOwnTestSlot(slot);
        }
        [UnityTest]
        public IEnumerator CorruptLoad_LeavesPreviouslyActiveRigidbodyAndIdsUntouched()
        {
            string slot=PrivateSlot();var pair=Open("W18 corruption",slot);
            try
            {
                var host=pair.Item2;
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
            finally {UnityEngine.Object.Destroy(pair.Item1);}
            yield return null;CleanOwnTestSlot(slot);
        }
        [UnityTest]
        public IEnumerator ColdRestart_RestoresRealRigidbodyAndConsumedIds()
        {
            string slot=PrivateSlot();
            var previous=Open("W18 previous host",slot);
            byte[] saved;
            Assert.IsTrue(previous.Item2.TryRestoreBytes(
                IsolatedTwinRigSaveHost.MakeFixture(41,4,true,false),41,4));
            Assert.IsTrue(previous.Item2.TrySaveBytes(out saved));
            // Save output advances generation to 5, not old generation 4.
            UnityEngine.Object.Destroy(previous.Item1);
            yield return null; // actual OnDestroy releases the old owner lease
            var next=Open("W18 newly restarted host",slot);
            try
            {
                Assert.IsTrue(next.Item2.TryRestoreBytes(saved,41,5));
                Assert.IsTrue(next.Item2.IsConsumed(0,2310001));
                Assert.IsFalse(next.Item2.IsConsumed(1,2310002));
                Assert.AreEqual(5,next.Item2.Generation);
                yield return null;
            }
            finally {UnityEngine.Object.Destroy(next.Item1);}
            yield return null;CleanOwnTestSlot(slot);
        }
        [UnityTest]
        public IEnumerator WrongGeneration_CannotPartiallyPublishRealPhysicalWorld()
        {
            string slot=PrivateSlot();var pair=Open("W18 version safety",slot);
            try
            {
                var host=pair.Item2;
                Assert.IsTrue(host.TryRestoreBytes(
                    IsolatedTwinRigSaveHost.MakeFixture(9,5,false,true),9,5));
                Assert.IsFalse(host.TryRestoreBytes(
                    IsolatedTwinRigSaveHost.MakeFixture(9,3,true,false),9,3));
                Assert.IsFalse(host.IsConsumed(0,2310001));
                Assert.IsTrue(host.IsConsumed(1,2310002));
                Assert.AreEqual(5,host.Generation);
                yield return null;
            }
            finally {UnityEngine.Object.Destroy(pair.Item1);}
            yield return null;CleanOwnTestSlot(slot);
        }
        [UnityTest]
        public IEnumerator SecondActiveHostSameSlot_DeniedUntilFirstHostDestroyed()
        {
            string slot=PrivateSlot();
            var old=Open("W18 leased host A",slot);
            old.Item2.TryRestoreBytes(IsolatedTwinRigSaveHost.MakeFixture(61,3,true,false),61,3);
            GameObject stranger=new GameObject("W18 same slot untrusted host B");
            stranger.SetActive(false);
            var second=stranger.AddComponent<IsolatedTwinRigSaveHost>();
            Assert.IsTrue(second.ConfigureCheckpointSlotBeforeAwake(slot));
            stranger.SetActive(true);
            Assert.IsFalse(second.OwnsCheckpointSlot);
            Assert.IsFalse(second.TryRestoreBytes(
                IsolatedTwinRigSaveHost.MakeFixture(61,4,false,true),61,4));
            Assert.IsFalse(second.HasWorld);
            UnityEngine.Object.Destroy(old.Item1);
            yield return null;
            var fresh=Open("W18 new sole owner after actual destroy",slot);
            try
            {
                Assert.IsTrue(fresh.Item2.TryRestoreBytes(
                    IsolatedTwinRigSaveHost.MakeFixture(61,4,false,true),61,4));
                Assert.IsTrue(fresh.Item2.IsConsumed(1,2310002));
                yield return null;
            }
            finally
            {
                UnityEngine.Object.Destroy(stranger);
                UnityEngine.Object.Destroy(fresh.Item1);
            }
            yield return null;CleanOwnTestSlot(slot);
        }
    }
}
