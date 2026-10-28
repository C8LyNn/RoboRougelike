using System.Linq;
using NUnit.Framework;
using RoboRougelike.R3;
using RoboRougelike.R4;
using UnityEngine;

public sealed class OwnedRebuildPricingTests
{
    static R3VehicleModel Model(R3GarageEconomy economy=null)=>new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light),economy);
    [TestCase(R3PartKind.Armor,5)][TestCase(R3PartKind.Wheel,15)][TestCase(R3PartKind.Gun,30)]
    public void OwnedDestroyedRebuildCostsLessThanPurchase(R3PartKind kind,int purchase)
    {
        var m=Model();var p=new R3PartState(Vector3Int.zero,kind,0);int quote=m.RepairCost(p);
        Assert.That(m.Price(kind),Is.EqualTo(purchase));Assert.That(quote,Is.GreaterThan(0).And.LessThan(purchase));
    }
    [TestCase(R3PartKind.Wheel)][TestCase(R3PartKind.Gun)]
    public void OwnedRebuildChargesOnceAndKeepsPhysicalGuid(R3PartKind kind)
    {
        var m=Model();var p=new R3PartState(Vector3Int.zero,kind,0);Assert.That(m.ReceiveSalvage(new[]{p}),Is.True);
        Assert.That(m.RepairInventory(p.Id),Is.False);Assert.That(m.Inventory.Single().Health,Is.Zero);
        m.Award(100);var quote=m.RepairCost(m.Inventory.Single());var balance=m.Scrap;
        Assert.That(m.RepairInventory(p.Id),Is.True);Assert.That(m.Inventory.Single().Id,Is.EqualTo(p.Id));
        Assert.That(m.Inventory.Single().Health,Is.EqualTo(1));Assert.That(m.Scrap,Is.EqualTo(balance-quote));
        Assert.That(m.RepairInventory(p.Id),Is.False);Assert.That(m.Scrap,Is.EqualTo(balance-quote));
    }
    [Test] public void OrdinaryPartialRepairRetainsExistingPrice()
    {var m=Model();Assert.That(m.RepairCost(new R3PartState(Vector3Int.zero,R3PartKind.Wheel,.78f)),Is.EqualTo(2));}
    [Test] public void OneScrapPurchaseHasNoFreeOrFullPriceRebuild()
    {
        var m=Model(new R3GarageEconomy{WheelCost=1});var p=new R3PartState(Vector3Int.zero,R3PartKind.Wheel,0);
        Assert.That(m.ReceiveSalvage(new[]{p}),Is.True);m.Award(1);
        Assert.That(m.RepairCost(m.Inventory.Single()),Is.Zero);Assert.That(m.RepairInventory(p.Id),Is.False);
        Assert.That(m.Scrap,Is.EqualTo(1));Assert.That(m.Owns(p.Id),Is.True);Assert.That(m.Inventory.Single().Health,Is.Zero);
    }
    [Test] public void EmergencyRepairCannotBypassOneScrapQuoteGuard()
    {
        var m=Model(new R3GarageEconomy{WheelCost=1});m.Award(100);
        foreach(var wheel in m.Parts.Where(p=>p.Kind==R3PartKind.Wheel&&p.Cell.x<0))wheel.Health=0;
        var before=m.Scrap;Assert.That(m.CanDeploy,Is.False);Assert.That(m.EmergencyRepair(),Is.False);
        Assert.That(m.Scrap,Is.EqualTo(before));Assert.That(m.Parts.Where(p=>p.Kind==R3PartKind.Wheel&&p.Cell.x<0).All(p=>p.Health==0),Is.True);
    }
}
