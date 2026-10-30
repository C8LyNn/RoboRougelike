// B01-62 P17 W21 — NEW OWN VERSIONED SOURCE-ONLY bridge.
// Compile ACTUAL unchanged #681 ICanonicalCommitPort and ACTUAL #669
// SingleFileSaveSlot source; never copy codecs, never modify frozen #510/#556.
#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using B01.CanonicalWorldApi001;
using B01.DetachedSaveContract001;
using B01.SingleFileSaveSlot001;

namespace B01.CanonicalSlotAdapter001
{
    public enum CanonicalSessionOpenStatus
    { Accepted, Busy, Invalid, Corrupt, NoOwner }

    // This class is not a Unity MonoBehaviour and does not gain access to
    // frozen #510's private Rig[] or AtomicDockLedger.
    public sealed class CanonicalSlotSession : ICanonicalCommitPort, IDisposable
    {
        private readonly SingleFileSaveSlot slot;
        private readonly long runId;
        private readonly Guid slotId;
        private CanonicalWorldGateway? gateway;
        private byte[]? armedPayload;
        private string armedHash="";
        private bool restoring,closed,terminalQuarantine;
        // NEVER let Dispose/finalization unlock a potentially live physics
        // world. Retain OS FileStream ownership for rest of process if the
        // caller has not PROVED actual physical retirement on its game thread.
        private static readonly object AbandonedSync=new object();
        private static readonly List<CanonicalSlotSession> Unretired=new List<CanonicalSlotSession>();

        private CanonicalSlotSession(SingleFileSaveSlot slot,long run,Guid guid)
        {this.slot=slot;runId=run;slotId=guid;}

        public bool Quarantined => terminalQuarantine||slot.Quarantined||
            (gateway?.Quarantined ?? false);
        public bool HasWorld => gateway?.HasWorld??false;
        public bool OwnsSlot => !closed&&!slot.Closed;
        public Guid SlotId=>slotId;
        public long RunId=>runId;

        public static CanonicalSessionOpenStatus TryOpen(string ownedDirectory,long run,
            Guid guid,bool create, out CanonicalSlotSession? session,
            IAtomicEnvelopePublisher? publisher=null)
        {
            session=null;
            SlotStatus st=create
                ?SingleFileSaveSlot.TryCreateNew(ownedDirectory,run,guid,out var x,publisher)
                :SingleFileSaveSlot.TryOpenExisting(ownedDirectory,run,guid,out x,publisher);
            if(st==SlotStatus.Accepted&&x!=null)
            {session=new CanonicalSlotSession(x,run,guid);return CanonicalSessionOpenStatus.Accepted;}
            if(st==SlotStatus.Busy)return CanonicalSessionOpenStatus.Busy;
            if(st==SlotStatus.Invalid)return CanonicalSessionOpenStatus.Invalid;
            if(st==SlotStatus.WrongOwner)return CanonicalSessionOpenStatus.NoOwner;
            return CanonicalSessionOpenStatus.Corrupt;
        }
        // One session owns ONE gateway; two gateways sharing a port is forbidden.
        public bool Attach(ICanonicalMutationGate gate,ICanonicalRigFactory factory)
        {
            if(closed||gateway!=null||gate==null||factory==null)return false;
            gateway=new CanonicalWorldGateway(gate,factory,this);
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
        public bool TryCloseAfterPhysicalRetirement(Func<bool> allOwnedRootsGone)
        {
            if(closed)return true;
            if(restoring||allOwnedRootsGone==null)return false;
            bool proven=false;
            try {proven=allOwnedRootsGone();}
            catch{return false;}
            if(!proven)return false;
            try {slot.Dispose();closed=true;return true;}
            catch{terminalQuarantine=true;return false;}
        }
        public void Dispose()
        {
            if(closed)return;
            if(!HasWorld&&!restoring)
            {slot.Dispose();closed=true;return;}
            // Explicit safer fail closed. OS lease not surrendered with a live
            // Rigidbody world; Unretired roots intentionally retain session.
            terminalQuarantine=true;
            lock(AbandonedSync){if(!Unretired.Contains(this))Unretired.Add(this);}
        }
    }
}
