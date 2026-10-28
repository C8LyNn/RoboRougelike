// B01-21 PASS16: executes exact original MAPGEN001 planner/route validator +
// MAPGENWORLD001 policy/replay; output three immutable geometry artifacts.
// Unity vector/math is explicitly shimmed, not native Unity/PhysX/URP.
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;
using System.Linq;
using System.Collections.Generic;
using B01.MAPGEN001;
using B01.MAPGENWORLD001;
using UnityEngine;

internal static class ArenaBuild
{
    const string Input="Research/B01-21/P16/arena_requests.json";
    const string Dir="Research/B01-21/P16/generated";
    const string GameSha="589b60a912096d1e9917cb0bbac73a53e7543b4f";
    static readonly CultureInfo C=CultureInfo.InvariantCulture;
    static int pass,fail;
    static readonly JsonSerializerOptions Opt=new(){WriteIndented=true};
    static void Require(bool b,string why){if(!b)throw new InvalidOperationException(why);}
    static void Test(string label,Action a)
    {
        try{a();pass++;Console.WriteLine("MAPGEN_P16_PASS | "+label);}
        catch(Exception e){fail++;Console.Error.WriteLine("MAPGEN_P16_FAIL | "+label+" | "+e);}
    }
    static float F(JsonElement x,string key)=>x.GetProperty(key).GetSingle();
    static int I(JsonElement x,string key)=>x.GetProperty(key).GetInt32();
    static string S(JsonElement x,string key)=>x.GetProperty(key).GetString();
    static object P(Vector3 p)=>new{ x=p.x,y=p.y,z=p.z };
    static object E(MapElement p)=>new {kind=p.Kind.ToString(),position=P(p.Position),size=P(p.Size),pitchDegrees=p.Rotation.eulerAngles.x};
    static MapVehicleProfile Profile(JsonElement x)=>new()
    {
        Width=F(x,"width"),Length=F(x,"length"),MinTurnRadius=F(x,"minTurnRadius"),
        MaxSlopeDegrees=F(x,"maxSlopeDegrees")
    };
    static MapGenConfig Config(JsonElement x)=>new()
    {
        Segments=I(x,"segments"),ObstacleDensity=I(x,"obstacleDensity"),
        Difficulty=F(x,"difficulty"),LaneWidth=F(x,"laneWidth")
    };
    static MapWorldRequest Request(MapRecipe recipe,int seed,MapGenConfig cfg,MapVehicleProfile p,float clearance)=>
        new(){Recipe=recipe,Seed=seed,Config=cfg,Vehicle=p,EnvelopeMeasured=true,ClearancePerSideMeters=clearance};
    static string Number(float x)=>x.ToString("0.###",C);
    static string Xml(string s)=>s.Replace("&","&amp;").Replace("<","&lt;").Replace(">","&gt;").Replace("\"","&quot;");
    static string Svg(string id,MapBlueprint plan,string profileCode,int covers)
    {
        var sb=new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        sb.AppendLine("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 1020 720\" role=\"img\" aria-labelledby=\"title description\">");
        sb.AppendLine($"<title id=\"title\">{Xml(id)} — MAPGEN001 actual source blueprint</title>");
        sb.AppendLine("<desc id=\"description\">Generated from original MAPGEN001 deterministic CombatArena recipe. Top-down 64 by 64 metre arena, four boundaries, three enemy spawn descriptors, protected central 12 metre corridor. Source QA only, real vehicle physics not measured.</desc>");
        sb.AppendLine("<rect width=\"1020\" height=\"720\" fill=\"#141e2a\"/>");
        sb.AppendLine($"<text x=\"55\" y=\"54\" font-family=\"sans-serif\" font-size=\"26\" fill=\"#f4f7f9\" font-weight=\"bold\">{Xml(id)}</text>");
        sb.AppendLine($"<text x=\"55\" y=\"78\" font-family=\"sans-serif\" font-size=\"14\" fill=\"#b3c2cd\">Actual original MapGenPlanner.Plan result — seed {plan.Seed}, profile: historical reference only</text>");
        sb.AppendLine("<g transform=\"translate(78 111)\" id=\"actual-source-geometry\">");
        // XZ top-down: world -32..+32 displayed over 512 px, world Z increases upwards.
        sb.AppendLine("<rect x=\"0\" y=\"0\" width=\"512\" height=\"512\" fill=\"#394b54\"/>");
        sb.AppendLine("<g stroke=\"#5d717a\" stroke-width=\".6\" opacity=\".55\">");
        for(int i=0;i<=8;i++){int p=i*64;sb.AppendLine($"<line x1=\"{p}\" x2=\"{p}\" y1=\"0\" y2=\"512\"/><line x1=\"0\" x2=\"512\" y1=\"{p}\" y2=\"{p}\"/>");}
        sb.AppendLine("</g>");
        sb.AppendLine("<rect x=\"208\" y=\"0\" width=\"96\" height=\"512\" fill=\"#327c74\" opacity=\".20\"/>");
        sb.AppendLine("<g id=\"solid-world-elements\">");
        foreach(var e in plan.Elements)
        {
            if(e.Kind==MapElementKind.Floor)continue;
            float w=e.Size.x*8,z=e.Size.z*8;
            float x=(e.Position.x+32)*8-w/2,y=(32-e.Position.z)*8-z/2;
            string color=e.Kind==MapElementKind.Boundary?"#c79f7a":
                e.Kind==MapElementKind.Cover?"#b96954":"#9f7baf";
            sb.AppendLine($"<rect data-kind=\"{e.Kind}\" x=\"{Number(x)}\" y=\"{Number(y)}\" width=\"{Number(w)}\" height=\"{Number(z)}\" fill=\"{color}\" opacity=\".93\"/>");
        }
        sb.AppendLine("</g>");
        sb.AppendLine("<path d=\"M 256 456 L 256 56\" stroke=\"#a0f6da\" stroke-width=\"3\" stroke-dasharray=\"12 8\" fill=\"none\" opacity=\".85\"/>");
        void Mark(Vector3 v,float radius,string color,string kind)
        {
            sb.AppendLine($"<circle data-kind=\"{kind}\" cx=\"{Number((v.x+32)*8)}\" cy=\"{Number((32-v.z)*8)}\" r=\"{Number(radius)}\" fill=\"{color}\" stroke=\"#ffffff\" stroke-width=\"2\"/>");
        }
        Mark(plan.Start,8,"#3fe5b9","player-start");
        Mark(plan.Finish,8,"#ffdb6b","finish");
        foreach(var spawn in plan.EnemySpawns)Mark(spawn,5,"#f0789b","enemy-spawn-descriptor");
        sb.AppendLine("</g>");
        sb.AppendLine("<g font-family=\"sans-serif\" font-size=\"18\" fill=\"#eff4f7\">");
        sb.AppendLine($"<text x=\"640\" y=\"148\" font-weight=\"bold\">Seed {plan.Seed} / CombatArena</text>");
        sb.AppendLine($"<text x=\"640\" y=\"185\">Cover elements: {covers}</text>");
        sb.AppendLine($"<text x=\"640\" y=\"218\">Total source elements: {plan.Elements.Count}</text>");
        sb.AppendLine("<text x=\"640\" y=\"250\">Arena: 64 × 64 metres</text>");
        sb.AppendLine("<text x=\"640\" y=\"282\">Centre clearance: 12 m</text>");
        sb.AppendLine("<text x=\"640\" y=\"314\">Enemy spawn descriptors: 3</text>");
        sb.AppendLine("<text x=\"640\" y=\"347\">Floor + 4 solid boundaries</text>");
        sb.AppendLine("<text x=\"640\" y=\"392\" font-size=\"14\" fill=\"#a6c8c4\">Source route: "+Xml(profileCode)+"</text>");
        sb.AppendLine("<text x=\"640\" y=\"440\" font-size=\"14\" fill=\"#98a9b6\">No enemies spawned by this data</text>");
        sb.AppendLine("<text x=\"640\" y=\"464\" font-size=\"14\" fill=\"#98a9b6\">No real Light / Heavy PhysX run</text>");
        sb.AppendLine("<text x=\"640\" y=\"488\" font-size=\"14\" fill=\"#98a9b6\">No canonical integration or G1</text>");
        sb.AppendLine("</g>");
        sb.AppendLine("<text x=\"55\" y=\"668\" fill=\"#becbd5\" font-family=\"sans-serif\" font-size=\"12\">B01-21-P16 • immutable original-source output • generated by C# executable with labeled Unity math shim</text>");
        sb.AppendLine("</svg>");
        return sb.ToString();
    }
    static void Emit(string name,string content)
    {
        string path=Dir+"/"+name;
        if(File.Exists(path))
        {
            Require(File.ReadAllText(path)==content,"committed generated file not reproducible: "+path);
            Console.WriteLine("MAPGEN_P16_REGENERATED_BYTE_IDENTICAL | "+path);
        }
        else
        {
            Directory.CreateDirectory(Dir);
            File.WriteAllText(path,content);
            Console.WriteLine("MAPGEN_P16_GENERATED_NEW | "+path);
        }
        Console.WriteLine("MAPGEN_P16_FILE_B64 | "+name+" | "+Convert.ToBase64String(Encoding.UTF8.GetBytes(content)));
    }
    static int Main()
    {
        try
        {
            using var json=JsonDocument.Parse(File.ReadAllText(Input));
            var root=json.RootElement;
            Require(S(root,"schema")=="B01-21-P16-MAPGEN-REQUESTS-v1","wrong schema");
            Require(S(root,"inputGameCommit")==GameSha,"input game SHA drift; rebase on newest verified original source");
            var refs=root.GetProperty("historicalReferenceProfiles");
            var light=Profile(refs.GetProperty("light"));
            var heavy=Profile(refs.GetProperty("heavy"));
            float clearance=F(root,"clearancePerSideMeters");
            var all=root.GetProperty("arenas").EnumerateArray().ToArray();
            Require(all.Length==3,"must supply exactly 3 genuine world requests");
            var hashes=new HashSet<string>();
            var ids=new HashSet<string>();
            var manifests=new List<object>();
            foreach(var a in all)
            {
                string id=S(a,"id"),rec=S(a,"recipe");
                int seed=I(a,"seed");
                MapGenConfig cfg=Config(a.GetProperty("config"));
                Require(ids.Add(id)&&rec=="CombatArena","unknown or duplicate arena id");
                MapBlueprint plan=null;
                string lightReceipt="",heavyReceipt="";
                MapGenRouteValidator.Report lightRoute=default,heavyRoute=default;
                Test(id+"/ACTUAL_SOURCE_LIGHT_HEAVY_PLAN_REPLAY",() =>
                {
                    MapWorldRequest l=Request(MapRecipe.CombatArena,seed,cfg,light,clearance);
                    MapWorldRequest h=Request(MapRecipe.CombatArena,seed,cfg,heavy,clearance);
                    Require(MapWorldPolicy.TryPlan(l,out var p1,out var lr),"Light original MAPWORLD policy reject: "+lr);
                    Require(MapWorldPolicy.TryPlan(h,out var p2,out var hr),"Heavy original MAPWORLD policy reject: "+hr);
                    Require(p1.GeometryHash==p2.GeometryHash&&p1.Elements.Count==p2.Elements.Count,
                        "Original MAPGEN profile produced different topology");
                    var replay=MapGenPlanner.Plan(MapRecipe.CombatArena,seed,cfg,heavy);
                    Require(replay.GeometryHash==p1.GeometryHash,"same seed/config original source geometry hash drift");
                    Require(hashes.Add(p1.GeometryHash),"two distinct authored arena requests alias one layout hash");
                    lightRoute=MapGenRouteValidator.Validate(p1,light);
                    heavyRoute=MapGenRouteValidator.Validate(p2,heavy);
                    Require(lightRoute.Passed&&heavyRoute.Passed,"actual producer route validator failed Light or Heavy");
                    Require(p1.GuaranteedClearanceWidth>=heavy.Width+2*clearance,"original certified main avenue narrow");
                    Require(p1.Elements.Count(x=>x.Kind==MapElementKind.Floor)==1 &&
                            p1.Elements.Count(x=>x.Kind==MapElementKind.Boundary)==4 &&
                            p1.EnemySpawns.Count==3 && p1.Checkpoints.Count==0,"original source arena geometry incorrect");
                    Require(p1.Start.z==-25f&&p1.Finish.z==25f,"original endpoints drift");
                    lightReceipt=MapWorldReplayCodec.Encode(l,p1);
                    heavyReceipt=MapWorldReplayCodec.Encode(h,p2);
                    Require(MapWorldReplayCodec.TryDecode(lightReceipt,out var l2,out var restored,out var lf) &&
                            lf==MapWorldFailure.None && restored.GeometryHash==p1.GeometryHash &&
                            l2.Seed==seed,"Light immutable original MAPGEN replay mismatch");
                    Require(MapWorldReplayCodec.TryDecode(heavyReceipt,out var h2,out var rp,out var hf) &&
                            hf==MapWorldFailure.None && rp.GeometryHash==p1.GeometryHash &&
                            h2.Seed==seed,"Heavy immutable original MAPGEN replay mismatch");
                    string bad=heavyReceipt.Substring(0,heavyReceipt.Length-1)+(heavyReceipt.EndsWith("0")?"1":"0");
                    Require(!MapWorldReplayCodec.TryDecode(bad,out _,out _,out var reason) &&
                            reason==MapWorldFailure.InvalidReplayReceipt,
                            "checksum tamper accepted");
                    var unmeasured=Request(MapRecipe.CombatArena,seed,cfg,heavy,clearance);
                    unmeasured.EnvelopeMeasured=false;
                    Require(!MapWorldPolicy.TryPlan(unmeasured,out _,out var reason2) &&
                            reason2==MapWorldFailure.UnmeasuredEnvelope,"unmeasured rig accepted");
                    plan=p1;
                });
                if(plan==null)continue;
                int covers=plan.Elements.Count(x=>x.Kind==MapElementKind.Cover);
                var generated=new
                {
                    schema="B01-21-P16-ACTUAL-MAPGEN001-BLUEPRINT-v1",
                    id,recipe=rec,seed,
                    originalGameSha=GameSha,
                    generationVersion=plan.GenerationVersion,
                    config=new{segments=cfg.Segments,obstacleDensity=cfg.ObstacleDensity,
                        difficulty=cfg.Difficulty,laneWidth=cfg.LaneWidth},
                    geometryHash=plan.GeometryHash,sourceDiagnostics=plan.Diagnostics,
                    guaranteedClearanceWidthMeters=plan.GuaranteedClearanceWidth,
                    sourceBounds=new{center=P(plan.Bounds.center),size=P(plan.Bounds.size)},
                    start=P(plan.Start),finish=P(plan.Finish),
                    enemySpawnDescriptors=plan.EnemySpawns.Select(P).ToArray(),
                    elements=plan.Elements.Select(E).ToArray(),
                    referenceProfiles=new{
                        light=new{measuredPreviously=true,width=light.Width,length=light.Length,
                            minTurnRadius=light.MinTurnRadius,maxSlopeDegrees=light.MaxSlopeDegrees,
                            clearancePerSideMeters=clearance,routePassed=lightRoute.Passed,
                            routeCode=lightRoute.Code,lightRoute.ExploredCells,replayReceipt=lightReceipt},
                        heavy=new{measuredPreviously=true,width=heavy.Width,length=heavy.Length,
                            minTurnRadius=heavy.MinTurnRadius,maxSlopeDegrees=heavy.MaxSlopeDegrees,
                            clearancePerSideMeters=clearance,routePassed=heavyRoute.Passed,
                            routeCode=heavyRoute.Code,heavyRoute.ExploredCells,replayReceipt=heavyReceipt}},
                    claims=new{originalCsharpExecutedWithUnityMathShim=true,actualUnity=false,
                        physicsCertified=false,currentAssembledRigMeasured=false,
                        enemyAiTested=false,integratedIntoGame=false}
                };
                string bp=JsonSerializer.Serialize(generated,Opt)+"\n";
                string svg=Svg(id,plan,heavyRoute.Code,covers);
                Emit(id+".blueprint.json",bp);
                Emit(id+".svg",svg);
                manifests.Add(new{id,seed,geometryHash=plan.GeometryHash,covers,
                    originalElementCount=plan.Elements.Count,
                    blueprintFile=id+".blueprint.json",previewFile=id+".svg",
                    routeLight=lightRoute.Code,routeHeavy=heavyRoute.Code});
            }
            Test("THREE_NONALIASING_ORIGINAL_SOURCE_GENERATED_ARENAS",()=>Require(hashes.Count==3&&ids.Count==3,
                "must have three different actually generated source hashes"));
            if(fail==0)
            {
                string manifest=JsonSerializer.Serialize(new{schema="B01-21-P16-MAPGEN-PACKAGE-v1",
                    inputSha=GameSha,originalSource="B01.MAPGEN001.MapGenPlanner+MapGenRouteValidator;B01.MAPGENWORLD001.MapWorldPolicy+MapWorldReplayCodec",
                    dataRequest="arena_requests.json",nativeUnity="NOT_RUN",physx="NOT_RUN",
                    actualCsharpShimPass=pass,arenas=manifests},Opt)+"\n";
                Emit("generated_manifest.json",manifest);
            }
        }
        catch(Exception e){fail++;Console.Error.WriteLine("MAPGEN_P16_FATAL | "+e);}
        Console.WriteLine($"MAPGEN_P16_RESULT PASS={pass} FAIL={fail} NATIVE_UNITY=NOT_RUN");
        return fail==0?0:2;
    }
}
