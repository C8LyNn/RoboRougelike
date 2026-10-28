#!/usr/bin/env python3
"""PASS16 new Codex R1 winner transaction: full-original C# method post-fix GREEN.

Independently executes immutable original R1RunController.Update() text,
complete original R1RunProgress, R1UpgradeDefinition, R3VehicleModel,
R4SalvageLedger and R4WeaponDefinition, with unrelated Unity scene/API
signature doubles. Reuses PASS15 original-source fixture without rewriting its
historical old-RED contract. Native Unity / G1 / full run is NOT_RUN.
"""
import hashlib
import json
import subprocess
import tempfile
from pathlib import Path
from urllib.request import urlopen
import test_original_r1_reward_terminal as old

CODEX="561c223f7f01c1e609548bf1c8c66b5132ec5485"
FILES={
 "Assets/R1/R1RunController.cs":"3db48a98faba36e124f13aef543f6a89e7c49147",
 "Assets/R1/R1RunProgress.cs":"a91bfe0eb5d3f92dab2e8359aec4520a6d01f698",
 "Assets/R1/R1UpgradeDefinition.cs":"5c5731d5ec7571fa57b5779b2300fd250090ebf4",
 "Assets/R3/R3VehicleModel.cs":"8cc68faf89793c3a803e5807594c9faf0df10091",
 "Assets/R4/R4WeaponDefinition.cs":"502f6d7f70d4e8078260de632ab181f244a4d730",
 "Assets/R4/R4SalvageLedger.cs":"e542c4b8c423091f00b86d953bd777598fc634ec"
}
NEW_TEST=r"""
    Positive("R1_P16_E_INVALID_UPGRADE_CATALOG_FAILS_BEFORE_ANY_PAYOUT",()=>{
        var c=R1RunController.Fresh();
        // Fully original R1Progress validator: two distinct ids instead of three,
        // representing a mutated/malformed catalog after valid run start.
        c.Definition.Upgrades=new[]{
            new R1UpgradeDefinition{Id="A",Title="A"},
            new R1UpgradeDefinition{Id="B",Title="B"}};
        string before=State(c);
        c.Tick();
        Check(c.Error!=null&&c.Error.Contains("co najmniej trzy"),
            "original R1 winner failed to report invalid reward catalog");
        Check(c.Progress.State==R1RunState.Combat&&
              c.Progress.CompletedEncounters==0&&
              c.VehicleModel.Scrap==0&&c.EarnedScrap==0&&
              c.LastScrapReward==0&&c.VehicleModel.Inventory.Count==0&&
              c.RecoveryWindows==0,
              "R1 paid or granted win before validated offer transition "+State(c));
        string after=State(c);
        Check(before==after,"failed winner changed wallet, inventory, encounter or progress");
        c.Tick();c.Tick();
        Check(State(c)==after,"repeated invalid winner doubled reward "+State(c));
        // Test-harness-only recovery: clear synthetic Error and restore good
        // catalog; NOT a claim Unity UI can recover from this Error by itself.
        c.Error=null;
        c.Definition.Upgrades=new[]{
            new R1UpgradeDefinition{Id="A",Title="A"},
            new R1UpgradeDefinition{Id="B",Title="B"},
            new R1UpgradeDefinition{Id="C",Title="C"}};
        c.Tick();
        Check(c.VehicleModel.Scrap==80&&c.EarnedScrap==80&&
              c.Progress.CompletedEncounters==1&&
              c.Progress.State==R1RunState.Salvage&&c.RecoveryWindows==1,
              "valid original source did not pay exactly once after data recovery");
        after=State(c);c.Tick();
        Check(State(c)==after,"post-recovery winner duplicated reward");
    });
"""
def make_program():
    base=old.PROGRAM
    start=base.index('    KnownRed("R1_P15_E_WIN_TRANSITION_EXCEPTION_AFTER_WALLET_COMMIT"')
    end=base.index('    string result=argv[0];',start)
    program=base[:start]+NEW_TEST+base[end:]
    replacements={
        "4 original positive controls green / 1 original P1 conditional exception-atomicity RED":
          "5 original-code post-fix positive controls including invalid catalog fail-atomic",
        "return green==4&&redWitness==1?0:1;":"return green==5&&redWitness==0?0:1;",
        '"/4 conditional_product_red="':'"/5 conditional_product_red="',
    }
    for a,b in replacements.items():
        if program.count(a)!=1:raise RuntimeError("PASS15_TEST_FIXTURE_FORMAT_CHANGED "+a)
        program=program.replace(a,b)
    return program

def main():
    out=Path("artifacts/chl-econ/codex-r1-win-atomic-postfix.json").resolve()
    out.parent.mkdir(parents=True,exist_ok=True)
    with tempfile.TemporaryDirectory(prefix="b01-22-p16-r1-postfix-") as temp:
        root=Path(temp)
        for file,sha in FILES.items():
            with urlopen("https://raw.githubusercontent.com/C8LyNn/RoboRougelike/"+
                         CODEX+"/"+file,timeout=45) as resp:
                raw=resp.read()
            if old.blob_sha(raw)!=sha:
                raise RuntimeError("ORIGINAL_CODEX_R1_NEW_BLOB_MISMATCH "+file)
            dest=root/file
            dest.parent.mkdir(parents=True,exist_ok=True)
            dest.write_bytes(raw)
            print("PINNED_ORIGINAL_NEW_R1_CODEX_BLOB",file,sha)
        source=(root/"Assets/R1/R1RunController.cs").read_text(encoding="utf8")
        original=old.extract_method(source)
        if ("Progress.WinEncounter" not in original or
            original.index("Progress.WinEncounter")>original.index("VehicleModel.Award") or
            'Debug.LogWarning("[R1] "+Error)' not in original):
            raise RuntimeError("CODEX_WIN_FIX_SOURCE_NOT_PRESENT")
        wrapper=old.WRAPPER_START.replace("// {{EXACT_ORIGINAL_UPDATE}}",original)
        if wrapper.count(original)!=1:raise RuntimeError("R1_EXTRACT_TEXT_CORRUPT")
        (root/"OriginalR1Update.cs").write_text(wrapper,encoding="utf8")
        outside=(old.HERE/"Weapon302/OutsideUnityApi.cs").read_text(encoding="utf8")
        before,tail=outside.split("namespace RoboRougelike.R1\n",1)
        _,after=tail.split("namespace RoboRougelike.R3\n",1)
        stubs=before+"namespace RoboRougelike.R3\n"+after
        needle="public static float Clamp01(float x)=>System.Math.Clamp(x,0f,1f);"
        if stubs.count(needle)!=1:
            raise RuntimeError("R1_UNITY_SIGNATURE_SCOPE_CHANGED")
        stubs=stubs.replace(needle,needle+
            "\n public static float Min(float a,float b)=>System.Math.Min(a,b);"+
            "\n public static float Max(float a,float b)=>System.Math.Max(a,b);")
        u=old.SHIMS
        debug="public static class Debug {public static void Log(string msg) {} }"
        if u.count(debug)!=1:raise RuntimeError("ORIGINAL_R1_DEBUG_STUB_CHANGED")
        u=u.replace(debug,
            "public static class Debug {public static void Log(string msg) {} "+
            "public static void LogWarning(string msg) {} }")
        (root/"OutsideUnityApi.cs").write_text(stubs+u,encoding="utf8")
        (root/"RewardTerminal.csproj").write_text(old.PROJECT,encoding="utf8")
        (root/"Program.cs").write_text(make_program(),encoding="utf8")
        run=subprocess.run(["dotnet","run","--project",str(root/"RewardTerminal.csproj"),
            "-c","Release","--",str(out)],cwd=root,capture_output=True,text=True,timeout=180)
        print(run.stdout[-7500:])
        if run.stderr:print(run.stderr[-3000:])
        if run.returncode:raise RuntimeError("ORIGINAL_R1_POSTFIX_TEST_FAILED exit="+str(run.returncode))
    receipt=json.loads(out.read_text(encoding="utf8"))
    if receipt.get("positive")!=5 or receipt.get("negative_product_red")!=0:
        raise RuntimeError("FAIL_CLOSED_POSTFIX_ACCEPTANCE_RESULT")
    receipt["source_commit"]=CODEX
    receipt["source_blobs"]=FILES
    receipt["original_R1_update_sha256"]=hashlib.sha256(original.encode("utf8")).hexdigest()
    receipt["scope"]="complete original R1 Progress+R3+R4, exact original R1 Update method; no actual Unity"
    receipt["native_unity"]="NOT_RUN"
    out.write_text(json.dumps(receipt,sort_keys=True,indent=2)+"\n",encoding="utf8")
    print("PASS16_POSTFIX_ORIGINAL_R1_5_OF_5_GREEN including formerly RED no-double-wallet")

if __name__=="__main__":
    main()
