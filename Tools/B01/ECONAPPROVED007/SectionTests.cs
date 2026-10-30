using System;
using System.Linq;
using System.Collections.Generic;
using RoboRougelike.B01.Economy.ApprovedV7;

static class SectionChecks
{
    static int count;
    static void Ok(bool yes,string why)
    {if(!yes)throw new Exception("W25_SECTION_RED "+why);count++;}
    static ApprovedEconomySnapshotV7 Make()
    {
        return new ApprovedEconomySnapshotV7 {
            OwnerId="owner",RunId="run",Generation=1,Wins=1,
            FreeUsedForWin=1,Scrap=0,
            ConsumedVictoryIds=new List<string>{"genuine-combat-win-one"},
            Parts=new List<OwnedModuleV7>{
                new OwnedModuleV7{Id=Tests.A,Category="Core",Hp=1,PurchaseScrap=100},
                new OwnedModuleV7{Id=Tests.B,Category="Drive",Hp=0,PurchaseScrap=1},
                new OwnedModuleV7{Id=Tests.C,Category="Gun",Hp=0,PurchaseScrap=5}
            }
        };
    }
    public static int Run()
    {
        var s=Make();
        Ok(ApprovedEconomyPolicyV7.Valid(s),"model initial");
        Ok(ApprovedEconomyPolicyV7.TryReserveDestroyed(s,Tests.B,out var paid,out var why),
            "zero-quote PAID reserve "+why);
        var ticket=paid.Pending.Single();
        Ok(ticket.ChargedScrap==0&&!ticket.Free&&
           paid.FreeUsedForWin==1&&paid.Scrap==0,
           "0 PAID does not consume a free reward ticket or mint currency");
        Ok(ApprovedEconomyPolicyV7.TryReserveOneShotLoaner(paid,
            new CanonicalDeploymentFactsV7 {
                OwnerId="owner",RunId="run",Generation=paid.Generation,
                CoreId=Tests.A,CoreFunctional=true,CanDeploy=false,
                HasGenuineNextChallenge=true,EncounterId="next-real-encounter",
                EssentialDestroyedPhysicalIds=new[]{Tests.B}
            },Tests.P,out var reserved,out why),"loaner reserve staged "+why);
        Ok(CanonicalEconomySectionV7.TryEncode(reserved,out var bytes,out why),
            "encode all owner fields "+why);
        Ok(bytes.Length>CanonicalEconomySectionV7.HeaderBytes&&
           bytes.Length<CanonicalEconomySectionV7.MaxSectionBytes,
           "bounded section physical bytes");
        Ok(CanonicalEconomySectionV7.TryDecode(bytes,out var reloaded,out why),
            "fresh independent decoder run "+why);
        Ok(reloaded.Generation==reserved.Generation&&
           reloaded.OwnerId=="owner"&&reloaded.RunId=="run"&&
           reloaded.Scrap==0&&reloaded.Wins==1&&
           reloaded.FreeUsedForWin==1&&reloaded.Pending.Count==1&&
           !reloaded.Pending[0].Free&&reloaded.Pending[0].ChargedScrap==0&&
           reloaded.Parts.Single(p=>p.Id==Tests.B).Hp==0&&
           reloaded.Loaner.Phase==LoanerPhaseV7.Reserved&&
           reloaded.Loaner.PermitId==Tests.P&&
           reloaded.ConsumedVictoryIds.Single()=="genuine-combat-win-one",
           "physical owner/IDs/wallet/zero PAID/0HP/loaner/win IDs survived");
        Ok(CanonicalEconomySectionV7.TryEncode(reloaded,out var identical,out why)&&
           bytes.SequenceEqual(identical),"binary byte exact deterministic roundtrip");
        var modified=(byte[])bytes.Clone();modified[modified.Length-4]^=0x01;
        Ok(!CanonicalEconomySectionV7.TryDecode(modified,out _,out why)&&
           why=="ECON_SECTION_CHECKSUM","changed physical economy byte rejected");
        Ok(!CanonicalEconomySectionV7.TryDecode(bytes.Take(bytes.Length-1).ToArray(),
            out _,out why),"torn end-of-section rejected");
        var appended=bytes.Concat(new byte[]{0}).ToArray();
        Ok(!CanonicalEconomySectionV7.TryDecode(appended,out _,out why)&&
           why=="ECON_SECTION_LENGTH","appended foreign data denied");
        var magic=(byte[])bytes.Clone();magic[0]^=0x01;
        Ok(!CanonicalEconomySectionV7.TryDecode(magic,out _,out why)&&
           why=="ECON_SECTION_MAGIC","wrong section magic denied");
        var length=(byte[])bytes.Clone();length[9]^=0x01;
        Ok(!CanonicalEconomySectionV7.TryDecode(length,out _,out why)&&
           why=="ECON_SECTION_LENGTH","forged length denied");
        Ok(!CanonicalEconomySectionV7.TryDecode(null,out _,out _),
           "null section denied");
        var duplicate=reserved.Copy();
        duplicate.Parts.Add(duplicate.Parts[0].Copy());
        Ok(!CanonicalEconomySectionV7.TryEncode(duplicate,out _,out _),
           "duplicate physical ID cannot encode");
        var noLoaner=reserved.Copy();
        noLoaner.Loaner.Phase=LoanerPhaseV7.Entered;
        noLoaner.Loaner.EverEntered=true;
        Ok(CanonicalEconomySectionV7.TryEncode(noLoaner,out var enteredBytes,out why),
           "durably embed spent-on-entry truth");
        Ok(CanonicalEconomySectionV7.TryDecode(enteredBytes,out var entry,out why)&&
           entry.Loaner.Phase==LoanerPhaseV7.Entered&&entry.Loaner.EverEntered,
           "one-shot entered state persists across Save reload");
        Ok(!ApprovedEconomyPolicyV7.TryConsumeLoanerOnTrustedChallengeEntry(
           entry,"next-real-encounter",Tests.P,out _,out _),
           "cannot enter already-consumed loaner after restart");
        Ok(ApprovedEconomyPolicyV7.TryStageTrustedLoss(entry,
           "next-real-encounter",out var lost,out why),"real loss transitions staged "+why);
        Ok(CanonicalEconomySectionV7.TryEncode(lost,out var lostBytes,out why)&&
           CanonicalEconomySectionV7.TryDecode(lostBytes,out var lostRead,out why)&&
           lostRead.Loaner.Phase==LoanerPhaseV7.Spent&&
           lostRead.Wins==1&&lostRead.Scrap==0&&
           lostRead.Pending.Count==1,"loss cannot heal, rearm or mint wins");
        var fork=lostRead.Copy();fork.Generation=lostRead.Generation;
        fork.Loaner.PermitId=Tests.D;
        Ok(CanonicalEconomySectionV7.TryEncode(fork,out var forkBytes,out why)&&
           !forkBytes.SequenceEqual(lostBytes),
           "same-generation different ledger has different section hash, must be fenced by 749");
        Ok(!ApprovedEconomyPolicyV7.TryStageTrustedVictory(lostRead,
           "genuine-combat-win-one","next-real-encounter",out _,out _),
           "replayed win cannot recharge zero-paid coupon");
        var win=lostRead.Copy();
        Ok(ApprovedEconomyPolicyV7.TryStageTrustedVictory(win,
           "genuine-combat-win-two","next-real-encounter",out var nextWin,out why),
           "trusted second win staged "+why);
        Ok(nextWin.Pending.Count==0&&nextWin.Wins==2&&
           nextWin.Parts.Single(p=>p.Id==Tests.B).Hp==1&&
           nextWin.Scrap==0,"NEXT distinct verified win heals same stable physical ID");
        Ok(CanonicalEconomySectionV7.TryEncode(nextWin,out var winBytes,out why)&&
           CanonicalEconomySectionV7.TryDecode(winBytes,out var winCopy,out why)&&
           winCopy.ConsumedVictoryIds.SequenceEqual(new[]{
               "genuine-combat-win-one","genuine-combat-win-two"
           }),"next-win settled ledger encoded without extra duplicated reward");
        Ok(!CanonicalEconomySectionV7.TryDecode(new byte[CanonicalEconomySectionV7.MaxSectionBytes+1],
           out _,out _),"oversized section denied before allocation");
        Console.WriteLine("W25_SECTION_SOURCE_PASS checks="+count+" checksum_not_authenticity=true owner_749=NOT_WIRED");
        return count;
    }
}
