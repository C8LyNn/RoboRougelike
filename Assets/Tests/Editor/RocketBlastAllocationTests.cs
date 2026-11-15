using System.Collections;
using NUnit.Framework;
using RoboRougelike.Mechanics.Rockets;
using RoboRougelike.WeaponIntegration;
using RogueMachine.P12PipelineSafe;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class NestedRocketBlastTrigger : MonoBehaviour,IRocketDamageReceiver
{
    public RocketProjectile Next;
    public int Hits {get;private set;}
    public void ReceiveRocketDamage(float damage,Vector3 point,Vector3 impulse,GameObject attacker)
    {Hits++;Next.ApplyInterceptionDamage(100);}
}

public sealed class RocketBlastAllocationTests
{
    [UnitySetUp] public IEnumerator Setup()
    {EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);yield return new EnterPlayMode();}

    [UnityTest] public IEnumerator ManyActualExplosionsDoNotAllocateWorstCaseBuffersEachTime()
    {
        const int count=50;
        var owner=new GameObject("Blast allocation owner");owner.transform.position=new Vector3(1000,1000,1000);
        var body=owner.AddComponent<Rigidbody>();body.isKinematic=true;
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=owner.transform.position+Vector3.forward;
        var hp=target.AddComponent<P12PipelineSafeDamageable>();hp.Configure(10000,false);target.AddComponent<RRLRocketDamageReceiver>();
        var rockets=new RocketProjectile[count];
        try
        {
            var spec=new RocketConfig{maxBlastColliders=1024,maxCastHits=512,explodeWhenIntercepted=true}.Snapshot();
            for(int i=0;i<count;i++)
            {
                var item=new GameObject("Blast allocation rocket "+i);item.transform.position=owner.transform.position;
                var rocket=item.AddComponent<RocketProjectile>();
                Assert.That(rocket.TryInitialize(spec,body,owner.transform,owner,null,Vector3.forward),Is.True);
                rocket.enabled=false;rockets[i]=rocket;
            }
            Physics.SyncTransforms();
            using(var recorder=Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Memory,"GC Allocated In Frame",16))
            {
                Assert.That(recorder.Valid,Is.True);yield return null;yield return null;long idle=recorder.LastValue;
                foreach(var rocket in rockets)rocket.ApplyInterceptionDamage(100);
                yield return null;long sample=recorder.LastValue;
                var control=new byte[1024*1024];control[0]=1;yield return null;long controlBytes=recorder.LastValue;System.GC.KeepAlive(control);
                Debug.Log($"[ROCKET_BLAST_ALLOC] count={count} idle={idle} sample={sample} control={controlBytes}");
                Assert.That(controlBytes,Is.GreaterThanOrEqualTo(1024*1024));
                Assert.That(hp.CurrentHp,Is.LessThan(10000),"The explosions must actually damage a target.");
                Assert.That(sample-idle,Is.LessThan(500000),"Sparse explosions should not allocate their maximum query buffers each time.");
            }
        }
        finally{foreach(var rocket in rockets)if(rocket!=null)Object.Destroy(rocket.gameObject);Object.Destroy(target);Object.Destroy(owner);}
    }

    [UnityTest] public IEnumerator NestedActualExplosionsBothKeepTheirTargets()
    {
        var owner=new GameObject("Nested blast owner");owner.transform.position=new Vector3(1000,1000,1000);
        var body=owner.AddComponent<Rigidbody>();body.isKinematic=true;
        var trigger=GameObject.CreatePrimitive(PrimitiveType.Cube);trigger.transform.position=owner.transform.position+Vector3.forward;
        var triggerReceiver=trigger.AddComponent<NestedRocketBlastTrigger>();
        var firstTarget=GameObject.CreatePrimitive(PrimitiveType.Cube);firstTarget.transform.position=owner.transform.position+new Vector3(-2,0,0);
        var firstHp=firstTarget.AddComponent<P12PipelineSafeDamageable>();firstHp.Configure(500,false);firstTarget.AddComponent<RRLRocketDamageReceiver>();
        var nestedTarget=GameObject.CreatePrimitive(PrimitiveType.Cube);nestedTarget.transform.position=owner.transform.position+new Vector3(10,0,1);
        var nestedHp=nestedTarget.AddComponent<P12PipelineSafeDamageable>();nestedHp.Configure(500,false);nestedTarget.AddComponent<RRLRocketDamageReceiver>();
        var outerObject=new GameObject("Outer rocket");outerObject.transform.position=owner.transform.position;
        var innerObject=new GameObject("Inner rocket");innerObject.transform.position=owner.transform.position+Vector3.right*10;
        try
        {
            var spec=new RocketConfig{explodeWhenIntercepted=true}.Snapshot();
            var outer=outerObject.AddComponent<RocketProjectile>();var inner=innerObject.AddComponent<RocketProjectile>();
            Assert.That(outer.TryInitialize(spec,body,owner.transform,owner,null,Vector3.forward),Is.True);
            Assert.That(inner.TryInitialize(spec,body,owner.transform,owner,null,Vector3.forward),Is.True);
            triggerReceiver.Next=inner;Physics.SyncTransforms();outer.ApplyInterceptionDamage(100);
            Assert.That(triggerReceiver.Hits,Is.EqualTo(1));
            Assert.That(firstHp.CurrentHp,Is.LessThan(500),"The outer explosion must keep its other target.");
            Assert.That(nestedHp.CurrentHp,Is.LessThan(500),"The nested explosion must reach its own target.");
        }
        finally{Object.Destroy(owner);Object.Destroy(trigger);Object.Destroy(firstTarget);Object.Destroy(nestedTarget);Object.Destroy(outerObject);Object.Destroy(innerObject);}
        yield return null;
    }

    [UnityTest] public IEnumerator ExtremeColliderCrowdingStillResolvesOneActualExplosion()
    {
        var owner=new GameObject("Extreme blast owner");owner.transform.position=new Vector3(1000,1000,1000);
        var body=owner.AddComponent<Rigidbody>();body.isKinematic=true;
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=owner.transform.position+Vector3.forward;
        var hp=target.AddComponent<P12PipelineSafeDamageable>();hp.Configure(500,false);target.AddComponent<RRLRocketDamageReceiver>();
        for(int i=0;i<4100;i++)target.AddComponent<SphereCollider>().radius=.45f;
        var rocketObject=new GameObject("Extreme blast projectile");rocketObject.transform.position=owner.transform.position;
        try
        {
            var rocket=rocketObject.AddComponent<RocketProjectile>();
            var spec=new RocketConfig{maxBlastColliders=8,maxCastHits=8,explodeWhenIntercepted=true}.Snapshot();
            Assert.That(rocket.TryInitialize(spec,body,owner.transform,owner,null,Vector3.forward),Is.True);
            Physics.SyncTransforms();rocket.ApplyInterceptionDamage(100);
            Assert.That(hp.CurrentHp,Is.LessThan(500));
            Assert.That(500-hp.CurrentHp,Is.LessThanOrEqualTo(spec.CenterDamage),"Many colliders cannot multiply damage to the same part.");
        }
        finally{Object.Destroy(owner);Object.Destroy(target);Object.Destroy(rocketObject);}
        yield return null;
    }

    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying)yield return new ExitPlayMode();}
}
