// W11 B01-21. Compiles frozen REAL MapGenPlanner, MapGenData and MapGenRouteValidator
// twice under each pinned Git revision using pre-existing UnityEngine SURROGATE.
// NO Unity Editor / Rigidbody / URP renderer executed here.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using B01.MAPGEN001;
using UnityEngine;

public sealed class W11Case
{
    public string Id { get; set; } = "";
    public string Recipe { get; set; } = "";
    public int Seed { get; set; }
    public string Profile { get; set; } = "";
    public float Width { get; set; }
    public float Length { get; set; }
    public float LaneWidth { get; set; }
    public int Segments { get; set; }
    public float Difficulty { get; set; }
    public string Result { get; set; } = "";
    public string RouteCode { get; set; } = "";
    public string GeometryHash { get; set; } = "";
    public int ElementCount { get; set; }
    public int RampCount { get; set; }
    public float? MinimumAxialFloorOverlapM { get; set; }
    public float? MinimumFloorXOverlapM { get; set; }
    public float? MinimumRampBoundaryLateralGapM { get; set; }
    public float? MaximumRampFloorIntrusionM { get; set; }
    public float? MinimumRampWidthMinusVehicleWidthM { get; set; }
    public string Note { get; set; } = "";
}
public static class W11Runner
{
    static readonly List<W11Case> Rows = new();
    static MapVehicleProfile Rig(bool heavy, bool measured)
    {
        var p = heavy ? MapVehicleProfile.Heavy : MapVehicleProfile.Light;
        if (measured) p.Width = heavy ? 5.180f : 2.980f;
        // Retain conservative nominal length, longer than measured hull 4.920/2.720.
        return p;
    }

    static void Add(string id, MapRecipe recipe, int seed, bool heavy,
                    bool measured, MapGenConfig cfg)
    {
        var vehicle = Rig(heavy, measured);
        var row = new W11Case {
            Id=id, Recipe=recipe.ToString(), Seed=seed,
            Profile=(heavy?"Heavy":"Light")+(measured?"_MEASURED":"_NOMINAL"),
            Width=vehicle.Width, Length=vehicle.Length, LaneWidth=cfg.LaneWidth,
            Segments=cfg.Segments, Difficulty=cfg.Difficulty
        };
        try
        {
            var map=MapGenPlanner.Plan(recipe,seed,cfg,vehicle);
            var route=MapGenRouteValidator.Validate(map,vehicle);
            row.Result=route.Passed?"SOURCE_LAYOUT_PASS":"ROUTE_VALIDATOR_REJECTED";
            row.RouteCode=route.Code;
            row.GeometryHash=map.GeometryHash;
            row.ElementCount=map.Elements.Count;
            var floors=map.Elements.Where(x=>x.Kind==MapElementKind.Floor)
                .OrderBy(x=>x.Position.z).ToArray();
            float? minAxial=null, minX=null;
            for(int i=1;i<floors.Length;i++)
            {
                float z=(floors[i-1].Size.z+floors[i].Size.z)*.5f-
                        MathF.Abs(floors[i].Position.z-floors[i-1].Position.z);
                float x=(floors[i-1].Size.x+floors[i].Size.x)*.5f-
                        MathF.Abs(floors[i].Position.x-floors[i-1].Position.x);
                minAxial=minAxial.HasValue?MathF.Min(minAxial.Value,z):z;
                minX=minX.HasValue?MathF.Min(minX.Value,x):x;
            }
            row.MinimumAxialFloorOverlapM=minAxial;
            row.MinimumFloorXOverlapM=minX;
            float? minGap=null,maxIntrusion=null,minRampWidth=null;
            foreach(var ramp in map.Elements.Where(x=>x.Kind==MapElementKind.Ramp))
            {
                row.RampCount++;
                float widthMargin=ramp.Size.x-vehicle.Width;
                minRampWidth=minRampWidth.HasValue?MathF.Min(minRampWidth.Value,widthMargin):widthMargin;
                float pitch=ramp.Rotation.eulerAngles.x*MathF.PI/180f;
                float yProjection=.5f*(MathF.Abs(MathF.Cos(pitch))*ramp.Size.y+
                                            MathF.Abs(MathF.Sin(pitch))*ramp.Size.z);
                float rampBottom=ramp.Position.y-yProjection;
                foreach(var floor in floors)
                {
                    float xOverlap=.5f*(floor.Size.x+ramp.Size.x)-
                                   MathF.Abs(floor.Position.x-ramp.Position.x);
                    float zHalf=.5f*(MathF.Abs(MathF.Cos(pitch))*ramp.Size.z+
                                    MathF.Abs(MathF.Sin(pitch))*ramp.Size.y);
                    float zOverlap=.5f*floor.Size.z+zHalf-
                                   MathF.Abs(floor.Position.z-ramp.Position.z);
                    if(xOverlap>0 && zOverlap>0)
                    {
                        float intrusion=MathF.Max(0f,floor.Position.y+floor.Size.y*.5f-rampBottom);
                        maxIntrusion=maxIntrusion.HasValue?MathF.Max(maxIntrusion.Value,intrusion):intrusion;
                    }
                }
                foreach(var boundary in map.Elements.Where(x=>x.Kind==MapElementKind.Boundary))
                {
                    if(MathF.Abs(boundary.Position.z-ramp.Position.z)>10f) continue;
                    float xGap=MathF.Abs(boundary.Position.x-ramp.Position.x)-
                               .5f*(boundary.Size.x+ramp.Size.x);
                    // Inspect only closest corresponding side boundary.
                    if(MathF.Abs(boundary.Position.x-ramp.Position.x) < cfg.LaneWidth*.65f)
                        minGap=minGap.HasValue?MathF.Min(minGap.Value,xGap):xGap;
                }
            }
            row.MinimumRampBoundaryLateralGapM=minGap;
            row.MaximumRampFloorIntrusionM=maxIntrusion;
            row.MinimumRampWidthMinusVehicleWidthM=minRampWidth;
            row.Note="Floor seam/rotated ramp AABB checks are conservative XZ/Y projections, NOT collider manifold or actual vehicle PhysX.";
        }
        catch(ArgumentException ex)
        {
            row.Result="CONFIG_REJECTED";
            row.RouteCode=ex.Message;
            row.Note="Expected rejection may indicate product-profile mismatch, not generator bug.";
        }
        catch(Exception ex)
        {
            row.Result="UNEXPECTED_EXCEPTION";
            row.RouteCode=ex.GetType().Name+":"+ex.Message;
        }
        Rows.Add(row);
    }
    public static int Main(string[] args)
    {
        if(args.Length<2) {Console.Error.WriteLine("usage: W11Cases <REVISION> <OUTPUT_JSON>");return 3;}
        var cfg=MapGenConfig.Default;
        // Actual target collider widths; 12 original R6 route combinations.
        foreach(var recipe in new[]{MapRecipe.CombatArena,MapRecipe.ObstacleCourse})
        foreach(int seed in new[]{7,31,83})
        foreach(bool heavy in new[]{false,true})
            Add("BASE/"+recipe+"/"+seed+"/"+(heavy?"Heavy":"Light"),recipe,seed,heavy,true,cfg);
        // Historical Heavy normal vs hard measured widths; keep repeats as distinct trials.
        foreach(int seed in new[]{2,7,31})
            Add("HEAVY_NORMAL/"+seed,MapRecipe.ObstacleCourse,seed,true,true,cfg);
        var hard=MapGenConfig.Default;hard.Difficulty=1f;hard.ObstacleDensity=100;
        foreach(int seed in new[]{0,2,7,19})
            Add("HEAVY_HARD/"+seed,MapRecipe.ObstacleCourse,seed,true,true,hard);
        // Crucial contrast: old nominal Light10m vs actual 2.980m Light10m.
        var narrow=MapGenConfig.Default;
        narrow.LaneWidth=10f;narrow.Segments=3;narrow.Difficulty=1f;narrow.ObstacleDensity=100;
        foreach(int seed in new[]{2,12,14,22,28})
        {
            Add("NARROW10_NOMINAL/"+seed,MapRecipe.ObstacleCourse,seed,false,false,narrow);
            Add("NARROW10_MEASURED/"+seed,MapRecipe.ObstacleCourse,seed,false,true,narrow);
        }
        // Same layout, two profile envelopes: validate that geometry hash is not profile-dependent.
        foreach(var recipe in new[]{MapRecipe.CombatArena,MapRecipe.ObstacleCourse})
        foreach(bool heavy in new[]{false,true})
            Add("PROFILE_CONTROL/"+recipe+"/"+(heavy?"Heavy":"Light"),recipe,7,heavy,false,cfg);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[1]))!);
        File.WriteAllText(args[1],JsonSerializer.Serialize(
          new {revision=args[0],tier="ACTUAL_FROZEN_MAPGEN_CSHARP_UNITY_SURROGATE_NOT_PHYSX",
               count=Rows.Count,cases=Rows},new JsonSerializerOptions{WriteIndented=true}));
        foreach(var row in Rows)
            Console.WriteLine(args[0]+" | "+row.Id+" | "+row.Result+
                " | "+row.RouteCode+" | "+row.GeometryHash);
        Console.WriteLine(args[0]+" CASES="+Rows.Count+" UNITY_NATIVE=NOT_RUN PHYSX=NOT_RUN");
        return Rows.Any(x=>x.Result=="UNEXPECTED_EXCEPTION")?2:0;
    }
}
