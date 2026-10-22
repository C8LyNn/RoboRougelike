using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.R5;

public class RRLW2ModelTests
{
    static readonly System.Collections.Generic.List<R1UpgradeDefinition> created=new System.Collections.Generic.List<R1UpgradeDefinition>();
    [TearDown] public void Cleanup(){foreach(var u in created)UnityEngine.Object.DestroyImmediate(u);created.Clear();}
    static R3VehicleModel Model()
    {
        var d=new R3VehicleDesign();
        d.Parts.Add(new R3PartState(Vector3Int.zero,R3PartKind.Core));
        d.Parts.Add(new R3PartState(Vector3Int.up,R3PartKind.Gun));
        d.Parts.Add(new R3PartState(Vector3Int.left,R3PartKind.Wheel));
        d.Parts.Add(new R3PartState(Vector3Int.right,R3PartKind.Wheel));
        return new R3VehicleModel(d);
    }
    static R1UpgradeDefinition Upgrade(string id,R1UpgradeEffect effect)
    {var u=ScriptableObject.CreateInstance<R1UpgradeDefinition>();u.Id=id;u.Title=id;u.Effect=effect;u.Amount=.2f;created.Add(u);return u;}
    static R1UpgradeDefinition[] Upgrades()=>new[]{Upgrade("damage",R1UpgradeEffect.Damage),Upgrade("hull",R1UpgradeEffect.Hull),Upgrade("core",R1UpgradeEffect.Core)};
    static R1RunProgress Rewards(bool final=false)
    {
        var p=new R1RunProgress();p.Start();
        if(final)for(int i=0;i<2;i++){p.WinEncounter(Upgrades(),new System.Random(1));p.OpenSalvage();p.CompleteUnifiedRewards(Array.Empty<int>());p.LeaveGarage();}
        p.WinEncounter(Upgrades(),new System.Random(1));p.OpenSalvage();return p;
    }
    static R4SalvageLedger Ledger()
    {
        var l=new R4SalvageLedger(3);
        l.Record("foe",new[]{new R4SalvageItem("intact","foe/left",R3PartKind.Wheel,0,1),
            new R4SalvageItem("damaged","foe/right",R3PartKind.Gun,0,.5f),
            new R4SalvageItem("destroyed","foe/top",R3PartKind.Gun,0,0),
            new R4SalvageItem("armor","foe/hull",R3PartKind.Armor,0,1)});
        return l;
    }
    static bool Claim(R1RunProgress p,R4SalvageLedger l,R3VehicleModel m,string[] parts,int[] bonuses)
    {
        var type=typeof(R4SalvageLedger).Assembly.GetType("RoboRougelike.R4.R4RewardSettlement");
        Assert.That(type,Is.Not.Null,"Live controller needs one shared validated settlement boundary.");
        return (bool)type.GetMethod("TryClaim").Invoke(null,new object[]{p,l,m,parts,bonuses});
    }
    [TestCase(0,0)][TestCase(0,1)][TestCase(0,2)][TestCase(0,3)]
    [TestCase(1,0)][TestCase(1,1)][TestCase(1,2)][TestCase(2,0)][TestCase(2,1)][TestCase(3,0)]
    public void EveryRewardCompositionSettlesOnceAndAdvancesDirectlyToGarage(int bonuses,int parts)
    {
        var p=Rewards();var l=Ledger();var m=Model();
        var ids=new[]{"intact","damaged","destroyed"}.Take(parts).ToArray();
        int expectedScrap=parts==0?8:parts==1?5:parts==2?2:1; // wheel3 + half gun3 + wreck1 + armor1
        var picked=p.Offers.Take(bonuses).Select(u=>u.Id).ToArray();
        Assert.That(Claim(p,l,m,ids,Enumerable.Range(0,bonuses).ToArray()),Is.True);
        Assert.That(p.State,Is.EqualTo(R1RunState.Garage));Assert.That(p.Acquired.Count,Is.EqualTo(bonuses));
        Assert.That(p.Stats.Damage,Is.EqualTo(picked.Contains("damage")?1.2f:1));
        Assert.That(p.Stats.Hull,Is.EqualTo(picked.Contains("hull")?1.2f:1));
        Assert.That(p.Stats.Core,Is.EqualTo(picked.Contains("core")?1.2f:1));
        Assert.That(m.Inventory.Select(x=>x.Id),Is.EquivalentTo(ids));Assert.That(m.Scrap,Is.EqualTo(expectedScrap));
        if(parts==3)Assert.That(m.Inventory.Single(x=>x.Id=="destroyed").Health,Is.Zero);
        var stats=p.Stats.Copy();
        Assert.That(Claim(p,l,m,ids,Enumerable.Range(0,bonuses).ToArray()),Is.False);
        Assert.That(m.Scrap,Is.EqualTo(expectedScrap));Assert.That(p.Stats.Damage,Is.EqualTo(stats.Damage));
        Assert.That(p.Stats.Hull,Is.EqualTo(stats.Hull));Assert.That(p.Stats.Core,Is.EqualTo(stats.Core));
    }
    [TestCase("four")][TestCase("duplicate-part")][TestCase("duplicate-bonus")][TestCase("armor")][TestCase("invalid")]
    public void InvalidSelectionDoesNotPartiallyAward(string mistake)
    {
        var p=Rewards();var l=Ledger();var m=Model();
        var ids=mistake=="four"?new[]{"intact","damaged"}:mistake=="duplicate-part"?new[]{"intact","intact"}:mistake=="armor"?new[]{"armor"}:mistake=="invalid"?new[]{"missing"}:Array.Empty<string>();
        var bonuses=mistake=="four"?new[]{0,1}:mistake=="duplicate-bonus"?new[]{0,0}:Array.Empty<int>();
        Assert.That(Claim(p,l,m,ids,bonuses),Is.False);
        Assert.That(p.State,Is.EqualTo(R1RunState.Salvage));Assert.That(p.Acquired,Is.Empty);
        Assert.That(m.Scrap,Is.Zero);Assert.That(m.Inventory,Is.Empty);Assert.That(l.Closed,Is.False);
    }
    [Test] public void FinalRewardsResolveToWonWithNoBonusOrGarage()
    {
        var p=Rewards(true);var l=Ledger();var m=Model();
        Assert.That(p.Offers,Is.Empty);Assert.That(Claim(p,l,m,Array.Empty<string>(),new[]{0}),Is.False);
        Assert.That(Claim(p,l,m,new[]{"destroyed"},Array.Empty<int>()),Is.True);
        Assert.That(p.State,Is.EqualTo(R1RunState.Won));Assert.That(p.LeaveGarage(),Is.False);
    }
    [TestCase(0f,30)][TestCase(.01f,11)][TestCase(.25f,8)][TestCase(.5f,6)][TestCase(.99f,1)][TestCase(1f,0)]
    public void RepairQuotesUseRealPriceAndCondition(float hp,int price)
    {
        var m=Model();m.Parts[1].Health=hp;Assert.That(m.RepairCost(m.Parts[1]),Is.EqualTo(price));
    }
    [Test] public void BarelyDamagedPartStillCostsOneScrap()
    {var m=Model();m.Parts[1].Health=.999999f;Assert.That(m.RepairCost(m.Parts[1]),Is.EqualTo(1));}
    [Test] public void DestroyedInventoryCannotMountUntilPaidRebuildAndCannotChargeTwice()
    {
        var m=Model();Assert.That(m.ReceiveSalvage(new[]{new R3PartState(default,R3PartKind.Gun,0){Id="wreck"}}),Is.True);
        Assert.That(m.Mount("wreck",Vector3Int.forward),Is.False);m.Award(29);
        Assert.That(m.RepairInventory("wreck"),Is.False);Assert.That(m.Scrap,Is.EqualTo(29));
        m.Award(1);Assert.That(m.RepairInventory("wreck"),Is.True);Assert.That(m.Scrap,Is.Zero);
        Assert.That(m.RepairInventory("wreck"),Is.False);Assert.That(m.Inventory.Single().Health,Is.EqualTo(1));
        Assert.That(m.Mount("wreck",Vector3Int.forward),Is.True);
    }
    [Test] public void EmergencyRebuildCannotBeFreeOrPartiallySpendWhenUnaffordable()
    {
        var m=Model();m.Parts[1].Health=0;m.Parts[2].Health=0;m.Award(44);
        Assert.That(m.EmergencyRepair(),Is.False,"Full rebuild of gun30 + wheel15 needs45 scrap.");
        Assert.That(m.Scrap,Is.EqualTo(44));Assert.That(m.Parts[1].Health,Is.Zero);Assert.That(m.Parts[2].Health,Is.Zero);
        m.Award(1);Assert.That(m.EmergencyRepair(),Is.True);Assert.That(m.Scrap,Is.Zero);
        Assert.That(m.Parts[1].Health,Is.EqualTo(1));Assert.That(m.Parts[2].Health,Is.EqualTo(1));
        Assert.That(m.EmergencyRepair(),Is.False);
    }
    [Test] public void EmergencyAndBulkQuotesMatchActualDeduction()
    {
        var m=Model();m.Parts[1].Health=0;m.Parts[2].Health=0;m.Award(50);
        var property=typeof(R3VehicleModel).GetProperty("EmergencyRepairCost");Assert.That(property,Is.Not.Null);
        Assert.That(property.GetValue(m),Is.EqualTo(45));m.EmergencyRepair();Assert.That(m.Scrap,Is.EqualTo(5));
        m.Parts[1].Health=.5f;m.Parts[2].Health=.5f;
        var bulk=typeof(R3VehicleModel).GetProperty("AffordableRepairCost");Assert.That(bulk,Is.Not.Null);
        Assert.That(bulk.GetValue(m),Is.EqualTo(3));m.RepairAffordable();Assert.That(m.Scrap,Is.EqualTo(2));
    }
    [Test] public void ThreatIncludesTheActuallySpawnedNormalEscort()
    {
        var a=new R1EncounterDefinition{Roster=new[]{R3EnemyArchetype.Scout},Variants=new[]{R5EnemyVariant.Normal},EnemyCount=1,MoveSpeed=0,HealthMultiplier=1,DamageMultiplier=1,FireIntervalMultiplier=1,Arena=R5ArenaKind.SalvageYard};
        Assert.That(R5RunPlan.Threat(a),Is.EqualTo(5.6f).Within(.001f),"Two normal scouts: (0.8+1+1)*2.");
    }
    [Test] public void CoreNeverEntersLootAndArmorCanOnlyBeDismantled()
    {
        var l=new R4SalvageLedger(3);var m=Model();
        Assert.That(l.Record("core",new[]{new R4SalvageItem("core","foe",R3PartKind.Core,0,1)}),Is.False);
        Assert.That(l.Items,Is.Empty);Assert.That(l.Record("armor",new[]{new R4SalvageItem("armor","foe",R3PartKind.Armor,0,1)}),Is.True);
        Assert.That(l.Claim(m,new[]{"armor"}),Is.False);Assert.That(l.Closed,Is.False);
        Assert.That(l.Claim(m,Array.Empty<string>()),Is.True);Assert.That(m.Inventory,Is.Empty);Assert.That(m.Scrap,Is.EqualTo(1));
    }
}
