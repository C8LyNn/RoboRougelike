// Independent W19 R3 #642 actual unchanged original selected C# core.
using System;
using System.IO;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;
using B01.SecureReattachV2;
using B01.UnitySaveBridge001;

internal static class Program
{
    const long Run=61946;
    static int checks, red;
    static void Check(bool ok,string what)
    {
        if(!ok)throw new Exception("B01_62_W19_FAIL:"+what);
        checks++;
    }
    static PersistedPart Part(int slot,bool detached)
    {
        var s=SecureRigCatalog.At(slot);
        long owner=FrozenPartCatalog.Owner(slot);
        return new PersistedPart(s.PartId,owner,detached?0:owner,
            FrozenPartCatalog.Asset(slot),FrozenPartCatalog.Socket(slot),
            detached?PartDockState.Detached:PartDockState.Attached,
            detached,!detached,
            new Vector3d(s.RootX+s.PartOffsetX,detached?2.2:1.2,0),
            0,0,0,1,
            detached?new Vector3d(0.25,0,0):new Vector3d(0,0,0),
            new Vector3d(0,0,0),s.PartMassKg);
    }
    static byte[] Bytes(long epoch,bool light,bool heavy,long run=Run) =>
        SnapshotCodec.Encode(new RunSnapshot(run,epoch,
            new[]{Part(0,light),Part(1,heavy)}));
    sealed class PhysicalWorld:IPreparedWorld
    {
        readonly RunSnapshot saved;
        public bool Active,Retired;
        public PhysicalWorld(RunSnapshot snapshot){saved=snapshot;}
        public RunSnapshot Capture()=>saved;
        public void Activate(){Active=true;}
        public void Deactivate(){Active=false;}
        public void Retire(){Active=false;Retired=true;}
    }
    sealed class Factory:IWorldFactory
    {
        public PhysicalWorld? Last;
        public int Prepared;
        public IPreparedWorld Prepare(RunSnapshot snapshot)
        {
            Prepared++;
            return Last=new PhysicalWorld(snapshot);
        }
    }
    // Durably COMMIT using the genuine selected original #642
    // FileCheckpointStore, then only lose ACK across the public interface.
    // This models the proven #637 uncertainty *without changing producer code*.
    sealed class CommitAckLostStore:ICheckpointStore
    {
        public readonly FileCheckpointStore Real;
        public bool LoseAck,ReturnFalse;
        public int ActualDurableCommits;
        public CommitAckLostStore(string path){Real=new FileCheckpointStore(path);}
        public bool Read(long run,out long gen,out string hash) =>
            Real.Read(run,out gen,out hash);
        public bool Commit(long run,long previous,string oldHash,long next,string newHash)
        {
            if(ReturnFalse)return false;
            bool success=Real.Commit(run,previous,oldHash,next,newHash);
            if(success)ActualDurableCommits++;
            if(success&&LoseAck)throw new IOException("ACK lost after durable store committed");
            return success;
        }
    }
    static (AtomicWorldSwap swap,Factory factory,PhysicalWorld first,
        CommitAckLostStore store) Seed(string scratch,string name)
    {
        string path=Path.Combine(scratch,name+".checkpoint");
        var store=new CommitAckLostStore(path);
        var factory=new Factory();
        var host=new AtomicWorldSwap(factory,store);
        Check(host.TryLoad(Bytes(10,true,false),Run,1,()=>true)==WorldSwapVerdict.Accepted,
            name+":true original #642 stage/Activate/durable first commit");
        PhysicalWorld old=factory.Last??throw new Exception("missing first real logical world");
        Check(old.Active&&host.IsConsumed(0,FrozenPartCatalog.Id(0))&&
            !host.IsConsumed(1,FrozenPartCatalog.Id(1)),
            name+":stable original #556 and W06 Light consumed ID");
        Check(store.Real.Read(Run,out long epoch,out string hash)&&epoch==10&&hash.Length==64,
            name+":original real file sidecar persisted gen10");
        return(host,factory,old,store);
    }
    static void Go()
    {
        string scratch=Path.Combine(Path.GetTempPath(),"b01-62-w19-r3-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(scratch);
        try
        {
            Check(ColdRestore.TryStage(Bytes(11,false,true),out ColdRestore? staged) &&
                staged!=null&&staged.ContainsConsumedId(1,FrozenPartCatalog.Id(1)) &&
                !staged.ContainsConsumedId(0,FrozenPartCatalog.Id(0)),
                "original #556 ColdRestore + W06 Heavy consumed ID");
            {
                var(host,factory,old,store)=Seed(scratch,"normal");
                Check(host.TryLoad(Bytes(11,false,true),Run,11,()=>true)==WorldSwapVerdict.Accepted,
                    "normal R3 #642 commits next generation");
                Check(old.Retired&&factory.Last!.Active&&
                    !host.IsConsumed(0,FrozenPartCatalog.Id(0))&&
                    host.IsConsumed(1,FrozenPartCatalog.Id(1)),
                    "normal actual original Heavy ID restored and old state retired");
                Check(store.Real.Read(Run,out long generation,out _)&&generation==11,
                    "normal durable selected #642 physical checkpoint gen11");
                Check(host.TryLoad(Bytes(11,true,false),Run,11,()=>true)
                    ==WorldSwapVerdict.StaleGeneration,
                    "R3 prevents same-gen fork from active world");
            }
            {
                var(host,_,old,store)=Seed(scratch,"commit_false");
                store.ReturnFalse=true;
                Check(host.TryLoad(Bytes(11,false,true),Run,11,()=>true)
                    ==WorldSwapVerdict.RollbackQuarantine,
                    "original selected R3 Commit false immediate quarantine");
                Check(host.Quarantined&&old.Active&&
                    store.Real.Read(Run,out long generation,out _)&&generation==10,
                    "original selected R3 successful rollback old remains physical active");
                Check(!host.TrySave(Run,()=>true,out _),"quarantine denies Save");
            }
            {
                var(host,_,old,store)=Seed(scratch,"uncertain_ack");
                store.LoseAck=true;
                WorldSwapVerdict verdict=host.TryLoad(Bytes(11,false,true),Run,11,()=>true);
                Check(store.Real.Read(Run,out long durable,out _)&&durable==11 &&
                    store.ActualDurableCommits==2,
                    "injected ACK loss occurred AFTER original true physical disk commit");
                Check(verdict==WorldSwapVerdict.HostException&&!host.Quarantined&&
                      old.Active&&host.Generation==10,
                      "EXPECTED_SOURCE_RED: ambiguous durable commit incorrectly HostException not quarantine");
                red++;
                Check(host.IsConsumed(0,FrozenPartCatalog.Id(0))&&
                     !host.IsConsumed(1,FrozenPartCatalog.Id(1)),
                    "stale original consumed Light ledger remains physically active");
                Check(!host.TrySave(Run,()=>true,out _),
                    "R3 #642 newly fixes former #596 stale world export on changed durable fingerprint");
                store.LoseAck=false; // subsequent new call now obtains an ordinary ACK
                Check(host.TryLoad(Bytes(12,false,true),Run,12,()=>true)
                    ==WorldSwapVerdict.Accepted,
                    "EXPECTED_SOURCE_RED: uncertain commit host permits another restore without quarantine");
                red++;
                Check(!host.Quarantined&&store.Real.Read(Run,out long gen12,out _)&&gen12==12,
                    "source confirmed poisoned host still mutable after uncertain durable state");
            }
            {
                var(host,factory,old,store)=Seed(scratch,"foreign");
                int before=factory.Prepared;
                Check(host.TryLoad(Bytes(11,false,true,Run+1),Run,11,()=>true)
                    ==WorldSwapVerdict.WrongRun&&factory.Prepared==before&&old.Active,
                    "wrong run ID rejected before physical stage");
                byte[] bad=Bytes(11,false,true);bad[8]^=0x40;
                Check(host.TryLoad(bad,Run,11,()=>true)==WorldSwapVerdict.InvalidSnapshot&&
                    store.Real.Read(Run,out long epoch,out _)&&epoch==10,
                    "corrupt binary denied, durable old generation unchanged");
            }
            Console.WriteLine("B0162_W19_EXACT_R3_642_ORIGINAL_SOURCE_ASSERTIONS_PASS="+checks);
            Console.WriteLine("B0162_W19_SOURCE_RED_COMMIT_UNCERTAIN_REPRODUCED="+red);
            Console.WriteLine("B0162_W19_R3_STALE_EXPORT_BLOCKED=TRUE R3_QUARANTINE_BYPASS_RED=TRUE");
            Console.WriteLine("R3_PROVISIONAL_SOURCE_G0_HOLD_P0 G1_NOT_RUN G2_HOLD NOT_MERGED");
        }
        finally
        {
            // Only own UUID temporary root, created in this invocation.
            Directory.Delete(scratch,recursive:true);
        }
    }
    public static int Main()
    {
        try {Go();return 0;}
        catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
    }
}
