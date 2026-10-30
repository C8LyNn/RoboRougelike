using System;
using B01.DetachedSaveContract001;

namespace B01.UnitySaveBridge001
{
    // Host-agnostic transaction core. Uses EXACT original selected #556
    // schema/ColdRestore, not a copied codec or alternate Save format.
    public enum WorldSwapVerdict
    {
        Accepted, InvalidSnapshot, WrongRun, StaleGeneration, HostBusy,
        HostException, RollbackQuarantine
    }
    public interface IPreparedWorld
    {
        // New world must remain entirely inactive until Activate().
        RunSnapshot Capture();
        void Activate();
        void Deactivate();
        // Retire inactive old world. May be deferred; never reactivate it.
        void Retire();
    }
    public interface IWorldFactory
    {
        // Must clean up all partial GameObjects if Prepare throws.
        IPreparedWorld Prepare(RunSnapshot snapshot);
    }
    public sealed class AtomicWorldSwap
    {
        private readonly IWorldFactory factory;
        private readonly ICheckpointFence checkpoint;
        private IPreparedWorld? current;
        private ColdRestore? currentCold;
        private bool busy, quarantined, cleanupPending;
        public bool Busy => busy;
        public bool Quarantined => quarantined;
        public bool CleanupPending => cleanupPending;
        public bool HasWorld => current != null;
        public long Generation => currentCold == null ? 0 : currentCold.Snapshot.Generation;
        public bool IsConsumed(int slot,long id) =>
            currentCold != null && currentCold.ContainsConsumedId(slot,id);

        // Default uses an explicitly volatile fence for the isolated demo ONLY.
        // Production must inject a durable ICheckpointFence bound to the save slot.
        public AtomicWorldSwap(IWorldFactory factory)
            : this(factory,new VolatileCheckpointFence()) {}
        public AtomicWorldSwap(IWorldFactory factory,ICheckpointFence checkpoint)
        {
            this.factory=factory??throw new ArgumentNullException(nameof(factory));
            this.checkpoint=checkpoint??throw new ArgumentNullException(nameof(checkpoint));
        }
        public bool DurableFence => checkpoint.IsDurable;

        private static bool Idle(Func<bool> callback)
        {
            try{return callback();}
            catch{return false;}
        }

        // Fail-closed SAVE only. No file write/rename and no access to frozen #510
        // private Rig/AtomicDockLedger. Host must gate all its state mutators while Busy.
        public bool TrySave(long expectedRun, Func<bool> hostIdle,out byte[] bytes)
        {
            bytes=null!;
            if(busy||quarantined||current==null||currentCold==null||
               expectedRun<=0||hostIdle==null)return false;
            busy=true;
            try
            {
                if(!Idle(hostIdle))return false;
                RunSnapshot snap=current.Capture();
                if(!SnapshotRules.Valid(snap)||snap.RunId!=expectedRun||
                   snap.Generation!=currentCold.Snapshot.Generation)return false;
                // Motion legitimately changes detached world poses/velocities after Load.
                // Only immutable identity, authority and detached/consumed coupling must match.
                if(!SameIdentityAndLedger(snap,currentCold.Snapshot))return false;
                byte[] pending=SnapshotCodec.Encode(snap);
                // A durable fence cannot authorize a physically changed
                // checkpoint with SAME generation. Host must version + durably
                // write a new checkpoint via an owner-approved save transaction.
                if(checkpoint.IsDurable&&checkpoint.Check(snap.RunId,
                    snap.Generation,CheckpointHash.Of(pending),true)!=FenceVerdict.Allowed)
                    return false;
                if(!Idle(hostIdle))return false;
                bytes=pending;
                return true;
            }
            catch {return false;}
            finally {busy=false;}
        }

        public WorldSwapVerdict TryLoad(byte[] bytes,long expectedRun,
            long minimumGeneration,Func<bool> hostIdle)
        {
            if(quarantined)return WorldSwapVerdict.RollbackQuarantine;
            if(busy)return WorldSwapVerdict.HostBusy;
            if(hostIdle==null)return WorldSwapVerdict.HostException;
            busy=true;
            IPreparedWorld? candidate=null;
            IPreparedWorld? old=current;
            bool touchedOld=false,touchedNew=false,published=false,authoritySealAttempted=false;
            WorldSwapVerdict verdict=WorldSwapVerdict.HostException;
            try
            {
                // No early return: an error from Abort/Retire/old Reactivate must
                // override the outgoing verdict to QUARANTINE.
                if(!Idle(hostIdle))verdict=WorldSwapVerdict.HostBusy;
                else if(!ColdRestore.TryStage(bytes,out ColdRestore? ready)||ready==null)
                    verdict=WorldSwapVerdict.InvalidSnapshot;
                else if(expectedRun<=0||ready.Snapshot.RunId!=expectedRun)
                    verdict=WorldSwapVerdict.WrongRun;
                else if(minimumGeneration<=0||ready.Snapshot.Generation<minimumGeneration||
                    (currentCold!=null&&ready.Snapshot.Generation<=currentCold.Snapshot.Generation))
                    // Same-run same-generation replay is not allowed on a LIVE world,
                    // even if exact bytes are repeated: prevents divergent worlds.
                    verdict=WorldSwapVerdict.StaleGeneration;
                else
                {
                    RunSnapshot snapshot=ready.Snapshot;
                    bool valid=true;
                    for(int i=0;i<FrozenPartCatalog.Slots;i++)
                    {
                        PersistedPart part=snapshot.Parts[i];
                        if(ready.ContainsConsumedId(i,part.Id)!=part.Consumed ||
                           ready.ConsumedCount(i)!=(part.Consumed?1:0))
                            valid=false;
                    }
                    if(!valid)verdict=WorldSwapVerdict.InvalidSnapshot;
                    else
                    {
                        // Source/checksum validation must precede durable fence check.
                        FenceVerdict fenceVerdict=checkpoint.Check(snapshot.RunId,
                            snapshot.Generation,CheckpointHash.Of(bytes),old==null);
                        if(fenceVerdict==FenceVerdict.Stale ||
                           fenceVerdict==FenceVerdict.Conflict)
                            verdict=WorldSwapVerdict.StaleGeneration;
                        else if(fenceVerdict!=FenceVerdict.Allowed)
                        {
                            quarantined=true;
                            verdict=WorldSwapVerdict.RollbackQuarantine;
                        }
                        else
                        {
                            candidate=factory.Prepare(snapshot);
                            if(candidate==null)throw new InvalidOperationException("Missing candidate");
                            if(!Consistent(candidate.Capture(),snapshot))
                                throw new InvalidOperationException("Inactive stage readback differs");
                            if(!Idle(hostIdle))verdict=WorldSwapVerdict.HostBusy;
                            else
                            {
                                if(old!=null){touchedOld=true;old.Deactivate();}
                                touchedNew=true;
                                candidate.Activate();
                                // #572: activation itself can mutate owner/IDs/pose/velocities.
                                // Always verify activated actual world BEFORE publishing.
                                if(!Consistent(candidate.Capture(),snapshot))
                                    throw new InvalidOperationException("Activated world changed snapshot");
                                if(!Idle(hostIdle))verdict=WorldSwapVerdict.HostBusy;
                                else
                                {
                                    // A partial/throwing durable seal is ambiguous. Fail
                                    // closed with quarantine even if physical rollback works.
                                    authoritySealAttempted=true;
                                    checkpoint.Seal(snapshot.RunId,snapshot.Generation,
                                        CheckpointHash.Of(bytes));
                                    current=candidate;
                                    currentCold=ready;
                                    candidate=null;
                                    published=true;
                                    verdict=WorldSwapVerdict.Accepted;
                                    if(old!=null)
                                    {
                                        try{old.Retire();}
                                        catch{cleanupPending=true;}
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                verdict=WorldSwapVerdict.HostException;
                if(authoritySealAttempted)quarantined=true;
            }
            finally
            {
                if(!published)
                {
                    bool rollbackOk=true;
                    if(candidate!=null)
                    {
                        try{if(touchedNew)candidate.Deactivate();}
                        catch{rollbackOk=false;}
                        try{candidate.Retire();}catch{rollbackOk=false;}
                    }
                    if(touchedOld&&old!=null)
                        try{old.Activate();}catch{rollbackOk=false;}
                    if(!rollbackOk)quarantined=true;
                }
                busy=false;
            }
            // Never return ordinary REJECT after failed Abort/Retire/rollback.
            return quarantined?WorldSwapVerdict.RollbackQuarantine:verdict;
        }

        private static bool SameIdentityAndLedger(RunSnapshot a,RunSnapshot b)
        {
            if(!SnapshotRules.Valid(a)||!SnapshotRules.Valid(b)||
               a.RunId!=b.RunId||a.Generation!=b.Generation)return false;
            for(int i=0;i<FrozenPartCatalog.Slots;i++)
            {
                PersistedPart x=a.Parts[i], y=b.Parts[i];
                if(x.Id!=y.Id||x.OwnerId!=y.OwnerId||x.ParentId!=y.ParentId||
                   x.AssetGuid!=y.AssetGuid||x.SocketGuid!=y.SocketGuid||
                   x.State!=y.State||x.Consumed!=y.Consumed||
                   x.Kinematic!=y.Kinematic||!Close(x.MassKg,y.MassKg))return false;
            }
            return true;
        }
        // Strict identity/state equivalence (Unity float roundtrips allowed 1e-4);
        // checks original W06 consumed bit, parents, mass and all physical axes.
        public static bool Consistent(RunSnapshot a,RunSnapshot b)
        {
            if(!SnapshotRules.Valid(a)||!SnapshotRules.Valid(b)||
               a.RunId!=b.RunId||a.Generation!=b.Generation)return false;
            for(int i=0;i<FrozenPartCatalog.Slots;i++)
            {
                PersistedPart x=a.Parts[i],y=b.Parts[i];
                if(x.Id!=y.Id||x.OwnerId!=y.OwnerId||x.ParentId!=y.ParentId||
                   x.AssetGuid!=y.AssetGuid||x.SocketGuid!=y.SocketGuid||
                   x.State!=y.State||x.Consumed!=y.Consumed||
                   x.Kinematic!=y.Kinematic||!Close(x.MassKg,y.MassKg)||
                   !Close(x.Qx,y.Qx)||!Close(x.Qy,y.Qy)||!Close(x.Qz,y.Qz)||
                   !Close(x.Qw,y.Qw)||!Vec(x.Position,y.Position)||
                   !Vec(x.LinearVelocity,y.LinearVelocity)||
                   !Vec(x.AngularVelocity,y.AngularVelocity))return false;
            }
            return true;
        }
        private static bool Close(double a,double b) => Math.Abs(a-b)<=0.0001;
        private static bool Vec(B01.DebrisRepulse001.Vector3d a,
            B01.DebrisRepulse001.Vector3d b) =>
            Close(a.X,b.X)&&Close(a.Y,b.Y)&&Close(a.Z,b.Z);
    }
}
