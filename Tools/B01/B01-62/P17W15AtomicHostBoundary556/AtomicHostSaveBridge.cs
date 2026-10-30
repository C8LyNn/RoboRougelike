// B01-62 P17/W15 -- ISOLATED Unity-host-facing transactional boundary.
// No schema duplication. Compiles against exact original B01-23 #556 + frozen #510 original R2 ledger/W06.
// NOT wired to #510 private Rig[]: approved host must implement IAtomicSaveHost in NEW version.
using System;
using System.IO;
using B01.DebrisRepulse001;
using B01.DetachedSaveContract001;
using B01.SecureReattachV2;

namespace B01.SaveHostBoundary556
{
    // The host implementation owns the physical roots and their effective lifetime.
    // Capture must read both physical rigs and the original R2 Contains(ID) atomically.
    // Prepare must allocate all bodies/parent/poses + newly created R2 ledgers OFFSTAGE.
    // Publishing must be one logical ownership switch; Rollback must restore old roots.
    // Neither port can access #510 private Rig[] without a separately authorized owner API.
    public interface IAtomicSaveHost
    {
        bool IsIdle();
        long CurrentRunId { get; }
        long MinimumGeneration { get; }
        CapturedWorld Capture();
        IPreparedWorld Prepare(RunSnapshot snapshot, AtomicDockLedger[] newLedgers);
    }
    public sealed class CapturedWorld
    {
        public RunSnapshot Snapshot { get; }
        private readonly bool[] originalConsumed;
        public CapturedWorld(RunSnapshot snapshot, bool lightConsumed, bool heavyConsumed)
        {
            Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
            originalConsumed = new[] {lightConsumed, heavyConsumed};
        }
        public bool ContainsConsumed(int index) => originalConsumed[index];
    }
    public interface IPreparedWorld
    {
        // Must not mutate the active world until Publish. On failure Rollback restores it.
        // No scene object Destroy() until successful publish + safe retirement of old world.
        void Publish();
        void Rollback();
    }
    public sealed class AtomicHostSaveBridge
    {
        private readonly IAtomicSaveHost host;
        private bool operation;
        private bool quarantined;
        private static readonly Vector3d Zero=new Vector3d(0,0,0);
        public bool Busy => operation;
        public bool Quarantined => quarantined;
        public AtomicHostSaveBridge(IAtomicSaveHost host)
        {this.host=host??throw new ArgumentNullException(nameof(host));}
        private bool Idle()
        {
            try{return !quarantined && host.IsIdle();}
            catch {return false;}
        }
        public bool TryCapture(out byte[] save)
        {
            save=Array.Empty<byte>();
            if(operation || quarantined)return false;
            operation=true;
            try
            {
                if(!Idle())return false;
                CapturedWorld captured=host.Capture();
                if(captured==null || !SnapshotRules.Valid(captured.Snapshot))return false;
                var snap=captured.Snapshot;
                if(snap.RunId!=host.CurrentRunId ||
                   snap.Generation<host.MinimumGeneration || host.MinimumGeneration<=0)return false;
                for(int i=0;i<FrozenPartCatalog.Slots;i++)
                {
                    PersistedPart p=snap.Parts[i];
                    if(captured.ContainsConsumed(i)!=p.Consumed ||
                       p.Id!=FrozenPartCatalog.Id(i))return false;
                }
                if(!Idle())return false;
                byte[] candidate=SnapshotCodec.Encode(snap); // exact ORIGINAL frozen #556 codec
                if(!Idle())return false;
                save=candidate;
                return true;
            }
            catch {return false;}
            finally {operation=false;}
        }
        // Write-new-slot only: an existing save or another owner file is NEVER overwritten.
        // Same-directory flush+move is a source-level crash-risk reduction,
        // not a verified power-loss durable directory fsync on every platform.
        public bool TrySaveNewFile(string path)
        {
            if(string.IsNullOrWhiteSpace(path) || operation || quarantined)return false;
            string temp="";
            bool retained=false;
            try
            {
                string full=Path.GetFullPath(path);
                string dir=Path.GetDirectoryName(full)!;
                if(!Directory.Exists(dir) || File.Exists(full) || Directory.Exists(full))
                    return false;
                if(!TryCapture(out byte[] bytes))return false;
                // Hold this boundary for the entire physical disk operation.
                operation=true;retained=true;
                temp=full+"."+Guid.NewGuid().ToString("N")+".atomic-tmp";
                using(var fs=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None))
                {
                    fs.Write(bytes,0,bytes.Length);
                    fs.Flush(true);
                }
                if(!Idle())return false;
                File.Move(temp,full); // fails if another owner created target after preflight
                return true;
            }
            catch{return false;}
            finally
            {
                if(retained)operation=false;
                if(temp.Length>0)
                {
                    try {if(File.Exists(temp))File.Delete(temp);}
                    catch {quarantined=true;}
                }
            }
        }
        public RestoreVerdict TryLoadFile(string path)
        {
            if(string.IsNullOrWhiteSpace(path))return RestoreVerdict.InvalidSnapshot;
            if(operation)return RestoreVerdict.TransactionBusy;
            if(quarantined)return RestoreVerdict.Quarantine;
            try
            {
                var info=new FileInfo(path);
                if(!info.Exists || info.Length==0 || info.Length>SnapshotCodec.MaxBytes)
                    return RestoreVerdict.InvalidSnapshot;
                return TryColdLoad(File.ReadAllBytes(path));
            }
            catch{return RestoreVerdict.InvalidSnapshot;}
        }
        public RestoreVerdict TryColdLoad(byte[] bytes)
        {
            if(quarantined)return RestoreVerdict.Quarantine;
            if(operation)return RestoreVerdict.TransactionBusy;
            operation=true;
            IPreparedWorld? ticket=null;
            bool published=false;
            try
            {
                if(!Idle())return RestoreVerdict.TransactionBusy;
                // Never accept unknown schema/foreign #510 SHA/checksum/candidate.
                if(!SnapshotCodec.TryDecode(bytes,out RunSnapshot? s)||s==null)
                    return RestoreVerdict.InvalidSnapshot;
                if(s.RunId!=host.CurrentRunId || host.CurrentRunId<=0)
                    return RestoreVerdict.WrongRun;
                if(host.MinimumGeneration<=0 || s.Generation<host.MinimumGeneration)
                    return RestoreVerdict.StaleGeneration;
                if(!ColdRestore.TryStage(bytes,out ColdRestore? logical)||logical==null)
                    return RestoreVerdict.InvalidSnapshot;
                // Prepare authentic ORIGINAL frozen #510 R2 AtomicDockLedger objects:
                // original #556 logical W06 cannot be substituted for physical R2 ledger.
                var ledgers=new AtomicDockLedger[FrozenPartCatalog.Slots];
                for(int i=0;i<ledgers.Length;i++)
                {
                    PersistedPart p=s.Parts[i];
                    var l=new AtomicDockLedger();
                    if(p.State==PartDockState.Detached)
                    {
                        if(!l.TryDetach(p.Id,Zero,Zero,Zero,p.MassKg,0,0,out Vector3d impulse) ||
                           impulse.MagnitudeSquared!=0 || l.Count!=1 || !l.Contains(p.Id) ||
                           !logical.ContainsConsumedId(i,p.Id))return RestoreVerdict.InvalidSnapshot;
                    }
                    else if(l.Count!=0 || l.Contains(p.Id) || logical.ConsumedCount(i)!=0)
                        return RestoreVerdict.InvalidSnapshot;
                    ledgers[i]=l;
                }
                // Host must allocate full new *physical* state without activating any root.
                // Do not give old mutable live rig to Prepare.
                ticket=host.Prepare(s,ledgers);
                if(ticket==null)return RestoreVerdict.Quarantine;
                if(!Idle())return RestoreVerdict.TransactionBusy;
                if(s.RunId!=host.CurrentRunId)return RestoreVerdict.WrongRun;
                if(s.Generation<host.MinimumGeneration)return RestoreVerdict.StaleGeneration;
                // Unity host ticket is exclusively responsible for reversible physical swap.
                ticket.Publish();
                published=true;
                return RestoreVerdict.Accepted;
            }
            catch {return RestoreVerdict.Quarantine;}
            finally
            {
                if(ticket!=null && !published)
                {
                    try {ticket.Rollback();}
                    catch {quarantined=true;}
                }
                operation=false;
            }
        }
    }
}
