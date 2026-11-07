using System.Reflection;
using NUnit.Framework;
using RoboRougelike.R1;
using UnityEngine;

public sealed class CompactCombatHUDTests
{
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
