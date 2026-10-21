using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.Mechanics.Booster;
using RoboRougelike.Mechanics.MOV002JumpDrive;
using RoboRougelike.Mechanics.MOV004GyroscopicStabilizer;

public class MOVSeriesDeviceTests
{
    GameObject fixtures;R3VisualPalette palette;
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        R1RunController.Instance.OpenSandboxGarage();Time.timeScale=1;
        fixtures=new GameObject("Original MOV integration fixtures");palette=UnityEngine.Object.FindFirstObjectByType<R3VisualPalette>();
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.SetParent(fixtures.transform);
        floor.transform.position=new Vector3(0,-.5f,1000);floor.transform.localScale=new Vector3(180,1,180);
    }
    public static IEnumerator Seconds(float seconds){float end=Time.time+seconds;while(Time.time<end)yield return null;}
    // EditMode runner hosts EnterPlayMode fixtures but does not honor runtime yield objects.
    public static IEnumerator FixedStep(){float before=Time.fixedTime;while(Time.fixedTime<=before)yield return null;}
    R1VehicleState Build(R3VehicleKind kind,float x,params R3PartKind[] devices)
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(kind));
        // Real plans on contiguous top hull cells, without a second damage/ownership model.
        int y=kind==R3VehicleKind.Light?1:2;
        for(int i=0;i<devices.Length;i++)model.Parts.Add(new R3PartState(new Vector3Int(i%2==0?-1:1,y,i/2),devices[i]));
        var v=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(x,.7f,1000),Quaternion.identity);
        v.transform.SetParent(fixtures.transform,true);v.Controller.SetGameplayInputEnabled(true);return v;
    }
    static Component Adapter(R1VehicleState v)
    {
        var type=Type.GetType("RoboRougelike.MOVSeries.MOVSeriesVehicleAdapter, Assembly-CSharp");
        Assert.That(type,Is.Not.Null,"Real vehicle coordinator missing.");var c=v.GetComponent(type);Assert.That(c,Is.Not.Null);return c;
    }
    static bool Action(R1VehicleState v,string method)=>(bool)Adapter(v).GetType().GetMethod(method).Invoke(Adapter(v),null);
    [UnityTest] public IEnumerator ActualPartsBindOneBodyWithoutBecomingWeapons()
    {
        var v=Build(R3VehicleKind.Light,0,R3PartKind.Booster,R3PartKind.JumpDrive,R3PartKind.Gyroscope);
        Adapter(v);Assert.That(v.GetComponentsInChildren<VehicleBooster>().Length,Is.EqualTo(1));
        Assert.That(v.GetComponentsInChildren<JumpDrive>().Single().VehicleBody,Is.SameAs(v.GetComponent<Rigidbody>()));
        Assert.That(v.GetComponentsInChildren<GyroStabilizer>().Single().VehicleBody,Is.SameAs(v.GetComponent<Rigidbody>()));
        Assert.That(v.Modules.ActiveWeaponCount,Is.EqualTo(1));Assert.That(v.GetComponentsInChildren<Rigidbody>().Length,Is.EqualTo(1));
        Assert.That(v.GetComponent<Rigidbody>().useGravity,Is.True);yield return null;
    }
    [UnityTest] public IEnumerator BoosterHasFiniteMassSensitiveThrustAndPersistentCooldown()
    {
        var kinds=new[]{R3VehicleKind.Light,R3VehicleKind.Standard,R3VehicleKind.Heavy};var speeds=new float[3];
        for(int i=0;i<3;i++)
        {
            var v=Build(kinds[i],-40+i*40,R3PartKind.Booster,R3PartKind.Gyroscope);yield return Seconds(.6f);
            Assert.That(Action(v,"TryBoost"),Is.True);Assert.That(Action(v,"TryBoost"),Is.False);yield return Seconds(.8f);
            var body=v.GetComponent<Rigidbody>();speeds[i]=body.linearVelocity.z;
            var boost=v.GetComponentInChildren<VehicleBooster>();var material=v.GetComponentInChildren<Collider>().sharedMaterial;
            Debug.Log($"MOV001 actual {kinds[i]} mass={body.mass:F1} vz={speeds[i]:F3} active={boost.IsBoosting} remain={boost.RemainingBoostSeconds:F3} control={v.Controller.GameplayInputEnabled} up={body.rotation*Vector3.up} friction={material.dynamicFriction} available={boost.IsOperational} pos={body.position}");
            Assert.That(speeds[i],Is.InRange(.3f,10));
            var model=v.GetComponent<R3VehicleAssembly>().Model;var state=model.Parts.Single(p=>p.Kind==R3PartKind.Booster);
            Assert.That(state.DeviceReadyAt,Is.GreaterThan(Time.time));
            v.gameObject.SetActive(false);var rebuilt=R3VehicleAssembly.Build(model.DeepCopy(),new R1RunStats(),palette,new Vector3(-40+i*40,.7f,1020),Quaternion.identity);
            rebuilt.transform.SetParent(fixtures.transform,true);rebuilt.Controller.SetGameplayInputEnabled(true);
            Assert.That(Action(rebuilt,"TryBoost"),Is.False,"Rebuild must not reset cooldown.");rebuilt.gameObject.SetActive(false);
        }
        Assert.That(speeds[0],Is.GreaterThan(speeds[1]));Assert.That(speeds[1],Is.GreaterThan(speeds[2]));
    }
    [UnityTest] public IEnumerator JumpPreservesHorizontalMomentumRejectsAirAndCooperatesWithBoostGyro()
    {
        var v=Build(R3VehicleKind.Standard,0,R3PartKind.JumpDrive,R3PartKind.Gyroscope,R3PartKind.Booster);yield return Seconds(.7f);
        var b=v.GetComponent<Rigidbody>();b.linearVelocity=new Vector3(1,0,2);float before=b.position.y;
        Assert.That(Action(v,"TryJump"),Is.True);Assert.That(b.linearVelocity.x,Is.EqualTo(1).Within(.001));
        Assert.That(b.linearVelocity.z,Is.EqualTo(2).Within(.001));Assert.That(b.linearVelocity.y,Is.InRange(1,5.21f));
        Assert.That(Action(v,"TryBoost"),Is.True);Assert.That(Action(v,"TryJump"),Is.False);
        float peak=before;float end=Time.time+1;while(Time.time<end){peak=Mathf.Max(peak,b.position.y);yield return null;}
        Debug.Log($"MOV002 actual Standard peak={peak-before:F3} vz={b.linearVelocity.z:F3}");
        Assert.That(peak-before,Is.GreaterThan(.2f));Assert.That(b.linearVelocity.z,Is.GreaterThan(2));
        var airborne=Build(R3VehicleKind.Light,30,R3PartKind.JumpDrive);airborne.GetComponent<Rigidbody>().position+=Vector3.up*5;
        airborne.transform.position=airborne.GetComponent<Rigidbody>().position;Physics.SyncTransforms();
        Assert.That(Action(airborne,"TryJump"),Is.False);
    }
    [UnityTest] public IEnumerator GyroReducesRollWithoutCancellingYawAndCanBeToggled()
    {
        var v=Build(R3VehicleKind.Heavy,0,R3PartKind.Gyroscope,R3PartKind.Booster);Adapter(v);
        // Free flight isolates the stabilizer from wheel yaw servo and floor contact.
        v.Controller.enabled=false;var b=v.GetComponent<Rigidbody>();b.useGravity=false;b.position+=Vector3.up*8;
        b.rotation=Quaternion.Euler(0,0,30);b.angularVelocity=Vector3.up*.8f;float initial=30;
        yield return Seconds(1);float tilt=Vector3.Angle(b.rotation*Vector3.up,Vector3.up);
        Debug.Log($"MOV004 Heavy tilt={tilt:F3} yaw={b.angularVelocity.y:F3}");
        Assert.That(tilt,Is.LessThan(initial*.5f));Assert.That(b.angularVelocity.y,Is.GreaterThan(.5f));
        Assert.That(Action(v,"ToggleGyro"),Is.True);yield return FixedStep();
        Assert.That(v.GetComponentInChildren<GyroStabilizer>().LastAngularAcceleration,Is.EqualTo(Vector3.zero));
    }
    [UnityTest] public IEnumerator SameTickDetachDeathAndRebuildUseRealPartHealth()
    {
        var v=Build(R3VehicleKind.Light,0,R3PartKind.Booster,R3PartKind.JumpDrive,R3PartKind.Gyroscope);Adapter(v);
        var parts=v.GetComponent<R3VehicleAssembly>().Parts;var booster=parts.Single(p=>p.Kind==R3PartKind.Booster);
        float mass=v.GetComponent<Rigidbody>().mass;
        booster.Block.MarkDetached();Assert.That(booster.GetComponent<VehicleBooster>().IsOperational,Is.False);
        Assert.That(Action(v,"TryBoost"),Is.False);
        Assert.That(v.GetComponent<Rigidbody>().mass,Is.EqualTo(mass-18).Within(.001),"Graph-detached parts cannot remain in propulsion mass.");
        var gyro=parts.Single(p=>p.Kind==R3PartKind.Gyroscope);gyro.Damageable.ApplyDamage(10000,gyro.transform.position,Vector3.zero);
        Assert.That(gyro.GetComponent<GyroStabilizer>().IsOperational,Is.False);
        var jump=parts.Single(p=>p.Kind==R3PartKind.JumpDrive);jump.transform.SetParent(fixtures.transform,true);
        Assert.That(jump.GetComponent<JumpDrive>().TryActivate(),Is.Not.EqualTo(JumpDriveResult.Activated));
        var model=v.GetComponent<R3VehicleAssembly>().Model;v.GetComponent<R3VehicleAssembly>().Capture();
        model.Award(1000);Assert.That(model.Repair(booster.Cell),Is.True);Assert.That(model.Repair(gyro.Cell),Is.True);
        v.gameObject.SetActive(false);var rebuilt=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(20,.7f,1000),Quaternion.identity);
        rebuilt.transform.SetParent(fixtures.transform,true);rebuilt.Controller.SetGameplayInputEnabled(true);
        Assert.That(Action(rebuilt,"TryBoost"),Is.True);Assert.That(rebuilt.GetComponentInChildren<GyroStabilizer>().IsOperational,Is.True);yield return null;
    }
    [UnityTest] public IEnumerator PhysicalJumpHasMassSensitiveImpulseOnEveryStarter()
    {
        float previous=10;
        foreach(var kind in new[]{R3VehicleKind.Light,R3VehicleKind.Standard,R3VehicleKind.Heavy})
        {
            var v=Build(kind,0,R3PartKind.JumpDrive,R3PartKind.Gyroscope);yield return Seconds(.5f);
            var b=v.GetComponent<Rigidbody>();float y=b.position.y;Assert.That(Action(v,"TryJump"),Is.True);
            float speed=b.linearVelocity.y;Assert.That(speed,Is.InRange(1,5.21f));Assert.That(speed,Is.LessThan(previous));previous=speed;
            float peak=y;float end=Time.time+1;while(Time.time<end){peak=Mathf.Max(peak,b.position.y);yield return null;}
            Debug.Log($"MOV002 actual {kind} mass={b.mass:F0} initialVy={speed:F3} height={peak-y:F3}");
            Assert.That(peak-y,Is.GreaterThan(.10f));Assert.That(Vector3.Angle(b.rotation*Vector3.up,Vector3.up),Is.LessThan(10));v.gameObject.SetActive(false);
        }
    }
    [UnityTest] public IEnumerator DestroyedStructuralBridgeStopsMountedDeviceBeforeNextForce()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        model.Parts.Add(new R3PartState(new Vector3Int(0,2,0),R3PartKind.Armor));
        model.Parts.Add(new R3PartState(new Vector3Int(0,3,0),R3PartKind.Booster));
        var v=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(0,.7f,1000),Quaternion.identity);
        v.transform.SetParent(fixtures.transform,true);v.Controller.SetGameplayInputEnabled(true);yield return Seconds(.35f);
        Assert.That(Action(v,"TryBoost"),Is.True);var assembly=v.GetComponent<R3VehicleAssembly>();
        // Prevent the graph's periodic Update from accidentally masking a missing pre-force refresh.
        var nextCheck=v.Structure.GetType().GetField("nextCheckTime",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        Assert.That(nextCheck,Is.Not.Null);nextCheck.SetValue(v.Structure,Time.time+10);
        var bridge=assembly.Parts.Single(p=>p.Cell==new Vector3Int(0,2,0));var boost=assembly.Parts.Single(p=>p.Kind==R3PartKind.Booster).GetComponent<VehicleBooster>();
        bridge.Damageable.ApplyDamage(10000,bridge.transform.position,Vector3.zero);
        yield return FixedStep();
        Assert.That(boost.IsOperational,Is.False);Assert.That(boost.IsBoosting,Is.False);
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
