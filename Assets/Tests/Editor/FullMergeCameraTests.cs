using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor.SceneManagement;
using RogueMachine.P12PipelineSafe;
using RoboRougelike.R1;
using B01.CAMERA001;

public sealed class FullMergeCameraTests
{
    R1RunController run;
    [UnitySetUp] public IEnumerator Setup(){EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();while(R1RunController.Instance?.Progress==null)yield return null;run=R1RunController.Instance;Assert.That(run.PrepareUnifiedRun(),Is.True);yield return null;yield return null;}
    [UnityTest] public IEnumerator FacadeDelegatesToExactlyOneDisabledAutonomousRigAndGarageKeepsPose()
    {
        var follow=Object.FindFirstObjectByType<P12PipelineSafeCamera>();
        var rig=follow.GetComponent<Camera001Rig>();Assert.That(rig,Is.Not.Null,"Imported CAMERA must actually own the delegated pose");
        Assert.That(rig.enabled,Is.False,"Facade steps it once; autonomous LateUpdate stays off");
        Vector3 p=follow.transform.position;Quaternion q=follow.transform.rotation;yield return null;yield return null;
        Assert.That(Vector3.Distance(follow.transform.position,p),Is.LessThan(.001f));Assert.That(Quaternion.Angle(follow.transform.rotation,q),Is.LessThan(.001f));
        Assert.That(run.ChooseGeneratedArena(),Is.True);while(run.Transitioning)yield return null;yield return null;
        Assert.That(rig.enabled,Is.False);Assert.That(rig.ActualOwnHullIntersectsCrosshair,Is.False);
    }
    [UnityTest] public IEnumerator OwnRootLevelProjectileIsNotConsumedAsCrosshairTarget()
    {
        var camera=Camera.main;var ray=camera.ViewportPointToRay(new Vector3(.5f,.5f));
        Vector3 before=run.Player.Controller.ModuleAimPoint();
        var shell=GameObject.CreatePrimitive(PrimitiveType.Sphere);shell.transform.position=ray.GetPoint(1);shell.transform.localScale=Vector3.one*.4f;
        var body=shell.AddComponent<Rigidbody>();body.isKinematic=true;
        shell.AddComponent<P17PhysicalShell>().Configure(run.Player.transform,1,6,ray.direction,null);
        Physics.SyncTransforms();Vector3 after=run.Player.Controller.ModuleAimPoint();
        Assert.That(Vector3.Distance(before,after),Is.LessThan(.001f),"Own projectile must not steal the actual firing target");Object.Destroy(shell);yield return null;
    }
    [UnityTest] public IEnumerator DelegatedCameraIgnoresTransientDebrisButStillClipsRealWalls()
    {
        var follow=Object.FindFirstObjectByType<P12PipelineSafeCamera>();var rig=follow.GetComponent<Camera001Rig>();Assert.That(rig,Is.Not.Null);
        var c=GameObject.CreatePrimitive(PrimitiveType.Cube);c.transform.position=run.Player.transform.position+Vector3.up*2;c.transform.localScale=Vector3.one;
        c.AddComponent<RoboRougelike.R3.R3Debris>();c.GetComponent<Collider>().enabled=true;
        var filter=typeof(Camera001Rig).GetProperty("WorldColliderFilter");Assert.That(filter,Is.Not.Null);
        var predicate=(System.Func<Collider,bool>)filter.GetValue(rig);Assert.That(predicate(c.GetComponent<Collider>()),Is.False);
        Object.Destroy(c);yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
