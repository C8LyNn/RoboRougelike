using System;
using System.IO;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;
using B01.SecureReattachV2;
using B01.UnitySaveBridge001;

internal static class Program
{
    private const long Run=61719;
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
            bool uncertainIncident=false;
            Check("W19_DURABLE_COMMIT_SUCCEEDED_THEN_THREW_EXACT_R3",()=>{
                var(swap,fac,old,store)=Seed(NewSlot(root,"throw-post"));
                store.ThrowAfterCommitted=true;
                var verdict=swap.TryLoad(Snapshot(11,false,true),Run,11,()=>true);
#if FIXED
                uncertainIncident=verdict==WorldSwapVerdict.RollbackQuarantine&&
                    swap.Quarantined&&swap.HasWorld&&
                    LiveAndOld(swap,old,10)&&IDs(swap,true,false)&&
                    fac.Last!.Retired&&ReadEpoch(store.Real,Run,11)&&
                    store.DurableCommitCount==2;
#else
                // Reproduce P0 on the REAL unchanged producer #642.
                // A successful source-test witness is RELEASE RED for original #642.
                uncertainIncident=verdict==WorldSwapVerdict.HostException&&
                    !swap.Quarantined&&LiveAndOld(swap,old,10)&&
                    IDs(swap,true,false)&&fac.Last!.Retired&&
                    ReadEpoch(store.Real,Run,11)&&store.DurableCommitCount==2;
#endif
                return uncertainIncident;
            });
            Check("W19_POST_COMMIT_OLD_WORLD_CANNOT_EXPORT_STALE_LIGHT_ID",()=>{
                var(swap,_,old,store)=Seed(NewSlot(root,"throw-no-promote"));
                store.ThrowAfterCommitted=true;
                var verdict=swap.TryLoad(Snapshot(11,false,true),Run,11,()=>true);
#if FIXED
                if(verdict!=WorldSwapVerdict.RollbackQuarantine||!swap.Quarantined)return false;
#else
                if(verdict!=WorldSwapVerdict.HostException||swap.Quarantined)return false;
#endif
                return old.Active&&ReadEpoch(store.Real,Run,11)&&
                    !swap.TrySave(Run,()=>true,out _);
            });
            Check("W19_R3_PER_RUN_GUID_CHECKPOINT_PATHS_ISOLATE_IDENTITY",()=>{
                var guid=Guid.Parse("aabbccdd-aabb-4444-8888-112233445566");
                var g2=Guid.Parse("01234567-0123-4567-89ab-abcdef012345");
                string a=CheckpointSlotPath.ForRun(root,Run,guid);
                string b=CheckpointSlotPath.ForRun(root,Run+1,guid);
                string c=CheckpointSlotPath.ForRun(root,Run,g2);
                return a!=b&&a!=c&&b!=c&&
                    a.Contains("run_"+Run+"_slot_")&&
                    !a.Contains("..")&&
                    Path.GetDirectoryName(a)==Path.GetDirectoryName(b);
            });
            Check("W19_R3_REJECT_EMPTY_GUID_AND_BAD_RUN",()=>{
                try{CheckpointSlotPath.ForRun(root,0,Guid.NewGuid());return false;}
                catch(ArgumentException){}
                try{CheckpointSlotPath.ForRun(root,Run,Guid.Empty);return false;}
                catch(ArgumentException){}
                return true;
            });
            Check("W19_R3_STALE_OLD_HOST_AFTER_ANOTHER_HOST_ADVANCES_CANNOT_SAVE",()=>{
                string path=NewSlot(root,"stale-host");
                var(swap,fac,old,store)=Seed(path);
                var other=new AtomicWorldSwap(new Factory(),new FileCheckpointStore(path));
                if(other.TryLoad(Snapshot(11,false,true),Run,11,()=>true)!=WorldSwapVerdict.Accepted)return false;
                return !swap.TrySave(Run,()=>true,out _)&&
                    LiveAndOld(swap,old,10)&&IDs(swap,true,false)&&
                    ReadEpoch(store.Real,Run,11)&&fac.Preparations==1;
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

            Console.WriteLine("B0162_W19_SELECTED642_REAL_ORIGINAL_SHA=22cec9888047994ae523cbe7bf1b0a483d64cec4");
#if FIXED
            Console.WriteLine("B0162_W19_R3_CORRECTED_COMMIT_AMBIGUITY=QUARANTINED");
#else
            Console.WriteLine("B0162_W19_R3_ORIGINAL_SOURCE_COMMIT_ACK_RED=REPRODUCED");
#endif
            Console.WriteLine("B0162_W19_SOURCE_ONLY_NATIVE_UNITY_NOT_RUN_CANONICAL_510_NOT_INTEGRATED");
            Console.WriteLine("B0162_W19_SOURCE_TOTAL "+(pass+fail)+" PASS "+pass+" FAIL "+fail);
            return fail==0&&uncertainIncident?0:1;
        }
        finally {Directory.Delete(root,true);}
    }
}
