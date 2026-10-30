using System;
using System.Reflection;
using RogueMachine.P12PipelineSafe;
using RoboRougelike.B0131.Art;
using UnityEngine;

static class Program
{
    private static int passed;
    static void Check(bool yes,string name)
    {
        if(!yes)throw new InvalidOperationException("P12_W25_REAL_CSHARP_SOURCE_RED "+name);
        passed++;
        Console.WriteLine("ORIGINAL_P12_W24_W25_SOURCE_TEST_PASS "+passed+" "+name);
    }
    static P12PipelineSafeDamageable Source(float max=100)
    {
        var s=new P12PipelineSafeDamageable();
        s.Configure(max,false); // real original P12 method, no fake P12 implementation
        return s;
    }
    static B0131W24ArmorDamageVisual Visual(out GameObject ok,out GameObject damaged,out GameObject destroyed)
    {
        var v=new B0131W24ArmorDamageVisual();
        ok=new GameObject();damaged=new GameObject();destroyed=new GameObject();
        if(!v.ConfigureVisuals(ok,damaged,destroyed))throw new Exception("original W24 visual config failed");
        return v;
    }
    static void Member(object target,string name,object value)
    {
        var field=target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic);
        if(field==null)throw new Exception("Original P12 private source ABI drift "+name);
        field.SetValue(target,value);
    }
    static void Method(object target,string name)
    {
        var method=target.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic);
        if(method==null)throw new Exception("Original W25 Unity lifecycle ABI drift "+name);
        method.Invoke(target,null);
    }
    static void Main()
    {
        var bind=new B0131W25P12DamageVisualBinding();
        var s=Source();
        var v=Visual(out var ok,out var damage,out var destroyed);
        Check(!bind.IsBound,"unbound-by-default");
        Check(!bind.RefreshVisual(),"unbound-fails-closed");
        Check(!bind.Bind(null!,v),"reject-null-P12");
        Check(!bind.Bind(s,null!),"reject-null-W24-visual");
        Check(!bind.SetDamageThreshold(float.NaN),"reject-NaN-threshold");
        Check(!bind.SetDamageThreshold(float.PositiveInfinity),"reject-Infinity-threshold");
        Check(!bind.SetDamageThreshold(0f),"reject-0-threshold");
        Check(!bind.SetDamageThreshold(1f),"reject-1-threshold");
        Check(bind.Bind(s,v),"bind-actual-P12-W24");
        Check(bind.IsBound && ok.activeSelf&&!damage.activeSelf&&!destroyed.activeSelf,
              "real-full-hp-armor-OK");

        var before=s.CurrentHp;
        Check(bind.RefreshVisual() && before==s.CurrentHp,
              "read-only-refresh-does-not-touch-authoritative-P12-hp");
        s.ApplyDamage(49f,Vector3.zero,Vector3.up);
        Check(bind.RefreshVisual() && s.CurrentHp==51 &&
              ok.activeSelf&&!damage.activeSelf,"49-damage-still-OK");
        s.ApplyDamage(1f,Vector3.zero,Vector3.up);
        Check(bind.RefreshVisual() && s.CurrentHp==50 && damage.activeSelf&&!ok.activeSelf,
              "50pct-threshold-damaged-inclusive");
        s.RestoreHealthRatio(0.85f);
        Check(bind.RefreshVisual() && ok.activeSelf&&!damage.activeSelf,
              "P12-real-repair-restores-OK-geometry");
        Check(bind.SetDamageThreshold(.9f) && damage.activeSelf,
              "live-threshold-update-recomputes-from-original-P12");
        Check(bind.SetDamageThreshold(.25f) && ok.activeSelf,
              "threshold-lowered-returns-OK");
        Check(!bind.SetDamageThreshold(-.2f) && bind.DamageThreshold==.25f && ok.activeSelf,
              "invalid-threshold-unchanged-state");
        // Invalid P12 API snapshot cannot pretend OK or force destruction.
        Member(s,"currentHp",float.NaN);
        Check(!bind.RefreshVisual() && ok.activeSelf,
              "NaN-authoritative-HP-reject-without-state-corruption");
        Member(s,"currentHp",110f);
        Check(!bind.RefreshVisual() && ok.activeSelf,
              "more-than-maximum-HP-fail-closed");
        Member(s,"maxHp",0f);
        Check(!bind.RefreshVisual() && ok.activeSelf,
              "invalid-MaxHP-fail-closed");
        Member(s,"maxHp",100f);Member(s,"currentHp",85f);

        // The actual original P12 Damageable event is emitted during Die.
        // No fake Damageable class, real ApplyDamage->Die->Died event chain.
        s.ApplyDamage(100f,Vector3.zero,Vector3.up);
        Check(s.IsDead && destroyed.activeSelf&&!damage.activeSelf&&!ok.activeSelf,
              "real-P12-Die-event-immediate-destroyed-visual");
        Check(bind.RefreshVisual()&&destroyed.activeSelf,
              "death-latched-not-implicitly-repaired");
        var s2=Source();
        Check(bind.Bind(s2,v) && ok.activeSelf&&!destroyed.activeSelf,
              "explicit-new-P12-rebind-clears-old-death-latch");
        // Old original P12 source event cannot mutate new bound visual state.
        Check(!bind.Bind(null!,v) && bind.IsBound && ok.activeSelf,
              "failed-NULL-rebind-retains-original-binding");
        Check(bind.SetDamageThreshold(.5f),"restore-default-threshold");
        s2.ApplyDamage(70f,Vector3.zero,Vector3.up);
        Check(bind.RefreshVisual() && damage.activeSelf && s2.CurrentHp==30f,
              "actual-P12-damage-automatically-maps-to-partial-armor");
        Method(bind,"OnDisable");bind.isActiveAndEnabled=false;
        s2.ApplyDamage(40f,Vector3.zero,Vector3.up);
        Check(s2.IsDead && damage.activeSelf&&!destroyed.activeSelf,
              "disabled-source-Died-unsubscribed-no-mutation");
        bind.isActiveAndEnabled=true;Method(bind,"OnEnable");
        Check(destroyed.activeSelf && bind.RefreshVisual(),
              "reenabled-reads-real-P12-IsDead-and-recovers-destroyed-display");
        Check(passed==28,"28-source-ABI-and-real-P12-event-tests");
        Console.WriteLine("B0131_W25_REAL_P12_W24_W25_DIRECT_CSHARP_SOURCE_29_CASES_PASS__UNITY_NATIVE_G1_NOT_RUN");
    }
}
