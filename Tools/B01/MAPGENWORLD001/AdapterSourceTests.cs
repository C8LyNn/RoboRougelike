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
