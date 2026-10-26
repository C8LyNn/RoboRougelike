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
        Console.WriteLine("CAMERA_DIAG_EXACT_SOURCE_CSHARP_PASS="+passed+
            " FAIL="+failed+" NATIVE_UNITY=NOT_RUN");
        return failed==0?0:2;
    }
}
