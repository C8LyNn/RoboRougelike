// B01-21 independent orthogonal diagnostics; compiles exact frozen MAPGEN planner/validator
// against its existing UnityEngine surrogate. This is NOT Unity/PhysX/R6 vehicle execution.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using B01.MAPGEN001;
using UnityEngine;

sealed class DiagnosticRow
{
    public string Id { get; set; } = "";
    public string Tier { get; set; } = "CSHARP_ORIGINAL_SOURCE_UNITY_SURROGATE";
    public string Classification { get; set; } = "";
    public string Result { get; set; } = "";
    public string Recipe { get; set; } = "";
    public string Rig { get; set; } = "";
    public int? Seed { get; set; }
    public string Config { get; set; } = "";
    public string GeometryHash { get; set; } = "";
    public string RouteCode { get; set; } = "";
    public int Waypoints { get; set; }
    public float InitialForwardProjection { get; set; }
    public float MinFloorSeamOverlapMetres { get; set; }
    public string Expected { get; set; } = "";
    public string Actual { get; set; } = "";
    public string NativeUnity { get; set; } = "NOT_RUN";
    public string R6WheelPhysx { get; set; } = "NOT_RUN";
}
static class Program
{
    const string SourceSha = "003bced54480d509ffc0d49d26daf87160a8d609";
    static readonly List<DiagnosticRow> Rows = new();
    static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    static string F(float f) => f.ToString("0.###", CultureInfo.InvariantCulture);
    static string ConfigText(MapGenConfig cfg) =>
        "segments=" + cfg.Segments + ";density=" + cfg.ObstacleDensity +
        ";difficulty=" + F(cfg.Difficulty) + ";lane=" + F(cfg.LaneWidth);

    static void Scenario(string id, MapRecipe recipe, int seed, bool heavy, MapGenConfig config)
    {
        var profile = heavy ? MapVehicleProfile.Heavy : MapVehicleProfile.Light;
        var row = new DiagnosticRow {
            Id = id, Recipe = recipe.ToString(), Seed = seed,
            Rig = heavy ? "Heavy" : "Light", Config = ConfigText(config),
            Classification = "GENERATED_LAYOUT_DIAGNOSTIC",
            Expected = "planner valid; source route clear; waypoint order forward; floor seams overlap"
        };
        try
        {
            MapBlueprint blueprint = MapGenPlanner.Plan(recipe, seed, config, profile);
            row.GeometryHash = blueprint.GeometryHash;
            var report = MapGenRouteValidator.Validate(blueprint, profile);
            row.RouteCode = report.Code;
            var goals = new List<Vector3>(blueprint.Checkpoints);
            if (goals.Count == 0 || Vector3.Distance(goals[goals.Count - 1], blueprint.Finish) > .001f)
                goals.Add(blueprint.Finish);
            row.Waypoints = goals.Count;
            // Initial cube rotation is Quaternion.identity and its forward is +Z.
            // Projection checks the controller's target assumption, not actual driving dynamics.
            row.InitialForwardProjection = goals[0].z - blueprint.Start.z;
            var floors = blueprint.Elements.Where(x => x.Kind == MapElementKind.Floor)
                .OrderBy(x => x.Position.z).ToArray();
            float minSeam = float.PositiveInfinity;
            if (recipe == MapRecipe.ObstacleCourse)
            {
                if (floors.Length != config.Segments)
                    throw new InvalidOperationException("FLOOR_SEGMENT_COUNT:" + floors.Length);
                for (int i = 1; i < floors.Length; i++)
                {
                    var a = floors[i - 1]; var b = floors[i];
                    float zOverlap = (a.Size.z + b.Size.z) * .5f -
                                     MathF.Abs(b.Position.z - a.Position.z);
                    float xOverlap = (a.Size.x + b.Size.x) * .5f -
                                     MathF.Abs(b.Position.x - a.Position.x);
                    minSeam = MathF.Min(minSeam, zOverlap);
                    if (zOverlap <= 0f || xOverlap <= 0f)
                        throw new InvalidOperationException("OPEN_FLOOR_SEAM:" + i +
                            ":z=" + F(zOverlap) + ":x=" + F(xOverlap));
                }
            }
            row.MinFloorSeamOverlapMetres = float.IsPositiveInfinity(minSeam) ? 0f : minSeam;
            if (!report.Passed) throw new InvalidOperationException("ROUTE:" + report.Code);
            if (row.InitialForwardProjection <= 0f)
                throw new InvalidOperationException("FIRST_GOAL_BEHIND_INITIAL_FORWARD");
            if (recipe == MapRecipe.ObstacleCourse && blueprint.Checkpoints.Count != config.Segments)
                throw new InvalidOperationException("WAYPOINT_COUNT_MISMATCH");
            if (recipe == MapRecipe.CombatArena && blueprint.Checkpoints.Count != 0)
                throw new InvalidOperationException("UNEXPECTED_ARENA_CHECKPOINTS");
            for (int i = 1; i < goals.Count; i++)
                if (goals[i].z <= goals[i - 1].z)
                    throw new InvalidOperationException("NONMONOTONIC_GOAL_Z");
            row.Result = "PASS_HEADLESS_GEOMETRY_ONLY";
            row.Actual = report.Code + ";goals=" + goals.Count +
                ";firstForwardZ=" + F(row.InitialForwardProjection) +
                ";minSeamZ=" + F(row.MinFloorSeamOverlapMetres);
        }
        catch (Exception ex)
        {
            row.Result = "FAIL_HEADLESS_DIAGNOSTIC";
            row.Actual = ex.GetType().Name + ":" + ex.Message;
        }
        Rows.Add(row);
    }

    static void PitCases()
    {
        var vehicle = MapVehicleProfile.Light;
        float hw = vehicle.Width * .5f + .25f, hl = vehicle.Length * .5f + .25f;
        var p = new MapBlueprint {
            Recipe = MapRecipe.CombatArena,
            Start = new Vector3(0f, 1.25f, 0f),
            Finish = new Vector3(0f, 1.25f, 0f),
            Bounds = new Bounds(Vector3.zero, new Vector3(40f, 8f, 40f))
        };
        foreach (int x in new[] { -1, 1 })
            foreach (int z in new[] { -1, 1 })
                p.Elements.Add(new MapElement(MapElementKind.Floor,
                    new Vector3(x * hw, -.45f, z * hl),
                    new Vector3(.5f, .9f, .5f), Quaternion.identity));
        var r = MapGenRouteValidator.Validate(p, vehicle);
        Rows.Add(new DiagnosticRow {
            Id = "PIT/ORIGINAL_START_EQUALS_FINISH", Recipe = "CombatArena", Rig = "Light",
            Classification = "TEST_FIXTURE_EXPECTATION",
            Expected = "START_BLOCKED (actual fail-closed, not unsafe approval)",
            Actual = r.Code, RouteCode = r.Code,
            Result = !r.Passed && r.Code == "START_BLOCKED" ? "PASS_REPRODUCED_EXPECTATION_MISMATCH" : "FAIL_HEADLESS_DIAGNOSTIC"
        });
        // A distinct, fully supported start/finish connected only through a floor hole:
        // this actually exercises the swept centreline check, unlike the original fixture.
        var hole = MapGenPlanner.Plan(MapRecipe.CombatArena, 7,
            MapGenConfig.Default, MapVehicleProfile.Light);
        hole.Elements.RemoveAll(e => e.Kind == MapElementKind.Floor);
        foreach (float z in new[] { -16f, 16f })
            hole.Elements.Add(new MapElement(MapElementKind.Floor,
                new Vector3(0f, -.45f, z), new Vector3(64f, .9f, 30f),
                Quaternion.identity));
        r = MapGenRouteValidator.Validate(hole, vehicle);
        Rows.Add(new DiagnosticRow {
            Id = "PIT/SUPPORTED_ENDPOINTS_CENTRE_HOLE", Recipe = "CombatArena",
            Seed = 7, Rig = "Light", Classification = "ORTHOGONAL_SPINE_NEGATIVE",
            Expected = "SPINE_FOOTPRINT_BLOCKED (both endpoints supported)",
            Actual = r.Code, RouteCode = r.Code,
            Result = !r.Passed && r.Code == "SPINE_FOOTPRINT_BLOCKED" ? "PASS_NEGATIVE_REJECTED" : "FAIL_HEADLESS_DIAGNOSTIC"
        });
    }

    static void InspectUnityTestStructure(string root)
    {
        string path = Path.Combine(root, "Assets/B01Packages/MAPGEN001/Tests/PlayMode/MapGenPhysicalTraversalTests.cs");
        var row = new DiagnosticRow {
            Id = "HARNESS/PLAYMODE_MATRIX_INDEPENDENCE", Classification = "TEST_HARNESS_DESIGN",
            Tier = "STATIC_ORIGINAL_SOURCE_INSPECTION",
            Expected = "12 separate independent NUnit outcomes are required for 12 physical drives",
        };
        try
        {
            string code = File.ReadAllText(path);
            bool monolithic = code.Contains("LightAndHeavyRigidbodyDriveThreeSeedsForBothRecipes") &&
                code.Contains("foreach (MapRecipe recipe") &&
                code.Contains("foreach (int seed") &&
                code.Contains("foreach (bool heavy") &&
                code.Contains("Assert.IsTrue(reached");
            row.Actual = monolithic ?
                "One UnityTest with nested 12-case loops and an Assert in each iteration; first failure aborts matrix" :
                "Original test structure changed; review needed";
            row.Result = monolithic ? "PASS_MONOLITHIC_HARNESS_DEFECT_IDENTIFIED" : "FAIL_HEADLESS_DIAGNOSTIC";
        }
        catch (Exception ex) { row.Result = "FAIL_HEADLESS_DIAGNOSTIC"; row.Actual = ex.Message; }
        Rows.Add(row);
    }

    static int Main(string[] args)
    {
        string root = args.Length > 0 ? Path.GetFullPath(args[0]) : Directory.GetCurrentDirectory();
        string output = args.Length > 1 ? Path.GetFullPath(args[1]) : Path.Combine(root, "mapgen-diag-001-evidence");
        Directory.CreateDirectory(output);
        PitCases();
        foreach (var recipe in new[] { MapRecipe.CombatArena, MapRecipe.ObstacleCourse })
        foreach (int seed in new[] { 7, 31, 83 })
        foreach (bool heavy in new[] { false, true })
            Scenario("R6_PENDING/" + recipe + "/" + seed + "/" + (heavy ? "Heavy" : "Light"),
                recipe, seed, heavy, MapGenConfig.Default);
        foreach (int seed in new[] { 2, 7, 31 })
            Scenario("HEAVY/NORMAL/" + seed, MapRecipe.ObstacleCourse, seed, true, MapGenConfig.Default);
        foreach (int seed in new[] { 0, 2, 7, 19 })
        {
            var c = MapGenConfig.Default;
            c.Difficulty = 1f; c.ObstacleDensity = 100;
            Scenario("HEAVY/HARD/" + seed, MapRecipe.ObstacleCourse, seed, true, c);
        }
        foreach (int seed in new[] { 2, 12, 14, 22, 28 })
        {
            var c = MapGenConfig.Default;
            c.Difficulty = 1f; c.ObstacleDensity = 100; c.Segments = 3; c.LaneWidth = 10f;
            Scenario("LIGHT/NARROW10/" + seed, MapRecipe.ObstacleCourse, seed, false, c);
        }
        InspectUnityTestStructure(root);
        int fail = Rows.Count(x => x.Result.StartsWith("FAIL_", StringComparison.Ordinal));
        var receipt = new {
            tool = "B01-21 / MAPGEN-DIAG-001",
            source_sha = SourceSha,
            tier = "ACTUAL_FROZEN_MAPGEN_CSHARP_PLUS_EXISTING_UNITY_SURROGATE",
            note = "No licensed Unity, no BoxCollider or Rigidbody, no grounded R6 wheels",
            test_rows = Rows.Count,
            headless_fail = fail,
            physics_rows_pending = Rows.Count(x => x.Id.StartsWith("R6_PENDING/")),
            unity_native = "NOT_RUN",
            r6_physx = "NOT_RUN",
            cases = Rows
        };
        File.WriteAllText(Path.Combine(output, "results.json"), JsonSerializer.Serialize(receipt, JsonOptions));
        var table = new StringBuilder("| ID | Result | Actual | Geometry hash |\n|---|---|---|---|\n");
        foreach (var row in Rows)
        {
            string compact = row.Actual.Replace("|", "/").Replace("\r", " ").Replace("\n", " ");
            table.Append("| ").Append(row.Id).Append(" | ").Append(row.Result).Append(" | ")
                .Append(compact).Append(" | ").Append(row.GeometryHash).AppendLine(" |");
        }
        File.WriteAllText(Path.Combine(output, "cases.md"), table.ToString());
        foreach (var row in Rows)
        {
            string file = string.Concat(row.Id.Select(c => char.IsLetterOrDigit(c) ? c : '_')) + ".json";
            File.WriteAllText(Path.Combine(output, file), JsonSerializer.Serialize(row, JsonOptions));
        }
        Console.WriteLine("MAPGEN_DIAG_001_SOURCE_SHA=" + SourceSha);
        Console.WriteLine("HEADLESS_DIAGNOSTIC_ROWS=" + Rows.Count + " FAIL=" + fail);
        foreach (var row in Rows) Console.WriteLine(row.Id + "\t" + row.Result + "\t" + row.Actual);
        Console.WriteLine("UNITY6000_3_14f1_NATIVE=NOT_RUN R6_ACTUAL_PHYSX=NOT_RUN");
        return fail == 0 ? 0 : 2;
    }
}
