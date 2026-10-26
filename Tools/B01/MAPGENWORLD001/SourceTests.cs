// Real MapWorldPolicy + real immutable MAPGEN producer C# executed with synthetic Unity math.
 // Does not compile the Unity MonoBehaviour adapter or exercise PhysX or URP.
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using B01.MAPGEN001;
using B01.MAPGENWORLD001;

internal static class SourceTests
{
    private sealed class Case
    {
        public string id { get; set; }
        public string status { get; set; }
        public string expected { get; set; }
        public string actual { get; set; }
        public string geometryHash { get; set; }
    }
    private static readonly List<Case> Cases = new List<Case>();
    private static MapWorldRequest Make(MapRecipe recipe,int seed,bool heavy)
    {
        var p = heavy ? MapVehicleProfile.Heavy : MapVehicleProfile.Light;
        p.Width = heavy ? 5.180f : 2.980f;
        return new MapWorldRequest { Recipe=recipe,Seed=seed,Config=MapGenConfig.Default,
            Vehicle=p,EnvelopeMeasured=true,ClearancePerSideMeters=.25f };
    }
    private static void Test(string id,MapWorldRequest req,bool expected,
                             MapWorldFailure expectedReason=MapWorldFailure.None)
    {
        bool result=false;MapWorldFailure reason=MapWorldFailure.None;string hash="";
        try
        {
            result=MapWorldPolicy.TryPlan(req,out var plan,out reason);
            hash=plan?.GeometryHash??"";
        }
        catch(Exception ex)
        {
            Cases.Add(new Case{id=id,status="FAIL",expected=expected.ToString(),actual=ex.ToString()});return;
        }
        bool ok=result==expected && reason==(expected?MapWorldFailure.None:expectedReason)
                && (!result || hash.Length==64);
        Cases.Add(new Case{id=id,status=ok?"PASS":"FAIL",
            expected=(expected?"ACCEPT":"REJECT:"+expectedReason),
            actual=(result?"ACCEPT":"REJECT:"+reason),
            geometryHash=hash});
    }
    private static int Main(string[] args)
    {
        foreach(var recipe in new[]{MapRecipe.CombatArena,MapRecipe.ObstacleCourse})
            foreach(int seed in new[]{7,31,83})
                foreach(bool heavy in new[]{false,true})
                    Test("CORE/"+recipe+"/"+seed+"/"+(heavy?"Heavy":"Light"),Make(recipe,seed,heavy),true);
        foreach(int seed in new[]{2,7,31})
            Test("HEAVY_NORMAL/"+seed,Make(MapRecipe.ObstacleCourse,seed,true),true);
        foreach(int seed in new[]{0,2,7,19})
        {
            var r=Make(MapRecipe.ObstacleCourse,seed,true);
            r.Config = new MapGenConfig {Segments=4,ObstacleDensity=100,Difficulty=1f,LaneWidth=16f};
            Test("HEAVY_HARD/"+seed,r,true);
        }
        foreach(int seed in new[]{2,12,14,22,28})
        {
            var r=Make(MapRecipe.ObstacleCourse,seed,false);
            r.Config=new MapGenConfig {Segments=3,ObstacleDensity=100,Difficulty=1f,LaneWidth=10f};
            Test("LIGHT_REAL_10M_REJECT/"+seed,r,false,MapWorldFailure.RejectedGeneratorConfig);
        }
        Test("NULL_REQUEST",null,false,MapWorldFailure.MissingRequest);
        var u=Make(MapRecipe.CombatArena,7,false);u.EnvelopeMeasured=false;
        Test("UNKNOWN_ENVELOPE",u,false,MapWorldFailure.UnmeasuredEnvelope);
        var c=Make(MapRecipe.CombatArena,7,false);c.ClearancePerSideMeters=.24f;
        Test("LOW_CLEARANCE",c,false,MapWorldFailure.InvalidClearance);
        var n=Make(MapRecipe.CombatArena,7,false);n.Vehicle.Width=float.NaN;
        Test("INVALID_WIDTH_NAN",n,false,MapWorldFailure.RejectedGeneratorConfig);
        var a=Make(MapRecipe.CombatArena,7,false);a.Recipe=(MapRecipe)999;
        Test("INVALID_RECIPE",a,false,MapWorldFailure.RejectedGeneratorConfig);
        var m=Make(MapRecipe.ObstacleCourse,7,true);
        m.Vehicle.Width=5.90f;m.ClearancePerSideMeters=.50f;
        Test("INSUFFICIENT_TRUE_MARGIN",m,false,MapWorldFailure.InsufficientMainRouteClearance);
        var x=Make(MapRecipe.CombatArena,7,false);
        MapWorldPolicy.TryPlan(x,out var left,out _);
        MapWorldPolicy.TryPlan(x,out var right,out _);
        Cases.Add(new Case{id="SAME_SEED_REPEAT_HASH",
            status=left.GeometryHash==right.GeometryHash?"PASS":"FAIL",
            expected="SAME_HASH",actual=left.GeometryHash==right.GeometryHash?"SAME_HASH":"HASH_DRIFT",
            geometryHash=left.GeometryHash});
        var report=new {source="REAL_CSHARP_MAPWORLD_POLICY_PLUS_MAPGEN_WITH_UNITY_SURROGATE",
            nativeUnity="NOT_RUN",physx="NOT_RUN",cases=Cases.Count,
            passed=Cases.Count(t=>t.status=="PASS"),failed=Cases.Count(t=>t.status=="FAIL"),results=Cases};
        var path=args.Length>0?args[0]:"mapworld-source-results.json";
        var dir=Path.GetDirectoryName(Path.GetFullPath(path));Directory.CreateDirectory(dir);
        File.WriteAllText(path,JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));
        foreach(var item in Cases) Console.WriteLine("MAPWORLD_CASE "+item.id+" "+item.status+" "+item.actual);
        Console.WriteLine("MAPWORLD_SOURCE_RESULT total="+Cases.Count+" pass="+report.passed+" fail="+report.failed+" UNITY_NATIVE=NOT_RUN");
        return report.failed==0?0:2;
    }
}
