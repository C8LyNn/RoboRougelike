using System.Linq;
using NUnit.Framework;
using RoboRougelike.R3;
using RoboRougelike.R4;
using UnityEngine;

// Model-level EditMode tests; these do not create the game scene or capture input.
public sealed class WeaponTypeLimitTests
{
    static R3VehicleModel Model()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Heavy));
        model.Award(10000);
        return model;
    }

    static Vector3Int Cell(int index) => new Vector3Int(index%7-3,2,index/7-3);

    [TestCase(1)] [TestCase(2)] [TestCase(4)] [TestCase(12)]
    public void TwoTypesPermitRequestedNumberOfGuns(int count)
    {
        var model=Model();
        for(int i=1;i<count;i++)
            Assert.That(model.Buy(Cell(i-1),R3PartKind.Gun,i%2==0?R4WeaponKind.Basic:R4WeaponKind.Automatic),Is.True,$"gun {i+1}");
        Assert.That(model.Parts.Count(p=>p.Kind==R3PartKind.Gun),Is.EqualTo(count));
        Assert.That(model.CanDeploy,Is.True);
    }

    [Test] public void ThirdTypeRejectedWithoutSpendingAndAnotherExistingTypeAllowed()
    {
        var model=Model();Assert.That(model.Buy(Cell(0),R3PartKind.Gun,R4WeaponKind.Automatic),Is.True);
        var funds=model.Scrap;var count=model.Parts.Count;
        Assert.That(model.PlacementError(Cell(1),R3PartKind.Gun,false,R4WeaponKind.Heavy),Is.Not.Null);
        Assert.That(model.Buy(Cell(1),R3PartKind.Gun,R4WeaponKind.Heavy),Is.False);
        Assert.That(model.Scrap,Is.EqualTo(funds));Assert.That(model.Parts.Count,Is.EqualTo(count));
        Assert.That(model.Buy(Cell(1),R3PartKind.Gun,R4WeaponKind.Automatic),Is.True);
    }

    [Test] public void LostPlanReservesItsTypeUntilRemovedSoRepairCannotBypassLimit()
    {
        var model=Model();Assert.That(model.Buy(Cell(0),R3PartKind.Gun,R4WeaponKind.Automatic),Is.True);
        var automatic=model.At(Cell(0));automatic.Health=0;
        Assert.That(model.Buy(Cell(1),R3PartKind.Gun,R4WeaponKind.Heavy),Is.False);
        Assert.That(model.Repair(Cell(0)),Is.True);
        Assert.That(model.Buy(Cell(1),R3PartKind.Gun,R4WeaponKind.Heavy),Is.False);
        Assert.That(model.Remove(Cell(0)),Is.True);
        Assert.That(model.Buy(Cell(1),R3PartKind.Gun,R4WeaponKind.Heavy),Is.True);
    }

    [Test] public void RecoveredThirdTypeMayBeStoredAndRepairedButNotMounted()
    {
        var model=Model();Assert.That(model.Buy(Cell(0),R3PartKind.Gun,R4WeaponKind.Automatic),Is.True);
        var recovered=new R3PartState(Vector3Int.zero,R3PartKind.Gun,0){Weapon=R4WeaponKind.Heavy};
        Assert.That(model.ReceiveSalvage(new[]{recovered}),Is.True);
        Assert.That(model.RepairInventory(recovered.Id),Is.True);
        Assert.That(model.OwnedPlacementError(recovered.Id,Cell(1),Vector3Int.zero),Is.Not.Null);
        Assert.That(model.Mount(recovered.Id,Cell(1)),Is.False);
        Assert.That(model.Inventory.Any(p=>p.Id==recovered.Id),Is.True);
    }

    [Test] public void CopyAndDeserializedInvalidDesignCannotDeployOrRestoreThirdType()
    {
        var source=Model();Assert.That(source.Buy(Cell(0),R3PartKind.Gun,R4WeaponKind.Automatic),Is.True);
        var copied=source.DeepCopy();Assert.That(copied.Buy(Cell(1),R3PartKind.Gun,R4WeaponKind.Heavy),Is.False);
        var design=new R3VehicleDesign();
        foreach(var p in source.Parts)design.Parts.Add(JsonUtility.FromJson<R3PartState>(JsonUtility.ToJson(p)));
        design.Parts.Add(new R3PartState(Cell(1),R3PartKind.Gun,0){Weapon=R4WeaponKind.Heavy});
        var loaded=new R3VehicleModel(design);loaded.Award(10000);
        Assert.That(loaded.CanDeploy,Is.False);
        Assert.That(loaded.Repair(Cell(1)),Is.False);
        Assert.That(loaded.DeepCopy().CanDeploy,Is.False);
    }
}
