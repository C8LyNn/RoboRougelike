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
    [UnityTest] public IEnumerator LargeWalletAndSaturatedRunSummarySurviveRealSaveAndLoad()
    {
        Assert.That(run.SelectVehicle(R3VehicleKind.Light),Is.True);Assert.That(run.PrepareUnifiedRun(),Is.True);yield return null;
        run.VehicleModel.Award(150000);int balance=run.VehicleModel.Scrap;Assert.That(balance,Is.GreaterThan(100000));
        typeof(R1RunController).GetProperty("EarnedScrap").GetSetMethod(true).Invoke(run,new object[]{int.MaxValue});
        Assert.That(run.SaveRunCheckpoint(),Is.True,run.RunSaveMessage);var previous=run;run.RestartRun();
        for(int i=0;i<300&&(R1RunController.Instance==previous||R1RunController.Instance?.Progress==null);i++)yield return null;
        run=R1RunController.Instance;Assert.That(run.LoadRunCheckpoint(),Is.True,run.RunSaveMessage);
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(balance));Assert.That(run.EarnedScrap,Is.EqualTo(int.MaxValue));
    }
    [UnityTest] public IEnumerator MissingGarageCameraRejectsLoadWithoutPartialRun()
    {
        var savedProgress=new R1RunProgress(6,true);Assert.That(savedProgress.Start()&&savedProgress.PrepareGarage(),Is.True);
        var savedModel=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        R1RunCheckpointStorage.Write(path,new R1RunCheckpoint{vehicle=(int)R3VehicleKind.Light,runId=Guid.NewGuid().ToString("N"),progress=savedProgress.MakeGarageCheckpoint(),model=savedModel.MakeCheckpoint(Time.time)});
        var original=File.ReadAllText(path);var initialProgress=run.Progress;var initialPlan=run.Plan;
        var field=typeof(R1RunController).GetField("followCamera",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        Assert.That(field,Is.Not.Null);var camera=field.GetValue(run);
        try
        {
            field.SetValue(run,null);
            Assert.That(run.LoadRunCheckpoint(),Is.False);
            Assert.That(run.RunSaveMessage,Does.Contain("kamery"));
            Assert.That(run.Progress,Is.SameAs(initialProgress));Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Ready));
            Assert.That(run.Plan,Is.SameAs(initialPlan));Assert.That(run.VehicleModel,Is.Null);Assert.That(run.Player,Is.Null);
            Assert.That(File.ReadAllText(path),Is.EqualTo(original),"A failed load must preserve the checkpoint.");
        }
        finally{field.SetValue(run,camera);}
        yield return null;
    }
    [UnityTest] public IEnumerator RejectedLoadDoesNotCommitTechnologyToProfile()
    {
        var progress=new R1RunProgress(6,true);Assert.That(progress.Start()&&progress.PrepareGarage(),Is.True);
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        Assert.That(model.ReceiveSalvage(new[]{new R3PartState(default,R3PartKind.Gun){Id=Guid.NewGuid().ToString("N"),Weapon=RoboRougelike.R4.R4WeaponKind.Railgun}}),Is.True);
        R1RunCheckpointStorage.Write(path,new R1RunCheckpoint{vehicle=(int)R3VehicleKind.Light,runId=Guid.NewGuid().ToString("N"),progress=progress.MakeGarageCheckpoint(),model=model.MakeCheckpoint(Time.time)});
        string original=File.ReadAllText(path);
        string technologyPath=Path.Combine(Application.temporaryCachePath,"rrl-technology-test-"+Guid.NewGuid().ToString("N")+".json");
        var technology=new R1TechnologyProgress(technologyPath);
        typeof(R1RunController).GetProperty("Technology").GetSetMethod(true).Invoke(run,new object[]{technology});
        var stages=run.Definition.RouteStages;
        try
        {
            Assert.That(technology.IsUnlocked(R3PartKind.Gun,RoboRougelike.R4.R4WeaponKind.Railgun),Is.False);
            run.Definition.RouteStages=null;
            Assert.That(run.LoadRunCheckpoint(),Is.False,"An invalid route must reject the load.");
            Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Ready));Assert.That(run.VehicleModel,Is.Null);
            Assert.That(technology.IsUnlocked(R3PartKind.Gun,RoboRougelike.R4.R4WeaponKind.Railgun),Is.False,"A rejected load must not change technology in memory.");
            Assert.That(File.Exists(technologyPath),Is.False,"A rejected load must not write a technology profile.");
            Assert.That(File.ReadAllText(path),Is.EqualTo(original));
            run.Definition.RouteStages=stages;
            Assert.That(run.LoadRunCheckpoint(),Is.True,run.RunSaveMessage);
            Assert.That(technology.IsUnlocked(R3PartKind.Gun,RoboRougelike.R4.R4WeaponKind.Railgun),Is.True);
            Assert.That(File.Exists(technologyPath),Is.True,"A successful load should preserve legacy technology discovery.");
            Assert.That(File.ReadAllText(path),Is.EqualTo(original));
        }
        finally{run.Definition.RouteStages=stages;if(File.Exists(technologyPath))File.Delete(technologyPath);}
        yield return null;
    }
    [UnityTest] public IEnumerator SuccessfulLoadClearsResolvedTechnologyWriteError()
    {
        var progress=new R1RunProgress(6,true);Assert.That(progress.Start()&&progress.PrepareGarage(),Is.True);
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        var gun=new R3PartState(default,R3PartKind.Gun){Weapon=RoboRougelike.R4.R4WeaponKind.Railgun};
        Assert.That(model.ReceiveSalvage(new[]{gun}),Is.True);
        R1RunCheckpointStorage.Write(path,new R1RunCheckpoint{vehicle=(int)R3VehicleKind.Light,
            runId=Guid.NewGuid().ToString("N"),progress=progress.MakeGarageCheckpoint(),model=model.MakeCheckpoint(Time.time)});
        string parent=path+"-tech-blocked",target=Path.Combine(parent,"technology.json");
        try
        {
            File.WriteAllText(parent,"temporary blocker");var technology=new R1TechnologyProgress(target);
            technology.Discover(new[]{gun});Assert.That(technology.Error,Is.Not.Null);
            typeof(R1RunController).GetProperty("Technology").GetSetMethod(true).Invoke(run,new object[]{technology});
            typeof(R1RunController).GetProperty("TechnologyMessage").GetSetMethod(true).Invoke(run,new object[]{technology.Error});
            File.Delete(parent);Directory.CreateDirectory(parent);
            Assert.That(run.LoadRunCheckpoint(),Is.True,run.RunSaveMessage);
            Assert.That(technology.Error,Is.Null);Assert.That(File.Exists(target),Is.True);
            Assert.That(run.TechnologyMessage,Does.Not.Contain("Nie zapisano"));
            Assert.That(new R1TechnologyProgress(target).IsUnlocked(R3PartKind.Gun,RoboRougelike.R4.R4WeaponKind.Railgun),Is.True);
        }
        finally
        {
            if(File.Exists(target))File.Delete(target);
            if(Directory.Exists(parent))Directory.Delete(parent);
            if(File.Exists(parent))File.Delete(parent);
        }
        yield return null;
    }
    [UnityTest] public IEnumerator SelectedStarterVariantAndBudgetSurviveSaveAndLoad()
    {
        Assert.That(run.SelectVehicle(R3VehicleKind.Light,3),Is.True);
        Assert.That(run.PrepareUnifiedRun(),Is.True);yield return null;
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(200));
        Assert.That(run.SaveRunCheckpoint(),Is.True,run.RunSaveMessage);
        Assert.That(R1RunCheckpointStorage.Read(path).variant,Is.EqualTo(3));
        var previous=run;run.RestartRun();
        for(int i=0;i<300&&(R1RunController.Instance==previous||R1RunController.Instance?.Progress==null);i++)yield return null;
        run=R1RunController.Instance;Assert.That(run.LoadRunCheckpoint(),Is.True,run.RunSaveMessage);
        Assert.That(run.SelectedVehicle,Is.EqualTo(R3VehicleKind.Light));Assert.That(run.SelectedVehicleVariant,Is.EqualTo(3));
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(200));
        Assert.That(run.VehicleModel.Design.Name,Is.EqualTo(R3VehicleCatalog.Create(R3VehicleKind.Light,3).Name));
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
    [UnityTest] public IEnumerator LegacyWideBridgeLoadsWithNoticeAndDoesNotOverwriteTheOriginalFile()
    {
        var progress=new R1RunProgress(6,true);Assert.That(progress.Start(),Is.True);Assert.That(progress.PrepareGarage(),Is.True);
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));var saved=model.MakeCheckpoint(Time.time);
        saved.energyVersion=0;saved.hasA=saved.hasB=false;
        var core=new R3PartState(Vector3Int.zero,R3PartKind.Core){DefenseRechargeAge=float.MaxValue};
        var shield=new R3PartState(Vector3Int.right,R3PartKind.DirectionalShield,.8f){DefenseRechargeAge=float.MaxValue};
        var armor=new R3PartState(new Vector3Int(3,0,0),R3PartKind.Armor,.55f){DefenseRechargeAge=float.MaxValue};
        saved.mounted=new[]{core,shield,armor};saved.inventory=new R3PartState[0];
        saved.wheels=new R3WheelCheckpoint[0];saved.groups=new R3GroupCheckpoint[0];
        R1RunCheckpointStorage.Write(path,new R1RunCheckpoint{vehicle=(int)R3VehicleKind.Light,runId=Guid.NewGuid().ToString("N"),
            progress=progress.MakeGarageCheckpoint(),model=saved});
        string original=File.ReadAllText(path);
        Assert.That(run.LoadRunCheckpoint(),Is.True,run.RunSaveMessage);
        Assert.That(run.RunSaveMessage,Does.Contain("magazynu"));
        Assert.That(run.VehicleModel.Inventory.Single(p=>p.Id==armor.Id).Health,Is.EqualTo(.55f));
        Assert.That(run.VehicleModel.Parts.Select(p=>p.Id),Is.EquivalentTo(new[]{core.Id,shield.Id}));
        Assert.That(File.ReadAllText(path),Is.EqualTo(original),"Loading must not modify the original save.");
        yield return null;
    }
    [UnityTest] public IEnumerator LegacySideMountedGunLoadsIntoInventoryWithNotice()
    {
        CheckLegacySideMountedGun(run,path);
        yield return null;
    }
    static void CheckLegacySideMountedGun(R1RunController run,string path)
    {
        var progress=new R1RunProgress(6,true);Assert.That(progress.Start(),Is.True);Assert.That(progress.PrepareGarage(),Is.True);
        // Construct an actual old side-only connection, independent of future
        // starter hull changes which may put a valid support under the gun.
        var design=new R3VehicleDesign{Name="Legacy side mount"};
        design.Parts.Add(new R3PartState(Vector3Int.zero,R3PartKind.Core));
        design.Parts.Add(new R3PartState(Vector3Int.right,R3PartKind.Armor));
        design.Parts.Add(new R3PartState(new Vector3Int(2,0,0),R3PartKind.Gun,.43f){ContactMask=R3MountingGrid.AllFaces});
        var model=new R3VehicleModel(design);var saved=model.MakeCheckpoint(Time.time);
        var gun=saved.mounted.First(p=>p.Kind==R3PartKind.Gun);
        string id=gun.Id;
        R1RunCheckpointStorage.Write(path,new R1RunCheckpoint{vehicle=(int)R3VehicleKind.Standard,runId=Guid.NewGuid().ToString("N"),
            progress=progress.MakeGarageCheckpoint(),model=saved});
        string original=File.ReadAllText(path);
        Assert.That(run.LoadRunCheckpoint(),Is.True,run.RunSaveMessage);
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Garage));
        Assert.That(run.RunSaveMessage,Does.Contain("punktów mocowania"));
        Assert.That(run.VehicleModel.Inventory.Single(p=>p.Id==id).Health,Is.EqualTo(.43f));
        Assert.That(run.Player.GetComponent<R3VehicleAssembly>().Parts.Any(p=>p.Id==id),Is.False);
        Assert.That(File.ReadAllText(path),Is.EqualTo(original),"Loading does not overwrite the source file.");
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {
        if(File.Exists(path))File.Delete(path);R1RunCheckpointStorage.TestPath=null;
        if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}
    }
}
