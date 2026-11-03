// B01-22 W31: independent long-campaign test of the ORIGINAL, sole #885
// full-world V3 OS Save with immutable #780 and #855 V7 production code.
// No new writer, fake Unity PhysX, sidecar store or owner policy change.
#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using B01.WholeGameEconomy003;
using B01.CanonicalColdRead003;
using B01.CanonicalWorldApi001;
using B01.DebrisRepulse001;
using B01.DetachedSaveContract001;
using B01.SecureReattachV2;
using RoboRougelike.B01.Economy.ApprovedV7;
using RoboRougelike.B01.Economy.AdmissionW29;

internal static class B0122W31OriginalCampaign
{
    const long Run=6310022;
    static readonly Guid Slot=Guid.Parse("63102631-0000-4000-8000-000000000556");
    static int checks;
    static void Require(bool v,string reason)
    {
        if(!v)throw new InvalidOperationException("W31_TEST_FAIL "+reason);
        checks++;
    }
    sealed class Quiescent:ICanonicalMutationGate
    {
        public bool IsQuiescent()=>true;
    }
    sealed class NoStage:ICanonicalRigFactory
    {
        public ICanonicalStageOwnership BeginOwnedStage(RunSnapshot s,ColdRestore c)
          =>throw new InvalidOperationException("source fixture cannot instantiate Unity physics");
    }
    sealed class NoCommit:ICanonicalCommitPort
    {
        public bool Read(long r,out long g,out string hash)
        {g=0;hash="";return false;}
        public bool Commit(long r,long p,string h,long n,string nh)=>false;
    }
    static string SlotPath(string root)=>Path.Combine(root,
        "b0162_canonical_single_authority_v3",
        "run_"+Run.ToString(CultureInfo.InvariantCulture)+"_slot_"+Slot.ToString("N"));
    static string OwnerId=>Slot.ToString("N");
    static string RunId=>Run.ToString(CultureInfo.InvariantCulture);
    static string PartId(int n)=>WholeGameEconomySlotV3.EconomicPartId(FrozenPartCatalog.Id(n));
    static byte[] World(long gen)
    {
        var all=new PersistedPart[FrozenPartCatalog.Slots];
        for(int i=0;i<all.Length;i++)
        {
            var part=SecureRigCatalog.At(i);
            long owned=FrozenPartCatalog.Owner(i);
            all[i]=new PersistedPart(part.PartId,owned,owned,
                FrozenPartCatalog.Asset(i),FrozenPartCatalog.Socket(i),
                PartDockState.Attached,false,true,
                new Vector3d(part.RootX+part.PartOffsetX,1.2,0),
                0,0,0,1,new Vector3d(0,0,0),new Vector3d(0,0,0),
                part.PartMassKg);
        }
        return SnapshotCodec.Encode(new RunSnapshot(Run,gen,all));
    }
    static ApprovedEconomySnapshotV7 Initial()
    {
        var parts=new List<OwnedModuleV7>();
        for(int i=0;i<FrozenPartCatalog.Slots;i++)
            parts.Add(new OwnedModuleV7 {
                Id=PartId(i),
                Category=i==0?"Core":i==1?"EssentialDrive":"Armor",
                Hp=i==1?0:1,PurchaseScrap=i==1?1:100,ReservationId=""
            });
        return new ApprovedEconomySnapshotV7 {
            Version=7,OwnerId=OwnerId,RunId=RunId,Generation=0,
            Scrap=0,Wins=0,FreeUsedForWin=0,Parts=parts,
            Pending=new List<RebuildTicketV7>(),
            ConsumedVictoryIds=new List<string>(),
            Loaner=new OneShotLoanerV7 {
                Phase=LoanerPhaseV7.None,EverEntered=false,
                EncounterId="",PermitId=""
            }
        };
    }
    static CanonicalEconomyIdentityW29 Identity(long g)
        =>new CanonicalEconomyIdentityW29(OwnerId,RunId,g);
    static CanonicalWorldGateway OpenGateway(string root,
        out CanonicalWorldSlotLease physical)
    {
        Require(CanonicalWorldSlotLease.TryAcquire(root,Run,Slot,
            out var obtained)&&obtained!=null,
            "original single physical OS owner acquired");
        physical=obtained!;
        return new CanonicalWorldGateway(new Quiescent(),
            new NoStage(),new NoCommit(),physical);
    }
    static void Persist(WholeGameEconomySlotV3 writer,
        ApprovedEconomySnapshotV7 state,long previous)
    {
        Require(state.Generation==previous+1,
            "selected V7 increments exactly one full-world CAS generation");
        Require(EconomyV7AdmissionW29.TryAdmit(state,
            Identity(state.Generation),out var reason)&&reason=="OK",
            "immutable #855 validates each original staged economy");
        var before=World(previous+1);
        Require(writer.Commit(previous,before,state)==WholeGameResult.Accepted,
            "original selected #896 full-world OS CAS commit "+previous);
        Require(writer.Read(out long diskGen,out byte[] actual,
            out var decoded)==WholeGameResult.Accepted&&
            diskGen==state.Generation&&actual.SequenceEqual(before)&&
            decoded!=null&&decoded.Wins==state.Wins&&
            decoded.Scrap==state.Scrap&&
            decoded.Loaner.Phase==state.Loaner.Phase&&
            decoded.FreeUsedForWin==state.FreeUsedForWin&&
            decoded.Pending.Count==state.Pending.Count&&
            decoded.ConsumedVictoryIds.SequenceEqual(state.ConsumedVictoryIds),
            "original #896 one-envelope direct readback after CAS");
    }
    static void Campaign(string root)
    {
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(Path.GetDirectoryName(SlotPath(root))!);
        var gateway=OpenGateway(root,out var physical);
        WholeGameEconomySlotV3? save=null;
        try
        {
            Require(WholeGameEconomySlotV3.Create(SlotPath(root),Run,Slot,
                physical,out save)==WholeGameResult.Accepted&&save!=null,
                "original #885 sole full-store created");
            var bad=Initial();
            bad.Generation=1;
            var drive=bad.Parts[1];
            drive.ReservationId="w31-invalid-boot-free";
            bad.Pending.Add(new RebuildTicketV7 {
                ModuleId=drive.Id,ReservationId=drive.ReservationId,
                AfterWins=0,ChargedScrap=0,PurchaseScrap=1,Free=true
            });
            Require(ApprovedEconomyPolicyV7.Valid(bad),
                "original V7 still accepts fabricated Win0-free in isolation");
            Require(save!.Commit(0,World(1),bad)==WholeGameResult.Invalid,
                "REAL #885 full writer rejects impossible Win0 free pre-publish");
            Require(save.Read(out _,out _,out _) == WholeGameResult.Empty,
                "rejected malformed first write creates NO game envelope");
            Require(!save.Quarantined,
                "invalid caller input does not poison legitimate initial save");

            var start=Initial();
            Require(ApprovedEconomyPolicyV7.TryReserveDestroyed(
                start,start.Parts[1].Id,out var state,out var why)&&
                state!=null&&!state.Pending.Single().Free&&
                state.Pending.Single().ChargedScrap==0&&
                state.Wins==0&&state.FreeUsedForWin==0,
                "genuine first reserve quote0 explicitly PAID with zero victories");
            Persist(save,state!,0);
            const string encounter="w31-encounter-one";
            const string permit="aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
            var facts=new CanonicalDeploymentFactsV7 {
                OwnerId=OwnerId,RunId=RunId,Generation=state!.Generation,
                CoreId=start.Parts[0].Id,CoreFunctional=true,
                CanDeploy=false,HasGenuineNextChallenge=true,
                EncounterId=encounter,
                EssentialDestroyedPhysicalIds=new[]{start.Parts[1].Id}
            };
            Require(ApprovedEconomyPolicyV7.TryReserveOneShotLoaner(
                state,facts,permit,out var reserved,out why)&&reserved!=null,
                "model-owned living-core/disabled-drive next real fixture allows ONE loaner");
            Persist(save,reserved!,state.Generation);
            Require(ApprovedEconomyPolicyV7.TryConsumeLoanerOnTrustedChallengeEntry(
                reserved!,encounter,permit,out var entered,out why)&&entered!=null&&
                entered.Loaner.EverEntered,
                "actual V7 one-shot enters and becomes permanently used");
            Persist(save,entered!,reserved!.Generation);
            Require(WholeGameEconomySlotV3.Open(SlotPath(root),Run,Slot,
                physical,out var competitor)==WholeGameResult.Busy&&competitor==null,
                "original #885 no duplicate writer under same physical owner");
            Require(CanonicalColdReadV3.ReadClosedSlot(root,Run,Slot,
                out long busyGen,out byte[] busyWorld,out var busyEcon)
                ==WholeGameResult.Busy&&busyGen==0&&busyWorld.Length==0&&
                busyEcon==null,
                "actual distinct cold reader cannot steal active OS lease");
            var current=entered!;
            int countFree=0,confirmedSettled=0;
            for(int win=1;win<=24;win++)
            {
                var id="w31-controlled-win-"+win.ToString("D2");
                var fight=win==1?encounter:"w31-encounter-"+win.ToString("D2");
                Require(ApprovedEconomyPolicyV7.TryStageTrustedVictory(
                    current,id,fight,out var victorious,out why)&&
                    victorious!=null,
                    "original distinct scenario victory stages at win "+win+": "+why);
                Require(victorious!.Wins==win&&
                    victorious.Parts[1].Hp==1&&
                    victorious.Pending.Count==0,
                    "next distinct win restores PREVIOUS owned part, not same fight");
                Require(victorious.Loaner.Phase==LoanerPhaseV7.Spent&&
                    victorious.Loaner.EverEntered,
                    "one-shot Spent persists, no rearm after any victory");
                confirmedSettled++;
                Persist(save,victorious,current.Generation);
                var post=victorious.Copy();
                // QA-controlled next encounter casualty (NOT native Unity):
                // this 0HP occurs only AFTER the previous encounter concluded.
                post.Parts[1].Hp=0;
                Require(ApprovedEconomyPolicyV7.TryReserveDestroyed(
                    post,post.Parts[1].Id,out var queued,out why)&&queued!=null,
                    "next modeled encounter damaged essential is reservable");
                Require(queued!.Pending.Count==1&&queued.Pending[0].Free&&
                    queued.Pending[0].ChargedScrap==0&&
                    queued.Pending[0].AfterWins==win&&
                    queued.FreeUsedForWin==win&&queued.Parts[1].Hp==0,
                    "exactly one genuine post-win FREE pending to NEXT win");
                countFree++;
                Persist(save,queued,victorious.Generation);
                current=queued;
            }
            Require(current.Wins==24&&current.ConsumedVictoryIds.Count==24&&
                current.ConsumedVictoryIds.Distinct(StringComparer.Ordinal).Count()==24&&
                current.FreeUsedForWin==24&&current.Pending.Count==1&&
                current.Parts[1].Hp==0&&current.Scrap==0&&
                countFree==24&&confirmedSettled==24,
                "24 distinct wins 24 FREE pending reservations 24 previous restorations plus paid0");
            Require(!ApprovedEconomyPolicyV7.TryReserveOneShotLoaner(
                current,new CanonicalDeploymentFactsV7 {
                    OwnerId=OwnerId,RunId=RunId,Generation=current.Generation,
                    CoreId=current.Parts[0].Id,CoreFunctional=true,CanDeploy=false,
                    HasGenuineNextChallenge=true,EncounterId="future",
                    EssentialDestroyedPhysicalIds=new[]{current.Parts[1].Id}
                }, "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",out _,out _),
                "one-shot loaner never rearmed after 24 victories");
            Require(!ApprovedEconomyPolicyV7.TryStageTrustedVictory(
                current,"w31-controlled-win-24","w31-encounter-24",out _,out _),
                "replaying an original 24th win cannot settle the final waiting part");
            Console.WriteLine("W31_896_24_FIGHT_FULL_SAVE_SOURCE_PASS wins=24 restorations=24 free=24 paid_zero=1 pending=1 loaner=Spent generation="+current.Generation+" checks="+checks);
        }
        finally
        {
            save?.Dispose();
            Require(gateway.TryCloseAfterPhysicalRetirement(()=>true),
                "original sole OS owner retired without a native Unity root");
        }
    }
    static void Cold(string root)
    {
        Require(CanonicalColdReadV3.ReadClosedSlot(root,Run,Slot,
            out long gen,out byte[] world,out var economy)
            ==WholeGameResult.Accepted&&economy!=null,
            "new CLR process independently cold-loads ORIGINAL selected #896");
        Require(ColdRestore.TryStage(world,out var worldStage)&&
            worldStage!=null&&worldStage.Snapshot.RunId==Run&&
            worldStage.Snapshot.Generation==gen,
            "original physical #556 world IDs and generation survive process boundary");
        Require(economy!.Generation==gen&&economy.Wins==24&&
            economy.FreeUsedForWin==24&&economy.Scrap==0&&
            economy.Pending.Count==1&&economy.Pending[0].Free&&
            economy.Pending[0].AfterWins==24&&
            economy.Parts[1].Hp==0&&
            economy.Loaner.Phase==LoanerPhaseV7.Spent&&
            economy.Loaner.EverEntered&&
            economy.ConsumedVictoryIds.Count==24&&
            economy.ConsumedVictoryIds.Distinct(StringComparer.Ordinal).Count()==24,
            "single full-file cold Save has all 24 authentic-fixture wins, wallet, free, spent loaner and queued module");
        Require(EconomyV7AdmissionW29.TryAdmit(economy,
            Identity(gen),out var why)&&why=="OK",
            "original #855 host-bound admission still authenticates final cold wallet");
        Require(!ApprovedEconomyPolicyV7.TryStageTrustedVictory(
            economy,"w31-controlled-win-24","w31-encounter-24",
            out _,out _),
            "persistent cold Save replays no victory");
        Console.WriteLine("W31_896_COLD_SECOND_CLR_PASS wins=24 loaner=Spent pending=1 checks="+checks);
    }
    public static int Main(string[] args)
    {
        try
        {
            if(args.Length!=2)return 2;
            if(args[0]=="--scenario")
            {
                Require(!Directory.Exists(args[1]),
                    "QA temp root must be newly absent; never overwrite foreign data");
                Campaign(Path.GetFullPath(args[1]));
                return 0;
            }
            if(args[0]=="--cold")
            {
                Cold(Path.GetFullPath(args[1]));
                return 0;
            }
            return 2;
        }
        catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
    }
}
