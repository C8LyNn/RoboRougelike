#!/usr/bin/env python3
"""PASS15 B01-22: execute ORIGINAL Codex R1 Update terminal-payout method,
ORIGINAL R1 progress, ORIGINAL R3 wallet/GUID, ORIGINAL R4 salvage ledger.

Original methods are fetched byte-exact by immutable commit/Git blob.
Only the original Update() method is lifted verbatim from its very large
Unity MonoBehaviour/partial class to the disposable .NET8 C# fixture;
ALL references outside this extracted method (Unity, environment,
scene, coroutine, controller, R5 plan) are labeled signature doubles.
R1RunProgress, R3VehicleModel, R4WeaponDefinition, R4SalvageLedger and
R1UpgradeDefinition compile as complete unmodified original source files.

This is not a native Unity test, nor an integrated end-to-end run. A true
original-code payout-before-failed-transition regression is EXPECTED RED,
not a game/security GREEN and not an actual repeatable normal-play exploit.
"""
import hashlib
import json
import re
import shutil
import subprocess
import tempfile
from pathlib import Path
from urllib.request import urlopen

COMMIT = "231005c6d0351945a8ee6684f3856af118f0e94f"
HERE = Path(__file__).resolve().parent
BASE = "https://raw.githubusercontent.com/C8LyNn/RoboRougelike/" + COMMIT + "/"
FILES = {
    "Assets/R1/R1RunController.cs":"a675950028ce01a9ef450ad57dd5b524ab838b2f",
    "Assets/R1/R1RunProgress.cs":"a91bfe0eb5d3f92dab2e8359aec4520a6d01f698",
    "Assets/R1/R1UpgradeDefinition.cs":"5c5731d5ec7571fa57b5779b2300fd250090ebf4",
    "Assets/R3/R3VehicleModel.cs":"9f1bf190c7535182c6ce375cbfe67f5b2862e811",
    "Assets/R4/R4WeaponDefinition.cs":"502f6d7f70d4e8078260de632ab181f244a4d730",
    "Assets/R4/R4SalvageLedger.cs":"9ad541bc0c18b3791fc6df6091ff4674f7f8a684",
}
PROJECT = """<Project Sdk="Microsoft.NET.Sdk">
<PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework>
<Nullable>disable</Nullable><ImplicitUsings>enable</ImplicitUsings>
<TreatWarningsAsErrors>true</TreatWarningsAsErrors>
<EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup>
<ItemGroup><Compile Include="Program.cs"/><Compile Include="OutsideUnityApi.cs"/>
<Compile Include="OriginalR1Update.cs"/>
<Compile Include="Assets/R1/R1RunProgress.cs"/>
<Compile Include="Assets/R1/R1UpgradeDefinition.cs"/>
<Compile Include="Assets/R3/R3VehicleModel.cs"/>
<Compile Include="Assets/R4/R4WeaponDefinition.cs"/>
<Compile Include="Assets/R4/R4SalvageLedger.cs"/>
</ItemGroup></Project>"""
SHIMS = """
namespace UnityEngine {
 public static class Debug {public static void Log(string msg) {} }
 public class ScriptableObject {}
 [System.AttributeUsage(System.AttributeTargets.Class)]
 public sealed class CreateAssetMenuAttribute:System.Attribute { public string menuName; }
 [System.AttributeUsage(System.AttributeTargets.Field)]
 public sealed class TextAreaAttribute:System.Attribute {}
 [System.AttributeUsage(System.AttributeTargets.Field)]
 public sealed class RangeAttribute:System.Attribute {public RangeAttribute(float a,float b){}}
}
"""
WRAPPER_START = r"""// Exact body of ORIGINAL R1RunController.Update() is inserted at marker
// without changing a single byte of that original method.
using System;
using System.Linq;
using System.Collections.Generic;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4;
using UnityEngine;
namespace RoboRougelike.MapSystemR4 { public enum MapR4Status { Completed } }
namespace RoboRougelike.R1 {
 public sealed class FakeEnemy {public bool IsDefeated;}
 public sealed class R3VehicleAssembly {public int Captures;public void Capture(){Captures++;}}
 public sealed class FakePlayer {
     public string DefeatReason;
     public readonly R3VehicleAssembly Assembly=new();
     public T GetComponent<T>() where T:class => Assembly as T;
 }
 public sealed class FakeEncounter {
     public readonly List<FakeEnemy> Enemies=new(){new FakeEnemy{IsDefeated=true}};
     public bool Cleared=true;
     public int Clears;
     public void Clear(){Clears++;}
 }
 public sealed class FakeRace {public bool Won;}
 public sealed class FakeRoad {
     public RoboRougelike.MapSystemR4.MapR4Status Status;
 }
 public sealed class FakeEncounterDef {public int ScrapReward=80;}
 public sealed class FakeRunDefinition {
     public FakeEncounterDef[] Encounters={new()};
     public R1UpgradeDefinition[] Upgrades={
         new R1UpgradeDefinition{Id="A",Title="A"},
         new R1UpgradeDefinition{Id="B",Title="B"},
         new R1UpgradeDefinition{Id="C",Title="C"}};
 }
 public sealed class FakePlan {
     public bool ThrowOnRandom;
     public Random UpgradeRandom(int index) {
         if(ThrowOnRandom)throw new InvalidOperationException("INJECTED_PLAN_RANDOM_FAILURE");
         return new Random(123+index);
     }
 }
 // Fake only the rest of the R1RunController partial class / Unity scene,
 // never any branch/algorithm inside Update() or the R3/R4/R1 original sources.
 public sealed class R1RunController {
     public bool Transitioning,Paused,QuitRequested,ReactorActive;
     public R1RunProgress Progress;
     public FakePlayer Player;
     public FakeEncounter Encounter;
     public FakeRunDefinition Definition;
     public R3VehicleModel VehicleModel;
     public FakePlan Plan;
     public FakeRace CheckpointRace;
     public FakeRoad RoadChallenge;
     public int LastScrapReward,EarnedScrap,DefeatedEnemies;
     public string Error;
     public int RecoveryWindows,SimulationStops,ReactorClears,FullClears,ArenaClears;
     public bool ReadSessionInput()=>false;
     public void MapEditorTick(){}
     public void ReadMenuInput(){}
     public void UpdateRange(){}
     public string ReactorDefeat()=>null;
     public string FullActivityDefeat()=>null;
     public bool UpdateReactorDefense()=>true;
     public bool UpdateFullActivity()=>true;
     public void OpenRecoveryWindow(){RecoveryWindows++;}
     public void SetSimulation(bool enabled){if(!enabled)SimulationStops++;}
     public void ClearReactorActivity(){ReactorClears++;}
     public void ClearFullActivities(){FullClears++;}
     public void ClearGeneratedArena(){ArenaClears++;}
     public void Tick()=>Update();
     public static R1RunController Fresh(){
         var m=new R3VehicleModel(new R3VehicleDesign());
         var prog=new R1RunProgress(3);
         if(!prog.Start())throw new InvalidOperationException("PROGRESS_START_FAILED");
         return new R1RunController {
             Progress=prog,VehicleModel=m,Player=new FakePlayer(),
             Encounter=new FakeEncounter(),Plan=new FakePlan(),
             Definition=new FakeRunDefinition()
         };
     }
     // {{EXACT_ORIGINAL_UPDATE}}
 }
}
"""
PROGRAM = r"""using System;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4;
using UnityEngine;

static class Tests {
 static readonly List<object> results=new();
 static int green=0,redWitness=0;
 static void Check(bool x,string why){if(!x)throw new Exception(why);}
 static object Snapshot(R1RunController c)=>new {
    wallet=c.VehicleModel.Scrap,
    earned=c.EarnedScrap,
    last=c.LastScrapReward,
    state=c.Progress.State.ToString(),
    encounter=c.Progress.CompletedEncounters,
    recovered=c.VehicleModel.Inventory.Select(p=>new {p.Id,p.Kind,p.Health}).OrderBy(p=>p.Id).ToArray(),
    live=c.VehicleModel.Parts.Select(p=>new {p.Id,p.Kind,p.Health}).OrderBy(p=>p.Id).ToArray(),
    rewardWindows=c.RecoveryWindows
 };
 static string State(R1RunController c)=>JsonSerializer.Serialize(Snapshot(c));
 static void Positive(string id,Action a) {
    a();green++;
    Console.WriteLine("ORIGINAL_R1_POSITIVE_GREEN "+id);
    results.Add(new{id,status="PASS",type="ORIGINAL_POSITIVE"});
 }
 static void KnownRed(string id,Action a) {
    a();redWitness++;
    Console.WriteLine("ORIGINAL_R1_KNOWN_PRODUCT_RED "+id);
    results.Add(new{id,status="KNOWN_PRODUCT_RED",type="ORIGINAL_EXCEPTION_ATOMICITY"});
 }
 static int Main(string[] argv) {
    Positive("R1_P15_A_FIRST_WIN_80_SCRAP_ONCE_PLUS_REPEAT_FINISH",()=>{
        var c=R1RunController.Fresh();string before=State(c);
        Check(c.VehicleModel.Scrap==0&&c.Progress.State==R1RunState.Combat,
            "initial Combat wallet not zero");
        c.Tick();
        Check(c.VehicleModel.Scrap==80&&c.EarnedScrap==80&&
              c.LastScrapReward==80&&c.Progress.CompletedEncounters==1&&
              c.Progress.State==R1RunState.Salvage&&c.RecoveryWindows==1,
              "winner transition/reward incorrect "+State(c));
        string after=State(c);
        c.Tick();c.Tick();
        Check(State(c)==after,"second and third terminal Tick duplicated award");
        Check(before!=after,"positive control did not change state");
    });
    Positive("R1_P15_B_DEFEAT_PRECEDES_SIMULTANEOUS_FINISH",()=>{
        var c=R1RunController.Fresh();
        c.Player.DefeatReason="destroyed";
        string before=State(c);
        c.Tick();
        Check(c.Progress.State==R1RunState.Lost&&
            c.VehicleModel.Scrap==0&&c.EarnedScrap==0&&
            c.RecoveryWindows==0,"defeat+finish falsely awarded");
        string lost=State(c);c.Tick();
        Check(State(c)==lost,"loss terminal replay changed wallet/stock");
        Check(before!=lost,"defeat did not transition");
    });
    Positive("R1_P15_C_QUIT_PREEMPTS_WIN_AND_POST_WIN_QUIT_NO_REPAY",()=>{
        var c=R1RunController.Fresh();
        c.QuitRequested=true;
        string before=State(c);c.Tick();
        Check(State(c)==before,"quit-before-win changed money or stock");
        c.QuitRequested=false;c.Tick();
        Check(c.VehicleModel.Scrap==80&&c.Progress.State==R1RunState.Salvage,
            "normal win after unblocked quit failed");
        c.QuitRequested=true;string after=State(c);c.Tick();c.Tick();
        Check(State(c)==after,"finish+quit replay changed economy");
    });
    Positive("R1_P15_D_REAL_R4_SALVAGE_CLAIM_ONCE_IN_SAME_RUN",()=>{
        var c=R1RunController.Fresh();c.Tick();
        var loot=new R4SalvageLedger(3);
        const string enemy="run/encounter/0/enemy/0";
        const string physical="e2381d90e7ad46a3b6b1eac483172c0f";
        Check(loot.Record(enemy,new[]{new R4SalvageItem(
            physical,"enemy",R3PartKind.Wheel,R4WeaponKind.Basic,.75f)}),
            "original R4 ledger Record failed");
        Check(!loot.Record(enemy,new R4SalvageItem[0]),
            "same original enemy event recorded twice");
        int before=c.VehicleModel.Scrap;
        Check(loot.Claim(c.VehicleModel,new[]{physical}),"valid original salvage Claim failed");
        var item=c.VehicleModel.Inventory.Single();
        Check(item.Id==physical&&item.Health==.75f&&c.VehicleModel.Scrap==before,
            "original R4 physical GUID/wallet changed unexpectedly");
        string granted=State(c);
        Check(!loot.Claim(c.VehicleModel,new[]{physical})&&loot.Closed,
            "closed ledger allowed second claim");
        Check(State(c)==granted,"second salvage claim duplicated money/inventory");
    });
    KnownRed("R1_P15_E_WIN_TRANSITION_EXCEPTION_AFTER_WALLET_COMMIT",()=>{
        var c=R1RunController.Fresh();
        // The original win validates 3 distinct Upgrade IDs only AFTER
        // R1.Update has credited Scrap. Simulate original live catalog
        // corrupted DURING encounter, after normally valid run startup.
        c.Definition.Upgrades=new[]{
            new R1UpgradeDefinition{Id="A",Title="A"},
            new R1UpgradeDefinition{Id="B",Title="B"}};
        int thrown=0;
        for(int i=0;i<2;i++){
            try{c.Tick();}catch(InvalidOperationException ex){
                Check(ex.Message.Contains("co najmniej trzy"),
                    "exception is not the original WinEncounter catalog validator");
                thrown++;
            }
        }
        Check(thrown==2,"failed original transition did not repeat");
        Check(c.Progress.State==R1RunState.Combat&&
              c.Progress.CompletedEncounters==0&&
              c.VehicleModel.Scrap==160&&c.EarnedScrap==160&&
              c.RecoveryWindows==0&&c.VehicleModel.Inventory.Count==0,
              "expected genuine original-source payout-before-throw repeated twice "+
               State(c));
        Console.WriteLine("R1_P15_EXCEPTION_WITNESS wallet=0_to_80_to_160 status=KNOWN_PRODUCT_RED");
    });
    string result=argv[0];
    System.IO.File.WriteAllText(result,JsonSerializer.Serialize(new{
      source="CODEX_310_EXACT_ORIGINAL_R1_UPDATE_METHOD_R1PROGRESS_R3_R4",
      expected="4 original positive controls green / 1 original P1 conditional exception-atomicity RED",
      positive=green,negative_product_red=redWitness,results,
      unity_native="NOT_RUN",entire_R1_MonoBehaviour_compiled=false,
      original_update_method_byte_exact=true
    },new JsonSerializerOptions{WriteIndented=true}));
    Console.WriteLine("ORIGINAL_R1_REWARD_TERMINAL_RESULT positive="+green+
        "/4 conditional_product_red="+redWitness+"/1");
    return green==4&&redWitness==1?0:1;
 }
}
"""
def blob_sha(raw):
    return hashlib.sha1(b"blob "+str(len(raw)).encode()+b"\0"+raw).hexdigest()

def extract_method(src):
    match=re.search(r"\bprivate\s+void\s+Update\s*\(\s*\)",src)
    if not match:raise RuntimeError("ORIGINAL_UPDATE_METHOD_MISSING")
    opening=src.find("{",match.end())
    if opening<0:raise RuntimeError("ORIGINAL_UPDATE_OPEN_BRACE_MISSING")
    depth=0
    for pos in range(opening,len(src)):
        if src[pos]=="{":depth+=1
        elif src[pos]=="}":
            depth-=1
            if depth==0:
                return src[match.start():pos+1]
    raise RuntimeError("ORIGINAL_UPDATE_BRACES_UNBALANCED")

def main():
    output=Path("artifacts/chl-econ/original-r1-terminal-idempotence.json").resolve()
    output.parent.mkdir(parents=True,exist_ok=True)
    with tempfile.TemporaryDirectory(prefix="b01-22-r1-original-p15-") as temp:
        root=Path(temp)
        for file,expected_sha in FILES.items():
            with urlopen(BASE+file,timeout=45) as response:
                source_bytes=response.read()
            actual=blob_sha(source_bytes)
            if actual!=expected_sha:
                raise RuntimeError("FAIL_CLOSED_CODEX310_GIT_BLOB_MISMATCH "+file+" "+
                                   actual+" != "+expected_sha)
            target=root/file
            target.parent.mkdir(parents=True,exist_ok=True)
            target.write_bytes(source_bytes)
            print("ORIGINAL_CODEX_BLOB_VERIFIED",file,expected_sha)
        original=(root/"Assets/R1/R1RunController.cs").read_text(encoding="utf8")
        method=extract_method(original)
        extracted_sha=hashlib.sha256(method.encode("utf8")).hexdigest()
        wrapper=WRAPPER_START.replace("// {{EXACT_ORIGINAL_UPDATE}}",method)
        if method not in wrapper or wrapper.count(method)!=1:
            raise RuntimeError("ORIGINAL_UPDATE_INSERTION_NOT_BYTE_EXACT")
        (root/"OriginalR1Update.cs").write_text(wrapper,encoding="utf8")
        outside=(HERE/"Weapon302/OutsideUnityApi.cs").read_text(encoding="utf8")
        before,tail=outside.split("namespace RoboRougelike.R1\n",1)
        _,after=tail.split("namespace RoboRougelike.R3\n",1)
        minimal=before+"namespace RoboRougelike.R3\n"+after
        needle="public static float Clamp01(float x)=>System.Math.Clamp(x,0f,1f);"
        if minimal.count(needle)!=1:
            raise RuntimeError("UNEXPECTED_UNITY_SIGNATURE_FILE")
        minimal=minimal.replace(needle,needle+
          "\n public static float Min(float a,float b)=>System.Math.Min(a,b);"+
          "\n public static float Max(float a,float b)=>System.Math.Max(a,b);")
        (root/"OutsideUnityApi.cs").write_text(minimal+SHIMS,encoding="utf8")
        (root/"RewardTerminal.csproj").write_text(PROJECT,encoding="utf8")
        (root/"Program.cs").write_text(PROGRAM,encoding="utf8")
        command=["dotnet","run","--project",str(root/"RewardTerminal.csproj"),
                 "-c","Release","--",str(output)]
        test=subprocess.run(command,cwd=root,text=True,
                 capture_output=True,timeout=180,check=False)
        print(test.stdout[-8000:])
        if test.stderr:print(test.stderr[-3500:])
        if test.returncode!=0:
            raise RuntimeError("ORIGINAL_R1_CSHARP_EXECUTION_FAILED "+
                               str(test.returncode))
    receipt=json.loads(output.read_text(encoding="utf8"))
    if (receipt.get("positive")!=4 or receipt.get("negative_product_red")!=1 or
        not receipt.get("original_update_method_byte_exact")):
        raise RuntimeError("ORIGINAL_R1_RESULT_CONTRACT_NOT_MATCHED")
    receipt["codex_commit"]=COMMIT
    receipt["verified_blobs"]=FILES
    receipt["original_update_method_sha256"]=extracted_sha
    receipt["origin_scope"]="R1.Update method extracted unchanged; original full R1Progress,R3,R4 Ledger"
    output.write_text(json.dumps(receipt,indent=2,sort_keys=True)+"\n",encoding="utf8")
    print("PASS15_ORIGINAL_R1_ACTUAL_CSHARP 4/4_POSITIVE 1/1_KNOWN_PRODUCT_RED "+
          "UNITY_NOT_RUN METHOD_SHA256="+extracted_sha)

if __name__=="__main__":
    main()
