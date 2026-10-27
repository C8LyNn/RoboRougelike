using NUnit.Framework;
using UnityEngine;
using RoboRougelike.R3;

public class R3GarageModelTests
{
    [Test] public void ThreeBlueprintsHaveDistinctConnectedGeometryAndMobility()
    {
        var standard=R3VehicleCatalog.Create(R3VehicleKind.Standard);
        var light=R3VehicleCatalog.Create(R3VehicleKind.Light);
        var heavy=R3VehicleCatalog.Create(R3VehicleKind.Heavy);
        Assert.That(light.Parts.Count,Is.LessThan(standard.Parts.Count));
        Assert.That(heavy.Parts.Count,Is.GreaterThan(standard.Parts.Count));
        Assert.That(light.Speed,Is.GreaterThan(standard.Speed));Assert.That(heavy.Speed,Is.LessThan(standard.Speed));
        foreach(var design in new[]{standard,light,heavy})
        { Assert.That(R3VehicleModel.Connected(design.Parts),Is.True);Assert.That(new R3VehicleModel(design).CanDeploy,Is.True); }
    }
    private static R3VehicleDesign Blueprint()
    {
        var design = new R3VehicleDesign { Name = "Fixture", Speed = 7.5f, TurnSpeed = 82 };
        design.Parts.Add(new R3PartState(Vector3Int.zero, R3PartKind.Core));
        design.Parts.Add(new R3PartState(Vector3Int.up, R3PartKind.Gun));
        design.Parts.Add(new R3PartState(Vector3Int.left, R3PartKind.Wheel));
        design.Parts.Add(new R3PartState(Vector3Int.right, R3PartKind.Wheel));
        return design;
    }
    [Test] public void RunDamageDoesNotMutateIntactBlueprint()
    {
        var blueprint = Blueprint(); var model = new R3VehicleModel(blueprint);
        model.Parts[0].Health = 0.3f;
        Assert.That(blueprint.Parts[0].Health, Is.EqualTo(1));
        Assert.That(new R3VehicleModel(blueprint).Parts[0].Health, Is.EqualTo(1));
    }
    [Test] public void BuyingChecksConnectivityOccupancyAndFundsAtomically()
    {
        var model = new R3VehicleModel(Blueprint());
        Assert.That(model.Buy(Vector3Int.forward, R3PartKind.Armor), Is.False);
        model.Award(20);
        Assert.That(model.Buy(new Vector3Int(5, 0, 5), R3PartKind.Armor), Is.False);
        Assert.That(model.Buy(Vector3Int.zero, R3PartKind.Armor), Is.False);
        Assert.That(model.Scrap, Is.EqualTo(20));
        Assert.That(model.Buy(Vector3Int.forward, R3PartKind.Armor), Is.True);
        Assert.That(model.Scrap, Is.EqualTo(15));
        Assert.That(model.Buy(Vector3Int.forward, R3PartKind.Gun), Is.False);
        Assert.That(model.Parts.Count, Is.EqualTo(5));
    }
    [Test] public void RepairIsCheaperThanReplacingLostModuleAndDoesNotCreateFreeParts()
    {
        var model = new R3VehicleModel(Blueprint()); model.Award(50);
        var wheel = model.Parts[2]; wheel.Health = 0.5f;
        Assert.That(model.RepairCost(wheel), Is.EqualTo(3));
        Assert.That(model.Repair(wheel.Cell), Is.True);
        Assert.That(wheel.Health, Is.EqualTo(1));
        Assert.That(model.Scrap, Is.EqualTo(47));
        wheel.Health = 0;
        Assert.That(model.RepairCost(wheel), Is.EqualTo(15));
        Assert.That(model.Repair(wheel.Cell), Is.True);
        Assert.That(model.Scrap, Is.EqualTo(32));
        Assert.That(model.Repair(wheel.Cell), Is.False);
    }
    [Test] public void RemovalProtectsCoreAndConnectedFragmentsButAllowsMissingGun()
    {
        var model = new R3VehicleModel(Blueprint()); model.Award(20);
        Assert.That(model.Remove(Vector3Int.zero), Is.False);
        Assert.That(model.Remove(Vector3Int.up), Is.True);
        Assert.That(model.CanDeploy, Is.False);
        Assert.That(model.Buy(Vector3Int.forward, R3PartKind.Armor), Is.True);
        Assert.That(model.Buy(Vector3Int.forward * 2, R3PartKind.Armor), Is.True);
        Assert.That(model.Remove(Vector3Int.forward), Is.False);
        Assert.That(model.Remove(Vector3Int.forward * 2), Is.True);
        Assert.That(model.Scrap, Is.EqualTo(10), "Usuwanie nie produkuje waluty.");
    }
    [Test] public void EmergencyOnlyRebuildsMissingEssentialsAtFullPrice()
    {
        var model = new R3VehicleModel(Blueprint());
        Assert.That(model.EmergencyRepair(), Is.False);
        model.Parts[1].Health = 0; model.Parts[2].Health = 0; model.Award(45);
        Assert.That(model.CanDeploy, Is.False);
        Assert.That(model.EmergencyRepair(), Is.True);
        Assert.That(model.CanDeploy, Is.True);
        Assert.That(model.Parts[1].Health, Is.EqualTo(1f));
        Assert.That(model.Parts[2].Health, Is.EqualTo(1f));
        Assert.That(model.Parts[0].Health, Is.EqualTo(1));
        Assert.That(model.Scrap, Is.Zero);
        Assert.That(model.EmergencyRepair(), Is.False);
    }
}
