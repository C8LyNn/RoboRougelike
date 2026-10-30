using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text.Json;
using B01.LevelChallenges.SweepOrb;
static class Tests
{
    static int pass,neg;
    static readonly List<string> names=new();
    static void Need(bool ok,string why){if(!ok)throw new Exception(why);}
    static void Close(double a,double b){Need(Math.Abs(a-b)<1e-7,"wrong value "+a+" vs "+b);}
    static void Case(string name,Action action){try{action();pass++;names.Add(name);Console.WriteLine("PASS "+name);}catch(Exception e){Console.Error.WriteLine("FAIL "+name+" "+e);throw;}}
    static void Reject(string name,Action action){Case("NEGATIVE_"+name,()=>{bool thrown=false;try{action();}catch(ArgumentException){thrown=true;}Need(thrown,"should reject");});neg++;}
    static SweepConfig C(double offset=0)=>new("TEST",28,40,36,40,1,1,2,3,offset);
    static SweepSample S(double dt,double x0,double z0,double x1,double z1,bool alive=true,bool paused=false)=>new(dt,x0,z0,x1,z1,alive,paused);
    static SweepResult Step(SweepController c,int n,double dt,double x,double z)
    {SweepResult r=default;for(int i=0;i<n;i++)r=c.Advance(S(dt,x,z,x,z));return r;}
    static string GitBlob(byte[] raw)
    {var h=Encoding.UTF8.GetBytes("blob "+raw.Length+"\0");var all=new byte[h.Length+raw.Length];Buffer.BlockCopy(h,0,all,0,h.Length);Buffer.BlockCopy(raw,0,all,h.Length,raw.Length);return Convert.ToHexString(SHA1.HashData(all)).ToLowerInvariant();}
    static void Map(JsonElement p,string path,string sha,int n)
    {
        string id=p.GetProperty("levelId").GetString()!;
        byte[] data=File.ReadAllBytes(path);
        Case(id+"_GIT_OBJECT",()=>Need(GitBlob(data)==sha&&p.GetProperty("mapGitBlob").GetString()==sha,"map altered"));
        using var doc=JsonDocument.Parse(data);
        var items=doc.RootElement.GetProperty("items").EnumerateArray().ToArray();
        Case(id+"_ACTUAL_MAPDOC",()=>Need(doc.RootElement.GetProperty("version").GetInt32()==2&&items.Length==n,"not original"));
        var floor=new HashSet<string>(items.Where(x=>x.GetProperty("kind").GetInt32()==0).Select(x=>x.GetProperty("x").GetInt32()+","+x.GetProperty("z").GetInt32()));
        var blocked=new HashSet<string>(items.Where(x=>x.GetProperty("kind").GetInt32()!=0).Select(x=>x.GetProperty("x").GetInt32()+","+x.GetProperty("z").GetInt32()));
        var o=p.GetProperty("orb");int x=o.GetProperty("fromCell")[0].GetInt32(),z=o.GetProperty("fromCell")[1].GetInt32(),x1=o.GetProperty("toCell")[0].GetInt32(),z1=o.GetProperty("toCell")[1].GetInt32();
        Case(id+"_REAL_3_CELL_RAIL",()=>{Need(x1-x==2&&z==z1,"bad rail");for(int k=x;k<=x1;k++)Need(floor.Contains(k+","+z)&&!blocked.Contains(k+","+z),"obstructed cell");});
        Case(id+"_WORLD_SCALE_AND_SPAWN_FINISH",()=>{
            Close(o.GetProperty("fromWorld")[0].GetDouble(),x*4);Close(o.GetProperty("fromWorld")[1].GetDouble(),z*4);
            Close(o.GetProperty("toWorld")[0].GetDouble(),x1*4);Close(o.GetProperty("toWorld")[1].GetDouble(),z1*4);
            foreach(var i in items.Where(v=>v.GetProperty("kind").GetInt32()==4||v.GetProperty("kind").GetInt32()==5))
                Need(i.GetProperty("z").GetInt32()!=z||i.GetProperty("x").GetInt32()<x||i.GetProperty("x").GetInt32()>x1,"spawn finish overlap");
        });
    }
    static void Main(string[] args)
    {
        Need(args.Length==4,"four arguments required");
        var c=new SweepController(C());
        Case("PHASES_AT_REAL_BOUNDARIES",()=>Need(c.StateAt(0)==SweepState.Telegraph&&c.StateAt(.999999)==SweepState.Telegraph&&c.StateAt(1)==SweepState.Moving&&c.StateAt(2.999999)==SweepState.Moving&&c.StateAt(3)==SweepState.SafeReturn&&c.StateAt(6)==SweepState.Telegraph,"wrong phase"));
        Case("MOVING_CENTER_REAL_POSITION",()=>{c.OrbPosition(1,out double x,out double z);Close(x,28);c.OrbPosition(2,out x,out z);Close(x,32);c.OrbPosition(3,out x,out z);Close(x,36);Close(z,40);});
        Case("TELEGRAPH_NEVER_DAMAGES",()=>{var q=new SweepController(C());Need(!Step(q,3,.25,28,40).DamageRequest,"damage in warning");});
        Case("FIRST_SWEEP_SINGLE_DAMAGE",()=>{var q=new SweepController(C());Need(Step(q,4,.25,28,40).DamageRequest,"no first hit");Need(!q.Advance(S(0,28,40,28,40)).DamageRequest,"double hit");});
        Case("PLAYER_CROSSING_BETWEEN_HOST_SAMPLES",()=>{var q=new SweepController(C());Step(q,5,.25,100,40);var r=q.Advance(S(.25,10,40,50,40));Need(r.Contact&&r.DamageRequest,"tunnel through orb");});
        Case("PARALLEL_SAFE_PATH",()=>{var q=new SweepController(C());Step(q,5,.25,100,40);Need(!q.Advance(S(.25,10,44,50,44)).DamageRequest,"false hit");});
        Case("NEXT_ACTIVATION_REARMS",()=>{var q=new SweepController(C());Need(Step(q,4,.25,28,40).DamageRequest,"first");Step(q,20,.25,36,40);Need(Step(q,4,.25,28,40).DamageRequest,"second");});
        Case("PAUSED_HAZARD_FROZEN",()=>{var q=new SweepController(C());Step(q,4,.25,100,40);double at=q.Elapsed;for(int i=0;i<5;i++)Need(!q.Advance(S(.25,28,40,28,40,true,true)).DamageRequest,"paused grant");Close(q.Elapsed,at);Need(q.Advance(S(0,28,40,28,40)).DamageRequest,"resume");});
        Case("DEAD_ROBOT_SAFE",()=>{var q=new SweepController(C());Step(q,4,.25,100,40);Need(!q.Advance(S(.1,28,40,29,40,false)).DamageRequest,"dead hit");});
        Case("RESET_REARMS",()=>{var q=new SweepController(C());Step(q,4,.25,28,40);q.Reset();Close(q.Elapsed,0);Need(Step(q,4,.25,28,40).DamageRequest,"no reset grant");});
        Case("OFFSET_ALTERNATE_SWEEP",()=>{var q=new SweepController(C(3));Need(q.StateAt(0)==SweepState.SafeReturn,"offset");Need(Step(q,16,.25,28,40).DamageRequest,"offset no grant");});
        Case("DECIMAL_TIMESTEP_PARTITIONS",()=>{foreach(var p in new (int,double)[]{(4,.25),(8,.125),(10,.1),(20,.05),(40,.025)}){var q=new SweepController(C());Need(Step(q,p.Item1,p.Item2,28,40).DamageRequest,"partition "+p.Item1);}});
        Case("RADIUS_BOUNDARY_INCLUSIVE",()=>{var q=new SweepController(C());Step(q,4,.25,100,40);Need(q.Advance(S(0,28,41,28,41)).DamageRequest,"edge");var b=new SweepController(C());Step(b,4,.25,100,40);Need(!b.Advance(S(0,28,41.001,28,41.001)).DamageRequest,"outside");});
        Case("CONTROLLERS_ISOLATED",()=>{var q=new SweepController(C());var b=new SweepController(C());Step(q,4,.25,28,40);Close(b.Elapsed,0);});
        var invalid=new SweepController(C());
        Reject("NAN_DT",()=>invalid.Advance(S(double.NaN,0,0,0,0)));
        Reject("NEGATIVE_DT",()=>invalid.Advance(S(-.1,0,0,0,0)));
        Reject("OVERSIZED_DT",()=>invalid.Advance(S(.251,0,0,0,0)));
        Reject("NAN_POSITION",()=>invalid.Advance(S(.1,double.NaN,0,0,0)));
        Reject("INFINITE_POSITION",()=>invalid.Advance(S(.1,0,0,double.PositiveInfinity,0)));
        Reject("ZERO_RADIUS",()=>new SweepController(new("BAD",0,0,1,0,0,1,2,3,0)));
        Reject("ZERO_RAIL",()=>new SweepController(new("BAD",0,0,0,0,1,1,2,3,0)));
        Reject("INVALID_OFFSET",()=>new SweepController(new("BAD",0,0,1,0,1,1,2,3,6)));
        Reject("INVALID_WARNING",()=>new SweepController(new("BAD",0,0,1,0,1,0,2,3,0)));
        Reject("INVALID_PREVIEW_TIME",()=>invalid.StateAt(-1));
        Case("INVALID_INPUTS_ATOMIC",()=>Close(invalid.Elapsed,0));
        using var json=JsonDocument.Parse(File.ReadAllText(args[0]));
        var levels=json.RootElement.GetProperty("levels").EnumerateArray().ToArray();Need(levels.Length==2,"expected two original maps");
        Map(levels[0],args[1],"53eb696950121465befea2855eda4d7268750016",415);
        Map(levels[1],args[2],"013f5fe2058e76233f580a439e8402c0fe09f124",360);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[3]))!);
        File.WriteAllText(args[3],JsonSerializer.Serialize(new{package="B01-25-SWEEPORB001/R1",passed=pass,negative=neg,tests=names,unity="NOT_RUN"},new JsonSerializerOptions{WriteIndented=true})+"\n");
        Console.WriteLine("RESULT PASS "+pass+"/"+pass+"; "+neg+" explicit negatives; Unity native NOT_RUN");
    }
}
