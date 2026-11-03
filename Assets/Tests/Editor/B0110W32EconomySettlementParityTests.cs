using System.Linq;
using NUnit.Framework;
using UnityEngine;
using RoboRougelike.R3;

// B01-10 W32 — RED-FIRST tests against ORIGINAL actual R3 model.
// Owner-approved V7 (#731/#780): any 0HP owned-part rebuild order, including PAID 0,
// settles on the next distinct authenticated WIN, never at the moment of booking.
// These tests are intentionally RED on the current #877 model. Source CI is not Unity G1.
public sealed class B0110W32EconomySettlementParityTests
{
    static R3VehicleModel BuildConnectedArmor(int armorPrice, float hp)
    {
        var design = new R3VehicleDesign();
        design.Parts.Add(new R3PartState(Vector3Int.zero, R3PartKind.Core));
        design.Parts.Add(new R3PartState(Vector3Int.right, R3PartKind.Armor, hp));
        return new R3VehicleModel(design, new R3GarageEconomy { ArmorCost = armorPrice });
    }

    [Test]
    public void DestroyedOwnedMountedPartPaysV7QuoteButStaysAtZeroUntilAuthenticatedNextWin()
    {
        var model = BuildConnectedArmor(100, 0f);
        model.Award(100);
        var part = model.Parts.Single(p => p.Kind == R3PartKind.Armor);
        var guid = part.Id;
        Assert.That(model.RepairCost(part), Is.EqualTo(65),
            "Owner-approved floor(0.65 * 100) = 65; current live game incorrectly quotes 35.");
        Assert.That(model.Repair(part.Cell), Is.True,
            "An affordable real mounted-part rebuild ORDER should be accepted by the canonical economy, not immediately applied.");
        Assert.That(model.Scrap, Is.EqualTo(35), "Paid booking debits one 65 Scrap quote exactly once.");
        Assert.That(part.Id, Is.EqualTo(guid), "A booked rebuild must not change the owned GUID.");
        Assert.That(part.Health, Is.Zero,
            "V7 next-distinct-win settlement: no HP can be restored at booking time, before a trusted victory receipt.");
        // Post-win release must be proven separately by the actual R1 + #896 one-writer
        // binder. This model has no WinId API: never fake a victory in this fixture.
    }

    [Test]
    public void AlreadyDamagedButOperationalPartKeepsExistingImmediatePartialRepairPolicy()
    {
        var model = BuildConnectedArmor(100, .5f);
        model.Award(100);
        var part = model.Parts.Single(p => p.Kind == R3PartKind.Armor);
        var guid = part.Id;
        Assert.That(model.RepairCost(part), Is.EqualTo(18),
            "Owner left above-zero HP repair policy separate: ceil((1-.5)*100*.35) = 18.");
        Assert.That(model.Repair(part.Cell), Is.True,
            "Do not accidentally delay operational partial HP repairs when changing 0HP rebuild flow.");
        Assert.That(model.Scrap, Is.EqualTo(82));
        Assert.That(part.Health, Is.EqualTo(1f));
        Assert.That(part.Id, Is.EqualTo(guid));
    }
}
