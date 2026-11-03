// B01-62 W31: deterministic original one-writer input mutation QA.
// Real OS File.Replace/File.Move and original #729 .owner. Source-only
// replay factory, NOT Unity PhysX; real game-native G1 remains pending.
#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using B01.CanonicalWorldApi001;
using B01.CanonicalWholeGameAdapter003;
using B01.CanonicalColdRead003;
using B01.WholeGameEconomy003;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;
using B01.SecureReattachV2;
using RoboRougelike.B01.Economy.ApprovedV7;
internal static class W31AtomicPairQA
{
    const long Run=6310062;
    static readonly Guid Slot=Guid.Parse("63102631-0000-4000-8000-000000000556");
    static int pass,fail;
    static void Check(string name,Func<bool> action)
    {
        try{if(action()){pass++;Console.WriteLine("PASS "+name);}
             else{fail++;Console.WriteLine("FAIL "+name);}}
        catch(Exception e){fail++;Console.WriteLine("FAIL "+name+" "+e);}
    }
    static bool Scratch(Func<string,bool> action)
    {
        string root=Path.Combine(Path.GetTempPath(),"b0162-w31-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try{return action(root);}finally{Directory.Delete(root,true);}
    }
    static string StoreDir(string root)=>Path.Combine(root,
        "b0162_canonical_single_authority_v3",
        "run_"+Run+"_slot_"+Slot.ToString("N"));

    static byte[] World(long generation)
    {
        var parts=new PersistedPart[FrozenPartCatalog.Slots];
        for(int i=0;i<parts.Length;i++)
        {
            var spec=SecureRigCatalog.At(i);
            long id=FrozenPartCatalog.Owner(i);
            parts[i]=new PersistedPart(spec.PartId,id,id,
                FrozenPartCatalog.Asset(i),FrozenPartCatalog.Socket(i),
                PartDockState.Attached,false,true,
                new Vector3d((double)(float)(spec.RootX+spec.PartOffsetX),1.2f,0),
                0,0,0,1,new Vector3d(0,0,0),
                new Vector3d(0,0,0),spec.PartMassKg);
        }
        return SnapshotCodec.Encode(new RunSnapshot(Run,generation,parts));
    }
    static ApprovedEconomySnapshotV7 Economy(long generation,int scrap)
    {
        var parts=new List<OwnedModuleV7>();
        for(int i=0;i<FrozenPartCatalog.Slots;i++)
            parts.Add(new OwnedModuleV7 {
                Id=WholeGameEconomySlotV3.EconomicPartId(FrozenPartCatalog.Id(i)),
                Category=i==0?"Core":"Weapon",Hp=1,PurchaseScrap=100,
                ReservationId=""
            });
        return new ApprovedEconomySnapshotV7 {
            Version=7,OwnerId=Slot.ToString("N"),RunId=Run.ToString(),
            Generation=generation,Scrap=scrap,Wins=0,FreeUsedForWin=0,
            Parts=parts,Pending=new List<RebuildTicketV7>(),
            ConsumedVictoryIds=new List<string>(),
            Loaner=new OneShotLoanerV7 {
                Phase=LoanerPhaseV7.None,EverEntered=false,
                EncounterId="",PermitId=""
            }
        };
    }
    sealed class DenyPhysics:ICanonicalMutationGate
    { public bool IsQuiescent()=>false; }
    sealed class NoStage:ICanonicalRigFactory
    {
        public ICanonicalStageOwnership BeginOwnedStage(RunSnapshot s,ColdRestore plan)
          =>throw new InvalidOperationException("No game physics here");
    }
    sealed class NoPort:ICanonicalCommitPort
    {
        public bool Read(long run,out long g,out string hash){g=0;hash="";return false;}
        public bool Commit(long run,long prior,string priorHash,long next,string hash)=>false;
    }
    sealed class MutatingPublisher:IOneFilePublisher
    {
        readonly Action mutate;
        public bool Called;
        public MutatingPublisher(Action action){mutate=action;}
        public void Publish(string staging,string authoritative)
        {
            new OneFileOsPublisher().Publish(staging,authoritative);
            Called=true;
            mutate();
        }
    }
    static bool WriterCommitWithHostMutation()
      =>Scratch(root=>{
        byte[] submitted=World(1);
        var submittedEcon=Economy(1,29);
        byte[] expected=(byte[])submitted.Clone();
        var expectedEcon=submittedEcon.Copy();
        var publisher=new MutatingPublisher(()=>{
            submitted[0]^=0x55; // original caller can reuse mutable array
            submittedEcon.Scrap=999;
            submittedEcon.Parts[0].Hp=0;
        });
        if(!CanonicalWorldSlotLease.TryAcquire(root,Run,Slot,out var lease)||
            lease==null)return false;
        var gateway=new CanonicalWorldGateway(new DenyPhysics(),
            new NoStage(),new NoPort(),lease);
        Directory.CreateDirectory(Path.GetDirectoryName(StoreDir(root))!);
        WholeGameEconomySlotV3? store=null;
        bool retired=false;
        try
        {
            if(WholeGameEconomySlotV3.Create(StoreDir(root),Run,Slot,lease,
                out store,publisher)!=WholeGameResult.Accepted||store==null)
                return false;
            if(store.Commit(0,submitted,submittedEcon)!=WholeGameResult.Accepted||
               !publisher.Called||store.Quarantined)return false;
            if(store.Read(out long gen,out byte[] world,out var wallet)!=
                WholeGameResult.Accepted||wallet==null||gen!=1||
                !world.SequenceEqual(expected)||wallet.Scrap!=expectedEcon.Scrap||
                wallet.Parts[0].Hp!=expectedEcon.Parts[0].Hp)return false;
        }
        finally
        {
            store?.Dispose();
            retired=gateway.TryCloseAfterPhysicalRetirement(()=>true);
        }
        if(!retired)return false;
        if(CanonicalColdReadV3.ReadClosedSlot(root,Run,Slot,
            out long coldGen,out byte[] coldWorld,out var coldWallet)!=
                WholeGameResult.Accepted||coldWallet==null||coldGen!=1||
            !coldWorld.SequenceEqual(expected)||
            coldWallet.Scrap!=expectedEcon.Scrap)return false;
        Console.WriteLine("W31_MUTATED_CALLER_AFTER_FILE_PUBLISH_NEVER_FALSE_QUARANTINE=PASS");
        return true;
      });

    sealed class MutatingGate:ICanonicalMutationGate
    {
        readonly Action once;
        bool done;
        public bool Called;
        public MutatingGate(Action onFirst){once=onFirst;}
        public bool IsQuiescent()
        {
            if(!done){done=true;once();Called=true;}
            return true;
        }
    }
    sealed class ReplayWorld:ICanonicalStagedWorld
    {
        readonly RunSnapshot state;
        public bool Destroyed;
        public ReplayWorld(RunSnapshot value){state=value;}
        public RunSnapshot Capture()=>state;
        public void Activate(){}
        public void Deactivate(){}
        public void Retire(){Destroyed=true;}
    }
    sealed class ReplayFactory:ICanonicalRigFactory
    {
        readonly List<ReplayWorld> worlds=new List<ReplayWorld>();
        public bool AllGone()=>worlds.All(x=>x.Destroyed);
        public ICanonicalStageOwnership BeginOwnedStage(RunSnapshot s,ColdRestore plan)
          =>new ReplayStage(this,s);
        sealed class ReplayStage:ICanonicalStageOwnership
        {
            readonly ReplayFactory owner;
            readonly RunSnapshot source;
            ReplayWorld? world;
            public ReplayStage(ReplayFactory factory,RunSnapshot state)
            {owner=factory;source=state;}
            public ICanonicalStagedWorld PrepareInactive()
            {
                world=new ReplayWorld(source);owner.worlds.Add(world);
                return world;
            }
            public bool CleanupUnpublishedAndVerifyDestroyed()
            {
                if(world!=null)world.Retire();
                return world==null||world.Destroyed;
            }
        }
    }
    static bool TypedRestoreGateMutatesOriginalInput()
      =>Scratch(root=>{
        byte[] submitted=World(1);
        var econ=Economy(1,58);
        byte[] expected=(byte[])submitted.Clone();
        var expectedEcon=econ.Copy();
        var gate=new MutatingGate(()=>{
            submitted[0]^=0x75; // happens BEFORE original #729 gateway parses bytes
            econ.Scrap=987;
            econ.Parts[0].Hp=0;
        });
        var factory=new ReplayFactory();
        if(CanonicalWholeGameSessionV3.TryOpen(root,Run,Slot,true,
            gate,factory,out var session)!=WholeGameSessionOpen.Accepted||
            session==null)return false;
        bool retired=false;
        try
        {
            if(session.TryRestore(submitted,econ,Run,1)!=
                 CanonicalGateResult.Accepted||
               !gate.Called||session.Quarantined||!session.HasWorld||
               !session.TryCapture(out byte[] actual,out var wallet)||
               wallet==null||!actual.SequenceEqual(expected)||
               wallet.Scrap!=expectedEcon.Scrap||
               wallet.Parts[0].Hp!=expectedEcon.Parts[0].Hp)return false;
        }
        finally
        {
            retired=session.TryCloseAfterPhysicalRetirement(factory.AllGone);
            session.Dispose();
        }
        if(!retired)return false;
        if(CanonicalColdReadV3.ReadClosedSlot(root,Run,Slot,
            out long gen,out byte[] world,out var wallet2)!=
             WholeGameResult.Accepted||wallet2==null||gen!=1||
             !world.SequenceEqual(expected)||
             wallet2.Scrap!=expectedEcon.Scrap)return false;
        Console.WriteLine("W31_729_GATE_MUTATES_ORIGINAL_INPUT_STAGED_PAIR_FROZEN=PASS");
        return true;
      });
    static int Main()
    {
        Check("Caller mutation after actual Save publish retains valid authoritative pair",
            WriterCommitWithHostMutation);
        Check("Gate mutation before original gateway staging retains frozen world and V7",
            TypedRestoreGateMutatesOriginalInput);
        Console.WriteLine("B0162_W31_MUTABLE_INPUT_REAL_OS_TOTAL "+
            (pass+fail)+" PASS "+pass+" FAIL "+fail);
        Console.WriteLine("W31_THIS_IS_SOURCE_REAL_OS_NOT_NATIVE_UNITY_PHYSX");
        return fail==0?0:1;
    }
}
