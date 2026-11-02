// B01-22/W27: EXACT unchanged ECON V7 C#9 multi-encounter regression.
// No competing runtime implementation, wallet, Save writer or spoofed live win.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RoboRougelike.B01.Economy.ApprovedV7;

internal static class CampaignChecks
{
    public const string Core="11111111111111111111111111111111";
    public const string Drive="22222222222222222222222222222222";
    public const string Weapon="33333333333333333333333333333333";
    public const string Armor="44444444444444444444444444444444";
    public const string Permit="55555555555555555555555555555555";
    private static int checks;
    private static void Assert(bool yes,string name)
    {
        if(!yes)throw new InvalidOperationException("SOURCE_REGRESSION_FAIL: "+name);
        checks++;
    }
    static ApprovedEconomySnapshotV7 NewRun(int scrap)
    {
        var s=new ApprovedEconomySnapshotV7 {
            OwnerId="w27-owner",RunId="w27-run",Scrap=scrap,
            Parts=new List<OwnedModuleV7> {
                new OwnedModuleV7 {Id=Core,Category="Core",Hp=1,PurchaseScrap=100},
                new OwnedModuleV7 {Id=Drive,Category="Drive",Hp=1,PurchaseScrap=1},
                new OwnedModuleV7 {Id=Weapon,Category="Weapon",Hp=1,PurchaseScrap=100},
                new OwnedModuleV7 {Id=Armor,Category="OptionalArmor",Hp=1,PurchaseScrap=30}
            }
        };
        Assert(ApprovedEconomyPolicyV7.Valid(s),"fresh owner V7 ledger");
        return s;
    }
    static ApprovedEconomySnapshotV7 DestroyAfterEncounter(ApprovedEconomySnapshotV7 state,
        params string[] ids)
    {
        // Represents already-authoritative COMBAT host events, not game runtime.
        // No fake receipts, wallet modifications or physical GameObjects here.
        var next=state.Copy();
        foreach(var id in ids)
        {
            var p=next.Parts.Single(x=>x.Id==id);
            Assert(p.Hp==1&&string.IsNullOrEmpty(p.ReservationId),
                "previous challenge restored or new part before destruction "+id);
            p.Hp=0;
        }
        Assert(ApprovedEconomyPolicyV7.Valid(next),"postfight destroyed detached view");
        return next;
    }
    static ApprovedEconomySnapshotV7 Reserve(ApprovedEconomySnapshotV7 state,
        string id)
    {
        bool ok=ApprovedEconomyPolicyV7.TryReserveDestroyed(state,id,
            out var proposed,out var reason);
        Assert(ok,"owned postfight reservation "+id+" reason="+reason);
        Assert(state.Parts.Single(x=>x.Id==id).Hp==0,
            "existing owner remained destroyed during staged reservation");
        Assert(proposed.Parts.Single(x=>x.Id==id).Hp==0,
            "reserved part NEVER instantly returns to 1 HP");
        return proposed;
    }
    static ApprovedEconomySnapshotV7 Win(ApprovedEconomySnapshotV7 state,
        string receipt,string encounter)
    {
        // Original production function is a REDUCER ONLY: the "receipt" below
        // is a scenario input, not issued by real authoritative combat.
        Assert(ApprovedEconomyPolicyV7.TryStageTrustedVictory(state,receipt,encounter,
            out var updated,out var reason),"stage unique scenario victory "+reason);
        Assert(updated.ConsumedVictoryIds.Last()==receipt,
            "true distinct scenario receipt committed to staged owner");
        return updated;
    }
    static ApprovedEconomySnapshotV7 Roundtrip(ApprovedEconomySnapshotV7 state)
    {
        Assert(CanonicalEconomySectionV7.TryEncode(state,out var bytes,out var why),
            "encode original V7 "+why);
        Assert(CanonicalEconomySectionV7.TryDecode(bytes,out var decoded,out why),
            "decode actual original V7 "+why);
        Assert(decoded.OwnerId==state.OwnerId&&decoded.RunId==state.RunId&&
            decoded.Generation==state.Generation&&decoded.Wins==state.Wins&&
            decoded.Scrap==state.Scrap&&
            decoded.FreeUsedForWin==state.FreeUsedForWin&&
            decoded.ConsumedVictoryIds.SequenceEqual(state.ConsumedVictoryIds)&&
            decoded.Pending.Count==state.Pending.Count&&
            decoded.Loaner.Phase==state.Loaner.Phase&&
            decoded.Parts.Select(x=>x.Id).SequenceEqual(state.Parts.Select(x=>x.Id)),
            "same full ledger across in-memory snapshot serialization");
        return decoded;
    }
    static void PriceBoundaries()
    {
        foreach(var p in new[]{1,2,3,5,15,30,100,101,999,int.MaxValue})
        {
            Assert(ApprovedEconomyPolicyV7.TryPaidQuote(p,out var q),
                "positive purchase quotes "+p);
            Assert(q==(int)((long)p*65/100)&&q>=0&&q<p,
                "65% FLOOR STRICTLY BELOW new price "+p);
        }
        Assert(!ApprovedEconomyPolicyV7.TryPaidQuote(0,out _),"reject purchase0");
    }
    static ApprovedEconomySnapshotV7 Campaign(int encounters)
    {
        var s=NewRun(5000);
        int freeCount=0,paidCount=0,zeroPaidCount=0,spent=0,restored=0;
        for(int i=1;i<=encounters;i++)
        {
            int before=s.Wins;
            var queued=s.Pending.Select(x=>x.ModuleId).ToArray();
            s=Win(s,"genuine-scenario-receipt-"+i,"encounter-"+i);
            Assert(s.Wins==before+1,"one win exactly once");
            Assert(s.Pending.Count==0,"previous fight tickets settled at next win");
            foreach(var partId in queued)
                Assert(s.Parts.Single(p=>p.Id==partId).Hp==1,
                    "prior distinct encounter physically restored "+partId);
            restored+=queued.Length;
            var original=s.Copy();
            Assert(!ApprovedEconomyPolicyV7.TryStageTrustedVictory(s,
                "genuine-scenario-receipt-"+i,"encounter-"+i,out _,out _),
                "same-win replay fails; cannot immediately restore new 0HP");
            Assert(s.Generation==original.Generation&&
                s.Scrap==original.Scrap&&s.Wins==original.Wins,
                "rejected replay never changes any wallet/epoch");
            s=DestroyAfterEncounter(s,Weapon,Armor,Drive);
            s=Reserve(s,Weapon); // one free for a genuine earlier W_i
            s=Reserve(s,Armor); // PAID 19 each
            s=Reserve(s,Drive); // PAID 0, not free
            Assert(s.Pending.Count==3,"three distinct physically owned queued parts");
            var qW=s.Pending.Single(x=>x.ModuleId==Weapon);
            var qA=s.Pending.Single(x=>x.ModuleId==Armor);
            var qD=s.Pending.Single(x=>x.ModuleId==Drive);
            Assert(qW.Free&&qW.ChargedScrap==0&&
                !qA.Free&&qA.ChargedScrap==19&&
                !qD.Free&&qD.ChargedScrap==0,
                "one free, nineteen paid, zero-priced PAID not another free");
            Assert(s.FreeUsedForWin==i,"free voucher consumed only at current winner");
            Assert(!ApprovedEconomyPolicyV7.TryReserveDestroyed(s,Weapon,out _,out _),
                "same owned physical GUID cannot queue twice");
            freeCount++;
            paidCount+=2;zeroPaidCount++;
            spent+=19;
            s=Roundtrip(s);
            Assert(s.Scrap==5000-spent&&s.Wins==i,
                "wallet survives each of the "+i+" genuine-scenario encounters");
        }
        Assert(s.Pending.Count==3,"last fight's parts PENDING until future distinct win");
        Assert(restored==(encounters-1)*3,"total real previous-win settlements");
        Assert(freeCount==encounters&&paidCount==2*encounters&&
            zeroPaidCount==encounters,
            "global quota: exactly one free per distinct winner across campaign");
        Assert(s.Scrap==5000-19*encounters&&s.FreeUsedForWin==encounters,
            "exact 19 Scrap paid each fight; no duplicate free quota");
        Assert(s.ConsumedVictoryIds.Count==encounters &&
            s.ConsumedVictoryIds.Distinct(StringComparer.Ordinal).Count()==encounters,
            "all distinct receipts survive N fights and serialized restores");
        Console.WriteLine("CAMPAIGN_SOURCE_PASS battles="+encounters+
            " queued="+(encounters*3)+" restored="+restored+
            " pending=3 free="+freeCount+" paid="+paidCount+
            " zero_paid="+zeroPaidCount+" wallet="+s.Scrap);
        return s;
    }
    static CanonicalDeploymentFactsV7 Facts(ApprovedEconomySnapshotV7 s,
        bool hasEncounter=true,bool canDeploy=false)
    {
        return new CanonicalDeploymentFactsV7 {
            OwnerId=s.OwnerId,RunId=s.RunId,Generation=s.Generation,
            CoreId=Core,CoreFunctional=true,CanDeploy=canDeploy,
            HasGenuineNextChallenge=hasEncounter,
            EncounterId="loaner-encounter-1",
            EssentialDestroyedPhysicalIds=new[]{Drive}
        };
    }
    static void ZeroWalletLoanerWonSoftlock()
    {
        var first=NewRun(0);
        first=DestroyAfterEncounter(first,Drive);
        Assert(ApprovedEconomyPolicyV7.TryReserveDestroyed(first,Drive,
            out var zeroPaid,out var why)&&
            zeroPaid.Pending.Single().ChargedScrap==0&&
            !zeroPaid.Pending.Single().Free,
            "P1 priced drive CAN queue zero Scrap PAID, never a free coupon");
        // The tiny P1 Drive can be queued for paid 0. To test real unrepairable
        // essential Drive, elevate equivalent purchase cost to 100.
        var costly=first.Copy();
        costly.Parts.Single(p=>p.Id==Drive).PurchaseScrap=100;
        Assert(!ApprovedEconomyPolicyV7.TryReserveDestroyed(costly,Drive,
            out _,out why)&&why=="INSUFFICIENT_SCRAP",
            "essential damaged expensive drive with zero funds cannot queue");
        Assert(ApprovedEconomyPolicyV7.TryReserveOneShotLoaner(costly,
            Facts(costly),Permit,out var reserved,out why),
            "living Core and genuine next fight allows ONE rescue slot");
        Assert(ApprovedEconomyPolicyV7.TryConsumeLoanerOnTrustedChallengeEntry(reserved,
            "loaner-encounter-1",Permit,out var entered,out why),
            "one-shot admitted to real encounter in staged state");
        var winning=Win(entered,"loaner-distinct-win-1","loaner-encounter-1");
        Assert(winning.Wins==1&&winning.Loaner.Phase==LoanerPhaseV7.Spent&&
            winning.Scrap==0&&winning.Parts.Single(p=>p.Id==Drive).Hp==0 &&
            winning.Pending.Count==0,
            "victory mints an unused free coupon, NOT a rebuilt essential drive");
        var queued=Reserve(winning,Drive);
        Assert(queued.FreeUsedForWin==1&&queued.Pending.Count==1&&
            queued.Pending[0].Free&&queued.Parts.Single(p=>p.Id==Drive).Hp==0,
            "after WIN user can reserve essential free, but it still awaits WIN2");
        Assert(!ApprovedEconomyPolicyV7.TryReserveOneShotLoaner(queued,
            Facts(queued),Permit,out _,out why)&&
            why=="ONE_SHOT_LOANER_ALREADY_RESERVED_OR_SPENT",
            "ONE-SHOT rescue cannot rearm for a second genuine victory");
        queued=Roundtrip(queued);
        Assert(queued.Loaner.EverEntered&&queued.Loaner.Phase==LoanerPhaseV7.Spent &&
            queued.Pending.Single().ModuleId==Drive,
            "cold-style economy section retains trapped outstanding drive");
        // Source result is NOT a Unity product guarantee. With the ONLY
        // essential drive still 0HP, zero funds, no second loaner, there is
        // no legal second entry under owner-approved gameplay assumptions.
        bool actualDriveOperational=queued.Parts.Single(p=>p.Id==Drive).Hp>0;
        bool emergencyRoute=queued.Loaner.Phase==LoanerPhaseV7.None;
        Assert(!actualDriveOperational&&!emergencyRoute,
            "P0 softlock witness detected: loaner win, essential still 0HP");
        Console.WriteLine("P0_SOFTLOCK_WITNESS_DETECTED scenario=zero_wallet_dead_essential_alive_core"+
            " loaner_win1=true free_ticket_reserved_after_win=true next_win_required=true"+
            " loaner_spent=true normal_deploy=false source_flow=HOLD");
    }
    static void LoanerDefeatAndOptionalDamage()
    {
        var game=NewRun(0);
        game=DestroyAfterEncounter(game,Drive);
        game.Parts.Single(x=>x.Id==Drive).PurchaseScrap=100;
        Assert(!ApprovedEconomyPolicyV7.TryReserveOneShotLoaner(game,
            Facts(game,canDeploy:true),Permit,out _,out _),
            "optional break must not incorrectly qualify as rescue");
        Assert(!ApprovedEconomyPolicyV7.TryReserveOneShotLoaner(game,
            Facts(game,hasEncounter:false),Permit,out _,out _),
            "no genuine challenge -> no emergency loaner");
        game.Parts.Single(x=>x.Id==Core).Hp=0;
        Assert(!ApprovedEconomyPolicyV7.TryReserveOneShotLoaner(game,
            Facts(game),Permit,out _,out _),
            "broken Core denies borrowed vehicle even with fake HUD functional");
        game.Parts.Single(x=>x.Id==Core).Hp=1;
        Assert(ApprovedEconomyPolicyV7.TryReserveOneShotLoaner(game,
            Facts(game),Permit,out var reserved,out _),"single loaner reserves");
        Assert(ApprovedEconomyPolicyV7.TryConsumeLoanerOnTrustedChallengeEntry(reserved,
            "loaner-encounter-1",Permit,out var entered,out _),
            "single loaner entered");
        Assert(ApprovedEconomyPolicyV7.TryStageTrustedLoss(entered,
            "loaner-encounter-1",out var lost,out _),"loss consumes opportunity");
        Assert(lost.Wins==0&&lost.Loaner.Phase==LoanerPhaseV7.Spent&&
            lost.Loaner.EverEntered&&lost.Scrap==0&&
            lost.Parts.Single(p=>p.Id==Drive).Hp==0,
            "losing cannot mint money, heal or reset the one shot");
        Assert(!ApprovedEconomyPolicyV7.TryReserveOneShotLoaner(lost,
            Facts(lost),Permit,out _,out _),"cannot rearm loaner after loss");
        Console.WriteLine("LOSS_SOURCE_PASS zero_wallet=true win=0 loaner_spent=true"+
            " mandatory_game_over_or_other_recovery_design=true");
    }
    static int Main(string[] args)
    {
        try
        {
            if(args.Length!=2)return 2;
            if(args[0]=="--cold")
            {
                Assert(CanonicalEconomySectionV7.TryDecode(File.ReadAllBytes(args[1]),
                    out var persisted,out var reason),"new independent CLR decoder "+reason);
                Assert(persisted.Wins==24&&persisted.Pending.Count==3&&
                    persisted.ConsumedVictoryIds.Count==24&&
                    persisted.FreeUsedForWin==24&&persisted.Scrap==4544,
                    "real file reopens original V7 wallet+IDs after process restart");
                Assert(persisted.Pending.Count(p=>p.Free)==1&&
                    persisted.Pending.Count(p=>p.ChargedScrap==0&&!p.Free)==1,
                    "persisted coupon not conflated with zero priced PAID ticket");
                Console.WriteLine("W27_COLD_SOURCE_PASS checks="+checks+
                    " separate_CRL_process=true original_797_NOT_EXECUTED=true");
                return 0;
            }
            if(args[0]!="--scenario")return 2;
            PriceBoundaries();
            var campaign=Campaign(24);
            var finalState=Roundtrip(campaign);
            Assert(finalState.Pending.Count==3&&finalState.Wins==24,
                "postcampaign serialization remains PENDING");
            File.WriteAllBytes(args[1],Encode(finalState));
            ZeroWalletLoanerWonSoftlock();
            LoanerDefeatAndOptionalDamage();
            Console.WriteLine("W27_ORIGINAL_V7_REGRESSION_PASS checks="+checks+
                " unity_g1=NOT_RUN game_playtest_participants=0"+
                " original_save_797_NOT_EXECUTED=true");
            return 0;
        }
        catch(Exception e){Console.Error.WriteLine(e);return 1;}
    }
    static byte[] Encode(ApprovedEconomySnapshotV7 s)
    {
        if(!CanonicalEconomySectionV7.TryEncode(s,out var data,out var reason))
            throw new InvalidOperationException(reason);
        return data;
    }
}
