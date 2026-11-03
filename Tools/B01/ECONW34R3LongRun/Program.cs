// B01-62 W33: original R3 factory sizes with actual original single Save V3.
// The game factory below is ORIGINAL SOURCE compiled through a tiny type shim.
// There is no real Unity Physics or authenticated combat receipt in this test.
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
using B01.SecureReattachV2;
using B01.DebrisRepulse001;
using RoboRougelike.R3;
using RoboRougelike.B01.Economy.ApprovedV7;
internal static class W34EconomyCampaign
{
    const long Run=6340022;
    static readonly Guid Slot=Guid.Parse("63400034-0000-4000-8000-000000000022");
    static int pass,fail;
    static void Test(string label,Func<bool> act)
    {
        try{if(act()){pass++;Console.WriteLine("PASS "+label);}
             else{fail++;Console.WriteLine("FAIL "+label);}}
        catch(Exception x){fail++;Console.WriteLine("FAIL "+label+" "+x);}
    }
    static bool Temp(Func<string,bool> action)
    {
        string dir=Path.Combine(Path.GetTempPath(),"b0162-w33-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try{return action(dir);}finally{Directory.Delete(dir,true);}
    }
    static string SlotDir(string root)=>Path.Combine(root,
      "b0162_canonical_single_authority_v3",
      "run_"+Run+"_slot_"+Slot.ToString("N"));
    sealed class Idle:ICanonicalMutationGate{public bool IsQuiescent()=>false;}
    sealed class NoRig:ICanonicalRigFactory
    {
        public ICanonicalStageOwnership BeginOwnedStage(RunSnapshot s,ColdRestore c)
            =>throw new InvalidOperationException("No Unity physical stage in this OS test");
    }
    sealed class NoCommit:ICanonicalCommitPort
    {
        public bool Read(long run,out long g,out string h){g=0;h="";return false;}
        public bool Commit(long run,long prior,string old,long next,string hash)=>false;
    }
    static bool Open(string root,out CanonicalWorldGateway? gateway,
        out WholeGameEconomySlotV3? store)
    {
        gateway=null;store=null;
        if(!CanonicalWorldSlotLease.TryAcquire(root,Run,Slot,out var lease)||
            lease==null)return false;
        gateway=new CanonicalWorldGateway(new Idle(),new NoRig(),new NoCommit(),lease);
        Directory.CreateDirectory(Path.GetDirectoryName(SlotDir(root))!);
        return WholeGameEconomySlotV3.Create(SlotDir(root),Run,Slot,lease,out store)
              ==WholeGameResult.Accepted&&store!=null;
    }
    static bool Close(CanonicalWorldGateway? gateway,WholeGameEconomySlotV3? store)
    {
        store?.Dispose();
        return gateway!=null&&gateway.TryCloseAfterPhysicalRetirement(()=>true);
    }
    static CanonicalR3SnapshotV1 World(R3VehicleKind kind,long generation)
    {
        var original=R3VehicleCatalog.Create(kind); // exact current game factory
        var snap=new CanonicalR3SnapshotV1{
            RunId=Run,Generation=generation,VehicleKind=(int)kind
        };
        long counter=0;
        foreach(var p in original.Parts)
        {
            counter++;
            snap.Parts.Add(new R3SavedPartV1{
                Id=Guid.NewGuid().ToString("N"),
                PhysicalId=770000+counter,OwnerPhysicalId=770000,
                Kind=(int)p.Kind,Weapon=0,Location=R3PartLocationV1.Mounted,
                CellX=p.Cell.x,CellY=p.Cell.y,CellZ=p.Cell.z,
                Contacts=63,Hp=1,Ammo=-1,Mass=1,Qw=1,
                DefenseCharge=-1,DefenseRechargeAge=float.PositiveInfinity,
                Source="real game factory source",
                Px=p.Cell.x*.55f,Py=p.Cell.y*.55f,Pz=p.Cell.z*.55f
            });
        }
        return snap;
    }
    static ApprovedEconomySnapshotV7 Econ(CanonicalR3SnapshotV1 w,int scrap=0)
    {
        var e=new ApprovedEconomySnapshotV7{
            Version=7,OwnerId=Slot.ToString("N"),RunId=Run.ToString(),
            Generation=w.Generation,Scrap=scrap,Wins=0,FreeUsedForWin=0,
            Parts=new List<OwnedModuleV7>(),Pending=new List<RebuildTicketV7>(),
            ConsumedVictoryIds=new List<string>(),
            Loaner=new OneShotLoanerV7{
                Phase=LoanerPhaseV7.None,EverEntered=false,
                EncounterId="",PermitId=""
            }
        };
        foreach(var p in w.Parts)
            e.Parts.Add(new OwnedModuleV7{
                Id=p.Id,Hp=p.Hp,
                Category=p.Kind==3?"Core":p.Kind==1?"Wheel":
                         p.Kind==2?"Gun":"Armor",
                PurchaseScrap=100,ReservationId=""
            });
        return e;
    }

    // These checks drive the ORIGINAL #923 disk writer and real original #780
    // V7 reducer. All victory IDs and eligibility facts are HARNESS INPUTS.
    // This is not a Unity fixture or authenticated player combat evidence.
    static int checks;
    static void Need(bool ok,string label)
    {
        if(!ok)throw new InvalidOperationException("W34_B0122_SOURCE_RED: "+label);
        checks++;
    }
    static void Save(WholeGameEconomySlotV3 slot,CanonicalR3SnapshotV1 world,
                     ApprovedEconomySnapshotV7 economy,string label)
    {
        world.Generation=economy.Generation;
        Need(CanonicalR3WorldCodecV1.Valid(world),label+" valid 139/83 owned world");
        Need(ApprovedEconomyPolicyV7.Valid(economy),label+" valid original V7");
        byte[] binary=CanonicalR3WorldCodecV1.Encode(world);
        Need(slot.Commit(economy.Generation-1,binary,economy)==WholeGameResult.Accepted,
            label+" actual original CAS FileStream");
        Need(slot.Read(out long generation,out byte[] reread,out var wallet)==
             WholeGameResult.Accepted && generation==economy.Generation &&
             wallet!=null && wallet.Wins==economy.Wins &&
             wallet.Scrap==economy.Scrap && wallet.Pending.Count==economy.Pending.Count,
             label+" actual original one-file readback");
        Need(CanonicalR3WorldCodecV1.TryDecode(reread,out var restored) &&
             restored!=null && restored.Parts.Count==world.Parts.Count &&
             restored.Parts.Select(p=>p.Id).SequenceEqual(world.Parts.Select(p=>p.Id)),
             label+" dynamic full rig stable identity");
    }
    static void SyncHp(CanonicalR3SnapshotV1 world,ApprovedEconomySnapshotV7 economy)
    {
        foreach(var p in world.Parts)
            p.Hp=economy.Parts.Single(e=>e.Id==p.Id).Hp;
    }
    static void ScenarioCampaign(string root)
    {
        Directory.CreateDirectory(root);
        CanonicalR3SnapshotV1 world=World(R3VehicleKind.Heavy,1);
        var armor=world.Parts.Where(p=>p.Kind==0).ToList();
        Need(world.Parts.Count==139,"real original Heavy factory full 139 parts");
        Need(armor.Count>=27,"genuine Heavy factory 27 distinct armor modules");
        // Actual dynamic codec includes mounted, inventory and detached records.
        armor[25].Location=R3PartLocationV1.Inventory;
        armor[26].Location=R3PartLocationV1.Detached;
        armor[26].ConsumedDetach=true;
        armor[25].Ammo=8;
        armor[26].Vx=2.5f;
        armor[26].Wx=.5f;
        var initial=armor[0];
        initial.Hp=0;
        var economy=Econ(world,0);
        economy.Parts.Single(p=>p.Id==initial.Id).PurchaseScrap=1;
        Need(ApprovedEconomyPolicyV7.TryPaidQuote(1,out int zero) && zero==0,
             "purchase1 always PAID quote0, not free starter coupon");
        Need(Open(root,out var gateway,out var store)&&store!=null,
             "one original OS owner lease and one Save Store");
        try
        {
            Save(store!,world,economy,"initial heavy");
            Need(ApprovedEconomyPolicyV7.TryReserveDestroyed(economy,initial.Id,
                out var paid,out var why) && paid!=null, "zero-quote paid reservation");
            Need(paid!.Pending.Count==1 && !paid.Pending[0].Free &&
                 paid.Pending[0].ChargedScrap==0 && paid.Wins==0 &&
                 paid.Parts.Single(p=>p.Id==initial.Id).Hp==0,
                 "real V7 paid zero leaves destroyed part 0HP pending at startup");
            economy=paid;
            Save(store!,world,economy,"reserve owned P1 paid0");
            string prior=initial.Id;
            for(int win=1;win<=24;win++)
            {
                var newlyDestroyed=armor[win];
                Need(newlyDestroyed.Location==R3PartLocationV1.Mounted &&
                     newlyDestroyed.Hp==1,
                     "one NEW original mounted armor casualty for modeled fight "+win);
                // Model test-only combat damage PRIOR to the winner's receipt.
                // The authentic Unity combat receipt must be supplied by a FUTURE host.
                newlyDestroyed.Hp=0;
                economy.Parts.Single(p=>p.Id==newlyDestroyed.Id).Hp=0;
                string winId="w34-synthetic-owned-receipt-"+win.ToString("D2");
                string encounter="w34-source-encounter-"+win.ToString("D2");
                Need(ApprovedEconomyPolicyV7.TryStageTrustedVictory(economy,
                    winId,encounter,out var victory,out var reason)&&victory!=null,
                    "original V7 distinct source-injected victory stage "+win);
                Need(victory!.Wins==win &&
                     victory.ConsumedVictoryIds.Count==win &&
                     victory.Parts.Single(p=>p.Id==prior).Hp==1 &&
                     victory.Parts.Single(p=>p.Id==newlyDestroyed.Id).Hp==0 &&
                     victory.Pending.Count==0,
                     "only previous-fight 0HP restores, current-fight casualty stays zero "+win);
                Need(!ApprovedEconomyPolicyV7.TryStageTrustedVictory(victory,
                    winId,encounter,out _,out _),
                    "win id replay never duplicates rewards "+win);
                economy=victory;
                SyncHp(world,economy);
                Save(store!,world,economy,"victory "+win);
                Need(ApprovedEconomyPolicyV7.TryReserveDestroyed(economy,
                    newlyDestroyed.Id,out var booked,out var bookReason) &&
                    booked!=null,"newly destroyed owned ID queued only AFTER modeled win "+win);
                Need(booked!.Pending.Count==1 &&
                     booked.Pending[0].Free &&
                     booked.Pending[0].AfterWins==win &&
                     booked.Pending[0].ChargedScrap==0 &&
                     booked.FreeUsedForWin==win &&
                     booked.Parts.Single(p=>p.Id==newlyDestroyed.Id).Hp==0,
                     "exactly one newly earned free quota pending NEXT victory "+win);
                Need(!ApprovedEconomyPolicyV7.TryReserveDestroyed(booked,
                    newlyDestroyed.Id,out _,out _),
                    "cannot reserve duplicate owned physical GUID "+win);
                economy=booked;
                Save(store!,world,economy,"free next-win booking "+win);
                prior=newlyDestroyed.Id;
            }
            Need(economy.Wins==24&&economy.ConsumedVictoryIds.Count==24 &&
                 economy.Scrap==0&&economy.Pending.Count==1 &&
                 economy.Pending[0].Free && economy.Pending[0].ModuleId==prior,
                 "final 24 wins, 24 prior settlements, 1 genuine next-win PENDING");
            Need(world.Parts.Single(p=>p.Id==prior).Hp==0,
                 "last destroyed part not healed by its own modeled encounter");
            Need(world.Parts.Count(p=>p.Location==R3PartLocationV1.Inventory)==1 &&
                 world.Parts.Count(p=>p.Location==R3PartLocationV1.Detached)==1,
                 "inventory and physically detached persistent part records unchanged");
            Console.WriteLine("W34_923_24_WINS_REAL_ONE_FILE_SAVE_PASS wins=24 parts=139 pending=1");
        }
        finally
        {
            Need(Close(gateway,store),"close OS owner after full campaign");
        }
    }
    static void ScenarioLoaner(string root)
    {
        Directory.CreateDirectory(root);
        var world=World(R3VehicleKind.Standard,1);
        Need(world.Parts.Count==83,"actual original Standard 83");
        var core=world.Parts.Single(p=>p.Kind==3);
        var wheel=world.Parts.First(p=>p.Kind==1);
        wheel.Hp=0;
        var eco=Econ(world,0);
        Need(!ApprovedEconomyPolicyV7.TryReserveDestroyed(eco,wheel.Id,
            out _,out _),
            "expensive essential Wheel cannot be booked with 0 currency");
        Need(Open(root,out var gateway,out var store)&&store!=null,
             "one canonical OS owner for 83-part source loaner");
        try
        {
            Save(store!,world,eco,"loaner initial blocked Wheel");
            string encounter="w34-source-next-borrowed-fight";
            string permit=Guid.NewGuid().ToString("N");
            var facts=new CanonicalDeploymentFactsV7{
                OwnerId=eco.OwnerId,RunId=eco.RunId,Generation=eco.Generation,
                CoreId=core.Id,EncounterId=encounter,CoreFunctional=true,
                CanDeploy=false,HasGenuineNextChallenge=true,
                EssentialDestroyedPhysicalIds=new[]{wheel.Id}
            };
            Need(ApprovedEconomyPolicyV7.TryReserveOneShotLoaner(eco,facts,permit,
                out var reserved,out _)&&reserved!=null,
                "original loaner reducer source facts only, NO physical Unity probe");
            eco=reserved!;
            Need(eco.Loaner.Phase==LoanerPhaseV7.Reserved,
                "once reserved weaker source loaner");
            Save(store!,world,eco,"loaner one-shot reserved");
            Need(ApprovedEconomyPolicyV7.TryConsumeLoanerOnTrustedChallengeEntry(
                eco,encounter,permit,out var entered,out _)&&entered!=null,
                "source-supplied loaner challenge entry");
            eco=entered!;
            Save(store!,world,eco,"loaner one-shot entered");
            Need(ApprovedEconomyPolicyV7.TryStageTrustedVictory(eco,
                 "w34-source-borrowed-win-one",encounter,out var won,out _)
                 && won!=null,"modeled loaner victory");
            eco=won!;
            Need(eco.Loaner.Phase==LoanerPhaseV7.Spent &&
                 eco.Loaner.EverEntered && eco.Wins==1 &&
                 eco.Parts.Single(p=>p.Id==wheel.Id).Hp==0,
                 "borrowed vehicle consumed; essential original wheel still broken");
            Save(store!,world,eco,"loaner spent modeled Win1");
            Need(ApprovedEconomyPolicyV7.TryReserveDestroyed(eco,wheel.Id,
                out var ticket,out _)&&ticket!=null,
                "Win1 releases ONE free reservation, not early repair");
            eco=ticket!;
            Need(eco.Pending.Count==1&&eco.Pending[0].Free&&
                 eco.Pending[0].AfterWins==1&&
                 eco.Parts.Single(p=>p.Id==wheel.Id).Hp==0&&
                 eco.Loaner.Phase==LoanerPhaseV7.Spent,
                 "one-shot loaner spent + free essential Wheel PENDING Win2");
            Save(store!,world,eco,"spent loaner last available free order pending");
            facts.Generation=eco.Generation;
            Need(!ApprovedEconomyPolicyV7.TryReserveOneShotLoaner(eco,facts,
                Guid.NewGuid().ToString("N"),out _,out _),
                "spent loaner cannot be reserved again after Win1");
            Need(!ApprovedEconomyPolicyV7.TryStageTrustedVictory(eco,
                "w34-source-borrowed-win-one",encounter,out _,out _),
                "borrowed Win1 event cannot be replayed for fake Win2");
            Console.WriteLine("W34_923_LOANER_SPENT_SOFTLOCK_RISK_SOURCE_RED wins=1 wheelHP=0 wallet=0 loaner=Spent ticketAfterWins=1");
        }
        finally
        {
            Need(Close(gateway,store),"close OS owner after loaner state");
        }
    }
    static void Cold(string root)
    {
        Need(CanonicalColdReadV3.ReadClosedSlot(
             Path.Combine(root,"campaign"),Run,Slot,
             out long generation,out byte[] data,out var economy)==
             WholeGameResult.Accepted && economy!=null,
             "second REAL CLR cold read saved 139-part campaign");
        Need(generation==50&&economy!.Generation==50&&economy.Wins==24 &&
             economy.Scrap==0 && economy.ConsumedVictoryIds.Count==24 &&
             economy.Pending.Count==1&&economy.Pending[0].Free,
             "cold 50 generations, wallet, wins and final free queue");
        Need(CanonicalR3WorldCodecV1.TryDecode(data,out var world)&&
             world!=null&&world.Parts.Count==139&&
             world.Parts.Count(p=>p.Location==R3PartLocationV1.Inventory)==1&&
             world.Parts.Count(p=>p.Location==R3PartLocationV1.Detached)==1&&
             world.Parts.Single(p=>p.Id==economy!.Pending[0].ModuleId).Hp==0,
             "cold full 139-part dynamic IDs, physical detached, pending HP0");
        Need(!ApprovedEconomyPolicyV7.TryStageTrustedVictory(economy!,
             "w34-synthetic-owned-receipt-24","w34-source-encounter-24",
             out _,out _),"cold consumed win24 cannot reaward");
        Need(CanonicalColdReadV3.ReadClosedSlot(
             Path.Combine(root,"loaner"),Run,Slot,
             out long borrowedGen,out byte[] borrowed,out var loaner)==
             WholeGameResult.Accepted && loaner!=null,
             "second REAL CLR cold read saved one-shot loaner");
        Need(borrowedGen==5&&loaner!.Generation==5&&loaner.Wins==1&&
             loaner.Scrap==0&&loaner.Pending.Count==1&&
             loaner.Loaner.Phase==LoanerPhaseV7.Spent &&
             loaner.Loaner.EverEntered&&loaner.Pending[0].AfterWins==1,
             "cold spent loaner and Win2-only free booking persist together");
        Need(CanonicalR3WorldCodecV1.TryDecode(borrowed,out var loanWorld)&&
             loanWorld!=null&&loanWorld.Parts.Count==83&&
             loanWorld.Parts.Single(p=>p.Id==loaner!.Pending[0].ModuleId).Hp==0,
             "cold original 83-part Standard essential Wheel remains HP0");
        Console.WriteLine("W34_923_COLD_INDEPENDENT_CLR_PASS heavy139_wins=24 standard83_loaner=Spent");
    }
    static int Main(string[] args)
    {
        try
        {
            if(args.Length!=2)throw new ArgumentException("Expected --scenario or --cold + temp root");
            if(args[0]=="--scenario")
            {
                ScenarioCampaign(Path.Combine(args[1],"campaign"));
                ScenarioLoaner(Path.Combine(args[1],"loaner"));
            }
            else if(args[0]=="--cold")Cold(args[1]);
            else throw new ArgumentException("Unrecognized test stage");
            Console.WriteLine("W34_923_ORIGINAL_SOURCE_ASSERTIONS_PASS count="+checks);
            Console.WriteLine("W34_NATIVE_UNITY_G1_NOT_RUN_REAL_COMBAT_RECEIPT_NOT_ATTESTED_PRODUCT_SOFTLOCK_RED");
            return 0;
        }
        catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
    }
}
