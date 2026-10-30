// B01-62 P17 W24 new isolated selected #749 DESCENDANT R2 (NOT yet chosen).
// Reuses original #729 V3 FOUR-ARG gateway + ONE .owner, only V3R2 unique store.
// B01-62 W23 new version: exact #712-derived adapter with only one #729 V3 .owner authority.
// B01-62 P17 W21 — NEW OWN VERSIONED SOURCE-ONLY bridge.
// Compile ACTUAL unchanged #681 ICanonicalCommitPort and ACTUAL #669
// CanonicalSingleAuthorityStoreV3R2 source; never copy codecs, never modify frozen #510/#556.
#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using B01.CanonicalWorldApi001;
using B01.DetachedSaveContract001;
using B01.CanonicalSingleAuthorityStore004;

namespace B01.CanonicalSlotAdapter004
{
    public enum CanonicalSessionOpenStatus
    { Accepted, Busy, Invalid, Corrupt, NoOwner }

    // This class is not a Unity MonoBehaviour and does not gain access to
    // frozen #510's private Rig[] or AtomicDockLedger.
    public sealed class CanonicalSlotSessionV3R2 : ICanonicalCommitPort, IDisposable
    {
        private readonly CanonicalSingleAuthorityStoreV3R2 slot;
        private readonly CanonicalWorldSlotLease authority;
        private readonly long runId;
        private readonly Guid slotId;
        private CanonicalWorldGateway? gateway;
        private byte[]? armedPayload;
        private string armedHash="";
        private bool restoring,closed,terminalQuarantine;
        // P0 #707: even unpublished partial #691 private world may survive a
        // FAILED CleanupUnpublishedAndVerifyDestroyed: gateway.Quarantined=true,
        // gateway.HasWorld=false. NEVER release lifetime slot for such an owner.
        // NEVER let Dispose/finalization unlock a potentially live physics
        // world. Retain OS FileStream ownership for rest of process if the
        // caller has not PROVED actual physical retirement on its game thread.
        private static readonly object AbandonedSync=new object();
        private static readonly List<CanonicalSlotSessionV3R2> Unretired=new List<CanonicalSlotSessionV3R2>();

        private CanonicalSlotSessionV3R2(CanonicalSingleAuthorityStoreV3R2 slot,long run,Guid guid,CanonicalWorldSlotLease owner)
        {this.slot=slot;runId=run;slotId=guid;authority=owner;}

        public bool Quarantined => terminalQuarantine||slot.Quarantined||
            (gateway?.Quarantined ?? false);
        public bool HasWorld => gateway?.HasWorld??false;
        public bool OwnsSlot => !closed&&authority.Live;
        public Guid SlotId=>slotId;
        public long RunId=>runId;

        // ONE real OS .owner shared by V3 world and V3 physical Save store.
        public static CanonicalSessionOpenStatus TryOpen(string privateRoot,long run,
            Guid guid,bool create,out CanonicalSlotSessionV3R2? session,
            IAtomicEnvelopePublisher? publisher=null)
        {
            session=null;
            if(!CanonicalWorldSlotLease.TryAcquire(privateRoot,run,guid,
                out CanonicalWorldSlotLease? owner)||owner==null)
                return CanonicalSessionOpenStatus.Busy;
            try
            {
                string saveBase=Path.Combine(Path.GetFullPath(privateRoot),
                    "b0162_canonical_single_authority_v3");
                if(File.Exists(saveBase)||
                   (Directory.Exists(saveBase)&&
                   (File.GetAttributes(saveBase)&FileAttributes.ReparsePoint)!=0))
                {owner.TryCancelUnbound();return CanonicalSessionOpenStatus.NoOwner;}
                Directory.CreateDirectory(saveBase);
                string dir=Path.Combine(saveBase,"run_"+run.ToString(
                    System.Globalization.CultureInfo.InvariantCulture)+
                    "_slot_"+guid.ToString("N"));
                SlotStatus status=create
                    ?CanonicalSingleAuthorityStoreV3R2.TryCreateNew(dir,run,guid,owner,
                        out var store,publisher)
                    :CanonicalSingleAuthorityStoreV3R2.TryOpenExisting(dir,run,guid,owner,
                        out store,publisher);
                if(status==SlotStatus.Accepted&&store!=null)
                {session=new CanonicalSlotSessionV3R2(store,run,guid,owner);
                 return CanonicalSessionOpenStatus.Accepted;}
                owner.TryCancelUnbound();
                if(status==SlotStatus.Invalid)return CanonicalSessionOpenStatus.Invalid;
                if(status==SlotStatus.Busy)return CanonicalSessionOpenStatus.Busy;
                if(status==SlotStatus.WrongOwner)return CanonicalSessionOpenStatus.NoOwner;
                return CanonicalSessionOpenStatus.Corrupt;
            }
            catch
            {owner.TryCancelUnbound();return CanonicalSessionOpenStatus.Corrupt;}
        }
        // One session owns ONE gateway; two gateways sharing a port is forbidden.
        public bool Attach(ICanonicalMutationGate gate,ICanonicalRigFactory factory)
        {
            if(closed||gateway!=null||gate==null||factory==null)return false;
            gateway=new CanonicalWorldGateway(gate,factory,this,authority);
            return true;
        }
        public static string SourceHash(byte[] original556)
        {
            if(original556==null)throw new ArgumentNullException(nameof(original556));
            using(var sha=SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(original556))
                    .Replace("-","").ToLowerInvariant();
        }
        private bool Arm(byte[] bytes,long expectedRun)
        {
            if(closed||Quarantined||bytes==null||bytes.Length==0||
               expectedRun!=runId||
               !ColdRestore.TryStage(bytes,out ColdRestore? cold)||cold==null||
               cold.Snapshot.RunId!=runId)return false;
            armedPayload=(byte[])bytes.Clone();
            armedHash=SourceHash(armedPayload);
            return true;
        }
        public CanonicalGateResult TryRestore(byte[] bytes,long expectedRun,long minimumGeneration)
        {
            if(Quarantined)return CanonicalGateResult.RollbackQuarantine;
            if(closed)return CanonicalGateResult.HostBusy;
            if(restoring)return CanonicalGateResult.HostBusy;
            if(gateway==null)return CanonicalGateResult.HostException;
            restoring=true;
            try
            {
                if(!Arm(bytes,expectedRun))return CanonicalGateResult.InvalidSnapshot;
                CanonicalGateResult result=gateway.TryRestore(bytes,expectedRun,minimumGeneration);
                if(gateway.Quarantined||slot.Quarantined)
                {terminalQuarantine=true;return CanonicalGateResult.RollbackQuarantine;}
                return result;
            }
            catch
            {terminalQuarantine=true;return CanonicalGateResult.RollbackQuarantine;}
            finally
            {armedPayload=null;armedHash="";restoring=false;}
        }
        public bool TryCapturePreview(out byte[] original556)
        {
            original556=Array.Empty<byte>();
            if(closed||Quarantined||restoring||gateway==null)return false;
            return gateway.TryCapturePreview(out original556);
        }
        // Contract required by the ORIGINAL frozen #681 gateway.
        public bool Read(long run,out long generation,out string hash)
        {
            generation=0;hash="";
            if(closed||Quarantined||run!=runId)return false;
            SlotStatus status=slot.TryRead(out long epoch,out byte[] bytes);
            if(status==SlotStatus.Empty)return true;
            if(status!=SlotStatus.Accepted)
            {terminalQuarantine=true;return false;}
            if(!ColdRestore.TryStage(bytes,out ColdRestore? cold)||cold==null||
               cold.Snapshot.RunId!=runId||cold.Snapshot.Generation!=epoch)
            {terminalQuarantine=true;return false;}
            generation=epoch;hash=SourceHash(bytes);
            return true;
        }
        public bool Commit(long run,long prior,string priorHash,long next,string hash)
        {
            if(closed||Quarantined||!restoring||run!=runId||
               armedPayload==null||prior<0||next<=prior||
               string.IsNullOrEmpty(hash)||hash!=armedHash||
               priorHash==null)return false;
            // The producer #681 currently allows a generation jump, but
            // single-file #669 intentionally demands exactly +1.
            if(prior==long.MaxValue||next!=prior+1)return false;
            if(!ColdRestore.TryStage(armedPayload,out ColdRestore? staged)||
               staged==null||staged.Snapshot.RunId!=runId||
               staged.Snapshot.Generation!=next)return false;
            if(!Read(run,out long current,out string evidence)||
               current!=prior||evidence!=priorHash)
            {terminalQuarantine=true;return false;}
            SlotStatus committed=slot.TryCommit(prior,armedPayload);
            if(committed!=SlotStatus.Accepted)
            {terminalQuarantine=true;return false;}
            // This exact original API verifies READBACK of the newly
            // committed full #556 binary payload rather than a CAS sidecar.
            if(!Read(run,out long actual,out string actualHash)||
               actual!=next||actualHash!=hash)
            {terminalQuarantine=true;return false;}
            return true;
        }
        // The SAME original #729 gateway is the ONLY owner of releasing .owner.
        public bool TryCloseAfterPhysicalRetirement(Func<bool> allOwnedRootsGone)
        {
            if(closed)return true;
            if(restoring||allOwnedRootsGone==null)return false;
            if(gateway==null)
            {if(!authority.TryCancelUnbound())return false;
             slot.Dispose();closed=true;return true;}
            if(!gateway.TryCloseAfterPhysicalRetirement(allOwnedRootsGone))
                return false;
            slot.Dispose();closed=true;return true;
        }
        public void Dispose()
        {
            if(closed)return;
            // Bound private partial worlds may exist even with HasWorld=false.
            if(gateway==null&&!Quarantined)
            {if(authority.TryCancelUnbound())
             {slot.Dispose();closed=true;return;}}
            terminalQuarantine=true;
            lock(AbandonedSync){if(!Unretired.Contains(this))Unretired.Add(this);}
        }
    }
}
