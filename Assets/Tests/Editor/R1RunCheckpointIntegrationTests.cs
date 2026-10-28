using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R1;
using RoboRougelike.R3;

public sealed class R1RunCheckpointIntegrationTests
{
    string path;R1RunController run;
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        path=Path.Combine(Application.temporaryCachePath,"rrl-save-test-"+Guid.NewGuid().ToString("N")+".json");
        R1RunCheckpointStorage.TestPath=path;
        Assert.That(R1RunCheckpointStorage.PathName,Is.EqualTo(path),"Test must never write the real save slot");
        for(int i=0;i<300&&R1RunController.Instance?.Progress==null;i++)yield return null;
        run=R1RunController.Instance;Assert.That(run?.Progress,Is.Not.Null);
    }
    [UnityTest] public IEnumerator SaveAfterVictoryRestoresGarageWithoutDuplicatingPartsOrReward()
    {
        Assert.That(run.SelectVehicle(R3VehicleKind.Light),Is.True);Assert.That(run.PrepareUnifiedRun(),Is.True);yield return null;
        Assert.That(run.ChooseLD02Arena(),Is.True);while(run.Transitioning)yield return null;
        run.Encounter.SetCombatActive(false);
        foreach(var enemy in run.Encounter.Enemies.ToArray())enemy.Core.ApplyDamage(100000,enemy.transform.position,Vector3.zero);
        for(int i=0;i<300&&run.Progress.State!=R1RunState.Salvage;i++)yield return null;
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Salvage));
        var loot=run.Salvage.Items.First(p=>p.Selectable);
        Assert.That(run.ConfirmRewards(new[]{loot.Id},new[]{0}),Is.True);yield return null;
        var gun=run.VehicleModel.Parts.First(p=>p.Kind==R3PartKind.Gun);
        Assert.That(run.GarageRemove(gun.Cell),Is.True);yield return null;
        var wheel=run.VehicleModel.Parts.First(p=>p.Kind==R3PartKind.Wheel);
        Assert.That(run.GarageRemove(wheel.Cell),Is.True);yield return null;
        var armor=run.Player.GetComponent<R3VehicleAssembly>().Parts.First(p=>p.Kind==R3PartKind.Armor);
        string armorId=armor.Id;armor.Damageable.ApplyDamage(10,armor.transform.position,Vector3.zero);
        var ids=run.VehicleModel.Parts.Select(p=>p.Id).Concat(run.VehicleModel.Inventory.Select(p=>p.Id)).OrderBy(x=>x).ToArray();
        int balance=run.VehicleModel.Scrap;Assert.That(run.SaveRunCheckpoint(),Is.True,run.RunSaveMessage);
        Assert.That(File.Exists(path),Is.True);
        Assert.That(run.SaveRunCheckpoint(),Is.True,"Atomic replacement of an existing checkpoint");
        Assert.That(Directory.GetFiles(Path.GetDirectoryName(path),Path.GetFileName(path)+".*.tmp"),Is.Empty);
        var previous=run;run.RestartRun();
        for(int i=0;i<300&&(R1RunController.Instance==previous||R1RunController.Instance?.Progress==null);i++)yield return null;
        run=R1RunController.Instance;Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Ready));
        Assert.That(run.LoadRunCheckpoint(),Is.True,run.RunSaveMessage);
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Garage));Assert.That(run.Progress.CompletedEncounters,Is.EqualTo(1));
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(balance));
        Assert.That(run.VehicleModel.Parts.Single(p=>p.Id==armorId).Health,Is.LessThan(1));
        Assert.That(run.VehicleModel.Parts.Select(p=>p.Id).Concat(run.VehicleModel.Inventory.Select(p=>p.Id)).OrderBy(x=>x),Is.EqualTo(ids));
        Assert.That(run.LoadRunCheckpoint(),Is.False,"Cannot load again over an active run");
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(balance));
        Assert.That(run.GarageMount(gun.Id,gun.Cell,gun.RotationQuarterTurns),Is.True);
        Assert.That(run.GarageMount(wheel.Id,wheel.Cell,wheel.RotationQuarterTurns),Is.True,"Restored factory wheel retains its mounting rules");
        yield return null;Assert.That(run.ChooseLD02Arena(),Is.True);while(run.Transitioning)yield return null;
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Combat));
    }
    [UnityTest] public IEnumerator CorruptOrUnsupportedSaveCannotChangeTheReadyRun()
    {
        R1RunCheckpointStorage.Write(path,new R1RunCheckpoint{version=99});
        Assert.That(run.LoadRunCheckpoint(),Is.False);Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Ready));
        R1RunCheckpointStorage.Write(path,new R1RunCheckpoint{version=1});
        string valid=File.ReadAllText(path);File.WriteAllText(path,valid.Replace("sha256", "badHash"));
        Assert.That(run.LoadRunCheckpoint(),Is.False,"A mismatched checksum must reject the entire save");
        File.WriteAllText(path,"{broken");
        Assert.That(run.LoadRunCheckpoint(),Is.False);Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Ready));
        var progress=new R1RunProgress(6,true);Assert.That(progress.Start(),Is.True);Assert.That(progress.PrepareGarage(),Is.True);
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        var document=new R1RunCheckpoint{vehicle=(int)R3VehicleKind.Light,runId=Guid.NewGuid().ToString("N"),
            progress=progress.MakeGarageCheckpoint(),model=model.MakeCheckpoint(Time.time)};
        document.model.mounted[1].Id=document.model.mounted[0].Id;
        R1RunCheckpointStorage.Write(path,document);
        Assert.That(run.LoadRunCheckpoint(),Is.False,"A correctly checksummed save with duplicate IDs is still invalid");
        Assert.That(run.VehicleModel,Is.Null);yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {
        if(File.Exists(path))File.Delete(path);R1RunCheckpointStorage.TestPath=null;
        if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}
    }
}
