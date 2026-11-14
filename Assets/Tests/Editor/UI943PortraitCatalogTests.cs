using System.Reflection;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4;
using UnityEngine;

public sealed class UI943PortraitCatalogTests
{
    static Rect RectFor(string name, params object[] args)
    {
        var method=typeof(R1RunHUD).GetMethod(name,BindingFlags.Static|BindingFlags.NonPublic);
        Assert.That(method,Is.Not.Null,$"Brakuje geometrii katalogu: {name}");
        return (Rect)method.Invoke(null,args);
    }

    [Test] public void ListsAndActionsFitPortraitCanvasWithoutOverlapping()
    {
        var canvas=new Rect(0,0,768,1024);
        var groups=RectFor("PortraitCatalogGroupsViewport");
        var items=RectFor("PortraitCatalogItemsViewport");
        var bulkRepair=RectFor("PortraitCatalogBulkRepairRect");
        var buy=RectFor("PortraitCatalogBuyRect");
        var back=RectFor("PortraitCatalogBackRect");
        var rectangles=new[]{groups,items,bulkRepair,buy,back};
        for(int i=0;i<rectangles.Length;i++)
        {
            Assert.That(canvas.Contains(rectangles[i].min)&&canvas.Contains(rectangles[i].max-Vector2.one),Is.True,$"Element {i} poza płótnem.");
            for(int j=0;j<i;j++) Assert.That(rectangles[i].Overlaps(rectangles[j]),Is.False,$"Elementy {i}, {j} nachodzą na siebie.");
        }
        Assert.That(groups.width,Is.GreaterThanOrEqualTo(300));
        Assert.That(items.width,Is.GreaterThanOrEqualTo(300));
    }

    [Test] public void EveryGroupAndInstanceRowCanBeReachedByScrolling()
    {
        var groups=RectFor("PortraitCatalogGroupsViewport");
        var items=RectFor("PortraitCatalogItemsViewport");
        foreach(var index in new[]{0,1,20,99})
        {
            var group=RectFor("PortraitCatalogGroupRow",index);
            var item=RectFor("PortraitCatalogItemRow",index);
            Assert.That(group.xMin,Is.GreaterThanOrEqualTo(0));
            Assert.That(group.xMax,Is.LessThan(groups.width));
            Assert.That(item.xMin,Is.GreaterThanOrEqualTo(0));
            Assert.That(item.xMax,Is.LessThan(items.width));
            Assert.That(group.yMin,Is.EqualTo(index*56).Within(.01f));
            Assert.That(item.yMin,Is.EqualTo(index*108).Within(.01f));
            Assert.That(group.height,Is.GreaterThanOrEqualTo(44));
            Assert.That(item.height,Is.LessThanOrEqualTo(108));
        }
    }

    [TestCase("Koło skrętne drogowe","kolo",true)]
    [TestCase("Żyroskop","ZYRO",true)]
    [TestCase("Pancerz • zaokrąglenie","okrag",true)]
    [TestCase("Rocket Launcher","launcher",true)]
    [TestCase("Koło terenowe","laser",false)]
    [TestCase("Koło terenowe","   ",true)]
    public void SearchMatchesNamesWithoutCaseOrPolishDiacritics(string name,string query,bool expected)
    {
        var method=typeof(R1RunHUD).GetMethod("CatalogNameMatches",BindingFlags.Static|BindingFlags.NonPublic);
        Assert.That(method,Is.Not.Null);
        Assert.That((bool)method.Invoke(null,new object[]{name,query}),Is.EqualTo(expected));
    }

    [Test] public void SharedCatalogContainsAllDistinctBaseGroups()
    {
        var model=new R3VehicleModel(new R3VehicleDesign());
        var method=typeof(R1RunHUD).GetMethod("BuildCatalogEntries",BindingFlags.Static|BindingFlags.NonPublic);
        Assert.That(method,Is.Not.Null);
        var entries=((IEnumerable)method.Invoke(null,new object[]{model})).Cast<object>().ToArray();
        object Field(object entry,string name)=>entry.GetType().GetField(name).GetValue(entry);
        Assert.That(entries.Count(e=>(R3PartKind)Field(e,"Kind")==R3PartKind.Wheel),Is.EqualTo(12));
        Assert.That(entries.Count(e=>(R3PartKind)Field(e,"Kind")==R3PartKind.Armor),Is.EqualTo(7));
        Assert.That(entries.Count(e=>(R3PartKind)Field(e,"Kind")==R3PartKind.Gun),Is.EqualTo(R4WeaponDefinition.Count));
        var healer=entries.Single(e=>(R3PartKind)Field(e,"Kind")==R3PartKind.HealingDroneGenerator);
        Assert.That(Field(healer,"Name"),Is.EqualTo("Generator drona naprawczego"));
        var support=entries.Single(e=>(R3PartKind)Field(e,"Kind")==R3PartKind.SupportDroneGenerator);
        Assert.That(Field(support,"Name"),Is.EqualTo("Generator drona energetycznego"));
        Assert.That(entries.Count(e=>(R3PartKind)Field(e,"Kind")==R3PartKind.ShieldTurretGenerator),Is.EqualTo(1));
        Assert.That(entries.Select(e=>(Field(e,"Kind"),Field(e,"Weapon"),Field(e,"Group"))).Distinct().Count(),Is.EqualTo(entries.Length));
    }
    [Test] public void DifferentBonusEffectsWithTheSameIdDoNotShareAStack()
    {
        var model=new R3VehicleModel(new R3VehicleDesign());
        var damage=ScriptableObject.CreateInstance<R1UpgradeDefinition>();damage.Id="shared";damage.Title="Moduł";damage.Effect=R1UpgradeEffect.Damage;damage.Amount=.2f;
        var strong=ScriptableObject.CreateInstance<R1UpgradeDefinition>();strong.Id="shared";strong.Title="Moduł";strong.Effect=R1UpgradeEffect.Damage;strong.Amount=.4f;
        var speed=ScriptableObject.CreateInstance<R1UpgradeDefinition>();speed.Id="shared";speed.Title="Moduł";speed.Effect=R1UpgradeEffect.Mobility;speed.Amount=.2f;
        try
        {
            Assert.That(model.ReceiveBonuses(new[]{damage,speed,strong,damage}),Is.True);
            model.Inventory.Last().Tier=2;
            var method=typeof(R1RunHUD).GetMethod("BuildCatalogEntries",BindingFlags.Static|BindingFlags.NonPublic);
            var entries=((IEnumerable)method.Invoke(null,new object[]{model})).Cast<object>()
                .Where(entry=>(R3PartKind)entry.GetType().GetField("Kind").GetValue(entry)==R3PartKind.BonusModule).ToArray();
            Assert.That(entries.Length,Is.EqualTo(3),"Different effects and strengths must not be silently merged; tiers of the same bonus stay together.");
            var names=entries.Select(entry=>(string)entry.GetType().GetField("Name").GetValue(entry)).ToArray();
            Assert.That(names.Distinct().Count(),Is.EqualTo(3),"The player must be able to distinguish the stacks.");
        }
        finally{Object.DestroyImmediate(damage);Object.DestroyImmediate(strong);Object.DestroyImmediate(speed);}
    }
}
