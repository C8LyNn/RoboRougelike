using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using B01.LevelChallenges.MagnetLane.R3;
static class OriginalTests
{
    static int n,negative;
    static readonly List<string> names=new();
    static void Check(bool yes,string msg){if(!yes)throw new Exception(msg);}
    static void Eq(double a,double b,string msg){Check(Math.Abs(a-b)<1e-9,msg+": "+a+" != "+b);}
    static void Case(string id,Action f){try{f();n++;names.Add(id);Console.WriteLine("PASS "+id);}catch(Exception e){Console.Error.WriteLine("FAIL "+id+" "+e);throw;}}
    static void Reject(string id,Action act){Case("NEGATIVE_"+id,()=>{bool denied=false;try{act();}catch(ArgumentException){denied=true;}Check(denied,id);});negative++;}
    static MagnetLaneController Lane()=>new(new MagnetConfig(.6,2.4,1,60,3,1.5));
    static MagnetSample At(double dt,double x,double z,double massKg,bool paused=false,bool alive=true)
        =>new MagnetSample(dt,x,z,x,z,massKg,paused,alive);
    static MagnetOutput Steps(MagnetLaneController c,int times,double dt,double x=0,double z=0,double kg=1.5,bool paused=false,bool alive=true)
    {MagnetOutput o=default;for(int i=0;i<times;i++)o=c.Advance(At(dt,x,z,kg,paused,alive));return o;}
    static void Main(string[] args)
    {
        Check(args.Length==1,"one output receipt required");
        Case("INITIAL_WARNING_NO_FORCE",()=>{var c=Lane();var o=Steps(c,5,.1);Check(o.Phase==MagnetPhase.Warning&&o.ForceZ==0,"warning");});
        Case("ACTIVATION_AFTER_600MS",()=>{var c=Lane();Steps(c,6,.1);var o=Steps(c,1,.1);Check(o.Phase==MagnetPhase.Active&&o.ForceZ>0,"active");Eq(o.ForceZ,60,"maximum force");});
        Case("HALF_FRAME_ACTIVATION_INTEGRATED",()=>{var c=Lane();Steps(c,11,.05);var o=c.Advance(At(.1,0,0,1.5));Eq(o.ForceZ,30,"half .1 active");Eq(o.ActiveFraction,.5,"half active");});
        Case("LIGHT_ACTUAL_ACCELERATION",()=>{var c=Lane();Steps(c,6,.1);var o=c.Advance(At(.1,0,0,1.5));Eq(o.AccelerationZ,40,"light m/s2");});
        Case("HEAVY_ACTUAL_ACCELERATION",()=>{var c=Lane();Steps(c,6,.1);var o=c.Advance(At(.1,0,0,12));Eq(o.AccelerationZ,5,"heavy m/s2");});
        Case("HEAVY_EIGHTFOLD_RESISTANCE",()=>{var a=Lane();var b=Lane();Steps(a,6,.1);Steps(b,6,.1);var x=a.Advance(At(.1,0,0,1.5));var y=b.Advance(At(.1,0,0,12));Eq(x.ForceZ,y.ForceZ,"same Newtons");Eq(x.AccelerationZ/y.AccelerationZ,8,"mass ratio");});
        Case("EDGE_TAPERS_TO_ZERO",()=>{var c=Lane();Steps(c,6,.1);var o=c.Advance(At(.1,0,1.5,1.5));Eq(o.ForceZ,0,"edge");});
        Case("HALF_WIDE_TAPER",()=>{var c=Lane();Steps(c,6,.1);var o=c.Advance(At(.1,0,.75,1.5));Eq(o.ForceZ,30,"half strength");});
        Case("OUTSIDE_X_NO_FORCE",()=>{var c=Lane();Steps(c,6,.1);var o=c.Advance(At(.1,4,0,1.5));Eq(o.ForceZ,0,"outside X");});
        Case("OUTSIDE_Z_NO_FORCE",()=>{var c=Lane();Steps(c,6,.1);var o=c.Advance(At(.1,0,1.51,1.5));Eq(o.ForceZ,0,"outside Z");});
        Case("SAFE_NORTHERN_BYPASS",()=>{var c=Lane();Steps(c,6,.1);var o=c.Advance(At(.1,0,4,12));Eq(o.ForceZ,0,"safe Z4");});
        Case("DEAD_BODY_NO_MAGNETIC_FORCE",()=>{var c=Lane();Steps(c,6,.1);var o=c.Advance(At(.1,0,0,12,false,false));Eq(o.ForceZ,0,"dead");});
        Case("PAUSE_FREEZES_CLOCK_AND_POLARITY",()=>{var c=Lane();Steps(c,7,.1);double t=c.Clock;int p=c.Polarity;var o=Steps(c,50,.1,0,0,1.5,true);Eq(c.Clock,t,"pause");Check(c.Polarity==p&&o.ForceZ==0,"no pause force");});
        Case("SAFE_PERIOD_NO_FORCE",()=>{var c=Lane();Steps(c,30,.1);var o=Steps(c,5,.1);Check(o.Phase==MagnetPhase.Safe&&o.ForceZ==0,"safe");});
        Case("COMPLETE_CYCLE_FLIPS_POLARITY",()=>{var c=Lane();var o=Steps(c,40,.1);Check(c.CompletedCycles==1&&c.Polarity==-1&&o.Phase==MagnetPhase.Warning,"flip");});
        Case("SECOND_CYCLE_NEGATIVE_FORCE",()=>{var c=Lane();Steps(c,46,.1);var o=c.Advance(At(.1,0,0,12));Eq(o.ForceZ,-60,"polarity reversed");});
        Case("TWO_CYCLES_RESET_POLARITY",()=>{var c=Lane();Steps(c,80,.1);Check(c.Polarity==1&&c.CompletedCycles==2,"two cycles");});
        Case("RESTART_CLEARS_CLOCK",()=>{var c=Lane();Steps(c,46,.1);c.Reset();Check(c.Clock==0&&c.Polarity==1&&c.CompletedCycles==0,"reset");});
        Case("ZERO_DT_NEVER_FORCE",()=>{var c=Lane();var o=c.Advance(At(0,0,0,1.5));Eq(o.ForceZ,0,"zero");Eq(c.Clock,0,"clock");});
        Case("TIMESTEP_PARTITION_FORCE_INTEGRAL",()=>{var a=Lane();var b=Lane();Steps(a,11,.05);Steps(b,22,.025);var x=a.Advance(At(.1,0,0,1.5));var y1=b.Advance(At(.05,0,0,1.5));var y2=b.Advance(At(.05,0,0,1.5));Eq(x.ForceZ*.1,y1.ForceZ*.05+y2.ForceZ*.05,"integral");});
        Case("ATOMIC_INVALID_SAMPLE",()=>{var c=Lane();Steps(c,7,.1);double t=c.Clock;int p=c.Polarity;try{c.Advance(At(.2,0,0,1.5));}catch(ArgumentException){}Eq(c.Clock,t,"clock unaffected");Check(c.Polarity==p,"polarity");});
        Case("FULL_100_CYCLES",()=>{var c=Lane();Steps(c,4000,.1);Check(c.CompletedCycles==100&&c.Polarity==1,"long run stable");});
        Case("INDEPENDENT_INSTANCES",()=>{var a=Lane();var b=Lane();Steps(a,50,.1);Check(b.Clock==0&&b.CompletedCycles==0,"instance isolation");});
        Case("AUTHORED_SCENE_BYPASS",()=>{
            using var j=JsonDocument.Parse(File.ReadAllText("Research/B01-25/P17/MAGNETLANE001R3/arena.json"));
            var root=j.RootElement;Check(root.GetProperty("schemaVersion").GetInt32()==1,"schema");
            var lane=root.GetProperty("magnetLane");var safe=root.GetProperty("safeBypass");
            Check(lane.GetProperty("center")[1].GetDouble()==0&&safe.GetProperty("center")[1].GetDouble()==4,"bypass");
        });

        Case("R1_RED_FAST_CROSS_ENDPOINTS_OUTSIDE_R2_FORCE",()=>{
            Check(Math.Abs(-7)>3&&Math.Abs(7)>3,"old source RED witness preconditions");
            var c=Lane();Steps(c,6,.1);
            var o=c.Advance(new MagnetSample(.1,-7,0,7,0,1.5));
            Eq(o.ForceZ,60*6.0/14.0,"new exact central crossing");
            Eq(o.ActiveFraction,6.0/14.0,"time fraction");
            Check(!o.InsideLane,"end outside, time inside");
        });
        Case("R2_ENTERS_LANE_FINAL_CENTER_ONLY_OLD_OVERAPPLIED",()=>{
            var c=Lane();Steps(c,6,.1);
            var o=c.Advance(new MagnetSample(.1,-7,0,0,0,1.5));
            Eq(o.ForceZ,60*3.0/7.0,"only actual entered portion");
        });
        Case("R2_EXITS_LANE_TO_OUTSIDE",()=>{
            var c=Lane();Steps(c,6,.1);
            var o=c.Advance(new MagnetSample(.1,0,0,7,0,1.5));
            Eq(o.ForceZ,60*3.0/7.0,"only portion before leaving");
        });
        Case("R2_SPATIAL_CROSS_Z0_EXACT_TRIANGULAR_TAPER",()=>{
            double occupied;
            var strength=MagnetLaneController.StrengthIntegral(0,-2,0,2,3,1.5,out occupied);
            Eq(occupied,.75,"75 percent track enters field");
            Eq(strength,.375,"2 triangles area integral");
        });
        Case("R2_FAST_NORTH_SAFE_BYPASS_NO_FORCE",()=>{
            var c=Lane();Steps(c,6,.1);
            var o=c.Advance(new MagnetSample(.1,-100,4,100,4,12));
            Eq(o.ForceZ,0,"bypass never magnetized");
        });
        Case("R2_PARTIAL_ACTIVE_AND_FAST_CROSS_COMPOSE",()=>{
            var c=Lane();Steps(c,11,.05);
            var o=c.Advance(new MagnetSample(.1,-7,0,7,0,1.5));
            Eq(o.ForceZ,60*3.0/14.0,"only active second half and middle 3/14");
        });
        Case("R2_SPATIAL_TRAVEL_PARTITION_INTEGRAL",()=>{
            var a=Lane();var b=Lane();Steps(a,6,.1);Steps(b,6,.1);
            var whole=a.Advance(new MagnetSample(.1,-7,0,7,0,1.5));
            var first=b.Advance(new MagnetSample(.05,-7,0,0,0,1.5));
            var last=b.Advance(new MagnetSample(.05,0,0,7,0,1.5));
            Eq(.1*whole.ForceZ,.05*first.ForceZ+.05*last.ForceZ,"partition exact");
        });
        Case("R2_MOVING_Z_GRADIENT_INTEGRAL",()=>{
            var c=Lane();Steps(c,6,.1);
            var o=c.Advance(new MagnetSample(.1,0,0,0,1.5,1.5));
            Eq(o.ForceZ,30,"integral linear taper");
        });
        Case("R2_POLARITY_REVERSE_SPATIAL_CROSS",()=>{
            var c=Lane();Steps(c,46,.1);
            var o=c.Advance(new MagnetSample(.1,-7,0,7,0,12));
            Eq(o.ForceZ,-60*6.0/14.0,"negative polarity");
        });
        Case("R2_DEAD_MOVING_VEHICLE_NO_FORCE",()=>{
            var c=Lane();Steps(c,6,.1);
            var o=c.Advance(new MagnetSample(.1,-7,0,7,0,12,false,false));
            Eq(o.ForceZ,0,"dead moving body");
        });
        Case("R2_PAUSED_MOVING_PATH_NO_FORCE_OR_CLOCK",()=>{
            var c=Lane();Steps(c,6,.1);var time=c.Clock;
            var o=c.Advance(new MagnetSample(.1,-7,0,7,0,12,true));
            Eq(o.ForceZ,0,"paused moving body");Eq(c.Clock,time,"frozen clock");
        });
        Case("R2_TOUCH_ONLY_NO_NONPHYSICAL_INTEGRAL",()=>{
            double occupied;
            Eq(MagnetLaneController.StrengthIntegral(-7,0,-3,0,3,1.5,out occupied),0,"one boundary point");
            Eq(occupied,0,"no time coverage");
        });
        Case("R2_INVALID_FROM_POSITION_ATOMIC",()=>{
            var c=Lane();Steps(c,6,.1);double old=c.Clock;
            bool rejected=false;try{c.Advance(new MagnetSample(.1,double.NaN,0,0,0,12));}
            catch(ArgumentException){rejected=true;}
            Check(rejected,"rejected");Eq(c.Clock,old,"atomic clock");
        });
        Reject("R2_INFINITE_FROM_Z",()=>new MagnetLaneController(new MagnetConfig(.6,2.4,1,60,3,1.5))
            .Advance(new MagnetSample(.05,0,double.PositiveInfinity,0,0,1.5)));
        Reject("R2_SPATIAL_INTEGRAL_BAD_BOUNDS",()=>{double occupied;
            MagnetLaneController.StrengthIntegral(0,0,1,1,0,1.5,out occupied);});

        Case("R3_OVERFLOW_AT_WARNING_BOUNDARY_ATOMIC",()=>{
            var c=Lane();Steps(c,11,.05);double old=c.Clock;int polarity=c.Polarity;
            bool rejected=false;
            try{c.Advance(new MagnetSample(.1,-1e308,0,1e308,0,12));}
            catch(ArgumentException){rejected=true;}
            Check(rejected,"overflow must be rejected");
            Eq(c.Clock,old,"no partially promoted warning clock");
            Check(c.Polarity==polarity&&c.CompletedCycles==0,"no metadata promotion");
        });
        Case("R3_INVALID_IN_SAFE_PHASE_NO_PARTIAL_ROTATION",()=>{
            var c=Lane();Steps(c,79,.05);double old=c.Clock;int polarity=c.Polarity,cycles=c.CompletedCycles;
            bool rejected=false;
            try{c.Advance(new MagnetSample(.1,-1e308,0,1e308,0,1.5));}
            catch(ArgumentException){rejected=true;}
            Check(rejected,"overflow");
            Eq(c.Clock,old,"clock");Check(c.Polarity==polarity&&c.CompletedCycles==cycles,"polarity and cycles");
        });
        Case("R3_INVALID_PAUSED_SAMPLE_STILL_REJECTED",()=>{
            var c=Lane();double old=c.Clock;bool rejected=false;
            try{c.Advance(new MagnetSample(.1,-1e308,0,1e308,0,12,true));}
            catch(ArgumentException){rejected=true;}
            Check(rejected,"no silently accepting invalid paused telemetry");Eq(c.Clock,old,"clock");
        });
        Case("R3_DIRECT_INTEGRAL_OVERFLOW_PRECHECK",()=>{
            bool rejected=false;
            try{double occupied;MagnetLaneController.StrengthIntegral(-1e308,0,1e308,0,3,1.5,out occupied);}
            catch(ArgumentException){rejected=true;}
            Check(rejected,"direct clip should not admit intermediary NaN/Infinity");
        });
        Case("R3_VALID_FAST_R2_SEGMENT_UNCHANGED",()=>{
            var c=Lane();Steps(c,6,.1);var output=c.Advance(new MagnetSample(.1,-7,0,7,0,12));
            Eq(output.ForceZ,60*6.0/14.0,"swept force preserved");
        });
        Case("R3_VALID_DISPLACEMENT_200M_BYPASS",()=>{
            var c=Lane();Steps(c,6,.1);var output=c.Advance(new MagnetSample(.1,-100,4,100,4,12));
            Eq(output.ForceZ,0,"true northern bypass remains safe");
        });
        Case("R3_LIMIT_COORDINATE_INCLUSIVE",()=>{
            var c=Lane();var output=c.Advance(new MagnetSample(.1,1000000,4,1000000,4,12));
            Eq(output.ForceZ,0,"boundary accepted");
        });
        Case("R3_LIMIT_DISPLACEMENT_INCLUSIVE",()=>{
            var c=Lane();var output=c.Advance(new MagnetSample(.1,-500,4,500,4,12));
            Eq(output.ForceZ,0,"exact 1000m displacement accepted");
        });
        Reject("R3_WORLD_POSITION_OUT_OF_RANGE",()=>Lane().Advance(new MagnetSample(.1,1000000.01,0,0,0,12)));
        Reject("R3_EXCESSIVE_TELEPORT_1001M",()=>Lane().Advance(new MagnetSample(.1,-501,0,500,0,12)));
        Reject("R3_BAD_INTEGRAL_BOUND_101",()=>{double time;MagnetLaneController.StrengthIntegral(0,0,1,0,101,1.5,out time);});
        Case("R3_CONTINUE_AFTER_REJECT_WITHOUT_HIDDEN_STATE_CHANGE",()=>{
            var c=Lane();Steps(c,11,.05);
            double old=c.Clock;
            try{c.Advance(new MagnetSample(.1,-1e308,0,1e308,0,12));}catch(ArgumentException){}
            Eq(c.Clock,old,"atomic");
            var output=c.Advance(new MagnetSample(.1,0,0,0,0,12));
            Eq(output.ForceZ,30,"correct crossing after rejected transaction");
        });
        var c0=Lane();
        Reject("NAN_DT",()=>c0.Advance(At(double.NaN,0,0,1.5)));
        Reject("NEGATIVE_DT",()=>c0.Advance(At(-.01,0,0,1.5)));
        Reject("OVERSIZE_TICK",()=>c0.Advance(At(.101,0,0,1.5)));
        Reject("INFINITE_X",()=>c0.Advance(At(.02,double.PositiveInfinity,0,1.5)));
        Reject("NAN_Z",()=>c0.Advance(At(.02,0,double.NaN,1.5)));
        Reject("NEGATIVE_MASS",()=>c0.Advance(At(.02,0,0,-1)));
        Reject("MISSING_MASS",()=>c0.Advance(At(.02,0,0,0)));
        Reject("NAN_CONFIG",()=>new MagnetConfig(.6,2.4,1,double.NaN,3,1.5));
        Reject("BAD_FORCE_CONFIG",()=>new MagnetConfig(.6,2.4,1,0,3,1.5));
        Reject("BAD_SPATIAL_CONFIG",()=>new MagnetConfig(.6,2.4,1,60,3,-1));
        Case("NEGATIVES_DID_NOT_CHANGE_CONTROLLER",()=>{Eq(c0.Clock,0,"atomically reject");Check(c0.Polarity==1,"polarity");});
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[0]))!);
        File.WriteAllText(args[0],JsonSerializer.Serialize(new{package="B01-25-MAGNETLANE001/R3",passed=n,negative,names,unityNative="NOT_RUN",codexAck=false},new JsonSerializerOptions{WriteIndented=true})+"\n");
        Console.WriteLine("RESULT PASS "+n+"/"+n+" (including "+negative+" negative input rejects); UNITY_NATIVE_NOT_RUN");
    }
}
