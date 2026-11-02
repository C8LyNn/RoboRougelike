using System;
using RogueMachine.P12PipelineSafe;
using RoboRougelike.B0131.Art;
using UnityEngine;
static class TestW27
{
    private static int cases=0;
    private static void Check(bool ok,string name)
    {
        if(!ok) throw new InvalidOperationException("REAL_ORIGINAL_W27_MANAGED_SOURCE_RED: "+name);
        Console.WriteLine("W27_REAL_ORIGINAL_P12_W24_W25_W27_CSHARP_PASS "+(++cases)+" "+name);
    }
    private static (P12PipelineSafeDamageable, B0131W24ArmorDamageVisual,
        B0131W25P12DamageVisualBinding, B0131W27ArmorLiveDemoDriver,
        GameObject,GameObject,GameObject) Setup()
    {
        var p12=new P12PipelineSafeDamageable();
        p12.Configure(100f,false);
        var visual=new B0131W24ArmorDamageVisual();
        var ok=new GameObject(); var damaged=new GameObject(); var destroyed=new GameObject();
        if(!visual.ConfigureVisuals(ok,damaged,destroyed))throw new Exception("original W24 visual missing");
        var binder=new B0131W25P12DamageVisualBinding();
        if(!binder.Bind(p12,visual))throw new Exception("original W25 binding failed");
        var driver=new B0131W27ArmorLiveDemoDriver();
        if(!driver.BindDemo(p12,binder))throw new Exception("W27 original driver binding failed");
        return(p12,visual,binder,driver,ok,damaged,destroyed);
    }
    static void Main()
    {
        var t=Setup();
        Check(t.Item4.IsReady,"01-source-explicitly-bound");
        Check(t.Item5.activeSelf && !t.Item6.activeSelf && !t.Item7.activeSelf,"02-original-OK-start");
        Check(!t.Item4.Damage(0f),"03-no-zero-damage");
        Check(!t.Item4.Damage(-5f),"04-negative-rejected");
        Check(!t.Item4.Damage(float.NaN),"05-NaN-rejected");
        Check(!t.Item4.Damage(float.PositiveInfinity),"06-infinity-rejected");
        Check(t.Item1.CurrentHp==100f,"07-invalid-damage-no-effect-on-original-P12");
        Check(t.Item4.Damage(40f) && t.Item1.CurrentHp==60f,"08-original-P12-health-60");
        Check(t.Item5.activeSelf && !t.Item6.activeSelf,"09-original-geometry-still-OK");
        Check(t.Item4.Damage(10f) && t.Item1.CurrentHp==50f,"10-original-P12-health-50");
        Check(t.Item6.activeSelf && !t.Item5.activeSelf,"11-original-geometry-damaged-50");
        Check(t.Item4.RepairToFull() && t.Item1.CurrentHp==100f,"12-actual-P12-repair");
        Check(t.Item5.activeSelf && !t.Item6.activeSelf,"13-geometry-after-repair-OK");
        Check(t.Item4.Damage(1000f) && t.Item1.IsDead,"14-original-P12-Die");
        Check(t.Item7.activeSelf && !t.Item5.activeSelf && !t.Item6.activeSelf,"15-original-W25-event-destroyed");
        Check(!t.Item4.Damage(1f),"16-no-second-damage-after-death");
        Check(!t.Item4.RepairToFull(),"17-no-corpse-restore");
        Check(t.Item7.activeSelf,"18-original-geometry-remains-destroyed");
        var broken=new B0131W27ArmorLiveDemoDriver();
        Check(!broken.IsReady && !broken.Damage(25f) && !broken.RepairToFull(),"19-unbound-driver-fail-closed");
        Check(!broken.BindDemo(null!,t.Item3),"20-reject-null-real-P12");
        Check(!broken.BindDemo(t.Item1,null!),"21-reject-null-real-binder");
        var t2=Setup();
        Check(t2.Item4.IsReady && t2.Item5.activeSelf,"22-disposable-recreation-real-P12-new-source");
        Check(t2.Item4.Damage(60f) && t2.Item6.activeSelf,"23-new-source-damage-no-shared-state");
        Check(t.Item7.activeSelf,"24-old-dead-model-not-unintentionally-reset");
        Check(cases==24,"25-verified-exact-source-regression-count");
        Console.WriteLine("B0131_W27_ORIGINAL_FOUR_SOURCE_MANAGED_STUB_25_OF_25_PASS__NATIVE_UNITY_NOT_RUN");
    }
}
