// B01-10 / P17 W27 / ECON-CAMPAIGN-001
// DIRECT EXACT B01-22 V7 C# SOURCE REGRESSION across multiple battles.
// Simulated combat event IDs ARE NOT genuine authenticated game receipts.
// Simulated physical damage IS NOT real Rig[]/Rigidbody/CanDeploy.
// The original producer sources are linked by exact commit+Git blob via CI.
using System;
using System.Linq;
using System.Collections.Generic;
using RoboRougelike.B01.Economy.ApprovedV7;

internal static class CampaignChecks
{
    static int checks, scenarios;
    static string GuidN(int value)=>value.ToString("x32");
    const string Owner="b01-10-offline-source-owner";
    const string Run="b01-10-offline-source-run";
    static readonly string Core=GuidN(1),Gun=GuidN(2),Permit=GuidN(9);
    static void Ok(bool condition,string label)
    {
        if(!condition)throw new Exception("W27_SOURCE_REGRESSION_FAIL: "+label);
        checks++;
    }
    static ApprovedEconomySnapshotV7 Init(int cash=800,bool brokenGun=false,int purchase=100)
    {
        return new ApprovedEconomySnapshotV7{
            OwnerId=Owner,RunId=Run,Scrap=cash,
            Parts=new List<OwnedModuleV7>{
                new OwnedModuleV7{Id=Core,Category="Core",Hp=1,PurchaseScrap=100},
                new OwnedModuleV7{Id=Gun,Category="EssentialGun",Hp=brokenGun?0:1,
                                  PurchaseScrap=purchase}
            }
        };
    }
    static ApprovedEconomySnapshotV7 Win(ApprovedEconomySnapshotV7 current,int id)
    {
        var good=ApprovedEconomyPolicyV7.TryStageTrustedVictory(current,
            "simulated-winner-"+id,"simulated-encounter-"+id,out var next,out var reason);
        Ok(good,"V7 original winner stage "+id+" "+reason);
        return next;
    }
    static ApprovedEconomySnapshotV7 Reserve(ApprovedEconomySnapshotV7 current,string id)
    {
        var good=ApprovedEconomyPolicyV7.TryReserveDestroyed(current,id,
            out var next,out var reason);
        Ok(good,"V7 original reserve "+id+" "+reason);
        return next;
    }
    static ApprovedEconomySnapshotV7 ColdSectionRoundTrip(ApprovedEconomySnapshotV7 s)
    {
        Ok(CanonicalEconomySectionV7.TryEncode(s,out var bytes,out var reason),
            "V7 original encode "+reason);
        Ok(CanonicalEconomySectionV7.TryDecode(bytes,out var copy,out reason),
            "V7 original decode "+reason);
        Ok(s.OwnerId==copy.OwnerId&&s.RunId==copy.RunId&&
           s.Generation==copy.Generation&&s.Wins==copy.Wins&&
           s.Scrap==copy.Scrap&&s.FreeUsedForWin==copy.FreeUsedForWin&&
           s.Parts.Count==copy.Parts.Count&&s.Pending.Count==copy.Pending.Count&&
           s.Loaner.Phase==copy.Loaner.Phase&&
           s.Loaner.EverEntered==copy.Loaner.EverEntered&&
           s.ConsumedVictoryIds.SequenceEqual(copy.ConsumedVictoryIds),
           "V7 original SECTION owner/wallet/win/parts/pending/loaner survived");
        return copy;
    }
    static void AddBroken(ApprovedEconomySnapshotV7 s,int id,int purchase)
    {
        s.Parts.Add(new OwnedModuleV7{
            Id=GuidN(id),Category="DamagedNonCore",Hp=0,PurchaseScrap=purchase
        });
        Ok(ApprovedEconomyPolicyV7.Valid(s),
           "synthetic physical damage is a valid owner state, not game authoritative");
    }
    static void TwelveConsecutiveVictories()
    {
        var s=Init();
        for(int battle=1;battle<=12;battle++)
        {
            var previousPending=s.Pending.Select(x=>x.ModuleId).ToArray();
            s=Win(s,battle);
            Ok(s.Pending.Count==0,"prior pending finished at new distinct victory");
            foreach(var id in previousPending)
                Ok(s.Parts.Single(p=>p.Id==id).Hp==1,
                   "previously booked physical ID restored only by distinct win");
            string a=GuidN(1000+battle*2),b=GuidN(1001+battle*2);
            AddBroken(s,1000+battle*2,100);
            AddBroken(s,1001+battle*2,100);
            Ok(s.Parts.Single(p=>p.Id==a).Hp==0&&
               s.Parts.Single(p=>p.Id==b).Hp==0,
               "newly broken in same victory not automatically restored");
            s=Reserve(s,a);s=Reserve(s,b);
            Ok(s.Pending.Count==2&&s.Pending.Count(t=>t.Free)==1&&
               s.Pending.Count(t=>!t.Free&&t.ChargedScrap==65)==1,
               "one free + one paid per distinct winner");
            s=ColdSectionRoundTrip(s);
        }
        Ok(s.Wins==12&&s.ConsumedVictoryIds.Count==12,
           "twelve accepted unique scripted win IDs recorded in V7");
        Ok(s.FreeUsedForWin==12&&s.Scrap==20,
           "twelve win campaign exactly 780 paid Scrap deducted");
        Ok(s.Parts.Count(p=>p.Hp==0)==2&&
           s.Parts.Count(p=>p.Hp==1)==24&&s.Pending.Count==2,
           "twelve WIN campaign: 22 restored new modules, 2 pending, Core+gun intact");
        Ok(!ApprovedEconomyPolicyV7.TryStageTrustedVictory(s,
             "simulated-winner-12","simulated-encounter-12",out _,out var reject)
             &&reject=="VICTORY_ID_REPLAY","replay cannot mint winner quota");
        s=Win(s,13);
        Ok(s.Pending.Count==0&&s.Parts.Count(p=>p.Hp==0)==0&&s.Scrap==20,
           "13th unique WIN clears prior queue without another wallet deduction");
        scenarios++;
        Console.WriteLine("W27_CAMPAIGN_12WINS 12_FREE 12_PAID 780_SCRAP 22_RESTORED_2_PENDING PASS");
    }
    static void PriceAndCouponContinuity()
    {
        foreach(var item in new[]{(1,0),(2,1),(3,1),(5,3),
                                   (15,9),(30,19),(100,65),
                                   (int.MaxValue,1395864370)})
            Ok(ApprovedEconomyPolicyV7.TryPaidQuote(item.Item1,out var actual)&&
               actual==item.Item2&&actual<item.Item1,
               "original floor65 positive price strict cheaper "+item.Item1);
        var s=Init(50);
        s=Win(s,101);
        int[] ids={201,202,203,204};
        int[] prices={30,1,5,15};
        for(int i=0;i<ids.Length;i++)AddBroken(s,ids[i],prices[i]);
        for(int i=0;i<ids.Length;i++)s=Reserve(s,GuidN(ids[i]));
        Ok(s.Pending.Count==4&&s.Pending.Count(x=>x.Free)==1&&
           s.Pending.Count(x=>!x.Free)==3&&
           s.Pending.Single(x=>x.ModuleId==GuidN(202)).ChargedScrap==0&&
           !s.Pending.Single(x=>x.ModuleId==GuidN(202)).Free,
           "paid-zero vs one free no duplicate winner coupon");
        Ok(s.Scrap==38&&s.FreeUsedForWin==1,
           "real V7 source deducted 0+3+9 from initial50");
        Ok(!ApprovedEconomyPolicyV7.TryReserveDestroyed(
             s,GuidN(202),out _,out var reject)&&
             reject=="ALREADY_RESERVED","duplicate physical id denied");
        s=ColdSectionRoundTrip(s);
        s=Win(s,102);
        Ok(s.Pending.Count==0&&ids.All(x=>s.Parts.Single(p=>p.Id==GuidN(x)).Hp==1)&&
           s.Scrap==38&&s.Wins==2,
           "four prior owned IDs rebuilt on NEXT unique WIN, wallet persisted");
        scenarios++;
        Console.WriteLine("W27_QUOTE_AND_COUPON price1_paid0 free1 paid3 next_win_restore PASS");
    }
    static CanonicalDeploymentFactsV7 BrokenFacts(ApprovedEconomySnapshotV7 s,string encounter)
    {
        return new CanonicalDeploymentFactsV7{
            OwnerId=s.OwnerId,RunId=s.RunId,Generation=s.Generation,
            CoreId=Core,CoreFunctional=true,CanDeploy=false,
            HasGenuineNextChallenge=true,EncounterId=encounter,
            EssentialDestroyedPhysicalIds=new[]{Gun}
        };
    }
    static ApprovedEconomySnapshotV7 EnterLoaner(ApprovedEconomySnapshotV7 s,
        string encounter)
    {
        Ok(ApprovedEconomyPolicyV7.TryReserveOneShotLoaner(
            s,BrokenFacts(s,encounter),Permit,out var reserve,out var reason),
            "eligible one-use loaner reserved "+reason);
        Ok(ApprovedEconomyPolicyV7.TryConsumeLoanerOnTrustedChallengeEntry(
            reserve,encounter,Permit,out var entered,out reason),
            "one-use loaner consumed on entry "+reason);
        Ok(entered.Loaner.Phase==LoanerPhaseV7.Entered&&
           entered.Loaner.EverEntered,"entry consumed; cannot reuse after restart");
        return ColdSectionRoundTrip(entered);
    }
    static void LoanerLossAcrossColdRestore()
    {
        var s=EnterLoaner(Init(0,true),"simulated-loaner-loss");
        Ok(ApprovedEconomyPolicyV7.TryStageTrustedLoss(s,
            "simulated-loaner-loss",out var lost,out var reason),
            "authentic outcome is NOT supplied; stage test-only loss "+reason);
        lost=ColdSectionRoundTrip(lost);
        Ok(lost.Loaner.Phase==LoanerPhaseV7.Spent&&lost.Wins==0&&
           lost.Scrap==0&&lost.Parts.Single(p=>p.Id==Gun).Hp==0,
           "loaner loss no fake win, heal, money or rearm");
        Ok(!ApprovedEconomyPolicyV7.TryReserveOneShotLoaner(lost,
            BrokenFacts(lost,"retry"),GuidN(90),out _,out var refusal)&&
            refusal=="ONE_SHOT_LOANER_ALREADY_RESERVED_OR_SPENT",
            "single-use loaner cannot be rearmed across cold section restore");
        scenarios++;
        Console.WriteLine("W27_LOANER_LOSS serial_spent_no_rearm_no_loot PASS");
    }
    static void LoanerVictorySoftlockNegativeWitness()
    {
        var s=Init(0,true);
        Ok(!ApprovedEconomyPolicyV7.TryReserveDestroyed(s,Gun,
            out _,out var cashFailure)&&cashFailure=="INSUFFICIENT_SCRAP",
            "no zero-win bootstrap and 0 wallet prevents early gun order");
        s=EnterLoaner(s,"simulated-loaner-win");
        Ok(ApprovedEconomyPolicyV7.TryStageTrustedVictory(s,
            "synthetic-victory-physical-untrusted","simulated-loaner-win",
            out var won,out var why),"V7 stage synthetic loaner WIN "+why);
        won=ColdSectionRoundTrip(won);
        Ok(won.Loaner.Phase==LoanerPhaseV7.Spent&&
           won.Parts.Single(p=>p.Id==Gun).Hp==0&&
           won.Pending.Count==0&&won.Wins==1,
           "CONFIRMED ORIGINAL V7 SOURCE gap: winning loaner did not heal unreserved gun");
        won=Reserve(won,Gun);
        Ok(won.Pending.Count==1&&won.Pending[0].Free&&
           won.Parts.Single(p=>p.Id==Gun).Hp==0&&
           won.Loaner.Phase==LoanerPhaseV7.Spent,
           "next-win-only pending gun still nondeployable and loaner exhausted");
        scenarios++;
        Console.WriteLine("W27_EXPECTED_SOURCE_CONTRACT_RED loaner_won_zero_cash_no_prior_order_gun0hp PASS");
    }
    static void DeploymentEligibilityNegatives()
    {
        var s=Init(0,true);
        var noChallenge=BrokenFacts(s,"none");
        noChallenge.HasGenuineNextChallenge=false;
        Ok(!ApprovedEconomyPolicyV7.TryReserveOneShotLoaner(
            s,noChallenge,Permit,out _,out _),"no next encounter -> no loaner");
        var optional=BrokenFacts(s,"optional");
        optional.CanDeploy=true;
        Ok(!ApprovedEconomyPolicyV7.TryReserveOneShotLoaner(
            s,optional,Permit,out _,out _),"functional robot -> no emergency loaner");
        var deadCore=BrokenFacts(s,"dead-core");
        deadCore.CoreFunctional=false;
        Ok(!ApprovedEconomyPolicyV7.TryReserveOneShotLoaner(
            s,deadCore,Permit,out _,out _),"dead Core -> no loaner");
        Ok(s.Loaner.Phase==LoanerPhaseV7.None&&s.Wins==0,
           "denied evidence cannot alter source loaner/win state");
        scenarios++;
        Console.WriteLine("W27_LOANER_PRECHECK no_next/dead_core/candeploy_nonessential PASS");
    }
    static void BinaryHistoryCorruption()
    {
        var s=Init();
        for(int i=1;i<=8;i++)s=Win(s,300+i);
        Ok(CanonicalEconomySectionV7.TryEncode(s,out var bytes,out var reason),
           "real V7 binary after eight distinct wins "+reason);
        var corrupt=(byte[])bytes.Clone();corrupt[corrupt.Length-1]^=0x10;
        Ok(!CanonicalEconomySectionV7.TryDecode(corrupt,out _,out _),
           "real V7 section rejects corrupted current wallet/win state");
        Ok(!CanonicalEconomySectionV7.TryDecode(
            bytes.Take(bytes.Length-5).ToArray(),out _,out _),
           "real V7 section rejects truncation");
        Ok(CanonicalEconomySectionV7.TryDecode(bytes,out var back,out reason)&&
           back.Wins==8&&back.ConsumedVictoryIds.Count==8&&
           !ApprovedEconomyPolicyV7.TryStageTrustedVictory(
                back,"simulated-winner-308","simulated-encounter-308",
                out _,out var replay)&&replay=="VICTORY_ID_REPLAY",
           "real V7 binary cold roundtrip retains anti-replay winner IDs");
        scenarios++;
        Console.WriteLine("W27_V7_BINARY_8WINS checksum/truncation/replay_source PASS");
    }
    static int Main()
    {
        try
        {
            TwelveConsecutiveVictories();
            PriceAndCouponContinuity();
            LoanerLossAcrossColdRestore();
            LoanerVictorySoftlockNegativeWitness();
            DeploymentEligibilityNegatives();
            BinaryHistoryCorruption();
            Ok(scenarios==6,"all six original-source multi-battle scenarios executed");
            Console.WriteLine("W27_SOURCE_TESTS_PASS scenarios="+scenarios+
                " checks="+checks+" | EXACT_ORIGINAL_B01_22_V7"+
                " | GAME_INTEGRATION=HOLD | NATIVE_UNITY=NOT_RUN"+
                " | REAL_COMBAT_PROOF=NOT_AVAILABLE");
            return 0;
        }
        catch(Exception e)
        {
            Console.Error.WriteLine(e.ToString());
            return 1;
        }
    }
}
