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
        var model=new R3VehicleModel(design,economy);model.Award(200); // isolate construction budget from shop prices
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
    [Test] public void EighteenWheelsCanBeMountedWhenBudgetAndSpaceAllow()
    {
        var design=new R3VehicleDesign{Name="Many wheels"};
        design.Parts.Add(new R3PartState(Vector3Int.zero,R3PartKind.Core));
        for(int x=-1;x<=1;x++)for(int z=-4;z<=4;z++)
            if(x!=0||z!=0)design.Parts.Add(new R3PartState(new Vector3Int(x,0,z),R3PartKind.Armor));
        var model=new R3VehicleModel(design,new R3GarageEconomy{Construction=new R3ConstructionSettings{StartingBudget=1000}});model.Award(2000);
        foreach(int side in new[]{-1,1})for(int z=-4;z<=4;z++)
        {
            var cell=new Vector3Int(side*2,0,z);
            Assert.That(model.Buy(cell,R3PartKind.Wheel),Is.True,$"{cell}: {model.PlacementError(cell,R3PartKind.Wheel)}");
        }
        Assert.That(model.Parts.Count(p=>p.Kind==R3PartKind.Wheel),Is.EqualTo(18));
        Assert.That(model.ConstructionBudgetUsed,Is.LessThanOrEqualTo(model.ConstructionBudgetLimit));
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
    [Test] public void MilestonesIncreaseBudgetOnceAndSurviveCloneAndCheckpoint()
    {
        var method=typeof(R3VehicleModel).GetMethod("ApplyCompletedBudgetMilestones");
        Assert.That(method,Is.Not.Null,"Budget must grow when major stages are completed.");
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        method.Invoke(model,new object[]{1});Assert.That(model.ConstructionBudgetLimit,Is.EqualTo(200));
        method.Invoke(model,new object[]{2});Assert.That(model.ConstructionBudgetLimit,Is.EqualTo(220));
        method.Invoke(model,new object[]{2});Assert.That(model.ConstructionBudgetLimit,Is.EqualTo(220));
        var clone=model.DeepCopy();method.Invoke(clone,new object[]{2});Assert.That(clone.ConstructionBudgetLimit,Is.EqualTo(220));
        var data=JsonUtility.FromJson<R3ModelCheckpoint>(JsonUtility.ToJson(model.MakeCheckpoint(0)));
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(data,R3VehicleKind.Light,model.Economy,0,out var restored,out var error),Is.True,error);
        method.Invoke(restored,new object[]{2});Assert.That(restored.ConstructionBudgetLimit,Is.EqualTo(220));
        method.Invoke(restored,new object[]{4});Assert.That(restored.ConstructionBudgetLimit,Is.EqualTo(240));
    }
    [Test] public void SizeClassDependsOnPhysicalCountRatherThanPartTypeOrTier()
    {
        var count=typeof(R3VehicleModel).GetProperty("PhysicalPartCount");
        var size=typeof(R3VehicleModel).GetProperty("ConstructionSizeClass");
        Assert.That(count,Is.Not.Null);Assert.That(size,Is.Not.Null);
        var design=new R3VehicleDesign{Name="Size"};design.Parts.Add(new R3PartState(Vector3Int.zero,R3PartKind.Core));
        design.Parts.Add(new R3PartState(Vector3Int.right,R3PartKind.Armor));
        var model=new R3VehicleModel(design);
        var baseline=size.GetValue(model);Assert.That(count.GetValue(model),Is.EqualTo(2));
        model.Parts[1].Kind=R3PartKind.Battery;model.Parts[1].Tier=3;
        Assert.That(size.GetValue(model),Is.EqualTo(baseline));Assert.That(count.GetValue(model),Is.EqualTo(2));
        model.Parts[1].Health=0;Assert.That(count.GetValue(model),Is.EqualTo(1));
        model.Economy.Construction.SmallPhysicalParts=1;model.Economy.Construction.MediumPhysicalParts=2;
        Assert.That(model.ConstructionSizeClass,Is.EqualTo(R3ConstructionSize.Small));
        model.Parts[1].Health=1;Assert.That(model.ConstructionSizeClass,Is.EqualTo(R3ConstructionSize.Medium));
        model.Parts.Add(new R3PartState(Vector3Int.up,R3PartKind.Armor));
        Assert.That(model.ConstructionSizeClass,Is.EqualTo(R3ConstructionSize.Large));
    }
    [Test] public void MilestoneCadenceAndAmountAreConfigurableAndLegacyCanCatchUp()
    {
        var settings=new R3ConstructionSettings{StartingBudget=100,StageInterval=3,StageBudgetIncrease=17};
        var design=new R3VehicleDesign{Name="Progress"};design.Parts.Add(new R3PartState(Vector3Int.zero,R3PartKind.Core));
        var model=new R3VehicleModel(design,new R3GarageEconomy{Construction=settings});
        Assert.That(model.ApplyCompletedBudgetMilestones(2),Is.EqualTo(0));
        Assert.That(model.ApplyCompletedBudgetMilestones(6),Is.EqualTo(34));
        Assert.That(model.ConstructionBudgetLimit,Is.EqualTo(134));
        var old=model.MakeCheckpoint(0);old.constructionBudgetVersion=0;old.constructionBudgetMilestonesApplied=0;
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(old,R3VehicleKind.Light,model.Economy,0,out var restored,out var error),Is.True,error);
        Assert.That(restored.ApplyCompletedBudgetMilestones(6),Is.EqualTo(34));
        Assert.That(restored.ApplyCompletedBudgetMilestones(6),Is.Zero);
    }
}
