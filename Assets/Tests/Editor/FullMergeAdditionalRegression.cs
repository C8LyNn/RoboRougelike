using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.MapSystemR4;
using RogueMachine.P12PipelineSafe;
using B01.CAMERA001;
public sealed class FullMergeAdditionalRegression
{
    R1RunController run;
    [UnitySetUp]public IEnumerator Setup(){EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();while(R1RunController.Instance?.Progress==null)yield return null;run=R1RunController.Instance;run.SelectVehicle(R3VehicleKind.Light);Assert.That(run.OpenSandboxGarage(),Is.True);yield return null;yield return null;}
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,v);
    [UnityTest]public IEnumerator MinimumPitchFramesActualRobotInsideViewport()
    {
        run.Garage.Close();yield return null;
        var follow=Object.FindFirstObjectByType<P12PipelineSafeCamera>();follow.Configure(run.Player.transform,run.Player.CameraPivot);follow.SetGameplayInputEnabled(true);
        Set(follow,"yaw",0f);Set(follow,"pitch",18f);Set(follow,"distance",7.8f);
        typeof(P12PipelineSafeCamera).GetMethod("LateUpdate",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(follow,null);
        var b=Camera001Rig.GetHullBounds(run.Player.transform);var camera=Camera.main;
        foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1})foreach(int z in new[]{-1,1}){
            var p=camera.WorldToViewportPoint(b.center+Vector3.Scale(b.extents,new Vector3(x,y,z)));
            Assert.That(p.z,Is.GreaterThan(0));Assert.That(p.y,Is.InRange(.02f,.98f),"Own hull clipped below image despite safe centre ray");
            Assert.That(p.x,Is.InRange(.02f,.98f));
        }
    }
    [UnityTest]public IEnumerator CameraKeepsUpWithFastActualBodyWithoutBypassingItsSweep()
    {
        Assert.That(run.EnterTestRange(),Is.True);while(run.Transitioning)yield return null;
        run.Player.Controller.enabled=false;var body=run.Player.GetComponent<Rigidbody>();var rig=Camera.main.GetComponent<Camera001Rig>();var start=body.position;
        // Explicit velocity-controlled camera stress fixture, NOT a driving acceptance.
        float deadline=Time.realtimeSinceStartup+8;
        for(int i=0;i<100;i++){body.linearVelocity=Vector3.forward*22;float before=Time.fixedTime;while(Time.fixedTime<=before){Assert.That(Time.realtimeSinceStartup,Is.LessThan(deadline),"Owned fixed-clock fixture must not hang");yield return null;}}
        yield return null;
        Assert.That(body.position.z-start.z,Is.GreaterThan(30));
        Assert.That(Vector3.Distance(rig.transform.position,rig.LastSolution.DesiredCameraWorld),Is.LessThan(5),"Total15m/s cap loses a22m/s robot indefinitely");
        Assert.That(rig.WorldOverlapAtCamera,Is.False);
    }
    [UnityTest]public IEnumerator LostPartRestorationCannotSpendOrRecreateThroughForeignCollider()
    {
        var assembly=run.Player.GetComponent<R3VehicleAssembly>();var part=assembly.Parts.Single(p=>p.Cell==new Vector3Int(1,0,2));var cell=part.Cell;
        part.Damageable.ApplyDamage(100000,part.transform.position,Vector3.zero);assembly.Capture();yield return null;yield return null;
        Assert.That(run.VehicleModel.At(cell).Health,Is.Zero);
        var obstacle=GameObject.CreatePrimitive(PrimitiveType.Cube);obstacle.transform.position=run.Player.transform.TransformPoint((Vector3)cell*.55f);obstacle.transform.localScale=Vector3.one*.3f;Physics.SyncTransforms();
        int scrap=run.VehicleModel.Scrap;Assert.That(run.GarageRepair(cell),Is.False);Assert.That(run.VehicleModel.Scrap,Is.EqualTo(scrap));Assert.That(run.VehicleModel.At(cell).Health,Is.Zero);
        Object.Destroy(obstacle);yield return null;Assert.That(run.GarageRepair(cell),Is.True);
    }
    [UnityTest]public IEnumerator FactoryWheelCanReturnAndRestoreWithoutIgnoringForgedForeignColliders()
    {
        var old=run;run.RestartRun();while(R1RunController.Instance==old||R1RunController.Instance?.Progress==null)yield return null;
        run=R1RunController.Instance;run.SelectVehicle(R3VehicleKind.Standard);Assert.That(run.OpenSandboxGarage(),Is.True);yield return null;yield return null;
        var cell=new Vector3Int(-3,0,0);string id=run.VehicleModel.At(cell).Id;int money=run.VehicleModel.Scrap;
        Assert.That(run.GarageRemove(cell),Is.True);yield return null;
        Assert.That(run.VehicleModel.OwnedPlacementError(id,cell,default),Is.Null);
        Assert.That(run.GarageMount(id,cell),Is.True);yield return null;
        Assert.That(run.VehicleModel.At(cell).Id,Is.EqualTo(id));Assert.That(run.VehicleModel.Scrap,Is.EqualTo(money));
        var assembly=run.Player.GetComponent<R3VehicleAssembly>();var wheel=assembly.Parts.Single(p=>p.Id==id);
        wheel.Damageable.ApplyDamage(100000,wheel.transform.position,Vector3.zero);assembly.Capture();yield return null;yield return null;
        var obstacle=GameObject.CreatePrimitive(PrimitiveType.Cube);obstacle.transform.position=run.Player.transform.TransformPoint((Vector3)cell*.55f);obstacle.transform.localScale=Vector3.one*.2f;
        obstacle.AddComponent<R3MountedPart>().Id=id;Physics.SyncTransforms();
        Assert.That(run.GarageRepair(cell),Is.False,"A forged matching ID outside the vehicle never gains factory exemption");
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(money));Assert.That(run.VehicleModel.At(cell).Health,Is.Zero);
        Object.Destroy(obstacle);yield return null;Assert.That(run.GarageRepair(cell),Is.True);
        Assert.That(run.VehicleModel.At(cell).Id,Is.EqualTo(id));Assert.That(run.VehicleModel.At(cell).Health,Is.EqualTo(1));
    }
    [UnityTearDown]public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
