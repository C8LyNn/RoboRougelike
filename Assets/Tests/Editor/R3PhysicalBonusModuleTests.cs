using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using RoboRougelike.R1;
using RoboRougelike.R3;

public sealed class R3PhysicalBonusModuleTests
{
    private static R1UpgradeDefinition MakeUpgrade(R1UpgradeEffect effect=R1UpgradeEffect.Damage)
    {
        var upgrade=ScriptableObject.CreateInstance<R1UpgradeDefinition>();
        upgrade.Id="bonus-test";upgrade.Title="Fizyczne wzmocnienie";
        upgrade.Description="Aktywne po montażu";upgrade.Effect=effect;upgrade.Amount=0.25f;
        return upgrade;
    }
    [Test]
    public void RewardIsOneInventoryItemAndDoesNotApplyPermanentStats()
    {
        var upgrade=MakeUpgrade();
        try
        {
            var progress=new R1RunProgress(3);
            Assert.IsTrue(progress.Start());
            var catalog=new[]{upgrade,MakeUpgrade(R1UpgradeEffect.Hull),MakeUpgrade(R1UpgradeEffect.Mobility)};
            try
            {
                catalog[1].Id="hull-test";catalog[2].Id="drive-test";
                Assert.IsTrue(progress.WinEncounter(catalog,new System.Random(7)));
                Assert.IsTrue(progress.OpenSalvage());
                Assert.IsTrue(progress.CompleteUnifiedRewards(new[]{0}));
                Assert.AreEqual(1f,progress.Stats.Damage,0.0001f,
                    "Claiming a bonus must not permanently change player stats.");
                Assert.AreEqual(1,progress.Acquired.Count);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(catalog[1]);
                UnityEngine.Object.DestroyImmediate(catalog[2]);
            }
        }
        finally {UnityEngine.Object.DestroyImmediate(upgrade);}
    }
    [Test]
    public void ModuleOccupiesFourCellsButIsOnePart()
    {
        var upgrade=MakeUpgrade();
        try
        {
            var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Standard));
            var initial=model.Parts.Count;
            Assert.IsTrue(model.ReceiveBonuses(new[]{upgrade}));
            var module=model.Inventory.Single(p=>p.Kind==R3PartKind.BonusModule);
            Assert.AreEqual("bonus-test",module.UpgradeId);
            Assert.AreEqual(1,model.Inventory.Count);
            Assert.AreEqual(initial,model.Parts.Count);
            var anchor=new Vector3Int(1,2,0);
            Assert.IsNull(model.PlacementError(anchor,R3PartKind.BonusModule,true));
            Assert.IsTrue(model.Mount(module.Id,anchor));
            Assert.AreEqual(initial+1,model.Parts.Count);
            Assert.AreEqual(0,model.Inventory.Count);
            foreach(var cell in R3VehicleModel.OccupiedCells(anchor,R3PartKind.BonusModule))
                Assert.AreSame(model.At(anchor),model.At(cell));
            Assert.IsTrue(R3VehicleModel.Connected(model.Parts));
            Assert.IsNotNull(model.PlacementError(new Vector3Int(2,2,1),R3PartKind.Armor));
            Assert.IsFalse(model.Buy(new Vector3Int(3,2,0),R3PartKind.BonusModule));
            Assert.IsTrue(model.Remove(new Vector3Int(2,2,1)),
                "Removing any cell removes the whole module.");
            Assert.AreEqual(initial,model.Parts.Count);
            Assert.AreEqual(1,model.Inventory.Count);
            Assert.AreEqual(module.Id,model.Inventory[0].Id);
        }
        finally {UnityEngine.Object.DestroyImmediate(upgrade);}
    }
    [Test]
    public void FootprintMustFitBoundsAndNotOverlapExistingParts()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Standard));
        Assert.IsNotNull(model.PlacementError(new Vector3Int(6,2,0),R3PartKind.BonusModule,true));
        Assert.IsNotNull(model.PlacementError(new Vector3Int(0,1,0),R3PartKind.BonusModule,true));
        Assert.AreEqual(4,R3VehicleModel.OccupiedCells(new Vector3Int(0,3,0),R3PartKind.BonusModule).Count());
    }
    [Test]
    public void CopyPreservesUpgradeIdentityAndSingleModuleCondition()
    {
        var upgrade=MakeUpgrade(R1UpgradeEffect.Reload);
        try
        {
            var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
            Assert.IsTrue(model.ReceiveBonuses(new[]{upgrade}));
            var part=model.Inventory.Single();
            part.Health=0.4f;
            var copy=model.DeepCopy().Inventory.Single();
            Assert.AreEqual(part.Id,copy.Id);
            Assert.AreEqual(part.UpgradeId,copy.UpgradeId);
            Assert.AreEqual(part.UpgradeEffect,copy.UpgradeEffect);
            Assert.AreEqual(part.UpgradeAmount,copy.UpgradeAmount);
            Assert.AreEqual(part.Health,copy.Health);
        }
        finally {UnityEngine.Object.DestroyImmediate(upgrade);}
    }
}
