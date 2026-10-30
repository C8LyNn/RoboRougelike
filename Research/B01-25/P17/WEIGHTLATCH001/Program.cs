using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using B01.LevelChallenges.WeightLatch;

public static class WeightLatchExecutableTests
{
    static readonly List<string> Ok=new();
    static int rejected;
    static WeightLatchController Gate()=>new WeightLatchController(new GateConfig(8,1.5,-3,1,1.2,3));
    static GateInput I(double dt=.02,double x=0,double z=-3,double mass=12,
        bool contact=true,bool paused=false,bool alive=true)
        =>new GateInput(dt,x,z,mass,contact,paused,alive);
    static void Check(bool v,string why){if(!v)throw new Exception(why);}
    static void Eq(double got,double expected,string why,double eps=1e-8)
        =>Check(Math.Abs(got-expected)<eps,why+" got="+got+" expected="+expected);
    static void Case(string name,Action run)
    {try{run();Ok.Add(name);Console.WriteLine("PASS "+name);}
     catch(Exception e){Console.Error.WriteLine("FAIL "+name+" "+e);throw;}}
    static void Reject(string name,Action run)
    {Case("NEGATIVE_"+name,()=>{
        bool denied=false;try{run();}catch(ArgumentException){denied=true;}
        Check(denied,"Invalid input accepted");});rejected++;}
    static void Run(string[] args)
    {
        Case("INITIAL_CLOSED_ZERO_CLOCK",()=>{var g=Gate();Check(g.State.Motion==GateMotion.Closed,"closed");Eq(g.State.ElapsedSeconds,0,"time");});
        Case("HEAVY_REAL_CONTACT_RAISES",()=>{var s=Gate().Advance(I());Check(s.OpenFraction>0&&s.QualifiedHeavyContact,"raise");});
        Case("LIGHT_1_5KG_CANNOT_UNLOCK",()=>{var s=Gate().Advance(I(mass:1.5));Eq(s.OpenFraction,0,"light");});
        Case("HEAVY_THRESHOLD_EXACT_8KG",()=>Check(Gate().Advance(I(mass:8)).QualifiedHeavyContact,"threshold"));
        Case("BELOW_HEAVY_THRESHOLD",()=>Eq(Gate().Advance(I(mass:7.999)).OpenFraction,0,"below"));
        Case("NO_REAL_COLLIDER_CONTACT",()=>Eq(Gate().Advance(I(contact:false)).OpenFraction,0,"physical flag"));
        Case("OUTSIDE_PAD_X",()=>Eq(Gate().Advance(I(x:1.6)).OpenFraction,0,"X"));
        Case("OUTSIDE_PAD_Z",()=>Eq(Gate().Advance(I(z:-1.9)).OpenFraction,0,"Z"));
        Case("PAD_BOUNDARY_X_INCLUDED",()=>Check(Gate().Advance(I(x:1.5)).QualifiedHeavyContact,"X inclusive"));
        Case("PAD_BOUNDARY_Z_INCLUDED",()=>Check(Gate().Advance(I(z:-4)).QualifiedHeavyContact,"Z inclusive"));
        Case("DEAD_VEHICLE_NO_PRESSURE",()=>Eq(Gate().Advance(I(alive:false)).OpenFraction,0,"dead"));
        Case("LIGHT_CAN_USE_SEPARATE_ALWAYS_SAFE_DETOUR_X4",()=>Eq(Gate().Advance(I(x:4,mass:1.5)).OpenFraction,0,"bypass"));
        Case("HEAVY_CANNOT_TRIGGER_ON_X4_BYPASS",()=>Eq(Gate().Advance(I(x:4,mass:12)).OpenFraction,0,"bypass"));
        Case("PUSH_HEAVY_60_STEPS_OPENS_FULLY",()=>{
            var g=Gate();for(int j=0;j<60;j++)g.Advance(I());
            Eq(g.State.OpenFraction,1,"fully open");Check(g.State.Motion==GateMotion.Open,"open state");
        });
        Case("OPEN_NEVER_EXCEEDS_ONE",()=>{
            var g=Gate();for(int j=0;j<200;j++)g.Advance(I());
            Eq(g.State.OpenFraction,1,"clamp");
        });
        Case("ONE_PRESS_ONE_ACTIVATION_EVENT",()=>{
            var g=Gate();for(int j=0;j<150;j++)g.Advance(I());
            Check(g.State.ActivationCount==1,"repeat counted");
        });
        Case("HEAVY_LEAVES_AND_GATE_STAYS_OPEN_TEMPORARILY",()=>{
            var g=Gate();for(int j=0;j<60;j++)g.Advance(I());
            for(int j=0;j<100;j++)g.Advance(I(contact:false));
            Eq(g.State.OpenFraction,1,"latch preserved");
            Check(g.State.Motion==GateMotion.Open,"open while grace");
        });
        Case("GATE_CLOSES_AFTER_GRACE_AND_TRAVEL",()=>{
            var g=Gate();for(int j=0;j<60;j++)g.Advance(I());
            for(int j=0;j<240;j++)g.Advance(I(contact:false));
            Eq(g.State.OpenFraction,0,"returns closed");Check(g.State.Motion==GateMotion.Closed,"closed");
        });
        Case("GATE_LOWERING_TRANSITION_BEFORE_CLOSED",()=>{
            var g=Gate();for(int j=0;j<60;j++)g.Advance(I());
            for(int j=0;j<160;j++)g.Advance(I(contact:false));
            Check(g.State.Motion==GateMotion.Lowering,"lowering");
        });
        Case("HEAVY_REPRESS_DURING_LOWERING_REOPENS",()=>{
            var g=Gate();for(int j=0;j<60;j++)g.Advance(I());
            for(int j=0;j<160;j++)g.Advance(I(contact:false));
            double before=g.State.OpenFraction;g.Advance(I());
            Check(g.State.OpenFraction>before&&g.State.Motion==GateMotion.Raising,"reopen");
        });
        Case("SECOND_PRESS_AFTER_CLOSURE_SECOND_ACTIVATION",()=>{
            var g=Gate();g.Advance(I());for(int j=0;j<220;j++)g.Advance(I(contact:false));
            g.Advance(I());Check(g.State.ActivationCount==2,"second activation");
        });
        Case("NO_SOFTLOCK_BYPASS_INDEPENDENT_OF_GATE_TIME",()=>{
            var g=Gate();for(int j=0;j<140;j++)g.Advance(I(x:4,mass:1.5));
            Eq(g.State.OpenFraction,0,"safe corridor unblocked");
        });
        Case("RESET_CLEARS_LATCH_AND_ACTIVATION",()=>{
            var g=Gate();g.Advance(I());g.Reset();Eq(g.State.OpenFraction,0,"fraction");
            Eq(g.State.HoldRemainingSeconds,0,"hold");Check(g.State.ActivationCount==0,"count");
        });
        Case("PAUSE_FREEZES_OPEN_AND_LATCH_HOLD",()=>{
            var g=Gate();g.Advance(I());var before=g.State;
            for(int j=0;j<20;j++)g.Advance(I(paused:true,contact:false));
            Eq(g.State.OpenFraction,before.OpenFraction,"fraction");
            Eq(g.State.HoldRemainingSeconds,before.HoldRemainingSeconds,"hold");
            Eq(g.State.ElapsedSeconds,before.ElapsedSeconds,"time");
        });
        Case("ZERO_DELTA_NO_STATE_CHANGE",()=>{
            var g=Gate();g.Advance(I());var before=g.State;
            g.Advance(I(dt:0,contact:false));Eq(g.State.HoldRemainingSeconds,before.HoldRemainingSeconds,"time");
        });
        Case("LEAVING_PAD_REFRESH_RELEASE_AFTER_EXACT_HOLD_TIME",()=>{
            var g=Gate();g.Advance(I());for(int j=0;j<151;j++)g.Advance(I(contact:false));
            Check(g.State.HoldRemainingSeconds==0,"grace expired");
        });
        Case("VALIDATE_ALL_TELEMETRY_EVEN_PAUSED",()=>{
            var g=Gate();var before=g.State;bool thrown=false;
            try{g.Advance(I(dt:double.NaN,paused:true));}catch(ArgumentException){thrown=true;}
            Check(thrown&&g.State.ElapsedSeconds==before.ElapsedSeconds,"paused invalid allowed");
        });
        Case("INVALID_FRAME_DOES_NOT_PARTIALLY_MUTATE",()=>{
            var g=Gate();g.Advance(I());var old=g.State;
            try{g.Advance(I(dt:double.PositiveInfinity));}catch(ArgumentException){}
            Eq(g.State.OpenFraction,old.OpenFraction,"open");Eq(g.State.HoldRemainingSeconds,old.HoldRemainingSeconds,"hold");
            Check(g.State.ActivationCount==old.ActivationCount,"activation");
        });
        Case("DETERMINISTIC_REPLAY_TWO_INDEPENDENT_RUNS",()=>{
            var a=Gate();var b=Gate();for(int j=0;j<350;j++){
                var sample=I(mass:j%9==0?1.5:12,contact:j%11<7);
                var x=a.Advance(sample);var y=b.Advance(sample);
                Eq(x.OpenFraction,y.OpenFraction,"fraction");Eq(x.HoldRemainingSeconds,y.HoldRemainingSeconds,"hold");
                Check(x.ActivationCount==y.ActivationCount&&x.Motion==y.Motion,"metadata");
            }
        });
        Case("PHASES_COMPOSE_VALID_BOUNDS",()=>{
            var g=Gate();for(int j=0;j<600;j++){
                var s=g.Advance(I(contact:j%160<70));
                Check(s.OpenFraction>=0&&s.OpenFraction<=1&&s.HoldRemainingSeconds>=0,"invariant");
            }
        });
        Case("LATCH_HOLD_TIME_RESETS_ON_HEAVY_PRESSURE",()=>{
            var g=Gate();g.Advance(I());for(int j=0;j<100;j++)g.Advance(I(contact:false));
            g.Advance(I());Eq(g.State.HoldRemainingSeconds,3,"refresh");
        });
        Case("TIME_COUNTS_ONLY_VALID_NONPAUSED_FIXEDSTEPS",()=>{
            var g=Gate();for(int j=0;j<100;j++)g.Advance(I(dt:.02,contact:false));
            Eq(g.State.ElapsedSeconds,2,"clock",1e-7);
        });
        Reject("DEFAULT_CSHARP9_STRUCT_CONFIG_MUST_FAIL_CLOSED",()=>new WeightLatchController(new GateConfig()));
        Reject("CONFIG_NEGATIVE_THRESHOLD",()=>new GateConfig(-1));
        Reject("CONFIG_NONFINITE_CENTER",()=>new GateConfig(padCenterZ:double.PositiveInfinity));
        Reject("CONFIG_ZERO_HOLD",()=>new GateConfig(holdSeconds:0));
        Reject("CONFIG_ZERO_TRAVEL",()=>new GateConfig(moveSeconds:0));
        Reject("NEGATIVE_DT",()=>Gate().Advance(I(dt:-.01)));
        Reject("DT_TOO_LARGE_NO_SUBSTEP",()=>Gate().Advance(I(dt:.051)));
        Reject("NONFINITE_POSITION_X",()=>Gate().Advance(I(x:double.NaN)));
        Reject("NONFINITE_POSITION_Z",()=>Gate().Advance(I(z:double.PositiveInfinity)));
        Reject("MASS_ZERO",()=>Gate().Advance(I(mass:0)));
        Reject("MASS_TOO_LARGE",()=>Gate().Advance(I(mass:1001)));
        Reject("OUT_OF_WORLD_RANGE",()=>Gate().Advance(I(x:1000001)));
        Reject("INVALID_PAUSED_SAMPLE",()=>Gate().Advance(I(z:double.NaN,paused:true)));
        Check(args.Length==1,"expected one output receipt path");
        string path=Path.GetFullPath(args[0]);Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path,JsonSerializer.Serialize(new{package="B01-25-WEIGHTLATCH001/R1",
           realSourceTestsPassed=Ok.Count,negativeTestsPassed=rejected,nativeUnity="NOT_RUN",
           tests=Ok},new JsonSerializerOptions{WriteIndented=true})+"\n");
        Console.WriteLine("WEIGHTLATCH_ORIGINAL_CSHARP9_PASS "+Ok.Count+"/"+Ok.Count+
           " NEGATIVE="+rejected+" UNITY_NATIVE_NOT_RUN");
    }
    public static void Main(string[] args)=>Run(args);
}
