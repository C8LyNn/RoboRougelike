using System.Reflection;
using NUnit.Framework;
using RoboRougelike.R1;
using UnityEngine;

public sealed class UI943PortraitMenuTests
{
    [TestCase("",2026,"Błędny seed\nUżyty: 2026")]
    [TestCase("abc",7,"Błędny seed\nUżyty: 7")]
    [TestCase("2147483648",-4,"Błędny seed\nUżyty: -4")]
    [TestCase("-2147483648",2026,null)]
    [TestCase("42",2026,null)]
    public void InvalidSeedReportsTheLastValidValue(string text,int lastValid,string expected)
    {
        var method=typeof(R1RunHUD).GetMethod("SeedInputWarning",BindingFlags.Static|BindingFlags.NonPublic);
        Assert.That(method,Is.Not.Null);
        Assert.That((string)method.Invoke(null,new object[]{text,lastValid}),Is.EqualTo(expected));
    }

    [TestCase(768,1024,true)] [TestCase(1024,768,false)] [TestCase(1366,768,false)]
    [TestCase(1920,1080,false)] [TestCase(3840,2160,false)]
    public void PortraitLayoutIsSelectedOnlyForPortraitReadyMenu(int width,int height,bool expected)
    {
        var m=typeof(R1RunHUD).GetMethod("UsePortraitReady",BindingFlags.Static|BindingFlags.NonPublic);
        Assert.That(m,Is.Not.Null,"Pionowe menu potrzebuje czytelnego układu.");
        Assert.That((bool)m.Invoke(null,new object[]{width,height,true,false}),Is.EqualTo(expected));
        Assert.That((bool)m.Invoke(null,new object[]{width,height,false,false}),Is.False,"Inne ekrany zachowują swój układ.");
        Assert.That((bool)m.Invoke(null,new object[]{width,height,true,true}),Is.False,"Pauza i błędy zachowują swój układ.");
    }

    [Test] public void PortraitCardsAndActionsStayInsideCanvasWithoutCollisions()
    {
        var flags=BindingFlags.Static|BindingFlags.NonPublic;
        var cardMethod=typeof(R1RunHUD).GetMethod("PortraitVehicleCard",flags);
        var actionMethod=typeof(R1RunHUD).GetMethod("PortraitActionRect",flags);
        Assert.That(cardMethod,Is.Not.Null);Assert.That(actionMethod,Is.Not.Null);
        var canvas=new Rect(0,0,768,1024);
        var rects=new System.Collections.Generic.List<Rect>();
        for(int i=0;i<3;i++)rects.Add((Rect)cardMethod.Invoke(null,new object[]{i}));
        for(int i=0;i<4;i++)rects.Add((Rect)actionMethod.Invoke(null,new object[]{i}));
        for(int i=0;i<rects.Count;i++)
        {
            Assert.That(canvas.Contains(rects[i].min)&&canvas.Contains(rects[i].max-Vector2.one),Is.True,$"Element {i} wychodzi poza ekran.");
            for(int j=0;j<i;j++)Assert.That(rects[i].Overlaps(rects[j]),Is.False,$"Elementy {i} i {j} nakładają się.");
        }
    }
}
