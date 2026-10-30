using System;
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
    public interface ICanonicalRigFactory
    {
        // The original #556 ColdRestore plan contains genuine frozen W06 ledger.
        // Future owner-approved #510 port must create its own PRIVATE R2 ledger,
        // not expose the frozen one or use reflection.
        ICanonicalStagedWorld BuildInactive(RunSnapshot snapshot, ColdRestore ledgerPlan);
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
        private ICanonicalStagedWorld? active;
        private RunSnapshot? accepted;
        private bool busy, quarantined;
        public bool Quarantined => quarantined;
        public bool HasWorld => active != null;
        public long Generation => accepted?.Generation ?? 0;

        public CanonicalWorldGateway(ICanonicalMutationGate gate,
            ICanonicalRigFactory factory, ICanonicalCommitPort durable)
        {
            this.gate=gate??throw new ArgumentNullException(nameof(gate));
            this.factory=factory??throw new ArgumentNullException(nameof(factory));
            this.durable=durable??throw new ArgumentNullException(nameof(durable));
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
            if(busy||quarantined||active==null||accepted==null)return false;
            // Acquire the transaction guard BEFORE calling any user-provided
            // quiescence callback; that callback may re-enter Save or Load.
            busy=true;
            try
            {
                if(!Idle())return false;
                RunSnapshot now=active.Capture();
                if(!SnapshotRules.Valid(now)||now.RunId!=accepted.RunId||
                   now.Generation!=accepted.Generation||!Idle())return false;
                bytes=SnapshotCodec.Encode(now);return true;
            }
            catch{return false;}
            finally{busy=false;}
        }
        public CanonicalGateResult TryRestore(byte[] bytes,long expectedRun,long minimumGeneration)
        {
            if(quarantined)return CanonicalGateResult.RollbackQuarantine;
            if(busy)return CanonicalGateResult.HostBusy;
            // P0: the quiescence gate can synchronously re-enter this API.
            // Set busy BEFORE the first gate callback (which lives below).
            busy=true;
            ICanonicalStagedWorld? staged=null, prior=active;
            bool touchedPrior=false,touchedNew=false,published=false;
            bool uncertainDurableCommit=false;
            CanonicalGateResult result=CanonicalGateResult.HostException;
            try
            {
                do
                {
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
                    staged=factory.BuildInactive(snap,cold);
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
