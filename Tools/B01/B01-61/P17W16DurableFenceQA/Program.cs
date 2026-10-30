// B01-61 W16: original newly authored B01-23 CheckpointFence & AtomicWorldSwap.
// Six SEPARATE OS processes run against an actual disposable filesystem fence.
// No fake checkpoint algorithms, no new Save schema; fake only IWorldFactory.
using System;
using System.IO;
using B01.UnitySaveBridge001;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;
using B01.SecureReattachV2;

internal sealed class World : IPreparedWorld
{
    public RunSnapshot Data;
    public bool Active, Retired, CorruptOnActivate, ThrowOnActivate, ThrowOnDeactivate, ThrowOnRetire;
    public int ActivateCount, DeactivateCount;
    public World(RunSnapshot data){Data=data;}
    public RunSnapshot Capture()
    {
        if(Retired)throw new InvalidOperationException("retired");
        if(CorruptOnActivate&&Active)
            return new RunSnapshot(Data.RunId,Data.Generation,new[]{Data.Parts[1],Data.Parts[0]});
        return Data;
    }
    public void Activate()
    {
        Active=true;ActivateCount++;
        if(ThrowOnActivate)throw new IOException("candidate partial activate");
    }
    public void Deactivate()
    {
        Active=false;DeactivateCount++;
        if(ThrowOnDeactivate)throw new IOException("rollback deactivation failure");
    }
    public void Retire()
    {
        if(ThrowOnRetire)throw new IOException("old world cleanup failure");
        Retired=true;Active=false;
    }
}
internal sealed class Factory : IWorldFactory
{
    public World Last=null!;
    public int Prepared;
    public bool CorruptOnActivate, ThrowOnActivate, ThrowOnDeactivate;
    public IPreparedWorld Prepare(RunSnapshot snap)
    {
        Prepared++;
        Last=new World(snap){
            CorruptOnActivate=CorruptOnActivate,
            ThrowOnActivate=ThrowOnActivate,ThrowOnDeactivate=ThrowOnDeactivate
        };
        return Last;
    }
}
internal static class Program
{
    const long RUN=77;
    static int assertions;
    static void Check(bool ok,string why)
    {
        assertions++;
        if(!ok)throw new Exception("INDEPENDENT_B0161_W16_TEST_FAIL "+why);
    }
    static PersistedPart P(int i,bool detached)
    {
        var zero=new Vector3d(0,0,0);
        return new PersistedPart(
            FrozenPartCatalog.Id(i),FrozenPartCatalog.Owner(i),
            detached?0:FrozenPartCatalog.Owner(i),
            FrozenPartCatalog.Asset(i),FrozenPartCatalog.Socket(i),
            detached?PartDockState.Detached:PartDockState.Attached,
            detached,!detached,new Vector3d(i+0.25,1.5,-.5),
            0,0,0,1,detached?new Vector3d(.25,.5,-.25):zero,
            zero,FrozenPartCatalog.Mass(i));
    }
    static byte[] Bytes(long gen,bool light,bool heavy)=>
        SnapshotCodec.Encode(new RunSnapshot(RUN,gen,new[]{P(0,light),P(1,heavy)}));
    static void Consumed(AtomicWorldSwap a,bool light,bool heavy,long gen,string label)
    {
        Check(a.HasWorld&&a.Generation==gen,label+" world generation");
        Check(a.IsConsumed(0,SecureRigCatalog.Light.PartId)==light&&
              a.IsConsumed(1,SecureRigCatalog.Heavy.PartId)==heavy,
              label+" true ORIGINAL W06 consumed Light/Heavy ID state");
    }
    static void Transaction()
    {
        var f=new Factory();
        var bridge=new AtomicWorldSwap(f); // intentionally explicit original VOLATILE demo default
        Check(!bridge.DurableFence,"demo default fence must NOT claim durable");
        Check(bridge.TryLoad(Bytes(15,true,false),RUN,15,()=>true)==WorldSwapVerdict.Accepted,
              "actual original W16 initial valid physical world staged+published");
        World old=f.Last;
        Consumed(bridge,true,false,15,"initial");
        Check(bridge.TryLoad(Bytes(15,false,true),RUN,15,()=>true)==WorldSwapVerdict.StaleGeneration,
              "W16 fixes W15 #588 divergent equal-gen live replacement");
        Check(f.Last==old&&old.Active&&!old.Retired&&f.Prepared==1,
              "equal-gen denial BEFORE new world Prepare, old not deactivated/retired");
        Consumed(bridge,true,false,15,"equal-gen rejection");
        Console.WriteLine("W16_ORIGINAL_SOURCE_FIX_588_EQUAL_GEN_CONFLICT_GREEN");
        f.CorruptOnActivate=true;
        var verdict=bridge.TryLoad(Bytes(16,false,true),RUN,15,()=>true);
        Check(verdict==WorldSwapVerdict.HostException,
              "original source after-activation world ID mutation rejected");
        Check(f.Last.Retired&&!f.Last.Active&&old.Active&&!old.Retired &&
              f.Last.DeactivateCount>0,
              "corrupted candidate retired; old world reactivated");
        Consumed(bridge,true,false,15,"postactivation rollback");
        Console.WriteLine("W16_ORIGINAL_SOURCE_FIX_572_POST_ACTIVATION_CHECK_GREEN");
        f.CorruptOnActivate=false;
        f.ThrowOnActivate=true;f.ThrowOnDeactivate=true;
        var fatal=bridge.TryLoad(Bytes(16,false,true),RUN,15,()=>true);
        Check(fatal==WorldSwapVerdict.RollbackQuarantine&&bridge.Quarantined,
              "FIRST failure with failed candidate rollback must return quarantine");
        Check(bridge.TryLoad(Bytes(17,false,false),RUN,15,()=>true)==WorldSwapVerdict.RollbackQuarantine,
              "quarantine sticky blocks subsequent load");
        Check(!bridge.TrySave(RUN,()=>true,out byte[] denied)&&denied==null,
              "quarantined original host refuses any Save");
        Check(old.Active&&!old.Retired&&bridge.IsConsumed(0,2310001),
              "original prior W06 ID retained behind quarantine");
        Console.WriteLine("W16_ORIGINAL_SOURCE_FIX_582_FIRST_VERDICT_QUARANTINE_GREEN");
        var secondFactory=new Factory();
        var second=new AtomicWorldSwap(secondFactory);
        Check(second.TryLoad(Bytes(20,true,false),RUN,20,()=>true)==WorldSwapVerdict.Accepted,
              "clean setup for old world deferred cleanup test");
        var retiring=secondFactory.Last;
        retiring.ThrowOnRetire=true;
        Check(second.TryLoad(Bytes(21,false,true),RUN,20,()=>true)==WorldSwapVerdict.Accepted,
              "world replacement accepted even when old Retire fails");
        Check(second.CleanupPending&&!retiring.Active&&!retiring.Retired &&
              secondFactory.Last.Active,
              "old world inactive but cleanup pending; no owner retry public interface");
        Consumed(second,false,true,21,"cleanup deferred");
        Console.WriteLine("W16_ORIGINAL_SOURCE_CLEANUP_PENDING_BOUNDARY=RETIRED_WORLD_NOT_DISPOSED");
        Console.WriteLine("W16_TRANSACTION_ORIGINAL_W16_QA_PASS assertions="+assertions);
    }
    static void Fence(string mode,string path)
    {
        path=Path.GetFullPath(path);
        var f=new Factory();
        var a=new AtomicWorldSwap(f,new FileCheckpointFence(path));
        Check(a.DurableFence,"real original FileCheckpointFence is durable storage delegate");
        if(mode=="seal")
        {
            Check(!File.Exists(path),"isolated scratch watermark absent before first Save");
            Check(a.TryLoad(Bytes(31,true,false),RUN,31,()=>true)==WorldSwapVerdict.Accepted,
                  "real W16 file-backed fence initial transaction accepted");
            Check(File.Exists(path)&&new FileInfo(path).Length==88,
                  "actual original durable fence SHA256 wire file is exactly 88 bytes");
            Consumed(a,true,false,31,"initial durable seal");
            Console.WriteLine("W16_DISK_FENCE_INITIAL_SEAL_PASS size=88");
        }
        else if(mode=="restart")
        {
            Check(File.Exists(path),"new process sees real saved fence from previous process");
            Check(a.TryLoad(Bytes(31,true,false),RUN,31,()=>true)==WorldSwapVerdict.Accepted,
                  "identical checkpoint hash cold Load permitted on new process with no world");
            Consumed(a,true,false,31,"actual cross-process restore");
            RunSnapshot parsed=null!;
            Check(a.TrySave(RUN,()=>true,out byte[] saved)&&saved!=null &&
                  SnapshotCodec.TryDecode(saved,out parsed)&&parsed!=null,
                  "actual immutable #556 codec file-fenced Save after cold restore");
            Check(parsed.Parts[0].Id==2310001&&parsed.Parts[1].Id==2310002,
                  "original #510 stable numeric IDs survive process restart");
            var record=f.Last.Data;
            var old=record.Parts[0];
            var parts=record.Parts;
            parts[0]=new PersistedPart(old.Id,old.OwnerId,old.ParentId,old.AssetGuid,
                old.SocketGuid,old.State,old.Consumed,old.Kinematic,
                new Vector3d(old.Position.X+1,old.Position.Y,old.Position.Z),
                old.Qx,old.Qy,old.Qz,old.Qw,old.LinearVelocity,old.AngularVelocity,old.MassKg);
            f.Last.Data=new RunSnapshot(RUN,31,parts);
            Check(!a.TrySave(RUN,()=>true,out byte[] conflict)&&conflict==null,
                  "durable checkpoint blocks edited physical position on SAME generation");
            Console.WriteLine("W16_FRESH_PROCESS_EXACT_HASH_RESTORE_AND_MOTION_FORK_REFUSAL_PASS");
        }
        else if(mode=="fork")
        {
            Check(a.TryLoad(Bytes(31,false,true),RUN,31,()=>true)==WorldSwapVerdict.StaleGeneration,
                  "valid but different same-run+generation hash rejected by actual file fence");
            Check(!a.HasWorld&&f.Prepared==0&&!a.Quarantined,
                  "durable fork denied before candidate creation and no false quarantine");
            Check(a.TryLoad(Bytes(30,true,false),RUN,30,()=>true)==WorldSwapVerdict.StaleGeneration,
                  "older generation refused even on fresh host with no world");
            Check(f.Prepared==0&&!a.HasWorld,"stale durable replay does not mutate a world");
            Console.WriteLine("W16_CROSS_PROCESS_SAME_GEN_HASH_FORK_AND_STALE_REPLAY_REFUSED");
        }
        else if(mode=="advance")
        {
            Check(a.TryLoad(Bytes(32,false,true),RUN,31,()=>true)==WorldSwapVerdict.Accepted,
                  "valid +1 generation committed via original durable Seal");
            Consumed(a,false,true,32,"new durable W06 state");
            Check(File.Exists(path)&&new FileInfo(path).Length==88,
                  "File.Replace maintains one bounded 88 byte original watermark");
            Console.WriteLine("W16_REAL_FILE_CHECKPOINT_ADVANCE_32_PASS");
        }
        else if(mode=="stale")
        {
            Check(a.TryLoad(Bytes(31,true,false),RUN,31,()=>true)==WorldSwapVerdict.StaleGeneration,
                  "previously valid checkpoint cannot be loaded on restart after newer file watermark");
            Check(!a.HasWorld&&f.Prepared==0,"old disk replay denied before world prepare");
            Console.WriteLine("W16_FRESH_PROCESS_PRIOR_GENERATION_REPLAY_REJECTED");
        }
        else if(mode=="corrupt")
        {
            byte[] data=File.ReadAllBytes(path);
            Check(data.Length==88,"corrupt only owned 88byte temporary file");
            data[45]^=0x10;
            File.WriteAllBytes(path,data); // QA scratch ONLY, never user save or Codex path
            var brokenFactory=new Factory();
            var broken=new AtomicWorldSwap(brokenFactory,new FileCheckpointFence(path));
            Check(broken.TryLoad(Bytes(33,true,false),RUN,31,()=>true)==WorldSwapVerdict.RollbackQuarantine,
                  "actual original W16 rejects corrupted watermark instead of starting world");
            Check(broken.Quarantined&&!broken.HasWorld&&brokenFactory.Prepared==0,
                  "unverifiable disk fence sets sticky quarantine before physical preparation");
            Console.WriteLine("W16_CORRUPTED_DURABLE_FENCE_REJECTED_QUARANTINE_PASS");
        }
        else throw new ArgumentException("unknown QA mode");
        Console.WriteLine("W16_EXACT_ORIGINAL_FENCE_QA_PROCESS_PASS mode="+mode+
           " assertions="+assertions+" native_unity=NOT_RUN");
    }
    public static int Main(string[] args)
    {
        try
        {
            if(args.Length==0)Transaction();
            else if(args.Length==2)Fence(args[0],args[1]);
            else throw new ArgumentException("usage: [mode absolute-fence-path]");
            Console.WriteLine("SOURCE_TEST_EXECUTED_ORIGINAL_PRODUCER_W16=YES");
            Console.WriteLine("REAL_UNITY_PHYSX=NOT_RUN CANONICAL_GAME_510=NOT_INTEGRATED");
            return 0;
        }
        catch(Exception e){Console.Error.WriteLine("B01_61_W16_INDEPENDENT_QA_FAILURE "+e);return 1;}
    }
}
