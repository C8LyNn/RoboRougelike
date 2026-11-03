#!/usr/bin/env python3
"""Run exact original R3 economy methods against a portable .NET8 stub host.
SOURCE-ISOLATED, NOT Unity Editor, not physical R3 vehicle integration.
"""
import json
import os
from pathlib import Path
import subprocess
import tempfile

ROOT=Path(__file__).resolve().parents[4]
BASE="3bd9b159997924dcecb4e1cd1d41137896e5199e"
GAME="Assets/R3/R3VehicleModel.cs"
QA="Assets/Tests/Editor/B0110W31EconomyParityTests.cs"
META=QA+".meta"
OTHER="Assets/Tests/Editor/GarageEditFreedomTests.cs"
TOOL="Tools/B01/B0123/W32/verify_economy.py"
WORKFLOW=".github/workflows/b01-23-w32-econ-parity.yml"
EXPECTED={GAME,QA,META,OTHER,TOOL,WORKFLOW}

def git(*args):
    return subprocess.check_output(["git",*args],cwd=ROOT,text=True,encoding="utf-8").strip()

files=set(git("diff","--name-only",BASE,"HEAD").splitlines())
assert files==EXPECTED,("unexpected scope",sorted(files^EXPECTED))
assert git("hash-object",QA)=="37293e66b7f5d8a3b3479ab80047f3c3bacff884"
assert git("hash-object",META)=="d350155f18021a60040c7b82e984a32c68241896"
src=(ROOT/GAME).read_text(encoding="utf-8-sig")
parent=git("show",BASE+":"+GAME)
qa=(ROOT/QA).read_text(encoding="utf-8-sig")
assert "04789746afe84cd7b86254ad3c412d21" in (ROOT/META).read_text(encoding="utf-8")
assert "OwnedDestroyedArmorRebuildQuoteMatchesApprovedV7Floor65" in qa
assert "PurchasePriceOneAllowsZeroScrapPaidRebuildWithoutFreeWinCoupon" in qa

def method(source,signature):
    start=source.index(signature)
    opening=source.index("{",start)
    depth=0
    for i in range(opening,len(source)):
        if source[i]=="{": depth+=1
        elif source[i]=="}":
            depth-=1
            if depth==0: return source[start:i+1]
    raise ValueError("Unclosed original R3 method: "+signature)

signatures=[
    "public int RepairCost(R3PartState part)",
    "public bool Repair(Vector3Int cell)",
    "public bool RepairInventory(string id)",
    "public bool EmergencyRepair()",
]
methods=[method(src,x) for x in signatures]
original=[method(parent,x) for x in signatures]
assert src.count("((long)purchase*65L)/100L")==1
assert "Mathf.CeilToInt((1-part.Health)*purchase*0.35f)" in methods[0]
assert "price <= 0" not in methods[1]
assert "cost<=0" not in methods[2]
assert "RepairCost(p)<=0" not in methods[3]
assert "return Math.Max(1,Mathf.CeilToInt((1-part.Health)*purchase*0.35f));" in methods[0]
assert methods[0]!=original[0] and methods[1]!=original[1]
assert methods[2]!=original[2] and methods[3]!=original[3]
assert "SetRestorationGate" in src
assert (ROOT/OTHER).read_text(encoding="utf-8-sig").count("W32: paid-zero")==1

stub=r"""
using System;
using System.Collections.Generic;
using System.Linq;
enum R3PartKind { Armor, Wheel, Gun }
enum R4WeaponKind { Basic }
struct Vector3Int { public int X; public Vector3Int(int x){X=x;} }
class R3PartState {
 public string Id=Guid.NewGuid().ToString("N");
 public R3PartKind Kind;
 public R4WeaponKind Weapon=R4WeaponKind.Basic;
 public float Health;
 public Vector3Int Cell,RotationQuarterTurns;
 public int ContactMask=63;
 public R3PartState(R3PartKind kind,float hp,int cell=0) {
  Kind=kind;Health=hp;Cell=new Vector3Int(cell);
 }
}
static class Mathf {
 public static int CeilToInt(float value)=>(int)Math.Ceiling(value);
}
class R3VehicleModel {
 public int Scrap=0;
 public int NewPrice=5;
 public bool InvalidWeaponTypes=false;
 public bool Neighbour=true;
 public readonly List<R3PartState> Parts=new List<R3PartState>();
 public readonly List<R3PartState> inventory=new List<R3PartState>();
 public Func<R3PartState[],bool> restorationGate;
 public List<R3PartState> emergency;
 public int Price(R3PartKind k,R4WeaponKind w)=>NewPrice;
 public static bool ValidEconomicPart(R3PartState p)=>p!=null &&
     !float.IsNaN(p.Health)&&!float.IsInfinity(p.Health)&&p.Health>=0&&p.Health<=1;
 public R3PartState At(Vector3Int cell)=>Parts.FirstOrDefault(p=>p.Cell.X==cell.X);
 public bool HasAliveExternalNeighbour(Vector3Int cell,R3PartKind kind,Vector3Int rotation,int mask)=>Neighbour;
 private List<R3PartState> EmergencyRepairPlan()=>emergency;
 // The following bodies are extracted byte-for-byte from real Git source:
__EXTRACTED__
}
class Program {
 static int passes=0;
 static void Ok(bool yes,string msg){if(!yes)throw new Exception("ASSERT_RED: "+msg);passes++;}
 public static void Main(){
  int[] prices={1,5,15,30,100,20};
  int[] quotes={0,3,9,19,65,13};
  for(int i=0;i<prices.Length;i++){
   var m=new R3VehicleModel{NewPrice=prices[i]};
   Ok(m.RepairCost(new R3PartState(R3PartKind.Armor,0f))==quotes[i],"V7 floor65 price "+prices[i]);
  }
  var partial=new R3VehicleModel{NewPrice=20};
  Ok(partial.RepairCost(new R3PartState(R3PartKind.Armor,.5f))==4,"partial35ceil unchanged");
  Ok(partial.RepairCost(new R3PartState(R3PartKind.Armor,1f))==0,"healthy quote0");
  Ok(partial.RepairCost(null)==0,"null quote0");
  Ok(partial.RepairCost(new R3PartState(R3PartKind.Armor,float.NaN))==0,"invalid HP quote0");
  var inv=new R3VehicleModel{NewPrice=1};
  var owned=new R3PartState(R3PartKind.Wheel,0f);var id=owned.Id;inv.inventory.Add(owned);
  Ok(inv.RepairInventory(id),"inventory paid-zero can execute");
  Ok(inv.Scrap==0 && owned.Health==1f && id==owned.Id,"inventory wallet/HP/ID atomic");
  Ok(!inv.RepairInventory(id),"healthy inventory repeat denied");
  var mounted=new R3VehicleModel{NewPrice=1};
  var wreck=new R3PartState(R3PartKind.Armor,0f,12);mounted.Parts.Add(wreck);
  int called=0;mounted.restorationGate=parts=>{called++;return true;};
  Ok(mounted.Repair(wreck.Cell),"mounted paid-zero can execute");
  Ok(called==1 && mounted.Scrap==0 && wreck.Health==1f,"mounted has restoration authorization and no wallet debit");
  Ok(!mounted.Repair(wreck.Cell),"healthy mounted repeat denied");
  var deny=new R3VehicleModel{NewPrice=1};var blocked=new R3PartState(R3PartKind.Armor,0f,5);
  deny.Parts.Add(blocked);deny.restorationGate=parts=>false;
  Ok(!deny.Repair(blocked.Cell),"real restoration callback remains fail-closed");
  Ok(blocked.Health==0f && deny.Scrap==0,"no HP/wallet mutation on denied physical repair");
  var emerg=new R3VehicleModel{NewPrice=1};
  var essential=new R3PartState(R3PartKind.Wheel,0f);
  emerg.emergency=new List<R3PartState>{essential};emerg.restorationGate=parts=>true;
  Ok(emerg.EmergencyRepair(),"nonempty zero-quote emergency plan executes");
  Ok(essential.Health==1f&&emerg.Scrap==0,"emergency cost0 still paid and ID unchanged");
  Ok(!emerg.EmergencyRepair(),"healthy emergency plan repeat denied");
  var eDeny=new R3VehicleModel{NewPrice=1};
  var eBlock=new R3PartState(R3PartKind.Wheel,0f);
  eDeny.emergency=new List<R3PartState>{eBlock};eDeny.restorationGate=parts=>false;
  Ok(!eDeny.EmergencyRepair(),"emergency restoration gate denies");
  Ok(eDeny.Scrap==0 && eBlock.Health==0f,"no mutation on denied emergency");
  Console.WriteLine("ORIGINAL_R3_METHOD_SOURCE_ISOLATED_PASS checks="+passes);
 }
}
"""

project="""<Project Sdk="Microsoft.NET.Sdk">
<PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework>
<ImplicitUsings>disable</ImplicitUsings><Nullable>disable</Nullable><LangVersion>9.0</LangVersion>
</PropertyGroup></Project>"""

def execute(chunks,should_pass):
    with tempfile.TemporaryDirectory(prefix="b0123-w32-") as work:
        temp=Path(work)
        (temp/"SourceMethods.csproj").write_text(project,encoding="utf-8")
        (temp/"Program.cs").write_text(stub.replace("__EXTRACTED__","\n".join(chunks)),encoding="utf-8")
        env=os.environ.copy()
        env["DOTNET_CLI_TELEMETRY_OPTOUT"]="1"
        env["DOTNET_NOLOGO"]="1"
        env["NUGET_PACKAGES"]=str(temp/"packages")
        proc=subprocess.run(["dotnet","run","--project",str(temp/"SourceMethods.csproj"),
                             "-c","Release","--nologo"],
                            capture_output=True,text=True,encoding="utf-8",env=env,timeout=120)
        if should_pass:
            assert proc.returncode==0,(proc.stdout[-3000:],proc.stderr[-3000:])
            assert "ORIGINAL_R3_METHOD_SOURCE_ISOLATED_PASS" in proc.stdout,proc.stdout[-3000:]
            return proc.stdout.strip().splitlines()[-1]
        assert proc.returncode!=0 and "ASSERT_RED" in proc.stdout+proc.stderr,(
            "mutant not rejected",proc.returncode,proc.stdout[-2000:],proc.stderr[-2000:])
        return "MUTANT_REJECTED"

real=execute(methods,True)
bad1=methods.copy()
bad1[0]=bad1[0].replace("((long)purchase*65L)/100L","0")
assert bad1[0]!=methods[0]
bad2=methods.copy()
bad2[2]=bad2[2].replace("part.Health>=1f","cost<=0")
assert bad2[2]!=methods[2]
bad3=methods.copy()
bad3[3]=bad3[3].replace("p.Health>=1f","RepairCost(p)<=0")
assert bad3[3]!=methods[3]
for changed in [bad1,bad2,bad3]:
    execute(changed,False)
receipt={"kind":"SOURCE_ISOLATED_DOTNET8_NOT_UNITY","base":BASE,
         "head":git("rev-parse","HEAD"),"files":sorted(files),
         "original_production_methods":signatures,"actual_result":real,
         "mutants_rejected":3,"original_B0110_QA_unmodified":True,
         "Unity_EditMode":"NOT_RUN","Unity_PlayMode":"NOT_RUN",
         "Windows_Player":"NOT_BUILT"}
print(json.dumps(receipt,sort_keys=True,ensure_ascii=False))
