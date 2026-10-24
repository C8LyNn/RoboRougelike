using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.Mechanics.OBJ001;
using RogueMachine.P12PipelineSafe;

public class OBJ001IntegrationTests
{
    R1RunController run;
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;run=R1RunController.Instance;
    }
    static object Call(object target,string name,params object[] values)
    {
        var method=target.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.Public);
        Assert.That(method,Is.Not.Null,"Missing playable OBJ001 contract: "+name);return method.Invoke(target,values);
    }
    static T Read<T>(object target,string name)
    {
        var property=target.GetType().GetProperty(name);Assert.That(property,Is.Not.Null,"Missing OBJ001 observation: "+name);return (T)property.GetValue(target);
    }
    [UnityTest] public IEnumerator OriginalObjectiveSupportsExplicitCompletionOnlyForLiveActiveDefense()
    {
        var obj=new GameObject("Actual imported objective").AddComponent<ReactorDefenseObjective>();
        obj.SetAutomaticClock(false);obj.Configure(100,60);
        Assert.That((bool)Call(obj,"CompleteDefense"),Is.False);Assert.That(obj.Begin(),Is.True);
        Assert.That((bool)Call(obj,"CompleteDefense"),Is.True);Assert.That(obj.State,Is.EqualTo(DefenseState.Won));
        Assert.That((bool)Call(obj,"CompleteDefense"),Is.False);yield return null;
    }
    [UnityTest] public IEnumerator HostEntryUsesActualPreparationGarageAndCanonicalEncounter()
    {
        Assert.That(run.SelectVehicle(R3VehicleKind.Light),Is.True);
        Assert.That((bool)Call(run,"PrepareReactorRun"),Is.True);Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Garage));
        Assert.That(run.SandboxWorkshop,Is.False);Assert.That(run.VehicleModel.Scrap,Is.Zero);
        Assert.That((bool)Call(run,"ChooseReactorDefense"),Is.True);while(run.Transitioning)yield return null;
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Combat));Assert.That(Read<bool>(run,"ReactorActive"),Is.True);
        Assert.That(Read<int>(run,"DefenseWave"),Is.EqualTo(1));Assert.That(Read<int>(run,"DefenseWaveCount"),Is.EqualTo(3));
        Assert.That(Read<float>(run,"ReactorHp"),Is.EqualTo(600));Assert.That(run.Encounter.AliveCount,Is.EqualTo(2));
    }
    [UnityTest] public IEnumerator ImportedObjectiveUsesTheActualDamageableAsItsOnlyHealthAuthority()
    {
        var type=Type.GetType("RoboRougelike.OBJ001Integration.OBJ001ReactorActor, Assembly-CSharp");
        Assert.That(type,Is.Not.Null,"Original package needs a real host damage bridge, not a demo HP copy");
        var actor=type.GetMethod("Build").Invoke(null,new object[]{run.transform,run.GetComponent<R3VisualPalette>().Parts[0],new Vector3(100,1.2f,100),100f});
        var objective=Read<ReactorDefenseObjective>(actor,"Objective");var hp=Read<P12PipelineSafeDamageable>(actor,"Damageable");
        Assert.That(objective.Begin(),Is.True);hp.ApplyDamage(30,hp.transform.position,Vector3.forward);Call(objective,"SynchronizeIntegrity");
        Assert.That(hp.CurrentHp,Is.EqualTo(70));Assert.That(objective.Integrity,Is.EqualTo(hp.CurrentHp));
        hp.SetDurabilityMultiplier(2);Call(objective,"SynchronizeIntegrity");
        Assert.That(objective.MaximumIntegrity,Is.EqualTo(hp.MaxHp));Assert.That(objective.Integrity,Is.EqualTo(hp.CurrentHp));
        hp.ApplyDamage(10000,hp.transform.position,Vector3.forward);Call(objective,"SynchronizeIntegrity");
        Assert.That(objective.State,Is.EqualTo(DefenseState.Lost));Assert.That((bool)Call(objective,"CompleteDefense"),Is.False);
        objective.ResetObjective();Assert.That(objective.Begin(),Is.False,"Reset cannot resurrect an already destroyed physical actor");yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
