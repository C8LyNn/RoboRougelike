using System.Linq;
using NUnit.Framework;
using RoboRougelike.R3;
using UnityEngine;

// B01-60 / P17 W32. Original in-game R3VehicleModel, NOT a new store/reducer.
// Deliberate product RED: floor65%, PAID-zero and delayed next-WinId settlement missing.
// #910/#915 price-only fixes are incomplete; Unity Editor/Player not run.
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
    public void ZeroQuotedMountedWheelBooksOnceButStaysAtZeroHpUntilNextTrueVictory()
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
            "book real paid-zero PENDING order without fake victory");
        Assert.That(part.Health, Is.Zero, "destroyed 0HP must remain PENDING until NEXT DISTINCT authentic WinId");
        Assert.That(model.Scrap, Is.Zero);
        Assert.That(model.At(Vector3Int.right).Id, Is.EqualTo(ownedId));
        Assert.That(model.Repair(Vector3Int.right), Is.False,
            "must not duplicate pending booking before a new genuine victory");
    }

    [Test]
    public void ZeroQuotedBulkRebuildBooksOnceWithoutImmediateRestoration()
    {
        var model = WheelAtOneScrap();
        var part = model.At(Vector3Int.right);
        var id = part.Id;
        Assert.That(model.RepairCost(part), Is.Zero);
        Assert.That(model.RepairAffordable(), Is.EqualTo(1),
            "bulk must book the one legitimate zero-price PAID order");
        Assert.That(model.Scrap, Is.Zero);
        Assert.That(part.Health, Is.Zero, "bulk PAID order must not instantly heal 0HP");
        Assert.That(model.At(Vector3Int.right).Id, Is.EqualTo(id));
        Assert.That(model.RepairAffordable(), Is.Zero,
            "pending order must not be rebooked or loop infinitely");
    }

    [Test]
    public void ZeroQuotedEmergencyBooksPendingStillCannotDeployAndPreservesIds()
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
            "emergency paid-zero booking must remain pending, without fabricated victory");
        Assert.That(model.At(Vector3Int.right).Health, Is.Zero, "next real win still required");
        Assert.That(model.CanDeploy, Is.False, "do not silently override loaner/End Run #834");
        Assert.That(model.Scrap, Is.Zero);
        Assert.That(model.Parts.Select(p => p.Id).OrderBy(id => id).ToArray(),
            Is.EqualTo(idsBefore), "same IDs, no duplicate/extra/loaner part");
        Assert.That(model.EmergencyRepair(), Is.False,
            "do not rebook a pending emergency order before authentic next win");
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
    // Real live ORIGINAL R3 dynamic world input. This does NOT test absent #913 Save binder.
    [TestCase(R3VehicleKind.Light, 20)]
    [TestCase(R3VehicleKind.Standard, 83)]
    [TestCase(R3VehicleKind.Heavy, 139)]
    public void ActualR3VehicleHasFullDynamicWorldAndStableIdsNotTwoSlots(R3VehicleKind kind, int expected)
    {
        var model = new R3VehicleModel(R3VehicleCatalog.Create(kind));
        var ids = model.Parts.Select(p => p.Id).OrderBy(id => id).ToArray();
        Assert.That(model.Parts.Count, Is.EqualTo(expected), "real catalog must retain every original part");
        Assert.That(ids.Distinct().Count(), Is.EqualTo(expected), "no reused live part GUIDs");
        Assert.That(ids.All(id => id != null && id.Length == 32), Is.True, "real R3 GUID identity");
        Assert.That(model.DeepCopy().Parts.Select(p => p.Id).OrderBy(id => id).ToArray(),
            Is.EqualTo(ids), "all original R3 part IDs survive normal model copy");
        Assert.That(expected, Is.GreaterThan(2),
            "frozen #896 two-slot format cannot represent this full R3 world; real Save cold G1 BLOCKED #913");
    }

}
