// Independent real C# MAPGEN planner+validator exercise with UnityEngine surrogate.
// This executes EXACT referenced MapGenData/MapGenPlanner/MapGenRouteValidator C# files,
// NOT real Unity Editor/PhysX/weighted R6 wheels. Every fail is recorded.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using B01.MAPGEN001;
using UnityEngine;

sealed class CaseResult
{
    public string Id { get; set; }="";
    public string Result { get; set; }="";
    public string Detail { get; set; }="";
    public string GeometryHash { get; set; }="";
}
static class Program
{
    static readonly List<CaseResult> All=new();
    static void Check(string id,MapRecipe recipe,int seed,MapGenConfig cfg,MapVehicleProfile vehicle)
    {
        try {
            var plan=MapGenPlanner.Plan(recipe,seed,cfg,vehicle);
            var report=MapGenRouteValidator.Validate(plan,vehicle);
            if(!report.Passed)throw new InvalidOperationException(report.Code);
            var second=MapGenPlanner.Plan(recipe,seed,cfg,vehicle);
            if(second.GeometryHash!=plan.GeometryHash)
                throw new InvalidOperationException("NONDETERMINISTIC_LAYOUT");
            if(!plan.Diagnostics.Contains("BFS_SPINE_CLEARANCE_OK"))
                throw new InvalidOperationException("NO_CLEARANCE_CERTIFICATE");
            All.Add(new CaseResult{Id=id,Result="PLAN_VALIDATED_SOURCE_CSHARP",GeometryHash=plan.GeometryHash,Detail=report.Code});
        }catch(Exception ex) {
            All.Add(new CaseResult{Id=id,Result="FAIL_REAL_PLANNER",Detail=ex.GetType().Name+": "+ex.Message});
        }
    }
    static void Negative(string id,MapRecipe recipe,MapGenConfig config,MapVehicleProfile vehicle)
    {
        try {
            var result=MapGenPlanner.Plan(recipe,5,config,vehicle);
            All.Add(new CaseResult{Id=id,Result="FAIL_NEGATIVE_NOT_REJECTED",GeometryHash=result.GeometryHash});
        }catch(ArgumentException ex){All.Add(new CaseResult{Id=id,Result="EXPECTED_REJECTION",Detail=ex.Message});}
        catch(Exception ex){All.Add(new CaseResult{Id=id,Result="FAIL_WRONG_EXCEPTION",Detail=ex.ToString()});}
    }
    static int Main(string[] args)
    {
        string outDir=args.Length>0?args[0]:"headless-mapgen-evidence";
        Directory.CreateDirectory(outDir);
        // Exhaustive current-source test: 100 seeds per recipe and both vehicle profiles at default.
        foreach(MapRecipe recipe in new[]{MapRecipe.CombatArena,MapRecipe.ObstacleCourse})
        foreach(bool heavy in new[]{false,true})
        for(int seed=0;seed<100;seed++) {
            var profile=heavy?MapVehicleProfile.Heavy:MapVehicleProfile.Light;
            Check($"DEFAULT/{recipe}/{(heavy?"Heavy":"Light")}/{seed}",recipe,seed,MapGenConfig.Default,profile);
        }
        // Prior suspect seeds 2,7,31 normal & 0,2,7,19 hard; source test, NOT actual wheel driving.
        foreach(float dif in new[]{.5f,1f})
        foreach(int seed in new[]{0,2,7,19,31})
        {
            var cfg=MapGenConfig.Default;
            cfg.Difficulty=dif;
            Check($"SUSPECT/Heavy/{dif.ToString(CultureInfo.InvariantCulture)}/{seed}",
                  MapRecipe.ObstacleCourse,seed,cfg,MapVehicleProfile.Heavy);
        }
        // 100 seeds x 3 course lengths x two real vehicle envelope configurations.
        foreach(int seg in new[]{3,4,5})
        foreach(bool heavy in new[]{false,true})
        for(int seed=0;seed<100;seed++)
        {
            var cfg=MapGenConfig.Default;
            cfg.Segments=seg;cfg.Difficulty=1f;cfg.ObstacleDensity=100;cfg.LaneWidth=heavy?16f:10f;
            Check($"STRESS/{seg}/{(heavy?"Heavy":"Light")}/{seed}",
                  MapRecipe.ObstacleCourse,seed,cfg,heavy?MapVehicleProfile.Heavy:MapVehicleProfile.Light);
        }
        var bad=MapGenConfig.Default;bad.LaneWidth=10;
        Negative("NEGATIVE/HeavyCourse10m",MapRecipe.ObstacleCourse,bad,MapVehicleProfile.Heavy);
        bad=MapGenConfig.Default;bad.Segments=2;
        Negative("NEGATIVE/TooFewSegments",MapRecipe.ObstacleCourse,bad,MapVehicleProfile.Light);
        var broken=MapVehicleProfile.Light;broken.MaxSlopeDegrees=float.NaN;
        Negative("NEGATIVE/NaNSlope",MapRecipe.ObstacleCourse,MapGenConfig.Default,broken);
        var fails=All.Where(c=>c.Result.StartsWith("FAIL_",StringComparison.Ordinal)).ToArray();
        var snapshots=All.Where(x=>x.Result=="PLAN_VALIDATED_SOURCE_CSHARP").ToArray();
        var summary=new {
            name="B01-MAPGEN-001 SOURCE-REAL-C#-HEADLESS",
            meaning="EXACT planner/route-validator compiled and executed using surrogate UnityEngine structs; NOT native Unity compile/PlayMode or physics",
            total_cases=All.Count,passed_source_cases=snapshots.Length,
            negative_rejections=All.Count(x=>x.Result=="EXPECTED_REJECTION"),
            failed_cases=fails.Length,unique_geometry_hashes=snapshots.Select(x=>x.GeometryHash).Distinct().Count(),
            cases=All
        };
        File.WriteAllText(Path.Combine(outDir,"results.json"),
             JsonSerializer.Serialize(summary,new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine($"SOURCE_CSHARP_CASES={All.Count} SOURCE_PASS={snapshots.Length} NEGATIVE_PASS={summary.negative_rejections} FAIL={fails.Length} DISTINCT_LAYOUTS={summary.unique_geometry_hashes}");
        foreach(var f in fails.Take(35))Console.WriteLine("REAL_CSHARP_FAIL "+f.Id+" => "+f.Detail);
        Console.WriteLine("UNITY_NATIVE_EDITMODE=NOT_RUN UNITY_NATIVE_PLAYMODE=NOT_RUN PHYSICAL_R6_DRIVES=0/12");
        return fails.Length>0?2:0;
    }
}
