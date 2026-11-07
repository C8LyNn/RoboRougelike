using System.Reflection;
using NUnit.Framework;
using RoboRougelike.R1;
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
        var buy=RectFor("PortraitCatalogBuyRect");
        var back=RectFor("PortraitCatalogBackRect");
        var rectangles=new[]{groups,items,buy,back};
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
}
