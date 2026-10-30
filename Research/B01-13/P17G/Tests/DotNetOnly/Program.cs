using System;
using System.Reflection;
using Swarm.B01.Playtesting;
using UnityEngine;
using C = Swarm.B01.Playtesting.InputDiscoveryLedger.Control;

internal static class TestProgram
{
    private static int pass, fail;

    private static void Assert(bool condition, string why = "Assertion failed")
    {
        if (!condition) throw new Exception(why);
    }
    private static void Equal<T>(T expected, T got)
    {
        if (!object.Equals(expected, got))
            throw new Exception("Expected " + expected + " got " + got);
    }
    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new Exception("Expected " + typeof(T).Name);
    }
    private static void Case(string name, Action action)
    {
        try { action(); Console.WriteLine("PASS " + name); pass++; }
        catch (Exception e) { Console.Error.WriteLine("FAIL " + name + " " + e.Message); fail++; }
    }
    private static InputDiscoveryLedger Ledger()
    {
        var l = new InputDiscoveryLedger();
        l.Begin(10.0);
        return l;
    }
    private static void PrivateCall(InputDiscoveryOverlay overlay, string method)
    {
        var m = typeof(InputDiscoveryOverlay).GetMethod(method,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert(m != null);
        try { m!.Invoke(overlay, null); }
        catch (TargetInvocationException e) when (e.InnerException != null)
        { throw e.InnerException; }
    }
    private static InputDiscoveryOverlay Overlay(double now = 10.0)
    {
        Input.Clear(); GUILayout.Clear();
        Time.realtimeSinceStartupAsDouble = now;
        var view = new InputDiscoveryOverlay();
        PrivateCall(view, "OnEnable");
        return view;
    }
    private static void Press(InputDiscoveryOverlay view, double now, KeyCode key)
    {
        Input.Clear(); Time.realtimeSinceStartupAsDouble = now;
        Input.Keys.Add(key);
        PrivateCall(view, "Update");
    }
    private static void MousePress(InputDiscoveryOverlay view, double now, int button)
    {
        Input.Clear(); Time.realtimeSinceStartupAsDouble = now;
        Input.Mouse.Add(button);
        PrivateCall(view, "Update");
    }

    public static int Main()
    {
        Case("seven_categorized_raw_inputs", () => Equal(7, InputDiscoveryLedger.ControlCount));
        Case("ledger_starts_explicitly", () => Equal(false, new InputDiscoveryLedger().IsStarted));
        Case("begin_sets_started", () => Equal(true, Ledger().IsStarted));
        Case("begin_starts_with_zero_seen", () => Equal(0, Ledger().SeenCount));
        Case("begin_starts_with_seven_unseen", () => Equal(7, Ledger().UnseenCount));
        Case("unseen_first_press_null", () => Equal<long?>(null, Ledger().FirstPressMilliseconds(C.KeyboardW)));
        Case("first_press_at_zero_ms", () => Equal(true, Ledger().Observe(C.KeyboardW, 10.0)));
        Case("first_press_1500_ms", () =>
        {
            var l=Ledger(); l.Observe(C.KeyboardD,11.5);Equal<long?>(1500,l.FirstPressMilliseconds(C.KeyboardD));
        });
        Case("fractional_milliseconds_floored", () =>
        {
            var l=Ledger();l.Observe(C.MouseLeft,10.2505);
            Equal<long?>(250,l.FirstPressMilliseconds(C.MouseLeft));
        });
        Case("seven_different_inputs_observed", () =>
        {
            var l=Ledger();for(int i=0;i<7;i++)l.Observe((C)i, 10.2+i*0.1);
            Equal(7,l.SeenCount); Equal(0,l.UnseenCount);
        });
        Case("repeat_returns_false", () =>
        {
            var l=Ledger();l.Observe(C.KeyboardW,10.1);
            Equal(false,l.Observe(C.KeyboardW,10.2));
        });
        Case("repeat_preserves_first_time", () =>
        {
            var l=Ledger();l.Observe(C.KeyboardW,10.1);l.Observe(C.KeyboardW,11.0);
            Equal<long?>(99,l.FirstPressMilliseconds(C.KeyboardW));
        });
        Case("repeat_counter_increments", () =>
        {
            var l=Ledger();l.Observe(C.KeyboardW,10.1);l.Observe(C.KeyboardW,11.0);
            Equal(1,l.RepeatCount(C.KeyboardW));
        });
        Case("repeat_does_not_increment_seen_count", () =>
        {
            var l=Ledger();l.Observe(C.MouseRight,10.1);l.Observe(C.MouseRight,12.0);
            Equal(1,l.SeenCount);
        });
        Case("begin_resets_first_press", () =>
        {
            var l=Ledger();l.Observe(C.KeyboardW,10.1);l.Begin(13.0);
            Equal<long?>(null,l.FirstPressMilliseconds(C.KeyboardW));
        });
        Case("begin_resets_repeat_counts", () =>
        {
            var l=Ledger();l.Observe(C.KeyboardW,10.1);l.Observe(C.KeyboardW,10.2);l.Begin(13);
            Equal(0,l.RepeatCount(C.KeyboardW));
        });
        Case("new_session_relative_time", () =>
        {
            var l=Ledger();l.Begin(99);l.Observe(C.KeyboardW,100);
            Equal<long?>(1000,l.FirstPressMilliseconds(C.KeyboardW));
        });
        Case("record_without_begin_rejected", () =>
            Throws<InvalidOperationException>(() => new InputDiscoveryLedger().Observe(C.KeyboardW,1)));
        Case("invalid_action_negative_rejected", () =>
            Throws<ArgumentOutOfRangeException>(() => Ledger().Observe((C)(-1), 11)));
        Case("invalid_action_overflow_rejected", () =>
            Throws<ArgumentOutOfRangeException>(() => Ledger().Observe((C)7,11)));
        Case("invalid_action_get_first_rejected", () =>
            Throws<ArgumentOutOfRangeException>(() => Ledger().FirstPressMilliseconds((C)123)));
        Case("invalid_action_get_repeat_rejected", () =>
            Throws<ArgumentOutOfRangeException>(() => Ledger().RepeatCount((C)123)));
        Case("invalid_action_display_rejected", () =>
            Throws<ArgumentOutOfRangeException>(() => InputDiscoveryLedger.DisplayName((C)123)));
        Case("all_display_names_distinct", () =>
        {
            var names=new System.Collections.Generic.HashSet<string>();
            for(int i=0;i<7;i++)Assert(names.Add(InputDiscoveryLedger.DisplayName((C)i)));
        });
        Case("nan_begin_rejected", () =>
            Throws<ArgumentOutOfRangeException>(()=>new InputDiscoveryLedger().Begin(double.NaN)));
        Case("infinite_begin_rejected", () =>
            Throws<ArgumentOutOfRangeException>(()=>new InputDiscoveryLedger().Begin(double.PositiveInfinity)));
        Case("negative_begin_rejected", () =>
            Throws<ArgumentOutOfRangeException>(()=>new InputDiscoveryLedger().Begin(-1)));
        Case("nan_observation_rejected", () =>
            Throws<ArgumentOutOfRangeException>(()=>Ledger().Observe(C.KeyboardW,double.NaN)));
        Case("infinity_observation_rejected", () =>
            Throws<ArgumentOutOfRangeException>(()=>Ledger().Observe(C.KeyboardW,double.PositiveInfinity)));
        Case("time_reversal_rejected", () =>
        {
            var l=Ledger();l.Observe(C.KeyboardA,15);
            Throws<ArgumentOutOfRangeException>(()=>l.Observe(C.KeyboardW,14));
            Equal<long?>(null,l.FirstPressMilliseconds(C.KeyboardW));
        });
        Case("before_start_time_rejected", () =>
            Throws<ArgumentOutOfRangeException>(()=>Ledger().Observe(C.KeyboardW,9.99)));
        Case("six_hours_exactly_supported", () =>
        {
            var l=Ledger(); l.Observe(C.KeyboardW,10+21600);
            Equal<long?>(21600000,l.FirstPressMilliseconds(C.KeyboardW));
        });
        Case("over_six_hours_rejected", () =>
            Throws<ArgumentOutOfRangeException>(()=>Ledger().Observe(C.KeyboardW,10+21600.001)));
        Case("same_timestamp_multiple_inputs", () =>
        {
            var l=Ledger();l.Observe(C.KeyboardW,11);l.Observe(C.KeyboardA,11);
            Equal<long?>(1000,l.FirstPressMilliseconds(C.KeyboardA));
        });
        Case("overlay_initially_visible", () => Equal(true,Overlay().PanelVisible));
        Case("overlay_initially_empty", () => Equal(0,Overlay().SeenCount));
        Case("overlay_w_measures_first_input_from_unscaled_clock", () =>
        {
            var o=Overlay();Press(o,11.0,KeyCode.W);
            Equal<long?>(1000,o.FirstPressMilliseconds(C.KeyboardW));
        });
        Case("overlay_different_wasd_logged", () =>
        {
            var o=Overlay();Press(o,11,KeyCode.W);Press(o,12,KeyCode.A);
            Press(o,13,KeyCode.S);Press(o,14,KeyCode.D);
            Equal(4,o.SeenCount);
        });
        Case("overlay_space_not_inferred_to_boost", () =>
        {
            var o=Overlay();Press(o,11,KeyCode.Space);
            Equal<long?>(1000,o.FirstPressMilliseconds(C.KeyboardSpace));
        });
        Case("overlay_both_mouse_buttons_captured", () =>
        {
            var o=Overlay();MousePress(o,11,0);MousePress(o,12,1);
            Equal(2,o.SeenCount);
        });
        Case("overlay_f8_hides_without_counting", () =>
        {
            var o=Overlay();Press(o,11,KeyCode.F8);
            Equal(false,o.PanelVisible);Equal(0,o.SeenCount);
        });
        Case("overlay_f8_restores_view", () =>
        {
            var o=Overlay();Press(o,11,KeyCode.F8);Press(o,12,KeyCode.F8);
            Equal(true,o.PanelVisible);
        });
        Case("overlay_hidden_still_observes_input", () =>
        {
            var o=Overlay();Press(o,11,KeyCode.F8);Press(o,12,KeyCode.W);
            Equal<long?>(2000,o.FirstPressMilliseconds(C.KeyboardW));
        });
        Case("overlay_legacy_new_only_reports_unavailable", () =>
        {
            var o=Overlay();Input.FailLegacy=true;PrivateCall(o,"Update");
            Equal(true,o.LegacyInputUnsupported);Equal(0,o.SeenCount);
        });
        Case("overlay_legacy_unavailable_show_warning", () =>
        {
            var o=Overlay();Input.FailLegacy=true;PrivateCall(o,"Update");
            PrivateCall(o,"OnGUI");
            Assert(GUILayout.Labels.Exists(s=>s.Contains("Input Manager")));
        });
        Case("overlay_unavailable_does_not_repoll", () =>
        {
            var o=Overlay();Input.FailLegacy=true;PrivateCall(o,"Update");
            Input.Clear();Time.realtimeSinceStartupAsDouble=12;
            Input.Keys.Add(KeyCode.W);PrivateCall(o,"Update");
            Equal(0,o.SeenCount);
        });
        Case("overlay_gui_displays_observed_versus_unobserved", () =>
        {
            var o=Overlay();Press(o,11,KeyCode.W);PrivateCall(o,"OnGUI");
            Assert(GUILayout.Labels.Exists(s=>s.Contains("W: 1000 ms")));
            Assert(GUILayout.Labels.Exists(s=>s.Contains("D: BRAK ODCZYTU")));
        });
        Case("overlay_hidden_gui_does_not_draw", () =>
        {
            var o=Overlay();Press(o,11,KeyCode.F8);PrivateCall(o,"OnGUI");
            Equal(0,GUILayout.Labels.Count);
        });
        Case("overlay_reset_button_clears_and_resets_elapsed_origin", () =>
        {
            var o=Overlay();Press(o,11,KeyCode.W);
            Time.realtimeSinceStartupAsDouble=20;GUILayout.NextButtonPress=true;
            PrivateCall(o,"OnGUI");Equal(0,o.SeenCount);
            Press(o,21,KeyCode.W);Equal<long?>(1000,o.FirstPressMilliseconds(C.KeyboardW));
        });
        Case("overlay_reset_button_recovers_input_compatibility_guard", () =>
        {
            var o=Overlay();Input.FailLegacy=true;PrivateCall(o,"Update");
            Time.realtimeSinceStartupAsDouble=12;GUILayout.NextButtonPress=true;
            PrivateCall(o,"OnGUI");Equal(false,o.LegacyInputUnsupported);
        });
        Case("overlay_on_enable_clears_previous_state", () =>
        {
            var o=Overlay();Press(o,11,KeyCode.W);
            Time.realtimeSinceStartupAsDouble=30;PrivateCall(o,"OnEnable");
            Equal(0,o.SeenCount);Equal<long?>(null,o.FirstPressMilliseconds(C.KeyboardW));
        });

        Console.WriteLine("TOTAL " + pass + " PASS " + fail + " FAIL");
        return fail==0?0:1;
    }
}
