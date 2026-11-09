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

public sealed class RamContactProbe : MonoBehaviour
{
    public Transform Target; public bool Contact;
    void OnCollisionEnter(Collision collision) {if(Target!=null&&collision.collider.transform.IsChildOf(Target))Contact=true;}
    void OnCollisionStay(Collision collision) {if(Target!=null&&collision.collider.transform.IsChildOf(Target))Contact=true;}
}
public sealed class UnarmedEnemyRammingTests
{
    R1RunController run;P12PipelineSafeEnemyTurret enemy;Rigidbody body,playerBody;R6TestRange range;RamContactProbe probe;
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        float setupDeadline=Time.realtimeSinceStartup+25;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<setupDeadline)yield return null;
        run=R1RunController.Instance;Assert.That(run?.Progress,Is.Not.Null,"Game initialization timed out.");
        Assert.That(run.SelectVehicle(R3VehicleKind.Light),Is.True);Assert.That(run.OpenSandboxGarage(),Is.True);
        Assert.That(run.EnterTestRange(),Is.True);while(run.Transitioning)yield return null;
        range=run.GetComponent<R6TestRange>();enemy=range.TrainingTarget;Assert.That(enemy,Is.Not.Null);
        enemy.transform.SetParent(null,true);range.Layout.gameObject.SetActive(false);
        body=enemy.GetComponent<Rigidbody>();playerBody=run.Player.GetComponent<Rigidbody>();playerBody.isKinematic=true;
        body.position=playerBody.position+Vector3.forward*9;body.rotation=Quaternion.identity;
        body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;
        enemy.ConfigureMovement(5,15,0);enemy.WeaponBlock.ApplyDamage(10000,enemy.WeaponBlock.transform.position,Vector3.up);
        Assert.That(enemy.WeaponOnline,Is.False);probe=enemy.gameObject.AddComponent<RamContactProbe>();probe.Target=run.Player.transform;
        Physics.SyncTransforms();range.SetTrainingFire(true);yield return new WaitForFixedUpdate();
    }
    [UnityTest] public IEnumerator MobileUnarmedEnemyClosesDistanceAndPhysicallyContactsPlayer()
    {
        float initial=Vector3.Distance(body.position,playerBody.position),deadline=Time.realtimeSinceStartup+12;
        while(!probe.Contact&&Time.realtimeSinceStartup<deadline)yield return null;
        float final=Vector3.Distance(body.position,playerBody.position);
        Debug.Log($"[P16 RAM] contact={probe.Contact} distance={initial:0.00}->{final:0.00} wheels={enemy.ActiveWheelCount} drive={enemy.GetComponent<R6VehicleDrive>().GroundedWheels}");
        Assert.That(final,Is.LessThan(initial-2),"unarmed mobile robot should approach, not keep weapon standoff distance");
        Assert.That(probe.Contact,Is.True,"real colliders must make contact; no teleport or fake animation");
    }
    [UnityTest] public IEnumerator DestroyedWheelDriveCannotProduceArtificialRammingThrust()
    {
        var wheels=enemy.GetComponentsInChildren<P12PipelineSafeDamageable>().Where(R6VehicleDrive.IsWheel).ToArray();
        Assert.That(wheels.Length,Is.GreaterThan(0));
        foreach(var wheel in wheels)wheel.ApplyDamage(10000,wheel.transform.position,Vector3.up);
        var drive=enemy.GetComponent<R6VehicleDrive>();drive.RefreshMass();Assert.That(drive.Wheels,Is.Zero);
        yield return new WaitForSeconds(1);body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;
        var start=body.position;yield return new WaitForSeconds(2);
        Assert.That(Vector3.ProjectOnPlane(body.position-start,Vector3.up).magnitude,Is.LessThan(.6f));
        Assert.That(probe.Contact,Is.False);
    }
    [UnityTest] public IEnumerator ProlongedPhysicalContactTriggersRetreat()
    {
        float deadline=Time.realtimeSinceStartup+20;
        while(!probe.Contact&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(probe.Contact,Is.True,"The real enemy must first reach the player's colliders.");
        while(!enemy.RecoveringFromPlayerContact&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(enemy.RecoveringFromPlayerContact,Is.True,"Continuous contact should activate the retreat.");
        float atRetreat=Vector3.Distance(body.position,playerBody.position);
        float furthest=atRetreat;
        while(enemy.RecoveringFromPlayerContact&&Time.realtimeSinceStartup<deadline)
        {
            furthest=Mathf.Max(furthest,Vector3.Distance(body.position,playerBody.position));
            yield return null;
        }
        Assert.That(furthest,Is.GreaterThan(atRetreat+.15f),"The real wheel drive should move away, not merely change state.");
    }
    [UnityTest] public IEnumerator UnarmedEnemyDoesNotDriveThroughSolidWall()
    {
        var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="P16 solid wall";
        wall.transform.position=(body.position+playerBody.position)*.5f+Vector3.up;
        wall.transform.localScale=new Vector3(24,5,1);Physics.SyncTransforms();
        try
        {
            yield return new WaitForSeconds(4);
            Assert.That(probe.Contact,Is.False,"ramming cannot ignore world geometry");
            Assert.That(body.position.z,Is.GreaterThan(wall.transform.position.z),"cannot cross wall toward player");
        }
        finally{Object.Destroy(wall);}
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}

public sealed class ArmedEnemyRammingTests
{
    [UnityTest] public IEnumerator ArmedGunnerKeepsDistanceInsteadOfAutomaticallyRamming()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        float deadline=Time.realtimeSinceStartup+20;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<deadline)yield return null;
        var run=R1RunController.Instance;Assert.That(run.SelectVehicle(R3VehicleKind.Light),Is.True);
        Assert.That(run.OpenSandboxGarage(),Is.True);Assert.That(run.EnterTestRange(),Is.True);
        while(run.Transitioning&&Time.realtimeSinceStartup<deadline)yield return null;
        var range=run.GetComponent<R6TestRange>();var enemy=range.TrainingTarget;Assert.That(enemy.WeaponOnline,Is.True);
        enemy.transform.SetParent(null,true);range.Layout.gameObject.SetActive(false);
        var body=enemy.GetComponent<Rigidbody>();var player=run.Player.GetComponent<Rigidbody>();player.isKinematic=true;
        foreach(var hp in run.Player.GetComponentsInChildren<P12PipelineSafeDamageable>())hp.SetDurabilityMultiplier(50);
        enemy.transform.SetPositionAndRotation(player.position+Vector3.forward*9,Quaternion.identity);body.linearVelocity=Vector3.zero;
        enemy.ConfigureMovement(5,15,0);var probe=enemy.gameObject.AddComponent<RamContactProbe>();probe.Target=run.Player.transform;
        Physics.SyncTransforms();range.SetTrainingFire(true);float initial=Vector3.Distance(body.position,player.position);
        Assert.That(enemy.Archetype,Is.EqualTo(R3EnemyArchetype.Gunner));
        float started=Time.time;deadline=Time.realtimeSinceStartup+18;
        while(Time.time-started<6f&&Time.realtimeSinceStartup<deadline)yield return null;
        float final=Vector3.Distance(body.position,player.position);
        var drive=enemy.GetComponent<R6VehicleDrive>();
        Debug.Log($"Ranged gunner: contact={probe.Contact} distance={initial:F2}->{final:F2} pos={body.position} vel={body.linearVelocity} kinematic={body.isKinematic} gravity={body.useGravity} active={enemy.CombatActive} wheels={drive.Wheels} grounded={drive.GroundedWheels}");
        Assert.That(final,Is.GreaterThan(initial+.5f),"The ranged enemy should open distance from a nearby player.");
        Assert.That(probe.Contact,Is.False,"A ranged enemy must not automatically ram a nearby player.");
        Assert.That(enemy.WeaponOnline,Is.True);
        yield return new ExitPlayMode();
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
