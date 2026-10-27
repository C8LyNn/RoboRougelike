using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R1;
using RogueMachine.P12PipelineSafe;

public sealed class Camera275NativeTests
{
    R1RunController run;
    P12PipelineSafeCamera follow;
    Camera275EditorInputProbe probe;
    GameObject target;
    static readonly BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static float Field(object obj,string key)=>(float)obj.GetType().GetField(key,flags).GetValue(obj);
    static void Field(object obj,string key,float value)=>obj.GetType().GetField(key,flags).SetValue(obj,value);
    [UnitySetUp]public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        run=R1RunController.Instance;Assert.That(run.OpenSandboxGarage(),Is.True);
        yield return null;yield return null;
        Assert.That(run.EnterTestRange(),Is.True);
        while(run.Transitioning)yield return null;
        follow=Object.FindFirstObjectByType<P12PipelineSafeCamera>();
        // A stationary aim fixture, not a substitute for the separate real R6 drive tests.
        run.Player.GetComponent<Rigidbody>().isKinematic=true;
        var range=run.GetComponent<RoboRougelike.R6.R6TestRange>();
        range.Layout.gameObject.SetActive(false);
        Field(follow,"yaw",0);Field(follow,"pitch",18);
        yield return new WaitForSecondsRealtime(1);
    }
    [UnityTest]public IEnumerator VisibleLevelTargetAt100MetersCanBePlacedUnderCrosshair()
    {
        target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.name="Camera275 stationary aim target 100m";
        target.transform.position=run.Player.transform.position+Vector3.forward*100+Vector3.up*1.5f;
        target.transform.localScale=new Vector3(4,3,2);Physics.SyncTransforms();
        for(int frame=0;frame<120;frame++)
        {
            var viewport=Camera.main.WorldToViewportPoint(target.transform.position);
            Field(follow,"pitch",Field(follow,"pitch")-(viewport.y-.5f)*30);
            yield return null;
        }
        Debug.Log($"[CAM275] range100 finalPitch={Field(follow,"pitch")} rayHit={run.Player.Controller.LastConsumedAimCollider?.name} point={run.Player.Controller.LastConsumedAimPoint}");
        Assert.That(run.Player.Controller.LastConsumedAimCollider,Is.EqualTo(target.GetComponent<Collider>()),"The player must be able to aim at a visible level target 100m away; first floor hit is not that target");
        Assert.That(follow.ReceiverRig.ActualOwnHullIntersectsCrosshair,Is.False);
    }
    [UnityTest]public IEnumerator MouseOrbitRespondsOnTheRenderedFrameWithoutFollowSpeedLag()
    {
        float before=Camera.main.transform.eulerAngles.y;
        probe=new GameObject("CAM275 native mouse probe").AddComponent<Camera275EditorInputProbe>();
        yield return null;yield return null;
        Assert.That(probe.Sent,Is.True);Assert.That(probe.Observed.x,Is.EqualTo(100).Within(.01f));
        float requested=Field(follow,"yaw");float actual=Camera.main.transform.eulerAngles.y;
        Debug.Log($"[CAM275] mouse requestedYaw={requested} actualYaw={actual} before={before}");
        Assert.That(Mathf.Abs(Mathf.DeltaAngle(before,requested)),Is.GreaterThan(10),"Actual mouse input reached the production facade");
        Assert.That(Mathf.Abs(Mathf.DeltaAngle(actual,requested)),Is.LessThan(1.5f),"Follow damping must not make the deliberate mouse orbit lag several degrees");
        Assert.That(follow.ReceiverRig.WorldOverlapAtCamera,Is.False);
        Assert.That(follow.ReceiverRig.ActualOwnHullIntersectsCrosshair,Is.False);
    }
    [UnityTest]public IEnumerator ScrollZoomIsSmoothAndRemainsAnchoredToVehicle()
    {
        var native=new GameObject("CAM275 scroll input").AddComponent<Camera275RuntimeInput>();
        Vector3 root=run.Player.transform.position;float before=Vector3.Distance(follow.transform.position,follow.ReceiverRig.LastSolution.AimWorld);
        native.Scroll=new Vector2(0,-2400);
        yield return null;yield return null;
        Assert.That(follow.RequestedZoomDistance,Is.GreaterThan(40));
        Assert.That(follow.SmoothedZoomDistance,Is.LessThan(follow.RequestedZoomDistance),"Wheel changes target distance; rendered distance eases rather than snaps");
        float first=Vector3.Distance(follow.transform.position,follow.ReceiverRig.LastSolution.AimWorld);
        Assert.That(first-before,Is.LessThan(1));
        yield return new WaitForSecondsRealtime(3);
        Assert.That(Vector3.Distance(follow.transform.position,follow.ReceiverRig.LastSolution.AimWorld),Is.GreaterThan(40));
        Assert.That(follow.TargetRoot,Is.EqualTo(run.Player.transform));
        Assert.That(Vector3.Distance(root,run.Player.transform.position),Is.LessThan(.001f));
        Assert.That(follow.ReceiverRig.ActualOwnHullIntersectsCrosshair,Is.False);
        native.Scroll=new Vector2(0,3600);yield return null;yield return null;
        yield return new WaitForSecondsRealtime(3);
        Assert.That(follow.RequestedZoomDistance,Is.EqualTo(3).Within(.01));
        Assert.That(follow.TargetRoot,Is.EqualTo(run.Player.transform));
        Assert.That(follow.ReceiverRig.ActualOwnHullIntersectsCrosshair,Is.False);
        Object.DestroyImmediate(native.gameObject);
    }
    [UnityTearDown]public IEnumerator Cleanup()
    {
        if(probe!=null)Object.DestroyImmediate(probe.gameObject);
        if(target!=null)Object.Destroy(target);
        if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}
    }
}
