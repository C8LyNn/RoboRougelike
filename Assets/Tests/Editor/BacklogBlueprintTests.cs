using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using NUnit.Framework;
using RoboRougelike.R3;
using RoboRougelike.R1;
using RoboRougelike.R4;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class BacklogBlueprintTests
{
    static Type Library=>Type.GetType("RoboRougelike.R3.R3BlueprintLibrary, Assembly-CSharp");
    [Test] public void BlueprintWithLegacyGlobalBindingsRemainsValid()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        var blueprint=R3BlueprintLibrary.Capture(model,"Stary projekt");
        blueprint.globalBindings=blueprint.globalBindings.Take(blueprint.globalBindings.Length-1).ToArray();
        Assert.That(R3BlueprintLibrary.Valid(blueprint),Is.True);
        Assert.That(R3BlueprintLibrary.Preview(blueprint,model).MissingCount,Is.Zero);
    }
    [Test] public void BlueprintKeepsLayoutButDoesNotSerializeItemIdsHealthOrCurrency()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));model.Award(321);
        Assert.That(Library,Is.Not.Null,"A separate blueprint library is required");
        var capture=Library.GetMethod("Capture",BindingFlags.Public|BindingFlags.Static);
        Assert.That(capture,Is.Not.Null);
        var blueprint=capture.Invoke(null,new object[]{model,"Mój pojazd"});Assert.That(blueprint,Is.Not.Null);
        string json=JsonUtility.ToJson(blueprint);
        foreach(var part in model.Parts)Assert.That(json,Does.Not.Contain(part.Id));
        Assert.That(json,Does.Not.Contain("CurrentHp").And.Not.Contain("Health").And.Not.Contain("scrap"));
        Assert.That(json,Does.Contain("Mój pojazd"));
    }
    [Test] public void PreviewShowsMissingPartsWithoutChangingInventoryOrWallet()
    {
        Assert.That(Library,Is.Not.Null,"A separate blueprint library is required");
        var source=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        var blueprint=Library.GetMethod("Capture").Invoke(null,new object[]{source,"Test"});
        var design=new R3VehicleDesign{Name="Pusty",Speed=7,TurnSpeed=80,CoreHp=100};
        design.Parts.Add(new R3PartState(Vector3Int.zero,R3PartKind.Core));
        var target=new R3VehicleModel(design);target.Award(150);
        var result=Library.GetMethod("Preview").Invoke(null,new[]{blueprint,target});
        int missing=(int)result.GetType().GetProperty("MissingCount").GetValue(result);
        Assert.That(missing,Is.GreaterThan(0));Assert.That(target.Parts.Count,Is.EqualTo(1));Assert.That(target.Inventory,Is.Empty);Assert.That(target.Scrap,Is.EqualTo(150));
    }
    [Test] public void OwnedPartsAreUsedOnceAndBrokenPartsAreReported()
    {
        var source=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        var blueprint=R3BlueprintLibrary.Capture(source,"Test");
        var same=R3BlueprintLibrary.Preview(blueprint,source);Assert.That(same.MissingCount,Is.Zero);
        var wheel=source.Parts.First(p=>p.Kind==R3PartKind.Wheel);wheel.Health=0;
        var preview=R3BlueprintLibrary.Preview(blueprint,source);
        Assert.That(preview.MissingCount,Is.EqualTo(1));Assert.That(preview.BrokenCount,Is.EqualTo(1));
        Assert.That(preview.Missing.Single(),Does.Contain("naprawy"));
        Assert.That(source.Parts.Count(p=>p.Id==wheel.Id),Is.EqualTo(1));
    }
    [Test] public void DifferentContactMaskIsNotMistakenForAnAlreadyPlacedPart()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        var blueprint=R3BlueprintLibrary.Capture(model,"Styki");var armor=blueprint.parts.First(p=>p.kind==R3PartKind.Armor);
        armor.contacts=0;
        var plan=R3BlueprintLibrary.Plan(blueprint,model);
        Assert.That(plan.Placements.Single(p=>p.Cell==armor.cell).AlreadyPlaced,Is.False);
    }
    [Test] public void LocalSlotsRoundTripAndRejectBadData()
    {
        string directory=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"rrl-blueprint-"+Guid.NewGuid().ToString("N"));
        try
        {
            var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
            var library=new R3BlueprintLibrary(directory,3);var document=R3BlueprintLibrary.Capture(model,"Projekt A");
            library.Save(1,document);var loaded=library.Load(1);
            Assert.That(loaded.title,Is.EqualTo("Projekt A"));Assert.That(loaded.parts.Length,Is.EqualTo(model.Parts.Count-1));
            Assert.That(R3BlueprintLibrary.Preview(loaded,model).MissingCount,Is.Zero);
            Assert.Throws<ArgumentOutOfRangeException>(()=>library.Load(3));
            loaded.parts[0].cell=Vector3Int.zero;Assert.That(R3BlueprintLibrary.Valid(loaded),Is.False);
            Assert.Throws<System.IO.InvalidDataException>(()=>library.Save(1,loaded));
            Assert.That(library.Load(1).title,Is.EqualTo("Projekt A"),"Rejected writes cannot replace an earlier save");
            System.IO.File.WriteAllText(System.IO.Path.Combine(directory,"slot-2.json"),"{}");
            Assert.Throws<System.IO.InvalidDataException>(()=>library.Load(1),"Corrupt storage is rejected, not partially restored");
        }
        finally
        {
            string file=System.IO.Path.Combine(directory,"slot-2.json");if(System.IO.File.Exists(file))System.IO.File.Delete(file);
            if(System.IO.Directory.Exists(directory))System.IO.Directory.Delete(directory);
        }
    }
}

public sealed class BacklogBlueprintGarageTests
{
    R1RunController run;R1RunHUD hud;string directory;
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        for(int i=0;i<300&&R1RunController.Instance?.Progress==null;i++)yield return null;
        run=R1RunController.Instance;Assert.That(run?.Progress,Is.Not.Null);Assert.That(run.OpenSandboxGarage(),Is.True);
        directory=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"rrl-blueprint-garage-"+Guid.NewGuid().ToString("N"));
        hud=run.GetComponent<R1RunHUD>();typeof(R1RunHUD).GetField("blueprintLibrary",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(hud,new R3BlueprintLibrary(directory));
        Invoke("OpenBlueprintLibrary");
    }
    object Invoke(string name,params object[] args)=>typeof(R1RunHUD).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(hud,args);
    [UnityTest] public IEnumerator GarageSavesPreviewsAndConfirmsOverwriteWithoutChangingOwnedParts()
    {
        var model=run.VehicleModel;int money=model.Scrap;var ids=model.Parts.Concat(model.Inventory).Select(p=>p.Id).OrderBy(x=>x).ToArray();
        Invoke("SaveBlueprintSlot",run,0);var library=new R3BlueprintLibrary(directory);var first=library.Load(0);
        Assert.That(first.parts.Length,Is.EqualTo(model.Parts.Count-1));
        Invoke("PreviewBlueprintSlot",run,0);
        var preview=(R3BlueprintPreview)typeof(R1RunHUD).GetField("blueprintPreview",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(hud);
        Assert.That(preview.MissingCount,Is.Zero);
        var wheel=model.Parts.First(p=>p.Kind==R3PartKind.Wheel&&model.Parts.Any(other=>other!=p));
        Assert.That(run.GarageRemove(wheel.Cell),Is.True);yield return null;
        Invoke("SaveBlueprintSlot",run,0);Assert.That(library.Load(0).parts.Length,Is.EqualTo(first.parts.Length),"First click requests overwrite confirmation");
        Invoke("SaveBlueprintSlot",run,0);Assert.That(library.Load(0).parts.Length,Is.EqualTo(first.parts.Length-1));
        Assert.That(model.Scrap,Is.EqualTo(money));Assert.That(model.Parts.Concat(model.Inventory).Select(p=>p.Id).OrderBy(x=>x),Is.EqualTo(ids));
    }
    [UnityTest] public IEnumerator ApplyingBlueprintRemountsOwnedPartWithoutDuplicatingOrCharging()
    {
        var model=run.VehicleModel;var blueprint=R3BlueprintLibrary.Capture(model,"Powrót");
        var wheel=model.Parts.First(p=>p.Kind==R3PartKind.Wheel);string id=wheel.Id;wheel.Health=.65f;
        var allIds=model.Parts.Concat(model.Inventory).Select(p=>p.Id).OrderBy(x=>x).ToArray();int money=model.Scrap;
        Assert.That(run.GarageRemove(wheel.Cell),Is.True);yield return null;
        var apply=typeof(R1RunController).GetMethod("StartApplyBlueprint",BindingFlags.Public|BindingFlags.Instance);
        Assert.That(apply,Is.Not.Null,"Restoration must be available in the actual garage");
        Assert.That((bool)apply.Invoke(run,new object[]{blueprint}),Is.True);
        float deadline=Time.realtimeSinceStartup+30;while(run.Transitioning&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(run.Transitioning,Is.False,"Blueprint operation must finish");
        Assert.That(run.VehicleModel.Parts.Single(p=>p.Id==id).Health,Is.EqualTo(.65f).Within(.001));
        var physical=run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==id).Damageable;
        Assert.That(physical.CurrentHp/physical.MaxHp,Is.EqualTo(.65f).Within(.001));
        Assert.That(run.VehicleModel.Parts.Concat(run.VehicleModel.Inventory).Select(p=>p.Id).OrderBy(x=>x),Is.EqualTo(allIds));
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(money));
    }
    [UnityTest] public IEnumerator MissingPartsRefuseToStartWithoutChangingTheVehicle()
    {
        var model=run.VehicleModel;var blueprint=R3BlueprintLibrary.Capture(model,"Niekompletny");
        blueprint.parts=blueprint.parts.Concat(new[]{new R3BlueprintPart{cell=new Vector3Int(6,4,6),kind=R3PartKind.ShieldTurretGenerator,tier=1,contacts=R3MountingGrid.AllFaces}}).ToArray();
        int money=model.Scrap;var ids=model.Parts.Concat(model.Inventory).Select(p=>p.Id).OrderBy(x=>x).ToArray();
        Assert.That(run.StartApplyBlueprint(blueprint),Is.False);Assert.That(run.Transitioning,Is.False);
        Assert.That(run.BlueprintApplyMessage,Does.Contain("Brakuje"));Assert.That(run.VehicleModel,Is.SameAs(model));
        Assert.That(model.Scrap,Is.EqualTo(money));Assert.That(model.Parts.Concat(model.Inventory).Select(p=>p.Id).OrderBy(x=>x),Is.EqualTo(ids));yield return null;
    }
    [UnityTest] public IEnumerator PhysicalObstructionRollsBackTheWholeBlueprint()
    {
        var model=run.VehicleModel;var blueprint=R3BlueprintLibrary.Capture(model,"Przeszkoda");
        var wheel=model.Parts.First(p=>p.Kind==R3PartKind.Wheel);string id=wheel.Id;var cell=wheel.Cell;
        Assert.That(run.GarageRemove(cell),Is.True);yield return null;
        var player=run.Player;var ids=model.Parts.Concat(model.Inventory).Select(p=>p.Id).OrderBy(x=>x).ToArray();int money=model.Scrap;
        var obstruction=GameObject.CreatePrimitive(PrimitiveType.Cube);obstruction.transform.position=player.transform.TransformPoint((Vector3)cell*.55f);
        obstruction.transform.localScale=Vector3.one*.8f;Physics.SyncTransforms();
        Assert.That(run.StartApplyBlueprint(blueprint),Is.True);
        float deadline=Time.realtimeSinceStartup+30;while(run.Transitioning&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(run.Transitioning,Is.False);Assert.That(run.BlueprintApplyMessage,Does.Contain("Nie można"));
        Assert.That(run.VehicleModel,Is.SameAs(model));Assert.That(run.Player,Is.SameAs(player));Assert.That(player.gameObject.activeInHierarchy,Is.True);
        Assert.That(model.Inventory.Any(p=>p.Id==id),Is.True);Assert.That(model.Scrap,Is.EqualTo(money));
        Assert.That(model.Parts.Concat(model.Inventory).Select(p=>p.Id).OrderBy(x=>x),Is.EqualTo(ids));UnityEngine.Object.Destroy(obstruction);yield return null;
    }
    [UnityTest] public IEnumerator FullInventoryRejectsRebuildWithoutLosingOrMovingParts()
    {
        var model=run.VehicleModel;var blueprint=R3BlueprintLibrary.Capture(model,"Magazyn");
        var wheel=model.Parts.First(p=>p.Kind==R3PartKind.Wheel);string id=wheel.Id;var old=wheel.Cell;var rotation=wheel.RotationQuarterTurns;
        Assert.That(run.GarageRemove(old),Is.True);yield return null;
        var cells=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(n=>p.Cell+n)).Distinct();
        var choices=cells.Where(c=>c!=old&&model.OwnedPlacementError(id,c,rotation)==null).ToArray();
        Assert.That(choices,Is.Not.Empty);Assert.That(run.GarageMount(id,choices[0],rotation),Is.True);yield return null;
        var filler=Enumerable.Range(0,model.AvailableInventoryCapacity-model.Inventory.Count)
            .Select(_=>new R3PartState(default,R3PartKind.Battery)).ToArray();Assert.That(model.ReceiveSalvage(filler),Is.True);
        var ids=model.Parts.Concat(model.Inventory).Select(p=>p.Id).OrderBy(x=>x).ToArray();int money=model.Scrap;
        Assert.That(run.StartApplyBlueprint(blueprint),Is.True);
        float deadline=Time.realtimeSinceStartup+40;while(run.Transitioning&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(run.Transitioning,Is.False);Assert.That(run.BlueprintApplyMessage,Does.Contain("magazynie"));
        Assert.That(run.VehicleModel,Is.SameAs(model));Assert.That(model.Parts.Single(p=>p.Id==id).Cell,Is.EqualTo(choices[0]));
        Assert.That(model.Scrap,Is.EqualTo(money));Assert.That(model.Parts.Concat(model.Inventory).Select(p=>p.Id).OrderBy(x=>x),Is.EqualTo(ids));
    }
    [UnityTest] public IEnumerator ApplyingRestoresRecordedWeaponSlotWithoutChangingGlobalKeys()
    {
        var model=run.VehicleModel;var weapon=model.GetWeaponSlots().First(x=>x.HasValue).Value;
        Assert.That(model.AssignWeaponSlot(weapon,3),Is.True);var blueprint=R3BlueprintLibrary.Capture(model,"Sloty");
        Assert.That(model.AssignWeaponSlot(weapon,0),Is.True);
        Assert.That(run.StartApplyBlueprint(blueprint),Is.True);
        float deadline=Time.realtimeSinceStartup+30;while(run.Transitioning&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(run.Transitioning,Is.False);Assert.That(run.VehicleModel.GetWeaponSlots()[3],Is.EqualTo(weapon));
    }
    [UnityTest] public IEnumerator BlueprintRestoresGlobalControlsOnlyWhenExplicitlyRequested()
    {
        var profile=R1ControlProfile.Current;var original=profile.Snapshot();
        try
        {
            profile.ResetDefaults();var blueprint=R3BlueprintLibrary.Capture(run.VehicleModel,"Klawisze");
            Assert.That(profile.TryBind("MoveForward","K"),Is.True);
            Assert.That(run.StartApplyBlueprint(blueprint),Is.True);
            float deadline=Time.realtimeSinceStartup+30;while(run.Transitioning&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(run.BlueprintApplyMessage,Does.Contain("Odtworzono"));Assert.That(profile.Binding("MoveForward"),Is.EqualTo("K"));
            Assert.That(run.StartApplyBlueprintWithControls(blueprint),Is.True);
            deadline=Time.realtimeSinceStartup+30;while(run.Transitioning&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(run.BlueprintApplyMessage,Does.Contain("Odtworzono"));Assert.That(profile.Binding("MoveForward"),Is.EqualTo("W"));
        }
        finally{profile.TryRestoreSnapshot(original,out _);}
    }
    [UnityTest] public IEnumerator MovingBatteryDuringRestorationDoesNotSpendStoredEnergyOrCharges()
    {
        var model=run.VehicleModel;var blueprint=R3BlueprintLibrary.Capture(model,"Energia");
        var battery=model.Parts.First(p=>p.Kind==R3PartKind.Battery);string id=battery.Id;var old=battery.Cell;
        Assert.That(run.GarageRemove(old),Is.True);yield return null;
        var cells=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(n=>p.Cell+n)).Distinct();
        var choices=cells.Where(c=>c!=old&&model.OwnedPlacementError(id,c,default)==null).ToArray();
        Assert.That(choices,Is.Not.Empty);Assert.That(run.GarageMount(id,choices[0],default),Is.True);yield return null;
        model.Energy.Fill();float energy=model.Energy.Current;
        Assert.That(model.Charges.EnsurePool("blueprint-test",3),Is.True);Assert.That(model.Charges.TrySpend("blueprint-test",1),Is.True);
        Assert.That(run.StartApplyBlueprint(blueprint),Is.True);
        float deadline=Time.realtimeSinceStartup+40;while(run.Transitioning&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(run.Transitioning,Is.False);Assert.That(run.BlueprintApplyMessage,Does.Contain("Odtworzono"));
        Assert.That(run.VehicleModel.Parts.Single(p=>p.Id==id).Cell,Is.EqualTo(old));
        Assert.That(run.VehicleModel.Energy.Current,Is.EqualTo(energy).Within(.001));
        Assert.That(run.VehicleModel.Charges.Current("blueprint-test"),Is.EqualTo(2));
    }
    [UnityTest] public IEnumerator RestoredWeaponKeepsItsAmmunitionAndReloadDeadline()
    {
        var model=run.VehicleModel;model.Award(1000);
        Assert.That(model.PurchaseToInventory(R3PartKind.Gun,R4WeaponKind.Shotgun),Is.True);
        var shotgun=model.Inventory.Single(p=>p.Kind==R3PartKind.Gun&&p.Weapon==R4WeaponKind.Shotgun);string id=shotgun.Id;
        var cells=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(n=>p.Cell+n)).Distinct();
        var choices=cells.Where(c=>model.OwnedPlacementError(id,c,default)==null).ToArray();Assert.That(choices,Is.Not.Empty);
        Assert.That(run.GarageMount(id,choices[0],default),Is.True);yield return null;
        shotgun=model.Parts.Single(p=>p.Id==id);shotgun.Ammo=2;shotgun.ReloadReadyAt=Time.time+50;shotgun.DeviceReadyAt=Time.time+70;
        float reload=shotgun.ReloadReadyAt,ready=shotgun.DeviceReadyAt;
        var blueprint=R3BlueprintLibrary.Capture(model,"Magazynek");Assert.That(run.GarageRemove(shotgun.Cell),Is.True);yield return null;
        Assert.That(run.StartApplyBlueprint(blueprint),Is.True);
        float deadline=Time.realtimeSinceStartup+40;while(run.Transitioning&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(run.Transitioning,Is.False);Assert.That(run.BlueprintApplyMessage,Does.Contain("Odtworzono"));
        var restored=run.VehicleModel.Parts.Single(p=>p.Id==id);
        Assert.That(restored.Ammo,Is.EqualTo(2));Assert.That(restored.ReloadReadyAt,Is.EqualTo(reload).Within(.001));Assert.That(restored.DeviceReadyAt,Is.EqualTo(ready).Within(.001));
        var binding=run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==id).GetComponent<RoboRougelike.WeaponIntegration.RRLShotgunBinding>();
        Assert.That(binding.State.Ammo,Is.EqualTo(2));
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {
        if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}
        if(!string.IsNullOrEmpty(directory))
        {
            string file=System.IO.Path.Combine(directory,"slot-1.json");if(System.IO.File.Exists(file))System.IO.File.Delete(file);
            if(System.IO.Directory.Exists(directory))System.IO.Directory.Delete(directory);
        }
    }
}
