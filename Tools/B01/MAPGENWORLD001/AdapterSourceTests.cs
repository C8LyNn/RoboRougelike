// Actual MapGenWorldAdapter compiled and tested with FAKE MonoBehaviour/GameObject/MapGenBuilder.
 // NOT native Unity, not actual collision/URP or real producer MapGenBuilder execution.
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using B01.MAPGEN001;
using B01.MAPGENWORLD001;
using UnityEngine;

internal static class AdapterSourceTests
{
    static List<string> results=new List<string>();
    static void Check(string name,Action action)
    {
        try{action();results.Add("PASS "+name);Console.WriteLine("ADAPTER_CASE PASS "+name);}
        catch(Exception ex){results.Add("FAIL "+name+" "+ex.Message);Console.WriteLine("ADAPTER_CASE FAIL "+name+" "+ex);}
    }
    static MapWorldRequest Request(int seed=7,bool course=false)
    {
        var profile=MapVehicleProfile.Light;profile.Width=2.980f;
        return new MapWorldRequest {
            Recipe=course?MapRecipe.ObstacleCourse:MapRecipe.CombatArena,
            Seed=seed, Config=MapGenConfig.Default,Vehicle=profile,
            EnvelopeMeasured=true,ClearancePerSideMeters=.25f
        };
    }
    static (MapGenWorldAdapter,MapGenBuilder) Pair()
    {
        var go=new GameObject();
        var builder=go.AddComponent<MapGenBuilder>();
        var adapter=go.AddComponent<MapGenWorldAdapter>();
        return (adapter,builder);
    }
    static void Require(bool value,string msg){if(!value)throw new Exception(msg);}

    static string Resign(string[] fields)
    {
        string body=string.Join("|",fields,0,fields.Length-1);
        using(var h=System.Security.Cryptography.SHA256.Create())
        {
            string sig=BitConverter.ToString(h.ComputeHash(System.Text.Encoding.UTF8.GetBytes(body)))
                .Replace("-","").ToLowerInvariant();
            return body+"|"+sig;
        }
    }
    static int Main(string[] args)
    {
        Check("GENERATE_VALID_WORLD",()=>{
            var (a,b)=Pair();
            Require(a.TryGenerate(Request(),out var world,out var reason),"should generate "+reason);
            Require(world.Revision==1 && world.GeometryHash.Length==64,"snapshot metadata");
            Require(b.Current!=null && a.Current==world,"current references");
        });
        Check("INVALID_REQUEST_PRESERVES_LIVE_MAP",()=>{
            var (a,b)=Pair();int clear=0,ready=0;
            a.WorldCleared+= _ =>clear++;a.WorldReady+=_=>ready++;
            Require(a.TryGenerate(Request(),out var first,out _),"first");
            var bad=Request(31);bad.EnvelopeMeasured=false;
            Require(!a.TryRegenerate(bad,out var ignored,out var failure),"must reject");
            Require(failure==MapWorldFailure.UnmeasuredEnvelope && ignored==null,"failure reason");
            Require(a.Current==first && b.Current!=null && first.GeneratedRoot.activeInHierarchy,"preserved old");
            Require(ready==1 && clear==0,"no callbacks on reject");
        });
        Check("REGEN_CLEARS_THEN_READY_AND_INCREMENTS_REVISION",()=>{
            var (a,b)=Pair();var events=new List<string>();
            a.WorldReady+=x=>events.Add("R"+x.Revision);
            a.WorldCleared+=x=>events.Add("C"+x.Revision);
            Require(a.TryGenerate(Request(7),out var first,out _),"first");
            Require(a.TryRegenerate(Request(31),out var next,out _),"second");
            Require(!first.GeneratedRoot.activeInHierarchy && next.GeneratedRoot.activeInHierarchy,"world switch");
            Require(string.Join(",",events)=="R1,C1,R2","event order "+string.Join(",",events));
            Require(next.Revision==2 && first.GeometryHash!=next.GeometryHash,"revision hash");
        });
        Check("CLEAR_IDEMPOTENT",()=>{
            var (a,b)=Pair();int cleared=0;
            a.WorldCleared+=_=>cleared++;
            Require(a.TryGenerate(Request(),out var first,out _),"initial");
            a.ClearWorld();a.ClearWorld();
            Require(cleared==1 && a.Current==null && b.Current==null,"double clear");
        });
        Check("THROWING_ACTIVITY_CALLBACK_IS_ISOLATED",()=>{
            var (a,b)=Pair();int ready=0;
            a.WorldReady+=_=>throw new InvalidOperationException("consumer");
            a.WorldReady+=_=>ready++;
            Require(a.TryGenerate(Request(),out var world,out _),"callback exception must not undo committed map");
            Require(ready==1 && a.Current==world && Debug.Exceptions==1,"consumer isolated");
        });
        Check("DEFENSIVE_CHECKPOINT_SNAPSHOT",()=>{
            var (a,b)=Pair();Require(a.TryGenerate(Request(7,true),out var world,out _),"generate");
            var cps=world.Checkpoints;
            Require(cps.Length>0,"must contain checkpoints");
            var original=world.Checkpoints[0];
            cps[0]=new Vector3(5000,0,5000);
            Require(Vector3.Distance(world.Checkpoints[0],original)<.001f,"checkpoint array leaked mutability");
        });
        Check("SYNTHETIC_BUILDER_EXCEPTION_KEEPS_CURRENT",()=>{
            var (a,b)=Pair();Require(a.TryGenerate(Request(),out var first,out _),"first");
            b.FailNext=true;
            Require(!a.TryRegenerate(Request(31),out var nothing,out var reason),"must fail");
            Require(reason==MapWorldFailure.WorldBuildFailed && nothing==null,"failure");
            Require(a.Current==first && b.Current!=null && first.GeneratedRoot.activeInHierarchy,"no loss on producer exception");
        });
        Check("CORRUPTED_POST_BUILD_RESULT_FAILS_CLOSED_AND_CLEARS_OLD",()=>{
            var (a,b)=Pair();int cleared=0;a.WorldCleared+=_=>cleared++;
            Require(a.TryGenerate(Request(),out var old,out _),"first");
            b.CorruptNextHash=true;
            Require(!a.TryRegenerate(Request(31),out var next,out var reason),"reject unsafe result");
            Require(next==null && reason==MapWorldFailure.InvalidWorldResult,"wrong reason");
            Require(a.Current==null && b.Current==null && !old.GeneratedRoot.activeInHierarchy,"must clear stale");
            Require(cleared==1,"must notify disappeared old world");
        });
        foreach(var fault in new [] {
            FakeWorldFault.StartOutsideWorldBounds,
            FakeWorldFault.FinishOutsideWorldBounds,
            FakeWorldFault.CheckpointOutsideWorldBounds,
            FakeWorldFault.SpawnOutsideWorldBounds,
            FakeWorldFault.MissingCheckpoint,
            FakeWorldFault.WrongSeed,
            FakeWorldFault.WrongVersion,
            FakeWorldFault.NonFiniteFinish
        })
        {
            var mode=fault;
            Check("REJECT_CORRUPT_WORLD_METADATA_"+mode,()=>{
                var (a,b)=Pair();
                int clearCount=0,readyCount=0;
                a.WorldCleared+=_=>clearCount++;
                a.WorldReady+=_=>readyCount++;
                Require(a.TryGenerate(Request(7),out var first,out _),"baseline");
                b.NextFault=mode;
                bool course=(mode==FakeWorldFault.CheckpointOutsideWorldBounds ||
                             mode==FakeWorldFault.MissingCheckpoint);
                Require(!a.TryRegenerate(Request(31,course),out var failed,out var reason),
                    "must reject generated unsafe world metadata "+mode);
                Require(reason==MapWorldFailure.InvalidWorldResult && failed==null,
                    "wrong failure class for "+mode);
                Require(a.Current==null && b.Current==null && !first.GeneratedRoot.activeInHierarchy,
                    "invalid result must fail closed and clear old "+mode);
                Require(clearCount==1 && readyCount==1,
                    "activity callbacks must reflect invalid world teardown "+mode);
            });
        }

        Check("REPLAY_ROUNDTRIP_SAME_ARENA_WITH_HASH",()=>{
            var (a,b)=Pair();
            Require(a.TryGenerate(Request(7),out var first,out _),"first");
            Require(first.ReplayReceipt.StartsWith(MapWorldReplayCodec.Format+"|"),"version prefix");
            Require(a.TryReplay(first.ReplayReceipt,out var second,out var status),"replay "+status);
            Require(second.Seed==first.Seed && second.GeometryHash==first.GeometryHash,
                "mismatched replay geometry");
            Require(second.ReplayReceipt==first.ReplayReceipt,
                "replay not byte-identical");
            Require(second.Revision==2 && b.GenerateCalls==2,"revision/side effects");
        });
        Check("REPLAY_ROUNDTRIP_COURSE_HEAVY_SEED31",()=>{
            var (a,b)=Pair();
            var heavy=Request(31,true);
            heavy.Vehicle=B01.MAPGEN001.MapVehicleProfile.Heavy;
            heavy.Vehicle.Width=5.180f;
            Require(a.TryGenerate(heavy,out var first,out _),"initial heavy course");
            Require(a.TryReplay(first.ReplayReceipt,out var second,out var failure),"heavy replay "+failure);
            Require(second.GeometryHash==first.GeometryHash && second.Checkpoints.Length>0,
                "heavy checkpoints");
        });
        Check("REPLAY_CULTURE_INVARIANT_ROUNDTRIP",()=>{
            var saved=System.Globalization.CultureInfo.CurrentCulture;
            try{
                var (a,b)=Pair();
                var q=Request(83,true);q.Config.Difficulty=.727f;
                Require(a.TryGenerate(q,out var first,out _),"culture baseline");
                System.Globalization.CultureInfo.CurrentCulture=new System.Globalization.CultureInfo("pl-PL");
                Require(a.TryReplay(first.ReplayReceipt,out var next,out var failure),
                    "culture-dependent read "+failure);
                Require(next.ReplayReceipt==first.ReplayReceipt,"culture-dependent receipt");
            }
            finally {System.Globalization.CultureInfo.CurrentCulture=saved;}
        });
        foreach(var corrupt in new[]{ "NULL","EMPTY","PREFIX","CHECKSUM","TRUNCATED","OVERSIZE" })
        {
            var mode=corrupt;
            Check("REPLAY_REJECTS_CORRUPT_RECEIPT_"+mode,()=>{
                var (a,b)=Pair();
                int cleared=0,ready=0;
                a.WorldReady+=_=>ready++;a.WorldCleared+=_=>cleared++;
                Require(a.TryGenerate(Request(7),out var first,out _),"baseline");
                int count=b.GenerateCalls;
                string data=first.ReplayReceipt;
                switch(mode){
                    case "NULL":data=null;break;
                    case "EMPTY":data="";break;
                    case "PREFIX":data="UNKNOWN"+data.Substring(7);break;
                    case "CHECKSUM":data=data.Substring(0,data.Length-1)+(data.EndsWith("0")?"1":"0");break;
                    case "TRUNCATED":data=data.Substring(0,data.Length-3);break;
                    case "OVERSIZE":data=new string('X',2048);break;
                }
                Require(!a.TryReplay(data,out var second,out var failure),"should fail replay");
                Require(second==null && failure==MapWorldFailure.InvalidReplayReceipt,
                    "corrupt receipt wrong error "+failure);
                Require(a.Current==first && first.GeneratedRoot.activeInHierarchy
                    && b.GenerateCalls==count && ready==1 && cleared==0,
                    "corrupt replay changed current scene");
            });
        }
        Check("REPLAY_GENERATION_VERSION_DRIFT_NO_BUILD",()=>{
            var (a,b)=Pair();
            Require(a.TryGenerate(Request(7),out var old,out _),"baseline");
            var fields=old.ReplayReceipt.Split('|');
            fields[13]=(int.Parse(fields[13],System.Globalization.CultureInfo.InvariantCulture)+1)
                .ToString(System.Globalization.CultureInfo.InvariantCulture);
            string receipt=Resign(fields);
            int before=b.GenerateCalls;
            Require(!a.TryReplay(receipt,out var next,out var failure),"drift must reject");
            Require(failure==MapWorldFailure.ReplayGeometryMismatch &&
                next==null && a.Current==old && b.GenerateCalls==before,
                "generation version drift not detected before build");
        });
        Check("REPLAY_GEOMETRY_HASH_DRIFT_NO_BUILD",()=>{
            var (a,b)=Pair();
            Require(a.TryGenerate(Request(7),out var old,out _),"baseline");
            var fields=old.ReplayReceipt.Split('|');
            fields[12]=new string('a',64);
            if(fields[12]==old.GeometryHash)fields[12]=new string('b',64);
            var altered=Resign(fields);
            int before=b.GenerateCalls;
            Require(!a.TryReplay(altered,out var next,out var failure),"geometry mismatch");
            Require(failure==MapWorldFailure.ReplayGeometryMismatch && next==null &&
                a.Current==old && b.GenerateCalls==before && old.GeneratedRoot.activeInHierarchy,
                "geometry mismatch mutated source world");
        });
        Check("REPLAY_INVALID_MEASURED_HULL_REJECTS_PRESERVES",()=>{
            var (a,b)=Pair();
            Require(a.TryGenerate(Request(7),out var old,out _),"baseline");
            var fields=old.ReplayReceipt.Split('|');
            fields[7]="0"; // width zero; must not accept even with valid checksum.
            var altered=Resign(fields);
            int before=b.GenerateCalls;
            Require(!a.TryReplay(altered,out var next,out var failure),"zero width must reject");
            Require(failure==MapWorldFailure.RejectedGeneratorConfig && next==null &&
                a.Current==old && b.GenerateCalls==before,
                "bad measured envelope changed live geometry");
        });
        // W15: a validly re-signed save may refer to another real generated
        // layout while retaining the old geometry hash. A checksum alone is
        // insufficient. Assert drift is detected BEFORE MapGenBuilder.Generate.
        foreach(var mutation in new[]{"SEED_7_TO_31", "OBSTACLE_DENSITY_50_TO_100"})
        {
            var mode=mutation;
            Check("W15_SIGNED_REAL_LAYOUT_DRIFT_"+mode+"_NO_SCENE_MUTATION",()=>{
                var (a,b)=Pair();
                int ready=0,cleared=0;
                a.WorldReady+=_=>ready++;
                a.WorldCleared+=_=>cleared++;
                var initialRequest=Request(7,true); // Real planner: ObstacleCourse + measured Light 2.980m.
                Require(a.TryGenerate(initialRequest,out var initial,out var initFailure),
                    "W15 baseline "+initFailure);
                var saved=initial.ReplayReceipt.Split('|');
                var changedRequest=Request(7,true);
                if(mode=="SEED_7_TO_31")
                {
                    saved[2]="31";
                    changedRequest.Seed=31;
                }
                else
                {
                    saved[4]="100";
                    var cfg=changedRequest.Config;
                    cfg.ObstacleDensity=100;
                    changedRequest.Config=cfg;
                }
                var changedPlan=MapGenPlanner.Plan(changedRequest.Recipe,
                    changedRequest.Seed,changedRequest.Config,changedRequest.Vehicle);
                Require(changedPlan.GeometryHash!=initial.GeometryHash,
                    "test precondition: requested change must alter real producer layout");
                string forgedButConsistentChecksum=Resign(saved);
                Require(forgedButConsistentChecksum!=initial.ReplayReceipt,
                    "test precondition: receipt must be distinct");
                int calls=b.GenerateCalls;
                var originalRoot=initial.GeneratedRoot;
                var originalResult=b.Current;
                Require(!a.TryReplay(forgedButConsistentChecksum,out var rejected,out var failure),
                    "signed layout contradiction must be rejected");
                Require(failure==MapWorldFailure.ReplayGeometryMismatch && rejected==null,
                    "expected strict replay geometry mismatch, got "+failure);
                Require(b.GenerateCalls==calls && a.Current==initial &&
                    object.ReferenceEquals(b.Current,originalResult) &&
                    object.ReferenceEquals(initial.GeneratedRoot,originalRoot) &&
                    originalRoot.activeInHierarchy,"rejected replay modified real current world");
                Require(ready==1 && cleared==0,"rejected replay sent scene callbacks");
                Require(a.LastFailure==MapWorldFailure.ReplayGeometryMismatch,
                    "LastFailure must preserve exact replay cause");
            });
        }
        var n=results.Count(x=>x.StartsWith("FAIL"));
        string file=args.Length>0?args[0]:"mapworld-adapter-fake-results.json";
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(file)));
        File.WriteAllText(file,JsonSerializer.Serialize(new {
            tier="ACTUAL_ADAPTER_CSHARP_FAKE_MONOBEHAVIOUR_FAKE_BUILDER_NOT_NATIVE",
            total=results.Count,passed=results.Count-n,failed=n,
            nativeUnity="NOT_RUN",results
        },new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine("ADAPTER_SOURCE_RESULT total="+results.Count+" pass="+(results.Count-n)+" fail="+n+" UNITY_NATIVE=NOT_RUN");
        return n==0?0:2;
    }
}
