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
    // W32: paid-zero is a legitimate V7-priced transaction, NOT a free
    // victory rebuild. Even a zero quote must require a real owned wreck.
    [Test] public void OwnedMountedArmorAtPurchaseOneRepairsForPaidZeroAndKeepsGuid()
    {
        var design=new R3VehicleDesign();
        design.Parts.Add(new R3PartState(Vector3Int.zero,R3PartKind.Core));
        design.Parts.Add(new R3PartState(new Vector3Int(0,1,0),R3PartKind.Armor,0f));
        var model=new R3VehicleModel(design,new R3GarageEconomy{ArmorCost=1});
        var damaged=model.Parts.Single(p=>p.Kind==R3PartKind.Armor);
        string id=damaged.Id;
        int beforeParts=model.Parts.Count;
        Assert.That(model.Scrap,Is.Zero);
        Assert.That(model.RepairCost(damaged),Is.Zero);
        Assert.That(model.Repair(damaged.Cell),Is.True,
            "Owned connected 0HP Armor at price 1 is a PAID zero-Scrap rebuild.");
        Assert.That(model.Scrap,Is.Zero);
        Assert.That(damaged.Health,Is.EqualTo(1f));
        Assert.That(model.Parts.Single(p=>p.Id==id),Is.SameAs(damaged));
        Assert.That(model.Parts.Count,Is.EqualTo(beforeParts));
        Assert.That(model.Repair(damaged.Cell),Is.False,
            "Fully healthy part must not be repeatedly repaired at quote zero.");
    }

    [Test] public void EmergencyRepairAcceptsNonemptyZeroCostV7PlanWithoutMintingResources()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light),
            new R3GarageEconomy{WheelCost=1});
        var leftWheels=model.Parts.Where(p=>p.Kind==R3PartKind.Wheel&&p.Cell.x<0).ToArray();
        Assert.That(leftWheels.Length,Is.GreaterThan(0));
        foreach(var wheel in leftWheels)wheel.Health=0f;
        Assert.That(model.CanDeploy,Is.False);
        Assert.That(model.EmergencyRepairCost,Is.Zero);
        var originalIds=model.Parts.Select(p=>p.Id).OrderBy(x=>x).ToArray();
        int originalCount=model.Parts.Count;
        Assert.That(model.EmergencyRepair(),Is.True,
            "Nonempty genuine emergency restoration plan with zero quote is a PAID action.");
        Assert.That(model.Scrap,Is.Zero);
        Assert.That(model.CanDeploy,Is.True);
        Assert.That(model.Parts.Count,Is.EqualTo(originalCount));
        Assert.That(model.Parts.Select(p=>p.Id).OrderBy(x=>x),Is.EqualTo(originalIds));
        Assert.That(model.EmergencyRepair(),Is.False,
            "No accidental repeated zero-cost repair of an already deployable rig.");
    }

    [Test] public void AboveZeroHealthRepairKeepsOriginalPartialDamageQuote()
    {
        var model=new R3VehicleModel(new R3VehicleDesign(),
            new R3GarageEconomy{ArmorCost=20});
        var part=new R3PartState(Vector3Int.zero,R3PartKind.Armor,.5f);
        Assert.That(model.RepairCost(part),Is.EqualTo(4),
            "Only destroyed 0HP price changes to V7 65%-floor; partial repairs keep 35%-ceil.");
        part.Health=1f;
        Assert.That(model.RepairCost(part),Is.Zero);
        part.Health=float.NaN;
        Assert.That(model.RepairCost(part),Is.Zero);
    }

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

    [UnityTest] public IEnumerator RealGarageRejectsOwnedSawDiscPenetrationBeforeInventoryMutation()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity",OpenSceneMode.Single);
        yield return new EnterPlayMode();
        var until=Time.realtimeSinceStartup+15;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<until)
            yield return null;
        var run=R1RunController.Instance;
        Assert.That(run,Is.Not.Null);
        Assert.That(run.SelectVehicle(R3VehicleKind.Standard),Is.True);
        Assert.That(run.OpenSandboxGarage(),Is.True);
        var gun=run.VehicleModel.Parts.First(p=>p.Kind==R3PartKind.Gun);
        var id=gun.Id;
        var oldCell=gun.Cell;
        Assert.That(run.GarageRemove(oldCell),Is.True);
        var inventory=run.VehicleModel.Inventory.Single(p=>p.Id==id);
        inventory.Weapon=RoboRougelike.R4.R4WeaponKind.RotarySaw;
        int beforeWallet=run.VehicleModel.Scrap;
        int beforeParts=run.VehicleModel.Parts.Count;
        var issue=run.VehicleModel.OwnedPlacementError(id,oldCell,default);
        Assert.That(issue,Does.Contain("tarcza piły"),
            "Real central stock mount intersects own armor and must not be green.");
        Assert.That(run.GarageMount(id,oldCell,default),Is.False);
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(beforeWallet));
        Assert.That(run.VehicleModel.Parts.Count,Is.EqualTo(beforeParts));
        Assert.That(run.VehicleModel.Inventory.Single(p=>p.Id==id).Id,Is.EqualTo(id));
        // Exterior attachment remains available, never disable the saw globally.
        var exterior=new Vector3Int(0,1,4);
        // Real physical 260-collider stress for the 256-hit fail-closed query.
        var realPlayer=run.Player.transform;
        var blade=realPlayer.TransformPoint((Vector3)exterior*.55f+
            Vector3.up*(.6f*.52f)+Vector3.forward*(1.65f*.52f));
        var saturation=new GameObject[260];
        for(int i=0;i<saturation.Length;i++)
        {
            var blocker=new GameObject("W30 owner query saturation "+i);
            blocker.transform.SetParent(realPlayer,false);
            blocker.transform.position=blade;
            blocker.AddComponent<BoxCollider>().size=Vector3.one*.02f;
            saturation[i]=blocker;
        }
        Physics.SyncTransforms();
        var saturated=run.VehicleModel.OwnedPlacementError(id,exterior,default);
        Assert.That(saturated,Does.Contain("przepełnienie"),
            "Saturated owner sweep must fail closed, not silently truncate.");
        Assert.That(run.GarageMount(id,exterior,default),Is.False);
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(beforeWallet));
        Assert.That(run.VehicleModel.Inventory.Single(p=>p.Id==id).Id,Is.EqualTo(id));
        foreach(var blocker in saturation){blocker.SetActive(false);Object.Destroy(blocker);}
        Physics.SyncTransforms();
        Assert.That(run.VehicleModel.OwnedPlacementError(id,exterior,default),Is.Null,
            "Connected exterior physical saw mount must be possible: "+
                run.VehicleModel.OwnedPlacementError(id,exterior,default));
        Assert.That(run.GarageMount(id,exterior,default),Is.True);
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(beforeWallet));
        Assert.That(run.VehicleModel.Parts.Single(p=>p.Id==id).Cell,Is.EqualTo(exterior));
        yield return new ExitPlayMode();
    }
    [UnityTest] public IEnumerator NewArmorCannotBlockExistingSawAfterGaragePurchase()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity",OpenSceneMode.Single);
        yield return new EnterPlayMode();
        float deadline=Time.realtimeSinceStartup+15f;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<deadline)
            yield return null;
        var run=R1RunController.Instance;
        Assert.That(run,Is.Not.Null);
        Assert.That(run.SelectVehicle(R3VehicleKind.Light),Is.True);
        Assert.That(run.OpenSandboxGarage(),Is.True);
        run.VehicleModel.Award(100); // Fixture funds only; not a simulated victory.

        var oldGun=run.VehicleModel.Parts.First(p=>p.Kind==R3PartKind.Gun);
        string sawId=oldGun.Id;
        Assert.That(run.GarageRemove(oldGun.Cell),Is.True);
        // The original right-front wheel's protruding tire overlaps the new
        // side-mount cell. Remove it legitimately; do not disable any Collider.
        var wheel=run.VehicleModel.Parts.First(p=>p.Kind==R3PartKind.Wheel&&
            p.Cell==new Vector3Int(2,0,1));
        Assert.That(run.GarageRemove(wheel.Cell),Is.True);
        var owned=run.VehicleModel.Inventory.Single(p=>p.Id==sawId);
        owned.Weapon=RoboRougelike.R4.R4WeaponKind.RotarySaw;
        var sawCell=new Vector3Int(2,0,2);
        Assert.That(run.VehicleModel.OwnedPlacementError(sawId,sawCell,default),Is.Null,
            "Actual exterior saw must be legal, or this native fixture needs correction.");
        Assert.That(run.GarageMount(sawId,sawCell,default),Is.True);
        // Physical side support is outside the blade's thin X-axis disc,
        // but the next cell above the gun intersects the rotating disc.
        var support=new Vector3Int(1,1,2);
        Assert.That(run.VehicleModel.PlacementError(support,R3PartKind.Armor),Is.Null);
        Assert.That(run.GarageBuy(support,R3PartKind.Armor),Is.True);
        var blocked=new Vector3Int(2,1,2);
        int wallet=run.VehicleModel.Scrap;
        var ids=run.VehicleModel.Parts.Concat(run.VehicleModel.Inventory)
            .Select(p=>p.Id).OrderBy(id=>id).ToArray();
        int partsBefore=run.VehicleModel.Parts.Count;
        var error=run.VehicleModel.PlacementError(blocked,R3PartKind.Armor);
        Assert.That(error,Does.Contain("nowa część blokuje tarczę"),
            "Valid grid attachment must be rejected because it disables the already-mounted saw.");
        Assert.That(run.GarageBuy(blocked,R3PartKind.Armor),Is.False,
            "Real Buy must use precisely the same live functional gate as the preview.");
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(wallet));
        Assert.That(run.VehicleModel.Parts.Count,Is.EqualTo(partsBefore));
        Assert.That(run.VehicleModel.Parts.Concat(run.VehicleModel.Inventory)
            .Select(p=>p.Id).OrderBy(id=>id),Is.EqualTo(ids),
            "Failed Buy must not mutate ownership, GUIDs or inventory.");
        // Neighboring armor outside the same disc is still buildable.
        var clear=new Vector3Int(1,1,3);
        Assert.That(run.VehicleModel.PlacementError(clear,R3PartKind.Armor),Is.Null,
            "Reverse physical clearance cannot forbid the whole garage.");
        Assert.That(run.GarageBuy(clear,R3PartKind.Armor),Is.True);
        Assert.That(run.VehicleModel.Scrap,
            Is.EqualTo(wallet-run.VehicleModel.Price(R3PartKind.Armor)),
            "Successful legal purchase must charge exactly once.");
        yield return new ExitPlayMode();
    }

    [UnityTest] public IEnumerator ExistingDeadLegacySawCannotRepairOrEmergencyRepair()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity",OpenSceneMode.Single);
        yield return new EnterPlayMode();
        var until=Time.realtimeSinceStartup+15;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<until)
            yield return null;
        var run=R1RunController.Instance;
        Assert.That(run,Is.Not.Null);
        Assert.That(run.SelectVehicle(R3VehicleKind.Standard),Is.True);
        Assert.That(run.OpenSandboxGarage(),Is.True);
        var model=run.VehicleModel;
        var gun=model.Parts.First(p=>p.Kind==R3PartKind.Gun);
        string id=gun.Id;
        // Physically consistent legacy/stale damaged mount fixture: the old
        // authorizer could previously mount a centered saw without disc safety.
        gun.Weapon=RoboRougelike.R4.R4WeaponKind.RotarySaw;
        gun.Health=0f;
        var oldMounted=run.Player.GetComponent<R3VehicleAssembly>().Parts.First(p=>p.Id==id);
        oldMounted.gameObject.SetActive(false);
        model.Award(1000);
        Physics.SyncTransforms();
        Assert.That(model.RepairCost(gun),Is.GreaterThan(0));
        Assert.That(model.EmergencyRepairCost,Is.GreaterThan(0),
            "EmergencyRepair needs an actual affordable essential recovery plan.");
        int scrap=model.Scrap,parts=model.Parts.Count,stock=model.Inventory.Count;
        Assert.That(model.Repair(gun.Cell),Is.False,
            "Repair must refuse already installed legacy saw that intersects owned armor.");
        Assert.That(model.EmergencyRepair(),Is.False,
            "EmergencyRepair must use the same true physical functional restoration gate.");
        Assert.That(model.Scrap,Is.EqualTo(scrap));
        Assert.That(gun.Health,Is.Zero);
        Assert.That(gun.Id,Is.EqualTo(id));
        Assert.That(model.Parts.Count,Is.EqualTo(parts));
        Assert.That(model.Inventory.Count,Is.EqualTo(stock));
        yield return new ExitPlayMode();
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
