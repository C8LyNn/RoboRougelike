#!/usr/bin/env python3
"""PASS13 new boundary+failure atomicity tests against immutable ORIGINAL Codex #310 R3/R4.
This harness compiles the actual original C# with signature-only Unity/Mounting
doubles for unrelated code; it never uses substitute economy logic or Unity.
Not a D01 strict GREEN test and not R3 durable persistence.
"""
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import tempfile
from urllib.request import urlopen

HEAD = "231005c6d0351945a8ee6684f3856af118f0e94f"
ROOT = "https://raw.githubusercontent.com/C8LyNn/RoboRougelike/"
SOURCES = {
  "Assets/R3/R3VehicleModel.cs": "9f1bf190c7535182c6ce375cbfe67f5b2862e811",
  "Assets/R4/R4WeaponDefinition.cs": "502f6d7f70d4e8078260de632ab181f244a4d730",
}
HERE = Path(__file__).resolve().parent
PROJECT = """<Project Sdk="Microsoft.NET.Sdk">
<PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework>
<Nullable>disable</Nullable><ImplicitUsings>enable</ImplicitUsings>
<TreatWarningsAsErrors>true</TreatWarningsAsErrors>
<EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup>
<ItemGroup><Compile Include="Program.cs"/>
<Compile Include="OutsideUnityApi.cs"/>
<Compile Include="Assets/R3/R3VehicleModel.cs"/>
<Compile Include="Assets/R4/R4WeaponDefinition.cs"/></ItemGroup>
</Project>"""
PROGRAM = r"""using System;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using RoboRougelike.R3;
using RoboRougelike.R4;
using UnityEngine;
static class BoundsQA {
  static int good=0;static readonly List<string> names=new();
  static void Check(bool yes,string why){if(!yes)throw new Exception(why);}
  static R3VehicleModel Model()=>new(new R3VehicleDesign());
  static R3PartState Part(R3PartKind k,float hp=1)=>
    new(Vector3Int.zero,k,hp){Weapon=R4WeaponKind.Basic};
  static void Test(string name,Action a){a();good++;names.Add(name);Console.WriteLine("PASS "+name);}
  static int Main(string[] args){
    Test("B01_22_ECON_P13_01_ZERO_HP_BASIC_GUN_EXACT_FULL_REPAIR_COST",()=>{
      var m=Model();var p=Part(R3PartKind.Gun,0f);
      Check(m.ReceiveSalvage(new[]{p}),"precondition salvage");
      var item=m.Inventory.Single();string id=item.Id;
      int cost=m.RepairCost(item);Check(cost==30,"zero HP full Basic price");
      m.Award(cost-1);int before=m.Scrap;
      Check(!m.RepairInventory(id),"underfunded repair succeeded");
      Check(m.Scrap==before&&m.Inventory.Single().Id==id&&item.Health==0f,
        "underfunded repair modified authoritative inventory");
      m.Award(1);
      Check(m.RepairInventory(id),"exact funded zeroHP repair rejected");
      Check(m.Scrap==0&&m.Inventory.Single().Id==id&&item.Health==1f,
        "full price repaired item identity or wallet changed");
    });
    Test("B01_22_ECON_P13_02_HALF_HP_WHEEL_CEIL_QUOTE_EXACT_FUNDS",()=>{
      var m=Model();var p=Part(R3PartKind.Wheel,.5f);
      Check(m.ReceiveSalvage(new[]{p}),"precondition");
      var item=m.Inventory.Single();int cost=m.RepairCost(item);
      Check(cost==3,"halfHP wheel 15*.5*.35 ceil should be 3");
      m.Award(2);
      Check(!m.RepairInventory(item.Id)&&m.Scrap==2&&item.Health==.5f,
        "insufficient funds changed state");
      m.Award(1);
      Check(m.RepairInventory(item.Id)&&m.Scrap==0&&item.Health==1f,
        "exact fraction quote failed");
      Check(m.Inventory.Count==1&&m.Inventory.Single().Id==p.Id,
        "physical GUID changed during repair");
    });
    Test("B01_22_ECON_P13_03_ZERO_HP_DISMANTLE_PAYS_ZERO_LEAVES_OTHER_GUID",()=>{
      var m=Model();var zero=Part(R3PartKind.Wheel,0f);
      var other=Part(R3PartKind.Wheel,1f);
      Check(m.ReceiveSalvage(new[]{zero,other}),"precondition");
      m.Award(42);int start=m.Scrap;
      Check(m.DismantleValue(m.Inventory.Single(x=>x.Id==zero.Id))==0,
        "destroyed item should have zero quote");
      Check(m.Dismantle(zero.Id),"zeroHP dismantle rejected");
      Check(m.Scrap==start&&m.Inventory.Count==1&&
        m.Inventory.Single().Id==other.Id&&m.Inventory.Single().Health==1f,
        "dismantle lost other GUID or minted scrap");
    });
    Test("B01_22_ECON_P13_04_INVALID_MIXED_SALVAGE_BATCH_ALL_OR_NONE",()=>{
      var m=Model();var valid=Part(R3PartKind.Wheel,.5f);
      var invalid=Part(R3PartKind.Wheel,-.1f);
      m.Award(13);int start=m.Scrap;
      Check(!m.ReceiveSalvage(new[]{valid,invalid}),
        "mixed invalid health batch wrongly accepted");
      Check(m.Inventory.Count==0&&!m.Owns(valid.Id)&&!m.Owns(invalid.Id)
        &&m.Scrap==start,"partial grant or money mutation on failed batch");
      Check(m.ReceiveSalvage(new[]{valid}),"valid retry after bad batch denied");
      Check(m.Inventory.Count==1&&m.Inventory.Single().Id==valid.Id
        &&m.Inventory.Single().Health==.5f&&m.Scrap==start,
        "retry changed GUID, health or wallet");
    });
    var report=new {test="B01-22-PASS13-ORIGINAL-CODEX310-ECONOMIC-BOUNDARIES",
      codex_head="231005c6d0351945a8ee6684f3856af118f0e94f",
      original_csharp=true,unity_native="NOT_RUN",durable_storage="NOT_IMPLEMENTED",
      passed=good,total=4,cases=names};
    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(
      System.IO.Path.GetFullPath(args[0]))!);
    System.IO.File.WriteAllText(args[0],
      JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));
    Console.WriteLine("CODEX310_ORIGINAL_R3_ECON_BOUNDS "+good+"/4 PASS DOTNET8 NOT_UNITY");
    return good==4?0:1;
  }
}
"""
def git_blob(blob):
    return hashlib.sha1(b"blob "+str(len(blob)).encode()+b"\0"+blob).hexdigest()

def main():
    dest = Path("artifacts/chl-econ/codex310-econ-original-bounds.json").resolve()
    dest.parent.mkdir(parents=True,exist_ok=True)
    with tempfile.TemporaryDirectory(prefix="b01-22-p13-econ-") as folder:
        p=Path(folder)
        (p/"Tests.csproj").write_text(PROJECT,encoding="utf8")
        (p/"Program.cs").write_text(PROGRAM,encoding="utf8")
        shutil.copyfile(HERE/"OutsideUnityApi.cs",p/"OutsideUnityApi.cs")
        for name,blob_sha in SOURCES.items():
            with urlopen(ROOT+HEAD+"/"+name,timeout=40) as response:
                blob=response.read()
            if git_blob(blob)!=blob_sha:
                raise RuntimeError("FAIL_CLOSED_ORIGINAL_CODEX_GIT_BLOB_MISMATCH "+name)
            f=p/name;f.parent.mkdir(parents=True,exist_ok=True);f.write_bytes(blob)
            print("VERIFIED_ORIGINAL_CODEX_GIT_BLOB",name,blob_sha)
        subprocess.run(["dotnet","run","--project",str(p/"Tests.csproj"),
          "-c","Release","--",str(dest)],check=True,timeout=180)
    data=json.loads(dest.read_text(encoding="utf8"))
    if data["passed"]!=4 or data["total"]!=4:
        raise RuntimeError("CODEX310_ORIGINAL_ECON_BOUNDS_FAIL")
    print("PASS13_NEW_ORIGINAL_R3_ECON_RECEIPT_4_OF_4")

if __name__=="__main__":
    main()
