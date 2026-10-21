using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using RoboRougelike.R3;
using RoboRougelike.R4;

public class MOVSeriesModelTests
{
    public static R3PartKind Kind(string name)
    {
        Assert.That(Enum.TryParse(name,out R3PartKind kind),Is.True,"Missing original MOV construction category: "+name);
        return kind;
    }
    static Vector3Int Cell(int i)=>new Vector3Int(i%2==0?-1:1,0,3+i/2);
    [TestCase("Booster",35,18f,4)]
    [TestCase("JumpDrive",45,24f,2)]
    [TestCase("Blink",60,28f,1)]
    [TestCase("Gyroscope",30,16f,2)]
    [TestCase("OmniDrive",40,22f,8)]
    [TestCase("MagneticGrapple",45,24f,2)]
    [TestCase("HoverEngine",35,18f,8)]
    [TestCase("CrawlerTrack",45,40f,8)]
    public void OriginalPartsUseExistingEconomyMassInventoryAndLimits(string name,int price,float mass,int limit)
    {
        var kind=Kind(name);var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));model.Award(10000);
        Assert.That(model.Price(kind),Is.EqualTo(price));
        Assert.That(model.Buy(Cell(0),kind),Is.True);
        var part=model.At(Cell(0));string id=part.Id;
        Assert.That(R4WeaponDefinition.PartMass(part),Is.EqualTo(mass));
        Assert.That(R4WeaponDefinition.PartName(part),Is.Not.EqualTo("Pancerz"));
        part.Health=.4f;Assert.That(model.Repair(Cell(0)),Is.True);Assert.That(part.Health,Is.EqualTo(1));
        int scrap=model.Scrap;Assert.That(model.Remove(Cell(0)),Is.True);
        Assert.That(model.Inventory.Single().Id,Is.EqualTo(id));
        Assert.That(model.Mount(id,Cell(0)),Is.True);Assert.That(model.Mount(id,Cell(0)),Is.False);
        Assert.That(model.Inventory,Is.Empty);Assert.That(model.Scrap,Is.EqualTo(scrap));
        Assert.That(model.At(Cell(0)).Id,Is.EqualTo(id));
        for(int i=1;i<limit;i++)Assert.That(model.Buy(Cell(i),kind),Is.True);
        scrap=model.Scrap;Assert.That(model.Buy(Cell(limit),kind),Is.False);
        Assert.That(model.Scrap,Is.EqualTo(scrap));Assert.That(model.Parts.Count(p=>p.Kind==kind),Is.EqualTo(limit));
        part.Health=0;Assert.That(model.RepairCost(part),Is.EqualTo(price));
    }
    [Test] public void ExistingNumericKindsArePreservedAndUnknownStillRejected()
    {
        Assert.That((int)R3PartKind.Core,Is.EqualTo(3));Assert.That((int)R3PartKind.Suspension,Is.EqualTo(4));
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));model.Award(100);
        Assert.That(model.Buy(Cell(0),(R3PartKind)999),Is.False);Assert.That(model.Scrap,Is.EqualTo(100));
    }
    [Test] public void CooldownAndDeviceSettingsSurviveCopyWithoutMutatingOriginal()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));model.Award(100);
        Assert.That(model.Buy(Cell(0),Kind("Booster")),Is.True);var part=model.At(Cell(0));
        var ready=typeof(R3PartState).GetField("DeviceReadyAt");Assert.That(ready,Is.Not.Null,"Cooldown must survive reconstruction.");ready.SetValue(part,12.5f);
        var enabled=typeof(R3VehicleModel).GetField("GyroEnabled");Assert.That(enabled,Is.Not.Null);enabled.SetValue(model,false);
        var thrust=typeof(R3VehicleModel).GetField("BoosterThrustNewtons");Assert.That(thrust,Is.Not.Null);thrust.SetValue(model,2500f);
        var duration=typeof(R3VehicleModel).GetField("BoosterDurationSeconds");Assert.That(duration,Is.Not.Null);duration.SetValue(model,1.25f);
        var copy=model.DeepCopy();Assert.That(ready.GetValue(copy.At(Cell(0))),Is.EqualTo(12.5f));
        Assert.That(enabled.GetValue(copy),Is.EqualTo(false));Assert.That(thrust.GetValue(copy),Is.EqualTo(2500f));Assert.That(duration.GetValue(copy),Is.EqualTo(1.25f));
        ready.SetValue(copy.At(Cell(0)),90f);thrust.SetValue(copy,1000f);
        Assert.That(ready.GetValue(part),Is.EqualTo(12.5f));Assert.That(thrust.GetValue(model),Is.EqualTo(2500f));
    }
}
