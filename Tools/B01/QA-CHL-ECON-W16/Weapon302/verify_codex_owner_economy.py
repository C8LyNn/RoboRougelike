#!/usr/bin/env python3
"""B01-22 PASS14 Codex CV-01 + HQ #275 acceptance gate.

Compile unmodified ORIGINAL integrated R3VehicleModel.cs/R4WeaponDefinition.cs
from an EXACT user-provided commit SHA and verified Git blob identities.
No gameplay source is edited; unrelated Unity/mounting APIs are signature-only
test doubles. Two statuses: historical original-RED diagnostic and future GREEN.

Usage after Codex publishes a new owning SHA:
python Tools/B01/QA-CHL-ECON-W16/Weapon302/verify_codex_owner_economy.py \
 --sha <FULL_SHA> --r3-blob <R3_FILE_GIT_BLOB_SHA> \
 --r4-blob <R4_FILE_GIT_BLOB_SHA> --require-green
This REQUIRES genuine six GREEN security+owner rebuild contracts, and positive
damaged repair. It must NOT be accepted as Unity Editor/Player G1.
"""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import tempfile
from urllib.request import urlopen

ORIGINAL_SHA = "231005c6d0351945a8ee6684f3856af118f0e94f"
ORIGINAL_R3 = "9f1bf190c7535182c6ce375cbfe67f5b2862e811"
ORIGINAL_R4 = "502f6d7f70d4e8078260de632ab181f244a4d730"
HERE = Path(__file__).resolve().parent
REPO = "https://raw.githubusercontent.com/C8LyNn/RoboRougelike/"
PATHS = {
    "Assets/R3/R3VehicleModel.cs": "r3_blob",
    "Assets/R4/R4WeaponDefinition.cs": "r4_blob",
}
ECON_PROJECT = """<Project Sdk="Microsoft.NET.Sdk">
<PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework>
<Nullable>disable</Nullable><ImplicitUsings>enable</ImplicitUsings>
<EnableDefaultCompileItems>false</EnableDefaultCompileItems>
<TreatWarningsAsErrors>true</TreatWarningsAsErrors></PropertyGroup>
<ItemGroup>
<Compile Include="EconOwnerPolicy.cs" /><Compile Include="OutsideUnityApi.cs" />
<Compile Include="_pinned/Assets/R3/R3VehicleModel.cs" />
<Compile Include="_pinned/Assets/R4/R4WeaponDefinition.cs" />
</ItemGroup></Project>
"""
ECON_PROGRAM = r"""using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using RoboRougelike.R3;
using RoboRougelike.R4;
using UnityEngine;
static class EconOwnerPolicy {
    static int green,red;
    static readonly List<object> results=new();
    static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
    static R3VehicleModel Model()=>new(new R3VehicleDesign());
    static void Test(string id,Action action) {
        try {action();green++;results.Add(new {id,status="GREEN"});
            Console.WriteLine("OWNER_POLICY_GREEN "+id);}
        catch(Exception e){red++;results.Add(new {id,status="RED",reason=e.Message});
            Console.WriteLine("OWNER_POLICY_RED "+id+" "+e.Message);}
    }
    static void Rebuild(string label,R3PartKind kind,int purchasePrice) {
        var m=Model();
        var original=new R3PartState(Vector3Int.zero,kind,0f)
            {Weapon=R4WeaponKind.Basic};
        Check(m.Price(kind)==purchasePrice,label+" purchase price unexpected");
        Check(m.ReceiveSalvage(new[]{original}),label+" precondition salvage denied");
        string id=m.Inventory.Single().Id;
        int quote=m.RepairCost(m.Inventory.Single());
        Check(quote>0&&quote<purchasePrice,
            label+" P0 owner #275: expected 0 < REBUILD < PURCHASE "+
            purchasePrice+", actual rebuild="+quote);
        Check(!m.RepairInventory(id)&&m.Scrap==0&&
            m.Inventory.Single().Id==id&&m.Inventory.Single().Health==0,
            "0-Scrap repair unexpectedly changed ownership/wallet/HP");
        m.Award(quote-1);
        Check(!m.RepairInventory(id)&&m.Scrap==quote-1&&
            m.Inventory.Single().Id==id&&m.Inventory.Single().Health==0,
            "one Scrap short should never repair");
        m.Award(1);
        Check(m.RepairInventory(id)&&m.Scrap==0&&
            m.Inventory.Single().Id==id&&m.Inventory.Single().Health==1,
            "exactly funded discounted rebuild failed or re-IDed physical item");
    }
    static int Main(string[] args){
        // Armor price5 is ONLY a quote contract: 0HP Armor cannot pass current
        // ReceiveSalvage ingress. Do not fake an Armor inventory recovery.
        Test("P0_OWNER_PRICE5_ARMOR_ZERO_HP_QUOTE_STRICTLY_DISCOUNTED",()=>{
            var m=Model();var p=new R3PartState(Vector3Int.zero,R3PartKind.Armor,0f);
            int purchase=m.Price(p.Kind);
            int quote=m.RepairCost(p);
            Check(purchase==5,"Armor purchase price no longer 5");
            Check(quote>0&&quote<purchase,
                "Owner #275 expected 0<0HP Armor quote<5, actual="+quote);
        });
        Test("P0_OWNER_PRICE15_WHEEL_SAME_GUID_DISCOUNTED_REBUILD",()=>{
            Rebuild("Wheel",R3PartKind.Wheel,15);
        });
        Test("P0_OWNER_PRICE30_BASIC_GUN_SAME_GUID_DISCOUNTED_REBUILD",()=>{
            Rebuild("BasicGun",R3PartKind.Gun,30);
        });
        // Existing >0HP proportional repair MUST NOT regress when 0HP policy
        // is corrected: live original source at 78% wheel => ceil(1.155)=2.
        Test("POSITIVE_DAMAGED_78_PERCENT_WHEEL_STILL_REPAIRABLE",()=>{
            var m=Model();
            var p=new R3PartState(Vector3Int.zero,R3PartKind.Wheel,.78f);
            Check(m.ReceiveSalvage(new[]{p}),"damaged salvage denied");
            int quote=m.RepairCost(m.Inventory.Single());
            Check(quote==2,"78%HP Wheel expected 2 Scrap, got "+quote);
            m.Award(2);
            Check(m.RepairInventory(p.Id)&&m.Inventory.Single().Id==p.Id&&
                m.Inventory.Single().Health==1&&m.Scrap==0,
                "ordinary damaged repair regression");
        });
        string output=args.Length>0?args[0]:"economy-owner-policy.json";
        System.IO.File.WriteAllText(output,
            JsonSerializer.Serialize(new{owner="#275",green,red,cases=results,
                kind="ORIGINAL_CODEX_R3_CSHARP_DOTNET8_SIGNATURE_DOUBLES",
                unity_native="NOT_RUN",disk_inventory="NOT_IMPLEMENTED"},
                new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine("ECON_OWNER_POLICY_RESULT "+green+" GREEN / "+red+" RED");
        return red==0?0:1;
    }
}
"""
def git_blob(raw):
    return hashlib.sha1(b"blob "+str(len(raw)).encode("ascii")+b"\0"+raw).hexdigest()

def run(project,stage,path):
    proc=subprocess.run(["dotnet","run","--project",str(stage/project),
                         "-c","Release","--",str(path)],
                        cwd=stage,capture_output=True,text=True,timeout=180)
    print(proc.stdout[-5500:])
    if proc.stderr:
        print(proc.stderr[-2000:])
    if proc.returncode not in (0,1):
        raise RuntimeError("UNEXPECTED_COMPILATION_OR_TEST_EXIT "+
                           str(proc.returncode)+" "+project)
    if not path.exists():
        raise RuntimeError("NO_EXECUTED_JSON_RESULT "+project)
    return proc.returncode,json.loads(path.read_text(encoding="utf8"))

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument("--sha",default=ORIGINAL_SHA)
    parser.add_argument("--r3-blob",default=ORIGINAL_R3)
    parser.add_argument("--r4-blob",default=ORIGINAL_R4)
    mode=parser.add_mutually_exclusive_group(required=True)
    mode.add_argument("--expect-current-red",action="store_true")
    mode.add_argument("--require-green",action="store_true")
    args=parser.parse_args()
    expected={"Assets/R3/R3VehicleModel.cs":args.r3_blob,
              "Assets/R4/R4WeaponDefinition.cs":args.r4_blob}
    if any(len(x)!=40 or any(c not in "0123456789abcdef" for c in x)
           for x in (args.sha,*expected.values())):
        raise ValueError("FULL_LOWERCASE_HEX_SHA_REQUIRED")
    if args.expect_current_red and (
        args.sha!=ORIGINAL_SHA or args.r3_blob!=ORIGINAL_R3 or
        args.r4_blob!=ORIGINAL_R4):
        raise ValueError("BASELINE_RED_ONLY_ALLOWED_ON_IMMUTABLE_ORIGINAL_SHA")
    out=Path("artifacts/chl-econ/codex-owner-economy-gate.json").resolve()
    out.parent.mkdir(parents=True,exist_ok=True)
    with tempfile.TemporaryDirectory(prefix="b01-22-cv01-owner-") as tmp:
        stage=Path(tmp)
        (stage/"_pinned").mkdir()
        shutil.copy2(HERE/"SecurityContract.cs",stage/"SecurityContract.cs")
        shutil.copy2(HERE/"SecurityContract.csproj",stage/"SecurityContract.csproj")
        shutil.copy2(HERE/"OutsideUnityApi.cs",stage/"OutsideUnityApi.cs")
        (stage/"EconOwnerPolicy.cs").write_text(ECON_PROGRAM,encoding="utf8")
        (stage/"EconOwnerPolicy.csproj").write_text(ECON_PROJECT,encoding="utf8")
        for path,sha in expected.items():
            with urlopen(REPO+args.sha+"/"+path,timeout=40) as res:
                raw=res.read()
            actual=git_blob(raw)
            if actual!=sha:
                raise RuntimeError("CODEX_SOURCE_BLOB_MISMATCH "+path+
                                   " expected="+sha+" actual="+actual)
            dest=stage/"_pinned"/path
            dest.parent.mkdir(parents=True,exist_ok=True)
            dest.write_bytes(raw)
            print("SOURCE_VERIFIED",args.sha,path,sha)
        code_security,sec=run("SecurityContract.csproj",stage,stage/"sec.json")
        code_policy,pol=run("EconOwnerPolicy.csproj",stage,stage/"pol.json")
    ids={x["id"]:x["status"] for x in sec["cases"]}
    if set(ids)!={
        "E01_REJECT_INVALID_ENUM_BEFORE_SALVAGE_COMMIT",
        "E02_PREEXISTING_CORRUPT_REPAIR_IS_FAIL_CLOSED_ATOMIC",
        "E03_PREEXISTING_CORRUPT_DISMANTLE_NEVER_LOSES_ITEM"}:
        raise RuntimeError("SECURITY_CONTRACT_CASE_SET_CHANGED")
    if len(pol["cases"])!=4 or len({x["id"] for x in pol["cases"]})!=4:
        raise RuntimeError("OWNER_POLICY_CASE_SET_CHANGED")
    positive=next(x for x in pol["cases"] if x["id"].startswith("POSITIVE_"))
    strict_green=(sec["security_contract_pass"]==3 and
                  sec["security_contract_fail"]==0 and code_security==0)
    owner_green=(pol["green"]==4 and pol["red"]==0 and code_policy==0)
    baseline=(sec["security_contract_pass"]==0 and
              sec["security_contract_fail"]==3 and code_security==1 and
              pol["green"]==1 and pol["red"]==3 and code_policy==1 and
              positive["status"]=="GREEN")
    status=("CODEX_CV01_OWNER_POLICY_GREEN" if strict_green and owner_green
            else "EXPECTED_CURRENT_OWNER_SECURITY_RED" if baseline
            else "PARTIAL_OR_NEW_FAILURE_REVIEW_REQUIRED")
    receipt={
        "source":args.sha,"r3_blob":args.r3_blob,"r4_blob":args.r4_blob,
        "strict_security":{ "green":sec["security_contract_pass"],
            "red":sec["security_contract_fail"],"cases":sec["cases"]},
        "owner_rebuild_policy":{"green":pol["green"],"red":pol["red"],
            "cases":pol["cases"]},
        "status":status,"original_r3_r4_compiled":True,
        "native_unity":"NOT_RUN","cold_inventory_save":"NOT_IMPLEMENTED",
        "stub_scope":"UNITY_MOUNTING_SIGNATURES_ONLY_NO_ENGINE"
    }
    out.write_text(json.dumps(receipt,indent=2,sort_keys=True)+"\n",encoding="utf8")
    print("CODEX_CV01_P0_ECONOMY_GATE",status,
          "security",sec["security_contract_pass"],"/3",
          "rebuild",pol["green"]-int(positive["status"]=="GREEN"),"/3",
          "positive_damaged",positive["status"])
    if args.expect_current_red and not baseline:
        raise RuntimeError("CURRENT_RED_DIAGNOSTIC_CHANGED_OR_UNEXPECTED_FAILURE")
    if args.require_green and not (strict_green and owner_green):
        raise RuntimeError("CODEX_CV01_P0_NOT_YET_3_SECURITY_AND_3_REBUILD_GREEN")
    print("PASS14_ORIGINAL_SOURCE_GATE_VERIFIED_NOT_NATIVE")

if __name__=="__main__":
    main()
