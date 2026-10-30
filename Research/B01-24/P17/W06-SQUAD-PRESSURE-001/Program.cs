// B01-24 W06: actual cross-OS executable, original R3 source + original B01-24 C# decision engine.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RoboRougelike.R3;
using RoboRougelike.B0124.SquadPressure;

internal sealed class CaseFile
{
    public string? Schema {get;set;}
    public Scenario[]? AuthoredCases {get;set;}
}
internal sealed class Scenario
{
    public string? Id {get;set;}
    public SquadActor[] Members {get;set;} = Array.Empty<SquadActor>();
    public long[] Ticks {get;set;}=Array.Empty<long>();
}
internal sealed class SquadActor
{
    public int Id {get;set;}
    public string? Kind {get;set;}
    public float Distance {get;set;}
}
internal static class Program
{
    static int passed,failed;
    static void Check(string name,Action run)
    {
        try {run();passed++;Console.WriteLine("PASS "+name);}
        catch(Exception ex){failed++;Console.Error.WriteLine("FAIL "+name+" "+ex.GetType().Name+": "+ex.Message);}
    }
    static void Ensure(bool result,string why){if(!result)throw new InvalidOperationException(why);}
    static void Reject(Action action)
    {
        try {action();} catch(ArgumentException){return;}
        throw new InvalidOperationException("Invalid input was accepted");
    }
    static SquadPressurePolicy Rule()=>SquadPressurePolicy.Standard();
    static SquadPressureScheduler Single(R3EnemyArchetype a=R3EnemyArchetype.Scout)=>
        new SquadPressureScheduler(new[]{new PressureMember(10,a)},Rule());
    static PressureObservation[] Obs(int id=10,bool alive=true,bool visible=true,bool canExecute=true,float distance=12f)=>
        new[]{new PressureObservation(id,alive,visible,canExecute,distance)};
    static SquadPressureScheduler Pair()=>
        new SquadPressureScheduler(new[]{new PressureMember(10,R3EnemyArchetype.Scout),new PressureMember(20,R3EnemyArchetype.Gunner)},Rule());
    static PressureObservation[] Both(bool scout=true,bool gunner=true)=>
        new[]{new PressureObservation(10,true,true,scout,12),new PressureObservation(20,true,true,gunner,24)};
    static string GitBlob(string path)
    {
        byte[] payload=File.ReadAllBytes(path);
        byte[] prefix=Encoding.ASCII.GetBytes("blob "+payload.Length+"\0");
        var bytes=new byte[prefix.Length+payload.Length];Buffer.BlockCopy(prefix,0,bytes,0,prefix.Length);Buffer.BlockCopy(payload,0,bytes,prefix.Length,payload.Length);
        return Convert.ToHexString(SHA1.HashData(bytes)).ToLowerInvariant();
    }
    static string Digest(List<string> lines)=>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n",lines)))).ToLowerInvariant();
    static string ReadScenarioReceipts()
    {
        string root="Research/B01-24/P17/W06-SQUAD-PRESSURE-001/";
        using var json=JsonDocument.Parse(File.ReadAllText(root+"scenarios.json"));
        var data=json.RootElement;
        Ensure(data.GetProperty("schema").GetString()=="B01-24-SQUAD-PRESSURE-001-v1","wrong input schema");
        var src=data.GetProperty("source");
        Ensure(GitBlob(src.GetProperty("r3ArchetypePath").GetString()!)==src.GetProperty("r3ArchetypeBlob").GetString(),"source drift real R3 enum");
        var entries=data.GetProperty("authoredCases").EnumerateArray().ToArray();
        Ensure(entries.Length==3,"authored squad scenario count");
        var all=new List<string>();
        foreach(var sc in entries)
        {
            string id=sc.GetProperty("id").GetString()!;
            var members=sc.GetProperty("members").EnumerateArray().Select(m=>
                new PressureMember(m.GetProperty("id").GetInt32(),Enum.Parse<R3EnemyArchetype>(m.GetProperty("kind").GetString()!,false))).ToArray();
            var observations=sc.GetProperty("members").EnumerateArray().Select(m=>
                new PressureObservation(m.GetProperty("id").GetInt32(),true,true,true,m.GetProperty("distance").GetSingle())).ToArray();
            var s=new SquadPressureScheduler(members,Rule());
            int start=0,end=0,active=0;long last=-1;
            foreach(var time in sc.GetProperty("ticks").EnumerateArray())
            {
                long t=time.GetInt64();Ensure(t>last,"Nonmonotonic authored replay time");last=t;
                var result=s.Tick(t,observations);
                if(result.State==PressureState.Started)start++;
                if(result.State==PressureState.Ended)end++;
                if(result.State==PressureState.Active)active++;
                all.Add(id+"|"+t+"|"+result.State+"|"+result.MemberId+"|"+result.Intent+"|"+result.UntilTick+"|"+result.MarkedByScout);
            }
            Ensure(start>=2&&end>=1,"scenario never exercises actual reservation lifecycle: "+id);
            Console.WriteLine("REPLAY "+id+" starts="+start+" ends="+end+" active="+active);
        }
        string digest=Digest(all);
        Console.WriteLine("REPLAY_SHA256 "+digest+" ("+all.Count+" executed original source events)");
        return digest;
    }
    public static int Main()
    {
        Check("source pinned original Codex R3 archetype C# Git blob",()=>Ensure(
            GitBlob("Assets/R3/R3EnemyArchetype.cs")=="66cd1ee734b129117b3753d1205469a0496cb673","original R3 changed"));
        Check("original R3 archetype ABI and names from compiled actual source",()=>{
            Ensure((int)R3EnemyArchetype.Scout==0&&(int)R3EnemyArchetype.Bruiser==1&&
                (int)R3EnemyArchetype.Gunner==2&&(int)R3EnemyArchetype.Flanker==3,"R3 enum ABI drift");
            Ensure(R3EnemyProfiles.InEncounter(2,0)==R3EnemyArchetype.Bruiser&&
                R3EnemyProfiles.InEncounter(2,1)==R3EnemyArchetype.Gunner,"original R3 profile changed");
        });
        Check("single scout emits actual mark reservation",()=>{
            var p=Single();var t=p.Tick(0,Obs());Ensure(t.State==PressureState.Started&&
                t.Intent==PressureIntent.ScoutMark&&t.MemberId==10&&t.UntilTick==3,"wrong scout action");
        });
        Check("within attack window cannot double allocate",()=>{
            var p=Single();p.Tick(0,Obs());for(int t=1;t<3;t++)
                Ensure(p.Tick(t,Obs()).State==PressureState.Active,"multiple simultaneous grants");
        });
        Check("window expiration ends before allowing new start",()=>{
            var p=Single();p.Tick(0,Obs());Ensure(p.Tick(3,Obs()).State==PressureState.Ended,"end missing");
            Ensure(p.Tick(4,Obs()).State==PressureState.Idle,"global cooldown ignored");
        });
        Check("own role cooldown enforces no scout special action spam",()=>{
            var p=Single();p.Tick(0,Obs());p.Tick(3,Obs());p.Tick(5,Obs());
            Ensure(p.Tick(9,Obs()).State==PressureState.Idle,"cooldown ignored");
            Ensure(p.Tick(10,Obs()).State==PressureState.Started,"cooldown never recovers");
        });
        Check("two members alternate their actual R3 roles",()=>{
            var p=Pair();Ensure(p.Tick(0,Both()).Intent==PressureIntent.ScoutMark,"first should scout");
            p.Tick(3,Both());var t=p.Tick(5,Both());
            Ensure(t.Intent==PressureIntent.GunnerVolley&&t.MemberId==20&&t.MarkedByScout,"expected gunner marked priority");
        });
        Check("two members stay under one globally reserved special action",()=>{
            var p=Pair();p.Tick(0,Both());Ensure(p.Tick(1,Both()).State==PressureState.Active,"overbooking");
        });
        Check("scout marker automatically expires, never persists forever",()=>{
            var p=Pair();p.Tick(0,Both());p.Tick(3,Both());
            // Target unavailable until after mark TTL and cooldown gap.
            for(int tick=5;tick<13;tick++)Ensure(p.Tick(tick,Both(false,false)).State==PressureState.Idle,"should be idle");
            var t=p.Tick(13,Both(false,true));Ensure(t.Intent==PressureIntent.GunnerVolley&&!t.MarkedByScout,"stale scout mark");
        });
        Check("boss/brawler style Bruiser maps to push, never a new ram controller",()=>{
            var p=Single(R3EnemyArchetype.Bruiser);Ensure(p.Tick(0,Obs(distance:12)).Intent==PressureIntent.BruiserPush,"no Bruiser push");
        });
        Check("Flanker tactical role has explicit pincer order",()=>{
            var p=Single(R3EnemyArchetype.Flanker);Ensure(p.Tick(0,Obs(distance:23)).Intent==PressureIntent.FlankerPincer,"no flanker order");
        });
        Check("Gunner maps to volley intent without modifying the weapon",()=>{
            var p=Single(R3EnemyArchetype.Gunner);Ensure(p.Tick(0,Obs(distance:50)).Intent==PressureIntent.GunnerVolley,"no gunner order");
        });
        Check("Bruiser cannot reserve past 16m original authored range",()=>{
            var p=Single(R3EnemyArchetype.Bruiser);Ensure(p.Tick(0,Obs(distance:17)).State==PressureState.Idle,"bruiser range");
        });
        Check("exact range edge is eligible",()=>{
            var p=Single(R3EnemyArchetype.Bruiser);Ensure(p.Tick(0,Obs(distance:16)).State==PressureState.Started,"edge rejected");
        });
        Check("lost target visibility cancels active reservation without a new grant",()=>{
            var p=Single();p.Tick(0,Obs());var t=p.Tick(1,Obs(visible:false));
            Ensure(t.State==PressureState.Ended&&!p.HasActiveReservation,"slot not released");
        });
        Check("dead enemy cancels active reservation",()=>{
            var p=Single();p.Tick(0,Obs());Ensure(p.Tick(1,Obs(alive:false)).State==PressureState.Ended,"dead reserved slot");
        });
        Check("disabled weapon/movement by owner cancels and cannot execute",()=>{
            var p=Single();p.Tick(0,Obs());Ensure(p.Tick(1,Obs(canExecute:false)).State==PressureState.Ended,"disabled still attacking");
        });
        Check("unarmed or disabled character never granted a new special ability",()=>{
            var p=Single();Ensure(p.Tick(0,Obs(canExecute:false)).State==PressureState.Idle,"unexpected order");
        });
        Check("missing LOS does not propose a new action",()=>{
            var p=Single();Ensure(p.Tick(0,Obs(visible:false)).State==PressureState.Idle,"phantom LOS");
        });
        Check("all dead squad produces no action",()=>{
            var p=Pair();
            var o=new[]{new PressureObservation(10,false,false,false,1),new PressureObservation(20,false,false,false,1)};
            Ensure(p.Tick(0,o).State==PressureState.Idle,"dead squad reserved special");
        });
        Check("cannot construct squad with no members",()=>Reject(()=>new SquadPressureScheduler(Array.Empty<PressureMember>(),Rule())));
        Check("cannot construct squad with more than four members",()=>Reject(()=>new SquadPressureScheduler(
            Enumerable.Range(0,5).Select(i=>new PressureMember(i,R3EnemyArchetype.Scout)).ToArray(),Rule())));
        Check("cannot construct duplicate identity squad",()=>Reject(()=>new SquadPressureScheduler(
            new[]{new PressureMember(1,R3EnemyArchetype.Scout),new PressureMember(1,R3EnemyArchetype.Gunner)},Rule())));
        Check("cannot construct unknown R3 enum role",()=>Reject(()=>Single((R3EnemyArchetype)99)));
        Check("reject duplicate observations",()=>{var p=Pair();Reject(()=>p.Tick(0,new[]{Obs()[0],Obs()[0]}));});
        Check("reject missing/foreign observation IDs",()=>{var p=Pair();Reject(()=>p.Tick(0,new[]{Obs()[0],new PressureObservation(99,true,true,true,1)}));});
        Check("reject null or mismatched observation array",()=>{var p=Pair();Reject(()=>p.Tick(0,Obs()));Reject(()=>p.Tick(0,null!));});
        Check("reject NaN and Infinity distances before mutating tick",()=>{
            var p=Single();Reject(()=>p.Tick(0,Obs(distance:float.NaN)));
            Reject(()=>p.Tick(0,Obs(distance:float.PositiveInfinity)));
            Ensure(p.Tick(0,Obs()).State==PressureState.Started,"rejection modified internal state");
        });
        Check("negative/out-of-game distance is rejected",()=>{var p=Single();Reject(()=>p.Tick(0,Obs(distance:-1)));Reject(()=>p.Tick(0,Obs(distance:2001)));});
        Check("duplicate or backward tick rejected with no state mutation",()=>{
            var p=Single();p.Tick(0,Obs());Reject(()=>p.Tick(0,Obs()));Reject(()=>p.Tick(-1,Obs()));
            Ensure(p.Tick(1,Obs()).State==PressureState.Active,"state damaged after reject");
        });
        Check("tick arithmetic avoids long overflow",()=>{
            var p=Single();Reject(()=>p.Tick(long.MaxValue,Obs()));
        });
        Check("nonsensical policy windows/cooldowns/ranges rejected",()=>{
            Reject(()=>new SquadPressurePolicy(0,0,1,new[]{1,1,1,1},new[]{1f,1f,1f,1f}));
            Reject(()=>new SquadPressurePolicy(4,0,1,new[]{1,4,4,4},new[]{1f,1f,1f,1f}));
            Reject(()=>new SquadPressurePolicy(1,0,1,new[]{1,1,1,1},new[]{1f,float.NaN,1f,1f}));
        });
        Check("deep copy of original roster forbids mutation during run",()=>{
            var a=new[]{new PressureMember(10,R3EnemyArchetype.Scout)};
            var p=new SquadPressureScheduler(a,Rule());a[0]=new PressureMember(20,R3EnemyArchetype.Gunner);
            Ensure(p.Tick(0,Obs()).Intent==PressureIntent.ScoutMark,"roster mutation escaped");
        });
        Check("member observation order cannot alter deterministic arbitration",()=>{
            var a=Pair();var b=Pair();var x=Both();var reversed=new[]{x[1],x[0]};
            for(int t=0;t<=25;t++){
                var u=a.Tick(t,x);var v=b.Tick(t,reversed);
                Ensure(u.State==v.State&&u.MemberId==v.MemberId&&u.Intent==v.Intent&&u.MarkedByScout==v.MarkedByScout,"order affected tick "+t);
            }
        });
        Check("separate squads never share cooldown or active reservation",()=>{
            var a=Pair();var b=Pair();
            Ensure(a.Tick(0,Both()).State==PressureState.Started,"a");
            Ensure(b.Tick(0,Both()).State==PressureState.Started,"b");
            Ensure(a.HasActiveReservation&&b.HasActiveReservation,"cross-squad corruption");
        });
        Check("fairness: all four canonical archetypes get a reservation",()=>{
            var members=new[]{
                new PressureMember(1,R3EnemyArchetype.Scout),new PressureMember(2,R3EnemyArchetype.Bruiser),
                new PressureMember(3,R3EnemyArchetype.Gunner),new PressureMember(4,R3EnemyArchetype.Flanker)};
            var obs=new[]{
                new PressureObservation(1,true,true,true,10),new PressureObservation(2,true,true,true,10),
                new PressureObservation(3,true,true,true,10),new PressureObservation(4,true,true,true,10)};
            var s=new SquadPressureScheduler(members,Rule());var seen=new HashSet<int>();
            for(int t=0;t<=60;t++){var e=s.Tick(t,obs);if(e.State==PressureState.Started)seen.Add(e.MemberId);}
            Ensure(seen.Count==4,"starved canonical R3 role");
        });
        Check("real authored three squad scenarios execute end-to-end",()=>{
            string hash=ReadScenarioReceipts();
            Ensure(hash.Length==64 && hash.All(c=>"0123456789abcdef".Contains(c)),"invalid scenario trace receipt");
        });
        Check("original authored replay deterministic SHA across independent replays",()=>{
            string a=ReadScenarioReceipts();string b=ReadScenarioReceipts();
            Ensure(a==b,"replay result nondeterministic");
        });
        Console.WriteLine("B01-24 SQUAD-PRESSURE-001 real compiled original R3 + original scheduler: "+passed+" PASS / "+failed+" FAIL; Unity/PhysX/Player NOT_RUN");
        return failed==0?0:1;
    }
}
