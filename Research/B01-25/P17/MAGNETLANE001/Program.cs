using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using B01.LevelChallenges.MagnetLane;
static class OriginalTests
{
    static int n,negative;
    static readonly List<string> names=new();
    static void Check(bool yes,string msg){if(!yes)throw new Exception(msg);}
    static void Eq(double a,double b,string msg){Check(Math.Abs(a-b)<1e-9,msg+": "+a+" != "+b);}
    static void Case(string id,Action f){try{f();n++;names.Add(id);Console.WriteLine("PASS "+id);}catch(Exception e){Console.Error.WriteLine("FAIL "+id+" "+e);throw;}}
    static void Reject(string id,Action act){Case("NEGATIVE_"+id,()=>{bool denied=false;try{act();}catch(ArgumentException){denied=true;}Check(denied,id);});negative++;}
    static MagnetLaneController Lane()=>new(new MagnetConfig(.6,2.4,1,60,3,1.5));
    static MagnetOutput Steps(MagnetLaneController c,int times,double dt,double x=0,double z=0,double kg=1.5,bool paused=false,bool alive=true)
    {MagnetOutput o=default;for(int i=0;i<times;i++)o=c.Advance(new MagnetSample(dt,x,z,kg,paused,alive));return o;}
    static void Main(string[] args)
    {
        Check(args.Length==1,"one output receipt required");
        Case("INITIAL_WARNING_NO_FORCE",()=>{var c=Lane();var o=Steps(c,5,.1);Check(o.Phase==MagnetPhase.Warning&&o.ForceZ==0,"warning");});
        Case("ACTIVATION_AFTER_600MS",()=>{var c=Lane();Steps(c,6,.1);var o=Steps(c,1,.1);Check(o.Phase==MagnetPhase.Active&&o.ForceZ>0,"active");Eq(o.ForceZ,60,"maximum force");});
        Case("HALF_FRAME_ACTIVATION_INTEGRATED",()=>{var c=Lane();Steps(c,11,.05);var o=c.Advance(new MagnetSample(.1,0,0,1.5));Eq(o.ForceZ,30,"half .1 active");Eq(o.ActiveFraction,.5,"half active");});
        Case("LIGHT_ACTUAL_ACCELERATION",()=>{var c=Lane();Steps(c,6,.1);var o=c.Advance(new MagnetSample(.1,0,0,1.5));Eq(o.AccelerationZ,40,"light m/s2");});
        Case("HEAVY_ACTUAL_ACCELERATION",()=>{var c=Lane();Steps(c,6,.1);var o=c.Advance(new MagnetSample(.1,0,0,12));Eq(o.AccelerationZ,5,"heavy m/s2");});
        Case("HEAVY_EIGHTFOLD_RESISTANCE",()=>{var a=Lane();var b=Lane();Steps(a,6,.1);Steps(b,6,.1);var x=a.Advance(new MagnetSample(.1,0,0,1.5));var y=b.Advance(new MagnetSample(.1,0,0,12));Eq(x.ForceZ,y.ForceZ,"same Newtons");Eq(x.AccelerationZ/y.AccelerationZ,8,"mass ratio");});
        Case("EDGE_TAPERS_TO_ZERO",()=>{var c=Lane();Steps(c,6,.1);var o=c.Advance(new MagnetSample(.1,0,1.5,1.5));Eq(o.ForceZ,0,"edge");});
        Case("HALF_WIDE_TAPER",()=>{var c=Lane();Steps(c,6,.1);var o=c.Advance(new MagnetSample(.1,0,.75,1.5));Eq(o.ForceZ,30,"half strength");});
        Case("OUTSIDE_X_NO_FORCE",()=>{var c=Lane();Steps(c,6,.1);var o=c.Advance(new MagnetSample(.1,4,0,1.5));Eq(o.ForceZ,0,"outside X");});
        Case("OUTSIDE_Z_NO_FORCE",()=>{var c=Lane();Steps(c,6,.1);var o=c.Advance(new MagnetSample(.1,0,1.51,1.5));Eq(o.ForceZ,0,"outside Z");});
        Case("SAFE_NORTHERN_BYPASS",()=>{var c=Lane();Steps(c,6,.1);var o=c.Advance(new MagnetSample(.1,0,4,12));Eq(o.ForceZ,0,"safe Z4");});
        Case("DEAD_BODY_NO_MAGNETIC_FORCE",()=>{var c=Lane();Steps(c,6,.1);var o=c.Advance(new MagnetSample(.1,0,0,12,false,false));Eq(o.ForceZ,0,"dead");});
        Case("PAUSE_FREEZES_CLOCK_AND_POLARITY",()=>{var c=Lane();Steps(c,7,.1);double t=c.Clock;int p=c.Polarity;var o=Steps(c,50,.1,0,0,1.5,true);Eq(c.Clock,t,"pause");Check(c.Polarity==p&&o.ForceZ==0,"no pause force");});
        Case("SAFE_PERIOD_NO_FORCE",()=>{var c=Lane();Steps(c,30,.1);var o=Steps(c,5,.1);Check(o.Phase==MagnetPhase.Safe&&o.ForceZ==0,"safe");});
        Case("COMPLETE_CYCLE_FLIPS_POLARITY",()=>{var c=Lane();var o=Steps(c,40,.1);Check(c.CompletedCycles==1&&c.Polarity==-1&&o.Phase==MagnetPhase.Warning,"flip");});
        Case("SECOND_CYCLE_NEGATIVE_FORCE",()=>{var c=Lane();Steps(c,46,.1);var o=c.Advance(new MagnetSample(.1,0,0,12));Eq(o.ForceZ,-60,"polarity reversed");});
        Case("TWO_CYCLES_RESET_POLARITY",()=>{var c=Lane();Steps(c,80,.1);Check(c.Polarity==1&&c.CompletedCycles==2,"two cycles");});
        Case("RESTART_CLEARS_CLOCK",()=>{var c=Lane();Steps(c,46,.1);c.Reset();Check(c.Clock==0&&c.Polarity==1&&c.CompletedCycles==0,"reset");});
        Case("ZERO_DT_NEVER_FORCE",()=>{var c=Lane();var o=c.Advance(new MagnetSample(0,0,0,1.5));Eq(o.ForceZ,0,"zero");Eq(c.Clock,0,"clock");});
        Case("TIMESTEP_PARTITION_FORCE_INTEGRAL",()=>{var a=Lane();var b=Lane();Steps(a,11,.05);Steps(b,22,.025);var x=a.Advance(new MagnetSample(.1,0,0,1.5));var y1=b.Advance(new MagnetSample(.05,0,0,1.5));var y2=b.Advance(new MagnetSample(.05,0,0,1.5));Eq(x.ForceZ*.1,y1.ForceZ*.05+y2.ForceZ*.05,"integral");});
        Case("ATOMIC_INVALID_SAMPLE",()=>{var c=Lane();Steps(c,7,.1);double t=c.Clock;int p=c.Polarity;try{c.Advance(new MagnetSample(.2,0,0,1.5));}catch(ArgumentException){}Eq(c.Clock,t,"clock unaffected");Check(c.Polarity==p,"polarity");});
        Case("FULL_100_CYCLES",()=>{var c=Lane();Steps(c,4000,.1);Check(c.CompletedCycles==100&&c.Polarity==1,"long run stable");});
        Case("INDEPENDENT_INSTANCES",()=>{var a=Lane();var b=Lane();Steps(a,50,.1);Check(b.Clock==0&&b.CompletedCycles==0,"instance isolation");});
        Case("AUTHORED_SCENE_BYPASS",()=>{
            using var j=JsonDocument.Parse(File.ReadAllText("Research/B01-25/P17/MAGNETLANE001/arena.json"));
            var root=j.RootElement;Check(root.GetProperty("schemaVersion").GetInt32()==1,"schema");
            var lane=root.GetProperty("magnetLane");var safe=root.GetProperty("safeBypass");
            Check(lane.GetProperty("center")[1].GetDouble()==0&&safe.GetProperty("center")[1].GetDouble()==4,"bypass");
        });
        var c0=Lane();
        Reject("NAN_DT",()=>c0.Advance(new MagnetSample(double.NaN,0,0,1.5)));
        Reject("NEGATIVE_DT",()=>c0.Advance(new MagnetSample(-.01,0,0,1.5)));
        Reject("OVERSIZE_TICK",()=>c0.Advance(new MagnetSample(.101,0,0,1.5)));
        Reject("INFINITE_X",()=>c0.Advance(new MagnetSample(.02,double.PositiveInfinity,0,1.5)));
        Reject("NAN_Z",()=>c0.Advance(new MagnetSample(.02,0,double.NaN,1.5)));
        Reject("NEGATIVE_MASS",()=>c0.Advance(new MagnetSample(.02,0,0,-1)));
        Reject("MISSING_MASS",()=>c0.Advance(new MagnetSample(.02,0,0,0)));
        Reject("NAN_CONFIG",()=>new MagnetConfig(.6,2.4,1,double.NaN,3,1.5));
        Reject("BAD_FORCE_CONFIG",()=>new MagnetConfig(.6,2.4,1,0,3,1.5));
        Reject("BAD_SPATIAL_CONFIG",()=>new MagnetConfig(.6,2.4,1,60,3,-1));
        Case("NEGATIVES_DID_NOT_CHANGE_CONTROLLER",()=>{Eq(c0.Clock,0,"atomically reject");Check(c0.Polarity==1,"polarity");});
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[0]))!);
        File.WriteAllText(args[0],JsonSerializer.Serialize(new{package="B01-25-MAGNETLANE001/R1",passed=n,negative,names,unityNative="NOT_RUN",codexAck=false},new JsonSerializerOptions{WriteIndented=true})+"\n");
        Console.WriteLine("RESULT PASS "+n+"/"+n+" (including "+negative+" negative input rejects); UNITY_NATIVE_NOT_RUN");
    }
}
