// B01-22 W10: user-selected, read-only physical stock preview CLI.
// This is a test/demo host of the SAME original Unity-compatible planner.
// No game run, Unity APIs, wallet, purchase, inventory writes or mount actions.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using B01KitReadiness001;

internal static class ReadinessCli
{
    private const string ApprovedCatalogPath =
        "Research/B01-22/P17/EQUIPMENT-KITS-001/catalog.json";
    private const int MaxInputBytes = 65536;
    private static readonly JsonSerializerOptions OutputOptions =
        new JsonSerializerOptions { WriteIndented = true, IncludeFields = true };

    private static void RequireKeys(JsonElement element, params string[] allowed)
    {
        if (element.ValueKind != JsonValueKind.Object)
            throw new ArgumentException("INPUT_NOT_OBJECT");
        var names = new HashSet<string>(StringComparer.Ordinal);
        var known = new HashSet<string>(allowed, StringComparer.Ordinal);
        foreach (var entry in element.EnumerateObject())
            if (!known.Contains(entry.Name) || !names.Add(entry.Name))
                throw new ArgumentException("DUPLICATE_OR_FOREIGN_JSON_FIELD");
        if (names.Count != known.Count)
            throw new ArgumentException("REQUIRED_JSON_FIELD_MISSING");
    }

    private static string Str(JsonElement item) => item.GetString() ??
        throw new ArgumentException("NULL_STRING");
    private static int Num(JsonElement item) => item.GetInt32();

    private static KitRecipe[] LoadApprovedRecipes(string catalogText)
    {
        using (var doc = JsonDocument.Parse(catalogText))
        {
            var source = doc.RootElement;
            if (Str(source.GetProperty("version")) != "1.0.3" ||
                Str(source.GetProperty("provenance").GetProperty("codex_sha")) !=
                    "203a0805251ed28293888e43f2d49f0df63767c6")
                throw new ArgumentException("UNAPPROVED_CATALOG");
            var physical = source.GetProperty("part_prices");
            var weapons = source.GetProperty("weapon_prices");
            var entries = source.GetProperty("kits");
            if (entries.GetArrayLength() != 10)
                throw new ArgumentException("INVALID_CATALOG_SIZE");
            return entries.EnumerateArray().Select(kit => new KitRecipe
            {
                Id = Str(kit.GetProperty("id")),
                Parts = kit.GetProperty("parts").EnumerateArray().Select(part =>
                {
                    string kind = Str(part.GetProperty("kind"));
                    string weapon = part.TryGetProperty("weapon", out var value)
                        ? Str(value) : "";
                    int scrap = kind == "Gun"
                        ? Num(weapons.GetProperty(weapon)) : Num(physical.GetProperty(kind));
                    return new PartNeed
                    {
                        Kind = kind, Weapon = weapon,
                        Quantity = Num(part.GetProperty("count")), UnitScrap = scrap
                    };
                }).ToArray()
            }).ToArray();
        }
    }

    public static string Evaluate(string requestedJson, string approvedCatalogJson)
    {
        using (var doc = JsonDocument.Parse(requestedJson))
        {
            var input = doc.RootElement;
            RequireKeys(input, "schema", "source_sha", "reference_only",
                "requests", "owned_parts");
            if (Str(input.GetProperty("schema")) != "b01-kitreadiness-request-v1" ||
                Str(input.GetProperty("source_sha")) != KitReadinessPlanner.SourceSha ||
                input.GetProperty("reference_only").ValueKind != JsonValueKind.True)
                throw new ArgumentException("INVALID_REQUEST_PROVENANCE");
            var requests = input.GetProperty("requests");
            var parts = input.GetProperty("owned_parts");
            if (requests.ValueKind != JsonValueKind.Array ||
                parts.ValueKind != JsonValueKind.Array ||
                requests.GetArrayLength() > 100 || parts.GetArrayLength() > 100)
                throw new ArgumentException("INVALID_REQUEST_LENGTH");
            string[] ids = requests.EnumerateArray().Select(Str).ToArray();
            OwnedPart[] owned = parts.EnumerateArray().Select(part =>
            {
                if (part.ValueKind != JsonValueKind.Object)
                    throw new ArgumentException("INVALID_STOCK_ENTRY");
                bool gun = part.TryGetProperty("weapon", out var w);
                if (gun) RequireKeys(part, "kind", "weapon", "count");
                else RequireKeys(part, "kind", "count");
                return new OwnedPart
                {
                    Kind = Str(part.GetProperty("kind")),
                    Weapon = gun ? Str(w) : "",
                    Quantity = Num(part.GetProperty("count"))
                };
            }).ToArray();
            // Main game planner validates ALL 154 immutable #411 semantic
            // BOM/price fields AND rejects malformed, duplicate or alien stock.
            KitRecipe[] recipes = LoadApprovedRecipes(approvedCatalogJson);
            ReadinessPlan result = KitReadinessPlanner.Preview(
                KitReadinessPlanner.SourceSha, recipes, owned, ids);
            // Forecast addresses the full requested batch (including kits
            // currently blocked), not only accepted atomic virtual reservations.
            ProcurementForecast procurement = KitProcurementForecast.Calculate(
                KitReadinessPlanner.SourceSha, recipes, owned, ids);
            KitBuildOrderAdvice buildOrder = KitBuildOrderAdvisor.Advise(
                KitReadinessPlanner.SourceSha, recipes, owned, ids);
            return JsonSerializer.Serialize(new
            {
                schema = "b01-kitreadiness-preview-v1",
                source_sha = KitReadinessPlanner.SourceSha,
                reference_only = true,
                modifies_game = false,
                request_count = ids.Length,
                result,
                procurement,
                buildOrder
            }, OutputOptions).Replace("\r\n", "\n") + "\n";
        }
    }

    private static bool IsGamePath(string repositoryRoot, string output)
    {
        string relative = Path.GetRelativePath(repositoryRoot, output).Replace('\\', '/');
        string[] guarded = { "Assets", "Packages", "ProjectSettings",
                             "Library", "UserSettings", ".git" };
        return guarded.Any(folder => relative.Equals(folder,
            StringComparison.OrdinalIgnoreCase) || relative.StartsWith(
                folder + "/", StringComparison.OrdinalIgnoreCase));
    }

    // Refuse aliases that redirect new output into a real Unity/game directory.
    // An ancestor symlink can bypass a simple lexical Assets/ path check.
    private static bool HasSymlinkedParent(string output)
    {
        string directory = Path.GetDirectoryName(output);
        while (!String.IsNullOrEmpty(directory))
        {
            if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0)
                return true;
            string previous = Path.GetDirectoryName(directory);
            if (String.IsNullOrEmpty(previous) || previous == directory) break;
            directory = previous;
        }
        return false;
    }

    public static int Run(string inputPath, string outputPath, string catalogPath,
                          string repositoryRoot)
    {
        try
        {
            string input = Path.GetFullPath(inputPath);
            string output = Path.GetFullPath(outputPath);
            if (input == output || Path.GetExtension(output) != ".json" ||
                IsGamePath(repositoryRoot, output) || HasSymlinkedParent(output) ||
                !Directory.Exists(Path.GetDirectoryName(output)) ||
                !File.Exists(input) ||
                (File.GetAttributes(input) & FileAttributes.ReparsePoint) != 0 ||
                (new FileInfo(input)).Length > MaxInputBytes)
                throw new ArgumentException("UNSAFE_SOURCE_OR_OUTPUT_PATH");
            // Parse/validate and run ALL actual original C# before creating output.
            string originalInput = File.ReadAllText(input, new UTF8Encoding(false, true));
            string catalog = File.ReadAllText(catalogPath, new UTF8Encoding(false, true));
            string result = Evaluate(originalInput, catalog);
            byte[] bytes = new UTF8Encoding(false).GetBytes(result);
            // Atomic no-clobber. No touching game files or caller's input.
            using (var file = new FileStream(output, FileMode.CreateNew, FileAccess.Write,
                FileShare.None))
                file.Write(bytes, 0, bytes.Length);
            Console.WriteLine("KITREADINESS_USER_PREVIEW_PASS");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine("KITREADINESS_FAIL_CLOSED:" + error.GetType().Name +
                ":" + error.Message);
            return 2;
        }
    }

    public static int Main(string[] args)
    {
        if (args.Length != 4 || args[0] != "--input" || args[2] != "--out")
        {
            Console.Error.WriteLine("USAGE: --input <request.json> --out <new.json>");
            return 2;
        }
        string root = Directory.GetCurrentDirectory();
        return Run(args[1], args[3], Path.Combine(root, ApprovedCatalogPath), root);
    }
}
