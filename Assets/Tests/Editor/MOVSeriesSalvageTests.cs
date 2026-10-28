using System.Linq;
using NUnit.Framework;
using UnityEngine;
using RoboRougelike.R3;
using RoboRougelike.R4;

public class MOVSeriesSalvageTests
{
    [TestCase(R3PartKind.Booster)] [TestCase(R3PartKind.JumpDrive)] [TestCase(R3PartKind.Blink)]
    [TestCase(R3PartKind.Gyroscope)] [TestCase(R3PartKind.OmniDrive)] [TestCase(R3PartKind.MagneticGrapple)]
    [TestCase(R3PartKind.HoverEngine)] [TestCase(R3PartKind.CrawlerTrack)]
    public void ExistingSalvageLedgerRecoversRealKindHealthIdThenMountsAndDismantlesOnce(R3PartKind kind)
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));var ledger=new R4SalvageLedger(1);
        string id="original-mov-"+kind;
        Assert.That(ledger.Record("enemy",new[]{new R4SalvageItem(id,"Encounter",kind,R4WeaponKind.Basic,.6f),new R4SalvageItem(id+"-lost","Encounter",kind,R4WeaponKind.Basic,0)}),Is.True);
        Assert.That(ledger.Items.Single(p=>p.Id==id+"-lost").Selectable,Is.True,"W2 also allows retaining a wreck for paid rebuilding.");
        Assert.That(ledger.Claim(model,new[]{id}),Is.True);
        var item=model.Inventory.Single();Assert.That(item.Id,Is.EqualTo(id));Assert.That(item.Kind,Is.EqualTo(kind));Assert.That(item.Health,Is.EqualTo(.6f));
        Assert.That(model.Mount(id,new Vector3Int(-1,0,3)),Is.True);Assert.That(model.Mount(id,new Vector3Int(1,0,3)),Is.False);
        Assert.That(model.Remove(new Vector3Int(-1,0,3)),Is.True);int expected=model.DismantleValue(model.Inventory.Single()),before=model.Scrap;
        Assert.That(model.Dismantle(id),Is.True);Assert.That(model.Scrap,Is.EqualTo(before+expected));Assert.That(model.Dismantle(id),Is.False);
        Assert.That(ledger.Claim(model,new[]{id}),Is.False);Assert.That(model.Inventory,Is.Empty);
        var wrecks=new R4SalvageLedger(1);
        Assert.That(wrecks.Record("other-enemy",new[]{new R4SalvageItem(id+"-wreck","Encounter",kind,R4WeaponKind.Basic,0)}),Is.True);
        Assert.That(wrecks.Claim(model,new[]{id+"-wreck"}),Is.True);Assert.That(model.Inventory.Single().Health,Is.Zero);
        Assert.That(model.Mount(id+"-wreck",new Vector3Int(-1,0,3)),Is.False);
        int fullPrice=kind==R3PartKind.Booster||kind==R3PartKind.HoverEngine?35:
            kind==R3PartKind.Blink?60:kind==R3PartKind.Gyroscope?30:kind==R3PartKind.OmniDrive?40:45;
        int rebuildPrice=Mathf.CeilToInt(fullPrice*.35f);
        Assert.That(model.RepairCost(model.Inventory.Single()),Is.EqualTo(rebuildPrice));model.Award(fullPrice);before=model.Scrap;
        Assert.That(model.RepairInventory(id+"-wreck"),Is.True);Assert.That(model.Scrap,Is.EqualTo(before-rebuildPrice));
        Assert.That(model.Inventory.Single().Health,Is.EqualTo(1));Assert.That(model.RepairInventory(id+"-wreck"),Is.False);
        Assert.That(model.Mount(id+"-wreck",new Vector3Int(-1,0,3)),Is.True);Assert.That(wrecks.Claim(model,new[]{id+"-wreck"}),Is.False);
    }
}
