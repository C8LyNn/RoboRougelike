using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.Mechanics.MOV009;
using RoboRougelike.MOV009Integration;

public sealed class SuspensionUxTests
{
    static object Capture(R1RunController run)
    {
        var type=typeof(MOV009VehicleAdapter).Assembly.GetType("RoboRougelike.MOV009Integration.MOV009Presentation");
        Assert.That(type,Is.Not.Null,"A single presentation must report actual suspension status");
        var method=type.GetMethod("Capture",BindingFlags.Public|BindingFlags.Static);
        Assert.That(method,Is.Not.Null);return method.Invoke(null,new object[]{run});
    }
    static T Field<T>(object snapshot,string name)=>(T)snapshot.GetType().GetField(name).GetValue(snapshot);
    [UnityTest] public IEnumerator RealForceAndPauseAreNotConfusedWithSelectedMode()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        float deadline=Time.realtimeSinceStartup+15;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<deadline)yield return null;
        var run=R1RunController.Instance;Assert.That(run.SelectVehicle(R3VehicleKind.Light),Is.True);Assert.That(run.OpenSandboxGarage(),Is.True);
        foreach(var cell in MOV009ModelTests.Cells(R3VehicleKind.Light))Assert.That(run.GarageBuy(cell,R3PartKind.Suspension),Is.True);
        Assert.That(run.ConfigureSuspension(true,ActiveSuspensionSupportMode.FullSupport,.1f),Is.True);
        Assert.That(run.EnterTestRange(),Is.True);while(run.Transitioning)yield return null;
        deadline=Time.realtimeSinceStartup+8;
        while(!Field<bool>(Capture(run),"Applying")&&Time.realtimeSinceStartup<deadline)yield return null;
        var live=Capture(run);Assert.That(Field<bool>(live,"Applying"),Is.True,"must be backed by actual force");
        Assert.That(Field<float>(live,"ForceNewtons"),Is.GreaterThan(0));Assert.That(Field<string>(live,"Status"),Does.Contain("Pracuje"));
        run.TogglePause();var paused=Capture(run);
        Assert.That(Field<bool>(paused,"Applying"),Is.False);Assert.That(Field<float>(paused,"ForceNewtons"),Is.Zero);
        Assert.That(Field<bool>(paused,"CanConfigure"),Is.True);Assert.That(Field<string>(paused,"Status"),Does.Contain("Gotowe"));
        yield return new ExitPlayMode();
    }
    [UnityTest] public IEnumerator CombatPauseShowsControlsAreReadOnly()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        float deadline=Time.realtimeSinceStartup+15;while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<deadline)yield return null;
        var run=R1RunController.Instance;Assert.That(run.PrepareUnifiedRun(),Is.True);
        Assert.That(run.StartNextEncounter(),Is.True);Assert.That(run.ChooseRoute(0),Is.True);while(run.Transitioning)yield return null;
        run.TogglePause();Assert.That(Field<bool>(Capture(run),"CanConfigure"),Is.False);
        yield return new ExitPlayMode();
    }
    [UnityTest] public IEnumerator GarageExplainsMissingWorkingOffHoverAndDestroyedModules()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        float deadline=Time.realtimeSinceStartup+15;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<deadline)yield return null;
        var run=R1RunController.Instance;Assert.That(run.SelectVehicle(R3VehicleKind.Light),Is.True);
        Assert.That(run.OpenSandboxGarage(),Is.True);
        var missing=Capture(run);Assert.That(Field<string>(missing,"Status"),Does.Contain("Brak"));
        Assert.That(Field<int>(missing,"OperationalCount"),Is.Zero);Assert.That(Field<bool>(missing,"Applying"),Is.False);
        var cell=MOV009ModelTests.Cells(R3VehicleKind.Light)[0];Assert.That(run.GarageBuy(cell,R3PartKind.Suspension),Is.True);
        var ready=Capture(run);Assert.That(Field<int>(ready,"OperationalCount"),Is.EqualTo(1));
        Assert.That(Field<string>(ready,"Status"),Does.Contain("Gotowe"));Assert.That(Field<string>(ready,"Mode"),Is.EqualTo("Full"));
        Assert.That(run.ConfigureSuspension(false,ActiveSuspensionSupportMode.FullSupport,.2f),Is.True);
        var off=Capture(run);Assert.That(Field<string>(off,"Mode"),Is.EqualTo("OFF"));Assert.That(Field<bool>(off,"Applying"),Is.False);
        Assert.That(run.ConfigureSuspension(true,ActiveSuspensionSupportMode.AssistOnly,.2f),Is.True);
        var assist=Capture(run);Assert.That(Field<string>(assist,"Mode"),Is.EqualTo("Assist"));Assert.That(Field<float>(assist,"Offset"),Is.EqualTo(.2f).Within(.001f));
        Assert.That(run.GarageBuy(MOV009ModelTests.Cells(R3VehicleKind.Light)[3],R3PartKind.HoverEngine),Is.True);
        Assert.That(run.ConfigureDrive(R3DriveKind.Hover),Is.True);
        var hover=Capture(run);Assert.That(Field<string>(hover,"Status"),Does.Contain("Hover"));Assert.That(Field<bool>(hover,"Applying"),Is.False);
        Assert.That(run.ConfigureDrive(R3DriveKind.Wheels),Is.True);
        var part=run.Player.GetComponent<R3VehicleAssembly>().Parts.First(p=>p.Kind==R3PartKind.Suspension);
        part.Damageable.ApplyDamage(10000,part.transform.position,Vector3.up);
        var damaged=Capture(run);Assert.That(Field<int>(damaged,"OperationalCount"),Is.Zero);Assert.That(Field<bool>(damaged,"Applying"),Is.False);
        Assert.That(Field<string>(damaged,"Status"),Does.Contain("sprawnych"));
        Assert.That(run.GarageRemove(cell),Is.True);
        Assert.That(Field<string>(Capture(run),"Status"),Does.Contain("Brak"));
        yield return new ExitPlayMode();
    }
}
