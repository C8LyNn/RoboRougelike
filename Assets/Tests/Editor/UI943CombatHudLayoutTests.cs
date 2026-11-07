using System.Reflection;
using NUnit.Framework;
using RoboRougelike.R1;
using UnityEngine;

public sealed class UI943CombatHudLayoutTests
{
    [TestCase(1920,1080,1)] [TestCase(1366,768,1)] [TestCase(1024,768,1)]
    [TestCase(768,1024,1)] [TestCase(3840,2160,1)]
    [TestCase(1920,1080,4)] [TestCase(1366,768,4)] [TestCase(1024,768,4)]
    [TestCase(768,1024,4)] [TestCase(3840,2160,4)]
    public void DefensePanelDoesNotCoverStatusOrMovement(int width,int height,int count)
    {
        var method=typeof(R1RunHUD).GetMethod("DefensePanelRect",BindingFlags.NonPublic|BindingFlags.Static);
        Assert.That(method,Is.Not.Null,"Panel obrony musi mieć sprawdzalny obszar.");
        var panel=(Rect)method.Invoke(null,new object[]{count});
        float scale=Mathf.Min(width/1280f,height/720f);
        var offset=new Vector2((width-1280*scale)/2,(height-720*scale)/2);
        Rect ScreenRect(Rect r)=>new Rect(offset.x+r.x*scale,offset.y+r.y*scale,r.width*scale,r.height*scale);
        var visible=ScreenRect(panel);
        Assert.That(visible.Overlaps(ScreenRect(new Rect(20,18,660,180))),Is.False,"Obrona zasłania status pojazdu lub odliczanie energii.");
        Assert.That(visible.Overlaps(ScreenRect(new Rect(18,490,610,125))),Is.False,"Obrona zasłania telemetrię napędu.");
        Assert.That(visible.xMin,Is.GreaterThanOrEqualTo(0));Assert.That(visible.yMin,Is.GreaterThanOrEqualTo(0));
        Assert.That(visible.xMax,Is.LessThanOrEqualTo(width));Assert.That(visible.yMax,Is.LessThanOrEqualTo(height));
    }
}
