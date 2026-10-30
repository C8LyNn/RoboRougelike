using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using B01.LevelChallenges.CounterBridge;

public static class OriginalCounterBridgeSourceTests
{
    static readonly List<string> Passed=new();
    static int negatives;
    static CounterBridgeConfig Config()=>new CounterBridgeConfig(1.5,3,250,500,600,18);
    static CounterBridgeController Bridge()=>new CounterBridgeController(Config());
    static BridgeSample Input(double dt=.02,double x=0,double z=2,double mass=12,
        bool contact=true,bool paused=false,bool alive=true)
        =>new BridgeSample(dt,x,z,mass,contact,paused,alive);
    static void True(bool x,string reason){if(!x)throw new Exception(reason);}
    static void Eq(double actual,double expected,string reason,double tolerance=1e-7)
    {True(Math.Abs(actual-expected)<=tolerance,reason+" got="+actual+" expected="+expected);}
    static void Case(string name,Action action)
    {
        try{action();Passed.Add(name);Console.WriteLine("PASS "+name);}
        catch(Exception e){Console.Error.WriteLine("FAIL "+name+": "+e);throw;}
    }
    static void Reject(string name,Action action)
    {
        Case("NEGATIVE_"+name,()=>{
            bool rejected=false;try{action();}catch(ArgumentException){rejected=true;}
            True(rejected,"Expected ArgumentException");
        });negatives++;
    }
    static void Start(string[] args)
    {
        Case("R1_DEFAULT_INITIAL_REST",()=>{var b=Bridge();Eq(b.Clock,0,"clock");
            Eq(b.Last.AngleDeg,0,"angle");Eq(b.Last.AngularVelocityDegS,0,"speed");});
        Case("R1_HEAVY_EXERTS_EIGHT_TIMES_LIGHT_TORQUE",()=>{
            var a=Bridge().Advance(Input(mass:1.5));var b=Bridge().Advance(Input(mass:12));
            Eq(b.PressureTorqueNm/a.PressureTorqueNm,8,"mass/torque ratio");
            True(b.AngleDeg>a.AngleDeg&&a.AngleDeg>0,"actual response");
        });
        Case("R1_POSITIVE_Z_TIPS_POSITIVE",()=>True(Bridge().Advance(Input(z:2)).AngleDeg>0,"positive"));
        Case("R1_NEGATIVE_Z_TIPS_NEGATIVE",()=>True(Bridge().Advance(Input(z:-2)).AngleDeg<0,"negative"));
        Case("R1_CENTER_ZERO_TORQUE",()=>Eq(Bridge().Advance(Input(z:0)).PressureTorqueNm,0,"pivot"));
        Case("R1_ACTUAL_SUPPORT_REQUIRED",()=>{var s=Bridge().Advance(Input(contact:false));
            Eq(s.AngleDeg,0,"contact");True(!s.VehicleSupported,"support");});
        Case("R1_BYPASS_X4_NEVER_TIPS",()=>{var s=Bridge().Advance(Input(x:4));
            Eq(s.AngleDeg,0,"bypass");True(!s.VehicleSupported,"supported");});
        Case("R1_OUTSIDE_LONGITUDINAL_NEVER_TIPS",()=>Eq(Bridge().Advance(Input(z:4)).AngleDeg,0,"z out"));
        Case("R1_BOUNDARY_CONTACT_ACCEPTED",()=>True(Bridge().Advance(Input(x:1.5,z:3)).VehicleSupported,"edge"));
        Case("R1_HULL_OUTSIDE_X_DENIED",()=>True(!Bridge().Advance(Input(x:1.50001)).VehicleSupported,"outside"));
        Case("R1_DEAD_VEHICLE_NO_TORQUE",()=>Eq(Bridge().Advance(Input(alive:false)).PressureTorqueNm,0,"dead"));
        Case("R1_PAUSE_STOPS_PHYSICS_AND_CLOCK",()=>{
            var b=Bridge();b.Advance(Input());var c=b.Clock;var a=b.Last.AngleDeg;
            var state=b.Advance(Input(paused:true));Eq(state.AngleDeg,a,"angle");Eq(b.Clock,c,"clock");
        });
        Case("R1_ZERO_DELTA_NO_CLOCK",()=>{var b=Bridge();b.Advance(Input(dt:0));Eq(b.Clock,0,"zero");});
        Case("R1_FIXED_STEP_CLOCK_ACCUMULATION",()=>{var b=Bridge();
            for(int i=0;i<20;i++)b.Advance(Input());Eq(b.Clock,.4,"clock",1e-9);});
        Case("R1_BRIDGE_MECHANICAL_STOPS_PRESERVE_INVARIANT",()=>{
            var b=Bridge();bool stopped=false;
            for(int i=0;i<400;i++){var s=b.Advance(Input(z:3,mass:100));
                if(s.AtMechanicalStop)stopped=true;
                True(Math.Abs(s.AngleDeg)<=18+1e-9,"angle exceeded mechanical stop");
            }True(stopped,"no mechanical stop");});
        Case("R1_AFTER_RELEASE_SPRING_RECENTERS",()=>{
            var b=Bridge();for(int i=0;i<25;i++)b.Advance(Input());
            var angle=b.Last.AngleDeg;
            for(int i=0;i<200;i++)b.Advance(Input(contact:false));
            True(Math.Abs(b.Last.AngleDeg)<angle,"recentered");});
        Case("R1_PASSIVE_FORCE_NEVER_TILTS_IF_NOT_ON_BRIDGE",()=>{
            var b=Bridge();for(int i=0;i<60;i++)b.Advance(Input(x:4,mass:100));
            Eq(b.Last.AngleDeg,0,"north safe bypass");
        });
        Case("R1_LIGHT_AND_HEAVY_DIFFERENT_ANGULAR_RESPONSE",()=>{
            var l=Bridge();var h=Bridge();for(int i=0;i<20;i++)
            {l.Advance(Input(mass:1.5));h.Advance(Input(mass:12));}
            True(h.Last.AngleDeg>l.Last.AngleDeg*5,"under-load contrast");
        });
        Case("R1_DETERMINISTIC_REPLAY_TWO_INDEPENDENT_RUNS",()=>{
            var a=Bridge();var b=Bridge();for(int i=0;i<100;i++)
            {var item=Input(z:(i%2==0?2:-2),mass:(i%3==0?1.5:12));
             var x=a.Advance(item);var y=b.Advance(item);
             Eq(x.AngleDeg,y.AngleDeg,"angle");Eq(x.Elapsed,y.Elapsed,"time");}
        });
        Case("R1_RESET_REMOVES_PREVIOUS_TORQUE_AND_CLOCK",()=>{
            var b=Bridge();b.Advance(Input());b.Reset();Eq(b.Last.AngleDeg,0,"angle");
            Eq(b.Last.PressureTorqueNm,0,"torque");Eq(b.Clock,0,"clock");});
        Case("R1_RESTART_IS_EQUIVALENT_TO_NEW_CONTROLLER",()=>{
            var a=Bridge();a.Advance(Input(z:-3));a.Reset();
            Eq(a.Advance(Input()).AngleDeg,Bridge().Advance(Input()).AngleDeg,"replay");
        });
        Case("R1_EXACT_FIRST_SEMI_IMPLICIT_STEP",()=>{
            var s=Bridge().Advance(Input());
            double torque=12*9.81*2;
            Eq(s.PressureTorqueNm,torque,"torque");
            Eq(s.AngleDeg,(torque/250)*.02*.02*180/Math.PI,"semi implicit",1e-9);
        });
        Case("R1_SUPPORTED_FLAG_CLIPPED_BY_ACTUAL_GEOMETRY",()=>{
            var s=Bridge().Advance(Input(x:-99,contact:true));
            True(!s.VehicleSupported,"host contact alone not sufficient");
        });
        Reject("NEGATIVE_CONFIG_INERTIA_ZERO",()=>new CounterBridgeConfig(1.5,3,0,45,90,18));
        Reject("NEGATIVE_CONFIG_BAD_MAX_ANGLE",()=>new CounterBridgeConfig(1.5,3,250,45,90,40));
        Reject("NEGATIVE_CONFIG_NONFINITE_GRAVITY",()=>new CounterBridgeConfig(1.5,3,250,45,90,18,double.NaN));
        Reject("NEGATIVE_TIMESTEP_ABOVE_50MS",()=>Bridge().Advance(Input(dt:.05001)));
        Reject("NEGATIVE_NEGATIVE_TIMESTEP",()=>Bridge().Advance(Input(dt:-.01)));
        Reject("NEGATIVE_NONFINITE_POSITION_X",()=>Bridge().Advance(Input(x:double.NaN)));
        Reject("NEGATIVE_NONFINITE_POSITION_Z",()=>Bridge().Advance(Input(z:double.PositiveInfinity)));
        Reject("NEGATIVE_INVALID_MASS_ZERO",()=>Bridge().Advance(Input(mass:0)));
        Reject("NEGATIVE_INVALID_MASS_TOO_LARGE",()=>Bridge().Advance(Input(mass:1001)));
        Reject("NEGATIVE_TOO_FAR_COORDINATE",()=>Bridge().Advance(Input(x:1000001)));
        Case("R1_INVALID_INPUT_CANNOT_MUTATE_PHASE_OR_ANGLE",()=>{
            var b=Bridge();b.Advance(Input());double t=b.Clock,a=b.Last.AngleDeg;
            try{b.Advance(Input(dt:.06));}catch(ArgumentException){}
            Eq(b.Clock,t,"clock");Eq(b.Last.AngleDeg,a,"angle");});
        Case("R1_PAUSED_INVALID_HOST_SAMPLE_REJECTED_ATOMIC",()=>{
            var b=Bridge();double time=b.Clock;bool threw=false;
            try{b.Advance(Input(dt:double.NaN,paused:true));}catch(ArgumentException){threw=true;}
            True(threw,"invalid pause accepted");Eq(b.Clock,time,"atomic paused");});
        Case("R1_DISABLE_CONTACT_RECOVERS_WITH_NO_TELEPORT",()=>{
            var b=Bridge();for(int i=0;i<30;i++)b.Advance(Input());
            double prior=b.Last.AngleDeg;
            for(int i=0;i<80;i++)b.Advance(Input(contact:false));
            True(Math.Abs(b.Last.AngleDeg)<prior,"spring did not restore");});
        Case("R1_SEPARATE_RESTART_NOT_A_GAME_SAVE_TRANSACTION",()=>{
            var b=Bridge();for(int i=0;i<30;i++)b.Advance(Input());
            b.Reset();True(b.Last.AngleDeg==0&&b.Clock==0,"local only");
        });
        string outPath=Path.GetFullPath(args[0]);
        Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
        File.WriteAllText(outPath,JsonSerializer.Serialize(new{
            package="B01-25-COUNTERBRIDGE001/R1",status="ACTUAL_SOURCE_ONLY",
            passed=Passed.Count,negativeInputs=negatives,nativeUnity="NOT_RUN",tests=Passed
        },new JsonSerializerOptions{WriteIndented=true})+"\n");
        Console.WriteLine("COUNTERBRIDGE_ORIGINAL_SOURCE_PASS "+Passed.Count+"/"+Passed.Count+
                          " NEGATIVE="+negatives+" UNITY_NATIVE_NOT_RUN");
    }
    public static void Main(string[] args)
    {if(args.Length!=1)throw new ArgumentException("Expected output JSON filename");Start(args);}
}
