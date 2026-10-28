using System.Linq;
using NUnit.Framework;
using UnityEngine;
using RoboRougelike.R3;

public sealed class R3ModelCheckpointTests
{
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
