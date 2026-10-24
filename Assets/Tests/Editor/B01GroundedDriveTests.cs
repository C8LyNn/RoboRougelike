using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R6;

public class B01GroundedDriveTests
{
    GameObject fixtures;R1VehicleState vehicle;Rigidbody body;R6VehicleDrive drive;Collider floor;
    R3VehicleAssembly assembly;
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        R1RunController.Instance.OpenSandboxGarage();Time.timeScale=1;
        fixtures=new GameObject("B01 actual physical contact fixtures");
        floor=Solid(new Vector3(0,-.5f,1000),new Vector3(80,1,80));
        vehicle=R3VehicleAssembly.Build(new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light)),new R1RunStats(),
            Object.FindFirstObjectByType<R3VisualPalette>(),new Vector3(0,.7f,1000),Quaternion.identity);
        vehicle.transform.SetParent(fixtures.transform,true);vehicle.Controller.SetGameplayInputEnabled(true);vehicle.Controller.enabled=false;
        assembly=vehicle.GetComponent<R3VehicleAssembly>();body=vehicle.GetComponent<Rigidbody>();drive=vehicle.GetComponent<R6VehicleDrive>();
        yield return MOVSeriesDeviceTests.FixedStep();
    }
    Collider Solid(Vector3 p,Vector3 size)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.transform.SetParent(fixtures.transform);g.transform.position=p;g.transform.localScale=size;
        return g.GetComponent<Collider>();
    }
    void Pose(Vector3 p,Quaternion q)
    {
        body.useGravity=false;body.constraints=RigidbodyConstraints.None;body.position=p;body.rotation=q;
        body.linearVelocity=body.angularVelocity=Vector3.zero;Physics.SyncTransforms();drive.RefreshMass();
        // Recovery must not contaminate a test isolating the Wheel force boundary.
        foreach(var b in vehicle.GetComponents<MonoBehaviour>())if(b.GetType().Name=="R6CoreRecovery")b.enabled=false;
    }
    int Contacts {get{var p=drive.GetType().GetProperty("GroundedWheels");Assert.That(p,Is.Not.Null,"Expose actual grounded wheels separately from installed wheels");return (int)p.GetValue(drive);}}
    IEnumerator Motor(float seconds,bool ai=false)
    {
        float end=Time.time+seconds;while(Time.time<end)
        {if(ai)drive.StepWorld(Vector3.forward,10);else drive.Step(1,1,10,5,90);yield return MOVSeriesDeviceTests.FixedStep();}
    }
    [UnityTest] public IEnumerator PHY_GROUND_01_FourRealTreadsOnFloorDriveAndSteer()
    {
        Pose(new Vector3(0,.65f,1000),Quaternion.identity);body.constraints=RigidbodyConstraints.FreezePositionY|RigidbodyConstraints.FreezeRotationX|RigidbodyConstraints.FreezeRotationZ;
        yield return Motor(.35f);Assert.That(body.linearVelocity.z,Is.GreaterThan(.5f));Assert.That(body.angularVelocity.y,Is.GreaterThan(.1f));
        Assert.That(Contacts,Is.EqualTo(4));Assert.That(drive.Wheels,Is.EqualTo(4));
    }
    [UnityTest] public IEnumerator PHY_GROUND_02_AirborneWheelsCannotAddHorizontalDriveOrYaw()
    {
        Pose(new Vector3(0,5,1000),Quaternion.identity);body.linearVelocity=new Vector3(1,0,2);yield return Motor(.25f);
        Assert.That(body.linearVelocity.x,Is.EqualTo(1).Within(.03f),"Air input must not cancel lateral inertia");
        Assert.That(body.linearVelocity.z,Is.EqualTo(2).Within(.03f),"Installed wheels cannot thrust without terrain contact");
        Assert.That(body.angularVelocity.y,Is.Zero.Within(.01f));Assert.That(Contacts,Is.Zero);
    }
    [UnityTest] public IEnumerator PHY_GROUND_02_AIWorldDriveCannotThrustInAir()
    {
        Pose(new Vector3(0,5,1000),Quaternion.identity);yield return Motor(.25f,true);
        Assert.That(body.linearVelocity.magnitude,Is.LessThan(.01f));Assert.That(Contacts,Is.Zero);
    }
    [UnityTest] public IEnumerator PHY_GROUND_03_DestroyedAndDetachedWheelsImmediatelyLoseContactAuthority()
    {
        Pose(new Vector3(0,.65f,1000),Quaternion.identity);drive.Step(0,0,10,5,90);Assert.That(Contacts,Is.EqualTo(4));
        var wheels=assembly.Parts.Where(p=>p.Kind==R3PartKind.Wheel&&p.Cell.x<0).ToArray();
        for(int i=0;i<2;i++)
        {
            wheels[i].Damageable.ApplyDamage(10000,wheels[i].transform.position,Vector3.zero);vehicle.Structure.EvaluateStructure();Physics.SyncTransforms();
            drive.Step(0,0,10,5,90);Assert.That(Contacts,Is.EqualTo(3-i));
        }
        Assert.That(Mathf.Abs(drive.WheelImbalance),Is.GreaterThan(.5f));yield return null;
    }
    [UnityTest] public IEnumerator PHY_GROUND_04_InvertedTreadsCannotDriveRobotOnItsRoof()
    {
        Pose(new Vector3(0,1.2f,1000),Quaternion.Euler(0,0,180));yield return Motor(.25f);
        Assert.That(new Vector2(body.linearVelocity.x,body.linearVelocity.z).magnitude,Is.LessThan(.01f));
        Assert.That(body.angularVelocity.magnitude,Is.LessThan(.01f));Assert.That(Contacts,Is.Zero);
    }
    [UnityTest] public IEnumerator PHY_GROUND_05_OnlySupportedSideAtLedgeReceivesTraction()
    {
        floor.enabled=false;Solid(new Vector3(-1.1f,-.5f,1000),new Vector3(.7f,1,8));
        Pose(new Vector3(0,.65f,1000),Quaternion.identity);drive.Step(1,0,10,5,90);
        Assert.That(Contacts,Is.EqualTo(2));Assert.That(Mathf.Abs(drive.WheelImbalance),Is.GreaterThan(.5f));yield return null;
    }
    [UnityTest] public IEnumerator PHY_GROUND_06_FallingAndJumpMomentumSurviveLossOfContact()
    {
        Pose(new Vector3(0,5,1000),Quaternion.identity);body.useGravity=true;body.linearVelocity=new Vector3(2,3,4);
        yield return Motor(.2f);Assert.That(body.linearVelocity.x,Is.EqualTo(2).Within(.05f));Assert.That(body.linearVelocity.z,Is.EqualTo(4).Within(.05f));
        Assert.That(body.linearVelocity.y,Is.InRange(.5f,2));Assert.That(Contacts,Is.Zero);
    }
    [UnityTest] public IEnumerator PHY_GROUND_FiltersOwnTriggersAndLoosePartsInsteadOfInventingTerrain()
    {
        floor.enabled=false;var trigger=Solid(new Vector3(0,-.5f,1000),new Vector3(8,1,8));trigger.isTrigger=true;
        var loose=Solid(new Vector3(0,-.5f,1000),new Vector3(8,1,8));loose.gameObject.AddComponent<Rigidbody>().isKinematic=true;
        Pose(new Vector3(0,.65f,1000),Quaternion.identity);yield return Motor(.2f);
        Assert.That(body.linearVelocity.magnitude,Is.LessThan(.01f));Assert.That(Contacts,Is.Zero);
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
