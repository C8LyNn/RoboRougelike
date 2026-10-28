#!/usr/bin/env python3
"""B01-22 PASS16: real changed Codex R4 salvage transaction C# execution.

Uses complete immutable ORIGINAL Codex R3/R4 source files verified by Git blobs.
Only unrelated Unity + R3 physical mounting APIs are signature doubles.
NOT an integration patch, Unity run, approval of G1/G2 or legacy ECON package.
"""
import hashlib
import json
import subprocess
import tempfile
from pathlib import Path
from urllib.request import urlopen

HERE=Path(__file__).resolve().parent
CODEX="589b60a912096d1e9917cb0bbac73a53e7543b4f"
SOURCE={
    "Assets/R3/R3VehicleModel.cs":"8cc68faf89793c3a803e5807594c9faf0df10091",
    "Assets/R4/R4WeaponDefinition.cs":"502f6d7f70d4e8078260de632ab181f244a4d730",
    "Assets/R4/R4SalvageLedger.cs":"e542c4b8c423091f00b86d953bd777598fc634ec",
}
PROJECT="""<Project Sdk="Microsoft.NET.Sdk">
<PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework>
<Nullable>disable</Nullable><ImplicitUsings>enable</ImplicitUsings>
<EnableDefaultCompileItems>false</EnableDefaultCompileItems>
<TreatWarningsAsErrors>true</TreatWarningsAsErrors></PropertyGroup>
<ItemGroup>
<Compile Include="Main.cs" />
<Compile Include="OutsideUnityApi.cs" />
<Compile Include="Assets/R3/R3VehicleModel.cs" />
<Compile Include="Assets/R4/R4WeaponDefinition.cs" />
<Compile Include="Assets/R4/R4SalvageLedger.cs" />
</ItemGroup></Project>
"""
PROGRAM=r"""using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using RoboRougelike.R3;
using RoboRougelike.R4;
using UnityEngine;
static class Runner {
 static int green;
 static readonly List<object> cases = new();
 static readonly string W="e2381d90e7ad46a3b6b1eac483172c0f";
 static readonly string G="ff23bc410d034ca49f0ef2247e8a25e3";
 static readonly string B="0d64dc0d1d064c9596e9bedfcd600107";
 static R3VehicleModel New()=>new(new R3VehicleDesign());
 static R4SalvageItem Item(string id,R3PartKind kind,float hp,R4WeaponKind weapon=R4WeaponKind.Basic)=>
    new R4SalvageItem(id,"original enemy",kind,weapon,hp);
 static void Check(bool ok,string note){if(!ok)throw new Exception(note);}
 static void Run(string id,Action test) {
    try {test();green++;cases.Add(new{id,status="PASS"});Console.WriteLine("ORIGINAL_R4_GREEN "+id);}
    catch(Exception e) {cases.Add(new{id,status="FAIL",why=e.Message});
        Console.WriteLine("ORIGINAL_R4_RED "+id+" "+e.Message);}
 }
 static int Main(string[] argv) {
    Run("P16_R4_01_INVALID_WEAPON_RECORD_ATOMIC_AND_RETRY",()=>{
      var m=New();var ledger=new R4SalvageLedger(3);
      var invalid=Item(G,R3PartKind.Gun,.8f,(R4WeaponKind)987654);
      Check(!ledger.Record("enemy/A",new[]{invalid}),"malformed weapon admitted to original R4 Record");
      Check(ledger.Items.Count==0&&!ledger.Closed&&ledger.DismantledScrap==0&&m.Scrap==0,
          "failed Record mutated ledger or wallet");
      Check(ledger.Record("enemy/A",new[]{Item(W,R3PartKind.Wheel,.5f)}),
          "failed Record consumed original enemy event identifier");
      Check(ledger.Items.Count==1,"original recovered item count");
    });
    Run("P16_R4_02_ORIGINAL_RECOVER_AND_DISMANTLE_EXACT_ONCE",()=>{
      var m=New();var ledger=new R4SalvageLedger(3);
      var incoming=new[]{Item(W,R3PartKind.Wheel,0f),Item(G,R3PartKind.Gun,.8f)};
      Check(ledger.Record("enemy/B",incoming),"original R4 Record denied legitimate batch");
      int quote=ledger.PreviewScrap(m,new[]{W});
      Check(quote==6,"original Gun30 at 80%HP should yield Floor(30*.25*.8)=6");
      Check(ledger.Claim(m,new[]{W}),"one valid R4 Claim refused");
      Check(ledger.Closed&&ledger.DismantledScrap==6&&m.Scrap==6,
          "real R4 ledger failed exact payout once");
      Check(m.Inventory.Count==1&&m.Inventory[0].Id==W&&m.Inventory[0].Health==0f,
          "real R3 lost physical selected wreck GUID/HP");
      string before=JsonSerializer.Serialize(new{wallet=m.Scrap,owner=m.Inventory.Select(p=>p.Id).ToArray()});
      Check(!ledger.Claim(m,new[]{W}),"closed original salvage claimed twice");
      Check(before==JsonSerializer.Serialize(new{wallet=m.Scrap,owner=m.Inventory.Select(p=>p.Id).ToArray()}),
          "repeat Claim paid or changed physical inventory");
    });
    Run("P16_R4_03_BAD_SELECTION_FAIL_CLOSED_AND_VALID_RETRY",()=>{
      var m=New();var ledger=new R4SalvageLedger(2);
      Check(ledger.Record("enemy/C",new[]{Item(W,R3PartKind.Wheel,.75f)}),"record valid");
      Check(!ledger.Claim(m,new[]{B}),"unknown physical GUID was claimed");
      Check(!ledger.Closed&&m.Scrap==0&&m.Inventory.Count==0,
          "invalid selection mutated original wallet or closed ledger");
      Check(ledger.Claim(m,new[]{W}),"valid retry should succeed once");
      Check(ledger.Closed&&m.Inventory.Single().Id==W&&m.Scrap==0,
          "retry physical ownership not preserved");
      Check(!ledger.Record("enemy/C",new[]{Item(B,R3PartKind.Wheel,.5f)}),
          "closed ledger admitted stale enemy event");
    });
    Run("P16_R4_04_DUPLICATE_ITEM_ID_ACROSS_ENEMIES_REJECTED",()=>{
      var m=New();var ledger=new R4SalvageLedger(2);
      Check(ledger.Record("enemy/D",new[]{Item(W,R3PartKind.Wheel,.5f)}),"first Record");
      Check(!ledger.Record("enemy/E",new[]{Item(W,R3PartKind.Wheel,.8f)}),
          "same physical GUID admitted for second enemy");
      Check(ledger.Items.Count==1&&ledger.Items[0].Health==.5f&&m.Scrap==0,
          "duplicate GUID changed original valid loot");
      Check(ledger.Claim(m,new[]{W})&&m.Inventory.Single().Id==W,
          "original owner lost unique GUID during claim");
    });
    var receipt=new {
        original_source="COMPLETE_UNMODIFIED_R3_R4_SALVAGE_CSHARP",
        unity_api="SIGNATURE_SHIMS_ONLY_NOT_UNITY",
        game_integrated=false,green,total=4,cases
    };
    System.IO.File.WriteAllText(argv[0],JsonSerializer.Serialize(receipt,
       new JsonSerializerOptions{WriteIndented=true}));
    Console.WriteLine("ORIGINAL_CODEX_R4_NEW_SOURCE "+green+"/4 GREEN DOTNET8 UNITY_NOT_RUN");
    return green==4?0:1;
 }
}
"""
def blob(raw):
    return hashlib.sha1(b"blob "+str(len(raw)).encode()+b"\0"+raw).hexdigest()

def main():
    out=Path("artifacts/chl-econ/codex-new-r4-salvage.json").resolve()
    out.parent.mkdir(parents=True,exist_ok=True)
    with tempfile.TemporaryDirectory(prefix="b01-22-p16-r4-") as td:
        root=Path(td)
        for path,sha in SOURCE.items():
            with urlopen("https://raw.githubusercontent.com/C8LyNn/RoboRougelike/"+CODEX+"/"+path,
                         timeout=40) as res:
                raw=res.read()
            if blob(raw)!=sha:
                raise RuntimeError("ORIGINAL_GIT_BLOB_CHANGED "+path)
            dest=root/path
            dest.parent.mkdir(parents=True,exist_ok=True)
            dest.write_bytes(raw)
            print("ORIGINAL_R4_BLOB_VERIFIED",path,sha)
        (root/"Main.cs").write_text(PROGRAM,encoding="utf-8")
        (root/"Runner.csproj").write_text(PROJECT,encoding="utf-8")
        signature=(HERE/"Weapon302/OutsideUnityApi.cs").read_bytes()
        (root/"OutsideUnityApi.cs").write_bytes(signature)
        p=subprocess.run(["dotnet","run","--project",str(root/"Runner.csproj"),
                 "-c","Release","--",str(out)],capture_output=True,text=True,timeout=200)
        print(p.stdout[-6500:])
        if p.stderr:print(p.stderr[-2500:])
        if p.returncode:
            raise RuntimeError("ORIGINAL_R4_CSHARP_FAIL exit="+str(p.returncode))
    result=json.loads(out.read_text(encoding="utf8"))
    if result.get("green")!=4 or any(c.get("status")!="PASS" for c in result["cases"]):
        raise RuntimeError("REAL_R4_RECEIPT_FAILED")
    result["codex_commit"]=CODEX
    result["git_blobs"]=SOURCE
    out.write_text(json.dumps(result,indent=2,sort_keys=True)+"\n",encoding="utf8")
    print("PASS16_ORIGINAL_R4_RECEIPT 4/4 GREEN WIN_UBUNTU_NT not_unity")

if __name__=="__main__":
    main()
