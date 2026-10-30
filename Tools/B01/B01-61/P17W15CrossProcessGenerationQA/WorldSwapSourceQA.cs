using System;
using System.IO;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;
using B01.UnitySaveBridge001;

// Adversarial test doubles only for the IWorldFactory/IPreparedWorld boundary.
// Production AtomicWorldSwap, #556 codec and W06 ledger are ORIGINAL direct sources.
// This does NOT compile/execute UnityEngine or native Rigidbody/PhysX.
internal static class WorldSwapSourceQA
{
    private static int pass, fail;
    private static void Check(string name,Func<bool> run)
    {
        try
        {
            if(run()){pass++;Console.WriteLine("PASS BRIDGE_"+name);}
            else{fail++;Console.WriteLine("FAIL BRIDGE_"+name);}
        }
        catch(Exception e){fail++;Console.WriteLine("FAIL BRIDGE_"+name+" "+e);}
    }

    private sealed class World : IPreparedWorld
    {
        private readonly RunSnapshot expected;
        public bool Active, Retired, FailActivate, FailDeactivate, FailRetire, CorruptAfterActivate;
        public World(RunSnapshot source){expected=source;}
        public RunSnapshot Capture()
        {
            if(CorruptAfterActivate&&Active)
                return new RunSnapshot(expected.RunId,expected.Generation,
                    new [] {expected.Parts[0],expected.Parts[0]});
            return expected;
        }
        public void Activate()
        {
            if(Retired)throw new InvalidOperationException("retired");
            Active=true;
            if(FailActivate)throw new InvalidOperationException("mid-activation failure");
        }
        public void Deactivate()
        {
            Active=false;
            if(FailDeactivate)throw new InvalidOperationException("failed deactivate");
        }
        public void Retire()
        {
            Retired=true;Active=false;
            if(FailRetire)throw new InvalidOperationException("retirement deferred");
        }
    }
    private sealed class Factory : IWorldFactory
    {
        public bool FailPrepare, NextFailActivate, NextFailDeactivate, NextCorruptAfterActivate;
        public World? Last;
        public IPreparedWorld Prepare(RunSnapshot snapshot)
        {
            if(FailPrepare)throw new InvalidOperationException("prepare failure");
            var world=new World(snapshot)
            {
                FailActivate=NextFailActivate,FailDeactivate=NextFailDeactivate,
                CorruptAfterActivate=NextCorruptAfterActivate
            };
            NextFailActivate=false;NextFailDeactivate=false;NextCorruptAfterActivate=false;
            Last=world;
            return world;
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
    private static byte[] Bytes(long generation,bool a,bool b,long run=77)
        => SnapshotCodec.Encode(new RunSnapshot(run,generation,
            new [] {Part(0,a),Part(1,b)}));
    private static (AtomicWorldSwap swap,Factory factory,World world) Seed()
    {
        var f=new Factory();
        var swap=new AtomicWorldSwap(f);
        if(swap.TryLoad(Bytes(1,true,false),77,1,()=>true)!=WorldSwapVerdict.Accepted ||
           f.Last==null || !f.Last.Active)
            throw new InvalidOperationException("source bridge bootstrap failed");
        return (swap,f,f.Last);
    }

    public static int Main()
    {
        Check("real_selected_556_first_world_cold_ID",()=>{
            var (swap,_,world)=Seed();
            return swap.HasWorld&&swap.Generation==1&&world.Active&&
                swap.IsConsumed(0,FrozenPartCatalog.Id(0))&&
                !swap.IsConsumed(1,FrozenPartCatalog.Id(1));
        });
        Check("real_source_save_encoding_W06_ID",()=>{
            var (swap,_,world)=Seed();
            return swap.TrySave(77,()=>true,out byte[] bytes)&&bytes!=null&&
                ColdRestore.TryStage(bytes,out ColdRestore restored)&&
                restored!=null&&restored.Snapshot.Generation==1&&
                restored.ContainsConsumedId(0,FrozenPartCatalog.Id(0))&&world.Active;
        });
        Check("historic_wrong_busy_callback_self_denies",()=>{
            // Mirrors the PRE-FIX Unity host calling CanOperate from within Busy.
            var f=new Factory();var swap=new AtomicWorldSwap(f);
            var verdict=swap.TryLoad(Bytes(1,true,false),77,1,()=>!swap.Busy);
            return verdict==WorldSwapVerdict.HostBusy&&!swap.HasWorld&&f.Last==null;
        });
        Check("fixed_external_host_idle_callback_accepts",()=>{
            var f=new Factory();var swap=new AtomicWorldSwap(f);
            var verdict=swap.TryLoad(Bytes(1,true,false),77,1,()=>true);
            return verdict==WorldSwapVerdict.Accepted&&swap.HasWorld&&f.Last!.Active;
        });
        Check("busy_readiness_false_after_prepare_retains_old",()=>{
            var (swap,f,old)=Seed();int count=0;
            var result=swap.TryLoad(Bytes(2,false,true),77,1,()=>++count==1);
            return result==WorldSwapVerdict.HostBusy&&count==2&&
                old.Active&&swap.Generation==1&&f.Last!=old&&f.Last!.Retired;
        });
        Check("wrong_run_does_not_call_factory_or_mutate_old",()=>{
            var (swap,f,old)=Seed();
            var result=swap.TryLoad(Bytes(2,false,true,88),77,1,()=>true);
            return result==WorldSwapVerdict.WrongRun&&f.Last==old&&
                old.Active&&swap.Generation==1;
        });
        Check("malformed_binary_does_not_touch_active_world",()=>{
            var (swap,f,old)=Seed();byte[] bad=Bytes(2,false,true);bad[8]^=0x10;
            var result=swap.TryLoad(bad,77,1,()=>true);
            return result==WorldSwapVerdict.InvalidSnapshot&&old.Active&&
                f.Last==old&&swap.Generation==1;
        });
        Check("stale_generation_does_not_touch_active_world",()=>{
            var (swap,f,old)=Seed();
            var result=swap.TryLoad(Bytes(1,false,true),77,2,()=>true);
            return result==WorldSwapVerdict.StaleGeneration&&old.Active&&
                f.Last==old&&swap.Generation==1;
        });
        Check("factory_exception_no_partial_world_commit",()=>{
            var (swap,f,old)=Seed();f.FailPrepare=true;
            var result=swap.TryLoad(Bytes(2,false,true),77,1,()=>true);
            return result==WorldSwapVerdict.HostException&&old.Active&&
                !swap.Quarantined&&swap.Generation==1&&f.Last==old;
        });
        Check("activation_failure_reactivates_old_world",()=>{
            var (swap,f,old)=Seed();f.NextFailActivate=true;
            var result=swap.TryLoad(Bytes(2,false,true),77,1,()=>true);
            return result==WorldSwapVerdict.HostException&&
                old.Active&&f.Last!=old&&f.Last!.Retired&&
                swap.Generation==1&&!swap.Quarantined;
        });
        Check("post_activation_capture_mismatch_rolls_back",()=>{
            var (swap,f,old)=Seed();f.NextCorruptAfterActivate=true;
            var result=swap.TryLoad(Bytes(2,false,true),77,1,()=>true);
            return result==WorldSwapVerdict.HostException&&old.Active&&
                f.Last!.Retired&&swap.Generation==1;
        });
        Check("rollback_failure_sets_quarantine_and_deny_future",()=>{
            var (swap,f,old)=Seed();
            f.NextFailActivate=true;f.NextFailDeactivate=true;
            var result=swap.TryLoad(Bytes(2,false,true),77,1,()=>true);
            var denied=swap.TryLoad(Bytes(3,false,true),77,1,()=>true);
            return result==WorldSwapVerdict.HostException&&swap.Quarantined&&
                denied==WorldSwapVerdict.RollbackQuarantine&&swap.Generation==1&&old.Active;
        });
        Check("reentrant_load_during_idle_is_denied",()=>{
            var (swap,_,old)=Seed();
            WorldSwapVerdict nested=WorldSwapVerdict.InvalidSnapshot;
            var outer=swap.TryLoad(Bytes(2,false,true),77,1,()=>{
                nested=swap.TryLoad(Bytes(3,true,true),77,1,()=>true);
                return true;
            });
            return outer==WorldSwapVerdict.Accepted&&
                nested==WorldSwapVerdict.HostBusy&&swap.Generation==2&&!old.Active;
        });
        Console.WriteLine("BRIDGE_CORE_SELECTED_SOURCE_DIRECT_QA=EXECUTED");
        Console.WriteLine("UNITY_HOST_CALLBACK_CODE_PIN_REQUIRED=TRUE");
        Console.WriteLine("UNITY_NATIVE_PHYSX=NOT_RUN LIVE_CANONICAL_510_RESTORE=NOT_IMPLEMENTED");
        Console.WriteLine("BRIDGE TOTAL "+(pass+fail)+" PASS "+pass+" FAIL "+fail);
        return fail==0?0:1;
    }
}
