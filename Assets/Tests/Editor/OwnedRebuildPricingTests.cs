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
        Assert.That(m.Price(kind),Is.EqualTo(purchase*3));Assert.That(quote,Is.GreaterThan(0).And.LessThan(purchase));
    }
    [Test] public void StoreMultiplierDoesNotChangeOwnedRepairOrDismantling()
    {
        var economy=new R3GarageEconomy();
        var multiplier=typeof(R3GarageEconomy).GetField("StandardPurchaseMultiplier");
        Assert.That(multiplier,Is.Not.Null,"Ordinary shop multiplier must be configurable.");
        var model=Model(economy);var wheel=new R3PartState(Vector3Int.zero,R3PartKind.Wheel,.5f);
        int repair=model.RepairCost(wheel),sale=model.DismantleValue(wheel);
        Assert.That(model.Price(R3PartKind.Wheel),Is.EqualTo(45));
        Assert.That(model.Price(R3PartKind.Gun,R4WeaponKind.RocketLauncher),Is.EqualTo(3*R4WeaponDefinition.Get(R4WeaponKind.RocketLauncher).Price));
        multiplier.SetValue(economy,2f);
        Assert.That(model.Price(R3PartKind.Wheel),Is.EqualTo(30));
        Assert.That(model.DeepCopy().Price(R3PartKind.Wheel),Is.EqualTo(30));
        Assert.That(model.RepairCost(wheel),Is.EqualTo(repair));Assert.That(model.DismantleValue(wheel),Is.EqualTo(sale));
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
        foreach(var wheel in m.Parts.Where(p=>p.Kind==R3PartKind.Wheel&&p.Cell.x<0)){wheel.Tier=1;wheel.Health=0;}
        var before=m.Scrap;Assert.That(m.CanDeploy,Is.False);Assert.That(m.EmergencyRepair(),Is.False);
        Assert.That(m.Scrap,Is.EqualTo(before));Assert.That(m.Parts.Where(p=>p.Kind==R3PartKind.Wheel&&p.Cell.x<0).All(p=>p.Health==0),Is.True);
    }
    [Test] public void EmergencyRepairDoesNotChargeForIncompletePlanWithUnrepairableBridge()
    {
        var design=new R3VehicleDesign{Name="Disconnected fragment"};
        design.Parts.Add(new R3PartState(Vector3Int.zero,R3PartKind.Core));
        design.Parts.Add(new R3PartState(Vector3Int.up,R3PartKind.Gun,0));
        design.Parts.Add(new R3PartState(Vector3Int.left,R3PartKind.Wheel));
        design.Parts.Add(new R3PartState(Vector3Int.right,R3PartKind.Wheel));
        design.Parts.Add(new R3PartState(new Vector3Int(-1,0,1),R3PartKind.Armor,0));
        design.Parts.Add(new R3PartState(new Vector3Int(-1,0,2),R3PartKind.Armor));
        var m=new R3VehicleModel(design,new R3GarageEconomy{ArmorCost=1});m.Award(100);
        var gun=m.At(Vector3Int.up);int before=m.Scrap;
        Assert.That(m.CanDeploy,Is.False);
        Assert.That(m.EmergencyRepairCost,Is.Zero,"An unrepairable bridge cannot be included in an actionable quote.");
        Assert.That(m.EmergencyRepair(),Is.False);
        Assert.That(gun.Health,Is.Zero);Assert.That(m.Scrap,Is.EqualTo(before));
    }
}
