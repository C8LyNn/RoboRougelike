// This test program COMPILIES the ACTUAL package C# state machine, no surrogate.
// Real Unity Physics, GameObjects and demo are NOT_RUN without a Unity runner.
using B01.CHL003;
using System;
using System.Collections.Generic;

static class Program
{
    static int pass, fail;
    static void Check(string label, bool good)
    {
        if (good) pass++;
        else { fail++; Console.WriteLine("FAIL: "+label); }
    }
    static bool Throws<T>(Action a) where T:Exception
    {
        try { a(); return false; } catch (T) { return true; }
    }
    static CHL003CourseCore New(int count=4,int actor=42,float limit=0)
    {
        var core = new CHL003CourseCore();
        core.Configure(count,actor,limit);
        return core;
    }
    static int Main()
    {
        var e=New();int cps=0,wins=0,losses=0;
        e.CheckpointReached += _=>cps++;
        e.Completed+=_=>wins++;
        e.Failed+=_=>losses++;
        Check("ready",e.State==CHL003CourseCore.Phase.Ready && e.CheckpointIndex==0);
        Check("prebegin-rejected",!e.EnterCheckpoint(42,0)&&!e.EnterFinish(42));
        Check("begin-once",e.Begin()&&!e.Begin());
        Check("wrong-actor-checkpoint",!e.EnterCheckpoint(15,0)&&e.CheckpointIndex==0);
        Check("out-of-order",!e.EnterCheckpoint(42,3)&&e.CheckpointIndex==0);
        Check("early-finish",!e.EnterFinish(42)&&wins==0);
        for(int i=0;i<4;i++)
        {
            Check("ordered-cp-"+i,e.EnterCheckpoint(42,i));
            Check("duplicate-cp-"+i,!e.EnterCheckpoint(42,i));
        }
        Check("count-events",cps==4);
        Check("foreign-finish",!e.EnterFinish(71));
        e.AdvanceTime(1.5f);
        Check("finish",e.EnterFinish(42)&&e.State==CHL003CourseCore.Phase.Completed);
        Check("finish-exactly-once",!e.EnterFinish(42)&&wins==1);
        Check("completed-immutable",!e.Fail(42,CHL003CourseCore.Failure.FellOffCourse)&&
            !e.EnterCheckpoint(42,0));
        float finished=e.ElapsedSeconds;e.AdvanceTime(50f);
        Check("completed-time-frozen",e.ElapsedSeconds==finished);
        e.Retry();
        Check("ready-reset",e.CheckpointIndex==0&&e.ElapsedSeconds==0f&&
            e.State==CHL003CourseCore.Phase.Ready&&e.LastFailure==CHL003CourseCore.Failure.None);
        Check("retry-no-extra-wins",wins==1);
        e.Begin();e.AdvanceTime(.4f);e.SetPaused(true);e.AdvanceTime(200f);
        Check("paused-time-frozen",Math.Abs(e.ElapsedSeconds-.4f)<.00001f);
        Check("paused-checkpoint-rejected",!e.EnterCheckpoint(42,0));
        e.SetPaused(false);e.AdvanceTime(.1f);
        Check("unpause",Math.Abs(e.ElapsedSeconds-.5f)<.00001f);
        Check("wrong-actor-fail",!e.Fail(41,CHL003CourseCore.Failure.FellOffCourse));
        Check("real-fall-fail",e.Fail(42,CHL003CourseCore.Failure.FellOffCourse));
        Check("failure-once",!e.Fail(42,CHL003CourseCore.Failure.FellOffCourse)&&losses==1);
        e.Cancel();Check("cancel-to-ready",e.State==CHL003CourseCore.Phase.Ready&&e.ElapsedSeconds==0);
        Check("no-cancel-reward",wins==1);
        Check("cannot-configure-running",New().State==CHL003CourseCore.Phase.Ready);
        var busy=New();busy.Begin();
        Check("cannot-configure-live",Throws<InvalidOperationException>(()=>busy.Configure(3,42)));
        Check("count-underflow",Throws<ArgumentOutOfRangeException>(()=>New(2)));
        Check("count-overflow",Throws<ArgumentOutOfRangeException>(()=>New(6)));
        Check("actor-zero",Throws<ArgumentException>(()=>New(4,0)));
        Check("time-negative",Throws<ArgumentOutOfRangeException>(()=>New(4,42,-1)));
        Check("time-nan",Throws<ArgumentOutOfRangeException>(()=>New(4,42,float.NaN)));
        Check("time-inf",Throws<ArgumentOutOfRangeException>(()=>New(4,42,float.PositiveInfinity)));
        Check("negative-tick",Throws<ArgumentOutOfRangeException>(()=>busy.AdvanceTime(-1f)));
        Check("nan-tick",Throws<ArgumentOutOfRangeException>(()=>busy.AdvanceTime(float.NaN)));
        Check("inf-tick",Throws<ArgumentOutOfRangeException>(()=>busy.AdvanceTime(float.PositiveInfinity)));
        var timeout=New(3,42,3f);timeout.Begin();timeout.AdvanceTime(2.5f);timeout.AdvanceTime(2f);
        Check("timed-failure",timeout.State==CHL003CourseCore.Phase.Failed &&
            timeout.LastFailure==CHL003CourseCore.Failure.TimeLimit&&timeout.ElapsedSeconds==3f);
        Check("timeout-not-completion",!timeout.EnterFinish(42));
        timeout.Retry();Check("timeout-clears",timeout.LastFailure==CHL003CourseCore.Failure.None);
        // 800 deterministic complete trials: 3..5 gates, seeded foreign actors,
        // dup/out-of-order, retry, timed pause, exact-once and predictable events.
        var random=new Random(20261027);
        for(int trial=0;trial<800;trial++)
        {
            int count=random.Next(3,6),actor=random.Next(1,100000);
            var run=New(count,actor);int cpCount=0,completed=0;
            run.CheckpointReached+=_=>cpCount++;run.Completed+=_=>completed++;
            Check("random-begin-"+trial,run.Begin());
            for(int n=0;n<count;n++)
            {
                Check("random-skip-"+trial+"-"+n,!run.EnterCheckpoint(actor,count));
                Check("random-foreign-"+trial+"-"+n,!run.EnterCheckpoint(actor+1,n));
                Check("random-legit-"+trial+"-"+n,run.EnterCheckpoint(actor,n));
                Check("random-dupe-"+trial+"-"+n,!run.EnterCheckpoint(actor,n));
            }
            Check("random-premature-"+trial,run.CheckpointIndex==count);
            Check("random-finish-"+trial,run.EnterFinish(actor));
            Check("random-double-"+trial,!run.EnterFinish(actor));
            Check("random-eventcount-"+trial,cpCount==count&&completed==1);
            run.Retry();run.Begin();
            Check("random-new-run-"+trial,run.CheckpointIndex==0&&run.State==CHL003CourseCore.Phase.Running);
        }
        // SAME FixedUpdate regression, both callback orders; all checkpoints
        // passed earlier, final CP and finish physically co-located.
        for(int order=0;order<2;order++)
        {
            var arbiter=new CHL003FinishArbiter();
            const long step=100;
            if(order==0)
            {
                Check("finish-first-queue",arbiter.ObserveFinishContact(step,true));
                arbiter.LastCheckpointAccepted(step);
            }
            else
            {
                arbiter.LastCheckpointAccepted(step);
                Check("checkpoint-first-queue",arbiter.ObserveFinishContact(step,true));
            }
            Check("never-complete-same-step-"+order,
                !arbiter.TryCommit(step,true,true,false));
            Check("physical-overlap-mandatory-"+order,
                !arbiter.TryCommit(step+1,true,false,false));
            arbiter.Reset();
            arbiter.ObserveFinishContact(step,true);
            arbiter.LastCheckpointAccepted(step);
            Check("one-fixed-step-later-"+order,
                arbiter.TryCommit(step+1,true,true,false));
            Check("completion-consumed-once-"+order,
                !arbiter.TryCommit(step+2,true,true,false));
            arbiter.Reset();
            Check("early-finish-ignored-"+order,
                !arbiter.ObserveFinishContact(step,false));
            Check("retry-cancels-contact-"+order,
                !arbiter.TryCommit(step+1,true,true,false));
        }
        var noPreviousCp=new CHL003FinishArbiter();
        noPreviousCp.ObserveFinishContact(30,true);
        Check("missing-last-checkpoint-does-not-win",
            !noPreviousCp.TryCommit(31,true,true,false));
        noPreviousCp.LastCheckpointAccepted(32);
        Check("pause-refuses-queued-finish",
            !noPreviousCp.TryCommit(33,true,true,true));
        noPreviousCp.CancelPending();
        Check("paused-queue-cleared",
            !noPreviousCp.TryCommit(34,true,true,false));
        Check("invalid-arbiter-step",
            Throws<ArgumentOutOfRangeException>(()=>noPreviousCp.ObserveFinishContact(-1,true)));
        var m=new CHL003TrialMetrics();
        m.Reset("LIGHT",1,270f,2.0f,2.3f);
        m.Begin();
        m.RecordPhysics(0f,1.02f,0f,0f,0f);
        m.RecordPhysics(0f,1.12f,1f,5f,.10f);
        m.RecordCollision("CHL003_REAL_LOW_BARRIER",4,true);
        m.RecordCollision("CHL003_SOLID_RAMP",2,true);
        m.RecordCollision("CHL003_NARROW_SOLID_BRIDGE",3,true);
        m.RecordCheckpoint(1,.15f);
        m.RecordCompleted(.2f);
        Check("real-contact-source-counters",
            m.CollisionEntries==3 && m.ContactPoints==9 &&
            m.BarrierContacts==4 && m.RampContacts==2 && m.BridgeContacts==3);
        Check("physical-frame-telemetry",
            m.PhysicsSamples==2 && m.PeakSpeedMetersPerSecond==5f &&
            m.DistanceMeters>1f && m.MaxHeightMeters>m.MinHeightMeters);
        Check("event-and-aggregate-receipt",
            m.ToCsv().Contains("CHECKPOINT") && m.ToCsv().Contains("COLLISION") &&
            m.SummaryCsv().Contains("Completed"));
        Check("exactly-one-run-completion",m.Completions==1);
        m.RecordCompleted(5f);
        Check("duplicate-completion-rejected",m.Completions==1);
        Check("negative-physics-speed-red",
            Throws<ArgumentOutOfRangeException>(()=>m.RecordPhysics(0,0,0,-1,.2f)));
        Check("invalid-contact-count-red",
            Throws<ArgumentOutOfRangeException>(()=>m.RecordCollision("BARRIER",-1,true)));
        Check("wrong-checkpoint-order-red",
            Throws<InvalidOperationException>(()=> {var u=new CHL003TrialMetrics();
                u.Reset("HEAVY",1,1150,2.82f,3.4f);u.Begin();
                u.RecordCheckpoint(2,.1f);}));
        m.Reset("HEAVY",2,1150f,2.82f,3.4f);m.Begin();
        Check("heavy-distinct-mass-and-footprint",
            m.MassKg==1150f && m.WidthMeters==2.82f && m.LengthMeters==3.4f &&
            m.PhysicsSamples==0 && m.ContactPoints==0 && m.Completions==0);
        m.RecordPhysics(0,1,0,0,0);
        m.RecordFailed("FellOffCourse",.5f);
        Check("fall-tracked-not-win",m.Falls==1 && m.Completions==0 &&
            m.TerminalState=="FellOffCourse");
        m.Reset("HEAVY",3,1150f,2.82f,3.4f);
        Check("retry-clears-fall-and-distance",
            m.Falls==0 && m.PhysicsSamples==0 && m.DistanceMeters==0 &&
            m.ContactPoints==0 && m.Checkpoints==0);
        // Deliberate non-finite input and bad profile must fail closed.
        Check("invalid-profile-red",
            Throws<ArgumentException>(()=>new CHL003TrialMetrics().Reset("UNKNOWN",1,1,1,1)));
        Check("invalid-height-red",
            Throws<ArgumentOutOfRangeException>(()=> {
                var u=new CHL003TrialMetrics();u.Reset("LIGHT",1,1,1,1);u.Begin();
                u.RecordPhysics(0,float.NaN,0,0,0);}));
        Console.WriteLine("CHL003_FINISH_ARBITER_AND_TELEMETRY_CHECKS_COMPLETE");
        Console.WriteLine("CHL003_ACTUAL_CSHARP_CORE PASS="+pass+" FAIL="+fail+
            " NATIVE_UNITY=NOT_RUN");
        return fail==0?0:2;
    }
}
