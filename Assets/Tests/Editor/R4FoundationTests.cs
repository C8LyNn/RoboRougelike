using System.Collections;
using NUnit.Framework;
using UnityEngine;
using RoboRougelike.R3;

public class R4FoundationTests
{
    [Test] public void RemovingOwnedLivePartReturnsItToRunInventory()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Standard));model.Award(5);
        var cell=new Vector3Int(0,2,1);Assert.That(model.Buy(cell,R3PartKind.Armor),Is.True);
        model.At(cell).Health=0.4f;Assert.That(model.Remove(cell),Is.True);
        var property=typeof(R3VehicleModel).GetProperty("Inventory");
        Assert.That(property,Is.Not.Null,"Zdjęta żywa część musi trafić do magazynu wyprawy.");
        var inventory=(IList)property.GetValue(model);
        Assert.That(inventory.Count,Is.EqualTo(1));
        Assert.That(((R3PartState)inventory[0]).Health,Is.EqualTo(0.4f));
        Assert.That(model.Scrap,Is.Zero);
    }
}
