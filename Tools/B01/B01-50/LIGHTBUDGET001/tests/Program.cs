using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using B01.Performance.LightBudget001;

internal static class Program
{
    private static int passed;
    private static int failed;
    private static readonly List<string> errors = new();

    private static CosmeticLightCandidate C(int id, int priority = 50, float squaredDistance = 100,
        bool eligible = true, bool lit = false) =>
        new(id, priority, squaredDistance, eligible, lit);

    private static bool[] Select(CosmeticLightBudget p, CosmeticLightCandidate[] cases,
        int budget, float hysteresis = 0f)
    {
        var output = new bool[cases.Length];
        if (!p.TrySelect(cases, budget, hysteresis, output, out string error))
            throw new InvalidOperationException(error);
        return output;
    }

    private static void Check(bool ok, string reason = "unexpected result")
    {
        if (!ok) throw new Exception(reason);
    }

    private static void AssertReject(CosmeticLightBudget p, CosmeticLightCandidate[] c, int budget,
        float hysteresis = 0, bool[]? supplied = null)
    {
        var output = supplied ?? Enumerable.Repeat(true, c.Length).ToArray();
        var baseline = output.ToArray();
        Check(!p.TrySelect(c, budget, hysteresis, output, out string err), "expected REJECT");
        Check(err.Length != 0, "missing reason");
        Check(output.SequenceEqual(baseline), "INVALID candidates partially committed");
    }

    private static void Run(string name, Action scenario)
    {
        try
        {
            scenario();
            passed++;
            Console.WriteLine("PASS " + name);
        }
        catch (Exception exc)
        {
            failed++;
            errors.Add(name + ": " + exc.Message);
            Console.WriteLine("FAIL " + name + " " + exc);
        }
    }

    public static int Main(string[] args)
    {
        if (args.Length != 2 || args[0] != "--receipt")
        {
            Console.Error.WriteLine("Usage: --receipt path");
            return 3;
        }

        Run("closest equal-priority light wins", () =>
        {
            var s = Select(new CosmeticLightBudget(4), new[] { C(1, squaredDistance: 25), C(2, squaredDistance: 4) }, 1);
            Check(!s[0] && s[1]);
        });
        Run("higher priority wins over distance", () =>
        {
            var s = Select(new CosmeticLightBudget(3), new[] { C(0, 100, 1000), C(1, 50, 0) }, 1);
            Check(s[0] && !s[1]);
        });
        Run("ties broken deterministically by stable id", () =>
        {
            var s = Select(new CosmeticLightBudget(4), new[] { C(17), C(4), C(9) }, 1);
            Check(!s[0] && s[1] && !s[2]);
        });
        Run("input reordering does not change selected id", () =>
        {
            var budget = new CosmeticLightBudget(4);
            var a = new[] { C(7), C(5), C(6) };
            var b = new[] { C(6), C(7), C(5) };
            Check(a.Where((x, i) => Select(budget, a, 1)[i]).Single().Id ==
                  b.Where((x, i) => Select(budget, b, 1)[i]).Single().Id);
        });
        Run("hysteresis preserves previous nearby light", () =>
        {
            var s = Select(new CosmeticLightBudget(4),
                new[] { C(10, squaredDistance: 100, lit: true), C(11, squaredDistance: 81) }, 1, 2);
            Check(s[0] && !s[1]);
        });
        Run("hysteresis does not outrank much closer light", () =>
        {
            var s = Select(new CosmeticLightBudget(4),
                new[] { C(10, squaredDistance: 400, lit: true), C(11, squaredDistance: 1) }, 1, 2);
            Check(!s[0] && s[1]);
        });
        Run("hysteresis never defeats higher importance", () =>
        {
            var s = Select(new CosmeticLightBudget(4),
                new[] { C(10, 10, 1, lit: true), C(11, 90, 100) }, 1, 50);
            Check(!s[0] && s[1]);
        });
        Run("budget zero turns off all opt-in lights", () =>
        {
            Check(Select(new CosmeticLightBudget(3), new[] { C(0), C(1), C(2) }, 0).All(x => !x));
        });
        Run("exact maximum honored", () =>
        {
            Check(Select(new CosmeticLightBudget(3), new[] { C(0), C(1), C(2) }, 2).Count(x => x) == 2);
        });
        Run("maximum greater than eligible does not invent light", () =>
        {
            var s = Select(new CosmeticLightBudget(3),
                new[] { C(0, eligible: false), C(1), C(2, eligible: false) }, 3);
            Check(!s[0] && s[1] && !s[2]);
        });
        Run("all ineligible retains zero", () =>
        {
            Check(Select(new CosmeticLightBudget(2),
                new[] { C(0, eligible: false), C(1, eligible: false) }, 2).All(x => !x));
        });
        Run("previously off unowned slot can remain excluded", () =>
        {
            var s = Select(new CosmeticLightBudget(2),
                new[] { C(0, eligible: false, lit: false), C(1, eligible: true) }, 1);
            Check(!s[0] && s[1]);
        });
        Run("single candidate", () =>
        {
            Check(Select(new CosmeticLightBudget(1), new[] { C(100) }, 1).Single());
        });
        Run("empty candidate list", () =>
        {
            Check(Select(new CosmeticLightBudget(1), Array.Empty<CosmeticLightCandidate>(), 0).Length == 0);
        });
        Run("distance zero is allowed", () =>
        {
            Check(Select(new CosmeticLightBudget(1), new[] { C(0, squaredDistance: 0) }, 1)[0]);
        });
        Run("maximum finite distance is allowed", () =>
        {
            Check(Select(new CosmeticLightBudget(1), new[] { C(0, squaredDistance: 100000000f) }, 1)[0]);
        });
        Run("maximum 256 candidates and 32 selected", () =>
        {
            var candidates = Enumerable.Range(0, 256).Select(i => C(i, squaredDistance: i)).ToArray();
            var s = Select(new CosmeticLightBudget(256), candidates, 32);
            Check(s.Count(v => v) == 32 && s.Take(32).All(v => v) && s.Skip(32).All(v => !v));
        });
        Run("invalid capacity zero rejected", () =>
        {
            try { _ = new CosmeticLightBudget(0); throw new Exception("accepted"); }
            catch (ArgumentOutOfRangeException) { }
        });
        Run("invalid capacity over 256 rejected", () =>
        {
            try { _ = new CosmeticLightBudget(257); throw new Exception("accepted"); }
            catch (ArgumentOutOfRangeException) { }
        });
        Run("too many candidates rejected without output mutation", () =>
            AssertReject(new CosmeticLightBudget(1), new[] { C(0), C(1) }, 1));
        Run("duplicate id rejected", () =>
            AssertReject(new CosmeticLightBudget(2), new[] { C(0), C(0) }, 1));
        Run("duplicate id among ineligible rejected", () =>
            AssertReject(new CosmeticLightBudget(2), new[] { C(0), C(0, eligible: false) }, 1));
        Run("negative id rejected", () =>
            AssertReject(new CosmeticLightBudget(1), new[] { C(-1) }, 1));
        Run("negative priority rejected", () =>
            AssertReject(new CosmeticLightBudget(1), new[] { C(0, -1) }, 1));
        Run("priority above 100 rejected", () =>
            AssertReject(new CosmeticLightBudget(1), new[] { C(0, 101) }, 1));
        Run("negative distance rejected", () =>
            AssertReject(new CosmeticLightBudget(1), new[] { C(0, squaredDistance: -1) }, 1));
        Run("nan distance rejected", () =>
            AssertReject(new CosmeticLightBudget(1), new[] { C(0, squaredDistance: float.NaN) }, 1));
        Run("positive infinity distance rejected", () =>
            AssertReject(new CosmeticLightBudget(1), new[] { C(0, squaredDistance: float.PositiveInfinity) }, 1));
        Run("distance over cap rejected", () =>
            AssertReject(new CosmeticLightBudget(1), new[] { C(0, squaredDistance: 100001000f) }, 1));
        Run("negative hysteresis rejected", () =>
            AssertReject(new CosmeticLightBudget(1), new[] { C(0) }, 1, -1));
        Run("nan hysteresis rejected", () =>
            AssertReject(new CosmeticLightBudget(1), new[] { C(0) }, 1, float.NaN));
        Run("infinite hysteresis rejected", () =>
            AssertReject(new CosmeticLightBudget(1), new[] { C(0) }, 1, float.PositiveInfinity));
        Run("hysteresis >50 rejected", () =>
            AssertReject(new CosmeticLightBudget(1), new[] { C(0) }, 1, 51));
        Run("negative budget rejected", () =>
            AssertReject(new CosmeticLightBudget(1), new[] { C(0) }, -1));
        Run("budget >count rejected", () =>
            AssertReject(new CosmeticLightBudget(1), new[] { C(0) }, 2));
        Run("short output buffer rejected without mutation", () =>
            AssertReject(new CosmeticLightBudget(2), new[] { C(0), C(1) }, 1, supplied: new[] { true }));
        Run("null output rejected", () =>
        {
            var p = new CosmeticLightBudget(1);
            Check(!p.TrySelect(new[] { C(0) }, 1, 1, null!, out string e) && e.Length > 0);
        });
        Run("null candidate list rejected", () =>
        {
            var p = new CosmeticLightBudget(1);
            Check(!p.TrySelect(null!, 0, 1, new bool[1], out string e) && e.Length > 0);
        });
        Run("recovery after an invalid update", () =>
        {
            var p = new CosmeticLightBudget(2);
            AssertReject(p, new[] { C(1), C(1) }, 1);
            Check(Select(p, new[] { C(1), C(2) }, 1).Count(x => x) == 1);
        });
        Run("repeated valid sample deterministic", () =>
        {
            var p = new CosmeticLightBudget(2);
            var c = new[] { C(8, 50, 25), C(9, 50, 36) };
            Check(Select(p, c, 1).SequenceEqual(Select(p, c, 1)));
        });
        Run("camera movement reorders lights", () =>
        {
            var p = new CosmeticLightBudget(2);
            Check(Select(p, new[] { C(0, 50, 1), C(1, 50, 100) }, 1)[0]);
            Check(Select(p, new[] { C(0, 50, 100), C(1, 50, 1) }, 1)[1]);
        });
        Run("output tail outside active count untouched", () =>
        {
            var p = new CosmeticLightBudget(2);
            var output = new[] { false, true, true, true };
            Check(p.TrySelect(new[] { C(0), C(1) }, 1, 0, output, out _));
            Check(output[2] && output[3]);
        });

        var receipt = new
        {
            package_id = "B01-50-LIGHTBUDGET-001",
            schema = "b0150-source-test-v1",
            total = passed + failed,
            passed,
            failed,
            nativeUnity = "NOT_RUN",
            actualGpuDrawCalls = "NOT_MEASURED",
            actualFps = "NOT_MEASURED",
            errors
        };
        string outputFile = args[1];
        if (File.Exists(outputFile))
        {
            Console.Error.WriteLine("Refusing to overwrite receipt.");
            return 4;
        }
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputFile))!);
        File.WriteAllText(outputFile, JsonSerializer.Serialize(receipt, new JsonSerializerOptions { WriteIndented = true }) + "\n");
        Console.WriteLine($"LIGHTBUDGET_SOURCE_TESTS {passed}/{passed + failed} PASS, failed={failed}");
        return failed == 0 ? 0 : 1;
    }
}
