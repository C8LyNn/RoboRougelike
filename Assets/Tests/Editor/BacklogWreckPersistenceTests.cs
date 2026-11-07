using System.Linq;
using NUnit.Framework;
using RoboRougelike.R3;
using UnityEngine;

public sealed class BacklogWreckPersistenceTests
{
    [TestCase(0f)]
    [TestCase(.3f)]
    public void RemovingOwnDamagedPartKeepsItsIdentityConditionAndCheckpoint(float health)
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        var part=model.Parts.First(p=>p.Kind==R3PartKind.Gun);
        var id=part.Id;var cell=part.Cell;part.Health=health;part.Ammo=3;
        Assert.That(model.Remove(cell),Is.True);
        var stored=model.Inventory.Single(p=>p.Id==id);
        Assert.That(stored.Health,Is.EqualTo(health));Assert.That(stored.Ammo,Is.EqualTo(3));
        Assert.That(model.At(cell),Is.Null);
        var checkpoint=model.MakeCheckpoint(0);
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(checkpoint,R3VehicleKind.Light,model.Economy,0,out var loaded,out var error),Is.True,error);
        Assert.That(loaded.Inventory.Single(p=>p.Id==id).Health,Is.EqualTo(health));
        loaded.Award(100);Assert.That(loaded.RepairInventory(id),Is.True);
        Assert.That(loaded.Inventory.Single(p=>p.Id==id).Health,Is.EqualTo(1));
        Assert.That(loaded.Mount(id,cell),Is.True);
        Assert.That(loaded.At(cell).Id,Is.EqualTo(id));
    }

    [Test]
    public void FullInventoryDoesNotDiscardWreckOrRemoveCore()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        Assert.That(model.ReceiveSalvage(Enumerable.Range(0,R3VehicleModel.InventoryCapacity)
            .Select(_=>new R3PartState(default,R3PartKind.Wheel))),Is.True);
        var gun=model.Parts.First(p=>p.Kind==R3PartKind.Gun);gun.Health=0;
        Assert.That(model.Remove(gun.Cell),Is.False);
        Assert.That(model.At(gun.Cell),Is.SameAs(gun));
        Assert.That(model.Remove(Vector3Int.zero),Is.False);
        Assert.That(model.Inventory.Count,Is.EqualTo(R3VehicleModel.InventoryCapacity));
    }
}
