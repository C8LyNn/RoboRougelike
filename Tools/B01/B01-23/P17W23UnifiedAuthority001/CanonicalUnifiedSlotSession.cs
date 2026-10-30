// B01-23 P17 W23 — One real OS authority for both original #729 gateway
// and unchanged original #669 single-envelope slot, adopting B01-62 #712 rules.
// V4 is NEW author-owned source; does not edit B01-62 #669/#712 or frozen #510.
// SOURCE-ONLY until real #510 private rig + Rigidbody port / Unity native G1.
#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using B01.CanonicalWorldApi001;
using B01.DetachedSaveContract001;
using B01.SingleFileSaveSlot001;

namespace B01.CanonicalUnifiedAuthorityV4
{
    public enum UnifiedOpenStatus { Accepted, Busy, Invalid, WrongOwner, IOError }

    // The ENTIRE authoritative B01-62 SingleFileSaveSlot is private.
    // No caller can close its genuine slot.lease while an active/partial rig
    // survives; the Gateway owns exactly one adopted token referring to it.
    public sealed class CanonicalUnifiedSlotSession : ICanonicalCommitPort, IDisposable
    {
        private static readonly object EscrowSync=new object();
        private static readonly List<CanonicalUnifiedSlotSession> Unretired=
            new List<CanonicalUnifiedSlotSession>();

        private readonly SingleFileSaveSlot original669;
        private readonly CanonicalWorldSlotLease worldToken;
        private readonly long runId;
        private readonly Guid slotGuid;
        private CanonicalWorldGateway? gateway;
        private byte[]? armed;
        private string armedHash="";
        private bool restoring,closed,quarantined;
        public bool Quarantined=>quarantined||original669.Quarantined||
            (gateway?.Quarantined??false);
        public bool HasWorld=>gateway?.HasWorld??false;
        public bool OwnsAuthority=>!closed&&!original669.Closed;
        public Guid SlotId=>slotGuid;
        public long RunId=>runId;

        private CanonicalUnifiedSlotSession(SingleFileSaveSlot source,
            CanonicalWorldSlotLease owner)
        {
            original669=source;worldToken=owner;
            runId=source.RunId;slotGuid=source.SlotId;
        }

        private static bool TrustedComponents(string directory)
        {
            try
            {
                string path=Path.GetFullPath(directory);
                if(!Path.IsPathFullyQualified(path))return false;
                // Prevent foreign symlink-junction traversal as far as the
                // managed OS FileAttributes API permits (not a hostile-FS
                // atomic/openat security proof).
                for(string? candidate=path;candidate!=null;
                    candidate=Path.GetDirectoryName(candidate))
                {
                    if((Directory.Exists(candidate)||File.Exists(candidate))&&
                        (File.GetAttributes(candidate)&FileAttributes.ReparsePoint)!=0)
                        return false;
                    string? parent=Path.GetDirectoryName(candidate);
                    if(parent==candidate)break;
                }
                return true;
            }
            catch{return false;}
        }
        public static string CanonicalDirectory(string trustedPrivateRoot,long run,
            Guid slot)
            => Path.Combine(Path.GetFullPath(trustedPrivateRoot),
                "b01_canonical_slot_run_"+run.ToString(
                    System.Globalization.CultureInfo.InvariantCulture)+
                "_guid_"+slot.ToString("N"));

        public static UnifiedOpenStatus TryOpen(string trustedPrivateRoot,long run,
            Guid guid,bool create,out CanonicalUnifiedSlotSession? session,
            IAtomicEnvelopePublisher? writer=null)
        {
            session=null;
            if(run<=0||guid==Guid.Empty||string.IsNullOrWhiteSpace(trustedPrivateRoot)||
                !Path.IsPathFullyQualified(trustedPrivateRoot)||
                !Directory.Exists(trustedPrivateRoot)||
                !TrustedComponents(trustedPrivateRoot))
                return UnifiedOpenStatus.Invalid;
            // Only ONE well-defined physical path per run+GUID below trusted
            // private storage root; callers cannot inject a second directory.
            string exactSlotDirectory=CanonicalDirectory(trustedPrivateRoot,run,guid);
            if(!TrustedComponents(exactSlotDirectory))
                return UnifiedOpenStatus.Invalid;
            SlotStatus status=create
                ? SingleFileSaveSlot.TryCreateNew(exactSlotDirectory,run,guid,
                    out SingleFileSaveSlot? slot,writer)
                : SingleFileSaveSlot.TryOpenExisting(exactSlotDirectory,run,guid,
                    out slot,writer);
            if(status==SlotStatus.Busy)return UnifiedOpenStatus.Busy;
            if(status==SlotStatus.WrongOwner)return UnifiedOpenStatus.WrongOwner;
            if(status==SlotStatus.Invalid)return UnifiedOpenStatus.Invalid;
            if(status!=SlotStatus.Accepted||slot==null)return UnifiedOpenStatus.IOError;

            // SINGLE OS handle: original669 holds slot.lease. V4 does NOT open
            // any second .owner stream. Only internal glue adopts this slot.
            CanonicalWorldSlotLease? worldLease;
            if(!CanonicalWorldSlotLease.TryAdoptRealSlot(exactSlotDirectory,run,guid,
                ()=>!slot.Closed,()=>
                {
                    if(slot.Closed)return false;
                    slot.Dispose();
                    return slot.Closed;
                },out worldLease)||worldLease==null)
            {
                slot.Dispose(); // no gateway or rig has been created
                return UnifiedOpenStatus.Busy;
            }
            session=new CanonicalUnifiedSlotSession(slot,worldLease);
            return UnifiedOpenStatus.Accepted;
        }
        public bool Attach(ICanonicalMutationGate gate,ICanonicalRigFactory factory)
        {
            if(closed||restoring||Quarantined||gateway!=null||
                gate==null||factory==null||!OwnsAuthority)return false;
            try
            {
                gateway=new CanonicalWorldGateway(gate,factory,this,worldToken);
                return true;
            }
            catch
            {
                quarantined=true;return false;
            }
        }
        public static string SourceHash(byte[] original556)
        {
            if(original556==null)throw new ArgumentNullException(nameof(original556));
            return Convert.ToHexString(SHA256.HashData(original556)).ToLowerInvariant();
        }
        public SlotStatus TryReadSaved(out byte[] original556,out long generation)
        {
            original556=Array.Empty<byte>();generation=0;
            if(closed||restoring||Quarantined||!OwnsAuthority)return SlotStatus.Quarantined;
            return original669.TryRead(out generation,out original556);
        }
        public CanonicalGateResult TryRestore(byte[] original556,long run,
            long minimumGeneration)
        {
            if(closed||Quarantined)return CanonicalGateResult.RollbackQuarantine;
            if(restoring||gateway==null||run!=runId)return CanonicalGateResult.HostBusy;
            if(original556==null||
                !ColdRestore.TryStage(original556,out ColdRestore? plan)||
                plan==null||plan.Snapshot.RunId!=runId)
                return CanonicalGateResult.InvalidSnapshot;
            restoring=true;
            armed=(byte[])original556.Clone();
            armedHash=SourceHash(armed);
            try
            {
                CanonicalGateResult result=gateway.TryRestore(original556,run,
                    minimumGeneration);
                if(gateway.Quarantined||original669.Quarantined)
                {quarantined=true;return CanonicalGateResult.RollbackQuarantine;}
                return result;
            }
            catch{quarantined=true;return CanonicalGateResult.RollbackQuarantine;}
            finally{armed=null;armedHash="";restoring=false;}
        }
        public bool TryCapturePreview(out byte[] original556)
        {
            original556=Array.Empty<byte>();
            return !closed&&!restoring&&!Quarantined&&gateway!=null&&
                gateway.TryCapturePreview(out original556);
        }
        public bool Read(long run,out long generation,out string hash)
        {
            generation=0;hash="";
            if(closed||Quarantined||run!=runId||!OwnsAuthority)return false;
            SlotStatus st=original669.TryRead(out long gen,out byte[] payload);
            if(st==SlotStatus.Empty)return true;
            if(st!=SlotStatus.Accepted)
            {quarantined=true;return false;}
            if(!ColdRestore.TryStage(payload,out ColdRestore? restored)||restored==null||
               restored.Snapshot.RunId!=runId||restored.Snapshot.Generation!=gen)
            {quarantined=true;return false;}
            generation=gen;hash=SourceHash(payload);
            return true;
        }
        public bool Commit(long run,long prior,string priorHash,long next,string hash)
        {
            if(closed||Quarantined||!restoring||!OwnsAuthority||run!=runId||
                armed==null||hash!=armedHash||priorHash==null||prior<0||
                prior==long.MaxValue||next!=prior+1)return false;
            if(!ColdRestore.TryStage(armed,out ColdRestore? plan)||plan==null||
                plan.Snapshot.RunId!=runId||plan.Snapshot.Generation!=next)
                return false;
            if(!Read(run,out long gen,out string actual)||gen!=prior||
                actual!=priorHash)
            {quarantined=true;return false;}
            SlotStatus result=original669.TryCommit(prior,armed);
            if(result!=SlotStatus.Accepted)
            {quarantined=true;return false;}
            if(!Read(run,out long observed,out string fingerprint)||
                observed!=next||fingerprint!=hash)
            {quarantined=true;return false;}
            return true;
        }
        // ONE retirement => ONE physical OS lease release. Not two callbacks
        // or two physical FileStreams. The owner callback must prove ALL real
        // published+partially staged Unity roots have been retired; cannot be
        // automatically certified by this managed source-only wrapper.
        public bool TryCloseAfterPhysicalRetirement(Func<bool> allRootsReallyGone)
        {
            if(closed)return true;
            if(restoring||gateway==null||allRootsReallyGone==null)return false;
            bool ok=gateway.TryCloseAfterPhysicalRetirement(allRootsReallyGone);
            if(ok&&original669.Closed){closed=true;return true;}
            return false;
        }
        public void Dispose()
        {
            if(closed)return;
            if(gateway==null)
            {
                // No physical gateway ever bound => no GameObjects ever staged.
                if(worldToken.TryCancelUnbound()){closed=true;return;}
            }
            // Once attached, even HasWorld=false may hide orphan partial rig.
            // NEVER release #669 slot.lease before explicit physical proof.
            quarantined=true;
            lock(EscrowSync)
                if(!Unretired.Contains(this))Unretired.Add(this);
        }
    }
}
