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
        Assert.That(ledger.Claim(model,new[]{id+"-lost"}),Is.False);Assert.That(ledger.Claim(model,new[]{id}),Is.True);
        var item=model.Inventory.Single();Assert.That(item.Id,Is.EqualTo(id));Assert.That(item.Kind,Is.EqualTo(kind));Assert.That(item.Health,Is.EqualTo(.6f));
        Assert.That(model.Mount(id,new Vector3Int(-1,0,3)),Is.True);Assert.That(model.Mount(id,new Vector3Int(1,0,3)),Is.False);
        Assert.That(model.Remove(new Vector3Int(-1,0,3)),Is.True);int expected=model.DismantleValue(model.Inventory.Single()),before=model.Scrap;
        Assert.That(model.Dismantle(id),Is.True);Assert.That(model.Scrap,Is.EqualTo(before+expected));Assert.That(model.Dismantle(id),Is.False);
        Assert.That(ledger.Claim(model,new[]{id}),Is.False);Assert.That(model.Inventory,Is.Empty);
    }
}
