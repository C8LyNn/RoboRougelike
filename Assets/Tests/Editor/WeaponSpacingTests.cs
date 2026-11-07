using System.Linq;
using NUnit.Framework;
using RoboRougelike.R3;
using RoboRougelike.R4;
using UnityEngine;

public sealed class WeaponSpacingTests
{
    static R3VehicleModel Light()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        model.Award(10000);return model;
    }

    [TestCase(1,0,0)] [TestCase(-1,0,0)] [TestCase(0,0,1)] [TestCase(0,0,-1)]
    [TestCase(1,0,1)] [TestCase(-1,0,-1)] [TestCase(1,0,-1)] [TestCase(-1,0,1)]
    public void NewGunCannotBePlacedOnNeighbouringFaceOrDiagonal(int dx,int dy,int dz)
    {
        var model=Light();var cell=new Vector3Int(dx,1+dy,dz);
        var before=model.Scrap;
        Assert.That(model.PlacementError(cell,R3PartKind.Gun),Does.Contain("odstęp"));
        Assert.That(model.Buy(cell,R3PartKind.Gun),Is.False);
        Assert.That(model.Scrap,Is.EqualTo(before));
    }

    [Test] public void VerticalDiagonalIsAlsoReservedForGunAcrossRotations()
    {
        var design=R3VehicleCatalog.Create(R3VehicleKind.Light);
        design.Parts.Add(new R3PartState(new Vector3Int(1,1,0),R3PartKind.Armor));
        design.Parts.Add(new R3PartState(new Vector3Int(1,1,1),R3PartKind.Armor));
        var model=new R3VehicleModel(design);model.Award(10000);
        foreach(var rotation in new[]{Vector3Int.zero,new Vector3Int(1,0,0),new Vector3Int(0,1,0),new Vector3Int(0,0,1)})
        {
            Assert.That(model.PlacementError(new Vector3Int(1,2,0),R3PartKind.Gun,false,R4WeaponKind.Basic,rotation),Does.Contain("odstęp"));
            Assert.That(model.PlacementError(new Vector3Int(1,2,1),R3PartKind.Gun,false,R4WeaponKind.Basic,rotation),Does.Contain("odstęp"));
        }
    }

    [Test] public void SpacingAlsoAppliesBelowAnExistingGun()
    {
        var design=R3VehicleCatalog.Create(R3VehicleKind.Light);
        design.Parts.First(p=>p.Kind==R3PartKind.Gun).Cell=new Vector3Int(0,2,0);
        design.Parts.Add(new R3PartState(new Vector3Int(0,1,0),R3PartKind.Armor));
        var model=new R3VehicleModel(design);model.Award(10000);
        Assert.That(model.PlacementError(new Vector3Int(1,1,0),R3PartKind.Gun),Does.Contain("odstęp"));
        Assert.That(model.PlacementError(new Vector3Int(1,1,1),R3PartKind.Gun),Does.Contain("odstęp"));
    }

    [Test] public void GapOfOneCellPermitsGunAndNonWeaponNeighboursRemainAllowed()
    {
        var model=Light();
        Assert.That(model.PlacementError(new Vector3Int(0,1,2),R3PartKind.Gun),Is.Null);
        Assert.That(model.Buy(new Vector3Int(0,1,2),R3PartKind.Gun),Is.True);
        Assert.That(model.PlacementError(new Vector3Int(1,1,0),R3PartKind.Armor),Is.Null);
    }

    [Test] public void StoredAndRecoveredGunsCannotBypassSpacing()
    {
        var model=Light();var recovered=new R3PartState(Vector3Int.zero,R3PartKind.Gun,.5f){Weapon=R4WeaponKind.Basic};
        Assert.That(model.ReceiveSalvage(new[]{recovered}),Is.True);
        Assert.That(model.RepairInventory(recovered.Id),Is.True);
        var target=new Vector3Int(1,1,0);
        Assert.That(model.OwnedPlacementError(recovered.Id,target,Vector3Int.zero),Does.Contain("odstęp"));
        Assert.That(model.Mount(recovered.Id,target),Is.False);
        Assert.That(model.Inventory.Any(p=>p.Id==recovered.Id),Is.True);
    }

    [Test] public void LegacyAdjacentGunsLoadAndCanBeRepairedWithoutLosingParts()
    {
        var legacy=R3VehicleCatalog.Create(R3VehicleKind.Light);
        var neighbour=new R3PartState(new Vector3Int(1,1,0),R3PartKind.Gun,0){Weapon=R4WeaponKind.Basic};
        legacy.Parts.Add(neighbour);
        var model=new R3VehicleModel(legacy);model.Award(10000);
        string savedId=model.At(neighbour.Cell).Id;var saved=model.MakeCheckpoint(0);
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(saved,R3VehicleKind.Light,new R3GarageEconomy(),0,out var restored,out var error),Is.True,error);
        Assert.That(restored.Parts.Any(p=>p.Id==savedId),Is.True);
        Assert.That(restored.Repair(neighbour.Cell),Is.True);
        Assert.That(restored.At(neighbour.Cell).Health,Is.EqualTo(1));
    }

    [TestCase(R3VehicleKind.Light,1)] [TestCase(R3VehicleKind.Standard,2)] [TestCase(R3VehicleKind.Heavy,3)]
    public void DefaultVehiclesKeepGunsWithRequiredSpacing(R3VehicleKind kind,int expectedCount)
    {
        var guns=R3VehicleCatalog.Create(kind).Parts.Where(p=>p.Kind==R3PartKind.Gun).ToArray();
        Assert.That(guns.Length,Is.EqualTo(expectedCount));
        for(int i=0;i<guns.Length;i++)for(int j=i+1;j<guns.Length;j++)
        {
            var d=guns[i].Cell-guns[j].Cell;
            Assert.That(Mathf.Max(Mathf.Abs(d.x),Mathf.Abs(d.y),Mathf.Abs(d.z)),Is.GreaterThan(1));
        }
    }
}
