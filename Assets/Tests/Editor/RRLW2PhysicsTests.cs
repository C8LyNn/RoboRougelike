using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R3;
using RoboRougelike.R6;
using RogueMachine.P12PipelineSafe;

public class RRLW2PhysicsTests
{
    readonly List<GameObject> objects=new List<GameObject>();
    GameObject Cube(Vector3 position)
    {var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.transform.position=position;objects.Add(g);return g;}
    static IEnumerator Fixed(float seconds)
    {float end=Time.fixedTime+seconds,until=Time.realtimeSinceStartup+5;while(Time.fixedTime<end&&Time.realtimeSinceStartup<until)yield return null;Assert.That(Time.fixedTime,Is.GreaterThanOrEqualTo(end));}
    [UnitySetUp] public IEnumerator Setup(){EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);yield return new EnterPlayMode();Time.timeScale=1;}
    [UnityTest] public IEnumerator SeparationDisablesCollidersAndGravityBeforeFirstPhysicsTick()
    {
        var g=Cube(Vector3.zero);var b=g.AddComponent<Rigidbody>();var col=g.GetComponent<Collider>();
        g.AddComponent<R3Debris>();
        Assert.That(col.enabled,Is.False,"Start is too late: another shot can arrive in the same frame.");
        Assert.That(b.useGravity,Is.False);yield return null;
    }
    [UnityTest] public IEnumerator HeavyWreckGetsMassIndependentVelocityAndAutomaticallyStartsGrace()
    {
        foreach(float mass in new[]{1f,500f})
        {
            var g=Cube(new Vector3(0,10,mass));var b=g.AddComponent<Rigidbody>();b.mass=mass;
            R6DebrisRelease.Eject(b,null,Vector3.right);Assert.That(g.GetComponent<Collider>().enabled,Is.False);
            yield return Fixed(.04f);Assert.That(b.linearVelocity.x,Is.InRange(5.8f,6.2f));Assert.That(b.linearVelocity.y,Is.InRange(2.3f,2.7f));
        }
    }
    [UnityTest] public IEnumerator PauseResumeRestoresGravityAndOnlyOriginallyEnabledColliders()
    {
        var g=Cube(new Vector3(0,10,0));var b=g.AddComponent<Rigidbody>();
        var child=Cube(new Vector3(1,10,0));child.transform.SetParent(g.transform);child.GetComponent<Collider>().enabled=false;
        g.AddComponent<R3Debris>();yield return null;Time.timeScale=0;
        yield return new WaitForSecondsRealtime(.15f);Time.timeScale=1;yield return Fixed(.6f);
        Assert.That(g.GetComponent<Collider>().enabled,Is.True);Assert.That(child.GetComponent<Collider>().enabled,Is.False);
        Assert.That(b.useGravity,Is.True);Assert.That(b.linearVelocity.y,Is.LessThan(0));
    }
    [UnityTest] public IEnumerator RealProjectilePassesFreshlyReleasedWreckAndDamagesLivingPart()
    {
        var wreck=Cube(Vector3.zero);var rb=wreck.AddComponent<Rigidbody>();rb.mass=500;
        R6DebrisRelease.Eject(rb,null,Vector3.right);
        var live=Cube(Vector3.forward*3);var hp=live.AddComponent<P12PipelineSafeDamageable>();hp.Configure(100,false);
        var shot=GameObject.CreatePrimitive(PrimitiveType.Sphere);objects.Add(shot);shot.transform.position=Vector3.back*2;shot.transform.localScale=Vector3.one*.15f;
        var body=shot.AddComponent<Rigidbody>();body.useGravity=false;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
        shot.AddComponent<P17PhysicalShell>().Configure(null,25,2,Vector3.forward,null);body.linearVelocity=Vector3.forward*30;
        yield return Fixed(.3f);Assert.That(hp.CurrentHp,Is.EqualTo(75).Within(.01f));
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {Time.timeScale=1;foreach(var g in objects)if(g!=null)Object.Destroy(g);objects.Clear();if(Application.isPlaying)yield return new ExitPlayMode();}
    [UnityTest] public IEnumerator RestingWreckKeepsWorldCollisionsWithoutConsumingProjectiles()
    {
        var wreck=Cube(Vector3.zero);var rb=wreck.AddComponent<Rigidbody>();rb.useGravity=false;
        wreck.AddComponent<R3Debris>();yield return Fixed(.6f);
        Assert.That(wreck.GetComponent<Collider>().enabled,Is.True,"Ordinary world collisions are restored.");
        var live=Cube(Vector3.forward*3);var hp=live.AddComponent<P12PipelineSafeDamageable>();hp.Configure(100,false);
        var shot=GameObject.CreatePrimitive(PrimitiveType.Sphere);objects.Add(shot);shot.transform.position=Vector3.back*2;shot.transform.localScale=Vector3.one*.15f;
        var body=shot.AddComponent<Rigidbody>();body.useGravity=false;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
        shot.AddComponent<P17PhysicalShell>().Configure(null,25,2,Vector3.forward,null);body.linearVelocity=Vector3.forward*30;
        yield return Fixed(.3f);Assert.That(hp.CurrentHp,Is.EqualTo(75).Within(.01f));
    }
    [UnityTest] public IEnumerator TwoSimultaneousShotsDestroyFrontBlockAndStillDamageThePartBehind()
    {
        var front=Cube(Vector3.zero);front.AddComponent<P12PipelineSafeDamageable>().Configure(25,true);
        var live=Cube(Vector3.forward*3);var hp=live.AddComponent<P12PipelineSafeDamageable>();hp.Configure(100,false);
        foreach(float x in new[]{-.12f,.12f})
        {
            var shot=GameObject.CreatePrimitive(PrimitiveType.Sphere);objects.Add(shot);shot.transform.position=new Vector3(x,0,-2);shot.transform.localScale=Vector3.one*.15f;
            var body=shot.AddComponent<Rigidbody>();body.useGravity=false;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            shot.AddComponent<P17PhysicalShell>().Configure(null,25,2,Vector3.forward,null);body.linearVelocity=Vector3.forward*30;
        }
        yield return Fixed(.3f);Assert.That(hp.CurrentHp,Is.EqualTo(75).Within(.01f));
    }
}
