using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using RogueMachine.P12PipelineSafe;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public sealed class AimQueryPerformanceTests
{
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        yield return new EnterPlayMode();
    }

    [UnityTest] public IEnumerator DenseOwnHullDoesNotHideNearestTargetOrAllocatePerQuery()
    {
        var owner=new GameObject("Aim owner");
        var cameraObject=new GameObject("Aim camera");
        var near=new GameObject("Near target");
        var far=new GameObject("Far target");
        try
        {
            LogAssert.Expect(LogType.Error,"[Pack 18] No structural blocks found for the player vehicle.");
            LogAssert.Expect(LogType.Error,"[Pack 18] No structural blocks found for the player vehicle.");
            var controller=owner.AddComponent<P12PipelineSafeTankController>();
            controller.enabled=false;
            owner.transform.position=new Vector3(1000,1000,1000);
            cameraObject.transform.position=owner.transform.position;
            var camera=cameraObject.AddComponent<Camera>();
            typeof(P12PipelineSafeTankController).GetField("aimCamera",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(controller,camera);
            for(int i=0;i<80;i++)
            {
                var part=new GameObject("Own part "+i);
                part.transform.SetParent(owner.transform,false);
                part.transform.localPosition=Vector3.forward*(1+i*.04f);
                part.AddComponent<BoxCollider>().size=Vector3.one*.02f;
            }
            near.transform.position=owner.transform.position+Vector3.forward*6;
            var nearCollider=near.AddComponent<BoxCollider>();
            far.transform.position=owner.transform.position+Vector3.forward*10;
            var farCollider=far.AddComponent<BoxCollider>();
            Physics.SyncTransforms();
            var method=typeof(P12PipelineSafeTankController).GetMethod("FindAimPoint",BindingFlags.Instance|BindingFlags.NonPublic);
            var aim=(Func<bool,Vector3>)Delegate.CreateDelegate(typeof(Func<bool,Vector3>),controller,method);
            aim(true);
            Assert.That(controller.LastConsumedAimCollider,Is.EqualTo(nearCollider));
            near.SetActive(false);Physics.SyncTransforms();aim(true);
            Assert.That(controller.LastConsumedAimCollider,Is.EqualTo(farCollider));
            far.SetActive(false);Physics.SyncTransforms();var fallback=aim(true);
            Assert.That(controller.LastConsumedAimCollider,Is.Null);
            Assert.That(fallback.z,Is.EqualTo(camera.ViewportPointToRay(new Vector3(.5f,.5f,0)).origin.z+160).Within(.01f));
            near.SetActive(true);Physics.SyncTransforms();aim(true);
            using(var recorder=Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Memory,"GC Allocated In Frame",16))
            {
                Assert.That(recorder.Valid,Is.True);yield return null;yield return null;long idle=recorder.LastValue;
                for(int i=0;i<50;i++)aim(true);
                yield return null;long sample=recorder.LastValue;
                var control=new byte[1024*1024];control[0]=1;yield return null;long controlBytes=recorder.LastValue;GC.KeepAlive(control);
                Debug.Log($"[AIM_ALLOC] queries=50 ownParts=80 idle={idle} sample={sample} control={controlBytes}");
                Assert.That(controlBytes,Is.GreaterThanOrEqualTo(1024*1024));
                Assert.That(sample-idle,Is.LessThan(20000),"Repeated aim queries should not allocate hit arrays.");
            }
            var dense=new GameObject("Extreme own hull");dense.transform.SetParent(owner.transform,false);
            dense.transform.localPosition=Vector3.forward*4;
            for(int i=0;i<1050;i++)dense.AddComponent<BoxCollider>().size=Vector3.one*.02f;
            Physics.SyncTransforms();aim(true);
            Assert.That(controller.LastConsumedAimCollider,Is.EqualTo(nearCollider),"Even more than 1024 own colliders must not hide a valid target.");
        }
        finally{Object.Destroy(owner);Object.Destroy(cameraObject);Object.Destroy(near);Object.Destroy(far);}
    }

    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying)yield return new ExitPlayMode();}
}
