// B01-60 P17 W03 — independently run unchanged ORIGINAL R5Arena.Build C#,
// capture the REAL Box primitive positions/sizes and test B01-24 spawn data.
// Unity API shims are explicit; this is NOT actual native Unity/PhysX testing.
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using RoboRougelike.R5;
using UnityEngine;

public static class Program {
    record BoxData(string name,double x,double z,double width,double depth);
    static readonly Dictionary<int,List<BoxData>> Original=new();
    static readonly Dictionary<int,int> ExpectedBoxCount=new(){{0,10},{1,6},{2,10}};
    static int positiveLayouts,positivePositions;
    static int negativePass;
    static readonly List<string> failures=new();
    static readonly List<string> negativeResults=new();
    static void Fail(string id)=>throw new InvalidOperationException(id);
    static void Need(bool predicate,string id){if(!predicate)Fail(id);}
    static double Num(JsonNode node) {
        if(node==null)Fail("NULL_NUMBER");
        double value;
        try{value=node.GetValue<double>();}
        catch { Fail("NONNUMERIC");return 0;}
        Need(double.IsFinite(value),"NONFINITE");
        return value;
    }
    static string String(JsonNode node)=>node?.GetValue<string>() ?? "";
    static double Length(double x,double z)=>Math.Sqrt(x*x+z*z);
    static (double x,double z) Coord(JsonNode node) {
        Need(node is JsonArray arr && arr.Count==2,"BAD_POSITION");
        return (Num(node[0]),Num(node[1]));
    }
    static void CaptureOriginalCSharpGeometry() {
        foreach(int kind in new[]{0,1,2}) {
            var arena=new R5Arena();
            arena.Initialize(new Material());
            arena.Build((R5ArenaKind)kind); // Calls real ORIGINAL R5Arena.Build()
            Need(arena.PlayerSpawn.x==0 && arena.PlayerSpawn.z==-22,"PLAYER_SPAWN_CONTRACT_CHANGED");
            var primitives=GameObject.All
                .Where(go=>go.transform.parent==arena.Layout && go.GetComponent<Renderer>()!=null)
                .Select(go=>new BoxData(go.name,go.transform.localPosition.x,go.transform.localPosition.z,
                                      go.transform.localScale.x,go.transform.localScale.z))
                .ToList();
            Need(primitives.Count==ExpectedBoxCount[kind],"ORIGINAL_GEOMETRY_CHANGED");
            Need(primitives.Count(b=>b.name is "Północ" or "Południe" or "Zachód" or "Wschód")==4,
                 "WORLD_WALL_COUNT_CHANGED");
            Original[kind]=primitives;
        }
    }
    static bool CircleHitsBox(double x,double z,double radius,BoxData b) {
        var dx=Math.Max(Math.Abs(x-b.x)-b.width/2,0);
        var dz=Math.Max(Math.Abs(z-b.z)-b.depth/2,0);
        return dx*dx+dz*dz<=radius*radius;
    }
    static void Verify(JsonNode json) {
        Need(String(json?["package_id"])=="B01-24-ENEMY-FORMATIONS-002" &&
             String(json?["version"])=="1.0.0","SCHEMA");
        double radius=Num(json["conservative_vehicle_radius_m"]);
        double spacing=Num(json["min_separation_m"]);
        double player=Num(json["min_player_separation_m"]);
        Need(radius>=2.5 && spacing>=7.5 && player>=27,"MARGIN_POLICY");
        Need(json["formations"] is JsonArray arr && arr.Count==3,"FORMATION_COUNT");
        var ids=new HashSet<string>();
        var arenas=new HashSet<int>();
        int layouts=0,points=0;
        foreach(var formation in json["formations"].AsArray()) {
            var id=String(formation?["id"]);
            Need(id.StartsWith("B0124-") && ids.Add(id),"DUPLICATE_ID");
            int kind=(int)Num(formation["arena"]);
            Need(Original.ContainsKey(kind) && arenas.Add(kind),"ARENA_KIND");
            var options=formation["options"]?.AsObject();
            Need(options!=null && options.Count==4,"OPTION_COUNT");
            foreach(int count in new[]{1,2,3,4}) {
                var positions=options[count.ToString()]?.AsArray();
                Need(positions!=null && positions.Count==count,"WRONG_SPAWN_COUNT");
                var prior=new List<(double x,double z)>();
                foreach(var raw in positions) {
                    var p=Coord(raw);
                    Need(p.x>-41+radius && p.x<41-radius &&
                         p.z>-41+radius && p.z<41-radius,"WORLD_BORDER");
                    Need(Length(p.x,p.z+22)>=player,"PLAYER_NEAR");
                    foreach(var primitive in Original[kind])
                        Need(!CircleHitsBox(p.x,p.z,radius,primitive),
                             "WORLD_STATIC_COLLISION");
                    foreach(var prev in prior)
                        Need(Length(p.x-prev.x,p.z-prev.z)>=spacing,"ENEMY_SPACING");
                    prior.Add(p);
                    points++;
                }
                layouts++;
            }
        }
        Need(layouts==12 && points==30,"LAYOUT_OR_POINT_BUDGET");
    }
    static void Negative(JsonNode original,string name,string required,Action<JsonNode> edit) {
        var copy=original.DeepClone();
        edit(copy);
        try {
            Verify(copy);
            Fail("MUTATION_NOT_REJECTED_"+name);
        } catch(InvalidOperationException e) {
            if(e.Message!=required) {
                failures.Add(name+" expected="+required+" observed="+e.Message);
                return;
            }
            negativePass++;
            negativeResults.Add(name+":"+required);
        }
    }
    public static int Main(string[] args) {
        if(args.Length!=1){Console.Error.WriteLine("Usage: dotnet run -- path/to/original/formations.json");return 3;}
        try {
            CaptureOriginalCSharpGeometry();
            var original=JsonNode.Parse(File.ReadAllText(args[0]));
            Verify(original);
            positiveLayouts=12;positivePositions=30;
            Negative(original,"cover-actual-primitive","WORLD_STATIC_COLLISION",j=>{
                j["formations"][0]["options"]["1"][0][0]=21;
                j["formations"][0]["options"]["1"][0][1]=13;
            });
            Negative(original,"player-proximity","PLAYER_NEAR",j=>{
                j["formations"][0]["options"]["1"][0][0]=0;
                j["formations"][0]["options"]["1"][0][1]=-22;
            });
            Negative(original,"enemy-duplicate","ENEMY_SPACING",j=>{
                var first=j["formations"][0]["options"]["2"][0].DeepClone();
                j["formations"][0]["options"]["2"][1]=first;
            });
            Negative(original,"wrong-count","WRONG_SPAWN_COUNT",j=>
                j["formations"][0]["options"]["4"].AsArray().RemoveAt(3));
            Negative(original,"wrong-kind","ARENA_KIND",j=>j["formations"][1]["arena"]=17);
            Negative(original,"outside-walls","WORLD_BORDER",j=>
                j["formations"][2]["options"]["1"][0][0]=200);
            Negative(original,"weakened-policy","MARGIN_POLICY",j=>
                j["min_player_separation_m"]=0);
            Negative(original,"duplicate-id","DUPLICATE_ID",j=>
                j["formations"][1]["id"]=String(j["formations"][0]["id"]));
            Need(negativePass==8 && failures.Count==0,"NEGATIVE_CONTROLS_FAILED");
            var output=new {
                package="B01-60-ENEMY-FORMATIONS-SOURCE-QA-001",
                originalArenaBuild="EXECUTED_UNMODIFIED_ORIGINAL_CSHARP_WITH_DISPOSABLE_UNITY_SHIMS",
                testedSource="B01-24-FORMATIONS-002",
                codexGameplaySha="203a0805251ed28293888e43f2d49f0df63767c6",
                originalArenaStaticBoxes=Original.ToDictionary(k=>k.Key.ToString(),v=>v.Value.Count),
                actualOriginalCSharpCalls=3,
                layouts=positiveLayouts,
                spawnPoints=positivePositions,
                negativeCount=negativePass,
                negativeCases=negativeResults,
                status="SOURCE_QA_PASS",
                nativeUnity="NOT_RUN",
                nativePhysX="NOT_RUN",
                actualPrefabDimensions="UNKNOWN",
                visualHumanPlaytest="NOT_RUN",
                codexIntegration="NOT_DONE",
                g2="HOLD"
            };
            Console.WriteLine(JsonSerializer.Serialize(output,new JsonSerializerOptions{WriteIndented=true}));
            return 0;
        } catch(Exception e) {
            Console.Error.WriteLine("SOURCE_QA_FAIL: "+e);
            foreach(var item in failures)Console.Error.WriteLine("NEGATIVE_FAIL: "+item);
            return 1;
        }
    }
}