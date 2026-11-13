using System.Reflection;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using UnityEngine;

public sealed class UI943PortraitRouteTests
{
    static MethodInfo Method(string name)=>typeof(R1RunHUD).GetMethod(name,BindingFlags.Static|BindingFlags.NonPublic);
    [TestCase(5,0,5,false,"teraz")] [TestCase(5,6,5,false,"ukończone")]
    [TestCase(5,0,1,true,"dalej")] [TestCase(5,0,5,true,"teraz")]
    public void FinalRouteStageShowsBossAndProgressWithoutRelyingOnColor(int index,int completed,int current,bool portrait,string status)
    {
        var method=Method("RouteStageCaption");Assert.That(method,Is.Not.Null);
        var text=(string)method.Invoke(null,new object[]{index,completed,current,portrait});
        Assert.That(text,Does.Contain("OBLĘŻNIK").And.Contain(status));
    }
    [Test]
    public void RouteRewardPreviewUsesActualWalletCapacityWithoutSpending()
    {
        var method=Method("RouteRewardText");Assert.That(method,Is.Not.Null);
        var model=new R3VehicleModel(new R3VehicleDesign());
        string Text()=>(string)method.Invoke(null,new object[]{model,50});
        Assert.That(Text(),Does.Contain("+50 Z").And.Not.Contain("limit"));
        model.Award(R3VehicleModel.MaxScrap-10);int before=model.Scrap;
        Assert.That(Text(),Does.Contain("+10 Z").And.Contain("limit"));
        Assert.That(model.Scrap,Is.EqualTo(before));
    }
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
    [Test] public void GarageReturnButtonFitsWithoutCoveringRouteChoices()
    {
        var method=Method("PortraitRouteGarageButton");Assert.That(method,Is.Not.Null);
        var button=(Rect)method.Invoke(null,null);var canvas=new Rect(0,0,768,1024);
        Assert.That(canvas.Contains(button.min)&&canvas.Contains(button.max-Vector2.one),Is.True);
        Assert.That(button.height,Is.GreaterThanOrEqualTo(44));
        for(int i=0;i<2;i++)Assert.That(button.Overlaps((Rect)Method("PortraitRouteCard").Invoke(null,new object[]{i})),Is.False);
    }
}
