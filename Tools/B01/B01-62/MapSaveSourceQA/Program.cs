// B01-62 QA-ONLY: compile unmodified Codex #310 MapStorage.cs + MapDraft.cs.
// UnityEngine shim is limited to Vector2Int/Application/JsonUtility; Unity itself is NOT run.
using System;
using System.Collections.Generic;
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

    private static void Case(string id, string category, Func<bool> check)
    {
        try
        {
            bool met = check();
            string status = category switch
            {
                "NEGATIVE" => met ? "FIX_ACCEPTANCE_GREEN" : "KNOWN_DEFECT_RED",
                "POSITIVE" => met ? "ORIGINAL_CSHARP_POSITIVE_PASS" : "ORIGINAL_CSHARP_POSITIVE_FAIL",
                "OBSERVATION" => met ? "SOURCE_WITNESS_OBSERVED" : "SOURCE_WITNESS_NOT_OBSERVED",
                _ => "INVALID_CATEGORY"
            };
            if (status is "ORIGINAL_CSHARP_POSITIVE_FAIL" or "INVALID_CATEGORY") unexpected++;
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

    public static int Main()
    {
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

        Console.WriteLine("RESULT_JSON=" + JsonSerializer.Serialize(new
        {
            source = "UNMODIFIED_CODEX_CSHARP_WITH_UNITY_SHIM",
            nativeUnity = "NOT_RUN",
            windowsProcessCrash = "NOT_RUN",
            unexpected,
            cases = Evidence
        }));
        return unexpected == 0 ? 0 : 1;
    }
}
