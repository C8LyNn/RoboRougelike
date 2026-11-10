using System.Reflection;
using NUnit.Framework;
using RoboRougelike.R1;
using UnityEngine;

public sealed class UI943CombatHudLayoutTests
{
    [TestCase(false,false,75)][TestCase(false,true,115)][TestCase(true,false,160)][TestCase(true,true,160)]
    public void SuspensionPanelOnlyUsesSpaceNeededForDetailsAndWarnings(bool details,bool warning,int height)
    {
        var method=typeof(R1RunHUD).GetMethod("SuspensionTelemetryPanelRect",BindingFlags.NonPublic|BindingFlags.Static);
        Assert.That(method,Is.Not.Null);
        var panel=(Rect)method.Invoke(null,new object[]{details,warning});
        Assert.That(panel.height,Is.EqualTo(height));
        Assert.That(panel.yMin,Is.GreaterThanOrEqualTo(348),"Weapon status must remain unobstructed.");
        Assert.That(panel.yMax,Is.LessThanOrEqualTo(516),"Damage notices must remain unobstructed.");
    }
    [TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)]
    public void CompactMovementPanelFitsBetweenDefenseAndRangeInstructions(int rows)
    {
        var flags=BindingFlags.NonPublic|BindingFlags.Static;
        var movement=typeof(R1RunHUD).GetMethod("CompactMovementPanelRect",flags);
        var defense=typeof(R1RunHUD).GetMethod("DefensePanelRect",flags);
        Assert.That(movement,Is.Not.Null);Assert.That(defense,Is.Not.Null);
        var panel=(Rect)movement.Invoke(null,new object[]{rows});
        var defensePanel=(Rect)defense.Invoke(null,new object[]{9});
        Assert.That(panel.Overlaps(defensePanel),Is.False);
        Assert.That(panel.yMax,Is.LessThanOrEqualTo(625));
        Assert.That(panel.height,Is.GreaterThanOrEqualTo(rows*24));
    }
    [TestCase(1)] [TestCase(4)] [TestCase(9)]
    public void DefenseHeadingAndRowsFitInsideTheirPanel(int count)
    {
        var flags=BindingFlags.NonPublic|BindingFlags.Static;
        var panelMethod=typeof(R1RunHUD).GetMethod("DefensePanelRect",flags);
        var titleMethod=typeof(R1RunHUD).GetMethod("DefenseTitleRect",flags);
        var rowMethod=typeof(R1RunHUD).GetMethod("DefenseRowRect",flags);
        Assert.That(panelMethod,Is.Not.Null);Assert.That(titleMethod,Is.Not.Null);Assert.That(rowMethod,Is.Not.Null);
        var panel=(Rect)panelMethod.Invoke(null,new object[]{count});
        var title=(Rect)titleMethod.Invoke(null,null);
        Assert.That(title.height,Is.GreaterThanOrEqualTo(30),"Nagłówek 24 px nie może być ucięty przez niski prostokąt.");
        Assert.That(title.yMin,Is.GreaterThanOrEqualTo(panel.yMin));Assert.That(title.yMax,Is.LessThanOrEqualTo(panel.yMax));
        for(int i=0;i<count;i++)
        {
            var row=(Rect)rowMethod.Invoke(null,new object[]{i});
            Assert.That(row.Overlaps(title),Is.False);
            Assert.That(row.yMin,Is.GreaterThanOrEqualTo(panel.yMin));Assert.That(row.yMax,Is.LessThanOrEqualTo(panel.yMax));
        }
    }
    [TestCase(1920,1080,1)] [TestCase(1366,768,1)] [TestCase(1024,768,1)]
    [TestCase(768,1024,1)] [TestCase(3840,2160,1)]
    [TestCase(1920,1080,4)] [TestCase(1366,768,4)] [TestCase(1024,768,4)]
    [TestCase(768,1024,4)] [TestCase(3840,2160,4)]
    [TestCase(1920,1080,10)] [TestCase(1024,768,10)] [TestCase(768,1024,180)]
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
