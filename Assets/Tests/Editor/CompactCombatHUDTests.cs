using System.Reflection;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using UnityEngine;

public sealed class CompactCombatHUDTests
{
    [Test]
    public void RunSummaryDoesNotPromiseOldVehicleOrAssumeFinalBoss()
    {
        var flags=BindingFlags.Static|BindingFlags.NonPublic;
        var detail=typeof(R1RunHUD).GetMethod("RunEndOutcome",flags);
        var note=typeof(R1RunHUD).GetMethod("RunEndNewRunNote",flags);
        var recovered=typeof(R1RunHUD).GetMethod("RunEndRecovery",flags);
        Assert.That(detail,Is.Not.Null);Assert.That(note,Is.Not.Null);Assert.That(recovered,Is.Not.Null);
        var victory=(string)detail.Invoke(null,new object[]{true,""});
        Assert.That(victory,Does.Contain("ukończona").And.Not.Contain("Oblężnik"));
        Assert.That((string)detail.Invoke(null,new object[]{false,"Zniszczono rdzeń."}),Does.Contain("rdzeń"));
        Assert.That((string)note.Invoke(null,null),Does.Contain("wybierzesz").And.Contain("wylosowanych"));
        Assert.That((string)recovered.Invoke(null,new object[]{3}),Does.Contain("części: 3"));
    }
    [Test] public void StarterSummaryShowsUsableEnergyAfterPassiveReservations()
    {
        var design=new R3VehicleDesign();
        design.Parts.Add(new R3PartState(Vector3Int.zero,R3PartKind.Core));
        design.Parts.Add(new R3PartState(Vector3Int.up,R3PartKind.Battery));
        design.Parts.Add(new R3PartState(Vector3Int.right,R3PartKind.DrivePower));
        design.Parts.Add(new R3PartState(Vector3Int.left,R3PartKind.DrivePower));
        var method=typeof(R1RunHUD).GetMethod("StarterVehicleSummary",BindingFlags.Static|BindingFlags.NonPublic);
        Assert.That(method,Is.Not.Null);
        var summary=(string)method.Invoke(null,new object[]{design});
        Assert.That(summary,Does.Contain("Energia: 550"),"500 core + 100 battery - 2×25 passive reservation.");
    }
    [Test] public void GarageMotorAdviceDoesNotClaimAnArbitraryCap()
    {
        var method=typeof(R1RunHUD).GetMethod("GarageWheelPowerAdvice",BindingFlags.Static|BindingFlags.NonPublic);
        Assert.That(method,Is.Not.Null);
        string below=(string)method.Invoke(null,new object[]{4f});
        string many=(string)method.Invoke(null,new object[]{12f});
        Assert.That(below,Does.Contain("dołóż silnik"));
        Assert.That(many,Does.Not.Contain("Limit silników"));
        Assert.That(many,Does.Contain("dołóż silnik"));
        Assert.That(many,Does.Contain("koła"));
    }
    [TestCase(0f,100f,0f)]
    [TestCase(50f,100f,.5f)]
    [TestCase(110f,100f,1f)]
    [TestCase(-1f,100f,0f)]
    [TestCase(1f,0f,0f)]
    public void ResourceBarsClampToSafeFractions(float value,float maximum,float expected)
    {
        var method=typeof(R1RunHUD).GetMethod("BarFraction",BindingFlags.Static|BindingFlags.NonPublic);
        Assert.That(method,Is.Not.Null);Assert.That((float)method.Invoke(null,new object[]{value,maximum}),Is.EqualTo(expected).Within(.0001f));
        Assert.That((float)method.Invoke(null,new object[]{float.NaN,maximum}),Is.Zero);
    }
    [Test] public void CombatWarningsExplainLowEnergyAndPrioritizeCriticalCore()
    {
        var method=typeof(R1RunHUD).GetMethod("CombatResourceWarning",BindingFlags.Static|BindingFlags.NonPublic);
        Assert.That(method,Is.Not.Null);
        string Warning(float hp,float maxHp,float energy,float maxEnergy)=>(string)method.Invoke(null,new object[]{hp,maxHp,energy,maxEnergy});
        Assert.That(Warning(100,100,500,500),Is.Null);
        Assert.That(Warning(100,100,100,500),Does.Contain("energia").IgnoreCase.And.Contain("postój"));
        Assert.That(Warning(25,100,100,500),Does.Contain("rdzenia").IgnoreCase);
        Assert.That(Warning(26,100,101,500),Is.Null);
        Assert.That(Warning(float.NaN,100,float.NaN,500),Is.Null);
        Assert.That(Warning(1,0,1,0),Is.Null,"Missing resource data must not produce a false alert.");
    }
    [Test] public void EquipmentWarningsDistinguishLastWheelAndInactiveWeapons()
    {
        var method=typeof(R1RunHUD).GetMethod("CombatEquipmentWarning",BindingFlags.Static|BindingFlags.NonPublic);
        Assert.That(method,Is.Not.Null);
        string Warning(R3DriveKind drive,int initial,int surviving,bool inactive)=>(string)method.Invoke(null,new object[]{drive,initial,surviving,inactive});
        Assert.That(Warning(R3DriveKind.Wheels,6,6,false),Is.Null);
        Assert.That(Warning(R3DriveKind.Wheels,6,1,false),Does.Contain("koło").And.Contain("porażk"));
        Assert.That(Warning(R3DriveKind.Hover,6,1,false),Is.Null,"Wheels are not the selected drive.");
        Assert.That(Warning(R3DriveKind.Wheels,6,4,true),Does.Contain("Uzbrojenie").And.Contain("nieaktywne"));
        Assert.That(Warning(R3DriveKind.Wheels,6,1,true),Does.Contain("koło"),"The imminent loss condition takes priority.");
    }
    [Test] public void EnemyHealthRowsFitExistingPanelWithoutOverlapping()
    {
        var flags=BindingFlags.Static|BindingFlags.NonPublic;
        var label=typeof(R1RunHUD).GetMethod("EnemyStatusLabelRect",flags);
        var bar=typeof(R1RunHUD).GetMethod("EnemyStatusBarRect",flags);
        Assert.That(label,Is.Not.Null);Assert.That(bar,Is.Not.Null);
        var panel=new Rect(925,18,335,160);var previous=new Rect();
        for(int i=0;i<4;i++)
        {
            var text=(Rect)label.Invoke(null,new object[]{i});var fill=(Rect)bar.Invoke(null,new object[]{i});
            Assert.That(panel.Contains(text.min)&&panel.Contains(text.max-Vector2.one),Is.True);
            Assert.That(panel.Contains(fill.min)&&panel.Contains(fill.max-Vector2.one),Is.True);
            Assert.That(text.Overlaps(fill),Is.False);
            if(i>0)Assert.That(previous.Overlaps(text),Is.False);
            previous=fill;
        }
    }
    [Test] public void DetailsShortcutTogglesWithoutConsumingMovementOrMouse()
    {
        var go=new GameObject("HUD shortcut test");
        try
        {
            var hud=go.AddComponent<R1RunHUD>();var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            var toggle=typeof(R1RunHUD).GetMethod("UpdateCombatDetails",flags);
            var open=typeof(R1RunHUD).GetField("combatDetailsOpen",flags);
            Assert.That(toggle,Is.Not.Null);Assert.That(open,Is.Not.Null);
            foreach(var other in new[]{new Event{type=EventType.KeyDown,keyCode=KeyCode.W},new Event{type=EventType.MouseDown,button=0}})
            {var type=other.type;toggle.Invoke(hud,new object[]{other});Assert.That(other.type,Is.EqualTo(type));Assert.That((bool)open.GetValue(hud),Is.False);}
            var press=new Event{type=EventType.KeyDown,keyCode=KeyCode.I};toggle.Invoke(hud,new object[]{press});
            Assert.That(press.type,Is.EqualTo(EventType.Used));Assert.That((bool)open.GetValue(hud),Is.True);
            toggle.Invoke(hud,new object[]{new Event{type=EventType.KeyDown,keyCode=KeyCode.I}});Assert.That((bool)open.GetValue(hud),Is.False);
        }
        finally{Object.DestroyImmediate(go);}
    }
    [Test] public void CombatDetailsUsesReboundKeyInsteadOfHardcodedI()
    {
        var profile=R1ControlProfile.Current;profile.ResetDefaults();var go=new GameObject("HUD remap test");
        try
        {
            Assert.That(profile.TryBind("CombatDetails","P"),Is.True);
            var hud=go.AddComponent<R1RunHUD>();var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            var toggle=typeof(R1RunHUD).GetMethod("UpdateCombatDetails",flags);var open=typeof(R1RunHUD).GetField("combatDetailsOpen",flags);
            toggle.Invoke(hud,new object[]{new Event{type=EventType.KeyDown,keyCode=KeyCode.I}});Assert.That((bool)open.GetValue(hud),Is.False);
            var key=new Event{type=EventType.KeyDown,keyCode=KeyCode.P};toggle.Invoke(hud,new object[]{key});
            Assert.That((bool)open.GetValue(hud),Is.True);Assert.That(key.type,Is.EqualTo(EventType.Used));
        }
        finally{profile.ResetDefaults();Object.DestroyImmediate(go);}
    }
}
