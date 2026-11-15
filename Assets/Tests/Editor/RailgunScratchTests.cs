using System.Collections;
using NUnit.Framework;
using RoboRougelike.WeaponIntegration;
using RoboRougelike.WPN002Railgun;
using RogueMachine.P12PipelineSafe;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class RailScratchWall : MonoBehaviour,IRRLHitReceiver
{
    public int Hits {get;private set;}
    public bool ReceiveHit(float damage,Vector3 point,Vector3 direction,Transform source){Hits++;return true;}
}

public sealed class RailgunScratchTests
{
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        yield return new EnterPlayMode();
    }

    [UnityTest] public IEnumerator ManyRailProjectilesDoNotEachAllocateLargeHitBuffers()
    {
        var shots=new GameObject[100];
        for(int i=0;i<shots.Length;i++)shots[i]=new GameObject("Rail allocation fixture "+i);
        try
        {
            using(var recorder=Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Memory,"GC Allocated In Frame",16))
            {
                Assert.That(recorder.Valid,Is.True);yield return null;yield return null;long idle=recorder.LastValue;
                foreach(var shot in shots)shot.AddComponent<RRLRailPenetration>();
                yield return null;long sample=recorder.LastValue;
                var control=new byte[1024*1024];control[0]=1;yield return null;long controlBytes=recorder.LastValue;System.GC.KeepAlive(control);
                Debug.Log($"[RAIL_CREATE_ALLOC] count={shots.Length} idle={idle} sample={sample} control={controlBytes}");
                Assert.That(controlBytes,Is.GreaterThanOrEqualTo(1024*1024));
                Assert.That(sample-idle,Is.LessThan(250000),"Each shot should not allocate a large physics query buffer.");
            }
        }
        finally{foreach(var shot in shots)if(shot!=null)Object.Destroy(shot);}
    }

    [UnityTest] public IEnumerator NestedDamageCallbackDoesNotOverwriteOuterProjectileTargets()
    {
        var owner=new GameObject("Rail owner");var body=owner.AddComponent<Rigidbody>();body.isKinematic=true;
        var outerObject=new GameObject("Outer rail");outerObject.AddComponent<Rigidbody>().isKinematic=true;
        var nestedObject=new GameObject("Nested rail");nestedObject.AddComponent<Rigidbody>().isKinematic=true;
        var first=GameObject.CreatePrimitive(PrimitiveType.Cube);first.transform.position=new Vector3(1000,1000,2);
        var firstHp=first.AddComponent<P12PipelineSafeDamageable>();firstHp.Configure(1,false);
        var second=GameObject.CreatePrimitive(PrimitiveType.Cube);second.transform.position=new Vector3(1000,1000,4);
        var secondHp=second.AddComponent<P12PipelineSafeDamageable>();secondHp.Configure(500,false);
        var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=new Vector3(1010,1000,2);
        var receiver=wall.AddComponent<RailScratchWall>();
        try
        {
            var spec=new RailgunConfig().Snapshot();
            var outer=outerObject.AddComponent<RRLRailPenetration>();
            var nested=nestedObject.AddComponent<RRLRailPenetration>();
            outer.Bind(new RailgunShot(1,new Vector3(1000,1000,0),Vector3.forward,Vector3.forward*100,spec,body,owner),1);
            nested.Bind(new RailgunShot(2,new Vector3(1010,1000,0),Vector3.forward,Vector3.forward*100,spec,body,owner),1);
            firstHp.Died+=()=>nested.Step(.1f);
            Physics.SyncTransforms();outer.Step(.1f);
            Assert.That(receiver.Hits,Is.EqualTo(1),"The nested projectile must reach its own target.");
            Assert.That(secondHp.CurrentHp,Is.LessThan(500),"The original projectile must still process its next target.");
        }
        finally{Object.Destroy(owner);Object.Destroy(outerObject);Object.Destroy(nestedObject);Object.Destroy(first);Object.Destroy(second);Object.Destroy(wall);}
        yield return null;
    }

    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying)yield return new ExitPlayMode();}
}
