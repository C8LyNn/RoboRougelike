using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R6;
using RogueMachine.P12PipelineSafe;
public class R6ReviewTests
{
    R1RunController run;
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;run=R1RunController.Instance;
    }
    IEnumerator Combat(){run.StartRun();run.ChooseRoute(0);while(run.Transitioning)yield return null;run.Encounter.SetCombatActive(false);}
    IEnumerator Range(){run.OpenSandboxGarage();run.EnterTestRange();while(run.Transitioning)yield return null;}
    IEnumerator Seconds(float seconds){float end=Time.time+seconds;while(Time.time<end)yield return null;}
    [UnityTest] public IEnumerator SingleDriveKeepsWheelTelemetryFreshThroughNormalLifecycle()
    {
        yield return Combat();yield return Seconds(.2f);
        Assert.That(run.Player.GetComponents<R6VehicleDrive>().Length,Is.EqualTo(1));
        var wheel=run.Player.GetComponent<R3VehicleAssembly>().Parts.First(p=>p.Kind==R3PartKind.Wheel);
        wheel.Damageable.ApplyDamage(10000,wheel.transform.position,Vector3.left);yield return Seconds(.25f);
        Assert.That(run.Player.GetComponent<R6VehicleDrive>().Wheels,Is.EqualTo(run.Player.Modules.ActiveWheelCount));
    }
    [UnityTest] public IEnumerator InertRangeVehicleHasRollingResistanceAndUpdatesDamagedMass()
    {
        yield return Range();var dummy=GameObject.Find("Pojazd do przepychania");var body=dummy.GetComponent<Rigidbody>();
        body.linearVelocity=Vector3.back*2;yield return Seconds(.8f);
        Assert.That(body.linearVelocity.magnitude,Is.LessThan(1.8f),"The test vehicle must not coast indefinitely.");
        float mass=body.mass;
        foreach(var part in dummy.GetComponent<R3VehicleAssembly>().Parts.Where(p=>p.Kind==R3PartKind.Armor).Take(5))part.Damageable.ApplyDamage(10000,part.transform.position,Vector3.up);
        yield return Seconds(.25f);Assert.That(body.mass,Is.LessThan(mass-30));
    }
    static Color ColorOf(Renderer renderer){var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);return block.GetColor("_BaseColor");}
    [UnityTest] public IEnumerator RangeCoreAndDamagedDummyRetainTheirIdentifyingColors()
    {
        yield return Range();var target=GameObject.Find("Poligon R6").GetComponentsInChildren<P12PipelineSafeDamageable>().First(p=>p.name=="Rdzeń celu");
        var armor=target.transform.parent.GetComponentsInChildren<P12PipelineSafeDamageable>().First(p=>p.name=="Pancerz celu");
        Assert.That(Vector4.Distance(ColorOf(target.GetComponent<Renderer>()),ColorOf(armor.GetComponent<Renderer>())),Is.GreaterThan(.4f));
        var dummy=GameObject.Find("Pojazd do przepychania").GetComponent<R3VehicleAssembly>().Parts.First(p=>p.Kind==R3PartKind.Armor);
        dummy.Damageable.ApplyDamage(30,dummy.transform.position,Vector3.up);yield return Seconds(.2f);
        var color=ColorOf(dummy.GetComponent<Renderer>());Assert.That(color.r,Is.GreaterThan(color.g+.1f));
    }
    [UnityTest] public IEnumerator DisabledNormalTurretInheritsOwnerVelocityAndSpin()
    {
        yield return Combat();var enemy=run.Encounter.Enemies.Single();var owner=enemy.GetComponent<Rigidbody>();var yaw=enemy.transform.Find("Enemy_Yaw");
        owner.linearVelocity=Vector3.forward*4;owner.angularVelocity=Vector3.up*.3f;
        enemy.WeaponBlock.ApplyDamage(10000,enemy.WeaponBlock.transform.position,Vector3.up);enemy.SetCombatActive(true);enemy.SendMessage("Update");
        Assert.That(yaw.IsChildOf(enemy.transform),Is.False);var debris=yaw.GetComponent<Rigidbody>();
        Assert.That(debris.linearVelocity.z,Is.GreaterThan(3));Assert.That(debris.angularVelocity.y,Is.EqualTo(.3f).Within(.01f));
        Assert.That(debris.maxDepenetrationVelocity,Is.LessThanOrEqualTo(1.5f));
    }
    [UnityTest] public IEnumerator CoreDeathReleasesTurretBeforeOwnerMomentumIsCleared()
    {
        yield return Combat();var enemy=run.Encounter.Enemies.Single();var owner=enemy.GetComponent<Rigidbody>();var yaw=enemy.transform.Find("Enemy_Yaw");
        owner.linearVelocity=Vector3.forward*4;enemy.Core.ApplyDamage(10000,enemy.Core.transform.position,Vector3.up);
        enemy.GetComponent<P12PipelineSafeVehicleStructure>().EvaluateStructure();enemy.SendMessage("Update");
        Assert.That(yaw.GetComponent<Rigidbody>(),Is.Not.Null);Assert.That(yaw.GetComponent<Rigidbody>().linearVelocity.z,Is.GreaterThan(3));
    }
    [UnityTest] public IEnumerator WheelLessEnemyGraduallyStopsRatherThanSlidingForever()
    {
        yield return Combat();var enemy=run.Encounter.Enemies.Single();
        foreach(var wheel in enemy.GetComponentsInChildren<P12PipelineSafeDamageable>().Where(p=>p.name.StartsWith("Koło")).ToArray())wheel.ApplyDamage(10000,wheel.transform.position,Vector3.up);
        var body=enemy.GetComponent<Rigidbody>();body.linearVelocity=Vector3.back*2;
        yield return Seconds(1.5f);Assert.That(body.linearVelocity.magnitude,Is.LessThan(.5f));Assert.That(enemy.ActiveWheelCount,Is.Zero);
    }
    [UnityTest] public IEnumerator InitiallyOverlappingDebrisIgnoresOnlyOwnerPairsAndRestoresThem()
    {
        yield return Combat();var part=run.Player.GetComponent<R3VehicleAssembly>().Parts.First(p=>p.Kind==R3PartKind.Armor);
        var fragment=GameObject.CreatePrimitive(PrimitiveType.Cube);fragment.transform.position=part.transform.position;fragment.transform.localScale=Vector3.one*.4f;
        var body=fragment.AddComponent<Rigidbody>();body.useGravity=false;Physics.SyncTransforms();var collider=fragment.GetComponent<Collider>();var ownerCollider=part.GetComponent<Collider>();
        var owner=run.Player.GetComponent<Rigidbody>();R6DebrisRelease.Release(body,owner,Vector3.zero,Vector3.zero,8);
        Assert.That(Physics.GetIgnoreCollision(collider,ownerCollider),Is.True);
        Assert.That(Physics.GetIgnoreCollision(collider,GameObject.Find("P12_Ground").GetComponent<Collider>()),Is.False);
        body.position+=Vector3.right*12;Physics.SyncTransforms();yield return Seconds(.1f);
        Assert.That(Physics.GetIgnoreCollision(collider,ownerCollider),Is.False);Assert.That(owner.linearVelocity.magnitude,Is.LessThan(.2f));
    }
    [UnityTest] public IEnumerator LowAndHighSpeedBarrierContactsStayBounded()
    {
        yield return Combat();var body=run.Player.GetComponent<Rigidbody>();
        foreach(float speed in new[]{.6f,9f})
        {
            run.Player.MoveTo(new Vector3(-27,.7f,-30),Quaternion.identity);Physics.SyncTransforms();
            var bounds=run.Player.GetComponent<BoxCollider>().bounds;
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=new Vector3(-27,2,bounds.max.z+.52f);wall.transform.localScale=new Vector3(12,4,1);Physics.SyncTransforms();
            body.linearVelocity=Vector3.forward*speed;float until=Time.time+.8f,peak=0,spin=0;
            while(Time.time<until){peak=Mathf.Max(peak,body.linearVelocity.magnitude);spin=Mathf.Max(spin,body.angularVelocity.magnitude);Assert.That(body.position.y,Is.EqualTo(.7f).Within(.01f));yield return null;}
            Debug.Log($"R6 barrier {speed}m/s: peak={peak:0.00}, spin={spin:0.00}, final={body.linearVelocity}");
            Assert.That(peak,Is.LessThan(speed+.3f));Assert.That(spin,Is.LessThan(1));Assert.That(body.linearVelocity.z,Is.GreaterThan(-1.6f));
            wall.SetActive(false);Object.Destroy(wall);
        }
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
