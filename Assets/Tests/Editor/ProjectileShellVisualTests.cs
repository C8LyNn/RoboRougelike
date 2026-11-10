using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using RogueMachine.P12PipelineSafe;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class ProjectileShellVisualTests
{
    [UnitySetUp] public IEnumerator Setup(){EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);yield return new EnterPlayMode();}
    [UnityTest] public IEnumerator ShellUsesElongatedVisualButKeepsOnePhysicalCollider()
    {
        var shell=GameObject.CreatePrimitive(PrimitiveType.Sphere);shell.transform.localScale=Vector3.one*.2f;
        var body=shell.AddComponent<Rigidbody>();body.useGravity=false;body.linearVelocity=Vector3.forward*10;
        var physical=shell.GetComponent<SphereCollider>();var source=shell.GetComponent<Renderer>();
        var projectile=shell.AddComponent<P17PhysicalShell>();projectile.Configure(null,20,5,Vector3.forward,null);
        var visual=shell.transform.Find("P17 shell visual");Assert.That(visual,Is.Not.Null);
        Assert.That(source.enabled,Is.False);Assert.That(physical.enabled,Is.True);
        Assert.That(shell.GetComponentsInChildren<Collider>(true).Count(c=>c.enabled),Is.EqualTo(1));
        Assert.That(visual.localScale.y,Is.GreaterThan(visual.localScale.x*2));
        yield return null;
        Assert.That(Vector3.Dot(visual.up.normalized,Vector3.forward),Is.GreaterThan(.99f));
        body.linearVelocity=Vector3.right*10;yield return null;yield return null;
        Assert.That(Vector3.Dot(visual.up.normalized,Vector3.right),Is.GreaterThan(.99f),"Appearance tracks real flight direction.");
        Object.Destroy(shell);
    }
    [UnityTest] public IEnumerator OwnerCollisionFilterTracksNewPartsWithoutPerShotArrays()
    {
        var owner=new GameObject("Many-part owner");
        var shell=new GameObject("Collision filter projectile");
        try
        {
            for(int i=0;i<180;i++)
            {
                var part=new GameObject("Owner part "+i);part.transform.SetParent(owner.transform,false);
                part.transform.localPosition=Vector3.right*i;part.AddComponent<BoxCollider>();
            }
            var own=shell.AddComponent<SphereCollider>();shell.AddComponent<Rigidbody>().useGravity=false;
            var projectile=shell.AddComponent<P17PhysicalShell>();projectile.Configure(owner.transform,20,60,Vector3.forward,null);
            var newPart=new GameObject("New owner part");newPart.transform.SetParent(owner.transform,false);
            var newCollider=newPart.AddComponent<BoxCollider>();
            Assert.That(Physics.GetIgnoreCollision(own,newCollider),Is.False);
            var method=typeof(P17PhysicalShell).GetMethod("IgnoreOwnerCollisions",BindingFlags.Instance|BindingFlags.NonPublic);
            var apply=(System.Action)System.Delegate.CreateDelegate(typeof(System.Action),projectile,method);
            apply();Assert.That(Physics.GetIgnoreCollision(own,newCollider),Is.True,"Newly mounted colliders must be included.");
            for(int i=0;i<3;i++)apply();
            using(var recorder=Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Memory,"GC Allocated In Frame",16))
            {
                Assert.That(recorder.Valid,Is.True);yield return null;yield return null;long idle=recorder.LastValue;
                for(int i=0;i<50;i++)apply();
                yield return null;long sample=recorder.LastValue;
                var control=new byte[1024*1024];control[0]=1;yield return null;long controlBytes=recorder.LastValue;System.GC.KeepAlive(control);
                Debug.Log($"[PROJECTILE_OWNER_ALLOC] queries=50 colliders=181 idle={idle} sample={sample} control={controlBytes}");
                Assert.That(controlBytes,Is.GreaterThanOrEqualTo(1024*1024),"The profiler must detect a known allocation.");
                Assert.That(sample-idle,Is.LessThan(20000),"Owner collision setup should not allocate a collider array per projectile.");
            }
        }
        finally{Object.Destroy(shell);Object.Destroy(owner);}
    }
    [UnityTest] public IEnumerator TargetHelperFilterKeepsDamageablePartsHittableWithoutPerShotArrays()
    {
        var target=new GameObject("Target with helper colliders");
        var shell=new GameObject("Target filter projectile");
        shell.transform.position=new Vector3(1000,1000,1000);
        try
        {
            for(int i=0;i<180;i++)
            {
                var helper=new GameObject("Helper "+i);helper.transform.SetParent(target.transform,false);
                helper.transform.localPosition=Vector3.right*i;helper.AddComponent<BoxCollider>();
            }
            var damageable=GameObject.CreatePrimitive(PrimitiveType.Cube);damageable.name="Damageable part";damageable.transform.SetParent(target.transform,false);
            var damageCollider=damageable.GetComponent<BoxCollider>();
            damageable.AddComponent<P12PipelineSafeDamageable>().Configure(100,false);
            var child=new GameObject("Collider below damageable parent");child.transform.SetParent(damageable.transform,false);
            var childCollider=child.AddComponent<BoxCollider>();
            var own=shell.AddComponent<SphereCollider>();shell.AddComponent<Rigidbody>().useGravity=false;
            var projectile=shell.AddComponent<P17PhysicalShell>();projectile.Configure(null,20,60,Vector3.forward,target.transform);
            var newHelper=new GameObject("New target helper");newHelper.transform.SetParent(target.transform,false);
            var newCollider=newHelper.AddComponent<BoxCollider>();
            var method=typeof(P17PhysicalShell).GetMethod("IgnoreTargetHelpers",BindingFlags.Instance|BindingFlags.NonPublic);
            var apply=(System.Action)System.Delegate.CreateDelegate(typeof(System.Action),projectile,method);
            apply();
            Assert.That(Physics.GetIgnoreCollision(own,newCollider),Is.True,"New helper colliders must be ignored.");
            Assert.That(Physics.GetIgnoreCollision(own,damageCollider),Is.False,"Real target parts must still be hittable.");
            Assert.That(Physics.GetIgnoreCollision(own,childCollider),Is.False,"A collider below a damageable parent must remain hittable.");
            for(int i=0;i<3;i++)apply();
            using(var recorder=Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Memory,"GC Allocated In Frame",16))
            {
                Assert.That(recorder.Valid,Is.True);yield return null;yield return null;long idle=recorder.LastValue;
                for(int i=0;i<50;i++)apply();
                yield return null;long sample=recorder.LastValue;
                var control=new byte[1024*1024];control[0]=1;yield return null;long controlBytes=recorder.LastValue;System.GC.KeepAlive(control);
                Debug.Log($"[PROJECTILE_TARGET_ALLOC] queries=50 colliders=182 idle={idle} sample={sample} control={controlBytes}");
                Assert.That(controlBytes,Is.GreaterThanOrEqualTo(1024*1024));
                Assert.That(sample-idle,Is.LessThan(20000),"Target helper setup should not allocate a collider array per projectile.");
            }
        }
        finally{Object.Destroy(shell);Object.Destroy(target);}
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
