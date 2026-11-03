// B01-60 P17/W34. Independent ORIGINAL #923 V3 OS Save negative acceptance.
// This compiles the unchanged #923 real writer/codec and actual #877 factory source.
// No Unity PhysX, Rig[] physical adapter or combat-authenticated WinId is simulated.
#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using B01.CanonicalR3World001;
using B01.CanonicalWorldApi001;
using B01.WholeGameEconomy003;
using B01.CanonicalColdRead003;
using B01.DetachedSaveContract001;
using RoboRougelike.R3;
using RoboRougelike.B01.Economy.ApprovedV7;

internal static class B0160W34PhysicalIdentityActualOS
{
    static readonly long Run=6340060;
    static readonly Guid Slot=Guid.Parse("63400034-0000-4000-8000-000000000060");
    static int passed,failed;
    sealed class Idle:ICanonicalMutationGate { public bool IsQuiescent()=>false; }
    sealed class NoPhysicalStage:ICanonicalRigFactory
    {
        public ICanonicalStageOwnership BeginOwnedStage(RunSnapshot s,ColdRestore c)
            =>throw new InvalidOperationException("There is NO Unity Rig[] binder in this OS QA");
    }
    sealed class NoCommit:ICanonicalCommitPort
    {
        public bool Read(long run,out long generation,out string hash)
        {generation=0;hash="";return false;}
        public bool Commit(long run,long prior,string oldHash,long next,string hash)=>false;
    }
    static string SlotDir(string root)=>Path.Combine(root,
        "b0162_canonical_single_authority_v3",
        "run_"+Run+"_slot_"+Slot.ToString("N"));
    static bool Open(string root,out CanonicalWorldGateway? gateway,
        out WholeGameEconomySlotV3? writer)
    {
        gateway=null;writer=null;
        if(!CanonicalWorldSlotLease.TryAcquire(root,Run,Slot,out var lease)||
           lease==null)return false;
        gateway=new CanonicalWorldGateway(new Idle(),new NoPhysicalStage(),
            new NoCommit(),lease);
        Directory.CreateDirectory(Path.GetDirectoryName(SlotDir(root))!);
        return WholeGameEconomySlotV3.Create(SlotDir(root),Run,Slot,lease,out writer)
               ==WholeGameResult.Accepted && writer!=null;
    }
    static bool Close(CanonicalWorldGateway? gateway,WholeGameEconomySlotV3? writer)
    {
        writer?.Dispose();
        return gateway!=null && gateway.TryCloseAfterPhysicalRetirement(()=>true);
    }
    static bool Temp(Func<string,bool> work)
    {
        string root=Path.Combine(Path.GetTempPath(),"b0160-w34-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try{return work(root);}
        finally{Directory.Delete(root,true);}
    }
    static CanonicalR3SnapshotV1 RealLight(long generation)
    {
        // Actual original R3 catalog, NOT a two-part substitute.
        var design=R3VehicleCatalog.Create(R3VehicleKind.Light); // REAL original factory catalog but test-only R3PartState shim has no Id/Weapon fields
        if(design.Parts.Count!=20)throw new InvalidDataException("Wrong original factory");
        var snap=new CanonicalR3SnapshotV1{
            RunId=Run,Generation=generation,VehicleKind=(int)R3VehicleKind.Light
        };
        long partPhysical=880000;
        foreach(var p in design.Parts)
        {
            partPhysical++;
            snap.Parts.Add(new R3SavedPartV1{
                Id=Guid.NewGuid().ToString("N"),PhysicalId=partPhysical,OwnerPhysicalId=880000,
                Location=R3PartLocationV1.Mounted,
                Kind=(int)p.Kind,Weapon=0,Hp=1,
                CellX=p.Cell.x,CellY=p.Cell.y,CellZ=p.Cell.z,
                Contacts=63,Ammo=-1,Mass=1,Qw=1,
                Source="Original Light20 with QA-only physical ID mapping",
                DefenseCharge=-1,DefenseRechargeAge=float.PositiveInfinity,
                Px=p.Cell.x*.55f,Py=p.Cell.y*.55f,Pz=p.Cell.z*.55f
            });
        }
        if(!CanonicalR3WorldCodecV1.Valid(snap))
            throw new InvalidDataException("Original Light20 mapping rejected before OS test");
        return snap;
    }
    static ApprovedEconomySnapshotV7 Wallet(CanonicalR3SnapshotV1 world,int scrap)
    {
        return new ApprovedEconomySnapshotV7{
            Version=7,OwnerId=Slot.ToString("N"),RunId=Run.ToString(),
            Generation=world.Generation,Scrap=scrap,Wins=0,FreeUsedForWin=0,
            Parts=world.Parts.Select(p=>new OwnedModuleV7{
                Id=p.Id,Hp=p.Hp,PurchaseScrap=100,ReservationId="",
                Category=p.Kind==3?"Core":p.Kind==1?"Wheel":
                         p.Kind==2?"Gun":"Armor"
            }).ToList(),
            Pending=new List<RebuildTicketV7>(),
            ConsumedVictoryIds=new List<string>(),
            Loaner=new OneShotLoanerV7{
                Phase=LoanerPhaseV7.None,EverEntered=false,
                EncounterId="",PermitId=""
            }
        };
    }
    static bool StablePartIdsAcrossTwoAtomicGenerationsAndActualColdRead()
      =>Temp(root=>{
        var world=RealLight(1);
        var wallet=Wallet(world,80);
        string id=world.Parts[5].Id;
        long physical=world.Parts[5].PhysicalId;
        if(!Open(root,out var gateway,out var store)||store==null)return false;
        bool done=false;
        try{
            if(store.Commit(0,CanonicalR3WorldCodecV1.Encode(world),wallet)
               !=WholeGameResult.Accepted)return false;
            world.Generation=2;
            wallet.Generation=2;wallet.Scrap=45;
            world.Parts[5].Ammo=17;
            done=store.Commit(1,CanonicalR3WorldCodecV1.Encode(world),wallet)
                 ==WholeGameResult.Accepted && !store.Quarantined;
        }finally{if(!Close(gateway,store))done=false;}
        if(!done)return false;
        if(CanonicalColdReadV3.ReadClosedSlot(root,Run,Slot,
            out long generation,out byte[] raw,out var coldWallet)
           !=WholeGameResult.Accepted ||generation!=2||coldWallet==null||
           coldWallet.Scrap!=45||
           !CanonicalR3WorldCodecV1.TryDecode(raw,out var actual)||actual==null||
           actual.Parts.Count!=20)return false;
        var same=actual.Parts.Single(x=>x.Id==id);
        return same.PhysicalId==physical&&same.Ammo==17&&
               actual.Parts.Select(x=>x.Id).SequenceEqual(world.Parts.Select(x=>x.Id));
      });
    // This EXPECTED PRODUCT RED test deliberately demonstrates a real current
    // inter-generation GUID -> PhysicalId binding hole in the single original
    // #923 Save writer. GREEN means the DEFECT was correctly reproduced.
    static bool SameOwnedGuidCanChangeItsPersistentPhysicalLedgerId()
      =>Temp(root=>{
        var world=RealLight(1);
        var wallet=Wallet(world,60);
        string guid=world.Parts[7].Id;
        long oldPhysical=world.Parts[7].PhysicalId;
        if(!Open(root,out var gateway,out var store)||store==null)return false;
        bool demonstrated=false;
        try{
            if(store.Commit(0,CanonicalR3WorldCodecV1.Encode(world),wallet)
               !=WholeGameResult.Accepted)return false;
            world.Generation=2;wallet.Generation=2;
            world.Parts[7].PhysicalId=oldPhysical+1000000; // valid, unique, NO owned GUID change
            if(!CanonicalR3WorldCodecV1.Valid(world))return false;
            // This currently ACCEPTS because #923 compares current GUID+HP
            // vs current V7 but never compares prior GUID -> physical ID.
            var result=store.Commit(1,CanonicalR3WorldCodecV1.Encode(world),wallet);
            demonstrated=result==WholeGameResult.Accepted&&!store.Quarantined;
            Console.WriteLine("W34_ORIGINAL_923_SAME_GUID_PHYSICAL_ID_REBIND="+result+
                " EXPECTED_PRODUCT_P0_RED=Accepted; source witness, NOT Unity");
        }finally{if(!Close(gateway,store))demonstrated=false;}
        if(!demonstrated)return false;
        if(CanonicalColdReadV3.ReadClosedSlot(root,Run,Slot,
           out long generation,out byte[] bytes,out var econ)
           !=WholeGameResult.Accepted||generation!=2||econ==null||
           !CanonicalR3WorldCodecV1.TryDecode(bytes,out var actual)||actual==null)
             return false;
        var altered=actual.Parts.Single(x=>x.Id==guid);
        bool witnessed=altered.PhysicalId==oldPhysical+1000000 &&
                       econ.Parts.Count==20 &&
                       econ.Parts.Single(x=>x.Id==guid).Hp==altered.Hp;
        if(witnessed)Console.WriteLine(
          "B0160_W34_SOURCE_WITNESS_PASS_PRODUCT_R3_LEDGER_IDENTITY_P0_RED");
        return witnessed;
      });
    static bool DuplicatePhysicalIdsWithinGenerationAreStillRejected()
    {
        var world=RealLight(1);
        world.Parts[7].PhysicalId=world.Parts[6].PhysicalId;
        if(CanonicalR3WorldCodecV1.Valid(world))return false;
        try{
            CanonicalR3WorldCodecV1.Encode(world);
            return false;
        }catch(InvalidDataException){return true;}
    }
    static void Check(string name,Func<bool> run)
    {
        try{if(run()){passed++;Console.WriteLine("PASS "+name);}
             else{failed++;Console.WriteLine("FAIL "+name);}}
        catch(Exception ex){failed++;Console.WriteLine("FAIL "+name+" "+ex);}
    }
    static int Main()
    {
        Check("Real factory Light20 stable GUID+physical ID 2 generations OS cold",
            StablePartIdsAcrossTwoAtomicGenerationsAndActualColdRead);
        Check("Negative: original #923 accepts SAME owned GUID with DIFFERENT physical ID after g2",
            SameOwnedGuidCanChangeItsPersistentPhysicalLedgerId);
        Check("Positive: within-generation duplicate numeric physical IDs denied",
            DuplicatePhysicalIdsWithinGenerationAreStillRejected);
        Console.WriteLine("B0160_W34_ORIGINAL_923_PHYSICAL_ID_OS_SOURCE_TOTAL "+
            (passed+failed)+" PASS "+passed+" FAIL "+failed);
        Console.WriteLine("B0160_W34_NATIVE_UNITY=NOT_RUN REAL_R3_BINDER=NOT_INTEGRATED");
        return failed==0?0:1;
    }
}
