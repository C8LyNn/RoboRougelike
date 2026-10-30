using System;
using System.IO;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;
using B01.SecureReattachV2;
using B01.UnitySaveBridge001;

internal static class Program
{
    private const long Run=61718;
    private static int pass,fail;
    private static void Check(string label, Func<bool> predicate)
    {
        try
        {
            if(predicate()){pass++;Console.WriteLine("PASS "+label);}
            else{fail++;Console.WriteLine("FAIL "+label);}
        }
        catch(Exception ex){fail++;Console.WriteLine("FAIL "+label+" "+ex);}
    }
    private static PersistedPart Part(int slot,bool detached)
    {
        var s=SecureRigCatalog.At(slot);
        var owner=FrozenPartCatalog.Owner(slot);
        return new PersistedPart(s.PartId,owner,detached?0:owner,
            FrozenPartCatalog.Asset(slot),FrozenPartCatalog.Socket(slot),
            detached?PartDockState.Detached:PartDockState.Attached,
            detached,!detached,
            new Vector3d(s.RootX+s.PartOffsetX,1.2+(detached?1:0),0),
            0,0,0,1,detached?new Vector3d(.25,0,0):new Vector3d(0,0,0),
            new Vector3d(0,0,0),s.PartMassKg);
    }
    private static byte[] Snapshot(long gen,bool light,bool heavy,long run=Run)
        => SnapshotCodec.Encode(new RunSnapshot(run,gen,
            new []{Part(0,light),Part(1,heavy)}));

    // Only the public IPreparedWorld abstraction is faked. Exact original
    // selected #596 transaction, file checkpoint store, selected #556 and W06
    // are DIRECTLY compiled as real C# (not ported/rewritten code).
    private sealed class World:IPreparedWorld
    {
        private readonly RunSnapshot saved;
        public bool Active,Retired,ThrowDeactivate,MutateAfterActivation;
        public World(RunSnapshot snapshot){saved=snapshot;}
        public RunSnapshot Capture()
        {
            if(!Active||!MutateAfterActivation)return saved;
            var parts=saved.Parts;
            parts[0]=parts[1];
            return new RunSnapshot(saved.RunId,saved.Generation,parts);
        }
        public void Activate(){Active=true;}
        public void Deactivate(){Active=false;if(ThrowDeactivate)throw new IOException("cannot undo staged world");}
        public void Retire(){Retired=true;Active=false;}
    }
    private sealed class Factory:IWorldFactory
    {
        public World? Last;
        public int Preparations;
        public bool FailRollback,PostActivateMutation;
        public IPreparedWorld Prepare(RunSnapshot snapshot)
        {
            Preparations++;
            Last=new World(snapshot) {
                ThrowDeactivate=FailRollback,
                MutateAfterActivation=PostActivateMutation
            };
            FailRollback=PostActivateMutation=false;
            return Last;
        }
    }

    // Independent *adversarial* ICheckpointStore that delegates all actual
    // persistent storage operations to the REAL original FileCheckpointStore.
    // Simulates a disk/transport implementation that completed a checkpoint
    // publication but throws before returning acknowledgement to the caller.
    private sealed class FaultAfterDurableCommitStore:ICheckpointStore
    {
        public readonly FileCheckpointStore Real;
        public bool ThrowAfterCommitted, ReturnFalseWithoutCommit;
        public int DurableCommitCount;
        public FaultAfterDurableCommitStore(string path)
        {Real=new FileCheckpointStore(path);}
        public bool Read(long run,out long generation,out string hash)
            => Real.Read(run,out generation,out hash);
        public bool Commit(long run,long previous,string previousHash,long next,string hash)
        {
            if(ReturnFalseWithoutCommit)return false;
            bool ok=Real.Commit(run,previous,previousHash,next,hash);
            if(ok)DurableCommitCount++;
            if(ok&&ThrowAfterCommitted)throw new IOException("checkpoint durably committed, ACK missing");
            return ok;
        }
    }
    private static (AtomicWorldSwap swap,Factory factory,World old,
        FaultAfterDurableCommitStore store) Seed(string filename)
    {
        var store=new FaultAfterDurableCommitStore(filename);
        var factory=new Factory();
        var swap=new AtomicWorldSwap(factory,store);
        if(swap.TryLoad(Snapshot(10,true,false),Run,1,()=>true)
            !=WorldSwapVerdict.Accepted||factory.Last==null||
            !factory.Last.Active||store.DurableCommitCount!=1)
            throw new InvalidOperationException("seed source failed");
        return (swap,factory,factory.Last,store);
    }
    private static bool IDs(AtomicWorldSwap swap,bool light,bool heavy)=>
        swap.IsConsumed(0,FrozenPartCatalog.Id(0))==light&&
        swap.IsConsumed(1,FrozenPartCatalog.Id(1))==heavy;
    private static string NewSlot(string root,string name)
    {
        var dir=Path.Combine(root,name);
        Directory.CreateDirectory(dir);
        return Path.Combine(dir,"source-selected596.checkpoint");
    }
    private static bool ReadEpoch(FileCheckpointStore store,long run,long epoch)
        =>store.Read(run,out long g,out string _) && g==epoch;
    private static bool CanDecode(byte[] raw,long expected,bool light,bool heavy)
    {
        if(!SnapshotCodec.TryDecode(raw,out RunSnapshot? s)||s==null||
            s.Generation!=expected||s.RunId!=Run)return false;
        return s.Parts[0].Id==FrozenPartCatalog.Id(0)&&
            s.Parts[1].Id==FrozenPartCatalog.Id(1)&&
            s.Parts[0].Consumed==light&&s.Parts[1].Consumed==heavy;
    }
    private static bool LiveAndOld(AtomicWorldSwap swap,World old,long gen)
        =>swap.HasWorld&&swap.Generation==gen&&old.Active&&!old.Retired;

    public static int Main(string[] args)
    {
        var root=Path.Combine(Path.GetTempPath(),"b01-61-w18-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            Check("BASELINE_REAL_596_OS_CHECKPOINT_AND_ORIGINAL_W06_LIGHT",()=>{
                var(s,_,old,store)=Seed(NewSlot(root,"base"));
                return LiveAndOld(s,old,10)&&IDs(s,true,false)&&
                    ReadEpoch(store.Real,Run,10);
            });
            Check("NORMAL_NEW_EPOCH_COMMITS_REAL_FILE_AND_HEAVY_LEDGER",()=>{
                var path=NewSlot(root,"normal");
                var(s,f,old,store)=Seed(path);
                var v=s.TryLoad(Snapshot(11,false,true),Run,11,()=>true);
                return v==WorldSwapVerdict.Accepted&&old.Retired&&
                    f.Last!=old&&f.Last!.Active&&s.Generation==11&&
                    IDs(s,false,true)&&ReadEpoch(store.Real,Run,11);
            });
            Check("SAME_GENERATION_FORK_DENIED_BEFORE_BUILD",()=>{
                var(s,f,old,store)=Seed(NewSlot(root,"fork"));
                int count=f.Preparations;
                var v=s.TryLoad(Snapshot(10,false,true),Run,10,()=>true);
                return v==WorldSwapVerdict.StaleGeneration&&f.Preparations==count&&
                    LiveAndOld(s,old,10)&&IDs(s,true,false)&&
                    ReadEpoch(store.Real,Run,10);
            });
            Check("POST_ACTIVATION_ID_MISMATCH_ROLLS_BACK_BEFORE_DISK",()=>{
                var(s,f,old,store)=Seed(NewSlot(root,"post"));
                f.PostActivateMutation=true;
                var v=s.TryLoad(Snapshot(11,false,true),Run,11,()=>true);
                return v==WorldSwapVerdict.HostException&&LiveAndOld(s,old,10)&&
                    !s.Quarantined&&IDs(s,true,false)&&f.Last!.Retired&&
                    ReadEpoch(store.Real,Run,10);
            });
            Check("FAILED_PHYSICAL_ROLLBACK_STICKY_QUARANTINE_FIRST_CALL",()=>{
                var(s,f,old,store)=Seed(NewSlot(root,"abort"));
                f.PostActivateMutation=true;f.FailRollback=true;
                var v=s.TryLoad(Snapshot(11,false,true),Run,11,()=>true);
                return v==WorldSwapVerdict.RollbackQuarantine&&s.Quarantined&&
                    s.TryLoad(Snapshot(12,true,true),Run,12,()=>true)
                        ==WorldSwapVerdict.RollbackQuarantine&&
                    !s.TrySave(Run,()=>true,out _)&&
                    old.Active&&ReadEpoch(store.Real,Run,10);
            });
            Check("COMMIT_EXPLICIT_FALSE_QUARANTINES_AND_PRESERVES_OLD",()=>{
                var(s,_,old,store)=Seed(NewSlot(root,"false"));
                store.ReturnFalseWithoutCommit=true;
                var v=s.TryLoad(Snapshot(11,false,true),Run,11,()=>true);
                return v==WorldSwapVerdict.RollbackQuarantine&&s.Quarantined&&
                    old.Active&&ReadEpoch(store.Real,Run,10);
            });
            bool uncertainRed=false;
            Check("RED_DURABLE_COMMIT_SUCCEEDED_BUT_THREW_HOST_NOT_QUARANTINED",()=>{
                var(s,f,old,store)=Seed(NewSlot(root,"throw-post"));
                store.ThrowAfterCommitted=true;
                var v=s.TryLoad(Snapshot(11,false,true),Run,11,()=>true);
                uncertainRed=v==WorldSwapVerdict.HostException&&
                    !s.Quarantined&&LiveAndOld(s,old,10)&&IDs(s,true,false)&&
                    f.Last!.Retired&&ReadEpoch(store.Real,Run,11)&&
                    store.DurableCommitCount==2;
                return uncertainRed;
            });
            bool staleBlocked=false;
            Check("R3_STALE_OLD_WORLD_CANNOT_EXPORT_AFTER_DURABLE_EXCEPTION",()=>{
                var path=NewSlot(root,"throw-promote");
                var(s,_,old,store)=Seed(path);
                store.ThrowAfterCommitted=true;
                if(s.TryLoad(Snapshot(11,false,true),Run,11,()=>true)
                    !=WorldSwapVerdict.HostException)return false;
                if(!old.Active||s.Quarantined||!ReadEpoch(store.Real,Run,11))return false;
                bool ok=s.TrySave(Run,()=>true,out byte[] pending);
                staleBlocked=!ok&&pending==null&&
                    !s.Quarantined&&s.Generation==10;
                return staleBlocked;
            });
            Check("COLD_FRESH_INSTANCE_REFUSES_STALE_AFTER_DURABLE_COMMIT_EXCEPTION",()=>{
                string path=NewSlot(root,"cold");
                var(s,_,old,store)=Seed(path);store.ThrowAfterCommitted=true;
                s.TryLoad(Snapshot(11,false,true),Run,11,()=>true);
                var f=new Factory();
                var cold=new AtomicWorldSwap(f,new FileCheckpointStore(path));
                var v=cold.TryLoad(Snapshot(10,true,false),Run,1,()=>true);
                return v==WorldSwapVerdict.StaleGeneration&&
                    f.Preparations==0&&!cold.HasWorld&&old.Active;
            });
            Check("WRONG_RUN_REJECTED_WITHOUT_WRITING_FILE",()=>{
                var(s,f,old,store)=Seed(NewSlot(root,"foreign-run"));
                int count=f.Preparations;
                var v=s.TryLoad(Snapshot(11,false,true,Run+1),Run,1,()=>true);
                return v==WorldSwapVerdict.WrongRun&&f.Preparations==count&&
                    LiveAndOld(s,old,10)&&ReadEpoch(store.Real,Run,10);
            });
            Check("BROKEN_BINARY_REJECTED_WITHOUT_WRITING_FILE",()=>{
                var(s,f,old,store)=Seed(NewSlot(root,"broken"));
                byte[] data=Snapshot(11,false,true);data[7]^=0x10;
                int count=f.Preparations;
                var v=s.TryLoad(data,Run,1,()=>true);
                return v==WorldSwapVerdict.InvalidSnapshot&&
                    f.Preparations==count&&LiveAndOld(s,old,10)&&
                    ReadEpoch(store.Real,Run,10);
            });
            Check("FRESH_FILE_LOCK_CAS_RACE_REFUSES_STALE_SECOND_WRITER",()=>{
                string path=NewSlot(root,"cas");
                var(s,_,_,store)=Seed(path);
                if(!store.Real.Read(Run,out long gen,out string digest)||gen!=10)
                    return false;
                var other=new FileCheckpointStore(path);
                const string first="aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
                const string second="bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
                return other.Commit(Run,10,digest,11,first)&&
                    !store.Real.Commit(Run,10,digest,11,second)&&
                    store.Real.Read(Run,out long actual,out string now)&&
                    actual==11&&now==first&&s.Generation==10;
            });
            Check("EXPLICIT_HOST_BUSY_DENIED_BEFORE_STAGING",()=>{
                var(s,f,old,store)=Seed(NewSlot(root,"busy"));
                int staged=f.Preparations;
                var v=s.TryLoad(Snapshot(11,false,true),Run,11,()=>false);
                return v==WorldSwapVerdict.HostBusy&&f.Preparations==staged&&
                    LiveAndOld(s,old,10)&&ReadEpoch(store.Real,Run,10);
            });

            Check("R3_TWO_COORDINATORS_SAME_SLOT_STALE_WRITER_DENIED",()=>{
                var path=NewSlot(root,"two-hosts");
                var(a,_,old,store)=Seed(path);
                var b=new AtomicWorldSwap(new Factory(),new FileCheckpointStore(path));
                if(b.TryLoad(Snapshot(11,false,true),Run,11,()=>true)
                    !=WorldSwapVerdict.Accepted)return false;
                bool exported=a.TrySave(Run,()=>true,out byte[] pending);
                return !exported&&pending==null&&!a.Quarantined&&
                    a.Generation==10&&old.Active&&ReadEpoch(store.Real,Run,11)&&
                    !IDs(a,false,true)&&IDs(b,false,true);
            });
            Check("R3_CURRENT_DURABLE_OWNER_SAVES_NEXT_WITH_ORIGINAL_W06_IDS",()=>{
                var path=NewSlot(root,"owner-save");
                var(a,_,_,_store)=Seed(path);
                if(a.TryLoad(Snapshot(11,false,true),Run,11,()=>true)
                    !=WorldSwapVerdict.Accepted)return false;
                return a.TrySave(Run,()=>true,out byte[] output)&&
                    output!=null&&CanDecode(output,12,false,true);
            });
            Check("R3_UNIQUE_RUN_AND_GUID_SLOT_PATHS_NO_PATH_ESCAPE",()=>{
                string privateRoot=Path.Combine(root,"slots");
                Guid key1=Guid.Parse("d6d24449-7722-4e0a-bf27-ce9f7e8c097a");
                Guid key2=Guid.Parse("f3793e29-a10e-491c-a8e7-f784ddcabc1a");
                string a=CheckpointSlotPath.ForRun(privateRoot,Run,key1);
                string b=CheckpointSlotPath.ForRun(privateRoot,Run,key2);
                string c=CheckpointSlotPath.ForRun(privateRoot,Run+1,key1);
                bool wrong=false;
                try {CheckpointSlotPath.ForRun(privateRoot,Run,Guid.Empty);}
                catch(ArgumentException){wrong=true;}
                return a!=b&&b!=c&&a!=c&&wrong&&
                    a.StartsWith(privateRoot,StringComparison.Ordinal)&&
                    !a.Contains("..")&&a.EndsWith(".checkpoint",StringComparison.Ordinal);
            });

            Console.WriteLine("B01_61_ORIGINAL_R3_642_EXACT_SOURCE_EXECUTED=TRUE");
            Console.WriteLine("DURABLE_COMMIT_THROW_AFTER_REAL_WRITE_RED="+
                (uncertainRed?"REPRODUCED":"NOT_REPRODUCED"));
            Console.WriteLine("R3_STALE_WORLD_SAVE_BLOCK="+
                (staleBlocked?"PASS":"FAIL"));
            Console.WriteLine("NO_UNITY_PHYSX=NATIVE_G1_NOT_RUN LIVE_510_LEDGER=UNINTEGRATED G2=HOLD");
            Console.WriteLine("TOTAL "+(pass+fail)+" PASS "+pass+" FAIL "+fail);
            if(fail!=0)return 1;
            bool hold=uncertainRed;
            Console.WriteLine("SOURCE_RELEASE_GATE="+(hold?"HOLD_P0_RED":"NO_REPRODUCTION_REASSESS"));
            return args.Length==1&&args[0]=="--release"?(hold?3:0):0;
        }
        finally {Directory.Delete(root,true);}
    }
}
