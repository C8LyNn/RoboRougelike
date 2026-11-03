using System;
using System.Linq;
using System.Collections.Generic;
using RoboRougelike.B01.Economy.ApprovedV7;
using RoboRougelike.B01.Economy.AdmissionW29;

internal static class Program
{
    static int checks;
    static void Ok(bool works,string name)
    {
        if(!works)throw new InvalidOperationException("W29_ADMISSION_TEST_RED: "+name);
        checks++;
    }
    const string Core="11111111111111111111111111111111";
    const string Drive="22222222222222222222222222222222";
    const string Reservation="33333333333333333333333333333333";
    const string Permit="44444444444444444444444444444444";
    static readonly CanonicalEconomyIdentityW29 Owner=
        new CanonicalEconomyIdentityW29("slot-owner","run-one",0);
    static ApprovedEconomySnapshotV7 Make(int price=100)
    {
        return new ApprovedEconomySnapshotV7 {
            OwnerId="slot-owner",RunId="run-one",Generation=0,  
            Parts=new List<OwnedModuleV7> {
                new OwnedModuleV7{Id=Core,Category="Core",Hp=1,PurchaseScrap=100},
                new OwnedModuleV7{Id=Drive,Category="EssentialDrive",Hp=0,PurchaseScrap=price}
            }
        };
    }
    static void ZeroWinFakeFreeCannotBeAdmitted()
    {
        var fake=Make();
        var drive=fake.Parts[1];
        drive.ReservationId=Reservation;
        fake.Pending.Add(new RebuildTicketV7 {
            ModuleId=Drive,ReservationId=Reservation,AfterWins=0,
            ChargedScrap=0,PurchaseScrap=100,Free=true
        });
        Ok(ApprovedEconomyPolicyV7.Valid(fake),
            "actual original V7 validator accepts manually forged WIN0 FREE");
        Ok(CanonicalEconomySectionV7.TryEncode(fake,out var actualBytes,out var why),
            "actual original V7 binary encoder accepts the malformed snapshot "+why);
        Ok(CanonicalEconomySectionV7.TryDecode(actualBytes,out var oldDecoded,out why)&&
           oldDecoded.Pending.Single().Free,
           "actual original V7 binary decoder retains WIN0 FREE");
        Ok(!EconomyV7AdmissionW29.TryAdmit(fake,Owner,out why)&&
           why=="FREE_TICKET_BEFORE_FIRST_AUTHENTIC_VICTORY",
           "new actual admission runtime rejects malicious or faulty migrated state");
        Ok(!EconomyV7AdmissionW29.TryEncodeForCanonicalOwner(fake,Owner,
            out var failedBytes,out why)&&failedBytes==null&&
           why=="FREE_TICKET_BEFORE_FIRST_AUTHENTIC_VICTORY",
           "cannot publish bad epoch0 FREE using checked codec");
        Ok(!EconomyV7AdmissionW29.TryDecodeForCanonicalOwner(actualBytes,Owner,
            out var failedState,out why)&&failedState==null&&
           why=="FREE_TICKET_BEFORE_FIRST_AUTHENTIC_VICTORY",
           "cannot import valid-checksum malformed epoch0 FREE using checked decoder");
    }
    static void LegitimateZeroPaidIsNotFree()
    {
        var s=Make(1);
        Ok(ApprovedEconomyPolicyV7.TryReserveDestroyed(s,Drive,
            out var paid,out var why),"P1=>zero PAID actual reducer "+why);
        Ok(paid.Pending.Count==1&&!paid.Pending[0].Free&&
           paid.Pending[0].ChargedScrap==0&&paid.Wins==0&&
           paid.FreeUsedForWin==0,"no bootstrapped free; P1 quote0 explicit PAID");
        var trusted=new CanonicalEconomyIdentityW29(
            Owner.OwnerId,Owner.RunId,paid.Generation);
        Ok(EconomyV7AdmissionW29.TryEncodeForCanonicalOwner(paid,trusted,
            out var bytes,out why),"legit zero PAID output allowed "+why);
        Ok(EconomyV7AdmissionW29.TryDecodeForCanonicalOwner(bytes,trusted,
            out var decoded,out why)&&
           !decoded.Pending[0].Free&&decoded.Pending[0].ChargedScrap==0,
           "legit zero PAID survives original section and strict canonical admission");
        Ok(!EconomyV7AdmissionW29.TryDecodeForCanonicalOwner(bytes,
            new CanonicalEconomyIdentityW29("foreign",Owner.RunId,paid.Generation),
            out _,out why)&&why=="FOREIGN_OWNER_OR_RUN",
            "no cross-owner import");
        Ok(!EconomyV7AdmissionW29.TryDecodeForCanonicalOwner(bytes,
            new CanonicalEconomyIdentityW29(Owner.OwnerId,"different-run",paid.Generation),
            out _,out why)&&why=="FOREIGN_OWNER_OR_RUN",
            "no cross-run import");
        Ok(!EconomyV7AdmissionW29.TryDecodeForCanonicalOwner(bytes,Owner,
            out _,out why)&&why=="FOREIGN_OR_STALE_GENERATION",
            "no stale generation replay");
        Ok(!EconomyV7AdmissionW29.TryDecodeForCanonicalOwner(bytes,null,
            out _,out why)&&why=="MISSING_TRUSTED_CANONICAL_OWNER",
            "no admission without real host identity");
        var damage=(byte[])bytes.Clone();damage[damage.Length-1]^=1;
        Ok(!EconomyV7AdmissionW29.TryDecodeForCanonicalOwner(damage,trusted,
            out _,out _),"corrupt original V7 SHA section denied");
    }
    static void RealFreeWinIsAllowedAndLoanerSpentPersists()
    {
        var start=Make();
        Ok(ApprovedEconomyPolicyV7.TryStageTrustedVictory(start,
            "scenario-win-1","encounter-1",out var won,out var why),
            "test-only host receipt original V7 stage "+why);
        Ok(ApprovedEconomyPolicyV7.TryReserveDestroyed(won,Drive,
            out var pending,out why),"first legitimate winner free ticket "+why);
        Ok(pending.Wins==1&&pending.FreeUsedForWin==1&&
           pending.Pending.Count==1&&pending.Pending[0].Free,
           "genuine-scenario Win1 owns only one available FREE ticket");
        var trusted=new CanonicalEconomyIdentityW29(
            Owner.OwnerId,Owner.RunId,pending.Generation);
        Ok(EconomyV7AdmissionW29.TryEncodeForCanonicalOwner(pending,trusted,
            out var bytes,out why),"legit first-WIN free may be stored");
        Ok(EconomyV7AdmissionW29.TryDecodeForCanonicalOwner(bytes,trusted,
            out var decoded,out why)&&decoded.Pending[0].Free&&
           decoded.ConsumedVictoryIds.SequenceEqual(new[]{"scenario-win-1"}),
           "legit Win1 ticket and exactly one receipt maintained");
        Ok(!ApprovedEconomyPolicyV7.TryReserveDestroyed(pending,Drive,
            out _,out _),"same owned GUID cannot be reserved again");
        Ok(ApprovedEconomyPolicyV7.TryStageTrustedVictory(pending,
            "scenario-win-2","encounter-2",out var settled,out why)&&
           settled.Parts.Single(x=>x.Id==Drive).Hp==1&&settled.Pending.Count==0,
           "next distinct win restores already pending part, no immediate free");
        Ok(!ApprovedEconomyPolicyV7.TryStageTrustedVictory(settled,
            "scenario-win-2","encounter-2",out _,out _),
            "winner replay denied by original reducer");
        var lo=Make(100);
        Ok(ApprovedEconomyPolicyV7.TryReserveOneShotLoaner(lo,
            new CanonicalDeploymentFactsV7 {
                OwnerId=lo.OwnerId,RunId=lo.RunId,Generation=lo.Generation,
                CoreId=Core,CoreFunctional=true,CanDeploy=false,
                HasGenuineNextChallenge=true,EncounterId="encounter-loaner",
                EssentialDestroyedPhysicalIds=new[]{Drive}
            },Permit,out var reserved,out why),"test-only physical facts grant one loaner");
        Ok(ApprovedEconomyPolicyV7.TryConsumeLoanerOnTrustedChallengeEntry(reserved,
            "encounter-loaner",Permit,out var entered,out why),
            "loaner consumed at challenge entry");
        Ok(ApprovedEconomyPolicyV7.TryStageTrustedLoss(entered,
            "encounter-loaner",out var lost,out why)&&
           lost.Loaner.Phase==LoanerPhaseV7.Spent,
           "loaner lost only once, no victory payout");
        trusted=new CanonicalEconomyIdentityW29(
            Owner.OwnerId,Owner.RunId,lost.Generation);
        Ok(EconomyV7AdmissionW29.TryEncodeForCanonicalOwner(lost,trusted,
            out bytes,out why)&&
           EconomyV7AdmissionW29.TryDecodeForCanonicalOwner(bytes,trusted,
            out decoded,out why)&&decoded.Loaner.Phase==LoanerPhaseV7.Spent,
            "Spent loaner persists across checked binary section");
    }
    static int Main()
    {
        try
        {
            ZeroWinFakeFreeCannotBeAdmitted();
            LegitimateZeroPaidIsNotFree();
            RealFreeWinIsAllowedAndLoanerSpentPersists();
            Console.WriteLine("W29_ECON_ADMISSION_SOURCE_PASS checks="+checks+
              " original_V7_input_flaw_reproduced=true checked_guard_denies=true"+
              " authoritative_826_save_NOT_WIRED=true unity_g1_NOT_RUN=true");
            return 0;
        }
        catch(Exception e){Console.Error.WriteLine(e);return 1;}
    }
}
