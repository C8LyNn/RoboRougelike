using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using RoboRougelike.R3;

public sealed class R3ModelCheckpointTests
{
    [Test] public void TraversalCanCrossAllCellsOfOnePartWithoutCreatingExtraIdentities()
    {
        var core=new R3PartState(Vector3Int.zero,R3PartKind.Core);
        var beam=new R3PartState(Vector3Int.right,R3PartKind.Armor);
        var end=new R3PartState(new Vector3Int(3,0,0),R3PartKind.Armor);
        var cells=new Dictionary<string,Vector3Int[]>{
            {core.Id,new[]{core.Cell}},{beam.Id,new[]{beam.Cell,new Vector3Int(2,0,0)}},{end.Id,new[]{end.Cell}}};
        var parts=new[]{core,beam,end};
        var reached=R3ConnectionTraversal.Reachable(parts,new[]{core.Id},false,p=>cells[p.Id]);
        Assert.That(reached,Is.EquivalentTo(new[]{core.Id,beam.Id,end.Id}),"One two-cell part links its far end to the core.");
        beam.ContactMask=R3MountingGrid.FaceBit(Vector3Int.left);
        reached=R3ConnectionTraversal.Reachable(parts,new[]{core.Id},false,p=>cells[p.Id]);
        Assert.That(reached,Does.Contain(beam.Id));Assert.That(reached,Does.Not.Contain(end.Id),"Unmatched outer ports cannot create a connection.");
    }
    [Test] public void OldStarterContactsAreNormalizedWithoutMutatingTheSavedData()
    {
        var source=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Standard));
        var data=source.MakeCheckpoint(0);
        foreach(var p in data.mounted.Where(p=>p.Kind==R3PartKind.Gun||p.Kind==R3PartKind.Wheel))p.ContactMask=R3MountingGrid.AllFaces;
        var ids=data.mounted.Select(p=>p.Id).ToArray();
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(data,R3VehicleKind.Standard,null,0,out var loaded,out var error),Is.True,error);
        Assert.That(loaded.Parts.Select(p=>p.Id),Is.EquivalentTo(ids));
        Assert.That(loaded.LegacyPortPartsMovedToInventory,Is.Zero);
        foreach(var p in loaded.Parts.Where(p=>p.Kind==R3PartKind.Gun||p.Kind==R3PartKind.Wheel))
            Assert.That(p.ContactMask,Is.EqualTo(R3VehicleModel.PlacementContacts(p.Kind,p)));
        Assert.That(data.mounted.Where(p=>p.Kind==R3PartKind.Gun||p.Kind==R3PartKind.Wheel)
            .All(p=>p.ContactMask==R3MountingGrid.AllFaces),Is.True,"Restoring must not edit the input save.");
    }

    static R3ModelCheckpoint LegacySideMountedGun()
    {
        var data=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Standard)).MakeCheckpoint(0);
        var gun=data.mounted.First(p=>p.Kind==R3PartKind.Gun);
        gun.Cell=new Vector3Int(3,1,1);gun.ContactMask=R3MountingGrid.AllFaces;gun.Health=.43f;
        return data;
    }

    [Test] public void UnsupportedLegacySideGunMovesToInventoryWithoutLosingIdentityOrHealth()
    {
        var data=LegacySideMountedGun();var gun=data.mounted.First(p=>p.Kind==R3PartKind.Gun);string id=gun.Id;
        var originalIds=data.mounted.Concat(data.inventory).Select(p=>p.Id).ToArray();int scrap=data.scrap;
        Assert.That(R3VehicleModel.Connected(data.mounted),Is.True,"The old arbitrary side port was accepted.");
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(data,R3VehicleKind.Standard,null,0,out var loaded,out var error),Is.True,error);
        Assert.That(loaded.LegacyPortPartsMovedToInventory,Is.EqualTo(1));
        Assert.That(loaded.Parts.Any(p=>p.Id==id),Is.False);
        var stored=loaded.Inventory.Single(p=>p.Id==id);
        Assert.That(stored.Health,Is.EqualTo(.43f));
        Assert.That(stored.ContactMask,Is.EqualTo(R3MountingGrid.FaceBit(Vector3Int.down)));
        Assert.That(gun.ContactMask,Is.EqualTo(R3MountingGrid.AllFaces),"Input checkpoint remains unchanged.");
        Assert.That(R3VehicleModel.Connected(loaded.Parts),Is.True);
        Assert.That(loaded.Parts.Concat(loaded.Inventory).Select(p=>p.Id),Is.EquivalentTo(originalIds));
        Assert.That(loaded.Scrap,Is.EqualTo(scrap));
        Assert.That(loaded.Mount(id,new Vector3Int(2,2,2)),Is.True,"The preserved gun can be remounted at a compatible upper socket.");
        var savedAgain=loaded.MakeCheckpoint(0);
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(savedAgain,R3VehicleKind.Standard,null,0,out var reloaded,out var reError),Is.True,reError);
        Assert.That(reloaded.Parts.Single(p=>p.Id==id).Health,Is.EqualTo(.43f));
    }

    [Test] public void FullInventoryBlocksLegacyPortMigrationWithoutChangingTheSave()
    {
        var data=LegacySideMountedGun();var gun=data.mounted.First(p=>p.Kind==R3PartKind.Gun);
        data.inventory=Enumerable.Range(0,R3VehicleModel.InventoryCapacity)
            .Select(_=>new R3PartState(default,R3PartKind.Armor){DefenseRechargeAge=float.MaxValue}).ToArray();
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(data,R3VehicleKind.Standard,null,0,out var loaded,out var error),Is.False);
        Assert.That(loaded,Is.Null);Assert.That(error,Does.Contain("magazyn"));
        Assert.That(data.mounted.Any(p=>p.Id==gun.Id),Is.True);Assert.That(gun.ContactMask,Is.EqualTo(R3MountingGrid.AllFaces));
    }
    [Test] public void FirstDeploymentAfterSaveFillsBatteryAddedInGarage()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Standard));
        var saved=JsonUtility.FromJson<R3ModelCheckpoint>(JsonUtility.ToJson(model.MakeCheckpoint(0)));
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(saved,R3VehicleKind.Standard,model.Economy,0,out var loaded,out var error),Is.True,error);
        loaded.Award(100);
        var cell=loaded.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
            .First(c=>loaded.PlacementError(c,R3PartKind.Battery)==null);
        Assert.That(loaded.Buy(cell,R3PartKind.Battery),Is.True);
        loaded.PrepareInitialEnergy();
        Assert.That(loaded.Energy.Maximum,Is.EqualTo(700).Within(.01f));
        Assert.That(loaded.Energy.Current,Is.EqualTo(700).Within(.01f));
    }
    [Test] public void DepletedAndLegacyCheckpointsDoNotRefillOnDeployment()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Standard));
        model.PrepareInitialEnergy();Assert.That(model.Energy.TrySpend(100),Is.True);
        var saved=JsonUtility.FromJson<R3ModelCheckpoint>(JsonUtility.ToJson(model.MakeCheckpoint(0)));
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(saved,R3VehicleKind.Standard,model.Economy,0,out var loaded,out var error),Is.True,error);
        loaded.PrepareInitialEnergy();Assert.That(loaded.Energy.Current,Is.EqualTo(500).Within(.01f));
        saved.hasInitialEnergyPrepared=false;saved.initialEnergyPrepared=false;
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(saved,R3VehicleKind.Standard,model.Economy,0,out loaded,out error),Is.True,error);
        loaded.PrepareInitialEnergy();Assert.That(loaded.Energy.Current,Is.EqualTo(500).Within(.01f),"Legacy saves must not receive a free refill.");
    }
    [Test] public void JsonRoundTripPreservesOwnedIdsConditionInventoryAndResources()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        model.Award(75);var gun=model.Parts.First(p=>p.Kind==R3PartKind.Gun);
        Assert.That(model.Remove(gun.Cell),Is.True);
        var armor=model.Parts.First(p=>p.Kind==R3PartKind.Armor);armor.Health=.45f;
        var wheel=model.Parts.First(p=>p.Kind==R3PartKind.Wheel);wheel.DeviceReadyAt=120;wheel.ReloadReadyAt=125;
        var json=JsonUtility.ToJson(model.MakeCheckpoint(100));
        var decoded=JsonUtility.FromJson<R3ModelCheckpoint>(json);
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(decoded,R3VehicleKind.Light,model.Economy,10,out var restored,out var error),Is.True,error);
        Assert.That(restored.Scrap,Is.EqualTo(75));
        Assert.That(restored.Parts.Select(p=>p.Id),Is.EquivalentTo(model.Parts.Select(p=>p.Id)));
        Assert.That(restored.Inventory.Select(p=>p.Id),Is.EquivalentTo(model.Inventory.Select(p=>p.Id)));
        Assert.That(restored.Parts.Single(p=>p.Id==armor.Id).Health,Is.EqualTo(.45f));
        var savedWheel=restored.Parts.Single(p=>p.Id==wheel.Id);
        Assert.That(savedWheel.DeviceReadyAt,Is.EqualTo(30));Assert.That(savedWheel.ReloadReadyAt,Is.EqualTo(35));
        Assert.That(restored.Mount(gun.Id,gun.Cell,gun.RotationQuarterTurns),Is.True);
    }
    [Test] public void DuplicateOwnedIdAndInvalidHealthAreRejectedWithoutProducingAModel()
    {
        var source=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        var data=source.MakeCheckpoint(0);data.mounted[1].Id=data.mounted[0].Id;
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(data,R3VehicleKind.Light,null,0,out var restored,out _),Is.False);
        Assert.That(restored,Is.Null);
        data=source.MakeCheckpoint(0);data.mounted[0].Health=float.NaN;
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(data,R3VehicleKind.Light,null,0,out restored,out _),Is.False);
        Assert.That(restored,Is.Null);
        data=source.MakeCheckpoint(0);data.hoverHeight=1e30f;
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(data,R3VehicleKind.Light,null,0,out restored,out _),Is.False);
    }
    [Test] public void GroupPreferenceSurvivesSaveWhileGunIsInInventory()
    {
        var source=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        var gun=source.Parts.First(p=>p.Kind==R3PartKind.Gun);
        source.SwapWeaponGroups();Assert.That(source.WeaponGroupB,Is.EqualTo(gun.Weapon));
        Assert.That(source.Remove(gun.Cell),Is.True);
        var data=JsonUtility.FromJson<R3ModelCheckpoint>(JsonUtility.ToJson(source.MakeCheckpoint(0)));
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(data,R3VehicleKind.Light,null,0,out var restored,out var error),Is.True,error);
        Assert.That(restored.Mount(gun.Id,gun.Cell,gun.RotationQuarterTurns),Is.True);
        Assert.That(restored.WeaponGroupA,Is.Null);Assert.That(restored.WeaponGroupB,Is.EqualTo(gun.Weapon));
    }
}
