// Original v2 C#9 compiled under real .NET8; explicit surrogate durable R3 owner, NOT Unity/PhysX.
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using RoboRougelike.B01.Economy.RebuildV2;
using System.Threading;

sealed class SingleFileOwnerStore : IAtomicRebuildOwnerStoreV2
{
    readonly string file;
    static readonly JsonSerializerOptions Json=new JsonSerializerOptions {IncludeFields=true,WriteIndented=true};
    public bool LoseAckAfterNextCommit, FailBeforeNextCommit;
    public SingleFileOwnerStore(string file){this.file=file;}
    public static void Init(string file,string owner,string run,params string[] parts)
    {
        var s=new OwnerRebuildStateV2{OwnerId=owner,RunId=run,Scrap=1000};
        foreach(var id in parts)s.Parts.Add(new OwnedPartV2 {
            Id=id,Kind="Wheel",Health=0,Purchase=100,PaidQuote=70
        });
        File.WriteAllText(file,JsonSerializer.Serialize(s,Json));
    }
    static OwnerRebuildStateV2 Load(string file) =>
        JsonSerializer.Deserialize<OwnerRebuildStateV2>(File.ReadAllText(file),Json);
    public bool TryRead(string owner,string run,out OwnerRebuildStateV2 state,out string reason)
    {
        try
        {
            state=Load(file);
            if(state.OwnerId!=owner||state.RunId!=run)
            {state=null;reason="FOREIGN_OWNER_RUN";return false;}
            reason="OK";return true;
        }
        catch(Exception){state=null;reason="UNREADABLE_DURABLE_SLOT";return false;}
    }
    public PublishResultV2 TryPublish(string owner,string run,long expected,
        OwnerRebuildStateV2 next,out string reason)
    {
        // FileShare.None is a real cross-process exclusive owner CAS fence.
        // This reference only stages ONE test file; production R3+save integration is absent.
        try
        {
            using(var lease=new FileStream(file+".lease",FileMode.OpenOrCreate,
                FileAccess.ReadWrite,FileShare.None))
            {
                var old=Load(file);
                if(old.OwnerId!=owner||old.RunId!=run)
                {reason="FOREIGN_OWNER_RUN";return PublishResultV2.RejectedNoWrite;}
                if(old.Generation!=expected)
                {reason="STALE_GENERATION";return PublishResultV2.Conflict;}
                if(FailBeforeNextCommit)
                {FailBeforeNextCommit=false;reason="BEFORE_WRITE";return PublishResultV2.RejectedNoWrite;}
                var tmp=file+"."+Guid.NewGuid().ToString("N")+".tmp";
                try
                {
                    using(var output=new FileStream(tmp,FileMode.CreateNew,FileAccess.Write,FileShare.None))
                    using(var writer=new StreamWriter(output))
                    {
                        writer.Write(JsonSerializer.Serialize(next,Json));
                        writer.Flush();output.Flush(true);
                    }
                    File.Move(tmp,file,true); // one-file atomic name swap; NOT whole game G2
                }
                finally {if(File.Exists(tmp))File.Delete(tmp);}
                if(LoseAckAfterNextCommit)
                {LoseAckAfterNextCommit=false;reason="AFTER_DURABLE_WRITE";return PublishResultV2.Uncertain;}
                reason="OK";return PublishResultV2.Committed;
            }
        }
        catch(IOException){reason="IO_UNKNOWN";return PublishResultV2.Uncertain;}
    }
}

static class Program
{
    const string Owner="owner-alpha", Run="run-01";
    const string A="aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    const string B="bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
    const string C="cccccccccccccccccccccccccccccccc";
    static int checks;
    static void Yes(bool ok,string message)
    {if(!ok)throw new Exception("FAILED "+message);checks++;}
    static OwnerRebuildStateV2 State(RebuildEconomyAuthorityV2 s)
    {Yes(s.TryReadAuthoritative(out var x,out var why),"read "+why);return x;}
    static void Scenario(string file)
    {
        SingleFileOwnerStore.Init(file,Owner,Run,A,B,C);
        var disk=new SingleFileOwnerStore(file);
        var first=new RebuildEconomyAuthorityV2(disk,Owner,Run);
        var second=new RebuildEconomyAuthorityV2(disk,Owner,Run);
        var stale=State(first);
        Yes(first.TryReserveDestroyed(A,out var why),"first free "+why);
        Yes(second.TryReserveDestroyed(B,out why),"second paid "+why);
        var q=State(first);
        Yes(q.Scrap==930&&q.Pending.Count==2&&q.Pending.Count(x=>x.WasFree)==1,
            "ONE owner-global free, second pays 70");
        Yes(q.Parts.Single(x=>x.Id==A).Health==0&&
            q.Parts.Single(x=>x.Id==B).Health==0,"no early repair");
        Yes(!second.TryAuthorizeResume(stale,out why)&&
            why=="STALE_OR_FORKED_REBUILD_CHECKPOINT","reject stale save snapshot");
        var fork=q.Copy();fork.Scrap++;
        Yes(!first.TryAuthorizeResume(fork,out why)&&
            why=="STALE_OR_FORKED_REBUILD_CHECKPOINT","reject same-generation fork");
        Yes(first.TryAuthorizeResume(q,out why),"current owner snapshot authorized");
        Yes(!second.TryFinishChallenge("win-1",false,out why)&&
            why=="NOT_AUTHENTICATED_VICTORY","loss cannot advance");
        // An authoritative later shop repricing must NOT mutate a previously paid quote.
        var quoted=State(first);
        var revised=quoted.Copy();
        revised.Parts.Single(x=>x.Id==B).Purchase=130;
        revised.Parts.Single(x=>x.Id==B).PaidQuote=90;
        revised.Generation++;
        Yes(disk.TryPublish(Owner,Run,quoted.Generation,revised,out why)==PublishResultV2.Committed,
            "atomic host repricing "+why);
        var repriced=State(second);
        Yes(repriced.Pending.Single(x=>x.PartId==B).Charged==70&&repriced.Scrap==930,
            "accepted quote frozen despite current price change");
        Yes(first.TryFinishChallenge("win-1",true,out why),"authenticated victory "+why);
        var won=State(second);
        Yes(won.Wins==1&&won.AppliedVictoryIds.SequenceEqual(new[]{"win-1"})&&
            won.Pending.Count==0&&won.Scrap==930&&
            won.Parts.Single(x=>x.Id==A).Health==1&&
            won.Parts.Single(x=>x.Id==B).Health==1,
            "two physical IDs healed once, one event recorded");
        Yes(!second.TryFinishChallenge("win-1",true,out why)&&
            why=="VICTORY_ID_REPLAY","distinct service rejects victory replay");
        Yes(second.TryReserveDestroyed(C,out why),"free slot refreshed only after win");
        var after=State(first);
        Yes(after.Pending.Count==1&&after.Pending[0].WasFree&&after.Scrap==930,
            "next epoch exactly one free");
        Yes(!first.TryReserveDestroyed(C,out why)&&why=="ALREADY_REBUILDING",
            "duplicate part blocked");
        var foreign=new RebuildEconomyAuthorityV2(disk,"another-owner",Run);
        Yes(!foreign.TryReadAuthoritative(out _,out why),"foreign owner refused");
        var wrongRun=new RebuildEconomyAuthorityV2(disk,Owner,"run-02");
        Yes(!wrongRun.TryFinishChallenge("win-2",true,out why),"foreign run refused");
        Yes(!first.TryAuthorizeResume(won,out why),"old before-new-queue stale");
        Console.WriteLine("PASS V2 two-service owner quota / victory replay / generations / fork / run / loss / physical IDs");
    }
    static void Faults(string dir)
    {
        var p=Path.Combine(dir,"lostack.json");
        SingleFileOwnerStore.Init(p,Owner,Run,A,B);
        var disk=new SingleFileOwnerStore(p){LoseAckAfterNextCommit=true};
        var q=new RebuildEconomyAuthorityV2(disk,Owner,Run);
        Yes(!q.TryReserveDestroyed(A,out var why)&&why=="AMBIGUOUS_COMMIT_QUARANTINE",
            "lost ACK fails closed");
        Yes(q.IsQuarantined,"sticky quarantine");
        Yes(!q.TryReserveDestroyed(B,out why)&&why=="OWNER_QUARANTINED","no stale write");
        var fresh=new RebuildEconomyAuthorityV2(new SingleFileOwnerStore(p),Owner,Run);
        var current=State(fresh);
        Yes(current.Generation==1&&current.Pending.Count==1&&current.Scrap==1000,
            "real committed free reservation retained after lost ACK");
        var p2=Path.Combine(dir,"reject.json");
        SingleFileOwnerStore.Init(p2,Owner,Run,A,B);
        var disk2=new SingleFileOwnerStore(p2){FailBeforeNextCommit=true};
        var q2=new RebuildEconomyAuthorityV2(disk2,Owner,Run);
        Yes(!q2.TryReserveDestroyed(A,out why)&&why=="BEFORE_WRITE"&&
            !q2.IsQuarantined,"known no-write rejection not quarantine");
        Yes(State(q2).Generation==0,"no write on reject");
        Yes(q2.TryReserveDestroyed(A,out why),"safe retry after never committed");
        Console.WriteLine("PASS V2 true OS file commit ACK loss & no-write negative control");
    }
    static void Simulation()
    {
        // MODEL only: 12 wins x two destroyed parts per epoch; same 70 quote.
        int repairs=24,epochs=12;
        int freePerWin=epochs,freePerRun=1;
        int perWinCost=(repairs-freePerWin)*70;
        int perRunCost=(repairs-freePerRun)*70;
        Yes(perWinCost==840&&perRunCost==1610,"transparent hypothesis arithmetic");
        Console.WriteLine("BALANCE_MODEL_ONLY {repairs:24,wins:12,per_win_free:12,per_win_paid_scrap:"+
            perWinCost+",per_run_free:1,per_run_paid_scrap:"+perRunCost+
            ",delta_scrap:"+(perRunCost-perWinCost)+",human_playtests:NOT_RUN}");
    }
    static int Main(string[] args)
    {
        if(args.Length<2){Console.Error.WriteLine("usage: <file> --scenario|--verify");return 2;}
        try
        {
            if(args[1]=="--scenario")
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[0])));
                Scenario(args[0]);Faults(Path.GetDirectoryName(Path.GetFullPath(args[0])));
                Simulation();
            }
            else if(args[1]=="--verify")
            {
                var next=new RebuildEconomyAuthorityV2(new SingleFileOwnerStore(args[0]),Owner,Run);
                var s=State(next);
                Yes(s.Wins==1&&s.AppliedVictoryIds.Contains("win-1")&&
                    s.Pending.Count==1&&s.Scrap==930,"cross-process single physical slot");
                Yes(!next.TryFinishChallenge("win-1",true,out var why)&&
                    why=="VICTORY_ID_REPLAY","cross-process replay rejected");
                Yes(!next.TryReserveDestroyed(C,out why)&&why=="ALREADY_REBUILDING",
                    "cross-process reservation held");
                Console.WriteLine("PASS V2 cross-process actual file restore/replay checks");
            }
            else throw new Exception("UNSUPPORTED_MODE");
            Console.WriteLine("SOURCE_CSHARP_TESTS_PASS checks="+checks+" NATIVE_UNITY_NOT_RUN LIVE_R3_NOT_TESTED");
            return 0;
        }
        catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
    }
}
