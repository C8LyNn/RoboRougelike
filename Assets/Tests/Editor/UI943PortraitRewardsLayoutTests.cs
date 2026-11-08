using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RoboRougelike.R1;
using UnityEngine;

public sealed class UI943PortraitRewardsLayoutTests
{
    static MethodInfo Method(string name)
    {var m=typeof(R1RunHUD).GetMethod(name,BindingFlags.Static|BindingFlags.NonPublic);Assert.That(m,Is.Not.Null,name);return m;}
    static Rect RectOf(string name,params object[] args)=>(Rect)Method(name).Invoke(null,args);
    [Test] public void NoLootRequiresOnlyTheBonusAndExplainsIt()
    {
        var caption=(string)Method("RewardConfirmCaption").Invoke(null,new object[]{0});
        var intro=(string)Method("RewardIntro").Invoke(null,new object[]{0,25});
        Assert.That(caption,Does.Contain("bonus").And.Not.Contain("oba"));
        Assert.That(intro,Does.Contain("Brak części").And.Contain("25 Z"));
        Assert.That((bool)Method("RewardSelectionReady").Invoke(null,new object[]{0,3,-1,0,0,0}),Is.True);
        Assert.That((bool)Method("RewardSelectionReady").Invoke(null,new object[]{0,3,-1,1,0,0}),Is.False);
        Assert.That((string)Method("RewardConfirmCaption").Invoke(null,new object[]{1}),Does.Contain("oba"));
    }
    static void In(Rect outer,Rect inner,string name)
    {Assert.That(outer.Contains(inner.min)&&outer.Contains(inner.max-Vector2.one),Is.True,name);}
    [TestCase(768,1024,true)] [TestCase(1024,768,false)] [TestCase(1920,1080,false)]
    public void PortraitIsOnlyUsedForUnobscuredSalvage(int width,int height,bool expected)
    {
        var m=Method("UsePortraitSalvage");
        Assert.That((bool)m.Invoke(null,new object[]{width,height,true,false}),Is.EqualTo(expected));
        Assert.That((bool)m.Invoke(null,new object[]{width,height,false,false}),Is.False);
        Assert.That((bool)m.Invoke(null,new object[]{width,height,true,true}),Is.False,"Pauza, błąd, mapa i przejście zachowują swój układ.");
    }
    [Test] public void RewardCardsAndControlsFitWithoutOverlap()
    {
        var canvas=new Rect(0,0,768,1024);var areas=new List<Rect>();
        for(int i=0;i<3;i++)areas.Add(RectOf("PortraitRewardBonusCard",i));
        for(int i=0;i<6;i++)areas.Add(RectOf("PortraitRewardLootCard",i));
        areas.Add(RectOf("PortraitRewardPager"));areas.Add(RectOf("PortraitRewardSummary"));areas.Add(RectOf("PortraitRewardConfirm"));
        for(int i=0;i<areas.Count;i++)
        {In(canvas,areas[i],"Poza ekranem "+i);for(int j=0;j<i;j++)Assert.That(areas[i].Overlaps(areas[j]),Is.False,$"Obszary {i} i {j} nachodzą na siebie.");}
        Assert.That(RectOf("PortraitRewardConfirm").height,Is.GreaterThanOrEqualTo(44));
        for(int i=0;i<6;i++)
        {
            var card=RectOf("PortraitRewardLootCard",i);var details=RectOf("PortraitRewardLootDetails",i);var button=RectOf("PortraitRewardLootButton",i);var source=RectOf("PortraitRewardLootSource",i);
            In(card,details,"Dane łupu "+i);In(card,button,"Przycisk łupu "+i);In(card,source,"Źródło "+i);
            Assert.That(details.Overlaps(button),Is.False,"Przycisk nie zasłania HP, naprawy ani amunicji.");
            Assert.That(source.Overlaps(button)||source.Overlaps(details),Is.False,"Źródło nie zasłania pozostałych danych.");
            Assert.That(button.height,Is.GreaterThanOrEqualTo(44));
        }
    }
    [Test] public void InventoryCardsPagerAndReturnFitWithoutOverlap()
    {
        var canvas=new Rect(0,0,768,1024);var areas=new List<Rect>();
        for(int i=0;i<5;i++)areas.Add(RectOf("PortraitRewardInventoryCard",i));
        areas.Add(RectOf("PortraitRewardInventoryPager"));areas.Add(RectOf("PortraitRewardInventoryBack"));
        for(int i=0;i<areas.Count;i++)
        {In(canvas,areas[i],"Poza ekranem "+i);for(int j=0;j<i;j++)Assert.That(areas[i].Overlaps(areas[j]),Is.False,$"Obszary {i} i {j} nachodzą na siebie.");}
        for(int i=0;i<5;i++)
        {var card=RectOf("PortraitRewardInventoryCard",i);var text=RectOf("PortraitRewardInventoryDetails",i);var button=RectOf("PortraitRewardDismantleButton",i);In(card,text,"Dane "+i);In(card,button,"Demontaż "+i);Assert.That(text.Overlaps(button),Is.False);Assert.That(button.height,Is.GreaterThanOrEqualTo(44));}
    }
}
