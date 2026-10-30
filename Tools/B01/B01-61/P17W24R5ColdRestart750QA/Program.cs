#nullable enable
using System;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;
using B01.SecureReattachV2;
using B01.UnitySaveBridge001;

internal static class Independent750
{
    const long Run=41;
    static int passed,failed;
    static void Check(string name,Func<bool> fn)
    {try{if(fn()){passed++;Console.WriteLine("PASS "+name);}
      else{failed++;Console.WriteLine("FAIL "+name);}}
     catch(Exception ex){failed++;Console.WriteLine("FAIL "+name+" "+ex);}}
    static byte[] Snapshot(long run,long generation,bool light,bool heavy)
    {
        var parts=new PersistedPart[2];
        for(int i=0;i<2;i++)
        {
            var s=SecureRigCatalog.At(i);
            bool d=i==0?light:heavy;
            long owner=FrozenPartCatalog.Owner(i);
            parts[i]=new PersistedPart(s.PartId,owner,d?0:owner,
                FrozenPartCatalog.Asset(i),FrozenPartCatalog.Socket(i),
                d?PartDockState.Detached:PartDockState.Attached,d,!d,
                new Vector3d(s.RootX+s.PartOffsetX,d?2.2:1.2,0),
                0,0,0,1,d?new Vector3d(.25,0,0):new Vector3d(0,0,0),
                new Vector3d(0,0,0),s.PartMassKg);
        }
        return SnapshotCodec.Encode(new RunSnapshot(run,generation,parts));
    }
    sealed class World:IPreparedWorld
    {
        readonly RunSnapshot snapshot;
        public bool Active,Retired;
        public World(RunSnapshot s){snapshot=s;}
        public RunSnapshot Capture()=>snapshot;
        public void Activate(){Active=true;}
        public void Deactivate(){Active=false;}
        public void Retire(){Retired=true;Active=false;}
    }
    sealed class Factory:IWorldFactory
    {
        public World? Last;
        public int Built;
        public IPreparedWorld Prepare(RunSnapshot s)
        {Built++;return Last=new World(s);}
    }
    static bool F54()
    {
        var durable=new MemoryCheckpointStore();
        var f=new Factory();
        var first=new AtomicWorldSwap(f,durable);
        if(first.TryLoad(Snapshot(Run,4,true,false),Run,4,()=>true)!=
           WorldSwapVerdict.Accepted||first.Generation!=4||
           !first.IsConsumed(0,FrozenPartCatalog.Id(0))||
           first.IsConsumed(1,FrozenPartCatalog.Id(1)))return false;
        if(!first.TrySave(Run,()=>true,out byte[] saved)||
           !SnapshotCodec.TryDecode(saved,out RunSnapshot? snapshot)||snapshot==null||
           snapshot.Generation!=5||snapshot.RunId!=Run||
           !ColdRestore.TryStage(saved,out ColdRestore? cold)||cold==null||
           !cold.ContainsConsumedId(0,FrozenPartCatalog.Id(0))||
           cold.ContainsConsumedId(1,FrozenPartCatalog.Id(1)))return false;
        Console.WriteLine("W24_SELECTED_R5_690_ACTUAL_CORE_556_G4_SAVE_BYTES_G5=PASS");
        f.Last?.Retire();
        var nextFactory=new Factory();
        var restarted=new AtomicWorldSwap(nextFactory,durable);
        if(restarted.TryLoad(saved,Run,5,()=>true)!=WorldSwapVerdict.Accepted||
           restarted.Generation!=5||nextFactory.Built!=1||
           !restarted.IsConsumed(0,FrozenPartCatalog.Id(0))||
           restarted.IsConsumed(1,FrozenPartCatalog.Id(1))||
           nextFactory.Last==null||!nextFactory.Last.Active)return false;
        Console.WriteLine("W24_SELECTED_R5_690_COLD_RESTART_SAVED_G5_ACCEPTED=PASS");
        return restarted.TryLoad(Snapshot(Run,4,true,false),Run,4,()=>true)==
               WorldSwapVerdict.StaleGeneration&&restarted.Generation==5;
    }
    static bool Reentry()
    {
        var a=new AtomicWorldSwap(new Factory());
        int calls=0;
        bool ok=a.TryLoad(Snapshot(Run,4,true,false),Run,4,()=>{
            calls++;
            return a.TryLoad(Snapshot(Run,6,false,true),Run,6,()=>true)==
                   WorldSwapVerdict.HostBusy;
        })==WorldSwapVerdict.Accepted&&calls>=2;
        return ok&&a.Generation==4;
    }
    static bool InvalidRuns()
    {
        var a=new AtomicWorldSwap(new Factory());
        if(a.TryLoad(Snapshot(Run+1,4,true,false),Run,4,()=>true)!=
             WorldSwapVerdict.WrongRun)return false;
        byte[] bytes=Snapshot(Run,4,true,false);
        bytes[90]^=1;
        return a.TryLoad(bytes,Run,4,()=>true)==WorldSwapVerdict.InvalidSnapshot&&
               !a.HasWorld&&a.Generation==0;
    }
    static bool SnapshotMasks()
    {
        for(int i=0;i<4;i++)
        {
            bool light=(i&1)!=0,heavy=(i&2)!=0;
            if(!ColdRestore.TryStage(Snapshot(Run,5,light,heavy),
                    out ColdRestore? cold)||cold==null||
               cold.Snapshot.Generation!=5||
               cold.ContainsConsumedId(0,FrozenPartCatalog.Id(0))!=light||
               cold.ContainsConsumedId(1,FrozenPartCatalog.Id(1))!=heavy)
                return false;
        }
        return true;
    }
    static bool NoFork()
    {
        var durable=new MemoryCheckpointStore();
        var first=new AtomicWorldSwap(new Factory(),durable);
        if(first.TryLoad(Snapshot(Run,4,true,false),Run,4,()=>true)!=WorldSwapVerdict.Accepted)
            return false;
        var second=new AtomicWorldSwap(new Factory(),durable);
        return second.TryLoad(Snapshot(Run,4,false,true),Run,4,()=>true)==
                   WorldSwapVerdict.StaleGeneration&&!second.HasWorld;
    }
    public static int Main()
    {
        Check("ORIGINAL_556_W06_FOUR_ID_MASKS_CORRECT",SnapshotMasks);
        Check("ORIGINAL_R5_ATOMIC_WORLD_SWAP_GENERATION_4_TO_5_REAL_COLD",F54);
        Check("ORIGINAL_R5_REENTRANT_GATE_DENIED",Reentry);
        Check("ORIGINAL_R5_WRONG_RUN_CORRUPT_ORIGINAL_SNAPSHOT_DENIED",InvalidRuns);
        Check("ORIGINAL_R5_SAME_GENERATION_ALTERNATE_CONSUMED_ID_FORK_DENIED",NoFork);
        Console.WriteLine("TOTAL "+(passed+failed)+" PASS "+passed+" FAIL "+failed);
        Console.WriteLine("SOURCE_FIXTURE_750=CORRECT_GENERATION_5_ONLY NATIVE_UNITY_G1_NOT_RUN");
        return failed==0?0:1;
    }
}
