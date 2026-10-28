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
        while(R1RunController.Instance?.Progress==null)yield return null;run=R1RunController.Instance;
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
