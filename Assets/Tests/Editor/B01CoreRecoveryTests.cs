using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R6;
using RoboRougelike.Mechanics.EMP;
using Object=UnityEngine.Object;

public class B01CoreRecoveryTests
{
    GameObject fixtures;R1RunController run;R1VehicleState vehicle;Rigidbody body;MonoBehaviour recovery;
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;run=R1RunController.Instance;
        run.SelectVehicle(R3VehicleKind.Light);run.OpenSandboxGarage();run.EnterTestRange();while(run.Transitioning)yield return null;
        fixtures=new GameObject("B01 real Core recovery fixtures");Solid(new Vector3(1000,-.5f,1000),new Vector3(30,1,30));
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        // A real contiguous broad roof makes inversion stable instead of balancing on a gun barrel.
        for(int y=1;y<=2;y++)for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++)
            if(!model.Parts.Any(p=>p.Cell==new Vector3Int(x,y,z)))model.Parts.Add(new R3PartState(new Vector3Int(x,y,z),R3PartKind.Armor));
        vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),run.GetComponent<R3VisualPalette>(),new Vector3(1000,.7f,1000),Quaternion.identity);
        vehicle.transform.SetParent(fixtures.transform,true);vehicle.Controller.SetGameplayInputEnabled(true);vehicle.Controller.enabled=false;
        body=vehicle.GetComponent<Rigidbody>();recovery=vehicle.GetComponents<MonoBehaviour>().SingleOrDefault(c=>c.GetType().Name=="R6CoreRecovery");
    }
    Collider Solid(Vector3 p,Vector3 scale)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.transform.SetParent(fixtures.transform);g.transform.position=p;g.transform.localScale=scale;return g.GetComponent<Collider>();
    }
    void Require(){Assert.That(recovery,Is.Not.Null,"Core must own the bounded emergency capability in the actual assembled vehicle");}
    int Attempts=>(int)recovery.GetType().GetProperty("AttemptsStarted").GetValue(recovery);
    float Telemetry(string name)=>Convert.ToSingle(recovery.GetType().GetProperty(name).GetValue(recovery));
    void PoseRoof()
    {
        body.useGravity=true;body.constraints=RigidbodyConstraints.None;body.interpolation=RigidbodyInterpolation.None;
        body.transform.SetPositionAndRotation(new Vector3(1000,1.55f,1000),Quaternion.Euler(0,0,180));body.position=new Vector3(1000,1.55f,1000);
        body.rotation=Quaternion.Euler(0,0,180);body.linearVelocity=body.angularVelocity=Vector3.zero;Physics.SyncTransforms();
    }
    [UnityTest] public IEnumerator PHY_CORE_01_SustainedPhysicalInversionRequiresConfirmationBeforeOneAttempt()
    {
        Require();PoseRoof();yield return MOVSeriesDeviceTests.Seconds(.6f);Assert.That(Attempts,Is.Zero);
        yield return MOVSeriesDeviceTests.Seconds(2);Assert.That(Attempts,Is.EqualTo(1));
    }
    [UnityTest] public IEnumerator PHY_CORE_02_NormalSlopeAndAirborneTumbleCannotTriggerRecovery()
    {
        Require();var ramp=Solid(new Vector3(1000,1,1000),new Vector3(12,1,20));ramp.transform.rotation=Quaternion.Euler(-10,0,0);
        var p=ramp.transform.TransformPoint(new Vector3(0,.5f,0))+ramp.transform.up*.65f;
        body.interpolation=RigidbodyInterpolation.None;body.useGravity=true;body.constraints=RigidbodyConstraints.None;
        body.transform.SetPositionAndRotation(p,ramp.transform.rotation);body.position=p;body.rotation=ramp.transform.rotation;Physics.SyncTransforms();
        yield return MOVSeriesDeviceTests.Seconds(2);Assert.That(Attempts,Is.Zero);
        // Freeze height only to isolate terrain detection from the slow-motion gate in an airborne body.
        body.constraints=RigidbodyConstraints.FreezePositionY;body.transform.SetPositionAndRotation(new Vector3(1000,100,1000),Quaternion.Euler(0,0,180));
        body.position=new Vector3(1000,100,1000);body.rotation=Quaternion.Euler(0,0,180);body.linearVelocity=body.angularVelocity=Vector3.zero;
        Physics.SyncTransforms();yield return MOVSeriesDeviceTests.Seconds(2.5f);Assert.That(Attempts,Is.Zero);
        Assert.That(body.position.y,Is.EqualTo(100).Within(.01f));Assert.That(body.angularVelocity.magnitude,Is.LessThan(.01f));
    }
    [UnityTest] public IEnumerator PHY_CORE_03_LivingCoreReturnsActualBodyToWheelsWithoutHealingOrRestoringParts()
    {
        Require();var hp=vehicle.Core;hp.RestoreHealthRatio(.65f);float originalHp=hp.CurrentHp;
        var assembly=vehicle.GetComponent<R3VehicleAssembly>();int count=assembly.Parts.Count;
        PoseRoof();yield return MOVSeriesDeviceTests.Seconds(6);
        Debug.Log($"PHY Core result attempts={Attempts} up={body.transform.up} pos={body.position} v={body.linearVelocity} w={body.angularVelocity}");
        Assert.That(Attempts,Is.EqualTo(1));Assert.That(Vector3.Dot(body.transform.up,Vector3.up),Is.GreaterThan(.75f));
        Assert.That(hp.CurrentHp,Is.EqualTo(originalHp));Assert.That(assembly.Parts.Count,Is.EqualTo(count));
        vehicle.GetComponent<R6VehicleDrive>().RefreshMass();
        Assert.That(vehicle.GetComponent<R6VehicleDrive>().GroundedWheels,Is.GreaterThan(0));
    }
    [UnityTest] public IEnumerator PHY_CORE_04_DeadCoreCannotRightTheVehicle()
    {
        Require();PoseRoof();vehicle.Core.ApplyDamage(100000,vehicle.Core.transform.position,Vector3.zero);vehicle.Structure.EvaluateStructure();
        yield return MOVSeriesDeviceTests.Seconds(3);Assert.That(Attempts,Is.Zero);
    }
    [UnityTest] public IEnumerator PHY_CORE_04_EmpOrPausedRunCannotSpendTheEmergencyAttempt()
    {
        Require();PoseRoof();var emp=vehicle.Core.GetComponent<EmpReceiver>();Assert.That(emp,Is.Not.Null,"The new Core capability must honor existing EMP ownership/state");
        emp.ApplyEmp(6);yield return MOVSeriesDeviceTests.Seconds(2.5f);Assert.That(Attempts,Is.Zero);
        run.TogglePause();yield return null;Assert.That(Attempts,Is.Zero);run.TogglePause();
        Assert.That(vehicle.GetComponent<R3VehicleAssembly>().Model.Parts.Single(p=>p.Kind==R3PartKind.Core).DeviceReadyAt,Is.Zero);
    }
    [UnityTest] public IEnumerator PHY_CORE_05_CooldownAndRealObstructionBoundMomentumAndDoNotPermitAirRetries()
    {
        Require();PoseRoof();
        var tuning=recovery.GetType().GetField("Tuning").GetValue(recovery);tuning.GetType().GetField("MaximumTorqueNm").SetValue(tuning,200f);
        // A physical low ceiling prevents the full rotation; no pose assignment can tunnel through it.
        var ceiling=Solid(new Vector3(1000,2.45f,1000),new Vector3(8,.3f,8));
        float maxY=body.position.y,maxSpeed=0,until=Time.time+5;
        while(Time.time<until){maxY=Mathf.Max(maxY,body.position.y);maxSpeed=Mathf.Max(maxSpeed,body.linearVelocity.magnitude);yield return null;}
        Assert.That(Attempts,Is.EqualTo(1));Assert.That(maxY,Is.LessThan(2.3f));Assert.That(maxSpeed,Is.LessThan(6));
        Assert.That(Telemetry("LastLiftImpulse"),Is.InRange(0,1800));Assert.That(Telemetry("LastPeakTorque"),Is.InRange(0,200.1f));
        Assert.That(Telemetry("CooldownLeft"),Is.GreaterThan(0));
        body.constraints=RigidbodyConstraints.FreezePositionY;body.transform.SetPositionAndRotation(new Vector3(1000,10,1000),Quaternion.Euler(0,0,180));
        body.position=new Vector3(1000,10,1000);body.rotation=Quaternion.Euler(0,0,180);body.useGravity=true;body.linearVelocity=body.angularVelocity=Vector3.zero;Physics.SyncTransforms();
        yield return MOVSeriesDeviceTests.Seconds(9);Assert.That(Attempts,Is.EqualTo(1),"Expired cooldown alone cannot permit an airborne retry");
    }
    [UnityTest] public IEnumerator PHY_CORE_03_HeavyAssembledRobotCanRecoverWithTheSameBoundedCapability()
    {
        vehicle.gameObject.SetActive(false);var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Heavy));
        for(int y=2;y<=3;y++)for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++)
            if(!model.Parts.Any(p=>p.Cell==new Vector3Int(x,y,z)))model.Parts.Add(new R3PartState(new Vector3Int(x,y,z),R3PartKind.Armor));
        vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),run.GetComponent<R3VisualPalette>(),new Vector3(1008,.7f,1000),Quaternion.identity);
        vehicle.transform.SetParent(fixtures.transform,true);vehicle.Controller.SetGameplayInputEnabled(true);vehicle.Controller.enabled=false;
        body=vehicle.GetComponent<Rigidbody>();recovery=vehicle.GetComponents<MonoBehaviour>().Single(c=>c.GetType().Name=="R6CoreRecovery");
        body.interpolation=RigidbodyInterpolation.None;body.transform.SetPositionAndRotation(new Vector3(1008,2.1f,1000),Quaternion.Euler(0,0,180));
        body.position=new Vector3(1008,2.1f,1000);body.rotation=Quaternion.Euler(0,0,180);Physics.SyncTransforms();
        Debug.Log($"PHY Heavy initial pose={body.position} rotation={body.rotation.eulerAngles}/{body.transform.eulerAngles}");
        yield return MOVSeriesDeviceTests.Seconds(7);vehicle.GetComponent<R6VehicleDrive>().RefreshMass();
        Debug.Log($"PHY Heavy Core attempts={Attempts} mass={body.mass} up={body.transform.up} pos={body.position} peakTorque={Telemetry("LastPeakTorque")}");
        Assert.That(Attempts,Is.EqualTo(1));Assert.That(Vector3.Dot(body.transform.up,Vector3.up),Is.GreaterThan(.75f));
        Assert.That(vehicle.GetComponent<R6VehicleDrive>().GroundedWheels,Is.GreaterThan(0));
        Assert.That(Telemetry("LastLiftImpulse"),Is.LessThanOrEqualTo(1800.1f));Assert.That(Telemetry("LastPeakTorque"),Is.LessThanOrEqualTo(30000.1f));
    }
    [UnityTest] public IEnumerator PHY_CORE_05_DefaultTorqueOffsetPayloadRemainsInsideActualCeilingAndWalls()
    {
        Require();var model=vehicle.GetComponent<R3VehicleAssembly>().Model.DeepCopy();
        model.Parts.Add(new R3PartState(new Vector3Int(2,1,0),R3PartKind.Armor));
        model.Parts.Add(new R3PartState(new Vector3Int(2,2,0),R3PartKind.Armor));
        vehicle.gameObject.SetActive(false);Object.Destroy(vehicle.gameObject);
        vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),run.GetComponent<R3VisualPalette>(),new Vector3(1000,.7f,1000),Quaternion.identity);
        vehicle.transform.SetParent(fixtures.transform,true);vehicle.Controller.SetGameplayInputEnabled(true);vehicle.Controller.enabled=false;
        body=vehicle.GetComponent<Rigidbody>();recovery=vehicle.GetComponents<MonoBehaviour>().Single(c=>c.GetType().Name=="R6CoreRecovery");PoseRoof();
        var solids=vehicle.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger&&c.attachedRigidbody==body).ToArray();
        var bounds=solids[0].bounds;foreach(var c in solids)bounds.Encapsulate(c.bounds);
        float left=bounds.min.x-.12f,right=bounds.max.x+.12f,front=bounds.min.z-.12f,back=bounds.max.z+.12f,top=bounds.max.y+.12f;
        Solid(new Vector3((left+right)/2,top+.15f,(front+back)/2),new Vector3(right-left+.6f,.3f,back-front+.6f));
        Solid(new Vector3(left-.15f,top/2,(front+back)/2),new Vector3(.3f,top,back-front+.6f));
        Solid(new Vector3(right+.15f,top/2,(front+back)/2),new Vector3(.3f,top,back-front+.6f));
        Solid(new Vector3((left+right)/2,top/2,front-.15f),new Vector3(right-left+.6f,top,.3f));
        Solid(new Vector3((left+right)/2,top/2,back+.15f),new Vector3(right-left+.6f,top,.3f));Physics.SyncTransforms();
        float health=vehicle.Core.CurrentHp;int count=model.Parts.Count;float end=Time.time+5,maxOverlap=0,maxStep=0;var previous=body.rotation;
        while(Time.time<end)
        {
            yield return new WaitForFixedUpdate();maxStep=Mathf.Max(maxStep,Quaternion.Angle(previous,body.rotation));previous=body.rotation;
            foreach(var c in solids)if(c!=null){var b=c.bounds;maxOverlap=Mathf.Max(maxOverlap,left-b.min.x,b.max.x-right,front-b.min.z,b.max.z-back,b.max.y-top,-b.min.y);}
        }
        Debug.Log($"PHY default cage mass={body.mass} attempts={Attempts} overlap={maxOverlap} step={maxStep} torque={Telemetry("LastPeakTorque")}");
        Assert.That(Attempts,Is.EqualTo(1));Assert.That((bool)recovery.GetType().GetProperty("IsRecovering").GetValue(recovery),Is.False);
        Assert.That(Telemetry("CooldownLeft"),Is.GreaterThan(0));Assert.That(Telemetry("LastPeakTorque"),Is.InRange(0,30000.1f));
        Assert.That(maxOverlap,Is.LessThan(.07f),"Actual compound geometry must remain on the permitted side of each cage surface");
        Assert.That(maxStep,Is.LessThan(15),"No instantaneous rotation step");Assert.That(vehicle.Core.CurrentHp,Is.EqualTo(health));
        Assert.That(vehicle.GetComponent<R3VehicleAssembly>().Parts.Count,Is.EqualTo(count));
    }
    [UnityTest] public IEnumerator PHY_CORE_05_RebuiltCoreKeepsCooldownAndCannotRestoreDestroyedWheel()
    {
        Require();PoseRoof();yield return MOVSeriesDeviceTests.Seconds(2.5f);Assert.That(Attempts,Is.EqualTo(1));
        var assembly=vehicle.GetComponent<R3VehicleAssembly>();var lost=assembly.Parts.First(p=>p.Kind==R3PartKind.Wheel);
        lost.Damageable.ApplyDamage(100000,lost.transform.position,Vector3.zero);vehicle.Structure.EvaluateStructure();assembly.Capture();
        var copy=assembly.Model.DeepCopy();float deadline=copy.Parts.Single(p=>p.Kind==R3PartKind.Core).DeviceReadyAt;
        Assert.That(deadline,Is.GreaterThan(Time.time));Assert.That(copy.Parts.Single(p=>p.Id==lost.Id).Health,Is.Zero);
        vehicle.gameObject.SetActive(false);
        var rebuilt=R3VehicleAssembly.Build(copy,new R1RunStats(),run.GetComponent<R3VisualPalette>(),new Vector3(1008,.7f,1000),Quaternion.identity);
        rebuilt.transform.SetParent(fixtures.transform,true);
        var next=rebuilt.GetComponents<MonoBehaviour>().Single(c=>c.GetType().Name=="R6CoreRecovery");
        Assert.That(Convert.ToSingle(next.GetType().GetProperty("CooldownLeft").GetValue(next)),Is.EqualTo(deadline-Time.time).Within(.05f));
        Assert.That(rebuilt.GetComponent<R3VehicleAssembly>().Parts.Count(p=>p.Kind==R3PartKind.Wheel),Is.EqualTo(3));yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
