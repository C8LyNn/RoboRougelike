// B01-22 W21 real .NET8/C#9 ORIGINAL V3 runtime under synthetic single-slot OS source harness.
// Test-only combat authority issues trusted winning receipts; not Unity/PhysX/player gameplay.
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using RoboRougelike.B01.Economy.RebuildV3;

sealed class TestEnvelope
{
    public OwnerRebuildStateV3 State;
    public List<string> TrustedUnconsumedVictories=new List<string>();
}
sealed class TestDiskHost : IAtomicRebuildOwnerStoreV3
{
    readonly string file;
    static readonly JsonSerializerOptions Opt=new JsonSerializerOptions{IncludeFields=true,WriteIndented=true};
    public bool LostCommitAck, RejectBeforeWrite;
    public TestDiskHost(string path){file=path;}
    static TestEnvelope Load(string path)=>JsonSerializer.Deserialize<TestEnvelope>(File.ReadAllText(path),Opt);
    static void Write(string path,TestEnvelope value)
    {
        string tmp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try
        {
            using(var stream=new FileStream(tmp,FileMode.CreateNew,FileAccess.Write,FileShare.None))
            using(var writer=new StreamWriter(stream))
            {
                writer.Write(JsonSerializer.Serialize(value,Opt));
                writer.Flush();stream.Flush(true);
            }
            File.Move(tmp,path,true); // one-file test proof; NOT power-loss directory durability G2
        }
        finally {if(File.Exists(tmp))File.Delete(tmp);}
    }
    FileStream Lock() => new FileStream(file+".lock",FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
    public static void Init(string file,params string[] ids)
    {
        var s=new OwnerRebuildStateV3{OwnerId="owner-A",RunId="run-A",Scrap=350};
        foreach(var id in ids)s.Parts.Add(new OwnedPartV3{
            Id=id,Kind="Wheel",Health=0,Purchase=100,PaidQuote=70
        });
        Write(file,new TestEnvelope{State=s});
    }
    // Simulates an event arriving from trusted COMBAT, not from an economy client.
    public bool IssueVictoryFromTrustedCombatOnly(string id)
    {
        using(var lease=Lock())
        {
            var d=Load(file);
            if(string.IsNullOrWhiteSpace(id)||d.TrustedUnconsumedVictories.Contains(id)||
                d.State.AppliedVictoryIds.Contains(id))return false;
            d.TrustedUnconsumedVictories.Add(id);Write(file,d);return true;
        }
    }
    public bool TryRead(string owner,string run,out OwnerRebuildStateV3 state,out string reason)
    {
        try
        {
            state=Load(file).State.Copy();
            if(state.OwnerId!=owner||state.RunId!=run)
            {state=null;reason="FOREIGN_OWNER_RUN";return false;}
            reason="OK";return true;
        }
        catch(Exception){state=null;reason="FILE_UNAVAILABLE";return false;}
    }
    public PublishResultV3 TryPublish(string owner,string run,long expected,
        OwnerRebuildStateV3 next,string candidateWinId,out string reason)
    {
        try
        {
            using(var lease=Lock())
            {
                var d=Load(file);
                var old=d.State;
                if(old.OwnerId!=owner||old.RunId!=run)
                {reason="FOREIGN_OWNER_RUN";return PublishResultV3.RejectedNoWrite;}
                if(old.Generation!=expected)
                {reason="OWNER_STALE";return PublishResultV3.Conflict;}
                if(RejectBeforeWrite)
                {RejectBeforeWrite=false;reason="KNOWN_NO_WRITE";return PublishResultV3.RejectedNoWrite;}
                if(candidateWinId!=null)
                {
                    if(!d.TrustedUnconsumedVictories.Contains(candidateWinId)||
                       old.AppliedVictoryIds.Contains(candidateWinId)||
                       next.Wins!=old.Wins+1||
                       next.AppliedVictoryIds.Count!=old.AppliedVictoryIds.Count+1||
                       !next.AppliedVictoryIds.Contains(candidateWinId))
                    {reason="UNTRUSTED_OR_USED_WIN";return PublishResultV3.RejectedNoWrite;}
                    d.TrustedUnconsumedVictories.Remove(candidateWinId);
                }
                else if(next.Wins!=old.Wins||
                        !next.AppliedVictoryIds.SequenceEqual(old.AppliedVictoryIds))
                {reason="UNAUTHORIZED_WIN_MUTATION";return PublishResultV3.RejectedNoWrite;}
                d.State=next.Copy();Write(file,d);
                if(LostCommitAck)
                {LostCommitAck=false;reason="COMMITTED_ACK_LOST";return PublishResultV3.Uncertain;}
                reason="OK";return PublishResultV3.Committed;
            }
        }
        catch(IOException){reason="MAYBE_WRITTEN";return PublishResultV3.Uncertain;}
    }
}
static class Program
{
    const string A="aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    const string B="bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
    const string C="cccccccccccccccccccccccccccccccc";
    const string D="dddddddddddddddddddddddddddddddd";
    static int count;
    static void Ok(bool p,string why){if(!p)throw new Exception("ASSERT_FAIL: "+why);count++;}
    static OwnerRebuildStateV3 State(RebuildEconomyAuthorityV3 q)
    {
        bool good=q.TryReadAuthoritative(out var s,out var why);
        Ok(good,"read owner "+why);return s;
    }
    static void MainCase(string file)
    {
        TestDiskHost.Init(file,A,B,C,D);
        var store=new TestDiskHost(file);
        var a=new RebuildEconomyAuthorityV3(store,"owner-A","run-A");
        var b=new RebuildEconomyAuthorityV3(new TestDiskHost(file),"owner-A","run-A");
        Ok(!typeof(RebuildEconomyAuthorityV3).GetMethods().Any(m=>m.Name=="TryFinishChallenge"&&
            m.GetParameters().Any(p=>p.ParameterType==typeof(bool))),
            "cannot pass untrusted authenticatedWin bool");
        Ok(!a.TryFinishChallenge("forged-1",out var err)&&err=="UNTRUSTED_OR_USED_WIN",
            "forged win refused without host issued record");
        var unchanged=State(a);
        Ok(unchanged.Wins==0&&unchanged.Generation==0&&unchanged.Scrap==350&&
            unchanged.AppliedVictoryIds.Count==0,"no fake epoch or money mutation");
        Ok(a.TryReserveDestroyed(A,out err),"first free "+err);
        Ok(b.TryReserveDestroyed(B,out err),"same owner second paid "+err);
        var q=State(a);
        Ok(q.Pending.Count==2&&q.Pending.Count(t=>t.WasFree)==1&&q.Scrap==280,
            "exactly one global free, one 70 paid");
        Ok(q.Parts.Where(p=>p.Id==A||p.Id==B).All(p=>p.Health==0),
            "queued 0HP remains unavailable, not instant heal");
        Ok(!b.TryFinishChallenge("spoofed-win",out err)&&err=="UNTRUSTED_OR_USED_WIN",
            "untrusted victory cannot heal queued 0HP");
        var before=State(b);
        Ok(before.Pending.Count==2&&before.Scrap==280&&before.Wins==0,
            "spoofed winner no side effect");
        Ok(store.IssueVictoryFromTrustedCombatOnly("real-win-01"),"combat won event issued");
        Ok(a.TryFinishChallenge("real-win-01",out err),"real trusted first win "+err);
        var after=State(b);
        Ok(after.Wins==1&&after.Pending.Count==0&&after.Scrap==280&&
            after.Parts.First(p=>p.Id==A).Health==1&&after.Parts.First(p=>p.Id==B).Health==1&&
            after.AppliedVictoryIds.SequenceEqual(new[]{"real-win-01"}),
            "atomic real win batch completes exact IDs once");
        Ok(!b.TryFinishChallenge("real-win-01",out err)&&err=="VICTORY_ID_REPLAY",
            "two-instance replay blocked");
        Ok(!store.IssueVictoryFromTrustedCombatOnly("real-win-01"),
            "trusted combat cannot reissue committed win id");
        Ok(!b.TryAuthorizeResume(q,out err)&&err=="STALE_OR_FORKED_REBUILD_CHECKPOINT",
            "stale pre-win save denied");
        var fork=after.Copy();fork.Scrap+=99;
        Ok(!a.TryAuthorizeResume(fork,out err)&&err=="STALE_OR_FORKED_REBUILD_CHECKPOINT",
            "same-gen different wallet rejected");
        Ok(b.TryReserveDestroyed(C,out err),"free coupon refreshed after real win");
        Ok(a.TryReserveDestroyed(D,out err),"paid after next free");
        var active=State(a);
        Ok(active.Scrap==210&&active.Pending.Count==2&&
            active.Pending.Count(x=>x.WasFree)==1&&active.Wins==1,
            "one coupon in epoch one, paid second");
        Ok(!a.TryFinishChallenge("not-real-win-02",out err)&&err=="UNTRUSTED_OR_USED_WIN",
            "no next win means pending parts still unavailable");
        Ok(State(b).Parts.Where(p=>p.Id==C||p.Id==D).All(p=>p.Health==0),
            "next distinct authenticated win required to heal");
        Ok(store.IssueVictoryFromTrustedCombatOnly("real-win-02"),
            "issue next real win");
        Ok(b.TryFinishChallenge("real-win-02",out err),"next win heals queued "+err);
        var last=State(a);
        Ok(last.Wins==2&&last.Pending.Count==0&&last.Scrap==210&&
            last.Parts.All(p=>p.Health==1),"deferred complete with unique win");
        var wrong=new RebuildEconomyAuthorityV3(store,"owner-B","run-A");
        Ok(!wrong.TryFinishChallenge("real-win-03",out err),"different owner cannot mutate");
        var staleRun=new RebuildEconomyAuthorityV3(store,"owner-A","run-B");
        Ok(!staleRun.TryReadAuthoritative(out _,out err),"different run isolated");
        Console.WriteLine("PASS REAL CSHARP SOURCE V3 genuine-combat-host authority two-services delayed 0HP win replay paid/free");
    }
    static void Faults(string path)
    {
        var p=Path.Combine(path,"lost-ack.json");
        TestDiskHost.Init(p,A,B);
        var store=new TestDiskHost(p){LostCommitAck=true};
        var q=new RebuildEconomyAuthorityV3(store,"owner-A","run-A");
        Ok(!q.TryReserveDestroyed(A,out var why)&&why=="AMBIGUOUS_COMMIT_QUARANTINE",
            "durable reserve ACK loss quarantine");
        Ok(q.IsQuarantined&& !q.TryReserveDestroyed(B,out why)&&
            why=="OWNER_QUARANTINED","sticky old-world denial");
        var recovery=new RebuildEconomyAuthorityV3(new TestDiskHost(p),"owner-A","run-A");
        var state=State(recovery);
        Ok(state.Generation==1&&state.Pending.Count==1&&state.Scrap==350,
            "cold re-read authoritative persisted g1");
        var fake=Path.Combine(path,"lost-victory.json");
        TestDiskHost.Init(fake,A,B);
        var f=new TestDiskHost(fake);
        Ok(f.IssueVictoryFromTrustedCombatOnly("real-fault-win"),"trusted win prep");
        f.LostCommitAck=true;
        var old=new RebuildEconomyAuthorityV3(f,"owner-A","run-A");
        Ok(!old.TryFinishChallenge("real-fault-win",out why)&&
            why=="AMBIGUOUS_COMMIT_QUARANTINE","victory Commit ACK lost quarantined");
        Ok(old.IsQuarantined,"victory service sticky quarantine");
        var cold=new RebuildEconomyAuthorityV3(new TestDiskHost(fake),"owner-A","run-A");
        Ok(State(cold).Wins==1,"win was durably committed");
        Ok(!cold.TryFinishChallenge("real-fault-win",out why)&&
            why=="VICTORY_ID_REPLAY","cold restart cannot double count");
        var noWrite=Path.Combine(path,"known-reject.json");
        TestDiskHost.Init(noWrite,A,B);
        var rejectedStore=new TestDiskHost(noWrite){RejectBeforeWrite=true};
        var retry=new RebuildEconomyAuthorityV3(rejectedStore,"owner-A","run-A");
        Ok(!retry.TryReserveDestroyed(A,out why)&&why=="KNOWN_NO_WRITE"&&
            !retry.IsQuarantined,"known no-write does not quarantine");
        Ok(retry.TryReserveDestroyed(A,out why),"known no-write safe retry");
        Console.WriteLine("PASS REAL CSHARP SOURCE V3 ACK-loss win/reservation/ID + negative known no-write");
    }
    static void Balance()
    {
        // Model-only assumption; two destroyed AFTER every victory means last two still pending.
        const int wins=12, damagedPerWin=2, paidQuote=70;
        const int freeWin=wins, freeRun=1;
        int orders=wins*damagedPerWin;
        int perWin=(orders-freeWin)*paidQuote, perRun=(orders-freeRun)*paidQuote;
        Ok(perWin==840&&perRun==1610&&orders==24,"two cadence toy benchmark");
        Ok((wins-1)*damagedPerWin==22 && orders-22==2,
            "last two parts remain pending after final win");
        Console.WriteLine("BALANCE_MODEL_ONLY wins=12 orders=24 completed=22 pending=2 per_win_spent=840 per_run_spent=1610 human_playtests=NOT_RUN");
    }
    static int Main(string[] args)
    {
        if(args.Length!=2)return 2;
        try
        {
            if(args[0]=="--scenario")
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[1])));
                MainCase(args[1]);Faults(Path.GetDirectoryName(Path.GetFullPath(args[1])));Balance();
            }
            else if(args[0]=="--cold")
            {
                var a=new RebuildEconomyAuthorityV3(new TestDiskHost(args[1]),"owner-A","run-A");
                var s=State(a);
                Ok(s.Wins==2&&s.Pending.Count==0&&s.Scrap==210&&
                    s.AppliedVictoryIds.SequenceEqual(new[]{"real-win-01","real-win-02"}),
                    "independent cold OS process restore");
                Ok(!a.TryFinishChallenge("real-win-02",out var err)&&err=="VICTORY_ID_REPLAY",
                    "cold separate process win dedupe");
                Ok(!a.TryFinishChallenge("forged-after-restart",out err)&&
                    err=="UNTRUSTED_OR_USED_WIN","restart forged win denied");
                Console.WriteLine("PASS cold OS process authority still mandatory");
            }
            else return 2;
            Console.WriteLine("ECON_V3_SOURCE_PASS checks="+count+" real_Unity=NOT_RUN live_R3=NOT_WIRED");
            return 0;
        }
        catch(Exception e){Console.Error.WriteLine(e);return 1;}
    }
}
