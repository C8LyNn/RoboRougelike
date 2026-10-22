using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4;

public sealed class RRLW34RewardTests
{
    readonly List<R1UpgradeDefinition> created=new List<R1UpgradeDefinition>();
    R1UpgradeDefinition[] Catalog()
    {
        var effects=new[]{R1UpgradeEffect.Damage,R1UpgradeEffect.Hull,R1UpgradeEffect.Mobility};
        return effects.Select((e,i)=>{var u=ScriptableObject.CreateInstance<R1UpgradeDefinition>();
            u.Id="w34-"+i;u.Title=u.Id;u.Effect=e;u.Amount=.25f;created.Add(u);return u;}).ToArray();
    }
    [TearDown] public void Cleanup(){foreach(var u in created)UnityEngine.Object.DestroyImmediate(u);created.Clear();}
    R1RunProgress Rewards()
    {var p=new R1RunProgress();p.Start();Assert.That(p.WinEncounter(Catalog(),new System.Random(7)),Is.True);Assert.That(p.OpenSalvage(),Is.True);return p;}
    static R3VehicleModel Model()=>new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Standard));
    static R4SalvageLedger Ledger()
    {
        var l=new R4SalvageLedger(3);
        Assert.That(l.Record("enemy",new[]{new R4SalvageItem("p0","foe/left",R3PartKind.Wheel,0,1),
            new R4SalvageItem("p1","foe/gun",R3PartKind.Gun,0,0),new R4SalvageItem("p2","foe/right",R3PartKind.Wheel,0,.5f),
            new R4SalvageItem("p3","foe/back",R3PartKind.Wheel,0,1),new R4SalvageItem("armor","foe/hull",R3PartKind.Armor,0,1)}),Is.True);
        return l;
    }
    static string Snapshot(R3VehicleModel m)=>m.Scrap+"|"+string.Join(";",m.Parts.Concat(m.Inventory).Select(p=>JsonUtility.ToJson(p)));
    [TestCase(0,9)][TestCase(1,6)][TestCase(2,5)][TestCase(3,4)]
    public void OnePhysicalBonusAndIndependentPartsSettleExactlyOnce(int count,int scrap)
    {
        var p=Rewards();var l=Ledger();var m=Model();var ids=new[]{"p0","p1","p2"}.Take(count).ToArray();
        Assert.That(R4RewardSettlement.TryClaim(p,l,m,ids,new[]{0}),Is.True,"One bonus must not consume any of the three salvage slots.");
        Assert.That(m.Inventory.Count,Is.EqualTo(count+1));Assert.That(m.Inventory.Count(x=>x.Kind.ToString()=="BonusModule"),Is.EqualTo(1));
        Assert.That(m.Inventory.Where(x=>x.Kind.ToString()!="BonusModule").Select(x=>x.Id),Is.EquivalentTo(ids));
        Assert.That(m.Inventory.Select(x=>x.Id).Distinct().Count(),Is.EqualTo(count+1));
        Assert.That(m.Scrap,Is.EqualTo(scrap));Assert.That(l.DismantledScrap,Is.EqualTo(scrap));Assert.That(l.Closed,Is.True);
        Assert.That(p.Stats.Damage,Is.EqualTo(1));Assert.That(p.Stats.Hull,Is.EqualTo(1));Assert.That(p.Stats.Mobility,Is.EqualTo(1));
        Assert.That(p.Acquired.Count,Is.EqualTo(1));Assert.That(p.State,Is.EqualTo(R1RunState.Garage));
        if(count>=2)Assert.That(m.Inventory.Single(x=>x.Id=="p1").Health,Is.Zero);
        string before=Snapshot(m);Assert.That(R4RewardSettlement.TryClaim(p,l,m,ids,new[]{0}),Is.False);Assert.That(Snapshot(m),Is.EqualTo(before));Assert.That(p.Acquired.Count,Is.EqualTo(1));
    }
    [TestCase(0)][TestCase(2)][TestCase(3)]
    public void RequiredExactlyOneBonusRejectsWithoutAnySalvageMutation(int bonusCount)
    {
        var p=Rewards();var l=Ledger();var m=Model();string before=Snapshot(m);
        Assert.That(R4RewardSettlement.TryClaim(p,l,m,Array.Empty<string>(),Enumerable.Range(0,bonusCount)),Is.False);
        Assert.That(l.Closed,Is.False);Assert.That(l.DismantledScrap,Is.Zero);Assert.That(Snapshot(m),Is.EqualTo(before));
        Assert.That(p.State,Is.EqualTo(R1RunState.Salvage));Assert.That(p.Offers.Count,Is.EqualTo(3));Assert.That(p.Acquired,Is.Empty);
    }
    [TestCase("nan")][TestCase("infinite")][TestCase("effect")]
    public void InvalidPhysicalBonusCannotPartiallyPayOrClaimSalvage(string invalid)
    {
        var p=Rewards();var l=Ledger();var m=Model();
        if(invalid=="nan")p.Offers[0].Amount=float.NaN;else if(invalid=="infinite")p.Offers[0].Amount=float.PositiveInfinity;else p.Offers[0].Effect=(R1UpgradeEffect)999;
        string before=Snapshot(m);
        Assert.That(R4RewardSettlement.TryClaim(p,l,m,new[]{"p0"},new[]{0}),Is.False);
        Assert.That(Snapshot(m),Is.EqualTo(before));Assert.That(l.Closed,Is.False);Assert.That(l.DismantledScrap,Is.Zero);
        Assert.That(p.State,Is.EqualTo(R1RunState.Salvage));Assert.That(p.Acquired,Is.Empty);
    }
    [TestCase(196,true)][TestCase(197,false)]
    public void InventoryCapacityPreflightIncludesBonusAndAllThreeParts(int occupied,bool allowed)
    {
        var p=Rewards();var l=Ledger();var m=Model();
        Assert.That(m.ReceiveSalvage(Enumerable.Range(0,occupied).Select(i=>new R3PartState(default,R3PartKind.Wheel){Id="stored-"+i})),Is.True);
        string before=Snapshot(m);
        Assert.That(R4RewardSettlement.TryClaim(p,l,m,new[]{"p0","p1","p2"},new[]{0}),Is.EqualTo(allowed));
        if(allowed){Assert.That(m.Inventory.Count,Is.EqualTo(200));Assert.That(m.Scrap,Is.EqualTo(4));}
        else{Assert.That(Snapshot(m),Is.EqualTo(before));Assert.That(l.Closed,Is.False);Assert.That(p.State,Is.EqualTo(R1RunState.Salvage));}
    }
    [Test] public void FinalEncounterAlsoAwardsOneInactiveModuleAndThreePartsBeforeWon()
    {
        var p=new R1RunProgress();p.Start();var m=Model();
        for(int i=0;i<3;i++)
        {
            Assert.That(p.WinEncounter(Catalog(),new System.Random(i)),Is.True);Assert.That(p.Offers.Count,Is.EqualTo(3));Assert.That(p.OpenSalvage(),Is.True);
            var l=Ledger();var ids=i==2?new[]{"p0","p1","p2"}:Array.Empty<string>();
            Assert.That(R4RewardSettlement.TryClaim(p,l,m,ids,new[]{0}),Is.True);
            if(i<2)Assert.That(p.LeaveGarage(),Is.True);
        }
        Assert.That(p.State,Is.EqualTo(R1RunState.Won));Assert.That(m.Inventory.Count,Is.EqualTo(6));
        Assert.That(m.Inventory.Count(x=>x.Kind.ToString()=="BonusModule"),Is.EqualTo(3));Assert.That(p.Stats.Damage,Is.EqualTo(1));
    }
}
