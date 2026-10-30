// B01-25 SYNCRELAY-001: execute ACTUAL C#9 original relay rule source, map fixtures, negatives.
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Linq;
using B01.LevelChallenges;

internal static class Proof
{
    static int passed,negative;
    static readonly List<string> checks = new();
    static void Need(bool valid,string reason){if(!valid)throw new Exception(reason);}
    static void Near(double a,double b,string reason){Need(Math.Abs(a-b)<1e-8,reason+" actual "+a+" expected "+b);}
    static void Test(string id,Action check)
    {
        try{check();passed++;checks.Add(id);Console.WriteLine("PASS "+id);}
        catch(Exception e){Console.Error.WriteLine("FAIL "+id+": "+e);throw;}
    }
    static void Reject(string id,Action action)
    {
        Test("NEGATIVE_"+id,()=>{bool thrown=false;try{action();}catch(ArgumentException){thrown=true;}Need(thrown,"must throw explicit ArgumentException");});
        negative++;
    }
    static string Blob(byte[] bytes)
    {
        var prefix=Encoding.UTF8.GetBytes("blob "+bytes.Length+"\0");
        var raw=new byte[prefix.Length+bytes.Length];
        Buffer.BlockCopy(prefix,0,raw,0,prefix.Length);
        Buffer.BlockCopy(bytes,0,raw,prefix.Length,bytes.Length);
        return Convert.ToHexString(SHA1.HashData(raw)).ToLowerInvariant();
    }
    static string Str(JsonElement e,string key)=>e.GetProperty(key).GetString()??"";
    static double Num(JsonElement e,string key)=>e.GetProperty(key).GetDouble();
    static int Int(JsonElement e,string key)=>e.GetProperty(key).GetInt32();
    static int[] Pair(JsonElement e,string key)=>e.GetProperty(key).EnumerateArray().Select(v=>v.GetInt32()).ToArray();
    static RelaySite Site(JsonElement j)
    {
        var xy=j.GetProperty("world").EnumerateArray().Select(v=>v.GetDouble()).ToArray();
        return new RelaySite(Str(j,"id"),xy[0],xy[1],Num(j,"radius"));
    }
    static SyncRelayConfig Config(JsonElement j)
    {
        var nodes=j.GetProperty("sites").EnumerateArray().ToArray();
        Need(nodes.Length==2,"two relay sites expected");
        return new SyncRelayConfig(Str(j,"id"),Site(nodes[0]),Site(nodes[1]),Num(j,"windowSeconds"));
    }
    static string Key(int x,int z)=>x+","+z;
    static int? WalkDistance(HashSet<string> floor,HashSet<string> walls,int[] start,int[] finish)
    {
        var q=new Queue<(int x,int z,int d)>();q.Enqueue((start[0],start[1],0));
        var seen=new HashSet<string>();
        while(q.Count>0){
            var (x,z,d)=q.Dequeue();
            var k=Key(x,z);
            if(!floor.Contains(k)||walls.Contains(k)||!seen.Add(k))continue;
            if(x==finish[0]&&z==finish[1])return d;
            q.Enqueue((x+1,z,d+1));q.Enqueue((x-1,z,d+1));
            q.Enqueue((x,z+1,d+1));q.Enqueue((x,z-1,d+1));
        }
        return null;
    }
    static void ValidateMap(JsonElement profile,byte[] source,string expectedBlob,int expectedCount)
    {
        string id=Str(profile,"id");
        Test(id+"_AUTHENTIC_GIT_SOURCE",()=>Need(Blob(source)==expectedBlob&&Str(profile,"mapGitBlob")==expectedBlob,"wrong source map Git blob"));
        using var json=JsonDocument.Parse(source);
        var map=json.RootElement;
        var items=map.GetProperty("items").EnumerateArray().ToArray();
        Test(id+"_MAP_DOCUMENT_V2",()=>Need(Int(map,"version")==2&&!map.GetProperty("globalFloor").GetBoolean()&&items.Length==expectedCount,"wrong authored MapDocument"));
        var floor=new HashSet<string>(items.Where(v=>Int(v,"kind")==0).Select(v=>Key(Int(v,"x"),Int(v,"z"))));
        var walls=new HashSet<string>(items.Where(v=>Int(v,"kind")==1).Select(v=>Key(Int(v,"x"),Int(v,"z"))));
        var spawn=items.Single(v=>Int(v,"kind")==4);
        var finish=items.Single(v=>Int(v,"kind")==5);
        int[] sp={Int(spawn,"x"),Int(spawn,"z")},ed={Int(finish,"x"),Int(finish,"z")};
        var sites=profile.GetProperty("sites").EnumerateArray().ToArray();
        SyncRelayConfig config=Config(profile);
        Test(id+"_CONFIG_VALID",()=>config.Validate());
        Test(id+"_SITES_GROUND_ONLY",()=>{
            foreach(var s in sites){
                int[] c=Pair(s,"cell");
                Need(c.Length==2&&floor.Contains(Key(c[0],c[1])),"missing original site floor");
                Need(!items.Any(v=>Int(v,"kind")!=0&&Int(v,"x")==c[0]&&Int(v,"z")==c[1]),"site center occupied by map object");
                var position=s.GetProperty("world").EnumerateArray().Select(v=>v.GetDouble()).ToArray();
                Near(position[0],c[0]*4,"grid x correspondence");
                Near(position[1],c[1]*4,"grid z correspondence");
            }
        });
        Test(id+"_NO_SPAWN_FINISH_OVERLAP",()=>{
            foreach(var s in sites){
                var c=Pair(s,"cell");Need(Key(c[0],c[1])!=Key(sp[0],sp[1])&&Key(c[0],c[1])!=Key(ed[0],ed[1]),"relay would overwrite spawn or finish");
            }
        });
        Test(id+"_STATIC_FLOOR_ROUTE_REACHABLE",()=>{
            int[] a=Pair(sites[0],"cell"),b=Pair(sites[1],"cell");
            var d0=WalkDistance(floor,walls,sp,a);
            var d1=WalkDistance(floor,walls,a,b);
            var d2=WalkDistance(floor,walls,b,ed);
            var d3=WalkDistance(floor,walls,sp,b);
            var d4=WalkDistance(floor,walls,b,a);
            var d5=WalkDistance(floor,walls,a,ed);
            Need(d0.HasValue&&d1.HasValue&&d2.HasValue&&d3.HasValue&&d4.HasValue&&d5.HasValue,
                "unordered relay traversal source-softlocks from either direction");
        });
        Test(id+"_TIME_WINDOW_CONSERVATIVE_SOURCE_ONLY",()=>{
            int[] a=Pair(sites[0],"cell"),b=Pair(sites[1],"cell");
            int cells=WalkDistance(floor,walls,a,b)??999;
            Need(cells*4.0/config.PairWindowSeconds<=2.0,
                "relay window may require >2m/s average on source Manhattan shortest path");
            // This source check is NOT vehicle physics or Light/Heavy native drive.
        });
    }
    static void Main(string[] args)
    {
        if(args.Length!=4){
            Console.Error.WriteLine("Usage: profiles.json CLEARANCE arena.json SLALOM arena.json receipt.json");
            Environment.ExitCode=2;return;
        }
        using var data=JsonDocument.Parse(File.ReadAllText(args[0]));
        var scenarios=data.RootElement.GetProperty("levels").EnumerateArray().ToArray();
        Need(scenarios.Length==2,"expected two existing authored map sources");
        ValidateMap(scenarios[0],File.ReadAllBytes(args[1]),"53eb696950121465befea2855eda4d7268750016",415);
        ValidateMap(scenarios[1],File.ReadAllBytes(args[2]),"013f5fe2058e76233f580a439e8402c0fe09f124",360);
        var cfg=Config(scenarios[0]);
        var a=cfg.A;var b=cfg.B;
        var zone=new SyncRelayController(cfg);
        Test("INITIAL_IDLE",()=>Need(zone.Phase==RelayPhase.Idle&&zone.FirstIndex==-1&&zone.RemainingSeconds==0,"not idle"));
        Test("CAN_ACTIVATE_A_FIRST",()=>{
            var r=zone.Activate(a.Id,a.X,a.Z,true,false);
            Need(r.Accepted&&r.Phase==RelayPhase.WaitingForPartner&&r.FirstRelayId==a.Id&&!r.Unlocked,"first relay not accepted");
            Near(r.RemainingSeconds,cfg.PairWindowSeconds,"full window after first activation");
        });
        Test("SAME_SITE_SPAM_NOT_A_SECOND_ACTIVATION",()=>{
            var r=zone.Activate(a.Id,a.X,a.Z,true,false);
            Need(!r.Accepted&&!r.BecameUnlocked&&!r.Unlocked&&zone.FirstIndex==0,"same relay must not unlock");
        });
        Test("CLOCK_ADVANCES_WHILE_WAITING",()=>{
            var r=zone.Advance(2,true,false);
            Need(r.Phase==RelayPhase.WaitingForPartner&&!r.TimedOut,"should still wait");
            Near(r.RemainingSeconds,cfg.PairWindowSeconds-2,"elapsed");
        });
        Test("PAUSE_DOES_NOT_TICK",()=>{
            var r=zone.Advance(300,true,true);
            Near(r.RemainingSeconds,cfg.PairWindowSeconds-2,"paused timer should not move");
        });
        Test("PAUSED_INTERACTION_REJECTED",()=>{
            var r=zone.Activate(b.Id,b.X,b.Z,true,true);
            Need(!r.Accepted&&!r.BecameUnlocked&&!r.Unlocked,"pause cannot unlock");
        });
        Test("OUT_OF_RANGE_SECOND_RELAY_REJECTED",()=>{
            var r=zone.Activate(b.Id,b.X+20,b.Z,true,false);
            Need(!r.Accepted&&!r.Unlocked&&r.FirstRelayId==a.Id,"range requirement ignored");
        });
        Test("SECOND_PARTNER_UNLOCKS_ONCE",()=>{
            var r=zone.Activate(b.Id,b.X,b.Z,true,false);
            Need(r.Accepted&&r.BecameUnlocked&&r.Unlocked&&zone.Phase==RelayPhase.Unlocked,"partner not unlocking");
        });
        Test("UNLOCK_LATCHES_WITHOUT_REEMISSION",()=>{
            var r=zone.Activate(b.Id,b.X,b.Z,true,false);
            Need(r.Unlocked&&!r.Accepted&&!r.BecameUnlocked,"duplicate unlocked event");
            r=zone.Advance(120,true,false);
            Need(r.Unlocked&&!r.BecameUnlocked&&zone.Phase==RelayPhase.Unlocked,"unlock must latch while alive");
        });
        Test("DEATH_CLEARS_UNLOCK",()=>{
            var r=zone.Advance(0,false,false);
            Need(!r.Unlocked&&zone.Phase==RelayPhase.Idle&&zone.FirstIndex==-1,"death must reset even after unlock");
        });
        Test("REVERSE_ORDER_ALLOWED",()=>{
            var r=zone.Activate(b.Id,b.X,b.Z,true,false);
            Need(r.Accepted&&r.FirstRelayId==b.Id,"B cannot be first");
            r=zone.Activate(a.Id,a.X,a.Z,true,false);
            Need(r.Unlocked&&r.BecameUnlocked,"reverse order must unlock");
        });
        Test("RESET_CLEARS_UNLOCK",()=>{
            zone.Reset();Need(zone.Phase==RelayPhase.Idle&&zone.FirstIndex==-1,"manual reset failed");
        });
        Test("EXACT_DEADLINE_EXPIRES",()=>{
            zone.Activate(a.Id,a.X,a.Z,true,false);
            var r=zone.Advance(cfg.PairWindowSeconds,true,false);
            Need(r.TimedOut&&r.Phase==RelayPhase.Idle&&!r.Unlocked,"exact deadline must fail safely");
        });
        Test("TIMEOUT_ALLOWS_RETRY_NOT_SOFTLOCK",()=>{
            var r=zone.Activate(b.Id,b.X,b.Z,true,false);
            Need(r.Accepted&&zone.FirstIndex==1,"timed-out run cannot restart");
            r=zone.Activate(a.Id,a.X,a.Z,true,false);
            Need(r.Unlocked&&r.BecameUnlocked,"retry after timeout must succeed");
        });
        Test("JUST_BEFORE_DEADLINE_SUCCEEDS",()=>{
            zone.Reset();zone.Activate(a.Id,a.X,a.Z,true,false);
            var r=zone.Advance(cfg.PairWindowSeconds-.000001,true,false);
            Need(!r.TimedOut&&r.RemainingSeconds>0,"premature timeout");
            r=zone.Activate(b.Id,b.X,b.Z,true,false);
            Need(r.Unlocked&&r.BecameUnlocked,"near deadline should succeed");
        });
        Test("DIED_AFTER_FIRST_RELAY_RESETS",()=>{
            zone.Reset();zone.Activate(a.Id,a.X,a.Z,true,false);
            var r=zone.Activate(b.Id,b.X,b.Z,false,false);
            Need(!r.Unlocked&&zone.Phase==RelayPhase.Idle,"interaction from dead player must reset");
        });
        Test("IDLE_CLOCK_DOES_NOT_START",()=>{
            zone.Reset();var r=zone.Advance(9999,true,false);
            Need(r.Phase==RelayPhase.Idle&&!r.TimedOut,"timer should not start before first interaction");
        });
        Test("SITE_BOUNDARY_INCLUSIVE",()=>{
            zone.Reset();
            var r=zone.Activate(a.Id,a.X+a.InteractionRadius,a.Z,true,false);
            Need(r.Accepted,"site exact floating-point radius must count");
        });
        Test("JUST_OUTSIDE_SITE_REJECTED",()=>{
            zone.Reset();
            var r=zone.Activate(a.Id,a.X+a.InteractionRadius+.001,a.Z,true,false);
            Need(!r.Accepted&&r.Phase==RelayPhase.Idle,"outside radius must not activate");
        });
        Test("DELTA_PARTITION_EQUIVALENCE",()=>{
            var lhs=new SyncRelayController(cfg);var rhs=new SyncRelayController(cfg);
            lhs.Activate(a.Id,a.X,a.Z,true,false);rhs.Activate(a.Id,a.X,a.Z,true,false);
            lhs.Advance(8,true,false);
            for(int i=0;i<32;i++)rhs.Advance(.25,true,false);
            Near(lhs.RemainingSeconds,rhs.RemainingSeconds,"frame partition discrepancy");
        });
        Test("SECOND_LEVEL_INDEPENDENT_CONFIG",()=>{
            var second=Config(scenarios[1]);
            var c=new SyncRelayController(second);
            var r=c.Activate(second.B.Id,second.B.X,second.B.Z,true,false);
            Need(r.Accepted&&r.FirstRelayId==second.B.Id&&second.PairWindowSeconds!=cfg.PairWindowSeconds,"second map profile not independent");
        });
        Test("CONTROLLERS_DO_NOT_SHARE_STATE",()=>{
            var lhs=new SyncRelayController(cfg);var rhs=new SyncRelayController(cfg);
            lhs.Activate(a.Id,a.X,a.Z,true,false);
            Need(rhs.Phase==RelayPhase.Idle&&rhs.RemainingSeconds==0,"shared mutable state detected");
        });
        var neg=new SyncRelayController(cfg);
        Reject("EMPTY_INTERACTION_ID",()=>neg.Activate("",a.X,a.Z,true,false));
        Reject("UNKNOWN_INTERACTION_ID",()=>neg.Activate("not-present",a.X,a.Z,true,false));
        Reject("NAN_PLAYER_X",()=>neg.Activate(a.Id,double.NaN,a.Z,true,false));
        Reject("INFINITE_PLAYER_Z",()=>neg.Activate(a.Id,a.X,double.PositiveInfinity,true,false));
        Reject("NEGATIVE_CLOCK_DELTA",()=>neg.Advance(-1,true,false));
        Reject("NAN_CLOCK_DELTA",()=>neg.Advance(double.NaN,true,false));
        Reject("INFINITE_CLOCK_DELTA",()=>neg.Advance(double.PositiveInfinity,true,false));
        Test("NEGATIVES_DO_NOT_MUTATE_STATE",()=>Need(neg.Phase==RelayPhase.Idle&&neg.RemainingSeconds==0,"failed input changed state"));
        Reject("DUPLICATE_SITE_IDS",()=>new SyncRelayController(new("TEST",a,new RelaySite(a.Id,b.X,b.Z,b.InteractionRadius),24)));
        Reject("TOO_SHORT_WINDOW",()=>new SyncRelayController(new("TEST",a,b,1)));
        Reject("TOO_LONG_WINDOW",()=>new SyncRelayController(new("TEST",a,b,121)));
        Reject("NAN_WINDOW",()=>new SyncRelayController(new("TEST",a,b,double.NaN)));
        Reject("ZERO_SITE_RADIUS",()=>new SyncRelayController(new("TEST",new RelaySite("A",a.X,a.Z,0),b,24)));
        Reject("INFINITE_SITE_CENTER",()=>new SyncRelayController(new("TEST",a,new RelaySite("B",double.PositiveInfinity,b.Z,1),24)));
        Reject("OVERLAPPING_SITES",()=>new SyncRelayController(new("TEST",a,new RelaySite("B",a.X+1,a.Z,1.6),24)));
        Reject("INVALID_EMPTY_CONFIG_ID",()=>new SyncRelayController(new("",a,b,24)));
        Test("NO_UNAUTHORIZED_GLOBAL_DAMAGE_REWARD_FINISH",()=>{
            Need(typeof(SyncRelayController).GetEvents().Length==0,"global game event unexpectedly registered");
        });
        Console.WriteLine("RESULT PASS "+passed+"/"+passed+" ORIGINAL_CSHARP9_SYNCRELAY_CHECKS ("+negative+" explicit negatives), two original map blobs; UNITY_NATIVE=NOT_RUN");
        var output=new {package="B01-25-SYNCRELAY-001/R1",passed,negative,
            sourceBlobClearance="53eb696950121465befea2855eda4d7268750016",
            sourceBlobSlalom="013f5fe2058e76233f580a439e8402c0fe09f124",
            nativeUnity="NOT_RUN",checks};
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[3]))!);
        File.WriteAllText(args[3],JsonSerializer.Serialize(output,new JsonSerializerOptions{WriteIndented=true})+"\n");
    }
}