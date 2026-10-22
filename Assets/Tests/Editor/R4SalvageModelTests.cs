using System.Linq;
using NUnit.Framework;
using UnityEngine;
using RoboRougelike.R3;
using RoboRougelike.R4;

public class R4SalvageModelTests
{
    private R3VehicleModel Model()=>new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Standard));
    private R4SalvageItem Item(string id,float hp=1)=>new R4SalvageItem(id,"run/1/scout/gun",R3PartKind.Gun,R4WeaponKind.Automatic,hp);
    [Test] public void SnapshotLimitAndAtomicClaimPreserveHpAndRejectReplay()
    {
        var model=Model();var ledger=new R4SalvageLedger(2);
        Assert.That(ledger.Record("enemy",new[]{Item("a",0.4f),Item("b"),Item("c"),Item("dead",0)}),Is.True);
        Assert.That(ledger.Record("enemy",new[]{Item("d")}),Is.False);
        Assert.That(ledger.Claim(model,new[]{"a","b","c"}),Is.False);
        Assert.That(ledger.Items.Single(p=>p.Id=="dead").Selectable,Is.True,"Destroyed modules are eligible rebuild plans.");
        Assert.That(ledger.Claim(model,new[]{"a","a"}),Is.False);
        Assert.That(model.Scrap,Is.Zero);Assert.That(model.Inventory,Is.Empty);
        Assert.That(ledger.Claim(model,new[]{"a","b"}),Is.True);
        Assert.That(model.Inventory.Single(p=>p.Id=="a").Health,Is.EqualTo(0.4f));
        Assert.That(model.Inventory.Single(p=>p.Id=="a").Source,Is.EqualTo("run/1/scout/gun"));
        Assert.That(model.Scrap,Is.EqualTo(10)); // healthy automatic 9 + destroyed wreck 1
        Assert.That(ledger.Claim(model,new[]{"c"}),Is.False);Assert.That(model.Scrap,Is.EqualTo(10));
        Assert.That(ledger.Record("late",new[]{Item("late")}),Is.False);
    }
    [Test] public void FreeMountRemovalRepairAndDismantleAreAtomicAndCannotPrintScrap()
    {
        var model=Model();var ledger=new R4SalvageLedger();ledger.Record("enemy",new[]{Item("gun",0.4f)});
        ledger.Claim(model,new[]{"gun"});var cell=new Vector3Int(1,2,0);
        Assert.That(model.Mount("gun",new Vector3Int(6,5,7)),Is.False);
        Assert.That(model.Mount("gun",cell),Is.True);Assert.That(model.Scrap,Is.Zero);
        Assert.That(model.At(cell).Health,Is.EqualTo(0.4f));Assert.That(model.Inventory,Is.Empty);
        Assert.That(model.Remove(cell),Is.True);Assert.That(model.Inventory.Single().Id,Is.EqualTo("gun"));
        Assert.That(model.RepairInventory("gun"),Is.False);Assert.That(model.Inventory.Single().Health,Is.EqualTo(0.4f));
        model.Award(8);Assert.That(model.RepairInventory("gun"),Is.True);Assert.That(model.Scrap,Is.Zero);
        Assert.That(model.Inventory.Single().Health,Is.EqualTo(1));
        Assert.That(model.Dismantle("gun"),Is.True);Assert.That(model.Scrap,Is.EqualTo(9));
        Assert.That(model.Dismantle("gun"),Is.False);Assert.That(model.Mount("gun",cell),Is.False);
        foreach(var kind in new[]{R4WeaponKind.Basic,R4WeaponKind.Automatic,R4WeaponKind.Heavy})
        {
            model.Award(100);int before=model.Scrap;
            Assert.That(model.Buy(cell,R3PartKind.Gun,kind),Is.True);string id=model.At(cell).Id;
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
