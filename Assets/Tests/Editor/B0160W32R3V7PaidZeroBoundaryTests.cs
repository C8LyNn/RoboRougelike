using System.Linq;
using NUnit.Framework;
using RoboRougelike.R3;
using UnityEngine;

// B01-60 / P17 W32. Original in-game R3VehicleModel, NOT a new store/reducer.
// Deliberate product RED at the pinned #877 SHA until approved V7 price and
// zero-cost *paid* repair semantics are implemented by the actual game owner.
// Only the source cases are authored here. No Unity editor/Player has run.
public sealed class B0160W32R3V7PaidZeroBoundaryTests
{
    private static R3VehicleModel WheelAtOneScrap()
    {
        var design = new R3VehicleDesign();
        design.Parts.Add(new R3PartState(Vector3Int.zero, R3PartKind.Core));
        design.Parts.Add(new R3PartState(Vector3Int.right, R3PartKind.Wheel, 0f));
        return new R3VehicleModel(design, new R3GarageEconomy { WheelCost = 1 })
        {
            SelectedDrive = R3DriveKind.Wheels
        };
    }

    [Test]
    public void ZeroQuotedMountedWheelRepairsAtNoScrapWithoutReplacingOwnedId()
    {
        var model = WheelAtOneScrap();
        var part = model.At(Vector3Int.right);
        var ownedId = part.Id;
        Assert.That(model.Scrap, Is.Zero, "exact zero wallet precondition");
        Assert.That(model.Price(R3PartKind.Wheel), Is.EqualTo(1));
        Assert.That(model.RepairCost(part), Is.Zero,
            "V7 floor(65% × 1) is zero, still an owner-paid repair");
        Assert.That(model.At(Vector3Int.zero).Health, Is.GreaterThan(0f),
            "there must be a physically connected live core neighbor");
        Assert.That(model.Repair(Vector3Int.right), Is.True,
            "the existing R3 mounted Repair() must accept a valid paid-zero quote");
        Assert.That(part.Health, Is.EqualTo(1f));
        Assert.That(model.Scrap, Is.Zero);
        Assert.That(model.At(Vector3Int.right).Id, Is.EqualTo(ownedId));
        Assert.That(model.Repair(Vector3Int.right), Is.False,
            "cannot repeatedly rebuild an already intact owned part");
    }

    [Test]
    public void ZeroQuotedBulkRepairActuallyRestoresOneWithoutSpendingScrap()
    {
        var model = WheelAtOneScrap();
        var part = model.At(Vector3Int.right);
        var id = part.Id;
        Assert.That(model.RepairCost(part), Is.Zero);
        Assert.That(model.RepairAffordable(), Is.EqualTo(1),
            "RepairAffordable must not omit a legal zero-priced PAID repair");
        Assert.That(model.Scrap, Is.Zero);
        Assert.That(part.Health, Is.EqualTo(1f));
        Assert.That(model.At(Vector3Int.right).Id, Is.EqualTo(id));
        Assert.That(model.RepairAffordable(), Is.Zero,
            "a completed bulk repair cannot be applied twice");
    }

    [Test]
    public void ZeroQuotedEmergencyRepairRecoversRequiredRightWheelAndPreservesIds()
    {
        var design = new R3VehicleDesign();
        design.Parts.Add(new R3PartState(Vector3Int.zero, R3PartKind.Core));
        design.Parts.Add(new R3PartState(Vector3Int.up, R3PartKind.Gun));
        design.Parts.Add(new R3PartState(Vector3Int.left, R3PartKind.Wheel));
        design.Parts.Add(new R3PartState(Vector3Int.right, R3PartKind.Wheel, 0f));
        var model = new R3VehicleModel(design,
            new R3GarageEconomy { WheelCost = 1 })
        { SelectedDrive = R3DriveKind.Wheels };
        var idsBefore = model.Parts.Select(p => p.Id).OrderBy(id => id).ToArray();
        Assert.That(model.CanDeploy, Is.False,
            "one destroyed essential wheel prevents the original real R3 rig deployment");
        Assert.That(model.EmergencyRepairCost, Is.Zero,
            "the genuine emergency plan must contain the zero-cost right wheel");
        Assert.That(model.Scrap, Is.Zero);
        Assert.That(model.EmergencyRepair(), Is.True,
            "emergency PAID-zero repair must restore the original rig without a free-win coupon");
        Assert.That(model.At(Vector3Int.right).Health, Is.EqualTo(1f));
        Assert.That(model.CanDeploy, Is.True);
        Assert.That(model.Scrap, Is.Zero);
        Assert.That(model.Parts.Select(p => p.Id).OrderBy(id => id).ToArray(),
            Is.EqualTo(idsBefore), "same IDs, no duplicate/extra/loaner part");
        Assert.That(model.EmergencyRepair(), Is.False,
            "no duplicate emergency transaction after deployability is restored");
    }

    [Test]
    public void ExistingNonzeroPartialHpRepairRateAndOneDebitRemainUnchanged()
    {
        var design = new R3VehicleDesign();
        design.Parts.Add(new R3PartState(Vector3Int.zero, R3PartKind.Core));
        design.Parts.Add(new R3PartState(Vector3Int.right, R3PartKind.Wheel, .5f));
        var model = new R3VehicleModel(design,
            new R3GarageEconomy { WheelCost = 30 });
        model.Award(10);
        var part = model.At(Vector3Int.right);
        var id = part.Id;
        Assert.That(model.RepairCost(part), Is.EqualTo(6),
            "only destroyed zero-HP rebuild price changes to V7; 50% HP keeps old 35% proportional repair");
        Assert.That(model.Repair(Vector3Int.right), Is.True);
        Assert.That(model.Scrap, Is.EqualTo(4));
        Assert.That(part.Health, Is.EqualTo(1f));
        Assert.That(part.Id, Is.EqualTo(id));
        Assert.That(model.Repair(Vector3Int.right), Is.False);
        Assert.That(model.Scrap, Is.EqualTo(4));
    }

    [Test]
    public void NonzeroDestroyedRebuildStillRequiresFundsNoScrapOrIdMutationOnDenial()
    {
        var design = new R3VehicleDesign();
        design.Parts.Add(new R3PartState(Vector3Int.zero, R3PartKind.Core));
        design.Parts.Add(new R3PartState(Vector3Int.right, R3PartKind.Wheel, 0f));
        var model = new R3VehicleModel(design,
            new R3GarageEconomy { WheelCost = 5 });
        var part = model.At(Vector3Int.right);
        var id = part.Id;
        Assert.That(model.RepairCost(part), Is.EqualTo(3),
            "V7 floor(65% × purchase5)=3, not old ceil(35%)=2");
        Assert.That(model.Scrap, Is.Zero);
        Assert.That(model.Repair(Vector3Int.right), Is.False,
            "paid nonzero repair still requires enough Scrap and must be atomic");
        Assert.That(model.Scrap, Is.Zero);
        Assert.That(part.Health, Is.Zero);
        Assert.That(model.At(Vector3Int.right).Id, Is.EqualTo(id));
    }
}
