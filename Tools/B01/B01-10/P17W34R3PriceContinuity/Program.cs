// B01-10 P17/W34: EXECUTED SOURCE-ONLY negative economic-authority witness.
// This compiles EXACT existing #923 R3 codec and one original V3 disk writer.
// NO new game runtime, no second Save, no simulated gameplay acceptance.
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

internal static class B0110W34PriceContinuity
{
    const long RunId=6333410;
    static readonly Guid Slot=Guid.Parse("63334100-0000-4000-8000-000000000010");
    static int passed,failed;

    sealed class Idle : ICanonicalMutationGate { public bool IsQuiescent()=>false; }
    sealed class NoRig : ICanonicalRigFactory
    {
        public ICanonicalStageOwnership BeginOwnedStage(RunSnapshot s,ColdRestore c)
            =>throw new InvalidOperationException("No Unity physical stage in source-only QA");
    }
    sealed class NoCommit : ICanonicalCommitPort
    {
        public bool Read(long r,out long gen,out string hash)
        { gen=0;hash="";return false; }
        public bool Commit(long r,long prior,string oldHash,long next,string hash)
            =>false;
    }
    static void Check(string name,Func<bool> assertion)
    {
        try
        {
            bool result=assertion();
            if(result){passed++;Console.WriteLine("PASS "+name);}
            else {failed++;Console.WriteLine("FAIL "+name);}
        }
        catch(Exception ex){failed++;Console.WriteLine("FAIL "+name+" "+ex);}
    }
    static string SlotDir(string root)=>Path.Combine(root,
        "b0162_canonical_single_authority_v3",
        "run_"+RunId+"_slot_"+Slot.ToString("N"));
    static bool Open(string root,out CanonicalWorldGateway? owner,
        out WholeGameEconomySlotV3? store)
    {
        owner=null;store=null;
        if(!CanonicalWorldSlotLease.TryAcquire(root,RunId,Slot,out var lease)||
           lease==null)return false;
        owner=new CanonicalWorldGateway(new Idle(),new NoRig(),new NoCommit(),lease);
        Directory.CreateDirectory(Path.GetDirectoryName(SlotDir(root))!);
        return WholeGameEconomySlotV3.Create(SlotDir(root),RunId,Slot,lease,out store)
            ==WholeGameResult.Accepted&&store!=null;
    }
    static bool Close(CanonicalWorldGateway? owner,WholeGameEconomySlotV3? store)
    {
        store?.Dispose();
        return owner!=null&&owner.TryCloseAfterPhysicalRetirement(()=>true);
    }
    static bool Temp(Func<string,bool> run)
    {
        string root=Path.Combine(Path.GetTempPath(),"b0110-w34-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try{return run(root);}
        finally{Directory.Delete(root,true);}
    }
    static CanonicalR3SnapshotV1 World(bool destroyFirstArmor)
    {
        // The actual ORIGINAL game factory Create() is compiled from #877,
        // not a recreated Light layout or a fake two-part fixture.
        var design=R3VehicleCatalog.Create(R3VehicleKind.Light);
        var world=new CanonicalR3SnapshotV1{
            RunId=RunId,Generation=1,VehicleKind=(int)R3VehicleKind.Light
        };
        int i=0;
        foreach(var source in design.Parts)
        {
            i++;
            world.Parts.Add(new R3SavedPartV1 {
                Id=Guid.NewGuid().ToString("N"),PhysicalId=770000+i,
                OwnerPhysicalId=770000,Kind=(int)source.Kind,Weapon=0,
                Location=R3PartLocationV1.Mounted,CellX=source.Cell.x,
                CellY=source.Cell.y,CellZ=source.Cell.z,Contacts=63,
                Hp=1,Ammo=-1,Mass=1,Qw=1,
                DefenseCharge=-1,DefenseRechargeAge=float.PositiveInfinity,
                Px=source.Cell.x*.55f,Py=source.Cell.y*.55f,Pz=source.Cell.z*.55f
            });
        }
        if(destroyFirstArmor)
            world.Parts.First(p=>p.Kind==(int)R3PartKind.Armor).Hp=0;
        return world;
    }
    static string Category(int k)=>k==(int)R3PartKind.Core?"Core":
        k==(int)R3PartKind.Wheel?"Wheel":
        k==(int)R3PartKind.Gun?"Gun":"Armor";
    static int DefaultPurchase(int k)=>k==(int)R3PartKind.Armor?5:
        k==(int)R3PartKind.Wheel?15:
        k==(int)R3PartKind.Gun?30:5;
    static ApprovedEconomySnapshotV7 Econ(CanonicalR3SnapshotV1 world)
    {
        return new ApprovedEconomySnapshotV7 {
            Version=7,OwnerId=Slot.ToString("N"),RunId=RunId.ToString(),
            Generation=world.Generation,Wins=0,Scrap=0,FreeUsedForWin=0,
            ConsumedVictoryIds=new List<string>(),
            Pending=new List<RebuildTicketV7>(),
            Loaner=new OneShotLoanerV7 {
                Phase=LoanerPhaseV7.None,EverEntered=false,
                EncounterId="",PermitId=""
            },
            Parts=world.Parts.Select(p=>new OwnedModuleV7 {
                Id=p.Id,Category=Category(p.Kind),Hp=p.Hp,
                PurchaseScrap=DefaultPurchase(p.Kind),ReservationId=""
            }).ToList()
        };
    }
    static bool ColdPersistPrice(int overrideArmorPrice,int expectedQuote)
    {
        return Temp(root=>{
            var world=World(false);
            var econ=Econ(world);
            var armor=world.Parts.First(p=>p.Kind==(int)R3PartKind.Armor);
            var item=econ.Parts.Single(p=>p.Id==armor.Id);
            item.PurchaseScrap=overrideArmorPrice;
            if(!CanonicalR3WorldCodecV1.Valid(world)||world.Parts.Count!=20||
               !ApprovedEconomyPolicyV7.TryPaidQuote(item.PurchaseScrap,out int quote)||
               quote!=expectedQuote)return false;

            if(!Open(root,out var owner,out var store)||store==null)return false;
            bool accepted=false;
            try
            {
                accepted=store.Commit(0,CanonicalR3WorldCodecV1.Encode(world),econ)
                    ==WholeGameResult.Accepted;
            }
            finally{if(!Close(owner,store))accepted=false;}
            if(!accepted)return false;
            if(CanonicalColdReadV3.ReadClosedSlot(root,RunId,Slot,
                 out long gen,out byte[] bytes,out var cold)!=
                 WholeGameResult.Accepted||gen!=1||cold==null||
                 !CanonicalR3WorldCodecV1.TryDecode(bytes,out var restored)||
                 restored==null||restored.Parts.Count!=20)return false;
            var post=cold.Parts.Single(p=>p.Id==armor.Id);
            if(post.PurchaseScrap!=overrideArmorPrice||
               !ApprovedEconomyPolicyV7.TryPaidQuote(post.PurchaseScrap,
                  out int coldQuote)||coldQuote!=expectedQuote)return false;
            return true;
        });
    }
    static bool WrongPaidZeroArmorBookingPersists()
    {
        return Temp(root=>{
            var world=World(true);
            var econ=Econ(world);
            var armor=world.Parts.First(p=>p.Kind==(int)R3PartKind.Armor);
            var owned=econ.Parts.Single(p=>p.Id==armor.Id);
            owned.PurchaseScrap=1; // Deliberately WRONG for default Armor(5).
            if(!ApprovedEconomyPolicyV7.TryReserveDestroyed(econ,armor.Id,
                out var booked,out _)||booked==null||
               booked.Pending.Count!=1||booked.Pending[0].Free||
               booked.Pending[0].ChargedScrap!=0||
               booked.Parts.Single(p=>p.Id==armor.Id).Hp!=0)return false;
            if(booked.Generation!=2)return false;
            if(!Open(root,out var owner,out var store)||store==null)return false;
            bool accepted=false;
            try
            {
                accepted=store.Commit(0,CanonicalR3WorldCodecV1.Encode(world),econ)
                   ==WholeGameResult.Accepted;
                if(accepted)
                {
                    world.Generation=booked.Generation;
                    accepted=store.Commit(1,CanonicalR3WorldCodecV1.Encode(world),booked)
                       ==WholeGameResult.Accepted;
                }
            }
            finally{if(!Close(owner,store))accepted=false;}
            if(!accepted)return false;
            if(CanonicalColdReadV3.ReadClosedSlot(root,RunId,Slot,
                 out long gen,out byte[] bytes,out var cold)!=
                 WholeGameResult.Accepted||cold==null||gen!=2||
                 !CanonicalR3WorldCodecV1.TryDecode(bytes,out var restored)||
                 restored==null)return false;
            var saved=cold.Parts.Single(p=>p.Id==armor.Id);
            return saved.PurchaseScrap==1&&saved.Hp==0&&cold.Pending.Count==1&&
                cold.Pending[0].ChargedScrap==0&&!cold.Pending[0].Free&&
                restored.Parts.Single(p=>p.Id==armor.Id).Hp==0;
        });
    }

    static int Main()
    {
        Check("ORIGINAL Light factory default Armor P5 -> V7 PAID 3 cold roundtrip",
           ()=>ColdPersistPrice(5,3));
        Check("EXPECTED PRODUCT AUTHORITY GAP: #923 accepts default Armor saved as P100 -> PAID 65",
           ()=>ColdPersistPrice(100,65));
        Check("EXPECTED PRODUCT AUTHORITY GAP: a forged/default-mismatched Armor P1 books PAID0 pending across cold",
           WrongPaidZeroArmorBookingPersists);
        Console.WriteLine($"B0110_W34_R3_ORIGINAL_ONE_WRITER_SOURCE_WITNESS_TOTAL {passed} PASS {failed} FAIL");
        Console.WriteLine("W34_SOURCE_SAVE_ADMISSION_ACCEPTS_UNAUTHENTICATED_GAME_PRICE_NOT_AN_ACTUAL_UNITY_EXPLOIT");
        Console.WriteLine("NATIVE_UNITY_G1=NOT_RUN WINDOWS_PLAYER=NOT_BUILT GAME_BINDER=ABSENT");
        return failed==0?0:1;
    }
}
