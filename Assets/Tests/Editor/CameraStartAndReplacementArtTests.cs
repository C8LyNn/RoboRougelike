using System.Collections;
using System.Linq;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.R4_1;
using RogueMachine.P12PipelineSafe;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class CameraStartAndReplacementArtTests
{
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        float until=Time.realtimeSinceStartup+20;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<until)yield return null;
        Assert.That(R1RunController.Instance,Is.Not.Null);
    }
    [UnityTest] public IEnumerator TeleportSynchronizesVisualTransformWhilePhysicsIsPaused()
    {
        var run=R1RunController.Instance;Assert.That(run.PrepareUnifiedRun(),Is.True);
        var body=run.Player.GetComponent<Rigidbody>();Assert.That(body.interpolation,Is.EqualTo(RigidbodyInterpolation.Interpolate));
        Assert.That(Time.timeScale,Is.Zero);Assert.That(run.Player.transform.position.y,Is.GreaterThan(400));
        var position=new Vector3(3,.7f,-22);var rotation=Quaternion.Euler(0,90,0);
        run.Player.MoveTo(position,rotation);
        Assert.That(Vector3.Distance(body.position,position),Is.LessThan(.001f));
        Assert.That(Vector3.Distance(run.Player.transform.position,position),Is.LessThan(.001f),"Rendered pose must not stay in the workshop while physics is paused.");
        Assert.That(Quaternion.Angle(run.Player.transform.rotation,rotation),Is.LessThan(.01f));yield return null;
    }
    [UnityTest] public IEnumerator StartingBattleFramesTheActualVehicleWithoutWaitingForCameraToTravelFromGarage()
    {
        var run=R1RunController.Instance;Assert.That(run.PrepareUnifiedRun(),Is.True);
        Assert.That(run.Player.transform.position.y,Is.GreaterThan(400));
        Assert.That(run.StartNextEncounter(),Is.True);Assert.That(run.ChooseRoute(0),Is.True);
        float until=Time.realtimeSinceStartup+20;while(run.Transitioning&&Time.realtimeSinceStartup<until)yield return null;
        Assert.That(run.Transitioning,Is.False);run.Encounter.SetCombatActive(false);yield return null;
        var camera=Object.FindFirstObjectByType<P12PipelineSafeCamera>();var body=run.Player.GetComponent<Rigidbody>();
        var distance=Vector3.Distance(camera.transform.position,body.position);
        Debug.Log($"CAM_START_QA camera={camera.transform.position} body={body.position} transform={run.Player.transform.position} distance={distance} requested={camera.RequestedZoomDistance} fit={camera.ReceiverRig.ReceiverFitDistance} bounds={camera.ReceiverRig.LastHullBounds}");
        Assert.That(distance,Is.LessThan(35f),"Camera must immediately frame the battle vehicle, not the old workshop.");
        Assert.That(Vector3.Distance(camera.ReceiverRig.LastHullBounds.center,body.position),Is.LessThan(15f));
    }
    [UnityTest] public IEnumerator SuppliedModelsReplaceTheOldVisibleMountingCubes()
    {
        var run=R1RunController.Instance;Assert.That(run.PrepareUnifiedRun(),Is.True);yield return null;
        var parts=run.Player.GetComponent<R3VehicleAssembly>().Parts;
        var wheels=parts.Where(p=>p.Kind==R3PartKind.Wheel).ToArray();var guns=parts.Where(p=>p.Kind==R3PartKind.Gun&&p.Weapon==R4WeaponKind.Basic).ToArray();
        Assert.That(wheels.Length,Is.GreaterThan(0));Assert.That(guns.Length,Is.GreaterThan(0));
        foreach(var part in wheels.Concat(guns))
        {
            Assert.That(part.GetComponent<Renderer>().enabled,Is.False,part.Kind+" must not retain a visible primitive cube.");
            Assert.That(part.GetComponent<Collider>().enabled,Is.True,"Original collider is retained for construction and damage.");
            Assert.That(part.GetComponentsInChildren<Renderer>().Any(r=>r!=part.GetComponent<Renderer>()&&r.enabled),Is.True,"Replacement model must be visible.");
        }
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
