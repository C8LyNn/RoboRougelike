using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R1;
using RoboRougelike.R4;

public sealed class Camera275AimNativeTests
{
    Camera275AimQA fixture;Camera275RuntimeInput input;R1RunController run;
    [UnitySetUp]public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;run=R1RunController.Instance;
        fixture=new GameObject("CAM275 aim test").AddComponent<Camera275AimQA>();input=fixture.gameObject.AddComponent<Camera275RuntimeInput>();
    }
    IEnumerator Basic(float distance)
    {
        var result=new Camera275AimQA.ShotResult{weapon="Basic",requestedDistance=distance};
        yield return fixture.Execute(run,input,R4WeaponKind.Basic,distance,result);
        Assert.That(result.status,Is.EqualTo("Passed"),result.error);
        Assert.That(result.damage,Is.GreaterThan(0),"A physical projectile must actually reduce canonical P12 HP");
    }
    [UnityTest]public IEnumerator ActualBasicProjectileHitsAt10Meters()=>Basic(10);
    [UnityTest]public IEnumerator ActualBasicProjectileHitsAt30Meters()=>Basic(30);
    [UnityTest]public IEnumerator ActualBasicProjectileHitsAt60Meters()=>Basic(60);
    [UnityTest]public IEnumerator ActualBasicProjectileHitsAt100Meters()=>Basic(100);
    [UnityTest]public IEnumerator BeamDoesNotPretendToReach100Meters()
    {
        var result=new Camera275AimQA.ShotResult{weapon="BeamCutter",requestedDistance=100};
        yield return fixture.Execute(run,input,R4WeaponKind.BeamCutter,100,result);
        Assert.That(result.status,Is.EqualTo("Passed"),result.error);Assert.That(result.range,Is.EqualTo(16));
        Assert.That(result.damage,Is.Zero);StringAssert.Contains("poza zasięgiem",result.feedback);
    }
    [UnityTearDown]public IEnumerator Cleanup()
    {
        if(fixture!=null)Object.DestroyImmediate(fixture.gameObject);
        if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}
    }
}
