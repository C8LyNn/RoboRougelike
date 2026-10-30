using System;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using B01.DetachedSaveContract001;

namespace B01.CanonicalWorldApi001
{
    // P17-W20, owner-approved NEW VERSION / #666. No access to original #510
    // private Rig[] / AtomicDockLedger. A future separately approved game adapter
    // MUST implement these contracts directly (NO reflection).
    public enum CanonicalGateResult
    {
        Accepted, InvalidSnapshot, WrongRun, StaleGeneration, HostBusy,
        HostException, RollbackQuarantine
    }

    public interface ICanonicalMutationGate
    {
        // Must lock all real #510 detach/reattach, AI, GUI, and Unity physics writers.
        bool IsQuiescent();
    }
    public interface ICanonicalStagedWorld
    {
        // MUST observe real Rigidbody transforms/parents, velocity, private
        // #510 ledger consumed IDs and stable socket/asset identity.
        RunSnapshot Capture();
        // BuildInactive never installs an active collider or mutates old world.
        void Activate();
        void Deactivate();
        void Retire();
    }
    public interface ICanonicalStageOwnership
    {
        // The factory MUST return this ownable staging handle BEFORE creating
        // any Unity GameObject, Rigidbody, or private R2 ledger object.
        // PrepareInactive may then throw, but this handle continues to own
        // every partly constructed physical root, not just the final world.
        ICanonicalStagedWorld PrepareInactive();
        // Must CLEAN UP and VERIFY that no owned active/retired/deferred object
        // can be resurrected. false or exception -> permanent quarantine.
        bool CleanupUnpublishedAndVerifyDestroyed();
    }
    public interface ICanonicalRigFactory
    {
        // The original #556 ColdRestore plan contains genuine frozen W06 ledger.
        // BeginOwnedStage must only create an empty tracked staging handle:
        // NO physical objects may be allocated before returning it.
        ICanonicalStageOwnership BeginOwnedStage(RunSnapshot snapshot,
            ColdRestore ledgerPlan);
    }
    public interface ICanonicalCommitPort
    {
        // B01-62 owns full Save payload + revision crash recovery. These APIs
        // are external authority hooks, NOT proof of atomic two-file persistence.
        bool Read(long run, out long generation, out string hash);
        bool Commit(long run, long prior, string priorHash, long next, string hash);
    }

    public sealed class CanonicalWorldGateway
    {
        private readonly int mainThread = Thread.CurrentThread.ManagedThreadId;
        private readonly ICanonicalMutationGate gate;
        private readonly ICanonicalRigFactory factory;
        private readonly ICanonicalCommitPort durable;
        private readonly FileStream ownerLease;
        private readonly long ownerRun;
        private readonly Guid ownerSlot;
        private bool physicallyClosed;
        private static readonly HashSet<CanonicalWorldGateway> live =
            new HashSet<CanonicalWorldGateway>();
        private ICanonicalStagedWorld? active;
        private RunSnapshot? accepted;
        private bool busy, quarantined;
        public bool Quarantined => quarantined;
        public bool HasWorld => active != null;
        public long Generation => accepted?.Generation ?? 0;

        // W22 MAJOR OWNER API: bare three-argument gateways no longer exist.
        // The root MUST be a trusted, private owner-selected directory.
        public CanonicalWorldGateway(ICanonicalMutationGate gate,
            ICanonicalRigFactory factory, ICanonicalCommitPort durable,
            string trustedPrivateRoot,long run,Guid persistentSlot)
        {
            this.gate=gate??throw new ArgumentNullException(nameof(gate));
            this.factory=factory??throw new ArgumentNullException(nameof(factory));
            this.durable=durable??throw new ArgumentNullException(nameof(durable));
            if(run<=0||persistentSlot==Guid.Empty||string.IsNullOrWhiteSpace(trustedPrivateRoot))
                throw new ArgumentException("Trusted run, slot and root required");
            ownerRun=run;ownerSlot=persistentSlot;
            string dir=Path.Combine(Path.GetFullPath(trustedPrivateRoot),
                "b0123_canonical_authority_v3");
            Directory.CreateDirectory(dir);
            string file=Path.Combine(dir,"run_"+run.ToString(
                System.Globalization.CultureInfo.InvariantCulture)+"_slot_"+
                persistentSlot.ToString("N")+".owner");
            ownerLease=new FileStream(file,FileMode.OpenOrCreate,
                FileAccess.ReadWrite,FileShare.None);
            lock(live)live.Add(this); // prevent GC releasing active-world OS lease
        }
        public Guid SlotId => ownerSlot;
        public bool OwnerLeaseHeld => !physicallyClosed;
        public bool TryCloseAfterVerifiedRetirement(Func<bool> allOwnedWorldsDestroyed)
        {
            if(physicallyClosed)return true;
            if(busy||Thread.CurrentThread.ManagedThreadId!=mainThread||
               allOwnedWorldsDestroyed==null)return false;
            busy=true;
            try
            {
                // This must attest ALL partial + published private #510 roots;
                // merely HasWorld=false after Prepare failure is NOT enough.
                if(!allOwnedWorldsDestroyed()){quarantined=true;return false;}
                ownerLease.Dispose();
                physicallyClosed=true;active=null;accepted=null;
                lock(live)live.Remove(this);
                return true;
            }
            catch{quarantined=true;return false;}
            finally{busy=false;}
        }
        private bool Idle()
        {
            if(Thread.CurrentThread.ManagedThreadId!=mainThread)return false;
            try { return gate.IsQuiescent(); }
            catch { return false; }
        }
        private static bool Equivalent(RunSnapshot a, RunSnapshot b)
        {
            if(!SnapshotRules.Valid(a)||!SnapshotRules.Valid(b))return false;
            // Equality uses exact original #556 wire encoding, including
            // consumed bits, parent/socket/owner, pose and velocities.
            byte[] x=SnapshotCodec.Encode(a),y=SnapshotCodec.Encode(b);
            if(x.Length!=y.Length)return false;
            int diff=0;for(int i=0;i<x.Length;i++)diff|=x[i]^y[i];
            return diff==0;
        }
        private static string Fingerprint(byte[] bytes)
        {
            using(var sha=SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();
        }
        // SOURCE-ONLY export. NOT a committed game save; B01-62 has to provide
        // one recoverable snapshot+watermark atomic commit and generation bump.
        public bool TryCapturePreview(out byte[] bytes)
        {
            bytes=null!;
            if(busy||quarantined||physicallyClosed||active==null||accepted==null)return false;
            // Acquire the transaction guard BEFORE calling any user-provided
            // quiescence callback; that callback may re-enter Save or Load.
            busy=true;
            try
            {
                if(!Idle())return false;
                RunSnapshot now=active.Capture();
                if(!SnapshotRules.Valid(now)||now.RunId!=accepted.RunId||
                   now.Generation!=accepted.Generation||!Idle())return false;
                bytes=SnapshotCodec.Encode(now);
                // Read back the SAME durable authority before offering a preview.
                // External valid backup rollback or malicious bypass is detected
                // against the published generation and original #556 fingerprint.
                if(!durable.Read(ownerRun,out long epoch,out string digest)||
                   epoch!=accepted.Generation||
                   !string.Equals(digest,Fingerprint(bytes),StringComparison.Ordinal))
                {quarantined=true;bytes=null!;return false;}
                return true;
            }
            catch{return false;}
            finally{busy=false;}
        }
        public CanonicalGateResult TryRestore(byte[] bytes,long expectedRun,long minimumGeneration)
        {
            if(quarantined)return CanonicalGateResult.RollbackQuarantine;
            if(busy||physicallyClosed)return CanonicalGateResult.HostBusy;
            if(expectedRun!=ownerRun)return CanonicalGateResult.WrongRun;
            // P0: the quiescence gate can synchronously re-enter this API.
            // Set busy BEFORE the first gate callback (which lives below).
            busy=true;
            ICanonicalStageOwnership? ownedStage=null;
            ICanonicalStagedWorld? staged=null, prior=active;
            bool touchedPrior=false,touchedNew=false,published=false;
            bool uncertainDurableCommit=false;
            CanonicalGateResult result=CanonicalGateResult.HostException;
            try
            {
                do
                {
                    // The first quiescence check must be INSIDE the busy lease.
                    // Check before decoding, reading disk or staging new physics.
                    if(!Idle()){result=CanonicalGateResult.HostBusy;break;}
                    if(!ColdRestore.TryStage(bytes,out ColdRestore? cold)||cold==null)
                    {result=CanonicalGateResult.InvalidSnapshot;break;}
                    RunSnapshot snap=cold.Snapshot;
                    if(expectedRun<=0||snap.RunId!=expectedRun)
                    {result=CanonicalGateResult.WrongRun;break;}
                    if(minimumGeneration<=0||snap.Generation<minimumGeneration||
                       (accepted!=null&&snap.Generation<=accepted.Generation))
                    {result=CanonicalGateResult.StaleGeneration;break;}
                    if(!durable.Read(expectedRun,out long disk,out string hash))
                    {quarantined=true;result=CanonicalGateResult.RollbackQuarantine;break;}
                    string fp=Fingerprint(bytes);
                    if(snap.Generation<disk||
                       (snap.Generation==disk&&(hash!=fp||active!=null)))
                    {result=CanonicalGateResult.StaleGeneration;break;}
                    bool replay=snap.Generation==disk;
                    for(int i=0;i<FrozenPartCatalog.Slots;i++)
                    {
                        PersistedPart p=snap.Parts[i];
                        if(cold.ContainsConsumedId(i,p.Id)!=p.Consumed||
                           cold.ConsumedCount(i)!=(p.Consumed?1:0))
                        {result=CanonicalGateResult.InvalidSnapshot;break;}
                    }
                    if(result==CanonicalGateResult.InvalidSnapshot)break;
                    // The owner is returned BEFORE partial construction begins.
                    // If PrepareInactive throws after allocating even one child
                    // GameObject, finally still has an explicit cleanup handle.
                    ownedStage=factory.BeginOwnedStage(snap,cold);
                    if(ownedStage==null)
                        throw new InvalidOperationException("No tracked staging owner");
                    staged=ownedStage.PrepareInactive();
                    if(staged==null)throw new InvalidOperationException("Empty world stage");
                    if(!Equivalent(staged.Capture(),snap))
                        throw new InvalidOperationException("Private ledger/physics preflight mismatch");
                    if(!Idle()){result=CanonicalGateResult.HostBusy;break;}
                    if(prior!=null){touchedPrior=true;prior.Deactivate();}
                    touchedNew=true;staged.Activate();
                    if(!Equivalent(staged.Capture(),snap))
                        throw new InvalidOperationException("Private ledger/physics post-activation mismatch");
                    if(!replay)
                    {
                        uncertainDurableCommit=true; // BEFORE any external write
                        if(!durable.Commit(expectedRun,disk,hash,snap.Generation,fp))
                        {
                            quarantined=true;result=CanonicalGateResult.RollbackQuarantine;
                            break;
                        }
                    }
                    if(!Equivalent(staged.Capture(),snap))
                    {
                        quarantined=true;result=CanonicalGateResult.RollbackQuarantine;
                        break;
                    }
                    active=staged;accepted=snap;staged=null;published=true;
                    uncertainDurableCommit=false;
                    result=CanonicalGateResult.Accepted;
                    if(prior!=null)
                    {
                        try{prior.Retire();}
                        catch{quarantined=true;result=CanonicalGateResult.RollbackQuarantine;}
                    }
                }while(false);
            }
            catch
            {
                if(uncertainDurableCommit)
                {quarantined=true;result=CanonicalGateResult.RollbackQuarantine;}
                else result=CanonicalGateResult.HostException;
            }
            finally
            {
                if(!published)
                {
                    bool safe=true;
                    if(staged!=null)
                    {
                        try{if(touchedNew)staged.Deactivate();}catch{safe=false;}
                        try{staged.Retire();}catch{safe=false;}
                    }
                    // Even if PrepareInactive threw before returning staged,
                    // the separate owned handle MUST retire every partly built
                    // root and prove its physical destruction/quiescence.
                    if(ownedStage!=null)
                    {
                        try
                        {
                            if(!ownedStage.CleanupUnpublishedAndVerifyDestroyed())
                                safe=false;
                        }
                        catch{safe=false;}
                    }
                    if(touchedPrior&&prior!=null)
                    {
                        try{prior.Activate();}catch{safe=false;}
                    }
                    if(!safe)
                    {quarantined=true;result=CanonicalGateResult.RollbackQuarantine;}
                }
                busy=false;
            }
            return result;
        }
    }
}
