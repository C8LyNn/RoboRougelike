using System.Linq;
using NUnit.Framework;
using UnityEngine;
using RoboRougelike.R3;
using RoboRougelike.R4;

public class R4SalvageModelTests
{
    private R3VehicleModel Model()=>new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Standard));
    private R4SalvageItem Item(string id,float hp=1)=>new R4SalvageItem(id,"run/1/scout/gun",R3PartKind.Gun,R4WeaponKind.Automatic,hp);
    [Test] public void MissedPhysicalLootRequiresPaymentAndCannotAlsoBecomeAFreeReward()
    {
        var model=Model();var ledger=new R4SalvageLedger(2);
        Assert.That(ledger.Record("enemy",new[]{Item("missed",.4f),Item("reward",.7f)},R3EnemyArchetype.Scout),Is.True);
        var mark=typeof(R4SalvageLedger).GetMethod("MarkPhysicalDrop");var recover=typeof(R4SalvageLedger).GetMethod("TryRecoverMissed");
        Assert.That(mark,Is.Not.Null,"A spawned drop must be tracked separately from ordinary rewards");Assert.That(recover,Is.Not.Null);
        Assert.That((bool)mark.Invoke(ledger,new object[]{"missed"}),Is.True);
        Assert.That(ledger.CreateBasketOffers(2,0).SelectMany(b=>b.Items).Any(p=>p.Id=="missed"),Is.False);
        Assert.That(ledger.Claim(model,new[]{"missed"}),Is.False,"Missed drops cannot be redeemed for free");
        Assert.That((bool)recover.Invoke(ledger,new object[]{model,"missed"}),Is.False,"Insufficient scrap must not award the item");
        Assert.That(model.Inventory,Is.Empty);model.Award(20);
        Assert.That((bool)recover.Invoke(ledger,new object[]{model,"missed"}),Is.True);
        Assert.That(model.Scrap,Is.EqualTo(10));Assert.That(model.Inventory.Single(p=>p.Id=="missed").Health,Is.EqualTo(.4f));
        Assert.That((bool)recover.Invoke(ledger,new object[]{model,"missed"}),Is.False,"Recovery cannot be repeated");
        Assert.That(ledger.Claim(model,new[]{"reward"}),Is.True);Assert.That(model.Inventory.Count(p=>p.Id=="missed"),Is.EqualTo(1));
    }
    [Test] public void MissedWheelRecoveryRefundsPaymentWhenFullPackageDoesNotFit()
    {
        var model=Model();model.Award(10000);
        while(model.Inventory.Count+model.GrantQuantity(R3PartKind.Armor)<=model.AvailableInventoryCapacity)
            Assert.That(model.PurchaseToInventory(R3PartKind.Armor),Is.True);
        var ledger=new R4SalvageLedger();var wheel=new R4SalvageItem("missed-wheel","enemy",R3PartKind.Wheel,R4WeaponKind.Basic,.5f);
        Assert.That(ledger.Record("enemy",new[]{wheel}),Is.True);Assert.That(ledger.MarkPhysicalDrop(wheel.Id),Is.True);
        int before=model.Scrap;Assert.That(ledger.TryRecoverMissed(model,wheel.Id),Is.False);
        Assert.That(model.Scrap,Is.EqualTo(before));Assert.That(ledger.MissedPhysicalItems.Any(p=>p.Id==wheel.Id),Is.True);
        Assert.That(model.Dismantle(model.Inventory[0].Id),Is.True);Assert.That(model.Dismantle(model.Inventory[0].Id),Is.True);
        before=model.Scrap;Assert.That(ledger.TryRecoverMissed(model,wheel.Id),Is.True);
        Assert.That(model.Scrap,Is.EqualTo(before-model.Economy.MissedLootRecoveryCost));
        Assert.That(model.Inventory.Count(p=>p.Kind==R3PartKind.Wheel),Is.EqualTo(2));
    }
    [Test] public void MissedLootPriceUsesConfigAndSurvivesModelCopy()
    {
        var model=Model();model.Economy.MissedLootRecoveryCost=17;var copy=model.DeepCopy();
        Assert.That(copy.Economy.MissedLootRecoveryCost,Is.EqualTo(17));copy.Award(20);
        var ledger=new R4SalvageLedger();Assert.That(ledger.Record("enemy",new[]{Item("priced")}),Is.True);
        Assert.That(ledger.MarkPhysicalDrop("priced"),Is.True);Assert.That(ledger.TryRecoverMissed(copy,"priced"),Is.True);
        Assert.That(copy.Scrap,Is.EqualTo(3));
    }
    [Test] public void PhysicalPickupPreservesIdentityAndCannotAlsoBeClaimedOrDismantledAsReward()
    {
        var model=Model();var ledger=new R4SalvageLedger(2);
        Assert.That(ledger.Record("enemy",new[]{Item("pickup",.4f),Item("reward",.7f)},R3EnemyArchetype.Scout),Is.True);
        var collect=typeof(R4SalvageLedger).GetMethod("TryCollectPhysical");
        Assert.That(collect,Is.Not.Null,"The physical pickup must settle against the canonical ledger");
        Assert.That((bool)collect.Invoke(ledger,new object[]{model,"pickup"}),Is.True);
        Assert.That(model.Inventory.Single(p=>p.Id=="pickup").Health,Is.EqualTo(.4f));
        Assert.That((bool)collect.Invoke(ledger,new object[]{model,"pickup"}),Is.False);
        Assert.That(ledger.Items.Any(p=>p.Id=="pickup"),Is.False);
        Assert.That(ledger.CreateBasketOffers(1,0).SelectMany(b=>b.Items).Any(p=>p.Id=="pickup"),Is.False);
        Assert.That(ledger.Claim(model,new[]{"pickup"}),Is.False);
        Assert.That(ledger.Claim(model,new[]{"reward"}),Is.True);
        Assert.That(model.Inventory.Count(p=>p.Id=="pickup"),Is.EqualTo(1));
        Assert.That(model.Scrap,Is.Zero,"A collected item must not also become scrap");
    }
    [Test] public void PhysicalPickupRemainsConsumedEvenAfterPlayerDismantlesIt()
    {
        var model=Model();var ledger=new R4SalvageLedger(1);
        Assert.That(ledger.Record("enemy",new[]{Item("world-drop")},R3EnemyArchetype.Scout),Is.True);
        Assert.That(ledger.TryCollectPhysical(model,"world-drop"),Is.True);
        Assert.That(model.Dismantle("world-drop"),Is.True);
        int before=model.Scrap;
        Assert.That(ledger.TryCollectPhysical(model,"world-drop"),Is.False);
        Assert.That(ledger.Claim(model,new[]{"world-drop"}),Is.False);
        Assert.That(ledger.Claim(model,System.Array.Empty<string>()),Is.True);
        Assert.That(model.Scrap,Is.EqualTo(before));
    }
    [Test] public void WheelPickupWaitsForSpaceForWholePackageWithoutConsumingDrop()
    {
        var model=Model();model.Award(10000);
        while(model.Inventory.Count+model.GrantQuantity(R3PartKind.Armor)<=model.AvailableInventoryCapacity)
            Assert.That(model.PurchaseToInventory(R3PartKind.Armor),Is.True);
        Assert.That(model.Inventory.Count,Is.EqualTo(model.AvailableInventoryCapacity));
        var ledger=new R4SalvageLedger();
        var wheel=new R4SalvageItem("wheel-drop","enemy/wheel",R3PartKind.Wheel,R4WeaponKind.Basic,.6f);
        Assert.That(ledger.Record("enemy",new[]{wheel}),Is.True);
        Assert.That(ledger.TryCollectPhysical(model,wheel.Id),Is.False);
        Assert.That(ledger.Items.Any(p=>p.Id==wheel.Id),Is.True);
        Assert.That(model.Dismantle(model.Inventory[0].Id),Is.True);
        Assert.That(ledger.TryCollectPhysical(model,wheel.Id),Is.False,"One free slot is insufficient for two wheels");
        Assert.That(model.Dismantle(model.Inventory[0].Id),Is.True);
        Assert.That(ledger.TryCollectPhysical(model,wheel.Id),Is.True);
        Assert.That(model.Inventory.Count(p=>p.Kind==R3PartKind.Wheel),Is.EqualTo(2));
        Assert.That(model.Inventory.Single(p=>p.Id==wheel.Id).Health,Is.EqualTo(.6f));
        Assert.That(ledger.Items.Any(p=>p.Id==wheel.Id),Is.False);
    }
    [Test] public void PartDroppedBeforeEnemyDeathIsRecordedOnceAfterFullWreckCapture()
    {
        var model=Model();var ledger=new R4SalvageLedger(2);var early=Item("early-gun",0);var later=Item("later-gun",.8f);
        var record=typeof(R4SalvageLedger).GetMethod("RecordDroppedPart");
        Assert.That(record,Is.Not.Null,"Destroyed parts need a canonical early-drop transaction");
        Assert.That((bool)record.Invoke(ledger,new object[]{"enemy",early}),Is.True);
        Assert.That(ledger.TryCollectPhysical(model,"early-gun"),Is.True);
        Assert.That(ledger.Record("other",new[]{early},R3EnemyArchetype.Gunner),Is.False,"Another enemy cannot reuse the identity");
        Assert.That(ledger.Record("enemy",new[]{early,later},R3EnemyArchetype.Gunner),Is.True);
        Assert.That(ledger.Items.Count(p=>p.Id=="early-gun"),Is.Zero);
        Assert.That(ledger.Items.Count(p=>p.Id=="later-gun"),Is.EqualTo(1));
        Assert.That(model.Inventory.Count(p=>p.Id=="early-gun"),Is.EqualTo(1));
        Assert.That(ledger.CreateBasketOffers(2,0).SelectMany(p=>p.Items).Any(p=>p.Id=="early-gun"),Is.False);
    }
    [Test] public void ClaimedEnemyPartsArriveHealthyWithoutChangingSnapshotsAmmoScrapOrOwnDamage()
    {
        var model=Model();var own=model.Parts.First(p=>p.Kind==R3PartKind.Armor);own.Health=.4f;
        int repair=model.RepairCost(own);Assert.That(repair,Is.GreaterThan(0));
        var snapshot=new R3PartState(default,R3PartKind.Gun,.25f){Id="enemy-gun",Source="enemy/gun",Weapon=R4WeaponKind.Railgun,
            Ammo=4,RotationQuarterTurns=new Vector3Int(0,1,0),ContactMask=R3MountingGrid.AllFaces,
            DeviceReadyAt=12,ReloadReadyAt=10,DisruptionRemaining=2,ThermalHeat=.35f,ThermalLocked=true};
        var ledger=new R4SalvageLedger(2);
        Assert.That(ledger.Record("enemy",new[]{new R4SalvageItem(snapshot,.25f),Item("destroyed",0),Item("unselected",.5f)}),Is.True);
        int preview=ledger.PreviewScrap(model,new[]{"enemy-gun","destroyed"});
        Assert.That(ledger.Claim(model,new[]{"enemy-gun","destroyed"}),Is.True);
        var gun=model.Inventory.Single(p=>p.Id=="enemy-gun");
        Assert.That(gun.Health,Is.EqualTo(1));Assert.That(gun.Ammo,Is.EqualTo(4));Assert.That(gun.Weapon,Is.EqualTo(R4WeaponKind.Railgun));
        Assert.That(gun.Source,Is.EqualTo(snapshot.Source));Assert.That(gun.RotationQuarterTurns,Is.EqualTo(snapshot.RotationQuarterTurns));
        Assert.That(gun.ContactMask,Is.EqualTo(snapshot.ContactMask));Assert.That(model.Inventory.Single(p=>p.Id=="destroyed").Health,Is.EqualTo(1));
        Assert.That(gun.DeviceReadyAt,Is.EqualTo(12));Assert.That(gun.ReloadReadyAt,Is.EqualTo(10));
        Assert.That(gun.DisruptionRemaining,Is.EqualTo(2));Assert.That(gun.ThermalHeat,Is.EqualTo(.35f));Assert.That(gun.ThermalLocked,Is.True);
        Assert.That(snapshot.Health,Is.EqualTo(.25f));Assert.That(ledger.Items.Single(p=>p.Id=="enemy-gun").Health,Is.EqualTo(.25f));
        Assert.That(model.Scrap,Is.EqualTo(preview));Assert.That(own.Health,Is.EqualTo(.4f));Assert.That(model.RepairCost(own),Is.EqualTo(repair));
        Assert.That(ledger.Claim(model,new[]{"unselected"}),Is.False,"Odbiór i zapłata nie mogą się powtórzyć.");
    }
    [Test] public void SnapshotLimitAndAtomicClaimRestoreRewardsAndRejectReplay()
    {
        var model=Model();var ledger=new R4SalvageLedger(2);
        Assert.That(ledger.Record("enemy",new[]{Item("a",0.4f),Item("b"),Item("c"),Item("dead",0)}),Is.True);
        Assert.That(ledger.Record("enemy",new[]{Item("d")}),Is.False);
        Assert.That(ledger.Claim(model,new[]{"a","b","c"}),Is.False);
        Assert.That(ledger.Items.Single(p=>p.Id=="dead").Selectable,Is.True,"Destroyed enemy parts can be recovered healthy.");
        Assert.That(ledger.Claim(model,new[]{"a","a"}),Is.False);
        Assert.That(model.Scrap,Is.Zero);Assert.That(model.Inventory,Is.Empty);
        Assert.That(ledger.Claim(model,new[]{"a","b"}),Is.True);
        Assert.That(model.Inventory.Single(p=>p.Id=="a").Health,Is.EqualTo(1));
        Assert.That(model.Inventory.Single(p=>p.Id=="a").Source,Is.EqualTo("run/1/scout/gun"));
        Assert.That(model.Scrap,Is.EqualTo(10)); // healthy automatic 9 + destroyed wreck 1
        Assert.That(ledger.Claim(model,new[]{"c"}),Is.False);Assert.That(model.Scrap,Is.EqualTo(10));
        Assert.That(ledger.Record("late",new[]{Item("late")}),Is.False);
    }
    [Test] public void FreeMountRemovalRepairAndDismantleAreAtomicAndCannotPrintScrap()
    {
        var model=Model();var ledger=new R4SalvageLedger();ledger.Record("enemy",new[]{Item("gun",0.4f)});
        ledger.Claim(model,new[]{"gun"});var cell=new Vector3Int(0,2,-2);
        Assert.That(model.Mount("gun",new Vector3Int(6,5,7)),Is.False);
        Assert.That(model.Mount("gun",cell),Is.True);Assert.That(model.Scrap,Is.Zero);
        Assert.That(model.At(cell).Health,Is.EqualTo(1));Assert.That(model.Inventory,Is.Empty);
        Assert.That(model.Remove(cell),Is.True);Assert.That(model.Inventory.Single().Id,Is.EqualTo("gun"));
        model.Inventory.Single().Health=.4f; // Damage after ownership still needs paid repair.
        Assert.That(model.RepairInventory("gun"),Is.False);Assert.That(model.Inventory.Single().Health,Is.EqualTo(0.4f));
        model.Award(8);Assert.That(model.RepairInventory("gun"),Is.True);Assert.That(model.Scrap,Is.Zero);
        Assert.That(model.Inventory.Single().Health,Is.EqualTo(1));
        Assert.That(model.Dismantle("gun"),Is.True);Assert.That(model.Scrap,Is.EqualTo(9));
        Assert.That(model.Dismantle("gun"),Is.False);Assert.That(model.Mount("gun",cell),Is.False);
        foreach(var kind in new[]{R4WeaponKind.Basic,R4WeaponKind.Automatic,R4WeaponKind.Heavy})
        {
            model.Award(model.Price(R3PartKind.Gun,kind));int before=model.Scrap;
            Assert.That(model.Buy(cell,R3PartKind.Gun,kind),Is.True,$"Purchase {kind}: {model.PlacementError(cell,R3PartKind.Gun,weapon:kind)}");string id=model.At(cell).Id;
            Assert.That(model.Remove(cell),Is.True);Assert.That(model.Dismantle(id),Is.True);
            Assert.That(model.Scrap,Is.LessThan(before));
        }
    }
    [Test] public void RunOwnershipAndLedgerIdsDoNotSurviveNewModel()
    {
        var model=Model();var ledger=new R4SalvageLedger(1);ledger.Record("enemy",new[]{Item("gun")});ledger.Claim(model,new[]{"gun"});
        var fresh=Model();Assert.That(fresh.Inventory,Is.Empty);Assert.That(fresh.Scrap,Is.Zero);
        Assert.That(fresh.Parts.Select(p=>p.Id).Intersect(model.Parts.Select(p=>p.Id)),Is.Empty);
        var duplicate=new R4SalvageLedger();duplicate.Record("enemy",new[]{Item("gun")});
        Assert.That(duplicate.Claim(model,new[]{"gun"}),Is.False);Assert.That(model.Inventory.Count,Is.EqualTo(1));
    }
}
