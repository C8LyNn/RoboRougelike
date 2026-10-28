// B01-20 P16 — exact original MAPGEN planner/route-validator C# reused WITHOUT editing them.
// The referenced UnitySurrogate emulates only vector math, NOT a Unity/PhysX run.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using B01.MAPGEN001;
using UnityEngine;

public sealed class ArenaPack {
    public string PackageId { get; set; } = "";
    public int SchemaVersion { get; set; }
    public string OriginalMapgenOwnerCommit { get; set; } = "";
    public string OriginalMapgenRecipe { get; set; } = "";
    public string RequiredUnityVersion { get; set; } = "";
    public string[] Profiles { get; set; } = Array.Empty<string>();
    public ArenaVariant[] Variants { get; set; } = Array.Empty<ArenaVariant>();
}
public sealed class ArenaVariant {
    public string Id { get; set; } = "";
    public string Label { get; set; } = "";
    public MapGenConfig Config { get; set; }
    public int[] Seeds { get; set; } = Array.Empty<int>();
}
internal static class ArenaProgram {
    const string OriginalSha = "5be31039411f89f17654b1c332f8692883fa03cc";
    static readonly JsonSerializerOptions Json = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        IncludeFields = true,
        WriteIndented = true
    };
    static void Check(bool ok, string message) {
        if (!ok) throw new InvalidOperationException(message);
    }
    static object Vec(Vector3 v) => new { x=v.x, y=v.y, z=v.z };
    static object Element(MapElement e) => new {
        kind=e.Kind.ToString(), position=Vec(e.Position), size=Vec(e.Size),
        rotationEuler=Vec(e.Rotation.eulerAngles)
    };
    static int Main(string[] args) {
        if (args.Length!=2) { Console.Error.WriteLine("Usage: ArenaProgram presets.json output-dir"); return 2; }
        try {
            var pack=JsonSerializer.Deserialize<ArenaPack>(File.ReadAllText(args[0]),Json);
            Check(pack!=null && pack.PackageId=="MAPGEN-ARENA-P16" && pack.SchemaVersion==1,"INVALID_PRESET_SCHEMA");
            Check(pack!.OriginalMapgenOwnerCommit==OriginalSha &&
                  pack.OriginalMapgenRecipe=="CombatArena" &&
                  pack.RequiredUnityVersion=="6000.3.14f1","WRONG_SOURCE_OR_RECIPE_VERSION");
            Check(pack.Profiles.SequenceEqual(new[]{"Light","Heavy"}),"PROFILE_SET");
            Check(pack.Variants.Length==3 && pack.Variants.Select(v=>v.Id).Distinct().Count()==3,"THREE_VARIANTS");
            Directory.CreateDirectory(args[1]);
            var all=new List<object>();
            var hashes=new HashSet<string>();
            int passes=0, negatives=0;
            foreach(var variant in pack.Variants) {
                Check(variant.Id.StartsWith("P16-ARENA-",StringComparison.Ordinal),"INVALID_VARIANT_ID");
                Check(variant.Seeds.Length==6 && variant.Seeds.Distinct().Count()==6,"SIX_UNIQUE_SEEDS");
                var invalidConfig=variant.Config; invalidConfig.Difficulty=1.01f;
                bool badDifficulty=false;
                try { MapGenPlanner.Plan(MapRecipe.CombatArena,7,invalidConfig,MapVehicleProfile.Light); }
                catch(ArgumentException) { badDifficulty=true; }
                Check(badDifficulty,"DIFFICULTY_NEGATIVE_NOT_REJECTED"); negatives++;
                var impossible=MapVehicleProfile.Heavy; impossible.Width=6.01f;
                bool badWidth=false;
                try { MapGenPlanner.Plan(MapRecipe.CombatArena,7,variant.Config,impossible); }
                catch(ArgumentException) { badWidth=true; }
                Check(badWidth,"HEAVY_WIDTH_NEGATIVE_NOT_REJECTED"); negatives++;
                var variantHashes=new HashSet<string>();
                foreach(int seed in variant.Seeds) {
                    MapBlueprint? physical=null;
                    var profiles=new List<object>();
                    foreach(var (profileName,profile) in new[]{
                        ("Light",MapVehicleProfile.Light),("Heavy",MapVehicleProfile.Heavy)}) {
                        // Both calls use the ORIGINAL C# planner; no fixed layout selection.
                        var plan=MapGenPlanner.Plan(MapRecipe.CombatArena,seed,variant.Config,profile);
                        var repeat=MapGenPlanner.Plan(MapRecipe.CombatArena,seed,variant.Config,profile);
                        var route=MapGenRouteValidator.Validate(plan,profile);
                        Check(route.Passed && route.Code=="BFS_SPINE_CLEARANCE_OK",
                              "SOURCE_ROUTE_FAIL:"+variant.Id+"/"+seed+"/"+profileName+":"+route.Code);
                        Check(plan.GeometryHash==repeat.GeometryHash &&
                              plan.GeometryHash==MapGenPlanner.HashLayout(plan),"DETERMINISM_FAIL");
                        Check(plan.GuaranteedClearanceWidth>=profile.Width+.5f,"PROFILE_CLEARANCE_FAIL");
                        Check(plan.EnemySpawns.Count==3,"ENEMY_SPAWN_COUNT_FAIL");
                        if(physical!=null)Check(physical.GeometryHash==plan.GeometryHash,"PROFILE_CHANGES_GEOMETRY");
                        physical=plan;
                        passes++;
                        profiles.Add(new {profile=profileName,sourceRouteStatus=route.Code,
                            exploredCells=route.ExploredCells,sourceValidatorPassed=route.Passed});
                    }
                    Check(physical!=null,"NO_MAP_BLUEPRINT");
                    Check(variantHashes.Add(physical!.GeometryHash),"DUPLICATE_SEED_LAYOUT");
                    Check(hashes.Add(physical.GeometryHash),"DUPLICATE_LAYOUT_BETWEEN_VARIANTS");
                    var file=variant.Id+"-seed-"+seed+".json";
                    File.WriteAllText(Path.Combine(args[1],file),
                        JsonSerializer.Serialize(new {
                            packageId=pack.PackageId,variantId=variant.Id,variantLabel=variant.Label,
                            sourceOwnerSha=OriginalSha,sourceRecipe="CombatArena",seed,config=variant.Config,
                            generationVersion=physical.GenerationVersion,geometryHash=physical.GeometryHash,
                            guaranteedClearanceWidthM=physical.GuaranteedClearanceWidth,
                            start=Vec(physical.Start),finish=Vec(physical.Finish),
                            enemySpawns=physical.EnemySpawns.Select(Vec).ToArray(),
                            checkpoints=physical.Checkpoints.Select(Vec).ToArray(),
                            bounds=new{center=Vec(physical.Bounds.center),size=Vec(physical.Bounds.size)},
                            elements=physical.Elements.Select(Element).ToArray(),sourceValidation=profiles,
                            nativeUnityPhysics="NOT_RUN"
                        },Json)+"\n");
                    all.Add(new {variant=variant.Id,seed,file,geometryHash=physical.GeometryHash,
                        sourceElementCount=physical.Elements.Count});
                }
                Check(variantHashes.Count==6,"SEED_VARIETY_FAIL");
            }
            Check(hashes.Count==18 && passes==36 && negatives==6,"TOTAL_COVERAGE_FAIL");
            File.WriteAllText(Path.Combine(args[1],"results.json"),
                JsonSerializer.Serialize(new {
                    status="SOURCE_ORIGINAL_CSHARP_PASS",sourceSha=OriginalSha,
                    totalVariants=3,uniqueGeneratedMaps=hashes.Count,actualOriginalCsharpPlans=passes,
                    originalPlannerNegativeRejections=negatives,actualUnityNative="NOT_RUN",
                    actualPhysxDrives=0,generatedBlueprints=all
                },Json)+"\n");
            Console.WriteLine("MAPGEN_P16_SOURCE_PASS variants=3 blueprints=18 OriginalCSharpPlanAndRoute=36 negatives=6 Unity=NOT_RUN");
            return 0;
        } catch(Exception ex) {
            Console.Error.WriteLine("MAPGEN_P16_SOURCE_FAIL "+ex);
            return 2;
        }
    }
}
