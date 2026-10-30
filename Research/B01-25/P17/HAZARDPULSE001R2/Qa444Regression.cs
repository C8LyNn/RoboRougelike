// Independent B01-60 receiver: executes B01-25 R2 PulseCore.cs directly (producer replay of independent #444).
// This is test-only C#9; nothing in canonical Unity or frozen producer is changed.
using System;
using B01.LevelChallenges;

internal static class HazardBoundaryIndependentQa
{
    private static int pass, fail, checks;
    private static void Assert(bool ok,string reason)
    {
        checks++;
        if(!ok)throw new InvalidOperationException(reason);
    }
    private static void Case(string name,Action action)
    {
        try{action();pass++;Console.WriteLine("PASS "+name);}
        catch(Exception ex){fail++;Console.Error.WriteLine("FAIL "+name+" "+ex.Message);}
    }
    private static void Reject(Action action)
    {
        try{action();}catch(ArgumentException){return;}
        throw new InvalidOperationException("Expected ArgumentException");
    }
    private static PulseZoneConfig Config(double offset=0)=>
        new PulseZoneConfig("QA_BOUNDARY",28,32,1.35,1,2,3,offset);
    private static PulseSample Sample(double dt,bool alive=true,bool paused=false,double x=28,double z=32)=>
        new PulseSample(dt,x,z,alive,paused);
    private static PulseSnapshot Run(PulseZoneController p,int steps,double delta)
    {
        PulseSnapshot last=default;
        for(int i=0;i<steps;i++)last=p.Advance(Sample(delta));
        return last;
    }
    public static int Main()
    {
        Case("runtime compiles actual producer state machine",()=>{
            var p=new PulseZoneController(Config());
            Assert(p.PhaseAt(0)==PulsePhase.Warning,"initial phase");
            Assert(p.PhaseAt(1)==PulsePhase.Active,"exact 1s phase");
            Assert(p.PhaseAt(3)==PulsePhase.Recovery,"exact 3s phase");
            Assert(p.PhaseAt(6)==PulsePhase.Warning,"period wrap");
        });
        Case("FRAMERATE_BOUNDARY_0_1S_v_0_25S",()=>{
            // A fixed 1.000s time consists of ten 0.1s or four 0.25s legal host steps.
            // Binary floating point usually sums 10*0.1 to 0.9999999999999999.
            // The gameplay contract claims frame-partition determinism.
            var fast=new PulseZoneController(Config());
            var slow=new PulseZoneController(Config());
            var a=Run(fast,10,0.1);
            var b=Run(slow,4,0.25);
            Assert(a.Phase==b.Phase,
                "SOURCE DEFECT: identical elapsed nominal 1s gives phase "+a.Phase+
                " at 10x0.1, vs "+b.Phase+" at 4x0.25 (elapsed="+
                fast.ElapsedSeconds.ToString("R")+")");
            Assert(a.DamagePulse==b.DamagePulse,
                "SOURCE DEFECT: identical nominal 1s emits different damage authorizations");
        });
        Case("substep exact quarter seconds match phase and pulse",()=>{
            var a=new PulseZoneController(Config());
            var b=new PulseZoneController(Config());
            int pulseA=0,pulseB=0;
            for(int i=0;i<48;i++)
            {
                var x=a.Advance(Sample(.25));if(x.DamagePulse)pulseA++;
                var y=b.Advance(Sample(.125));if(y.DamagePulse)pulseB++;
                y=b.Advance(Sample(.125));if(y.DamagePulse)pulseB++;
                Assert(x.Phase==y.Phase,"quarter-v-eighth phase mismatch step="+i);
            }
            Assert(pulseA==2&&pulseB==2,"expected two cycles, once per cycle");
        });
        Case("same active window cannot farm damage through exits or zero delta",()=>{
            var p=new PulseZoneController(Config());
            Run(p,4,.25);
            int damage=0;
            p.Reset();
            for(int i=0;i<12;i++)
            {
                var s=p.Advance(Sample(.25,x:i%2==0?28:36));
                if(s.DamagePulse)damage++;
                var z=p.Advance(Sample(0,x:28));
                if(z.DamagePulse)damage++;
            }
            Assert(damage==1,"reentry/0dt caused "+damage+" grants");
        });
        Case("pause inside active zone freezes deadline and forbids grants",()=>{
            var p=new PulseZoneController(Config());
            Run(p,4,.25);
            double old=p.ElapsedSeconds;
            for(int i=0;i<200;i++)
            {
                var snapshot=p.Advance(Sample(.25,paused:true));
                Assert(!snapshot.DamagePulse,"damage while paused");
            }
            Assert(p.ElapsedSeconds==old,"elapsed changed on pause");
        });
        Case("invalid telemetry is rejected atomically",()=>{
            var p=new PulseZoneController(Config());
            Run(p,2,.25);
            double old=p.ElapsedSeconds;
            Reject(()=>p.Advance(Sample(-.01)));
            Reject(()=>p.Advance(Sample(double.NaN)));
            Reject(()=>p.Advance(Sample(double.PositiveInfinity)));
            Reject(()=>p.Advance(Sample(.250001)));
            Reject(()=>p.Advance(Sample(.1,x:double.NaN)));
            Reject(()=>p.Advance(Sample(.1,z:double.PositiveInfinity)));
            Assert(p.ElapsedSeconds==old,"invalid sample moved internal time");
        });
        Case("alternating authored 3-second offset never simultaneously active",()=>{
            var a=new PulseZoneController(Config());
            var b=new PulseZoneController(Config(3));
            for(int i=0;i<1600;i++)
            {
                double t=i*.025;var x=a.PhaseAt(t);var y=b.PhaseAt(t);
                Assert(!(x==PulsePhase.Active&&y==PulsePhase.Active),"simultaneous hazards t="+t);
            }
        });
        Case("no exposure for dead or outside player",()=>{
            var p=new PulseZoneController(Config());
            Run(p,3,.25);
            var dead=p.Advance(Sample(.25,alive:false));
            Assert(!dead.DamagePulse&&!dead.Exposed,"dead actor damaged");
            var far=p.Advance(Sample(.25,x:100));
            Assert(!far.DamagePulse&&!far.Exposed,"distant actor damaged");
        });
        Case("inclusive authored circle boundary and outside epsilon",()=>{
            var p=new PulseZoneController(Config());
            Assert(p.Contains(29.35,32),"exact radius omitted");
            Assert(!p.Contains(29.35001,32),"external actor incorrectly inside");
            Assert(!p.Contains(double.NaN,32),"NaN accepted as inside");
        });
        Case("reset recreates first-cycle grant",()=>{
            var p=new PulseZoneController(Config());Run(p,4,.25);
            p.Reset();
            var s=Run(p,4,.25);
            Assert(s.DamagePulse,"reset did not rearm damage once");
            Assert(!p.Advance(Sample(0)).DamagePulse,"duplicate on reset frame");
        });
        Console.WriteLine("B01-60 INDEPENDENT QA: "+pass+" PASS / "+fail+
            " FAIL; "+checks+" checked conditions; actual R2 PulseCore.cs executed; "+
            "Unity Editor/PlayMode/PhysX/real HP = NOT_RUN");
        return fail==0?0:1;
    }
}
