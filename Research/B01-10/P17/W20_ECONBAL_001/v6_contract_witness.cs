// B01-10 PASS17 W25: test the ACTUAL, exact SHA-pinned B01-22 C# V6.
// Diagnostics are EXPECTED CONTRACT RED at purchase=1 and no-wallet
// one-shot loaner. A detector PASS is NOT a product implementation PASS.
using System;
using System.Collections.Generic;
using RoboRougelike.B01.Economy.ApprovedV6;

static class SourceV6ContractWitness
{
    const string Core="aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    const string Gun="bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
    const string Permit="cccccccccccccccccccccccccccccccc";
    static void Check(bool pass,string message)
    {
        if(!pass)throw new Exception("ORIGINAL_SOURCE_WITNESS_FAILED: "+message);
    }
    static ApprovedEconomySnapshotV6 Owner(int purchase=100)
    {
        return new ApprovedEconomySnapshotV6 {
            OwnerId="one-owner",RunId="one-run",Scrap=0,
            Parts=new List<OwnedModuleV6> {
                new OwnedModuleV6{Id=Core,Category="Core",Hp=1,PurchaseScrap=100},
                new OwnedModuleV6{Id=Gun,Category="Gun",Hp=0,PurchaseScrap=purchase}
            }
        };
    }
    static OwnedModuleV6 TheGun(ApprovedEconomySnapshotV6 s)
        =>s.Parts.Find(p=>p.Id==Gun);
    static void Main()
    {
        int[] prices={1,2,3,5,15,30,100};
        int[] originalV6={1,1,2,3,10,20,65};
        int[] target={0,1,2,3,10,20,65};
        for(int i=0;i<prices.Length;i++)
        {
            Check(ApprovedEconomyPolicyV6.TryPaidQuote(prices[i],out int actual),
                "original C# V6 rejects positive purchase "+prices[i]);
            Check(actual==originalV6[i],
                "actual V6 original quotation mismatch price "+prices[i]);
            Check(target[i]==Math.Min((int)(((long)prices[i]*65+50)/100),
                                     prices[i]-1),
                "owner strict-lower proposed target mismatches");
            Check(i==0?actual>=prices[i]:actual<prices[i],
                "unexpected original V6 strict-cheaper witness at "+prices[i]);
        }
        Console.WriteLine("C_SHARP_ORIGINAL_V6_QUOTE 7/7 EXACT_SOURCE VALUES PASS");
        Console.WriteLine("SOURCE_CONTRACT_RED_CONFIRMED price=1 original_quote=1"
            +" owner_strict_cheaper_target=0; producer fix NOT APPLIED");

        // Source V6 cannot give a free order at zero victories. A real
        // combat WIN must be attested separately by an authoritative host.
        var s=Owner(15);
        Check(ApprovedEconomyPolicyV6.Valid(s),"valid pristine source V6");
        Check(!ApprovedEconomyPolicyV6.TryReserveDestroyed(s,Gun,
                    out var rejected,out var denial),
              "no-cash first-order accepted before genuine victory");
        Check(denial=="INSUFFICIENT_SCRAP",
              "unexpected no-cash owner V6 denial reason");
        Check(ApprovedEconomyPolicyV6.TryStageTrustedVictory(s,
                    "winner-one","encounter-one",out var won,out var winReason),
              "policy must stage first winner (not authenticating it)");
        Check(won.Wins==1&&won.Pending.Count==0&&TheGun(won).Hp==0,
              "first actual winner incorrectly restored unreserved gun");
        Check(ApprovedEconomyPolicyV6.TryReserveDestroyed(won,Gun,
                    out var queued,out var orderReason),
              "one eligible free AFTER first winner must queue");
        Check(queued.Pending.Count==1&&queued.Pending[0].Free&&
              queued.Pending[0].ChargedScrap==0&&TheGun(queued).Hp==0,
              "booked free repair did not defer to next WIN");
        Check(ApprovedEconomyPolicyV6.TryStageTrustedVictory(queued,
                    "winner-two","encounter-two",out var healed,out var reason),
              "distinct second winner should heal old ticket");
        Check(TheGun(healed).Hp==1&&healed.Pending.Count==0&&healed.Wins==2,
              "next distinct winner failed to complete prior queued rebuild");
        Console.WriteLine("C_SHARP_ORIGINAL_V6_DISTINCT_WIN_DEFER 1/1 SOURCE PASS");

        // Independent direct ORIGINAL C# negative witness: one-use loaner
        // can win, but an unreserved gun at 0 Scrap is still broken and the
        // one-shot loaner is spent. This is a gameplay-integration HOLD.
        s=Owner();
        var facts=new CanonicalDeploymentFactsV6{
            OwnerId=s.OwnerId, RunId=s.RunId, CoreId=Core,
            EncounterId="encounter-loaner", Generation=s.Generation,
            CoreFunctional=true,CanDeploy=false,HasGenuineNextChallenge=true,
            EssentialDestroyedPhysicalIds=new[]{Gun}
        };
        Check(!ApprovedEconomyPolicyV6.TryReserveDestroyed(s,Gun,
                    out rejected,out denial) && denial=="INSUFFICIENT_SCRAP",
              "no-cash unreserved required part should be blocked");
        Check(ApprovedEconomyPolicyV6.TryReserveOneShotLoaner(s,facts,
                    Permit,out var reserved,out var reserveReason),
              "source V6 eligible single loaner reserve");
        Check(ApprovedEconomyPolicyV6.TryConsumeLoanerOnTrustedChallengeEntry(
                    reserved,"encounter-loaner",Permit,
                    out var entered,out var entryReason),
              "source V6 entered loaner");
        Check(ApprovedEconomyPolicyV6.TryStageTrustedVictory(
                    entered,"winner-loaner","encounter-loaner",
                    out var finished,out var finishReason),
              "source V6 staged correct entered-loaner victory");
        Check(finished.Loaner.Phase==LoanerPhaseV6.Spent&&
              finished.Loaner.EverEntered&&TheGun(finished).Hp==0&&
              finished.Pending.Count==0&&finished.Wins==1,
              "source V6 did not expose exhausted loaner + broken gun");
        Check(ApprovedEconomyPolicyV6.TryReserveDestroyed(finished,Gun,
                    out var tooLate,out var lateReason),
              "source V6 postwinner free ticket should become available");
        Check(tooLate.Pending.Count==1&&tooLate.Pending[0].Free&&
              TheGun(tooLate).Hp==0&&
              tooLate.Loaner.Phase==LoanerPhaseV6.Spent,
              "source V6 missing second-unreachable-win circular risk");
        Console.WriteLine("C_SHARP_ORIGINAL_V6_LOANER_ZERO_SCRAP_NEGATIVE"
            +" 1/1 EXPECTED_SOURCE_POLICY_HOLD");
        Console.WriteLine("B01_10_V6_ORIGINAL_SOURCE_DETECTOR_PASS"
            +" | V6_SOURCE_GAME_CONTRACT=RED | UNITY_G1=NOT_RUN");
    }
}
