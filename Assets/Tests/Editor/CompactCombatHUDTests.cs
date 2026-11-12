using System.Reflection;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using UnityEngine;

public sealed class CompactCombatHUDTests
{
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
    [Test] public void GarageMotorAdviceDoesNotSuggestAddingPowerAtTheCap()
    {
        var method=typeof(R1RunHUD).GetMethod("GarageWheelPowerAdvice",BindingFlags.Static|BindingFlags.NonPublic);
        Assert.That(method,Is.Not.Null);
        string below=(string)method.Invoke(null,new object[]{4f});
        string capped=(string)method.Invoke(null,new object[]{5f});
        Assert.That(below,Does.Contain("dołóż silnik"));
        Assert.That(capped,Does.Contain("Limit silników"));
        Assert.That(capped,Does.Not.Contain("dołóż silnik"));
        Assert.That(capped,Does.Contain("koła"));
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
}
