using System.Linq;
using NUnit.Framework;
using RoboRougelike.R3;
using RoboRougelike.R4;
using UnityEngine;

public sealed class InvalidWeaponTransactionTests
{
    static R3VehicleModel Model() { var m = new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light)); m.Award(100); return m; }
    static R3PartState Gun(R4WeaponKind weapon, float hp=0.5f) => new R3PartState(Vector3Int.zero,R3PartKind.Gun,hp){Weapon=weapon};
    [Test] public void RejectsMixedInvalidSalvageWithoutGrantingOwnership()
    {
        var model=Model(); var good=Gun(R4WeaponKind.Basic); var bad=Gun((R4WeaponKind)1234567); var scrap=model.Scrap;
        Assert.That(model.ReceiveSalvage(new[]{good,bad}),Is.False);
        Assert.That(model.Owns(good.Id),Is.False); Assert.That(model.Owns(bad.Id),Is.False);
        Assert.That(model.Inventory,Is.Empty); Assert.That(model.Scrap,Is.EqualTo(scrap));
    }
    [Test] public void RejectsRepairAndDismantleOfCorruptedOwnedGunWithoutMutation()
    {
        var model=Model();var gun=Gun(R4WeaponKind.Basic);Assert.That(model.ReceiveSalvage(new[]{gun}),Is.True);
        var owned=model.Inventory.Single();owned.Weapon=(R4WeaponKind)(-1);var scrap=model.Scrap;var hp=owned.Health;var id=owned.Id;
        Assert.That(()=>model.RepairInventory(id),Throws.Nothing);Assert.That(model.RepairInventory(id),Is.False);
        Assert.That(()=>model.Dismantle(id),Throws.Nothing);Assert.That(model.Dismantle(id),Is.False);
        Assert.That(model.Owns(id),Is.True);Assert.That(model.Inventory.Single().Id,Is.EqualTo(id));
        Assert.That(owned.Health,Is.EqualTo(hp));Assert.That(model.Scrap,Is.EqualTo(scrap));
    }
    [Test] public void LedgerRejectsMixedInvalidEquipmentAtomically()
    {
        var ledger=new R4SalvageLedger();var good=Gun(R4WeaponKind.Basic);var bad=Gun((R4WeaponKind)999);
        Assert.That(ledger.Record("enemy",new[]{new R4SalvageItem(good,1),new R4SalvageItem(bad,1)}),Is.False);
        Assert.That(ledger.Items,Is.Empty);
        Assert.That(ledger.Record("enemy",new[]{new R4SalvageItem(good,1)}),Is.True);
    }
    [Test] public void HealthyValidTransactionsPreserveGuidAndChargeOnce()
    {
        var model=Model();var gun=Gun(R4WeaponKind.Automatic);Assert.That(model.ReceiveSalvage(new[]{gun}),Is.True);
        var item=model.Inventory.Single();var id=item.Id;var cost=model.RepairCost(item);var before=model.Scrap;
        Assert.That(model.RepairInventory(id),Is.True);Assert.That(item.Health,Is.EqualTo(1));Assert.That(model.Scrap,Is.EqualTo(before-cost));
        var credit=model.DismantleValue(item);Assert.That(model.Dismantle(id),Is.True);Assert.That(model.Owns(id),Is.False);
        Assert.That(model.Scrap,Is.EqualTo(before-cost+credit));Assert.That(model.Dismantle(id),Is.False);
    }
    [Test] public void UnaffordableRepairLeavesOwnershipHealthAndWalletIntact()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        var gun=Gun(R4WeaponKind.Automatic);Assert.That(model.ReceiveSalvage(new[]{gun}),Is.True);
        Assert.That(model.RepairInventory(gun.Id),Is.False);Assert.That(model.Scrap,Is.Zero);
        Assert.That(model.Owns(gun.Id),Is.True);Assert.That(model.Inventory.Single().Health,Is.EqualTo(gun.Health));
    }
}
