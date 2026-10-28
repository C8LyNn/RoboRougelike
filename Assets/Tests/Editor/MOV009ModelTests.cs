using System.Linq;
using NUnit.Framework;
using UnityEngine;
using RoboRougelike.R3;

public class MOV009ModelTests
{
    public const R3PartKind Suspension=(R3PartKind)4;
    public static Vector3Int[] Cells(R3VehicleKind kind)
    {
        int w=kind==R3VehicleKind.Light?1:kind==R3VehicleKind.Heavy?3:2;
        int z=kind==R3VehicleKind.Light?3:kind==R3VehicleKind.Heavy?5:4;
        return new[]{new Vector3Int(-w,0,-z),new Vector3Int(w,0,-z),new Vector3Int(-w,0,z),new Vector3Int(w,0,z)};
    }
    [Test] public void SuspensionPurchaseUsesOwnPriceAndMountingRules()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));model.Award(100);
        Assert.That(model.Buy(Cells(R3VehicleKind.Light)[0],Suspension),Is.True);
        Assert.That(model.Scrap,Is.EqualTo(75),"Suspension must not fall through to the armor price.");
        Assert.That(model.Buy(Cells(R3VehicleKind.Light)[0],Suspension),Is.False);
        Assert.That(model.Scrap,Is.EqualTo(75));Assert.That(model.CanDeploy,Is.True);
    }
    [Test] public void SuspensionRepairRemoveAndRemountPreserveIdentityWithoutFreeItems()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));model.Award(100);
        var cell=Cells(R3VehicleKind.Light)[0];Assert.That(model.Buy(cell,Suspension),Is.True);
        var part=model.At(cell);string id=part.Id;part.Health=.4f;
        Assert.That(model.Repair(cell),Is.True);Assert.That(part.Health,Is.EqualTo(1));
        int money=model.Scrap;Assert.That(model.Remove(cell),Is.True);Assert.That(model.Inventory.Single().Id,Is.EqualTo(id));
        Assert.That(model.Mount(id,cell),Is.True);Assert.That(model.Inventory,Is.Empty);
        Assert.That(model.Mount(id,cell),Is.False);Assert.That(model.Scrap,Is.EqualTo(money));
        Assert.That(model.At(cell).Id,Is.EqualTo(id));
        model.At(cell).Health=0;Assert.That(model.RepairCost(model.At(cell)),Is.EqualTo(9));
    }
    [Test] public void NinthSuspensionCannotConsumeMoneyOrCreateAnItem()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));model.Award(1000);
        for(int z=3;z<7;z++)foreach(int x in new[]{-1,1})Assert.That(model.Buy(new Vector3Int(x,0,z),Suspension),Is.True);
        Assert.That(model.Buy(new Vector3Int(-1,0,7),Suspension),Is.False);
        Assert.That(model.Parts.Count(p=>p.Kind==Suspension),Is.EqualTo(8));Assert.That(model.Scrap,Is.EqualTo(800));
    }
}
