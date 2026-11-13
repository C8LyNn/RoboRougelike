using System.Reflection;
using NUnit.Framework;
using RoboRougelike.R1;
using UnityEngine;

public sealed class UI943CombatHudLayoutTests
{
    [TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)]
    public void MultipleActiveAbilityRowsDoNotOverlapDetailsOrButton(int count)
    {
        var flags=BindingFlags.NonPublic|BindingFlags.Static;
        var extra=typeof(R1RunHUD).GetMethod("CombatAbilityExtraHeight",flags);
        var row=typeof(R1RunHUD).GetMethod("CombatAbilityStatusRect",flags);
        Assert.That(extra,Is.Not.Null);Assert.That(row,Is.Not.Null);
        int height=(int)extra.Invoke(null,new object[]{count});
        var details=new Rect(36,120+height,630,26);var button=new Rect(535,88,130,30);
        for(int i=0;i<count;i++)
        {
            var rect=(Rect)row.Invoke(null,new object[]{i});
            Assert.That(rect.Overlaps(details),Is.False);
            Assert.That(rect.Overlaps(button),Is.False);
            if(i>0)Assert.That(rect.Overlaps((Rect)row.Invoke(null,new object[]{i-1})),Is.False);
        }
    }
    [TestCase(18,42)] [TestCase(36,50)] [TestCase(90,65)]
    public void AimStatusPanelFitsTextAndStaysOutsideCombatPanels(float textHeight,float expected)
    {
        var method=typeof(R1RunHUD).GetMethod("AimStatusPanelRect",BindingFlags.NonPublic|BindingFlags.Static);
        Assert.That(method,Is.Not.Null);
        var rect=(Rect)method.Invoke(null,new object[]{textHeight});
        Assert.That(rect.height,Is.EqualTo(expected));
        Assert.That(rect.Overlaps(new Rect(20,18,660,250)),Is.False);
        Assert.That(rect.Overlaps(new Rect(925,18,335,160)),Is.False);
        Assert.That(rect.Overlaps(new Rect(624,344,32,32)),Is.False);
    }

    [Test] public void AimWarningOnlyHighlightsUnavailableOrPartiallyBlockedShots()
    {
        var method=typeof(R1RunHUD).GetMethod("AimStatusNeedsAttention",BindingFlags.NonPublic|BindingFlags.Static);
        Assert.That(method,Is.Not.Null);
        bool Warn(int guns,bool surface,int clear)=>(bool)method.Invoke(null,new object[]{guns,surface,clear});
        Assert.That(Warn(0,false,0),Is.True);
        Assert.That(Warn(2,false,0),Is.False,"Looking at empty space is not an error.");
        Assert.That(Warn(2,true,0),Is.True);
        Assert.That(Warn(2,true,1),Is.True,"A partly blocked group is still worth noticing.");
        Assert.That(Warn(2,true,2),Is.False);
    }
    [TestCase(0,false)] [TestCase(1,false)] [TestCase(2,false)] [TestCase(4,false)] [TestCase(0,true)]
    public void EncounterPanelFitsVisibleRowsAndAvoidsCenter(int rows,bool defeated)
    {
        var flags=BindingFlags.NonPublic|BindingFlags.Static;
        var panelMethod=typeof(R1RunHUD).GetMethod("CombatEnemyPanelRect",flags);
        var labelMethod=typeof(R1RunHUD).GetMethod("EnemyStatusLabelRect",flags);
        var barMethod=typeof(R1RunHUD).GetMethod("EnemyStatusBarRect",flags);
        Assert.That(panelMethod,Is.Not.Null);
        var panel=(Rect)panelMethod.Invoke(null,new object[]{rows,false,defeated});
        for(int i=0;i<rows;i++)
        {
            var label=(Rect)labelMethod.Invoke(null,new object[]{i});
            var bar=(Rect)barMethod.Invoke(null,new object[]{i});
            Assert.That(panel.Contains(label.min)&&panel.Contains(label.max-Vector2.one),Is.True);
            Assert.That(panel.Contains(bar.min)&&panel.Contains(bar.max-Vector2.one),Is.True);
        }
        if(defeated)Assert.That(panel.yMax,Is.GreaterThanOrEqualTo(130));
        Assert.That(panel.xMin,Is.GreaterThan(656),"The aiming area must remain clear.");
        Assert.That(panel.yMax,Is.LessThanOrEqualTo(190));
        var special=(Rect)panelMethod.Invoke(null,new object[]{rows,true,false});
        Assert.That(special.height,Is.EqualTo(160),"Reactor and racing modes need their existing space.");
    }

    [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(6)]
    public void WeaponPanelUsesOnlySpaceNeededByVisibleWeapons(int count)
    {
        var method=typeof(R1RunHUD).GetMethod("CombatWeaponPanelRect",BindingFlags.NonPublic|BindingFlags.Static);
        Assert.That(method,Is.Not.Null);
        var panel=(Rect)method.Invoke(null,new object[]{count});
        Assert.That(panel.yMax,Is.GreaterThanOrEqualTo(276));
        if(count>0)Assert.That(panel.yMax,Is.GreaterThanOrEqualTo(304));
        if(count>1)Assert.That(panel.yMax,Is.GreaterThanOrEqualTo(330));
        Assert.That(panel.yMax,Is.LessThanOrEqualTo(348));
        Assert.That(panel.xMin,Is.GreaterThan(656));
    }
    [Test] public void SuspensionPauseIsRelevantForMountedAndDestroyedPlansOnly()
    {
        var method=typeof(R1RunHUD).GetMethod("ShowSuspensionPause",BindingFlags.NonPublic|BindingFlags.Static);
        Assert.That(method,Is.Not.Null);
        var model=new RoboRougelike.R3.R3VehicleModel(RoboRougelike.R3.R3VehicleCatalog.Create(RoboRougelike.R3.R3VehicleKind.Light));
        bool Visible()=>(bool)method.Invoke(null,new object[]{model});
        Assert.That(Visible(),Is.False);
        var support=new RoboRougelike.R3.R3PartState(Vector3Int.up,RoboRougelike.R3.R3PartKind.Suspension,0);
        model.Parts.Add(support);
        Assert.That(Visible(),Is.True,"A destroyed mounted plan still needs an explanation.");
        model.Parts.Remove(support);Assert.That(Visible(),Is.False);
    }
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
