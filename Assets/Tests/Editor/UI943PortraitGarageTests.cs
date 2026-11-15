using NUnit.Framework;
using System.Reflection;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4_1;
using UnityEngine;

public sealed class UI943PortraitGarageTests
{
    [Test] public void GarageRotationInputUsesConfiguredKeysAndAllowsShift()
    {
        var method=typeof(R3GarageRotationInput).GetMethod("TryStepForProfile",BindingFlags.Static|BindingFlags.Public);
        Assert.That(method,Is.Not.Null);
        var profile=new R1ControlProfile(System.IO.Path.Combine(System.IO.Path.GetTempPath(),"RRL-garage-rotation-"+System.Guid.NewGuid()+".json"));
        Assert.That(profile.TryBind("GarageRotateYRight","K"),Is.True);
        var args=new object[]{new Event{type=EventType.KeyDown,keyCode=KeyCode.K,shift=true},profile,default(Vector3Int)};
        Assert.That((bool)method.Invoke(null,args),Is.True);Assert.That((Vector3Int)args[2],Is.EqualTo(new Vector3Int(0,1,0)));
        args[0]=new Event{type=EventType.KeyDown,keyCode=KeyCode.E,shift=true};Assert.That((bool)method.Invoke(null,args),Is.False);
        args[0]=new Event{type=EventType.KeyDown,keyCode=KeyCode.K,control=true};Assert.That((bool)method.Invoke(null,args),Is.False);
    }
    [Test] public void GarageToolShortcutUsesProfileAndRespectsAssignedKeypadKeys()
    {
        var method=typeof(R3GarageView).GetMethod("ToolShortcutForProfile",BindingFlags.Static|BindingFlags.NonPublic);
        Assert.That(method,Is.Not.Null);
        var profile=new R1ControlProfile(System.IO.Path.Combine(System.IO.Path.GetTempPath(),"RRL-garage-tools-"+System.Guid.NewGuid()+".json"));
        object Match(KeyCode key)=>method.Invoke(null,new object[]{new Event{type=EventType.KeyDown,keyCode=key},profile});
        Assert.That(Match(KeyCode.Alpha3),Is.EqualTo(R41GarageTool.Repair));Assert.That(Match(KeyCode.Keypad3),Is.EqualTo(R41GarageTool.Repair));
        Assert.That(profile.TryBind("GarageFrame","Numpad3"),Is.True);Assert.That(Match(KeyCode.Keypad3),Is.Null);
        Assert.That(profile.TryBind("GarageToolRepair","K"),Is.True);
        Assert.That(Match(KeyCode.K),Is.EqualTo(R41GarageTool.Repair));Assert.That(Match(KeyCode.Alpha3),Is.Null);
        Assert.That(profile.TryBind("GarageToolRepair","MouseBack"),Is.True);
        Assert.That(method.Invoke(null,new object[]{new Event{type=EventType.MouseDown,button=3},profile}),Is.EqualTo(R41GarageTool.Repair));
    }
    [TestCase(2,true,"kolejnej części")]
    [TestCase(1,false,"zniszczone")]
    [TestCase(0,false,"Brak kolejnych")]
    public void MountedStackFeedbackExplainsWhatRemains(int remaining,bool usable,string expected)
    {
        var method=typeof(R3GarageView).GetMethod("MountedStackFeedback",BindingFlags.Static|BindingFlags.NonPublic);
        Assert.That(method,Is.Not.Null);
        Assert.That((string)method.Invoke(null,new object[]{remaining,usable}),Does.Contain(expected));
    }
    [TestCase(KeyCode.Alpha1,false,R41GarageTool.Mount)]
    [TestCase(KeyCode.Alpha2,false,R41GarageTool.Inspect)]
    [TestCase(KeyCode.Alpha3,false,R41GarageTool.Repair)]
    [TestCase(KeyCode.Alpha4,false,R41GarageTool.Remove)]
    [TestCase(KeyCode.Keypad1,false,R41GarageTool.Mount)]
    [TestCase(KeyCode.Keypad4,false,R41GarageTool.Remove)]
    [TestCase(KeyCode.Alpha1,true,null)]
    [TestCase(KeyCode.A,false,null)]
    public void ToolShortcutsIgnoreModifiersAndOtherKeys(KeyCode key,bool modified,object expected)
    {
        var method=typeof(R3GarageView).GetMethod("ToolShortcutForProfile",BindingFlags.Static|BindingFlags.NonPublic);
        var profile=new R1ControlProfile(System.IO.Path.Combine(System.IO.Path.GetTempPath(),"RRL-tool-keys-"+System.Guid.NewGuid()+".json"));
        var input=new Event{type=EventType.KeyDown,keyCode=key,modifiers=modified?EventModifiers.Control:EventModifiers.None};
        Assert.That(method,Is.Not.Null);
        Assert.That(method.Invoke(null,new object[]{input,profile}),Is.EqualTo(expected));
    }
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

    [TestCase(false)]
    [TestCase(true)]
    public void GarageStatusPanelKeepsVehicleViewAndMainActionsClear(bool portrait)
    {
        var method=typeof(R1RunHUD).GetMethod("GarageNoticeRect",BindingFlags.Static|BindingFlags.NonPublic);
        Assert.That(method,Is.Not.Null);
        var notice=(Rect)method.Invoke(null,new object[]{portrait});
        var view=portrait?R3GarageView.PortraitOpening:new Rect(276,86,994,562);
        var test=portrait?new Rect(24,960,350,44):new Rect(870,663,185,43);
        var next=portrait?new Rect(394,960,350,44):new Rect(1066,663,195,43);
        Assert.That(notice.Overlaps(view),Is.False);
        Assert.That(notice.Overlaps(test),Is.False);
        Assert.That(notice.Overlaps(next),Is.False);
    }
}
