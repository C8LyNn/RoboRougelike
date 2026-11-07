using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R1;
using RoboRougelike.R3;

public sealed class IsolatedGarageLocationTests
{
    R1RunController run;
    string savePath;

    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");
        yield return new EnterPlayMode();
        float limit=Time.realtimeSinceStartup+20;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<limit)yield return null;
        run=R1RunController.Instance;Assert.That(run?.Progress,Is.Not.Null);
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {
        R1RunCheckpointStorage.TestPath=null;
        if(!string.IsNullOrEmpty(savePath)&&File.Exists(savePath))File.Delete(savePath);
        if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}
    }

    void AssertGaragePose()
    {
        var space=GameObject.Find("Garaż — osobna przestrzeń");
        Assert.That(space,Is.Not.Null,"Garaż musi mieć własną przestrzeń roboczą.");
        Assert.That(Vector3.Distance(run.Player.transform.position,space.transform.position),Is.LessThan(2));
        Assert.That(Vector3.Distance(run.Player.transform.position,Vector3.zero),Is.GreaterThan(100),"Garaż musi być fizycznie poza areną.");
        Assert.That(Quaternion.Angle(run.Player.transform.rotation,Quaternion.identity),Is.LessThan(.01f));
        var body=run.Player.GetComponent<Rigidbody>();
        Assert.That(body.linearVelocity.sqrMagnitude,Is.LessThan(.0001f));
        Assert.That(body.angularVelocity.sqrMagnitude,Is.LessThan(.0001f));
    }

    [UnityTest] public IEnumerator WorkshopRangeAndMapEditorKeepSeparateLocations()
    {
        Assert.That(run.SelectVehicle(R3VehicleKind.Light),Is.True);
        Assert.That(run.OpenSandboxGarage(),Is.True);AssertGaragePose();
        var original=run.VehicleModel;int scrap=original.Scrap;
        Assert.That(run.EnterTestRange(),Is.True);while(run.Transitioning)yield return null;
        Assert.That(GameObject.Find("Garaż — osobna przestrzeń"),Is.Null,"Garaż nie może zostać na poligonie.");
        Assert.That(run.Player.transform.position.y,Is.LessThan(100));
        var body=run.Player.GetComponent<Rigidbody>();body.rotation=Quaternion.Euler(42,71,29);
        body.linearVelocity=new Vector3(4,2,1);body.angularVelocity=new Vector3(1,2,3);
        Assert.That(run.ReturnFromTestRange(),Is.True);while(run.Transitioning)yield return null;
        Assert.That(run.VehicleModel,Is.SameAs(original));Assert.That(run.VehicleModel.Scrap,Is.EqualTo(scrap));AssertGaragePose();
    }

    [UnityTest] public IEnumerator VictoryMovesVehicleToUprightGarageBeforeFurtherBuilding()
    {
        Assert.That(run.PrepareUnifiedRun(),Is.True);AssertGaragePose();
        Assert.That(run.StartNextEncounter(),Is.True);Assert.That(run.ChooseRoute(0),Is.True);
        while(run.Transitioning)yield return null;
        Assert.That(GameObject.Find("Garaż — osobna przestrzeń"),Is.Null);
        run.Encounter.SetCombatActive(false);
        RRLW2TestFlow.DefeatAll(run);
        float limit=Time.realtimeSinceStartup+20;
        while(run.Progress.State!=R1RunState.Salvage&&Time.realtimeSinceStartup<limit)yield return null;
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Salvage));
        // Salvage pauses simulation; reproduce the off-axis/spinning final pose
        // without allowing physics to turn it into an unrelated defeat.
        var body=run.Player.GetComponent<Rigidbody>();body.rotation=Quaternion.Euler(37,64,23);
        body.linearVelocity=new Vector3(3,2,1);body.angularVelocity=new Vector3(1,2,3);
        Assert.That(RRLW2TestFlow.Claim(run,Array.Empty<string>()),Is.True);AssertGaragePose();
    }

    [UnityTest] public IEnumerator MapEditorClosesWorkshopAndRestoresItOnReturn()
    {
        Assert.That(run.OpenMapEditor(),Is.True);Assert.That(run.MapEditing,Is.True);
        Assert.That(GameObject.Find("Garaż — osobna przestrzeń"),Is.Null);
        run.MapEditorGarage();Assert.That(run.MapEditing,Is.False);AssertGaragePose();
        Assert.That(run.ReturnToMapEditorFromGarage(),Is.True);Assert.That(run.MapEditing,Is.True);
        Assert.That(GameObject.Find("Garaż — osobna przestrzeń"),Is.Null);
        yield return null;
    }

    [UnityTest] public IEnumerator LoadingGarageCheckpointRecreatesNeutralWorkshopWithoutChangingInventory()
    {
        savePath=Path.Combine(Application.temporaryCachePath,"rrl-garage-location-"+Guid.NewGuid().ToString("N")+".json");
        R1RunCheckpointStorage.TestPath=savePath;
        Assert.That(run.PrepareUnifiedRun(),Is.True);AssertGaragePose();
        var scrap=run.VehicleModel.Scrap;var count=run.VehicleModel.Parts.Count;
        Assert.That(run.SaveRunCheckpoint(),Is.True,run.RunSaveMessage);
        var old=run;run.RestartRun();float limit=Time.realtimeSinceStartup+20;
        while((R1RunController.Instance==old||R1RunController.Instance?.Progress==null)&&Time.realtimeSinceStartup<limit)yield return null;
        run=R1RunController.Instance;Assert.That(run?.Progress?.State,Is.EqualTo(R1RunState.Ready));
        Assert.That(run.LoadRunCheckpoint(),Is.True,run.RunSaveMessage);
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(scrap));Assert.That(run.VehicleModel.Parts.Count,Is.EqualTo(count));
        AssertGaragePose();
    }
}
