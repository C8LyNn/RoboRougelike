// B01-10 PASS17 W25: test the ACTUAL, exact SHA-pinned B01-22 C# V7.
// Diagnostics are EXPECTED CONTRACT RED at purchase=1 and no-wallet
// one-shot loaner. A detector PASS is NOT a product implementation PASS.
using System;
using System.Collections.Generic;
using RoboRougelike.B01.Economy.ApprovedV7;

static class SourceV7ContractWitness
{
    const string Core="aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    const string Gun="bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
    const string Permit="cccccccccccccccccccccccccccccccc";
    static void Check(bool pass,string message)
    {
        if(!pass)throw new Exception("ORIGINAL_SOURCE_WITNESS_FAILED: "+message);
    }
    static ApprovedEconomySnapshotV7 Owner(int purchase=100)
    {
        return new ApprovedEconomySnapshotV7 {
            OwnerId="one-owner",RunId="one-run",Scrap=0,
            Parts=new List<OwnedModuleV7> {
                new OwnedModuleV7{Id=Core,Category="Core",Hp=1,PurchaseScrap=100},
                new OwnedModuleV7{Id=Gun,Category="Gun",Hp=0,PurchaseScrap=purchase}
            }
        };
    }
    static OwnedModuleV7 TheGun(ApprovedEconomySnapshotV7 s)
        =>s.Parts.Find(p=>p.Id==Gun);
    static void Main()
    {
        int[] prices={1,2,3,5,15,30,100};
        int[] originalV7={0,1,1,3,9,19,65};
        int[] target={0,1,1,3,9,19,65};
        for(int i=0;i<prices.Length;i++)
        {
            Check(ApprovedEconomyPolicyV7.TryPaidQuote(prices[i],out int actual),
                "original C# V7 rejects positive purchase "+prices[i]);
            Check(actual==originalV7[i],
                "actual V7 original quotation mismatch price "+prices[i]);
            Check(target[i]==(int)(((long)prices[i]*65)/100),
                "owner strict-lower proposed target mismatches");
            Check(actual<prices[i],
                "unexpected original V7 strict-cheaper witness at "+prices[i]);
        }
        Console.WriteLine("C_SHARP_ORIGINAL_V7_QUOTE 7/7 EXACT_SOURCE VALUES PASS");
        Console.WriteLine("SOURCE_CONTRACT_GREEN_CONFIRMED price=1 original_quote=0"
            +" owner_strict_cheaper_target=0; V7 source fixed quotation");

        // Source V7 cannot give a free order at zero victories. A real
        // combat WIN must be attested separately by an authoritative host.
        var s=Owner(15);
        Check(ApprovedEconomyPolicyV7.Valid(s),"valid pristine source V7");
        Check(!ApprovedEconomyPolicyV7.TryReserveDestroyed(s,Gun,
                    out var rejected,out var denial),
              "no-cash first-order accepted before genuine victory");
        Check(denial=="INSUFFICIENT_SCRAP",
              "unexpected no-cash owner V7 denial reason");
        Check(ApprovedEconomyPolicyV7.TryStageTrustedVictory(s,
                    "winner-one","encounter-one",out var won,out var winReason),
              "policy must stage first winner (not authenticating it)");
        Check(won.Wins==1&&won.Pending.Count==0&&TheGun(won).Hp==0,
              "first actual winner incorrectly restored unreserved gun");
        Check(ApprovedEconomyPolicyV7.TryReserveDestroyed(won,Gun,
                    out var queued,out var orderReason),
              "one eligible free AFTER first winner must queue");
        Check(queued.Pending.Count==1&&queued.Pending[0].Free&&
              queued.Pending[0].ChargedScrap==0&&TheGun(queued).Hp==0,
              "booked free repair did not defer to next WIN");
        Check(ApprovedEconomyPolicyV7.TryStageTrustedVictory(queued,
                    "winner-two","encounter-two",out var healed,out var reason),
              "distinct second winner should heal old ticket");
        Check(TheGun(healed).Hp==1&&healed.Pending.Count==0&&healed.Wins==2,
              "next distinct winner failed to complete prior queued rebuild");
        Console.WriteLine("C_SHARP_ORIGINAL_V7_DISTINCT_WIN_DEFER 1/1 SOURCE PASS");

        // Independent direct ORIGINAL C# negative witness: one-use loaner
        // can win, but an unreserved gun at 0 Scrap is still broken and the
        // one-shot loaner is spent. This is a gameplay-integration HOLD.
        s=Owner();
        var facts=new CanonicalDeploymentFactsV7{
            OwnerId=s.OwnerId, RunId=s.RunId, CoreId=Core,
            EncounterId="encounter-loaner", Generation=s.Generation,
            CoreFunctional=true,CanDeploy=false,HasGenuineNextChallenge=true,
            EssentialDestroyedPhysicalIds=new[]{Gun}
        };
        Check(!ApprovedEconomyPolicyV7.TryReserveDestroyed(s,Gun,
                    out rejected,out denial) && denial=="INSUFFICIENT_SCRAP",
              "no-cash unreserved required part should be blocked");
        Check(ApprovedEconomyPolicyV7.TryReserveOneShotLoaner(s,facts,
                    Permit,out var reserved,out var reserveReason),
              "source V7 eligible single loaner reserve");
        Check(ApprovedEconomyPolicyV7.TryConsumeLoanerOnTrustedChallengeEntry(
                    reserved,"encounter-loaner",Permit,
                    out var entered,out var entryReason),
              "source V7 entered loaner");
        Check(ApprovedEconomyPolicyV7.TryStageTrustedVictory(
                    entered,"winner-loaner","encounter-loaner",
                    out var finished,out var finishReason),
              "source V7 staged correct entered-loaner victory");
        Check(finished.Loaner.Phase==LoanerPhaseV7.Spent&&
              finished.Loaner.EverEntered&&TheGun(finished).Hp==0&&
              finished.Pending.Count==0&&finished.Wins==1,
              "source V7 did not expose exhausted loaner + broken gun");
        Check(ApprovedEconomyPolicyV7.TryReserveDestroyed(finished,Gun,
                    out var tooLate,out var lateReason),
              "source V7 postwinner free ticket should become available");
        Check(tooLate.Pending.Count==1&&tooLate.Pending[0].Free&&
              TheGun(tooLate).Hp==0&&
              tooLate.Loaner.Phase==LoanerPhaseV7.Spent,
              "source V7 missing second-unreachable-win circular risk");
        Console.WriteLine("C_SHARP_ORIGINAL_V7_LOANER_ZERO_SCRAP_NEGATIVE"
            +" 1/1 EXPECTED_SOURCE_POLICY_HOLD");
        Console.WriteLine("B01_10_V7_ORIGINAL_SOURCE_DETECTOR_PASS"
            +" | V7_QUOTE_SOURCE_CONTRACT=GREEN | LOANER_RECOVERY_HOLD | UNITY_G1=NOT_RUN");
    }
}
