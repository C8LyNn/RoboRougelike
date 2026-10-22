using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RogueMachine.P12PipelineSafe;

public class R6PhysicsTests
{
    R1RunController run;
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        run=R1RunController.Instance;run.StartRun();run.ChooseRoute(0);
        while(run.Transitioning)yield return null;
        foreach(var enemy in run.Encounter.Enemies)enemy.SetCombatActive(false);
    }
    [UnityTest] public IEnumerator ExternalImpulseRetainsInertiaWithoutInput()
    {
        var body=run.Player.GetComponent<Rigidbody>();body.linearVelocity=Vector3.forward*3;
        run.Player.Controller.SendMessage("FixedUpdate");
        Debug.Log("R6 inertia after drive step: "+body.linearVelocity);
        Assert.That(body.linearVelocity.z,Is.GreaterThan(2),"Drive must not erase solver velocity every tick.");
        Assert.That(body.linearVelocity.z,Is.LessThanOrEqualTo(3.01f));
        yield return null;
    }
    [UnityTest] public IEnumerator EnemyHasFiniteDynamicMassAndCanBePushed()
    {
        var body=run.Encounter.Enemies.First().GetComponent<Rigidbody>();
        Assert.That(body.isKinematic,Is.False,"An infinite-mass enemy cannot be pushed.");
        Vector3 start=body.position;body.AddForce(Vector3.right*body.mass*2,ForceMode.Impulse);
        float end=Time.time+.25f;while(Time.time<end)yield return null;
        Assert.That(body.position.x-start.x,Is.GreaterThan(.1f));
    }
    [UnityTest] public IEnumerator DetachedPartInheritsVehiclePointVelocity()
    {
        var root=run.Player.GetComponent<Rigidbody>();root.linearVelocity=Vector3.forward*5;
        var part=run.Player.GetComponent<R3VehicleAssembly>().Parts.First(p=>p.Kind==R3PartKind.Armor);
        part.Damageable.ApplyDamage(100000,part.transform.position,Vector3.right);
        var fragment=part.GetComponent<Rigidbody>();Assert.That(fragment,Is.Not.Null);
        Debug.Log("R6 released block velocity: "+fragment.linearVelocity);
        Assert.That(fragment.linearVelocity.z,Is.GreaterThan(4),"Debris must inherit momentum at release.");
        yield return null;
    }
    [UnityTest] public IEnumerator VehicleContactsUseRealPartsAndLeaveOpenHoles()
    {
        var tank=run.Player.Controller;
        Assert.That(tank.GetComponentsInChildren<Collider>().Count(c=>c.enabled&&!c.isTrigger),Is.GreaterThan(15));
        Assert.That(tank.GetComponent<BoxCollider>().isTrigger,Is.True,"Query bounds must not fill holes or duplicate part contacts.");
        yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
