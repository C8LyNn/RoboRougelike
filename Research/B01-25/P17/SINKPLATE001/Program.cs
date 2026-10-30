using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using B01.LevelChallenges.SinkPlate;
static class TestRunner
{
    static int ok,neg;
    static readonly List<string> names=new();
    static void Check(bool x,string why){if(!x)throw new Exception(why);}
    static void Eq(double a,double b,string why){Check(Math.Abs(a-b)<1e-8,why+": "+a+" != "+b);}
    static void Case(string n,Action f){try{f();ok++;names.Add(n);Console.WriteLine("PASS "+n);}catch(Exception e){Console.Error.WriteLine("FAIL "+n+" "+e);throw;}}
    static void Reject(string n,Action f)
    {Case("NEGATIVE_"+n,()=>{bool threw=false;try{f();}catch(ArgumentException){threw=true;}Check(threw,"not rejected");});neg++;}
    static PlateConfig C()=>new(4,.8,.35,1.2,.4);
    static SinkPlateController Plate()=>new(C());
    static PlateState Steps(SinkPlateController c,int n,double dt,bool on,double kg)
    {PlateState r=default;for(int i=0;i<n;i++)r=c.Advance(new(dt,on,kg));return r;}
    static void Main(string[] argv)
    {
        Check(argv.Length==1,"one receipt path required");
        Case("READY_IS_SUPPORTING",()=>{var c=Plate();Check(c.Snapshot().SupportsVehicle && c.Phase==PlatePhase.Ready,"ready");});
        Case("EMPTY_PLATE_NEVER_TRIGGERS",()=>{var c=Plate();Steps(c,20,.1,false,1.5);Check(c.Phase==PlatePhase.Ready&&c.CompletedCollapses==0,"empty");});
        Case("LIGHT_WARNING_800MS",()=>{var c=Plate();var r=Steps(c,7,.1,true,1.5);Check(r.Phase==PlatePhase.Warning&&r.SupportsVehicle,"warning");Eq(r.WarningDuration,.8,"light");r=c.Advance(new(.1,true,1.5));Check(r.Phase==PlatePhase.Dropped&&r.CollapseStarted&&!r.SupportsVehicle,"light collapsed");});
        Case("HEAVY_WARNING_350MS",()=>{var c=Plate();var r=Steps(c,3,.1,true,12);Check(r.Phase==PlatePhase.Warning,"warning");r=c.Advance(new(.05,true,12));Check(r.Phase==PlatePhase.Dropped&&r.CollapseStarted,"heavy collapse");Eq(r.WarningDuration,.35,"heavy");});
        Case("EXACT_HEAVY_THRESHOLD",()=>{var c=Plate();c.Advance(new(0,true,4));Eq(c.LockedWarning,.35,"threshold");});
        Case("NO_MASS_TOGGLE_EXPLOIT_MID_WARNING",()=>{var c=Plate();c.Advance(new(.1,true,1.5));c.Advance(new(.1,true,12));Eq(c.LockedWarning,.8,"locked light");});
        Case("LEAVING_WARNING_DOES_NOT_CANCEL_COLLAPSE",()=>{var c=Plate();c.Advance(new(.1,true,1.5));Steps(c,7,.1,false,1.5);Check(c.Phase==PlatePhase.Dropped,"timer should continue");});
        Case("PAUSE_FREEZES_STATE_TIMER",()=>{var c=Plate();c.Advance(new(.2,true,12));double before=c.PhaseElapsed;for(int i=0;i<10;i++)c.Advance(new(.1,true,12,true));Eq(c.PhaseElapsed,before,"paused");Check(c.Phase==PlatePhase.Warning,"phase");});
        Case("COLLAPSE_ONLY_ONE_EVENT",()=>{var c=Plate();int grants=0;for(int i=0;i<30;i++)if(c.Advance(new(.1,true,12)).CollapseStarted)grants++;Check(grants==1&&c.CompletedCollapses==1,"single collapse");});
        Case("DROP_AND_RECOVERY_COLLIDER_DISABLED",()=>{var c=Plate();Steps(c,4,.1,true,12);Check(!c.Snapshot().SupportsVehicle,"dropped");Steps(c,12,.1,true,12);Check(c.Phase==PlatePhase.Recovering&&!c.Snapshot().SupportsVehicle,"recovery");});
        Case("STANDING_ON_RESTORED_PLATE_NO_REARM",()=>{var c=Plate();Steps(c,25,.1,true,12);Check(c.Phase==PlatePhase.AwaitClear&&c.Snapshot().SupportsVehicle,"await clear");Steps(c,30,.1,true,12);Check(c.CompletedCollapses==1,"farming");});
        Case("STEP_OFF_TO_REARM",()=>{var c=Plate();Steps(c,25,.1,true,12);c.Advance(new(0,false,12));Check(c.Phase==PlatePhase.Ready,"clear");c.Advance(new(0,true,12));Check(c.Phase==PlatePhase.Warning&&c.CompletedCollapses==1,"fresh entry");});
        Case("RESET_RESTORES_SUPPORT",()=>{var c=Plate();Steps(c,4,.1,true,12);c.Reset();Check(c.Phase==PlatePhase.Ready&&c.CompletedCollapses==0&&c.Snapshot().SupportsVehicle,"reset");});
        Case("TIME_PARTITION_INVARIANT_HEAVY",()=>{var a=Plate();var b=Plate();Steps(a,4,.1,true,12);Steps(b,8,.05,true,12);Check(a.Phase==b.Phase && a.CompletedCollapses==b.CompletedCollapses,"partition");Eq(a.PhaseElapsed,b.PhaseElapsed,"elapsed");});
        Case("TIME_PARTITION_INVARIANT_LIGHT",()=>{var a=Plate();var b=Plate();Steps(a,8,.1,true,1.5);Steps(b,16,.05,true,1.5);Check(a.Phase==b.Phase&&a.CompletedCollapses==b.CompletedCollapses,"partition");});
        Case("UNPAUSED_ZERO_DT_TRIGGERS_WITHOUT_DAMAGE",()=>{var c=Plate();var r=c.Advance(new(0,true,12));Check(r.Phase==PlatePhase.Warning&&!r.CollapseStarted,"zero");});
        Case("SIMULTANEOUS_TWO_PLATES_INDEPENDENT",()=>{var a=Plate();var b=Plate();Steps(a,4,.1,true,12);Check(b.Phase==PlatePhase.Ready,"shared state");});
        Case("FULL_50_CYCLES_NO_DUPLICATES",()=>{var c=Plate();for(int run=0;run<50;run++){Steps(c,25,.1,true,12);Check(c.Phase==PlatePhase.AwaitClear&&c.CompletedCollapses==run+1,"round");c.Advance(new(0,false,12));}Check(c.CompletedCollapses==50,"count");});
        Case("AUTHORED_ALTERNATE_SAFE_LANE",()=>{
            using var json=JsonDocument.Parse(File.ReadAllText("Research/B01-25/P17/SINKPLATE001/arena.json"));
            var r=json.RootElement;
            Check(r.GetProperty("version").GetInt32()==1&&r.GetProperty("metersPerCell").GetInt32()==1,"arena");
            var plate=r.GetProperty("plate"),bypass=r.GetProperty("safeBypass");
            Check(plate.GetProperty("center")[0].GetDouble()==0 &&
                plate.GetProperty("center")[1].GetDouble()==0,"center");
            Check(plate.GetProperty("size")[0].GetDouble()==6,"6m plate");
            Check(bypass.GetProperty("center")[1].GetDouble()>=4,"separate bypass");
        });
        var bad=Plate();Reject("NAN_DELTA",()=>bad.Advance(new(double.NaN,true,1.5)));
        Reject("NEGATIVE_DELTA",()=>bad.Advance(new(-.1,true,1.5)));
        Reject("OVERSIZED_HOST_TICK",()=>bad.Advance(new(.101,true,1.5)));
        Reject("NONFINITE_MASS",()=>bad.Advance(new(.05,true,double.PositiveInfinity)));
        Reject("ZERO_MASS",()=>bad.Advance(new(.05,true,0)));
        Reject("BAD_CONFIG_WARNING",()=>new SinkPlateController(new(4,.3,.4,1.2,.4)));
        Reject("BAD_CONFIG_RECOVERY",()=>new SinkPlateController(new(4,.8,.35,1.2,0)));
        Case("INVALID_INPUT_ATOMIC",()=>Check(bad.Phase==PlatePhase.Ready&&bad.CompletedCollapses==0,"mutated"));
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(argv[0]))!);
        File.WriteAllText(argv[0],JsonSerializer.Serialize(new{package="B01-25-SINKPLATE001/R1",tests=ok,negatives=neg,names,unityNative="NOT_RUN",codexAck=false},new JsonSerializerOptions{WriteIndented=true})+"\n");
        Console.WriteLine("RESULT PASS "+ok+"/"+ok+" (including "+neg+" negatives); UNITY_NATIVE_NOT_RUN");
    }
}
