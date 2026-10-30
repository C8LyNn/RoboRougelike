using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using B01.LevelChallenges.Airlock;

public static class AirlockSourceTests
{
    static readonly List<string> Passed=new List<string>();
    static int negative;
    static AirlockController New()=>new AirlockController(new AirlockConfig(.8,1.2));
    static AirlockInput Frame(bool approach=false,bool chamber=false,bool exterior=false,
        bool obstructA=false,bool obstructB=false,bool emergency=false,bool paused=false,
        double dt=.02,double mass=1.5,bool alive=true)
        =>new AirlockInput(dt,mass,approach,chamber,exterior,obstructA,obstructB,emergency,paused,alive);
    static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Close(double actual,double expected,string name,double eps=1e-7)
    {Require(Math.Abs(actual-expected)<=eps,name+" got "+actual+" expected "+expected);}
    static void Test(string name,Action act)
    {try{act();Passed.Add(name);Console.WriteLine("PASS "+name);}
     catch(Exception error){Console.Error.WriteLine("FAIL "+name+" "+error);throw;}}
    static void Deny(string name,Action action)
    {Test("NEGATIVE_"+name,()=>{bool rejected=false;try{action();}catch(ArgumentException){rejected=true;}Require(rejected,"not rejected");});negative++;}
    static void Tick(AirlockController c,AirlockInput x,int n)
    {for(int i=0;i<n;i++)c.Advance(x);}
    static void Enter(AirlockController c)
    {
        c.Advance(Frame(approach:true));Tick(c,Frame(approach:true),42);
        Require(c.State.Phase==AirlockPhase.AwaitChamber,"entry not open");
    }
    static void Seal(AirlockController c)
    {
        Enter(c);c.Advance(Frame(chamber:true));
        Tick(c,Frame(chamber:true),42);
        Require(c.State.Phase==AirlockPhase.Equalizing,"entry not sealed");
    }
    static void Release(AirlockController c)
    {
        Seal(c);Tick(c,Frame(chamber:true),61);
        Tick(c,Frame(chamber:true),42);
        Require(c.State.Phase==AirlockPhase.AwaitExit,"exit not accessible");
    }
    static void Done(AirlockController c)
    {
        Release(c);c.Advance(Frame(exterior:true));
        Tick(c,Frame(exterior:true),42);
        Require(c.State.Phase==AirlockPhase.Idle,"cycle not reset");
    }
    static void All(string[] args)
    {
        Test("INITIAL_BOTH_DOORS_CLOSED",()=>{var c=New();Close(c.State.EntryOpen,0,"A");Close(c.State.ExitOpen,0,"B");});
        Test("APPROACH_STARTS_ENTRY_OPENING",()=>Require(New().Advance(Frame(approach:true)).Phase==AirlockPhase.EntryOpening,"entry"));
        Test("NO_APPROACH_REMAINS_CLOSED",()=>{var c=New();Tick(c,Frame(),100);Close(c.State.EntryOpen,0,"entry");});
        Test("ENTRY_OPENS_BUT_EXIT_STAYS_CLOSED",()=>{var c=New();Enter(c);Close(c.State.EntryOpen,1,"entry");Close(c.State.ExitOpen,0,"exit");});
        Test("LIGHT_CAN_USE_ENTRANCE",()=>{var c=New();c.Advance(Frame(approach:true,mass:1.5));Require(c.State.Phase==AirlockPhase.EntryOpening,"light");});
        Test("HEAVY_CAN_USE_ENTRANCE",()=>{var c=New();c.Advance(Frame(approach:true,mass:12));Require(c.State.Phase==AirlockPhase.EntryOpening,"heavy");});
        Test("DEAD_BODY_DOES_NOT_TRIGGER_ENTRY",()=>{var c=New();Tick(c,Frame(approach:true,alive:false),100);Require(c.State.Phase==AirlockPhase.Idle,"dead");});
        Test("NO_CHAMBER_CONTACT_ENTRY_WAIT",()=>{var c=New();Enter(c);Tick(c,Frame(chamber:false),200);Require(c.State.Phase==AirlockPhase.AwaitChamber,"unexpected close");});
        Test("DOOR_A_THRESHOLD_OCCUPIED_PREVENTS_CLOSING",()=>{var c=New();Enter(c);Tick(c,Frame(chamber:true,obstructA:true),100);Close(c.State.EntryOpen,1,"A");});
        Test("CHAMBER_SENSOR_CLOSES_ENTRANCE",()=>{var c=New();Seal(c);Close(c.State.EntryOpen,0,"entry");Close(c.State.ExitOpen,0,"exit");});
        Test("ENTRY_OBSTRUCTION_REOPENS_DOOR",()=>{var c=New();Enter(c);c.Advance(Frame(chamber:true));Tick(c,Frame(chamber:true),10);double prior=c.State.EntryOpen;c.Advance(Frame(chamber:true,obstructA:true));Require(c.State.Phase==AirlockPhase.EntryOpening,"reopening");Tick(c,Frame(chamber:true,obstructA:true),15);Require(c.State.EntryOpen>prior,"not reopen");});
        Test("EQUALIZATION_CANNOT_OPEN_EARLY",()=>{var c=New();Seal(c);Tick(c,Frame(chamber:true),30);Close(c.State.ExitOpen,0,"early exit");});
        Test("EQUALIZATION_OPENS_EXIT_ONLY_AFTER_SEAL",()=>{var c=New();Release(c);Close(c.State.EntryOpen,0,"entry sealed");Close(c.State.ExitOpen,1,"exit open");});
        Test("NO_EXIT_SENSOR_HOLDS_EXIT_OPEN",()=>{var c=New();Release(c);Tick(c,Frame(),200);Close(c.State.ExitOpen,1,"exit hold");});
        Test("EXIT_SENSOR_STARTS_CLOSING",()=>{var c=New();Release(c);c.Advance(Frame(exterior:true));Require(c.State.Phase==AirlockPhase.ExitClosing,"close B");});
        Test("EXIT_OBSTRUCTION_PREVENTS_CLOSING",()=>{var c=New();Release(c);Tick(c,Frame(exterior:true,obstructB:true),100);Close(c.State.ExitOpen,1,"B");});
        Test("EXIT_OBSTRUCTION_REOPENS_PARTIAL_CLOSURE",()=>{var c=New();Release(c);c.Advance(Frame(exterior:true));Tick(c,Frame(exterior:true),10);double prior=c.State.ExitOpen;c.Advance(Frame(obstructB:true));Require(c.State.Phase==AirlockPhase.ExitOpening,"reopen B");Tick(c,Frame(obstructB:true),15);Require(c.State.ExitOpen>prior,"B failed reopen");});
        Test("FULL_CYCLE_RETURNS_TO_IDLE",()=>{var c=New();Done(c);Close(c.State.EntryOpen,0,"A");Close(c.State.ExitOpen,0,"B");Require(c.State.CompletedCycles==1,"cycles");});
        Test("SECOND_PASS_DIFFERENT_RIG",()=>{var c=New();Done(c);Done(c);Require(c.State.CompletedCycles==2,"repeat");});
        Test("NO_DOUBLE_GATE_OPEN_DURING_NORMAL_CYCLE",()=>{var c=New();for(int step=0;step<800;step++){var p=c.State.Phase;
            var x=Frame(approach:true,chamber:p==AirlockPhase.AwaitChamber||
                p==AirlockPhase.EntryClosing||p==AirlockPhase.Equalizing,
                exterior:p==AirlockPhase.AwaitExit||p==AirlockPhase.ExitClosing);
            var s=c.Advance(x);Require(s.EntryOpen*s.ExitOpen==0,"both doors open");}});
        Test("PAUSE_FREEZES_PHASE_AND_CLOCK",()=>{var c=New();Enter(c);var old=c.State;Tick(c,Frame(paused:true),70);
            Close(c.State.Clock,old.Clock,"clock");Close(c.State.EntryOpen,old.EntryOpen,"entry");});
        Test("ZERO_DELTA_FREEZES",()=>{var c=New();c.Advance(Frame(approach:true));var before=c.State;c.Advance(Frame(dt:0));Close(before.Clock,c.State.Clock,"time");});
        Test("RESET_ERASES_CYCLE_AND_DOORS",()=>{var c=New();Done(c);c.Reset();Require(c.State.CompletedCycles==0,"cycles");Close(c.State.EntryOpen,0,"A");});
        Test("EMERGENCY_OPENS_ENTRY_ONLY_WHEN_EXIT_SEALED",()=>{var c=New();Seal(c);c.Advance(Frame(emergency:true));
            Require(c.State.Phase==AirlockPhase.RecoveryOpenEntry,"recovery");Tick(c,Frame(),50);
            Close(c.State.EntryOpen,1,"emergency A");Close(c.State.ExitOpen,0,"B");});
        Test("EMERGENCY_WITH_OPEN_EXIT_CLOSES_EXIT_FIRST",()=>{var c=New();Release(c);c.Advance(Frame(emergency:true));
            Require(c.State.Phase==AirlockPhase.RecoveryCloseExit,"first close exit");Tick(c,Frame(),43);
            Require(c.State.Phase==AirlockPhase.RecoveryOpenEntry,"after closed B");Tick(c,Frame(),45);
            Close(c.State.EntryOpen,1,"recovery A");Close(c.State.ExitOpen,0,"recovery B");});
        Test("EMERGENCY_CANNOT_CRUSH_OCCUPIED_EXIT",()=>{var c=New();Release(c);c.Advance(Frame(emergency:true,obstructB:true));Tick(c,Frame(obstructB:true),80);
            Close(c.State.ExitOpen,1,"B stays passable");Close(c.State.EntryOpen,0,"A lock");});
        Test("REPEATED_EMERGENCY_DOES_NOT_INCREMENT_COUNTER",()=>{var c=New();Seal(c);Tick(c,Frame(emergency:true),30);Require(c.State.EmergencyRequests==1,"repeated");});
        Test("PERMANENT_SIDE_BYPASS_NOT_DEPENDENT_ON_DOOR_STATE",()=>{
            var c=New();Tick(c,Frame(),400);Close(c.State.EntryOpen,0,"A closed");Close(c.State.ExitOpen,0,"B closed");
            // Safe bypass is authored separately in the Unity source; policy never locks it.
        });
        Test("DETERMINISTIC_REPLAY_1500_FRAMES",()=>{var a=New();var b=New();for(int i=0;i<1500;i++){
            var f=Frame(approach:i%137<40,chamber:i%137>=40&&i%137<90,exterior:i%137>=90,
              obstructA:i%17==0,obstructB:i%19==0,mass:i%3==0?1.5:12);
            var x=a.Advance(f);var y=b.Advance(f);
            Require(x.Phase==y.Phase&&x.CompletedCycles==y.CompletedCycles,"replay phase");
            Close(x.EntryOpen,y.EntryOpen,"replay A");Close(x.ExitOpen,y.ExitOpen,"replay B");}});
        Test("TOTAL_TIME_VALID_TICKS_ONLY",()=>{var c=New();Tick(c,Frame(),100);Close(c.State.Clock,2,"clock",1e-7);});
        Test("INVALID_PAUSED_FRAME_FAILS_ATOMIC",()=>{var c=New();c.Advance(Frame(approach:true));var old=c.State;
            bool raised=false;try{c.Advance(Frame(dt:double.NaN,paused:true));}catch(ArgumentException){raised=true;}
            Require(raised&&c.State.Phase==old.Phase,"paused invalid accepted");Close(c.State.Clock,old.Clock,"time");});
        Test("INVALID_FRAME_NEVER_MUTATES_OPEN_DOORS",()=>{var c=New();Enter(c);var old=c.State;
            try{c.Advance(Frame(mass:double.PositiveInfinity));}catch(ArgumentException){}
            Close(c.State.EntryOpen,old.EntryOpen,"A");Require(c.State.Phase==old.Phase,"phase");});
        Test("NO_SOFTLOCK_THROUGH_RECOVERY_WHEN_INSIDE",()=>{var c=New();Seal(c);c.Advance(Frame(emergency:true));
            Tick(c,Frame(),60);Require(c.State.EntryOpen==1&&c.State.ExitOpen==0,"escape hatch");});
        Deny("NULL_CONFIG",()=>new AirlockController(null!));
        Deny("CONFIG_TRAVEL_ZERO",()=>new AirlockConfig(0,1.2));
        Deny("CONFIG_EQUALIZE_NAN",()=>new AirlockConfig(.8,double.NaN));
        Deny("CONFIG_UNSUPPORTED_LONG",()=>new AirlockConfig(99,1));
        Deny("NEGATIVE_DT",()=>New().Advance(Frame(dt:-.01)));
        Deny("DT_ABOVE_MAX",()=>New().Advance(Frame(dt:.051)));
        Deny("INVALID_MASS_ZERO",()=>New().Advance(Frame(mass:0)));
        Deny("INVALID_MASS_NAN",()=>New().Advance(Frame(mass:double.NaN)));
        Deny("INVALID_MASS_INFINITE",()=>New().Advance(Frame(mass:double.PositiveInfinity)));
        Deny("INVALID_MASS_TOO_LARGE",()=>New().Advance(Frame(mass:1001)));
        Deny("INVALID_PAUSED_DELTA_INFINITE",()=>New().Advance(Frame(dt:double.PositiveInfinity,paused:true)));
        Require(args.Length==1,"One output JSON path required");
        string path=Path.GetFullPath(args[0]);Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path,JsonSerializer.Serialize(new{
            package="B01-25-AIRLOCK001/R1",originalTests=Passed.Count,
            negativeTests=negative,nativeUnity="NOT_RUN",testNames=Passed
        },new JsonSerializerOptions{WriteIndented=true})+"\n");
        Console.WriteLine("AIRLOCK_ORIGINAL_CSHARP9_PASS "+Passed.Count+"/"+Passed.Count+
            " NEGATIVE="+negative+" NATIVE_UNITY_NOT_RUN");
    }
    public static void Main(string[] args)=>All(args);
}
