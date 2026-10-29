// B01-25-HOLDOUT-001 original executable level-challenge rules and scenarios.
// Run on ACTUAL released C# via dotnet 8 in CI. NO claims of Unity native.
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using System.Collections.Generic;
using B01.LevelChallenges;

static class ChallengeProof
{
    static int successes=0, negativeCases=0;
    static readonly List<string> receipts=new();
    static void Check(string label,Action act)
    {
        try{act();Console.WriteLine("PASS "+label);successes++;receipts.Add(label);}
        catch(Exception e){Console.Error.WriteLine("FAIL "+label+" -> "+e);throw;}
    }
    static void Need(bool value,string why){if(!value)throw new Exception(why);}
    static void Near(double actual,double expected,string reason)
    {Need(Math.Abs(actual-expected)<1e-9,reason+" expected="+expected+" actual="+actual);}
    static void Reject(string label,Action act)
    {
        Check("NEGATIVE_"+label,()=>{
            bool rejected=false;
            try{act();}catch(ArgumentException){rejected=true;}
            Need(rejected,"expected ArgumentException");});
        negativeCases++;
    }
    static string Blob(byte[] raw)
    {
        byte[] prefix=Encoding.UTF8.GetBytes("blob "+raw.Length+"\0");
        byte[] bytes=new byte[prefix.Length+raw.Length];
        Buffer.BlockCopy(prefix,0,bytes,0,prefix.Length);
        Buffer.BlockCopy(raw,0,bytes,prefix.Length,raw.Length);
        return Convert.ToHexString(SHA1.HashData(bytes)).ToLowerInvariant();
    }
    static JsonElement Property(JsonElement e,string key)=>e.GetProperty(key);
    static int Int(JsonElement e,string key)=>Property(e,key).GetInt32();
    static double Num(JsonElement e,string key)=>Property(e,key).GetDouble();
    static string Str(JsonElement e,string key)=>Property(e,key).GetString()??"";
    static (int x,int z) Pair(JsonElement e,string key){
        var a=Property(e,key).EnumerateArray().Select(v=>v.GetInt32()).ToArray();
        Need(a.Length==2,"Invalid pair "+key);return(a[0],a[1]);
    }
    static HoldoutConfig Config(JsonElement p)=>new(Str(p,"id"),Property(p,"zoneWorldMeters")[0].GetDouble(),
        Property(p,"zoneWorldMeters")[1].GetDouble(),Num(p,"radiusMeters"),Num(p,"requiredSeconds"),Num(p,"outsideDecayPerSecond"));
    static HoldoutSample Sample(double dt,double x,double z,int hostiles=0,bool alive=true)=>new(dt,x,z,hostiles,alive);
    static void Main(string[] args)
    {
        if(args.Length!=4){Console.Error.WriteLine("Usage: Holdout001 <profiles.json> <clearance arena.json> <slalom arena.json> <receipt.json>");Environment.ExitCode=2;return;}
        var configText=File.ReadAllText(args[0]);using var cfgJson=JsonDocument.Parse(configText);
        var profiles=Property(cfgJson.RootElement,"profiles").EnumerateArray().ToArray();
        Need(profiles.Length==2,"Expected exactly two original challenge mappings");
        var mapFiles=new[]{args[1],args[2]};
        var authentic=new List<(string name,string mapBlob,JsonElement profile,HoldoutConfig cfg)>();
        for(int i=0;i<profiles.Length;i++)
        {
            var p=profiles[i]; var raw=File.ReadAllBytes(mapFiles[i]);
            var id=Str(p,"id"); var blob=Blob(raw);
            using var j=JsonDocument.Parse(raw);
            var items=Property(j.RootElement,"items").EnumerateArray().ToArray();
            var cell=Pair(p,"zoneCell");
            var target=Property(p,"zoneWorldMeters").EnumerateArray().Select(x=>x.GetDouble()).ToArray();
            var c=new HoldoutConfig(id,target[0],target[1],Num(p,"radiusMeters"),Num(p,"requiredSeconds"),Num(p,"outsideDecayPerSecond"));
            Check(id+"_ORIGINAL_GIT_BLOB",()=>Need(blob==Str(p,"mapGitBlob"),"source source blob mismatch "+blob));
            Check(id+"_MAPDOC_V2",()=>Need(Int(j.RootElement,"version")==2&&!Property(j.RootElement,"globalFloor").GetBoolean(),"MapDocument v2 local floor mandatory"));
            Check(id+"_ZONE_MAP_FLOOR_AND_UNOCCUPIED",()=>{
                bool floor=items.Any(it=>Int(it,"kind")==0&&Int(it,"x")==cell.x&&Int(it,"z")==cell.z);
                bool obstacle=items.Any(it=>Int(it,"kind")!=0&&Int(it,"x")==cell.x&&Int(it,"z")==cell.z);
                Need(floor&&!obstacle,"hold-zone center is an occupied or missing ground cell");
            });
            Check(id+"_ZONE_WORLD_ALIGNMENT",()=>{
                Near(c.CenterX,cell.x*4,"zone center x");Near(c.CenterZ,cell.z*4,"zone center z");
                var finish=items.Single(it=>Int(it,"kind")==5);
                double dx=Int(finish,"x")*4-c.CenterX,dz=Int(finish,"z")*4-c.CenterZ;
                Need(Math.Sqrt(dx*dx+dz*dz)>c.RadiusMeters+2,"Zone overlaps finish marker source center envelope");
            });
            Check(id+"_CONFIG_VALID",()=>c.Validate());
            authentic.Add((id,blob,p,c));
        }
        var first=authentic[0].cfg;
        var zone=new HoldoutController(first);
        var x=first.CenterX; var z=first.CenterZ;
        Check("WAITING_BEFORE_ENTRY",()=>{var r=zone.Tick(Sample(.25,x+8,z));Need(r.Phase==HoldoutPhase.Waiting&&r.ProgressSeconds==0,"not waiting");});
        Check("CHARGES_WHEN_SAFE_INSIDE",()=>{var r=zone.Tick(Sample(2,x,z));Need(r.Phase==HoldoutPhase.Charging&&!r.Ready, "should charge");Near(r.ProgressSeconds,2,"first charge");});
        Check("CONTESTED_FREEZES",()=>{var r=zone.Tick(Sample(5,x,z,3));Need(r.Phase==HoldoutPhase.Contested, "not contested");Near(r.ProgressSeconds,2,"contested shouldn't advance");});
        Check("OUTSIDE_DECAYS",()=>{var r=zone.Tick(Sample(1,x+8,z));Near(r.ProgressSeconds,.5,"outside decay");Need(r.Phase==HoldoutPhase.Recovering,"expected recovery");});
        Check("LONG_OUTSIDE_CLAMPS_ZERO",()=>{var r=zone.Tick(Sample(20,x+8,z));Near(r.ProgressSeconds,0,"negative progress forbidden");Need(r.Phase==HoldoutPhase.Waiting,"waiting");});
        Check("DEAD_RESET_IMMEDIATE",()=>{zone.Tick(Sample(1,x,z));var r=zone.Tick(Sample(.25,x,z,0,false));Near(r.ProgressSeconds,0,"death reset");Need(!zone.IsReady&&r.Phase==HoldoutPhase.Waiting,"should reset");});
        Check("ZERO_TIME_NO_PROGRESS",()=>{var r=zone.Tick(Sample(0,x,z));Near(r.ProgressSeconds,0,"zero time");Need(!r.BecameReady,"can't complete at zero");});
        Check("BOUNDARY_RADIUS_INCLUSIVE",()=>{Need(zone.Inside(x+first.RadiusMeters,z),"boundary inclusive");Need(!zone.Inside(x+first.RadiusMeters+.001,z),"outside boundary");});
        Check("REQUIRED_TIME_COMPLETES_EXACTLY",()=>{
            var r=zone.Tick(Sample(first.RequiredSeconds,x,z));
            Need(r.Ready&&r.BecameReady&&zone.IsReady,"should complete at exact time");
            Near(r.Fraction,1,"ready fraction");});
        Check("COMPLETE_EVENT_ONCE",()=>{var r=zone.Tick(Sample(7,x,z));Need(r.Ready&&!r.BecameReady,"ready double-fired");});
        Check("READY_STAYS_READY_WHEN_CONTESTED_OUTSIDE_ALIVE",()=>{
            var r=zone.Tick(Sample(18,x+90,z,10,true));Need(r.Ready&&zone.IsReady&&!r.BecameReady,"completion must latch");});
        Check("DEATH_AFTER_READY_RESETS",()=>{
            var r=zone.Tick(Sample(1,x+90,z,10,false));
            Need(!r.Ready&&!zone.IsReady&&!r.BecameReady&&r.ProgressSeconds==0,"death must invalidate ready latch");
        });
        Check("RESET_REARMS_OBJECTIVE",()=>{zone.Reset();Need(!zone.IsReady&&zone.ProgressSeconds==0,"reset");var r=zone.Tick(Sample(1,x,z));Need(!r.Ready&&r.Phase==HoldoutPhase.Charging,"must recharge");});
        Check("LARGE_FRAME_CLAMPS_REQUIRED",()=>{
            zone.Reset();var r=zone.Tick(Sample(10000,x,z));Near(r.ProgressSeconds,first.RequiredSeconds,"cap progress");Need(r.Ready&&r.BecameReady,"event");});
        Check("TIME_PARTITION_EQUIVALENCE",()=>{
            var a=new HoldoutController(first);var b=new HoldoutController(first);
            a.Tick(Sample(4,x,z));
            for(int j=0;j<40;j++)b.Tick(Sample(.1,x,z));
            Near(a.ProgressSeconds,b.ProgressSeconds,"frame partition");
        });
        Check("TWO_INDEPENDENT_LEVEL_PROFILES",()=>{
            var ca=authentic[0].cfg;var cb=authentic[1].cfg;
            Need(ca.Id!=cb.Id&&ca.CenterX!=cb.CenterX&&ca.RequiredSeconds!=cb.RequiredSeconds,"two scenarios expected");
            var a=new HoldoutController(ca);var b=new HoldoutController(cb);
            var ar=a.Tick(Sample(ca.RequiredSeconds,ca.CenterX,ca.CenterZ));
            var br=b.Tick(Sample(ca.RequiredSeconds,cb.CenterX,cb.CenterZ));
            Need(ar.Ready&&!br.Ready,"second zone must not complete at shorter first duration");
        });
        Check("CONTEST_COUNT_ZERO_VS_POSITIVE",()=>{
            var a=new HoldoutController(first);
            var t0=a.Tick(Sample(2,x,z,0));
            var t1=a.Tick(Sample(2,x,z,1));
            Need(t0.ProgressSeconds==t1.ProgressSeconds&&t1.Contested&&!t0.Contested,"hostile count");
        });
        Check("OUTSIDE_DECAY_ZERO_CUSTOM_RULE",()=>{
            var c=new HoldoutConfig(first.Id,first.CenterX,first.CenterZ,first.RadiusMeters,first.RequiredSeconds,0);
            var a=new HoldoutController(c);
            a.Tick(Sample(2,x,z));a.Tick(Sample(60,x+20,z));
            Near(a.ProgressSeconds,2,"no decay allowed");
        });
        Check("REPLAY_REPRODUCIBLE_OUTPUT_SEQUENCE",()=>{
            var cmds=new[]{Sample(.5,x,z),Sample(2,x,z,1),Sample(1,x+8,z),Sample(1,x,z),Sample(10,x,z)};
            var a=new HoldoutController(first);var b=new HoldoutController(first);
            var aa=cmds.Select(s=>a.Tick(s)).ToArray();
            var bb=cmds.Select(s=>b.Tick(s)).ToArray();
            Need(aa.SequenceEqual(bb),"same samples must create identical snapshots");
        });
        var prior=zone.ProgressSeconds;
        Reject("INVALID_NEGATIVE_DELTA",()=>zone.Tick(Sample(-.1,x,z)));
        Reject("INVALID_NAN_DELTA",()=>zone.Tick(Sample(double.NaN,x,z)));
        Reject("INVALID_INFINITE_DELTA",()=>zone.Tick(Sample(double.PositiveInfinity,x,z)));
        Reject("INVALID_NAN_PLAYER_X",()=>zone.Tick(Sample(1,double.NaN,z)));
        Reject("INVALID_INFINITY_PLAYER_Z",()=>zone.Tick(Sample(1,x,double.PositiveInfinity)));
        Reject("INVALID_HOSTILE_COUNT",()=>zone.Tick(Sample(1,x,z,-1)));
        Check("REJECTS_DO_NOT_MUTATE_PROGRESS",()=>Near(zone.ProgressSeconds,prior,"invalid sample state atomic"));
        Reject("INVALID_ZERO_RADIUS_CONFIG",()=>new HoldoutController(new HoldoutConfig(first.Id,first.CenterX,first.CenterZ,0,first.RequiredSeconds,first.OutsideDecayPerSecond)));
        Reject("INVALID_INFINITE_RADIUS_CONFIG",()=>new HoldoutController(new HoldoutConfig(first.Id,first.CenterX,first.CenterZ,double.PositiveInfinity,first.RequiredSeconds,first.OutsideDecayPerSecond)));
        Reject("INVALID_NEGATIVE_REQUIRED_CONFIG",()=>new HoldoutController(new HoldoutConfig(first.Id,first.CenterX,first.CenterZ,first.RadiusMeters,-1,first.OutsideDecayPerSecond)));
        Reject("INVALID_NEGATIVE_DECAY_CONFIG",()=>new HoldoutController(new HoldoutConfig(first.Id,first.CenterX,first.CenterZ,first.RadiusMeters,first.RequiredSeconds,-1)));
        Reject("INVALID_NAN_CENTER_CONFIG",()=>new HoldoutController(new HoldoutConfig(first.Id,first.CenterX,double.NaN,first.RadiusMeters,first.RequiredSeconds,first.OutsideDecayPerSecond)));
        Reject("INVALID_EMPTY_ID_CONFIG",()=>new HoldoutController(new HoldoutConfig("",first.CenterX,first.CenterZ,first.RadiusMeters,first.RequiredSeconds,first.OutsideDecayPerSecond)));
        Check("NO_UNBOUNDED_GLOBAL_GAME_MUTATION",()=>{
            // A challenge only computes its own state. No event is sent to MapFinish, battle or reward.
            Need(typeof(HoldoutController).GetEvents().Length==0,"Unexpected global event/economy coupling");
        });
        Console.WriteLine($"RESULT PASS: {successes}/{successes} original C# challenge assertions, including {negativeCases} malformed-input rejection cases, 2 original MapDocument v2 source blobs. UNITY_NATIVE/PHYSX/CODEX_INTEGRATION NOT_RUN");
        var receipt=new{packageId="B01-25-HOLDOUT-001/R1",testsPassed=successes,negativeInputCases=negativeCases,
            status="CSharp Original Rule Logic PASS / Unity Native NOT_RUN",
            mapInputs=authentic.Select(v=>new{id=v.name,blob=v.mapBlob,centerX=v.cfg.CenterX,centerZ=v.cfg.CenterZ,radius=v.cfg.RadiusMeters}).ToArray(),
            tests=receipts};
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[3]))!);
        File.WriteAllText(args[3],JsonSerializer.Serialize(receipt,new JsonSerializerOptions{WriteIndented=true})+"\n");
    }
}
