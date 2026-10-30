using System;
using System.Reflection;
using Swarm.B01.Playtesting;
using UnityEngine;
using S = Swarm.B01.Playtesting.MotionStallLedger.State;

internal static class Program
{
    private static int passed, failed;
    private static void Equal<T>(T want, T got)
    {
        if (!object.Equals(want, got)) throw new Exception("expected " + want + ", got " + got);
    }
    private static void True(bool actual) { if (!actual) throw new Exception("expected true"); }
    private static void False(bool actual) { if (actual) throw new Exception("expected false"); }
    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new Exception("Expected " + typeof(T).Name);
    }
    private static void Case(string title, Action act)
    {
        try { act(); Console.WriteLine("PASS " + title); passed++; }
        catch (Exception ex) { Console.Error.WriteLine("FAIL " + title + " : " + ex.Message); failed++; }
    }
    private static MotionStallLedger New()
    {
        var state = new MotionStallLedger();
        state.Begin(10.0, 0.0, 0.0);
        return state;
    }
    private static void Call(MotionStallOverlay obj, string method)
    {
        var m = typeof(MotionStallOverlay).GetMethod(method, BindingFlags.NonPublic|BindingFlags.Instance);
        if (m == null) throw new Exception("missing method " + method);
        try { m.Invoke(obj, null); }
        catch (TargetInvocationException e) when (e.InnerException != null) { throw e.InnerException; }
    }
    private static MotionStallOverlay NewView(out Transform transform)
    {
        Input.Reset(); GUILayout.Reset(); Time.realtimeSinceStartupAsDouble = 10;
        transform = new Transform { position = new Vector3(0, 5, 0) };
        var o = new MotionStallOverlay();
        typeof(MotionStallOverlay).GetField("observedVehicle",
            BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o, transform);
        Call(o, "OnEnable");
        return o;
    }
    private static void Frame(MotionStallOverlay view, Transform transform, double time,
                              float x, float z, bool w, bool s)
    {
        Time.realtimeSinceStartupAsDouble = time;
        transform.position = new Vector3(x, 5, z);
        Input.Reset();
        if (w) Input.KeysHeld.Add(KeyCode.W);
        if (s) Input.KeysHeld.Add(KeyCode.S);
        Call(view, "Update");
    }
    public static int Main()
    {
        Case("construct_unstarted", () => False(new MotionStallLedger().Started));
        Case("begin_started", () => True(New().Started));
        Case("begin_idle", () => Equal(S.Idle, New().CurrentState));
        Case("begin_counts_zero", () => { var l=New();Equal(0,l.AttemptCount);Equal(0,l.SuspectedStallCount);Equal(0,l.RecoveryCount); });
        Case("requires_begin", () => Throws<InvalidOperationException>(()=>new MotionStallLedger().Observe(1,0,0,true)));
        Case("first_attempt_starts", () => { var l=New();False(l.Observe(10.1,0,0,true));Equal(S.Attempting,l.CurrentState);Equal(1,l.AttemptCount); });
        Case("attempt_with_input_released_idle", () => { var l=New();l.Observe(10.1,0,0,true);l.Observe(10.2,0,0,false);Equal(S.Idle,l.CurrentState); });
        Case("no_input_no_stall", () => { var l=New();for(int i=1;i<=30;i++)l.Observe(10+i*.1,0,0,false);Equal(0,l.SuspectedStallCount); });
        Case("short_stillness_no_stall", () => { var l=New();for(int i=1;i<=20;i++)l.Observe(10+i*.1,0,0,true);Equal(0,l.SuspectedStallCount); });
        Case("two_seconds_suspected", () => { var l=New();bool hit=false;for(int i=0;i<=21;i++)hit |= l.Observe(10+i*.1,0,0,true);True(hit);Equal(S.SuspectedStall,l.CurrentState); });
        Case("one_event_only_while_still", () => { var l=New();int hits=0;for(int i=0;i<=50;i++)if(l.Observe(10+i*.1,0,0,true))hits++;Equal(1,hits);Equal(1,l.SuspectedStallCount); });
        Case("release_before_threshold_cancels", () => { var l=New();for(int i=0;i<=10;i++)l.Observe(10+i*.1,0,0,true);l.Observe(11.1,0,0,false);for(int i=12;i<=20;i++)l.Observe(10+i*.1,0,0,true);Equal(0,l.SuspectedStallCount); });
        Case("movement_resets_stillness_window", () => { var l=New();for(int i=0;i<=15;i++)l.Observe(10+i*.1,0,0,true);l.Observe(11.6,1,0,true);for(int i=17;i<=30;i++)l.Observe(10+i*.1,1,0,true);Equal(0,l.SuspectedStallCount); });
        Case("recovery_requires_significant_motion", () => { var l=New();for(int i=0;i<=21;i++)l.Observe(10+i*.1,0,0,true);l.Observe(12.2,0.002,0,true);Equal(0,l.RecoveryCount);Equal(S.SuspectedStall,l.CurrentState); });
        Case("motion_recovery_after_stall", () => { var l=New();for(int i=0;i<=21;i++)l.Observe(10+i*.1,0,0,true);l.Observe(12.2,1,0,true);Equal(1,l.RecoveryCount);Equal(S.JustRecovered,l.CurrentState); });
        Case("recovered_not_double_counted", () => { var l=New();for(int i=0;i<=21;i++)l.Observe(10+i*.1,0,0,true);l.Observe(12.2,1,0,true);l.Observe(12.3,2,0,true);Equal(1,l.RecoveryCount);Equal(S.Attempting,l.CurrentState); });
        Case("can_stall_again_after_recovery", () => { var l=New();for(int i=0;i<=21;i++)l.Observe(10+i*.1,0,0,true);l.Observe(12.2,1,0,true);for(int i=23;i<=44;i++)l.Observe(10+i*.1,1,0,true);Equal(2,l.SuspectedStallCount); });
        Case("releasing_stall_is_not_recovery", () => { var l=New();for(int i=0;i<=21;i++)l.Observe(10+i*.1,0,0,true);l.Observe(12.2,0,0,false);Equal(0,l.RecoveryCount);Equal(S.Idle,l.CurrentState); });
        Case("new_attempt_count_after_release", () => { var l=New();l.Observe(10.1,0,0,true);l.Observe(10.2,0,0,false);l.Observe(10.3,0,0,true);Equal(2,l.AttemptCount); });
        Case("z_axis_movement_counts", () => { var l=New();l.Observe(10,0,0,true);l.Observe(10.1,0,1,true);Equal(S.Attempting,l.CurrentState); });
        Case("vertical_motion_ignored", () => { var l=New();for(int i=0;i<=21;i++)l.Observe(10+i*.1,0,0,true);Equal(1,l.SuspectedStallCount); });
        Case("teleport_discontinuity_cancels", () => { var l=New();l.Observe(10,0,0,true);l.Observe(10.1,55,0,true);Equal(1,l.DiscontinuityCount);Equal(0,l.SuspectedStallCount);Equal(S.Idle,l.CurrentState); });
        Case("teleport_does_not_fake_recovery", () => { var l=New();for(int i=0;i<=21;i++)l.Observe(10+i*.1,0,0,true);l.Observe(12.2,90,0,true);Equal(0,l.RecoveryCount); });
        Case("sample_gap_cancels", () => { var l=New();l.Observe(10.1,0,0,true);l.Observe(11,0,0,true);Equal(1,l.DiscontinuityCount);Equal(S.Idle,l.CurrentState); });
        Case("sample_gap_cannot_false_stall", () => { var l=New();l.Observe(10.1,0,0,true);l.Observe(13,0,0,true);Equal(0,l.SuspectedStallCount); });
        Case("exact_max_gap_supported", () => { var l=New();l.Observe(10,0,0,true);l.Observe(10.5,0,0,true);Equal(0,l.DiscontinuityCount); });
        Case("equal_clock_safe", () => { var l=New();l.Observe(10,0,0,true);False(l.Observe(10,0,0,true));Equal(0,l.DiscontinuityCount); });
        Case("reversed_clock_rejected", () => { var l=New();l.Observe(11,0,0,true);Throws<ArgumentOutOfRangeException>(()=>l.Observe(10,0,0,true)); });
        Case("time_nan_rejected", () => Throws<ArgumentOutOfRangeException>(()=>New().Observe(double.NaN,0,0,true)));
        Case("time_infinite_rejected", () => Throws<ArgumentOutOfRangeException>(()=>New().Observe(double.PositiveInfinity,0,0,true)));
        Case("time_negative_rejected_on_begin", () => Throws<ArgumentOutOfRangeException>(()=>new MotionStallLedger().Begin(-1,0,0)));
        Case("x_nan_rejected", () => Throws<ArgumentOutOfRangeException>(()=>New().Observe(11,double.NaN,0,true)));
        Case("z_inf_rejected", () => Throws<ArgumentOutOfRangeException>(()=>New().Observe(11,0,double.NegativeInfinity,true)));
        Case("overflow_world_delta_rejected", () => { var l=New();l.Begin(10,double.MaxValue,0);Throws<ArgumentOutOfRangeException>(()=>l.Observe(11,-double.MaxValue,0,true)); });
        Case("bad_sample_does_not_mutate_state", () => { var l=New();l.Observe(10.1,0,0,true);Throws<ArgumentOutOfRangeException>(()=>l.Observe(10.05,1,0,true));Equal(S.Attempting,l.CurrentState); });
        Case("begin_resets_all_counters", () => { var l=New();for(int i=0;i<=21;i++)l.Observe(10+i*.1,0,0,true);l.Begin(50,1,2);Equal(0,l.AttemptCount);Equal(0,l.SuspectedStallCount);Equal(S.Idle,l.CurrentState); });
        Case("begin_invalid_preserves_existing", () => { var l=New();l.Observe(10.1,0,0,true);Throws<ArgumentOutOfRangeException>(()=>l.Begin(double.NaN,0,0));Equal(1,l.AttemptCount); });
        Case("speed_threshold_no_false_stall", () => { var l=New();for(int i=0;i<=25;i++)l.Observe(10+i*.1,i*.02,0,true);Equal(0,l.SuspectedStallCount); });
        Case("threshold_small_speed_flags", () => { var l=New();for(int i=0;i<=21;i++)l.Observe(10+i*.1,i*.001,0,true);Equal(1,l.SuspectedStallCount); });
        Case("view_initial_state", () => { Transform t; var view=NewView(out t);Equal(0,view.StallCount);Equal(S.Idle,view.CurrentState); });
        Case("view_requires_target_for_sample", () => { Transform t; var view=NewView(out t);typeof(MotionStallOverlay).GetField("observedVehicle",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(view,null);Frame(view,t,11,0,0,true,false);Equal(0,view.StallCount); });
        Case("view_w_held_detects_stall", () => { Transform t; var view=NewView(out t);for(int i=0;i<=21;i++)Frame(view,t,10+i*.1,0,0,true,false);Equal(1,view.StallCount); });
        Case("view_s_held_detects_stall", () => { Transform t;var view=NewView(out t);for(int i=0;i<=21;i++)Frame(view,t,10+i*.1,0,0,false,true);Equal(1,view.StallCount); });
        Case("view_key_release_no_stall", () => { Transform t;var view=NewView(out t);for(int i=0;i<=21;i++)Frame(view,t,10+i*.1,0,0,false,false);Equal(0,view.StallCount); });
        Case("view_recovery_from_transform_motion", () => { Transform t;var view=NewView(out t);for(int i=0;i<=21;i++)Frame(view,t,10+i*.1,0,0,true,false);Frame(view,t,12.2,1,0,true,false);Equal(1,view.RecoveryCount); });
        Case("view_f7_hides", () => { Transform t;var view=NewView(out t);Input.KeysDown.Add(KeyCode.F7);Call(view,"Update");Equal(false,view.IsVisible); });
        Case("view_f7_shows", () => { Transform t;var view=NewView(out t);Input.KeysDown.Add(KeyCode.F7);Call(view,"Update");Input.KeysDown.Clear();Input.KeysDown.Add(KeyCode.F7);Call(view,"Update");Equal(true,view.IsVisible); });
        Case("view_input_unavailable_fail_closed", () => { Transform t;var view=NewView(out t);Input.FailLegacy=true;Call(view,"Update");True(view.InputUnsupported);Equal(0,view.StallCount); });
        Case("view_incompatible_does_not_claim_stall", () => { Transform t;var view=NewView(out t);Input.FailLegacy=true;Call(view,"Update");Input.FailLegacy=false;for(int i=0;i<=21;i++)Frame(view,t,10+i*.1,0,0,true,false);Equal(0,view.StallCount); });
        Case("view_incompatible_gui_warning", () => { Transform t;var view=NewView(out t);Input.FailLegacy=true;Call(view,"Update");Call(view,"OnGUI");True(GUILayout.Labels.Exists(x=>x.Contains("INPUT NIEZGODNY"))); });
        Case("view_no_target_gui_warning", () => { Transform t;var view=NewView(out t);typeof(MotionStallOverlay).GetField("observedVehicle",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(view,null);Call(view,"OnGUI");True(GUILayout.Labels.Exists(x=>x.Contains("BRAK Transform"))); });
        Case("view_gui_reports_status", () => { Transform t;var view=NewView(out t);Call(view,"OnGUI");True(GUILayout.Labels.Exists(x=>x.Contains("Podejrzenia: 0"))); });
        Case("view_hidden_no_gui", () => { Transform t;var view=NewView(out t);Input.KeysDown.Add(KeyCode.F7);Call(view,"Update");Call(view,"OnGUI");Equal(0,GUILayout.Labels.Count); });
        Case("view_reset_button_clears", () => { Transform t;var view=NewView(out t);for(int i=0;i<=21;i++)Frame(view,t,10+i*.1,0,0,true,false);Time.realtimeSinceStartupAsDouble=20;GUILayout.NextButton=true;Call(view,"OnGUI");Equal(0,view.StallCount); });
        Case("view_reset_clears_input_error", () => { Transform t;var view=NewView(out t);Input.FailLegacy=true;Call(view,"Update");Time.realtimeSinceStartupAsDouble=20;GUILayout.NextButton=true;Call(view,"OnGUI");False(view.InputUnsupported); });
        Case("view_on_enable_resets", () => { Transform t;var view=NewView(out t);for(int i=0;i<=21;i++)Frame(view,t,10+i*.1,0,0,true,false);Time.realtimeSinceStartupAsDouble=30;Call(view,"OnEnable");Equal(0,view.StallCount); });
        Console.WriteLine("TOTAL " + passed + " PASS " + failed + " FAIL");
        return failed == 0 ? 0 : 1;
    }
}
