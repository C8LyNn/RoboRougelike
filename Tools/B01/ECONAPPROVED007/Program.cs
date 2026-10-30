// W24 ORIGINAL ECON APPPROVED V7 C#9 source test; disposable physical
// OS owner-lock+JSON envelope is TEST-ONLY and NOT installed in Unity game.
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using RoboRougelike.B01.Economy.ApprovedV7;

sealed class PhysicalTestOwner : IDisposable
{
    readonly string path;
    readonly FileStream lease;
    static readonly JsonSerializerOptions Json=new JsonSerializerOptions{IncludeFields=true};
    readonly HashSet<string> genuineWins=new HashSet<string>(StringComparer.Ordinal);
    public bool AckLostAfterWrite,Quarantined;
    public PhysicalTestOwner(string root)
    {
        path=root+".json";
        lease=new FileStream(root+".owner",FileMode.OpenOrCreate,
            FileAccess.ReadWrite,FileShare.None);
    }
    public static void Initialize(string root)
    {
        var s=new ApprovedEconomySnapshotV7 {
            OwnerId="owner",RunId="run",Scrap=400,
            Parts=new List<OwnedModuleV7>{
              new OwnedModuleV7{Id=Tests.A,Category="Core",Hp=1,PurchaseScrap=100},
              new OwnedModuleV7{Id=Tests.B,Category="Wheel",Hp=0,PurchaseScrap=100},
              new OwnedModuleV7{Id=Tests.C,Category="Gun",Hp=0,PurchaseScrap=100},
              new OwnedModuleV7{Id=Tests.D,Category="Frame",Hp=1,PurchaseScrap=1}
            }
        };
        File.WriteAllText(root+".json",JsonSerializer.Serialize(s,Json));
    }
    public ApprovedEconomySnapshotV7 Read()
        =>JsonSerializer.Deserialize<ApprovedEconomySnapshotV7>(File.ReadAllText(path),Json);
    public void Commit(ApprovedEconomySnapshotV7 old,ApprovedEconomySnapshotV7 next)
    {
        if(Quarantined)throw new Exception("QUARANTINED");
        var persisted=Read();
        if(persisted.Generation!=old.Generation)
            throw new Exception("CAS_GENERATION_CONFLICT");
        if(!ApprovedEconomyPolicyV7.Valid(next)||next.Generation!=old.Generation+1)
            throw new Exception("INVALID_STAGED_ECONOMY");
        var tmp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try
        {
            using(var fs=new FileStream(tmp,FileMode.CreateNew,FileAccess.Write,FileShare.None))
            using(var sw=new StreamWriter(fs))
            {
                sw.Write(JsonSerializer.Serialize(next,Json));sw.Flush();fs.Flush(true);
            }
            File.Move(tmp,path,true);
            if(AckLostAfterWrite)
            {AckLostAfterWrite=false;Quarantined=true;throw new Exception("LOST_COMMIT_ACK");}
        }
        finally {if(File.Exists(tmp))File.Delete(tmp);}
    }
    public bool TryWin(string id,string encounter,out string reason)
    {
        if(Quarantined){reason="QUARANTINED";return false;}
        if(!genuineWins.Contains(id))
        {reason="TRUSTED_COMBAT_RECEIPT_NOT_ISSUED";return false;}
        var old=Read();
        if(!ApprovedEconomyPolicyV7.TryStageTrustedVictory(old,id,encounter,out var next,out reason))
            return false;
        // This TEST-only journal and the durable owner share one single lock.
        // Real #749 must consume the encounter authority receipt in its Save.
        Commit(old,next);genuineWins.Remove(id);reason="OK";return true;
    }
    public void CombatOnlyIssueGenuineWin(string id)
    {
        if(!genuineWins.Add(id))throw new Exception("duplicate trusted source");
    }
    public void Dispose(){lease.Dispose();}
}

static class Tests
{
    public const string A="aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    public const string B="bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
    public const string C="cccccccccccccccccccccccccccccccc";
    public const string D="dddddddddddddddddddddddddddddddd";
    public const string P="eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee";
    static int checks;
    static void Ok(bool x,string what){if(!x)throw new Exception("ASSERT_FAILED: "+what);checks++;}
    static ApprovedEconomySnapshotV7 Reserve(ApprovedEconomySnapshotV7 s,string id)
    {
        bool good=ApprovedEconomyPolicyV7.TryReserveDestroyed(s,id,out var n,out var reason);
        Ok(good,"reserve "+id+" "+reason);return n;
    }
    static ApprovedEconomySnapshotV7 Win(ApprovedEconomySnapshotV7 s,string victory,string encounter)
    {
        bool good=ApprovedEconomyPolicyV7.TryStageTrustedVictory(s,victory,encounter,
            out var n,out var why);
        Ok(good,"stage genuine victory "+why);return n;
    }
    static ApprovedEconomySnapshotV7 Make()
    {
        return new ApprovedEconomySnapshotV7{
            OwnerId="owner",RunId="run",Scrap=400,
            Parts=new List<OwnedModuleV7>{
                new OwnedModuleV7{Id=A,Category="Core",Hp=1,PurchaseScrap=100},
                new OwnedModuleV7{Id=B,Category="Wheel",Hp=0,PurchaseScrap=100},
                new OwnedModuleV7{Id=C,Category="Weapon",Hp=0,PurchaseScrap=100}
            }
        };
    }
    static CanonicalDeploymentFactsV7 Blocked(ApprovedEconomySnapshotV7 s)=>
        new CanonicalDeploymentFactsV7 {
            OwnerId=s.OwnerId,RunId=s.RunId,Generation=s.Generation,
            CoreId=A,CoreFunctional=true,CanDeploy=false,
            HasGenuineNextChallenge=true,EncounterId="encounter-2",
            EssentialDestroyedPhysicalIds=new[]{B}
        };

    static void Pricing()
    {
        foreach(var pair in new[]{(1,0),(5,3),(15,9),(30,19),(100,65),
                                  (int.MaxValue,1395864370)})
        {
            Ok(ApprovedEconomyPolicyV7.TryPaidQuote(pair.Item1,out var quote)&&
               quote==pair.Item2,"approved 65% integer floor boundary "+pair.Item1);
        }
        Ok(!ApprovedEconomyPolicyV7.TryPaidQuote(0,out _),"zero purchase fails");
        Ok(!ApprovedEconomyPolicyV7.TryPaidQuote(-1,out _),"negative purchase fails");
        Ok(ApprovedEconomyPolicyV7.TryPaidQuote(1,out var zeroPaid)&&
           zeroPaid==0&&zeroPaid<1,"1 Scrap new part paid quote zero < purchase");
        Ok(ApprovedEconomyPolicyV7.TryPaidQuote(int.MaxValue,out var maxPaid)&&
           maxPaid<int.MaxValue,"maximum purchase cannot overflow or equal new");

        Ok(ApprovedEconomyPolicyV7.LoanerMaximumHealthPercent<100&&
           ApprovedEconomyPolicyV7.LoanerMaximumDamagePercent<100&&
           ApprovedEconomyPolicyV7.LoanerMaximumDrivePercent<100&&
           !ApprovedEconomyPolicyV7.LoanerMayTransferParts&&
           !ApprovedEconomyPolicyV7.LoanerMayPersistEquipment,
           "loaner is deliberately weaker and cannot transfer inventory");
    }
    static void Economy()
    {
        var init=Make();
        Ok(ApprovedEconomyPolicyV7.Valid(init),"new policy state valid");
        Ok(init.Wins==0&&init.FreeUsedForWin==0,"no bootstrap free coupon");
        var first=Reserve(init,B);
        Ok(first.Pending.Count==1&&!first.Pending[0].Free&&
           first.Pending[0].ChargedScrap==65&&first.Scrap==335&&
           first.Parts.Single(x=>x.Id==B).Hp==0,
           "paid 65% before first real win and stays destroyed");
        Ok(init.Generation==0&&init.Scrap==400,"source detached, no prior mutation");
        Ok(!ApprovedEconomyPolicyV7.TryReserveDestroyed(first,B,out _,out var why)&&
           why=="ALREADY_RESERVED","same physical GUID cannot reserve twice");
        var cheap=first.Copy();
        cheap.Parts.Add(new OwnedModuleV7{Id=D,Category="Tiny frame",
            Hp=0,PurchaseScrap=1});
        var zeroCharged=Reserve(cheap,D);
        Ok(zeroCharged.Pending.Count==2&&
           zeroCharged.Pending.Single(x=>x.ModuleId==D).ChargedScrap==0&&
           !zeroCharged.Pending.Single(x=>x.ModuleId==D).Free&&
           zeroCharged.FreeUsedForWin==0&&
           zeroCharged.Scrap==335,
           "zero Scrap PAID order is distinct from free credit and spends no coupon");
        Ok(ApprovedEconomyPolicyV7.Valid(zeroCharged),
           "mixed paid 65% + zero paid journal roundtrip validation");

        var won=Win(first,"realwin-01","encounter-01");
        Ok(won.Wins==1&&won.Pending.Count==0&&won.Parts.Single(x=>x.Id==B).Hp==1 &&
           won.Scrap==335 && won.FreeUsedForWin==0,
           "only subsequent real win heals reserved item and mints a free quota");
        Ok(!ApprovedEconomyPolicyV7.TryStageTrustedVictory(won,"realwin-01",
           "encounter-01",out _,out why)&&why=="VICTORY_ID_REPLAY",
           "already-consumed combat event ID fails");
        var free=Reserve(won,C);
        Ok(free.Scrap==335&&free.FreeUsedForWin==1&&free.Pending.Single().Free&&
           free.Parts.Single(x=>x.Id==C).Hp==0,
           "one real-win free coupon used to reserve, NOT instant heal");
        var additional=free.Copy();
        additional.Parts.Add(new OwnedModuleV7{
            Id=D,Category="Cheap frame",Hp=0,PurchaseScrap=5
        });
        var paid=Reserve(additional,D);
        Ok(paid.Scrap==332&&!paid.Pending.Single(t=>t.ModuleId==D).Free&&
           paid.Pending.Single(t=>t.ModuleId==D).ChargedScrap==3,
           "second same-epoch queue exactly paid 65% price5");
        Ok(!ApprovedEconomyPolicyV7.TryReserveDestroyed(paid,A,out _,out why)&&
           why=="MODULE_NOT_DESTROYED","cannot queue healthy core");
        Ok(!ApprovedEconomyPolicyV7.TryReserveDestroyed(paid,P,out _,out why)&&
           why=="FOREIGN_MODULE","not owned physical ID refused");
        var won2=Win(paid,"realwin-02","encounter-02");
        Ok(won2.Wins==2&&won2.Pending.Count==0&&won2.Scrap==332&&
           won2.Parts.Single(x=>x.Id==C).Hp==1&&
           won2.Parts.Single(x=>x.Id==D).Hp==1,
           "new distinct win restores free and paid without swapping IDs");
        Ok(ApprovedEconomyPolicyV7.Valid(won2),"fully settled owner still valid");
        Ok(!ApprovedEconomyPolicyV7.TryStageTrustedVictory(won2,"realwin-02",
           "encounter-02",out _,out why)&&why=="VICTORY_ID_REPLAY",
           "double event same win rejected");
        var broke=won2.Copy();broke.Parts.Add(broke.Parts[0].Copy());
        Ok(!ApprovedEconomyPolicyV7.Valid(broke),"duplicate stored physical ID invalid");
        var foreign=won2.Copy();foreign.Pending.Add(new RebuildTicketV7 {
            ModuleId=P,AfterWins=2,PurchaseScrap=100,ChargedScrap=0,Free=true,
            ReservationId="fake"
        });
        Ok(!ApprovedEconomyPolicyV7.Valid(foreign),"foreign ticket invalid");
        var noMoney=Make();noMoney.Scrap=50;
        Ok(!ApprovedEconomyPolicyV7.TryReserveDestroyed(noMoney,B,out _,out why)&&
           why=="INSUFFICIENT_SCRAP","fail closed before first win when 65 unavailable");
    }
    static void Loaner()
    {
        var s=Make();
        var eligible=Blocked(s);
        Ok(ApprovedEconomyPolicyV7.TryReserveOneShotLoaner(s,eligible,P,out var reserved,
            out var why),"one loaner reserved "+why);
        Ok(reserved.Loaner.Phase==LoanerPhaseV7.Reserved&&
           !reserved.Loaner.EverEntered&&s.Loaner.Phase==LoanerPhaseV7.None,
           "loaner reservation persisted in proposed SAME owner snapshot");
        Ok(!ApprovedEconomyPolicyV7.TryReserveOneShotLoaner(reserved,
            Blocked(reserved),D,out _,out why)&&
            why=="ONE_SHOT_LOANER_ALREADY_RESERVED_OR_SPENT",
            "cannot reserve another borrowed robot");
        Ok(!ApprovedEconomyPolicyV7.TryConsumeLoanerOnTrustedChallengeEntry(
            reserved,"wrong-encounter",P,out _,out why),
            "wrong challenge cannot enter borrowed robot");
        Ok(!ApprovedEconomyPolicyV7.TryConsumeLoanerOnTrustedChallengeEntry(
            reserved,eligible.EncounterId,D,out _,out why),
            "different permit cannot enter");
        Ok(ApprovedEconomyPolicyV7.TryConsumeLoanerOnTrustedChallengeEntry(
            reserved,eligible.EncounterId,P,out var entered,out why),
            "trusted next-challenge entry "+why);
        Ok(entered.Loaner.Phase==LoanerPhaseV7.Entered&&entered.Loaner.EverEntered,
            "consumed one lifetime token at actual entry");
        Ok(!ApprovedEconomyPolicyV7.TryConsumeLoanerOnTrustedChallengeEntry(
            entered,eligible.EncounterId,P,out _,out why),
            "cannot reenter same borrowed craft");
        Ok(!ApprovedEconomyPolicyV7.TryStageTrustedVictory(entered,"win-wrong",
            "another-challenge",out _,out why)&&why=="WRONG_LOANER_ENCOUNTER",
            "win cannot be claimed by other encounter while loaner active");
        var lost=ApprovedEconomyPolicyV7.TryStageTrustedLoss(entered,
            eligible.EncounterId,out var afterLoss,out why);
        Ok(lost&&afterLoss.Loaner.Phase==LoanerPhaseV7.Spent&&
           afterLoss.Wins==0&&afterLoss.Scrap==400&&
           afterLoss.Parts.Single(p=>p.Id==B).Hp==0,
           "actual loss spends loaner without win, rebuild or wallet credit");
        Ok(!ApprovedEconomyPolicyV7.TryReserveOneShotLoaner(afterLoss,
            Blocked(afterLoss),D,out _,out why),
            "loaner one use after losing");
        Ok(ApprovedEconomyPolicyV7.TryStageTrustedVictory(entered,
            "true-loaner-win",eligible.EncounterId,out var won,out why),
            "real win settles one borrowed encounter "+why);
        Ok(won.Loaner.Phase==LoanerPhaseV7.Spent&&won.Wins==1&&won.Scrap==400,
            "genuine borrowed victory credits exactly one normal win, no bonus wallet");
        Ok(!ApprovedEconomyPolicyV7.TryReserveOneShotLoaner(won,
            Blocked(won),D,out _,out why),"no borrowed robot after prior victory");

        foreach(var label in new[]{"dead_core","no_encounter","deployable",
                                      "missing_essential","foreign_essential",
                                      "stale","foreign_run","false_core"})
        {
            var bad=Make();var f=Blocked(bad);
            if(label=="dead_core")bad.Parts.Single(x=>x.Id==A).Hp=0;
            if(label=="no_encounter")f.HasGenuineNextChallenge=false;
            if(label=="deployable")f.CanDeploy=true;
            if(label=="missing_essential")f.EssentialDestroyedPhysicalIds=Array.Empty<string>();
            if(label=="foreign_essential")f.EssentialDestroyedPhysicalIds=new[]{P};
            if(label=="stale")f.Generation++;
            if(label=="foreign_run")f.RunId="intruder";
            if(label=="false_core")f.CoreFunctional=false;
            Ok(!ApprovedEconomyPolicyV7.TryReserveOneShotLoaner(bad,f,P,
                out _,out _),"approved blocker "+label);
        }
        // Optional broken module never blocks if canonical CanDeploy=true.
        var optional=Blocked(Make());optional.CanDeploy=true;
        Ok(!ApprovedEconomyPolicyV7.TryReserveOneShotLoaner(Make(),
            optional,P,out _,out _),"optional 0HP not blocked");
        var invalid=entered.Copy();
        invalid.Loaner.EverEntered=false;
        Ok(!ApprovedEconomyPolicyV7.Valid(invalid),"tampered loaner spent state fails");
    }

    static void Durable(string root)
    {
        PhysicalTestOwner.Initialize(root);
        using(var host=new PhysicalTestOwner(root))
        {
            var old=host.Read();
            var candidate=Reserve(old,B);
            host.Commit(old,candidate);
            Ok(host.Read().Scrap==335&&host.Read().Parts.Single(x=>x.Id==B).Hp==0,
                "real persisted OS JSON staged 65 price remains 0HP");
            host.CombatOnlyIssueGenuineWin("host-win-01");
            Ok(!host.TryWin("fake-win","enc-1",out var reason)&&
               reason=="TRUSTED_COMBAT_RECEIPT_NOT_ISSUED",
               "fake win cannot enter test-only trusted combat journal");
            Ok(host.TryWin("host-win-01","enc-1",out reason),"real journal win consumed");
            Ok(host.Read().Wins==1&&host.Read().Parts.Single(x=>x.Id==B).Hp==1,
               "real source reducer state survived durable file rename");
            var f=Blocked(host.Read());
            Ok(ApprovedEconomyPolicyV7.TryReserveOneShotLoaner(host.Read(),
               f,P,out _,out reason)==false,
               "no loaner when source says host physically ready only after repair");
            // Host is the sole owner. A second physical lease is denied.
            bool rejected=false;
            try{using(var another=new PhysicalTestOwner(root)){}}
            catch(IOException){rejected=true;}
            Ok(rejected,"single physical test owner lock excludes concurrent second");
            host.AckLostAfterWrite=true;
            var again=host.Read();
            again.Parts.Single(x=>x.Id==C).Hp=0;
            var proposed=Reserve(again,C);
            bool threw=false;
            try {host.Commit(again,proposed);}catch(Exception e)
            {threw=e.Message=="LOST_COMMIT_ACK";}
            Ok(threw&&host.Quarantined,
                "persisted commit before lost ACK quarantines test host");
            Ok(host.Read().Generation==proposed.Generation,
                "post-ACK-loss owner disk has new generation, no blind retry");
        }
        using(var cold=new PhysicalTestOwner(root))
        {
            Ok(cold.Read().Wins==1&&cold.Read().Scrap==335&&
               cold.Read().ConsumedVictoryIds.SequenceEqual(new[]{"host-win-01"}),
               "cold new owner lease recovers genuine ID and stable 65 wallet");
        }
        Console.WriteLine("PASS original source using physical test-only owner FileShare.None, durable JSON and ACK-loss quarantine");
    }
    static void Balance()
    {
        int wins=12,orders=wins*2,complete=(wins-1)*2;
        int quote=65;
        Ok(orders==24&&complete==22&&orders-complete==2,
           "two post-win destroyed pending after final win");
        Ok((orders-wins)*quote==780&&(orders-1)*quote==1495,
           "new approved 65% arithmetic 780 vs 1495, replaces W23 840/1610");
        Console.WriteLine("MODEL_ONLY wins=12 orders=24 completed=22 pending=2 paid_quote=65 spend_per_win=780 spend_per_run=1495 human_trials=0");
    }
    static int Main(string[] args)
    {
        if(args.Length!=2)return 2;
        try{
            if(args[0]=="--scenario")
            {
                Pricing();Economy();Loaner();Durable(args[1]);Balance();
                SectionChecks.Run();
            }
            else if(args[0]=="--cold")
            {
                using(var cold=new PhysicalTestOwner(args[1]))
                {
                    var s=cold.Read();
                    Ok(s.Wins==1&&s.ConsumedVictoryIds.Contains("host-win-01"),
                       "another completely new OS process sees one real win");
                    Ok(s.Scrap==335&&s.Pending.Count==1&&
                       s.Pending[0].ChargedScrap==0&&
                       s.Parts.Single(x=>x.Id==C).Hp==0,
                       "free voucher reservation is durable, not instantly healed");
                }
            }
            else if(args[0]=="--hold")
            {
                using(var active=new PhysicalTestOwner(args[1]))
                {
                    File.WriteAllText(args[1]+".ready","ONE_TEST_OWNER_HELD");
                    System.Threading.Thread.Sleep(4500);
                    File.Delete(args[1]+".ready");
                    Ok(true,"held test-owner OS lock for external competing process");
                }
            }
            else if(args[0]=="--contend")
            {
                bool refused=false;
                try{using(var other=new PhysicalTestOwner(args[1])){}}
                catch(IOException){refused=true;}
                Ok(refused,"outside second CLR process cannot grab same test owner");
            }
            else return 2;
            Console.WriteLine("ECON_V7_APPROVED_SOURCE_PASS checks="+checks+
                " real_Unity=NOT_RUN selected_749_wiring=NOT_WIRED");
            return 0;
        }
        catch(Exception e){Console.Error.WriteLine(e);return 1;}
    }
}
