using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R1;
using RoboRougelike.R3;

public sealed class GarageEditFreedomTests
{
    [Test] public void ModelCanRemoveLastGunAndEveryWheelButCannotDeploy()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Standard));
        var gun=model.Parts.First(p=>p.Kind==R3PartKind.Gun);
        Assert.That(model.Remove(gun.Cell),Is.True,"removing the last gun is a valid garage edit");
        Assert.That(model.CanDeploy,Is.False);Assert.That(model.DeployError,Does.Contain("broni"));
        foreach(var wheel in model.Parts.Where(p=>p.Kind==R3PartKind.Wheel).ToArray())
            Assert.That(model.Remove(wheel.Cell),Is.True,"every wheel can be removed");
        Assert.That(model.Parts.Any(p=>p.Kind==R3PartKind.Wheel),Is.False);
        Assert.That(model.CanDeploy,Is.False);Assert.That(model.Remove(Vector3Int.zero),Is.False,"core stays protected");
        Assert.That(model.Inventory.Select(p=>p.Id).Distinct().Count(),Is.EqualTo(model.Inventory.Count));
    }
    [Test] public void InventoryFullCannotDiscardOrDuplicateRemovedPart()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        var stored=Enumerable.Range(0,R3VehicleModel.InventoryCapacity).Select(i=>new R3PartState(Vector3Int.zero,R3PartKind.Wheel)).ToArray();
        Assert.That(model.ReceiveSalvage(stored),Is.True);
        var gun=model.Parts.First(p=>p.Kind==R3PartKind.Gun);var id=gun.Id;
        Assert.That(model.Remove(gun.Cell),Is.False);Assert.That(model.At(gun.Cell).Id,Is.EqualTo(id));
        Assert.That(model.Inventory.Count,Is.EqualTo(R3VehicleModel.InventoryCapacity));
        Assert.That(model.Parts.Concat(model.Inventory).Select(p=>p.Id).Distinct().Count(),Is.EqualTo(model.Parts.Count+model.Inventory.Count));
    }
    [TestCase(R3DriveKind.Tracks,R3PartKind.CrawlerTrack)]
    [TestCase(R3DriveKind.Omni,R3PartKind.OmniDrive)]
    [TestCase(R3DriveKind.Hover,R3PartKind.HoverEngine)]
    public void CanRemoveWheelsThenInstallAndReplaceSelectedDrive(R3DriveKind drive,R3PartKind kind)
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Standard));model.Award(1000);
        foreach(var wheel in model.Parts.Where(p=>p.Kind==R3PartKind.Wheel).ToArray())Assert.That(model.Remove(wheel.Cell),Is.True);
        Assert.That(model.CanDeploy,Is.False);
        var cells=drive==R3DriveKind.Tracks?new[]{new Vector3Int(-1,2,1),new Vector3Int(1,2,1)}:new[]{new Vector3Int(1,2,1)};
        foreach(var cell in cells)Assert.That(model.Buy(cell,kind),Is.True,cell.ToString());
        model.SelectedDrive=drive;Assert.That(model.CanDeploy,Is.True);
        var devices=model.Parts.Where(p=>p.Kind==kind).ToArray();int scrap=model.Scrap;
        foreach(var device in devices)Assert.That(model.Remove(device.Cell),Is.True);
        Assert.That(model.CanDeploy,Is.False);Assert.That(model.DeepCopy().CanDeploy,Is.False);
        foreach(var device in devices)Assert.That(model.Mount(device.Id,device.Cell,device.RotationQuarterTurns),Is.True);
        Assert.That(model.CanDeploy,Is.True);Assert.That(model.Scrap,Is.EqualTo(scrap),"remounting owned parts is free");
    }
    [Test] public void DestroyedGunOrOnlyOneWheelSideCannotDeploy()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        model.Parts.First(p=>p.Kind==R3PartKind.Gun).Health=0;Assert.That(model.CanDeploy,Is.False);
        model.Parts.First(p=>p.Kind==R3PartKind.Gun).Health=1;
        foreach(var wheel in model.Parts.Where(p=>p.Kind==R3PartKind.Wheel&&p.Cell.x<0))wheel.Health=0;
        Assert.That(model.CanDeploy,Is.False);Assert.That(model.DeployError,Does.Contain("koło"));
    }
    [UnityTest] public IEnumerator ActualMissionEntryRejectsIncompleteVehicleAndAcceptsRestoredGun()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity",OpenSceneMode.Single);yield return new EnterPlayMode();
        var until=Time.realtimeSinceStartup+15;while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<until)yield return null;
        var run=R1RunController.Instance;Assert.That(run.PrepareUnifiedRun(),Is.True);
        var gun=run.VehicleModel.Parts.First(p=>p.Kind==R3PartKind.Gun);Assert.That(run.GarageRemove(gun.Cell),Is.True);
        Assert.That(run.StartNextEncounter(),Is.False);Assert.That(run.CanChooseReactorDefense,Is.False);
        Assert.That(run.GarageMount(gun.Id,gun.Cell,gun.RotationQuarterTurns),Is.True);
        Assert.That(run.VehicleModel.CanDeploy,Is.True);Assert.That(run.StartNextEncounter(),Is.True);
        yield return new ExitPlayMode();
    }
    [UnityTest] public IEnumerator MapEditorCanBeEnteredWithIncompleteVehicleButTestIsBlocked()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity",OpenSceneMode.Single);yield return new EnterPlayMode();
        var until=Time.realtimeSinceStartup+15;while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<until)yield return null;
        var run=R1RunController.Instance;Assert.That(run.OpenMapEditor(),Is.True);run.MapEditorGarage();
        var gun=run.VehicleModel.Parts.First(p=>p.Kind==R3PartKind.Gun);Assert.That(run.GarageRemove(gun.Cell),Is.True);
        Assert.That(run.ReturnToMapEditorFromGarage(),Is.True);Assert.That(run.MapEditing,Is.True);
        run.ToggleMapFloor();
        Assert.That(run.EditorDraft.Place(RoboRougelike.MapEditor.MapItemKind.Spawn,0,-3,0),Is.True);
        Assert.That(run.EditorDraft.Place(RoboRougelike.MapEditor.MapItemKind.Finish,0,3,0),Is.True);
        Assert.That(run.TestEditedMap(),Is.False);Assert.That(run.MapEditing,Is.True);
        Assert.That(run.EditorMessage,Does.Contain("broni"));
        yield return new ExitPlayMode();
    }
    [UnityTest] public IEnumerator ActualGarageCanSwapWheelsForOmniAndBackWithoutTemporaryParts()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity",OpenSceneMode.Single);yield return new EnterPlayMode();
        var until=Time.realtimeSinceStartup+15;while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<until)yield return null;
        var run=R1RunController.Instance;Assert.That(run.SelectVehicle(R3VehicleKind.Light),Is.True);Assert.That(run.OpenSandboxGarage(),Is.True);
        var wheels=run.VehicleModel.Parts.Where(p=>p.Kind==R3PartKind.Wheel).ToArray();
        foreach(var wheel in wheels)Assert.That(run.GarageRemove(wheel.Cell),Is.True);
        Assert.That(run.VehicleModel.CanDeploy,Is.False);Assert.That(run.EnterTestRange(),Is.False);
        var cell=new Vector3Int(0,0,3);Assert.That(run.GarageBuy(cell,R3PartKind.OmniDrive),Is.True,"real physical mounting gate: "+run.VehicleModel.PlacementError(cell,R3PartKind.OmniDrive));
        Assert.That(run.ConfigureDrive(R3DriveKind.Omni),Is.True);Assert.That(run.VehicleModel.CanDeploy,Is.True);
        Assert.That(run.GarageRemove(cell),Is.True);Assert.That(run.VehicleModel.CanDeploy,Is.False);
        foreach(var wheel in wheels)Assert.That(run.GarageMount(wheel.Id,wheel.Cell,wheel.RotationQuarterTurns),Is.True);
        Assert.That(run.ConfigureDrive(R3DriveKind.Wheels),Is.True);Assert.That(run.VehicleModel.CanDeploy,Is.True);
        Assert.That(run.EnterTestRange(),Is.True);yield return null;yield return new ExitPlayMode();
    }
    [UnityTest] public IEnumerator RealGarageRebuildsAfterLastGunAndEveryWheelAreRemoved()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity",OpenSceneMode.Single);
        yield return new EnterPlayMode();
        var until=Time.realtimeSinceStartup+15;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<until)yield return null;
        var run=R1RunController.Instance;Assert.That(run,Is.Not.Null);Assert.That(run.OpenSandboxGarage(),Is.True);
        var gun=run.VehicleModel.Parts.First(p=>p.Kind==R3PartKind.Gun);
        var wheels=run.VehicleModel.Parts.Where(p=>p.Kind==R3PartKind.Wheel).ToArray();int scrap=run.VehicleModel.Scrap;
        Assert.That(run.GarageRemove(gun.Cell),Is.True,"last gun removed with live garage rebuild");
        Assert.That(run.Player,Is.Not.Null);Assert.That(run.VehicleModel.CanDeploy,Is.False);
        foreach(var wheel in wheels)
            Assert.That(run.GarageRemove(wheel.Cell),Is.True,"wheel removed with live garage rebuild");
        Assert.That(run.Player,Is.Not.Null);Assert.That(run.EnterTestRange(),Is.False);
        Assert.That(run.VehicleModel.Parts.Any(p=>p.Kind==R3PartKind.Gun||p.Kind==R3PartKind.Wheel),Is.False);
        R1EncounterManager.ClearTransientObjects();run.Player.GetComponent<R3VehicleAssembly>().FireGuns();
        Assert.That(Object.FindObjectsByType<RoboRougelike.R4.R4ProjectileOrigin>(FindObjectsSortMode.None),Is.Empty,"no phantom gun");
        Assert.That(run.GarageMount(gun.Id,gun.Cell,gun.RotationQuarterTurns),Is.True,"remount owned gun");
        foreach(var wheel in wheels)Assert.That(run.GarageMount(wheel.Id,wheel.Cell,wheel.RotationQuarterTurns),Is.True,"remount owned wheel "+wheel.Cell);
        Assert.That(run.VehicleModel.CanDeploy,Is.True);Assert.That(run.VehicleModel.Scrap,Is.EqualTo(scrap));
        Assert.That(run.EnterTestRange(),Is.True);yield return null;
        yield return new ExitPlayMode();
    }
}
