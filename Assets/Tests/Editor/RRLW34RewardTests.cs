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
    [Test] public void SalvageOptimizerStacksOnlyWhileMountedAndAliveAndPaysPreviewedAmount()
    {
        Assert.That(Enum.TryParse("SalvageOptimizer",out R3PartKind kind),Is.True);
        var m=Model();var l=Ledger();
        var a=new R3PartState(new Vector3Int(20,0,0),kind){Health=1};
        var b=new R3PartState(new Vector3Int(21,0,0),kind){Health=.01f};
        m.Parts.Add(a);m.Parts.Add(b);
        Assert.That(R3VehicleModel.IsPassiveEnergyPart(kind),Is.True);
        Assert.That(R3VehicleModel.IsPassiveSlotPart(kind),Is.True);
        Assert.That(l.PreviewScrap(m,Array.Empty<string>()),Is.EqualTo(13));
        a.Health=0;Assert.That(l.PreviewScrap(m,Array.Empty<string>()),Is.EqualTo(11));
        b.Health=0;Assert.That(l.PreviewScrap(m,Array.Empty<string>()),Is.EqualTo(9));
        a.Health=1;Assert.That(l.PreviewScrap(m,Array.Empty<string>()),Is.EqualTo(11));
        Assert.That(l.Claim(m,Array.Empty<string>()),Is.True);
        Assert.That(l.DismantledScrap,Is.EqualTo(11));Assert.That(m.Scrap,Is.EqualTo(11));
    }
    [Test] public void SalvageOptimizerUsesConfiguredBonusAndTierWithoutChangingDismantlePrice()
    {
        var m=Model();var l=Ledger();m.Economy.SalvageOptimizerBonus=.5f;
        var part=new R3PartState(new Vector3Int(20,0,0),R3PartKind.SalvageOptimizer){Tier=2};
        m.Parts.Add(part);
        var reward=l.Items.First().ToPart();int basePrice=m.DismantleValue(reward);
        Assert.That(l.PreviewScrap(m,Array.Empty<string>()),Is.EqualTo(18));
        Assert.That(m.DismantleValue(reward),Is.EqualTo(basePrice));
        Assert.That(m.DeepCopy().Economy.SalvageOptimizerBonus,Is.EqualTo(.5f));
        m.Economy.SalvageOptimizerBonus=float.NaN;
        Assert.That(l.PreviewScrap(m,Array.Empty<string>()),Is.EqualTo(9));
    }
    [TestCase(0,9)][TestCase(99995,5)][TestCase(100000,0)]
    public void ScrapPreviewAndLedgerReportOnlyAmountActuallyCredited(int initial,int expected)
    {
        var model=Model();var ledger=Ledger();model.Award(initial);
        int nominal=ledger.PreviewScrap(model,Array.Empty<string>());
        Assert.That(nominal,Is.EqualTo(9));
        Assert.That(model.PreviewAward(nominal),Is.EqualTo(expected));
        int before=model.Scrap;
        Assert.That(ledger.Claim(model,Array.Empty<string>()),Is.True);
        Assert.That(model.Scrap-before,Is.EqualTo(expected));
        Assert.That(ledger.DismantledScrap,Is.EqualTo(expected));
    }
    [TestCase(0)][TestCase(99999)][TestCase(100000)]
    public void DismantlePreviewMatchesWalletChangeAtTheLimit(int initial)
    {
        var model=Model();var part=new R3PartState(default,R3PartKind.Gun){Id="scrap-cap-part"};
        Assert.That(model.ReceiveSalvage(new[]{part}),Is.True);model.Award(initial);
        int before=model.Scrap;int preview=model.PreviewAward(model.DismantleValue(part));
        Assert.That(model.Dismantle(part.Id),Is.True);
        Assert.That(model.Scrap-before,Is.EqualTo(preview));
    }
    [TestCase(0,9)][TestCase(1,6)][TestCase(2,5)][TestCase(3,4)]
    public void OnePhysicalBonusAndIndependentPartsSettleExactlyOnce(int count,int scrap)
    {
        var p=Rewards();var l=Ledger();var m=Model();var ids=new[]{"p0","p1","p2"}.Take(count).ToArray();
        Assert.That(R4RewardSettlement.TryClaim(p,l,m,ids,new[]{0}),Is.True,"One bonus must not consume any of the three salvage slots.");
        int extras=(count>=1?1:0)+(count>=3?1:0);
        Assert.That(m.Inventory.Count,Is.EqualTo(count+extras+1));Assert.That(m.Inventory.Count(x=>x.Kind.ToString()=="BonusModule"),Is.EqualTo(1));
        foreach(string id in ids)Assert.That(m.Inventory.Any(x=>x.Id==id),Is.True,"Recovered originals retain identity.");
        Assert.That(m.Inventory.Select(x=>x.Id).Distinct().Count(),Is.EqualTo(count+extras+1));
        Assert.That(m.Scrap,Is.EqualTo(scrap));Assert.That(l.DismantledScrap,Is.EqualTo(scrap));Assert.That(l.Closed,Is.True);
        Assert.That(p.Stats.Damage,Is.EqualTo(1));Assert.That(p.Stats.Hull,Is.EqualTo(1));Assert.That(p.Stats.Mobility,Is.EqualTo(1));
        Assert.That(p.Acquired.Count,Is.EqualTo(1));Assert.That(p.State,Is.EqualTo(R1RunState.Garage));
        if(count>=2)Assert.That(m.Inventory.Single(x=>x.Id=="p1").Health,Is.EqualTo(1));
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
    [TestCase(194,true)][TestCase(195,false)]
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
        Assert.That(p.State,Is.EqualTo(R1RunState.Won));Assert.That(m.Inventory.Count,Is.EqualTo(8));
        Assert.That(m.Inventory.Count(x=>x.Kind.ToString()=="BonusModule"),Is.EqualTo(3));Assert.That(p.Stats.Damage,Is.EqualTo(1));
    }
}
