using System;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;
using B01.SecureSaveHost001;

// Independent QA of ORIGINAL B01-23 PR #585 AtomicColdUnityHostBridge.
// Only IIsolatedRigFactory / IIsolatedRigWorld are test doubles. Unity native NOT_RUN.
internal static class OfficialHostSourceQA
{
    private static int pass,fail;
    private static void Check(string name,Func<bool> run)
    {
        try{
            if(run()){pass++;Console.WriteLine("PASS HOST_"+name);}
            else{fail++;Console.WriteLine("FAIL HOST_"+name);}
        }catch(Exception e){fail++;Console.WriteLine("FAIL HOST_"+name+" "+e);}
    }
    private sealed class World : IIsolatedRigWorld
    {
        private readonly RunSnapshot source;
        public bool Visible, Disposed, FailShow, FailHide;
        public bool MutateOnShow, BadStageCapture;
        public World(RunSnapshot snapshot){source=snapshot;}
        public bool IsVisible=>Visible;
        public bool IsBusy=>false;
        public RunSnapshot Capture(long run,long generation)
        {
            if(Disposed)throw new InvalidOperationException("world disposed");
            if(BadStageCapture||(MutateOnShow&&Visible))
                return new RunSnapshot(source.RunId,source.Generation,
                    new []{source.Parts[0],source.Parts[0]});
            return source;
        }
        public void SetVisible(bool visible)
        {
            Visible=visible; // force fault AFTER a physical side effect
            if((visible&&FailShow)||(!visible&&FailHide))
                throw new InvalidOperationException("visibility fault");
        }
        public void Dispose()
        {
            Disposed=true;Visible=false;
        }
    }
    private sealed class Factory : IIsolatedRigFactory
    {
        public bool ThrowOnBuild,NextFailShow,NextFailHide,NextBadStageCapture,NextMutateOnShow;
        public World Last;
        public IIsolatedRigWorld Build(ColdRestore plan)
        {
            if(ThrowOnBuild)throw new InvalidOperationException("factory exploded");
            Last=new World(plan.Snapshot){
                FailShow=NextFailShow,FailHide=NextFailHide,
                BadStageCapture=NextBadStageCapture,MutateOnShow=NextMutateOnShow
            };
            NextFailShow=false;NextFailHide=false;
            NextBadStageCapture=false;NextMutateOnShow=false;
            return Last;
        }
    }
    private static PersistedPart Part(int slot,bool detached)
    {
        long owner=FrozenPartCatalog.Owner(slot);
        return new PersistedPart(FrozenPartCatalog.Id(slot),owner,detached?0:owner,
            FrozenPartCatalog.Asset(slot),FrozenPartCatalog.Socket(slot),
            detached?PartDockState.Detached:PartDockState.Attached,detached,!detached,
            new Vector3d(slot==0?1:5,2,0),0,0,0,1,
            detached?new Vector3d(.1,0,0):new Vector3d(0,0,0),
            new Vector3d(0,0,0),FrozenPartCatalog.Mass(slot));
    }
    private static byte[] Bytes(long generation,bool light,bool heavy,long run=77)
        => SnapshotCodec.Encode(new RunSnapshot(run,generation,
            new [] {Part(0,light),Part(1,heavy)}));
    private static (AtomicColdUnityHostBridge bridge,Factory factory,World old) Seed()
    {
        var f=new Factory();
        var b=new AtomicColdUnityHostBridge(77,f,()=>true);
        if(b.TryLoad(Bytes(1,true,false),1)!=RestoreVerdict.Accepted ||
           f.Last==null||!f.Last.Visible)
            throw new InvalidOperationException("real bridge failed setup");
        return (b,f,f.Last);
    }
    public static int Main()
    {
        Check("selected_556_cold_load_commits_visible_world",()=>{
            var (b,_,world)=Seed();
            return b.HasWorld&&b.Generation==1&&world.Visible&&!b.IsQuarantined;
        });
        Check("actual_save_binary_retains_original_W06_flags",()=>{
            var (b,_,world)=Seed();
            var verdict=b.TrySave(out byte[] bytes);
            return verdict==RestoreVerdict.Accepted&&bytes.Length>0&&world.Visible&&
                ColdRestore.TryStage(bytes,out ColdRestore plan)&&plan!=null&&
                plan.Snapshot.Generation==1&&
                plan.ContainsConsumedId(0,FrozenPartCatalog.Id(0))&&
                !plan.ContainsConsumedId(1,FrozenPartCatalog.Id(1));
        });
        Check("corrupt_snapshot_keeps_visible_old_world",()=>{
            var (b,f,old)=Seed();var bytes=Bytes(2,false,true);bytes[7]^=1;
            var verdict=b.TryLoad(bytes,1);
            return verdict==RestoreVerdict.InvalidSnapshot&&old.Visible&&
                b.Generation==1&&f.Last==old;
        });
        Check("wrong_run_keeps_original_world",()=>{
            var (b,f,old)=Seed();var verdict=b.TryLoad(Bytes(2,false,true,88),1);
            return verdict==RestoreVerdict.WrongRun&&old.Visible&&f.Last==old&&
                b.Generation==1;
        });
        Check("stale_generation_refuses_without_build",()=>{
            var (b,f,old)=Seed();
            var verdict=b.TryLoad(Bytes(1,false,true),2);
            return verdict==RestoreVerdict.StaleGeneration&&old.Visible&&f.Last==old;
        });
        Check("second_idle_failure_discards_prepared_world",()=>{
            int calls=0;
            var f=new Factory();
            var b=new AtomicColdUnityHostBridge(77,f,()=>++calls==1);
            var verdict=b.TryLoad(Bytes(1,true,false),1);
            return verdict==RestoreVerdict.TransactionBusy&&calls==2&&
                !b.HasWorld&&f.Last!=null&&f.Last.Disposed;
        });
        Check("build_exception_quarantines_without_old_mutation",()=>{
            var (b,f,old)=Seed();f.ThrowOnBuild=true;
            var verdict=b.TryLoad(Bytes(2,false,true),1);
            return verdict==RestoreVerdict.Quarantine&&b.IsQuarantined&&
                old.Visible&&b.Generation==1;
        });
        Check("stage_wrong_identity_rejected_and_disposed",()=>{
            var (b,f,old)=Seed();f.NextBadStageCapture=true;
            var verdict=b.TryLoad(Bytes(2,false,true),1);
            return verdict==RestoreVerdict.InvalidSnapshot&&old.Visible&&
                f.Last!=old&&f.Last.Disposed&&b.Generation==1;
        });
        Check("activation_exception_reactivates_old_world",()=>{
            var (b,f,old)=Seed();f.NextFailShow=true;
            var verdict=b.TryLoad(Bytes(2,false,true),1);
            return verdict==RestoreVerdict.Quarantine&&old.Visible&&
                f.Last.Disposed&&b.Generation==1&&!b.IsQuarantined;
        });
        Check("rollback_failure_permanently_quarantines",()=>{
            var (b,f,old)=Seed();f.NextFailShow=true;f.NextFailHide=true;
            var verdict=b.TryLoad(Bytes(2,false,true),1);
            return verdict==RestoreVerdict.Quarantine&&b.IsQuarantined&&
                b.TrySave(out _)==RestoreVerdict.Quarantine&&
                old.Visible&&b.Generation==1;
        });
        Check("reentrant_idle_load_denied_without_double_commit",()=>{
            AtomicColdUnityHostBridge b=null;
            RestoreVerdict inner=RestoreVerdict.InvalidSnapshot;
            var f=new Factory();
            b=new AtomicColdUnityHostBridge(77,f,()=>{
                if(b!=null)inner=b.TryLoad(Bytes(3,true,true),1);
                return true;
            });
            var verdict=b.TryLoad(Bytes(1,true,false),1);
            return verdict==RestoreVerdict.Accepted&&
                inner==RestoreVerdict.TransactionBusy&&b.Generation==1;
        });
        Check("same_generation_divergent_commit_has_no_host_revision_gate",()=>{
            var (b,f,old)=Seed();
            var verdict=b.TryLoad(Bytes(1,false,true),1);
            return verdict==RestoreVerdict.Accepted&&b.Generation==1&&
                !old.Visible&&old.Disposed&&f.Last!=old&&f.Last.Visible;
        });
        Check("RED_activate_mutates_world_without_post_commit_revalidation",()=>{
            var (b,f,old)=Seed();f.NextMutateOnShow=true;
            var verdict=b.TryLoad(Bytes(2,false,true),1);
            var save=b.TrySave(out _);
            return verdict==RestoreVerdict.Accepted&&
                save==RestoreVerdict.InvalidSnapshot&&b.Generation==2&&
                old.Disposed&&f.Last.Visible&&!b.IsQuarantined;
        });
        Console.WriteLine("OFFICIAL_BRIDGE_POST_ACTIVATION_DIVERGENCE=SOURCE_RED");
        Console.WriteLine("SELECTED_556_SOURCE_ONLY=TRUE ORIGINAL_OFFICIAL_BRIDGE_CORE=EXECUTED");
        Console.WriteLine("UNITY_G1_NOT_RUN LIVE_510_PRIVATE_LEDGER_INTEGRATION=NOT_IMPLEMENTED");
        Console.WriteLine("OFFICIAL BRIDGE TOTAL "+(pass+fail)+" PASS "+pass+" FAIL "+fail);
        return fail==0?0:1;
    }
}
