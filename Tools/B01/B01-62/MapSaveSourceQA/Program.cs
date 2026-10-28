// B01-62 QA-ONLY: compile unmodified Codex #310 MapStorage.cs + MapDraft.cs.
// UnityEngine shim is limited to Vector2Int/Application/JsonUtility; Unity itself is NOT run.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Text.Json;
using RoboRougelike.MapEditor;

namespace UnityEngine
{
    public readonly struct Vector2Int : IEquatable<Vector2Int>
    {
        public readonly int x, y;
        public Vector2Int(int x, int y) { this.x = x; this.y = y; }
        public bool Equals(Vector2Int other) => x == other.x && y == other.y;
        public override bool Equals(object? obj) => obj is Vector2Int other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(x, y);
    }
    public static class Application
    {
        public static string persistentDataPath => Path.GetTempPath();
    }
    public static class JsonUtility
    {
        // Isolated .NET compatibility shim. NOT Unity's JsonUtility implementation.
        static readonly JsonSerializerOptions Options = new() { IncludeFields = true };
        public static string ToJson(object value, bool pretty = false) =>
            JsonSerializer.Serialize(value, new JsonSerializerOptions(Options) { WriteIndented = pretty });
        public static T? FromJson<T>(string json) => JsonSerializer.Deserialize<T>(json, Options);
    }
}

public static class Program
{
    private static readonly List<object> Evidence = new();
    private static int unexpected;
    private static int d02Checks;
    private static int d02RemainingRed;

    private static void Case(string id, string category, Func<bool> check)
    {
        try
        {
            bool met = check();
            if (category == "NEGATIVE" &&
                (id.StartsWith("A01-") || id.StartsWith("A02-") ||
                 id.StartsWith("A03-") || id.StartsWith("A04-")))
            {
                d02Checks++;
                if (!met) d02RemainingRed++;
            }
            string status = category switch
            {
                "NEGATIVE" => met ? "FIX_ACCEPTANCE_GREEN" : "KNOWN_DEFECT_RED",
                "POSITIVE" => met ? "ORIGINAL_CSHARP_POSITIVE_PASS" : "ORIGINAL_CSHARP_POSITIVE_FAIL",
                "OBSERVATION" => met ? "SOURCE_WITNESS_OBSERVED" : "SOURCE_WITNESS_NOT_OBSERVED",
                _ => "INVALID_CATEGORY"
            };
            if (status is "ORIGINAL_CSHARP_POSITIVE_FAIL" or "SOURCE_WITNESS_NOT_OBSERVED" or "INVALID_CATEGORY") unexpected++;
            Evidence.Add(new { id, category, status });
            Console.WriteLine($"{id}: {status}");
        }
        catch (Exception e)
        {
            unexpected++;
            Evidence.Add(new { id, category, status = "HARNESS_EXCEPTION", detail = e.ToString() });
            Console.WriteLine($"{id}: HARNESS_EXCEPTION {e}");
        }
    }

    private static string TemporaryDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "b01-62-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static bool InTemporaryDirectory(Func<string, bool> test)
    {
        var directory = TemporaryDirectory();
        try { return test(directory); }
        finally { Directory.Delete(directory, recursive: true); }
    }

    private static MapDraft One(MapItemKind kind, int x = 0, int z = 0)
    {
        var draft = new MapDraft();
        if (!draft.Place(kind, x, z, 0)) throw new InvalidOperationException("Fixture rejected");
        return draft;
    }

    // A child process exits intentionally AFTER fully staging data but BEFORE promotion.
    // This is NOT a kill injected inside the unmodified MapStorage.Save implementation.
    private static int StageAndExit(string path)
    {
        string pending = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        byte[] bytes = Encoding.UTF8.GetBytes(One(MapItemKind.Wall, 3, 0).ToJson());
        using (var stream = new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush(flushToDisk: true);
        }
        Console.WriteLine("STAGED_BEFORE_PROMOTION " + Path.GetFileName(pending));
        Console.Out.Flush();
        Environment.Exit(73); // Explicit process termination. No MapStorage.Save or OS power failure.
        return 73;
    }

    private static int LoadInFreshProcess(string path)
    {
        var draft = new MapDraft();
        bool ok = MapStorage.Load(path, draft, out var error);
        Console.WriteLine("FRESH_PROCESS_LOAD=" + ok + " KIND=" +
                          (ok && draft.Items.Count == 1 ? draft.Items[0].kind.ToString() : "NONE") +
                          " ERR=" + error);
        return ok && draft.Items.Count == 1 && draft.Items[0].kind == MapItemKind.Ground ? 0 : 81;
    }

    private static (int exit, string output) InvokeOwnChild(string mode, string path)
    {
        string binary = Environment.ProcessPath ??
                        throw new InvalidOperationException("No .NET QA process path");
        var p = new ProcessStartInfo(binary)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        p.ArgumentList.Add(mode);
        p.ArgumentList.Add(path);
        using var child = Process.Start(p) ?? throw new InvalidOperationException("Unable to launch own .NET child");
        string output = child.StandardOutput.ReadToEnd();
        string errors = child.StandardError.ReadToEnd();
        if (!child.WaitForExit(15000))
        {
            child.Kill(entireProcessTree: true);
            throw new TimeoutException("C# isolated sub-process exceeded 15 seconds");
        }
        if (errors.Length > 0 && child.ExitCode == 0)
            Console.WriteLine("CHILD_STDERR_NONFATAL=" + errors.Trim());
        return (child.ExitCode, output);
    }

    public static int Main(string[] args)
    {
        if (args.Length == 1 && args[0] == "--selftest-failclosed")
        {
            Case("SELFTEST-intentionally-false-observation", "OBSERVATION", () => false);
            bool closed = unexpected == 1;
            Console.WriteLine("HARNESS_SELFTEST_FAIL_CLOSED=" + closed);
            return closed ? 73 : 0;
        }
        bool strictD02 = args.Length == 1 && args[0] == "--require-d02-green";
        if (args.Length == 2 && args[0] == "--stage-and-exit")
            return StageAndExit(args[1]);
        if (args.Length == 2 && args[0] == "--load-in-new-process")
            return LoadInFreshProcess(args[1]);
        Console.WriteLine("B01-62 original C# MapStorage+MapDraft QA | Codex PR310 pinned SHA");
        Console.WriteLine("NOTICE: original C# source methods + actual filesystem; Unity types/JSON shimmed.");
        Console.WriteLine("NOTICE: KNOWN_DEFECT_RED is observed failure, NOT a successful game fix.");

        Case("A01-reject-whitespace-alias", "NEGATIVE", () =>
            !MapStorage.TryPath(Path.GetTempPath(), " Tor 1 ", out _));

        Case("A02-reject-Windows-reserved-device-stems", "NEGATIVE", () =>
        {
            string[] invalid = { "CON", "con", "PRN", "AUX", "NUL",
                                 "COM1", "COM9", "LPT1", "LPT9" };
            return invalid.All(name => !MapStorage.TryPath(Path.GetTempPath(), name, out _));
        });

        Case("A03-reject-whitespace-physical-overwrite", "NEGATIVE", () =>
            InTemporaryDirectory(directory =>
            {
                if (!MapStorage.TryPath(directory, "Tor 1", out var original))
                    throw new Exception("Positive name rejected");
                MapStorage.Save(original!, One(MapItemKind.Ground, 0, 0));
                string committed = File.ReadAllText(original!);
                if (!MapStorage.TryPath(directory, " Tor 1 ", out var alias)) return true;
                MapStorage.Save(alias!, One(MapItemKind.Wall, 1, 0));
                return File.ReadAllText(original!) == committed;
            }));

        if (OperatingSystem.IsWindows())
        {
            Case("A04-Windows-casefold-overwrite", "NEGATIVE", () =>
                InTemporaryDirectory(directory =>
                {
                    if (!MapStorage.TryPath(directory, "Tor Case", out var upper) ||
                        !MapStorage.TryPath(directory, "tor case", out var lower))
                        throw new Exception("Positive names rejected");
                    MapStorage.Save(upper!, One(MapItemKind.Ground, 0, 0));
                    string committed = File.ReadAllText(upper!);
                    MapStorage.Save(lower!, One(MapItemKind.Wall, 1, 0));
                    return File.ReadAllText(upper!) == committed;
                }));
        }
        else
        {
            Evidence.Add(new { id = "A04-Windows-casefold-overwrite", category = "NEGATIVE",
                               status = "SKIPPED_NOT_WINDOWS" });
            Console.WriteLine("A04-Windows-casefold-overwrite: SKIPPED_NOT_WINDOWS");
        }

        Case("A05-valid-canonical-name", "POSITIVE", () =>
            MapStorage.TryPath(Path.GetTempPath(), "Tor 1", out var p) &&
            Path.GetFileName(p) == "Tor 1.json" &&
            !MapStorage.TryPath(Path.GetTempPath(), "../escape", out _));

        // A real post-Codex-fix acceptance mode. Unlike baseline evidence collection,
        // this mode returns NONZERO when D02 is still vulnerable or a test is absent.
        // It runs only existing A01–A05, not repeated unrelated B cases.
        if (strictD02)
        {
            int requiredChecks = OperatingSystem.IsWindows() ? 4 : 3;
            bool accepted = d02Checks == requiredChecks && d02RemainingRed == 0 && unexpected == 0;
            Console.WriteLine("D02_STRICT_GATE=" + (accepted ? "GREEN" : "RED") +
                              " checked=" + d02Checks + "/" + requiredChecks +
                              " remainingProductDefects=" + d02RemainingRed +
                              " harnessErrors=" + unexpected);
            return accepted ? 0 : 86;
        }

        Case("B01-real-file-v1-load-v2-save-reload", "POSITIVE", () =>
            InTemporaryDirectory(directory =>
            {
                var file = Path.Combine(directory, "legacy.json");
                const string legacy = "{\"version\":1,\"globalFloor\":true,\"items\":[" +
                    "{\"kind\":0,\"x\":0,\"z\":0,\"rotation\":0}," +
                    "{\"kind\":4,\"x\":0,\"z\":0,\"rotation\":0}," +
                    "{\"kind\":5,\"x\":2,\"z\":0,\"rotation\":0}]}";
                File.WriteAllText(file, legacy);
                var loaded = new MapDraft();
                if (!MapStorage.Load(file, loaded, out _)) return false;
                if (loaded.GlobalFloor || loaded.Items.Count != 3 ||
                    loaded.Items.Any(p => p.scaleX != 1 || p.scaleY != 1 || p.scaleZ != 1))
                    return false;
                MapStorage.Save(file, loaded);
                using var document = JsonDocument.Parse(File.ReadAllText(file));
                if (document.RootElement.GetProperty("version").GetInt32() != 2) return false;
                var roundtrip = new MapDraft();
                return MapStorage.Load(file, roundtrip, out _) &&
                       roundtrip.Items.Count == 3 &&
                       !roundtrip.GlobalFloor &&
                       roundtrip.ToJson() == loaded.ToJson();
            }));

        Case("B02-corrupt-scaled-v2-load-is-memory-atomic", "POSITIVE", () =>
            InTemporaryDirectory(directory =>
            {
                var path = Path.Combine(directory, "draft.json");
                var existing = One(MapItemKind.Ground);
                MapStorage.Save(path, existing);
                var loaded = new MapDraft();
                if (!MapStorage.Load(path, loaded, out _)) return false;
                string before = loaded.ToJson();
                File.WriteAllText(path,
                    "{\"version\":2,\"items\":[{\"kind\":1,\"x\":0,\"z\":0," +
                    "\"rotation\":0,\"scaleX\":0,\"scaleY\":1,\"scaleZ\":1}]}");
                bool rejected = !MapStorage.Load(path, loaded, out _);
                return rejected && before == loaded.ToJson();
            }));

        Case("B03-oversized-file-fails-closed", "POSITIVE", () =>
            InTemporaryDirectory(directory =>
            {
                var file = Path.Combine(directory, "oversize.json");
                var draft = One(MapItemKind.Finish);
                string before = draft.ToJson();
                File.WriteAllText(file, new string(' ', 1024 * 1024 + 1));
                return !MapStorage.Load(file, draft, out _) && before == draft.ToJson();
            }));

        Case("B04-existing-map-atomic-replacement-positive", "POSITIVE", () =>
            InTemporaryDirectory(directory =>
            {
                var file = Path.Combine(directory, "good.json");
                MapStorage.Save(file, One(MapItemKind.Ground));
                MapStorage.Save(file, One(MapItemKind.Wall, 4, 0));
                var loaded = new MapDraft();
                return MapStorage.Load(file, loaded, out _) &&
                       loaded.Items.Count == 1 && loaded.Items[0].kind == MapItemKind.Wall &&
                       Directory.GetFiles(directory, "*.tmp").Length == 0;
            }));

        // Crash boundary witness only: no actual process killed. HQ recovery policy unresolved.
        Case("B05-staged-orphan-is-undiscoverable", "OBSERVATION", () =>
            InTemporaryDirectory(directory =>
            {
                var path = Path.Combine(directory, "never-committed.json");
                var pending = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
                File.WriteAllText(pending, One(MapItemKind.Wall).ToJson());
                var target = new MapDraft();
                return !MapStorage.Load(path, target, out _) && File.Exists(pending) &&
                       !File.Exists(path);
            }));

        Case("B06-new-map-after-intentional-CSharp-process-termination", "OBSERVATION", () =>
            InTemporaryDirectory(directory =>
            {
                var path = Path.Combine(directory, "uncommitted.json");
                var child = InvokeOwnChild("--stage-and-exit", path);
                var draft = new MapDraft();
                bool loaded = MapStorage.Load(path, draft, out var error);
                string[] temps = Directory.GetFiles(directory, "uncommitted.json.*.tmp");
                return child.exit == 73 && child.output.Contains("STAGED_BEFORE_PROMOTION") &&
                       !loaded && !File.Exists(path) && temps.Length == 1 &&
                       File.ReadAllText(temps[0]).Contains("\"version\": 2") &&
                       draft.Items.Count == 0;
            }));

        Case("B07-existing-map-after-intentional-CSharp-process-termination", "OBSERVATION", () =>
            InTemporaryDirectory(directory =>
            {
                var path = Path.Combine(directory, "committed.json");
                MapStorage.Save(path, One(MapItemKind.Ground));
                byte[] before = File.ReadAllBytes(path);
                var child = InvokeOwnChild("--stage-and-exit", path);
                var loaded = new MapDraft();
                bool loadedOld = MapStorage.Load(path, loaded, out var error);
                string[] temps = Directory.GetFiles(directory, "committed.json.*.tmp");
                return child.exit == 73 && child.output.Contains("STAGED_BEFORE_PROMOTION") &&
                       loadedOld && before.SequenceEqual(File.ReadAllBytes(path)) &&
                       loaded.Items.Count == 1 && loaded.Items[0].kind == MapItemKind.Ground &&
                       temps.Length == 1 && File.ReadAllText(temps[0]).Contains("\"version\": 2");
            }));

        Case("B08-original-MapStorage-reload-across-two-processes", "POSITIVE", () =>
            InTemporaryDirectory(directory =>
            {
                var path = Path.Combine(directory, "durable.json");
                MapStorage.Save(path, One(MapItemKind.Ground));
                var child = InvokeOwnChild("--load-in-new-process", path);
                return child.exit == 0 && child.output.Contains("FRESH_PROCESS_LOAD=True KIND=Ground");
            }));

        // Real Windows OS file-sharing negative: force File.Replace to fail while
        // a committed canonical map has an exclusive file handle.
        // This is a genuine I/O exception path in unmodified MapStorage.Save.
        if (OperatingSystem.IsWindows())
        {
            Case("B09-locked-Windows-canonical-save-fails-without-data-loss", "POSITIVE", () =>
                InTemporaryDirectory(directory =>
                {
                    var file = Path.Combine(directory, "existing.json");
                    MapStorage.Save(file, One(MapItemKind.Ground));
                    var prior = File.ReadAllBytes(file);
                    bool rejected = false;
                    using (var locked = new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                    {
                        try { MapStorage.Save(file, One(MapItemKind.Wall, 6, 0)); }
                        catch (IOException) { rejected = true; }
                        if (!rejected) return false;
                        var lockedBytes = new byte[checked((int)locked.Length)];
                        locked.Position = 0;
                        locked.ReadExactly(lockedBytes);
                        if (!prior.SequenceEqual(lockedBytes)) return false;
                    }
                    var reload = new MapDraft();
                    return prior.SequenceEqual(File.ReadAllBytes(file)) &&
                           Directory.GetFiles(directory, "*.tmp").Length == 0 &&
                           MapStorage.Load(file, reload, out _) &&
                           reload.Items.Count == 1 && reload.Items[0].kind == MapItemKind.Ground;
                }));
        }
        else
        {
            Evidence.Add(new { id = "B09-locked-Windows-canonical-save-fails-without-data-loss",
                               category = "POSITIVE", status = "SKIPPED_NOT_WINDOWS" });
            Console.WriteLine("B09-locked-Windows-canonical-save-fails-without-data-loss: SKIPPED_NOT_WINDOWS");
        }

        // Windows filesystem negative distinct from B09's FileShare.None lock:
        // verify a ReadOnly-flagged committed map is never silently replaced.
        // The original MapStorage.Save code executes without modification.
        if (OperatingSystem.IsWindows())
        {
            Case("B10-readonly-Windows-canonical-save-rejects-data-loss", "POSITIVE", () =>
                InTemporaryDirectory(directory =>
                {
                    var file = Path.Combine(directory, "readonly-map.json");
                    MapStorage.Save(file, One(MapItemKind.Ground));
                    byte[] prior = File.ReadAllBytes(file);
                    File.SetAttributes(file, File.GetAttributes(file) | FileAttributes.ReadOnly);
                    bool rejected = false;
                    string outcome = "RETURNED_WITHOUT_EXCEPTION";
                    try
                    {
                        try { MapStorage.Save(file, One(MapItemKind.Wall, 7, 0)); }
                        catch (IOException ex) { rejected = true; outcome = ex.GetType().Name; }
                        catch (UnauthorizedAccessException ex) { rejected = true; outcome = ex.GetType().Name; }
                        Console.WriteLine("B10_ORIGINAL_SAVE_OUTCOME=" + outcome);
                        bool preserved = prior.SequenceEqual(File.ReadAllBytes(file));
                        bool noTemp = Directory.GetFiles(directory, "*.tmp").Length == 0;
                        var reloaded = new MapDraft();
                        bool oldReloads = MapStorage.Load(file, reloaded, out _) &&
                                          reloaded.Items.Count == 1 &&
                                          reloaded.Items[0].kind == MapItemKind.Ground;
                        return rejected && preserved && noTemp && oldReloads;
                    }
                    finally
                    {
                        if (File.Exists(file))
                            File.SetAttributes(file, File.GetAttributes(file) & ~FileAttributes.ReadOnly);
                    }
                }));
        }
        else
        {
            Evidence.Add(new { id = "B10-readonly-Windows-canonical-save-rejects-data-loss",
                               category = "POSITIVE", status = "SKIPPED_NOT_WINDOWS" });
            Console.WriteLine("B10-readonly-Windows-canonical-save-rejects-data-loss: SKIPPED_NOT_WINDOWS");
        }

        // B11: genuine concurrent OS file I/O using the original MapStorage.Save/Load.
        // This is separate from Windows ReadOnly and exclusive FileShare locks.
        // Each call sees an entire old/new MapDraft or fails without changing its
        // previously loaded in-memory state; no guarantee about which revision wins.
        Case("B11-concurrent-original-save-load-no-torn-map", "POSITIVE", () =>
            InTemporaryDirectory(directory =>
            {
                string file = Path.Combine(directory, "concurrent.json");
                MapStorage.Save(file, One(MapItemKind.Ground));
                using var go = new ManualResetEventSlim(false);
                var writer = Task.Run(() =>
                {
                    go.Wait();
                    int committedWrites = 0, ioConflicts = 0;
                    for (int k = 0; k < 96; k++)
                    {
                        try
                        {
                            MapStorage.Save(file, One(k % 2 == 0 ? MapItemKind.Wall : MapItemKind.Ground, k % 2, 0));
                            committedWrites++;
                        }
                        catch (IOException)
                        {
                            // Real Windows File.Replace can conflict with an open
                            // concurrent reader. A failed write is NOT a committed
                            // map revision; verify that old/new bytes stay valid.
                            ioConflicts++;
                        }
                        if (k % 8 == 0) Thread.Yield();
                    }
                    return (committedWrites, ioConflicts);
                });
                var reader = Task.Run(() =>
                {
                    go.Wait();
                    var draft = new MapDraft();
                    int successes = 0, transientFailures = 0;
                    for (int k = 0; k < 1024; k++)
                    {
                        string before = draft.ToJson();
                        bool loaded = MapStorage.Load(file, draft, out string error);
                        if (loaded)
                        {
                            successes++;
                            if (draft.Items.Count != 1 ||
                                (draft.Items[0].kind != MapItemKind.Ground &&
                                 draft.Items[0].kind != MapItemKind.Wall))
                                return (false, successes, transientFailures);
                        }
                        else
                        {
                            transientFailures++;
                            // Even under transient filesystem contention, Load must
                            // not replace the existing in-memory draft with partial JSON.
                            if (draft.ToJson() != before)
                                return (false, successes, transientFailures);
                        }
                        if (k % 16 == 0) Thread.Yield();
                    }
                    return (successes > 0, successes, transientFailures);
                });
                go.Set();
                if (!Task.WaitAll(new Task[] { writer, reader }, 20000))
                    throw new TimeoutException("B11 concurrent original source IO exceeded 20 seconds");
                var observed = reader.Result;
                var written = writer.Result;
                var final = new MapDraft();
                bool finalOk = MapStorage.Load(file, final, out _) &&
                    final.Items.Count == 1 &&
                    (final.Items[0].kind == MapItemKind.Ground ||
                     final.Items[0].kind == MapItemKind.Wall);
                bool noOrphans = Directory.GetFiles(directory, "*.tmp").Length == 0;
                Console.WriteLine($"B11_REAL_IO_RACE successfulLoads={observed.successes} " +
                                  $"transientLoadFailures={observed.transientFailures} " +
                                  $"committedWrites={written.committedWrites} " +
                                  $"originalFileReplaceIOConflicts={written.ioConflicts} " +
                                  $"finalValid={finalOk} orphans={(!noOrphans)}");
                return observed.Item1 && written.committedWrites > 0 &&
                       written.committedWrites + written.ioConflicts == 96 &&
                       finalOk && noOrphans;
            }));

        Console.WriteLine("RESULT_JSON=" + JsonSerializer.Serialize(new
        {
            source = "UNMODIFIED_CODEX_CSHARP_WITH_UNITY_SHIM",
            nativeUnity = "NOT_RUN",
            windowsProcessCrash = "STAGING_CHILD_EXIT_73_NOT_INJECTED_MAPSTORAGE_SAVE",
            unexpected,
            cases = Evidence
        }));
        return unexpected == 0 ? 0 : 1;
    }
}
