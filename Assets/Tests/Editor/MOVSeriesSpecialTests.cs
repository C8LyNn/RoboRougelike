using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.MOVSeries;
using RoboRougelike.Mechanics.MOV003Blink;
using RoboRougelike.Mechanics.MOV006MagneticGrapple;

public class MOVSeriesSpecialTests
{
    GameObject fixtures;R3VisualPalette palette;
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        R1RunController.Instance.OpenSandboxGarage();Time.timeScale=1;
        fixtures=new GameObject("MOV special acceptance");palette=UnityEngine.Object.FindFirstObjectByType<R3VisualPalette>();
        Box("Ground",new Vector3(0,-.5f,1000),new Vector3(180,1,180));
    }
    GameObject Box(string name,Vector3 pos,Vector3 size)
    {
        var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.SetParent(fixtures.transform);
        o.transform.position=pos;o.transform.localScale=size;return o;
    }
    R1VehicleState Build(R3VehicleKind kind,float x,params R3PartKind[] devices)
    {
        var m=new R3VehicleModel(R3VehicleCatalog.Create(kind));int y=kind==R3VehicleKind.Light?1:2;
        // The third Blink formerly intersected the forward grapple payload by
        // 23cm. Preserve the same devices/cost/abilities on a free rear cell.
        for(int i=0;i<devices.Length;i++)m.Parts.Add(new R3PartState(new Vector3Int(i%2==0?-1:1,y,i==2&&devices[i]==R3PartKind.Blink?-1:i/2),devices[i]));
        var v=R3VehicleAssembly.Build(m,new R1RunStats(),palette,new Vector3(x,.7f,1000),Quaternion.identity);
        v.transform.SetParent(fixtures.transform,true);v.Controller.SetGameplayInputEnabled(true);return v;
    }
    static bool Act(R1VehicleState v,string name,Vector3 direction)
    {
        var a=v.GetComponent<MOVSeriesVehicleAdapter>();Assert.That(a,Is.Not.Null);
        var method=a.GetType().GetMethod(name);Assert.That(method,Is.Not.Null,"Missing integrated action: "+name);
        return (bool)method.Invoke(a,new object[]{direction});
    }
    static void Release(R1VehicleState v)
    {
        var a=v.GetComponent<MOVSeriesVehicleAdapter>();var method=a.GetType().GetMethod("ReleaseGrapples");
        Assert.That(method,Is.Not.Null);method.Invoke(a,null);
    }
    [UnityTest] public IEnumerator BlinkTranslatesEveryConnectedPartOfAllThreeClassesAndPreservesMomentum()
    {
        foreach(var kind in new[]{R3VehicleKind.Light,R3VehicleKind.Standard,R3VehicleKind.Heavy})
        {
            var v=Build(kind,0,R3PartKind.Blink);yield return MOVSeriesDeviceTests.Seconds(.5f);
            var b=v.GetComponent<Rigidbody>();b.linearVelocity=new Vector3(1,0,2);b.angularVelocity=Vector3.up*.3f;
            var parts=v.GetComponent<R3VehicleAssembly>().Parts.ToArray();var before=parts.Select(p=>p.transform.position).ToArray();var local=parts.Select(p=>p.transform.localPosition).ToArray();
            Vector3 pose=b.position;Quaternion rotation=b.rotation;
            Assert.That(Act(v,"TryBlink",Vector3.forward),Is.True);
            Assert.That(Vector3.Distance(b.position-pose,Vector3.forward*8),Is.LessThan(.002));
            Assert.That(b.linearVelocity,Is.EqualTo(new Vector3(1,0,2)));Assert.That(b.angularVelocity,Is.EqualTo(Vector3.up*.3f));Assert.That(b.rotation,Is.EqualTo(rotation));
            for(int i=0;i<parts.Length;i++){Assert.That(Vector3.Distance(parts[i].transform.position,before[i]+Vector3.forward*8),Is.LessThan(.002));Assert.That(parts[i].transform.localPosition,Is.EqualTo(local[i]));}
            Assert.That(Act(v,"TryBlink",Vector3.forward),Is.False);Assert.That(v.GetComponentsInChildren<Rigidbody>().Length,Is.EqualTo(1));
            v.gameObject.SetActive(false);
        }
    }
    [UnityTest] public IEnumerator WideHullPathAndDestinationBlockFailuresDoNotSpendCooldown()
    {
        var v=Build(R3VehicleKind.Heavy,0,R3PartKind.Blink);yield return MOVSeriesDeviceTests.Seconds(.5f);var b=v.GetComponent<Rigidbody>();
        // Only a side of this wide construction touches the obstruction; the core ray is clear.
        var wall=Box("Side sweep wall",new Vector3(2,2,1004),new Vector3(.4f,4,1));Physics.SyncTransforms();
        Vector3 before=b.position;Assert.That(Act(v,"TryBlink",Vector3.forward),Is.False);Assert.That(b.position,Is.EqualTo(before));
        Assert.That(v.GetComponentInChildren<BlinkDrive>().CooldownRemaining,Is.Zero);wall.SetActive(false);
        var occupied=Box("Destination occupied",new Vector3(0,1,1008),new Vector3(2,2,2));Physics.SyncTransforms();
        Assert.That(Act(v,"TryBlink",Vector3.forward),Is.False);Assert.That(b.position,Is.EqualTo(before));occupied.SetActive(false);
        Assert.That(Act(v,"TryBlink",Vector3.forward),Is.True);
    }
    [UnityTest] public IEnumerator BlinkRejectsUnsupportedDestinationAndActorAsGround()
    {
        var v=Build(R3VehicleKind.Light,0,R3PartKind.Blink);yield return MOVSeriesDeviceTests.Seconds(.4f);
        var floor=fixtures.transform.Find("Ground");floor.gameObject.SetActive(false);
        var actor=Box("Foreign body under destination",new Vector3(0,-.5f,1008),new Vector3(15,1,15));actor.AddComponent<Rigidbody>().isKinematic=true;
        Physics.SyncTransforms();Vector3 before=v.GetComponent<Rigidbody>().position;
        Assert.That(Act(v,"TryBlink",Vector3.forward),Is.False);Assert.That(v.GetComponent<Rigidbody>().position,Is.EqualTo(before));
        Assert.That(v.GetComponentInChildren<BlinkDrive>().CooldownRemaining,Is.Zero);actor.SetActive(false);
        Assert.That(Act(v,"TryBlink",Vector3.forward),Is.False);
    }
    [UnityTest] public IEnumerator GrapplePullsStaticAnchorWithBoostAndRejectsBlinkUntilRelease()
    {
        var v=Build(R3VehicleKind.Light,0,R3PartKind.MagneticGrapple,R3PartKind.Booster,R3PartKind.Blink,R3PartKind.Gyroscope);
        Box("Static anchor",new Vector3(0,2,1015),new Vector3(6,4,1));yield return MOVSeriesDeviceTests.Seconds(.5f);
        var rope=v.GetComponent<MagneticGrappleController>();Assert.That(rope,Is.Not.Null);
        Assert.That(Act(v,"TryGrapple",Vector3.forward),Is.True);Assert.That(rope.IsAttached,Is.True);Vector3 before=v.GetComponent<Rigidbody>().position;
        Assert.That(Act(v,"TryBlink",Vector3.back),Is.False);Assert.That(v.GetComponent<Rigidbody>().position,Is.EqualTo(before));
        Assert.That(v.GetComponent<MOVSeriesVehicleAdapter>().TryBoost(),Is.True);yield return MOVSeriesDeviceTests.Seconds(.7f);
        Assert.That(v.GetComponent<Rigidbody>().position.z,Is.GreaterThan(before.z+.3f));Assert.That(v.GetComponent<Rigidbody>().linearVelocity.magnitude,Is.LessThan(14));
        Release(v);Assert.That(rope.IsAttached,Is.False);Assert.That(Act(v,"TryGrapple",Vector3.forward),Is.False);
        var model=v.GetComponent<R3VehicleAssembly>().Model;v.gameObject.SetActive(false);
        var rebuilt=R3VehicleAssembly.Build(model.DeepCopy(),new R1RunStats(),palette,new Vector3(0,.7f,1000),Quaternion.identity);
        rebuilt.transform.SetParent(fixtures.transform,true);rebuilt.Controller.SetGameplayInputEnabled(true);
        Assert.That(Act(rebuilt,"TryGrapple",Vector3.forward),Is.False,"Release cooldown survives rebuild and emitter changes.");
    }
    [UnityTest] public IEnumerator DynamicTargetHasOppositeReactionIncomingBlinkGuardAndEmitterDetachRelease()
    {
        var source=Build(R3VehicleKind.Standard,0,R3PartKind.MagneticGrapple,R3PartKind.Blink);
        var target=Build(R3VehicleKind.Light,0,R3PartKind.Blink);target.transform.position+=Vector3.forward*12;
        target.GetComponent<Rigidbody>().position=target.transform.position;Physics.SyncTransforms();yield return MOVSeriesDeviceTests.Seconds(.5f);
        // Isolate equal/opposite rope forces from independent wheel coasting and floor friction.
        foreach(var v in new[]{source,target})
        {
            v.Controller.enabled=false;var body=v.GetComponent<Rigidbody>();body.useGravity=false;
            body.position+=Vector3.up*5;v.transform.position=body.position;body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;
        }
        Physics.SyncTransforms();
        var sourceEmitter=source.GetComponentInChildren<MagneticGrappleEmitter>();Assert.That(sourceEmitter,Is.Not.Null);
        Assert.That(Act(source,"TryGrapple",target.Core.transform.position-sourceEmitter.Muzzle.position),Is.True);var rope=source.GetComponent<MagneticGrappleController>();
        Assert.That(rope.AttachedRigidbody,Is.SameAs(target.GetComponent<Rigidbody>()));Assert.That(Act(target,"TryBlink",Vector3.right),Is.False);
        float a=source.transform.position.z,b=target.transform.position.z;yield return MOVSeriesDeviceTests.Seconds(.6f);
        Debug.Log($"MOV006 real Standard/Light pull source={source.transform.position.z-a:F3} target={target.transform.position.z-b:F3}");
        Assert.That(source.transform.position.z,Is.GreaterThan(a+.05f));Assert.That(target.transform.position.z,Is.LessThan(b-.05f));
        var sourceBody=source.GetComponent<Rigidbody>();var targetBody=target.GetComponent<Rigidbody>();
        Assert.That((sourceBody.linearVelocity*sourceBody.mass+targetBody.linearVelocity*targetBody.mass).magnitude,Is.LessThan(.5f),"Internal rope forces conserve total linear momentum.");
        var emitter=source.GetComponentInChildren<MagneticGrappleEmitter>();emitter.GetComponent<R3MountedPart>().Block.MarkDetached();
        Assert.That(emitter.IsOperational,Is.False,"Direct emitter predicate observes the same-step detached flag.");
        Debug.Log($"MOV006 detach before fixed={Time.fixedTime:F3} enabled={rope.isActiveAndEnabled} activeSame={rope.ActiveEmitter==emitter}");
        yield return MOVSeriesDeviceTests.FixedStep();
        Debug.Log($"MOV006 detach after fixed={Time.fixedTime:F3} operational={emitter.IsOperational} attached={rope.IsAttached}");
        Assert.That(rope.IsAttached,Is.False);Assert.That(emitter.IsOperational,Is.False);
    }
    [UnityTest] public IEnumerator GroundedGrappleHonorsLightTargetForceLimitRatherThanMovingHeavySourceMagically()
    {
        var source=Build(R3VehicleKind.Standard,0,R3PartKind.MagneticGrapple);
        var target=Build(R3VehicleKind.Light,0,R3PartKind.Blink);target.transform.position+=Vector3.forward*12;
        target.GetComponent<Rigidbody>().position=target.transform.position;Physics.SyncTransforms();yield return MOVSeriesDeviceTests.Seconds(.5f);
        var emitter=source.GetComponentInChildren<MagneticGrappleEmitter>();
        Assert.That(Act(source,"TryGrapple",target.Core.transform.position-emitter.Muzzle.position),Is.True);
        float before=target.transform.position.z;yield return MOVSeriesDeviceTests.Seconds(.6f);
        Assert.That(target.transform.position.z,Is.LessThan(before-.05f));
        Assert.That(source.GetComponent<Rigidbody>().linearVelocity.magnitude,Is.LessThan(1));
        Assert.That(target.GetComponent<Rigidbody>().linearVelocity.magnitude,Is.LessThan(3));
    }
    [UnityTest] public IEnumerator EmitterDeathRepairAndSecondEmitterCannotResetVehicleCooldown()
    {
        var source=Build(R3VehicleKind.Light,0,R3PartKind.MagneticGrapple,R3PartKind.MagneticGrapple,R3PartKind.Blink);
        Box("Repair target",new Vector3(0,2,1015),new Vector3(6,4,1));yield return MOVSeriesDeviceTests.Seconds(.5f);
        Assert.That(Act(source,"TryGrapple",Vector3.forward),Is.True);var rope=source.GetComponent<MagneticGrappleController>();var emitter=rope.ActiveEmitter.GetComponent<R3MountedPart>();string id=emitter.Id;
        emitter.Damageable.ApplyDamage(10000,emitter.transform.position,Vector3.zero);Assert.That(rope.IsAttached,Is.False);
        Assert.That(Act(source,"TryGrapple",Vector3.forward),Is.False,"A second emitter shares release cooldown.");
        var assembly=source.GetComponent<R3VehicleAssembly>();var blink=assembly.Parts.Single(p=>p.Kind==R3PartKind.Blink);
        blink.Block.MarkDetached();Assert.That(blink.GetComponent<BlinkDrive>().Operational,Is.False);Assert.That(Act(source,"TryBlink",Vector3.back),Is.False);
        assembly.Capture();var model=assembly.Model;model.Award(500);
        Assert.That(model.Repair(emitter.Cell),Is.True);Assert.That(model.Repair(blink.Cell),Is.True);source.gameObject.SetActive(false);
        var rebuilt=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(0,.7f,1000),Quaternion.identity);
        rebuilt.transform.SetParent(fixtures.transform,true);rebuilt.Controller.SetGameplayInputEnabled(true);
        Assert.That(rebuilt.GetComponentsInChildren<MagneticGrappleEmitter>().Single(e=>e.GetComponent<R3MountedPart>().Id==id).IsOperational,Is.True);
        Assert.That(Act(rebuilt,"TryGrapple",Vector3.forward),Is.False);yield return MOVSeriesDeviceTests.Seconds(6.1f);
        Assert.That(Act(rebuilt,"TryGrapple",Vector3.forward),Is.True);Release(rebuilt);
        Assert.That(Act(rebuilt,"TryBlink",Vector3.back),Is.True);
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
