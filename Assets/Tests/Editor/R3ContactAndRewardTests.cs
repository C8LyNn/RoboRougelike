using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RogueMachine.P12PipelineSafe;

public sealed class R3ContactAndRewardTests
{
    private static R1UpgradeDefinition Upgrade(string id)
    {
        var u=ScriptableObject.CreateInstance<R1UpgradeDefinition>();
        u.Id=id;u.Title=id;u.Effect=R1UpgradeEffect.Damage;u.Amount=0.2f;
        return u;
    }

    [Test]
    public void OneBonusIsMandatoryAndIndependentOfThreeSalvagedParts()
    {
        var upgrades=new[]{Upgrade("a"),Upgrade("b"),Upgrade("c")};
        try
        {
            var progress=new R1RunProgress(3);
            Assert.IsTrue(progress.Start());
            Assert.IsTrue(progress.WinEncounter(upgrades,new System.Random(11)));
            Assert.IsTrue(progress.OpenSalvage());
            Assert.IsFalse(progress.CanCompleteUnifiedRewards(Array.Empty<int>()));
            Assert.IsFalse(progress.CanCompleteUnifiedRewards(new[]{0,1}));
            Assert.IsTrue(progress.CanCompleteUnifiedRewards(new[]{1}));

            // Salvage has a separate capacity of three. Neither system deducts
            // capacity from the other one: a single bonus + THREE parts is legal.
            var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Standard));
            var ledger=new R4SalvageLedger(3);
            var items=Enumerable.Range(0,4).Select(i=>new R4SalvageItem(
                "enemy-part-"+i,"wrak",R3PartKind.Wheel,R4WeaponKind.Basic,1f)).ToArray();
            Assert.IsTrue(ledger.Record("test-enemy",items));
            Assert.IsTrue(ledger.Claim(model,items.Take(3).Select(x=>x.Id)));
            Assert.AreEqual(3,model.Inventory.Count);
            Assert.IsTrue(progress.CompleteUnifiedRewards(new[]{1}));
            Assert.AreEqual(1,progress.Acquired.Count);
            Assert.AreEqual(1f,progress.Stats.Damage,0.001f);
        }
        finally {foreach(var u in upgrades)UnityEngine.Object.DestroyImmediate(u);}
    }

    [Test]
    public void FinalVictoriesAlsoProvideThreeBonusChoices()
    {
        var upgrades=new[]{Upgrade("a"),Upgrade("b"),Upgrade("c")};
        try
        {
            var p=new R1RunProgress(3);
            Assert.IsTrue(p.Start());
            for(int i=0;i<3;i++)
            {
                Assert.IsTrue(p.WinEncounter(upgrades,new System.Random(i+1)));
                Assert.AreEqual(3,p.Offers.Count);
                Assert.IsTrue(p.OpenSalvage());
                Assert.IsTrue(p.CanCompleteUnifiedRewards(new[]{2}));
                Assert.IsTrue(p.CompleteUnifiedRewards(new[]{2}));
                if(i<2)Assert.IsTrue(p.LeaveGarage());
                else Assert.AreEqual(R1RunState.Won,p.State);
            }
            Assert.AreEqual(3,p.Acquired.Count);
        }
        finally {foreach(var u in upgrades)UnityEngine.Object.DestroyImmediate(u);}
    }

    [Test]
    public void FaceContactsMustBeReciprocalNotJustAdjacent()
    {
        var core=new R3PartState(Vector3Int.zero,R3PartKind.Core) {
            ContactMask=R3MountingGrid.FaceBit(Vector3Int.right)};
        var next=new R3PartState(Vector3Int.right,R3PartKind.Armor) {
            ContactMask=R3MountingGrid.FaceBit(Vector3Int.left)};
        Assert.IsTrue(R3VehicleModel.Connected(new[]{core,next}));
        next.ContactMask=R3MountingGrid.FaceBit(Vector3Int.right);
        Assert.IsFalse(R3VehicleModel.Connected(new[]{core,next}));
    }

    [Test]
    public void MountRejectsMissingPortPair()
    {
        var design=R3VehicleCatalog.Create(R3VehicleKind.Light);
        var model=new R3VehicleModel(design);
        var bonus=Upgrade("x");
        try
        {
            Assert.IsTrue(model.ReceiveBonuses(new[]{bonus}));
            var item=model.Inventory.Single();
            item.ContactMask=0;
            Assert.IsNotNull(model.PlacementError(new Vector3Int(1,2,0),
                R3PartKind.BonusModule,true,R4WeaponKind.Basic,default,item.ContactMask));
            Assert.IsFalse(model.Mount(item.Id,new Vector3Int(1,2,0)));
        }
        finally {UnityEngine.Object.DestroyImmediate(bonus);}
    }

    [Test]
    public void TurningBonusOnDifferentAxesRotatesItsFourCellFootprint()
    {
        var origin=Vector3Int.zero;
        var flat=R3VehicleModel.OccupiedCells(origin,R3PartKind.BonusModule).ToArray();
        var standing=R3VehicleModel.OccupiedCells(origin,R3PartKind.BonusModule,
            new Vector3Int(0,0,1)).ToArray();
        var yawed=R3VehicleModel.OccupiedCells(origin,R3PartKind.BonusModule,
            new Vector3Int(0,1,0)).ToArray();
        Assert.AreEqual(4,flat.Distinct().Count());
        Assert.AreEqual(4,standing.Distinct().Count());
        Assert.AreEqual(4,yawed.Distinct().Count());
        Assert.AreEqual(1,flat.Select(x=>x.y).Distinct().Count());
        Assert.AreEqual(2,standing.Select(x=>x.y).Distinct().Count());
        Assert.AreEqual(1,yawed.Select(x=>x.y).Distinct().Count());
    }

    [Test]
    public void PortMaskRotatesTogetherWithThePart()
    {
        var p=new R3PartState(Vector3Int.zero,R3PartKind.Gun){
            ContactMask=R3MountingGrid.FaceBit(Vector3Int.right),
            RotationQuarterTurns=new Vector3Int(0,1,0)};
        Assert.IsTrue(R3MountingGrid.PortOnFace(p,Vector3Int.back));
        Assert.IsFalse(R3MountingGrid.PortOnFace(p,Vector3Int.right));
        Assert.AreEqual(new Vector3Int(0,3,0),
            R3MountingGrid.Normalize(new Vector3Int(0,-1,0)));
        var copied=p.Copy();
        Assert.AreEqual(p.RotationQuarterTurns,copied.RotationQuarterTurns);
        Assert.AreEqual(p.ContactMask,copied.ContactMask);
    }

    [Test]
    public void LiveBlockGraphEnforcesContactsAndPreservesInternalSockets()
    {
        var root=new GameObject("port-test");
        var a=new GameObject("a");
        var b=new GameObject("b");
        a.transform.SetParent(root.transform,false);b.transform.SetParent(root.transform,false);
        try
        {
            var ba=a.AddComponent<P12_PipelineSafePlayerBlock>();
            var bb=b.AddComponent<P12_PipelineSafePlayerBlock>();
            var ca=a.AddComponent<R3BlockMountPoints>();
            var cb=b.AddComponent<R3BlockMountPoints>();
            ca.Configure("a",R3MountingGrid.FaceBit(Vector3Int.right));
            cb.Configure("b",R3MountingGrid.FaceBit(Vector3Int.left));
            Assert.IsTrue(R3MountingGrid.Compatible(ba,bb,Vector3Int.right));
            cb.Configure("b",R3MountingGrid.FaceBit(Vector3Int.right));
            Assert.IsFalse(R3MountingGrid.Compatible(ba,bb,Vector3Int.right));
            cb.Configure("a",0);
            Assert.IsTrue(R3MountingGrid.Compatible(ba,bb,Vector3Int.right),
                "Inside a single multivoxel part, cells remain a physical unit.");
        }
        finally {UnityEngine.Object.DestroyImmediate(root);}
    }
}
