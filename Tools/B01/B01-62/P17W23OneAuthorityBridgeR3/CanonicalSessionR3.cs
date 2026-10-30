// W23 R3 versioned derivative of original #712 fefc2e263e068b913c6e59ec3c8375bbbe43db4e.
// Integrates ORIGINAL #729 V3 physically single OS .owner gate, no .lease.
// B01-62 P17 W21 — NEW OWN VERSIONED SOURCE-ONLY bridge.
// Compile ACTUAL unchanged #681 ICanonicalCommitPort and ACTUAL #669
// SingleAuthoritySaveSlot source; never copy codecs, never modify frozen #510/#556.
#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using B01.CanonicalWorldApi001;
using B01.DetachedSaveContract001;
using B01.SingleAuthoritySlot003;

namespace B01.CanonicalSlotAdapter003
{
    public enum CanonicalSessionOpenStatus
    { Accepted, Busy, Invalid, Corrupt, NoOwner }

    // This class is not a Unity MonoBehaviour and does not gain access to
    // frozen #510's private Rig[] or AtomicDockLedger.
    public sealed class CanonicalSlotSessionR3 : ICanonicalCommitPort, IDisposable
    {
        private readonly SingleAuthoritySaveSlot slot;
        private readonly CanonicalWorldSlotLease physicalOwner;
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
        private static readonly List<CanonicalSlotSessionR3> Unretired=new List<CanonicalSlotSessionR3>();

        private CanonicalSlotSessionR3(SingleAuthoritySaveSlot slot,CanonicalWorldSlotLease physicalOwner,long run,Guid guid)
        {this.slot=slot;this.physicalOwner=physicalOwner;runId=run;slotId=guid;}

        public bool Quarantined => terminalQuarantine||slot.Quarantined||
            (gateway?.Quarantined ?? false);
        public bool HasWorld => gateway?.HasWorld??false;
        public bool OwnsSlot => !closed&&!slot.Closed;
        public Guid SlotId=>slotId;
        public long RunId=>runId;

        public static CanonicalSessionOpenStatus TryOpen(string ownedDirectory,long run,
            Guid guid,bool create, out CanonicalSlotSessionR3? session,
            IAtomicEnvelopePublisher? publisher=null)
        {
            session=null;
            if(string.IsNullOrWhiteSpace(ownedDirectory)||
                !System.IO.Path.IsPathFullyQualified(ownedDirectory))
                return CanonicalSessionOpenStatus.Invalid;
            string? trustedRoot=System.IO.Path.GetDirectoryName(ownedDirectory);
            if(trustedRoot==null)return CanonicalSessionOpenStatus.Invalid;
            // EXACT #729 acquires its ONE .owner FileStream BEFORE any slot
            // file access, staging, quiescence callback or private game rig.
            if(!CanonicalWorldSlotLease.TryAcquire(trustedRoot,run,guid,
                out CanonicalWorldSlotLease? physicalOwner)||physicalOwner==null)
                return CanonicalSessionOpenStatus.Busy;
            SlotStatus st;
            SingleAuthoritySaveSlot? x=null;
            try
            {
                st=create
                    ?SingleAuthoritySaveSlot.TryCreateNew(ownedDirectory,run,guid,
                        physicalOwner,out x,publisher)
                    :SingleAuthoritySaveSlot.TryOpenExisting(ownedDirectory,run,guid,
                        physicalOwner,out x,publisher);
                if(st==SlotStatus.Accepted&&x!=null)
                {
                    session=new CanonicalSlotSessionR3(x,physicalOwner,run,guid);
                    return CanonicalSessionOpenStatus.Accepted;
                }
            }
            catch {st=SlotStatus.Corrupt;}
            // NEVER release a bound owner here. No Gateway exists, so this
            // is exclusively an UNBOUND failure with no allocated rig.
            physicalOwner.TryCancelUnbound();
            if(st==SlotStatus.Busy)return CanonicalSessionOpenStatus.Busy;
            if(st==SlotStatus.Invalid)return CanonicalSessionOpenStatus.Invalid;
            if(st==SlotStatus.WrongOwner)return CanonicalSessionOpenStatus.NoOwner;
            return CanonicalSessionOpenStatus.Corrupt;
        }
        // One session owns ONE gateway; two gateways sharing a port is forbidden.
        public bool Attach(ICanonicalMutationGate gate,ICanonicalRigFactory factory)
        {
            if(closed||gateway!=null||gate==null||factory==null)return false;
            gateway=new CanonicalWorldGateway(gate,factory,this,physicalOwner);
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
        // A concrete owner-approved Unity #510 adapter is responsible for
        // *actually* destroying its old/new roots and private ledger before
        // supplying an identity-specific proof callback. Mere OnDestroy of
        // a MonoBehaviour is NOT proof.
        // Unconditionally use the selected ORIGINAL V3 gateway for the ONE
        // physical owner lifetime. Never call physicalOwner.ReleaseVerified
        // or open/close an independent .lease from B01-62.
        public bool TryCloseAfterPhysicalRetirement(Func<bool> allOwnedRootsGone)
        {
            if(closed)return true;
            if(restoring||allOwnedRootsGone==null)return false;
            if(gateway==null)
            {
                if(!physicalOwner.TryCancelUnbound())return false;
                slot.Dispose();closed=true;return true;
            }
            bool retired;
            try {retired=gateway.TryCloseAfterPhysicalRetirement(allOwnedRootsGone);}
            catch {retired=false;}
            if(!retired){terminalQuarantine=true;return false;}
            slot.Dispose();closed=true;return true;
        }
        public void Dispose()
        {
            if(closed)return;
            // If nobody ever attached a gateway, there can be no staged root,
            // and we may cancel ONLY an unbound original V3 owner.
            if(gateway==null&&!restoring&&!Quarantined&&
               physicalOwner.TryCancelUnbound())
            {slot.Dispose();closed=true;return;}
            // Once gateway is bound, Dispose/GC/Unity teardown can NEVER
            // release the sole .owner authority without explicit physical proof,
            // regardless of HasWorld=false/partial-staged-world faults.
            terminalQuarantine=true;
            lock(AbandonedSync){if(!Unretired.Contains(this))Unretired.Add(this);}
        }

    }
}
