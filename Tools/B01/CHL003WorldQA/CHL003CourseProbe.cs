// B01-21 / CHL-003 supporting QA. This executes pinned ACTUAL MAPGEN001
// MapGenPlanner and MapGenRouteValidator C#, with clearly synthetic Unity math.
// No CHL/B01-20 source, no Unity PhysX, no R6 driving or approval of a course.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using B01.MAPGEN001;
using UnityEngine;

internal static class CHL003CourseProbe
{
    static readonly List<object> Rows=new List<object>();
    static int passed, failed;
    const string ProducerSha="5be31039411f89f17654b1c332f8692883fa03cc";
    static MapVehicleProfile Vehicle(bool heavy)
    {
        var p=heavy?MapVehicleProfile.Heavy:MapVehicleProfile.Light;
        p.Width=heavy?5.180f:2.980f; // actual user-calibrated reference, NOT nominal rig
        return p;
    }
    static MapGenConfig Config(int segments,float lane=16f) =>
        new MapGenConfig {Segments=segments,ObstacleDensity=50,Difficulty=.5f,LaneWidth=lane};
    static float[] Point(Vector3 p) => new[]{p.x,p.y,p.z};
    static void Assert(bool value,string id)
    {
        if(!value)throw new InvalidOperationException(id);
    }
    static void Record(string id,Action action)
    {
        try {action();passed++;Console.WriteLine("CHL003_CASE PASS "+id);}
        catch(Exception ex){failed++;Console.WriteLine("CHL003_CASE FAIL "+id+" "+ex);}
    }

    static void Positive(int seed,int segments,bool heavy)
    {
        var rig=Vehicle(heavy);var cfg=Config(segments);
        MapBlueprint p=MapGenPlanner.Plan(MapRecipe.ObstacleCourse,seed,cfg,rig);
        MapGenRouteValidator.Report route=MapGenRouteValidator.Validate(p,rig);
        Assert(route.Passed && route.Code=="BFS_SPINE_CLEARANCE_OK","NO_PROTECTED_ROUTE");
        Assert(p.Checkpoints.Count==segments,"WRONG_CHECKPOINT_COUNT");
        Assert(p.Start.z<p.Checkpoints[0].z &&
            Vector3.Distance(p.Checkpoints[^1],p.Finish)<.0001f,
            "LAST_CHECKPOINT_FINISH_COLOCATION_NOT_CAPTURED");
        for(int i=0;i<p.Checkpoints.Count;i++)
        {
            Assert(MathF.Abs(p.Checkpoints[i].z-(13f+i*16f))<.001f,
                "ORDERED_CHECKPOINT_Z");
            if(i>0)Assert(p.Checkpoints[i].z>p.Checkpoints[i-1].z,"NOT_MONOTONE");
        }
        Assert(p.GuaranteedClearanceWidth>=rig.Width+.5f,"MEASURED_CLEARANCE");
        Assert(p.Elements.Any(e=>e.Kind==MapElementKind.Floor),"NO_REAL_FLOOR_GEOMETRY");
        Assert(p.Elements.Any(e=>e.Kind==MapElementKind.Ramp),"NO_OPTIONAL_PHYSICAL_RAMP");
        Assert(p.GeometryHash!=null&&p.GeometryHash.Length==64,"HASH_MISSING");
        MapBlueprint rerun=MapGenPlanner.Plan(MapRecipe.ObstacleCourse,seed,cfg,rig);
        Assert(rerun.GeometryHash==p.GeometryHash,"NONDETERMINISTIC_GEOMETRY");
        Assert(rerun.Checkpoints.Count==p.Checkpoints.Count,"CHECKPOINT_DRIFT");
        for(int i=0;i<p.Checkpoints.Count;i++)
            Assert(Vector3.Distance(p.Checkpoints[i],rerun.Checkpoints[i])<.00001f,
                "POINT_DRIFT");
        Rows.Add(new {
            kind="COURSE_BASELINE",status="SOURCE_SURROGATE_PASS",seed,segments,
            rig=heavy?"HEAVY_MEASURED":"LIGHT_MEASURED",
            widthMeters=rig.Width,lengthMeters=rig.Length,turnRadiusMeters=rig.MinTurnRadius,
            minSideMarginMeters=.25f, laneMeters=cfg.LaneWidth,
            recipe="ObstacleCourse",generationVersion=p.GenerationVersion,
            geometryHash=p.GeometryHash,routeCode=route.Code,
            lastCheckpointCoincidesWithFinish=true,
            receiverMustDisambiguateFinishTrigger=true,
            validatorExploredCells=route.ExploredCells,
            start=Point(p.Start),finish=Point(p.Finish),
            checkpoints=p.Checkpoints.Select(Point).ToArray(),
            actualUnityPhysX="NOT_RUN",actualRigidbodyTraversal="NOT_RUN"
        });
    }

    static void NarrowReject(int seed,bool heavy)
    {
        var rig=Vehicle(heavy);var cfg=Config(3,10f);
        bool rejected=false;
        try{MapGenPlanner.Plan(MapRecipe.ObstacleCourse,seed,cfg,rig);}
        catch(ArgumentException ex){
            Assert(ex.Message.Contains("IMPOSSIBLE_COURSE_PROFILE"),"WRONG_REJECT_REASON "+ex.Message);
            rejected=true;
        }
        Assert(rejected,"IMPOSSIBLE_10M_ACCEPTED");
        Rows.Add(new{kind="COURSE_NARROW_REJECT",status="SOURCE_SURROGATE_PASS",
            seed,segments=3,rig=heavy?"HEAVY_MEASURED":"LIGHT_MEASURED",
            widthMeters=rig.Width,laneMeters=10f,
            rejectedReason="IMPOSSIBLE_COURSE_PROFILE",actualUnityPhysX="NOT_RUN"});
    }

    static void InjectBlockingGeometry()
    {
        var rig=Vehicle(false);
        var p=MapGenPlanner.Plan(MapRecipe.ObstacleCourse,7,Config(4),rig);
        var old=p.GeometryHash;
        p.Elements.Add(new MapElement(MapElementKind.Obstacle,
            new Vector3(p.Checkpoints[0].x,1f,p.Checkpoints[0].z),
            new Vector3(16f,2f,2f),Quaternion.identity));
        Assert(MapGenPlanner.HashLayout(p)!=old,"BLOCKED_GEOMETRY_TEST_NOT_DIFFERENT");
        var report=MapGenRouteValidator.Validate(p,rig);
        Assert(!report.Passed && report.Code=="SPINE_FOOTPRINT_BLOCKED",
            "INJECTED_PHYSICAL_ROUTE_OBSTACLE_NOT_REJECTED "+report.Code);
        Rows.Add(new{kind="INJECTED_ROUTE_BLOCK",status="SOURCE_SURROGATE_PASS",
            seed=7,oldGeometryHash=old,newGeometryHash=MapGenPlanner.HashLayout(p),
            rejectedReason=report.Code,actualUnityPhysX="NOT_RUN"});
    }

    static void RejectOutOfOrderCheckpoint()
    {
        var rig=Vehicle(false);
        var p=MapGenPlanner.Plan(MapRecipe.ObstacleCourse,31,Config(4),rig);
        // CHL must validate ordered checkpoint IDs, even when navigation geometry
        // otherwise exists: these are SOURCE fixture assertions, NOT CHL controller QA.
        int next=0;
        int[] attempted={0,2,1,3};
        bool wouldSkip=false;
        foreach(int index in attempted)
        {
            if(index!=next){wouldSkip=true;break;}
            next++;
        }
        Assert(wouldSkip && next==1,"OUT_OF_ORDER_TEST_PRECONDITION");
        Assert(p.Checkpoints.Count==4,"CHECKPOINT_FIXTURE_MISMATCH");
        Rows.Add(new{kind="CHECKPOINT_ORDER_NEGATIVE",status="FIXTURE_PRECONDITION_ONLY",
            seed=31,attemptedIndexOrder=attempted,expectedAcceptedPrefix=1,
            actualCHLController="NOT_RUN"});
    }

    static int Main(string[] args)
    {
        if(args.Length!=1){Console.Error.WriteLine("usage: dotnet run -- <receipt.json>");return 3;}
        foreach(int seed in new[]{7,31,83})
            foreach(int count in new[]{3,4,5})
                foreach(bool heavy in new[]{false,true})
                {
                    int s=seed,n=count;bool h=heavy;
                    Record("COURSE_"+s+"_"+n+"SEG_"+(h?"HEAVY":"LIGHT"),()=>Positive(s,n,h));
                }
        foreach(int seed in new[]{2,12,14,22,28})
            foreach(bool heavy in new[]{false,true})
            {int s=seed;bool h=heavy;Record("NARROW10_"+s+"_"+(h?"HEAVY":"LIGHT"),()=>NarrowReject(s,h));}
        Record("INJECTED_OBSTACLE_BLOCKS_PROTECTED_ROUTE",InjectBlockingGeometry);
        Record("OUT_OF_ORDER_CHECKPOINT_FIXTURE",RejectOutOfOrderCheckpoint);
        var report=new {
            packageId="CHL-003 R1 / B01-21 compatibility QA fixture only",
            owner="B01-21",producerPinnedSha=ProducerSha,
            authority="ACTUAL_FROZEN_MAPGEN_PLANNER_VALIDATOR_WITH_UNITY_MATH_SURROGATE",
            doesNotExercise="CHL_CONTROLLER_OR_PHYSX_OR_RIGIDBODY",
            nativeUnity="NOT_RUN",realVehicleDrive="NOT_RUN",passed,failed,
            total=passed+failed,sourceCases=Rows
        };
        string path=Path.GetFullPath(args[0]);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path,JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine($"CHL003_WORLD_SOURCE_RESULT total={passed+failed} pass={passed} fail={failed} nativeUnity=NOT_RUN");
        return failed==0?0:2;
    }
}
