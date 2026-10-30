using System;
using System.IO;
using B01.Canonical510Port001;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;
using B01.SecureReattachV2;
using B01.UnitySaveBridge001;

internal static class Independent
{
    const long Run=51061;
    static int pass,fail;
    static void Check(string name, Func<bool> test)
    {
        try {
            bool ok=test();
            if(ok){pass++;Console.WriteLine("PASS "+name);}
            else{fail++;Console.WriteLine("FAIL "+name);}
        }
        catch(Exception e){fail++;Console.WriteLine("FAIL "+name+" "+e);}
    }
    static byte[] Payload(long generation,bool light,bool heavy,long run=Run)
    {
        var parts=new PersistedPart[2];
        for(int i=0;i<2;i++) {
            bool detached=i==0?light:heavy;
            SecureRigSpec spec=SecureRigCatalog.At(i);
            long owner=FrozenPartCatalog.Owner(i);
            parts[i]=new PersistedPart(spec.PartId,owner,detached?0:owner,
                FrozenPartCatalog.Asset(i),FrozenPartCatalog.Socket(i),
                detached?PartDockState.Detached:PartDockState.Attached,
                detached,!detached,
                new Vector3d(spec.RootX+spec.PartOffsetX,detached?2.2:1.2,0),
                0,0,0,1,detached?new Vector3d(.25,0,0):new Vector3d(0,0,0),
                new Vector3d(0,0,0),spec.PartMassKg);
        }
        return SnapshotCodec.Encode(new RunSnapshot(run,generation,parts));
    }
    sealed class World:ICanonical510PreparedWorld
    {
        readonly RunSnapshot saved;
        public bool Active,Retired,ThrowOnDeactivate,ThrowOnRetire,WrongLedger,StageLive;
        public World(RunSnapshot p){saved=p;}
        public bool IsOffstageInactive=>!StageLive;
        public bool HasPrivateR2LedgerForSlot(int slot,long partId,bool consumed)
        {
            var item=saved.Parts[slot];
            return !WrongLedger&&item.Id==partId&&item.Consumed==consumed;
        }
        public RunSnapshot Capture()=>saved;
        public void Activate(){Active=true;}
        public void Deactivate()
        {
            if(ThrowOnDeactivate)throw new IOException("cannot stop private physical body");
            Active=false;
        }
        public void Retire()
        {
            if(ThrowOnRetire)throw new IOException("cannot remove private physical body");
            Retired=true;Active=false;
        }
    }
    sealed class Owner:ICanonical510WorldOwner
    {
        public long RunId{get;set;}=Run;
        public Guid PersistentSlotId{get;set;}=Guid.Parse("4681855c-b4aa-4135-9e22-03b31db3c882");
        public bool Idle=true, WrongLedger,StageLive,FailStop,FailRetire;
        public int Prepared;
        public World? Last,Old,Orphan;
        public bool MainThreadQuiescentAndOwnsLease()=>Idle;
        public ICanonical510PreparedWorld PrepareFreshPrivateRigAndLedger(RunSnapshot p)
        {
            Prepared++;
            var world=new World(p){
                WrongLedger=WrongLedger,StageLive=StageLive,
                ThrowOnDeactivate=FailStop,ThrowOnRetire=FailRetire
            };
            if(StageLive){world.Activate();Orphan=world;}
            if(Old==null)Old=world;
            Last=world;
            return world;
        }
    }
    static bool WithSlot(Func<string,bool> fn)
    {
        string dir=Path.Combine(Path.GetTempPath(),"b0161-w20-canonical-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try{return fn(dir);}
        finally{Directory.Delete(dir,true);}
    }
    static bool HasOriginalIds(RunSnapshot s,bool light,bool heavy)=>
        s.Parts.Length==2&&s.Parts[0].Id==2310001&&s.Parts[1].Id==2310002&&
        s.Parts[0].Consumed==light&&s.Parts[1].Consumed==heavy;
    static bool Saved(Canonical510OwnerGateway api,long gen,bool light,bool heavy)
    {
        if(!api.TryCaptureExact556(out byte[] b)||b==null||
           !SnapshotCodec.TryDecode(b,out RunSnapshot? s)||s==null)return false;
        return s.Generation==gen&&s.RunId==Run&&HasOriginalIds(s,light,heavy);
    }
    static bool ReadEpoch(string root,Owner owner,long generation)
    {
        var store=new FileCheckpointStore(CheckpointSlotPath.ForRun(root,owner.RunId,owner.PersistentSlotId));
        return store.Read(owner.RunId,out long observed,out string digest)&&
            observed==generation&&digest.Length==64;
    }
    static bool OriginalAndAuthorityMasks(string root)
    {
        for(int mask=0;mask<4;mask++) {
            var o=new Owner{PersistentSlotId=Guid.NewGuid()};
            var api=new Canonical510OwnerGateway(o,root);
            bool l=(mask&1)!=0,h=(mask&2)!=0;
            if(api.TryRestoreExact556(Payload(10,l,h))!=WorldSwapVerdict.Accepted||
               !api.HasWorld||api.Quarantined||o.Last==null||!o.Last.Active||
               !api.ContainsPrivateConsumedId(0,2310001)==l||
               !api.ContainsPrivateConsumedId(1,2310002)==h||
               !Saved(api,11,l,h)||!ReadEpoch(root,o,10))return false;
        }
        return true;
    }
    static bool NonLeakingStageFailure(string root,bool badLedger,bool active)
    {
        var o=new Owner{WrongLedger=badLedger,StageLive=active};
        var api=new Canonical510OwnerGateway(o,root);
        var verdict=api.TryRestoreExact556(Payload(10,true,false));
        return verdict==WorldSwapVerdict.HostException&&
            !api.HasWorld&&!api.Quarantined&&o.Last!=null&&
            o.Last.Retired&&!o.Last.Active&&!ReadEpoch(root,o,10);
    }
    static bool BrokenPrepareOrphansPhysics(string root)
    {
        var o=new Owner();
        var api=new Canonical510OwnerGateway(o,root);
        if(api.TryRestoreExact556(Payload(10,true,false))!=WorldSwapVerdict.Accepted||
           o.Last==null||!o.Last.Active||!ReadEpoch(root,o,10))return false;
        World original=o.Last;
        o.StageLive=true;o.WrongLedger=true;o.FailStop=true;o.FailRetire=true;
        var verdict=api.TryRestoreExact556(Payload(11,false,true));
        bool sourceRed=verdict==WorldSwapVerdict.HostException&&
           !api.Quarantined&&api.HasWorld&&api.Generation==10&&
           o.Last!=null&&o.Last!=original&&o.Last.Active&&
           !o.Last.Retired&&original.Active&&ReadEpoch(root,o,10);
        if(!sourceRed)return false;
        Console.WriteLine("W20_CANONICAL_510_PORT_PREPARE_FAILED_CLEANUP_ORPHAN_SOURCE_RED=REPRODUCED");
        World orphan=o.Last!;
        o.StageLive=false;o.WrongLedger=false;o.FailStop=false;o.FailRetire=false;
        var retry=api.TryRestoreExact556(Payload(11,false,true));
        bool duplicate=retry==WorldSwapVerdict.Accepted&&
            !api.Quarantined&&api.Generation==11&&
            o.Last!=null&&o.Last.Active&&o.Last!=orphan&&
            orphan.Active&&!orphan.Retired&&
            !original.Active&&original.Retired&&
            api.ContainsPrivateConsumedId(1,2310002)&&
            ReadEpoch(root,o,11);
        if(duplicate)
            Console.WriteLine("W20_CANONICAL_510_PORT_RETRY_PUBLISHES_WITH_ORPHAN_RIG=REPRODUCED");
        return duplicate;
    }
    static bool HealthyAbortedNewWorld(string root)
    {
        var o=new Owner{WrongLedger=true,StageLive=true};
        var api=new Canonical510OwnerGateway(o,root);
        bool failed=api.TryRestoreExact556(Payload(10,true,false))==WorldSwapVerdict.HostException;
        return failed&&o.Last!=null&&o.Last.Retired&&!o.Last.Active&&!api.HasWorld;
    }
    static bool OneCleanupFailureButOtherSucceeds(string root)
    {
        var o=new Owner{StageLive=true,WrongLedger=true,FailStop=true,FailRetire=false};
        var api=new Canonical510OwnerGateway(o,root);
        return api.TryRestoreExact556(Payload(10,true,false))==WorldSwapVerdict.HostException&&
            o.Last!=null&&o.Last.Retired&&!o.Last.Active;
    }
    static bool ForeignRunNoStage(string root)
    {
        var o=new Owner();var api=new Canonical510OwnerGateway(o,root);
        return api.TryRestoreExact556(Payload(10,false,true,Run+1))==WorldSwapVerdict.WrongRun&&
            !api.HasWorld&&o.Prepared==0;
    }
    static bool DenyBusyBeforePrepare(string root)
    {
        var o=new Owner{Idle=false};var api=new Canonical510OwnerGateway(o,root);
        return api.TryRestoreExact556(Payload(10,true,false))==WorldSwapVerdict.HostBusy&&o.Prepared==0;
    }
    static bool CheckValidNextSave(string root)
    {
        var o=new Owner();var api=new Canonical510OwnerGateway(o,root);
        if(api.TryRestoreExact556(Payload(10,true,false))!=WorldSwapVerdict.Accepted)return false;
        if(api.TryRestoreExact556(Payload(11,false,true))!=WorldSwapVerdict.Accepted)return false;
        return Saved(api,12,false,true)&&o.Old!=null&&o.Old.Retired&&
            ReadEpoch(root,o,11);
    }
    public static int Main(string[] args)
    {
        Check("W20_ORIGINAL_556_W06_ALL_FOUR_STABLE_LIGHT_HEAVY_STATES",()=>WithSlot(OriginalAndAuthorityMasks));
        Check("W20_INITIAL_WRONG_R2_PRIVATE_LEDGER_CLEANLY_RETIRES",()=>WithSlot(p=>NonLeakingStageFailure(p,true,false)));
        Check("W20_INITIAL_ACTIVE_STAGED_WORLD_CLEANLY_RETIRES",()=>WithSlot(p=>NonLeakingStageFailure(p,false,true)));
        Check("W20_ACTIVE_BAD_LEDGER_WITH_SUCCESSFUL_CLEANUP_NO_ORPHAN",()=>WithSlot(HealthyAbortedNewWorld));
        Check("W20_DEACTIVATE_THROW_BUT_RETIRE_SUCCEEDS_PREVENTS_ORPHAN",()=>WithSlot(OneCleanupFailureButOtherSucceeds));
        Check("W20_ORIGINAL_556_FOREIGN_RUN_DENIED_BEFORE_PRIVATE_RIG",()=>WithSlot(ForeignRunNoStage));
        Check("W20_OWNER_NOT_QUIESCENT_DENIES_BEFORE_PRIVATE_RIG",()=>WithSlot(DenyBusyBeforePrepare));
        Check("W20_NORMAL_GENERATION_ADVANCE_SAVES_HEAVY_WITH_ORIGINAL_IDS",()=>WithSlot(CheckValidNextSave));
        Check("RED_ORIGINAL_OWNERFACTORY_PREPARE_CLEANUP_FAILURE_NO_QUARANTINE_ORPHAN_AND_RETRY",()=>WithSlot(BrokenPrepareOrphansPhysics));
        Console.WriteLine("B0161_W20_EXACT_ORIGINAL_682_OWNER_PORT_DIRECT_CSHARP_EXECUTED");
        Console.WriteLine("SOURCE_QA_CASES_TOTAL="+(pass+fail)+" PASS="+pass+" FAIL="+fail);
        Console.WriteLine("SOURCE_RELEASE_GATE="+(fail==0?"HOLD_P0_PREPARE_CLEANUP_ORPHAN":"HARNESS_OR_SOURCE_FAILURE"));
        Console.WriteLine("NATIVE_UNITY_G1_NOT_RUN LIVE_CANONICAL_510_OWNER_NOT_IMPLEMENTED G2_HOLD NO_MERGE");
        return fail!=0?1:args.Length==1&&args[0]=="--release"?3:0;
    }
}
