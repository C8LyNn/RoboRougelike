using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using RoboRougelike.R3;

// B01-22 / P17 W32: genuine existing R3VehicleModel EditMode source.
// These Unity tests are NOT RUN until the native Unity 6000.3.14f1 gate.
public sealed class B0122W32RebuildTransactionsTests
{
    [Test]
    public void MountedOwnedPurchaseOne_PaidBookingMustRemainPendingUntilTrustedWin()
    {
        var design=new R3VehicleDesign();
        design.Parts.Add(new R3PartState(Vector3Int.zero,R3PartKind.Core));
        design.Parts.Add(new R3PartState(Vector3Int.right,R3PartKind.Armor,0f));
        var model=new R3VehicleModel(design,new R3GarageEconomy{ArmorCost=1});
        var target=model.Parts.Single(p=>p.Kind==R3PartKind.Armor);
        string ownedId=target.Id;
        Assert.That(model.Scrap,Is.Zero);
        Assert.That(model.RepairCost(target),Is.Zero);
        Assert.That(model.Repair(target.Cell),Is.True,
            "A paid-zero rebuild ORDER can be booked, but must not release HP now.");
        Assert.That(model.Scrap,Is.Zero);
        Assert.That(target.Health,Is.Zero,
            "V7 forbids immediate HP recovery: settle only after a future authentic distinct WinId in ONE Save.");
        Assert.That(target.Id,Is.EqualTo(ownedId));
        Assert.That(model.Repair(target.Cell),Is.False,
            "An already queued paid-zero rebuild may never be booked twice.");
        Assert.That(model.Parts.Count(p=>p.Id==ownedId),Is.EqualTo(1));
    }

    [Test]
    public void PartiallyDamagedArmor_StillUsesExistingThirtyFivePercentRate()
    {
        var design=new R3VehicleDesign();
        var part=new R3PartState(Vector3Int.zero,R3PartKind.Armor,0.5f);
        design.Parts.Add(part);
        var model=new R3VehicleModel(design,new R3GarageEconomy{ArmorCost=100});
        var mounted=model.Parts.Single();
        Assert.That(model.RepairCost(mounted),Is.EqualTo(18),
            "Above-zero HP repair pricing stays ceil((1-HP)*35%); V7 only changes 0HP rebuilding.");
        Assert.That(model.RepairCost(new R3PartState(default,R3PartKind.Armor,1f)),Is.Zero);
    }

    [Test]
    public void UnaffordableDestroyedRepair_DoesNotMutateWalletPartOrId()
    {
        var design=new R3VehicleDesign();
        design.Parts.Add(new R3PartState(Vector3Int.zero,R3PartKind.Core));
        design.Parts.Add(new R3PartState(Vector3Int.right,R3PartKind.Armor,0f));
        var model=new R3VehicleModel(design,new R3GarageEconomy{ArmorCost=100});
        var wreck=model.Parts.Single(p=>p.Kind==R3PartKind.Armor);
        var ownedId=wreck.Id;
        Assert.That(model.RepairCost(wreck),Is.EqualTo(65));
        Assert.That(model.Repair(wreck.Cell),Is.False);
        Assert.That(model.Scrap,Is.Zero);
        Assert.That(wreck.Health,Is.Zero);
        Assert.That(wreck.Id,Is.EqualTo(ownedId));
    }

    [Test]
    public void EmergencyRepair_AllZeroPaidOrdersMustStayPendingWithoutDuplication()
    {
        var design=new R3VehicleDesign();
        design.Parts.Add(new R3PartState(Vector3Int.zero,R3PartKind.Core));
        design.Parts.Add(new R3PartState(Vector3Int.left,R3PartKind.Armor));
        design.Parts.Add(new R3PartState(Vector3Int.right,R3PartKind.Armor));
        design.Parts.Add(new R3PartState(Vector3Int.left*2,R3PartKind.Wheel,0f));
        design.Parts.Add(new R3PartState(Vector3Int.right*2,R3PartKind.Wheel,0f));
        design.Parts.Add(new R3PartState(Vector3Int.forward,R3PartKind.Gun,0f));
        var model=new R3VehicleModel(design,new R3GarageEconomy{
            ArmorCost=5,WheelCost=1,GunCost=1
        });
        var before=model.Parts.ToDictionary(p=>p.Cell,p=>p.Id);
        Assert.That(model.Scrap,Is.Zero);
        Assert.That(model.CanDeploy,Is.False);
        Assert.That(model.EmergencyRepairCost,Is.Zero,
            "A valid three-part plan may be entirely PAID for 0 Scrap.");
        Assert.That(model.EmergencyRepair(),Is.True,
            "Every valid 0 Scrap order should be booked atomically, not settled early.");
        Assert.That(model.CanDeploy,Is.False,
            "Pending 0HP parts cannot become physically operational before a genuine later victory.");
        Assert.That(model.Parts.Where(p=>p.Kind==R3PartKind.Gun||p.Kind==R3PartKind.Wheel)
            .All(p=>p.Health==0f),Is.True,
            "All 0HP owned essentials must remain pending until actual V7 settlement.");
        Assert.That(model.Scrap,Is.Zero);
        Assert.That(model.Parts.Count,Is.EqualTo(before.Count));
        foreach(var part in model.Parts)
            Assert.That(part.Id,Is.EqualTo(before[part.Cell]));
        Assert.That(model.EmergencyRepair(),Is.False,
            "No duplicate emergency booking while original orders are already pending.");
        Assert.That(model.Scrap,Is.Zero);
    }

    [Test]
    public void DestroyedPriceMaxInt_UsesOverflowSafeSixtyFivePercentQuote()
    {
        var model=new R3VehicleModel(new R3VehicleDesign(),
            new R3GarageEconomy{ArmorCost=int.MaxValue});
        var wreck=new R3PartState(Vector3Int.zero,R3PartKind.Armor,0f);
        int quote=model.RepairCost(wreck);
        Assert.That(quote,Is.EqualTo((int)((long)int.MaxValue*65L/100L)));
        Assert.That(quote,Is.GreaterThanOrEqualTo(0));
        Assert.That(quote,Is.LessThan(int.MaxValue));
    }
}
