using NUnit.Framework;
using System.Reflection;
using RoboRougelike.R1;
using RoboRougelike.R3;
using UnityEngine;

public sealed class UI943PortraitGarageTests
{
    [Test]
    public void MapEditorAndOverlaysKeepTheirOwnLayout()
    {
        var method=typeof(R1RunHUD).GetMethod("UsePortraitGarage",BindingFlags.Static|BindingFlags.NonPublic);
        Assert.That(method,Is.Not.Null);
        bool Portrait(bool garage,bool editing,bool overlay)=>(bool)method.Invoke(null,new object[]{768,1024,garage,editing,overlay});
        Assert.That(Portrait(true,false,false),Is.True);
        Assert.That(Portrait(true,true,false),Is.False,"Edytor map używa swojego osobnego układu i transformacji wejścia.");
        Assert.That(Portrait(true,false,true),Is.False);
        Assert.That(Portrait(false,false,false),Is.False);
    }
    [Test]
    public void PortraitViewportUsesLargeDedicatedVehicleOpening()
    {
        var viewport=R3GarageView.ViewportForScreen(768,1024);
        Assert.That(viewport.x*768,Is.EqualTo(24).Within(.1f));
        Assert.That(viewport.y*1024,Is.EqualTo(448).Within(.1f));
        Assert.That(viewport.width*768,Is.EqualTo(720).Within(.1f));
        Assert.That(viewport.height*1024,Is.EqualTo(500).Within(.1f));
    }

    [Test]
    public void PortraitPlacementIsConfinedToVehicleOpening()
    {
        bool Hit(float x,float y)=>R3GarageView.InBuildArea(new Vector2(x,1024-y),768,1024);
        Assert.That(Hit(384,300),Is.True);
        Assert.That(Hit(384,610),Is.False,"Przyciski narzędzi nie mogą montować części.");
        Assert.That(Hit(384,60),Is.False);
        Assert.That(Hit(10,300),Is.False);
    }
    [TestCase(true,768,1024)]
    [TestCase(false,1280,720)]
    public void QuickRepairDoesNotOverlapPartListOrVehicleView(bool portrait,int width,int height)
    {
        Rect For(string name)=>(Rect)typeof(R1RunHUD).GetMethod(name,BindingFlags.Static|BindingFlags.NonPublic)
            .Invoke(null,new object[]{portrait});
        var list=For("QuickPartsArea");var repair=For("QuickRepairArea");
        var canvas=new Rect(0,0,width,height);
        Assert.That(canvas.Contains(repair.min)&&canvas.Contains(repair.max-Vector2.one),Is.True);
        Assert.That(repair.Overlaps(list),Is.False);
        if(portrait)Assert.That(repair.Overlaps(R3GarageView.PortraitOpening),Is.False);
    }
}
