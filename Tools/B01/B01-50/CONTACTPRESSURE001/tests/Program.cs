using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using B01.Performance.ContactPressure001;
using B01.Performance.ContactPressure001.Unity;
using UnityEngine;

internal static class Program
{
    private static int passed, failed;
    private static readonly List<string> failures = new List<string>();
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
    private static void Test(string label, Action action)
    {
        try { action(); ++passed; Console.WriteLine("PASS " + label); }
        catch (Exception ex) { ++failed; failures.Add(label + ": " + ex.Message); Console.WriteLine("FAIL " + label + " " + ex); }
    }
    private static void Invoke(object instance, string method, params object[] values)
    {
        MethodInfo m = instance.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic);
        Check(m != null, "Missing original production adapter method " + method);
        try { m.Invoke(instance, values); }
        catch (TargetInvocationException e) when (e.InnerException != null)
        { throw new Exception("Adapter method " + method + " threw " + e.InnerException.GetType().Name, e.InnerException); }
    }
    private static B0150OptionalContactPressureProbe Probe(int history = 4)
    {
        B0150OptionalContactPressureProbe p = new B0150OptionalContactPressureProbe();
        typeof(B0150OptionalContactPressureProbe).GetField("historyFixedTicks", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(p, history);
        Invoke(p, "Awake"); Invoke(p, "OnEnable");
        return p;
    }
    private static void Tick(B0150OptionalContactPressureProbe p) { Invoke(p, "FixedUpdate"); }
    private static void CollisionOf(B0150OptionalContactPressureProbe p, string method, int points)
    { Invoke(p, method, new Collision { contactCount = points }); }

    public static int Main(string[] args)
    {
        if (args.Length != 2 || args[0] != "--receipt") return 3;
        string output = args[1];
        if (File.Exists(output)) { Console.WriteLine("Refuse to overwrite existing receipt"); return 4; }

        Test("history rejects 3", () => { try { new ContactPressureMeter(3); throw new Exception("accepted 3"); } catch (ArgumentOutOfRangeException) {} });
        Test("history rejects 513", () => { try { new ContactPressureMeter(513); throw new Exception("accepted 513"); } catch (ArgumentOutOfRangeException) {} });
        Test("history accepts minimum", () => Check(new ContactPressureMeter(4).HistoryTicks == 4, "min rejected"));
        Test("history accepts maximum", () => Check(new ContactPressureMeter(512).HistoryTicks == 512, "max rejected"));
        Test("empty snapshot is explicit", () => { var s = new ContactPressureMeter(4).Snapshot(); Check(s.ObservedTicks == 0 && s.LatestTick == -1 && s.TotalCallbacks == 0, "bad empty"); });
        Test("cannot record before first tick", () => { var m = new ContactPressureMeter(4); Check(!m.TryRecord(ContactKind.Enter, 1) && m.Snapshot().ContactPoints == 0, "unexpected record"); });
        Test("invalid negative tick fail-closed", () => { var m = new ContactPressureMeter(4); Check(!m.BeginTick(-1) && m.LatestTick == -1, "accepted negative"); });
        Test("first tick begins at arbitrary nonnegative id", () => { var m = new ContactPressureMeter(4); Check(m.BeginTick(99) && m.LatestTick == 99 && m.ObservedTicks == 1, "first failed"); });
        Test("three original callback types and contact points", () => {
            var m = new ContactPressureMeter(4); m.BeginTick(1);
            Check(m.TryRecord(ContactKind.Enter, 2) && m.TryRecord(ContactKind.Stay, 4) &&
                m.TryRecord(ContactKind.Exit, 0), "callback rejected");
            var s = m.Snapshot(); Check(s.EnterCallbacks == 1 && s.StayCallbacks == 1 && s.ExitCallbacks == 1 &&
                s.TotalCallbacks == 3 && s.ContactPoints == 6 && s.PeakCallbacksPerTick == 3, "wrong sums");
        });
        Test("reject duplicate tick without state mutation", () => {
            var m = new ContactPressureMeter(4); m.BeginTick(2); m.TryRecord(ContactKind.Enter, 1);
            Check(!m.BeginTick(2) && m.Snapshot().EnterCallbacks == 1 && m.LatestTick == 2, "duplicate mutated");
        });
        Test("reject backwards tick without state mutation", () => {
            var m = new ContactPressureMeter(4); m.BeginTick(5); m.TryRecord(ContactKind.Enter, 1);
            Check(!m.BeginTick(4) && m.Snapshot().TotalCallbacks == 1 && m.LatestTick == 5, "backwards mutated");
        });
        Test("reject negative contact points failure atomic", () => {
            var m = new ContactPressureMeter(4); m.BeginTick(0);
            Check(!m.TryRecord(ContactKind.Stay, -1) && m.Snapshot().TotalCallbacks == 0, "invalid mutated");
        });
        Test("reject oversize collision points failure atomic", () => {
            var m = new ContactPressureMeter(4); m.BeginTick(0);
            Check(!m.TryRecord(ContactKind.Stay, 4097) && m.Snapshot().TotalCallbacks == 0, "invalid mutated");
        });
        Test("accept maximum legal contact points", () => {
            var m = new ContactPressureMeter(4); m.BeginTick(0);
            Check(m.TryRecord(ContactKind.Stay, 4096) && m.Snapshot().ContactPoints == 4096, "maximum rejected");
        });
        Test("reject unknown enum kind failure atomic", () => {
            var m = new ContactPressureMeter(4); m.BeginTick(0);
            Check(!m.TryRecord((ContactKind)99, 3) && m.Snapshot().ContactPoints == 0, "unknown accepted");
        });
        Test("reject negative enum kind failure atomic", () => {
            var m = new ContactPressureMeter(4); m.BeginTick(0);
            Check(!m.TryRecord((ContactKind)(-1), 3) && m.Snapshot().TotalCallbacks == 0, "negative accepted");
        });
        Test("small tick gap contributes zero observations", () => {
            var m = new ContactPressureMeter(4); m.BeginTick(0); m.TryRecord(ContactKind.Enter, 2);
            m.BeginTick(3); var s = m.Snapshot();
            Check(s.ObservedTicks == 4 && s.EnterCallbacks == 1 && s.P95CallbacksPerTick == 1, "gap wrong");
        });
        Test("large gap evicts all previous events", () => {
            var m = new ContactPressureMeter(4); m.BeginTick(0); m.TryRecord(ContactKind.Enter, 4);
            m.BeginTick(1000); var s = m.Snapshot();
            Check(s.ObservedTicks == 4 && s.TotalCallbacks == 0 && s.ContactPoints == 0, "old contact retained");
        });
        Test("ring evicts only oldest tick", () => {
            var m = new ContactPressureMeter(4);
            for (int i = 1; i <= 5; ++i) { m.BeginTick(i); for (int j = 0; j < i; ++j) m.TryRecord(ContactKind.Stay, 1); }
            var s = m.Snapshot();
            Check(s.ObservedTicks == 4 && s.TotalCallbacks == 14 && s.ContactPoints == 14 &&
                s.PeakCallbacksPerTick == 5, "ring evicted wrong entry");
        });
        Test("nearest-rank p95 is deterministic", () => {
            var m = new ContactPressureMeter(20);
            for (int i = 1; i <= 20; ++i) { m.BeginTick(i); if (i == 20) for (int n = 0; n < 10; ++n) m.TryRecord(ContactKind.Enter, 0); }
            var s = m.Snapshot();
            Check(s.PeakCallbacksPerTick == 10 && s.P95CallbacksPerTick == 0, "p95 ranks wrong");
        });
        Test("reset clears all old history and tick lease", () => {
            var m = new ContactPressureMeter(4); m.BeginTick(7); m.TryRecord(ContactKind.Stay, 8); m.Reset();
            Check(m.Snapshot().ObservedTicks == 0 && m.LatestTick == -1 && m.BeginTick(0), "reset failed");
        });
        Test("returned snapshot remains immutable after subsequent ticks", () => {
            var m = new ContactPressureMeter(4); m.BeginTick(1); m.TryRecord(ContactKind.Enter, 1);
            var before = m.Snapshot(); m.BeginTick(2); m.TryRecord(ContactKind.Exit, 4);
            Check(before.TotalCallbacks == 1 && before.ExitCallbacks == 0 && m.Snapshot().TotalCallbacks == 2, "mutable snapshot");
        });
        Test("long tick indices maintain correct bounded ring", () => {
            var m = new ContactPressureMeter(4); m.BeginTick(long.MaxValue - 1); m.TryRecord(ContactKind.Stay, 2);
            Check(m.BeginTick(long.MaxValue) && m.Snapshot().TotalCallbacks == 1 &&
                !m.BeginTick(long.MaxValue), "tick overflow/duplicate");
        });
        Test("adapter before first FixedUpdate returns no snapshot", () => {
            var p = Probe(); ContactPressureSnapshot s;
            Check(!p.TryCapture(out s), "invented contact sample");
        });
        Test("adapter original callbacks increment real production meter", () => {
            var p = Probe(); Tick(p); CollisionOf(p, "OnCollisionEnter", 2);
            CollisionOf(p, "OnCollisionStay", 3); CollisionOf(p, "OnCollisionExit", 0);
            ContactPressureSnapshot s; Check(p.TryCapture(out s) && s.TotalCallbacks == 3 &&
                s.ContactPoints == 5 && s.ObservedTicks == 1, "adapter did not capture original callbacks");
        });
        Test("adapter ignores callbacks before FixedUpdate", () => {
            var p = Probe(); CollisionOf(p, "OnCollisionEnter", 2); Tick(p);
            ContactPressureSnapshot s; Check(p.TryCapture(out s) && s.TotalCallbacks == 0, "sample before tick");
        });
        Test("adapter null collision cannot write state", () => {
            var p = Probe(); Tick(p); Invoke(p, "OnCollisionStay", (object)null);
            ContactPressureSnapshot s; Check(p.TryCapture(out s) && s.TotalCallbacks == 0, "null created callback");
        });
        Test("adapter rejects invalid history before accepting events", () => {
            var p = Probe(2); Tick(p); ContactPressureSnapshot s;
            Check(!p.enabled && !p.TryCapture(out s), "invalid history accepted");
        });
        Test("adapter reenable resets rolling samples", () => {
            var p = Probe(); Tick(p); CollisionOf(p, "OnCollisionEnter", 4);
            Invoke(p, "OnEnable"); ContactPressureSnapshot s;
            Check(!p.TryCapture(out s), "old run survived reenable");
            Tick(p); Check(p.TryCapture(out s) && s.TotalCallbacks == 0 && s.LatestTick == 1, "new run wrong");
        });
        Test("independent adapters do not share physics state", () => {
            var a = Probe(); var b = Probe(); Tick(a); Tick(b);
            CollisionOf(a, "OnCollisionEnter", 5);
            ContactPressureSnapshot x, y;
            Check(a.TryCapture(out x) && b.TryCapture(out y) && x.TotalCallbacks == 1 &&
                y.TotalCallbacks == 0 && y.ContactPoints == 0, "cross-owner contamination");
        });
        Test("adapter fixed-tick window excludes oldest collision", () => {
            var p = Probe(4);
            for (int i = 0; i < 5; ++i) { Tick(p); CollisionOf(p, "OnCollisionStay", i + 1); }
            ContactPressureSnapshot s; Check(p.TryCapture(out s) && s.TotalCallbacks == 4 &&
                s.ContactPoints == 14 && s.ObservedTicks == 4, "bad adapter ring");
        });
        Test("snapshot with zero contact points still counts callbacks", () => {
            var m = new ContactPressureMeter(4); m.BeginTick(0); m.TryRecord(ContactKind.Exit, 0);
            Check(m.Snapshot().ExitCallbacks == 1 && m.Snapshot().ContactPoints == 0, "zero contact dropped");
        });

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
        var receipt = new {
            schema = "B01-50-CONTACTPRESSURE-001-R1-EXACT-ORIGINAL-CSharp",
            total = passed + failed, passed, failed, failures,
            productionCoreExecuted = true, productionUnityAdapterExecutedWithExplicitSurrogate = true,
            unityApi = "SURROGATE_NOT_NATIVE_UNITY",
            unityG1 = "NOT_RUN", physx = "NOT_RUN", fpsGpu = "NOT_MEASURED",
            immutableCore = "Core/ContactPressureMeter.cs",
            immutableUnityAdapter = "Unity/B0150OptionalContactPressureProbe.cs"
        };
        File.WriteAllText(output, JsonSerializer.Serialize(receipt, new JsonSerializerOptions { WriteIndented = true }) + "\n");
        Console.WriteLine("ORIGINAL_CONTACTPRESSURE_SOURCE_TESTS " + passed + "/" + (passed+failed) +
                          " PASS, " + failed + " FAIL; NATIVE_UNITY_NOT_RUN");
        return failed == 0 ? 0 : 1;
    }
}
