using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using RogueMachine.P12PipelineSafe;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class EnemyCrowdedQueryPerformanceTests
{
    [UnityTest] public IEnumerator CrowdedMovementProbeHasBoundedSteadyStateAllocations()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        yield return new EnterPlayMode();
        var root=new GameObject("enemy query allocation fixture");root.transform.position=new Vector3(16000,0,16000);
        var body=root.AddComponent<Rigidbody>();body.isKinematic=true;
        var enemy=root.AddComponent<P12PipelineSafeEnemyTurret>();enemy.enabled=false;
        typeof(P12PipelineSafeEnemyTurret).GetField("movementBody",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(enemy,body);
        try
        {
            for(int i=0;i<300;i++)
            {
                var child=new GameObject("own collider "+i);child.transform.SetParent(root.transform,false);
                child.transform.localPosition=new Vector3(0,.75f,1.1f);child.AddComponent<BoxCollider>().size=Vector3.one*.01f;
            }
            Physics.SyncTransforms();
            var origin=body.position+Vector3.up*.75f;
            Assert.That(Physics.BoxCastAll(origin,new Vector3(.88f,.67f,.88f),Vector3.forward,Quaternion.identity,2,~0,QueryTriggerInteraction.Ignore).Length,Is.GreaterThan(256));
            var probe=(Func<Vector3,float,bool,bool>)Delegate.CreateDelegate(typeof(Func<Vector3,float,bool,bool>),enemy,
                typeof(P12PipelineSafeEnemyTurret).GetMethod("CanMove",BindingFlags.Instance|BindingFlags.NonPublic));
            for(int i=0;i<3;i++)Assert.That(probe(Vector3.forward,2,false),Is.True);
            using(var recorder=Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Memory,"GC Allocated In Frame",16))
            {
                Assert.That(recorder.Valid,Is.True);yield return null;yield return null;long idle=recorder.LastValue;
                for(int i=0;i<50;i++)probe(Vector3.forward,2,false);
                yield return null;long sample=recorder.LastValue;
                var control=new byte[1024*1024];control[0]=1;yield return null;long controlBytes=recorder.LastValue;GC.KeepAlive(control);
                Debug.Log($"[ENEMY_QUERY_ALLOC] queries=50 colliders=300 idle={idle} sample={sample} control={controlBytes}");
                Assert.That(controlBytes,Is.GreaterThanOrEqualTo(1024*1024));
                Assert.That(sample-idle,Is.LessThan(20000),"The same crowded movement probe must not allocate an array every tick.");
            }
        }
        finally{UnityEngine.Object.Destroy(root);}
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {if(Application.isPlaying)yield return new ExitPlayMode();}
}
