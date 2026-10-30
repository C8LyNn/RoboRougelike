// REAL Unity NUnit / PhysX PlayMode TEST SOURCE; not executed by dotnet CI.
using System;
using System.Collections;
using System.IO;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using B01.CanonicalRigPortW24;
using B01.CanonicalWorldApi001;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;
using B01.SecureReattachV2;

namespace B01.CanonicalRigPortW24.Tests
{
    public sealed class CanonicalPhysicalRigPortW24NativeTests
    {
        private sealed class Disk : ICanonicalCommitPort
        {
            public long Generation;
            public string Digest="";
            public bool Read(long run,out long generation,out string hash)
            {generation=Generation;hash=Digest;return run==81;}
            public bool Commit(long run,long prior,string priorHash,long next,string hash)
            {
                if(run!=81||prior!=Generation||priorHash!=Digest)return false;
                Generation=next;Digest=hash;return true;
            }
        }
        private static byte[] MakeSnapshot(int detachedMask,long generation=1)
        {
            var parts=new PersistedPart[2];
            for(int i=0;i<parts.Length;i++)
            {
                var spec=SecureRigCatalog.At(i);
                bool detached=(detachedMask&(1<<i))!=0;
                var pos=new Vector3d(i==0?-2f:4f,1.25f,0f);
                parts[i]=new PersistedPart(
                    spec.PartId,FrozenPartCatalog.Owner(i),
                    detached?0:FrozenPartCatalog.Owner(i),
                    FrozenPartCatalog.Asset(i),FrozenPartCatalog.Socket(i),
                    detached?PartDockState.Detached:PartDockState.Attached,
                    detached,!detached,pos,0,0,0,1,
                    detached?new Vector3d(1f,0f,0f):new Vector3d(0f,0f,0f),
                    new Vector3d(0f,0f,0f),spec.PartMassKg);
            }
            return SnapshotCodec.Encode(new RunSnapshot(81,generation,parts));
        }
        private static string NewOwnerRoot()
        {
            string path=Path.Combine(Application.temporaryCachePath,
                "b01w24-native-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }
        [UnityTest]
        public IEnumerator Original510AtomicLedger_RealRigidbodyDetachedAndAttachedParentAndVelocity()
        {
            var host=new GameObject("B01 W24 original #510 physical owner");
            string root=NewOwnerRoot();
            var port=host.AddComponent<CanonicalPhysicalRigPortW24>();
            CanonicalWorldSlotLease? lease=null;
            bool ownerReleased=false;
            try
            {
                Assert.IsTrue(port.SetExternalWritersQuiesced(true));
                Assert.IsTrue(CanonicalWorldSlotLease.TryAcquire(root,81,Guid.NewGuid(),out lease));
                var gateway=new CanonicalWorldGateway(port,port,new Disk(),lease!);
                byte[] original=MakeSnapshot(1);
                Assert.AreEqual(CanonicalGateResult.Accepted,gateway.TryRestore(original,81,1));
                Rigidbody? light=port.VisiblePartBody(0);
                Rigidbody? heavy=port.VisiblePartBody(1);
                Assert.IsNotNull(light);Assert.IsNotNull(heavy);
                Assert.IsNull(light!.transform.parent);
                Assert.IsNotNull(heavy!.transform.parent);
                Assert.IsFalse(light.isKinematic);
                Assert.IsTrue(heavy.isKinematic);
                Assert.AreEqual(1.5f,light.mass);
                Assert.AreEqual(12f,heavy.mass);
                Assert.AreEqual(1f,light.linearVelocity.x);
                Assert.IsTrue(port.VisibleConsumedId(0,2310001));
                Assert.IsFalse(port.VisibleConsumedId(1,2310002));
                Assert.IsTrue(gateway.TryCapturePreview(out byte[] exported));
                Assert.IsTrue(SnapshotCodec.TryDecode(exported,out RunSnapshot restored));
                Assert.AreEqual(81,restored.RunId);
                Assert.IsTrue(restored.Parts[0].Consumed);
                Assert.IsFalse(restored.Parts[1].Consumed);
                // Deferred Unity destruction must not free owner lease.
                Assert.IsFalse(gateway.TryCloseAfterPhysicalRetirement(
                    port.AllOwnedPhysicalRootsDestroyed));
                yield return null;
                Assert.IsTrue(gateway.TryCloseAfterPhysicalRetirement(
                    port.AllOwnedPhysicalRootsDestroyed));
                ownerReleased=true;
            }
            finally
            {
                UnityEngine.Object.Destroy(host);
                // Only our uniquely named, newly created disposable test root.
                if(ownerReleased&&Directory.Exists(root))Directory.Delete(root,true);
            }
        }
        [UnityTest]
        public IEnumerator AbortedCompetingWorld_PreservesActualLiveVelocityAndAngularVelocity()
        {
            // P0 W25: the OLD world has moved since the original #556 snapshot.
            // Deactivate/Reactivate is the canonical #729 rollback path when
            // the competing NEW world fails after touching the old world.
            var go=new GameObject("W25 live physics rollback");
            var port=go.AddComponent<CanonicalPhysicalRigPortW24>();
            try
            {
                Assert.IsTrue(port.SetExternalWritersQuiesced(true));
                byte[] original=MakeSnapshot(1);
                Assert.IsTrue(ColdRestore.TryStage(original,out ColdRestore stage));
                ICanonicalStageOwnership owned=port.BeginOwnedStage(stage.Snapshot,stage);
                ICanonicalStagedWorld world=owned.PrepareInactive();
                world.Activate();
                Rigidbody body=port.VisiblePartBody(0);
                Assert.IsNotNull(body);
                body.linearVelocity=new Vector3(2.5f,0,0);
                body.angularVelocity=new Vector3(0,0.5f,0);
                world.Deactivate();
                world.Activate();
                Assert.AreEqual(2.5f,body.linearVelocity.x,1e-4f,
                    "rollback must preserve CURRENT body speed, not initial #556 velocity 1");
                Assert.AreEqual(0.5f,body.angularVelocity.y,1e-4f,
                    "rollback must preserve CURRENT angular velocity");
                Assert.AreEqual(0f,body.transform.parent==null?0f:1f);
                Assert.IsTrue(port.VisibleConsumedId(0,2310001));
                world.Retire();
                yield return null; // Unity Destroy is deferred
                Assert.IsTrue(port.AllOwnedPhysicalRootsDestroyed());
            }
            finally { UnityEngine.Object.Destroy(go); }
        }

        [UnityTest]
        public IEnumerator InvalidOriginal556Binary_CannotCreateAnyUnityBodyOrConsumeId()
        {
            var go=new GameObject("B01 W24 invalid original #556");
            string root=NewOwnerRoot();CanonicalWorldSlotLease? lease=null;
            bool ownerReleased=false;
            try
            {
                var port=go.AddComponent<CanonicalPhysicalRigPortW24>();
                Assert.IsTrue(port.SetExternalWritersQuiesced(true));
                Assert.IsTrue(CanonicalWorldSlotLease.TryAcquire(root,81,Guid.NewGuid(),out lease));
                var gateway=new CanonicalWorldGateway(port,port,new Disk(),lease!);
                byte[] corrupt=MakeSnapshot(2);corrupt[100]^=0x40;
                Assert.AreEqual(CanonicalGateResult.InvalidSnapshot,
                    gateway.TryRestore(corrupt,81,1));
                Assert.IsNull(port.VisiblePartBody(0));
                Assert.IsNull(port.VisiblePartBody(1));
                Assert.IsTrue(port.AllOwnedPhysicalRootsDestroyed());
                Assert.IsTrue(gateway.TryCloseAfterPhysicalRetirement(
                    port.AllOwnedPhysicalRootsDestroyed));
                ownerReleased=true;
                yield return null;
            }
            finally
            {
                UnityEngine.Object.Destroy(go);
                if(ownerReleased&&Directory.Exists(root))Directory.Delete(root,true);
            }
        }
        [UnityTest]
        public IEnumerator QuiescenceRequired_BeforeRealPhysicalStage()
        {
            var go=new GameObject("B01 W24 input quiescence");
            string root=NewOwnerRoot();CanonicalWorldSlotLease? lease=null;
            bool ownerReleased=false;
            try
            {
                var port=go.AddComponent<CanonicalPhysicalRigPortW24>();
                Assert.IsTrue(CanonicalWorldSlotLease.TryAcquire(root,81,Guid.NewGuid(),out lease));
                var gateway=new CanonicalWorldGateway(port,port,new Disk(),lease!);
                Assert.AreEqual(CanonicalGateResult.HostBusy,
                    gateway.TryRestore(MakeSnapshot(3),81,1));
                Assert.IsNull(port.VisiblePartBody(0));
                Assert.IsTrue(gateway.TryCloseAfterPhysicalRetirement(
                    port.AllOwnedPhysicalRootsDestroyed));
                ownerReleased=true;
                yield return null;
            }
            finally
            {
                UnityEngine.Object.Destroy(go);
                if(ownerReleased&&Directory.Exists(root))Directory.Delete(root,true);
            }
        }
    }
}
