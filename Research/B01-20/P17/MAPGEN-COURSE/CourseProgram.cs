// MAPGEN-COURSE-P17: three NEW content profiles, original MAPGEN C# planner/validator.
// Exact original source files are directly linked in the .csproj, never copied/edited.
// UnitySurrogate is math-only for .NET source QA, not a Unity/PhysX runtime.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using B01.MAPGEN001;
using UnityEngine;

public sealed class Pack {
    public string PackageId {get;set;}="";
    public int SchemaVersion {get;set;}
    public string SourceSha {get;set;}="";
    public string Recipe {get;set;}="";
    public string UnityVersion {get;set;}="";
    public int[] Seeds {get;set;}=Array.Empty<int>();
    public string[] Profiles {get;set;}=Array.Empty<string>();
    public Variant[] Variants {get;set;}=Array.Empty<Variant>();
}
public sealed class Variant {
    public string Id {get;set;}="";
    public string Description {get;set;}="";
    public MapGenConfig Config {get;set;}
}
static class Program {
    const string SourceSha="5be31039411f89f17654b1c332f8692883fa03cc";
    static readonly JsonSerializerOptions Opt=new() {
        PropertyNamingPolicy=JsonNamingPolicy.CamelCase,PropertyNameCaseInsensitive=true,
        IncludeFields=true,WriteIndented=true
    };
    static void Assert(bool ok,string msg){if(!ok)throw new InvalidDataException(msg);}
    static object V(Vector3 p)=>new{x=p.x,y=p.y,z=p.z};
    static object El(MapElement e)=>new{
        kind=e.Kind.ToString(),position=V(e.Position),size=V(e.Size),
        rotationEuler=V(e.Rotation.eulerAngles)
    };
    static bool Rejected(MapGenConfig cfg,MapVehicleProfile profile) {
        try{MapGenPlanner.Plan(MapRecipe.ObstacleCourse,7,cfg,profile);return false;}
        catch(ArgumentException){return true;}
    }
    static int Main(string[] args) {
        if(args.Length!=2){Console.Error.WriteLine("Usage: dotnet run --project MapgenCourseP17.csproj -- presets.json output-dir");return 2;}
        try {
            var pack=JsonSerializer.Deserialize<Pack>(File.ReadAllText(args[0]),Opt);
            Assert(pack!=null && pack.PackageId=="MAPGEN-COURSE-P17" && pack.SchemaVersion==1,"SCHEMA_OR_PACKAGE");
            Assert(pack!.SourceSha==SourceSha && pack.Recipe=="ObstacleCourse" && pack.UnityVersion=="6000.3.14f1","SOURCE_OR_RECIPE");
            Assert(pack.Profiles.SequenceEqual(new[]{"Light","Heavy"}),"PROFILE_SET");
            Assert(pack.Seeds.Length==6 && pack.Seeds.Distinct().Count()==6,"SEED_SET");
            Assert(pack.Variants.Length==3 && pack.Variants.Select(v=>v.Id).Distinct().Count()==3,"VARIANT_SET");
            Assert(pack.Variants.Select(v=>v.Config.Segments).SequenceEqual(new[]{3,4,5}),"MUST_HAVE_3_4_5_SEGMENTS");
            Directory.CreateDirectory(args[1]);
            int validations=0, negatives=0;
            var hashes=new HashSet<string>();
            var receipts=new List<object>();
            var coverCounts=new List<object>();
            foreach(var variant in pack.Variants) {
                Assert(variant.Id.StartsWith("P17-COURSE-",StringComparison.Ordinal),"BAD_VARIANT_ID");
                var invalid=variant.Config;invalid.Segments=2;
                Assert(Rejected(invalid,MapVehicleProfile.Light),"NEGATIVE_INVALID_SEGMENTS");negatives++;
                invalid=variant.Config;invalid.Difficulty=1.01f;
                Assert(Rejected(invalid,MapVehicleProfile.Light),"NEGATIVE_INVALID_DIFFICULTY");negatives++;
                invalid=variant.Config;invalid.LaneWidth=10f;
                Assert(Rejected(invalid,MapVehicleProfile.Heavy),"NEGATIVE_HEAVY_NARROW_LANE");negatives++;
                var perVariant=new HashSet<string>();
                var totalObstacles=0;var totalRamps=0;
                foreach(var seed in pack.Seeds) {
                    MapBlueprint? candidate=null;
                    var validation=new List<object>();
                    foreach(var (name,profile) in new[]{
                        ("Light",MapVehicleProfile.Light),("Heavy",MapVehicleProfile.Heavy)}) {
                        var plan=MapGenPlanner.Plan(MapRecipe.ObstacleCourse,seed,variant.Config,profile);
                        var repeat=MapGenPlanner.Plan(MapRecipe.ObstacleCourse,seed,variant.Config,profile);
                        var result=MapGenRouteValidator.Validate(plan,profile);
                        Assert(result.Passed && result.Code=="BFS_SPINE_CLEARANCE_OK",
                               "UNSAFE_SOURCE_ROUTE:"+variant.Id+":"+seed+":"+name+":"+result.Code);
                        Assert(plan.GeometryHash==repeat.GeometryHash &&
                               plan.GeometryHash==MapGenPlanner.HashLayout(plan),"NONDETERMINISM");
                        Assert(plan.Checkpoints.Count==variant.Config.Segments,"CHECKPOINT_COUNT");
                        Assert(plan.Start.z<plan.Finish.z,"START_FINISH_ORDER");
                        Assert(plan.GuaranteedClearanceWidth>=profile.Width+.5f,"CLEARANCE_WIDTH");
                        if(candidate!=null)Assert(plan.GeometryHash==candidate.GeometryHash,"PROFILE_LAYOUT_CHANGED");
                        candidate=plan;
                        validations++;
                        validation.Add(new{profile=name,passed=result.Passed,code=result.Code,exploredCells=result.ExploredCells});
                    }
                    Assert(candidate!=null,"NO_GENERATED_BLUEPRINT");
                    var p=candidate!;
                    int floors=p.Elements.Count(e=>e.Kind==MapElementKind.Floor);
                    int bounds=p.Elements.Count(e=>e.Kind==MapElementKind.Boundary);
                    int ramps=p.Elements.Count(e=>e.Kind==MapElementKind.Ramp);
                    int obstacles=p.Elements.Count(e=>e.Kind==MapElementKind.Obstacle);
                    Assert(floors==variant.Config.Segments && bounds==variant.Config.Segments*2,"FLOOR_OR_BOUNDARY_COUNT");
                    Assert(ramps>=1,"NO_RAMP_CONTENT");
                    Assert(p.EnemySpawns.Count==0,"COURSE_SHOULD_NOT_CREATE_ENEMY_SPAWNS");
                    Assert(perVariant.Add(p.GeometryHash) && hashes.Add(p.GeometryHash),"GEOMETRY_HASH_DUPLICATE");
                    totalRamps+=ramps;totalObstacles+=obstacles;
                    string nameFile=variant.Id+"-seed-"+seed+".json";
                    File.WriteAllText(Path.Combine(args[1],nameFile),
                        JsonSerializer.Serialize(new{
                            packageId=pack.PackageId,variantId=variant.Id,description=variant.Description,
                            sourceSha=SourceSha,recipe=pack.Recipe,seed,config=variant.Config,
                            geometryHash=p.GeometryHash,generationVersion=p.GenerationVersion,
                            guaranteedClearanceWidthM=p.GuaranteedClearanceWidth,
                            start=V(p.Start),finish=V(p.Finish),checkpoints=p.Checkpoints.Select(V).ToArray(),
                            bounds=new{center=V(p.Bounds.center),size=V(p.Bounds.size)},
                            elements=p.Elements.Select(El).ToArray(),
                            counts=new{floors,boundaries=bounds,ramps,obstacles},profiles=validation,
                            nativeUnity="NOT_RUN",physxDrives=0
                        },Opt)+"\n");
                    receipts.Add(new{variant=variant.Id,seed,file=nameFile,geometryHash=p.GeometryHash,ramps,obstacles});
                }
                Assert(perVariant.Count==6,"INSUFFICIENT_SEED_VARIETY");
                coverCounts.Add(new{variant=variant.Id,meanObstacles=(float)totalObstacles/6,meanRamps=(float)totalRamps/6});
            }
            Assert(validations==36 && negatives==9 && hashes.Count==18 && receipts.Count==18,"COVERAGE_MISMATCH");
            File.WriteAllText(Path.Combine(args[1],"results.json"),
                JsonSerializer.Serialize(new{
                    packageId=pack.PackageId,sourceSha=SourceSha,
                    status="ORIGINAL_SOURCE_CSHARP_VALIDATED",variants=3,blueprints=18,
                    uniqueHashes=hashes.Count,originalCsharpRouteCases=validations,
                    originalPlannerInvalidRejections=negatives,realUnity="NOT_RUN",physxDrives=0,
                    counts=coverCounts,generated=receipts
                },Opt)+"\n");
            Console.WriteLine("P17_MAPGEN_COURSE_ORIGINAL_CSHARP_PASS=36 BLUEPRINTS=18 DISTINCT=18 NEGATIVES=9 UNITY=NOT_RUN");
            return 0;
        }catch(Exception e) {
            Console.Error.WriteLine("P17_MAPGEN_COURSE_FAIL "+e);
            return 2;
        }
    }
}
