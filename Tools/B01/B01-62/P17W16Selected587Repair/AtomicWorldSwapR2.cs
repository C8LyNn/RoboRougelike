using System;
using B01.DetachedSaveContract001;

namespace B01.W16Selected587Repair
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
    public enum AuthorityDecision { Reject, Replay, Advance }
    // Actual storage must survive process restart and reject same-generation forks.
    // No authoritative generation can be accepted using only in-memory Current.
    public interface IDurableGenerationAuthority
    {
        AuthorityDecision Evaluate(long run,long generation,byte[] exact556Bytes);
        bool TryCommit(long run,long generation,byte[] exact556Bytes);
    }
    public sealed class AtomicWorldSwap
    {
        private readonly IWorldFactory factory;
        private readonly IDurableGenerationAuthority authority;
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

        public AtomicWorldSwap(IWorldFactory factory, IDurableGenerationAuthority authority)
        {
            this.factory=factory??throw new ArgumentNullException(nameof(factory));
            this.authority=authority??throw new ArgumentNullException(nameof(authority));
        }

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
                if(!Idle(hostIdle))return false;
                bytes=pending;
                return true;
            }
            catch {return false;}
            finally {busy=false;}
        }

        // The final caller-visible result is decided AFTER compensation/cleanup.
        // A failed Abort/Retire/Reactivate MUST never escape as ordinary HostBusy/HostException.
        public WorldSwapVerdict TryLoad(byte[] bytes,long expectedRun,
            long minimumGeneration,Func<bool> hostIdle)
        {
            WorldSwapVerdict result=TryLoadCore(bytes,expectedRun,minimumGeneration,hostIdle);
            return quarantined?WorldSwapVerdict.RollbackQuarantine:result;
        }

        private WorldSwapVerdict TryLoadCore(byte[] bytes,long expectedRun,
            long minimumGeneration,Func<bool> hostIdle)
        {
            if(quarantined)return WorldSwapVerdict.RollbackQuarantine;
            if(busy)return WorldSwapVerdict.HostBusy;
            if(hostIdle==null)return WorldSwapVerdict.HostException;
            busy=true; // includes idle callbacks, original #556 decode, staging and publication
            IPreparedWorld? candidate=null;
            IPreparedWorld? old=current;
            bool touchedOld=false, touchedNew=false, published=false;
            try
            {
                if(!Idle(hostIdle))return WorldSwapVerdict.HostBusy;
                if(!ColdRestore.TryStage(bytes,out ColdRestore? restored)||restored==null)
                    return WorldSwapVerdict.InvalidSnapshot;
                RunSnapshot snapshot=restored.Snapshot;
                if(expectedRun<=0||snapshot.RunId!=expectedRun)return WorldSwapVerdict.WrongRun;
                if(minimumGeneration<=0||snapshot.Generation<minimumGeneration||
                   (currentCold!=null&&snapshot.Generation<=currentCold.Snapshot.Generation))
                    return WorldSwapVerdict.StaleGeneration;
                // Independent persisted authority is MANDATORY. A new host after restart
                // may replay ONLY the byte-identical last committed snapshot.
                // Same run+generation different bytes is a HARD fork rejection.
                AuthorityDecision disposition;
                try{disposition=authority.Evaluate(snapshot.RunId,snapshot.Generation,bytes);}
                catch{return WorldSwapVerdict.RollbackQuarantine;}
                if(disposition==AuthorityDecision.Reject)
                    return WorldSwapVerdict.StaleGeneration;
                if(disposition==AuthorityDecision.Replay && currentCold!=null)
                    return WorldSwapVerdict.StaleGeneration;
                for(int i=0;i<FrozenPartCatalog.Slots;i++)
                {
                    var part=snapshot.Parts[i];
                    if(restored.ContainsConsumedId(i,part.Id)!=part.Consumed ||
                       restored.ConsumedCount(i)!=(part.Consumed?1:0))
                        return WorldSwapVerdict.InvalidSnapshot;
                }

                // Create candidate inactive in isolation BEFORE touching old world.
                candidate=factory.Prepare(snapshot);
                if(candidate==null)throw new InvalidOperationException("Empty candidate");
                if(!Consistent(candidate.Capture(),snapshot))
                    throw new InvalidOperationException("Staging physics/identity mismatch");
                if(!Idle(hostIdle))return WorldSwapVerdict.HostBusy;
                // No await/thread switch; host controls all physical writers on main thread.
                if(old!=null){touchedOld=true;old.Deactivate();}
                touchedNew=true;
                candidate.Activate();
                if(!Consistent(candidate.Capture(),snapshot))
                    throw new InvalidOperationException("Activated world mismatch");
                if(!Idle(hostIdle))
                    throw new InvalidOperationException("Host became busy after activation");
                // The external authority persists run/epoch + exact #556 snapshot hash.
                // If durable authority fails or its outcome is uncertain, rollback the
                // physical candidate and quarantine: no false Accepted.
                if(disposition==AuthorityDecision.Advance)
                {
                    bool committed;
                    try {committed=authority.TryCommit(snapshot.RunId,snapshot.Generation,bytes);}
                    catch {quarantined=true;throw;}
                    if(!committed){quarantined=true;throw new InvalidOperationException("Durable epoch CAS failed");}
                }
                current=candidate;
                currentCold=restored;
                published=true;
                candidate=null;
                if(old!=null)
                {
                    try{old.Retire();}
                    catch{cleanupPending=true;} // retired world inactive: cleanup separately
                }
                return WorldSwapVerdict.Accepted;
            }
            catch
            {
                return WorldSwapVerdict.HostException;
            }
            finally
            {
                if(!published)
                {
                    bool rollbackOk=true;
                    if(candidate!=null)
                    {
                        // Deactivate even when Activate threw midway.
                        try {if(touchedNew)candidate.Deactivate();}catch{rollbackOk=false;}
                        try {candidate.Retire();}catch{rollbackOk=false;}
                    }
                    if(touchedOld&&old!=null)
                    {
                        try {old.Activate();}catch{rollbackOk=false;}
                    }
                    if(!rollbackOk)
                    {
                        quarantined=true;
                        // Quarantine overrides returned result via host's persistent flag.
                        // C# forbids return from finally; caller checks Quarantined.
                    }
                }
                busy=false;
            }
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
