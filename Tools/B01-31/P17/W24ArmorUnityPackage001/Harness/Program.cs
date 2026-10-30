using System;
using RoboRougelike.B0131.Art;
using UnityEngine;

static class Program
{
    private static int checks;
    private static void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException("MANAGED_STUB_RED "+name);
        Console.WriteLine("MANAGED_STUB_CASE_PASS "+name);
        ++checks;
    }

    public static void Main()
    {
        var visual = new B0131W24ArmorDamageVisual();
        var ok = new GameObject(true);
        var damaged = new GameObject(false);
        var destroyed = new GameObject(false);
        Check(!visual.SetVisualPhase(ArmorVisualPhase.Ok), "01-unconfigured-fail-closed");
        Check(!visual.ConfigureVisuals(ok, ok, destroyed), "02-duplicate-original-refs-rejected");
        Check(!visual.ConfigureVisuals(ok, damaged, null!), "03-null-original-rejected");
        Check(visual.ConfigureVisuals(ok, damaged, destroyed), "04-real-native-source-configure");
        Check(ok.activeSelf && !damaged.activeSelf && !destroyed.activeSelf, "05-initial-only-OK-visible");
        Check(visual.SetVisualPhase(ArmorVisualPhase.Damaged) && !ok.activeSelf && damaged.activeSelf
              && !destroyed.activeSelf && visual.CurrentVisual==ArmorVisualPhase.Damaged,
              "06-damaged-visual-isolation");
        Check(visual.SetVisualPhase(ArmorVisualPhase.Destroyed) && !ok.activeSelf &&
              !damaged.activeSelf && destroyed.activeSelf &&
              visual.CurrentVisual==ArmorVisualPhase.Destroyed,
              "07-destroyed-visual-isolation");
        Check(!visual.SetVisualPhase((ArmorVisualPhase)(-1)) &&
              destroyed.activeSelf && visual.CurrentVisual==ArmorVisualPhase.Destroyed,
              "08-negative-state-no-change");
        Check(!visual.SetVisualPhase((ArmorVisualPhase)(23)) &&
              destroyed.activeSelf && visual.CurrentVisual==ArmorVisualPhase.Destroyed,
              "09-unrecognized-state-no-change");
        Check(!visual.ConfigureVisuals(ok, damaged, damaged) && destroyed.activeSelf,
              "10-failed-reconfiguration-keeps-prior-valid-visual");
        Check(visual.SetVisualPhase(ArmorVisualPhase.Ok) && ok.activeSelf &&
              !damaged.activeSelf && !destroyed.activeSelf,
              "11-back-to-OK");
        Check(checks==11, "12-actual-source-managed-stub-regression-count");
        Console.WriteLine("B0131_W24_ACTUAL_ORIGINAL_CSHARP_SOURCE_MANAGED_STUB_12_PASS__UNITY_G1_NOT_RUN");
    }
}
