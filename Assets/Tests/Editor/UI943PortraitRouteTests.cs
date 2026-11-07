using System.Reflection;
using NUnit.Framework;
using RoboRougelike.R1;
using UnityEngine;

public sealed class UI943PortraitRouteTests
{
    static MethodInfo Method(string name)=>typeof(R1RunHUD).GetMethod(name,BindingFlags.Static|BindingFlags.NonPublic);
    [Test]
    public void PortraitRouteIsLimitedToItsOwnUnobscuredScreen()
    {
        var m=Method("UsePortraitRoute");Assert.That(m,Is.Not.Null);
        bool Use(int w,int h,bool route,bool overlay)=>(bool)m.Invoke(null,new object[]{w,h,route,overlay});
        Assert.That(Use(768,1024,true,false),Is.True);
        Assert.That(Use(1024,768,true,false),Is.False);
        Assert.That(Use(768,1024,false,false),Is.False);
        Assert.That(Use(768,1024,true,true),Is.False);
    }
    [Test]
    public void BothRouteChoicesFitWithoutOverlapAndHaveReadableButtons()
    {
        var cards=Method("PortraitRouteCard");var buttons=Method("PortraitRouteButton");
        Assert.That(cards,Is.Not.Null);Assert.That(buttons,Is.Not.Null);
        var canvas=new Rect(0,0,768,1024);
        var a=(Rect)cards.Invoke(null,new object[]{0});var b=(Rect)cards.Invoke(null,new object[]{1});
        Assert.That(a.Overlaps(b),Is.False);
        foreach(int i in new[]{0,1})
        {
            var card=(Rect)cards.Invoke(null,new object[]{i});var button=(Rect)buttons.Invoke(null,new object[]{i});
            Assert.That(canvas.Contains(card.min)&&canvas.Contains(card.max-Vector2.one),Is.True);
            Assert.That(card.Contains(button.min)&&card.Contains(button.max-Vector2.one),Is.True);
            Assert.That(button.height,Is.GreaterThanOrEqualTo(44));
        }
    }
}
