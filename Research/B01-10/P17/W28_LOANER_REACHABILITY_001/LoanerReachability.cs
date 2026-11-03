// B01-10 / P17 W28 / LOANER-REACHABILITY-001.
// Compile and EXECUTE exact ORIGINAL B01-22 #780 V7 code; do not rewrite ECON.
// Direct method calls in this source harness use SYNTHETIC IDs, not combat receipts.
// Source reachability != native Unity CanDeploy, world/save binder or Player result.
using System;
using System.Linq;
using System.Collections.Generic;
using RoboRougelike.B01.Economy.ApprovedV7;

static class LoanerReachability
{
    const string Core="00000000000000000000000000000001";
    const string Drive="00000000000000000000000000000002";
    const string Permit="00000000000000000000000000000003";
    const string Weapon="00000000000000000000000000000004";
    static int checks, cases;
    static void Check(bool b,string label)
    {
        if(!b)throw new Exception("W28_V7_REACHABILITY_RED "+label);
        checks++;
    }
    static ApprovedEconomySnapshotV7 Start(int price,int cash,bool broken=true)
    {
        var s=new ApprovedEconomySnapshotV7 {
            OwnerId="B01-10-W28-test-owner",RunId="source-only-run",Scrap=cash,
            Parts=new List<OwnedModuleV7>{
                new OwnedModuleV7{Id=Core,Category="Core",Hp=1,PurchaseScrap=100},
                new OwnedModuleV7{Id=Drive,Category="EssentialDrive",Hp=broken?0:1,PurchaseScrap=price}
            }
        };
        Check(ApprovedEconomyPolicyV7.Valid(s),"fresh snapshot");
        return s;
    }
    static ApprovedEconomySnapshotV7 SaveSection(ApprovedEconomySnapshotV7 s)
    {
        Check(CanonicalEconomySectionV7.TryEncode(s,out var bytes,out var why),
            "original section encode "+why);
        Check(CanonicalEconomySectionV7.TryDecode(bytes,out var clone,out why),
            "original section decode "+why);
        Check(clone.OwnerId==s.OwnerId&&clone.RunId==s.RunId&&
              clone.Wins==s.Wins&&clone.Scrap==s.Scrap&&
              clone.FreeUsedForWin==s.FreeUsedForWin&&
              clone.Generation==s.Generation&&
              clone.Pending.Count==s.Pending.Count&&
              clone.Loaner.Phase==s.Loaner.Phase&&
              clone.ConsumedVictoryIds.SequenceEqual(s.ConsumedVictoryIds),
              "original V7 section retained owner/wallet/loaner/wins");
        return clone;
    }
    static ApprovedEconomySnapshotV7 Ticket(ApprovedEconomySnapshotV7 s,bool free)
    {
        Check(ApprovedEconomyPolicyV7.TryReserveDestroyed(s,Drive,
            out var next,out var reason),"ticket reserved "+reason);
        Check(next.Pending.Count==1&&next.Pending[0].ModuleId==Drive&&
            next.Pending[0].Free==free&&next.Parts[1].Hp==0,
            "real owner V7 ticket free/pending/ID");
        return SaveSection(next);
    }
    static CanonicalDeploymentFactsV7 Facts(ApprovedEconomySnapshotV7 s,
        string encounter,bool coreAlive=true,bool available=true,bool deploy=false)
    {
        return new CanonicalDeploymentFactsV7 {
            OwnerId=s.OwnerId,RunId=s.RunId,Generation=s.Generation,
            CoreId=Core,CoreFunctional=coreAlive,CanDeploy=deploy,
            HasGenuineNextChallenge=available,EncounterId=encounter,
            EssentialDestroyedPhysicalIds=new[]{Drive}
        };
    }
    static ApprovedEconomySnapshotV7 Enter(ApprovedEconomySnapshotV7 s,
        string encounter,string[] requiredOwnedIds=null)
    {
        // The host must supply genuine physical CanDeploy facts. This test
        // explicitly enumerates two required broken GUIDs, not just one.
        var facts=Facts(s,encounter);
        if(requiredOwnedIds!=null)facts.EssentialDestroyedPhysicalIds=requiredOwnedIds;
        Check(ApprovedEconomyPolicyV7.TryReserveOneShotLoaner(s,
            facts,Permit,out var loaner,out var why),
            "V7 real original loaner reserve "+why);
        Check(ApprovedEconomyPolicyV7.TryConsumeLoanerOnTrustedChallengeEntry(
            loaner,encounter,Permit,out var entered,out why),
            "V7 original loaner consume "+why);
        Check(entered.Loaner.Phase==LoanerPhaseV7.Entered&&
            entered.Loaner.EverEntered,"loaner entered exactly once");
        return SaveSection(entered);
    }
    static ApprovedEconomySnapshotV7 Win(ApprovedEconomySnapshotV7 s,
        string unique,string encounter)
    {
        Check(ApprovedEconomyPolicyV7.TryStageTrustedVictory(
            s,unique,encounter,out var next,out var why),
            "V7 stage simulated winner "+why);
        return SaveSection(next);
    }
    static void CheckPrice0AtZeroCash()
    {
        var s=Start(1,0);
        Check(ApprovedEconomyPolicyV7.TryPaidQuote(1,out var quote)&&quote==0,
            "P1 paid0 strictly less");
        s=Ticket(s,false);
        Check(s.Scrap==0&&s.FreeUsedForWin==0&&s.Wins==0&&
            s.Pending[0].ChargedScrap==0,"paid-zero has NO free win");
        s=Enter(s,"e-p1");
        s=Win(s,"won-p1","e-p1");
        Check(s.Parts[1].Hp==1&&s.Pending.Count==0&&
            s.Loaner.Phase==LoanerPhaseV7.Spent&&s.Wins==1,
            "P1=0 PAID prebook -> Win1 actually restores owned drive");
        cases++;
        Console.WriteLine("W28_C1_PRICE1_CASH0_PAID0_LOANER_WIN RECOVERABLE_SOURCE PASS");
    }
    static void CheckPaidThreshold(int cash,bool expectRecover)
    {
        var s=Start(100,cash);
        if(expectRecover)
        {
            s=Ticket(s,false);
            Check(s.Scrap==cash-65&&s.Pending[0].ChargedScrap==65,
                "P100 quote65 prebooked");
        }
        else
        {
            Check(!ApprovedEconomyPolicyV7.TryReserveDestroyed(s,Drive,
                out _,out var no)&&no=="INSUFFICIENT_SCRAP",
                "cannot book expensive P100 with insufficient wallet");
        }
        s=Enter(s,"e-cash-"+cash);
        s=Win(s,"win-cash-"+cash,"e-cash-"+cash);
        Check(s.Loaner.Phase==LoanerPhaseV7.Spent&&s.Wins==1,
            "loaner cannot rearm");
        Check((s.Parts[1].Hp>0)==expectRecover,
            "actual V7 source recovery only if pre-win paid ticket");
        if(!expectRecover)
        {
            Check(s.Pending.Count==0&&s.Parts[1].Hp==0,
                "loaner WIN did NOT heal originally unbooked drive");
            s=Ticket(s,true);
            Check(s.Pending[0].Free&&s.Parts[1].Hp==0&&
                s.Loaner.Phase==LoanerPhaseV7.Spent,
                "post-win free ticket STILL requires unreachable second win");
        }
        else Check(s.Pending.Count==0,"paid prior ticket completed");
        cases++;
        Console.WriteLine("W28_CASH_THRESHOLD original_V7 cash="+cash+
            " reachability="+(expectRecover?"RECOVERABLE":"BLOCKED_AFTER_WIN")+" PASS");
    }
    static void CheckPriorWinFreeTicket()
    {
        var s=Start(100,0,false);
        // Source-only previously eligible genuine winner; real host must attest it.
        s=Win(s,"prior-verified-by-test-host","e-earlier");
        Check(s.Wins==1&&s.FreeUsedForWin==0&&s.Parts[1].Hp==1,
            "one existing winner free allowance present");
        // Damage injection stands for a game event outside V7 (not native physics).
        var physicallyDamaged=s.Copy();
        physicallyDamaged.Parts[1].Hp=0;
        Check(ApprovedEconomyPolicyV7.Valid(physicallyDamaged),
            "later hypothetical 0HP owned drive valid");
        s=Ticket(physicallyDamaged,true);
        Check(s.Scrap==0&&s.FreeUsedForWin==1&&s.Pending[0].Free,
            "prior win can queue one free pre-loaner repair");
        s=Enter(s,"e-win2");
        s=Win(s,"win-after-prior","e-win2");
        Check(s.Wins==2&&s.Parts[1].Hp==1&&s.Pending.Count==0&&
              s.Loaner.Phase==LoanerPhaseV7.Spent,
              "a genuinely PRIOR win makes the one-shot loaner win sufficient");
        cases++;
        Console.WriteLine("W28_C_PRIOR_WIN_CASH0_FREE_PREBOOK RECOVERABLE_SOURCE PASS");
    }
    static void CheckIllegalUnmintedFree()
    {
        var s=Start(100,0);
        // Try a fabricated pre-victory FREE ticket, which old V7 cannot encode.
        var fake=s.Copy();
        fake.Parts[1].ReservationId="fake-contingent-in-V7";
        fake.Pending.Add(new RebuildTicketV7 {
            ModuleId=Drive,ReservationId="fake-contingent-in-V7",
            AfterWins=0,Free=true,ChargedScrap=0,PurchaseScrap=100
        });
        // ACTUAL red witness on immutable original #780 V7:
        // Valid() accepts a crafted FREE booking at Wins=0 if FreeUsedForWin=0.
        // This cannot be created with legitimate TryReserveDestroyed() and
        // is NOT proof that a player can influence the one-writer host.
        Check(ApprovedEconomyPolicyV7.Valid(fake),
            "EXACT ORIGINAL SOURCE RED: crafted Free ticket passes Valid at Wins0");
        Check(CanonicalEconomySectionV7.TryEncode(fake,out var forged,out var why),
            "EXACT ORIGINAL SOURCE RED: binary encodes crafted Free epoch0 "+why);
        Check(CanonicalEconomySectionV7.TryDecode(forged,
            out var roundtrip,out why)&&roundtrip.Wins==0&&
            roundtrip.Pending.Count==1&&roundtrip.Pending[0].Free,
            "EXACT ORIGINAL SOURCE RED: binary decoder accepts impossible Free epoch0");
        Check(!ApprovedEconomyPolicyV7.TryReserveDestroyed(s,Drive,
            out _,out var expectedReject)&&expectedReject=="INSUFFICIENT_SCRAP",
            "legitimate source API still denies zero-wallet paid65 before any Win");
        Check(ApprovedEconomyPolicyV7.Valid(s)&&s.Pending.Count==0,
            "malformed test-only snapshot did not alter legitimate owner state");
        cases++;
        Console.WriteLine("W28_C_EPOCH0_FREE_VALIDATION_GAP_EXPECTED_SOURCE_RED PASS");
    }
    static void CheckLossOrBlockedEvidence()
    {
        var s=Start(100,0);
        var noChallenge=Facts(s,"invalid-next",available:false);
        Check(!ApprovedEconomyPolicyV7.TryReserveOneShotLoaner(
            s,noChallenge,Permit,out _,out _),"no future challenge denies loaner");
        var dead=Facts(s,"dead-core",coreAlive:false);
        Check(!ApprovedEconomyPolicyV7.TryReserveOneShotLoaner(
            s,dead,Permit,out _,out _),"dead core denies loaner");
        var deploy=Facts(s,"healthy-original",deploy:true);
        Check(!ApprovedEconomyPolicyV7.TryReserveOneShotLoaner(
            s,deploy,Permit,out _,out _),"a viable robot cannot get loaner");
        s=Enter(s,"e-loss");
        Check(ApprovedEconomyPolicyV7.TryStageTrustedLoss(s,"e-loss",
            out var lost,out var why),"source V7 loser "+why);
        lost=SaveSection(lost);
        Check(lost.Wins==0&&lost.Scrap==0&&lost.Parts[1].Hp==0&&
              lost.Loaner.Phase==LoanerPhaseV7.Spent,
              "loaner loss produced no win, healing or money");
        Check(!ApprovedEconomyPolicyV7.TryReserveOneShotLoaner(
            lost,Facts(lost,"reentry"),Permit,out _,out _),
            "spent one-shot must not repeat even after binary cold replay");
        cases++;
        Console.WriteLine("W28_C_LOANER_LOSS_CORE_AND_NEXT_GATES_NO_REARM PASS");
    }
    // W29 gameplay risk NOT covered by the single-drive softlock analysis:
    // one genuine prior win gives one Free order, but two required modules
    // cannot both be restored at cash=0 before the one-shot loaner WIN.
    // A proposed 1-GUID conditional free claim does not guarantee CanDeploy.
    static void CheckTwoEssentialPartsNeedTwoTickets()
    {
        foreach(var cash in new[]{0,65})
        {
            var initial=Start(100,cash,false);
            initial.Parts.Add(new OwnedModuleV7 {
                Id=Weapon,Category="EssentialGun",Hp=1,PurchaseScrap=100
            });
            Check(ApprovedEconomyPolicyV7.Valid(initial),
                "two original physical owned essential GUIDs source-valid");
            // Previous synthetic Win makes exactly ONE free entitlement legal.
            var s=Win(initial,"previous-victory-"+cash,"previous-fight-"+cash);
            Check(s.Wins==1&&s.FreeUsedForWin==0&&s.Pending.Count==0,
                "one previously earned source free booking, no coupons stockpiled");
            s=s.Copy();
            s.Parts.Single(p=>p.Id==Drive).Hp=0;
            s.Parts.Single(p=>p.Id==Weapon).Hp=0;
            Check(ApprovedEconomyPolicyV7.Valid(s),
                "both owned essential modules destroyed after earlier victory");
            Check(ApprovedEconomyPolicyV7.TryReserveDestroyed(
                s,Drive,out var queued,out var why),
                "first critical GUID prebooked with prior free coupon "+why);
            Check(queued.Pending.Count==1&&queued.Pending[0].Free&&
                queued.Parts.Single(p=>p.Id==Drive).Hp==0,
                "one Free ticket does not repair any HP before next WIN");
            s=SaveSection(queued);
            if(cash==0)
            {
                Check(!ApprovedEconomyPolicyV7.TryReserveDestroyed(
                    s,Weapon,out _,out var rejected)
                    &&rejected=="INSUFFICIENT_SCRAP",
                    "second essential GUID cannot be booked with single Free and cash0");
                Check(s.Pending.Count==1&&s.Scrap==0&&s.FreeUsedForWin==1,
                    "failed second order does not debit cash or duplicate free ticket");
            }
            else
            {
                Check(ApprovedEconomyPolicyV7.TryReserveDestroyed(
                    s,Weapon,out var second,out var why2),
                    "second required weapon paid65 pre-order "+why2);
                Check(second.Pending.Count==2&&
                    second.Pending.Count(p=>p.Free)==1&&
                    second.Pending.Single(p=>!p.Free).ChargedScrap==65&&
                    second.Scrap==0,
                    "two required parts = exactly one Free plus one Paid, cash65");
                s=SaveSection(second);
            }
            var fight="second-fight-"+cash;
            s=Enter(s,fight,new[]{Drive,Weapon});
            s=Win(s,"distinct-loaner-win-"+cash,fight);
            Check(s.Wins==2&&s.Loaner.Phase==LoanerPhaseV7.Spent&&
                s.Parts.Single(p=>p.Id==Drive).Hp==1,
                "borrowed WIN completes pre-existing Free Drive order and spends permit");
            if(cash==0)
            {
                Check(s.Parts.Single(p=>p.Id==Weapon).Hp==0&&s.Pending.Count==0,
                    "source P0: remaining ESSENTIAL weapon stays 0HP after legal loaner WIN");
                Check(!ApprovedEconomyPolicyV7.TryReserveOneShotLoaner(
                    s,Facts(s,"new-fight"),Permit,out _,out _),
                    "spent one-shot cannot rescue a second essential module");
                Console.WriteLine("W29_TWO_ESSENTIALS_CASH0_1_FREE SOURCE_BLOCKED_AFTER_LOANER_WIN PASS");
            }
            else
            {
                Check(s.Parts.Single(p=>p.Id==Weapon).Hp==1&&s.Pending.Count==0,
                    "source recovery with preexisting Free+Paid orders after next WIN");
                Console.WriteLine("W29_TWO_ESSENTIALS_CASH65_FREE_PLUS_PAID SOURCE_RECOVERABLE PASS");
            }
            cases++;
        }
    }
    static int Main()
    {
        try
        {
            CheckPrice0AtZeroCash();
            CheckPaidThreshold(64,false);
            CheckPaidThreshold(65,true);
            CheckPriorWinFreeTicket();
            CheckIllegalUnmintedFree();
            CheckLossOrBlockedEvidence();
            CheckTwoEssentialPartsNeedTwoTickets();
            Check(cases==8,"eight original source cases including two-essential rig");
            Console.WriteLine("W28_ORIGINAL_V7_REACHABILITY_SUITE_PASS"+
                " cases="+cases+" checks="+checks+
                " | P0_ZERO_SCRAP_ESSENTIAL_WIN_STILL_BLOCKED"+
                " | NO_PRODUCTION_REPAIR_FIX | UNITY_G1_NOT_RUN"+
                " | COMBAT_IDS_SYNTHETIC");
            return 0;
        }
        catch(Exception e) { Console.Error.WriteLine(e);return 1; }
    }
}
