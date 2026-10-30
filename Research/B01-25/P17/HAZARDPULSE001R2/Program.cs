// Executable ORIGINAL gameplay source proof; NO Unity/PhysX/game integration claim.
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using System.Security.Cryptography;
using B01.LevelChallenges;

static class Proof
{
    static int passed, rejected;
    static readonly List<string> names = new();
    static void Need(bool condition,string why){if(!condition)throw new Exception(why);}
    static void Close(double a,double b,string why)
    {Need(Math.Abs(a-b)<1e-8,why+" actual "+a+" expected "+b);}
    static void Test(string name,Action action)
    {
        try {action(); passed++; names.Add(name); Console.WriteLine("PASS "+name);}
        catch(Exception ex){Console.Error.WriteLine("FAIL "+name+": "+ex);throw;}
    }
    static void Reject(string name,Action action)
    {
        Test("NEGATIVE_"+name,()=>{
            bool failed=false;
            try{action();}catch(ArgumentException){failed=true;}
            Need(failed,"Expected explicit ArgumentException");
        });
        rejected++;
    }
    static string Blob(byte[] raw)
    {
        byte[] prefix=Encoding.UTF8.GetBytes("blob "+raw.Length+"\0");
        var data=new byte[prefix.Length+raw.Length];
        Buffer.BlockCopy(prefix,0,data,0,prefix.Length);
        Buffer.BlockCopy(raw,0,data,prefix.Length,raw.Length);
        return Convert.ToHexString(SHA1.HashData(data)).ToLowerInvariant();
    }
    static string String(JsonElement e,string name)=>e.GetProperty(name).GetString()!;
    static double Number(JsonElement e,string name)=>e.GetProperty(name).GetDouble();
    static int Int(JsonElement e,string name)=>e.GetProperty(name).GetInt32();
    static int[] Pair(JsonElement e,string name)=>e.GetProperty(name).EnumerateArray().Select(v=>v.GetInt32()).ToArray();
    static string Key(int x,int z)=>x+","+z;
    static PulseZoneConfig Make(JsonElement v)
    {
        double[] world=v.GetProperty("world").EnumerateArray().Select(x=>x.GetDouble()).ToArray();
        return new PulseZoneConfig(String(v,"id"),world[0],world[1],
            Number(v,"radius"),Number(v,"warning"),Number(v,"active"),
            Number(v,"recovery"),Number(v,"offset"));
    }
    static PulseSample Sample(double dt,double x,double z,bool alive=true,bool paused=false)
        => new(dt,x,z,alive,paused);
    static PulseSnapshot Step(PulseZoneController zone,int count,double dt,double x,double z)
    {
        PulseSnapshot result=default;
        for(int i=0;i<count;i++)result=zone.Advance(Sample(dt,x,z));
        return result;
    }
    static void VerifyMap(JsonElement p,byte[] raw,string expectedHash,int expectedItems)
    {
        string id=String(p,"levelId");
        Test(id+"_ORIGINAL_BYTE_HASH",()=>{
            Need(Blob(raw)==expectedHash && String(p,"mapGitBlob")==expectedHash,
                "original map byte hash must be pinned");
        });
        using var json=JsonDocument.Parse(raw);
        var map=json.RootElement;
        var items=map.GetProperty("items").EnumerateArray().ToArray();
        Test(id+"_MAPDOC_V2",()=>{
            Need(Int(map,"version")==2 && !map.GetProperty("globalFloor").GetBoolean(),
                "wrong MapDocument version");
            Need(items.Length==expectedItems,"unexpected original map item count");
        });
        var floor=new HashSet<string>(items.Where(i=>Int(i,"kind")==0).Select(i=>Key(Int(i,"x"),Int(i,"z"))));
        var blockers=new HashSet<string>(items.Where(i=>Int(i,"kind")==1).Select(i=>Key(Int(i,"x"),Int(i,"z"))));
        var spawn=items.Single(i=>Int(i,"kind")==4);
        var finish=items.Single(i=>Int(i,"kind")==5);
        var zones=p.GetProperty("zones").EnumerateArray().ToArray();
        Need(zones.Length==2,"two independent alternated hazards required");
        var a=Make(zones[0]);var b=Make(zones[1]);
        Test(id+"_ZONE_CONFIG_VALID",()=>{a.Validate();b.Validate();});
        Test(id+"_CENTERS_GROUND_AND_UNOCCUPIED",()=>{
            foreach(var zone in zones){
                int[] c=Pair(zone,"cell");
                var key=Key(c[0],c[1]);
                Need(c.Length==2&&floor.Contains(key),"missing original floor center");
                Need(items.All(i=>Int(i,"x")!=c[0]||Int(i,"z")!=c[1]||Int(i,"kind")==0),
                    "authored hazard intersects an existing item");
                double[] world=zone.GetProperty("world").EnumerateArray().Select(x=>x.GetDouble()).ToArray();
                Close(world[0],c[0]*4,"world x must match original grid");
                Close(world[1],c[1]*4,"world z must match original grid");
                double dx=world[0]-Int(spawn,"x")*4,dz=world[1]-Int(spawn,"z")*4;
                Need(Math.Sqrt(dx*dx+dz*dz)>a.RadiusMeters+2,
                    "hazard overlaps spawn source center envelope");
                dx=world[0]-Int(finish,"x")*4;dz=world[1]-Int(finish,"z")*4;
                Need(Math.Sqrt(dx*dx+dz*dz)>a.RadiusMeters+2,
                    "hazard overlaps finish source center envelope");
            }
        });
        Test(id+"_NONOVERLAPPING_ALTERNATING_LANES",()=>{
            double dx=a.CenterX-b.CenterX,dz=a.CenterZ-b.CenterZ;
            Need(Math.Sqrt(dx*dx+dz*dz)>a.RadiusMeters+b.RadiusMeters,
                "hazards intersect each other");
            Close(a.PeriodSeconds,b.PeriodSeconds,"different hazard clocks");
            Close(b.OffsetSeconds-a.OffsetSeconds,a.PeriodSeconds/2,
                "not staggered");
            var left=new PulseZoneController(a);
            var right=new PulseZoneController(b);
            for(int tick=0;tick<6*8*4;tick++){
                double t=tick*.25;
                Need(left.PhaseAt(t)!=PulsePhase.Active||right.PhaseAt(t)!=PulsePhase.Active,
                    "both crossing options active simultaneously");
            }
        });
        Test(id+"_STATIC_SPAWN_FINISH_REACHABLE",()=>{
            string start=Key(Int(spawn,"x"),Int(spawn,"z"));
            string goal=Key(Int(finish,"x"),Int(finish,"z"));
            var open=new Queue<string>();open.Enqueue(start);
            var reached=new HashSet<string>{start};
            int[] ox={1,-1,0,0},oz={0,0,1,-1};
            while(open.Count>0){
                var at=open.Dequeue();var parts=at.Split(',');int x=int.Parse(parts[0]),z=int.Parse(parts[1]);
                for(int k=0;k<4;k++){
                    var next=Key(x+ox[k],z+oz[k]);
                    if(floor.Contains(next)&&!blockers.Contains(next)&&reached.Add(next))
                        open.Enqueue(next);
                }
            }
            Need(reached.Contains(goal),"baseline original route softlocked");
        });
        Test(id+"_BOTH_HAZARDS_HAVE_WARNING",()=>{
            var z0=new PulseZoneController(a);var z1=new PulseZoneController(b);
            Need(z0.PhaseAt(0)==PulsePhase.Warning,"first hazard must telegraph");
            Need(z1.PhaseAt(0)==PulsePhase.Recovery,"other hazard must be safe");
        });
    }
    static void Main(string[] args)
    {
        if(args.Length!=4){Console.Error.WriteLine("Usage: profiles.json clearance.json slalom.json receipt.json");Environment.ExitCode=2;return;}
        using var data=JsonDocument.Parse(File.ReadAllText(args[0]));
        var levels=data.RootElement.GetProperty("levels").EnumerateArray().ToArray();
        Need(levels.Length==2,"expected 2 actual map profile links");
        VerifyMap(levels[0],File.ReadAllBytes(args[1]),"53eb696950121465befea2855eda4d7268750016",415);
        VerifyMap(levels[1],File.ReadAllBytes(args[2]),"013f5fe2058e76233f580a439e8402c0fe09f124",360);
        var cfg=Make(levels[0].GetProperty("zones")[0]);
        var x=cfg.CenterX;var z=cfg.CenterZ;
        var zone=new PulseZoneController(cfg);
        Test("INITIAL_WARNING_NOT_DANGEROUS",()=>{
            var r=zone.Advance(Sample(.25,x,z));
            Need(r.Phase==PulsePhase.Warning&&!r.Exposed&&!r.DamagePulse,"telegraph must be safe");
            Close(zone.ElapsedSeconds,.25,"initial clock");
        });
        Test("WARN_THEN_ACTIVE_EXACT_BOUNDARY",()=>{
            Need(zone.PhaseAt(.999999)==PulsePhase.Warning,"warn before 1s");
            Need(zone.PhaseAt(1)==PulsePhase.Active,"active from 1s");
            Need(zone.PhaseAt(3)==PulsePhase.Recovery,"recovery from 3s");
            Need(zone.PhaseAt(6)==PulsePhase.Warning,"warning next cycle");
        });
        Test("ACTIVE_START_ONE_DAMAGE_AUTHORIZATION",()=>{
            var r=Step(zone,3,.25,x,z);
            Need(r.Phase==PulsePhase.Active&&r.Exposed&&r.DamagePulse,"one-shot enter active");
            Close(zone.ElapsedSeconds,1,"activation time");
        });
        Test("ACTIVE_HOLD_NO_REPEAT_PER_FRAME",()=>{
            var r=Step(zone,2,.25,x,z);
            Need(r.Exposed&&!r.DamagePulse,"standing inside must not repeatedly hit");
        });
        Test("SAME_CYCLE_REENTRY_NO_REPEAT_DAMAGE",()=>{
            zone.Advance(Sample(.25,x+10,z));
            var r=zone.Advance(Sample(.25,x,z));
            Need(r.Exposed&&!r.DamagePulse,"leave/reenter same activation must not redamage");
        });
        Test("RECOVERY_NOT_EXPOSED",()=>{
            var r=Step(zone,4,.25,x,z);
            Need(r.Phase==PulsePhase.Recovery&&!r.Exposed&&!r.DamagePulse,
                "recovery should not emit damage");
        });
        Test("NEXT_CYCLE_REARMS_ONE_SHOT",()=>{
            var next=new PulseZoneController(cfg);
            var first=Step(next,4,.25,x,z);
            Need(first.DamagePulse,"first cycle must hit once");
            var safe=Step(next,20,.25,x,z);
            Need(safe.Phase==PulsePhase.Warning&&!safe.DamagePulse,
                "new-cycle warning is safe");
            var second=Step(next,4,.25,x,z);
            Need(second.Phase==PulsePhase.Active&&second.DamagePulse,
                "new cycle must re-arm once");
            Need(!next.Advance(Sample(.25,x,z)).DamagePulse,
                "same second cycle cannot repeat damage");
        });
        Test("RESET_REARMS_AFTER_FINISH",()=>{
            zone.Reset();Need(zone.ElapsedSeconds==0,"reset clock");
            var r=Step(zone,4,.25,x,z);
            Need(r.DamagePulse&&r.Phase==PulsePhase.Active,"first activation after reset should trigger");
        });
        Test("PAUSED_CLOCK_IS_FROZEN",()=>{
            var before=zone.ElapsedSeconds;
            var r=zone.Advance(Sample(.25,x,z,true,true));
            Close(zone.ElapsedSeconds,before,"pause must not advance clock");
            Need(!r.DamagePulse,"no pulse event while paused");
        });
        Test("PAUSED_ENTRY_IGNORES_DAMAGE_UNTIL_RESUME",()=>{
            var tmp=new PulseZoneController(cfg);
            Step(tmp,4,.25,x+10,z);
            var paused=tmp.Advance(Sample(.25,x,z,true,true));
            Need(paused.Exposed&&!paused.DamagePulse,"pause must not trigger damage");
            var resumed=tmp.Advance(Sample(0,x,z));
            Need(resumed.DamagePulse,"resume while standing in active hazard must trigger once");
        });
        Test("DEAD_ROBOT_NO_DAMAGE",()=>{
            var tmp=new PulseZoneController(cfg);
            var dead=Step(tmp,3,.25,x,z);
            dead=tmp.Advance(Sample(.25,x,z,false));
            Need(!dead.Exposed&&!dead.DamagePulse,"dead robots do not receive pulses");
        });
        Test("SAFE_OUTSIDE_ACTIVE_HAZARD",()=>{
            var tmp=new PulseZoneController(cfg);
            var far=Step(tmp,4,.25,x+10,z);
            Need(far.Phase==PulsePhase.Active&&!far.Exposed&&!far.DamagePulse,"out of zone safe");
        });
        Test("ENTER_ACTIVE_HAZARD_TRIGGERS_ONCE",()=>{
            var tmp=new PulseZoneController(cfg);
            Step(tmp,4,.25,x+10,z);
            var entered=tmp.Advance(Sample(0,x,z));
            Need(entered.DamagePulse&&entered.Exposed,"must trigger on active zone entry");
            Need(!tmp.Advance(Sample(0,x,z)).DamagePulse,"0dt cannot double hit");
        });
        Test("BOUNDARY_IS_INCLUSIVE",()=>{
            Need(zone.Contains(x+cfg.RadiusMeters,z),"boundary should count");
            Need(!zone.Contains(x+cfg.RadiusMeters+.0001,z),"outside must not count");
        });
        Test("PHASE_REMAINING_COUNTDOWN",()=>{
            var tmp=new PulseZoneController(cfg);
            var r=tmp.Advance(Sample(.25,x+10,z));
            Close(r.PhaseRemainingSeconds,.75,"warning countdown");
            r=Step(tmp,3,.25,x+10,z);
            Close(r.PhaseRemainingSeconds,2,"active countdown");
        });
        Test("TIME_PARTITION_DETERMINISM",()=>{
            var a=new PulseZoneController(cfg);
            var b=new PulseZoneController(cfg);
            Step(a,16,.25,x,z);
            Step(b,32,.125,x,z);
            Close(a.ElapsedSeconds,b.ElapsedSeconds,"same clock");
            Need(a.PhaseAt(a.ElapsedSeconds)==b.PhaseAt(b.ElapsedSeconds),"same phase");
        });
        Test("QA444_R2_1S_FRAME_PARTITIONS_AND_ONESHOT",()=>{
            foreach(var pair in new (int steps,double dt)[]{(4,.25),(5,.2),(8,.125),(10,.1),(20,.05),(40,.025),(100,.01)})
            {
                var p=new PulseZoneController(cfg);
                PulseSnapshot r=default;int pulses=0;
                for(int i=0;i<pair.steps;i++){
                    r=p.Advance(Sample(pair.dt,x,z));
                    if(r.DamagePulse)pulses++;
                }
                Need(r.Phase==PulsePhase.Active&&r.Exposed&&r.DamagePulse&&pulses==1,
                    "partition failed at 1s: "+pair.steps+" x "+pair.dt);
                Need(!p.Advance(Sample(0,x,z)).DamagePulse,"duplicate 0dt pulse");
            }
        });
        Test("QA444_R2_3S_6S_7S_FRAME_PARTITIONS",()=>{
            foreach(double dt in new double[]{.25,.2,.125,.1,.05,.025,.01})
            {
                var p=new PulseZoneController(cfg);
                int steps=(int)Math.Round(3/dt);
                var at3=Step(p,steps,dt,x,z);
                Need(at3.Phase==PulsePhase.Recovery&&!at3.DamagePulse,"3s "+dt);
                var at6=Step(p,steps,dt,x,z);
                Need(at6.Phase==PulsePhase.Warning&&!at6.DamagePulse,"6s "+dt);
                var at7=Step(p,(int)Math.Round(1/dt),dt,x,z);
                Need(at7.Phase==PulsePhase.Active&&at7.DamagePulse,"7s "+dt);
                Need(!p.Advance(Sample(0,x,z)).DamagePulse,"0dt re-pulse "+dt);
            }
        });
        Test("QA444_R2_NO_EARLY_MS_BOUNDARY",()=>{
            var p=new PulseZoneController(cfg);
            Need(p.PhaseAt(.999999)==PulsePhase.Warning,"premature activation");
            Need(p.PhaseAt(2.999999)==PulsePhase.Active,"premature recovery");
            Need(p.PhaseAt(5.999999)==PulsePhase.Recovery,"premature wrap");
        });
        Test("QA444_R2_OFFSET_4S_PARTITIONS",()=>{
            var offset=Make(levels[0].GetProperty("zones")[1]);
            foreach(var pair in new (int steps,double dt)[]{(16,.25),(40,.1),(80,.05),(400,.01)})
            {
                var p=new PulseZoneController(offset);
                var r=Step(p,pair.steps,pair.dt,offset.CenterX,offset.CenterZ);
                Need(r.Phase==PulsePhase.Active&&r.DamagePulse,
                    "offset partition "+pair.steps+" x "+pair.dt);
                Need(!p.Advance(Sample(0,offset.CenterX,offset.CenterZ)).DamagePulse,
                    "offset zero delta double hit");
            }
        });
        Test("STATE_IS_INSTANCE_ISOLATED",()=>{
            var a=new PulseZoneController(cfg);
            var b=new PulseZoneController(cfg);
            Step(a,8,.25,x,z);
            Close(b.ElapsedSeconds,0,"zone B not mutated by A");
        });
        Test("OFFSETED_SECOND_ZONE_SAFE_AT_FIRST_ACTIVE",()=>{
            var second=Make(levels[0].GetProperty("zones")[1]);
            var other=new PulseZoneController(second);
            Need(zone.PhaseAt(1)==PulsePhase.Active&&other.PhaseAt(1)==PulsePhase.Recovery,
                "staggered lane not safe when first active");
        });
        var invalidZone=new PulseZoneController(cfg);
        Reject("NEGATIVE_DELTA",()=>invalidZone.Advance(Sample(-.1,x,z)));
        Reject("NAN_DELTA",()=>invalidZone.Advance(Sample(double.NaN,x,z)));
        Reject("INFINITE_DELTA",()=>invalidZone.Advance(Sample(double.PositiveInfinity,x,z)));
        Reject("EXCESSIVE_DELTA_MUST_SUBSTEP",()=>invalidZone.Advance(Sample(.25000001,x,z)));
        Reject("NAN_PLAYER_X",()=>invalidZone.Advance(Sample(.1,double.NaN,z)));
        Reject("INFINITE_PLAYER_Z",()=>invalidZone.Advance(Sample(.1,x,double.PositiveInfinity)));
        Test("INVALID_INPUTS_ATOMIC",()=>Close(invalidZone.ElapsedSeconds,0,"no invalid telemetry should advance clock"));
        Reject("EMPTY_ID",()=>new PulseZoneController(new("",x,z,1.35,1,2,3,0)));
        Reject("ZERO_RADIUS",()=>new PulseZoneController(new("TEST",x,z,0,1,2,3,0)));
        Reject("NAN_CENTER",()=>new PulseZoneController(new("TEST",double.NaN,z,1.35,1,2,3,0)));
        Reject("NO_WARNING",()=>new PulseZoneController(new("TEST",x,z,1.35,0,2,3,0)));
        Reject("NEGATIVE_ACTIVE",()=>new PulseZoneController(new("TEST",x,z,1.35,1,-2,3,0)));
        Reject("ZERO_RECOVERY",()=>new PulseZoneController(new("TEST",x,z,1.35,1,2,0,0)));
        Reject("OFFSET_AT_PERIOD",()=>new PulseZoneController(new("TEST",x,z,1.35,1,2,3,6)));
        Reject("NEGATIVE_OFFSET",()=>new PulseZoneController(new("TEST",x,z,1.35,1,2,3,-1)));
        Reject("INFINITE_RADIUS",()=>new PulseZoneController(new("TEST",x,z,double.PositiveInfinity,1,2,3,0)));
        Reject("INVALID_ABSOLUTE_TIME",()=>invalidZone.PhaseAt(-1));
        Reject("NAN_ABSOLUTE_TIME",()=>invalidZone.PhaseAt(double.NaN));
        Test("NO_UNAUTHORIZED_DAMAGE_SIDE_EFFECT",()=>{
            Need(typeof(PulseZoneController).GetEvents().Length==0,
                "rule core must not call host game damage or rewards");
        });
        Console.WriteLine("RESULT PASS "+passed+"/"+passed+
            " real original C# hazard rules/map checks ("+rejected+
            " negative rejects); CSHARP9_COMPILER separate; UNITY_NATIVE_NOT_RUN");
        var receipt=new {packageId="B01-25-HAZARDPULSE-001/R2",passed,rejected,
            state="PRODUCER_CSHARP9_SOURCE_TESTED; UNITY_NATIVE_NOT_RUN",
            sourceMapBlobs=new []{"53eb696950121465befea2855eda4d7268750016",
                "013f5fe2058e76233f580a439e8402c0fe09f124"},
            checks=names};
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[3]))!);
        File.WriteAllText(args[3],JsonSerializer.Serialize(receipt,new JsonSerializerOptions{WriteIndented=true})+"\n");
    }
}