using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using B01.LevelChallenges.Conveyor;

public static class OriginalConveyorSourceTests
{
    private static readonly List<string> Names=new List<string>();
    private static int Negative;
    private static ConveyorController New()=>new ConveyorController(new ConveyorConfig(
        1.5,5,2.5,.5,3.5,.4,65));
    private static ConveyorSample S(double dt=.02,double x=0,double z=0,
        double mass=1.5,double velocity=0,bool contact=true,bool alive=true,bool paused=false)
        =>new ConveyorSample(dt,x,z,mass,velocity,contact,alive,paused);
    private static void Require(bool b,string msg){if(!b)throw new Exception(msg);}
    private static void Same(double a,double b,string msg,double tolerance=1e-6)
    {Require(Math.Abs(a-b)<=tolerance,msg+" actual="+a+" expected="+b);}
    private static void Test(string name,Action test)
    {try{test();Names.Add(name);Console.WriteLine("PASS "+name);}
     catch(Exception e){Console.Error.WriteLine("FAIL "+name+" "+e);throw;}}
    private static void Reject(string name,Action test)
    {Test("NEGATIVE_"+name,()=>{bool got=false;try{test();}catch(ArgumentException){got=true;}
         Require(got,"unsafe telemetry accepted");});Negative++;}
    private static void Ticks(ConveyorController c,int n,ConveyorSample s)
    {for(int i=0;i<n;i++)c.Advance(s);}
    private static void Run(string[] args)
    {
        Test("INITIAL_AT_REST",()=>{var c=New();Same(c.Last.ForceZ,0,"force");Same(c.Last.ElapsedSeconds,0,"clock");});
        Test("LIGHT_REAL_BELT_CONTACT_PUSHES_FORWARD",()=>Require(New().Advance(S()).ForceZ>0,"force"));
        Test("HEAVY_FORCE_LIMIT_SATURATES",()=>Same(New().Advance(S(mass:12)).ForceZ,65,"heavy budget"));
        Test("LIGHT_FORCE_EXACT_13_125_N",()=>Same(New().Advance(S()).ForceZ,13.125,"Newton"));
        Test("HEAVY_ACCELERATION_SMALLER_UNDER_BUDGET",()=>{
            double a=New().Advance(S()).ForceZ/1.5;
            double b=New().Advance(S(mass:12)).ForceZ/12;
            Require(a>b,"mass reaction");
        });
        Test("MATCHING_VELOCITY_NO_EXTRA_FORCE",()=>Same(New().Advance(S(velocity:3.5)).ForceZ,0,"matched"));
        Test("FASTER_THAN_BELT_SLOWS_WITH_NEGATIVE_FORCE",()=>Require(New().Advance(S(velocity:6)).ForceZ<0,"drag"));
        Test("REAL_COLLIDER_CONTACT_REQUIRED",()=>Same(New().Advance(S(contact:false)).ForceZ,0,"contact"));
        Test("NO_PASSIVE_FORCE_SAFE_BYPASS_AT_X4",()=>Same(New().Advance(S(x:4)).ForceZ,0,"bypass"));
        Test("NO_PASSIVE_FORCE_OUTSIDE_Z_EXTENT",()=>Same(New().Advance(S(z:6)).ForceZ,0,"outside"));
        Test("X_EDGE_INCLUDED_WHEN_PHYSICAL_CONTACT",()=>Require(New().Advance(S(x:1.5)).Supported,"edge"));
        Test("X_EDGE_EXCLUDED_OUTSIDE",()=>Require(!New().Advance(S(x:1.50001)).Supported,"edge outside"));
        Test("Z_EDGE_INCLUDED",()=>Require(New().Advance(S(z:-5)).Supported,"z edge"));
        Test("DEAD_VEHICLE_NOT_MOVED",()=>Same(New().Advance(S(alive:false)).ForceZ,0,"dead"));
        Test("FORWARD_PHASE_EXACT_START",()=>Require(New().Advance(S()).Phase==BeltPhase.Forward,"phase"));
        Test("WARNING_BEFORE_REVERSE_IS_FORCE_FREE",()=>{
            var c=New();Ticks(c,125,S());
            Require(c.Last.Phase==BeltPhase.ReversalWarning,"warn phase");
            Same(c.Last.ForceZ,0,"warn force");
        });
        Test("REVERSE_PHASE_APPLIES_NEGATIVE_FORCE",()=>{
            var c=New();Ticks(c,150,S());
            Require(c.Last.Phase==BeltPhase.Reverse,"reverse phase");
            Require(c.Last.ForceZ<0,"negative Newton");
        });
        Test("REVERSE_HEAVY_SATURATES_NEGATIVE_FORCE",()=>{
            var c=New();Ticks(c,150,S(mass:12));
            Same(c.Last.ForceZ,-65,"reverse heavy");
        });
        Test("WARNING_BEFORE_FORWARD_IS_FORCE_FREE",()=>{
            var c=New();Ticks(c,275,S());
            Require(c.Last.Phase==BeltPhase.ForwardWarning,"phase");
            Same(c.Last.ForceZ,0,"warning force");
        });
        Test("CYCLE_RESTART_FORWARD_AFTER_6_SECONDS",()=>{
            var c=New();Ticks(c,300,S());
            Require(c.Last.Phase==BeltPhase.Forward,"restart phase");
            Require(c.Last.CompletedPeriods==1,"period count");
            Require(c.Last.ForceZ>0,"forward return");
        });
        Test("TWO_FULL_PERIODS_COUNTED",()=>{
            var c=New();Ticks(c,600,S());
            Require(c.Last.CompletedPeriods==2,"two cycles");
        });
        Test("REVERSE_SPEED_MATCH_HAS_NO_FORCE",()=>{
            var c=New();Ticks(c,150,S(velocity:-3.5));
            Same(c.Last.ForceZ,0,"reverse speed matched");
        });
        Test("REVERSING_WHILE_MOVING_FORWARD_IS_BRAKING",()=>{
            var c=New();Ticks(c,150,S(velocity:3.5));
            Require(c.Last.ForceZ<0,"brake physical momentum");
        });
        Test("SAMPLE_ZERO_DELTA_DOES_NOT_ADVANCE",()=>{
            var c=New();c.Advance(S());var before=c.Last;
            var s=c.Advance(S(dt:0));
            Same(s.ElapsedSeconds,before.ElapsedSeconds,"no advance");
            Same(s.ForceZ,before.ForceZ,"same frame");
        });
        Test("PAUSE_FREEZES_CLOCK_AND_OWN_FORCE",()=>{
            var c=New();c.Advance(S());var before=c.Last;
            Ticks(c,100,S(paused:true));
            Same(c.Last.ElapsedSeconds,before.ElapsedSeconds,"paused time");
            Same(c.Last.ForceZ,before.ForceZ,"paused frame state");
        });
        Test("RESET_DROPS_PREVIOUS_PHASE_AND_FORCE",()=>{
            var c=New();Ticks(c,160,S());c.Reset();
            Same(c.Last.ForceZ,0,"reset force");Same(c.Last.ElapsedSeconds,0,"reset clock");
            Require(c.Last.Phase==BeltPhase.Forward,"reset phase");
        });
        Test("DETERMINISTIC_INDEPENDENT_REPLAY_900_STEPS",()=>{
            var a=New();var b=New();for(int i=0;i<900;i++){
                var v=S(x:i%19==0?4:0,mass:i%7==0?12:1.5,velocity:i%5==0?2:0,contact:i%13!=0);
                var f=a.Advance(v);var g=b.Advance(v);
                Same(f.ForceZ,g.ForceZ,"force");Same(f.ElapsedSeconds,g.ElapsedSeconds,"clock");
                Require(f.Phase==g.Phase&&f.CompletedPeriods==g.CompletedPeriods,"phase");
            }
        });
        Test("ABSOLUTE_FORCE_CAP_ALWAYS_MAINTAINED",()=>{
            var c=New();for(int i=0;i<400;i++){
                var f=c.Advance(S(mass:1000,velocity:i%2==0?1000:-1000));
                Require(Math.Abs(f.ForceZ)<=65+1e-9,"cap");
            }
        });
        Test("INVALID_SAMPLE_ATOMIC_NO_CLOCK_CHANGE",()=>{
            var c=New();c.Advance(S());var before=c.Last;
            try{c.Advance(S(dt:.07));}catch(ArgumentException){}
            Same(c.Last.ElapsedSeconds,before.ElapsedSeconds,"clock");
            Same(c.Last.ForceZ,before.ForceZ,"force");
        });
        Test("PAUSED_INVALID_PHYSICS_NOT_SILENT",()=>{
            var c=New();bool rejected=false;
            try{c.Advance(S(velocity:double.NaN,paused:true));}catch(ArgumentException){rejected=true;}
            Require(rejected&&c.Last.ElapsedSeconds==0,"invalid paused");
        });
        Test("MASS_FORCES_LINEAR_WHEN_UNSATURATED",()=>{
            double l=New().Advance(S(mass:1.5)).ForceZ;
            double m=New().Advance(S(mass:3)).ForceZ;
            Same(m,2*l,"force mass scale");
        });
        Test("NO_FORCE_IN_WARNING_EVEN_WITH_HEAVY_BAD_SPEED",()=>{
            var c=New();Ticks(c,125,S(mass:1000,velocity:1000));
            Same(c.Last.ForceZ,0,"off during warning");
        });
        Reject("NULL_CONFIG",()=>new ConveyorController(null!));
        Reject("BAD_RUN_TIME",()=>new ConveyorConfig(1.5,5,0,.5,3.5,.4,65));
        Reject("BAD_WARNING_NAN",()=>new ConveyorConfig(1.5,5,2.5,double.NaN,3.5,.4,65));
        Reject("BAD_SPEED_INFINITY",()=>new ConveyorConfig(1.5,5,2.5,.5,double.PositiveInfinity,.4,65));
        Reject("BAD_MAX_FORCE",()=>new ConveyorConfig(1.5,5,2.5,.5,3.5,.4,0));
        Reject("TIME_NEGATIVE",()=>New().Advance(S(dt:-.01)));
        Reject("TIME_TOO_LONG",()=>New().Advance(S(dt:.05001)));
        Reject("MASS_ZERO",()=>New().Advance(S(mass:0)));
        Reject("MASS_HUGE",()=>New().Advance(S(mass:1001)));
        Reject("VELOCITY_NAN",()=>New().Advance(S(velocity:double.NaN)));
        Reject("VELOCITY_INFINITY",()=>New().Advance(S(velocity:double.PositiveInfinity)));
        Reject("X_NAN",()=>New().Advance(S(x:double.NaN)));
        Reject("Z_INFINITY",()=>New().Advance(S(z:double.NegativeInfinity)));
        Reject("WORLD_TOO_FAR",()=>New().Advance(S(x:1000001)));
        Reject("INVALID_PAUSED_TIMESTEP",()=>New().Advance(S(dt:double.PositiveInfinity,paused:true)));
        Require(args.Length==1,"One JSON output argument required");
        var path=Path.GetFullPath(args[0]);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path,JsonSerializer.Serialize(new{
            package="B01-25-CONVEYOR001/R1",originalTests=Names.Count,
            negativeTests=Negative,nativeUnity="NOT_RUN",testNames=Names
        },new JsonSerializerOptions{WriteIndented=true})+"\n");
        Console.WriteLine("CONVEYOR_ORIGINAL_CSHARP9_PASS "+Names.Count+"/"+Names.Count+
            " NEGATIVE="+Negative+" NATIVE_UNITY_NOT_RUN");
    }
    public static void Main(string[] args)=>Run(args);
}
