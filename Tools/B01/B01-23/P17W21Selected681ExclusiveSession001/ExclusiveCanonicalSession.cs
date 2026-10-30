using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using B01.CanonicalWorldApi001;
using B01.DetachedSaveContract001;

namespace B01.Selected681LeaseSession001
{
    // W21 NEW VERSIONED installable ENTRYPOINT, not a mutation of frozen #681.
    // ICanonicalCommitPort still belongs to B01-62, not a second file authority.
    public interface IConfirmedPhysicalRetirement
    {
        // Must be true only after ALL private #510 old/new Rigidbody and R2
        // AtomicDockLedger worlds are physically destroyed, not merely inactive.
        bool RetireAllWorldsAndVerifyDestroyed();
    }

    public sealed class ExclusiveCanonicalSession : IDisposable
    {
        private static readonly List<ExclusiveCanonicalSession> live =
            new List<ExclusiveCanonicalSession>();
        private readonly int ownerThread=Thread.CurrentThread.ManagedThreadId;
        private readonly long run;
        private readonly Guid slot;
        private readonly ICanonicalCommitPort durable;
        private readonly IConfirmedPhysicalRetirement retirement;
        private readonly CanonicalWorldGateway inner;
        private FileStream? lease;
        private bool closed, poisoned;
        public bool Quarantined => poisoned||inner.Quarantined;
        public bool HasWorld => !closed&&inner.HasWorld;
        public long Generation => closed?0:inner.Generation;
        public string LeasePath { get; }

        public ExclusiveCanonicalSession(string privateRoot,long run,Guid slot,
            ICanonicalMutationGate gate, ICanonicalRigFactory factory,
            ICanonicalCommitPort durable,IConfirmedPhysicalRetirement retirement)
        {
            if(string.IsNullOrWhiteSpace(privateRoot)||run<=0||slot==Guid.Empty)
                throw new ArgumentException("Trusted private root, run and persistent slot required");
            this.run=run;this.slot=slot;
            this.durable=durable??throw new ArgumentNullException(nameof(durable));
            this.retirement=retirement??throw new ArgumentNullException(nameof(retirement));
            string directory=Path.Combine(Path.GetFullPath(privateRoot),"b0123_canonical_owner_leases");
            Directory.CreateDirectory(directory);
            LeasePath=Path.Combine(directory,"run_"+run.ToString(
                System.Globalization.CultureInfo.InvariantCulture)+"_slot_"+slot.ToString("N")+".owner");
            // Full-process owner-exclusive lock. The frozen bare #681 gateway is
            // not an admissible standalone host for final integration.
            lease=new FileStream(LeasePath,FileMode.OpenOrCreate,FileAccess.ReadWrite,
                FileShare.None);
            try{inner=new CanonicalWorldGateway(gate,factory,durable);}
            catch{lease.Dispose();lease=null;throw;}
            lock(live)live.Add(this); // GC must NEVER silently free an active authority
        }
        private bool Authorized =>
            !closed&&!poisoned&&lease!=null&&
            Thread.CurrentThread.ManagedThreadId==ownerThread;
        public CanonicalGateResult TryRestore(byte[] payload,long expectedRun,long minimum)
        {
            if(!Authorized)return Quarantined
                ?CanonicalGateResult.RollbackQuarantine:CanonicalGateResult.HostBusy;
            if(expectedRun!=run)return CanonicalGateResult.WrongRun;
            CanonicalGateResult result=inner.TryRestore(payload,expectedRun,minimum);
            if(inner.Quarantined)poisoned=true;
            return result;
        }
        public bool TryCaptureVerifiedPreview(out byte[] bytes)
        {
            bytes=Array.Empty<byte>();
            if(!Authorized||!inner.HasWorld||!inner.TryCapturePreview(out byte[] actual))
                return false;
            // Fail closed if an external backup/second writer changed the
            // same slot even though this gateway still has a physical world.
            if(!SnapshotCodec.TryDecode(actual,out RunSnapshot? snapshot)||
               snapshot==null||snapshot.RunId!=run)
            {poisoned=true;return false;}
            string digest;
            using(var sha=SHA256.Create())
                digest=BitConverter.ToString(sha.ComputeHash(actual))
                    .Replace("-","").ToLowerInvariant();
            try
            {
                if(!durable.Read(run,out long epoch,out string savedDigest)||
                   epoch!=snapshot.Generation||savedDigest!=digest)
                {poisoned=true;return false;}
            }
            catch{poisoned=true;return false;}
            bytes=actual;
            return true; // preview only; not durable Save
        }
        public bool TryCloseOnlyAfterAllOwnedWorldsGone()
        {
            if(closed)return true;
            if(Thread.CurrentThread.ManagedThreadId!=ownerThread)return false;
            // NEVER dispose the OS lease on an exception, missing owner proof,
            // frame-delayed Unity Destroy, ambiguous private ledger or orphan.
            bool certified;
            try{certified=retirement.RetireAllWorldsAndVerifyDestroyed();}
            catch{poisoned=true;return false;}
            if(!certified){poisoned=true;return false;}
            try{lease?.Dispose();}
            catch{poisoned=true;return false;}
            lease=null;closed=true;
            lock(live)live.Remove(this);
            return true;
        }
        public void Dispose(){TryCloseOnlyAfterAllOwnedWorldsGone();}
    }
}
