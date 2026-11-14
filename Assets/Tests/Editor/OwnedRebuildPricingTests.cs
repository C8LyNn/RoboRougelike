using System.Linq;
using NUnit.Framework;
using RoboRougelike.R3;
using RoboRougelike.R4;
using UnityEngine;

public sealed class OwnedRebuildPricingTests
{
    [TestCase(R3PartKind.Wheel)][TestCase(R3PartKind.OmniDrive)][TestCase(R3PartKind.HoverEngine)]
    [TestCase(R3PartKind.CrawlerTrack)][TestCase(R3PartKind.Booster)][TestCase(R3PartKind.JumpDrive)]
    [TestCase(R3PartKind.DrivePower)][TestCase(R3PartKind.Blink)][TestCase(R3PartKind.MagneticGrapple)]
    public void DrivePacksAreGrantedOnceByShopAndRewardsButNotByFusion(R3PartKind kind)
    {
        var model=Model();model.Award(1000);Assert.That(model.PurchaseToInventory(kind),Is.True);
        Assert.That(model.Inventory.Count,Is.EqualTo(2),"One shop offer gives two physical parts.");
        var loot=new R3PartState(default,kind){Id="recovered-drive"};var ledger=new R4SalvageLedger(3);
        Assert.That(ledger.Record("enemy",new[]{new R4SalvageItem(loot,1)}),Is.True);
        Assert.That(ledger.Claim(model,new[]{loot.Id}),Is.True);
        Assert.That(model.Inventory.Count,Is.EqualTo(4),"A recovered drive also gives one pair.");
        Assert.That(model.Inventory.Select(p=>p.Id).Distinct().Count(),Is.EqualTo(4));
        Assert.That(model.FuseInventory(model.Inventory.Take(3).Select(p=>p.Id)),Is.True);
        Assert.That(model.Inventory.Count,Is.EqualTo(2),"Fusion makes one part, not another reward pack.");
        Assert.That(model.Inventory.Count(p=>p.Tier==2),Is.EqualTo(1));
        var checkpoint=JsonUtility.FromJson<R3ModelCheckpoint>(JsonUtility.ToJson(model.MakeCheckpoint(0)));
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(checkpoint,R3VehicleKind.Light,model.Economy,0,out var restored,out var error),Is.True,error);
        Assert.That(restored.Inventory.Count(p=>p.Kind==kind&&p.Tier==2),Is.EqualTo(1));
    }
    [TestCase(R3PartKind.Blink)][TestCase(R3PartKind.MagneticGrapple)]
    public void ActiveMovementPickupGivesOneConfiguredPackWithOneOriginalId(R3PartKind kind)
    {
        var model=Model();model.Economy.DriveGrantQuantity=3;
        var part=new R3PartState(default,kind,.6f){Id="active-movement-drop"};
        var ledger=new R4SalvageLedger(3);
        Assert.That(ledger.Record("enemy",new[]{new R4SalvageItem(part,.6f)}),Is.True);
        Assert.That(ledger.MarkPhysicalDrop(part.Id),Is.True);
        Assert.That(ledger.TryCollectPhysical(model,part.Id),Is.True);
        Assert.That(model.Inventory.Count,Is.EqualTo(3));
        Assert.That(model.Inventory.Count(p=>p.Id==part.Id),Is.EqualTo(1));
        Assert.That(model.Inventory.Select(p=>p.Id).Distinct().Count(),Is.EqualTo(3));
        Assert.That(model.Inventory.All(p=>p.Kind==kind&&p.Health==.6f),Is.True);
        Assert.That(ledger.TryCollectPhysical(model,part.Id),Is.False);
    }
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
    [Test] public void StorePurchasesAwardConfiguredBundlesForOneListedPrice()
    {
        var model=Model();model.Award(1000);
        int price=model.Price(R3PartKind.Armor),before=model.Scrap;
        Assert.That(model.PurchaseToInventory(R3PartKind.Armor),Is.True);
        Assert.That(model.Inventory.Count(p=>p.Kind==R3PartKind.Armor),Is.EqualTo(5));
        Assert.That(model.Scrap,Is.EqualTo(before-price));
        price=model.Price(R3PartKind.Wheel);before=model.Scrap;
        Assert.That(model.PurchaseToInventory(R3PartKind.Wheel),Is.True);
        Assert.That(model.Inventory.Count(p=>p.Kind==R3PartKind.Wheel),Is.EqualTo(2));
        Assert.That(model.Scrap,Is.EqualTo(before-price));
        Assert.That(model.Inventory.Select(p=>p.Id).Distinct().Count(),Is.EqualTo(7));
        Assert.That(model.PurchaseToInventory(R3PartKind.Gun),Is.True);
        Assert.That(model.Inventory.Count(p=>p.Kind==R3PartKind.Gun),Is.EqualTo(1));
    }
    [Test] public void BundleCapacityFailureIsAtomic()
    {
        var model=Model();model.Award(1000);
        Assert.That(model.ReceiveSalvage(Enumerable.Range(0,199).Select(_=>new R3PartState(default,R3PartKind.Gun))),Is.True);
        int scrap=model.Scrap;Assert.That(model.PurchaseToInventory(R3PartKind.Wheel),Is.False);
        Assert.That(model.Scrap,Is.EqualTo(scrap));Assert.That(model.Inventory.Count,Is.EqualTo(199));
    }
    [Test] public void BundleTransferDoesNotMultiplyPartsAgain()
    {
        var model=Model();model.Award(1000);Assert.That(model.PurchaseToInventory(R3PartKind.Wheel),Is.True);
        var wheel=model.Inventory.First();string id=wheel.Id;
        var cell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
            .First(c=>model.OwnedPlacementError(id,c,default)==null);
        Assert.That(model.Mount(id,cell),Is.True);Assert.That(model.Inventory.Count,Is.EqualTo(1));
        Assert.That(model.Remove(cell),Is.True);Assert.That(model.Inventory.Count,Is.EqualTo(2));
        Assert.That(model.Inventory.Count(p=>p.Id==id),Is.EqualTo(1));
    }
    [Test] public void DirectConstructionPlacesOneAndStoresTheOtherFour()
    {
        var model=Model();model.Award(1000);
        var cell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
            .First(c=>model.PlacementError(c,R3PartKind.Armor)==null);
        int price=model.Price(R3PartKind.Armor),before=model.Scrap;
        Assert.That(model.Buy(cell,R3PartKind.Armor),Is.True);
        Assert.That(model.At(cell),Is.Not.Null);Assert.That(model.Inventory.Count(p=>p.Kind==R3PartKind.Armor),Is.EqualTo(4));
        Assert.That(model.Scrap,Is.EqualTo(before-price));
        Assert.That(model.Inventory.Select(p=>p.Id).Append(model.At(cell).Id).Distinct().Count(),Is.EqualTo(5));
    }
    [Test] public void BundleQuantitiesAreConfigurableAndDoNotMultiplyWedges()
    {
        var model=Model();model.Award(1000);model.Economy.DriveGrantQuantity=3;model.Economy.BlockGrantQuantity=7;
        Assert.That(model.GrantQuantity(R3PartKind.Armor,R3ArmorShape.Wedge),Is.EqualTo(1));
        Assert.That(model.PurchaseToInventory(R3PartKind.Wheel),Is.True);
        Assert.That(model.Inventory.Count,Is.EqualTo(3));
        Assert.That(model.DeepCopy().GrantQuantity(R3PartKind.Armor),Is.EqualTo(7));
    }
    [Test] public void RecoveredWheelAwardsTwoUniquePartsOnlyOnce()
    {
        var model=Model();var wheel=new R3PartState(default,R3PartKind.Wheel){Id="physical-wheel-1"};
        var ledger=new R4SalvageLedger(3);
        Assert.That(ledger.Record("enemy",new[]{new R4SalvageItem(wheel,1)}),Is.True);
        Assert.That(ledger.Claim(model,new[]{wheel.Id}),Is.True);
        Assert.That(model.Inventory.Count,Is.EqualTo(2));
        Assert.That(model.Inventory.Any(p=>p.Id==wheel.Id),Is.True);
        Assert.That(model.Inventory.Select(p=>p.Id).Distinct().Count(),Is.EqualTo(2));
        Assert.That(ledger.Claim(model,new[]{wheel.Id}),Is.False);Assert.That(model.Inventory.Count,Is.EqualTo(2));
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
