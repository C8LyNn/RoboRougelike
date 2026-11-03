using System.Linq;
using NUnit.Framework;
using UnityEngine;
using RoboRougelike.R3;

// B01-10 / P17 W31: intentionally RED until the existing real game's economy
// is reconciled with the owner-approved ECON V7 policy #731 / #780.
// These tests must NEVER be advertised as Unity PASS before running a
// real activated Unity 6000.3.14f1 editor at an exact integrated game SHA.
public sealed class B0110W31EconomyParityTests
{
    [TestCase(5, 3)]
    [TestCase(15, 9)]
    [TestCase(30, 19)]
    [TestCase(100, 65)]
    public void OwnedDestroyedArmorRebuildQuoteMatchesApprovedV7Floor65(int newPurchasePrice, int expectedDestroyedQuote)
    {
        var economy = new R3GarageEconomy { ArmorCost = newPurchasePrice };
        var model = new R3VehicleModel(new R3VehicleDesign(), economy);
        var destroyed = new R3PartState(Vector3Int.zero, R3PartKind.Armor, 0f);
        Assert.That(model.Price(R3PartKind.Armor), Is.EqualTo(newPurchasePrice),
            "Fixture must preserve an actual new-purchase price from the R3 model.");
        Assert.That(model.RepairCost(destroyed), Is.EqualTo(expectedDestroyedQuote),
            "The owned 0HP rebuild quote must match owner-approved V7 floor(65% of new price), not the old 35% partial-repair formula. Do not change above-zero HP repair policy via this test.");
    }

    [Test]
    public void PurchasePriceOneAcceptsZeroPaidOrderButMustNotImmediatelyRestoreHP()
    {
        var model = new R3VehicleModel(
            new R3VehicleDesign(),
            new R3GarageEconomy { WheelCost = 1 });
        var destroyed = new R3PartState(Vector3Int.zero, R3PartKind.Wheel, 0f);
        Assert.That(model.ReceiveSalvage(new[] { destroyed }), Is.True,
            "A uniquely owned destroyed wheel must enter inventory without creating a win or reward.");
        var owned = model.Inventory.Single(p => p.Id == destroyed.Id);
        Assert.That(model.Price(R3PartKind.Wheel), Is.EqualTo(1));
        Assert.That(model.RepairCost(owned), Is.EqualTo(0),
            "V7's paid-zero boundary: floor(.65 * 1) = 0, not a FREE victory entitlement.");
        Assert.That(model.RepairInventory(owned.Id), Is.True,
            "A zero-quoted PAID repair must be executable with 0 Scrap; current R3 code rejects price <= 0. This assertion is intentionally RED until authorized integration.");
        Assert.That(model.Scrap, Is.Zero);
        Assert.That(owned.Health, Is.Zero, 
            "V7 0HP paid order must remain physically pending until the NEXT distinct authentic victory; immediate HP restoration would violate approved cadence.");
        Assert.That(model.Inventory.Single(p => p.Id == owned.Id).Id, Is.EqualTo(destroyed.Id),
            "Repair must retain the original owned identifier.");
    }
}
