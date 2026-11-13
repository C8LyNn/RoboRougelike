using System.Linq;
using System.Reflection;
using NUnit.Framework;
using RoboRougelike.R3;
using RoboRougelike.R4;
using UnityEngine;

public sealed class BacklogConstructionBudgetTests
{
    [Test] public void PhysicalPartsHaveConfigurableWeightedCosts()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        var limit=typeof(R3VehicleModel).GetProperty("ConstructionBudgetLimit");
        var cost=typeof(R3VehicleModel).GetMethod("ConstructionPartCost",new[]{typeof(R3PartState)});
        Assert.That(limit,Is.Not.Null,"A vehicle must expose its current construction budget.");
        Assert.That(cost,Is.Not.Null,"Each physical part must have an explicit construction cost.");
        Assert.That(limit.GetValue(model),Is.EqualTo(200));
        Assert.That(cost.Invoke(model,new object[]{new R3PartState(default,R3PartKind.Armor)}),Is.EqualTo(2));
        Assert.That(cost.Invoke(model,new object[]{new R3PartState(default,R3PartKind.Armor){ArmorShape=R3ArmorShape.HalfBlock}}),Is.EqualTo(1));
        Assert.That((int)cost.Invoke(model,new object[]{new R3PartState(default,R3PartKind.Gun){Weapon=R4WeaponKind.Basic}}),Is.InRange(5,10));
    }
    [Test] public void AllTwelveStartingVehiclesFitTheBudgetAndRemainConnected()
    {
        var used=typeof(R3VehicleModel).GetProperty("ConstructionBudgetUsed");
        var limit=typeof(R3VehicleModel).GetProperty("ConstructionBudgetLimit");
        Assert.That(used,Is.Not.Null);Assert.That(limit,Is.Not.Null);
        foreach(R3VehicleKind kind in System.Enum.GetValues(typeof(R3VehicleKind)))
            for(int variant=0;variant<4;variant++)
            {
                var model=new R3VehicleModel(R3VehicleCatalog.Create(kind,variant));
                Assert.That((long)used.GetValue(model),Is.LessThanOrEqualTo((int)limit.GetValue(model)),kind+" "+variant);
                Assert.That((long)used.GetValue(model),Is.LessThanOrEqualTo((int)limit.GetValue(model)-20),kind+" "+variant+" needs space for initial upgrades");
                Assert.That(R3VehicleModel.Connected(model.Parts),Is.True,kind+" "+variant);
                Assert.That(model.Parts.Count(p=>p.Kind==R3PartKind.Gun),Is.EqualTo(kind==R3VehicleKind.Light?1:kind==R3VehicleKind.Standard?2:3),kind+" "+variant);
                Assert.That(model.Parts.Count(p=>p.Kind==R3PartKind.Wheel),Is.EqualTo(kind==R3VehicleKind.Light?(variant%2==0?4:6):kind==R3VehicleKind.Heavy?(variant==3?10:8):variant==2?8:6),kind+" "+variant);
                Assert.That(model.Parts.Any(p=>p.Kind==R3PartKind.Battery),Is.True,kind+" "+variant);
                Assert.That(model.Parts.Any(p=>p.Kind==R3PartKind.DrivePower),Is.True,kind+" "+variant);
                Assert.That(model.CanDeploy,Is.True,kind+" "+variant);
            }
    }
    [Test] public void BudgetCountsEachPhysicalPartAndBlocksOnlyAnOverBudgetMount()
    {
        var design=new R3VehicleDesign{Name="Budget fixture"};
        design.Parts.Add(new R3PartState(Vector3Int.zero,R3PartKind.Core));
        design.Parts.Add(new R3PartState(Vector3Int.right,R3PartKind.Armor));
        var economy=new R3GarageEconomy{Construction=new R3ConstructionSettings{StartingBudget=8}};
        var model=new R3VehicleModel(design,economy);model.Award(100);
        Assert.That(model.ConstructionBudgetUsed,Is.EqualTo(4));
        Assert.That(model.PurchaseToInventory(R3PartKind.Battery),Is.True);
        Assert.That(model.ConstructionBudgetUsed,Is.EqualTo(4),"Inventory is not mounted construction.");
        Assert.That(model.Buy(new Vector3Int(2,0,0),R3PartKind.Armor),Is.True);
        Assert.That(model.Buy(new Vector3Int(3,0,0),R3PartKind.Armor),Is.True);
        Assert.That(model.ConstructionBudgetUsed,Is.EqualTo(8));
        var balance=model.Scrap;
        Assert.That(model.Buy(new Vector3Int(4,0,0),R3PartKind.Armor),Is.False);
        Assert.That(model.PlacementError(new Vector3Int(4,0,0),R3PartKind.Armor),Does.Contain("Budżet"));
        Assert.That(model.Scrap,Is.EqualTo(balance));
        model.At(new Vector3Int(3,0,0)).Health=0;
        Assert.That(model.ConstructionBudgetUsed,Is.EqualTo(8),"A wreck plan still occupies its budget.");
        Assert.That(model.DeepCopy().ConstructionBudgetLimit,Is.EqualTo(8));
    }
    [Test] public void BudgetConfigurationAndCheckpointRemainCompatible()
    {
        var settings=new R3ConstructionSettings{StartingBudget=150,BlockCost=3,HalfBlockCost=1,WeaponCost=9,WeaponTierTwoCost=36,TierTwoMultiplier=4};
        var design=new R3VehicleDesign{Name="Budget save"};design.Parts.Add(new R3PartState(Vector3Int.zero,R3PartKind.Core));
        var model=new R3VehicleModel(design,new R3GarageEconomy{Construction=settings});
        Assert.That(model.ConstructionPartCost(new R3PartState(default,R3PartKind.Gun){Tier=2}),Is.EqualTo(36));
        var checkpoint=model.MakeCheckpoint(0);
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(checkpoint,R3VehicleKind.Light,model.Economy,0,out var restored,out var error),Is.True,error);
        Assert.That(restored.ConstructionBudgetLimit,Is.EqualTo(150));
        Assert.That(restored.ConstructionBudgetUsed,Is.EqualTo(3));
        checkpoint.constructionBudgetVersion=0;checkpoint.constructionBudgetLimit=0;
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(checkpoint,R3VehicleKind.Light,model.Economy,0,out restored,out error),Is.True,error);
        Assert.That(restored.ConstructionBudgetLimit,Is.EqualTo(150),"A legacy save uses its configured starting budget.");
    }
}
