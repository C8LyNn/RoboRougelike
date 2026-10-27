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
        Console.WriteLine("CHL003_ACTUAL_CSHARP_CORE PASS="+pass+" FAIL="+fail+
            " NATIVE_UNITY=NOT_RUN");
        return fail==0?0:2;
    }
}
