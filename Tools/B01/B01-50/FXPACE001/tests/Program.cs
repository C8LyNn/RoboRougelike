using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using B01.Performance.FxPacing;

internal static class Program
{
    private static int total;
    private static int passed;
    private static int failed;
    private static readonly List<string> failures = new List<string>();
    private static void Is(bool value, string reason)
    {
        if (!value) throw new InvalidOperationException(reason);
    }
    private static void Test(string name, Action run)
    {
        total++;
        try { run(); passed++; Console.WriteLine("PASS " + name); }
        catch (Exception e) { failed++; failures.Add(name + ": " + e.Message); Console.Error.WriteLine("FAIL " + name + ": " + e); }
    }
    private static void Throws<TException>(Action execute) where TException : Exception
    {
        try { execute(); }
        catch (TException) { return; }
        throw new Exception("Expected " + typeof(TException).Name);
    }

    public static int Main(string[] args)
    {
        Test("reject-zero-per-frame", () => Throws<ArgumentOutOfRangeException>(() => new CosmeticActivationBudget<string>(0, 10, 100)));
        Test("reject-too-large-per-frame", () => Throws<ArgumentOutOfRangeException>(() => new CosmeticActivationBudget<string>(129, 10, 100)));
        Test("reject-zero-queue-capacity", () => Throws<ArgumentOutOfRangeException>(() => new CosmeticActivationBudget<string>(4, 0, 100)));
        Test("reject-invalid-ttl-nan-infinity-negative", () =>
        {
            foreach (var value in new[] { double.NaN, double.PositiveInfinity, -1d, 30001d })
                Throws<ArgumentOutOfRangeException>(() => new CosmeticActivationBudget<string>(4, 10, value));
        });
        Test("reject-null-effect", () =>
        {
            var q = new CosmeticActivationBudget<string>(1, 5, 100);
            Throws<ArgumentNullException>(() => q.TrySchedule(null!, 0));
            Is(q.Pending == 0, "queued despite null");
        });
        Test("reject-unknown-priority", () =>
        {
            var q = new CosmeticActivationBudget<string>(1, 5, 100);
            Throws<ArgumentOutOfRangeException>(() => q.TrySchedule("a", 0, (CosmeticPriority)99));
        });
        Test("reject-nonfinite-or-negative-time", () =>
        {
            var q = new CosmeticActivationBudget<string>(1, 5, 100);
            foreach (var value in new[] { double.NaN, double.PositiveInfinity, -0.1d })
                Throws<ArgumentOutOfRangeException>(() => q.TrySchedule("a", value));
            Is(q.Pending == 0, "mutated after invalid timestamp");
        });
        Test("reject-clock-regression-without-reordering", () =>
        {
            var q = new CosmeticActivationBudget<string>(1, 5, 100);
            q.TrySchedule("first", 10);
            Throws<ArgumentOutOfRangeException>(() => q.TrySchedule("bad", 9));
            Is(q.Pending == 1, "bad clock changed queue");
        });
        Test("regular-overflow-is-bounded-and-counted", () =>
        {
            var q = new CosmeticActivationBudget<string>(1, 2, 100);
            Is(q.TrySchedule("a", 0), "first"); Is(q.TrySchedule("b", 0), "second");
            Is(!q.TrySchedule("c", 0), "third must be dropped");
            Is(q.Pending == 2 && q.DroppedOverflow == 1, "bad overflow count");
        });
        Test("important-preempts-oldest-regular", () =>
        {
            var q = new CosmeticActivationBudget<string>(3, 2, 100);
            q.TrySchedule("old", 0); q.TrySchedule("new", 0);
            Is(q.TrySchedule("high", 0, CosmeticPriority.Important), "high rejected");
            var seen = new List<string>(); q.DrainFrame(0, seen.Add);
            Is(seen.Count == 2 && seen[0] == "high" && seen[1] == "new", "preemption order");
            Is(q.DroppedOverflow == 1, "preemption not counted");
        });
        Test("important-cannot-replace-all-important", () =>
        {
            var q = new CosmeticActivationBudget<string>(2, 1, 100);
            q.TrySchedule("one", 0, CosmeticPriority.Important);
            Is(!q.TrySchedule("two", 0, CosmeticPriority.Important), "important should not drop existing important");
        });
        Test("fairness-two-important-then-one-regular", () =>
        {
            var q = new CosmeticActivationBudget<string>(9, 18, 100);
            for (int i = 0; i < 6; ++i) { q.TrySchedule("H" + i, 0, CosmeticPriority.Important); q.TrySchedule("R" + i, 0); }
            var seen = new List<string>(); q.DrainFrame(0, seen.Add);
            Is(string.Join(",", seen) == "H0,H1,R0,H2,H3,R1,H4,H5,R2", "fairness " + string.Join(",", seen));
        });
        Test("frame-budget-eight-not-nine", () =>
        {
            var q = new CosmeticActivationBudget<string>(8, 100, 1000);
            for (int i = 0; i < 80; ++i) q.TrySchedule("fx" + i, 0);
            int called = 0;
            Is(q.DrainFrame(0, _ => called++) == 8 && called == 8 && q.Pending == 72, "burst activation not bounded");
        });
        Test("expired-effects-never-activate", () =>
        {
            var q = new CosmeticActivationBudget<string>(4, 20, 10);
            q.TrySchedule("stale", 0);
            q.TrySchedule("fresh", 9);
            var seen = new List<string>();
            q.DrainFrame(11, seen.Add);
            Is(seen.Count == 1 && seen[0] == "fresh" && q.DroppedExpired == 1, "expired cosmetic was played");
        });
        Test("ttl-boundary-inclusive", () =>
        {
            var q = new CosmeticActivationBudget<string>(1, 5, 10);
            q.TrySchedule("one", 0);
            Is(q.DrainFrame(10, _ => {}) == 1, "ttl exact boundary should still play");
        });
        Test("cancel-queued-on-scene-reset", () =>
        {
            var q = new CosmeticActivationBudget<string>(2, 10, 100);
            for (int i = 0; i < 7; i++) q.TrySchedule("e" + i, 0);
            Is(q.CancelPending() == 7 && q.Cancelled == 7 && q.Pending == 0, "cancel incorrect");
            Is(q.DrainFrame(1, _ => {}) == 0, "cancelled items executed");
        });
        Test("callback-exception-is-not-retried", () =>
        {
            var q = new CosmeticActivationBudget<string>(2, 10, 100);
            q.TrySchedule("fail", 0); q.TrySchedule("next", 0);
            Throws<InvalidOperationException>(() => q.DrainFrame(0, _ => throw new InvalidOperationException("visual creation fail")));
            var seen = new List<string>(); q.DrainFrame(0, seen.Add);
            Is(seen.Count == 1 && seen[0] == "next" && q.DroppedActivationFailure == 1 && q.Activated == 1, "failure retry/metrics");
        });
        Test("no-work-frame-does-not-change-activation-total", () =>
        {
            var q = new CosmeticActivationBudget<string>(1, 1, 0);
            Is(q.DrainFrame(0, _ => throw new Exception("must never invoke")) == 0 && q.Activated == 0, "empty frame");
        });
        Test("duplicate-cosmetic-request-is-allowed-intentionally", () =>
        {
            var q = new CosmeticActivationBudget<string>(2, 2, 100);
            q.TrySchedule("same", 0); q.TrySchedule("same", 0);
            Is(q.DrainFrame(0, _ => {}) == 2, "multiple spark visual requests lost");
        });
        Test("new-schedule-expires-old-backlog-before-capacity-check", () =>
        {
            var q = new CosmeticActivationBudget<string>(1, 1, 10);
            q.TrySchedule("stale", 0);
            Is(q.TrySchedule("new", 11), "stale should not cause overflow");
            Is(q.DroppedExpired == 1 && q.DroppedOverflow == 0, "wrong expired/overflow counts");
        });
        Test("stress-300-burst-8-per-frame-no-spikes", () =>
        {
            var q = new CosmeticActivationBudget<string>(8, 400, 2000);
            var seen = new List<string>();
            for (int i = 0; i < 300; ++i) Is(q.TrySchedule(i.ToString(), 0), "unexpected reject");
            int frames = 0; int maxObserved = 0;
            while (q.Pending > 0 && frames < 100)
            {
                int count = q.DrainFrame(frames * 16, seen.Add);
                if (count > maxObserved) maxObserved = count;
                Is(count <= 8, "frame spike");
                frames++;
            }
            Is(seen.Count == 300 && frames == 38 && maxObserved == 8 && q.Activated == 300, "burst incomplete");
        });
        Test("sink-null-rejected-without-advancing-clock", () =>
        {
            var q = new CosmeticActivationBudget<string>(1, 2, 100);
            q.TrySchedule("fx", 2);
            Throws<ArgumentNullException>(() => q.DrainFrame(5, null!));
            Is(q.DrainFrame(3, _=>{}) == 1, "null sink advanced clock");
        });
        Test("zero-age-drops-past-only", () =>
        {
            var q = new CosmeticActivationBudget<string>(2, 2, 0);
            q.TrySchedule("instant", 2);
            Is(q.DrainFrame(2, _ => {}) == 1, "same-time effect expires");
            q.TrySchedule("old", 3);
            Is(q.DrainFrame(4, _ => {}) == 0 && q.DroppedExpired == 1, "old effect lingered");
        });

        var result = new Dictionary<string,object> {
            ["package"] = "B01-50-FXPACE-001/R1",
            ["classification"] = "ACTUAL_DOTNET_8_CSHARP_COSMETIC_SCHEDULER_TESTS_NOT_NATIVE_UNITY",
            ["passed"] = passed, ["failed"] = failed, ["total"] = total,
            ["failures"] = failures,
            ["nativeUnity"] = "NOT_RUN", ["measuredFPS"] = "NOT_MEASURED",
            ["stressBurst"] = "300 cosmetic activations delivered across exactly 38 frames at max 8/frame under synthetic 16ms frame timestamps"
        };
        if (args.Length == 2 && args[0] == "--receipt")
        {
            var dest = args[1];
            if (File.Exists(dest)) { Console.Error.WriteLine("Receipt exists; refusing overwrite"); return 3; }
            File.WriteAllText(dest, JsonSerializer.Serialize(result,new JsonSerializerOptions { WriteIndented = true }) + "\n");
        }
        Console.WriteLine($"TOTAL {passed}/{total} PASS; {failed} FAIL");
        return failed == 0 ? 0 : 1;
    }
}
