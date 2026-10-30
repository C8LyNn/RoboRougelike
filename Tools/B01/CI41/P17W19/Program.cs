using System;
using System.IO;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;
using B01.SecureReattachV2;
using B01.UnitySaveBridge001;

// Independently authored adversarial QA. No copied host or Save codec.
// Compiles actual candidate original AtomicWorldSwap.cs and original frozen #556/W06/#510.
internal static class Program
{
    const long Run=64119;
    static int checks;
    static void Must(bool ok,string what)
    {
        checks++;
        if(!ok)throw new InvalidOperationException("W19_ASSERT_FAIL:"+what);
    }
    static PersistedPart Part(int slot,bool detached)
    {
        SecureRigSpec s=SecureRigCatalog.At(slot);
        long owner=FrozenPartCatalog.Owner(slot);
        return new PersistedPart(s.PartId,owner,detached?0:owner,
            FrozenPartCatalog.Asset(slot),FrozenPartCatalog.Socket(slot),
            detached?PartDockState.Detached:PartDockState.Attached,
            detached,!detached,
            new Vector3d(s.RootX+s.PartOffsetX,detached?2.2:1.2,0),
            0,0,0,1,detached?new Vector3d(.25,0,0):new Vector3d(0,0,0),
            new Vector3d(0,0,0),s.PartMassKg);
    }
    static byte[] Bytes(long gen,bool light,bool heavy,long run=Run) =>
        SnapshotCodec.Encode(new RunSnapshot(run,gen,new[]{
            Part(0,light),Part(1,heavy)}));

    sealed class RealStagedWorld:IPreparedWorld
    {
        readonly RunSnapshot snapshot;
        public bool Active,Retired;
        public RealStagedWorld(RunSnapshot s){snapshot=s;}
        public RunSnapshot Capture()=>snapshot;
        public void Activate(){Active=true;}
        public void Deactivate(){Active=false;}
        public void Retire(){Retired=true;Active=false;}
    }
    sealed class Factory:IWorldFactory
    {
        public int Preparations;
        public RealStagedWorld? Latest;
        public IPreparedWorld Prepare(RunSnapshot s)
        {
            Preparations++;
            Latest=new RealStagedWorld(s);
            return Latest;
        }
    }
    sealed class DurableThenThrow:ICheckpointStore
    {
        readonly FileCheckpointStore original;
        public bool ThrowOnce,FalseOnce;
        public int TrueDurableCommits;
        public DurableThenThrow(string path){original=new FileCheckpointStore(path);}
        public bool Read(long run,out long gen,out string hash) =>
            original.Read(run,out gen,out hash);
        public bool Commit(long run,long old,string prior,long next,string hash)
        {
            if(FalseOnce){FalseOnce=false;return false;}
            bool ok=original.Commit(run,old,prior,next,hash);
            if(ok)TrueDurableCommits++;
            if(ok&&ThrowOnce)
            {
                ThrowOnce=false;
                throw new IOException("DISK_COMMIT_SUCCEEDED_ACK_FAILED");
            }
            return ok;
        }
        public bool Epoch(long value) =>
            original.Read(Run,out long gen,out string _) && gen==value;
    }
    static (AtomicWorldSwap swap,Factory factory,RealStagedWorld old,
        DurableThenThrow disk) Seed(string root,string name)
    {
        string dir=Path.Combine(root,name);
        Directory.CreateDirectory(dir);
        var disk=new DurableThenThrow(Path.Combine(dir,"owner.checkpoint"));
        var factory=new Factory();
        var swap=new AtomicWorldSwap(factory,disk);
        Must(swap.TryLoad(Bytes(10,true,false),Run,1,()=>true)==WorldSwapVerdict.Accepted,
            "baseline original R3 physical checkpoint gen10");
        Must(factory.Latest!=null&&factory.Latest.Active&&disk.Epoch(10),
            "baseline world and persisted revision");
        Must(swap.IsConsumed(0,FrozenPartCatalog.Id(0))&&
             !swap.IsConsumed(1,FrozenPartCatalog.Id(1)),
            "original W06 consumed Light/Heavy IDs");
        return (swap,factory,factory.Latest!,disk);
    }
    static bool OriginalSnapshot(byte[] data,long gen,bool light,bool heavy)
    {
        if(!SnapshotCodec.TryDecode(data,out RunSnapshot? s)||s==null ||
            s.RunId!=Run||s.Generation!=gen)return false;
        return s.Parts[0].Consumed==light&&s.Parts[1].Consumed==heavy
               &&s.Parts[0].Id==FrozenPartCatalog.Id(0)
               &&s.Parts[1].Id==FrozenPartCatalog.Id(1);
    }
    static int Main(string[] args)
    {
        if(args.Length!=2||!(args[0]=="A"||args[0]=="B") ||
           !(args[1]=="--audit"||args[1]=="--release"))
            return 10;
        string candidate=args[0];
        string root=Path.Combine(Path.GetTempPath(),"ci41_w19_original_r3_"+candidate+"_"+
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            // Positive independent source: valid Light->Heavy restores and durable ID.
            {
                var(s,f,old,d)=Seed(root,"positive");
                Must(s.TryLoad(Bytes(11,false,true),Run,11,()=>true)
                    ==WorldSwapVerdict.Accepted,"normal new epoch");
                Must(old.Retired&&f.Latest!=null&&f.Latest.Active&&d.Epoch(11),
                    "physical old retired and persisted new");
                Must(!s.IsConsumed(0,FrozenPartCatalog.Id(0)) &&
                     s.IsConsumed(1,FrozenPartCatalog.Id(1)),
                     "original W06 stable consumed ID swapped correctly");
                Must(s.TrySave(Run,()=>true,out byte[] saved),
                     "legitimate current host can save");
                Must(OriginalSnapshot(saved,12,false,true),"original #556 binary version12");
            }
            // Negative independent source: fork and invalid binary, no partial publish.
            {
                var(s,f,old,d)=Seed(root,"fork");
                int builds=f.Preparations;
                Must(s.TryLoad(Bytes(10,false,true),Run,10,()=>true)
                    ==WorldSwapVerdict.StaleGeneration,"same gen fork denied");
                byte[] bad=Bytes(11,false,true);bad[7]^=0x7f;
                Must(s.TryLoad(bad,Run,11,()=>true)==WorldSwapVerdict.InvalidSnapshot,
                    "corrupt binary denied");
                Must(f.Preparations==builds&&old.Active&&d.Epoch(10),
                    "neither denial stages or changes committed world");
            }
            // Control: negative Commit acknowledged FALSE must sticky quarantine.
            {
                var(s,f,old,d)=Seed(root,"explicit-false");
                d.FalseOnce=true;
                Must(s.TryLoad(Bytes(11,false,true),Run,11,()=>true)
                    ==WorldSwapVerdict.RollbackQuarantine,"explicit false quarantine");
                Must(s.Quarantined&&d.Epoch(10)&&old.Active&&f.Latest!.Retired,
                    "false commit old active persisted unchanged candidate retired");
            }
            // P0 uncertain disk commit: genuine ORIGINAL FileCheckpointStore writes to
            // actual OS, adversarial adapter throws AFTER success but BEFORE ACK.
            bool ambiguousUnsafe,staleExport;
            {
                var(s,f,old,d)=Seed(root,"ack-lost");
                d.ThrowOnce=true;
                var verdict=s.TryLoad(Bytes(11,false,true),Run,11,()=>true);
                Must(d.TrueDurableCommits==2&&d.Epoch(11),
                    "genuine original OS durable generation advanced despite throw");
                Must(old.Active&&!old.Retired&&f.Latest!.Retired,
                    "old physical world compensated, new retired");
                Must(s.IsConsumed(0,FrozenPartCatalog.Id(0)) &&
                     !s.IsConsumed(1,FrozenPartCatalog.Id(1)),
                    "Light ID remains OLD despite newer durable Heavy generation");
                ambiguousUnsafe=verdict==WorldSwapVerdict.HostException&&!s.Quarantined;
                Must(ambiguousUnsafe,"P0 original R3 ambiguous commit is NOT quarantined");
                bool generated=s.TrySave(Run,()=>true,out byte[] stale);
                staleExport=generated&&OriginalSnapshot(stale,12,true,false);
                if(candidate=="A")
                    Must(!staleExport,"R3A refuses stale consumed-Light export");
                else
                    Must(staleExport,"R3B still re-exports old consumed-Light at future gen");
                // Real later restart reads the durable world's true generation11.
                var freshFactory=new Factory();
                var cold=new AtomicWorldSwap(freshFactory,new FileCheckpointStore(
                    Path.Combine(root,"ack-lost","owner.checkpoint")));
                Must(cold.TryLoad(Bytes(10,true,false),Run,10,()=>true)
                    ==WorldSwapVerdict.StaleGeneration,"fresh instance refuses stale gen10");
                Must(cold.TryLoad(Bytes(11,false,true),Run,11,()=>true)
                    ==WorldSwapVerdict.Accepted,"fresh correctly accepts durable Heavy gen11");
                Must(!cold.IsConsumed(0,FrozenPartCatalog.Id(0))&&
                     cold.IsConsumed(1,FrozenPartCatalog.Id(1)),
                    "cold restored Heavy ID matches durable committed world");
            }
            Console.WriteLine("CI41_W19_ORIGINAL_"+candidate+
                "_UNCERTAIN_COMMIT_P0_RED_REPRODUCED durable_gen=11 "+
                "not_quarantined="+ambiguousUnsafe+" stale_export="+staleExport);
            Console.WriteLine("CI41_W19_ORIGINAL_"+candidate+
                "_DIRECT_CSHARP_PASS asserts="+checks+
                " exact_core_and_frozen_codec=TRUE native_unity=NOT_RUN");
            Console.WriteLine("CI41_W19_SOURCE_QA_WITNESS_PASS_RELEASE_HOLD_NOT_SELECTED");
            return args[1]=="--release"?3:0;
        }
        catch(Exception e)
        {
            Console.Error.WriteLine("CI41_W19_ORIGINAL_TEST_UNEXPECTED_FAIL "+
                candidate+" "+e);
            return 2;
        }
        finally
        {
            if(Directory.Exists(root))Directory.Delete(root,true);
        }
    }
}
