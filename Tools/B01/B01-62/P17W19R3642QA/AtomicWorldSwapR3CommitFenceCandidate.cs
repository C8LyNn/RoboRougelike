using System;
using System.Security.Cryptography;
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


    // Stable, path-safe identity function shared by actual Unity MonoBehaviour and
    // executed source tests. GUIDs and positive run IDs cannot inject directories.
    public static class CheckpointSlotPath
    {
        public static string ForRun(string persistentRoot,long run,Guid slot)
        {
            if(string.IsNullOrWhiteSpace(persistentRoot)||run<=0||slot==Guid.Empty)
                throw new ArgumentException("Run and owned slot must be valid");
            return System.IO.Path.Combine(persistentRoot,"b0123_isolated_save_slots",
                "run_"+run.ToString(System.Globalization.CultureInfo.InvariantCulture)+
                "_slot_"+slot.ToString("N")+".checkpoint");
        }
    }

    public interface ICheckpointStore
    {
        bool Read(long run,out long generation,out string hash);
        bool Commit(long run,long previous,string previousHash,long next,string hash);
    }
    // Source-test-only authority; the Unity host MUST use FileCheckpointStore.
    public sealed class MemoryCheckpointStore : ICheckpointStore
    {
        private long run,gen;
        private string fingerprint="";
        public bool Read(long id,out long generation,out string hash)
        {generation=gen;hash=fingerprint;return run==0||run==id;}
        public bool Commit(long id,long prev,string prevHash,long next,string hash)
        {
            if(!Read(id,out long g,out string h)||g!=prev||h!=prevHash||next<=prev)return false;
            run=id;gen=next;fingerprint=hash;return true;
        }
    }
    // Demo-only durable revision sidecar, not another #556 binary Save schema.
    // Same-volume atomic replace with file flush; directory fsync not guaranteed.
    public sealed class FileCheckpointStore : ICheckpointStore
    {
        private readonly string file;
        public FileCheckpointStore(string path)
        {
            if(string.IsNullOrWhiteSpace(path))throw new ArgumentException("checkpoint path");
            file=System.IO.Path.GetFullPath(path);
        }
        private bool ReadCore(long run,out long generation,out string hash)
        {
            generation=0;hash="";
            if(!System.IO.File.Exists(file))return true;
            if((System.IO.File.GetAttributes(file)&System.IO.FileAttributes.ReparsePoint)!=0)return false;
            string data=System.IO.File.ReadAllText(file);
            string[] values=data.Split('|');
            if(values.Length!=4||values[0]!="B0123CHK2"||
               !long.TryParse(values[1],out long owner)||owner!=run||
               !long.TryParse(values[2],out long seq)||seq<=0||
               values[3].Length!=64)return false;
            generation=seq;hash=values[3];return true;
        }
        public bool Read(long run,out long generation,out string hash)
        {
            generation=0;hash="";
            try
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(file)!);
                using(var gate=new System.IO.FileStream(file+".lock",
                    System.IO.FileMode.OpenOrCreate,System.IO.FileAccess.ReadWrite,System.IO.FileShare.None))
                    return ReadCore(run,out generation,out hash);
            }
            catch{return false;}
        }
        public bool Commit(long run,long prev,string prevHash,long next,string hash)
        {
            if(run<=0||next<=prev||hash.Length!=64)return false;
            string temp=file+"."+Guid.NewGuid().ToString("N")+".tmp";
            try
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(file)!);
                using(var gate=new System.IO.FileStream(file+".lock",
                    System.IO.FileMode.OpenOrCreate,System.IO.FileAccess.ReadWrite,System.IO.FileShare.None))
                {
                    if(!ReadCore(run,out long g,out string h)||g!=prev||h!=prevHash)return false;
                    byte[] b=System.Text.Encoding.ASCII.GetBytes("B0123CHK2|"+run+"|"+next+"|"+hash);
                    using(var f=new System.IO.FileStream(temp,System.IO.FileMode.CreateNew,
                        System.IO.FileAccess.Write,System.IO.FileShare.None))
                    {f.Write(b,0,b.Length);f.Flush(true);}
                    if(System.IO.File.Exists(file))System.IO.File.Replace(temp,file,null);
                    else System.IO.File.Move(temp,file);
                    return true;
                }
            }
            catch{return false;}
            finally{try{if(System.IO.File.Exists(temp))System.IO.File.Delete(temp);}catch{}}
        }
    }
    public sealed class AtomicWorldSwap
    {
        private readonly IWorldFactory factory;
        private readonly ICheckpointStore checkpoint;
        private long saveGeneration;
        private long publishedGeneration;
        private string publishedHash="";
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

        public AtomicWorldSwap(IWorldFactory factory):this(factory,new MemoryCheckpointStore()){}
        public AtomicWorldSwap(IWorldFactory factory,ICheckpointStore checkpoint)
        {
            this.factory=factory??throw new ArgumentNullException(nameof(factory));
            this.checkpoint=checkpoint??throw new ArgumentNullException(nameof(checkpoint));
        }
        private static string Hash(byte[] bytes)
        {
            using(var sha=SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();
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
                if(!checkpoint.Read(expectedRun,out long durable,out string diskHash))
                    return false;
                // W18: never mint a newer checkpoint from a physical world
                // invalidated by another owner advancing this durable slot.
                if(durable!=publishedGeneration||diskHash!=publishedHash||
                   currentCold.Snapshot.Generation!=publishedGeneration)
                    return false;
                long newest=Math.Max(Math.Max(currentCold.Snapshot.Generation,saveGeneration),durable);
                if(newest==long.MaxValue)return false;
                long next=newest+1;
                byte[] pending=SnapshotCodec.Encode(new RunSnapshot(snap.RunId,next,snap.Parts));
                if(!Idle(hostIdle))return false;
                saveGeneration=next;
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
            bool touchedOld=false,touchedNew=false,published=false;
            // R3 W19 source-only correction: Commit may durably write THEN throw.
            // Any exception after an attempted commit makes old authority uncertain.
            bool durabilityAttempted=false;
            WorldSwapVerdict verdict=WorldSwapVerdict.HostException;
            try
            {
                do
                {
                    if(!Idle(hostIdle)){verdict=WorldSwapVerdict.HostBusy;break;}
                    if(!ColdRestore.TryStage(bytes,out ColdRestore? ready)||ready==null)
                    {verdict=WorldSwapVerdict.InvalidSnapshot;break;}
                    RunSnapshot snap=ready.Snapshot;
                    if(expectedRun<=0||snap.RunId!=expectedRun)
                    {verdict=WorldSwapVerdict.WrongRun;break;}
                    if(minimumGeneration<=0||snap.Generation<minimumGeneration||
                       (currentCold!=null&&snap.Generation<=currentCold.Snapshot.Generation))
                    {verdict=WorldSwapVerdict.StaleGeneration;break;}
                    if(!checkpoint.Read(expectedRun,out long durable,out string digest))
                    {quarantined=true;verdict=WorldSwapVerdict.RollbackQuarantine;break;}
                    string fingerprint=Hash(bytes);
                    if(snap.Generation<durable||
                       (snap.Generation==durable&&(digest!=fingerprint||current!=null)))
                    {verdict=WorldSwapVerdict.StaleGeneration;break;}
                    bool replay=snap.Generation==durable;
                    bool invalid=false;
                    for(int i=0;i<FrozenPartCatalog.Slots;i++)
                    {
                        var p=snap.Parts[i];
                        if(ready.ContainsConsumedId(i,p.Id)!=p.Consumed||
                           ready.ConsumedCount(i)!=(p.Consumed?1:0))invalid=true;
                    }
                    if(invalid){verdict=WorldSwapVerdict.InvalidSnapshot;break;}
                    candidate=factory.Prepare(snap);
                    if(candidate==null)throw new InvalidOperationException("empty world");
                    if(!Consistent(candidate.Capture(),snap))
                        throw new InvalidOperationException("pre-activation physics/ID mismatch");
                    if(!Idle(hostIdle)){verdict=WorldSwapVerdict.HostBusy;break;}
                    if(old!=null){touchedOld=true;old.Deactivate();}
                    touchedNew=true;candidate.Activate();
                    // #572: physical state can mutate as a side effect of Activate().
                    if(!Consistent(candidate.Capture(),snap))
                        throw new InvalidOperationException("post-activation physics/ID mismatch");
                    if(!replay)
                    {
                        // Arm before external callback. Even a successful durable write
                        // can lose its acknowledgement or reenter/throw unpredictably.
                        durabilityAttempted=true;
                        if(!checkpoint.Commit(expectedRun,durable,digest,snap.Generation,fingerprint))
                        {
                            quarantined=true;verdict=WorldSwapVerdict.RollbackQuarantine;break;
                        }
                    }
                    if(!Consistent(candidate.Capture(),snap))
                    {
                        quarantined=true;verdict=WorldSwapVerdict.RollbackQuarantine;break;
                    }
                    current=candidate;currentCold=ready;
                    publishedGeneration=snap.Generation;
                    publishedHash=fingerprint;
                    saveGeneration=Math.Max(saveGeneration,snap.Generation);
                    candidate=null;published=true;
                    verdict=WorldSwapVerdict.Accepted;
                    if(old!=null)
                    {
                        try{old.Retire();}
                        catch
                        {
                            cleanupPending=true;quarantined=true;
                            verdict=WorldSwapVerdict.RollbackQuarantine;
                        }
                    }
                }while(false);
            }
            catch
            {
                // Rollback physical world may happen in finally, but the old
                // logical world must NEVER mint Save bytes after uncertain disk
                // advancement. Quarantine is sticky until an approved recovery.
                if(durabilityAttempted)
                {
                    quarantined=true;
                    verdict=WorldSwapVerdict.RollbackQuarantine;
                }
                else verdict=WorldSwapVerdict.HostException;
            }
            finally
            {
                if(!published)
                {
                    bool safe=true;
                    if(candidate!=null)
                    {
                        try{if(touchedNew)candidate.Deactivate();}catch{safe=false;}
                        try{candidate.Retire();}catch{safe=false;}
                    }
                    if(touchedOld&&old!=null)
                    {
                        try{old.Activate();}catch{safe=false;}
                    }
                    if(!safe)
                    {
                        quarantined=true;
                        verdict=WorldSwapVerdict.RollbackQuarantine; // #582 after cleanup
                    }
                }
                busy=false;
            }
            return verdict;
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
