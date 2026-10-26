// Actually compiles and executes the CAMERA001 diagnostic C# source with a
// tiny UnityEngine math surrogate; native Unity/PhysX execution remains NOT_RUN.
using System;
using B01.CAMERA001;
using UnityEngine;
using Diag= B01.CAMERA001.Camera001AimTransitionDiagnostics;
class Program
{
    private static int passed,failed;
    private static void Check(string name,bool valid)
    {
        if(valid){passed++;} else {failed++;Console.WriteLine("FAIL "+name);}
    }
    private static Diag.Sample Sample(Vector3 oldTarget,Vector3 newTarget,
       Vector3 muzzle,Vector3 priorForward,Vector3 nowForward,
       float dt=1f/60f,float camErr=0f,bool own=false,bool world=false)
    {
        return new Diag.Sample{
          PreviousTargetWorld=oldTarget,CurrentTargetWorld=newTarget,
          CurrentMuzzleWorld=muzzle,PreviousMuzzleForwardWorld=priorForward,
          CurrentMuzzleForwardWorld=nowForward,DeltaTimeSeconds=dt,
          TurretSlewDegreesPerSecond=170f,CameraFinalAimErrorDegrees=camErr,
          CameraOwnHullRayHit=own,CameraWorldOverlap=world
        };
    }
    static int Main()
    {
        var m=new Vector3(.00401f,1.5552f,27.8219f);
        var old=new Vector3(.00391f,1.914f,30.8045f);
        var next=new Vector3(.00269f,0f,34.8804f);
        var before=old-m;
        var obs=Diag.Measure(Sample(old,next,m,before,before));
        Check("native-QA-exemplar-jump-budget",obs.TargetChangeExceedsOneFrameTurretBudget);
        Check("native-QA-exemplar-labelled-cautiously",
            obs.Classification=="TARGET_STEP_EXCEEDS_TURRET_BUDGET_PLAUSIBLE");
        Check("native-QA-exemplar-gap",obs.CurrentMuzzleToTargetErrorDegrees>10f);
        Check("native-QA-exemplar-positive-catchup",obs.LowerBoundCatchUpSeconds>0f);
        Check("normal-aligned",Diag.Measure(Sample(new Vector3(0,1,30),
            new Vector3(0,1,30),new Vector3(0,1,0),
            Vector3.forward,Vector3.forward)).Classification=="ALIGNED");
        Check("camera-pose-priority",Diag.Measure(Sample(old,next,m,before,before,
            camErr:.1f)).Classification=="CAMERA_FINAL_POSE_UNSAFE");
        Check("own-hull-flag",Diag.Measure(Sample(old,next,m,before,before,
            own:true)).CameraFinalPoseUnsafe);
        Check("world-overlap-flag",Diag.Measure(Sample(old,next,m,before,before,
            world:true)).CameraFinalPoseUnsafe);
        Check("stable-nonzero-unresolved",Diag.Measure(Sample(
            new Vector3(0,0,20),new Vector3(0,0,20),Vector3.zero,
            Vector3.right,Vector3.right)).Classification=="UNRESOLVED_TRACKING_GAP_REQUIRES_PAIRED_NATIVE_QA");
        bool invalidDelta=false,invalidZero=false;
        try{Diag.Measure(Sample(old,next,m,before,before,dt:0f));}
        catch(ArgumentException){invalidDelta=true;}
        try{Diag.Measure(Sample(m,next,m,before,before));}
        catch(ArgumentException){invalidZero=true;}
        Check("reject-nonpositive-dt",invalidDelta);
        Check("reject-zero-aim",invalidZero);
        var rng=new Random(20261026);
        for(int i=0;i<1000;i++)
        {
            float z=5f+(float)rng.NextDouble()*100f;
            float x=((float)rng.NextDouble()-.5f)*20f;
            float y=((float)rng.NextDouble()-.5f)*12f;
            var tgt=new Vector3(x,y,z);
            var dir=tgt-Vector3.zero;
            var v=Diag.Measure(Sample(tgt,tgt,Vector3.zero,dir,dir));
            Check("stable-random-"+i,v.Classification=="ALIGNED"&&
                v.CurrentMuzzleToTargetErrorDegrees<.05f&&
                v.TargetDirectionChangeDegrees<.05f);
        }
        // W13: compile and exercise EXACT runtime Camera001AimTimeline.cs.
        var timeline=new Camera001AimTimeline(512);
        var first=new Camera001AimTimeline.Frame{
           FrameIndex=10,HitColliderInstanceId=101,HitKind="Enemy, \"Armor\"",
           Sample=Sample(old,old,m,before,before)
        };
        var v0=timeline.Capture(first);
        Check("w13-first-frame-not-pretend-retarget",!v0.FirstHitChanged);
        Check("w13-first-frame-recorded",timeline.Count==1);
        var after=new Camera001AimTimeline.Frame{
           FrameIndex=11,HitColliderInstanceId=102,HitKind="Floor",
           Sample=Sample(next,next,m,before,before)
        };
        after.Sample.PreviousTargetWorld=next; // spoof must be ignored
        var changed=timeline.Capture(after);
        Check("w13-change-actual-collider-id",changed.FirstHitChanged);
        Check("w13-target-step-from-real-prior",changed.Observation.TargetDirectionChangeDegrees>19f);
        Check("w13-retarget-over-budget",timeline.RetargetOverBudgetCount==1);
        Check("w13-peak-error",timeline.PeakTrackingErrorDegrees>15f);
        var csv=timeline.ToCsv();
        Check("w13-csv-escaped",csv.Contains("\"Enemy, \"\"Armor\"\"\""));
        Check("w13-csv-column-header",csv.StartsWith("frame,phase,hitId,hitKind"));
        Check("w13-records-copy",timeline.Snapshot().Length==2);
        bool nonmonotonic=false;
        try{timeline.Capture(after);}catch(ArgumentException){nonmonotonic=true;}
        Check("w13-monotonic-reject",nonmonotonic&&timeline.Count==2);
        var bad=after; bad.FrameIndex=12; bad.Sample.DeltaTimeSeconds=0f;
        bool rejected=false;try{timeline.Capture(bad);}catch(ArgumentException){rejected=true;}
        Check("w13-invalid-transactional",rejected&&timeline.Count==2&&timeline.ChangedFirstHitCount==1);
        var stable=new Camera001AimTimeline(512);
        for(int i=0;i<256;i++)
        {
            var target=new Vector3(i*.01f,1f,30f);
            var gun=(target-m);
            var record=stable.Capture(new Camera001AimTimeline.Frame{
                FrameIndex=i,HitColliderInstanceId=101,HitKind="Enemy",
                Sample=Sample(target,target,m,gun,gun)
            });
            Check("w13-256-stable-"+i,!record.FirstHitChanged&&
                record.Observation.Classification=="ALIGNED");
        }
        Check("w13-stable-no-fake-switch",stable.ChangedFirstHitCount==0);
        var cap=new Camera001AimTimeline(2);
        cap.Capture(first);
        cap.Capture(after);
        bool capReject=false;
        var third=after;third.FrameIndex=12;
        try{cap.Capture(third);}catch(InvalidOperationException){capReject=true;}
        Check("w13-bound-memory",capReject&&cap.Count==2);

        // W14: demonstrate the historical wrong "monotonic-only" recorder
        // accepted frame100 -> frame110 but tested a 10-frame target jump
        // against one 1/60 second. New code rejects before state mutation.
        var skippedTimeline=new Camera001AimTimeline();
        var w14first=new Camera001AimTimeline.Frame{
            FrameIndex=100,HitColliderInstanceId=101,HitKind="Enemy",
            Sample=Sample(old,old,m,before,before)
        };
        skippedTimeline.Capture(w14first);
        var w14late=new Camera001AimTimeline.Frame{
            FrameIndex=110,HitColliderInstanceId=102,HitKind="Floor",
            Sample=Sample(next,next,m,before,before)
        };
        var w14Before=skippedTimeline.ToCsv();
        bool skipsRejected=false;
        try{skippedTimeline.Capture(w14late);}
        catch(ArgumentException e){skipsRejected=e.Message.Contains("SKIPPED_FRAME");}
        Check("w14-skipped-frames-rejected",skipsRejected);
        Check("w14-failed-capture-is-atomic",skippedTimeline.Count==1 &&
            skippedTimeline.ChangedFirstHitCount==0 && skippedTimeline.ToCsv()==w14Before);
        w14late.FrameIndex=101;
        var validW14=skippedTimeline.Capture(w14late);
        Check("w14-contiguous-frame-still-valid",validW14.FirstHitChanged &&
            validW14.Observation.TargetChangeExceedsOneFrameTurretBudget);
        var frameBounds=new Camera001AimTimeline();
        var negativeFrame=w14first;negativeFrame.FrameIndex=-1;
        bool negativeRejected=false;
        try{frameBounds.Capture(negativeFrame);}
        catch(ArgumentException e){negativeRejected=e.Message.Contains("NEGATIVE_FRAME");}
        Check("w14-negative-frame-rejected",negativeRejected && frameBounds.Count==0);
        var maxFrame=w14first;maxFrame.FrameIndex=int.MaxValue;
        frameBounds.Capture(maxFrame);
        var wrapped=w14late;wrapped.FrameIndex=int.MinValue;
        bool overflowRejected=false;
        try{frameBounds.Capture(wrapped);}
        catch(ArgumentException){overflowRejected=true;}
        Check("w14-frame-wrap-rejected",overflowRejected && frameBounds.Count==1);

        Console.WriteLine("CAMERA_DIAG_EXACT_SOURCE_CSHARP_PASS="+passed+
            " FAIL="+failed+" NATIVE_UNITY=NOT_RUN");
        return failed==0?0:2;
    }
}
