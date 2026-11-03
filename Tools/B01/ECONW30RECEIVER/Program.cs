// W30 B01-22: real original #826 one-file owner writer + immutable #855
// admission C#9. Source/physical OS QA only. No new writer, no Unity shim.
#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using B01.WholeGameEconomy003;
using B01.CanonicalWorldApi001;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;
using B01.SecureReattachV2;
using RoboRougelike.B01.Economy.ApprovedV7;
using RoboRougelike.B01.Economy.AdmissionW29;

internal static class W30OriginalSaveAdmissionQA
{
    private const long Run=6250062;
    private static readonly Guid Slot=Guid.Parse("62502625-0000-4000-8000-000000000556");
    private static readonly string TicketId="33333333333333333333333333333333";
    private static int assertions;
    private static void Require(bool success,string message)
    {
        if(!success)throw new InvalidOperationException("W30_SOURCE_RED: "+message);
        assertions++;
    }
    private sealed class Gate:ICanonicalMutationGate
    {
        public bool IsQuiescent()=>true;
    }
    private sealed class Factory:ICanonicalRigFactory
    {
        public ICanonicalStageOwnership BeginOwnedStage(RunSnapshot s,ColdRestore c)
            =>throw new InvalidOperationException("NO source fixture may stage Unity physical bodies");
    }
    private sealed class Port:ICanonicalCommitPort
    {
        public bool Read(long run,out long gen,out string hash)
        {gen=0;hash="";return false;}
        public bool Commit(long run,long prior,string priorHash,long next,string hash)=>false;
    }
    private static string Dir(string root)=>Path.Combine(root,
        "b0162_canonical_single_authority_v3",
        "run_"+Run.ToString(CultureInfo.InvariantCulture)+"_slot_"+Slot.ToString("N"));
    private static CanonicalWorldGateway OpenOwner(string root,out CanonicalWorldSlotLease lease)
    {
        Require(CanonicalWorldSlotLease.TryAcquire(root,Run,Slot,
            out CanonicalWorldSlotLease? acquired)&&acquired!=null,
            "physical original #729 .owner acquired");
        lease=acquired!;
        return new CanonicalWorldGateway(new Gate(),new Factory(),new Port(),lease);
    }
    private static byte[] World(long generation)
    {
        var parts=new PersistedPart[FrozenPartCatalog.Slots];
        for(int i=0;i<parts.Length;i++)
        {
            var physical=SecureRigCatalog.At(i);
            long owner=FrozenPartCatalog.Owner(i);
            parts[i]=new PersistedPart(physical.PartId,owner,owner,
                FrozenPartCatalog.Asset(i),FrozenPartCatalog.Socket(i),
                PartDockState.Attached,false,true,
                new Vector3d(physical.RootX+physical.PartOffsetX,1.2,0),
                0,0,0,1,new Vector3d(0,0,0),
                new Vector3d(0,0,0),physical.PartMassKg);
        }
        return SnapshotCodec.Encode(new RunSnapshot(Run,generation,parts));
    }
    private static ApprovedEconomySnapshotV7 Economy(long generation,int scrap,
        bool destroyed=false,int purchase=100)
    {
        return new ApprovedEconomySnapshotV7 {
            Version=7,OwnerId=Slot.ToString("N"),
            RunId=Run.ToString(CultureInfo.InvariantCulture),
            Generation=generation,Scrap=scrap,Wins=0,FreeUsedForWin=0,
            Parts=new List<OwnedModuleV7>{
                new OwnedModuleV7 { Id=WholeGameEconomySlotV3.EconomicPartId(
                    FrozenPartCatalog.Id(0)),Category="Core",Hp=1,
                    PurchaseScrap=100,ReservationId="" },
                new OwnedModuleV7 { Id=WholeGameEconomySlotV3.EconomicPartId(
                    FrozenPartCatalog.Id(1)),Category="EssentialDrive",
                    Hp=destroyed?0:1,PurchaseScrap=purchase,ReservationId="" }
            }, Pending=new List<RebuildTicketV7>(),
            ConsumedVictoryIds=new List<string>(),
            Loaner=new OneShotLoanerV7 {Phase=LoanerPhaseV7.None,
                EverEntered=false,EncounterId="",PermitId=""}
        };
    }
    private static CanonicalEconomyIdentityW29 Trusted(long gen)
        =>new CanonicalEconomyIdentityW29(
            Slot.ToString("N"),Run.ToString(CultureInfo.InvariantCulture),gen);

    private static void UnsafeOriginalOwnerWrite(string root)
    {
        Directory.CreateDirectory(root);
        var gateway=OpenOwner(root,out var owner);
        WholeGameEconomySlotV3? store=null;
        try
        {
            Require(WholeGameEconomySlotV3.Create(Dir(root),Run,Slot,owner,
                out store)==WholeGameResult.Accepted && store!=null,
                "actual original #826 owner one-file store created");
            var unsafeV7=Economy(1,7,true,100);
            unsafeV7.Parts[1].ReservationId=TicketId;
            unsafeV7.Pending.Add(new RebuildTicketV7 {
                ModuleId=unsafeV7.Parts[1].Id,ReservationId=TicketId,
                AfterWins=0,ChargedScrap=0,PurchaseScrap=100,Free=true
            });
            Require(ApprovedEconomyPolicyV7.Valid(unsafeV7),
                "actual frozen V7 original Valid ALLOWS Win0 FREE unsafe snapshot");
            Require(CanonicalEconomySectionV7.TryEncode(unsafeV7,
                out var bytes,out var why)&&
                CanonicalEconomySectionV7.TryDecode(bytes,out var restored,out why)&&
                restored.Pending.Single().Free,
                "actual frozen V7 codec saves malformed Win0 FREE");
            Require(!EconomyV7AdmissionW29.TryDecodeForCanonicalOwner(
                bytes,Trusted(1),out _,out why)&&
                why=="FREE_TICKET_BEFORE_FIRST_AUTHENTIC_VICTORY",
                "actual immutable W29 admission rejects Win0 FREE");
            Require(!EconomyV7AdmissionW29.TryEncodeForCanonicalOwner(
                unsafeV7,Trusted(1),out _,out why)&&
                why=="FREE_TICKET_BEFORE_FIRST_AUTHENTIC_VICTORY",
                "new checked producer blocks initial forged Free ticket");
            Require(WholeGameEconomySlotV3.Open(Dir(root),Run,Slot,owner,
                out var duplicate)==WholeGameResult.Busy && duplicate==null,
                "same actual original physical owner cannot open two writers");
            // NEGATIVE OBSERVATION, NOT A "GREEN" SECURITY PROOF:
            // #826 has NOT integrated the new admission gate, so the actual
            // physical full-world writer currently accepts Win0 FREE.
            Require(store!.Commit(0,World(1),unsafeV7)==WholeGameResult.Accepted,
                "P0 RECEIVER GAP: actual original #826 persists the invalid FREE");
            Require(store.Read(out var gen,out var fullWorld,
                out var diskEcon)==WholeGameResult.Accepted &&
                gen==1 && diskEcon!=null && diskEcon.Pending.Single().Free &&
                fullWorld.SequenceEqual(World(1)),
                "real #826 one physical envelope contains unsafe V7 unchanged");
            Require(!EconomyV7AdmissionW29.TryAdmit(diskEcon!,
                Trusted(gen),out var reject)&&
                reject=="FREE_TICKET_BEFORE_FIRST_AUTHENTIC_VICTORY",
                "reviewing actual #826 physical disk reveals missing gate");
            Console.WriteLine("W30_ORIGINAL_826_INVALID_EPOCH0_FREE_PHYSICAL_WRITE_REPRODUCED product_gate=RED");
        }
        finally
        {
            store?.Dispose();
            Require(gateway.TryCloseAfterPhysicalRetirement(()=>true),
                "actual original #729 lease retired after source-only no-world stage");
        }
    }
    private static void LegalOriginalOwnerWrite(string root)
    {
        Directory.CreateDirectory(root);
        var gateway=OpenOwner(root,out var owner);
        WholeGameEconomySlotV3? store=null;
        try
        {
            Require(WholeGameEconomySlotV3.Create(Dir(root),Run,Slot,owner,
                out store)==WholeGameResult.Accepted&&store!=null,
                "separate legitimate owner-only full writer created");
            var initial=Economy(0,0,true,1);
            Require(ApprovedEconomyPolicyV7.TryReserveDestroyed(initial,
                initial.Parts[1].Id,out var zeroPaid,out var why) &&
                zeroPaid!=null && zeroPaid.Generation==1 &&
                zeroPaid.Pending.Count==1 && !zeroPaid.Pending[0].Free &&
                zeroPaid.Pending[0].ChargedScrap==0,
                "original genuine V7 zero-PAID not free price1");
            Require(EconomyV7AdmissionW29.TryEncodeForCanonicalOwner(
                zeroPaid!,Trusted(1),out var acceptedBytes,out why)&&
                EconomyV7AdmissionW29.TryDecodeForCanonicalOwner(
                acceptedBytes,Trusted(1),out var decoded,out why)&&
                decoded.Pending.Count==1&&!decoded.Pending[0].Free,
                "W29 checked gate admits genuine paid0 from exact host generation");
            Require(store!.Commit(0,World(1),zeroPaid!)==WholeGameResult.Accepted,
                "original one full physical game owner persists legal zero-paid");
            Require(ApprovedEconomyPolicyV7.TryStageTrustedVictory(
                zeroPaid!,"W30-fixture-win1","real-fixture-encounter",
                out var afterWin,out why)&&
                afterWin!=null && afterWin.Wins==1 &&
                afterWin.Generation==2 && afterWin.Pending.Count==0 &&
                afterWin.Parts[1].Hp==1,
                "previous-epoch owned P1 module heals only on NEXT modeled win");
            Require(EconomyV7AdmissionW29.TryEncodeForCanonicalOwner(
                afterWin!,Trusted(2),out acceptedBytes,out why),
                "genuine scenario Win1 allows checked host publication");
            Require(store.Commit(1,World(2),afterWin!)==WholeGameResult.Accepted,
                "same original owner publishes wallet+genuine modeled win+whole world");
            Require(store.Read(out var gen,out var diskWorld,
                out var diskEcon)==WholeGameResult.Accepted &&
                gen==2 && diskWorld.SequenceEqual(World(2)) &&
                diskEcon!=null && diskEcon.Wins==1 &&
                diskEcon.ConsumedVictoryIds.SequenceEqual(
                    new[]{"W30-fixture-win1"}) &&
                diskEcon.Parts[1].Hp==1,
                "original one-envelope readback has world + wallet + real V7");
            Require(!ApprovedEconomyPolicyV7.TryStageTrustedVictory(
                afterWin!,"W30-fixture-win1","real-fixture-encounter",
                out _,out _),"same scenario win never replays");
            Console.WriteLine("W30_ORIGINAL_826_AUTHORIZED_ZERO_PAID_AND_NEXT_WIN_PHYSICAL_PASS");
        }
        finally
        {
            store?.Dispose();
            Require(gateway.TryCloseAfterPhysicalRetirement(()=>true),
                "genuine canonical one-owner OS lease released after no-world stage");
        }
    }
    private static void ColdRead(string root,bool expectMalformed)
    {
        var gateway=OpenOwner(root,out var owner);
        WholeGameEconomySlotV3? store=null;
        try
        {
            Require(WholeGameEconomySlotV3.Open(Dir(root),Run,Slot,owner,
                out store)==WholeGameResult.Accepted&&store!=null,
                "independent cold CLR acquires original one physical owner");
            Require(store!.Read(out var gen,out var world,
                out var wallet)==WholeGameResult.Accepted &&
                wallet!=null && world.SequenceEqual(World(gen)),
                "original full world+V7 read from physical disk after process exit");
            Require(EconomyV7AdmissionW29.TryAdmit(wallet!,
                Trusted(gen),out var reason)==!expectMalformed,
                expectMalformed?"malformed Win0 FREE fail-closed cold admission":
                    "legitimate Win1 cold admission passes");
            if(expectMalformed)
            {
                Require(gen==1&&wallet!.Wins==0&&wallet.Pending.Single().Free&&
                    reason=="FREE_TICKET_BEFORE_FIRST_AUTHENTIC_VICTORY",
                    "actual cold invalid full Save must be quarantined by host adapter");
            }
            else
            {
                Require(gen==2&&wallet!.Wins==1&&wallet.Pending.Count==0&&
                    wallet.Parts[1].Hp==1 && wallet.Scrap==0 &&
                    wallet.ConsumedVictoryIds.SequenceEqual(new[]{"W30-fixture-win1"}),
                    "actual cold legal full Save retains stable physical ID, wallet and win");
            }
        }
        finally
        {
            store?.Dispose();
            Require(gateway.TryCloseAfterPhysicalRetirement(()=>true),
                "new CLR original physical lease retired without active Unity world");
        }
    }
    private static int Main(string[] args)
    {
        try
        {
            if(args.Length!=2)return 2;
            if(args[0]=="--scenario")
            {
                string root=Path.GetFullPath(args[1]);
                Require(!Directory.Exists(root),
                    "test runner root does not overlap prior foreign private data");
                Directory.CreateDirectory(root);
                UnsafeOriginalOwnerWrite(Path.Combine(root,"unsafe"));
                LegalOriginalOwnerWrite(Path.Combine(root,"legal"));
                Console.WriteLine("W30_SOURCE_SCENARIO_PASS assertions="+assertions+
                  " selected826_writer=REAL_OS actual_Unity_G1=NOT_RUN");
                return 0;
            }
            if(args[0]=="--cold")
            {
                ColdRead(Path.Combine(Path.GetFullPath(args[1]),"unsafe"),true);
                ColdRead(Path.Combine(Path.GetFullPath(args[1]),"legal"),false);
                Console.WriteLine("W30_SOURCE_COLD_PROCESS_PASS assertions="+assertions+
                  " only_original_826_file_writer=true game_integration=NOT_WIRED");
                return 0;
            }
            return 2;
        }
        catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
    }
}
